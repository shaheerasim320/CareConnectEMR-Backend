using CareConnectEMR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CareConnectEMR.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController:ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboard(CancellationToken ct)
        {
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

            if (role == "Admin")
            {
                var result = await _dashboardService.GetAdminDashboardAsync(ct);
                return StatusCode(result.StatusCode, result);
            }

            if (role == "Doctor")
            {
                var result = await _dashboardService.GetDoctorDashboardAsync(userId, ct);
                return StatusCode(result.StatusCode, result);
            }

            if (role == "Receptionist")
            {
                var result = await _dashboardService.GetReceptionistDashboardAsync(ct);
                return StatusCode(result.StatusCode, result);
            }

            return StatusCode(403, new { message = "No dashboard configured for this role." });
        }
    }
}
