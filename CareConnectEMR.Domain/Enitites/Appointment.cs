using CareConnectEMR.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Domain.Enitites
{
    public class Appointment : IAuditable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public Guid PatientId { get; set; }
        [Required]
        public string DoctorId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = AppointmentStatus.Scheduled;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public Patient Patient { get; set; } = null!;
        public ApplicationUser Doctor { get; set; } = null!;
        public bool RequiresFollowUp { get; set; } = false;
        public DateTime? FollowUpDate { get; set; }
        public int DurationMinutes =>
        (int)(EndTime - StartTime).TotalMinutes;
    }
}
