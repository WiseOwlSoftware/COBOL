// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>One operand's position in ISO §14.9.25.3 Table 16 — the axes the table actually keys on, which are
/// finer than <see cref="PicCategory"/>. Alphabetic and alphanumeric-edited are separate ROWS/COLUMNS there but
/// share <see cref="PicCategory.Alphanumeric"/> here, and the numeric row splits on integer-vs-noninteger.</summary>
/// <param name="Category">The §8.5.2.1 category.</param>
/// <param name="IsAlphabetic">PIC A — Table 16's Alphabetic row/column.</param>
/// <param name="IsEdited">Carries an edit mask — the Alphanumeric-edited and National-edited ROWS, which Table 16
/// prints separately from their plain categories. Both COLUMNS pair the edited form with the plain one
/// ("Alphanumeric-edited, Alphanumeric" and "National, National-edited"), which is why only the ROWS read this.</param>
/// <param name="IsNonInteger">A numeric operand with digits right of the decimal point — Table 16 splits the
/// numeric ROW into Integer and Noninteger and they differ in three columns.</param>
public readonly record struct Table16Operand(
    PicCategory Category, bool IsAlphabetic = false, bool IsEdited = false, bool IsNonInteger = false)
{
    /// <summary>The Table-16 position of a described item — its OWN picture, or a bit / national group's as-if
    /// picture (§13.18.29.4 GR1b/GR2b; D20/PB79: a national group is Table 16's NATIONAL row, never the GR4 group
    /// exemption); an alphanumeric group is the GR4 conversion-free copy.</summary>
    public static Table16Operand Of(DataItem item) =>
        item.OperandPic is not { } p
            ? new Table16Operand(PicCategory.Group)
            : new Table16Operand(p.Category, p.IsAlphabetic, p.EditMask is not null || p.LocaleEdit is not null,
                p.Category is PicCategory.Numeric && (p.IsFloat || p.Scale > 0));

    /// <summary>The Table-16 position of a PLACE — the entry every MOVE/INVOKE crossing must use, because a
    /// REFERENCE-MODIFIED view carries only PART of the inner item's finer flags (kb/Work PB72 → PB73): §8.4.3.3.4
    /// GR6 gives the unique data item "the same class, category, and usage as that defined for identifier-1"
    /// EXCEPT the exhaustive lettered rewrites — alphanumeric-edited → alphanumeric, national-edited → national,
    /// numeric / numeric-edited → alphanumeric or national — so a view is NEVER edited and NEVER numeric (PB72:
    /// <c>MOVE AE-ITEM(1:2) TO a-boolean</c> is alphanumeric → boolean "Yes", measured refused before), but it IS
    /// still ALPHABETIC over a PIC A item and BOOLEAN over a boolean one (PB73, adjudicated 2026-08-18: GR2's
    /// "as if redefined … alphanumeric" governs the ref-mod OPERATION, not the result — the 85 lineage kept the
    /// two rules apart — so <c>MOVE A-ITEM(1:2) TO a-boolean</c> and <c>MOVE B4(1:1) TO PIC 9</c> are the "No"
    /// cells their unsliced twins are). The CATEGORY routes through the ONE GR6 reader
    /// (<see cref="RefModPlace.CategoryOf"/>) and the alphabetic rider reads the inner PICTURE; a ref-mod view
    /// over a GROUP is an ELEMENTARY alphanumeric item (GR6's lead sentence), never Group-exempt.</summary>
    public static Table16Operand Of(Place p) => p switch
    {
        RefModPlace rm => new Table16Operand(rm.Category, IsAlphabetic: rm.Inner.Item.Pic is { IsAlphabetic: true }),
        _ => Of(p.Item),
    };

    /// <summary>The <c>--permissive</c> reading of a position (kb/Work PB73): the leniencies GnuCOBOL and this
    /// compiler's earlier releases extended — a NUMERIC-typed function treated as the Integer row (its literal
    /// text moves to a character receiver) and a reference-modified view read as plain alphanumeric (an alphabetic
    /// slice loses its row). Never the strict axis; the caller warns when only this reading admits the move.</summary>
    public static Table16Operand Lenient(Table16Operand op, bool isFunction, bool isRefModView) =>
        op with
        {
            IsNonInteger = op.IsNonInteger && !isFunction,
            IsAlphabetic = op.IsAlphabetic && !isRefModView,
        };
}

