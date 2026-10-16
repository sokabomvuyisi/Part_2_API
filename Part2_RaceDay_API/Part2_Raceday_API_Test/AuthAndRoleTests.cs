// =============================================================================
// AuthAndRoleTests.cs – Integration tests for authentication and role security
// =============================================================================
// These tests spin up a real (in-memory) version of the API using
// CustomWebApplicationFactory and make real HTTP calls.
//
// What we verify:
//   1. Registration works for both roles and rejects invalid roles
//   2. Login succeeds with correct credentials and fails with wrong ones
//   3. Unauthenticated requests receive 401
//   4. Role restrictions work (Participant cannot create events, etc.)
//   5. A full happy-path enrolment flow works
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

// IClassFixture means one factory (and therefore one in-memory DB) is shared
// across all tests in this class – faster and more realistic.
public class AuthAndRoleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    // Case-insensitive JSON so property names match regardless of camelCase/PascalCase
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthAndRoleTests(CustomWebApplicationFactory factory)
    {
        // HandleCookies = true is CRITICAL because our auth is session-based.
        // Without it the session cookie would be ignored between requests.
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    // =========================================================================
    // AUTHENTICATION TESTS
    // =========================================================================

    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        // Arrange – unique email so the test can be run many times
        var request = new RegisterRequest
        {
            FirstName = "Alice",
            LastName = "Organiser",
            Email = $"alice{Guid.NewGuid():N}@test.com",
            Password = "Password123",
            Role = "Organiser"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Organiser", body.Role);
        Assert.Equal(request.Email.ToLower(), body.Email);
    }

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var request = new RegisterRequest
        {
            FirstName = "Bob",
            LastName = "Runner",
            Email = $"bob{Guid.NewGuid():N}@test.com",
            Password = "Password123",
            Role = "Participant"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        // "Admin" is not a valid role in our system
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = $"invalid{Guid.NewGuid():N}@test.com",
            Password = "Password123",
            Role = "Admin"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_AndSetsSession()
    {
        var email = $"login{Guid.NewGuid():N}@test.com";
        await RegisterUser(email, "Password123", "Participant");

        var login = new LoginRequest { Email = email, Password = "Password123" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        Assert.Equal("Participant", body!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"wrong{Guid.NewGuid():N}@test.com";
        await RegisterUser(email, "Password123", "Participant");

        var login = new LoginRequest { Email = email, Password = "WrongPassword" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", login);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // ROLE ENFORCEMENT TESTS
    // =========================================================================

    [Fact]
    public async Task Unauthenticated_AccessProtectedEndpoint_Returns401()
    {
        // No login has been performed, so the session is empty
        var response = await _client.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var email = $"part{Guid.NewGuid():N}@test.com";
        await RegisterAndLogin(email, "Password123", "Participant");

        var create = new CreateEventRequest
        {
            Name = "Illegal Event",
            Description = "Should fail",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Cape Town",
            DistanceKm = 10,
            EventType = "Run"
        };

        // Participant is logged in but does not have the Organiser role
        var response = await _client.PostAsJsonAsync("/api/events", create);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var email = $"org{Guid.NewGuid():N}@test.com";
        await RegisterAndLogin(email, "Password123", "Organiser");

        var create = new CreateEventRequest
        {
            Name = "Cape Town 10K",
            Description = "Annual city run",
            EventDate = DateTime.UtcNow.AddDays(45),
            Location = "Cape Town",
            DistanceKm = 10,
            EventType = "Run"
        };

        var response = await _client.PostAsJsonAsync("/api/events", create);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanEnrol_Returns201()
    {
        // ----- Step 1: Organiser creates an event -----
        var orgEmail = $"org2{Guid.NewGuid():N}@test.com";
        await RegisterAndLogin(orgEmail, "Password123", "Organiser");

        var createEvent = new CreateEventRequest
        {
            Name = "Test Race",
            Description = "For enrolment test",
            EventDate = DateTime.UtcNow.AddDays(20),
            Location = "Johannesburg",
            DistanceKm = 21,
            EventType = "Run"
        };
        var eventResp = await _client.PostAsJsonAsync("/api/events", createEvent);
        eventResp.EnsureSuccessStatusCode();
        var eventDto = await eventResp.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);

        // ----- Step 2: Organiser adds a category -----
        var catReq = new CreateCategoryRequest { Name = "Open" };
        var catResp = await _client.PostAsJsonAsync($"/api/events/{eventDto!.EventId}/categories", catReq);
        catResp.EnsureSuccessStatusCode();
        var catDto = await catResp.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions);

        // ----- Step 3: Participant enrols -----
        var partEmail = $"part2{Guid.NewGuid():N}@test.com";
        await RegisterAndLogin(partEmail, "Password123", "Participant");

        var enrol = new CreateEnrolmentRequest
        {
            EventId = eventDto.EventId,
            CategoryId = catDto!.CategoryId
        };

        var enrolResp = await _client.PostAsJsonAsync("/api/enrolments", enrol);
        Assert.Equal(HttpStatusCode.Created, enrolResp.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        // Organisers are not allowed to enrol in events
        var email = $"org3{Guid.NewGuid():N}@test.com";
        await RegisterAndLogin(email, "Password123", "Organiser");

        var enrol = new CreateEnrolmentRequest { EventId = 1, CategoryId = 1 };
        var response = await _client.PostAsJsonAsync("/api/enrolments", enrol);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // =========================================================================
    // HELPER METHODS (keep the tests themselves short and readable)
    // =========================================================================

    /// <summary>Registers a new user. Throws if registration fails.</summary>
    private async Task RegisterUser(string email, string password, string role)
    {
        var request = new RegisterRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = password,
            Role = role
        };
        var resp = await _client.PostAsJsonAsync("/api/auth/register", request);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>Registers a user and immediately logs them in (sets the session cookie).</summary>
    private async Task RegisterAndLogin(string email, string password, string role)
    {
        await RegisterUser(email, password, role);
        var login = new LoginRequest { Email = email, Password = password };
        var resp = await _client.PostAsJsonAsync("/api/auth/login", login);
        resp.EnsureSuccessStatusCode();
    }
}
