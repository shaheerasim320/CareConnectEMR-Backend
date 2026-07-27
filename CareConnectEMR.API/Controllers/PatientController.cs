using CareConnectEMR.Application.DTOs.Patient;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareConnectEMR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin + "," + UserRoles.Doctor + "," + UserRoles.Receptionist)]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
        => _patientService = patientService;

    [HttpGet("list")]
    public async Task<IActionResult> GetPatients([FromQuery] PatientQueryParameters parameters, CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        PatientStatus? status = null;
        bool includeAll = false;

        if(role == UserRoles.Admin)
        {
            if (Enum.TryParse<PatientStatus>(Request.Query["Status"], out var parsedStatus))
            {
                status = parsedStatus;
                includeAll = false;
            }
            else
            {
                includeAll = true;
            }
        }
        var result = await _patientService.GetPatientsAsync(parameters, role, userId, status, includeAll, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("view/{id:guid}")]
    public async Task<IActionResult> GetPatientById(Guid id, CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _patientService.GetPatientByIdAsync(id, role, userId, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("register")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Receptionist)]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _patientService.CreatePatientAsync(request, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("contact/{id:guid}")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Receptionist)]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] UpdatePatientContactRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _patientService.UpdateContactAsync(id, request, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("identity/{id:guid}")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Receptionist)]
    public async Task<IActionResult> UpdateIdentity(Guid id, [FromBody] UpdatePatientIdentityRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _patientService.UpdateIdentityAsync(id, request, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("clinical/{id:guid}")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Doctor)]
    public async Task<IActionResult> UpdateClinical(Guid id, [FromBody] UpdatePatientClinicalRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _patientService.UpdateClinicalAsync(id, request, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("status/{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> UpdatePatientStatus(Guid id, [FromBody] UpdatePatientStatusRequest request, CancellationToken ct)
    {
        var result = await _patientService.UpdatePatientStatusAsync(id, request.Status, ct);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetPatientStats(CancellationToken ct)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _patientService.GetPatientStatsAsync(role, userId, ct);
        return StatusCode(result.StatusCode, result);
    }
}
