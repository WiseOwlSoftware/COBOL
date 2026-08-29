// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime.Globalization;

/// <summary>One POSIX LC_MONETARY placement convention (ISO/IEC 9945:2009 clause 7, incorporated normatively by
/// ISO/IEC 1989:2023 §8.2.1 — "Locale category names, the details of locale-categories, and locale field names
/// shall be as specified in ISO/IEC 9945:2009, Clause 7"): whether the currency string precedes the value
/// (<c>*_cs_precedes</c>), where the space sits (<c>*_sep_by_space</c> — 0 none; 1 between the currency-and-sign
/// unit and the value; 2 inside the currency/sign junction, else between the sign and the value), and the sign's
/// position (<c>*_sign_posn</c> — 0 parentheses enclose quantity AND currency string; 1 sign before both; 2 after
/// both; 3 immediately before the currency string; 4 immediately after it). ⚠ Neither <c>sep_by_space</c> nor the
/// <c>sign_posn</c> value semantics appear in ISO 1989 itself (the field table §8.2.2 lists neither) — they reach
/// COBOL only through §8.2.1's ISO 9945 reference, and were verified OPERATIONALLY: <see cref="Render"/> built
/// from them reproduces every .NET currency-pattern layout exactly (the static derivation below proves it at
/// process start).</summary>
public readonly record struct MonetaryConvention(bool CsPrecedes, int SepBySpace, int SignPosn);

/// <summary>
/// The ONE POSIX monetary layout renderer plus the DERIVED .NET-pattern→convention tables (DESIGN-locale-facility
/// §8; kb/Work PB64 T6). <see cref="Render"/> is the single place the (cs_precedes, sep_by_space, sign_posn)
/// cross-product is spelled out — <c>CobolLocaleEdit</c> renders THROUGH it and the derivation below proves .NET's
/// own layouts against it, so the editor and the recognizer cannot disagree about what a convention looks like.
/// <para>⛔ THE TABLES ARE DERIVED AT RUNTIME, in the static initializer, NEVER hand-written and never generated at
/// build time: a build-time table bakes the BUILD host's <see cref="NumberFormatInfo"/> semantics into the assembly
/// and can never fail on the RUN host, defeating the design's whole point ("a new ICU release that adds a pattern
/// fails the test instead of silently mis-editing"). The derivation PROBES every pattern value the running .NET
/// accepts (range DISCOVERED, never hard-coded — the documented 0..15 negative range is FALSE, this runtime accepts
/// 0..16 and the culture luy-KE uses 16) with collision-free sentinels, searches the convention space in a pinned
/// canonical order, and THROWS when no convention reproduces a layout — a future runtime that adds an
/// inexpressible layout fails loudly at first monetary use instead of mis-editing.</para>
/// <para>⚖ DETERMINATION (CONFORMANCE.md): <c>p_sign_posn</c> = 1 flat — .NET's positive currency pattern carries
/// NO sign slot (only 4 values, none placing a sign), so it yields a (cs_precedes, sep_by_space) PAIR and the sign
/// position is undeterminable. It is NOT mirrored from the negative convention: the invariant culture's
/// n_sign_posn is 0 (parentheses), and a mirrored positive would render <c>+1234.50</c> as <c>(¤1,234.50)</c>.</para>
/// </summary>
public static class MonetaryPlacement
{
    /// <summary>Lay out one monetary rendering per the POSIX convention — the B.1 shape of the T6 derivation.
    /// When <c>SignPosn</c> is 0 the parentheses ARE the sign and <paramref name="sign"/> is not used; when 3 or 4
    /// the sign is GLUED to the currency string and travels with it, which is what separates
    /// <c>sep_by_space</c> 1 (the space between the currency-and-sign unit and the value) from 2 (the space inside
    /// the currency/sign junction) — the single distinction separating .NET pattern 16 (<c>$- n</c>) from
    /// pattern 12 (<c>$ -n</c>).</summary>
    public static string Render(MonetaryConvention c, string cs, string sign, string val)
        => Render(c, cs, sign, val, out _);

