using CareConnectEMR.Application.DTOs.Auth;
using CareConnectEMR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CareConnectEMR.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(request, ct);            
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken ct)
        {
            var result = await _authService.RefreshTokenAsync(request, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(LogoutRequest request,CancellationToken ct)
        {
            var result = await _authService.LogoutAsync(request, ct);
             return StatusCode(result.StatusCode, result);
        }
    }
}
