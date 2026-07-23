// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Binding.Model;
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>A parsed PICTURE EDITING phrase (ISO §13.18.40.2 Format 1, COBOL-2023) handed to
/// <see cref="PictureAnalyzer.Analyze"/>: the DECODED editing character-1 text and its DECODED literal(s).
/// <see cref="IsForForm"/> distinguishes the sign-control FOR form (<see cref="Neg"/>/<see cref="Pos"/>, either
/// side null when unspecified per SR12c) from the simple-insertion IS form (<see cref="Simple"/>).</summary>
public sealed record EditingPhraseSpec(string Char1Text, string? Simple, string? Neg, string? Pos, bool IsForForm);

/// <summary>
/// The PICTURE character-string scanner and the USAGE-keyword mapper (ISO/IEC 1989:2023 §13.18.40 / §13.18.60) —
/// the ANALYSIS half split off <see cref="PicInfo"/> (P5.11c, DESIGN-data-model §2.7): <see cref="PicInfo"/> is
/// now a pure value record (the analyzed FACTS + the representation projections); the scanning, the SR2 symbol
/// whitelist, the category classification, and every entry-time diagnostic live here.
/// </summary>
public static class PictureAnalyzer
{
    /// <summary>
    /// Analyze a PICTURE string (already stripped of the <c>PIC</c> keyword) plus an optional usage keyword and the
    /// entry's own SIGN clause (<see langword="null"/> when the entry has none — a group-level SIGN may still apply,
    /// via the binder's post-build inheritance pass, ISO §13.18.52 GR1–3). <paramref name="currency"/> is the
    /// program's currency PICTURE SYMBOL (ISO §12.3.7 GR13; <c>$</c> per SR25) — a non-default symbol (e.g.
    /// NC107A's <c>W</c>, NC108M's <c>&lt;</c>) classifies exactly like <c>$</c>: its mask positions are
    /// fixed/floating currency insertion, making the item NUMERIC-EDITED (§13.18.40.4). <paramref name="edition"/>
    /// + <paramref name="where"/> carry the W2 loud-guard diagnostics: the symbol whitelist (§13.18.40.3 SR2) —
    /// a symbol outside the legal set is a COBOLNET0808 error, and the legal-but-unimplemented 2002+ symbols
    /// <c>N</c>/<c>1</c>/<c>E</c> route their introduction gates + a not-implemented error (never the historical
    /// silent fall-through to "pure numeric, zero digits"). Analyze sees the RAW picture — DECIMAL-POINT IS COMMA
    /// (ISO §13.18.40.3 SR13) swaps the ROLES of <c>,</c> and <c>.</c> at edit time (<c>CobolEdit.MaskScale</c>'s
    /// flag), not the symbols themselves, and both are whitelisted regardless.
    /// </summary>
    public static PicInfo Analyze(string picture, Usage usage, EditionContext edition, string where,
        SignSpec? sign = null, char currency = '$', bool blankWhenZero = false, bool explicitUsage = false,
        IReadOnlyList<EditingPhraseSpec>? editing = null)
    {
        // A TRAILING ';' is the clause SEPARATOR (ISO §8.3.5 rule 2 — a semicolon immediately followed by a
        // space is a separator; ';' is never a PICTURE symbol). The REAL cure is the W3 lexer-mode trim
        // (DEVLOG 596; VCR Table 7 row 7.14): PIC_STRING trims a trailing ','/';' when LA(1) is whitespace —
        // the separator shape — so a legal SR7 trailing-',' mask (NC125A's `…9,.`) keeps its comma. This
        // single-';'-strip stays as DEFENSE-IN-DEPTH for the funnel's other callers; `PIC 99;;` and a bare
        // `PIC ;` remain invalid and fall to the 0808 whitelist below (the adversarial-review fix for the
        // strip-to-empty leak).
        if (picture.EndsWith(';')) picture = picture[..^1];
        if (picture.Length == 0)
        {
            edition.Error("COBOLNET0808", $"invalid PICTURE character-string — {where} "
                + "(ISO §13.18.40.3 SR2: empty after separating the trailing ';' separator)");
            return PicInfo.Recovery();
        }

        // Expand (n) repetition into a flat symbol run, e.g. "X(4)" → "XXXX", "9(3)V99" → "999V99".
        string expanded = ExpandRepeats(picture);
        char cs = char.ToUpperInvariant(currency);

        // ── PICTURE EDITING phrases (ISO §13.18.40.2 Format 1, COBOL-2023): validate SR8–SR12 and build the
        // single-character render rules. char1Set lets the SR2 whitelist admit the declared editing characters
        // (else char-1 letters like 'L'/'T'/'G' would trip COBOLNET0808). The introduction gate below 2023 is fired
        // by VersionConformancePass.ParseArm.VisitPictureClause; the render-staged forms (multi-character literal,
        // floating character-1) raise COBOLNET0899 (P14 render GAP) here at ≥2023.
        var editRules = ValidateEditing(editing, expanded, edition, where, cs, out var char1Set);

        // ── The §13.18.40.3 SR2 symbol whitelist (the W2 loud guard). The legal ISO 2023 Format-1 symbols are
        // A B E N P S V X Z 0 1 9 / , . + - * CR DB and the program's currency symbol (§13.18.40.4 GR14;
        // ExpandRepeats has already uppercased, so the §8.1.3 GR3 case equivalence is folded). 'N' (national,
        // §8.5.2.10), '1' (boolean, §8.5.2.5) and 'E' (external float, §13.18.40.4 GR13b) are LEGAL 2002+
        // symbols with no implementation yet: each fires its ConstructRegistry introduction gate (0900 below
        // 2002) plus the not-implemented error at 2002+. Anything else is an invalid PICTURE. ──
        bool hasN = false, has1 = false, hasE = false;
        char? invalid = null;
        for (int i = 0; i < expanded.Length; i++)
        {
            char c = expanded[i];
            if (char1Set.Contains(c)) continue;   // a declared PICTURE EDITING character-1 (ISO §13.18.40.3 SR8) — not an invalid symbol
            switch (c)
            {
                // '$' is the currency picture symbol ONLY when no CURRENCY SIGN clause redefines it
                // (§12.3.7 SR25 default / §13.18.40.3 SR2) — under a custom symbol a stray '$' is not an
                // allowable picture symbol and previously slipped through as an ungated always-on leniency
                // (PIC $$$9 under CURRENCY "W" silently produced a wrong-shaped mask; adversarial-review fix,
                // DEVLOG 595). The corpus's custom-currency programs (NC107A 'W', NC108M '<') use no '$'
                // pictures, so the gate is corpus-safe.
                case '$' when cs == '$':
                    break;
                case 'A' or 'B' or 'P' or 'S' or 'V' or 'X' or 'Z'
                    or '0' or '9' or '/' or ',' or '.' or '+' or '-' or '*':
                    break;
                case 'C' when i + 1 < expanded.Length && expanded[i + 1] == 'R':
                case 'D' when i + 1 < expanded.Length && expanded[i + 1] == 'B':
                    i++; break;   // CR / DB — each two-character pair is ONE symbol (§13.18.40.3 SR12 NOTE 2)
                case 'N': hasN = true; break;
                case '1': has1 = true; break;
                case 'E': hasE = true; break;
                default:
                    if (c == cs) break;   // the program's currency symbol (ISO §12.3.7 GR13)
                    invalid ??= c;        // 'C'/'D' not opening CR/DB land here too — no legal lone use
                    break;
            }
        }
        if (hasE || invalid is not null || (hasN && has1))
        {
            if (hasE) StagedNotImplemented(edition, Constructs.PicExternalFloat2002, "Phase 6", where);   // the 0900 is now GateData via SkeletonGate below (14g.5)
            if (invalid is { } bad)
                // Wording is exact about what IS checked: symbol MEMBERSHIP in the SR2 inventory. The SR2/
                // §13.18.40.6 precedence-rule validation (symbol ORDER/multiplicity — 'PIC 99.99.99' etc.)
                // is a separate self-contained table walk, queued (adversarial-review minor, DEVLOG 595).
                edition.Error("COBOLNET0808", $"invalid PICTURE symbol '{bad}' in PICTURE {picture} — {where} "
                    + "(ISO §13.18.40.3 SR2: not an allowable picture symbol)");
            if (hasN && has1)
                // Precedence Table 10: the boolean symbol '1' combines with no other symbol; 'N' admits only
                // B 0 / N (§13.18.40.4 GR8–GR10) — a picture holding both can never be legal.
                edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} "
                    + "(ISO §13.18.40.6 Table 10: the 'N' and '1' picture symbols may not be combined)");
            // Recovery representation ONLY: the compile has already FAILED above — this shape merely keeps the
            // doomed emit pass crash-free (CompilerDriver reports bind diagnostics after Emit completes). An
            // external-float picture carries its 0900 forward on SkeletonGate for the bound-arm gate (14g.5).
            return PicInfo.Recovery(expanded.Length) with
                { SkeletonGate = hasE ? Constructs.PicExternalFloat2002 : null };
        }

        // ── Category national (§8.5.2.10) / boolean (§8.5.2.5) — LIVE, Phase 4a track (a). The introduction
        // gate stays at every entry point (COBOLNET0900 below 2002; the registry rows are silent at 2002+),
        // exactly the BINARY-CHAR/POINTER pattern. Usage resolution per §13.18.60.4: SR13a — PIC N with no
        // USAGE clause implies NATIONAL; SR20 — PIC N admits ONLY usage NATIONAL; SR13b — PIC 1 with no usage
        // is DISPLAY; SR5 — usage BIT requires a boolean picture; SR12 national-form boolean (PIC 1 USAGE
        // NATIONAL) is spec-legal but STAGED (0899). ──
        if (hasN)
        {
            if (expanded.All(c => c is 'N'))
            {
                // NationalData2002 (the introduction gate) fires on the RESOLVED item in the VersionConformancePass
                // GateData/GateReports enumerator (keyed on Pic.Category National); Step 14g.1.
                if (explicitUsage && usage is not Usage.National)
                    edition.Error("COBOLNET0881", $"{where}: a national PICTURE (symbol N) admits only USAGE "
                        + $"NATIONAL, not {usage} (ISO §13.18.60.3 SR20; SR13a implies NATIONAL when no USAGE "
                        + "clause is specified)");
                return new PicInfo(PicCategory.National, Usage.National,
                    Length: expanded.Length, Digits: 0, Scale: 0, Signed: false);
            }
            if (expanded.All(c => c is 'N' or 'B' or '0' or '/'))
            {
                // NATIONAL-EDITED (§13.18.40.4 GR10 / §8.5.2.11) — recognized, edition-gated, STAGED. The 0900 rides
                // SkeletonGate to the bound-arm GateData (14g.5); the ≥2002 0899 stays inline.
                StagedNotImplemented(edition, Constructs.NationalEdited2002, "Phase 4a residue", where);
                return PicInfo.Recovery(expanded.Length) with { SkeletonGate = Constructs.NationalEdited2002 };
            }
            edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} "
                + "(ISO §13.18.40.6 Table 10: 'N' may be combined only with the insertion symbols B 0 /)");
            return PicInfo.Recovery(expanded.Length);
        }
        if (has1)
        {
            if (expanded.All(c => c is '1'))
            {
                // BooleanData2002 (the introduction gate) fires on the RESOLVED item in the VersionConformancePass
                // GateData enumerator (keyed on Pic.Category Boolean); Step 14g.1.
                switch (usage)
                {
                    case Usage.Display or Usage.Bit:
                        break;   // display-form (SR13b) and bit-form (SR5) — identical D-B1 string storage
                    case Usage.National:
                        // SR12 admits a boolean picture under USAGE NATIONAL — spec-legal, representation
                        // staged (one national char per boolean position; nothing constructs it yet).
                        edition.Error(DiagnosticCatalog.NationalData, $"national-form boolean data (PIC 1 with USAGE NATIONAL) "
                            + $"is recognized but not yet implemented (Phase 4a residue) — {where} "
                            + "(ISO §13.18.60.3 SR12)");
                        usage = Usage.Display;
                        break;
                    default:
                        edition.Error("COBOLNET0881", $"{where}: a boolean PICTURE (symbol 1) admits only USAGE "
                            + $"DISPLAY, BIT, or NATIONAL, not {usage} (ISO §13.18.60.3 SR5/SR12/SR13b)");
                        usage = Usage.Display;
                        break;
                }
                return new PicInfo(PicCategory.Boolean, usage,
                    Length: expanded.Length, Digits: 0, Scale: 0, Signed: false);
            }
            edition.Error("COBOLNET0808", $"invalid PICTURE {picture} — {where} "
                + "(ISO §13.18.40.6 Table 10: the boolean symbol '1' may not be combined with any other symbol)");
            return PicInfo.Recovery(expanded.Length);
        }

        // ── USAGE BIT / NATIONAL against a picture WITHOUT the matching symbol (§13.18.60.4). BIT requires a
        // boolean picture (SR5 — hard error). NATIONAL admits national/boolean pictures (handled above) plus
        // the national-form NUMERIC legs (SR12 — spec-legal, STAGED 0899); an alphabetic/alphanumeric picture
        // under NATIONAL is illegal outright (SR12 + §13.18.40.3 SR30). Both recover to Display — the compile
        // has already failed; the value only keeps the doomed emit crash-free. ──
        if (usage is Usage.Bit)
        {
            edition.Error("COBOLNET0881", $"{where}: USAGE BIT requires a boolean PICTURE (symbol 1 only) — "
                + $"PICTURE {picture} is not boolean (ISO §13.18.60.3 SR5)");
            usage = Usage.Display;
        }
        else if (usage is Usage.National)
        {
            if (expanded.Any(c => c is 'X' or 'A'))
            {
                edition.Error("COBOLNET0881", $"{where}: USAGE NATIONAL may not be specified with an "
                    + $"alphabetic or alphanumeric PICTURE ({picture}) — it admits boolean, national, "
                    + "national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; "
                    + "§13.18.40.3 SR30)");
            }
            else
            {
                edition.Error(DiagnosticCatalog.NationalData, $"national-form numeric data (a numeric or numeric-edited "
                    + $"PICTURE {picture} with USAGE NATIONAL — national digits) is recognized but not yet "
                    + $"implemented (Phase 4a residue) — {where} (ISO §13.18.60.3 SR12)");
            }
            usage = Usage.Display;
        }

        bool signed = expanded.Contains('S');
        bool hasV = expanded.Contains('V');
        int digits = expanded.Count(c => c is '9');
        int afterV = hasV ? expanded[(expanded.IndexOf('V') + 1)..].Count(c => c is '9') : 0;

        // PICTURE 'P' scaling positions (ISO §13.18.40): each P holds no digit and no storage but shifts the implied
        // decimal point. TRAILING P (e.g. 99P) scales the stored digits UP → a NEGATIVE fraction scale (the value is
        // a multiple of 10^P). LEADING P (e.g. P(4)9) puts the point left of every digit → scale = leadingP + the
        // digit count (all 9s are fractional). The net SIGNED scale flows through the whole numeric pipeline; the
        // runtime Rescale handles a negative scale natively (Pow10 of the always-non-negative scale difference).
        // P positions classify against the DIGIT POSITIONS (9 and the suppression symbols Z/* — an EDITED
        // P-scaled mask like ZZZPP has no '9' at all, NC124A PICTURE-TEST-30), not just the literal nines.
        int firstNine = expanded.IndexOfAny(['9', 'Z', '*']), lastNine = expanded.LastIndexOfAny(['9', 'Z', '*']);
        int leadingP = 0, trailingP = 0;
        for (int i = 0; i < expanded.Length; i++)
            if (expanded[i] == 'P') { if (firstNine < 0 || i < firstNine) leadingP++; else if (i > lastNine) trailingP++; }
        int digitPositions = expanded.Count(c => c is '9' or 'Z' or '*');
        int scale = trailingP > 0 ? -trailingP : leadingP > 0 ? leadingP + digitPositions : afterV;

        // §13.18.40.3 SR14 DIGIT-POSITION count for the 1–31 capacity cap: the 9/Z/* positions, every P (counted in the
        // maximum digit positions though it stores no digit, §13.18.40.4), and the floating '+'/'-'/currency digit
        // positions — a floating string of a symbol appearing k≥2 times contributes k−1 (the leftmost is the
        // sign/currency, not a digit). Counting SYMBOL occurrences (not run length) naturally excludes embedded simple
        // insertions ($$,$$9 → 4 '$' → 3 digit positions). For a pure-numeric picture (no Z/*, no floating) this equals
        // Digits + P. (CA33 — the cap must NOT undercount to only the '9's, which let Z(35)/Z(11)9(8) slip past.)
        int floatingExtra = 0;
        foreach (char fsym in new[] { '+', '-', cs })
        {
            int fc = expanded.Count(ch => ch == fsym);
            if (fc >= 2) { floatingExtra = fc - 1; break; }   // §13.18.40.5 — at most one floating string per picture
        }
        int digitPos = digitPositions + leadingP + trailingP + floatingExtra;

        bool anyAlpha = expanded.Any(c => c is 'X' or 'A');
        // CR / DB are fixed-insertion editing symbols too (ISO §13.18.40.4) — `PIC 9(5)CR` is NUMERIC-EDITED
        // (NC104A MOVE-TEST-F1-14), not pure numeric with stray letters. The program's currency symbol (ISO
        // §12.3.7 GR13) is an editing symbol exactly like '$' — without it a `PIC WWWWW` would fall through to
        // "pure numeric, zero digits".
        bool anyEdit = expanded.Any(c => c is 'Z' or '*' or '+' or '-' or ',' or '.' or '$' or 'B' or '0' or '/' || c == cs)
            || expanded.Contains("CR", StringComparison.Ordinal) || expanded.Contains("DB", StringComparison.Ordinal)
            || char1Set.Count > 0;   // a PICTURE EDITING character-1 makes the item numeric-/alphanumeric-edited (ISO §13.18.40.5 Table 7)

        // ── USAGE BINARY / COMPUTATIONAL / PACKED-DECIMAL against an alphabetic/alphanumeric picture (ISO
        // §13.18.60.3 SR3): such a usage "shall be specified only with a picture character-string that describes a
        // numeric item". Mirrors the BIT SR5 / NATIONAL SR12 guards above — without it a `PIC XX COMP` silently
        // bound as category Alphanumeric with the numeric usage DROPPED. Recover to Display (the compile has
        // already failed; the value only keeps the doomed emit crash-free). The picture-less BINARY-CHAR/-SHORT/
        // -LONG/-DOUBLE usages take no PICTURE — a picture with them is a distinct error handled elsewhere.
        if (anyAlpha && usage is Usage.Binary or Usage.Comp5 or Usage.Packed)
        {
            string kw = usage switch
            {
                Usage.Binary => "BINARY", Usage.Packed => "PACKED-DECIMAL", Usage.Comp5 => "COMPUTATIONAL-5",
                _ => usage.ToString(),
            };
            edition.Error("COBOLNET0881", $"{where}: USAGE {kw} requires a PICTURE that describes a numeric "
                + $"item — PICTURE {picture} is alphabetic/alphanumeric (ISO §13.18.60.3 SR3)");
            usage = Usage.Display;
        }

        if (anyAlpha)
        {
            // ALPHANUMERIC-EDITED (ISO §13.18.40 — X/A/9 with B 0 / simple insertion, plus any EDITING character-1):
            // every position counts in the length, and the mask drives MOVE editing. A plain alphanumeric has no
            // insertion symbols and no editing character-1.
            bool edited = expanded.Any(c => c is 'B' or '0' or '/') || char1Set.Count > 0;
            return new PicInfo(PicCategory.Alphanumeric, usage,
                Length: expanded.Count(c => c is 'X' or 'A' or '9' or 'B' or '0' or '/' || char1Set.Contains(c)),
                Digits: 0, Scale: 0, Signed: false)
            { EditMask = edited ? expanded : null, IsAlphabetic = expanded.All(c => c is 'A'),
              EditingRules = editRules };
        }

        string signKind = PicInfo.SignKindFor(usage, signed, sign);

        // BLANK WHEN ZERO on a category-numeric picture DEFINES the item as numeric-edited (ISO §13.18.8 GR2;
        // SR1 admits it only without 'S', SR2 only usage display/national) — NC108M's `PIC 9(9) BLANK ZERO`
        // holds SPACES after a zero store and compares as an alphanumeric item.
        if (anyEdit || (blankWhenZero && digits > 0 && !signed && usage is Usage.Display))
            // Numeric-edited: the .NET storage is the formatted display image (string); width = edited symbol
            // count. NOTE no digits>0 requirement — an all-symbol mask (PIC ****, $$$$) is numeric-edited too,
            // its digit positions being the Z/*/floating symbols themselves (§13.18.40).
            return new PicInfo(PicCategory.NumericEdited, usage,
                Length: expanded.Count(c => c is not ('V' or 'S' or 'P')), Digits: digits, Scale: scale, Signed: signed)
            { SignKind = signKind, EditMask = expanded, EditingRules = editRules, DigitPositions = digitPos };

        // Pure numeric. The stored-digit count (Digits) and DISPLAY width (Length) are the '9' count — P holds no
        // storage; the implied decimal position lives entirely in the signed Scale.
        return new PicInfo(PicCategory.Numeric, usage, Length: digits, Digits: digits, Scale: scale, Signed: signed)
        { SignKind = signKind, DigitPositions = digitPos };
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

    /// <summary>Validate the PICTURE EDITING phrases (ISO §13.18.40.3 SR8–SR12; COBOL-2023) and build the
    /// single-character render rules. Emits the SR diagnostics (COBOLNET1591–1596); the render-staged forms
    /// (a literal wider than one character, or a floating character-1 — the same character-1 appearing ≥2 times
    /// under a FOR phrase) raise the P14 render-GAP COBOLNET0899 at ≥2023 and contribute NO render rule (the item
    /// still binds numeric-edited). <paramref name="char1Set"/> (uppercased) collects every accepted character-1 so
    /// the SR2 whitelist admits them. Returns null when there are no phrases, an SR error, or a staged phrase.</summary>
    private static IReadOnlyList<CobolEdit.EditRule>? ValidateEditing(
        IReadOnlyList<EditingPhraseSpec>? editing, string expanded, EditionContext edition, string where, char cs,
        out HashSet<char> char1Set)
    {
        char1Set = [];
        if (editing is null || editing.Count == 0) return null;

        // Pre-scan every phrase's character-1 (SR25 admits up to two extended sign symbols — e.g. a leftmost 'L'
        // and a rightmost 'F') so SR12b's "only character-1 and 9 . cs P V Z" test admits ALL declared editing
        // characters, not just the phrase under validation.
        var allChar1 = new HashSet<char>();
        foreach (var ph0 in editing)
            if ((ph0.Char1Text ?? "") is { Length: 1 } t0 && char.IsLetter(t0[0])) allChar1.Add(char.ToUpperInvariant(t0[0]));

        var rules = new List<CobolEdit.EditRule>();
        bool error = false, staged = false;

        foreach (var ph in editing)
        {
            // character-1 (§13.18.40.3 SR8): a single basic letter, not a CURRENCY-SIGN letter and not one of
            // A B C D E N P R S V X Z.
            string c1 = ph.Char1Text ?? "";
            if (c1.Length != 1 || !char.IsLetter(c1[0]))
            {
                edition.Error("COBOLNET1591", $"{where}: PICTURE EDITING character-1 must be a single basic letter "
                    + $"(ISO §13.18.40.3 SR8; got \"{c1}\")");
                error = true; continue;
            }
            char char1 = char.ToUpperInvariant(c1[0]);
            if (char1 == cs || "ABCDENPRSVXZ".IndexOf(char1) >= 0)
            {
                edition.Error("COBOLNET1591", $"{where}: PICTURE EDITING character-1 '{c1}' must be a basic letter "
                    + "other than A B C D E N P R S V X Z or a CURRENCY-SIGN letter (ISO §13.18.40.3 SR8)");
                error = true; continue;
            }
            // SR11: distinct character-1 across phrases.
            if (!char1Set.Add(char1))
            {
                edition.Error("COBOLNET1592", $"{where}: PICTURE EDITING character-1 '{c1}' is specified in more than "
                    + "one EDITING phrase (ISO §13.18.40.3 SR11 — each character-1 shall be distinct)");
                error = true; continue;
            }
            // SR10: character-1 shall appear at least once in the character-string.
            int occ = expanded.Count(x => char.ToUpperInvariant(x) == char1);
            if (occ == 0)
            {
                edition.Error("COBOLNET1593", $"{where}: PICTURE EDITING character-1 '{c1}' does not appear in the "
                    + "PICTURE character-string (ISO §13.18.40.3 SR10)");
                error = true; continue;
            }
            // SR9: no literal may exceed 50 characters (the national-vs-alphanumeric class matches the item's
            // category; the national-literal sub-check rides the national-edited staging).
            foreach (string? lit in new[] { ph.Simple, ph.Neg, ph.Pos })
                if (lit is { Length: > 50 })
                {
                    edition.Error("COBOLNET1594", $"{where}: a PICTURE EDITING literal exceeds 50 characters "
                        + "(ISO §13.18.40.3 SR9)");
                    error = true;
                }

            if (ph.IsForForm)
            {
                // SR12b: a FOR (extended sign-control) picture may contain only character-1 and 9 . cs P V Z.
                foreach (char mc in expanded)
                {
                    char mu = char.ToUpperInvariant(mc);
                    if (allChar1.Contains(mu) || mu is '9' or '.' or 'P' or 'V' or 'Z' || mu == cs) continue;
                    edition.Error("COBOLNET1596", $"{where}: with a FOR (extended editing sign control) EDITING "
                        + $"phrase the PICTURE character-string may contain only character-1 and 9 . {cs} P V Z "
                        + $"(ISO §13.18.40.3 SR12b; found '{mc}')");
                    error = true; break;
                }
                // SR12a: the NEGATIVE and POSITIVE literals (when both present) occupy the same number of positions.
                if (ph.Neg is { } n2 && ph.Pos is { } p3 && n2.Length != p3.Length)
                {
                    edition.Error("COBOLNET1595", $"{where}: the NEGATIVE and POSITIVE literals of a FOR EDITING "
                        + "phrase shall occupy the same number of character positions (ISO §13.18.40.3 SR12a)");
                    error = true; continue;
                }
                // SR12c: the unspecified side defaults to spaces of the specified literal's width. LANDABLE only for
                // a single-character literal at a SINGLE character-1 occurrence (fixed sign control); a wider literal
                // or a repeated character-1 (floating string) is the P14 render GAP.
                int width = (ph.Neg ?? ph.Pos)?.Length ?? 0;
                if (width == 1 && occ == 1)
                {
                    char neg = ph.Neg is { Length: 1 } ? ph.Neg[0] : ' ';
                    char pos = ph.Pos is { Length: 1 } ? ph.Pos[0] : ' ';
                    rules.Add(new CobolEdit.EditRule(char1, neg, pos));
                }
                else staged = true;
            }
            else
            {
                // IS (simple insertion) form — sign-independent (ISO §13.18.40.5 editing rule 3): character-1
                // inserts literal-1 at every occurrence, immune to sign. LANDABLE for a single-character literal
                // (any occurrence count); a wider literal is the P14 render GAP.
                string lit = ph.Simple ?? "";
                if (lit.Length == 1) rules.Add(new CobolEdit.EditRule(char1, lit[0], lit[0]));
                else staged = true;
            }
        }

        if (staged && edition.DialectLevel >= 2023)
            edition.Error(DiagnosticCatalog.ConstructStagedNotImplemented, $"{where}: a multi-character-literal or "
                + "floating PICTURE EDITING phrase is recognized but its variable-width render is not yet implemented "
                + "(ISO §13.18.40.5; the P14 render GAP — single-character insertion and single-occurrence sign "
                + "control are supported)");

        // Any SR error OR a staged phrase → no landable rule set is applied (the item still binds numeric-edited;
        // under the doomed emit character-1 renders verbatim — harmless, the compile has already failed).
        return error || staged || rules.Count == 0 ? null : rules;
    }

    /// <summary>
    /// Map a COBOL usage keyword (e.g. <c>COMP-3</c>) to a <see cref="Usage"/>. EVERY grammar-accepted keyword
    /// (the ISO §13.18.60 inventory in <c>CobolData.g4 usageClause/usageKeyword</c>) is recognized EXPLICITLY —
    /// the historical silent catch-all mapped the whole 2002 inventory (NATIONAL, BIT, POINTER, OBJECT
    /// REFERENCE, the FLOAT-x and BINARY-x families) to <see cref="Usage.Display"/>, a wrong-answer misbind (the
    /// W2 loud-guard sweep). An unrecognized keyword is a LOUD internal error — never Display. (The former
    /// <c>out bool skeleton</c> overload is DELETED, P5.11c: every keyword has been LIVE since the 14g.1
    /// introduction-gate migration and nothing ever set the flag — the parameter was constant-false dead code.)
    /// </summary>
    public static Usage ParseUsage(string? keyword, EditionContext edition, string where)
    {
        switch (keyword?.ToUpperInvariant().Replace("COMPUTATIONAL", "COMP"))
        {
            case null or "DISPLAY": return Usage.Display;
            case "COMP" or "COMP-4" or "BINARY": return Usage.Binary;
            case "COMP-3" or "PACKED-DECIMAL": return Usage.Packed;
            case "COMP-5": return Usage.Comp5;
            case "COMP-1": return Usage.Float;
            case "COMP-2": return Usage.Double;
            case "INDEX": return Usage.Index;
            // USAGE NATIONAL / BIT — LIVE (Phase 4a track (a)): only the introduction gate remains (0900
            // below 2002; the registry rows are silent at 2002+), the POINTER/BINARY-CHAR pattern. Picture
            // conformance (SR5/SR12/SR13/SR20) is Analyze's job; a picture-LESS elementary entry is caught at
            // the group-fixup pass (DataBinder.ResolveIndexItems — a group header legally sheds the usage to
            // its subordinates per §13.18.60.4 GR1).
            case "NATIONAL":
                return Usage.National;
            case "BIT":
                return Usage.Bit;
            // USAGE POINTER — LIVE (Phase-4b increment 1): only the introduction gate remains (0900 below
            // 2002; the registry row is silent at 2002+), like OBJECT REFERENCE. The caller synthesizes
            // PicInfo.PointerItem (PICTURE-less, the IndexItem pattern).
            case "POINTER":
                return Usage.Pointer;
            // USAGE PROGRAM-POINTER — LIVE (P10 Step 7): the introduction gate (0900 below 2002) fires from
            // UsageConstructId; the caller synthesizes PicInfo.ProgramPointerItem (PICTURE-less, the
            // PointerItem pattern). The restricted TO-prototype form stages loud at the BindEntry site.
            case "PROGRAM-POINTER":
                return Usage.ProgramPointer;
            // USAGE FUNCTION-POINTER — recognized, STAGED LOUD (function prototypes = P13): the member flows
            // through so the 2014 introduction gate still fires below 2014; at/above 2014 the named 0899-band
            // rejection is the honest state (never a silent Display misbind).
            case "FUNCTION-POINTER":
                edition.Error(DiagnosticCatalog.UsageFunctionPointer,
                    $"{where}: USAGE FUNCTION-POINTER (ISO §13.18.60 — a function-pointer data item)");
                return Usage.FunctionPointer;
            // LIVE as of the Phase-3 OO spine: only the introduction gate remains (0900 below 2002 — the
            // registry row is silent at 2002+); the caller synthesizes PicInfo.ObjectReferenceItem with the
            // declared class name (PICTURE-less per §13.18.60.4, the IndexItem pattern).
            case "OBJECT REFERENCE":
                return Usage.ObjectReference;
            // The fixed-width binary usages — LIVE (Phase 4 M2-DATA-1): only the introduction gate remains
            // (0900 below 2002; the registry row is silent at 2002+, like POINTER / OBJECT REFERENCE). The
            // caller synthesizes PicInfo.BinaryItem (PICTURE-less per §13.16.3 SR8; the IndexItem pattern).
            case "BINARY-CHAR":
                return Usage.BinaryChar;
            case "BINARY-SHORT":
                return Usage.BinaryShort;
            case "BINARY-LONG":
                return Usage.BinaryLong;
            case "BINARY-DOUBLE":
                return Usage.BinaryDouble;
            case "FLOAT-SHORT":   // the implementor-defined float trio (§13.18.60.4 GR13) — LIVE (Phase 6a, D16)
                return Usage.FloatShort;
            case "FLOAT-LONG":
                return Usage.FloatLong;
            case "FLOAT-EXTENDED":
                return Usage.FloatExtended;
            // The COBOL-2014 IEEE-754 interchange float family (§13.18.60.4 GR14-18). binary32/64 map EXACTLY to
            // native float/double (the pinned ISO/IEC 60559:2020 formats are conforming) — LIVE (P12 wave 3). The
            // introduction gate (0900 below 2014) fires from UsageConstructId.
            case "FLOAT-BINARY-32":
                return Usage.FloatBinary32;
            case "FLOAT-BINARY-64":
                return Usage.FloatBinary64;
            // FLOAT-BINARY-128 (binary128, GR16) and FLOAT-DECIMAL-16/34 (decimal64/128, GR17-18) are
            // PROCESSOR-DEPENDENT language elements (Annex A.3 items 17/19): .NET has no IEEE binary128 or IEEE
            // decimal64/128 type, and GR16-18 PIN the formats (a double/System.Decimal approximation would be
            // NON-conforming — the P12 re-scout catch). Documented non-support — rejected LOUD (never a silent wrong
            // representation). The member flows through so the 2014 introduction gate still fires below 2014.
            case "FLOAT-BINARY-128":
                edition.Error("COBOLNET1564", $"{where}: USAGE FLOAT-BINARY-128 (ISO/IEC 60559:2020 binary128, "
                    + "ISO §13.18.60.4 GR16) is a processor-dependent language element not supported by COBOL.NET "
                    + "(Annex A.3 item 17): .NET provides no IEEE 754 binary128 type, and GR16 pins the format so a "
                    + "double-backed approximation would be non-conforming");
                return Usage.FloatBinary128;
            case "FLOAT-DECIMAL-16":
                edition.Error("COBOLNET1564", $"{where}: USAGE FLOAT-DECIMAL-16 (ISO/IEC 60559:2020 decimal64, "
                    + "ISO §13.18.60.4 GR17) is a processor-dependent language element not supported by COBOL.NET "
                    + "(Annex A.3 item 19): .NET provides no IEEE 754 decimal64 type (System.Decimal is a different "
                    + "format)");
                return Usage.FloatDecimal16;
            case "FLOAT-DECIMAL-34":
                edition.Error("COBOLNET1564", $"{where}: USAGE FLOAT-DECIMAL-34 (ISO/IEC 60559:2020 decimal128, "
                    + "ISO §13.18.60.4 GR18) is a processor-dependent language element not supported by COBOL.NET "
                    + "(Annex A.3 item 19): .NET provides no IEEE 754 decimal128 type");
                return Usage.FloatDecimal34;
            case { } other:
                // The grammar admits nothing else — reaching here is a compiler defect (a new grammar
                // alternative without its ParseUsage arm). LOUD, never a silent Display misbind.
                edition.Error(DiagnosticCatalog.UsageKeywordUnmappedInternal,
                    $"internal: unrecognized USAGE keyword '{other}' — {where} (ISO §13.18.60; every "
                    + "grammar-accepted usage keyword must have an explicit ParseUsage mapping)");
                return Usage.Display;
        }
    }

    /// <summary>The ≥edition half of the W2 skeleton gate for a recognized-but-unimplemented PICTURE construct
    /// (external-float symbol E / national-edited): at or above the row's introducing edition — where the
    /// introduction <c>Check</c> is silent — a COBOLNET0899 "recognized but not yet implemented" naming the owning
    /// roadmap phase. Below the edition it is a NO-OP: the COBOLNET0900 introduction gate is fired instead by the
    /// post-bind <c>VersionConformancePass</c> GateData enumerator over <c>PicInfo.SkeletonGate</c> (Step 14g.5 — the
    /// category is recovered to Alphanumeric, erasing the parse identity, so the flag carries the gate forward). Either
    /// way the compile FAILS below its edition (the 0900) or above (the 0899) — never a silent misbind.</summary>
    private static void StagedNotImplemented(EditionContext edition, string rowId, string phase, string where)
    {
        var row = ConstructRegistry.Find(rowId)
            ?? throw new ArgumentException($"unregistered construct id '{rowId}'", nameof(rowId));
        if (edition.DialectLevel >= row.IntroducedIn)
            edition.Error(DiagnosticCatalog.ConstructStagedNotImplemented, $"{row.Display} is recognized but not yet implemented (owning "
                + $"roadmap phase: {phase}) — {where} ({row.Citation})");
    }
}
