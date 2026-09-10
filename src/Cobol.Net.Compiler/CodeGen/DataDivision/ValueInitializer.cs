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

    /// <summary>THE §14.6.2.3.2 action-1 background (kb/Work PB152) — shared with <see cref="GroupImageCodec"/>
    /// so the native-field arm and the Tier-B image arm cannot answer "what fills a VALUE-less item" differently.
    /// One instance per emitter so its section-root sets are built once.</summary>
    public InitialStateBackground Background { get; } = new(ctx);

    /// <summary>The C# initializer for a field: an array literal for an OCCURS table (every element initialized so
    /// none is left at <c>default</c>), a composed object-initializer for a group, else the elementary VALUE.
    /// <para><paramref name="outer"/> is the OCCURRENCE CONTEXT — the subscripts of every OCCURS level already
    /// entered on the way down from the record root, most inclusive first. It is empty for a level-01/77 field and
    /// grows by one at each OCCURS entry, so at any leaf it is exactly the tuple §13.18.63.3 SR20 keys a Format 2
    /// (table) VALUE by, and <see cref="DataItem.ValueAt"/> can answer "what initializes THIS occurrence" without
    /// the caller knowing which entry in the chain wrote the clause (kb/Work PB505). Threading it is what lets a
    /// table VALUE live on an entry SUBORDINATE to the OCCURS (SR18) and span several dimensions (GR12's
    /// odometer); before it, both were refused.</para></summary>
    public string FieldInit(DataItem item, Subscripts outer = default)
    {
        // A DYNAMIC-capacity table (§13.18.38 Format 4, D9): an out-of-line CobolDynTable seeded per occurrence with
        // the SAME one-occurrence initializer the fixed path repeats (heed DEVLOG 643 — seed EVERY occurrence). Opens
        // at FROM (min), raised to the §13.18.63.4 GR16 initial capacity when a table VALUE applies; TO is the
        // expected capacity; INITIALIZED is carried for the (always-on) new-occurrence seed.
        if (item.IsDynamicTable)
        {
            var s = item.OccursSpec!;
            int min = s.InitialCap ?? 0;
            string expected = s.ExpectedMax is int e ? e.ToString() : "null";
            string init = s.Initialized ? "true" : "false";
            if (!item.ContainsTableValue)
                return $"new CobolDynTable<{item.ElementType}>(() => {ElementInit(item, outer.With(1))}, {min}, "
                     + $"{expected}, {init})";
            // §13.18.63.4 GR16 fixed the initial capacity in the binder ("If more than one VALUE clause applies,
            // the maximum value thus calculated becomes the initial capacity"); occurrences within it take their
            // keyed element, and growth beyond re-seeds through the same function to the VALUE-less default.
            int cap = Math.Max(min, item.TableValueInitialCapacity ?? min);
            return $"new CobolDynTable<{item.ElementType}>({SeedSwitch(item, outer, cap)}, {min}, "
                 + $"{expected}, {init}, {cap})";
        }
        if (item.Occurs is { } n)
        {
            // Without a table VALUE in the subtree every occurrence is identical — compose ONE and repeat it (the
            // shape both lanes always had, and the reason a 49-level CCVS record still emits in linear time).
            if (!item.ContainsTableValue)
                return $"new {item.ElementType}[] {{ {string.Join(", ", Enumerable.Repeat(ElementInit(item, outer.With(1)), n))} }}";
            return $"new {item.ElementType}[] {{ "
                 + string.Join(", ", Enumerable.Range(1, n).Select(o => ElementInit(item, outer.With(o)))) + " }";
        }
        return ElementInit(item, outer);
    }

    /// <summary>ONE table element / one non-table field at <paramref name="subs"/>: a group composes its members
    /// (or takes its own §13.18.63.4 GR5 area VALUE), an elementary item takes the literal its VALUE clause gives
    /// this occurrence.</summary>
    private string ElementInit(DataItem item, Subscripts subs) =>
        item.IsGroup ? Slicer.ComposedInit(item, subs) : InitializerFor(item, subs);

    /// <summary>The per-occurrence seed function of a DYNAMIC-capacity table carrying (or containing) a Format 2
    /// VALUE: <c>(int __i) =&gt; __i switch { … }</c> over occurrences 1..<paramref name="cap"/>, defaulting to the
    /// VALUE-less element for anything the table grows to later. Occurrences whose element text is IDENTICAL share
    /// one arm — the common case is one literal over a whole range, and an arm per occurrence would put a
    /// thousand identical branches into the generated source.</summary>
    private string SeedSwitch(DataItem item, Subscripts outer, int cap)
    {
        // Subscript 0 identifies no table element (§13.18.63.3 SR20 admits none below 1), so it is the tuple that
        // deliberately matches no FROM..TO range: the element as it stands with no table VALUE keyed to it.
        string dflt = ElementInit(item, outer.With(0));
        var arms = new List<string>();
        var pending = new List<int>();
        string? pendingText = null;
        void Flush()
        {
            if (pendingText is null || pendingText == dflt) { pending.Clear(); pendingText = null; return; }
            arms.Add($"{string.Join(" or ", pending)} => {pendingText},");
            pending.Clear();
            pendingText = null;
        }
        for (int o = 1; o <= cap; o++)
        {
            string text = ElementInit(item, outer.With(o));
            if (pendingText is not null && !string.Equals(text, pendingText, StringComparison.Ordinal)) Flush();
            pendingText = text;
            pending.Add(o);
        }
        Flush();
        return arms.Count == 0
            ? $"(int __i) => {dflt}"
            : $"(int __i) => __i switch {{ {string.Join(" ", arms)} _ => {dflt} }}";
    }
    /// <summary>The C# initializer expression for an elementary item, from its VALUE clause or the COBOL default.</summary>
    public string InitializerFor(DataItem item, Subscripts subs = default)
    {
        var pic = item.Pic!;
        // ⛔ THE ONE READER for "what initializes this item at this occurrence" — DataItem.ValueAt: the Format-1
        // VALUE (the same for every occurrence, §13.18.63.4 GR9) or the Format-2 literal keyed to this subscript
        // tuple (GR12–GR15). The image lane asks the same property, so the two cannot disagree.
        string? effRaw = item.ValueAt(subs);

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

        // CCVS leniency: an ALPHANUMERIC literal VALUE on a NUMERIC item is read AS the numeric literal ISO
        // §13.18.63.3 SR2 asked for (the 85 corpus writes `PIC 999 VALUE "000"` — NC107A's DATA-P; DataBinder's
        // ValidateValueCategory diagnoses it, COBOLNET1657, and rewrites it to the number under --permissive).
        // ⛔ ONE RECIPE FOR BOTH STORAGE AXES (kb/Work PB188). The image arm used to hand-spell
        // `StrStore(chars, pic.Length)` — the PICTURE's DIGIT COUNT — which was right only while every
        // StoreAsImage numeric leaf was zoned DISPLAY, where digits and bytes are the same number. Since the
        // V59/PB164 widening a StoreAsImage leaf can be BYTE-FORM, and then they are not: `PIC 9(4) COMP` is 4
        // digits and 2 bytes, `PIC 9(18) COMP-5` is 18 and 8, `PIC S9(3) COMP-3` is 3 and 2 — and even a zoned
        // `PIC S999 SIGN SEPARATE` is 3 and 4. The window a CharImage leaf occupies is item.ImageWidth
        // (StorageFormPass), so a pic.Length-wide seed displaced every following member of the group image.
        // Routing through NumFormatImage — the SAME encoder the numeric arm below and GroupImageCodec use —
        // takes the width from the item's own pinned byte form BY CONSTRUCTION, and makes the leniency MEAN the
        // same thing on both axes: the number, encoded as this item stores numbers.
        if (effRaw is { } q && q.StartsWith('"') && pic.Category is PicCategory.Numeric && !pic.IsFloat)
        {
            string unscaledText = EmitText.UnscaledAtScale(CobolLiteral.Decode(q), pic.Scale);
            return item.StoreAsImage
                ? RuntimeApi.NumFormatImage(unscaledText, item.ProfileName)
                : CarrierInit(unscaledText, pic);
        }

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

        // ⛔ THE NO-VALUE BRANCH — §14.6.2.3.2 action 1, the BACKGROUND. `pic.DefaultInitializer` is the
        // NO-CLAUSE baseline and stays exactly that; when the OPTIONS paragraph wrote an INITIALIZE clause whose
        // section list selects this item, the background is the specified-fill-character instead. Action 1
        // precedes action 2 (the VALUE seed) and this is the only place that ordering has to appear, because a
        // field initializer is one expression and every VALUE-bearing path above has already returned.
        // ONE choke point, shared with the Tier-B image arm — see InitialStateBackground.
        if (effRaw is not { } raw) return Background.Seed(item, pic) ?? pic.DefaultInitializer;

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
    /// a non-negative source literal of ≤31 digits (ISO §8.3.3.3.2), rendered as <c>…L</c> or <c>Int128.Parse</c> —
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
    /// and width; otherwise null (ISO §8.3.3.6; HIGH/LOW = U+00FF/U+0000 per COBOLNET_DESIGN §14.9).</summary>
    public string? FigurativeInitializer(string raw, PicInfo pic)
    {
        if (FigurativeKind(raw) is not { } k) return null;
        string fillChar = FigurativeConstants.Fill(k, ctx.Data.Collating, pic.Category, ctx.Data.NationalCollating);
        return pic.Category is PicCategory.Numeric ? pic.DefaultInitializer : $"new string({fillChar}, {pic.Length})";
    }

    /// <summary>The figurative KIND of a VALUE text (ALL-stripped, ISO §8.3.3.6.4), or null when it is not a
    /// figurative constant. The ONE detector shared by <see cref="FigurativeInitializer"/>, the VCR 35
    /// numeric-edited figurative-ZERO branch, and the §13.18.63.4 GR5 group-area rule
    /// (<see cref="GroupValueSlicer.AreaTextOf"/> — internal for that third rider, kb/Work PB184).</summary>
    internal static char? FigurativeKind(string raw)
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
