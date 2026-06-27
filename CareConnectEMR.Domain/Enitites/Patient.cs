using CareConnectEMR.Domain.Common;
using CareConnectEMR.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Domain.Enitites
{
    public class Patient : IAuditable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? MRN { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public PatientStatus Status { get; set; } = PatientStatus.Active;

        public string FullName => $"{FirstName} {LastName}";
        public int Age =>
        DateOnly.FromDateTime(DateTime.UtcNow).Year - DateOfBirth.Year -
        (DateOnly.FromDateTime(DateTime.UtcNow) < DateOfBirth.AddYears(DateOnly.FromDateTime(DateTime.UtcNow).Year - DateOfBirth.Year) ? 1 : 0);

    }
}
