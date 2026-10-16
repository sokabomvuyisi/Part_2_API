// =============================================================================
// Dtos.cs – Data Transfer Objects (request & response models)
// =============================================================================
// Why do we use DTOs instead of returning the EF Core entities directly?
//
//  1. Security  – we never expose PasswordHash or internal navigation properties
//  2. Flexibility – the API shape can stay stable even if the database changes
//  3. Clarity   – the client only sees the fields it actually needs
//
// Naming convention used in this project:
//   XxxRequest  → data the client SENDS to the API
//   XxxDto      → data the API RETURNS to the client
//   AuthResponse → special response after register / login
// =============================================================================

namespace RaceDay.Contracts;

// ========== AUTH ==========

/// <summary>Data required to create a new account.</summary>
public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Must be exactly "Organiser" or "Participant"
    public string Role { get; set; } = string.Empty;
}

/// <summary>Data required to log in.</summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Returned after a successful register or login.</summary>
public class AuthResponse
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// ========== PROFILE ==========

/// <summary>Public view of a user's profile.</summary>
public class ProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>Fields a user is allowed to change on their own profile.</summary>
public class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// ========== EVENTS ==========

/// <summary>Data needed to create a new race event.</summary>
public class CreateEventRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }

    // Must be one of: Run, Walk, Cycle
    public string EventType { get; set; } = string.Empty;
}

/// <summary>Data needed to update an existing event.</summary>
public class UpdateEventRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
}

/// <summary>Event information returned to the client.</summary>
public class EventDto
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int OrganiserId { get; set; }
    public string? OrganiserName { get; set; }
}

// ========== CATEGORIES ==========

/// <summary>Data needed to create a category under an event.</summary>
public class CreateCategoryRequest
{
    // e.g. "Under 20", "Senior", "10km", "21km"
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Category information returned to the client.</summary>
public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ========== ENROLMENTS ==========

/// <summary>Data a Participant sends when enrolling in an event.</summary>
public class CreateEnrolmentRequest
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }
}

/// <summary>Enrolment information returned to the client.</summary>
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int ParticipantId { get; set; }
    public string? ParticipantName { get; set; }

    // "Pending", "Confirmed" or "Cancelled"
    public string Status { get; set; } = "Pending";
    public DateTime EnrolmentDate { get; set; }
}

/// <summary>Used by an Organiser to change the status of an enrolment.</summary>
public class UpdateEnrolmentStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

// ========== RESULTS ==========

/// <summary>Data an Organiser sends when recording a finish time.</summary>
public class CreateResultRequest
{
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}

/// <summary>Result information returned to the client.</summary>
public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public int ParticipantId { get; set; }
    public string? ParticipantName { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}
