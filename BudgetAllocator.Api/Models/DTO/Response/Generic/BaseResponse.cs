namespace BudgetAllocator.Api.Models.DTO.Response.Generic;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }

    // Static helper for success
    public static BaseResponse<T> Ok(T? data, string message = "Success")
    {
        return new BaseResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Timestamp = GetLagosTime()
        };
    }

    // Static helper for error
    public static BaseResponse<T> Error(string message, T? data = default)
    {
        return new BaseResponse<T>
        {
            Success = false,
            Message = message,
            Data = data,
            Timestamp = GetLagosTime()
        };
    }

    private static DateTime GetLagosTime()
    {
        try
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneInfo);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow.AddHours(1); // Fallback to WAT
        }
    }
}