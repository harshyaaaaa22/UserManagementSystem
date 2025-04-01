using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagementSystem.DTOs;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPermissionService _permissionService;

        public UsersController(IUserService userService, IPermissionService permissionService)
        {
            _userService = userService;
            _permissionService = permissionService;
        }

        [HttpGet]
   
        public async Task<IActionResult> GetAllUsers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasPermission = await _permissionService.HasPermissionAsync(userId, "User Management", "read");

            if (!hasPermission)
            {
                return Forbid();
            }

            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            // Only admins can view other users
            if (!isAdmin && currentUserId != id)
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto updateDto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            // Only admins can update other users
            if (!isAdmin && currentUserId != id)
            {
                return Forbid();
            }

            var result = await _userService.UpdateUserAsync(id, updateDto);
            if (!result)
            {
                return BadRequest(new { Success = false, Message = "Failed to update user." });
            }

            return Ok(new { Success = true, Message = "User updated successfully." });
        }


        [HttpDelete("{id}")]
      
        public async Task<IActionResult> DeleteUser(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasPermission = await _permissionService.HasPermissionAsync(userId, "User Management", "delete");

            if (!hasPermission)
            {
                return Forbid();
            }

            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return BadRequest(new { Success = false, Message = "Failed to delete user." });
            }

            return Ok(new { Success = true, Message = "User deleted successfully." });
        }
    }
}
