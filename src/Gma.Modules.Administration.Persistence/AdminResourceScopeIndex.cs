namespace Gma.Modules.Administration.Persistence;

using System.Security.Cryptography;
using System.Text;

internal static class AdminResourceScopeIndex
{
    public const int HashLength = 64;

    public static string? Create(string? canonicalResourceScope) =>
        canonicalResourceScope is null
            ? null
            : Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(canonicalResourceScope)));
}
