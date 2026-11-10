# Assumptions

This file records decisions made where the assignment does not prescribe a
specific implementation.

## Application identity

- Authentication is outside the assignment scope. The student is identified
  by the required `X-Student-Id` header.
- The frontend stores the entered student ID in browser local storage so a
  refresh keeps the current student context.

## Hold lifecycle

- `HOLD_DURATION_SECONDS` controls the hold duration and defaults to `600`
  seconds when the variable is not configured.
- Expired holds are marked `EXPIRED` when the related class or hold data is
  read, or when a new hold is created. This lazy expiration releases the seat
  without requiring a separate cleanup request.
- The payment button simulates a successful payment. No payment gateway is
  integrated.

## Database and identifiers

- PostgreSQL is the source of truth for classes and holds.
- EF Core migrations create and update the schema.
- Class and hold identifiers use compact `c_` and `h_` prefixed values to fit
  the database identifier length limit.
- A class name is unique.
- A student can have at most one `HELD` or `CONFIRMED` application per class.

## Local and container execution

- Local development uses PostgreSQL on `localhost:5432`.
- Docker Compose uses the `postgres` service name for backend-to-database
  communication and persists data in the `postgres-data` volume.
- The Compose configuration currently uses the local development PostgreSQL
  credentials shown in `docker-compose.yml`; production deployments must
  replace them with secrets.

## Testing

- Backend tests use EF Core's in-memory provider for fast API behavior tests.
- The backend test project uses EF Core's in-memory provider for fast API
  behavior tests. A real PostgreSQL concurrency check is also run against two
  Compose backend instances (`localhost:8080` and `localhost:8081`).
- The production hold endpoint uses a PostgreSQL transaction and row-level
  class lock to prevent overselling seats across backend processes.
