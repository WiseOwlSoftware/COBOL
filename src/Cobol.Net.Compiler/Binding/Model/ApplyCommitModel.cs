// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>
/// One source element's I-O-CONTROL APPLY COMMIT clause (ISO §12.4.6.3; kb/Work PB3338): what §12.4.6.3.4 GR1 makes
/// "subject to the commit and rollback facility" when the runtime element is activated (GR5).
/// <para><see cref="Files"/> are the file-name-1 operands (SR1, SR2); each is also flagged
/// <see cref="FileModel.SubjectToApplyCommit"/>, the antecedent of the claimed clauses' syntax rules.
/// <see cref="Items"/> is every data item the clause saves at COMMIT and restores at ROLLBACK: the identifier-1
/// operands (SR3) and, after them, the items SR9 makes "automatically subject to the APPLY COMMIT clause" — each listed
/// file's FILE STATUS item, its LINAGE data-names and its RECORD clause's DEPENDING item (§14.9.7.4 GR5, §14.9.36.4 GR3:
/// "This includes the file status data items and data-items specified in the linage or record clauses of the file
/// descriptions"). An item appears once, whichever way it arrived.</para>
/// </summary>
public sealed class ApplyCommitModel
{
    /// <summary>The file-name-1 operands, in written order, each once.</summary>
    public List<FileModel> Files { get; } = [];

    /// <summary>The identifier-1 operands as written, resolved post-build (<c>DataBinder.ResolveApplyCommit</c>): the
    /// data division is not bound when the I-O-CONTROL paragraph is.</summary>
    internal List<(string Name, IReadOnlyList<string> Qualifiers, CobolNet.Editions.DiagnosticCursor At)> Identifiers { get; } = [];

    /// <summary>The 01 and 77 items the facility saves and restores: the identifier-1 operands, then SR9's implicit items.</summary>
    public List<DataItem> Items { get; } = [];

    /// <summary>Where the clause is written — the position its clause-level syntax rules report at.</summary>
    public CobolNet.Editions.DiagnosticCursor At { get; init; }
}
