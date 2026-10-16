# RaceDay API – PROG6212 Part 2

## What is this project?
RaceDay is a RESTful API that lets people organise and take part in running, walking and cycling events.

There are two types of users:
- **Organiser** – creates events, adds categories, views who has entered, and records results.
- **Participant** – looks at events, enters an event by choosing a category, and views their own results.

## Technologies used
- ASP.NET Core 8 Web API
- Entity Framework Core (Code-First)
- SQL Server
- Server-side Sessions for login
- Password hashing (never store plain passwords)
- Swagger for testing
- xUnit for automated tests
- GitHub Actions for continuous integration

## How the project is organised
Part2_RaceDay_API/
├── Part2_RaceDay_API/          → the actual API (controllers, models, database)
├── Part2_RaceDay_API.Contracts/    → shared request/response objects (DTOs)
├── Part2_RaceDay_API_.Tests/    → automated tests
├── Part_1_Documents/                 → Part 1 ERD, endpoint plan and SQL script
└── .github/workflows/    → CI pipeline


## How to run the API
1. Open the solution in Visual Studio 2022.
2. Make sure SQL Server / SQL Express is running.
3. Open Package Manager Console and set the default project to **RaceDay.Api**.
4. Run these two commands:
5. Press F5. Swagger will open automatically at `/swagger`.

## How authentication works
- When a user registers, the password is **hashed** before it is saved.
- When a user logs in, a **server-side session** is created that stores their `UserId` and `Role`.
- Every protected endpoint checks the session.  
- No session → 401 Unauthorized  
- Wrong role → 403 Forbidden

## Main endpoints
| What you want to do              | Method | URL                              | Who can do it      |
|----------------------------------|--------|----------------------------------|--------------------|
| Register                         | POST   | /api/auth/register               | Anyone             |
| Login                            | POST   | /api/auth/login                  | Anyone             |
| Logout                           | POST   | /api/auth/logout                 | Logged-in user     |
| View / update my profile         | GET/PUT| /api/profile/me                  | Logged-in user     |
| View all events                  | GET    | /api/events                      | Logged-in user     |
| Create / update / delete event   | POST/PUT/DELETE | /api/events...          | Organiser only     |
| Manage categories                | POST/PUT/DELETE | /api/events/{id}/categories | Organiser only |
| Enrol in an event                | POST   | /api/enrolments                  | Participant only   |
| View my enrolments               | GET    | /api/enrolments/mine             | Participant only   |
| View enrolments for my event     | GET    | /api/enrolments/event/{id}       | Organiser only     |
| Record a result                  | POST   | /api/results                     | Organiser only     |
| View my results                  | GET    | /api/results/mine                | Participant only   |

## How to test
1. Open Swagger and try the full flow:
- Register an Organiser → Login → Create Event → Add Categories
- Register a Participant → Login → Enrol → View results
2. Try wrong-role actions (e.g. Participant trying to create an event) – you should get 403.
3. Run the unit tests:
dotnet test

## Continuous Integration
Every time code is pushed to GitHub, GitHub Actions automatically:
1. Restores packages
2. Builds the solution
3. Runs all unit tests

A green build means everything is working.

## Video demonstration
(https://youtu.be/yR6mpdnO58A)

## Part 1 documents
All planning documents (ERD, Endpoint Plan, SQL script) are kept in the `docs/` folder so the marker can see that the API matches the original design.
