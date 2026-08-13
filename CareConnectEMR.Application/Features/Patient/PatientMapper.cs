using CareConnectEMR.Application.DTOs.Patient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Features.Patient
{
    public static class PatientMapper
    {
        public static PatientResponse ToResponse(CareConnectEMR.Domain.Enitites.Patient p) => new()
        {
            Id = p.Id,
            MRN = p.MRN ?? string.Empty,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Age = p.Age,
            Gender = p.Gender,
            PhoneNumber = p.PhoneNumber,
            Email = p.Email,
            BloodType = p.BloodType,
            Allergies = p.Allergies,
            EmergencyContactName = p.EmergencyContactName,
            EmergencyContactNumber = p.EmergencyContactNumber,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Status = p.Status
        };

        public static PatientListResponse ToListItem(CareConnectEMR.Domain.Enitites.Patient p) => new()
        {
            Id = p.Id,
            MRN = p.MRN ?? string.Empty,
            FullName = p.FullName,
            Age = p.Age,
            Gender = p.Gender,
            PhoneNumber = p.PhoneNumber,
            BloodType = p.BloodType,
            CreatedAt = p.CreatedAt,
            Status = p.Status
        };
    }
}
