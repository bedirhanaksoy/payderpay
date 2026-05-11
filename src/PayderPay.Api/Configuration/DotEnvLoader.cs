using System.Text;

namespace PayderPay.Api.Configuration;

internal static class DotEnvLoader
{
    public static void LoadFromRepositoryRoot()
    {
        LoadFileIfExists(".env");

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        LoadFileIfExists($".env.{environment}");
    }

    private static void LoadFileIfExists(string fileName)
    {
        var filePath = FindFileUpwards(fileName);
        if (filePath is null)
        {
            return;
        }

        foreach (var line in File.ReadLines(filePath, Encoding.UTF8))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[7..].Trim();
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindFileUpwards(string fileName)
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
