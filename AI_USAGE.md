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
smoke tests were corrected manually, including an identifier-length issue and
frontend TypeScript typing issues.
