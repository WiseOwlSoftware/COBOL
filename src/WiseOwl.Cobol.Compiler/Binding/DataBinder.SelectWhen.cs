// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding;

public sealed partial class DataBinder
{
    /// <summary>Record the SELECT WHEN clause of one data description entry (ISO §2023:13.18.51; ISO/IEC 1989:2002 §13.16.49) on the item.
    /// SR1 — <i>"A SELECT WHEN clause may be specified only at the 01 level of a record description entry"</i> — is the only rule an entry
    /// can answer alone; a clause that breaks it is refused and dropped, so the entry binds as though it had not been written under an
    /// already-failed compile. SR2 - SR6 ask about the file's other records and the level-88 entries beneath them
    /// (<c>ResolveRecordFormatPass</c>).</summary>
    private void BindSelectWhen(DataItem item, SelectWhenSpec clause)
    {
        if (item.Level == 1)
        {
            item.SelectWhen = clause;
            return;
        }
        using var _ = Edition.At(clause.At);
        Edition.Error(DiagnosticCatalog.SelectWhenClauseViolation,
            $"SELECT WHEN is written in data description entry '{item.CobolName ?? "FILLER"}', whose level number is {item.Level:00}: "
            + $"a SELECT WHEN clause may be specified only at the 01 level of a record description entry ({RecordFormatCitations.SelectWhen(Edition.DialectLevel, "SR1")})");
    }
}
