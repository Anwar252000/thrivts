using System.Text;

namespace Thrivts.Infrastructure.Persistence.Conversions;

/// <summary>Shared PascalCase &lt;-&gt; snake_case helpers — see SnakeCaseEnumConverter for why this exists.</summary>
public static class EnumSnakeCase
{
    public static string ToSnakeCase(string value)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static TEnum FromSnakeCase<TEnum>(string value) where TEnum : struct, Enum
    {
        var pascal = string.Concat(value.Split('_').Select(word =>
            word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));
        return Enum.Parse<TEnum>(pascal);
    }
}
