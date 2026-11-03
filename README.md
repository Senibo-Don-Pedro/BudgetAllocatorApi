# 💰 Budget Allocator API

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge&logo=csharp&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?style=for-the-badge&logo=nuget&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-%2B%20Refresh%20Tokens-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)

A production-grade **ASP.NET Core (.NET 10) RESTful Web API** for personal and enterprise budget allocation. It enables users to define monthly net income profiles, automatically calculate monetary splits across customizable budget categories, convert allocations across international currencies using real-time exchange rates, and secure endpoints using **JWT Authentication with Refresh Token Rotation** and **Role-Based Access Control (RBAC)**.

---

## ✨ Key Architectural Highlights

- **Clean Layered Architecture:** Strict separation of concerns across `Controllers`, `Services`, `Interfaces`, `Entities`, `Data` (EF Core DbContext), and `Models` (`Config`, `DTOs`, `Enums`).
- **Standardized API Envelope (`BaseResponse<T>`):** Every API response (success or failure) is wrapped in a predictable JSON envelope localized to **West Africa Time (`Africa/Lagos`)**.
- **Financial Precision:** All monetary and percentage calculations use 128-bit base-10 `decimal` types backed by explicit SQL Server `decimal(18,2)` and `decimal(5,2)` column precision to prevent floating-point rounding errors.
- **Resilient External HTTP Integration:** Uses `IHttpClientFactory` (Typed `HttpClient`) paired with the **Options Pattern (`IOptions<T>`)** to fetch real-time FX rates without risking socket exhaustion or stale DNS caching.
- **Enterprise Security & Token Rotation:** Stateless HMAC-SHA256 JWT access tokens paired with stateful, cryptographically secure 64-byte Refresh Tokens persisted in SQL Server. Implements one-time **Refresh Token Rotation** (`IsRevoked`) and role-based endpoint protection (`[Authorize(Roles = "Admin")]`).
- **Strictly-Typed Domain Enums:** Enforces valid ISO-4217 currency codes (`CurrencyEnum`) and security roles (`RoleEnum`) at the API contract boundary and persists them as readable strings in SQL Server via EF Core value converters.

---

## 🏗️ Project Structure

```text
BudgetAllocator/
├── API_CONTRACT.md                            # Formal v1 API contract specification
├── LEARNING_NOTES.md                          # Architecture & Java-to-C# engineering notes
├── README.md                                  # Project documentation
└── BudgetAllocator.Api/
    ├── Controllers/
    │   ├── AuthController.cs                  # Login, Refresh Token rotation, Logout, & Claims inspection
    │   └── IncomeProfilesController.cs        # CRUD + Live FX conversion endpoints
    ├── Data/
    │   └── AppDbContext.cs                    # EF Core DbContext, Fluent API precision, & Enum converters
    ├── Entities/
    │   ├── BudgetCategory.cs                  # Child entity (1-to-Many with IncomeProfile)
    │   ├── IncomeProfile.cs                   # Parent entity representing an income source
    │   └── RefreshToken.cs                    # Stateful refresh token entity for token rotation
    ├── Interfaces/
    │   ├── IAuthService.cs                    # Contract for authentication & token management
    │   ├── ICurrencyExchangeService.cs        # Contract for external FX rate provider
    │   └── IIncomeProfileService.cs           # Contract for budget profile business logic
    ├── Migrations/                            # EF Core Code-First SQL Server migrations
    ├── Models/
    │   ├── Config/
    │   │   ├── ExchangeRateApiConfig.cs       # Strongly-typed Options binding for FX API
    │   │   └── JwtConfig.cs                   # Strongly-typed Options binding for JWT settings
    │   ├── DTO/
    │   │   ├── Request/                       # Validated request payloads (CreateIncomeProfileRequest, etc.)
    │   │   └── Response/                      # Clean API response projections & BaseResponse<T> envelope
    │   └── Enum/
    │       ├── CurrencyEnum.cs                # Supported ISO currencies (NGN, USD, GBP, EUR, CAD, GHS, KES, ZAR)
    │       └── RoleEnum.cs                    # Supported RBAC roles (User, Admin)
    ├── Services/
    │   ├── AuthService.cs                     # JWT generation, Refresh Token rotation & revocation
    │   ├── CurrencyExchangeService.cs         # Typed HttpClient integration with open.er-api.com
    │   └── IncomeProfileService.cs            # Core domain validation, allocation math & EF Core queries
    ├── Program.cs                             # DI container registration & HTTP middleware pipeline
    └── appsettings.json                       # Configuration template
```

---

## 🛠️ Tech Stack

| Layer                 | Technology                                                                |
| :-------------------- | :------------------------------------------------------------------------ |
| **Framework**         | ASP.NET Core Web API (.NET 10.0)                                          |
| **Language**          | C# (Nullable Reference Types enabled)                                     |
| **ORM & Database**    | Entity Framework Core 10 + Microsoft SQL Server 2022 (Docker)             |
| **Authentication**    | `Microsoft.AspNetCore.Authentication.JwtBearer` + Stateful Refresh Tokens |
| **External HTTP**     | Typed `HttpClient` (`IHttpClientFactory`) + `System.Net.Http.Json`        |
| **API Documentation** | Swashbuckle Swagger UI (`/swagger`) with JWT Bearer Security Scheme       |

