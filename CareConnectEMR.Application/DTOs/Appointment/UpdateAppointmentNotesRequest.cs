using System.ComponentModel.DataAnnotations;

namespace CareConnectEMR.Application.DTOs.Appointment;

public class UpdateAppointmentNotesRequest
{
    [Required, StringLength(2000)]
    public string Notes { get; set; } = string.Empty;
}
