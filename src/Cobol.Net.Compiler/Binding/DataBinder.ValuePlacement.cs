// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>⛔ THE §13.18.63.3 VALUE-CLAUSE PLACEMENT SCREEN — the rules that ask not what a VALUE clause's literal
/// is, but WHERE in the record the clause may be written, for every format that has such a rule, in one place
/// (kb/Work PB550 + PB551).
///
/// <para><b>The rules.</b> §13.18.63.3 SR12 (format 1): "The VALUE clause shall not be specified in a data
/// description entry that contains a REDEFINES clause or in an entry that is subordinate to an entry containing a
/// REDEFINES clause." SR16 carries it, unqualified, into format 2: "Syntax rules 10, 11,12,13,14,and 15 above
/// apply." SR25 is the condition-name twin over a different ancestor: "A format 3, 4, or 5 VALUE clause shall not
/// be specified in any data description entry that contains the CONSTANT RECORD clause, or in any data description
/// entry subordinate to a data description entry that contains the CONSTANT RECORD clause." All three are the same
/// question — does the entry, or an entry it is subordinate to, carry clause K — asked of the one ancestor chain.</para>
///
/// <para><b>Which formats reach which rule, and why the other legs are empty.</b> SR12 governs formats 1 and 2 only
/// (SR24 imports SRs 10 and 17 into format 3, never SR12), so a level-88 under a REDEFINES entry is legal and is
/// NOT screened here. SR25's format-4 leg is VACUOUS under today's grammar: a report group description entry admits
/// no CONSTANT RECORD clause (<c>reportGroupClause</c> has no such alternative), so the shape it forbids cannot be
/// written; its format-5 leg belongs to the VALIDATE facility, refused by name (COBOLNET1708, Annex A.4.14). The
/// FORMAT-1 VALUEs of a CONSTANT RECORD's own entries are the record's content (the CONSTANT RECORD clause,
/// §13.18.15) and stay legal — §13.18.63.3 SR25 names formats 3, 4 and 5 only.</para>
///
/// <para>⛔ <b>SR12 bars the redefinING entry, never the redefined one.</b> The anchor a REDEFINES clause names keeps
/// its VALUE (GroupImageCodec's SR12 note records the silent wrong answer that trap once produced), so the
/// predicate is the WRITTEN clause on the subject or an ancestor — <see cref="DataItem.RedefinesTargetName"/> —
/// never the storage fact <see cref="DataItem.RedefinesTarget"/>: the file section's implicit redefinitions
/// (§13.18.33.4 GR3) and a SAME RECORD AREA record (§12.4.6.4.4 GR2) write no clause and are not SR12's subject,
/// and a clause whose operand did not resolve is still a clause.</para>
///
/// <para><b>Why at bind, per written entry.</b> Both rules are about SOURCE ENTRIES, and the ancestor chain exists
/// the moment an entry is attached (<c>BindEntries</c> for formats 1/2, <c>BindCondition</c> for format 3) —
/// the same site the CONSTANT RECORD subordinate screens (§13.16.3 SR13, §13.18.40.3 SR32) use. A post-forest walk
/// would also see TYPE clones and report one written violation once per reference.</para>
///
/// <para><b>What it closes.</b> Before this screen both shapes compiled clean at every edition. A redefining
/// entry's VALUE was DISCARDED without a word — `05 B REDEFINES A PIC X(4) VALUE "ZZZZ".` displayed A's value,
/// because a redefines view never seeds the class's shared storage (only the canonical anchor does) — and a
/// condition-name under a CONSTANT RECORD was accepted and live.</para></summary>
public sealed partial class DataBinder
{
    /// <summary>§13.18.63.3 SR12/SR16 — screen a format 1 or format 2 VALUE clause on a just-attached data item.</summary>
    private void ScreenItemValuePlacement(DataItem item)
    {
        bool format2 = item.TableValues is not null;
        if (item.RawValue is null && !format2) return;
        if (EntryWithClause(item, static n => n.RedefinesTargetName is not null) is not { } redefining) return;
        string subject = $"data item '{item.CobolName ?? "FILLER"}'";
        Edition.Error(DiagnosticCatalog.ValueClausePlacement, $"{subject}: the VALUE clause shall not be specified "
            + (ReferenceEquals(redefining, item)
                ? "in a data description entry that contains a REDEFINES clause"
                : $"in an entry subordinate to '{redefining.CobolName ?? "FILLER"}', which contains a REDEFINES clause")
            + $" (ISO §13.18.63.3 SR12{(format2 ? ", applied to format 2 by SR16" : "")}) — the redefined entry "
            + $"'{redefining.RedefinesTargetName}' is the one that may carry an initial value");
    }

    /// <summary>§13.18.63.3 SR25 — screen a format 3 (condition-name) VALUE clause against its conditional
    /// variable's CONSTANT RECORD ancestry.</summary>
    private void ScreenConditionValuePlacement(string conditionName, DataItem conditionalVariable)
    {
        if (EntryWithClause(conditionalVariable, static n => n.IsConstantRecord) is not { } constantRecord) return;
        Edition.Error(DiagnosticCatalog.ValueClausePlacement, $"condition-name '{conditionName}': a format 3 VALUE "
            + $"clause shall not be specified in any data description entry subordinate to '"
            + $"{constantRecord.CobolName ?? "FILLER"}', which contains the CONSTANT RECORD clause (ISO §13.18.63.3 SR25)");
    }

    /// <summary>The subject itself or the nearest entry it is subordinate to that satisfies
    /// <paramref name="carries"/> — the one ancestor walk both placement rules ask.</summary>
    private static DataItem? EntryWithClause(DataItem subject, Func<DataItem, bool> carries)
    {
        for (DataItem? n = subject; n is not null; n = n.Parent)
            if (carries(n)) return n;
        return null;
    }
}
