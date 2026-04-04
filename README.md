# 🏥 CareConnect EMR API

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Architecture](https://img.shields.io/badge/Architecture-Clean-blue)
![Auth](https://img.shields.io/badge/Auth-JWT%20%2B%20Refresh%20Token-green)
![Database](https://img.shields.io/badge/Database-SQL%20Server-red)
![License](https://img.shields.io/badge/License-MIT-yellow)

**CareConnect EMR** is a **modular Electronic Medical Record (EMR) backend system** built using **ASP.NET Core Web API, Clean Architecture, and SQL Server**.

The project demonstrates **production-grade backend architecture**, including secure authentication, user management, patient management, and scalable API design for healthcare platforms.

The system is designed with **enterprise backend engineering practices**, making it easy to extend with additional healthcare modules.

# ✨ Core Features

- JWT Authentication with Refresh Tokens
- ASP.NET Identity User Management
- Role-based Authorization
- Patient Management System
- Pagination & Filtering APIs
- Soft Delete Implementation
- Audit Fields (CreatedAt / UpdatedAt)
- Clean Architecture (Layered Design)
- Secure Logout with Refresh Token Invalidation
- Modular Backend Structure for Scalability
---

# 🏥 Implemented Modules
## 👤 User Management

The system uses **ASP.NET Identity** to manage application users and roles.

Features include:

- User registration with role assignment
- Secure login using JWT authentication
- Refresh token generation and rotation
- Role-based access control
- Admin-controlled user management

Supported roles:
```
Admin
Receptionist
Doctor
```
---

## 🧑‍⚕️ Patient Management

The **Patient module** manages patient records within the system.

Features include:

- Patient registration
- Unique Medical Record Number (MRN) generation
- Patient search and pagination
- Partial patient updates
- Soft delete support
- Role-based access control

Example MRN format:

```
MRN-2026-000001
```
# 🏗 Architecture

The project follows **Clean Architecture principles**, separating responsibilities into layers.

```
                    ┌─────────────────────┐
                    │      API Layer      │
                    │  Controllers        │
                    │  Middleware         │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │   Application Layer │
                    │  DTOs               │
                    │  Interfaces         │
                    │  Business Logic     │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │     Domain Layer    │
                    │  Entities           │
                    │  Core Models        │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ Infrastructure Layer│
                    │  EF Core            │
                    │  Identity           │
                    │  Token Services     │
                    │  Database           │
                    └─────────────────────┘
```

---

# 📁 Project Structure

```
CareConnectEMR
│
├── CareConnectEMR.API
│   ├── Controllers
│   │   ├── AuthController
│   │   ├── UserController
│   │   └── PatientController
│   └── Program.cs
│
├── CareConnectEMR.Application
│   ├── DTOs
│   │   ├── Auth
│   │   ├── User
│   │   └── Patient
│   │
│   ├── Interfaces
│   │   ├── IAuthService
│   │   ├── IUserService
│   │   └── IPatientService
│   │
│   └── Services
│
├── CareConnectEMR.Domain
│   ├── Entities
│   │   └── Patient
│   │
│   └── Common
│
└── CareConnectEMR.Infrastructure
    ├── Persistence
    │   └── ApplicationDbContext
    │
    ├── Identity
    │   └── IdentitySeeder
    │
    └── Services
        └── TokenService
```

---

# 🔐 Authentication Flow

The system uses **Access Tokens + Refresh Tokens**.

### Login Flow

```
Client
   │
   │ POST /api/auth/login
   ▼
API
   │
   │ Validate user via Identity
   ▼
TokenService
   │
   │ Generate JWT Access Token
   │ Generate Refresh Token
   ▼
Response → AccessToken + RefreshToken
```

---

### Refresh Token Flow

```
Client
   │
   │ POST /api/auth/refresh-token
   ▼
API
   │
   │ Validate expired access token
   │ Validate refresh token in DB
   ▼
TokenService
   │
   │ Generate new access token
   ▼
Response → New Access Token
```

---

### Logout Flow

```
Client
   │
   │ POST /api/auth/logout
   ▼
API
   │
   │ Remove refresh token from database
   ▼
User must login again
```

---

# 🔑 API Endpoints

### Authentication

```
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/logout
```
---
### Users

```
POST /api/user
GET /api/user
GET /api/user/{id}
PATCH /api/user/{id}
DELETE /api/user/{id}
```
---
### Patients

```
GET /api/patient
GET /api/patient/{id}
POST /api/patient
PATCH /api/patient/{id}
DELETE /api/patient/{id}
```
---

# 🗄 Database Setup

Run migrations to create the database.

```
dotnet ef migrations add InitialIdentity \
--project CareConnectEMR.Infrastructure \
--startup-project CareConnectEMR.API
```

```
dotnet ef database update \
--project CareConnectEMR.Infrastructure \
--startup-project CareConnectEMR.API
```

---

# 👤 Default Admin Account (Development Only)

```
Email: admin@careconnect.com
Password: Admin@123
Role: Admin
```

⚠️ These credentials are for **development/testing only**.
You can change them in the **IdentitySeeder** or configuration.

---

# ▶ Running the Project

Clone the repository

```
git clone https://github.com/yourusername/CareConnectEMR.git
```

Navigate to the project

```
cd CareConnectEMR
```

Run migrations

```
dotnet ef database update
```

Run the API

```
dotnet run --project CareConnectEMR.API
```

Open Swagger

```
https://localhost:xxxx/swagger
```

---

# 🚧 Upcoming Modules

The system will expand into a full **Healthcare Management Platform**.

Planned modules:

* Doctor Management
* Appointment Scheduling
* Medical Records
* Prescriptions
* Billing & Payments

---

# 👨‍💻 Author

**Shaheer Asim**

Software Developer
Backend | .NET | Distributed Systems
