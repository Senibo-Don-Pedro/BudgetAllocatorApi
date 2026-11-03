# Budget Allocator API — v1 Contract Specification

## 1. Base URL, Versioning & Documentation

- **Local Base URL:** `http://localhost:5008/api/v1`
- **Interactive Swagger UI:** `http://localhost:5008/swagger`
- **OpenAPI Spec JSON:** `http://localhost:5008/swagger/v1/swagger.json`
- **Content-Type:** `application/json; charset=utf-8`

---

## 2. Standard Response Envelope (`BaseResponse<T>`)

Every API endpoint returns responses wrapped in a standardized JSON envelope. All envelope `timestamp` values are generated in **West Africa Time (`Africa/Lagos` / WAT, UTC+1)**.

```json
{
  "success": true,
  "message": "Human-readable status or error description",
  "data": { ... },
  "timestamp": "2026-11-03T08:30:00.123456"
}
```

### Supported Domain Enums

- **`CurrencyEnum`:** `"NGN"`, `"USD"`, `"GBP"`, `"EUR"`, `"CAD"`, `"GHS"`, `"KES"`, `"ZAR"`
- **`RoleEnum`:** `"User"`, `"Admin"`

---

## 3. Authentication & Security

Protected endpoints require a valid HMAC-SHA256 JWT passed in the HTTP `Authorization` header:

```http
Authorization: Bearer <accessToken>
```

| HTTP Status        | Meaning                                                                                                              |
| :----------------- | :------------------------------------------------------------------------------------------------------------------- |
| `401 Unauthorized` | Missing, expired, or invalid JWT access token (or invalid/revoked refresh token).                                    |
| `403 Forbidden`    | Valid JWT, but the authenticated user lacks the required `RoleEnum` (e.g., `"User"` attempting an `"Admin"` action). |

---

## 4. Authentication Endpoints (`/api/v1/auth`)

### 4.1 Login (Issue Access & Refresh Tokens)

Authenticates a user and returns a short-lived stateless JWT Access Token along with a stateful 64-byte Refresh Token stored in SQL Server.

- **Endpoint:** `POST /api/v1/auth/login`
- **Auth Required:** No (`[AllowAnonymous]`)

#### Request Body

```json
{
  "email": "engineer@lagos.dev",
  "password": "Password123!",
  "role": "Admin"
}
```

#### Success Response (`200 OK`)

```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "8f9d2a7b4c1e6f3a9b0d8e7c6a5b4f3e2d1c0b9a8f7e6d5c4b3a2f1e0d9c8b7a...",
    "email": "engineer@lagos.dev",
    "role": "Admin",
    "expiresAtUtc": "2026-11-03T08:35:00Z",
    "refreshTokenExpiresAtUtc": "2026-11-10T07:35:00Z"
  },
  "timestamp": "2026-11-03T08:35:00.512268"
}
```

---

### 4.2 Refresh Token (Token Rotation)

Exchanges a valid, unrevoked Refresh Token for a new JWT Access Token and a newly rotated Refresh Token. The consumed Refresh Token is immediately marked `IsRevoked = true` to prevent replay attacks.

- **Endpoint:** `POST /api/v1/auth/refresh`
- **Auth Required:** No (`[AllowAnonymous]`)

#### Request Body

```json
{
  "refreshToken": "8f9d2a7b4c1e6f3a9b0d8e7c6a5b4f3e2d1c0b9a8f7e6d5c4b3a2f1e0d9c8b7a..."
}
```

#### Success Response (`200 OK`)

```json
{
  "success": true,
  "message": "Token refreshed successfully.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b...",
    "email": "engineer@lagos.dev",
    "role": "Admin",
    "expiresAtUtc": "2026-11-03T09:35:00Z",
    "refreshTokenExpiresAtUtc": "2026-11-10T08:35:00Z"
  },
  "timestamp": "2026-11-03T08:36:12.104921"
}
```

#### Error Response (`401 Unauthorized` — Expired, Missing, or Already Rotated Token)

```json
{
  "success": false,
  "message": "Invalid or expired refresh token.",
  "data": null,
  "timestamp": "2026-11-03T08:36:45.992104"
}
```

---

### 4.3 Revoke Refresh Token (Logout)

Explicitly invalidates an active Refresh Token so it can no longer be used to mint new access tokens.

