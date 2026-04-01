# 🏥 CareConnect EMR API

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Architecture](https://img.shields.io/badge/Architecture-Clean-blue)
![Auth](https://img.shields.io/badge/Auth-JWT%20%2B%20Refresh%20Token-green)
![Database](https://img.shields.io/badge/Database-SQL%20Server-red)
![License](https://img.shields.io/badge/License-MIT-yellow)

CareConnect EMR is a **modular Electronic Medical Record (EMR) backend system** built using **ASP.NET Core Web API, Clean Architecture, and JWT authentication**.

The project demonstrates **production-grade backend architecture**, including:

* Secure authentication
* Role-based authorization
* Refresh token system
* Audit logging
* Scalable modular design for healthcare systems

---

# ✨ Features

✔ JWT Authentication
✔ Refresh Token with Remember Me
✔ ASP.NET Identity integration
✔ Role-based Authorization (Admin / Doctor)
✔ Clean Architecture
✔ Audit Fields (CreatedBy / UpdatedBy)
✔ Secure logout (refresh token invalidation)
✔ Modular structure for future healthcare features

---

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
                    └─────────────────────┘
```

---

# 📁 Project Structure

```
CareConnectEMR
│
├── CareConnectEMR.API
│   ├── Controllers
│   └── Program.cs
│
├── CareConnectEMR.Application
│   ├── DTOs
│   ├── Interfaces
│   └── Common
│
├── CareConnectEMR.Domain
│   ├── Entities
│   └── Common
│
└── CareConnectEMR.Infrastructure
    ├── Persistence
    ├── Services
    └── Seed
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

### Login

```
POST /api/auth/login
```

Request

```json
{
  "identifier": "admin@careconnect.com",
  "password": "Admin@123",
  "rememberMe": true
}
```

Response

```json
{
  "accessToken": "jwt_token",
  "refreshToken": "refresh_token",
  "accessTokenExpiry": "2026-03-30T14:00:00",
  "userId": "user_id",
  "fullName": "Evelyn Reed",
  "role": "Admin"
}
```

---

### Refresh Token

```
POST /api/auth/refresh-token
```

```json
{
  "accessToken": "expired_access_token",
  "refreshToken": "refresh_token"
}
```

---

### Logout

```
POST /api/auth/logout
```

Header

```
Authorization: Bearer {access_token}
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

* Patient Management
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
