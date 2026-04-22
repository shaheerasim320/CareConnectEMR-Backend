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

            if (result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(result.Data!.RefreshToken))
                {
                    Response.Cookies.Append("refreshToken", result.Data.RefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                        Expires = DateTime.UtcNow.AddDays(7),
                        Path = "/"
                    });
                }
            }

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(CancellationToken ct)
        {
            var oldToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(oldToken)) return Unauthorized();

            var request = new RefreshTokenRequest { RefreshToken = oldToken };

            var result = await _authService.RefreshTokenAsync(request, ct);

            if(result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(result.Data!.RefreshToken))
                {
                    Response.Cookies.Append("refreshToken", result.Data.RefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsHttps,
                        SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                        Expires = DateTime.UtcNow.AddDays(7),
                        Path = "/"
                    });
                }
            }
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var refreshToken = Request.Cookies["refreshToken"];

            var result = await _authService.LogoutAsync(refreshToken, ct);
             Response.Cookies.Delete("refreshToken", new CookieOptions { Path = "/" });
             return StatusCode(result.StatusCode, result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var result = await _authService.GetMeAsync(userId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
