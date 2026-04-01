using CareConnectEMR.Application.DTOs.Users;
using CareConnectEMR.Domain.Enitites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareConnectEMR.Application.Features.User
{
    public static class UserMapper
    {
        public static UserResponse ToResponse(ApplicationUser u, string role) => new()
        {
            Id = u.Id,
            FullName=u.FullName,
            UserName = u.UserName!,
            Email = u.Email!,
            Role = role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };

        public static List<UserResponse> ToListResponse(IEnumerable<ApplicationUser> users, Dictionary<string, string> roles)
        {
            return users.Select(user => new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = roles.ContainsKey(user.Id) ? roles[user.Id] : "",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            }).ToList();
        }
    }
}
