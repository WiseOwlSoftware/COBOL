// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The RENAMES clause's ENTRY-level syntax rules (ISO §2023:13.18.45.3; kb/Work PB1283): what its operands may BE (SR3, SR5,
/// SR6), where its window may lie (SR11) and where the entry may stand (SR2). They were all absent because
/// data-name-2 / data-name-3 were found by name through <see cref="SubtreeCandidates"/> — whose candidate set includes
/// the record ITSELF — and then used without being asked what they are. SR4 (operands name items of THIS record), SR7
/// (not subscripted), SR8 (what the range may hold) and SR10 (whole bytes) already had their screens beside the
/// resolution in <see cref="ResolveRedefinesPass.Run"/>.
/// </summary>
public sealed partial class DataBinder
{

    /// <summary>§2023:13.18.45.3 SR2 — "All RENAMES entries referring to data items within a given record shall immediately
    /// follow the last data description entry of the associated record description entry." Asked by <c>BindEntries</c>,
    /// the only place the entry ORDER exists. A level-66 entry needs an open associated record in this section (a
    /// level-77 item is no record description entry, and a constant entry ends the record before it — kb/Work PB2516),
    /// and once a record's RENAMES entries have begun no level 02-49 entry may follow before the next record.</summary>
    private void ScreenRenamesPlacement(DataItem? associatedRecord, string aliasName)
    {
        if (associatedRecord is { Level: not 77 }) return;
        Edition.Error(DiagnosticCatalog.RenamesEntryRule,
            $"'{aliasName}': the RENAMES entry follows "
            + (associatedRecord is null
                ? "no record description entry it can belong to (none precedes it in this section, or a constant entry ended it)"
                : "a level-77 item, which is not a record description entry")
            + " (ISO §2023:13.18.45.3 SR2: \"All RENAMES entries referring to data items within a given record shall "
            + "immediately follow the last data description entry of the associated record description entry.\")");
    }

    /// <summary>SR2's other half: an entry that continues a record whose RENAMES entries have already begun.</summary>
    private void ScreenEntryAfterRenames(string? entryName)
    {
        Edition.Error(DiagnosticCatalog.RenamesEntryRule,
            $"'{entryName ?? "FILLER"}': a data description entry follows the RENAMES entries of its record (ISO "
            + "§2023:13.18.45.3 SR2: \"All RENAMES entries referring to data items within a given record shall immediately "
            + "follow the last data description entry of the associated record description entry.\")");
    }
}
