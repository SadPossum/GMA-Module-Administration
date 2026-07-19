namespace Gma.Modules.Administration.Application.Models;

using System.Globalization;
using System.Text;

internal static class AdministrationAuditCursorCodec
{
    public const int MaxLength = 128;

    public static string Encode(AdministrationAuditCursor cursor)
    {
        string payload = string.Create(
            CultureInfo.InvariantCulture,
            $"{cursor.CreatedAtUtc.UtcDateTime.Ticks}:{cursor.Id:N}");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? value, out AdministrationAuditCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string encoded = value.Trim();
        if (encoded.Length > MaxLength || encoded.Any(char.IsControl))
        {
            return false;
        }

        try
        {
            string base64 = encoded.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
            string payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            string[] parts = payload.Split(':');
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long utcTicks) ||
                !Guid.TryParseExact(parts[1], "N", out Guid id) ||
                id == Guid.Empty)
            {
                return false;
            }

            DateTimeOffset createdAtUtc = new(utcTicks, TimeSpan.Zero);
            cursor = new AdministrationAuditCursor(createdAtUtc, id);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
