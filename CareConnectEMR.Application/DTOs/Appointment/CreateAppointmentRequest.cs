using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Appointment
{
    public class CreateAppointmentRequest
    {
        [Required]
        public Guid PatientId { get; set; }
        [Required]
        public string DoctorId { get; set; } = string.Empty;
        [Required]
        public DateTime StartTime { get; set; }
        [Required]
        public DateTime EndTime { get; set; }
        [StringLength(500)]
        public string? Reason { get; set; }
    }
}
