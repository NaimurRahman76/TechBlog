using Microsoft.AspNetCore.Identity;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces
{
    public interface IUserService
    {
        Task<(bool Succeeded, IEnumerable<IdentityError>? Errors)> CreateUserAsync(CreateUserDto userDto);
        Task<(bool Succeeded, IEnumerable<IdentityError>? Errors)> UpdateUserAsync(EditUserDto userDto);
        Task<bool> DeleteUserAsync(string userId);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<bool> ToggleAdminStatusAsync(string userId, bool isAdmin);
    }
}
