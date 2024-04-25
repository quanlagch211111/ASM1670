using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM1670.Models
{
    public class Profile
    {
        public int ProfileId { get; set; }
        public string? UserId { get; set; }
        public string? Description { get; set; }
        public ApplicationUser? User { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string Skill { get; set; } = "";
        public string? ProfilePicture { get; set; }
        public string? Experience { get; set; }
        [NotMapped]
        public IFormFile? ProfileImage { get; set; }
    }
}
