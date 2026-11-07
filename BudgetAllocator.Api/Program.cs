using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using BudgetAllocator.Api.Data;
using BudgetAllocator.Api.Exceptions;
using BudgetAllocator.Api.Interfaces;
using BudgetAllocator.Api.Models.Config;
using BudgetAllocator.Api.Models.DTO.Response.Generic;
using BudgetAllocator.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// 0. Configure Clean & Structured Console Logs (Serilog)
// 0. Production-Grade Structured Logging (Serilog)
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Fatal)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", p => p.StartsWith("/swagger")))
        .Enrich.FromLogContext();

    if (context.HostingEnvironment.IsDevelopment())
        // Local Dev: Clean human-readable console with short RequestId correlation
        loggerConfig.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{RequestId}] {Message:lj}{NewLine}")
            .WriteTo.Seq("http://localhost:5341");
    else
        // Production: Structured single-line JSON for Datadog / ELK / CloudWatch
        loggerConfig.WriteTo.Console(new RenderedCompactJsonFormatter());
});

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 1. Controllers + Enum-as-String JSON Converter
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

// 2. Database (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Config Bindings (Options Pattern)
builder.Services.Configure<ExchangeRateApiConfig>(
    builder.Configuration.GetSection("ExchangeRateApi"));
builder.Services.Configure<JwtConfig>(
    builder.Configuration.GetSection("Jwt"));

// 4. Services & HttpClient Registration
builder.Services.AddScoped<IIncomeProfileService, IncomeProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient<ICurrencyExchangeService, CurrencyExchangeService>();

// 5. JWT Authentication & Authorization
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtConfig>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
        
        // Custom JSON envelope for 401 Unauthorized & 403 Forbidden
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                // Suppress the default empty 401 response
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var message = context.AuthenticateFailure is SecurityTokenExpiredException
                    ? "Your access token has expired. Please refresh your token or log in again."
                    : "Unauthorized. A valid Bearer token is required.";

                var response = BaseResponse<object>.Error(message);
                await context.Response.WriteAsJsonAsync(response);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var response = BaseResponse<object>.Error("Forbidden. You do not have permission to perform this action.");
                await context.Response.WriteAsJsonAsync(response);
            }
        };
    });

builder.Services.AddAuthorization();

// 6. Swagger UI with JWT Padlock Button
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token here (no need to type 'Bearer ')."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} by {UserEmail} responded {StatusCode} in {Elapsed:0.00} ms";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var userEmail = httpContext.User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
        diagnosticContext.Set("UserEmail", userEmail);
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

// Top of Middleware Pipeline: Catch exceptions & log every HTTP request cleanly
// Request Logging FIRST, then Exception Handler
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "Budget Allocator API v1"); });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();