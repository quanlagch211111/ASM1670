using System.ComponentModel.DataAnnotations.Schema;

namespace ASM1670.Models
{
    public enum ApplicationStatus
    {
        Pending,
        Accepted,
        Processing,
        Finished,
        Rejected,
        Canceled
    }
    public class JobApplication
    {
        public int? JobApplicationId { get; set; }
        public string CoverLetter { get; set; } = "";
        public int? JobId { get; set; }
        public Job? Job { get; set; }
        public string? ApplicantId { get; set; }
        public ApplicationUser? Applicant { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public string? CVPicture { get; set; }
        [NotMapped]
        public IFormFile? CVImage { get; set; }
    }
}
