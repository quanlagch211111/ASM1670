using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASM1670.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ASM1670.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ASM1670Context _context;

        public NotificationsController(ASM1670Context context)
        {
            _context = context;
        }

        // GET: Notifications
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notification.Where(n => n.UserId == currentUserId).ToListAsync();
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            return View(notifications);
        }

        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notification.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                _context.Notification.Update(notification);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _context.Notification.FindAsync(id);
            if (notification != null)
            {
                _context.Notification.Remove(notification);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        public async Task<int> GetUnreadNotificationCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadCount = await _context.Notification.Where(n => n.UserId == userId && !n.IsRead).CountAsync();
            return unreadCount;
        }


    }
}
