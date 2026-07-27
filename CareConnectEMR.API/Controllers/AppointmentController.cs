using CareConnectEMR.Application.DTOs.Appointment;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Enums;
using CareConnectEMR.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareConnectEMR.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Doctor + "," + UserRoles.Receptionist)]
    public class AppointmentController:ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        public AppointmentController(IAppointmentService appointmentService) => _appointmentService = appointmentService;

        [HttpGet("list")]
        public async Task<IActionResult> GetAppointments([FromQuery] AppointmentQueryParameters parameters, CancellationToken ct)
        {
            var result = await _appointmentService.GetAppointmentsAsync(parameters, User.FindFirstValue(ClaimTypes.Role) ?? string.Empty, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("view/{id:guid}")]
        public async Task<IActionResult> GetAppointmentById(Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.GetAppointmentByIdAsync(id, User.FindFirstValue(ClaimTypes.Role) ?? string.Empty, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("register")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Receptionist)]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request, CancellationToken ct)
        {
            if(request.PatientId == Guid.Empty)
                ModelState.AddModelError(nameof(request.PatientId), "PatientId is required.");

            if(!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _appointmentService.CreateAppointmentAsync(request, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("update/{id:guid}")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Receptionist)]
        public async Task<IActionResult> UpdateAppointment(Guid id, [FromBody] UpdateAppointmentRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);
            var result = await _appointmentService.UpdateAppointmentAsync(id, request, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("clinical-notes/{id:guid}")]
        [Authorize(Roles = UserRoles.Doctor)]
        public async Task<IActionResult> UpdateClinicalNotes(Guid id, [FromBody] UpdateAppointmentNotesRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var result = await _appointmentService.UpdateClinicalNotesAsync(id, request, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("status/{id:guid}")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.UpdateStatusAsync(id, request, User.FindFirstValue(ClaimTypes.Role) ?? string.Empty, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("cancel/{id:guid}")]
        [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Receptionist)]
        public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.CancelAppointmentAsync(id, request, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetAppointmentStats(CancellationToken ct)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _appointmentService.GetAppointmentStatsAsync(role, userId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
