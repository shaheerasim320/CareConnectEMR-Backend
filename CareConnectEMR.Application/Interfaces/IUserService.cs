using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct);
        Task<Result<PagedResult<UserResponse>>> GetUsersAsync(UserQueryParameters parameters, CancellationToken ct);
        Task<Result<UserResponse>> GetUserByIdAsync(string userId, CancellationToken ct);
        Task<Result<UserResponse>> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken ct);
        Task<Result<string>> ResetPasswordAsync(string userId, ResetPasswordRequest request, CancellationToken ct);
        Task<Result<string>> DeleteUserAsync(string userId, CancellationToken ct);

    }
}
