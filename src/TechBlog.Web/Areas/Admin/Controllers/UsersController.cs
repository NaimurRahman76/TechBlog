using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechBlog.Core.DTOs;
using TechBlog.Core.Entities;
using TechBlog.Core.Interfaces;

namespace TechBlog.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                var list = users.ToList();
                var total = list.Count;
                var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = total;
                return View(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                TempData["ErrorMessage"] = "An error occurred while retrieving users.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            return View(new CreateUserDto());
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserDto model)
        {
            if (!ModelState.IsValid)
            return View(model);

            try
            {
                var (succeeded, errors) = await _userService.CreateUserAsync(model);
                if (succeeded)
                {
                    TempData["SuccessMessage"] = "User created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the user.");
            }

            return View(model);
        }

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var editUserDto = new EditUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                IsAdmin = user.IsAdmin
            };

            return View(editUserDto);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserDto model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var (succeeded, errors) = await _userService.UpdateUserAsync(model);
                if (succeeded)
                {
                    TempData["SuccessMessage"] = "User updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the user.");
            }

            return View(model);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not delete the user. They may be the last admin.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Users/ToggleAdmin/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id, bool isAdmin)
        {
            try
            {
                var result = await _userService.ToggleAdminStatusAsync(id, isAdmin);
                if (result)
                {
                    TempData["SuccessMessage"] = isAdmin 
                        ? "User granted admin privileges." 
                        : "User's admin privileges have been revoked.";
                }
                else
                {
                    TempData["ErrorMessage"] = isAdmin
                        ? "Failed to grant admin privileges. The user may not exist."
                        : "Cannot revoke admin privileges from the last admin user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling admin status for user {UserId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating user's admin status.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
