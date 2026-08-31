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
        // A Format 2 (table) VALUE (ISO §13.18.63.2, COBOL-2002): per-occurrence initialization. LANDABLE scope is an
        // elementary single-dimension table on its own OCCURS entry (DataBinder.ValidateTableValues staged the rest).
        if (item.TableValues is { Count: > 0 } && !item.IsGroup)
            return TableValueInit(item);

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

    /// <summary>The per-occurrence initializer of a Format 2 (table) VALUE (ISO §13.18.63.4 GR12–GR16): a fixed table
    /// becomes an array literal whose occurrences take their keyed literals (default outside any FROM..TO range); a
    /// dynamic table becomes a CobolDynTable opened at the GR16 initial capacity with a per-occurrence seed.</summary>
    private string TableValueInit(DataItem item)
    {
        var s = item.OccursSpec;
        int fillMax = item.Occurs ?? s?.ExpectedMax ?? 0;   // GR14 no-TO fill maximum
        var map = ResolveTableValueMap(item, fillMax);
        string ElemInit(int occ) => InitializerFor(item, map.TryGetValue(occ, out var lit) ? lit : null);

        if (item.IsDynamicTable)
        {
            int min = s?.InitialCap ?? 0;
            int? expected = s?.ExpectedMax;
            int initialCap = TableInitialCapacity(map, min, expected);
            string dflt = InitializerFor(item, null);
            string seedFn = map.Count == 0
                ? $"(int __i) => {dflt}"
                : $"(int __i) => __i switch {{ {string.Join(" ", map.Keys.OrderBy(k => k).Select(k => $"{k} => {ElemInit(k)},"))} _ => {dflt} }}";
            return $"new CobolDynTable<{item.ElementType}>({seedFn}, {min}, "
                 + $"{(expected is int e ? e.ToString() : "null")}, {(s!.Initialized ? "true" : "false")}, {initialCap})";
        }
        int n = item.Occurs!.Value;
        return $"new {item.ElementType}[] {{ {string.Join(", ", Enumerable.Range(1, n).Select(ElemInit))} }}";
    }

    /// <summary>Resolve occurrence → literal text (ISO §13.18.63.4 GR12 sequential fill, GR13 cyclic reuse under TO,
    /// GR14 no-TO = fill to the maximum, GR15 later FROM wins on overlap). Occurrences outside every FROM..TO range
    /// are absent (they take the element default — NOT asserted as a spec-guaranteed space/zero, §13.18.63.4).</summary>
    private static Dictionary<int, string> ResolveTableValueMap(DataItem item, int fillMax)
    {
        var map = new Dictionary<int, string>();
        foreach (var spec in item.TableValues!.OrderBy(sp => sp.Ordinal))
        {
            if (spec.Literals.Count == 0) continue;
            int from = spec.From[0];
            int to = spec.To?[0] ?? fillMax;
            for (int occ = from, k = 0; occ <= to; occ++, k++)
                map[occ] = spec.Literals[k % spec.Literals.Count];   // GR15 last-wins: a later phrase overwrites
        }
        return map;
    }

    /// <summary>The initial current capacity of a dynamic-capacity table from its Format 2 VALUE (ISO §13.18.63.4
    /// GR16): raised to the highest covered occurrence (= the MAX subscript-2), clamped within [min, expected],
    /// never below the minimum.</summary>
    private static int TableInitialCapacity(Dictionary<int, string> map, int min, int? expected)
    {
        int cap = map.Count > 0 ? Math.Max(min, map.Keys.Max()) : min;
        if (expected is { } exp) cap = Math.Min(cap, exp);   // GR16a proviso: within [min, expected]
        return Math.Max(cap, min);
    }

    /// <summary>The C# initializer expression for an elementary item, from its VALUE clause or the COBOL default.</summary>
    public string InitializerFor(DataItem item, string? rawOverride = null)
    {
        var pic = item.Pic!;
        // A Format 2 (table) VALUE supplies a per-occurrence literal (rawOverride); otherwise the item's own VALUE.
        string? effRaw = rawOverride ?? item.RawValue;

        // A DYNAMIC LENGTH item (ISO §8.5.1.10 / §13.18.19): the field is a native string. §8.6.4 — a VALUE clause
        // defines the initial length (MOVE-like, §13.18.63.4 GR7; stored truncated on the right to the LIMIT, no
        // padding); ABSENT a VALUE the initial length is zero (§8.6.4 second sentence — never a fixed-width fill).
        // A figurative VALUE other than `ALL literal` has length ONE (§8.3.3.6.4 GR3b — FigurativeInitializer fills
        // pic.Length = 1 for the single-symbol X/N picture), so it initializes to a single fill character, NOT "".
        if (item.IsDynamicLength)
        {
            if (effRaw is not { } dv) return "\"\"";
            if (FigurativeInitializer(dv, pic) is { } figFill) return figFill;
            return RuntimeApi.DynStore(EmitText.CsLiteral(CobolLiteral.Decode(dv)), item.DynLengthLimit.ToString());
        }

        // CCVS leniency: an ALPHANUMERIC literal VALUE on a numeric DISPLAY item stores its CHARACTERS as the
        // item's content (ISO §13.18.63 SR2 wants a numeric literal; the 85 corpus writes `PIC 999 VALUE "000"`
        // — NC107A's DATA-P — and the legacy oracle accepts the character form). Strict rejection is a future
        // version-conformance pass row.
        if (effRaw is { } q && q.StartsWith('"') && pic.Category is PicCategory.Numeric && !pic.IsFloat)
            return item.StoreAsImage
                ? RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(q)), $"{pic.Length}")
                : CarrierInit(EmitText.UnscaledAtScale(CobolLiteral.Decode(q), pic.Scale), pic);

        // A numeric leaf stored as its character image (whole-group-aliased / Tier-B): initialize to the BYTES
        // of its unscaled VALUE (zoned digits for DISPLAY, radix-2 / BCD for BINARY / PACKED — V59) (a numeric/figurative VALUE → that value; no VALUE → 0). The _P_ profile is
        // declared textually earlier (EmitProfiles runs first), so it is initialized before this use.
        if (item.StoreAsImage)
        {
            // A WINDOWED FLOAT member (the Step D arm-1 dissolution) seeds its IEEE window bytes from the
            // (float-literal or zero) VALUE through the ONE literal recipe (RawValueAsFloat) — the integer
            // lane would throw NoByteImage on an Ieee profile.
            if (pic.IsFloat)
                return RuntimeApi.NumFormatImageFloat(
                    effRaw is { } fv && FigurativeInitializer(fv, pic) is null ? RawValueAsFloat(fv, pic) : "0d",
                    item.ProfileName);
            string unscaled = effRaw is { } rv && FigurativeInitializer(rv, pic) is null
                ? EmitText.UnscaledAtScale(rv, pic.Scale)
                : "0L";
            return RuntimeApi.NumFormatImage(unscaled, item.ProfileName);
        }

        if (effRaw is not { } raw) return pic.DefaultInitializer;

        // A FORMAT-2 (LOCALE) item's numeric VALUE has NO compile-time image — §13.18.40.5 r11 + §14.6.6 r6 make
        // the locale the one current AT THE TIME of editing — so the initializer is a RUNTIME CobolLocaleEdit
        // call (the ONE producer, RuntimeApi.LocaleEditCompose; PB64 T6). A quoted literal falls through to the
        // verbatim store below (§13.18.63.3 SR7 — the programmer supplies the edited form).
        if (pic.LocaleEdit is not null && !raw.StartsWith('"') && !raw.StartsWith('\'')
            && TryParseNumeric(FigurativeKind(raw) == 'Z' ? "0" : raw, out var luv, out int lsc)
            && (FigurativeKind(raw) != 'Z' || ctx.Data.Edition.DialectLevel >= 2023))
            return RuntimeApi.LocaleEditCompose(pic, luv, lsc, item.BlankWhenZero);

        // A NUMERIC-EDITED item's numeric VALUE (a numeric literal, or the figurative ZERO at >= 2023) is its EDITED
        // image — the ONE compose (EditedImageOfNumericValue) the level-88 membership test shares. Below 2023 a
        // figurative ZERO falls through to the FigurativeInitializer zero-fill (the pre-2023 behavior, VCR 35).
        if (pic.Category is PicCategory.NumericEdited && EditedImageOfNumericValue(ctx, item, pic, raw) is { } editedImage)
            return EmitText.CsLiteral(editedImage);

        // Figurative constants (ZERO / SPACE / HIGH-VALUE / LOW-VALUE / QUOTE / NULL) fill the item to its width.
        if (FigurativeInitializer(raw, pic) is { } fig) return fig;

        // ALL "literal": the literal repeated to the item width (ISO §8.3.3.6.4 GR2; SR3 forbids it on a numeric item).
        if (EmitText.AllLiteralText(raw) is { } allLit && pic.Category is not PicCategory.Numeric)
            return EmitText.CsLiteral(EmitText.RepeatToWidth(allLit, pic.Length));

        return pic.Category switch
        {
            // A numeric-edited item's NUMERIC VALUE was composed above (EditedImageOfNumericValue); an alphanumeric
            // literal stores verbatim (§13.18.63.3 SR7 / NOTE 3: the programmer supplies the edited form).
            // National VALUE stores like alphanumeric on the char substrate (§13.18.63 SR5 — the N"…" literal,
            // already prefix-stripped by DecodeCobolString); boolean VALUE zero-pads (SR10; §14.6.8.6).
            PicCategory.Alphanumeric or PicCategory.NumericEdited or PicCategory.National =>
                RuntimeApi.StrStore(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}"),
            PicCategory.Boolean =>
                RuntimeApi.StrStoreBoolean(EmitText.CsLiteral(CobolLiteral.Decode(raw)), $"{pic.Length}", justifiedRight: false),
            PicCategory.Numeric when pic.IsFloat => RawValueAsFloat(raw, pic),
            PicCategory.Numeric => CarrierInit(EmitText.UnscaledAtScale(raw, pic.Scale), pic),
            _ => pic.DefaultInitializer,
        };
    }

    /// <summary>Cast a numeric VALUE literal to the item's CARRIER type where C# has no implicit conversion: an
    /// unsigned BinaryCapacity item carries <c>ulong</c> / <c>UInt128</c> (kb/Work R10), and its VALUE literal —
    /// a non-negative source literal of ≤31 digits (ISO §8.3.1.2), rendered as <c>…L</c> or <c>Int128.Parse</c> —
    /// converts exactly.</summary>
    private static string CarrierInit(string literal, PicInfo pic) =>
        pic.IsUnsignedWideBinary ? $"(UInt128)({literal})"
        : pic.IsUnsignedLongBinary ? $"(ulong)({literal})"
        : literal;

    /// <summary>The compile-time EDITED IMAGE of a numeric-edited item's numeric VALUE — a numeric literal converted
    /// "according to the rules for the MOVE statement" (ISO §13.18.63.3 SR6; formats 1, 2 and 4, so the item VALUE
    /// and a level-88 condition-name value alike), or the figurative ZERO / ZEROES (with or without ALL) at
    /// >= 2023, where SR6 treats it identically to the literal zero (VCR 35 — Annex E.2 item 28: pre-2023 it was
    /// a left-justified zero-fill, which the caller keeps). Null when <paramref name="raw"/> is anything else (an
    /// alphanumeric literal is SR7's edited form as written; SPACE / HIGH-VALUE … are the figurative fill). The
    /// dispatch on the item's FORM lives HERE and nowhere else (D21/PB66): a floating-point numeric-edited item
    /// composes through <see cref="RuntimeApi.EditComposeFloat"/> (the floating-point literal, or a zero form), a
    /// fixed-point one through <see cref="RuntimeApi.EditCompose"/> — the SAME runtime the MOVE uses, so the baked
    /// image is what MOVE literal TO item would store (BLANK WHEN ZERO included, NOTE 2).</summary>
    internal static string? EditedImageOfNumericValue(EmitContext ctx, DataItem item, PicInfo pic, string raw)
    {
        // A format-2 (LOCALE) item has NO compile-time image (the locale is runtime data) — the callers carry
        // their own runtime arm (RuntimeApi.LocaleEditCompose); returning null here keeps the EditMask derefs
        // below unreachable for it (PB64 T6).
        if (pic.LocaleEdit is not null) return null;
        if (raw.StartsWith('"') || raw.StartsWith('\'')) return null;
        bool zeroFigurative = FigurativeKind(raw) == 'Z';
        if (zeroFigurative && ctx.Data.Edition.DialectLevel < 2023) return null;
        if (pic.IsFloatEdited)
            return zeroFigurative || TryParseFloatLiteral(raw, out _, out _)
                ? RuntimeApi.EditComposeFloat(zeroFigurative ? Int128.Zero : ParsedSig(raw), zeroFigurative ? 0 : ParsedExp(raw),
                    pic.EditMask!, item.BlankWhenZero, ctx.Data.DecimalPointIsComma)
                : null;
        if (zeroFigurative)
            return RuntimeApi.EditCompose(Int128.Zero, pic.Scale, pic.EditMask!, item.BlankWhenZero,
                pic.CurrencyString, ctx.Data.DecimalPointIsComma, pic.EditingRules);
        return TryParseNumeric(raw, out var uv, out int sc)
            ? RuntimeApi.EditCompose(uv, sc, pic.EditMask!, item.BlankWhenZero, pic.CurrencyString,
                ctx.Data.DecimalPointIsComma, pic.EditingRules)
            : null;

        static Int128 ParsedSig(string r) { TryParseFloatLiteral(r, out var s, out _); return s; }
        static int ParsedExp(string r) { TryParseFloatLiteral(r, out _, out int e); return e; }
    }

    /// <summary>Parse a numeric literal — the FLOATING-POINT form too (ISO §8.3.3.3.3: two fixed-point literals joined by
    /// E, e.g. <c>-1.5E+3</c>) — to a significand and a power of ten (kb/Work PB66); a fixed-point literal is its unscaled
    /// value at −scale, the figurative ZERO / a zero literal 0E0.</summary>
    private static bool TryParseFloatLiteral(string raw, out Int128 sig, out int exp10)
    {
        sig = 0; exp10 = 0;
        string t = raw.Trim().ToUpperInvariant();
        if (t is "ZERO" or "ZEROS" or "ZEROES") return true;
        return CobolNet.Common.NumericLiteral.TryParseExact(t, out sig, out exp10);   // the ONE exact parser (PB99)
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
        if (FigurativeKind(raw) is not { } k) return null;
        string fillChar = FigurativeConstants.Fill(k, ctx.Data.Collating, pic.Category, ctx.Data.NationalCollating);
        return pic.Category is PicCategory.Numeric ? pic.DefaultInitializer : $"new string({fillChar}, {pic.Length})";
    }

    /// <summary>The figurative KIND of a VALUE text (ALL-stripped, ISO §8.3.3.6.4), or null when it is not a
    /// figurative constant. The ONE detector shared by <see cref="FigurativeInitializer"/> and the VCR 35
    /// numeric-edited figurative-ZERO branch.</summary>
    private static char? FigurativeKind(string raw)
    {
        string key = raw.ToUpperInvariant();
        // ALL <figurative-word> (e.g. ALL ZEROS, ALL SPACES) is equivalent to the bare figurative (a single-character
        // figurative repeated to the width); strip the ALL prefix when the remainder is a figurative WORD. (ALL "literal"
        // — repeating a multi-character literal — is a separate form left to the literal path. This strip predates
        // upper-casing, so only the GLUED spelling reaches the retry — preserved verbatim, see the FigurativeConstants
        // ALL-strip note.)
        if (FigurativeConstants.KindOf(key, includeNull: true) is null && key.StartsWith("ALL") && key.Length > 3
            && FigurativeConstants.KindOf(key[3..], includeNull: true) is not null)
            key = key[3..];
        return FigurativeConstants.KindOf(key, includeNull: true);
    }

    /// <summary>A numeric VALUE literal as a C# float/double literal for a COMP-1/COMP-2 item. Internal:
    /// the ONE literal recipe — the group-image codec's float backing seed reuses it (Step D).</summary>
    internal static string RawValueAsFloat(string raw, PicInfo pic) =>
        pic.IsSingle ? $"{raw.Trim().TrimStart('+')}f" : $"{raw.Trim().TrimStart('+')}d";   // COMP-1/FLOAT-SHORT → float literal, else double
}
