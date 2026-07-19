namespace Gma.Modules.Administration.Tests;

using Gma.Modules.Administration.Application.Models;
using Xunit;

[Trait("Category", "Unit")]
public sealed class AdministrationAuditCursorCodecTests
{
    [Fact]
    public void Cursor_round_trips_without_exposing_delimiters()
    {
        AdministrationAuditCursor expected = new(
            new DateTimeOffset(2026, 7, 19, 10, 0, 0, TimeSpan.Zero),
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        string encoded = AdministrationAuditCursorCodec.Encode(expected);
        bool decoded = AdministrationAuditCursorCodec.TryDecode(encoded, out AdministrationAuditCursor? actual);

        Assert.True(decoded);
        Assert.Equal(expected, actual);
        Assert.DoesNotContain(':', encoded);
    }

    [Theory]
    [InlineData("not-base64!")]
    [InlineData("YQ")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Cursor_rejects_malformed_values(string value)
    {
        Assert.False(AdministrationAuditCursorCodec.TryDecode(value, out _));
    }
}
