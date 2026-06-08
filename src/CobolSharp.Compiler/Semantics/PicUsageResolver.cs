// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.Diagnostics;

namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Resolves a data item's PIC string + USAGE clause into a concrete ITypeSymbol.
/// Called by the binder when creating DataSymbols.
/// </summary>
public static class PicUsageResolver
{
    public static ITypeSymbol ResolveForDataItem(
        string dataName,
        string? picString,
        UsageKind usage,
        DiagnosticBag diagnostics,
        int line,
        bool blankWhenZero = false,
        PicEnvironment? environment = null)
    {
        PicLayout? layout = null;

        if (picString != null)
        {
            layout = ParsePic(picString, diagnostics, line, blankWhenZero, environment);
        }

        var category = layout?.Category ?? CobolCategory.Unknown;
        bool isNumeric = category.IsNumericLike();
        bool isAlpha = category.IsAlphanumericLike();
        bool isBool = category.IsBooleanLike();

        // COMP-1/COMP-2 (float) and the COBOL-2002 fixed-width binary usages (BINARY-CHAR/SHORT/LONG/
        // DOUBLE) have no PIC clause but are numeric.
        if (picString == null && usage is UsageKind.Comp1 or UsageKind.Comp2
            or UsageKind.BinaryChar or UsageKind.BinaryShort
            or UsageKind.BinaryLong or UsageKind.BinaryDouble)
        {
            isNumeric = true;
            category = CobolCategory.Numeric;
        }
        // USAGE BIT with no PICTURE — a boolean item (normally accompanied by PIC 1(n); be defensive).
        else if (picString == null && usage == UsageKind.Bit)
        {
            isBool = true;
            category = CobolCategory.Boolean;
        }
        // USAGE POINTER (no PIC) — an opaque 8-byte machine-address handle (ISO §13.18.60.4); not numeric,
        // not alphanumeric. The 8-byte size is supplied by FieldSizeCalculator/ComputeStorageLength.
        else if (picString == null && usage == UsageKind.Pointer)
        {
            category = CobolCategory.Pointer;
        }
        // USAGE OBJECT REFERENCE (no PIC) — an OO object reference (ISO §13.18.60.4); a managed .NET reference,
        // never bytes (it occupies no WORKING-STORAGE; its home is the _OBJ_ static field). Not numeric/alphanumeric.
        else if (picString == null && usage == UsageKind.Object)
        {
            category = CobolCategory.ObjectReference;
        }
        // Group items (no PIC) are alphanumeric by default
        else if (picString == null && usage == UsageKind.Display)
        {
            isAlpha = true;
            category = CobolCategory.Alphanumeric;
        }

        var name = BuildTypeName(dataName, layout, usage);
        return new DataTypeSymbol(name, isNumeric, isAlpha, isBool, layout, usage);
    }

    /// <summary>
    /// Returns the first character in <paramref name="picBody"/> that is not a valid PICTURE symbol
    /// (ISO §13.18.40.3 SR2/SR8), or null if every symbol is valid. The runtime
    /// <see cref="PicDescriptorFactory.FromPicBody"/> silently skips unrecognized characters (its
    /// <c>default</c> arm), so a mixed picture like <c>9Q9</c> compiles with the Q swallowed; this
    /// compiler-side scan surfaces that. Valid symbols: 9 X A N S V P Z * + - , . / B 0, the program's
    /// currency symbol, and the two-character CR / DB. Scans the EXPANDED pattern so repeat counts like
    /// <c>9(5)</c> (the parentheses/integer are syntax, not symbols) are not mistaken for illegal chars.
    /// </summary>
    public static char? FindIllegalPicSymbol(string picBody, PicEnvironment? environment = null)
    {
        var env = environment ?? PicEnvironment.Default;
        char currency = char.ToUpperInvariant(env.CurrencySign);
        string expanded = PicDescriptorFactory.ExpandPattern(picBody.Trim().ToUpperInvariant());
        for (int i = 0; i < expanded.Length; i++)
        {
            char c = expanded[i];
            if (IsValidPicSymbol(c, currency)) continue;
            // ';' is a COBOL separator (equivalent to a space, never a picture symbol); the PIC lexer can
            // capture a trailing one into the picture token, so ignore it rather than flag it.
            if (c is ';' or ' ') continue;
            if (c == 'C' && i + 1 < expanded.Length && expanded[i + 1] == 'R') { i++; continue; } // CR
            if (c == 'D' && i + 1 < expanded.Length && expanded[i + 1] == 'B') { i++; continue; } // DB
            return c;
        }
        return null;
    }

    private static bool IsValidPicSymbol(char c, char currency) =>
        c is '9' or 'X' or 'A' or 'N' or '1' or 'S' or 'V' or 'P' or 'Z' or '*'
          or '+' or '-' or ',' or '.' or '/' or 'B' or '0'
        || c == currency;

