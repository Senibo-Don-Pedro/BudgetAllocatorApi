# .NET / C# Engineering Reference & Build Log (Java/Spring Boot Contrast)

Project: BudgetAllocator (ASP.NET Core Web API, EF Core, SQL Server in Docker)

---

## Part 1: C# Syntax & Language Essentials

### 1. Variable Declarations

- **Explicit typing:** `decimal salary = 5000.00m;` (the `m` suffix is required for `decimal` literals; `d` or no suffix means `double`).
- **Implicit typing (`var`):** `var profile = new IncomeProfile();` Same as Java 10+ `var`. Strongly typed at compile time, not dynamic like JS `let`.
- **Target-typed `new()` (C# 9+):** `public List<BudgetCategory> Categories { get; set; } = new();` Like Java's `new ArrayList<>()`, the type is inferred from the left side.

### 2. The Fat Arrow (`=>`): Two Meanings

**a) Lambda expression** (like Java `->` or TS `=>`). Used in LINQ and the EF Core Fluent API to pass an anonymous function or expression tree:

```csharp
profile.Property(x => x.SourceName).HasMaxLength(100);
var highCategories = categories.Where(c => c.Percentage > 20m).ToList();
```

**b) Expression-bodied member** (one-line shorthand). Replaces `{ return ...; }` when a method, constructor, or read-only property is a single expression:

```csharp
// Full syntax
public decimal CalculateAmount(decimal income, decimal pct) { return income * (pct / 100m); }

// Expression-bodied
public decimal CalculateAmount(decimal income, decimal pct) => income * (pct / 100m);
```

### 3. Null Safety & Operators

| Syntax     | Meaning                                                                                                                                           |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `string`   | Non-nullable reference (compiler warning if null)                                                                                                 |
| `string?`  | Nullable reference (can hold null)                                                                                                                |
| `= null!;` | Null-forgiving operator. Tells the compiler "EF Core or the serializer will populate this at runtime", which suppresses the uninitialized warning |
| `?.`       | Safe navigation: `user?.Name` returns null instead of throwing `NullReferenceException`                                                           |
| `??`       | Null-coalescing: `var currency = inputCurrency ?? "NGN";` (like Java `Objects.requireNonNullElse(input, "NGN")`)                                  |

### 4. Dependency Injection Lifetimes (ASP.NET Core vs Spring Boot)

| .NET Registration   | Spring Equivalent     | Lifetime Behavior                                                      |
| ------------------- | --------------------- | ---------------------------------------------------------------------- |
| `AddSingleton<T>()` | `@Scope("singleton")` | Created once for the whole app lifetime. Must be thread-safe.          |
| `AddScoped<T>()`    | `@RequestScope`       | Created once per HTTP request. Standard for `DbContext` and services.  |
| `AddTransient<T>()` | `@Scope("prototype")` | Created every time it is injected. For lightweight, stateless helpers. |

---

## Part 2: Step-by-Step Build Log

### Step 1: Solution, Project Setup & Configuration

**CLI commands**

```bash
dotnet new sln -n BudgetAllocator                                    # creates the workspace container (.slnx)
dotnet new webapi -n BudgetAllocator.Api --use-controllers           # Web API project with controller routing
dotnet sln add BudgetAllocator.Api/BudgetAllocator.Api.csproj        # links the project to the solution
dotnet add package Microsoft.EntityFrameworkCore.SqlServer           # SQL Server EF provider
dotnet add package Microsoft.EntityFrameworkCore.Design              # CLI migration tooling
```

**Key files**

- `.csproj`: MSBuild file (like `pom.xml`). Automatically globs all `**/*.cs` files.
- `Properties/launchSettings.json`: local dev profile (ports and `ASPNETCORE_ENVIRONMENT=Development`).
- `appsettings.Development.json`: local overrides (like `application-dev.yml`). The SQL Server connection string lives under `"ConnectionStrings": { "DefaultConnection": "Server=localhost,1433;..." }`. Note the **comma** before the port.

### Step 2: EF Core Entities, DbContext & Migrations

**Entities** (`Entities/IncomeProfile.cs`, `Entities/BudgetCategory.cs`)

- Plain C# classes (POCOs). No `@Entity` or `@Id` annotations needed.
- `Id` (`Guid`) is detected as the primary key by convention (`uniqueidentifier` in SQL Server).
- `IncomeProfileId` (`Guid`) plus an `IncomeProfile` navigation property on `BudgetCategory` is detected as a foreign key with `ON DELETE CASCADE`.
- Financial values always use `decimal` (`HasPrecision(18, 2)`), never `double`.

**DbContext** (`Data/AppDbContext.cs`)

- Inherits from `DbContext`. Combines JPA's `EntityManager` and the repository pattern through `DbSet<T>`.
- Registered in `Program.cs` with `builder.Services.AddDbContext<AppDbContext>(...)`, which gives it a Scoped lifetime.

**Migrations**

- `dotnet ef migrations add InitialCreate`: compares the C# classes to the model snapshot and generates C# `Up()` / `Down()` migration code.
- `dotnet ef database update`: applies pending migrations to SQL Server and records them in `__EFMigrationsHistory`.

---

## Progress Summary

- **Done:** Step 1 (workspace, CLI, `.csproj`, `appsettings.Development.json`, `Program.cs` DI) and Step 2 (entities with `Guid` and `decimal`, `AppDbContext`, Fluent API precision, initial SQL Server migration).
- **Areas to watch:**
  - `decimal` in C# vs `BigDecimal` in Java.
  - EF Core loading: lazy loading is off by default, so related data must be loaded explicitly with `.Include()` (eager loading). Hibernate defaults to lazy loading.
  - The three DI lifetimes, especially Scoped vs Singleton.
