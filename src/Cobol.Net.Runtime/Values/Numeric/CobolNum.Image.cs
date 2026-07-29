// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// THE record-image codec for a fixed-point numeric item — the one place a value becomes the BYTES it occupies,
/// and back (COBOLNET_DESIGN §14.4, numeric design D17). Every byte boundary goes through it: the whole-group
/// character image, a file record, a SORT/MERGE key window and a Tier-B REDEFINES backing.
/// <para>
/// The bytes are carried in a <see cref="string"/> under the LATIN-1 convention the record framing already uses —
/// chars 0–255 map 1:1 to bytes (<c>RecordFraming</c>) — so a binary or packed leaf rides the SAME image carrier
/// as a DISPLAY leaf and no second whole-group mechanism appears.
/// </para>
/// <para>
/// The representations are implementor-defined (ISO/IEC 1989:2023 §13.18.60.4 GR4 BINARY "a radix of 2 is used",
/// GR11 PACKED-DECIMAL "a radix of 10 … each digit position shall occupy the minimum possible configuration",
/// GR7 DISPLAY) and §4.2.16 obliges us to document ours — <see cref="NumericStorageForm"/> carries that
/// documentation and this file is its implementation. Widths come from <see cref="NumProfile.StorageLength"/>,
/// which is what <c>FUNCTION BYTE-LENGTH</c> reports (§15.14.4 GR1): ONE width, never two answers.
/// </para>
/// <para>
/// Scale is not represented — the image carries the UNSCALED digits and the decimal point stays implied, exactly
/// as it is in storage (§13.18.40.4: V occupies no position).
/// </para>
/// </summary>
public static partial class CobolNum
{
    /// <summary>Encode a fixed-point value as the bytes it occupies in a record, per the item's
    /// <see cref="NumProfile.StorageForm"/>. The result is EXACTLY the item's image width: its
    /// <see cref="NumProfile.StorageLength"/> for a binary/packed form, its digit run (plus a separate sign
    /// position) for the zoned form.</summary>
    public static string FormatImage(Int128 unscaled, in NumProfile item) => item.StorageForm switch
    {
        NumericStorageForm.Zoned => FormatDisplay(unscaled, item),
        NumericStorageForm.Binary => FormatBinaryImage(unscaled, item),
        NumericStorageForm.Packed or NumericStorageForm.PackedNoSign => FormatPackedImage(unscaled, item),
        _ => throw NoByteImage(item),
    };

    /// <summary>Storage-form bridge (the <see cref="FormatDisplay(string, in NumProfile)"/> pattern): a field whose
    /// backing the whole-group analysis already turned into its character IMAGE is in image form — pass it
    /// through. Lets the compiler emit ONE expression whose field storage is decided later.</summary>
    public static string FormatImage(string image, in NumProfile item) => image;

    /// <summary>Decode an item's record-image bytes back to its unscaled value — the inverse of
    /// <see cref="FormatImage(Int128, in NumProfile)"/>.</summary>
    public static Int128 ParseImage(string image, in NumProfile item) => item.StorageForm switch
    {
        NumericStorageForm.Zoned => ParseDisplay(image, item),
        NumericStorageForm.Binary => ParseBinaryImage(image, item),
        NumericStorageForm.Packed or NumericStorageForm.PackedNoSign => ParsePackedImage(image, item),
        _ => throw NoByteImage(item),
    };

    /// <summary>An item with no byte representation (<see cref="NumericStorageForm.None"/> — USAGE INDEX, whose
    /// occurrence-number carrier reaches no image at all, §13.18.60.4 GR10) reached a byte boundary. That is a
    /// compiler invariant break, never a COBOL runtime condition: the binder's <c>IsImageCapable</c> gate is
    /// supposed to make it unreachable. Fail LOUD rather than invent bytes — inventing them is exactly the class
    /// of defect this codec exists to retire.</summary>
    private static InvalidOperationException NoByteImage(in NumProfile item) =>
        new($"no byte representation for a numeric item with StorageForm={item.StorageForm} "
            + $"(Digits={item.Digits}, StorageLength={item.StorageLength}) — it must never reach a record image");

    // ── BINARY (radix 2, §13.18.60.4 GR4/GR6/GR12) ────────────────────────────────────────────────────────────
    // Two's complement, MOST SIGNIFICANT BYTE FIRST, in exactly StorageLength bytes. Big-endian is the choice
    // GR4 asks the implementor to make and is what IBM, Micro Focus and GnuCOBOL all write for USAGE BINARY, so
    // a data file interchanges. An UNSIGNED item holds the magnitude (§14.9.25.4 GR8 — "the absolute value").

