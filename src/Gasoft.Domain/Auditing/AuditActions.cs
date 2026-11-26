using Gasoft.Domain.Notifications;

namespace Gasoft.Domain.Auditing;

public sealed record AuditActionDefinition(string Action, string Category, string Name);

public static class AuditActions
{
    public static IReadOnlyList<AuditActionDefinition> All { get; } =
    [
        new("Created", "General", "Created"),
        new("Updated", "General", "Updated"),
        new("Deleted", "General", "Deleted"),
        new("Posted", "General", "Posted"),
        new("StatusChanged", "Accounting", "Status changed"),
        new("LogoUpdated", "Company", "Logo updated"),
        new("ProfileUpdated", "Security", "Profile updated"),
        new("AuthenticationSucceeded", "Security", "Signed in"),
        new("AuthenticationFailed", "Security", "Sign-in failed"),
        new("PasswordReset", "Security", "Password reset"),
        new("PasswordChanged", "Security", "Password changed"),
        new("DataExported", "Administration", "Data exported"),
        .. NotificationEvents.All.Select(item =>
            new AuditActionDefinition(item.EventType, item.Category, item.Name))
    ];

    public static AuditActionDefinition? Find(string action) =>
        All.FirstOrDefault(item => item.Action == action);
}
