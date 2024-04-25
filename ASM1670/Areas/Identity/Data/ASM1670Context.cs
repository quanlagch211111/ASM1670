using ASM1670.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace ASM1670.Data;

public class ASM1670Context : IdentityDbContext<ApplicationUser>
{
    public ASM1670Context(DbContextOptions<ASM1670Context> options)
        : base(options)
    {
    }

    public DbSet<Job> Job { get; set; } = default!;

    public DbSet<JobApplication> JobApplication { get; set; }

    public DbSet<Profile> Profile { get; set; }

    public DbSet<Notification> Notification { get; set; }


    public DbSet<ApplicationUser> ApplicationUser { get; set; }

    public DbSet<Category> Category { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<Profile>(p => p.UserId);
        builder.Entity<ChatMessage>()
           .HasOne(m => m.Sender)
           .WithMany(u => u.SentMessages)
           .HasForeignKey(m => m.SenderId)
           .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChatMessage>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
