// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The CONSTANT RECORD predicates the data binder keeps (kb/Work PB1262, PB1418): the two questions more than one asker puts
/// to a record's CONSTANT RECORD flag, so they stay on the binder while the subtree screens are the pass
/// (<see cref="Passes.CheckConstantRecordSubtreesPass"/>) and the constant-name binder is <see cref="ConstantEntryBinder"/>.
/// </summary>
public sealed partial class DataBinder
{
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
