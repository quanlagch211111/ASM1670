using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASM1670.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ASM1670.Models;
using ASM1670.Hubs;
using Microsoft.AspNetCore.SignalR;


namespace ASM1670.Controllers
{
    public class ChatMessagesController : Controller
    {
        private readonly ASM1670Context _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ChatMessagesController(ASM1670Context context, IHubContext<NotificationHub> hubContext )
        {
            _context = context;
            _hubContext = hubContext;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var users = await _context.ApplicationUser.Include(m=>m.Profile).ToListAsync();
            return View(users);
        }
        [Authorize]
        public async Task<IActionResult> Chat(string receiverId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == currentUserId))
                .Include(m => m.Sender).ThenInclude(u => u.Profile).Include(m => m.Receiver).ThenInclude(u => u.Profile)
                .ToListAsync();
            ViewBag.ReceiverId = receiverId;
            var receiver = _context.Users.Include(u => u.Profile).FirstOrDefault(u => u.Id == receiverId);
            ViewBag.Receiver = receiver;

            return View(messages);
        }



        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendMessage(string receiverId, string message)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                Timestamp = DateTime.Now
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);

            return RedirectToAction("Chat", new { receiverId = receiverId });
        }


    }
}
