# CareConnect EMR API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular client](https://img.shields.io/badge/client-Angular%2020-DD0031?logo=angular)](https://github.com/shaheerasim320/CareConnectEMR-Frontend)
[![Database](https://img.shields.io/badge/database-SQL%20Server-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)

Role-based ASP.NET Core API for a staff-facing EMR workflow. It manages secure staff sessions, patients, appointments, user administration, and live dashboard summaries for Admin, Doctor, and Receptionist roles.

**Frontend:** [CareConnect EMR — Angular](https://github.com/shaheerasim320/CareConnectEMR-Frontend)

## Highlights

- JWT access tokens with optional rotated, hashed refresh tokens in HttpOnly cookies
- Role-based authorization for Admin, Doctor, and Receptionist workflows
- Patient lifecycle management with generated medical record numbers (MRNs)
- Server-side audit logs for data changes, with actor, request context, changed fields, and before/after values
- Appointment booking, rescheduling, cancellation, and validated status transitions
- Role-specific dashboard data, using Dapper for reporting queries
- SQL Server, EF Core, ASP.NET Identity, Docker, Swagger, and health endpoint

## Tech stack

| Layer | Technology |
| --- | --- |
| API | ASP.NET Core 8, REST, Swagger |
| Identity | ASP.NET Core Identity, JWT bearer authentication |
| Data | SQL Server, Entity Framework Core, Dapper |
| Architecture | Layered monolith with Domain, Application, Infrastructure, and API projects |
| Client | [Angular 20](https://github.com/shaheerasim320/CareConnectEMR-Frontend) |

## Features and access

| Feature | Admin | Doctor | Receptionist |
| --- | :---: | :---: | :---: |
| Role-specific dashboard | ✓ | ✓ | ✓ |
| View patients | ✓ | ✓ | ✓ |
| Register patients | ✓ | — | ✓ |
| Update contact/registration details | ✓ | — | ✓ |
| Correct identity (name, DOB, gender) with reason/audit | ✓ | — | ✓ |
| Update clinical fields (blood type/allergies) | ✓ | ✓ | — |
| Change patient status | ✓ | — | — |
| View appointments | ✓ | ✓ | ✓ |
| Create, reschedule, cancel appointments | ✓ | — | ✓ |
| Update appointment status | Any valid transition | Complete own appointment | Scheduling/check-in transitions |
| Manage staff users | ✓ | — | — |

Patient list visibility is enforced on the server:

- Admins can filter by `status`; no `status` query means all patients.
- Doctors and receptionists always receive active patients only, regardless of supplied query parameters.
- Identity corrections require a reason; the audit log records the actor, time, reason, changed fields, and before/after values.

## API overview

| Area | Routes |
| --- | --- |
| Health | `GET /health` |
| Auth | `POST /api/Auth/login`, `refresh-token`, `logout` |
| Dashboard | `GET /api/Dashboard/summary` |
| Patients | `GET /api/Patient/list`, `view/{id}`, `stats`; `POST /register`; `PATCH /contact/{id}`, `identity/{id}`, `clinical/{id}`, `status/{id}` |
| Appointments | `GET /api/Appointment/list`, `view/{id}`, `stats`; `POST /register`; `PUT /update/{id}`; `PATCH /clinical-notes/{id}`, `status/{id}`; `DELETE /cancel/{id}` |
| Users | `POST /api/User/register`; `GET /list`, `view/{id}`; `PATCH /update/{id}`; `POST /reset-password/{id}`; `DELETE /delete/{id}` |

Interactive API documentation is available at `/swagger` in the Development environment.

## Project structure

```text
CareConnectEMR.API/             controllers, authentication pipeline, Swagger, configuration
CareConnectEMR.Application/     DTOs, result/paging contracts, service interfaces, mappers
CareConnectEMR.Domain/          entities, role/status constants, and audit contract
CareConnectEMR.Infrastructure/  EF Core, Identity, migrations, seeders, service implementations
```

Dependencies point inward: `API → Application → Domain`; `Infrastructure → Application → Domain`.

## Security rules

- Doctors can only list, view, and update the status of appointments assigned to them.
- Doctors can only view patients with an appointment assigned to them.
- Receptionists manage scheduling/check-in transitions; doctors complete their own checked-in appointments; admins may override valid transitions.
- Appointment assignment verifies that the selected staff user is active and has the Doctor role.
- Refreshing a session for a deactivated user revokes that refresh token and denies the request.
- Audit logs deliberately exclude password hashes, security stamps, concurrency stamps, and refresh-token hashes. Audit-log access is not exposed through the API.

## Run locally

Prerequisites: .NET 8 SDK and SQL Server.

```bash
git clone https://github.com/shaheerasim320/CareConnectEMR-Backend.git
cd CareConnectEMR-Backend/CareConnectEMR.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=CareConnectEMR;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "replace-with-a-random-secret-of-at-least-32-characters"

dotnet ef database update --project ../CareConnectEMR.Infrastructure --startup-project .
dotnet run
```

The API starts locally over HTTPS at `https://localhost:7024`; Swagger is available at `/swagger`.

For the Angular client, use its development proxy configuration and run it from the linked frontend repository.

## Docker

Create a `.env` file containing `DEFAULT_CONNECTION` and `JWT_KEY`, then run:

```bash
docker compose up --build
```

The compose setup runs the API on port `8080`. It expects an externally available SQL Server and does not apply migrations for you.

## Roadmap

- Frontend workflows for appointments, patient registration/editing, and user management
- Automated unit/integration tests and CI
- Production logging, monitoring, rate limiting, and a formal migration/deployment workflow
- Fine-grained authorization for appointment state transitions

## Notes

Development seed data creates roles, sample users, patients, and appointments when the database has no patients. Replace or disable it before any shared deployment.