---

## 🚀 Getting Started Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for running SQL Server locally)
- `dotnet-ef` CLI tool (`dotnet tool install --global dotnet-ef`)

### 1. Start SQL Server in Docker

Spin up a local SQL Server 2022 container on port `1433`:

```bash
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong!Password123" \
  -p 1433:1433 \
  --name sqlserver-budget \
  -d [mcr.microsoft.com/mssql/server:2022-latest](https://mcr.microsoft.com/mssql/server:2022-latest)
```

### 2. Configure `appsettings.Development.json`

Create an `appsettings.Development.json` file inside `BudgetAllocator.Api/` (this file is git-ignored to protect local secrets):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=BudgetAllocatorDb;User Id=sa;Password=YourStrong!Password123;TrustServerCertificate=True;"
  },
  "ExchangeRateApi": {
    "BaseUrl": "[https://open.er-api.com/v6/latest/](https://open.er-api.com/v6/latest/)"
  },
  "Jwt": {
    "Key": "SuperSecretKeyForBudgetAllocatorApi2026!MustBeAtLeast32BytesLong",
    "Issuer": "BudgetAllocator.Api",
    "Audience": "BudgetAllocator.Client",
    "ExpiryMinutes": 60
  }
}
```

### 3. Apply Database Migrations & Run the API

```bash
cd BudgetAllocator.Api

# Apply EF Core migrations to create BudgetAllocatorDb and tables
dotnet ef database update

# Start the Kestrel server
dotnet run
```

Once started, open your browser to the interactive **Swagger UI**:
👉 **`http://localhost:5008/swagger`**

---

## 🔐 Authentication & Testing Workflow (Swagger UI)

1. **Generate Tokens (`POST /api/v1/auth/login`):**
   - Execute the `/api/v1/auth/login` endpoint with `"role": "Admin"` (or `"User"`).
   - Copy the `accessToken` and `refreshToken` from the response body.
2. **Authorize in Swagger:**
   - Click the **Authorize** padlock button at the top right of Swagger UI.
   - Paste your `accessToken` into the value box and click **Authorize**.
3. **Test Protected Endpoints:**
   - Create an income profile (`POST /api/v1/income-profiles`), fetch profiles (`GET /api/v1/income-profiles`), or convert currencies (`GET /api/v1/income-profiles/{id}/convert?targetCurrency=USD`).
4. **Test Refresh Token Rotation (`POST /api/v1/auth/refresh`):**
   - Pass your `refreshToken` to receive a new JWT and a rotated refresh token. Attempting to reuse the old refresh token a second time will immediately return `401 Unauthorized`.

---

## 📡 API Endpoints Overview

| Method   | Endpoint                               | Auth                           | Description                                                           |
| :------- | :------------------------------------- | :----------------------------- | :-------------------------------------------------------------------- |
| `POST`   | `/api/v1/auth/login`                   | Public                         | Authenticates user and issues a signed JWT + Refresh Token            |
| `POST`   | `/api/v1/auth/refresh`                 | Public                         | Rotates a valid Refresh Token and issues a new JWT pair               |
| `POST`   | `/api/v1/auth/revoke`                  | `[Authorize]`                  | Revokes an active Refresh Token (Logout)                              |
| `GET`    | `/api/v1/auth/me`                      | `[Authorize]`                  | Returns the authenticated user's ID, email, and role from JWT claims  |
| `POST`   | `/api/v1/income-profiles`              | `[Authorize]`                  | Creates a monthly income profile and calculates category allocations  |
| `GET`    | `/api/v1/income-profiles`              | `[Authorize]`                  | Lists all saved income profiles and their budget categories           |
| `GET`    | `/api/v1/income-profiles/{id}`         | `[Authorize]`                  | Retrieves a single income profile by `Guid`                           |
| `GET`    | `/api/v1/income-profiles/{id}/convert` | `[Authorize]`                  | Converts an income profile into a target currency using live FX rates |
| `DELETE` | `/api/v1/income-profiles/{id}`         | `[Authorize(Roles = "Admin")]` | Deletes an income profile and cascades to its categories              |

_(For full request/response JSON schemas and domain validation rules, see [`API_CONTRACT.md`](./API_CONTRACT.md))._

---

## 🗺️ Future Roadmap

- [ ] Global Exception Handling Middleware (`IExceptionHandler`) for standardized `500 Internal Server Error` envelopes
- [ ] Server-side Pagination (`PagedData<T>`) with `.Skip()` and `.Take()` on `GET /api/v1/income-profiles`
- [ ] Unit & Integration Testing suite using `xUnit`, `Moq`, and `FluentAssertions`
- [ ] Caching exchange rate responses using `IMemoryCache` / Redis to reduce external HTTP latency
