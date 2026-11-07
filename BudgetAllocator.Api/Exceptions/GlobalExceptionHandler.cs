using BudgetAllocator.Api.Models.DTO.Response.Generic;
using Microsoft.AspNetCore.Diagnostics;

namespace BudgetAllocator.Api.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _env;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Grab the deepest root cause (e.g., "Connection refused") without dumping 50 stack frames
        var rootCause = exception.InnerException?.Message ?? exception.Message;

        _logger.LogError(
            "Exception [{ExceptionType}] on {Method} {Path} -> {RootCause}",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path,
            rootCause);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var errorMessage = _env.IsDevelopment()
            ? $"Internal Server Error: {rootCause}"
            : "An unexpected error occurred. Please try again later.";

        var response = BaseResponse<object>.Error(errorMessage);

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}