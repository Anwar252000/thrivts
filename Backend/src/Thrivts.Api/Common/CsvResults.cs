using System.Globalization;
using System.Text;

namespace Thrivts.Api.Common;

/// <summary>
/// Streams CSV rows straight to the response as they come off the database cursor — exports never
/// buffer the whole file in memory. Cells are quoted, and any cell starting with = + - @ is prefixed
/// with an apostrophe so a downloaded file can't execute formulas when opened in Excel (CSV injection).
/// </summary>
public static class CsvResults
{
    public static async Task WriteAsync<T>(HttpResponse response, string fileName, string[] header, IAsyncEnumerable<T> rows, Func<T, object?[]> project, CancellationToken ct)
    {
        response.ContentType = "text/csv; charset=utf-8";
        response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";

        await using var writer = new StreamWriter(response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), bufferSize: 16 * 1024);
        await writer.WriteLineAsync(string.Join(',', header.Select(Escape)));

        var count = 0;
        await foreach (var row in rows.WithCancellation(ct))
        {
            await writer.WriteLineAsync(string.Join(',', project(row).Select(v => Escape(Format(v)))));
            if (++count % 500 == 0)
                await writer.FlushAsync(ct);
        }

        await writer.FlushAsync(ct);
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly t => t.ToString("HH:mm", CultureInfo.InvariantCulture),
        DateTimeOffset dt => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        bool b => b ? "Yes" : "No",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string Escape(string value)
    {
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' && !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            value = "'" + value;

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
