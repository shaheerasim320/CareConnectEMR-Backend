using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs;
using CareConnectEMR.Application.DTOs.Auth;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Enitites;
using CareConnectEMR.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService, AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;

        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByEmailAsync(request.Identifier) ?? await _userManager.FindByNameAsync(request.Identifier);

            if (user == null || !user.IsActive)
                return Result<AuthResponse>.Unauthorized();

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!isPasswordValid)
                return Result<AuthResponse>.Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            string? refreshToken = null;
            string? refreshTokenHash = null;

            if (request.RememberMe)
            {
                refreshToken = _tokenService.GenerateRefreshToken();
                refreshTokenHash = _tokenService.HashToken(refreshToken);
            }

            user.LastLoginAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            AuthResponse response = new()
            {
                AccessToken = accessToken,
                UserId = user.Id,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty

            };

            if (request.RememberMe)
            {
                var entity = new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = refreshTokenHash!,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    IpAddress = GetIpAddress(), 
                    DeviceInfo = GetDeviceInfo()
                };
                _context.RefreshTokens.Add(entity);
                await _context.SaveChangesAsync(ct);
                response.RefreshToken = refreshToken;
            }

            return Result<AuthResponse>.Ok(response);
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        {
            var hash = _tokenService.HashToken(request.RefreshToken);

            var token = await _context.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.TokenHash == hash);

            if (token == null || token.Revoked || token.ExpiresAt < DateTime.UtcNow)
                return Result<AuthResponse>.Unauthorized("Invalid refresh token");

            var user = token.User;

            var roles = await _userManager.GetRolesAsync(user);

            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var newHash = _tokenService.HashToken(newRefreshToken);

            token.Revoked = true;

            var newToken = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                ReplacedByTokenId = token.Id,
                IpAddress = GetIpAddress(),     
                DeviceInfo = GetDeviceInfo()

            };

            _context.RefreshTokens.Add(newToken);

            await _context.SaveChangesAsync(ct);

            return Result<AuthResponse>.Ok(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty
            });
        }

        public async Task<Result<bool>> LogoutAsync(LogoutRequest request, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
                return Result<bool>.Ok(true);

            var hash = _tokenService.HashToken(request.RefreshToken);

            var token = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

            if (token == null)
                return Result<bool>.Ok(true);

            token.Revoked = true;

            await _context.SaveChangesAsync(ct);

            return Result<bool>.Ok(true);
        }

        private string GetIpAddress()
        {
            if (_httpContextAccessor.HttpContext?.Request.Headers.ContainsKey("X-Forwarded-For") == true)
                return _httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"]!;

            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Unknown";
        }

        private string GetDeviceInfo()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown Device";
        }
    }
}
