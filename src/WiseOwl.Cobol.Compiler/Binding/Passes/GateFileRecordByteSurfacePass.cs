// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The GateFileRecordByteSurface pass: row 24 of the per-unit resolve pipeline (FilesResolved to FilesResolved).
/// </summary>
internal sealed class GateFileRecordByteSurfacePass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "GateFileRecordByteSurface";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.FilesResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>The byte-surface gate for FILE records — the THIRD caller of <see cref="DataBinder.ByteWindowResidueOf"/>
    /// (kb/Work PB327, folding in the instruction PB231 left on that predicate). An FD/SD record area is written
    /// to and read from a physical file as a run of BYTES (ISO §2023:14.9.30.4 GR14/GR15 state the short/long-record
    /// rules over "the number of bytes in the record"), and the generated <c>AsImage()</c>/<c>FromImage()</c> pair
    /// IS that byte layout — so the question "can this leaf be laid out in a byte window?" is literally the one
    /// the REDEFINES classifier and the EXTERNAL/BASED cell forcer ask, and asking it in a second hand-written
    /// form is how the national half came to be screened here while bit and pointer were not.
    /// <para>⚠ AS OF PB231's POINTER THIRD (landed the same day, D-SLOT) THE PREDICATE REFUSES NOTHING, so this
    /// gate is currently VACUOUS — and it stays, for the reason the predicate's ALLOW-list direction exists: a
    /// category added to the model with no pinned representation must be refused BY NAME on every byte surface,
    /// this one included, rather than laid out as a zero-width alias. A pointer or message-tag leaf in a FILE record is
    /// not refused here either, and it is no longer a deferral (kb/Work PB4838): the owner decided that the file carries the
    /// leaf's 8-position token and READ, RETURN and SORT GIVING restore the reference within the run unit, so such a record is
    /// <c>RecordImageCapable</c> and its struct's <c>AsImage</c>/<c>FromImage</c> pair is the codec (docs/CONFORMANCE.md A.1
    /// items 212 and 216). An object-reference leaf cannot reach a file record: §2023:13.18.57.3 SR8 and
    /// §2023:13.18.60.3 SR15 refuse it in the file section.</para>
    /// <para>NATIONAL now RIDES: a national character position occupies two bytes, high-order first
    /// (§2023:13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins two), which the codec composes through
    /// <c>CobolBits.NatBytes</c> and decodes through <c>CobolBits.NatReadWindow</c>. What the fold refuses is what
    /// the predicate refuses everywhere else: a pointer/object-class leaf, and a leaf with no bound
    /// representation.</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var f in binder.State.Files.Declared)
            foreach (var rec in f.Records)
                foreach (var leaf in DataBinder.LeavesOf(rec))
                    if (DataBinder.ByteWindowResidueOf(leaf) is { } residue)
                        binder.Edition.Error(DiagnosticCatalog.NationalData, $"a file record cannot be laid out — record "
                            + $"'{rec.CobolName}' has {residue} — recognized but not yet implemented");
    }
}
