using CareConnectEMR.Application.DTOs.Dashboard.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Dashboard
{
    public class DoctorDashboardResponse
    {
        public StatCard MyAppointmentsToday { get; set; } = new();
        public StatCard MyCompletedToday { get; set; } = new();
        public StatCard TotalPatientsSeen { get; set; }

        public NextAppointmentDto? NextAppointment { get; set; }
        public List<TodayScheduleItem> TodaySchedule { get; set; } = [];

    }

    public class NextAppointmentDto
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientMRN { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class TodayScheduleItem
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientMRN { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
