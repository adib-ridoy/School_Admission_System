# School Admission System

A school admission application with a .NET Web API backend, React frontend,
and PostgreSQL database. Students can hold a seat temporarily and confirm or
cancel their application.

## Technology

- Backend: ASP.NET Core Web API on .NET 10
- Backend instances: two Compose processes on ports `8080` and `8081`
- Database: PostgreSQL
- ORM: Entity Framework Core with Npgsql
- Frontend: React, TypeScript, and Vite
- Production frontend server: Nginx
- Local orchestration: Docker Compose
- Backend tests: xUnit and EF Core InMemory

## Project structure

```text
backend/
  Controllers/       HTTP API endpoints
  Data/              DbContext and EF Core migrations
  DTOs/              API request and response types
  Models/            Domain entities and statuses
  Services/          Application services
  Tests/             Backend test project
frontend/
  src/               React application and API client
docker-compose.yml   PostgreSQL, backend, and frontend services
ASSUMPTIONS.md       Decisions made where the assignment is unclear
AI_USAGE.md          AI assistance and review notes
```

## Prerequisites

For local development, install:

- .NET SDK 10 or newer
- Node.js 24 or newer
- PostgreSQL 18 or Docker Desktop
- Git

Verify the tools:

```powershell
dotnet --version
node --version
git --version
docker --version
```

## Run with Docker Compose

This is the recommended setup because it starts PostgreSQL, the backend, and
the frontend together:

```powershell
docker compose up --build
```

Open:

- Frontend: `http://localhost:5173`
- Backend: `http://localhost:8080`
- Secondary backend: `http://localhost:8081`

The backend applies pending EF Core migrations at startup. PostgreSQL data is
stored in the `postgres-data` Docker volume.

Stop the services without deleting the database volume:

```powershell
docker compose down
```

To delete the containers and database volume as well:

```powershell
docker compose down -v
```

The Compose file contains development credentials. Replace them with secrets
before using this configuration outside local development.

## Run locally without Docker

Start PostgreSQL and create a database named `school_admission`. Then set the
backend connection string in the same PowerShell session used to start it:

```powershell
$env:DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=YOUR_PASSWORD"
$env:HOLD_DURATION_SECONDS="600"
dotnet run --project backend --urls http://localhost:8080
```

If the schema has not been created, apply the migrations:

```powershell
dotnet ef database update --project backend
```

In another terminal, install and start the frontend:

```powershell
npm.cmd install --prefix frontend
npm.cmd run dev --prefix frontend
```

The frontend runs at `http://localhost:5173`. Set
`VITE_API_BASE_URL` before starting it if the backend is not at
`http://localhost:8080`:

```powershell
$env:VITE_API_BASE_URL="http://localhost:8080"
npm.cmd run dev --prefix frontend
```

## Configuration

| Variable | Required | Default | Description |
|---|---:|---|---|
| `DATABASE_CONNECTION_STRING` | Backend: yes | None | PostgreSQL connection string |
| `HOLD_DURATION_SECONDS` | No | `600` | Duration of a seat hold |
| `VITE_API_BASE_URL` | No | `http://localhost:8080` | Backend URL embedded in the frontend build |

## API

Base URL:

```text
http://localhost:8080
```

### Endpoints

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/classes` | Create a class |
| `GET` | `/classes` | List classes and seat counts |
| `GET` | `/classes/{classId}` | Get one class |
| `POST` | `/classes/{classId}/holds` | Hold one seat |
| `GET` | `/holds/{holdId}` | Get a hold |
| `POST` | `/holds/{holdId}/confirm` | Confirm admission |
| `DELETE` | `/holds/{holdId}` | Cancel a hold |
| `GET` | `/me/holds` | List the current student's applications |

Requests involving a student require:

```http
X-Student-Id: student-001
```

Create a class:

```powershell
$body = @{ name = "Class 6 - Morning Shift"; totalSeats = 120 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8080/classes" `
  -Method Post -ContentType "application/json" -Body $body
```

Hold a seat:

```powershell
$headers = @{ "X-Student-Id" = "student-001" }
Invoke-RestMethod -Uri "http://localhost:8080/classes/c_123/holds" `
  -Method Post -Headers $headers
```

Confirm or cancel an application:

```powershell
Invoke-RestMethod -Uri "http://localhost:8080/holds/h_789/confirm" `
  -Method Post -Headers $headers

Invoke-RestMethod -Uri "http://localhost:8080/holds/h_789" `
  -Method Delete -Headers $headers
```

Hold statuses are `HELD`, `CONFIRMED`, `EXPIRED`, and `CANCELLED`.

## Tests

Run the backend tests:

```powershell
dotnet test backend\Tests\SchoolAdmission.Tests\SchoolAdmission.Tests.csproj
```

The suite covers class creation, the hold/confirmation workflow, duplicate
active applications, and concurrent seat-hold requests.

Build the frontend:

```powershell
npm.cmd run build --prefix frontend
```

Run the frontend automated tests:

```powershell
npm.cmd test --prefix frontend
```

Both backend containers use the same PostgreSQL database. To validate the
cross-process capacity guarantee after starting Compose, send concurrent hold
requests to both `http://localhost:8080` and `http://localhost:8081` and
confirm that `heldSeats + confirmedSeats` never exceeds `totalSeats`.

## Migrations

Apply existing migrations:

```powershell
dotnet ef database update --project backend
```

Create a migration after changing the entity model:

```powershell
dotnet ef migrations add MigrationName --project backend --output-dir Data/Migrations
```

## Troubleshooting

### `Failed to fetch` in the frontend

Ensure the backend is running on `http://localhost:8080` and that the browser
can open `http://localhost:8080/classes`. For local PowerShell execution,
set `DATABASE_CONNECTION_STRING` in the same terminal before `dotnet run`.

### Backend says the connection string is required

Set the variable before starting the backend:

```powershell
$env:DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=YOUR_PASSWORD"
```

### PostgreSQL authentication fails

Check the username, password, port, and database name. The Docker Compose
credentials and local PostgreSQL credentials must match the connection string.

### Port already in use

Stop the process using port `8080`, `5173`, or `5432`, or change the
corresponding port mapping and application URL.

### Docker database data is unexpectedly reset

Do not use `docker compose down -v` unless you intend to delete the
`postgres-data` volume.
