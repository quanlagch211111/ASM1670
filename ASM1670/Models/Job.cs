using Microsoft.AspNetCore.Builder;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASM1670.Models
{
    public class Job
    {
        public int? Id { get; set; }
        public string? EmployerId { get; set; }
        public ApplicationUser? Employer { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ProfessionalQualifications { get; set; }
        public decimal? Salary { get; set; }
        public string? Location { get; set; }
        public DateTime? DeadLine { get; set; }
        public string? JobPicture { get; set; }
        [NotMapped]
        public IFormFile? JobImage { get; set; }
        public ICollection<JobApplication>? JobApplications { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? Status { get; set; }
    }
}
