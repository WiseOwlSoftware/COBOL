// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Validation;

namespace CobolNet.Binding.Bound;

/// <summary>
/// The MOVE figurative-constant edition gates (ISO/IEC 1989:2023 §14.9.25.3 SR5; roadmap Phase 2 W2 track A —
/// VCR rows 1 / 92 / 128): SR5 permits "an ALL "literal" figurative constant (containing only digits) or an ALL
/// symbolic-character (representing a digit)" to move "to an integer numeric item"; "in all other cases, the move
/// of an alphanumeric figurative constant (SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", or ALL
/// symbolic-character) to either a numeric item or a numeric-edited item is prohibited". The prohibition is NEW in
/// 2023 — these moves were permitted through ISO 2014 (Annex E.2 item 1 bullet 1, the removals list) — so the
/// registry row "move-alphanumeric-figurative-removed-2023" routes it: silent below 2023, error at 2023 strict,
/// warning at 2023 --permissive (the pre-removal semantics preserved). The surviving digit-only-ALL-to-integer
/// case is itself obsolete at 2023 (the SR5 NOTE; Annex F.2 item 2) — row "move-all-digit-integer-obsolete-2023",
/// a 0903 warning at ≥2023 and silent below.
/// </summary>
public sealed partial class StatementBinder
{
    /// <summary>
    /// Apply the §14.9.25.3 SR5 gates to one bound MOVE (Format 1) and flag the pre-removal receivers'
    /// storage. Exemptions honored:
    /// <list type="bullet">
    /// <item>ZERO/ZEROS/ZEROES — the NUMERIC figurative (§8.3.3.6.4 GR4 "the numeric value '0'"); SR5's
    /// prohibition names only the alphanumeric figuratives, and Table 17 gives ZERO category numeric against a
    /// numeric receiver.</item>
    /// <item>GROUP receivers — a group move is a character copy without conversion (§14.9.25.4 GR4), not a
    /// numeric elementary move, so SR5 does not reach it.</item>
    /// <item>Reference-modified receivers — the unique result of reference modification is an elementary
    /// ALPHANUMERIC item whatever the underlying item (§8.4.2.4), so the receiver is not numeric.</item>
    /// <item>The digit-only single-character ALL "literal" into an INTEGER numeric item — SR5's surviving
    /// exception, valid at every edition; obsolete-flagged 0903 at ≥2023 (SR5 NOTE; Annex F.2 item 2).</item>
    /// </list>
    /// A digit-only ALL longer than one character is NOT the exception even though its characters are digits:
    /// §8.3.3.6.3 SR3 forbids associating an ALL literal whose length is greater than one with a numeric or
    /// numeric-edited item (the '85 obsolete element the legacy oracle still accepts — MOVE ALL "57" TO PIC 9(3)
    /// stores 575), so at 2023 it falls under the 0902 removal row with the other prohibited shapes.
    /// A digit-only ALL to a NON-integer numeric receiver (PIC 9V9) is likewise outside SR5's exception —
    /// 0902 at 2023; pre-2023 it fills every digit position (legacy-oracle adjudicated, provisional).
    /// </summary>
    private void MoveFigurativeEditionGates(BoundOperand source, IReadOnlyList<Place> targets)
    {
        // Classify the SENDER. Only the SR5 alphanumeric figuratives participate: SPACE, QUOTE, HIGH-VALUE,
        // LOW-VALUE, and ALL "literal". (ALL symbolic-character is in SR5's list too, but SYMBOLIC CHARACTERS
        // is not yet bound — its gate rides the same rows when it lands. NULL is not a §8.3.3.6 figurative
        // format and stays on its own path.)
        BoundAllLiteral? all = source as BoundAllLiteral;
        string figText = source switch
        {
            BoundFigurative { Kind: 'S' } => "SPACE",
            BoundFigurative { Kind: 'Q' } => "QUOTE",
            BoundFigurative { Kind: 'H' } => "HIGH-VALUE",
            BoundFigurative { Kind: 'L' } => "LOW-VALUE",
            BoundAllLiteral a => $"ALL \"{a.Literal}\"",
            _ => string.Empty,
        };
        if (figText.Length == 0) return;

        foreach (var t in targets)
        {
            if (t is RefModPlace || t.Item.IsGroup || t.Item.Pic is not { } pic) continue;   // exemptions above
            if (pic.Category is not (PicCategory.Numeric or PicCategory.NumericEdited)) continue;

            string where = $"MOVE {figText} TO {t.Item.CobolName}";
            // An INTEGER numeric item: fixed-point with no digit positions right of the decimal point (a
            // trailing-P picture scales by tens and is still integer-valued; a leading-P fraction is not).
            bool integerReceiver = pic is { Category: PicCategory.Numeric, IsFloat: false, Scale: <= 0 };
            if (all is { IsDigitOnly: true, Literal.Length: 1 } && integerReceiver)
            {
                // SR5's surviving exception — valid everywhere, obsolete at 2023 (0903; VCR rows 92/128).
                ConstructRegistry.Check(data.Edition, "move-all-digit-integer-obsolete-2023", where);
                continue;
            }
            // Every other shape: removed by ISO 2023 (Annex E.2 item 1 bullet 1; 0902 — VCR row 1).
            ConstructRegistry.Check(data.Edition, "move-alphanumeric-figurative-removed-2023", where);

            // Pre-removal storage (reachable at --std 85/2002/2014 and at 2023 --permissive): a NON-digit fill
            // (SPACE/QUOTE/HIGH-VALUE/LOW-VALUE, or an ALL literal containing a non-digit) deposits the fill
            // CHARACTERS as the receiver's character image — provisional (ratified decision 1; the legacy
            // oracle's byte fill: MOVE QUOTE TO PIC 9(3) leaves three quotation marks, IS NUMERIC is then
            // false, and a later numeric read decodes deterministically per §14.6.13.2). Flag an eligible
            // elementary numeric-DISPLAY receiver StoreAsImage — the SAME whole-group image substrate
            // DataBinder.MarkImageLeaves / the emitter's MarkStoreAsImage pass use (§14.9 MOVE GR4), with the
            // same eligibility rule; the storage-form bridge overloads (CobolNum.FormatDisplay/StoreDisplay —
            // see NumericImagePlace) absorb places resolved before this flag flips. A Tier-B REDEFINES window
            // needs no flag (its Write already takes the character image); a numeric-edited receiver is
            // string-backed by nature; a digit-only ALL stores its numeric value natively (no image needed).
            // An item in a REDEFINES shared-storage class keeps the flag its (already-run) tier classification
            // assigned — flipping it post-classification would desync the class backing (an unflagged Tier-A
            // alias receiver stays the emitter's narrow loud guard).
            if (all is not { IsDigitOnly: true } && t is not (RedefViewPlace or NumericImagePlace)
                && t.Item.Class is null
                && pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                t.Item.StoreAsImage = true;
        }
    }
}
