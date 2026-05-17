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
        private const string RefreshTokenCookieName = "refreshToken";

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(request, ct);

            if (result.IsSuccess && result.Data?.RefreshToken != null)
            {
                SetRefreshTokenCookie(result.Data.RefreshToken);
            }

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken(CancellationToken ct)
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];

            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "No refresh token cookie present." });

            var request = new RefreshTokenRequest { RefreshToken = refreshToken };
            var result = await _authService.RefreshTokenAsync(request, ct);

            if (result.IsSuccess && result.Data?.RefreshToken != null)
            {
                SetRefreshTokenCookie(result.Data.RefreshToken);
            }

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            var request = new LogoutRequest { RefreshToken = refreshToken ?? string.Empty };

            var result = await _authService.LogoutAsync(request, ct);

            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            return StatusCode(result.StatusCode, result);
        }

        private void SetRefreshTokenCookie(string token)
        {
            Response.Cookies.Append(RefreshTokenCookieName, token, new CookieOptions
            {
                HttpOnly = true,        
                Secure = true,         
                SameSite = SameSiteMode.None, 
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }
    }
}

