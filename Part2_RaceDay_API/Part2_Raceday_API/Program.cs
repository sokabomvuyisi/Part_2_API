using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Part2_Raceday_API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RaceDay RESTful API - Organiser & Participant"
    });

    c.EnableAnnotations();
});

// Database
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ===== Create / update the database automatically on startup =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    try
    {
        // This creates the database + tables + seeds the Roles if they don't exist
        db.Database.EnsureCreated();
        Console.WriteLine("Database is ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("=== DATABASE CONNECTION ERROR ===");
        Console.WriteLine(ex.Message);
        Console.WriteLine("Check your connection string in appsettings.Development.json");
        // We still continue so you can at least open Swagger and see the error clearly
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
