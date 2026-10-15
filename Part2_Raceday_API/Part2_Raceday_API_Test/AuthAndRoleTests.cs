using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthAndRoleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthAndRoleTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true // important for session
        });
    }

    // ========== AUTHENTICATION ==========

    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        var request = new RegisterRequest
        {
            FirstName = "Alice",
            LastName = "Organiser",
            Email = $"alice{Guid.NewGuid():N}@test.com",
            Password = "Password123",
            Role = "Organiser"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

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

    // ========== ROLE ENFORCEMENT ==========

    [Fact]
    public async Task Unauthenticated_AccessProtectedEndpoint_Returns401()
    {
        // Fresh client with no session
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
        // Create organiser + event first
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

        // Add a category
        var catReq = new CreateCategoryRequest { Name = "Open" };
        var catResp = await _client.PostAsJsonAsync($"/api/events/{eventDto!.EventId}/categories", catReq);
        catResp.EnsureSuccessStatusCode();
        var catDto = await catResp.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions);

        // Now login as participant and enrol
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
        var email = $"org3{Guid.NewGuid():N}@test.com";
        await RegisterAndLogin(email, "Password123", "Organiser");

        var enrol = new CreateEnrolmentRequest { EventId = 1, CategoryId = 1 };
        var response = await _client.PostAsJsonAsync("/api/enrolments", enrol);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ========== HELPERS ==========

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

    private async Task RegisterAndLogin(string email, string password, string role)
    {
        await RegisterUser(email, password, role);
        var login = new LoginRequest { Email = email, Password = password };
        var resp = await _client.PostAsJsonAsync("/api/auth/login", login);
        resp.EnsureSuccessStatusCode();
    }
}