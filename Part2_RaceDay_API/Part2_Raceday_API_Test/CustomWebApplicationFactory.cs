// =============================================================================
// CustomWebApplicationFactory.cs – Test host configuration
// =============================================================================
// This class is used by the integration tests.
// It replaces the real SQL Server database with an in-memory database so that:
//   1. Tests run fast
//   2. Tests do not touch the real development database
//   3. Each test run starts with a clean slate
//
// IMPORTANT: Seeding is done inside CreateHost (after the host is fully built).
// Doing it inside ConfigureServices is unreliable and was the cause of the
// earlier "Invalid role" 400 errors.
// =============================================================================

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Part2_Raceday_API.Data;
using Part2_Raceday_API.Models;

namespace RaceDay.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Called while the service collection is being built
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real SQL Server registration that Program.cs added
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Register an in-memory database instead
            // Using a fixed name so every test in the same factory shares the same DB
            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase("RaceDayTestDb");
            });
        });
    }

    // Called AFTER the host has been fully built – this is the reliable place to seed data
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        // Create the schema
        db.Database.EnsureCreated();

        // Seed the two roles that the whole application depends on
        // (InMemory databases do not always honour HasData the same way SQL Server does)
        if (!db.Roles.Any())
        {
            db.Roles.AddRange(
                new Role { RoleId = 1, Name = "Organiser" },
                new Role { RoleId = 2, Name = "Participant" }
            );
            db.SaveChanges();
        }

        return host;
    }
}
