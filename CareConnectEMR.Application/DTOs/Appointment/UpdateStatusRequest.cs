using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Appointment
{
    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? CancellationReason { get; set; }
        public bool? RequiresFollowUp { get; set; }
        public DateTime? FollowUpDate { get; set; }
    }
}
