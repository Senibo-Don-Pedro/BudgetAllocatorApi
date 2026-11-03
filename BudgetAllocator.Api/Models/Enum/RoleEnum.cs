using System.Text.Json.Serialization;

namespace BudgetAllocator.Api.Models.Enum;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RoleEnum
{
    User,
    Admin
}