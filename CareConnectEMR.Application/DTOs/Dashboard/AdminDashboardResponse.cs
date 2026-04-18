using CareConnectEMR.Application.DTOs.Dashboard.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Dashboard
{
    public class AdminDashboardResponse
    {
        public StatCard TotalPatients { get; set; } = new();
        public StatCard AppointmentsToday { get; set; } = new();
        public StatCard CompletedToday { get; set; } = new();
        public StatCard CancellationRate { get; set; } = new();

        public AppointmentBreakdown BreakdownToday { get; set; } = new();
        public List<DoctorLoad> TopDoctorsToday { get; set; } = [];
        public List<RecentPatient> RecentRegistrations { get; set; } = [];
    }

    public class AppointmentBreakdown
    {
        public int Scheduled { get; set; }
        public int Confirmed { get; set; }
        public int CheckedIn { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int NoShow { get; set; }
        public int Total => Scheduled + Confirmed + CheckedIn + Completed + Cancelled + NoShow;
    }

    public class DoctorLoad
    {
        public string DoctorId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int AppointmentCount { get; set; } 
        public int CompletedCount { get; set; }
    }

    public class RecentPatient
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MRN { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
