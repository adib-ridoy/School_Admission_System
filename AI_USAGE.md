# AI usage

This file records AI assistance used during the project and the review applied
to generated work.

# AI tools

GitHub Copilot was used for this project.

## Areas that used AI assistance

- Repository and folder scaffolding
- ASP.NET Core and EF Core database setup
- PostgreSQL migration configuration
- Backend DTOs and API controllers
- React frontend components and API client
- Dockerfiles and Docker Compose configuration
- Automated backend test structure
- README and project documentation

## Review process

- Build commands were run for the backend and frontend after implementation.
- The EF Core migration was applied to PostgreSQL.
- Docker Compose configuration was validated with `docker compose config`.
- Backend and frontend Docker images were built successfully.
- The Compose stack was started and the frontend and backend endpoints were
  checked.
- Four backend tests were run, including a concurrent seat-hold test.

AI-generated code was treated as a draft. Errors found during builds and
smoke tests were corrected manually. Three important examples are:

1. **Database identifier length error**

   The initial generated identifiers for classes and holds were longer than
   the database column limit. This was discovered when the EF Core migration
   and API smoke tests were run against PostgreSQL and the database rejected
   the inserted value because it exceeded the configured maximum length.
   The identifiers were changed to compact values with `c_` and `h_` prefixes,
   followed by a shortened GUID value. The migration and API tests were then
   run again to confirm that records could be inserted and retrieved
   successfully.

2. **Frontend TypeScript and asynchronous rendering errors**

   The first frontend test code used types and assertions that did not match
   the actual React component state and API response shapes. The frontend
   build reported TypeScript errors, and the initial test assertion also ran
   before the asynchronous class data had been rendered. These problems were
   found by running the TypeScript/Vite production build and the Vitest test
   command. The API response and component types were aligned, and the test
   was changed to wait for the UI update with React Testing Library's
   `waitFor`. The frontend tests and production build then passed.

3. **Concurrent seat overselling**

   The first concurrency check showed that two simultaneous requests could
   reserve the same final seat when the backend processes checked capacity at
   the same time. This was found by sending concurrent hold requests to a
   one-seat class and comparing the resulting holds with
   `heldSeats + confirmedSeats`. The hold endpoint was fixed to use a
   PostgreSQL transaction and a `FOR UPDATE` row-level lock on the class
   before checking capacity and creating the hold. The test was repeated
   against two backend containers using the same database; for ten requests
   competing for one seat, exactly one request succeeded and the capacity
   invariant remained valid.

4. **Docker Compose backend startup failure**

   The generated Compose configuration initially contained an invalid
   PostgreSQL connection string for the backend services. This was discovered
   when `docker compose up -d --build` completed but the secondary backend
   container stopped immediately. Container logs showed that connection-string
   initialization failed because the password value was malformed. The
   connection strings for both backend services were corrected to use the
   PostgreSQL password configured by the Compose database service. The stack
   was rebuilt, `docker compose ps` confirmed that both backend containers were
   running, and requests to ports `8080` and `8081` confirmed that both APIs
   could connect to the database.
