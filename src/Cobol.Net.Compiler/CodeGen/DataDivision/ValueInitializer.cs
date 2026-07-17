// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

/// <summary>The VALUE-or-default initializer rendering of the DATA DIVISION (P7 Step 9l): an OCCURS table's
/// array literal, a group's composed initializer (via <see cref="GroupValueSlicer"/>), an elementary item's
/// VALUE / figurative / COBOL default. Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class ValueInitializer(EmitContext ctx)
{
    /// <summary>The group-VALUE positional distributor (set once by <see cref="DataEmitter"/>).</summary>
    public GroupValueSlicer Slicer { get; set; } = null!;

    /// <summary>The C# initializer for a field: an array literal for an OCCURS table (every element initialized so
    /// none is left at <c>default</c>), a composed object-initializer for a group, else the elementary VALUE.</summary>
    public string FieldInit(DataItem item)
    {
        // A DYNAMIC-capacity table (§13.18.38 Format 4, D9): an out-of-line CobolDynTable seeded per occurrence with
        // the SAME one-occurrence initializer the fixed path repeats (heed DEVLOG 643 — seed EVERY occurrence). Opens
        // at FROM (min); TO is the expected capacity; INITIALIZED is carried for the (always-on) new-occurrence seed.
        if (item.IsDynamicTable)
        {
            string seed = item.IsGroup ? Slicer.ComposedInit(item) : InitializerFor(item);
            var s = item.OccursSpec!;
            return $"new CobolDynTable<{item.ElementType}>(() => {seed}, {s.InitialCap ?? 0}, "
                 + $"{(s.ExpectedMax is int e ? e.ToString() : "null")}, {(s.Initialized ? "true" : "false")})";
        }
        if (item.Occurs is { } n)
        {
            string element = item.IsGroup ? Slicer.ComposedInit(item) : InitializerFor(item);
            return $"new {item.ElementType}[] {{ {string.Join(", ", Enumerable.Repeat(element, n))} }}";
        }
        return item.IsGroup ? Slicer.ComposedInit(item) : InitializerFor(item);
    }

    /// <summary>The C# initializer expression for an elementary item, from its VALUE clause or the COBOL default.</summary>
    public string InitializerFor(DataItem item)
    {
        var pic = item.Pic!;

        // CCVS leniency: an ALPHANUMERIC literal VALUE on a numeric DISPLAY item stores its CHARACTERS as the
        // item's content (ISO §13.18.63 SR2 wants a numeric literal; the 85 corpus writes `PIC 999 VALUE "000"`
        // — NC107A's DATA-P — and the legacy oracle accepts the character form). Strict rejection is a future
        // version-conformance pass row.
        if (item.RawValue is { } q && q.StartsWith('"') && pic.Category is PicCategory.Numeric && !pic.IsFloat)
            return item.StoreAsImage
                ? RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(q)), $"{pic.Length}")
                : EmitText.UnscaledAtScale(CobolLiteral.Decode(q), pic.Scale);

        // A numeric-DISPLAY leaf stored as its character image (whole-group-aliased): initialize to the formatted
        // image of its unscaled VALUE (a numeric/figurative VALUE → that value; no VALUE → 0). The _P_ profile is
        // declared textually earlier (EmitProfiles runs first), so it is initialized before this use.
        if (item.StoreAsImage)
        {
            string unscaled = item.RawValue is { } rv && FigurativeInitializer(rv, pic) is null
                ? EmitText.UnscaledAtScale(rv, pic.Scale)
                : "0L";
            return RuntimeApi.NumFormatDisplay(unscaled, item.ProfileName);
        }

        if (item.RawValue is not { } raw) return pic.DefaultInitializer;

        // Figurative constants (ZERO / SPACE / HIGH-VALUE / LOW-VALUE / QUOTE / NULL) fill the item to its width.
        if (FigurativeInitializer(raw, pic) is { } fig) return fig;

        // ALL "literal": the literal repeated to the item width (ISO §8.3.3.6.4 GR2; SR3 forbids it on a numeric item).
        if (EmitText.AllLiteralText(raw) is { } allLit && pic.Category is not PicCategory.Numeric)
            return EmitText.CsLiteral(EmitText.RepeatToWidth(allLit, pic.Length));

        return pic.Category switch
        {
            // A NUMERIC literal VALUE on a numeric-edited item converts per the MOVE editing rules
            // (ISO §13.18.63 GR6) — the edited image is a compile-time constant, baked here. (An alphanumeric
            // literal stores verbatim — NOTE 3: the programmer supplies the edited form.)
            PicCategory.NumericEdited when !raw.StartsWith('"') && TryParseNumeric(raw, out var uv, out int sc) =>
                EmitText.CsLiteral(RuntimeApi.EditCompose(uv, sc, pic.EditMask!, item.BlankWhenZero,
                    ctx.Data.CurrencyPicSymbol, ctx.Data.DecimalPointIsComma)),
            // National VALUE stores like alphanumeric on the char substrate (§13.18.63 SR5 — the N"…" literal,
            // already prefix-stripped by DecodeCobolString); boolean VALUE zero-pads (SR10; §14.6.8.6).
            PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National =>
                RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}"),
            PicCategory.Boolean =>
                RuntimeApi.StrStoreBoolean(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}", justifiedRight: false),
            PicCategory.Numeric when pic.IsFloat => RawValueAsFloat(raw, pic),
            PicCategory.Numeric => EmitText.UnscaledAtScale(raw, pic.Scale),
            _ => pic.DefaultInitializer,
        };
    }

    /// <summary>Parse a canonical (dot-decimal) numeric VALUE text to its unscaled value + scale, for
    /// compile-time editing. False for any non-numeric shape (the caller falls back to verbatim store).</summary>
    internal static bool TryParseNumeric(string text, out Int128 unscaled, out int scale)
    {
        unscaled = 0;
        scale = 0;
        string t = text.Trim();
        bool neg = t.StartsWith('-');
        if (neg || t.StartsWith('+')) t = t[1..];
        int dot = t.IndexOf('.');
        string digits = dot < 0 ? t : t.Remove(dot, 1);
        if (digits.Length == 0 || !digits.All(char.IsAsciiDigit)) return false;
        scale = dot < 0 ? 0 : t.Length - dot - 1;
        foreach (char c in digits) unscaled = unscaled * 10 + (c - '0');
        if (neg) unscaled = -unscaled;
        return true;
    }

    /// <summary>If <paramref name="raw"/> is a figurative constant, its C# initializer given the receiver's category
    /// and width; otherwise null (ISO §8.3.1.2; HIGH/LOW = U+00FF/U+0000 per COBOLNET_DESIGN §14.9).</summary>
    public string? FigurativeInitializer(string raw, PicInfo pic)
    {
        string key = raw.ToUpperInvariant();
        // ALL <figurative-word> (e.g. ALL ZEROS, ALL SPACES) is equivalent to the bare figurative (a single-character
        // figurative repeated to the width); strip the ALL prefix when the remainder is a figurative WORD. (ALL "literal"
        // — repeating a multi-character literal — is a separate form left to the literal path. This site's strip
        // predates upper-casing, so only the GLUED spelling reaches the retry — preserved verbatim, see the
        // FigurativeConstants ALL-strip note.)
        if (FigurativeConstants.KindOf(key, includeNull: true) is null && key.StartsWith("ALL") && key.Length > 3
            && FigurativeConstants.KindOf(key[3..], includeNull: true) is not null)
            key = key[3..];
        if (FigurativeConstants.KindOf(key, includeNull: true) is not { } k) return null;
        string fillChar = FigurativeConstants.Fill(k, ctx.Data.Collating, pic.Category, ctx.Data.NationalCollating);
        return pic.Category is PicCategory.Numeric ? pic.DefaultInitializer : $"new string({fillChar}, {pic.Length})";
    }

    /// <summary>A numeric VALUE literal as a C# float/double literal for a COMP-1/COMP-2 item.</summary>
    private static string RawValueAsFloat(string raw, PicInfo pic) =>
        pic.IsSingle ? $"{raw.Trim().TrimStart('+')}f" : $"{raw.Trim().TrimStart('+')}d";   // COMP-1/FLOAT-SHORT → float literal, else double
}
