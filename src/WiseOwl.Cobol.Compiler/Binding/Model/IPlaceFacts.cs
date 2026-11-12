// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// The facts of a binding that <see cref="PlaceBuilder"/> reads and the data model does not carry on the item
/// (DESIGN-architecture-review §8.3 (4), kb/Work PB2375): the binder implements them, the builder asks them, and the
/// model layer therefore needs no reference to the binder. Each is answered for the unit's own binding, so a
/// builder built over a unit's facts after its binding finished builds the places the code generator needs.
/// </summary>
public interface IPlaceFacts
{
    /// <summary>True when the compilation group can enable EC-BOUND-ODO at all (an enabling <c>&gt;&gt;TURN</c>): a
    /// subscripted reference through an OCCURS DEPENDING level then carries the §2023:13.18.38.4 GR7 check
    /// (<see cref="OdoReferenceCheck"/>); false keeps the zero-scaffolding invariant.</summary>
    bool OdoReferenceChecking { get; }

    /// <summary>True when the item's storage is forced to its character image by a use the binder met EARLY — a
    /// fact recorded mid-bind, before the storage-form pass settles <c>StoreAsImage</c> (kb/Work PB646: a reference
    /// modification or a RENAMES span composes the item's image only when it is not already the item's storage).</summary>
    bool IsImageBackedEarly(DataItem item);

    /// <summary>The place of a report SUM counter whose register <paramref name="item"/> is (<c>ReportSumFamily.Register</c>):
    /// the counter's value is the report engine's, so its place is a <see cref="ReportSumCounterPlace"/>, never storage. A
    /// COBOL-85 data-division clause may name a counter (§1985:XIII.3.9.3 SR10d, kb/Work PB5310), and the clause's consumers ask
    /// its place by item. Null for every other item.</summary>
    ReportSumCounterPlace? SumCounterPlaceOf(DataItem item);
}