/// <summary>
/// ⭐ ISO §14.9.25.3 <b>Table 16 — Validity of types of MOVE statements</b>, in ONE place.
/// </summary>
/// <remarks>
/// <para>
/// The table is not only MOVE's. §14.8.2.3.3 rule 2d makes it the conformance rule for a BY CONTENT / BY VALUE
/// argument whose formal is not numeric, not an index item and not ANY LENGTH — "the conformance rules are the
/// same as for a MOVE statement with the argument as the sending operand and the corresponding formal parameter
/// as the receiving operand". So the INVOKE argument screen asks Table 16 the same question MOVE does, and
/// asking it in two places is how the two answers drift.
/// </para>
/// <para>
/// ⛔ THEY HAD ALREADY DRIFTED, WHICH IS WHY THIS EXISTS (fix-queue PB53). <c>MoveBinder</c> implemented the
/// table; <c>OoConformance.ContentMismatch</c> (then <c>OoBinder.OoContentMismatch</c>) fell back to §14.8.2.3.2 STRICT IDENTITY — the BY <b>REFERENCE</b>
/// rule — for boolean, national and numeric-edited formals. Identity is a much narrower test than Table 16, so
/// three pairings the standard admits were refused: boolean→national, alphanumeric→boolean and
/// national→boolean, each reported as a "category mismatch" naming a rule that does not govern the crossing.
/// </para>
/// <para>
/// ⛔ AND THE SOURCE-SHAPE RULES MOVED IN, BECAUSE A SECOND STATEMENT ASKS THEM (kb/Work PB416). The remark that
/// stood here said §14.9.25.3 SR6/SR7/SR8 "key on the bound operand's SHAPE, not on a Table-16 position, and
/// MoveBinder keeps them" — true of the axis, false of the OWNERSHIP: §14.9.20.3 SR4 says an INITIALIZE REPLACING
/// pair is legal only if "a MOVE statement with identifier-2 or literal-1 as the sending item and an item of the
/// specified category as the receiving operand shall be valid", which is the WHOLE validity question, not the
/// table alone. So <see cref="ShapeRefusal(BoundOperand, Table16Operand)"/> answers the three
/// receiver-position-keyed source-shape rules and <see cref="SenderClassRefusal"/> answers SR1's sending half,
/// and MOVE and INITIALIZE ask the same code.
/// Each caller keeps only its own FRAMING (its diagnostic code and the statement text it quotes back).
/// </para>
/// <para>
/// ⛔ AND A THIRD STATEMENT ASKS THE WHOLE QUESTION, WHICH IS WHY SR9 IS HERE TOO AND WHY THERE IS A COMPOSITE
/// ENTRY (kb/Work PB391). §14.7.6 rule 2 — "In a MOVE statement, at least one of the data items is an elementary
/// data item and the resulting move is valid according to the rules for the MOVE statement" — makes MOVE / ADD /
/// SUBTRACT CORRESPONDING's pairing decision the MOVE statement's own validity question, over two DATA ITEMS and
/// no bound operands. <see cref="DataItemRefusal"/> is that question in SR order; <see cref="VariableLengthRefusal"/>
/// is SR9's relation moved out of <c>StatementValidation.CheckVariableLengthMove</c>, which keeps its COBOLNET1931
/// framing. Rule 2 asked only <see cref="Refusal"/>, so SR8 and SR9 — the two rules SR10 explicitly defers to —
/// were unasked under CORRESPONDING: a BINARY-LONG namesake paired with a PIC X(5) one, and a variable-length-group
/// namesake paired with an elementary one and reached the run time.
/// </para>
/// <para>
/// ⚠ WHAT STILL STAYS WITH THE CALLER: §14.9.25.3 SR5's figurative→numeric prohibition, whose three edition rows
/// (permitted through 2014, removed at 2023, and the surviving digit-only-ALL-to-integer exception) live in
/// <c>VersionConformancePass.GateMove</c> and are re-derived from a BOUND MOVE — an edition-gated policy, not a
/// flat refusal, so it is not expressible as a <see langword="string"/>? here.
/// </para>
/// </remarks>
public static class MoveTable16
{
    /// <summary>Why Table 16 refuses this sending→receiving pair, or <see langword="null"/> when it admits it.
    /// A GROUP on either side is exempt — §14.9.25.4 GR4 makes such a move an alphanumeric character copy with
    /// no conversion, which the table does not describe.</summary>
    public static string? Refusal(Table16Operand sender, Table16Operand receiver)
    {
        if (sender.Category is PicCategory.Group || receiver.Category is PicCategory.Group) return null;

        // ── Table 16, BOOLEAN column: alphabetic, alphanumeric-edited, numeric and numeric-edited are "No" ──
        if (receiver.Category is PicCategory.Boolean)
            return sender.IsAlphabetic || sender.IsEdited
                   || sender.Category is PicCategory.Numeric or PicCategory.NumericEdited
                ? "an alphabetic, alphanumeric-edited, numeric or numeric-edited sending operand does not move "
                  + "to a boolean receiver (ISO §14.9.25.3 SR10, Table 16)"
                : null;

        // ── NATIONAL column: only a NONINTEGER numeric sender is "No" ──
        if (receiver.Category is PicCategory.National)
            return sender.IsNonInteger
                ? "a noninteger numeric sending operand does not move to a national receiver "
                  + "(ISO §14.9.25.3 SR10, Table 16)"
                : null;

        // ── NATIONAL-EDITED row: the ONLY "Yes" is the "National, National-edited" column — alphabetic,
        //    alphanumeric, alphanumeric-edited, boolean, numeric and numeric-edited receivers are all "No".
        //    It is a SEPARATE ROW from National (which is "Yes" into boolean and into the numeric column), and
        //    for the same reason the alphanumeric-edited row differs from alphanumeric: an edit mask has no
        //    de-editable value and no boolean characters. The receiving COLUMN, by contrast, PAIRS the two
        //    ("National, National-edited"), which is why only the ROW reads IsEdited — the column arm above is
        //    correct for a national-edited receiver as written. (kb/Work PB492.) ──
        if (sender is { Category: PicCategory.National, IsEdited: true })
            return "a national-edited sending operand moves only to a national or national-edited receiver "
                 + "(ISO §14.9.25.3 SR10, Table 16)";

        // ── NATIONAL row: alphabetic / alphanumeric / alphanumeric-edited receivers are "No" ──
        if (sender.Category is PicCategory.National)
            return receiver.Category is PicCategory.Alphanumeric
                ? "a national sending operand does not move to an alphabetic, alphanumeric or "
                  + "alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16; FUNCTION DISPLAY-OF is the "
                  + "sanctioned conversion)"
                : null;

        // ── BOOLEAN row: alphabetic / numeric / numeric-edited receivers are "No" (plain alphanumeric is Yes) ──
        if (sender.Category is PicCategory.Boolean)
            return receiver.IsAlphabetic
                   || receiver.Category is PicCategory.Numeric or PicCategory.NumericEdited
                ? "a boolean sending operand does not move to an alphabetic, numeric or numeric-edited receiver "
                  + "(ISO §14.9.25.3 SR10, Table 16)"
                : null;

        // ⭐ THE ALPHABETIC / EDITED / NONINTEGER AXES, COMPLETED (fix-queue PB72 — the arms below were absent
        // and every one of their "No" cells was a MEASURED silent acceptance; the table is read AS PRINTED at
        // specs/ISO_COBOL.md:25263, and with these four arms every cell over the modeled categories is decided
        // here). The classic '85 rows carry the same "No" cells, so all four arms are version-invariant.

        // ── ALPHABETIC column: a numeric or numeric-edited sender is "No" (`MOVE 5 TO a-pic-a` stored "5   ").
        //    Boolean and national senders are refused by their ROW arms above; the alphanumeric family is Yes. ──
        if (receiver.IsAlphabetic && sender.Category is PicCategory.Numeric or PicCategory.NumericEdited)
            return "a numeric or numeric-edited sending operand does not move to an alphabetic receiver "
                 + "(ISO §14.9.25.3 SR10, Table 16)";

        // ── ALPHABETIC row: numeric and numeric-edited receivers are "No" (a PIC A sender into PIC 9 stored
        //    zeros); boolean and national columns are covered above, the alphanumeric family is Yes. ──
        if (sender.IsAlphabetic && receiver.Category is PicCategory.Numeric or PicCategory.NumericEdited)
            return "an alphabetic sending operand does not move to a numeric or numeric-edited receiver "
                 + "(ISO §14.9.25.3 SR10, Table 16)";

        // ── ALPHANUMERIC-EDITED row: numeric and numeric-edited receivers are "No". The DE-EDITING move is
        //    the NUMERIC-edited row's (numeric-edited → numeric is Yes) — an ALPHANUMERIC edit mask has no
        //    de-editable value, which is exactly why the two rows differ. ⛔ The category guard is load-bearing:
        //    IsEdited is set for a NUMERIC-edited item too (Of reads the one EditMask field), and an unguarded
        //    arm refused the de-editing move — caught by the corpus (move_numeric_edited_source), not by reading. ──
        if (sender is { Category: PicCategory.Alphanumeric, IsEdited: true }
            && receiver.Category is PicCategory.Numeric or PicCategory.NumericEdited)
            return "an alphanumeric-edited sending operand does not move to a numeric or numeric-edited "
                 + "receiver (ISO §14.9.25.3 SR10, Table 16)";

        // ── NUMERIC row, Noninteger: alphabetic / alphanumeric / alphanumeric-edited receivers are "No"
        //    (`MOVE 5.5 TO a-pic-x` printed "5.5"); the INTEGER row's Yes is the classic digit-image move.
        //    The alphabetic receiver is already refused by the column arm above; this closes the plain and
        //    edited alphanumeric cells. ──
        if (sender is { Category: PicCategory.Numeric, IsNonInteger: true }
            && receiver.Category is PicCategory.Alphanumeric)
            return "a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or "
                 + "alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16)";

        return null;
    }

