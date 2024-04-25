using ASM1670.Data;
using ASM1670.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ASM1670.Hubs
{
   
    public class NotificationHub : Hub
    {
        private readonly ASM1670Context _context;

        public NotificationHub(ASM1670Context context)
        {
            _context = context;
        }
        public async Task SendNotification(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);

        }
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
        }


    }
}