- **Up next:** Step 3 (DTOs, validation, service layer, CRUD controller).

### Step 3: DTOs, LINQ, Service Layer & Controllers

- **Object Initializer Syntax (`new ClassName { ... }`):**
  - Calling methods or constructors with arguments uses `()`.
  - Instantiating a class and setting properties inline uses `{ Prop = value }` (empty `()` is optional).
- **`default` Keyword in Generics (`T? data = default`):**
  - Returns the default zeroed value for any generic type `T`: `null` for reference types (classes), `0` for numeric types (`int`, `decimal`), `false` for `bool`, and `Guid.Empty` for `Guid`.
- **Money Literal Suffix (`m`):**
  - `100m` or `0.08m` denotes a `decimal` literal. Without `m`, decimals like `0.08` are treated as `double` (64-bit binary float), and C# forbids multiplying `decimal * double`.
- **Naming & Class Conventions:**
  - Interfaces always start with `I` (`IIncomeProfileService`) because C# uses `:` for both `extends` and `implements`.
  - Private fields use `_camelCase` (`private readonly AppDbContext _db;`) so `this.` is never needed in constructors.
  - Helper methods that don't access instance fields (`_db`) are marked `private static` to avoid passing a hidden `this` pointer and to cache method-group delegates (`profiles.Select(MapToResponse)`).
- **LINQ Selector Overloads:**
  - Instead of Java's `.stream().map(c -> c.getPercentage()).reduce(...)`, LINQ passes the selector lambda directly into aggregation methods: `request.Categories.Sum(c => c.Percentage)`.
- **EF Core Read vs Write Behavior:**
  - `.AsNoTracking()` skips ChangeTracker snapshot caching for read-only (`GET`) queries (equivalent to Spring's `@Transactional(readOnly = true)`).
  - `.Include(p => p.Categories)` performs an eager SQL `LEFT JOIN` (EF Core has lazy loading turned OFF by default).
  - `await _db.SaveChangesAsync()` automatically wraps all staged inserts/updates/deletes in an atomic database transaction.
- **Controller Conventions (`ControllerBase`):**
  - `[ApiController]` automatically validates `[Required]` / `[Range]` DataAnnotations on `[FromBody]` DTOs and returns `400 Bad Request` if invalid (no `@Valid` needed).
  - Inheriting from `ControllerBase` provides HTTP response helpers (`Ok()`, `BadRequest()`, `NotFound()`, `CreatedAtAction()`).

### Step 4 & 5: External HTTP Calls, Options Pattern & Enums
* **Options Pattern (`IOptions<T>`):**
  * Binds a JSON section in `appsettings.json` (e.g., `"ExchangeRateApi"`, `"Jwt"`) to a strongly-typed C# class (`Models/Config/JwtConfig.cs`) via `builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"))`.
* **Typed `HttpClient` (`AddHttpClient<TInterface, TImplementation>()`):**
  * Never instantiate `new HttpClient()` manually inside a service (causes socket exhaustion and ignores DNS rotation). `AddHttpClient` uses `IHttpClientFactory` under the hood to pool TCP connections safely.
* **`Dictionary<TKey, TValue>` & `TryGetValue` (`out var`):**
  * Equivalent to Java's `HashMap<K, V>`. Accessing a missing key with `dict["key"]` throws a `KeyNotFoundException`. Always use `dict.TryGetValue(key, out var val)` to check existence and retrieve the value in a single hash lookup.
* **Enums as Strings in JSON & EF Core:**
  * `[JsonConverter(typeof(JsonStringEnumConverter))]` on an `enum` makes JSON serialization and Swagger UI render string names (`"NGN"`, `"Admin"`) instead of integers (`0`, `1`).
  * `.HasConversion<string>()` in `AppDbContext.OnModelCreating` stores the enum as `nvarchar` in SQL Server instead of `int`.

### Step 6: Security — JWT, Refresh Token Rotation & Role-Based Auth
* **ASP.NET Core Auth vs. Spring Security:**
  * Replaces custom `OncePerRequestFilter` + `SecurityFilterChain` with built-in `AddAuthentication().AddJwtBearer(...)` and `app.UseAuthentication()` / `app.UseAuthorization()`.
  * Middleware order is strict: `app.UseAuthentication()` (who are you?) MUST come before `app.UseAuthorization()` (are you allowed?).
* **Protecting Endpoints:**
  * `[Authorize]` requires a valid JWT (`401 Unauthorized` if missing/invalid).
  * `[Authorize(Roles = nameof(RoleEnum.Admin))]` checks the `ClaimTypes.Role` claim in the JWT (`403 Forbidden` if role doesn't match).
  * `[AllowAnonymous]` bypasses `[Authorize]` for public endpoints like `/login` and `/refresh`.
* **Reading Claims in Controllers:**
  * `ControllerBase.User` (`ClaimsPrincipal`) is the equivalent of `SecurityContextHolder`. Read claims with `User.FindFirstValue(ClaimTypes.Email)`.
* **Refresh Token Rotation:**
  * Short-lived stateless JWT Access Tokens (e.g., 60 mins) are paired with a long-lived stateful Refresh Token stored in SQL Server (`RefreshTokens` table). Every time `/auth/refresh` is called, the old Refresh Token is marked `IsRevoked = true` and a new pair is issued.