    /// <summary>The SENDER's position in the §14.9.25.3 Table 16 category matrix (fix-queue PB72: built in ONE
    /// place, and a FIELD builds through <c>Table16Operand.Of(Place)</c> so a ref-mod view takes §8.4.3.3.4
    /// GR2/GR6's rewrites — category via the one GR6 reader, and the finer alphabetic/edited/noninteger flags
    /// erased, because the unique data item a view creates is plain class-and-category alphanumeric). An
    /// INTRINSIC sender reports the §15.18.4 r3 ALPHABETIC rider alongside its result category — the finer row
    /// Table 16 keys on and <c>PicCategory</c> deliberately cannot express (the PIC A fold).
    /// <para>It is a NAMED reader rather than an inline switch because the FROM-phrase function-class rules
    /// (§14.9.32.3 SR2 · §14.9.51.3 SR4 · §14.9.35.3 SR9) ask the same question of the same operand, and a
    /// second reading of "what category is this sender" is exactly how a folded <c>FUNCTION LENGTH</c> — which
    /// reaches the binder as a plain numeric literal — would have been read as "unknown" and waved through
    /// (kb/Work PB348). It sits HERE rather than in <c>MoveBinder</c> because §14.9.20.3 SR4 makes INITIALIZE
    /// REPLACING's identifier-2 / literal-1 the sending item of the same hypothetical MOVE (kb/Work PB416).</para></summary>
    public static Table16Operand SenderPosition(BoundOperand source) => source switch
    {
        BoundStringLiteral sl => new Table16Operand(sl.Category),
        BoundAllLiteral al => new Table16Operand(al.Category),
        BoundFieldOperand f when f.Place is not RefModPlace && f.Place.Item.OperandPic is null =>
            new Table16Operand(PicCategory.Group),   // GR4 — an ALPHANUMERIC group moves without conversion (D20)
        BoundFieldOperand f => Table16Operand.Of(f.Place),
        BoundNumericLiteral nl => new Table16Operand(PicCategory.Numeric, IsNonInteger: nl.Text.Contains('.')),
        // An INTRINSIC sender's Table-16 row is its §15.2 TYPE (kb/Work PB73, adjudicated 2026-08-18): an
        // INTEGER function ("no digits to the right of the decimal point", §15.2 item 5 — resolved per call by
        // the ONE IntrinsicResultType reader, so MAX over integers is integer) is the Integer row; a NUMERIC
        // function (item 4) is the NONINTEGER row whatever a particular reference's value — §8.4.3.2.3 SR11's
        // principle for the integer-operand positions applies to the table's split too. The former admission
        // (IsNonInteger: false for every function) survives under --permissive as a warning, at the caller.
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } =>
            new Table16Operand(ic.ResultCategory, ic.ResultIsAlphabetic,
                IsNonInteger: ic.ResultCategory is PicCategory.Numeric && !IntrinsicResultType.IsIntegerOperand(source)),
        BoundComputedOperand => new Table16Operand(PicCategory.Numeric),
        _ => new Table16Operand(PicCategory.Group),   // figuratives (ShapeRefusal's own arms) / errors — category-exempt
    };

    /// <summary>The SOURCE-SHAPE refusals of ISO §14.9.25.3 — the rules that are decided by the sending operand's
    /// SHAPE against the receiving operand's Table-16 POSITION rather than by the table's own cells: SR8 (the
    /// fixed-width binary family), SR7 (a figurative constant whose characters are not boolean characters, and
    /// the <c>ALL</c>-literal form of the same) and SR6 (the figurative constant ZERO into an alphabetic item).
    /// Null when none of them refuses; the caller then asks <see cref="Refusal"/> for the table itself.
    /// <para>⛔ THE ORDER IS THE CALLER'S FORMER CONTROL FLOW, PRESERVED: SR8 short-circuits (a binary-family
    /// sender into a non-numeric receiver is answered by SR8, and §14.9.25.3 SR10 applies only "for all other
    /// cases not described in Syntax rules 8 and 9"), and an SR7/SR6 refusal likewise pre-empts the table,
    /// whose figurative row is category-exempt by construction.</para>
    /// <para>⛔ SR6 IS NEW HERE AND WAS UNIMPLEMENTED ON BOTH PATHS (kb/Work PB416): <c>MOVE ZERO TO a-PIC-A</c>
    /// stored <c>0000</c> and <c>INITIALIZE g REPLACING ALPHABETIC DATA BY ZERO</c> stored the same, because a
    /// figurative's Table-16 position is <c>PicCategory.Group</c> — deliberately exempt, since §8.3.3.6.4 GR4
    /// gives ZERO no fixed category — so the table can never be the rule that refuses it. SR6 is written
    /// precisely to cover what the table cannot.</para>
    /// <para>⛔ THE ITEM-KEYED HALF IS THE <see cref="ShapeRefusal(DataItem, Table16Operand)"/> OVERLOAD AND THIS
    /// ENTRY DELEGATES TO IT (kb/Work PB391). Every rule here whose antecedent reads "if identifier-1 references
    /// a DATA ITEM described with …" needs nothing but the sending ITEM, and §14.7.6 rule 2's CORRESPONDING
    /// filter has only an item — no bound operand exists for a pair until after the pairing decision is made.
    /// Writing SR8's test a second time over there is precisely the duplication kb/Work PB391 deleted, so the
    /// two entries are ONE body: the item-keyed rules live in the overload, this one adds only the arms a data
    /// item can never satisfy (a figurative constant is not a data item), and the NEXT item-keyed rule reaches
    /// both askers without an edit.</para></summary>
    public static string? ShapeRefusal(BoundOperand sender, Table16Operand receiver)
    {
        // SR8 and every other rule keyed on the sending DATA ITEM — asked through the one item-keyed entry.
        if (sender is BoundFieldOperand f && ShapeRefusal(f.Place.Item, receiver) is { } itemKeyed)
            return itemKeyed;

        // §14.9.25.3 SR7 — "Any figurative constant for which the associated character or characters are not
        // boolean characters shall not be moved to a boolean data item." ZERO is boolean zeros by context
        // (§8.3.3.6.4 GR4), which is why it is the one exempt Kind.
        if (receiver.Category is PicCategory.Boolean)
        {
            if (sender is BoundFigurative { Kind: not 'Z' })
                return "a figurative constant whose characters are not boolean characters shall not be moved "
                     + "to a boolean data item (ISO §14.9.25.3 SR7)";
            if (sender is BoundAllLiteral bal && (bal.Literal.Length == 0 || !bal.Literal.All(c => c is '0' or '1')))
                return $"ALL \"{bal.Literal}\" contains non-boolean characters and shall not be moved to a "
                     + "boolean data item (ISO §14.9.25.3 SR7)";
        }

        // §14.9.25.3 SR6 — "The figurative constant ZERO shall not be moved to an alphabetic data item."
        // ⚠ `ALL ZERO` IS the figurative constant ZERO and IS refused: §8.3.3.6.2 Format 1 prints ALL as an
        // OPTIONAL (un-underlined) word of the zero format, so it binds to the same 'Z' kind. What SR6 does NOT
        // reach is `ALL "0"` — Format 6's ALL-literal, a different figurative constant, which Table 16 places in
        // the Alphanumeric row that the Alphabetic column admits.
        // ⚠ A REFERENCE-MODIFIED PIC A RECEIVER IS ALPHABETIC HERE AND IS REFUSED ON BOTH AXES, INCLUDING
        // --permissive. Table16Operand.Of(Place) keeps the alphabetic rider over a ref-mod view (kb/Work PB73's
        // adjudication of §8.4.3.3.4 GR6), and the caller's --permissive lever is defined against TABLE 16's
        // reading of such a view, not against SR6 — which the standard states unconditionally, with no reference
        // to the table. Asked and answered rather than left to be rediscovered.
        if (sender is BoundFigurative { Kind: 'Z' } && receiver.IsAlphabetic)
            return "the figurative constant ZERO shall not be moved to an alphabetic data item "
                 + "(ISO §14.9.25.3 SR6)";

        return null;
    }

    /// <summary>The ITEM-KEYED source-shape refusals of ISO §14.9.25.3 — the rules whose antecedent is
    /// <i>"if identifier-1 references a data item described with …"</i>, so the sending DATA ITEM is the whole
    /// of their input. Today that is SR8 alone: <i>"If identifier-1 references a data item described with usage
    /// binary-char, binary-short, binary-long, or binary-double, identifier-2 shall reference a numeric or
    /// numeric-edited item."</i> §14.9.25.3 SR10 defers to it explicitly — <i>"for all other cases not described
    /// in Syntax rules 8 and 9"</i> — so it is asked BEFORE <see cref="Refusal"/>, never instead of it.
    /// <para>⛔ THIS IS THE HOME, AND THE <see cref="ShapeRefusal(BoundOperand, Table16Operand)"/> ENTRY CALLS IT
    /// (kb/Work PB391). Two askers reach it: <c>MoveBinder</c> / <c>InitializeBinder</c> through the bound-operand
    /// entry, and <c>CorrespondingBinder</c>'s §14.7.6 rule-2 filter through <see cref="DataItemRefusal"/>, which
    /// has data items and no bound operands at all. A new item-keyed rule goes HERE and both get it; putting one
    /// in the bound-operand entry instead is the two-arm split that made <c>MOVE CORRESPONDING</c> pair a
    /// <c>BINARY-LONG</c> sender with a <c>PIC X(5)</c> receiver while the written MOVE of the same two items was
    /// refused COBOLNET0819.</para>
    /// <para>⚠ A GROUP RECEIVER IS REFUSED, and that is the rule's letter: SR8 requires identifier-2 to
    /// reference "a numeric or numeric-edited item", and a group item is neither. The §14.9.25.4 GR4 group
    /// exemption is Table 16's, not SR8's — SR10 is the rule that routes to the table, and SR10 does not reach
    /// a case SR8 describes.</para></summary>
    public static string? ShapeRefusal(DataItem sender, Table16Operand receiver) =>
        sender.Pic is { Usage: Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble }
        && receiver.Category is not (PicCategory.Numeric or PicCategory.NumericEdited)
            ? "a BINARY-CHAR/-SHORT/-LONG/-DOUBLE sending operand shall reference only a numeric or "
              + "numeric-edited receiver (ISO §14.9.25.3 SR8)"
            : null;

    /// <summary>ISO §14.9.25.3 SR9 — <i>"If identifier-1 or identifier-2 references a variable-length group then
    /// these groups shall be compatible groups as specified in 8.5.1.12, Variable-length groups"</i> — as the
    /// REASON the move is invalid, or <see langword="null"/> when the rule is satisfied or not engaged. The
    /// relation itself is the ONE <see cref="VariableLengthCompatibility"/> module; this is the MOVE statement's
    /// application of it, and SR10 defers to it exactly as it defers to SR8.
    /// <para>A <see langword="null"/> operand means <i>"not a plain data item"</i> — a literal, a function
    /// result, a reference-modified operand (§8.4.3.3.4 GR6 makes it an ELEMENTARY alphanumeric item) or a
    /// level-66 RENAMES alias (§13.18.45 composes ONE elementary item). §8.5.1.12.1 states the prohibition in
    /// terms of the OTHER OPERAND — <i>"a variable-length group … may not undergo a comparison or a move
    /// operation, in either direction, explicitly or otherwise, unless the other operand is a compatible
    /// group"</i> — so such an operand is a violation, not a fall-through.</para>
    /// <para>⛔ IT IS A READER RATHER THAN A CHECK because its two askers frame it differently (kb/Work PB391),
    /// the same shape <see cref="SenderClassRefusal"/> already has: <c>StatementValidation</c>
    /// <c>.CheckVariableLengthMove</c> reports it as COBOLNET1931 about the MOVE the programmer wrote, and
    /// <see cref="DataItemRefusal"/> reads it SILENTLY, because §14.7.6 rule 2 makes an invalid move a pair that
    /// does not correspond rather than a diagnostic. Before this, CORRESPONDING did not ask it at all and a
    /// variable-length-group namesake paired with an elementary one, reaching the run time as a
    /// <c>NotImplementedCobolFeatureException</c> for the whole-group image the pair can never have.</para>
    /// <para>No edition gate is needed and none is written: a variable-length group can only be DECLARED from
    /// COBOL-2014 (the DYNAMIC LENGTH clause §13.18.19 and OCCURS Format 4 §13.18.38), so the rule is
    /// unreachable below 2014 by construction rather than by a predicate that could drift.</para></summary>
    public static string? VariableLengthRefusal(DataItem? sender, DataItem? receiver)
    {
        bool engaged = (receiver is not null && VariableLengthCompatibility.IsVariableLength(receiver))
                    || (sender is not null && VariableLengthCompatibility.IsVariableLength(sender));
        return !engaged ? null
            : sender is null || receiver is null
                ? $"the {(receiver is null ? "receiving" : "sending")} operand is not a group item: a "
                  + "variable-length group may move only to or from a compatible GROUP (ISO §8.5.1.12.1)"
                : VariableLengthCompatibility.Mismatch(sender, receiver);
    }

    /// <summary>
    /// ⭐ <b>THE WHOLE OF ISO §14.9.25.3's VALIDITY QUESTION FOR A DATA-ITEM SENDER AND A DATA-ITEM RECEIVER</b>,
    /// in SR order — the form §14.7.6 rule 2 asks. Null when the move is valid.
    /// <para>Rule 2 reads <i>"In a MOVE statement, at least one of the data items is an elementary data item and
    /// the resulting move is valid according to the rules for the MOVE statement"</i>, so the CORRESPONDING
    /// pairing decision IS this question, and asking it as three or four separate calls at the call site is how
    /// the next rule gets added to one asker and not the other. One entry, asked once (kb/Work PB391).</para>
    /// <para>⚠ SR1's class half (<see cref="SenderClassRefusal"/>) is deliberately NOT in the chain. Its only
    /// other asker excludes those operands EARLIER and more broadly: §14.7.6 rule 4 — <i>"Neither data item …
    /// is of class index, message-tag, object, or pointer"</i> — drops such a child on BOTH sides before any
    /// pair is formed (<c>CorrespondingBinder.CorrEligible</c>), where SR1 as written reaches only the sender.
    /// Adding it here would put two mechanisms on one question, which is what this class exists to prevent.</para>
    /// <para>⚠ AND NEITHER IS SR5's figurative→numeric prohibition, for the reason the class header gives: its
    /// three edition rows live in <c>VersionConformancePass.GateMove</c>, and a figurative constant is not a data
    /// item, so no operand this entry can be given could reach it.</para>
    /// </summary>
    public static string? DataItemRefusal(DataItem sender, DataItem receiver)
    {
        Table16Operand recv = Table16Operand.Of(receiver);
        return ShapeRefusal(sender, recv)                                  // SR8 — SR10 defers to it
            ?? VariableLengthRefusal(sender, receiver)                     // SR9 — SR10 defers to it
            ?? Refusal(Table16Operand.Of(sender), recv);                   // SR10 — "all other cases": Table 16
    }

    /// <summary>ISO §14.9.25.3 SR1's SENDING half — "The class of identifier-1 or identifier-2 shall not be
    /// index, message-tag, object, or pointer" — as the refusal text, or null when the sender's class is
    /// admitted.
    /// <para>⛔ IT IS A READER RATHER THAN A CHECK because its two askers frame it differently: <c>MoveBinder</c>
    /// reports it as COBOLNET0809 about the MOVE the programmer wrote, and <c>InitializeBinder</c> reports it as
    /// §14.9.20.3 SR4 about a MOVE that exists only in the rule (kb/Work PB416). One rule, one classifier
    /// (<c>IntrinsicArgumentRules.ClassOf</c> — the ONE §8.5.2.1 Table-2 reader), two framings.</para>
    /// <para>⛔ THE CLASS COMES FROM <c>IntrinsicArgumentRules.ClassOf</c>, NEVER FROM A RE-DERIVED USAGE TEST.
    /// The two arms this replaced each pattern-matched ONE operand shape — a field whose usage is index, and an
    /// intrinsic call whose §15.2 item 6 type is index — so the THIRD shape, an index-NAME operand
    /// (<c>BoundIndexRef</c>, which the same classifier has reported as class index since kb/Work R27), was
    /// class index to every other screen in the compiler and numeric to this one.</para></summary>
    public static string? SenderClassRefusal(BoundOperand sender) =>
        Sr1ClassRefusal(IntrinsicArgumentRules.ClassOf(sender),
            sender is BoundComputedOperand { Expr: BoundIntrinsicCall ic }
                ? $"§15.2 item 6 — FUNCTION {ic.Sig.Name} over index arguments is an INDEX function, of the "
                  + "class and category index"
                : null);

    /// <summary>ISO §14.9.25.3 SR1's RECEIVING half — the SAME sentence, which names "identifier-1 OR
    /// identifier-2" and therefore reaches a receiving operand exactly as it reaches a sending one.
    /// <para>⛔ THIS ARM EXISTS BECAUSE THE OTHER ONE WAS THE ONLY ONE THAT WAS EVER FIXED (kb/Work PB423 —
    /// <c>feedback_two_arm_dispatch</c>, this repository's most reproducible defect shape). <c>MoveBinder</c>'s
    /// receiver loop asked <c>t.Item.Pic is { Usage: Usage.Index }</c> — one USAGE, hand-written, beside a
    /// sending arm that had already been routed through the class table — so <c>MOVE NULL TO P</c> over a
    /// <c>USAGE POINTER</c> item reached ROSLYN and surfaced <c>CS0029: Cannot implicitly convert type 'string'
    /// to 'CobolNet.Runtime.ManagedPointer'</c>, a message about generated C#, in place of a COBOL diagnostic.
    /// Both arms now read one core, so the answer cannot differ by position.</para>
    /// <para>A reference-modified receiver is classified through <c>IntrinsicArgumentRules.ClassOfPlace</c> like
    /// any other place: §8.4.3.3.4 GR6's unique data item has the class its rules give it, and §8.4.3.3.3 SR5
    /// admits reference modification only over class alphanumeric, boolean or national anyway, so no excluded
    /// class can arrive wearing a modifier.</para></summary>
    public static string? ReceiverClassRefusal(Place receiver) =>
        Sr1ClassRefusal(IntrinsicArgumentRules.ClassOfPlace(receiver), null);

    /// <summary>
    /// ⭐ ISO §14.9.25.3 SR1 ITSELF — <i>"The class of identifier-1 or identifier-2 shall not be index,
    /// message-tag, object, or pointer"</i> — asked of ONE operand's class, whichever position it occupies.
    /// <para>⛔ THE EXCLUDED SET IS THE RULE'S WHOLE LIST, NOT THE ONE MEMBER THAT HAPPENED TO BE REACHABLE
    /// (kb/Work PB423). This test was <c>is not CobolClass.Index</c>, excused by a comment asserting that
    /// "message-tag/object/pointer classes cannot reach a bound MOVE yet (their usages are compile-gated
    /// skeletons)". <c>PicInfo</c> contradicted that premise in its own XML docs — <c>Usage.Pointer</c> "LIVE
    /// (Phase-4b increment 1)", <c>Usage.ProgramPointer</c> "LIVE (P10 Step 7)", <c>Usage.ObjectReference</c>
    /// "LIVE (the Phase-3 OO spine)" — and <c>conformance:2002/based_pointer</c> exercises a live USAGE POINTER
    /// end to end. MEASURED at <c>--std 2023</c> and <c>--std 2002</c>: <c>MOVE P TO Y</c> (pointer into
    /// <c>PIC X(8)</c>) printed <c>Y=[CobolNet]</c> — the CLR type name of the pointer carrier, deposited in a
    /// user's alphanumeric item with no diagnostic anywhere. A premise about what "cannot reach" a screen is
    /// self-invalidating: it is written while a feature is staged and never revisited when the feature lands,
    /// so the rule is written out in full instead.</para>
    /// <para>⚠ MESSAGE-TAG has no <see cref="CobolClass"/> member and needs none: the MCS facility is unmodeled
    /// (docs/CONFORMANCE.md §4) and <c>PictureAnalyzer.ParseUsage</c> refuses the usage by name (COBOLNET1943),
    /// so no operand of that class can be bound. <c>MoveOperandClassDriftTests</c> asserts that every usage
    /// §13.18.60.3 SR4 names — which is exactly the six producing these four classes — is refused here, so the
    /// moment MESSAGE-TAG or any other gains a bound model the test fails rather than the screen silently
    /// narrowing.</para>
    /// <para>Class POINTER spans THREE categories — data-pointer, function-pointer and program-pointer
    /// (§8.5.2.1 Table 2) — which is why the question is asked of the CLASS: a usage test would have missed
    /// FUNCTION-POINTER exactly as the old one missed the others.</para>
    /// </summary>
    /// <param name="cls">The operand's §8.5.2.1 Table-2 class, or null when it is not statically decidable (a
    /// figurative constant whose class the context chooses) — which fails OPEN, as every class screen here does.</param>
    /// <param name="why">An operand-shape-specific reason to append, or null for the class's own.</param>
    private static string? Sr1ClassRefusal(CobolClass? cls, string? why) => cls switch
    {
        CobolClass.Index => "a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; "
            + (why ?? "§13.18.60.3 SR10 — only a SEARCH or SET statement, a relation condition, an "
                    + "intrinsic-function or inline-method argument, or a procedure-division / CALL / INVOKE "
                    + "USING phrase may reference an index data item") + ")",
        CobolClass.Pointer => "a MOVE operand shall not be of class pointer — §8.5.2.1 Table 2 puts the "
            + "data-pointer, function-pointer and program-pointer categories in that class (ISO §14.9.25.3 SR1; "
            + "§13.18.60.3 SR9 — a data-pointer data item \"may be referenced explicitly only in a CALL "
            + "statement, an INITIALIZE statement, an INVOKE statement, a SET statement, a relation condition, "
            + "a procedure division header, the argument list of an inline invocation of a method, as an "
            + "argument in a function-identifier, in the RETURNING phrase of an ALLOCATE statement, or in a "
            + "FREE statement\", and SR8 says the same of a program-pointer). Use SET to assign a pointer",
        CobolClass.Object => "a MOVE operand shall not be of class object — §8.5.2.1 Table 2 gives usage "
            + "OBJECT REFERENCE the object-reference category of class object (ISO §14.9.25.3 SR1). Use SET to "
            + "assign an object reference",
        _ => null,
    };
}