    private static string FormatBinaryImage(Int128 unscaled, in NumProfile item)
    {
        int n = Width(item);
        if (!item.Signed && unscaled < 0) unscaled = -unscaled;
        UInt128 raw = Mask(unchecked((UInt128)unscaled), n);
        var chars = new char[n];
        for (int i = 0; i < n; i++) chars[i] = (char)(byte)(raw >> (8 * (n - 1 - i)));
        return new string(chars);
    }

    private static Int128 ParseBinaryImage(string image, in NumProfile item)
    {
        int n = Width(item);
        UInt128 raw = 0;
        // A window shorter than the pinned width can only arrive from incompatible data (a short record's pad,
        // §14.6.13.2 leaves it undefined): the bytes present are read as the LOW-order bytes, deterministically.
        int take = image is null ? 0 : Math.Min(n, image.Length);
        for (int i = 0; i < take; i++) raw = (raw << 8) | (byte)image![i];
        if (!item.Signed) return (Int128)raw;
        // Sign-extend from the pinned width: a set top bit is a negative two's-complement value.
        if (n >= 16) return unchecked((Int128)raw);
        UInt128 span = UInt128.One << (8 * n);
        return raw >= (span >> 1) ? (Int128)raw - (Int128)span : (Int128)raw;
    }

    private static UInt128 Mask(UInt128 v, int bytes) =>
        bytes >= 16 ? v : v & ((UInt128.One << (8 * bytes)) - 1);

    // ── PACKED-DECIMAL (radix 10 BCD, §13.18.60.4 GR11) ───────────────────────────────────────────────────────
    // Two digits per byte, most significant first, zero-padded on the left. NumericStorageForm.Packed reserves
    // the LOW nibble of the last byte for the sign — 0xC positive, 0xD negative, 0xF for an item with no
    // operational sign (the IBM / Micro Focus / GnuCOBOL convention); NumericStorageForm.PackedNoSign is the 2023
    // WITH NO SIGN form, which "reserves no storage for representing any sign value" (GR11), so every nibble is a
    // digit. The two forms can occupy the SAME number of bytes at an odd digit count — 3 digits is 2 bytes either
    // way — which is why the form, never the width, decides whether a sign nibble is present.

    private const byte SignPositive = 0x0C;
    private const byte SignNegative = 0x0D;
    private const byte SignUnsigned = 0x0F;

    private static string FormatPackedImage(Int128 unscaled, in NumProfile item)
    {
        int n = Width(item);
        bool hasSignNibble = item.StorageForm is NumericStorageForm.Packed;
        int digitNibbles = 2 * n - (hasSignNibble ? 1 : 0);
        bool negative = item.Signed && unscaled < 0;
        Int128 mag = unscaled < 0 ? -unscaled : unscaled;
        if (item.Digits > 0) mag %= Pow10Wide(item.Digits);   // the picture's digit capacity, as the zoned form does

        var nibbles = new byte[2 * n];
        for (int i = digitNibbles - 1; i >= 0; i--)
        {
            nibbles[i] = (byte)(mag % 10);
            mag /= 10;
        }
        if (hasSignNibble)
            nibbles[^1] = item.Signed ? (negative ? SignNegative : SignPositive) : SignUnsigned;

        var chars = new char[n];
        for (int i = 0; i < n; i++) chars[i] = (char)((nibbles[2 * i] << 4) | nibbles[2 * i + 1]);
        return new string(chars);
    }

    private static Int128 ParsePackedImage(string image, in NumProfile item)
    {
        int n = Width(item);
        bool hasSignNibble = item.StorageForm is NumericStorageForm.Packed;
        int take = image is null ? 0 : Math.Min(n, image.Length);
        int digitNibbles = 2 * take - (hasSignNibble ? 1 : 0);

        Int128 mag = 0;
        for (int i = 0; i < digitNibbles; i++)
        {
            int nib = (i % 2 == 0 ? image![i / 2] >> 4 : image![i / 2]) & 0x0F;
            // A non-decimal nibble is incompatible data (§14.6.13.2, undefined) — contribute no digit, exactly
            // as the zoned decoder ignores a non-digit character, so the decode stays deterministic.
            if (nib <= 9) mag = mag * 10 + nib;
        }
        if (!hasSignNibble || take == 0) return mag;
        // The IBM sign-nibble reading every packed producer agrees on: 0xB and 0xD are negative, everything else
        // (0xA, 0xC, 0xE, 0xF, and a digit written by a careless producer) is positive.
        int sign = image![take - 1] & 0x0F;
        return sign is 0x0B or 0x0D ? -mag : mag;
    }

    /// <summary>The pinned byte width a binary/packed form lays out. Zero would mean the profile claims a byte
    /// form without a width — a construction bug (<c>NumericStorageFormDriftTests</c> pins the pairing), so it
    /// fails loud here rather than silently producing an empty image.</summary>
    private static int Width(in NumProfile item) =>
        item.StorageLength > 0 ? item.StorageLength : throw NoByteImage(item);
}
