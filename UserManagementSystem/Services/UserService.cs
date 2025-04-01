using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementSystem.Data;
using UserManagementSystem.DTOs;
using UserManagementSystem.Models;

namespace UserManagementSystem.Services
{
    public interface IUserService
    {
        Task<AuthResponseDto> RegisterUserAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto verifyDto);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(string id);
        Task<bool> UpdateUserAsync(string id, UpdateUserDto updateDto);
        Task<bool> DeleteUserAsync(string id);
        Task LogUserActivityAsync(string userId, string activity);
    }

    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IJwtService jwtService,
            IEmailService emailService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _context = context;
        }

        public async Task<AuthResponseDto> RegisterUserAsync(RegisterUserDto registerDto)
        {
            if (await _userManager.FindByEmailAsync(registerDto.Email) != null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email already registered."
                };
            }

            // Validate role
            if (registerDto.Role != "Admin" && registerDto.Role != "Manager" && registerDto.Role != "User")
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid role. Choose either Admin, Manager, or User."
                };
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                Name = registerDto.Name,
                EmailVerificationToken = GenerateRandomToken(),
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddDays(1)
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            // Create role if it doesn't exist
            if (!await _roleManager.RoleExistsAsync(registerDto.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(registerDto.Role));
            }

            // Assign role to user
            await _userManager.AddToRoleAsync(user, registerDto.Role);

            // Send verification email
            await _emailService.SendVerificationEmailAsync(user.Email, user.Name, user.EmailVerificationToken);

            await LogUserActivityAsync(user.Id, "User registered");

            return new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful. Please verify your email.",
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    EmailVerified = user.EmailVerified,
                    Role = registerDto.Role
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            if (!user.EmailVerified)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Please verify your email before logging in."
                };
            }

            if (!await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            var token = _jwtService.GenerateToken(user, role);

            await LogUserActivityAsync(user.Id, "User logged in");

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    EmailVerified = user.EmailVerified,
                    Role = role
                }
            };
        }

        public async Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto verifyDto)
        {
            var user = await _userManager.FindByEmailAsync(verifyDto.Email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            if (user.EmailVerified)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email already verified."
                };
            }

            if (user.EmailVerificationToken != verifyDto.Token ||
                user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired token."
                };
            }

            user.EmailVerified = true;
            user.EmailVerificationToken = verifyDto.Token;
            user.EmailVerificationTokenExpiry = null;

            if(user.EmailVerificationToken != null)
            {
                user.EmailConfirmed = true;
            }
           

            await _userManager.UpdateAsync(user);
            await LogUserActivityAsync(user.Id, "Email verified");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";
            var token = _jwtService.GenerateToken(user, role);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Email verification successful.",
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    EmailVerified = user.EmailVerified,
                    Role = role
                }
            };
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    EmailVerified = user.EmailVerified,
                    Role = role
                });
            }

            return userDtos;
        }

        public async Task<UserDto> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                EmailVerified = user.EmailVerified,
                Role = role
            };
        }

        public async Task<bool> UpdateUserAsync(string id, UpdateUserDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
            {
                user.Name = updateDto.Name;
            }

            if (!string.IsNullOrEmpty(updateDto.Email) && user.Email != updateDto.Email)
            {
                user.Email = updateDto.Email;
                user.UserName = updateDto.Email;
                user.EmailVerified = false;
                user.EmailVerificationToken = GenerateRandomToken();
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddDays(1);

                // Send new verification email
                await _emailService.SendVerificationEmailAsync(user.Email, user.Name, user.EmailVerificationToken);
            }

            var result = await _userManager.UpdateAsync(user);
            await LogUserActivityAsync(id, "User profile updated");

            return result.Succeeded;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.DeleteAsync(user);
            await LogUserActivityAsync(id, "User deleted");

            return result.Succeeded;
        }

        public async Task LogUserActivityAsync(string userId, string activity)
        {
            var userActivity = new UserActivity
            {
                UserId = userId,
                Activity = activity,
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivities.Add(userActivity);
            await _context.SaveChangesAsync();
        }

        private string GenerateRandomToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
