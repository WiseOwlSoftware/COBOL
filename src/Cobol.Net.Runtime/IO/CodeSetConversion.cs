// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ THE ONE ISO §13.18.13.4 GR6 CODE-SET CONVERSION — "<i>On input, each coded character from the storage
/// medium is replaced with its associated native coded character as defined in the alphabet being used</i>"
/// (GR6 a) and "<i>On output, each native coded character in the record is replaced for the storage medium with
/// its associated coded character as defined in the alphabet being used</i>" (GR6 b). One instance per file
/// connector, or NULL — §13.18.13.4 GR7, "<i>If the CODE-SET clause is not specified, the native character set
/// is assumed for data on the external media</i>", which is the identity and costs nothing.
/// <para><b>The correspondence is the compiler's</b>, not the runtime's: the ALPHABET clause's coded character
/// set decides it (§12.3.7.4 GR7 i for a code-name — <c>CobolNet.Binding.ImplementorCodeNames</c>) and the
/// emitter hands the finished 256-entry table down as a literal, exactly as it hands down a collating sequence's
/// weights. This type holds no code page and knows no alphabet name.</para>
/// <para><b>What is converted, and what is not.</b> GR6 speaks of the characters of the RECORD; §13.18.13.4 GR3
/// applies "<i>the specified alphabet … for code-set conversion of all data items in each record</i>". The
/// framing this processor adds around a record — the §9.1.7.2 record-length header of a VARYING or keyed file,
/// and a line sequential file's line delimiter — is not data of the record and stays in the native encoding;
/// §9.1.7.2 leaves that framing implementor-defined ("<i>any information the implementor may add to the record on
/// the physical storage medium (such as record length headers)</i>"), and keeping it native is what lets one
/// framing serve a converted and an unconverted file alike. The determination is published in
/// <c>docs/CONFORMANCE.md</c> §2 row 27.</para>
/// <para><b>The boundary.</b> Conversion happens where the record's characters physically cross to or from the
/// medium and NOWHERE else, so a record area, a key value, a comparison and a lock identity are always in the
/// native character set: <c>SequentialConnector.NextFrame</c> (the one physical framing walk) and its
/// <c>EmitRecord</c>/<c>EmitRecordLine</c> twins on the write side, and <see cref="RecordFraming"/>'s store
/// payload for the keyed organizations.</para>
/// </summary>
public sealed class CodeSetConversion
{
    /// <summary>Medium code unit → the native character it represents (GR6 a). Its length is the number of
    /// characters in the coded character set; a single-byte code has 256.</summary>
    private readonly char[] _toNative;

    /// <summary>The inverse (GR6 b), indexed by native character over the record channel's one-byte window.
    /// <see cref="Unmapped"/> marks a native character this coded character set cannot represent.</summary>
    private readonly char[] _toMedium;

    /// <summary>The reverse table's "no such character in this set" marker. U+FFFF is safe as a sentinel here
    /// because the table is indexed 0…255 and holds MEDIUM code units, which for a single-byte code are 0…255 —
    /// never U+FFFF.</summary>
    private const char Unmapped = '￿';

    /// <summary>Build the conversion from §12.3.7.4 GR7 i's correspondence: <paramref name="toNative"/>[u] is the
    /// native character that medium code unit <c>u</c> represents.</summary>
    /// <exception cref="ArgumentException">The correspondence is not one this record channel can invert: it is
    /// not a single-byte code, or two code units share a native character, or a native character it names lies
    /// outside the one-byte record channel. A registered single-byte code page satisfies all three; the check
    /// states the requirement rather than trusting the caller.</exception>
    public CodeSetConversion(char[] toNative)
    {
        ArgumentNullException.ThrowIfNull(toNative);
        if (toNative.Length is 0 or > 256)
            throw new ArgumentException($"a CODE-SET coded character set shall be a single-byte code on this "
                + $"medium: {toNative.Length} characters (ISO §13.18.13.4 GR6)", nameof(toNative));
        _toNative = toNative;
        _toMedium = new char[256];
        Array.Fill(_toMedium, Unmapped);
        for (int unit = 0; unit < toNative.Length; unit++)
        {
            char native = toNative[unit];
            if (native > 0xFF)
                throw new ArgumentException($"medium code unit {unit} corresponds to U+{(int)native:X4}, which "
                    + "is outside the one-byte record channel (ISO §13.18.13.4 GR6 b)", nameof(toNative));
            if (_toMedium[native] != Unmapped)
                throw new ArgumentException($"medium code units {(int)_toMedium[native]} and {unit} both "
                    + $"correspond to U+{(int)native:X4}: the correspondence is not invertible "
                    + "(ISO §13.18.13.4 GR6 b)", nameof(toNative));
            _toMedium[native] = (char)unit;
        }
    }

    /// <summary>GR6 a — the native form of a record image just read from the storage medium.</summary>
    public string ToNative(string mediumImage)
    {
        if (mediumImage.Length == 0) return mediumImage;
        return string.Create(mediumImage.Length, (mediumImage, _toNative), static (dst, s) =>
        {
            var (src, map) = s;
            for (int i = 0; i < src.Length; i++)
            {
                char u = src[i];
                // A code unit outside the set has no associated native character; it is left as it stands rather
                // than mapped to something it is not — the medium held a byte this coded character set does not
                // define, which is a property of the FILE, not an error this READ may invent a status for
                // (§13.18.13.4 names no condition for it, and §9.1.13 has none). Unreachable for a complete
                // single-byte page.
                dst[i] = u < map.Length ? map[u] : u;
            }
        });
    }

    /// <summary>GR6 b — the storage-medium form of a record image about to be written.</summary>
    public string ToMedium(string nativeImage)
    {
        if (nativeImage.Length == 0) return nativeImage;
        return string.Create(nativeImage.Length, (nativeImage, _toMedium), static (dst, s) =>
        {
            var (src, map) = s;
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                char unit = c <= 0xFF ? map[c] : Unmapped;
                if (unit == Unmapped)
                    throw new InvalidOperationException($"the record contains U+{(int)c:X4}, which the file's "
                        + "CODE-SET coded character set does not represent (ISO §13.18.13.4 GR6 b)");
                dst[i] = unit;
            }
        });
    }
}