- **Endpoint:** `POST /api/v1/auth/revoke`
- **Auth Required:** Yes (`[Authorize]`)

#### Request Body

```json
{
  "refreshToken": "1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b..."
}
```

#### Success Response (`200 OK`)

```json
{
  "success": true,
  "message": "Refresh token revoked (logged out).",
  "data": true,
  "timestamp": "2026-11-03T08:37:10.331029"
}
```

#### Error Response (`400 Bad Request`)

```json
{
  "success": false,
  "message": "Refresh token not found or already revoked.",
  "data": false,
  "timestamp": "2026-11-03T08:37:22.881203"
}
```

---

### 4.4 Get Current Authenticated User (`/me`)

Extracts and returns the authenticated user's claims (`Sub` / `NameIdentifier`, `Email`, and `Role`) directly from the bearer JWT payload.

- **Endpoint:** `GET /api/v1/auth/me`
- **Auth Required:** Yes (`[Authorize]`)

#### Success Response (`200 OK`)

```json
{
  "success": true,
  "message": "Success",
  "data": {
    "userId": "6c85f1a2-9b3d-4e7a-8c1f-2d3e4f5a6b7c",
    "email": "engineer@lagos.dev",
    "role": "Admin"
  },
  "timestamp": "2026-11-03T08:38:00.412890"
}
```

---

## 5. Income Profile Endpoints (`/api/v1/income-profiles`)

### 5.1 Create Income Profile

Creates a monthly net income profile and automatically computes the monetary allocation (`allocatedAmount`) for each budget category.

- **Endpoint:** `POST /api/v1/income-profiles`
- **Auth Required:** Yes (`[Authorize]`)
- **Validation & Domain Rules:**
  - `sourceName`: Required, max `100` characters.
  - `monthlyNetIncome`: Required, `>= 0.01`.
  - `currency`: Required, must be a valid `CurrencyEnum` string (`"NGN"`, `"USD"`, `"GBP"`, `"EUR"`, `"CAD"`, `"GHS"`, `"KES"`, `"ZAR"`).
  - `categories`: Required, minimum `1` category item.
  - `categories[].name`: Required, max `100` characters.
  - `categories[].percentage`: Required, between `0.01` and `100.00`.
  - **Business Rule:** The sum of all `categories[].percentage` values **cannot exceed `100%`**.

#### Request Body

```json
{
  "sourceName": "Main Software Engineering Salary",
  "monthlyNetIncome": 850000,
  "currency": "NGN",
  "categories": [
    { "name": "Rent & Utilities", "percentage": 30 },
    { "name": "Investments & Savings", "percentage": 40 },
    { "name": "Food & Living", "percentage": 20 }
  ]
}
```

#### Success Response (`201 Created`)

- **Headers:** `Location: http://localhost:5008/api/v1/income-profiles/d4994d53-a195-496e-81cd-310d5ca715f1`

```json
{
  "success": true,
  "message": "Income profile created successfully.",
  "data": {
    "id": "d4994d53-a195-496e-81cd-310d5ca715f1",
    "sourceName": "Main Software Engineering Salary",
    "monthlyNetIncome": 850000.0,
    "currency": "NGN",
    "createdAtUtc": "2026-11-01T21:53:47.951477Z",
    "categories": [
      {
        "id": "ca8ab567-d3b5-49a0-9cc7-0dbf75355485",
        "name": "Rent & Utilities",
        "percentage": 30.0,
        "allocatedAmount": 255000.0
      },
      {
        "id": "c7e52175-8e01-4dad-95f9-12f232d4dc89",
        "name": "Investments & Savings",
        "percentage": 40.0,
        "allocatedAmount": 340000.0
      },
      {
        "id": "373ddedb-aabd-40e6-89e6-72c78bab4739",
        "name": "Food & Living",
        "percentage": 20.0,
        "allocatedAmount": 170000.0
      }
    ]
  },
  "timestamp": "2026-11-01T22:53:48.512268"
}
```

#### Error Response (`400 Bad Request` — Category Percentage Exceeds 100%)

```json
{
  "success": false,
  "message": "Total category percentage (120%) cannot exceed 100%.",
  "data": null,
  "timestamp": "2026-11-01T22:56:14.541694"
}
```

---

### 5.2 Get All Income Profiles

Retrieves all saved income profiles ordered from newest to oldest (`createdAtUtc` descending), eagerly loading their associated budget categories.

