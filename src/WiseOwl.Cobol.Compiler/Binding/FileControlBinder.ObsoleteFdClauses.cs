// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;
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

    /// <summary>Capture the VALUE OF clause: its entries, each literal-1 by its VALUE, for the external file connector's consistency
    /// rule (<see cref="FileDescriptionIdentity.ValueOfValue"/>), and its data-name-1 operands for <see cref="ResolveValueOfDataNames"/>.
    /// A literal-1's own rules are its token's (LiteralScreen, the format's edition gates included, kb/Work PB5416).</summary>
    private void CaptureValueOf(Core.ValueOfClauseContext clause, FileModel file)
    {
        using (Edition.At(clause))
            file.FdClauses[FileDescriptionIdentity.ValueOf] = new(
                FileDescriptionIdentity.ValueOfValue(clause.valueOfEntry().Select(e => (e.cobolWord().GetText(), e.literal() is { } lit ? ValueOfLiteralValue(lit) : null))),
                WiseOwl.Cobol.Frontend.Parsing.WrittenSource.Of(clause), Edition.Cursor);
        foreach (var entry in clause.valueOfEntry())
            if (entry.dataReference() is { } dref)
            {
                var (name, quals) = _host.ClauseDataName(dref, "VALUE OF");   // §1985:VII.3.9.3 SR1 "cannot be subscripted"
                using var _ = Edition.At(dref);
                _valueOfDataNames.Add((name, quals, Edition.Cursor));
            }
    }

    /// <summary>⛔ A VALUE OF literal-1 BY ITS VALUE, never its spelling (kb/Work PB5362): the literal is what implementor-name-1 "is made
    /// equal to" (§1985:VII.3.9.4 GR1), so two entries that write <c>'A'</c> and <c>"A"</c> (or <c>X"41"</c>) give it one value and
    /// are consistent, and <c>1</c>, <c>01</c> and <c>1.0</c> are one numeric value. The key carries the literal's CLASS beside its
    /// value (an alphanumeric <c>"1"</c> is not the numeric <c>1</c>, nor the national <c>N"1"</c>), and a nonnumeric value is
    /// hexadecimal, so no character of it can collide with the separators <see cref="FileDescriptionIdentity.ValueOfValue"/> joins
    /// the entries with. A figurative constant is its kind (<c>SPACE</c> and <c>SPACES</c> are one: "The singular and plural forms of figurative constants are equivalent", §1985:IV.4.2.2.2.3); its
    /// length comes from context, so it is not equated with any written literal. In COBOL-85, the only edition with VALUE OF, an
    /// alphanumeric literal is delimited by quotation marks: <c>'A'</c> and <c>X"41"</c> are COBOL-2002 formats the literal's own
    /// edition gate refuses (LiteralScreen.GateFormat; kb/Work PB5160, PB1545), so the delimiter-blind key states the rule for the
    /// value, and the numeric and figurative arms are where two COBOL-85 spellings agree.</summary>
    private string ValueOfLiteralValue(Core.LiteralContext lit)
    {
        if (lit.numericLiteral() is { } num)
        {
            string canonical = NumericLiteral.Normalize(num.GetText(), _host.State.Configuration.DecimalPointIsComma, out _);
            if (!NumericLiteral.TryParseExact(canonical, out var sig, out int exp))
                return "9:" + canonical;   // wider than Int128 (39+ digits): past every edition's literal capacity, compared as written
            while (sig != 0 && sig % 10 == 0) { sig /= 10; exp++; }
            return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"9:{sig}E{(sig == 0 ? 0 : exp)}");
        }
        var nn = lit.nonNumericLiteral();
        if (nn.figurativeConstant() is { } fig)
        {
            var form = WiseOwl.Cobol.CodeGen.FigurativeConstants.Classify(fig.GetText(), includeNull: false);
            return form.Kind is { } kind ? $"F:{kind}"
                : form.AllLiteral is { } all ? "ALL:" + Classed(CobolLiteral.ClassOf(all), CobolLiteral.Decode(all))
                : "F:" + CobolNames.UpperFold(fig.GetText());   // ALL symbolic-character / constant-name: named, compared by name
        }
        if (nn.concatenationExpression() is { } ce)
        {
            var folded = ConcatFolder.Peek(ce, _host.LiteralEnv);
            return Classed(folded.Category switch
            {
                PicCategory.National => LiteralClass.National,
                PicCategory.Boolean => LiteralClass.Boolean,
                _ => LiteralClass.Alphanumeric,
            }, folded.Value);
        }
        if (nn.predefinedNull() is not null) return "NULL";
        string raw = nn.GetText();
        return Classed(CobolLiteral.ClassOf(raw), CobolLiteral.Decode(raw));

        static string Classed(LiteralClass? c, string value) =>
            (c switch { LiteralClass.National => "N", LiteralClass.Boolean => "B", _ => "X" }) + ":"
            + Convert.ToHexString(System.Text.Encoding.Unicode.GetBytes(value));
    }

    /// <summary>The LABEL RECORDS clause's specification, STANDARD or OMITTED, which an external file connector's entries agree in (§1985:VII.3.6.3 GR4,
    /// imported by the relative file description, §1985:VIII.3.2.4; kb/Work PB5300). Labels are not processed, so it changes no I-O.</summary>
    private void CaptureLabelRecords(Core.LabelRecordsClauseContext clause, FileModel file)
    {
        using (Edition.At(clause))
        {
            string label = clause.STANDARD() is not null ? "STANDARD" : "OMITTED";
            file.FdClauses[FileDescriptionIdentity.Label] = new(label, label, Edition.Cursor);
        }
    }

    /// <summary>§1985:VII.3.9.3 SR1: "Data-name-1 should be qualified when necessary, but cannot be subscripted, nor can
    /// data-name-1 be an item described with the USAGE IS INDEX clause." SR2: "Data-name-1 must be in the Working-Storage
    /// Section." The subscript half is the clause-operand capture's refusal; an undefined or ambiguous name is the
    /// resolver's. A table element is the one data-name-1 that can never be written without a subscript (§1985:IV.4.3.8.2.3 SR4),
    /// so the name of one is refused here, by the predicate every reference's SR5 screen asks (<see cref="DataItem.IsTableElement"/>;
    /// kb/Work PB5299). SR1 bars "an item described with the USAGE IS INDEX clause", not an index data item: a group whose OWN
    /// entry writes USAGE IS INDEX is described with it too (the clause may be written at a group level and then "applies to
    /// each elementary item in the group", §1985:VI.5.14.4 GR1), so the group's own usage is asked beside an elementary item's
    /// resolved one, which a subordinate inherits (kb/Work PB5417). A group that merely CONTAINS an index item is not described
    /// with the clause and is admitted.</summary>
    private void ResolveValueOfDataNames()
    {
        foreach (var (name, quals, at) in _valueOfDataNames)
        {
            if (_host.ResolveClauseOperand(name, quals, "VALUE OF", at) is not { } item) continue;
            string? fault = item.Pic is { Usage: Usage.Index } || item.OwnUsage is Usage.Index
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
