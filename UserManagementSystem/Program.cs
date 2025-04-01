using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using UserManagementSystem.Data;
using UserManagementSystem.Models;
using UserManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure Identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
});

// Add Authentication and JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
        ClockSkew = TimeSpan.Zero
    };
});

// Register our services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });

    // Configure Swagger to use JWT Authorization
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedData(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

// Database seeding method
async Task SeedData(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
    // Ensure database is created
    context.Database.EnsureCreated();

    // Add default modules if they don't exist
    if (!context.Modules.Any())
    {
        var modules = new List<ApplicationModule>
        {
            new ApplicationModule { Name = "User Management" },
            new ApplicationModule { Name = "Asset Management" },
            new ApplicationModule { Name = "Reports" }
        };

        await context.Modules.AddRangeAsync(modules);
        await context.SaveChangesAsync();
    }

    // Create default roles if they don't exist
    string[] roleNames = { "Admin", "Manager", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Create default admin user if it doesn't exist
    var adminEmail = "admin@example.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            Name = "Admin User",
            EmailVerified = true // Admin is pre-verified
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    // Set up default permissions for roles
    await SetupDefaultPermissions(context, roleManager);
}

// Setup default permissions
async Task SetupDefaultPermissions(ApplicationDbContext context, RoleManager<IdentityRole> roleManager)
{
    var modules = await context.Modules.ToListAsync();

    foreach (var module in modules)
    {
        // Admin has all permissions
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole != null)
        {
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.ModuleId == module.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    ModuleId = module.Id,
                    CanCreate = true,
                    CanRead = true,
                    CanUpdate = true,
                    CanDelete = true
                });
            }
        }

        // Manager has create, read, update permissions
        var managerRole = await roleManager.FindByNameAsync("Manager");
        if (managerRole != null)
        {
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == managerRole.Id && rp.ModuleId == module.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = managerRole.Id,
                    ModuleId = module.Id,
                    CanCreate = true,
                    CanRead = true,
                    CanUpdate = true,
                    CanDelete = false
                });
            }
        }

        // User has only read permissions
        var userRole = await roleManager.FindByNameAsync("User");
        if (userRole != null)
        {
            if (!await context.RolePermissions.AnyAsync(rp => rp.RoleId == userRole.Id && rp.ModuleId == module.Id))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = userRole.Id,
                    ModuleId = module.Id,
                    CanCreate = false,
                    CanRead = true,
                    CanUpdate = false,
                    CanDelete = false
                });
            }
        }
    }

    await context.SaveChangesAsync();
}