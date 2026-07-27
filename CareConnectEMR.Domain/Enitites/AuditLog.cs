namespace CareConnectEMR.Domain.Enitites;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedProperties { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Reason { get; set; }
    public string? UserId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RequestPath { get; set; }
    public string? IpAddress { get; set; }
}
