using CareConnectEMR.Application.Common;
using CareConnectEMR.Application.DTOs.User;
using CareConnectEMR.Application.Features.User;
using CareConnectEMR.Application.Interfaces;
using CareConnectEMR.Domain.Enitites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Infrastructure.Services
{
    public class UserService:IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
        {
            var user = new ApplicationUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.UserName,
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded) return Result<UserResponse>.Fail(result.Errors.First().Description);

            var roleExists = await _roleManager.RoleExistsAsync(request.Role);

            if(!roleExists) return Result<UserResponse>.Fail($"Role '{request.Role}' does not exist.");

            await _userManager.AddToRoleAsync(user, request.Role);

            return Result<UserResponse>.Created(UserMapper.ToResponse(user, request.Role));

        }


        public async Task<Result<PagedResult<UserResponse>>> GetUsersAsync(UserQueryParameters parameters, CancellationToken ct)
        {
            if (parameters.Page < 1) parameters.Page = 1;

            var query = _userManager.Users.AsNoTracking();

            if (parameters.IsActive.HasValue)
                query = query.Where(u => u.IsActive == parameters.IsActive.Value);

            if (!string.IsNullOrEmpty(parameters.Search))
            {
                var search = parameters.Search.ToLower().Trim();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(search) ||
                    u.LastName.ToLower().Contains(search) ||
                    u.Email!.ToLower().Contains(search));
            }

            var totalItems = await query.CountAsync(ct);

            var users = await query
                .OrderBy(u => u.FirstName)
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync(ct);

            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.FirstOrDefault() ?? string.Empty;
            }

            var items = UserMapper.ToListResponse(users, userRoles);

            return Result<PagedResult<UserResponse>>.Ok(new PagedResult<UserResponse>
            {
                Items = items,
                TotalCount = totalItems,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            });
        }

        public async Task<Result<UserResponse>> GetUserByIdAsync(string userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if(user==null) return Result<UserResponse>.NotFound($"User with ID {userId} not found.");

            var roles = await _userManager.GetRolesAsync(user);

            return Result<UserResponse>.Ok(UserMapper.ToResponse(user, roles.FirstOrDefault() ?? "User"));
        }

        public async Task<Result<UserResponse>> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            
            if(user==null) return Result<UserResponse>.NotFound($"User with ID {userId} not found.");

            bool isChanged = false;

            if(request.FirstName!=null && request.FirstName != user.FirstName) { user.FirstName = request.FirstName; isChanged = true; }
            if(request.LastName!=null && request.LastName != user.LastName) { user.LastName = request.LastName; isChanged = true; }
            if(request.UserName!=null && request.UserName != user.UserName) { user.UserName = request.UserName; isChanged = true; }

            if(!isChanged && request.Role==null) return Result<UserResponse>.Fail("No changes detected.");

            var updateResult = await _userManager.UpdateAsync(user);
            if(!updateResult.Succeeded) return Result<UserResponse>.Fail(updateResult.Errors.First().Description);

            if(request.Role!=null)
            {
                var roleExists = await _roleManager.RoleExistsAsync(request.Role);
                if (!roleExists) return Result<UserResponse>.Fail($"Role '{request.Role}' does not exist.");
                var currentRoles = await _userManager.GetRolesAsync(user);
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded) return Result<UserResponse>.Fail(removeResult.Errors.First().Description);
                var addResult = await _userManager.AddToRoleAsync(user, request.Role);
                if (!addResult.Succeeded) return Result<UserResponse>.Fail(addResult.Errors.First().Description);
            }

            return Result<UserResponse>.Ok(UserMapper.ToResponse(user, request.Role ?? (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User"));
        }

        public async Task<Result<string>> ResetPasswordAsync(string userId, ResetPasswordRequest request, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Result<string>.NotFound($"User with ID {userId} not found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
                return Result<string>.Fail(result.Errors.First().Description);

            return Result<string>.Ok("Password reset successfully");
        }

        public async Task<Result<string>> DeleteUserAsync(string userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Result<string>.NotFound($"User with ID {userId} not found.");

            user.IsActive = false;

            await _userManager.UpdateAsync(user);

            return Result<string>.Ok("User deactivated successfully");
        }
    }
}
