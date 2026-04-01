using CareConnectEMR.Application.DTOs.Patient;
using CareConnectEMR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnectEMR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Doctor")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
        => _patientService = patientService;

    [HttpGet]
    public async Task<IActionResult> GetPatients(
        [FromQuery] PatientQueryParameters parameters, CancellationToken ct)
    {
        var result = await _patientService.GetPatientsAsync(parameters, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPatientById(Guid id, CancellationToken ct)
    {
        var result = await _patientService.GetPatientByIdAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePatient(
        [FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var result = await _patientService.CreatePatientAsync(request, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdatePatient(
        Guid id, [FromBody] UpdatePatientRequest request, CancellationToken ct)
    {
        var result = await _patientService.UpdatePatientAsync(id, request, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePatient(Guid id, CancellationToken ct)
    {
        var result = await _patientService.DeletePatientAsync(id, ct);
        return StatusCode(result.StatusCode, result);
    }
}