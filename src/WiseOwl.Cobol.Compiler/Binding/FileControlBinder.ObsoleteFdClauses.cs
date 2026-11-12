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
    private void CaptureValueOf(Core.ValueOfClauseContext clause, FileModel file)
    {
        using (Edition.At(clause))
            file.FdClauses[FileDescriptionIdentity.ValueOf] = new(
                FileDescriptionIdentity.ValueOfValue(clause.valueOfEntry().Select(e => (e.cobolWord().GetText(), e.literal()?.GetText()))), Edition.Cursor);
        foreach (var entry in clause.valueOfEntry())
            if (entry.dataReference() is { } dref)
            {
                var (name, quals) = _host.ClauseDataName(dref, "VALUE OF");   // §1985:VII.3.9.3 SR1 "cannot be subscripted"
                using var _ = Edition.At(dref);
                _valueOfDataNames.Add((name, quals, Edition.Cursor));
            }
    }

    /// <summary>The LABEL RECORDS clause's specification, STANDARD or OMITTED, which an external file connector's entries agree in (§1985:VII.3.6.3 GR4,
    /// imported by the relative file description, §1985:VIII.3.2.4; kb/Work PB5300). Labels are not processed, so it changes no I-O.</summary>
    private void CaptureLabelRecords(Core.LabelRecordsClauseContext clause, FileModel file)
    {
        using (Edition.At(clause))
            file.FdClauses[FileDescriptionIdentity.Label] = new(clause.STANDARD() is not null ? "STANDARD" : "OMITTED", Edition.Cursor);
    }

    /// <summary>§1985:VII.3.9.3 SR1: "Data-name-1 should be qualified when necessary, but cannot be subscripted, nor can
    /// data-name-1 be an item described with the USAGE IS INDEX clause." SR2: "Data-name-1 must be in the Working-Storage
    /// Section." The subscript half is the clause-operand capture's refusal; an undefined or ambiguous name is the
    /// resolver's. A table element is the one data-name-1 that can never be written without a subscript (§1985:IV.4.3.8.2.3 SR4),
    /// so the name of one is refused here, by the predicate every reference's SR5 screen asks (<see cref="DataItem.IsTableElement"/>;
    /// kb/Work PB5299).</summary>
    private void ResolveValueOfDataNames()
    {
        foreach (var (name, quals, at) in _valueOfDataNames)
        {
            if (_host.ResolveClauseOperand(name, quals, "VALUE OF", at) is not { } item) continue;
            string? fault = item.Pic is { Usage: Usage.Index }
                ? "is described with the USAGE IS INDEX clause (ISO §1985:VII.3.9.3 SR1: \"nor can data-name-1 be an item "
                  + "described with the USAGE IS INDEX clause\")"
                : item.IsTableElement
                    ? $"is a table element (its description contains, or is subordinate to, {item.SubscriptArity} OCCURS clause"
                      + $"{(item.SubscriptArity == 1 ? "" : "s")}), which can only be referenced subscripted, and a VALUE OF clause cannot subscript it (ISO §1985:VII.3.9.3 SR1: "
                      + "\"cannot be subscripted\"; §1985:IV.4.3.8.2.3 SR4: \"Each table element reference must be subscripted except\" a SEARCH subject, a REDEFINES "
                      + "clause or an OCCURS KEY IS phrase, none of which this is)"
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