- **Endpoint:** `GET /api/v1/income-profiles`
- **Auth Required:** Yes (`[Authorize]`)

#### Success Response (`200 OK`)

```json
{
  "success": true,
  "message": "Success",
  "data": [
    {
      "id": "d4994d53-a195-496e-81cd-310d5ca715f1",
      "sourceName": "Main Software Engineering Salary",
      "monthlyNetIncome": 850000.0,
      "currency": "NGN",
      "createdAtUtc": "2026-11-01T21:53:47.951477",
      "categories": [
        {
          "id": "ca8ab567-d3b5-49a0-9cc7-0dbf75355485",
          "name": "Rent & Utilities",
          "percentage": 30.0,
          "allocatedAmount": 255000.0
        }
      ]
    }
  ],
  "timestamp": "2026-11-03T08:40:12.112045"
}
```

---

### 5.3 Get Income Profile by ID

Retrieves a single income profile and its budget categories by its unique `Guid`.

- **Endpoint:** `GET /api/v1/income-profiles/{id}`
- **Auth Required:** Yes (`[Authorize]`)
- **Path Parameters:** `id` (`Guid` / `uuid`, required)

#### Success Response (`200 OK`)

Returns `BaseResponse<IncomeProfileResponse>` (same payload shape as section `5.1`).

#### Error Response (`404 Not Found`)

```json
{
  "success": false,
  "message": "Income profile not found.",
  "data": null,
  "timestamp": "2026-11-03T08:41:05.774120"
}
```

---

### 5.4 Convert Income Profile to Target Currency (Live FX)

Fetches real-time exchange rates from `open.er-api.com` and converts both the `monthlyNetIncome` and every category's `allocatedAmount` from the profile's original currency into the specified `targetCurrency`.

- **Endpoint:** `GET /api/v1/income-profiles/{id}/convert?targetCurrency=USD`
- **Auth Required:** Yes (`[Authorize]`)
- **Path Parameters:** `id` (`Guid` / `uuid`, required)
- **Query Parameters:** `targetCurrency` (`CurrencyEnum`, optional — defaults to `"USD"`)

#### Success Response (`200 OK`)

```json
{
  "success": true,
  "message": "Success",
  "data": {
    "profileId": "d4994d53-a195-496e-81cd-310d5ca715f1",
    "sourceName": "Main Software Engineering Salary",
    "originalCurrency": "NGN",
    "originalMonthlyNetIncome": 850000.0,
    "targetCurrency": "USD",
    "exchangeRate": 0.00074159,
    "convertedMonthlyNetIncome": 630.35,
    "convertedCategories": [
      {
        "id": "ca8ab567-d3b5-49a0-9cc7-0dbf75355485",
        "name": "Rent & Utilities",
        "percentage": 30.0,
        "allocatedAmount": 189.11
      },
      {
        "id": "c7e52175-8e01-4dad-95f9-12f232d4dc89",
        "name": "Investments & Savings",
        "percentage": 40.0,
        "allocatedAmount": 252.14
      },
      {
        "id": "373ddedb-aabd-40e6-89e6-72c78bab4739",
        "name": "Food & Living",
        "percentage": 20.0,
        "allocatedAmount": 126.07
      }
    ]
  },
  "timestamp": "2026-11-03T08:42:30.229104"
}
```

#### Error Response (`400 Bad Request` — Profile Not Found or Upstream FX Rate Unavailable)

```json
{
  "success": false,
  "message": "Income profile not found.",
  "data": null,
  "timestamp": "2026-11-03T08:43:01.119023"
}
```

---

### 5.5 Delete Income Profile (Admin Only)

Deletes an income profile by ID and cascades deletion to all associated `BudgetCategories` in SQL Server.

- **Endpoint:** `DELETE /api/v1/income-profiles/{id}`
- **Auth Required:** Yes (`[Authorize(Roles = "Admin")]`) — Requires `"Admin"` role in JWT claims.
- **Path Parameters:** `id` (`Guid` / `uuid`, required)

#### Success Response (`200 OK`)

```json
{
  "success": true,
  "message": "Income profile deleted successfully.",
  "data": true,
  "timestamp": "2026-11-03T08:44:15.601928"
}
```

#### Error Response (`404 Not Found`)

```json
{
  "success": false,
  "message": "Income profile not found.",
  "data": false,
  "timestamp": "2026-11-03T08:44:50.339182"
}
```
