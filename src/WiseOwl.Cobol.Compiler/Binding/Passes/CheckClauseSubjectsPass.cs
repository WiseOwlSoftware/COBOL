// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The CheckClauseSubjects pass: row 9 of the per-unit resolve pipeline (UsageResolved to UsageResolved).
/// </summary>
/// <remarks>
/// <para><b>Placement in the pipeline.</b>
/// The BLANK WHEN ZERO / JUSTIFIED SUBJECT screen (§2023:13.18.8.3 SR1/SR2, §2023:13.18.32.3 SR3; kb/Work PB507). After CheckPictureRequired, whose ElementaryOnly rows have already cleared both clauses from every GROUP, and after UsageInheritancePass because SR2's usage may be INHERITED (§2023:13.18.60.4 GR1). Before everything that reads the flags (the edit/store emitters), so a refused clause is never applied.
/// </para>
/// </remarks>
internal sealed class CheckClauseSubjectsPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "CheckClauseSubjects";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>The SUBJECT rules of BLANK WHEN ZERO and JUSTIFIED — what the elementary item they are written on
    /// may BE (kb/Work PB507). Runs after <c>UsageInheritancePass</c> and <see cref="CheckPictureRequiredPass.Run"/>, because
    /// §2023:13.18.8.3 SR2 reads the usage the subject is "implicitly or explicitly described as", and §2023:13.18.60.4 GR1
    /// inheritance supplies the implicit half: <c>01 G COMP. 05 A PIC 999 BLANK WHEN ZERO.</c> violates SR2 as
    /// surely as the direct spelling.
    /// <list type="bullet">
    /// <item>§2023:13.18.8.3 SR1 — category numeric-edited, or numeric without 'S'. The category is the one the
    /// PICTURE character-string gives, which §2023:13.18.8.4 GR2 then re-defines as numeric-edited for a display /
    /// national numeric subject; so both categories are admitted here. The 'S' half is §2023:13.18.40.3's own
    /// SR22 (COBOL1934, PictureComposition) — the same fact, reported once, at the picture.</item>
    /// <item>§2023:13.18.8.3 SR2 — usage display or national, written or inherited.</item>
    /// <item>§2023:13.18.32.3 SR3 — category alphabetic, alphanumeric, boolean or national (never an EDITED
    /// category: <see cref="PicInfo.EditMask"/> is what makes one, see its doc).</item>
    /// <item>§2023:13.18.32.3 SR4 — "shall not be specified for a dynamic-length elementary item" — has NO arm here,
    /// deliberately: §2023:13.16.3 SR18 already refuses JUSTIFIED beside DYNAMIC LENGTH (the only other clauses it
    /// permits are level-number, entry-name, PICTURE, USAGE and VALUE — COBOL1563, BindEntry), which is the
    /// same fact reported once. No TYPE / SAME AS path can compose the pair either: both copy from an entry that
    /// SR18 has already screened.</item>
    /// </list>
    /// Recovery clears the clause, so no consumer applies a blank-when-zero or a right justification to an item the
    /// clause may not describe. Reported over <see cref="DataBinder.ConformanceForest"/>, recovered over
    /// <see cref="DataBinder.CompositionForest"/>, the two-forest discipline.</summary>
    public void Run(DataBinder binder)
    {
        foreach (var item in binder.ConformanceForest())
        {
            if (!DataBinder.HasNoSubordinates(item) || item.Pic is not { } pic) continue;
            string name = item.CobolName ?? "FILLER";
            if (item.BlankWhenZero && DataBinder.BlankWhenZeroViolation(pic) is { } bwz)
            {
                using var _ = binder.Edition.At(item);
                binder.Edition.Error(DiagnosticCatalog.ClauseSubjectCategory, $"data item '{name}': {bwz}");
            }
            if (item.Justified && DataBinder.JustifiedViolation(pic) is { } jus)
            {
                using var _ = binder.Edition.At(item);
                binder.Edition.Error(DiagnosticCatalog.ClauseSubjectCategory, $"data item '{name}': {jus}");
            }
            if (item.Synchronized is not null && item.ClonedFrom is null
                && DataBinder.SynchronizedViolation(pic, binder.Edition.DialectLevel) is { } sync)
            {
                using var _ = binder.Edition.At(item);
                binder.Edition.Error(DiagnosticCatalog.ClauseSubjectCategory, $"data item '{name}': {sync}");
            }
        }

        foreach (var item in binder.CompositionForest())
        {
            if (item.Pic is not { } pic) continue;
            if (item.BlankWhenZero && DataBinder.BlankWhenZeroViolation(pic) is not null) item.BlankWhenZero = false;
            if (item.Justified && DataBinder.JustifiedViolation(pic) is not null) item.Justified = false;
            if (item.Synchronized is not null && DataBinder.SynchronizedViolation(pic, binder.Edition.DialectLevel) is not null)
                item.Synchronized = null;
        }
    }
}
