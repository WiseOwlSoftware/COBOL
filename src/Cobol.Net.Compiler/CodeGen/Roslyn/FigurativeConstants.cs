// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.CodeGen;

/// <summary>
/// THE figurative-constant service (P7 Step 4; DESIGN-codegen-backend §2.5 rationale #6): the ONE
/// word-recognition table and the ONE fill-character computation, consolidating the divergent copies
/// (<c>EmitText.FigurativeFill</c>, <c>EmitContext.FigFill</c> ×2, <c>FieldEmitter.FillCharFor</c>,
/// <c>ConditionRenderer.FigurativeFillChar</c>, <c>CSharpEmitter.FigurativeWordFill</c>, and the Report-Writer
/// word map). ISO §8.3.3.6: HIGH-/LOW-VALUE under a PROGRAM COLLATING SEQUENCE are the sequence's extreme
/// CHARACTERS (GR6/GR7 + §12.3.7 GR8/GR9 — character identity, not just comparison weight), natively
/// U+00FF/U+0000 (COBOLNET_DESIGN §14.9); category national/boolean use their OWN sequence, so the alphanumeric
/// PCS never applies to them (the D-N3 pin). The three call-site ALL-strip variants (whitespace-only vs
/// whitespace-or-letter vs pre-stripped) deliberately REMAIN at their sites — they differ observably and
/// unifying them is a behavior change queued for spec adjudication (§8.3.3.6.4), not a refactor.
/// </summary>
internal static class FigurativeConstants
{
    /// <summary>The figurative KIND of a figurative-constant word (already ALL-stripped by the caller), or
    /// <see langword="null"/> when the text is not a figurative word: Z(ERO/S/ES), S(PACE/S), Q(UOTE/S),
    /// H(IGH-VALUE/S), L(OW-VALUE/S), and — where the caller's context admits the pointer figurative as a
    /// character fill (<paramref name="includeNull"/>, ISO §8.3.3.6.4 — the VALUE-init and SET-TO-TRUE word
    /// maps historically accept NULL/NULLS as LOW-VALUE) — N(ULL/S).</summary>
    public static char? KindOf(string word, bool includeNull = false) => word.ToUpperInvariant() switch
    {
        "ZERO" or "ZEROS" or "ZEROES" => 'Z',
        "SPACE" or "SPACES" => 'S',
        "QUOTE" or "QUOTES" => 'Q',
        "HIGH-VALUE" or "HIGH-VALUES" => 'H',
        "LOW-VALUE" or "LOW-VALUES" => 'L',
        "NULL" or "NULLS" when includeNull => 'L',   // fills like LOW-VALUE (the FieldEmitter/SET-TO-TRUE maps)
        _ => null,
    };

    /// <summary>The RAW fill character for a figurative kind: the PCS extremes for H/L when a collating
    /// sequence is active and no category pin applies. National/boolean anchors never take the alphanumeric
    /// PCS (§8.3.3.6 GR6/GR7 — each category reads its OWN sequence): a NATIONAL anchor under an explicit
    /// non-native NATIONAL program collating sequence (<paramref name="natCollate"/> — an <c>ALPHABET … FOR
    /// NATIONAL</c> literal phrase, §12.3.7 GR8/GR9) takes THAT sequence's extremes; otherwise the D-N3
    /// pins (U+00FF/U+0000 — the flagged native-pin divergence stays byte-stable).</summary>
    public static char FillChar(char kind, AlphabetDef? collate, PicCategory? cat = null,
        NationalAlphabetDef? natCollate = null)
    {
        bool pinned = cat is PicCategory.National or PicCategory.Boolean;
        bool nat = cat is PicCategory.National && natCollate is not null;
        return kind switch
        {
            'Z' => '0',
            'S' => ' ',
            'Q' => '"',
            'H' when nat => natCollate!.HighValue,
            'L' or 'N' when nat => natCollate!.LowValue,
            'H' => !pinned && collate is { } hc ? hc.HighValue : 'ÿ',
            'L' or 'N' => !pinned && collate is { } lc ? lc.LowValue : '\0',
            _ => ' ',
        };
    }

    /// <summary>The C# <c>char</c>-literal FRAGMENT a figurative kind fills with. BYTE-IDENTITY NOTE (the 32
    /// characterization snapshots gate this): a PCS-ACTIVE H/L renders through Roslyn's
    /// <see cref="SymbolDisplay.FormatLiteral(char, bool)"/> (e.g. <c>'F'</c>) — exactly the former
    /// <c>EmitContext.FigFill</c>; the native pins keep the FIXED historical escape texts
    /// (<c>'ÿ'</c>/<c>'\0'</c>, never re-rendered) — exactly the former
    /// <c>EmitText.FigurativeFill</c>. A NATIONAL anchor under an explicit non-native NATIONAL PCS
    /// (<paramref name="natCollate"/>) renders THAT sequence's extremes (§12.3.7 GR8/GR9 — a new surface,
    /// no byte-identity constraint).</summary>
    public static string Fill(char kind, AlphabetDef? collate, PicCategory? cat = null,
        NationalAlphabetDef? natCollate = null)
    {
        bool pinned = cat is PicCategory.National or PicCategory.Boolean;
        bool nat = cat is PicCategory.National && natCollate is not null;
        return kind switch
        {
            'H' when nat => SymbolDisplay.FormatLiteral(natCollate!.HighValue, quote: true),
            'L' or 'N' when nat => SymbolDisplay.FormatLiteral(natCollate!.LowValue, quote: true),
            'H' when !pinned && collate is { } hc => SymbolDisplay.FormatLiteral(hc.HighValue, quote: true),
            'L' or 'N' when !pinned && collate is { } lc => SymbolDisplay.FormatLiteral(lc.LowValue, quote: true),
            'Z' => "'0'",
            'S' => "' '",
            'H' => "'\\u00ff'",
            'L' or 'N' => "'\\u0000'",
            'Q' => "'\\\"'",
            _ => "' '",
        };
    }

    /// <summary>The C# <c>string</c>-literal FRAGMENT holding ONE occurrence of a figurative kind's fill
    /// character — the SEED a §8.3.3.6.4 GR2 sizing repeats (kb/Work PB297). Same fill computation as
    /// <see cref="FillChar"/>, rendered as a string rather than a char, so the relation renderer can hand the
    /// figurative to <c>CobolString.CompareFig</c> without knowing its length: GR3 b) already makes ONE
    /// character the figurative's own length wherever no associated data item sizes it, so this fragment is
    /// both the seed and the length-unspecified value. Rendered through
    /// <see cref="SymbolDisplay.FormatLiteral(string, bool)"/>, which escapes the control characters the
    /// HIGH-/LOW-VALUE pins are (a raw NUL in emitted C# is the DEVLOG 788/790 hazard).</summary>
    public static string FillText(char kind, AlphabetDef? collate, PicCategory? cat = null,
        NationalAlphabetDef? natCollate = null) =>
        SymbolDisplay.FormatLiteral(FillChar(kind, collate, cat, natCollate).ToString(), quote: true);
}
