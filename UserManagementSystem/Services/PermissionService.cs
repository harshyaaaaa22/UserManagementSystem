using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.DTOs;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(string userId, string moduleName, string permission);
        Task<IEnumerable<RolePermissionDto>> GetAllRolePermissionsAsync();
        Task<bool> UpdateRolePermissionAsync(RolePermissionDto permissionDto);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;

        public PermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(string userId, string moduleName, string permission)
        {
            // Get the user's role from the AspNetUserRoles table
            var userRole = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(userRole))
                return false;

            // Get the module ID
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleName);

            if (module == null)
                return false;

            // Check the permission for this role and module
            var roleId = await _context.Roles
                .Where(r => r.Name == userRole)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(roleId))
                return false;

            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.ModuleId == module.Id);

            if (rolePermission == null)
                return false;

            // Check specific permission
            switch (permission.ToLower())
            {
                case "create":
                    return rolePermission.CanCreate;
                case "read":
                    return rolePermission.CanRead;
                case "update":
                    return rolePermission.CanUpdate;
                case "delete":
                    return rolePermission.CanDelete;
                default:
                    return false;
            }
        }

        public async Task<IEnumerable<RolePermissionDto>> GetAllRolePermissionsAsync()
        {
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Module)
                .Join(_context.Roles,
                    rp => rp.RoleId,
                    r => r.Id,
                    (rp, r) => new RolePermissionDto
                    {
                        Role = r.Name,
                        Module = rp.Module.Name,
                        CanCreate = rp.CanCreate,
                        CanRead = rp.CanRead,
                        CanUpdate = rp.CanUpdate,
                        CanDelete = rp.CanDelete
                    })
                .ToListAsync();

            return rolePermissions;
        }

        public async Task<bool> UpdateRolePermissionAsync(RolePermissionDto permissionDto)
        {
            // Get the role ID
            var roleId = await _context.Roles
                .Where(r => r.Name == permissionDto.Role)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(roleId))
                return false;

            // Get the module ID
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == permissionDto.Module);

            if (module == null)
                return false;

            // Find existing permission or create new one
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.ModuleId == module.Id);

            if (rolePermission == null)
            {
                rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    ModuleId = module.Id
                };
                _context.RolePermissions.Add(rolePermission);
            }

            // Update permissions
            rolePermission.CanCreate = permissionDto.CanCreate;
            rolePermission.CanRead = permissionDto.CanRead;
            rolePermission.CanUpdate = permissionDto.CanUpdate;
            rolePermission.CanDelete = permissionDto.CanDelete;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
