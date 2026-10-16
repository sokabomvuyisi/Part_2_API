// =============================================================================
// Program.cs – Application entry point and service configuration
// =============================================================================
// This is the "startup" file of the RaceDay API.
// It is responsible for:
//   1. Registering all services (controllers, database, session, Swagger)
//   2. Building the HTTP request pipeline (middleware order matters!)
//   3. Creating the database automatically when the app starts
// =============================================================================

using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Part2_Raceday_API.Data;

// Create the web application builder – this is the modern .NET 6+ minimal hosting model
var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. REGISTER SERVICES (Dependency Injection container)
// -----------------------------------------------------------------------------

// Enable API controllers (our AuthController, EventsController, etc.)
builder.Services.AddControllers();

// Needed for Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger so we can test the API in the browser
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RaceDay RESTful API - Organiser & Participant"
    });

    // Allows us to use [SwaggerOperation] and [SwaggerTag] attributes on controllers
    c.EnableAnnotations();
});

// Register Entity Framework Core with SQL Server.
// The connection string is read from appsettings.json / appsettings.Development.json
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -----------------------------------------------------------------------------
// SESSION support (server-side authentication)
// -----------------------------------------------------------------------------
// We store UserId and Role in the session after a successful login.
// This is how we know "who is currently logged in" on later requests.
builder.Services.AddDistributedMemoryCache();   // In-memory store for session data
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Session expires after 60 min of inactivity
    options.Cookie.HttpOnly = true;                 // Cookie cannot be read by JavaScript (security)
    options.Cookie.IsEssential = true;              // Required even if user rejects non-essential cookies
});

// Build the application
var app = builder.Build();

// -----------------------------------------------------------------------------
// 2. CREATE / UPDATE THE DATABASE ON STARTUP
// -----------------------------------------------------------------------------
// EnsureCreated() will:
//   - Create the database if it does not exist
//   - Create all tables based on our models
//   - Insert the two seed roles (Organiser & Participant)
// This is convenient for development. In production you would normally use migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    try
    {
        db.Database.EnsureCreated();
        Console.WriteLine("Database is ready.");
    }
    catch (Exception ex)
    {
        // If SQL Server is not running or the connection string is wrong,
        // we still let the app start so the developer can open Swagger and see the error.
        Console.WriteLine("=== DATABASE CONNECTION ERROR ===");
        Console.WriteLine(ex.Message);
        Console.WriteLine("Check your connection string in appsettings.Development.json");
    }
}

// -----------------------------------------------------------------------------
// 3. HTTP REQUEST PIPELINE (middleware order is important)
// -----------------------------------------------------------------------------

// Only show Swagger UI when we are in Development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
        c.RoutePrefix = "swagger";   // Swagger will be available at /swagger
    });
}

app.UseHttpsRedirection();   // Redirect HTTP → HTTPS
app.UseSession();            // Enable session middleware (must be before MapControllers)
app.UseAuthorization();      // Although we use a custom filter, this is still required
app.MapControllers();        // Map all [ApiController] routes

app.Run();

// This partial class is required so that WebApplicationFactory (used in tests)
// can reference the Program class.
public partial class Program { }
