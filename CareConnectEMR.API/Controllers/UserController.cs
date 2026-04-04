using CareConnectEMR.Application.DTOs.User;
using CareConnectEMR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareConnectEMR.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
        {
            var result = await _userService.CreateUserAsync(request, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUsers([FromQuery] UserQueryParameters parameters, CancellationToken ct)
        {
            var result = await _userService.GetUsersAsync(parameters, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("view/{id}")]
        public async Task<IActionResult> GetUserById(string id, CancellationToken ct)
        {
            var result = await _userService.GetUserByIdAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("update/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        {
            var result = await _userService.UpdateUserAsync(id, request, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("reset-password/{id}")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            var result = await _userService.ResetPasswordAsync(id, request, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(string id, CancellationToken ct)
        {
            var result = await _userService.DeleteUserAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
