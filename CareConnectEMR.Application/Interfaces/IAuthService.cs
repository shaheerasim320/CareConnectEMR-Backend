using CareConnectEMR.Application.DTOs;
using CareConnectEMR.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CareConnectEMR.Application.DTOs.Auth;

namespace CareConnectEMR.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
        Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct);
        Task<Result<string>> LogoutAsync(string userId, CancellationToken ct);
        Task<Result<UserDetailRequest>> GetCurrentUserAsync(string userId, CancellationToken ct);
    }
}
