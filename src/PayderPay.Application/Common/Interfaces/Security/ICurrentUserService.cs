namespace PayderPay.Application.Common.Interfaces.Security;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
