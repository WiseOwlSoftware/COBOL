// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckStorageLimits pass: the per-unit resolve pipeline's screen of THE LIMIT ON THE SIZE OF ONE DATA ITEM (SignResolved to
/// SignResolved; kb/Work PB5695): every item whose character image is longer than <see cref="RecordLayout.MaxCharacterPositions"/> is
/// refused, COBOL7641, through the one decider <see cref="RecordLayout.ExceedsSizeLimit"/>.
/// </summary>
/// <remarks>
/// <para><b>Why a limit, and why this one.</b> No edition's text bounds the size of a data item (an OCCURS integer and a PICTURE
/// repetition factor each bound only themselves), and §2023:4.5 says "Translation may
/// be unsuccessful due to factors other than lack of conformance of a compilation group", whose NOTE names "the limits of an
/// implementation". Each integer-n is already within COBOL2427's limit; their PRODUCT is not: <c>05 E PIC X(1000) OCCURS
/// 2000000000.</c> is 2,000,000,000,000 character positions. It compiled clean at every edition, its size wrapped in the
/// compiler's 32-bit model (emitted as −1,454,759,936), and the program committed memory until the host ran out (79.8 GB when
/// killed). The limit is the one an elementary item's PICTURE already met (COBOL2148): one data item's image is one .NET string,
/// elementary or group (docs/conformance determination D-ITEM-SIZE).</para>
/// <para><b>Placement in the pipeline.</b> After the TYPE clauses are expanded (an item's size includes its type's) and the
/// SIGN clauses inherited (a SEPARATE sign adds a character position, §2023:13.18.52.4 GR6), and BEFORE the REDEFINES passes and
/// every later reader of a width, so the refusal is the first word about the item. The widths saturate rather than wrap
/// (<see cref="RecordLayout.ImageWidth"/>), so the binder runs on to its other reports.</para>
/// <para><b>One report per oversized subtree:</b> the outermost item past the limit is named, and its subordinates are not
/// (each of them is only part of it). An item a REDEFINES places over another adds nothing to its group's size, so the walk
/// descends into every item within the limit to find one.</para>
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
    /// object's or method's data alike: <see cref="ForestSlice.Roots"/>).</summary>
    public void Run(DataBinder binder)
    {
        foreach (var root in binder.State.Forest.Roots) Screen(binder, root);
    }

    private static void Screen(DataBinder binder, DataItem item)
    {
        if (RecordLayout.ExceedsSizeLimit(item, out long width))
        {
            using var _ = binder.Edition.At(item);
            binder.Edition.Error(DiagnosticCatalog.DataItemBeyondSizeLimit,
                $"data item '{item.CobolName ?? "FILLER"}': its size, {Count(width)} character positions, exceeds this implementation's "
                + $"limit of {Count(RecordLayout.MaxCharacterPositions)} character positions for one data item, elementary or group (docs/CONFORMANCE.md §3 "
                + "D-ITEM-SIZE). ISO §2023:4.5: \"Translation may be unsuccessful due to factors other than lack of "
                + "conformance of a compilation group\" — the limits of an implementation");
            return;
        }
        foreach (var child in item.Children) Screen(binder, child);
    }

    /// <summary>A size as the diagnostic prints it; a size past <see cref="long"/> (the saturated width) is said to be so.</summary>
    private static string Count(long n) =>
        n == long.MaxValue ? "more than 9,223,372,036,854,775,806" : n.ToString("N0", CultureInfo.InvariantCulture);
}
