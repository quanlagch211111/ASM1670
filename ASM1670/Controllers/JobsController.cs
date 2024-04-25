using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ASM1670.Data;
using ASM1670.Models;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ASM1670.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ASM1670.Controllers
{
    public class JobsController(ASM1670Context context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> notificationHub) : Controller
    {
        private readonly ASM1670Context _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly IHubContext<NotificationHub> _notificationHub = notificationHub;

        public async Task<IActionResult> Index(string category)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            IQueryable<Job> jobsQuery = _context.Job.Where(j => j.Status == "Approved").Include(j => j.Category).Include(j => j.Employer).ThenInclude(j => j.Profile);
            var categories = await _context.Category.ToListAsync();
            ViewBag.Categories = categories;

            if (!string.IsNullOrEmpty(category))
            {
                jobsQuery = jobsQuery.Where(j => j.Category.Name == category);
            }
           
            var jobs = await jobsQuery.ToListAsync();
            if (jobs.Count == 0)
            {
                ViewBag.NoJobs = true;
            }
            else
            {
                ViewBag.NoJobs = false;
            }
            return View(jobs);
        }


        [Authorize]
        public async Task<IActionResult> Manage()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            // Lấy UserId của người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy danh sách công việc thuộc về người dùng hiện tại
            var userJobs = await _context.Job.Where(j => j.EmployerId == userId).Include(j=>j.Category).ToListAsync();

            return View(userJobs);
        }

        // GET: Jobs/PendingJobs
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingJobs()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var pendingJobs = _context.Job.Where(j => j.Status == "Pending").Include(j => j.Employer).ThenInclude(j=>j.Profile).Include(j => j.Category);
            return View(pendingJobs);
        }

        // POST: Jobs/ApproveJob/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveJob(int id)
        {
            var job = await _context.Job.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            job.Status = "Approved";
            _context.Job.Update(job);
            await _context.SaveChangesAsync();
            await CreateNotification(job.EmployerId, "Your job has been approved.");
            return RedirectToAction(nameof(PendingJobs));
        }

        // POST: Jobs/RejectJob/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectJob(int id)
        {
            var job = await _context.Job.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            // Xóa job khỏi database
            _context.Job.Remove(job);
            await _context.SaveChangesAsync();
            await CreateNotification(job.EmployerId, "Your job has been rejected.");

            return RedirectToAction(nameof(PendingJobs));
        }

        // GET: Jobs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Job
                .Include(j => j.Employer).Include(j => j.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // GET: Jobs/Create
        [Authorize(Roles = "Admin,Employers")]
        public async Task<IActionResult> Create()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "Name");

            return View();
        }

        // POST: Jobs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employers")]
        public async Task<IActionResult> Create([Bind("Id,EmployerId,Title,Description,ProfessionalQualifications,Salary,Location,DeadLine,JobImage, Status, CategoryId")] Job job)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            job.EmployerId = currentUserId;
            job.Status = "Pending";
            if (ModelState.IsValid)
            {
                if (job.JobImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + job.JobImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await job.JobImage.CopyToAsync(fileStream);
                    }
                    job.JobPicture = "/images/" + uniqueFileName;
                }
                _context.Add(job);
                await _context.SaveChangesAsync();
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    await CreateNotification(admin.Id, "A new job is pending for approval.");
                }
                return RedirectToAction(nameof(Index));
            }
            return View(job);
        }

        // GET: Jobs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Job.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (job.EmployerId != currentUserId)
            {
                return Forbid();
            }
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "Name");
            return View(job);
        }

        // POST Job Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("Id,EmployerId,Title,Description,ProfessionalQualifications,Salary,Location,DeadLine,JobImage, CategoryId")] Job job)
        {
            if (id == null || id != job.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var jobToUpdate = await _context.Job.FindAsync(id);
                    if (jobToUpdate == null)
                    {
                        return NotFound();
                    }

                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (jobToUpdate.EmployerId != currentUserId)
                    {
                        return Forbid();
                    }

                    jobToUpdate.Title = job.Title;
                    jobToUpdate.Description = job.Description;
                    jobToUpdate.ProfessionalQualifications = job.ProfessionalQualifications;
                    jobToUpdate.Location = job.Location;
                    jobToUpdate.Salary = job.Salary;
                    jobToUpdate.DeadLine = job.DeadLine;

                    // Cập nhật ảnh nếu có
                    if (job.JobImage != null)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + job.JobImage.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await job.JobImage.CopyToAsync(fileStream);
                        }
                        jobToUpdate.JobPicture = "/images/" + uniqueFileName;
                    }

                    _context.Update(jobToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.Id))
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
            return View(job);
        }

        // GET: Jobs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Job
                .Include(j => j.Employer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            // Kiểm tra xem có đơn ứng tuyển nào liên quan đến công việc không
            var applicationsCount = await _context.JobApplication
                .CountAsync(a => a.JobId == id);

            if (applicationsCount > 0)
            {
                return RedirectToAction(nameof(Index));
            }
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (job.EmployerId != currentUserId)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(job);
        }

        // POST: Jobs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Job.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            var applicationsCount = await _context.JobApplication
                .CountAsync(a => a.JobId == id);

            if (applicationsCount > 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (job.EmployerId != currentUserId)
            {
                return RedirectToAction(nameof(Index));
            }

            _context.Job.Remove(job);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchQuery)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var jobs = await _context.Job.Where(j => j.Title.Contains(searchQuery) && j.Status != "Pending")
                .Include(j => j.Category)
                .Include(j => j.Employer)
                .ThenInclude(j => j.Profile)
                .ToListAsync();
                .ToListAsync();

            if (jobs.Count == 0)
            {
                ViewBag.NoJobs = true;
            }
            else
            {
                ViewBag.NoJobs = false;
            }

            return View("Index", jobs);
        }


        private async Task CreateNotification(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message
            };

            _context.Notification.Add(notification);
            await _context.SaveChangesAsync();

            await _notificationHub.Clients.User(userId).SendAsync("ReceiveNotification", message);

        }

        public async Task<int> GetUnreadNotificationCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadCount = await _context.Notification.Where(n => n.UserId == userId && !n.IsRead).CountAsync();
            return unreadCount;
        }
        private bool JobExists(int? id)
        {
            return _context.Job.Any(e => e.Id == id);
        }
    }
}
