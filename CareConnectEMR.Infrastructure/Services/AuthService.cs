using CareConnectEMR.Application.DTOs;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Common;
using CareConnectEMR.Domain.Enitites;
using Microsoft.AspNetCore.Identity;
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

        public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Identifier) ?? await _userManager.FindByNameAsync(request.Identifier);

            if (user == null)
                return Result<AuthResponse>.Unauthorized();

            if (user.IsDeleted)
                return Result<AuthResponse>.Fail("User account is inactive", 403);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

            if (!isPasswordValid)
                return Result<AuthResponse>.Unauthorized();

            user.LastLoginAt = DateTime.UtcNow;

            var roles = await _userManager.GetRolesAsync(user);

            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            string? refreshToken = null;

            if (request.RememberMe)
            {
                refreshToken = _tokenService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            }

            user.LastLoginAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            AuthResponse response;

            response = new AuthResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty

            };

            if (request.RememberMe)
            {
                response.RefreshToken = refreshToken!;
            }

            return Result<AuthResponse>.Ok(response);
        }

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var userId = _tokenService.GetUserIdFromExpiredToken(request.accessToken);
            if (userId == null)
                return Result<AuthResponse>.Unauthorized("Invalid access token");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.RefreshToken != request.refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Result<AuthResponse>.Unauthorized("Refresh token expired or invalid");
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return Result<AuthResponse>.Ok(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty
            });
        }

        public async Task<Result<string>> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result<string>.NotFound("User not found");
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(user);
            return Result<string>.Ok("Logged out successfully");
        }
    }
}
