# 🍽️ RestaurantBookingAPI

[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?style=flat-square&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?style=flat-square)](https://docs.microsoft.com/en-us/ef/core/)
[![Database](https://img.shields.io/badge/Database-SQL%20Server-CC292B?style=flat-square&logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![Authentication](https://img.shields.io/badge/Auth-JWT%20Bearer-black?style=flat-square&logo=json-web-tokens)](https://jwt.io/)
[![Swagger](https://img.shields.io/badge/API%20Docs-Swagger%20%2F%20OpenAPI-85EA2D?style=flat-square&logo=swagger)](https://swagger.io/)

A modern, high-performance RESTful API for online restaurant discovery, table reservations, and restaurant management built with **ASP.NET Core (.NET 10)**, **Entity Framework Core**, and **SQL Server**.

---

## 📖 Table of Contents

- [Overview](#-overview)
- [Key Features](#-key-features)
- [Architecture & Design Patterns](#-architecture--design-patterns)
- [Tech Stack](#-tech-stack)
- [Project Structure](#-project-structure)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Configuration](#configuration)
  - [Database Setup](#database-setup)
  - [Running the Project](#running-the-project)
- [API Endpoints Overview](#-api-endpoints-overview)
- [Security & Authentication](#-security--authentication)
- [Frontend Integration (CORS)](#-frontend-integration-cors)
- [License](#-license)

---

## 🌟 Overview

**RestaurantBookingAPI** serves as the backend engine for a full-scale restaurant reservation platform. It powers customer reservations, owner administration, payment processing, image management, email notifications, and background maintenance workflows with secure role-based access.

---

## ✨ Key Features

### 🔐 Authentication & User Management
- **Role-Based Access Control (RBAC)**: `Customer`, `RestaurantOwner`, and `Admin` roles.
- **Secure Password Hashing**: Cryptographic password hashing and salt verification (`PasswordService`).
- **JWT Bearer Authentication**: Claims-based token authentication with configurable expiration.
- **Forgot & Reset Password**: Multi-step OTP verification delivered via SMTP email (`MailKit`).
- **Terms & Legal Acceptance**: Enforces acceptance tracking of legal documents/terms during registration (`LegalDocuments`).
- **Automated Inactivity Deactivation**: Background service (`UserInactivityService`) running scheduled checks to deactivate users idle for 30+ days.

### 🏢 Restaurant & Table Management
- **Restaurant Profile**: Manage details including address, location coordinates, contact info, ratings, and price tiers.
- **Cuisine Mapping**: Categorize restaurants with many-to-many cuisine relationships.
- **Table Capacity Management**: Track restaurant tables, guest capacities, and availability statuses.
- **Operating Hours**: Define operational days and shift timings per restaurant.
- **Media Gallery & Menus**: Multi-part file upload for restaurant banners and food menus, backed by `SixLabors.ImageSharp` image validation, thumbnailing, and static serving from `wwwroot`.

### 📅 Reservation & Booking Engine
- **Availability Checker**: Real-time table availability search filtering by date, time, and party size.
- **Conflict Prevention**: Concurrent booking collision avoidance and automated validation (`BookingConflictException`).
- **Lifecycle Management**: Booking status transitions (`Pending`, `Confirmed`, `Cancelled`, `Completed`).
- **Owner & Customer Portals**: Customers view and manage their personal reservations; owners view restaurant-specific incoming bookings and confirm/cancel them.

### 💳 Payments & Customer Perks
- **Payment Processing**: Payment initiation, status tracking, and demo mock transaction processing for reservations.
- **Customer Wishlist**: Save favorite dining spots to personal bookmarks.
- **Owner Dashboard**: Analytical metrics for restaurant owners (total bookings, revenue, upcoming visits).

---

## 🏛️ Architecture & Design Patterns

- **Clean Layered Architecture**: Clear separation of concerns between Controllers, Services, Repositories, and Data Models.
- **Repository Pattern**: Abstracted database access using generic and domain-specific interfaces (`IRepository` / `Repositories`).
- **Service Layer**: Business logic, transactional validation, and domain orchestration (`IServices` / `Services`).
- **Standardized API Envelope**: Unified response schema (`ApiResponse<T>`) for consistent HTTP outputs across all endpoints.
- **Global Error Handling**: Custom `ExceptionHandlingMiddleware` catching uncaught exceptions and returning structured error payloads.
- **Validation Pipeline**: Dedicated service-level domain validators (`RestaurantValidator`, `BookingValidator`, etc.).
- **Background Worker**: `IHostedService` background worker for background periodic cleanup tasks.

---

## 💻 Tech Stack

| Technology | Purpose |
|---|---|
| **.NET 10 (C# 13)** | Web API runtime and language |
| **ASP.NET Core Web API** | HTTP controller routing and middleware pipeline |
| **Entity Framework Core 10** | Object-Relational Mapper (ORM) |
| **Microsoft SQL Server / LocalDB** | Primary relational database |
| **Microsoft.AspNetCore.Authentication.JwtBearer** | JWT authentication & authorization |
| **Swashbuckle / Microsoft.AspNetCore.OpenApi** | Interactive OpenAPI / Swagger UI documentation |
| **MailKit** | SMTP email delivery for OTP verification |
| **SixLabors.ImageSharp** | Image manipulation, resizing, and validation |
| **Microsoft.Data.Sqlite** | GeoPackage and SQLite inspection support |

---

## 📂 Project Structure

```text
RestaurantBookingAPI/
├── Common/                  # Standardized response envelopes (ApiResponse, ApiResponseHelper)
├── Controllers/             # REST API Controller endpoints
│   ├── AuthController.cs
│   ├── BookingController.cs
│   ├── CuisineController.cs
│   ├── CustomerController.cs
│   ├── LegalDocumentsController.cs
│   ├── OperatingHourControllercs.cs
│   ├── OwnerDashboardController.cs
│   ├── PaymentController.cs
│   ├── RestaurantController.cs
│   ├── RestaurantMediaController.cs
│   ├── RestaurantTableController.cs
│   └── WishlistController.cs
├── Database/                # EF Core AppDbContext & SQL Stored Procedures / Scripts
│   ├── AppDbContext.cs
│   └── Phase4B_Payment_Hardening.sql
├── Exceptions/              # Custom application exceptions (NotFound, BadRequest, BookingConflict)
├── IRepository/             # Repository interfaces
├── IServices/               # Service layer contracts
├── Middleware/              # Custom middlewares (ExceptionHandlingMiddleware, UserActivityMiddleware)
├── Models/                  # Domain entities, DTOs, and Request/Response models
├── Properties/              # Launch profiles and environment configuration
├── Repositories/            # EF Core repository implementations
├── Security/                # Password hashing and JWT generation services
├── Services/                # Application business logic implementations & Hosted services
├── Validation/              # Input and business logic validators
├── wwwroot/                 # Uploaded media assets (images, menus)
├── appsettings.json         # Base configuration (Connection strings, JWT settings)
├── OsmDataInspector.cs      # Spatial GeoPackage data inspector utility
├── Program.cs               # Application entry point, DI container & middleware configuration
├── RestaurantBookingAPI.csproj
└── RestaurantBookingAPI.http # Sample HTTP requests for testing
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [SQL Server](https://www.microsoft.com/sql-server) or [SQL Server Express / LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- [Visual Studio 2022 / 2026](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit

### Configuration

Update your database connection string and JWT keys in `appsettings.json` (or via .NET User Secrets / environment variables):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=RestaurantBookingDB_SP;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Issuer": "RestaurantBookingAPI",
    "Audience": "RestaurantBookingClient",
    "Key": "YOUR_STRONG_SECRET_KEY_HERE"
  },
  "EmailSettings": {
    "SenderEmail": "your-email@example.com",
    "AppPassword": "your-app-password"
  }
}
```

> **Tip:** In production or development environments, secrets like `EmailSettings:AppPassword` and `Jwt:Key` can be securely managed using `dotnet user-secrets`.

### Database Setup

1. Ensure your SQL Server instance is running.
2. Apply database migrations or run the SQL initialization scripts in the `Database/` folder:
   ```bash
   dotnet ef database update
   ```
   *(Or execute the provided SQL scripts against your target database)*

### Running the Project

From the project root directory, run:

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the API
dotnet run
```

By default, the application will launch on:
- **HTTP**: `http://localhost:5074`
- **HTTPS**: `https://localhost:7033`
- **Swagger UI**: `https://localhost:7033/swagger` or `http://localhost:5074/swagger`

---

## 📡 API Endpoints Overview

### Authentication (`/api/auth`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Register a new customer |
| `POST` | `/api/auth/register-restaurant-owner` | Public | Register owner and primary restaurant |
| `POST` | `/api/auth/login` | Public | Authenticate user and receive JWT token |
| `POST` | `/api/auth/forgot-password` | Public | Send password reset OTP to email |
| `POST` | `/api/auth/verify-reset-otp` | Public | Verify reset OTP code |
| `POST` | `/api/auth/reset-password` | Public | Update password using verified OTP token |

### Restaurants (`/api/restaurants`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/restaurants` | Public | List all restaurants |
| `GET` | `/api/restaurants/{id}` | Public | Get restaurant details by ID |
| `POST` | `/api/restaurants` | Admin, Owner | Create a new restaurant |
| `PUT` | `/api/restaurants/{id}` | Admin | Update restaurant details |
| `DELETE` | `/api/restaurants/{id}` | Admin | Remove restaurant |
| `GET` | `/api/restaurants/my-restaurants` | RestaurantOwner | Get restaurants owned by current user |
| `GET` | `/api/restaurants/my-restaurants/{id}`| RestaurantOwner | Get specific owned restaurant |
| `POST` | `/api/restaurants/my-restaurants` | RestaurantOwner | Create restaurant under current owner |
| `PUT` | `/api/restaurants/my-restaurants/{id}`| RestaurantOwner | Update owned restaurant |
| `DELETE` | `/api/restaurants/my-restaurants/{id}`| RestaurantOwner | Delete owned restaurant |

### Media Management (`/api/restaurants/{restaurantId}`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/restaurants/{id}/images` | Public | Get restaurant images |
| `POST` | `/api/restaurants/{id}/images` | RestaurantOwner | Upload restaurant image (multipart/form-data) |
| `DELETE` | `/api/restaurants/{id}/images/{imageId}` | RestaurantOwner | Delete restaurant image |
| `PUT` | `/api/restaurants/{id}/images/{imageId}/primary` | RestaurantOwner | Set image as primary thumbnail |
| `GET` | `/api/restaurants/{id}/menu-images` | Public | Get restaurant menu images |
| `POST` | `/api/restaurants/{id}/menu-images` | RestaurantOwner | Upload menu image |
| `DELETE` | `/api/restaurants/{id}/menu-images/{imageId}` | RestaurantOwner | Delete menu image |

### Bookings & Reservations (`/api/bookings`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/bookings/availability` | Public | Check table availability |
| `GET` | `/api/bookings/my-bookings` | Customer | Get customer's booking history |
| `GET` | `/api/bookings/{id}` | Customer | Get booking details |
| `POST` | `/api/bookings` | Customer | Create a new reservation |
| `PUT` | `/api/bookings/{id}` | Customer | Update reservation |
| `POST` | `/api/bookings/{id}/cancel` | Customer | Cancel a booking |
| `GET` | `/api/bookings/my-restaurant-bookings` | RestaurantOwner | Get bookings for owned restaurants |
| `POST` | `/api/bookings/{id}/owner-confirm` | RestaurantOwner | Owner accepts/confirms reservation |
| `POST` | `/api/bookings/{id}/owner-cancel` | RestaurantOwner | Owner rejects/cancels reservation |

### Payments (`/api/payments`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/payments/initiate/{bookingId}` | Customer | Initiate payment for reservation |
| `GET` | `/api/payments/history` | Customer | View personal transaction history |
| `GET` | `/api/payments/{paymentId}` | Customer | Get single payment record |
| `POST` | `/api/payments/{paymentId}/process-demo` | Customer | Simulate payment transaction completion |

### Owner Dashboard (`/api/owner/dashboard` or `/api/owner-dashboard`)
| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/owner/dashboard` | RestaurantOwner, Admin | View performance metrics, counts, and revenue |

### Other Endpoints
- **Cuisines**: `GET /api/cuisines` - List supported cuisine categories.
- **Operating Hours**: `GET/POST/PUT/DELETE /api/operating-hours` - Manage shifts and daily operating schedules.
- **Restaurant Tables**: `GET/POST/PUT/DELETE /api/restaurant-tables` - Manage table counts, numbers, and capacities.
- **Customer Profiles**: `GET/PUT /api/customers/me` - Profile view and updates.
- **Wishlist**: `GET/POST/DELETE /api/wishlist` - Manage favorite restaurants.
- **Legal Documents**: `GET /api/legal-documents/terms` - Fetch latest terms and conditions.

---

## 🔒 Security & Authentication

All protected endpoints require a JWT token supplied in the HTTP `Authorization` header:

```http
Authorization: Bearer <your-jwt-token>
```

In the Swagger UI, click the **Authorize** button at the top right and enter your Bearer token to test authenticated routes.

---

## 🌐 Frontend Integration (CORS)

The API includes a pre-configured CORS policy (`AngularDevelopment`) enabling requests from:
- `http://localhost:4200` (Default Angular development server)

Custom CORS policies can be adjusted in [Program.cs](file:///c:/Users/Aspire/Desktop/RestaurantBookingAPI/Program.cs).

---

## 📄 License

This project is licensed under the [MIT License](LICENSE) (or proprietary as specified by the project owner).
