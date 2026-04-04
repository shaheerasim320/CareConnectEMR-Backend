using CareConnectEMR.Application.DTOs.Appointment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Features.Appointment
{
    public static class AppointmentMapper
    {
        public static AppointmentResponse ToResponse(CareConnectEMR.Domain.Enitites.Appointment a) => new()
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
            PatientMRN = a.Patient.MRN,
            DoctorId = a.DoctorId,
            DoctorName = a.Doctor.FirstName + " " + a.Doctor.LastName,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            DurationMinutes = a.DurationMinutes, 
            Status = a.Status,
            Reason = a.Reason,
            Notes = a.Notes,
            CancellationReason = a.CancellationReason,
            CreatedAt = a.CreatedAt
        };
    }
}
