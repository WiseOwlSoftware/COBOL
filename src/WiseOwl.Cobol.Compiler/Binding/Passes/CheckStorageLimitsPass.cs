// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckStorageLimits pass: the per-unit resolve pipeline's screen of THE LIMIT ON THE SIZE OF ONE DATA ITEM (SignResolved to
/// SignResolved; kb/Work PB5695; docs/conformance determination D-ITEM-SIZE): every item past it is refused, COBOL7641, through the one
/// decider <see cref="RecordLayout.ExceedsSizeLimit"/>, and the compile then STOPS (<see cref="DataDivisionRefused"/>).
/// </summary>
/// <remarks>
/// <para><b>Why a limit, and why this one.</b> No edition's text bounds the size of a data item (an OCCURS integer and a PICTURE
/// repetition factor each bound only themselves), and §2023:4.5 says "Translation may be unsuccessful due to factors other than lack
/// of conformance of a compilation group", whose NOTE names "the limits of an implementation"; X3.23-1985 says it as §1985:I.1.7 ("these limits will vary from one implementation of Standard COBOL to another"), which the message cites at --std 85. Each integer-n is already within
/// COBOL2427's limit; their PRODUCT is not: <c>05 E PIC X(1000) OCCURS 2000000000.</c> is 2,000,000,000,000 character positions. It
/// compiled clean at every edition, its size wrapped in the compiler's 32-bit model (emitted as −1,454,759,936), and the program
/// committed memory until the host ran out (79.8 GB when killed). The limit is the one an elementary item's PICTURE already met
/// (COBOL2148): one data item's image is one .NET string, elementary or group; its storage, overlays included, is at most two bytes
/// a position; and it holds at most as many elementary items as it may hold positions.</para>
/// <para><b>Placement, and why the compile stops.</b> After the TYPE clauses are expanded (an item's size includes its type's) and the
/// SIGN clauses inherited (a SEPARATE sign adds a character position, §2023:13.18.52.4 GR6), and BEFORE every pass that narrows a
/// group's size to an int (the REDEFINES passes first). Every size is computed in 64 bits (<see cref="RecordLayout"/>'s size
/// section) and narrowed by one function that refuses a size no admitted item has (<see cref="RecordLayout.ItemSize"/>), so a pass
/// that measured a refused item would be an internal compiler error. None does: once this pass reports, it ends the binding of the
/// compilation group, and the driver answers with the diagnostics so far (kb/Work PB5695 review R1-1: an over-limit group with a
/// REDEFINES over it went on to the classification pass and crashed the compiler with an OverflowException).</para>
/// <para><b>One report per oversized subtree:</b> the outermost item past the limit is named, and its subordinates are not (each of
/// them is only part of it). An item a REDEFINES places over another adds nothing to its group's image, so the walk descends into
/// every item within the limit to find one. A TYPEDEF template is screened too: the REDEFINES passes measure it.</para>
/// </remarks>
internal sealed class CheckStorageLimitsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckStorageLimits";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>Screen every record of the unit's forest (WORKING-STORAGE, LOCAL-STORAGE, FILE, LINKAGE, COMMUNICATION and an
    /// object's or method's data alike: <see cref="ForestSlice.Roots"/>) and every TYPEDEF template, then stop the compile when any
    /// item was refused.</summary>
    public void Run(DataBinder binder)
    {
        bool refused = false;
        foreach (var root in binder.State.Forest.Roots.Concat(binder.AllTypeDecls())) refused |= Screen(binder, root);
        if (refused) throw new DataDivisionRefused();
    }

    private static bool Screen(DataBinder binder, DataItem item)
    {
        if (RecordLayout.ExceedsSizeLimit(item, out var m))
        {
            string what = m.Image > RecordLayout.MaxCharacterPositions
                ? $"its size, {Count(m.Image)} character positions, exceeds this implementation's limit of "
                  + $"{Count(RecordLayout.MaxCharacterPositions)} character positions"
                : m.Extent > RecordLayout.MaxStorageBytes
                    ? $"its storage, {Count(m.Extent)} bytes with the items that redefine within it, exceeds this implementation's limit of "
                      + $"{Count(RecordLayout.MaxStorageBytes)} bytes"
                    : $"it holds {Count(m.Elementary)} elementary items, past this implementation's limit of "
                      + $"{Count(RecordLayout.MaxCharacterPositions)}";
            using var _ = binder.Edition.At(item);
            binder.Edition.Error(DiagnosticCatalog.DataItemBeyondSizeLimit,
                $"data item '{item.CobolName ?? "FILLER"}': {what} for one data item, elementary or group (docs/CONFORMANCE.md §3 "
                + $"D-ITEM-SIZE). {binder.Edition.Edition.Cite(LimitsOfAnImplementation1985, LimitsOfAnImplementation)}");
            return true;
        }
        bool refused = false;
        foreach (var child in item.Children) refused |= Screen(binder, child);
        return refused;
    }

    /// <summary>The rule that admits an implementation's limits, as the message quotes it in the edition in force (kb/Work PB4950).</summary>
    private const string LimitsOfAnImplementation1985 =
        "ISO §1985:I.1.7: \"It is recognized that these limits will vary from one implementation of Standard COBOL to another and may "
        + "prevent the successful translation of some programs that meet the requirements of Standard COBOL\"";

    private const string LimitsOfAnImplementation =
        "ISO §2023:4.5: \"Translation may be unsuccessful due to factors other than lack of conformance of a compilation group\" "
        + "— the limits of an implementation";

    /// <summary>A size as the diagnostic prints it; a size past <see cref="long"/> (the saturated width) is said to be so.</summary>
    private static string Count(long n) =>
        n == long.MaxValue ? "more than 9,223,372,036,854,775,806" : n.ToString("N0", CultureInfo.InvariantCulture);
}

/// <summary>⛔ THE COMPILE STOPS AFTER A DATA ITEM PAST THE SIZE LIMIT (kb/Work PB5695 review R1-1): thrown by
/// <see cref="CheckStorageLimitsPass"/> once it has reported COBOL7641, so no later pass measures the refused item, and caught by the
/// compile driver (<c>CompilerDriver.Compile</c>), which answers with the diagnostics reported so far. Not an error of its own: the
/// diagnostics carry the refusal.</summary>
internal sealed class DataDivisionRefused() : Exception("a data item past the size limit (COBOL7641) ended the binding of the compilation group");