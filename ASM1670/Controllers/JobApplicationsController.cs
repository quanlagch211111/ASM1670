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
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using ASM1670.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ASM1670.Controllers
{
    public class JobApplicationsController : Controller
    {
        private readonly ASM1670Context _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;


        public JobApplicationsController(ASM1670Context context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        // GET: JobApplications
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Index()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var aSM1670Context = _context.JobApplication.Include(j => j.Applicant).ThenInclude(j => j.Profile).Include(j => j.Job);
            return View(await aSM1670Context.ToListAsync());
        }
        // GET: JobApplications/Applications
        [Authorize(Roles = "Employers")]

        public async Task<IActionResult> Applications()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var jobs = await _context.Job.Where(j => j.EmployerId == currentUserId).ToListAsync();

            var jobApplications = await _context.JobApplication
                .Include(j => j.Applicant).ThenInclude(j=>j.Profile)
                .ToListAsync(); 

            jobApplications = jobApplications
                .Where(ja => jobs.Any(job => job.Id == ja.JobId))
                .ToList(); 

            return View(jobApplications);
        }


        // GET: JobApplications/MyApplications
        [Authorize(Roles = "Job Seekers")]
        public async Task<IActionResult> MyApplications()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var jobApplications = await _context.JobApplication
                .Include(j => j.Job)
                .Where(ja => ja.ApplicantId == currentUserId)
                .ToListAsync();

            return View(jobApplications);
        }


        // GET: JobApplications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var jobApplication = await _context.JobApplication
                .Include(j => j.Applicant)
                .ThenInclude(j => j.Profile)
                .Include(j => j.Job)
                .FirstOrDefaultAsync(m => m.JobApplicationId == id);
            if (jobApplication == null)
            {
                return NotFound();
            }

            return View(jobApplication);
        }

        // GET: JobApplications/Create
        public async Task<IActionResult> Create(int? jobId)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (jobId == null)
            {
                return RedirectToAction("Index", "Jobs");
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var job = await _context.Job.FindAsync(jobId);

            var existingApplication = await _context.JobApplication.FirstOrDefaultAsync(j => j.JobId == jobId && j.ApplicantId == currentUserId);
            if (existingApplication != null)
            {
                TempData["Message"] = "You have already applied for this job.";
                return RedirectToAction("MyApplications");
            }
            ViewData["ApplicantId"] = new SelectList(_context.ApplicationUser, "Id", "UserName");
            ViewData["JobId"] = jobId;

            return View();
        }

        // POST: JobApplications/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int jobId, [Bind("JobApplicationId,CoverLetter,ApplicantId,CVImage")] JobApplication jobApplication)
        {
            if (ModelState.IsValid)
            {
                if (jobApplication.CVImage != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + jobApplication.CVImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await jobApplication.CVImage.CopyToAsync(fileStream);
                    }
                    jobApplication.CVPicture = "/images/" + uniqueFileName;
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                jobApplication.ApplicantId = currentUserId;
                jobApplication.JobId = jobId;

                _context.Add(jobApplication);
                await _context.SaveChangesAsync();

                if (jobApplication.JobId != null)
                {
                    var job = await _context.Job.FindAsync(jobApplication.JobId);
                    if (job != null)
                    {
                        await CreateNotification(job.EmployerId, "You have a new job application.");
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction("MyApplications");
            }

            return View(jobApplication);
        }

        // GET: JobApplications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var jobApplication = await _context.JobApplication.FindAsync(id);
            if (jobApplication == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (jobApplication.ApplicantId != currentUserId)
            {
                return Forbid();
            }

            return View(jobApplication);
        }

        // POST: JobApplications/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, [Bind("JobApplicationId,CoverLetter,ApplicantId,CVImage")] JobApplication jobApplication)
        {
            if (id == null || id != jobApplication.JobApplicationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var jobApplicationToUpdate = await _context.JobApplication.FindAsync(id);
                    if (jobApplicationToUpdate == null)
                    {
                        return NotFound();
                    }

                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (jobApplicationToUpdate.ApplicantId != currentUserId)
                    {
                        return Forbid();
                    }

                    jobApplicationToUpdate.CoverLetter = jobApplication.CoverLetter;

                    if (jobApplication.CVImage != null)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + jobApplication.CVImage.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await jobApplication.CVImage.CopyToAsync(fileStream);
                        }
                        jobApplicationToUpdate.CVPicture = "/images/" + uniqueFileName;
                    }

                    _context.Update(jobApplicationToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobApplicationExists(jobApplication.JobApplicationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("MyApplications");
            }
            return View(jobApplication);
        }

        // GET: JobApplications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            if (id == null)
            {
                return NotFound();
            }

            var jobApplication = await _context.JobApplication
                .Include(j => j.Applicant)
                .Include(j => j.Job)
                .FirstOrDefaultAsync(m => m.JobApplicationId == id);
            if (jobApplication == null)
            {
                return NotFound();
            }

            return View(jobApplication);
        }

        // POST: JobApplications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var jobApplication = await _context.JobApplication.FindAsync(id);
            if (jobApplication != null)
            {
                _context.JobApplication.Remove(jobApplication);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("MyApplications");
        }
        // POST: JobApplications/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var jobApplication = await _context.JobApplication.FindAsync(id);
            if (jobApplication == null)
            {
                return NotFound();
            }

            jobApplication.Status = ApplicationStatus.Accepted;
            _context.Update(jobApplication);
            await _context.SaveChangesAsync();

            await CreateNotification(jobApplication.ApplicantId, "Your job application has been accepted.");

            return RedirectToAction("Applications");
        }

        // POST: JobApplications/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var jobApplication = await _context.JobApplication.FindAsync(id);
            if (jobApplication == null)
            {
                return NotFound();
            }

            jobApplication.Status = ApplicationStatus.Rejected;
            _context.Update(jobApplication);
            await _context.SaveChangesAsync();

            await CreateNotification(jobApplication.ApplicantId, "Your job application has been rejected.");

            return RedirectToAction("Applications");
        }

        // POST: JobApplications/ChangeStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, ApplicationStatus status)
        {
            var jobApplication = await _context.JobApplication.FindAsync(id);
            if (jobApplication == null)
            {
                return NotFound();
            }

            jobApplication.Status = status;
            _context.Update(jobApplication);
            await _context.SaveChangesAsync();

            var message = $"Your job application status has been changed to {status}.";
            await CreateNotification(jobApplication.ApplicantId, message);

            if (@User.IsInRole != null)
            {
                if (@User.IsInRole("Employers"))
                {
                    return RedirectToAction("Applications");
                }
                else if (@User.IsInRole("Job Seekers"))
                {
                    return RedirectToAction("MyApplications");
                }
            }
            return RedirectToAction(nameof(Index));
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
        private bool JobApplicationExists(int? id)
        {
            return _context.JobApplication.Any(e => e.JobApplicationId == id);
        }
    }
}