    private static string BuildTypeName(string dataName, PicLayout? pic, UsageKind usage)
    {
        if (pic == null)
            return $"{dataName}:{usage}";
        return $"{dataName}:PIC({pic.Category},{pic.Length})/{usage}";
    }

    /// <summary>
    /// Parse a PIC string into a PicLayout using the canonical Runtime.PicDescriptorFactory.
    /// Single pipeline: all PIC semantics are defined by PicDescriptorFactory.FromPicBody.
    /// PicLayout is a thin view for the compiler's type system.
    /// </summary>
    private static PicLayout ParsePic(string picString, DiagnosticBag diagnostics, int line,
        bool blankWhenZero = false, PicEnvironment? environment = null)
    {
        // Per ISO §13.18.52, signed DISPLAY numerics default to trailing overpunch
        // when no explicit SIGN clause is present. Detect 'S' in the PIC body upfront.
        bool bodySigned = picString.IndexOf('S', StringComparison.OrdinalIgnoreCase) >= 0;
        var signStorage = bodySigned ? SignStorageKind.TrailingOverpunch : SignStorageKind.None;

        var desc = Runtime.PicDescriptorFactory.FromPicBody(
            picString.Trim(),
            usage: UsageKind.Display,
            isSigned: false,               // S in the body will flip this
            signStorage: signStorage,
            blankWhenZero: blankWhenZero,
            environment: environment);

        return new PicLayout(
            Category: desc.Category,
            Length: desc.StorageLength,
            IntegerDigits: desc.TotalDigits - desc.FractionDigits,
            FractionDigits: desc.FractionDigits,
            LeadingPScaling: desc.LeadingScaleDigits,
            TrailingPScaling: desc.TrailingScaleDigits,
            IsSigned: desc.IsSigned,
            IsEdited: desc.HasEditing,
            BlankWhenZero: desc.BlankWhenZero);
    }
}

/// <summary>
/// Maps USAGE keyword text to UsageKind enum.
/// </summary>
public static class UsageMapper
{
    public static UsageKind FromUsageKeyword(string? keyword)
    {
        if (keyword == null)
            return UsageKind.Display;

        return keyword.ToUpperInvariant() switch
        {
            "DISPLAY" => UsageKind.Display,
            "COMP" or "COMPUTATIONAL" => UsageKind.Comp,
            "COMP-1" or "COMPUTATIONAL-1" => UsageKind.Comp1,
            "COMP-2" or "COMPUTATIONAL-2" => UsageKind.Comp2,
            // COBOL-2002 standard floating-point usages (ISO §13.18): FLOAT-SHORT is IEEE-754 single (= COMP-1),
            // FLOAT-LONG is IEEE-754 double (= COMP-2).
            "FLOAT-SHORT" => UsageKind.Comp1,
            "FLOAT-LONG" => UsageKind.Comp2,
            // FLOAT-EXTENDED is mapped to IEEE-754 double (COMP-2): .NET has no native 128-bit float, and the
            // standard permits implementor-defined extended precision (the platform's widest native float).
            "FLOAT-EXTENDED" => UsageKind.Comp2,
            "COMP-3" or "COMPUTATIONAL-3" => UsageKind.Comp3,
            "COMP-4" or "COMPUTATIONAL-4" => UsageKind.Binary,
            "COMP-5" or "COMPUTATIONAL-5" => UsageKind.Comp5,
            // COBOL-2002 fixed-width binary usages (ISO §13.18.60); SIGNED/UNSIGNED handled separately.
            "BINARY-CHAR" => UsageKind.BinaryChar,
            "BINARY-SHORT" => UsageKind.BinaryShort,
            "BINARY-LONG" => UsageKind.BinaryLong,
            "BINARY-DOUBLE" => UsageKind.BinaryDouble,
            "BINARY" => UsageKind.Binary,
            "PACKED-DECIMAL" => UsageKind.PackedDecimal,
            "INDEX" => UsageKind.Index,
            "POINTER" => UsageKind.Pointer,
            // USAGE NATIONAL (ISO §13.18.60.4): explicit national (UTF-16) data. Category is supplied by
            // the accompanying PIC N; this marks the usage. Sizing/MOVE/DISPLAY dispatch on the category.
            "NATIONAL" => UsageKind.National,
            // USAGE BIT (ISO §13.18.60.4): explicit boolean data. Category is supplied by the accompanying
            // PIC 1; this marks the usage. Sizing/MOVE/DISPLAY dispatch on the category.
            "BIT" => UsageKind.Bit,
            // An unmapped usage keyword is unknown, NOT an object reference: USAGE OBJECT REFERENCE has its own
            // dedicated grammar alt handled in SemanticBuilder before this mapper, and UsageKind.Object is now a
            // live value (zero-storage managed reference) — a stray keyword must not silently masquerade as one.
            _ => UsageKind.Unknown
        };
    }
}
