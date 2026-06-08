// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>The data category a PICTURE describes (ISO/IEC 1989:2023 §8.4.2).</summary>
public enum PicCategory
{
    /// <summary>A group item (no PIC) — maps to a C# <c>record struct</c>.</summary>
    Group,
    /// <summary>Alphanumeric (<c>X</c>) or alphabetic (<c>A</c>) — maps to <see cref="string"/>.</summary>
    Alphanumeric,
    /// <summary>Numeric (<c>9</c>/<c>S</c>/<c>V</c>/<c>P</c>) — maps to <see cref="long"/> or <see cref="decimal"/>.</summary>
    Numeric,
    /// <summary>Numeric-edited (<c>Z * $ , . + - CR DB B 0</c>) — a formatted display image; later slice.</summary>
    NumericEdited,
}

/// <summary>The physical representation a <c>USAGE</c> clause selects.</summary>
public enum Usage
{
    /// <summary>USAGE DISPLAY (the default) — character form.</summary>
    Display,
    /// <summary>COMP / COMP-4 / BINARY — binary integer.</summary>
    Binary,
    /// <summary>COMP-3 / PACKED-DECIMAL — packed decimal.</summary>
    Packed,
    /// <summary>COMP-5 — native binary (no PICTURE truncation).</summary>
    Comp5,
    /// <summary>COMP-1 — single-precision float.</summary>
    Float,
    /// <summary>COMP-2 — double-precision float.</summary>
    Double,
}

/// <summary>
/// The analyzed PICTURE + USAGE of an elementary item: its category, the numeric profile (digit count, decimal
/// scale, sign) and the .NET type COBOL.NET represents it with. This is pure spec analysis — no byte storage.
/// </summary>
/// <remarks>
/// <para><b>Numeric profile.</b> <see cref="Digits"/> is the count of <c>9</c> positions, <see cref="Scale"/> the
/// number of them after the implied decimal point (<c>V</c>), <see cref="Signed"/> whether an <c>S</c> is present.
/// These drive every <c>CobolNum</c> operation (PIC truncation / ROUNDED / SIZE ERROR).</para>
/// <para>Slice scope: <c>X/A/9/S/V</c> with <c>(n)</c> repetition, and the common usages. Editing symbols and
/// <c>P</c> scaling are recognized into <see cref="PicCategory.NumericEdited"/> / profile but full formatting is a
/// later slice.</para>
/// </remarks>
public sealed record PicInfo(
    PicCategory Category,
    Usage Usage,
    int Length,
    int Digits,
    int Scale,
    bool Signed)
{
    /// <summary>The C# type used to store this item's value.</summary>
    public string ClrType => Category switch
    {
        PicCategory.Alphanumeric or PicCategory.NumericEdited => "string",
        PicCategory.Numeric => Usage switch
        {
            Usage.Float => "float",
            Usage.Double => "double",
            _ => Scale > 0 ? "decimal" : "long",
        },
        _ => "object", // Group: never stored as a scalar (emitted as a record struct).
    };

    /// <summary>True when the .NET storage type is <see cref="decimal"/> (signed or scaled numeric).</summary>
    public bool IsDecimal => Category == PicCategory.Numeric && Usage is not (Usage.Float or Usage.Double) && Scale > 0;

    /// <summary>The default C# initializer for an item with no VALUE clause (COBOL initial state, ISO §13.18.63).</summary>
    public string DefaultInitializer => Category switch
    {
        // Alphanumeric defaults to spaces; numeric to zero. (Numeric-edited defaults to its zero image — later slice.)
        PicCategory.Alphanumeric or PicCategory.NumericEdited => $"new string(' ', {Length})",
        PicCategory.Numeric => Usage switch { Usage.Float => "0f", Usage.Double => "0d", _ => Scale > 0 ? "0m" : "0L" },
        _ => "default",
    };

    /// <summary>
    /// Analyze a PICTURE string (already stripped of the <c>PIC</c> keyword) plus an optional usage keyword.
    /// </summary>
    public static PicInfo Analyze(string picture, Usage usage)
    {
        // Expand (n) repetition into a flat symbol run, e.g. "X(4)" → "XXXX", "9(3)V99" → "999V99".
        string expanded = ExpandRepeats(picture);

        bool signed = expanded.Contains('S');
        bool hasV = expanded.Contains('V');
        int digits = expanded.Count(c => c is '9');
        int afterV = hasV ? expanded[(expanded.IndexOf('V') + 1)..].Count(c => c is '9') : 0;
        bool anyAlpha = expanded.Any(c => c is 'X' or 'A');
        bool anyEdit = expanded.Any(c => c is 'Z' or '*' or '+' or '-' or ',' or '.' or '$' or 'B' or '0' or '/');

        if (anyAlpha)
            // Alphanumeric length = count of character positions (X, A, and any 9 mixed in).
            return new PicInfo(PicCategory.Alphanumeric, usage,
                Length: expanded.Count(c => c is 'X' or 'A' or '9'), Digits: 0, Scale: 0, Signed: false);

        if (anyEdit && digits > 0)
            // Numeric-edited: the .NET storage is the formatted display image (string); width = edited symbol count.
            return new PicInfo(PicCategory.NumericEdited, usage,
                Length: expanded.Count(c => c is not ('V' or 'S')), Digits: digits, Scale: afterV, Signed: signed);

        // Pure numeric.
        return new PicInfo(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: afterV, Signed: signed);
    }

    /// <summary>Expand <c>symbol(n)</c> repetition factors into a flat symbol run (uppercased).</summary>
    private static string ExpandRepeats(string picture)
    {
        var sb = new System.Text.StringBuilder();
        string p = picture.ToUpperInvariant();
        for (int i = 0; i < p.Length; i++)
        {
            char c = p[i];
            if (c is ' ') continue;
            if (i + 1 < p.Length && p[i + 1] == '(')
            {
                int close = p.IndexOf(')', i + 2);
                if (close > 0 && int.TryParse(p[(i + 2)..close], out int n))
                {
                    sb.Append(c, n);
                    i = close;
                    continue;
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Map a COBOL usage keyword (e.g. <c>COMP-3</c>) to a <see cref="Usage"/>.</summary>
    public static Usage ParseUsage(string? keyword) => keyword?.ToUpperInvariant().Replace("COMPUTATIONAL", "COMP") switch
    {
        null or "DISPLAY" => Usage.Display,
        "COMP" or "COMP-4" or "BINARY" => Usage.Binary,
        "COMP-3" or "PACKED-DECIMAL" => Usage.Packed,
        "COMP-5" => Usage.Comp5,
        "COMP-1" => Usage.Float,
        "COMP-2" => Usage.Double,
        _ => Usage.Display,
    };
}
