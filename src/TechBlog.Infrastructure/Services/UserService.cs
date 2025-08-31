using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;

namespace TechBlog.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<(bool Succeeded, IEnumerable<IdentityError>? Errors)> CreateUserAsync(CreateUserDto userDto)
        {
            var user = new ApplicationUser
            {
                UserName = userDto.Email,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Bio = userDto.Bio,
                ProfileImageUrl = userDto.ProfileImageUrl,
                EmailConfirmed = true // Auto-confirm email for admin-created users
            };

            var result = await _userManager.CreateAsync(user, userDto.Password);

            if (result.Succeeded && userDto.IsAdmin)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                _logger.LogInformation("Created new admin user with email {Email}", userDto.Email);
            }
            else if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                _logger.LogInformation("Created new user with email {Email}", userDto.Email);
            }

            return (result.Succeeded, result.Succeeded ? null : result.Errors);
        }

        public async Task<(bool Succeeded, IEnumerable<IdentityError>? Errors)> UpdateUserAsync(EditUserDto userDto)
        {
            var user = await _userManager.FindByIdAsync(userDto.Id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for update", userDto.Id);
                return (false, new[] { new IdentityError { Description = "User not found." } });
            }

            // Update user properties
            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.Bio = userDto.Bio;
            user.ProfileImageUrl = userDto.ProfileImageUrl;

            // Update email if changed
            if (user.Email != userDto.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, userDto.Email);
                if (!setEmailResult.Succeeded)
                {
                    _logger.LogWarning("Failed to update email for user {UserId}", userDto.Id);
                    return (false, setEmailResult.Errors);
                }
                
                var setUsernameResult = await _userManager.SetUserNameAsync(user, userDto.Email);
                if (!setUsernameResult.Succeeded)
                {
                    _logger.LogWarning("Failed to update username for user {UserId}", userDto.Id);
                    return (false, setUsernameResult.Errors);
                }
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(userDto.NewPassword))
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    _logger.LogWarning("Failed to remove password for user {UserId}", userDto.Id);
                    return (false, removePasswordResult.Errors);
                }

                var addPasswordResult = await _userManager.AddPasswordAsync(user, userDto.NewPassword);
                if (!addPasswordResult.Succeeded)
                {
                    _logger.LogWarning("Failed to set new password for user {UserId}", userDto.Id);
                    return (false, addPasswordResult.Errors);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update user {UserId}", userDto.Id);
                return (false, updateResult.Errors);
            }

            // Update admin role if needed
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (userDto.IsAdmin && !isAdmin)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                _logger.LogInformation("Added admin role to user {UserId}", userDto.Id);
            }
            else if (!userDto.IsAdmin && isAdmin)
            {
                // Prevent removing the last admin
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count == 1 && adminUsers[0].Id == user.Id)
                {
                    _logger.LogWarning("Cannot remove the last admin user {UserId}", userDto.Id);
                    return (false, new[] { new IdentityError { Description = "Cannot remove the last admin user." } });
                }
                
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                _logger.LogInformation("Removed admin role from user {UserId}", userDto.Id);
            }

            return (true, null);
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for deletion", userId);
                return false;
            }

            // Prevent deleting the last admin
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count == 1 && adminUsers[0].Id == userId)
                {
                    _logger.LogWarning("Cannot delete the last admin user {UserId}", userId);
                    return false;
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted user with ID {UserId}", userId);
                return true;
            }

            _logger.LogWarning("Failed to delete user {UserId}: {Errors}", 
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Bio = user.Bio,
                    ProfileImageUrl = user.ProfileImageUrl,
                    IsAdmin = roles.Contains("Admin")
                });
            }

            return userDtos;
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                IsAdmin = roles.Contains("Admin")
            };
        }

        public async Task<bool> ToggleAdminStatusAsync(string userId, bool isAdmin)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            if (isAdmin)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                _logger.LogInformation("Added admin role to user {UserId}", userId);
            }
            else
            {
                // Prevent removing the last admin
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                if (adminUsers.Count == 1 && adminUsers[0].Id == userId)
                {
                    _logger.LogWarning("Cannot remove the last admin user {UserId}", userId);
                    return false;
                }
                
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                _logger.LogInformation("Removed admin role from user {UserId}", userId);
            }

            return true;
        }
    }
}
