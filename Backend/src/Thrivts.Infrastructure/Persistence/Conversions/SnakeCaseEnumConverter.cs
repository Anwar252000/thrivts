using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Thrivts.Infrastructure.Persistence.Conversions;

/// <summary>
/// Converts a C# PascalCase enum to/from the snake_case string label the live Postgres schema
/// actually stores (e.g. AwaitingPayment &lt;-&gt; awaiting_payment). Plain HasConversion&lt;string&gt;()
/// writes Enum.ToString() verbatim ("AwaitingPayment"), which is not a valid label for the real
/// enum/text columns — this converter is the fix, used everywhere a non-nullable enum maps to one
/// of them. For nullable enum properties, use EnumSnakeCase's helpers directly via HasConversion(...).
/// </summary>
public class SnakeCaseEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : struct, Enum
{
    public SnakeCaseEnumConverter() : base(
        v => EnumSnakeCase.ToSnakeCase(v.ToString()),
        v => EnumSnakeCase.FromSnakeCase<TEnum>(v))
    {
    }
}
