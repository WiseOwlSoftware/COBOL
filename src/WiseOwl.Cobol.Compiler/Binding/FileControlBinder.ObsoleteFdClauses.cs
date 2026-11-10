// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

// ⛔ THE OPERAND RULES OF THE OBSOLETE COBOL-85 FILE DESCRIPTION CLAUSES, IN ONE PLACE (kb/Work PB5070). DATA RECORDS
// (§1985:VII.3.5) and VALUE OF (§1985:VII.3.9) are obsolete elements of X3.23-1985, deleted from COBOL-2002 (the version
// gates, VersionConformancePass.VisitDataRecordsClause and VisitValueOfClause). Both are documentation or label processing
// this implementation does not perform, so neither changes the program's I-O; their syntax rules are rules of the source
// program and are enforced (COBOL6849). Their general formats are the grammar's (CobolData.g4 dataRecordsClause,
// valueOfClause). The LABEL RECORDS clause has no operand rule: its format is the whole rule (STANDARD or OMITTED).
public sealed partial class FileControlBinder
{
    /// <summary>VALUE OF data-name-1 operands as written, resolved and screened once the data division is bound
    /// (<see cref="ResolveValueOfDataNames"/>): a Working-Storage item may be described after the FILE SECTION.</summary>
    private readonly List<(string Name, IReadOnlyList<string> Qualifiers, Editions.DiagnosticCursor At)> _valueOfDataNames = [];

    /// <summary>§1985:VII.3.5.3 SR1: "Data-name-1 is the name of a data record and must have an 01 level-number record
    /// description, with the same name, associated with it." Asked of each data-name against the 01 entries of THIS file
    /// description (or sort-merge file description) entry.</summary>
    private void ScreenDataRecords(Core.DataRecordsClauseContext clause, IReadOnlyList<DataItem> records, string fileName)
    {
        foreach (var word in clause.cobolWord())
        {
            string name = word.GetText();
            if (records.Any(r => r.Level == 1 && r.CobolName is { } n && CobolNames.Comparer.Equals(n, name))) continue;
            using var _ = Edition.At(word);
            Edition.Error(DiagnosticCatalog.FileDescriptionClauseOperand, $"DATA RECORDS {name}: '{name}' is not the name of an "
                + $"01 record description of file '{fileName}' (ISO §1985:VII.3.5.3 SR1: \"Data-name-1 is the name of a data "
                + "record and must have an 01 level-number record description, with the same name, associated with it.\")");
        }
    }

    /// <summary>Capture the VALUE OF clause's data-name-1 operands (a literal-1 has no rule beyond its format).</summary>
    private void CaptureValueOf(Core.ValueOfClauseContext clause)
    {
        foreach (var entry in clause.valueOfEntry())
            if (entry.dataReference() is { } dref)
            {
                var (name, quals) = _host.ClauseDataName(dref, "VALUE OF");   // §1985:VII.3.9.3 SR1 "cannot be subscripted"
                using var _ = Edition.At(dref);
                _valueOfDataNames.Add((name, quals, Edition.Cursor));
            }
    }

    /// <summary>§1985:VII.3.9.3 SR1: "Data-name-1 should be qualified when necessary, but cannot be subscripted, nor can
    /// data-name-1 be an item described with the USAGE IS INDEX clause." SR2: "Data-name-1 must be in the Working-Storage
    /// Section." The subscript half is the clause-operand capture's refusal; an undefined or ambiguous name is the
    /// resolver's.</summary>
    private void ResolveValueOfDataNames()
    {
        foreach (var (name, quals, at) in _valueOfDataNames)
        {
            if (_host.ResolveClauseOperand(name, quals, "VALUE OF", at) is not { } item) continue;
            string? fault = item.Pic is { Usage: Usage.Index }
                ? "is described with the USAGE IS INDEX clause (ISO §1985:VII.3.9.3 SR1: \"nor can data-name-1 be an item "
                  + "described with the USAGE IS INDEX clause\")"
                : item.Section is not EntrySection.WorkingStorage
                    ? $"is defined in the {(item.Section is { } s ? DataBinder.SectionWords(s) : "data division")}, not in the "
                      + "Working-Storage Section (ISO §1985:VII.3.9.3 SR2: \"Data-name-1 must be in the Working-Storage Section.\")"
                    : null;
            if (fault is null) continue;
            using var _ = Edition.At(at);
            Edition.Error(DiagnosticCatalog.FileDescriptionClauseOperand,
                $"the VALUE OF clause's data-name-1 '{DataBinder.WrittenQualified(name, quals)}' {fault}");
        }
    }
}
