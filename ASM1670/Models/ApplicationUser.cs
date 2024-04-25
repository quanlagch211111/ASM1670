using Microsoft.AspNetCore.Identity;

namespace ASM1670.Models
{
    public class ApplicationUser : IdentityUser
    {
        public Profile? Profile { get; set; }
        public ICollection<JobApplication>? JobApplications { get; set; } 
        public ICollection<Job>? Job { get; set; } 
        public ICollection<Notification>? Notification { get; set; } 
        public ICollection<ChatMessage>? SentMessages { get; set; }
        public ICollection<ChatMessage>? ReceivedMessages { get; set; }

    }
}
