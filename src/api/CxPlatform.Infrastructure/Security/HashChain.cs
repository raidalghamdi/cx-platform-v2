using System.Security.Cryptography;
using System.Text;

namespace CxPlatform.Infrastructure.Security;

// Audit hash chain — SHA-256 over `prev || "|" || payload_json`.
// Genesis prev = 64 zeroes. Forward-only — never recompute or rewrite.
public static class HashChain
{
    public const string Genesis = "0000000000000000000000000000000000000000000000000000000000000000";

    public static string ComputeEntryHash(string prevHash, string payloadJson)
    {
        if (prevHash is null) throw new ArgumentNullException(nameof(prevHash));
        if (payloadJson is null) throw new ArgumentNullException(nameof(payloadJson));
        var bytes = Encoding.UTF8.GetBytes(prevHash + "|" + payloadJson);
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
