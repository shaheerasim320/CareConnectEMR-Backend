using CareConnectEMR.Application.DTOs.Dashboard.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Dashboard
{
    public class ReceptionistDashboardResponse
    {
        public StatCard AppointmentsToday { get; set; } = new();
        public StatCard CheckedInNow { get; set; }        
        public StatCard NewPatientsToday { get; set; } = new();

        public List<AppointmentQueueItem> TodayQueue { get; set; } = [];
    }

    public class AppointmentQueueItem
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientMRN { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
