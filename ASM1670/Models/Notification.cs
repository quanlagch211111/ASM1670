namespace ASM1670.Models
{
    public class Notification
    {
        public int? NotificationId { get; set; }
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; } = false;
    }
}
