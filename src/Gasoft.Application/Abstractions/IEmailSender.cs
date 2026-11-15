namespace Gasoft.Application.Abstractions;

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken);
}
