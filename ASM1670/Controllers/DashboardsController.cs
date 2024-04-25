using ASM1670.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ASM1670.Controllers
{
    public class DashboardsController : Controller
    {
        private readonly ASM1670Context _context;

        public DashboardsController( ASM1670Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.UnreadNotificationCount = await GetUnreadNotificationCount();
            return View();
        }

        public async Task<int> GetUnreadNotificationCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadCount = await _context.Notification.Where(n => n.UserId == userId && !n.IsRead).CountAsync();
            return unreadCount;
        }
    }
}
