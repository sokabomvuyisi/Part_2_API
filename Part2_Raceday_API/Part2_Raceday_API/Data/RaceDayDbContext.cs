using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles { get; set; }
    public DbSet<AppUser> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Role
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.RoleId);
            e.Property(r => r.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Name).IsUnique();
        });

        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();

            e.HasOne(u => u.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Event
        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.EventId);
            e.Property(ev => ev.Name).IsRequired().HasMaxLength(200);
            e.Property(ev => ev.Description).HasMaxLength(2000);
            e.Property(ev => ev.Location).IsRequired().HasMaxLength(300);
            e.Property(ev => ev.EventType).IsRequired().HasMaxLength(50);
            e.Property(ev => ev.DistanceKm).HasPrecision(8, 2);

            e.HasOne(ev => ev.Organiser)
             .WithMany(u => u.OrganisedEvents)
             .HasForeignKey(ev => ev.OrganiserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Category
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.Property(c => c.Description).HasMaxLength(500);

            e.HasOne(c => c.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(c => c.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Enrolment
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.HasKey(en => en.EnrolmentId);
            e.Property(en => en.Status).IsRequired().HasMaxLength(50);

            e.HasOne(en => en.Event)
             .WithMany(ev => ev.Enrolments)
             .HasForeignKey(en => en.EventId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(en => en.Category)
             .WithMany(c => c.Enrolments)
             .HasForeignKey(en => en.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(en => en.Participant)
             .WithMany(u => u.Enrolments)
             .HasForeignKey(en => en.ParticipantId)
             .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate enrolment of same participant in same event
            e.HasIndex(en => new { en.EventId, en.ParticipantId }).IsUnique();
        });

        // Result (1-to-0/1 with Enrolment)
        modelBuilder.Entity<Result>(e =>
        {
            e.HasKey(r => r.ResultId);

            e.HasOne(r => r.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(r => r.EnrolmentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => r.EnrolmentId).IsUnique();
        });

        // Seed roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, Name = "Organiser" },
            new Role { RoleId = 2, Name = "Participant" }
        );
    }
}