    /// <summary>As <see cref="Render(MonetaryConvention, string, string, string)"/>, also reporting where
    /// <paramref name="val"/> starts in the result — <c>CobolLocaleEdit</c> needs the offset to carry each
    /// hypothetical-item position's role through §13.18.40.5 r15 suppression into r14 b)'s per-character
    /// truncation test, and computing it HERE keeps the layout written once.</summary>
    public static string Render(MonetaryConvention c, string cs, string sign, string val, out int valStart)
    {
        // A space slot next to an ABSENT sign string is not emitted: POSIX's sep_by_space 2 separates "the
        // currency symbol and the sign string" / "the sign string from the value" — no sign string, no space.
        // (The derivation's probes always carry a one-char sign, so the tables are unaffected.)
        if (c.SignPosn == 0)
        {
            string gap = c.SepBySpace == 1 ? " " : "";
            if (c.CsPrecedes) { valStart = 1 + cs.Length + gap.Length; return "(" + cs + gap + val + ")"; }
            valStart = 1;
            return "(" + val + gap + cs + ")";
        }
        string sp2 = c.SepBySpace == 2 && sign.Length > 0 ? " " : "";
        string csGroup = c.SignPosn == 3 ? sign + sp2 + cs
                       : c.SignPosn == 4 ? cs + sp2 + sign
                       : cs;
        string sp1 = c.SepBySpace == 1 ? " " : "";
        string head = c.SignPosn == 1 ? sign + sp2 : "";
        valStart = c.CsPrecedes ? head.Length + csGroup.Length + sp1.Length : head.Length;
        string body = c.CsPrecedes ? csGroup + sp1 + val : val + sp1 + csGroup;
        return c.SignPosn == 2 ? head + body + sp2 + sign : head + body;
    }

    /// <summary>The convention for each <see cref="NumberFormatInfo.CurrencyNegativePattern"/> value this runtime
    /// accepts (index = the pattern value; length = the DISCOVERED accepted range, 17 on .NET 10).</summary>
    public static MonetaryConvention[] NegativeByPattern { get; }

    /// <summary>The (cs_precedes, sep_by_space) pair for each <see cref="NumberFormatInfo.CurrencyPositivePattern"/>
    /// value this runtime accepts. The pair's <c>SignPosn</c> carries the determined constant 1 (see the class doc)
    /// so a <see cref="MonetaryConvention"/> comes out ready to use.</summary>
    public static MonetaryConvention[] PositiveByPattern { get; }

    static MonetaryPlacement()
    {
        // Collision-free sentinels: the probe string is a permutation of exactly one 'Q', one '1', at most one
        // 'N' or one '(' ')' pair, and at most one ' ' — unambiguous by construction. (A REAL currency symbol
        // would not be: 22 cultures' symbol contains whitespace, 24 contain their own group separator.)
        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.CurrencySymbol = "Q";
        nfi.NegativeSign = "N";
        nfi.PositiveSign = "P";     // must never appear: proves the positive pattern has no sign slot
        nfi.CurrencyDecimalDigits = 0;
        nfi.CurrencyGroupSizes = [0];

        PositiveByPattern = Derive(nfi, negative: false);
        NegativeByPattern = Derive(nfi, negative: true);
    }

    private static MonetaryConvention[] Derive(NumberFormatInfo nfi, bool negative)
    {
        var table = new List<MonetaryConvention>();
        for (int v = 0; ; v++)
        {
            try
            {
                if (negative) nfi.CurrencyNegativePattern = v;
                else nfi.CurrencyPositivePattern = v;
            }
            catch (ArgumentOutOfRangeException) { break; }   // the accepted range is DISCOVERED, never assumed
            string probe = (negative ? -1m : 1m).ToString("C", nfi);
            if (probe.Contains('P'))
                throw new InvalidOperationException(
                    $"Currency{(negative ? "Negative" : "Positive")}Pattern {v} renders '{probe}' carrying a "
                    + "POSITIVE SIGN — .NET's currency patterns were signless when this derivation was written; "
                    + "DESIGN-locale-facility §8 must be re-derived before this runtime is supported.");
            table.Add(Match(probe, v, negative));
        }
        return [.. table];
    }

    /// <summary>The canonical search: sign_posn ascending 0..4 (skipped for the signless positive side, which
    /// takes the determined constant 1), then sep_by_space ascending 0..2, then cs_precedes true then false —
    /// FIRST convention whose <see cref="Render"/> reproduces the probe wins. The order is pinned by a unit test:
    /// several .NET layouts have more than one POSIX spelling (pattern 1 <c>-$n</c> is (1,0,1) and (1,0,3)) and a
    /// different order would silently change which one recognition prefers.</summary>
    private static MonetaryConvention Match(string probe, int pattern, bool negative)
    {
        foreach (int posn in negative ? new[] { 0, 1, 2, 3, 4 } : new[] { 1 })
            for (int sep = 0; sep <= 2; sep++)
                foreach (bool pre in new[] { true, false })
                {
                    var c = new MonetaryConvention(pre, sep, posn);
                    if (Render(c, "Q", negative ? "N" : "", "1") == probe) return c;
                }
        throw new InvalidOperationException(
            $"Currency{(negative ? "Negative" : "Positive")}Pattern {pattern} renders '{probe}', which no POSIX "
            + "(cs_precedes, sep_by_space, sign_posn) convention reproduces — DESIGN-locale-facility §8 must be "
            + "re-derived before this runtime is supported.");
    }
}
