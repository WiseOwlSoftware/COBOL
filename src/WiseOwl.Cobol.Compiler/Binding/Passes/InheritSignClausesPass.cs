// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Passes;

/// <summary>
/// The InheritSignClauses pass: row 11 of the per-unit resolve pipeline (UsageResolved to SignResolved).
/// </summary>
internal sealed class InheritSignClausesPass : IBindPass
{
    /// <inheritdoc/>
    public string Name => "InheritSignClauses";

    /// <inheritdoc/>
    public PassPhase Requires => PassPhase.UsageResolved;

    /// <inheritdoc/>
    public PassPhase Produces => PassPhase.SignResolved;

    /// <inheritdoc/>
    public PassViews Views => PassViews.None;

    /// <summary>Apply group-level SIGN clauses to subordinate signed numeric DISPLAY/NATIONAL items (ISO
    /// §2023:13.18.52 GR1–3): a SIGN on a group applies to every signed numeric item subordinate to it, the NEAREST
    /// enclosing clause takes precedence, and an item's OWN clause (already consumed by
    /// <see cref="PictureAnalyzer.Analyze"/> at entry bind) wins outright. Runs BEFORE the REDEFINES
    /// classification pass because a SEPARATE sign occupies its own character position (§2023:13.18.52 GR6a) — it
    /// widens the item's image, which feeds the class-max width.
    /// <para>⛔ THE TYPE DECLARATIONS TOO (kb/Work PB3682), as <see cref="UsageInheritancePass.Run"/>: a template and a subject
    /// of one declaration are one description to the §2023:8.5.3.1 comparison, so the template's leaves take the group's SIGN as
    /// its subject's do. A pointer restricted to a STRONG type compares the declaration's TEMPLATE form with the subject's:
    /// `01 TG TYPEDEF STRONG SIGN IS LEADING SEPARATE. 05 A PIC S9(3).` left the template's leaf at the default sign, so
    /// `SET P TO ADDRESS OF W` (W typed by TG) was refused as another type. The walk only applies, and a copy of a template
    /// already walked re-applies the same clause to the same profile.</para></summary>
    public void Run(DataBinder binder)
    {
        foreach (var root in binder.State.Forest.Roots) binder.InheritSignWalk(root, null);
        foreach (var declaration in binder.AllTypeDecls().ToList()) binder.InheritSignWalk(declaration, null);
    }
}
