// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Compiler.Binding.Model;

/// <summary>⛔ ISO §2023:13.18.22.4 GR5, THE ONE PLACE ITS TWO SENTENCES ARE APPLIED (kb/Work PB511, PB2724): "Literal-1, if
/// specified, is the name of the file connector or record that is externalized to the operating environment. If literal-1 is
/// not specified, the externalized name of the file connector or record is the name specified in the file description entry
/// or the data-name format of the entry-name clause, respectively." Both hosts of the one EXTERNAL clause ask it: the file
/// description entry's binder (<c>FileControlBinder.BindFileSection</c>) and the record (<see cref="DataItem.ExternalizedRecordName"/>).
/// It lives in the model layer because the record asks it, and the model depends on no binder.</summary>
internal static class ExternalClauseName
{
    /// <summary>The externalized name of an EXTERNAL file connector or record. <paramref name="literal1"/> is the AS phrase's
    /// screened value (<c>ExternalizedName.AsLiteralOf</c>, null when the clause writes none or SR3 refused it) and keeps its
    /// case, because a literal is its own character-string; <paramref name="subjectName"/> is a COBOL word, written in its one
    /// upper-case spelling (<see cref="CobolNames.UpperFold"/>: §2023:8.1.3.2's Annex C fold, kb/Work PB1965).</summary>
    public static string Of(string? literal1, string subjectName) => literal1 ?? CobolNames.UpperFold(subjectName);
}
