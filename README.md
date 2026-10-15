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
