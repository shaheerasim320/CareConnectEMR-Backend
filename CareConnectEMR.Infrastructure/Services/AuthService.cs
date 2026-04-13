using CareConnectEMR.Application.DTOs;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Application.Common;
using CareConnectEMR.Domain.Enitites;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CareConnectEMR.Application.DTOs.Auth;

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

        public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
        {
            var userId = _tokenService.GetUserIdFromExpiredToken(request.AccessToken);
            if (userId == null)
                return Result<AuthResponse>.Unauthorized("Invalid access token");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
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

        public async Task<Result<string>> LogoutAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result<string>.NotFound("User not found");

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _userManager.UpdateAsync(user);
            return Result<string>.Ok("Logged out successfully");
        }

        public async Task<Result<UserDetailRequest>> GetCurrentUserAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result<UserDetailRequest>.NotFound("User not found");
            var roles = await _userManager.GetRolesAsync(user);
            var userDetail = new UserDetailRequest
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Role = roles.FirstOrDefault() ?? string.Empty
            };
            return Result<UserDetailRequest>.Ok(userDetail);
        }
    }
}
