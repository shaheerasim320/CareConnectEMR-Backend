using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.DTOs.Patient
{
    public class PatientListResponse
    {
        public Guid Id { get; set; }
        public string MRN { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? BloodType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
