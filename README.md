# CareConnect EMR — Backend API

![CI](https://github.com/shaheerasim320/CareConnectEMR/actions/workflows/ci.yml/badge.svg)
![Deploy](https://github.com/shaheerasim320/CareConnectEMR/actions/workflows/deploy-monsterasp.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Architecture](https://img.shields.io/badge/Architecture-Clean-blue)
![Auth](https://img.shields.io/badge/Auth-JWT%20%2B%20Refresh%20Token-green)
![Database](https://img.shields.io/badge/Database-SQL%20Server-red)
![License](https://img.shields.io/badge/License-MIT-yellow)

**CareConnect EMR** is a production-grade **Electronic Medical Record backend system** built with **ASP.NET Core 8, Clean Architecture, SQL Server, and Docker**. Designed to mirror real hospital workflows at the level of platforms like CureMD and Cerner.

---

## Live Demo

| Resource | URL |
|----------|-----|
| API (Swagger) | `https://your-domain.monsterasp.net/swagger` |
| Health Check | `https://your-domain.monsterasp.net/health` |

---

## Features

- JWT authentication with refresh token rotation
- Role-based authorization — Admin, Doctor, Receptionist
- Patient management with auto-generated MRN (SQL Sequence)
- Appointment scheduling with state machine and double-booking prevention
- Role-specific dashboard with KPI stats and trend analysis
- Dapper for aggregate/reporting queries, EF Core for CRUD
- Soft delete across all entities
- Auto-audit fields via `SaveChangesAsync` override
- Pagination, search, and filtering on all list endpoints
- Global exception middleware — consistent JSON error responses
- FluentValidation on all request DTOs
- Docker support with multi-stage build
- CI/CD via GitHub Actions → MonsterASP.NET

---

## Modules

### Auth
JWT login supporting email or username. Refresh token rotation with `RememberMe` support. Server-side logout invalidates the refresh token immediately.

```
POST   /api/auth/login
POST   /api/auth/refresh-token
POST   /api/auth/logout
GET    /api/auth/me
```

### Users
Admin-only user management via ASP.NET Identity. Role assignment, partial updates, password reset through Identity pipeline.

```
POST   /api/user/register
GET    /api/user/list
GET    /api/user/view/{id}
PATCH  /api/user/update/{id}
POST   /api/user/reset-password/{id}
DELETE /api/user/delete/{id}
```

### Patients
Full patient lifecycle — registration, search, soft delete. MRN generated via SQL Sequence (race-condition safe). Doctors restricted to updating patients assigned to them via appointments.

```
GET    /api/patient/list
GET    /api/patient/view/{id}
POST   /api/patient/register
PATCH    /api/patient/update/{id}
DELETE /api/patient/delete/{id}
```

MRN format: `MRN-2026-000001`

### Appointments
Booking with `StartTime` / `EndTime` (not duration). State machine enforces valid transitions. Double-booking prevention via interval overlap check. Soft-deleted patients cannot be booked.

```
GET    /api/appointment/list
GET    /api/appointment/view/{id}
POST   /api/appointment/register
PUT    /api/appointment/update/{id}
PATCH  /api/appointment/status/{id}
DELETE /api/appointment/cancel/{id}
```

Status flow:
```
Scheduled → Confirmed → CheckedIn → Completed
Scheduled → Cancelled
Confirmed → Cancelled
CheckedIn → NoShow
```

### Dashboard
Single endpoint — returns role-specific response from JWT claim. Stats include count + trend vs previous period. Chart data (GROUP BY status, 7-day time series) ready for Chart.js.

```
GET    /api/dashboard/summary
```

| Role | Gets |
|------|------|
| Admin | Total patients, appointments today, completion rate, cancellation rate, breakdown by status, top doctors, recent registrations, 7-day trends |
| Doctor | Own appointments today, next appointment, today's schedule, 7-day personal trend |
| Receptionist | Today's queue, checked-in count, new patients today |

---

## Role Permission Matrix

| Action | Admin | Doctor | Receptionist |
|--------|-------|--------|--------------|
| Create patient | ✅ | ❌ | ✅ |
| View patients | ✅ | ✅ | ✅ |
| Update patient | ✅ | ✅ | ❌ |
| Delete patient | ✅ | ❌ | ❌ |
| Book appointment | ✅ | ❌ | ✅ |
| View appointments | ✅ | ✅ | ✅ |
| Reschedule appointment | ✅ | ❌ | ✅ |
| Complete appointment | ✅ | ✅ | ❌ |
| Cancel appointment | ✅ | ❌ | ✅ |
| User management | ✅ | ❌ | ❌ |

---

## Architecture

```
CareConnectEMR
├── CareConnectEMR.API              ← Controllers, Middleware, Program.cs
├── CareConnectEMR.Application      ← DTOs, Interfaces, Validators, Mappers
├── CareConnectEMR.Domain           ← Entities, Common (IAuditable, Result<T>)
└── CareConnectEMR.Infrastructure   ← EF Core, Identity, Services, Dapper queries
```

Dependency rule: arrows point inward only. Infrastructure implements Application interfaces. Domain has zero external dependencies.

```
API → Application → Domain
Infrastructure → Application → Domain
```

### Key patterns used

- **Result\<T\>** — all service methods return `Result<T>` instead of throwing exceptions for expected failures
- **IAuditable** — `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` auto-populated in `SaveChangesAsync`
- **Soft delete** — `IsDeleted` flag on Patient and User. Records are never hard deleted
- **SQL Sequence** — MRN generation uses `NEXT VALUE FOR dbo.PatientNumbers` (atomic, no race condition)
- **Dapper for reporting** — dashboard aggregate queries use Dapper for single-round-trip SQL
- **EF Core for CRUD** — all create/update/delete operations use EF Core with change tracking

---

## Database

SQL Server with EF Core migrations. Filtered indexes on `IsDeleted` columns. Composite indexes on frequently joined columns.

```bash
# Create migration
dotnet ef migrations add InitialCreate \
  --project CareConnectEMR.Infrastructure \
  --startup-project CareConnectEMR.API

# Apply migration
dotnet ef database update \
  --project CareConnectEMR.Infrastructure \
  --startup-project CareConnectEMR.API
```

---

## Running locally

### Option A — dotnet CLI

```bash
git clone https://github.com/YOUR_USERNAME/CareConnectEMR.git
cd CareConnectEMR
```

Create `CareConnectEMR.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CareConnectEMR;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YourLocalDevSecretKeyAtLeast32Chars!",
    "Issuer": "CareConnectEMR",
    "Audience": "CareConnectEMR-Angular-Client"
  }
}
```

```bash
dotnet ef database update \
  --project CareConnectEMR.Infrastructure \
  --startup-project CareConnectEMR.API

dotnet run --project CareConnectEMR.API
```

Open Swagger: `https://localhost:{port}/swagger`

---

### Option B — Docker Compose

Create a `.env` file at the solution root:

```env
DEFAULT_CONNECTION=Server=host.docker.internal;Database=CareConnectEMR;Trusted_Connection=True;TrustServerCertificate=True;
JWT_KEY=YourLocalDevSecretKeyAtLeast32Chars!
JWT_ISSUER=CareConnectEMR
JWT_AUDIENCE=CareConnectEMR-Angular-Client
API_HTTP_PORT=8080
```

```bash
docker-compose up --build
```

API available at `http://localhost:8080/swagger`

---

## CI/CD Pipeline

```
git push → main
     │
     ▼
ci.yml triggers
     ├── dotnet restore
     ├── dotnet build (Release)
     ├── dotnet test (skipped if no test projects)
     └── docker build (verifies image builds)
     │
     ▼  (only if ci passes)
deploy-monsterasp.yml triggers
     ├── dotnet publish
     ├── Generate appsettings.Production.json from secrets
     └── WebDeploy → MonsterASP.NET
```

### GitHub Secrets required

| Secret | Description |
|--------|-------------|
| `APP_DEFAULT_CONNECTION` | Production SQL Server connection string |
| `APP_JWT_KEY` | Production JWT signing key (32+ chars) |
| `APP_JWT_ISSUER` | JWT issuer (e.g. `CareConnectEMR`) |
| `APP_JWT_AUDIENCE` | JWT audience (e.g. `CareConnectEMR-Angular-Client`) |
| `MONSTERASP_WEBSITE_NAME` | Website name in MonsterASP.NET panel |
| `MONSTERASP_SERVER_COMPUTER_NAME` | WebDeploy server address |
| `MONSTERASP_SERVER_USERNAME` | WebDeploy username |
| `MONSTERASP_SERVER_PASSWORD` | WebDeploy password |

---

## Health Check

```
GET /health
```

Returns database connectivity status. Used by CI/CD pipeline to verify deployment succeeded.

```json
{
  "status": "Healthy",
  "results": {
    "sqlserver": { "status": "Healthy" }
  }
}
```

---

## Default Dev Credentials

```
Email:    admin@careconnect.com
Password: Admin@123
Role:     Admin
```

> These credentials are seeded via `IdentitySeeder.cs` for development only. Change before any public deployment.

---

## Planned — Phase 2

- Medical Records / SOAP notes (Doctor writes visit notes)
- Prescriptions module with drug interaction check
- Lab Tests module
- Patient portal (self-service role)
- SignalR real-time appointment status updates
- Nurse and Lab Technician roles

---

## Author

**Shaheer Asim**  
Software Engineer — .NET · Angular · SQL Server  
[GitHub](https://github.com/YOUR_USERNAME) · [LinkedIn](https://linkedin.com/in/YOUR_PROFILE)