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
- A real PostgreSQL concurrency check is also run against two Compose backend
  instances (`localhost:8080` and `localhost:8081`). Both instances use the
  same PostgreSQL database, so the test represents separate backend processes
  competing for the same class capacity.
- The production hold endpoint prevents overselling with the following
  sequence:
  1. It starts a database transaction.
  2. For a relational database, it loads the requested class with PostgreSQL's
     `FOR UPDATE` row-level lock. A second request for the same class must wait
     until the first transaction finishes.
  3. While holding that lock, it expires old holds, checks for an existing
     active application from the student, and counts active held and confirmed
     seats.
  4. It creates a new hold only when
     `heldSeats + confirmedSeats < totalSeats`.
  5. It saves the hold and commits the transaction. If no seat is available,
     it returns HTTP 409 and does not create a hold.
- The seat-capacity invariant is therefore
  `heldSeats + confirmedSeats <= totalSeats`. The row lock is important because
  an application-level check without a database lock could allow two backend
  processes to observe the same remaining seat before either one writes its
  hold.
- The real validation sent 10 simultaneous hold requests across both backend
  ports for a class with one seat. Exactly one request succeeded, the other
  requests received HTTP 409, and the final class state was one held seat and
  zero confirmed seats. This confirms the invariant across backend processes.
