using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using UserManagementSystem.Models;

namespace UserManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<ApplicationModule> Modules { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure existing relationships
            builder.Entity<UserActivity>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(ua => ua.UserId);

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Module)
                .WithMany(m => m.RolePermissions)
                .HasForeignKey(rp => rp.ModuleId);

            // Configure Product-Category relationship
            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        }
    }
}
