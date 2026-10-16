// =============================================================================
// RaceDayDbContext.cs – Entity Framework Core database context
// =============================================================================
// This class is the bridge between our C# models and the SQL Server database.
// It tells EF Core:
//   - Which tables exist (DbSet properties)
//   - How the tables are related (foreign keys, navigation properties)
//   - Validation rules (required fields, max lengths, unique indexes)
//   - Seed data (the two fixed roles: Organiser and Participant)
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Part2_Raceday_API.Models;

namespace Part2_Raceday_API.Data;

public class RaceDayDbContext : DbContext
{
    // Constructor receives the options (connection string, provider, etc.)
    // that were configured in Program.cs
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    // -------------------------------------------------------------------------
    // DbSet properties = the tables in the database
    // -------------------------------------------------------------------------
    public DbSet<Role> Roles { get; set; }
    public DbSet<AppUser> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    // -------------------------------------------------------------------------
    // OnModelCreating – fluent configuration of the database schema
    // -------------------------------------------------------------------------
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Role table -----
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.RoleId);
            e.Property(r => r.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Name).IsUnique();   // Role names must be unique
        });

        // ----- AppUser table -----
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.UserId);
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();  // No two users can share the same email
            e.Property(u => u.PasswordHash).IsRequired();

            // Relationship: many users belong to one role
            // Restrict delete so we cannot accidentally delete a role that still has users
            e.HasOne(u => u.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ----- Event table -----
        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.EventId);
            e.Property(ev => ev.Name).IsRequired().HasMaxLength(200);
            e.Property(ev => ev.Description).HasMaxLength(2000);
            e.Property(ev => ev.Location).IsRequired().HasMaxLength(300);
            e.Property(ev => ev.EventType).IsRequired().HasMaxLength(50);
            e.Property(ev => ev.DistanceKm).HasPrecision(8, 2);  // e.g. 21.10 km

            // Relationship: each event belongs to one Organiser (AppUser)
            e.HasOne(ev => ev.Organiser)
             .WithMany(u => u.OrganisedEvents)
             .HasForeignKey(ev => ev.OrganiserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ----- Category table -----
        // A category belongs to exactly one event (e.g. "Under 20", "Senior", "21 km")
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.Property(c => c.Description).HasMaxLength(500);

            // Cascade delete: if an event is deleted, its categories are also deleted
            e.HasOne(c => c.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(c => c.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- Enrolment table -----
        // Links a Participant to an Event + Category
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

            // Business rule: a participant can only enrol once in the same event
            e.HasIndex(en => new { en.EventId, en.ParticipantId }).IsUnique();
        });

        // ----- Result table -----
        // One-to-one (or zero) with Enrolment – a participant may or may not have a result yet
        modelBuilder.Entity<Result>(e =>
        {
            e.HasKey(r => r.ResultId);

            e.HasOne(r => r.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(r => r.EnrolmentId)
             .OnDelete(DeleteBehavior.Cascade);

            // Guarantee that each enrolment has at most one result
            e.HasIndex(r => r.EnrolmentId).IsUnique();
        });

        // -------------------------------------------------------------------------
        // SEED DATA – the two roles that the whole system depends on
        // -------------------------------------------------------------------------
        // These rows are inserted when the database is first created.
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, Name = "Organiser" },
            new Role { RoleId = 2, Name = "Participant" }
        );
    }
}
