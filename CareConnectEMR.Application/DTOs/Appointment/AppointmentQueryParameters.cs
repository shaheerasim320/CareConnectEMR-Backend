using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Appointment
{
    public class AppointmentQueryParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        public int Page { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? DoctorId { get; set; }
        public Guid? PatientId { get; set; }
        public string? Status { get; set; }
        public DateTime? Date { get; set; }   
        public DateTime? From { get; set; }   
        public DateTime? To { get; set; }     
    }
}
