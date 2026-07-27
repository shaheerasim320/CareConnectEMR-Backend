using System.ComponentModel.DataAnnotations;

namespace CareConnectEMR.Application.DTOs.Patient;

public class UpdatePatientIdentityRequest
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
