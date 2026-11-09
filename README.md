# School Admission System

The project is organized into a .NET backend and a React frontend.

## Project structure

```text
backend/             Backend application and backend tests
frontend/            React frontend application
docker-compose.yml   Local development services
ASSUMPTIONS.md       Decisions made where the assignment is unclear
AI_USAGE.md          AI tools and review notes
```

The backend is currently scaffolded as an ASP.NET Core Web API targeting
.NET 10. PostgreSQL and Entity Framework Core dependencies are included for
the database implementation phase.

## Build the backend

```powershell
dotnet build backend
```

The backend expects a PostgreSQL connection string through the
`DATABASE_CONNECTION_STRING` environment variable. The default local
development value is:

```text
Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres
```

Create or update the database schema after PostgreSQL is running:

```powershell
dotnet ef database update --project backend
```

Create a new migration after changing the entity model:

```powershell
dotnet ef migrations add MigrationName --project backend --output-dir Data/Migrations
```
