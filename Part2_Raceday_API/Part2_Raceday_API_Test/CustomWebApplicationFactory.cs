using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real SQL Server DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Add InMemory database for tests
            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase("RaceDayTestDb_" + Guid.NewGuid());
            });

            // Ensure the database is created and seeded
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            db.Database.EnsureCreated();

            // Seed roles if not present (InMemory may not run HasData the same way)
            if (!db.Roles.Any())
            {
                db.Roles.AddRange(
                    new Role { RoleId = 1, Name = "Organiser" },
                    new Role { RoleId = 2, Name = "Participant" }
                );
                db.SaveChanges();
            }
        });
    }
}