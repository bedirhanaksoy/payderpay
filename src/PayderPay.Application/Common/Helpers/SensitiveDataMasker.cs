namespace PayderPay.Application.Common.Helpers;

public static class SensitiveDataMasker
{
    /// <summary>
    /// Masks a person/full name keeping the first and last visible character of each token.
    /// Examples: "Ahmet Yılmaz" -> "A***t Y****z", "A" -> "*", "Mo" -> "M*".
    /// </summary>
    public static string MaskName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "***";
        }

        var tokens = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', tokens.Select(MaskToken));
    }

    /// <summary>
    /// Masks a subscriber/account number. Shows the first 2 and last 2 characters and replaces
    /// the middle with asterisks. For values shorter than 5 characters keeps a single visible
    /// character on each side.
    /// Examples: "12345678" -> "12****78", "1234" -> "1**4", "12" -> "**".
    /// </summary>
    public static string MaskSubscriberNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "***";
        }

        var trimmed = value.Trim();

        return trimmed.Length switch
        {
            <= 2 => new string('*', trimmed.Length),
            <= 4 => $"{trimmed[0]}{new string('*', trimmed.Length - 2)}{trimmed[^1]}",
            _ => $"{trimmed[..2]}{new string('*', trimmed.Length - 4)}{trimmed[^2..]}"
        };
    }

    private static string MaskToken(string token)
    {
        return token.Length switch
        {
            <= 1 => "*",
            2 => $"{token[0]}*",
            _ => $"{token[0]}{new string('*', token.Length - 2)}{token[^1]}"
        };
    }
}
