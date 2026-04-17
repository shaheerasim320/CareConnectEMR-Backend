using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs;
using CareConnectEMR.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
        Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct);
        Task<Result<bool>> LogoutAsync(string? refreshToken, CancellationToken ct);
        Task<Result<MeResponse>> GetMeAsync(string userId, CancellationToken ct);
    }
}
