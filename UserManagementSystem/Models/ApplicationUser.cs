using Microsoft.AspNetCore.Identity;

namespace UserManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public bool EmailVerified { get; set; } = false;
        public string EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiry { get; set; }

        // Navigation property
        public virtual ICollection<UserActivity> Activities { get; set; }
    }

    // For tracking user activities
    public class UserActivity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Activity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }

    // Defining application modules
    public class ApplicationModule
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Navigation property
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
    }

    // For role-based permissions on modules
    public class RolePermission
    {
        public int Id { get; set; }
        public string RoleId { get; set; }
        public int ModuleId { get; set; }
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }

        // Navigation properties
        public virtual ApplicationModule Module { get; set; }
    }
}
