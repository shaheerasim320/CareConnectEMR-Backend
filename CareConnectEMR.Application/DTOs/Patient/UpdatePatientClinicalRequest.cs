using System.ComponentModel.DataAnnotations;

namespace CareConnectEMR.Application.DTOs.Patient;

public class UpdatePatientClinicalRequest
{
    [StringLength(5)]
    public string? BloodType { get; set; }

    [StringLength(2000)]
    public string? Allergies { get; set; }
}
