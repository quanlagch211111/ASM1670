using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ASM1670.Models;
using ASM1670.Data;
using System.Security.Claims;

namespace ASM1670.Controllers
{

    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ASM1670Context _context;

        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, ASM1670Context context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ManageUserRole()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            ViewBag.CurrentUserRoles = currentUserRoles;

            var allRoles = await _roleManager.Roles.Where(r => r.Name != "Admin").Select(r => r.Name).ToListAsync();

            if (User.IsInRole("Admin"))
            {
                allRoles.Add("Admin");
            }

            ViewBag.AllRoles = allRoles;

            return View(currentUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateUserRole(string selectedRole)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (selectedRole == "Admin" && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var currentRoles = await _userManager.GetRolesAsync(currentUser);

            if (currentRoles.Contains(selectedRole))
            {
                return RedirectToAction("ManageUserRole");
            }

            await _userManager.RemoveFromRolesAsync(currentUser, currentRoles);

            await _userManager.AddToRoleAsync(currentUser, selectedRole);

            return RedirectToAction("ManageUserRole");
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RoleManagement()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            var users = _userManager.Users.ToList();

            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                var roles = _userManager.GetRolesAsync(user).Result;
                userRoles.Add(user.Id, roles.ToList());
            }
            ViewBag.UserRoles = userRoles;

            var userProfiles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var profile = _context.Profile.FirstOrDefault(p => p.UserId == user.Id);
                if (profile != null)
                {
                    userProfiles.Add(user.Id, profile.FullName);
                }
                else
                {
                    userProfiles.Add(user.Id, string.Empty);
                }
            }
            ViewBag.UserProfiles = userProfiles;

            return View(users.Where(u => u.Id != _userManager.GetUserId(User)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> UpdateRole(string userId, string selectedRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains(selectedRole))
            {
                return RedirectToAction("RoleManagement");
            }

            // Remove current roles
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            await _userManager.AddToRoleAsync(user, selectedRole);

            return RedirectToAction("RoleManagement");
        }

        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Index()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }
        [Authorize(Roles = "Admin")]

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            return View();
        }
        [Authorize(Roles = "Admin")]

        [HttpPost]
        public async Task<IActionResult> Create(IdentityRole model)
        {
            if (!await _roleManager.RoleExistsAsync(model.Name))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Name));
            }
            return RedirectToAction("Index");
        }
        [Authorize(Roles = "Admin")]

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }
        [Authorize(Roles = "Admin")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, IdentityRole model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var role = await _roleManager.FindByIdAsync(id);
                    if (role != null)
                    {
                        role.Name = model.Name;
                        var result = await _roleManager.UpdateAsync(role);
                        if (result.Succeeded)
                        {
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    return View(model);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
            }
            return View(model);
        }
        [Authorize(Roles = "Admin")]

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            return View(role);
        }

        [Authorize(Roles = "Admin")]

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            return NotFound();
        }

        public async Task<int> GetUnreadNotificationCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadCount = await _context.Notification.Where(n => n.UserId == userId && !n.IsRead).CountAsync();
            return unreadCount;
        }
    }
}
