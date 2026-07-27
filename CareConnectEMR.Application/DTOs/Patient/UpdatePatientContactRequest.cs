using System.ComponentModel.DataAnnotations;

namespace CareConnectEMR.Application.DTOs.Patient;

public class UpdatePatientContactRequest
{
    [StringLength(15)]
    public string? PhoneNumber { get; set; }

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? EmergencyContactName { get; set; }

    [StringLength(15)]
    public string? EmergencyContactNumber { get; set; }
}
