// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;

namespace CobolNet.Binding;

/// <summary>
/// The SYNCHRONIZED clause on a GROUP item — <b>ISO §13.18.55.4 GR1</b>: "When this clause is specified for a group
/// item, it is treated as though it had instead been separately specified for each of the subordinate elementary items
/// for which this clause is permitted." — and the COBOL-2023 edition gate that comes with it (Annex E.3.2 item 6).
///
/// <para><b>Why the flag is copied down (kb/Work PB539).</b> The clause changes no offset here (the one-byte natural
/// boundary, docs/CONFORMANCE.md DOC-A.1-195/197), so the LAYOUT half of GR1 holds by itself. The rules that ask
/// whether an elementary item is "described with" the clause do not: the §8.5.3.1 type-equivalence comparison
/// (<see cref="StrongTypeModel"/>) compares each pair of corresponding elementary items' SYNCHRONIZED clauses, and with
/// the flag left on the group entry <c>01 TC TYPEDEF STRONG SYNC. 05 X1 PIC X. …</c> and the same declaration written
/// with SYNC on each leaf were two different types, so a legal CALL was refused COBOL1688. So each elementary item
/// takes the form of the clause the nearest enclosing group wrote, unless its own entry wrote one (an entry's own
/// clause is the nearest, as for the SIGN clause, §13.18.52.4 GR3).</para>
///
/// <para><b>"For which this clause is permitted."</b> Two syntax rules forbid the clause on an item because of where
/// it stands, and below either the clause is not permitted, so the group's clause does not reach the item:
/// <list type="bullet">
/// <item>§13.18.63.3 SR14 — "If a VALUE clause is specified at the group level, subordinate items within that group
/// shall not be described with a JUSTIFIED or SYNCHRONIZED clause";</item>
/// <item>§13.16.3 SR13 — the SYNCHRONIZED clause "shall not be specified in the same data description entry with the
/// CONSTANT RECORD clause, or in any data description entry subordinate to a data description entry with the CONSTANT
/// RECORD clause".</item>
/// </list>
/// An entry that WRITES the clause below either is still refused by that rule; only the as-if copy stops.</para>
///
/// <para><b>Where it runs, and why there.</b> A TYPE DECLARATION takes it when it is completed as a declaration
/// (<see cref="ExpandTemplate"/>), BEFORE any TYPE subject copies it: §13.18.57.4 GR1 copies the declaration
/// "excluding the level-number, name, alignment …", so a declaration's group-level clause never reaches the TYPE
/// subject's own entry, but under GR1 above that clause was "instead" specified on the subordinate elementary items,
/// whose descriptions the subject does copy. The storage forest takes it once every TYPE and SAME AS reference is
/// expanded (<see cref="ExpandTypes"/>), so a TYPE subject that writes the clause itself reaches the leaves it copied.
/// The walk is idempotent, so a subtree completed ahead (<see cref="CompleteDescriptionAhead"/>) is safe to re-walk.</para>
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>§13.18.55.4 GR1 over the subtree rooted at <paramref name="item"/> — see the type summary — and the
    /// group-item edition gate for every group entry in it that WROTE the clause.</summary>
    /// <param name="item">The entry to visit.</param>
    /// <param name="inherited">The clause the nearest enclosing group wrote, or null.</param>
    /// <param name="permitted">False below a group-level VALUE or a CONSTANT RECORD, where the clause is not permitted.</param>
    private void InheritSynchronized(DataItem item, SynchronizedPhrase? inherited = null, bool permitted = true)
    {
        if (item.IsElementary)
        {
            if (permitted) item.Synchronized ??= inherited;
            return;
        }
        GateSynchronizedGroup(item);
        // §13.18.63.3 SR14 and §13.16.3 SR13: below a group-level VALUE or a CONSTANT RECORD nothing is handed down,
        // not even a clause a subordinate group writes itself (that entry is refused by the rule it breaks).
        permitted &= !HasGroupValue(item) && !item.IsConstantRecord;
        var down = item.Synchronized ?? inherited;
        foreach (var c in item.Children) InheritSynchronized(c, down, permitted);
    }

    /// <summary>SYNCHRONIZED on a GROUP item is a COBOL-2023 introduction (ISO Annex E.3.2 item 6 — "This clause may now
    /// be specified for a group level data item"; §13.18.55.3 SR1 is the 2023 text). It routes through the CANONICAL
    /// funnel like every other introduction, so it is a hard error on BOTH axes: §4.2.2's warning mechanism reports
    /// violations of the standard, and --permissive is the migration mode for constructs an edition REMOVED — it has no
    /// meaning for one the targeted edition has not yet acquired, which no pre-existing program can legally contain
    /// (CA14, owner-approved option (a)).
    /// <para>Asked of the entry that WROTE the clause (<see cref="DataItem.ClonedFrom"/> null), wherever it is written: a
    /// TYPEDEF's group as well as a storage group (kb/Work PB494 — the gate sat in the storage forest's usage walk, so
    /// <c>01 TG TYPEDEF SYNC.</c> compiled clean at COBOL-2002 and COBOL-2014), and never again at a member a TYPE
    /// clause copied from that declaration.</para></summary>
    private void GateSynchronizedGroup(DataItem group)
    {
        if (group.Synchronized is null || group.ClonedFrom is not null || Edition.DialectLevel >= 2023) return;
        using var _ = Edition.At(group);
        ConstructRegistry.Check(Edition.Edition, Edition.Sink, Constructs.SyncOnGroup2023,
            $"data item '{group.CobolName ?? "FILLER"}'");
    }
}
