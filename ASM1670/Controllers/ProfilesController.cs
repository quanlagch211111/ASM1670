using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ASM1670.Data;
using ASM1670.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace ASM1670.Controllers
{
    public class ProfilesController(ASM1670Context context, IWebHostEnvironment webHostEnvironment) : Controller
    {
        private readonly ASM1670Context _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

        public async Task<IActionResult> Index()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var aSM1670Context = _context.Profile.Include(p => p.User);
            return View(await aSM1670Context.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile.Include(p=>p.User)
                .FirstOrDefaultAsync(m => m.ProfileId == id);
            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        public async Task<IActionResult> MyCvProfile()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (User.Identity.IsAuthenticated)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var profile = await _context.Profile
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(m => m.UserId == userId);

                if (profile != null)
                {
                    return View("Details", profile);
                }
                else
                {
                    return RedirectToAction("Create", "Profiles");
                }
            }
            else
            {
                return Forbid();
            }
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingProfile = _context.Profile.FirstOrDefault(p => p.UserId == userId);
            if (existingProfile != null)
            {
                return RedirectToAction("Edit", new { id = existingProfile.ProfileId });
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProfileId,UserId,FullName,Description,Address,Skill,Experience,ProfileImage")] Profile profile)
        {
            if (ModelState.IsValid)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                profile.UserId = userId;
                if (profile.ProfileImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + profile.ProfileImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profile.ProfileImage.CopyToAsync(fileStream);
                    }
                    profile.ProfilePicture = "/images/" + uniqueFileName;
                }
                _context.Add(profile);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (profile.UserId != userId)
            {
                return Forbid();
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProfileId,UserId,FullName,Description,Address,Skill,Experience, ProfileImage")] Profile profile)
        {

            if (id != profile.ProfileId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    profile.UserId = currentUserId;
                    var profileToUpdate = await _context.Profile.FindAsync(id);
                    if (profileToUpdate == null)
                    {
                        return NotFound();
                    }

                    profileToUpdate.UserId = profile.UserId;
                    profileToUpdate.FullName = profile.FullName;
                    profileToUpdate.Address = profile.Address;
                    profileToUpdate.Description = profile.Description;
                    profileToUpdate.Skill = profile.Skill;
                    profileToUpdate.Experience = profile.Experience;

                    if (profile.ProfileImage != null)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + profile.ProfileImage.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await profile.ProfileImage.CopyToAsync(fileStream);
                        }
                        profileToUpdate.ProfilePicture = "/images/" + uniqueFileName;
                    }

                    _context.Update(profileToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProfileExists(profile.ProfileId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(profile);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.Profile
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProfileId == id);
            if (profile == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (profile.UserId != userId)
            {
                return Forbid();
            }

            return View(profile);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.Profile.FindAsync(id);
            if (profile != null)
            {
                _context.Profile.Remove(profile);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<int> GetUnreadNotificationCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadCount = await _context.Notification.Where(n => n.UserId == userId && !n.IsRead).CountAsync();
            return unreadCount;
        }
        private bool ProfileExists(int id)
        {
            return _context.Profile.Any(e => e.ProfileId == id);
        }
    }
}
