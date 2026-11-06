// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Security.Cryptography;
using System.Text;

namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ THE ONE SPELLING OF A MESSAGE SERVER'S NAME (docs/rearchitecture/DESIGN-message-control.md §0 Q3, §5.1). <see cref="Normalize"/> removes
/// trailing spaces and upper-cases, so two spellings of one name are one server. <see cref="Spell"/> is the directory name of the normalized
/// name under the socket directory's <c>s/</c> and <c>p/</c>: a name that matches <c>[A-Z0-9-]{1,14}</c> is spelled as itself, any other is
/// <c>_</c> followed by 13 characters, the RFC 4648 base32 of the first 8 bytes of the SHA-256 of its UTF-8 bytes, no padding. <c>_</c> lies
/// outside the kept alphabet, so a hashed spelling never equals a kept one.
/// </summary>
internal static class McsServerName
{
    /// <summary>The longest name spelled as itself.</summary>
    internal const int KeptLength = 14;

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>The name without trailing spaces, upper-cased with the invariant culture.</summary>
    internal static string Normalize(string name) => name.TrimEnd(' ').ToUpperInvariant();

    /// <summary>The directory-name spelling of an already normalized name.</summary>
    internal static string Spell(string normalized)
    {
        if (IsKept(normalized)) return normalized;
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(normalized), hash);
        return "_" + Base32(hash[..8]);
    }

    private static bool IsKept(string normalized)
    {
        if (normalized.Length is 0 or > KeptLength) return false;
        foreach (char c in normalized)
            if (!(c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-')) return false;
        return true;
    }

    /// <summary>RFC 4648 §6 base32 without padding: 8 bytes are 64 bits, 13 characters.</summary>
    private static string Base32(ReadOnlySpan<byte> bytes)
    {
        var text = new StringBuilder((bytes.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (byte b in bytes)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                text.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
            buffer &= (1 << bits) - 1;
        }
        if (bits > 0) text.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        return text.ToString();
    }
}
