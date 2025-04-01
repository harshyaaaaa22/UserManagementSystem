using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementSystem.DTOs;
using UserManagementSystem.Services;

namespace UserManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            var permissions = await _permissionService.GetAllRolePermissionsAsync();
            return Ok(permissions);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRolePermission(RolePermissionDto permissionDto)
        {
            var result = await _permissionService.UpdateRolePermissionAsync(permissionDto);
            if (!result)
            {
                return BadRequest(new { Success = false, Message = "Failed to update role permission." });
            }

            return Ok(new { Success = true, Message = "Role permission updated successfully." });
        }
    }
}
