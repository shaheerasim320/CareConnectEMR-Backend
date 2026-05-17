# CareConnect EMR — Backend API

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
| API (Swagger) | `http://careconnectemr-backend.runasp.net/swagger` |
| Health Check | `http://careconnectemr-backend.runasp.net/health` |

---

# Features

- JWT authentication with **refresh token rotation**
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

## Authentication System

The API uses **JWT Access Tokens with Refresh Token Rotation** for secure session management, fully supporting a **Backend-For-Frontend (BFF)** architecture.

### Access Token

- Short lived
- Contains user identity and role claims
- Signed using `HMAC SHA256`
- Expiration: **15 minutes**
- Returned in the JSON response body and stored only in memory by the client.

Access tokens are **not stored in the database**.

They are verified via signature and expiration.

### Refresh Token (BFF Cookie Pattern)

- Long lived token
- Stored **hashed in the database**
- Used to generate new access tokens
- Rotated on every refresh request
- Expiration: **7 days**
- **Issued as an `HttpOnly`, `Secure`, `SameSite=None` cookie** directly by the API.

By issuing the refresh token as an `HttpOnly` cookie, the backend actively implements a secure BFF pattern. The refresh token is completely inaccessible to frontend JavaScript, significantly mitigating XSS risks while allowing seamless, silent re-authentication.

---

# Refresh Token Security

The refresh token system follows **industry best practices used in modern APIs**.

Key security features:

- Refresh tokens are **hashed using SHA256 before storage**
- Raw refresh tokens are **never stored in the database**
- **Token rotation** is enforced
- Old refresh tokens are **revoked when used**
- Token reuse can be detected through the **token chain**

## Database Table

| Column | Description |
|------|------|
| Id | Primary key |
| UserId | Owner of token |
| TokenHash | SHA256 hash of refresh token |
| ExpiresAt | Expiration timestamp |
| Revoked | Whether token has been invalidated |
| ReplacedByTokenId | Next token in rotation chain |

## Example lifecycle: 
```
 Login
   │
   ▼
AccessToken1 + RefreshToken1
   │
   ▼
Refresh request
   │
   ▼
RefreshToken1 → revoked
RefreshToken2 → created
   │
   ▼
New AccessToken issued
```
This ensures a stolen refresh token **cannot be reused after rotation**.



# Modules

### Auth
JWT login supporting email or username. Refresh token rotation with `RememberMe` support. Server-side logout invalidates the refresh token immediately.

```
POST   /api/Auth/login
POST   /api/Auth/refresh-token
POST   /api/Auth/logout
```

### Users
Admin-only user management via ASP.NET Identity. Role assignment, partial updates, password reset through Identity pipeline.

```
POST   /api/User/register
GET    /api/User/list
GET    /api/User/view/{id}
PATCH  /api/User/update/{id}
POST   /api/User/reset-password/{id}
DELETE /api/User/delete/{id}
```

### Patients
Full patient lifecycle — registration, search, soft delete. MRN generated via SQL Sequence (race-condition safe). Doctors restricted to updating patients assigned to them via appointments.

```
GET    /api/Patient/list
GET    /api/Patient/view/{id}
POST   /api/Patient/register
PATCH    /api/Patient/update/{id}
DELETE /api/Patient/delete/{id}
```

MRN format: `MRN-2026-000001`

### Appointments
Booking with `StartTime` / `EndTime` (not duration). State machine enforces valid transitions. Double-booking prevention via interval overlap check. Soft-deleted patients cannot be booked.

```
GET    /api/Appointment/list
GET    /api/Appointment/view/{id}
POST   /api/Appointment/register
PUT    /api/Appointment/update/{id}
PATCH  /api/Appointment/status/{id}
DELETE /api/Appointment/cancel/{id}
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
GET    /api/Dashboard/summary
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

# Architecture

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

## Key patterns used

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

> ⚠️ Warning:  These credentials are seeded via `IdentitySeeder.cs` and `DataSeeder.cs` for development only. Change before any public deployment.

### Admin User

| Email | Password | Role | Full Name
|--------|-------------|-------------|-------------|
| admin@careconnect.com | Admin@123 | Admin | Evelyn Reed |

### Doctors

| Email | Password | Role | Full Name
|--------|-------------|-------------|-------------|
| sarah@careconnect.com | Doctor@123 | Doctor | Sarah Khan
| ali@careconnect.com | Doctor@123 | Doctor | Ali Ahmed
| maryam@careconnect.com | Doctor@123 | Doctor | Maryam Noor

### Front Desk (Receptionists)

| Email | Password | Role | Full Name
|--------|-------------|-------------|-------------|
| reception1@careconnect.com | Staff@123 | Receptionist | Reception One
| reception2@careconnect.com | Staff@123 | Receptionist | Reception Two

### 📊 Seeded Clinical Data Summary
The development database is automatically populated with a baseline of realistic data to facilitate dashboard testing and UI development.
- Total Patients: 30
   - MRN Generation: Automatically assigned from MRN-1000 to MRN-1029.
   - Demographics: Includes a realistic distribution of ages, genders, and blood types.
- Total Appointments: 50
  - Timeline: Chronologically distributed (approx. 3 days past, today, and 3 days future).
  - Status Variety: Randomized across all system states (Scheduled, Confirmed, CheckedIn, Completed, Cancelled, NoShow).
  - Doctor Assignment: Load-balanced across all three seeded doctors to test "Top Doctors" dashboard metrics.

---

## Planned — Phase 2

- Medical Records / SOAP notes (Doctor writes visit notes)
- Prescriptions module with drug interaction check
- Lab Tests module
- Patient portal (self-service role)
- SignalR real-time appointment status updates
- Nurse and Lab Technician roles

---

### Optional Swagger protection secrets

If you set both of these secrets, the deployed site will expose Swagger and protect it with HTTP Basic Auth. If you do not set them, Swagger stays disabled in production.

```text
APP_SWAGGER_USERNAME
APP_SWAGGER_PASSWORD
```

---

## Author

**Shaheer Asim**  
Software Engineer — .NET · Angular · SQL Server  
[GitHub](https://github.com/shaheerasim320) · [LinkedIn](https://linkedin.com/in/shaheer-asim-4b08a2367)