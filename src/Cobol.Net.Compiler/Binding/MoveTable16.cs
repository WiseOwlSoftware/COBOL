// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>One operand's position in ISO §14.9.25.3 Table 16 — the axes the table actually keys on, which are
/// finer than <see cref="PicCategory"/>. Alphabetic and alphanumeric-edited are separate ROWS/COLUMNS there but
/// share <see cref="PicCategory.Alphanumeric"/> here, and the numeric row splits on integer-vs-noninteger.</summary>
/// <param name="Category">The §8.5.2.1 category.</param>
/// <param name="IsAlphabetic">PIC A — Table 16's Alphabetic row/column.</param>
/// <param name="IsEdited">Carries an edit mask — the Alphanumeric-edited row (the COLUMN pairs it with plain
/// alphanumeric, which is why only the ROW reads this).</param>
/// <param name="IsNonInteger">A numeric operand with digits right of the decimal point — Table 16 splits the
/// numeric ROW into Integer and Noninteger and they differ in three columns.</param>
public readonly record struct Table16Operand(
    PicCategory Category, bool IsAlphabetic = false, bool IsEdited = false, bool IsNonInteger = false)
{
    /// <summary>The Table-16 position of a described elementary item.</summary>
    public static Table16Operand Of(DataItem item) =>
        item.Pic is not { } p
            ? new Table16Operand(PicCategory.Group)
            : new Table16Operand(p.Category, p.IsAlphabetic, p.EditMask is not null,
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
/// table; <c>OoBinder.OoContentMismatch</c> fell back to §14.8.2.3.2 STRICT IDENTITY — the BY <b>REFERENCE</b>
/// rule — for boolean, national and numeric-edited formals. Identity is a much narrower test than Table 16, so
/// three pairings the standard admits were refused: boolean→national, alphanumeric→boolean and
/// national→boolean, each reported as a "category mismatch" naming a rule that does not govern the crossing.
/// </para>
/// <para>
/// ⚠ WHAT STAYS WITH THE CALLER: the SOURCE-SHAPE rules that are not about categories at all — §14.9.25.3 SR7
/// (a figurative constant whose characters are not boolean characters, and the ALL-literal form of the same),
/// and SR8 (the fixed-width binary family). Those key on the bound operand's SHAPE, not on a Table-16 position,
/// and MoveBinder keeps them.
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
}
