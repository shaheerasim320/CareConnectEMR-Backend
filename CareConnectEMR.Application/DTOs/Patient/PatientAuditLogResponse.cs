using System.ComponentModel.DataAnnotations;

namespace CareConnectEMR.Application.DTOs.Patient
{
    public class PatientAuditLogResponse
    {
        public string Action { get; set; } = string.Empty;
        public string ChangedProperties { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}