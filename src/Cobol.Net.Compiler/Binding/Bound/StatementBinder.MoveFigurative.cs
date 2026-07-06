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
        // The §14.9.25.3 SR1 class check FIRST — version-invariant, every sender kind: "The class of
        // identifier-1 or identifier-2 shall not be index, message-tag, object, or pointer." An index data
        // item may be referenced only by SET, SEARCH, relation conditions, and as a function/USING argument
        // (§13.18.60 GR10) — a MOVE operand of class index is invalid at EVERY edition, never an Annex-E
        // removal (the W2 adversarial review caught the 0902 row mislabeling it "permitted through 2014").
        // Message-tag/object/pointer classes cannot reach a bound MOVE yet (their usages are compile-gated
        // skeletons, W2 track B) — this check gains those arms when their phases land.
        if (source is BoundFieldOperand { Place.Item.Pic.Usage: Usage.Index } sIdx)
            data.Edition.Error("COBOLNET0809",
                $"a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §13.18.60 GR10 — only SET, "
                + $"SEARCH, and relation conditions may reference an index data item) — MOVE {sIdx.Place.Item.CobolName}");
        foreach (var t in targets)
            if (t.Item.Pic is { Usage: Usage.Index })
                data.Edition.Error("COBOLNET0809",
                    $"a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §13.18.60 GR10) — "
                    + $"MOVE … TO {t.Item.CobolName}");

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
            if (pic.Usage is Usage.Index) continue;   // class index — SR1 errored above, never an SR5 row

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
            // QUOTE is the ONE figurative the spec's own change annex tracks separately: QUOTE→numeric was
            // designated OBSOLETE by ISO 2014 (Annex E.2 item 21 — "features that were classified as obsolete
            // in the previous COBOL standard"), then removed with the rest at 2023. Its row therefore carries
            // obsoleteIn 2014 (0903 warning at 2014) on top of the removal edge — the W2 adversarial review's
            // correction to VCR row 1's blanket "not even flagged obsolete in 2014" wording.
            if (source is BoundFigurative { Kind: 'Q' })
            {
                ConstructRegistry.Check(data.Edition, "move-quote-numeric-obsolete-2014", where);
            }
            else
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

    /// <summary>The receiver-view data category of a MOVE target for the §14.9.25.3 Table 16 legality check:
    /// a reference-modified receiver is the unique elementary item of its INNER's class — alphanumeric for the
    /// classic categories (§8.4.3.3 GR6), but ref-mod of a national/boolean item stays national/boolean (GR1
    /// positions are national/boolean positions; GR5a). Groups return null (a group move is a conversion-free
    /// character copy, GR4 — Table 16 does not reach it; bit/national GROUP-USAGE groups are grammar residue).</summary>
    private static PicCategory? MoveReceiverCategory(Place t) => t switch
    {
        _ when t.Item.IsGroup || t.Item.Pic is null => null,
        RefModPlace rm => rm.Inner.Item.Pic?.Category switch
        {
            PicCategory.National => PicCategory.National,
            PicCategory.Boolean => PicCategory.Boolean,
            _ => PicCategory.Alphanumeric,
        },
        _ => t.Item.Pic!.Category,
    };

    /// <summary>
    /// The §14.9.25.3 Table 16 category-legality arms for the 2002 categories (boolean / national), plus SR7
    /// (a figurative whose characters are not boolean shall not move to a boolean item) — all COBOLNET0819,
    /// version-invariant at ≥2002 (below 2002 the operands themselves are already 0900-introduction-gated).
    /// Only the arms Table 16 marks "No" around the NEW categories are checked here — the classic
    /// alphanumeric/numeric rows keep their existing paths (VCR rows 1/92/128 above). A GROUP sender or
    /// receiver is exempt (GR4 group moves copy characters without conversion).
    /// </summary>
    private void MoveCategoryLegality(BoundOperand source, IReadOnlyList<Place> targets)
    {
        // ── The SENDER's category view ──
        PicCategory? senderCat = source switch
        {
            BoundStringLiteral sl => sl.Category,
            BoundAllLiteral al => al.Category,
            BoundFieldOperand { Place: RefModPlace rm } => MoveReceiverCategory(rm),   // same view rule
            BoundFieldOperand f when f.Place.Item.IsGroup || f.Place.Item.Pic is null => null,   // GR4
            BoundFieldOperand f => f.Place.Item.Pic!.Category,
            BoundNumericLiteral => PicCategory.Numeric,
            // A computed expression is numeric unless it is an intrinsic call with a string result category.
            BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory,
            BoundComputedOperand => PicCategory.Numeric,
            _ => null,   // figuratives (SR7 below) / errors
        };
        bool senderNonInteger = source switch
        {
            BoundFieldOperand f => f.Place.Item.Pic is { Category: PicCategory.Numeric } p && (p.IsFloat || p.Scale > 0),
            BoundNumericLiteral nl => nl.Text.Contains('.'),
            _ => false,
        };
        bool senderIsAlphabetic = source is BoundFieldOperand fa && fa.Place.Item.Pic is { IsAlphabetic: true };
        bool senderIsAnEdited = source is BoundFieldOperand fe
            && fe.Place.Item.Pic is { Category: PicCategory.Alphanumeric, EditMask: not null };
        // §14.9.25.3 SR8: a fixed-width binary sender (BINARY-CHAR/-SHORT/-LONG/-DOUBLE) shall reference
        // only a numeric or numeric-edited receiver — SR10 (Table 16) applies only to cases NOT covered by
        // SR8, so this precedes the Table-16 arms. The family is 2002+ (absent from the '85 corpus), so
        // the check is corpus-safe at every receiver category.
        bool senderBinaryFamily = source is BoundFieldOperand fb
            && fb.Place.Item.Pic is { Usage: Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble };

        foreach (var t in targets)
        {
            if (MoveReceiverCategory(t) is not { } recvCat) continue;   // group receiver — GR4 exempt
            string where = $"MOVE … TO {t.Item.CobolName}";

            if (senderBinaryFamily && recvCat is not (PicCategory.Numeric or PicCategory.NumericEdited))
            {
                data.Edition.Error("COBOLNET0819", $"{where}: a BINARY-CHAR/-SHORT/-LONG/-DOUBLE sending "
                    + "operand shall reference only a numeric or numeric-edited receiver (ISO §14.9.25.3 SR8)");
                continue;
            }

            if (recvCat is PicCategory.Boolean)
            {
                // SR7: SPACE/QUOTE/HIGH-VALUE/LOW-VALUE (and ALL of non-boolean characters) never move to a
                // boolean item; ZERO is boolean zeros by context (§8.3.3.6.4 GR4). Table 16 Boolean column:
                // senders alphabetic / alphanumeric-edited / numeric / numeric-edited are "No".
                if (source is BoundFigurative { Kind: not 'Z' })
                    data.Edition.Error("COBOLNET0819", $"{where}: a figurative constant whose characters are "
                        + "not boolean characters shall not be moved to a boolean data item "
                        + "(ISO §14.9.25.3 SR7)");
                else if (source is BoundAllLiteral bal && (bal.Literal.Length == 0
                             || !bal.Literal.All(c => c is '0' or '1')))
                    data.Edition.Error("COBOLNET0819", $"{where}: ALL \"{bal.Literal}\" contains non-boolean "
                        + "characters and shall not be moved to a boolean data item (ISO §14.9.25.3 SR7)");
                else if (senderIsAlphabetic || senderIsAnEdited
                         || senderCat is PicCategory.Numeric or PicCategory.NumericEdited)
                    data.Edition.Error("COBOLNET0819", $"{where}: MOVE of an alphabetic, alphanumeric-edited, "
                        + "numeric, or numeric-edited operand to a boolean data item is invalid "
                        + "(ISO §14.9.25.3 SR10, Table 16)");
            }
            else if (recvCat is PicCategory.National)
            {
                // Table 16 National column: only a NON-integer numeric sender is "No".
                if (senderNonInteger)
                    data.Edition.Error("COBOLNET0819", $"{where}: MOVE of a non-integer numeric operand to a "
                        + "national data item is invalid (ISO §14.9.25.3 SR10, Table 16)");
            }
            else if (senderCat is PicCategory.National)
            {
                // Table 16 National row: alphabetic / alphanumeric / alphanumeric-edited receivers are "No"
                // (FUNCTION DISPLAY-OF §15.26 is the sanctioned national→alphanumeric narrowing — residue).
                if (recvCat is PicCategory.Alphanumeric)
                    data.Edition.Error("COBOLNET0819", $"{where}: MOVE of a national sending operand to an "
                        + "alphabetic, alphanumeric, or alphanumeric-edited receiver is invalid (ISO "
                        + "§14.9.25.3 SR10, Table 16; FUNCTION DISPLAY-OF is the sanctioned conversion)");
            }
            else if (senderCat is PicCategory.Boolean)
            {
                // Table 16 Boolean row: alphabetic / numeric / numeric-edited receivers are "No"
                // (boolean → plain alphanumeric is "Yes").
                if (t.Item.Pic is { IsAlphabetic: true }
                    || recvCat is PicCategory.Numeric or PicCategory.NumericEdited)
                    data.Edition.Error("COBOLNET0819", $"{where}: MOVE of a boolean sending operand to an "
                        + "alphabetic, numeric, or numeric-edited receiver is invalid "
                        + "(ISO §14.9.25.3 SR10, Table 16)");
            }
        }
    }

    /// <summary>
    /// Ref-mod STORE backing (the W2 adversarial-review fix, DEVLOG 595): a MOVE into a reference-modified
    /// slice of a numeric USAGE-DISPLAY item writes CHARACTERS into the item's character positions
    /// (§8.4.2.4 — the unique result is an elementary alphanumeric item). Without image backing the resolver
    /// wraps <c>NumericImagePlace(long)</c> and the spliced image ROUND-TRIPS through the <c>long</c> on
    /// store, silently losing any non-digit deposit (<c>MOVE SPACE TO N(1:2)</c> left N's digits — and the
    /// observable result flipped with whether UNRELATED code elsewhere image-backed the item). Mark the
    /// underlying item <c>StoreAsImage</c> at bind time for EVERY sender kind — digits round-trip either way,
    /// so the flag is safe, and the byte-semantics model (a ref-mod store is a character-cell write) is what
    /// §14.6.8's fixed-width transfer implies. Same substrate + eligibility as the figurative pass; a Tier-B
    /// window already writes character images; a REDEFINES-class member keeps its (already-run) tier
    /// classification — the emitter's narrow loud guard covers that residue.
    /// </summary>
    private static void MarkRefModStoreImage(IReadOnlyList<Place> targets)
    {
        foreach (var t in targets)
            if (t is RefModPlace rm
                && rm.Item is { Class: null, Pic: { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display } } item)
                item.StoreAsImage = true;
    }
}
