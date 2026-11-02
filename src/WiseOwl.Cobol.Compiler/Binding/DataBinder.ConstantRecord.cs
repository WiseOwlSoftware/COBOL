// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// What a CONSTANT RECORD's SUBTREE may not contain (kb/Work PB1262, PB487): §13.16.3 SR13 (the excluded clauses),
/// §13.18.40.3 SR32 (no format 2 PICTURE) and §13.18.38.3 SR19 / SR23 / SR33 (the OCCURS formats). Every one is a rule
/// about an item's POSITION — "any data item subordinate to" a CONSTANT RECORD — so it is a property of the COMPOSED
/// entry, and the pass walks <see cref="CompositionForest"/> after <c>ExpandTypes</c>. The screens used to run at
/// entry-attach time (<c>BindEntries</c>), which sees only the entries the programmer WROTE: an item a
/// <c>TYPE</c> or <c>SAME AS</c> reference brings into the record was never asked, so
/// <c>01 T TYPEDEF. 05 X PIC 9 BLANK WHEN ZERO. 01 C CONSTANT RECORD. 05 Y TYPE T.</c> compiled clean
/// (the two-arm dispatch, feedback_two_arm_dispatch). The same-ENTRY half of SR13 stays in <c>BindEntry</c>, where the
/// written clause set exists.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The subordinate screens of a CONSTANT RECORD, ONE walk. The record item itself is asked only SR33
    /// ("described with the CONSTANT RECORD clause or any data item subordinate to" one); every rule that says
    /// "subordinate" is asked of the descendants.</summary>
    internal void CheckConstantRecordSubtrees()
    {
        foreach (var item in CompositionForest())
        {
            if (!IsConstantRecordItem(item)) continue;
            bool subordinate = !item.IsConstantRecord;
            using var _ = Edition.At(item);
            string name = item.CobolName ?? "FILLER";

            // §13.16.3 SR13 ¶1 (the subordinate half). The excluded clauses are the written-clause set
            // DataClauseKinds.ConstantRecordExcluded; the item carries a flag for each one it can hold — select-when and
            // the validation clauses are declined facilities, refused where they are written, and never reach an item.
            if (subordinate && ExcludedClausesOf(item) is var excluded && excluded != DataClauseKind.None)
                Edition.Error(DiagnosticCatalog.ConstantRecordRule,
                    $"'{name}': the ANY LENGTH, BASED, BLANK WHEN ZERO, DYNAMIC LENGTH, select-when, SYNCHRONIZED, and "
                    + "TYPEDEF clauses and validation-clauses shall not be specified in any data description entry "
                    + "subordinate to a data description entry with the CONSTANT RECORD clause (ISO §13.16.3 SR13); "
                    + $"this entry specifies {DataClauseKinds.Name(excluded)}");

            // §13.18.40.3 SR32: a format 2 PICTURE clause shall not be specified in any data item subordinate to a
            // data item described with the CONSTANT RECORD clause.
            if (subordinate && item.Pic is { LocaleEdit: not null })
                Edition.Error(DiagnosticCatalog.PictureLocaleFormat2Violation,
                    $"'{name}': a format 2 PICTURE clause shall not be specified in any data item subordinate to a data "
                    + "item described with the CONSTANT RECORD clause (ISO §13.18.40.3 SR32)");

            // §13.18.38.3: SR33 bars the dynamic-capacity-table format (Format 4) on the record's items INCLUDING the
            // CONSTANT RECORD item; SR19 bars a Format 2 table, and SR23 the DEPENDING phrase of ANY format (the
            // report-writer Format 3 spelling the data division admits), in a SUBORDINATE item. A fixed Format 1
            // table stays legal.
            if (item.OccursSpec is not { } occurs) continue;
            if (occurs.IsDynamic)
                Edition.Error(DiagnosticCatalog.ConstantRecordRule,
                    $"'{name}': the dynamic-capacity-table format of the OCCURS clause shall not be specified in any data "
                    + "item described with the CONSTANT RECORD clause or any data item subordinate to one "
                    + "(ISO §13.18.38.3 SR33)");
            else if (subordinate && occurs.DependingName is not null)
                Edition.Error(DiagnosticCatalog.ConstantRecordRule,
                    $"'{name}': a format 2 OCCURS clause, and the DEPENDING phrase of any OCCURS format, shall not be "
                    + "specified in any data item subordinate to a data item described with the CONSTANT RECORD clause "
                    + "(ISO §13.18.38.3 SR19, SR23)");
        }
    }

    /// <summary>The <see cref="DataClauseKinds.ConstantRecordExcluded"/> clauses this composed item carries.</summary>
    private static DataClauseKind ExcludedClausesOf(DataItem item)
    {
        var found = DataClauseKind.None;
        if (item.IsAnyLength) found |= DataClauseKind.AnyLength;
        if (item.IsBased) found |= DataClauseKind.Based;
        if (item.BlankWhenZero) found |= DataClauseKind.BlankWhenZero;
        if (item.IsDynamicLength) found |= DataClauseKind.DynamicLength;
        if (item.Synchronized is not null) found |= DataClauseKind.Synchronized;
        if (item.IsTypedef) found |= DataClauseKind.Typedef;
        return found & DataClauseKinds.ConstantRecordExcluded;
    }

    /// <summary>Whether <paramref name="item"/> is, or is subordinate to, a CONSTANT RECORD (ISO §13.18.15.3
    /// SR2 — "neither the data item described by the subject of the entry nor any data item subordinate to
    /// [it] shall be specified as a receiving data item"). The flag lives on the level-01 root; this walk
    /// covers the subtree.</summary>
    internal bool IsConstantRecordItem(DataItem item)
    {
        for (DataItem? n = item; n is not null; n = n.Parent)
            if (n.IsConstantRecord) return true;
        return false;
    }

    /// <summary>The §13.18.15.3 SR2 receiving-operand prohibition for CONSTANT RECORD content — "Neither the data
    /// item described by the subject of the entry nor any data item subordinate to the subject of the entry shall be
    /// specified as a receiving data item". The <c>COBOL1548</c> message when the place is (or is subordinate to)
    /// a structured constant, else null. It REPORTS NOTHING: the receiving chokepoint reports it, and §8.4.3.2.4
    /// GR5's argument-manner test only asks it (kb/Work PB1418).
    /// <para>⛔ IT HAS EXACTLY ONE CALL SITE, AND THAT IS THE DESIGN: <c>ExpressionBinder.ReceivingPlaceBar</c>, the
    /// place half of the ONE receiving chokepoint's prohibition table. A verb reaches this rule by RESOLVING ITS
    /// RECEIVER through <c>ResolveReceiving</c>, never by calling here.
    /// The remark that used to stand here claimed it was "shared by every receiving chokepoint that resolves its
    /// own Place (ACCEPT / INITIALIZE / INSPECT targets / MOVE CORRESPONDING receivers)" — a sentence that
    /// described an intention, not the code: there was no second caller, and INITIALIZE silently destroyed a
    /// structured constant at run time for as long as it resolved identifier-1 with the plain resolver (kb/Work
    /// PB416, which routed it through the chokepoint). A verb whose receiver bypasses <c>ResolveReceiving</c>
    /// bypasses this rule AND the five others that live beside it, so the repair is always the routing.</para></summary>
    internal string? ConstantStoreProhibition(Place place, string what)
    {
        if (!IsConstantRecordItem(place.Item)) return null;
        DataItem root = place.Item;
        while (root.Parent is { } p) root = p;
        return $"{what} names a data item of the CONSTANT RECORD '{root.CobolName ?? root.ClrName}' — the content "
            + "of a structured constant cannot be modified (ISO §13.18.15.3 SR2)";
    }
}
