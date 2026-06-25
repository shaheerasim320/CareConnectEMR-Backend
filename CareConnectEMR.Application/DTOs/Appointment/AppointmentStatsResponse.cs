using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Appointment
{
    public class AppointmentStatsResponse
    {
        public int? TotalToday { get; set; }
        public int? CompletedToday { get; set; }
        public int? CancelledOrNoShowToday { get; set; }
        public int? AppointmentsToday { get; set; }
        public int? RemainingToday { get; set; }
    }
}
