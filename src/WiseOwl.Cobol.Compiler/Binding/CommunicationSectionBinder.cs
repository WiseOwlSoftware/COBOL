// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Communication;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>What each format of the communication description entry prints (ISO/IEC 1989:2002 A.1.2.1) and lays out (A.1.2.3 GR2, GR16, GR24):
/// the ONE table the entry's clauses are read against and its implicit description is built from.</summary>
internal static class CommunicationFormats
{
    /// <summary>One position of the area: its field, its size, and whether the implicit description is an unsigned integer.</summary>
    internal readonly record struct Slot(CommunicationField Field, int Size, bool Numeric);

    private static readonly Slot[] Input =
    [
        new(CommunicationField.Queue, 12, false),
        new(CommunicationField.SubQueue1, 12, false),
        new(CommunicationField.SubQueue2, 12, false),
        new(CommunicationField.SubQueue3, 12, false),
        new(CommunicationField.MessageDate, 6, true),
        new(CommunicationField.MessageTime, 8, true),
        new(CommunicationField.SymbolicSource, 12, false),
        new(CommunicationField.TextLength, 4, true),
        new(CommunicationField.EndKey, 1, false),
        new(CommunicationField.StatusKey, 2, false),
        new(CommunicationField.MessageCount, 6, true),
    ];

    private static readonly Slot[] InputOutput =
    [
        new(CommunicationField.MessageDate, 6, true),
        new(CommunicationField.MessageTime, 8, true),
        new(CommunicationField.SymbolicTerminal, 12, false),
        new(CommunicationField.TextLength, 4, true),
        new(CommunicationField.EndKey, 1, false),
        new(CommunicationField.StatusKey, 2, false),
    ];

    private static readonly Slot[] OutputHeader =
    [
        new(CommunicationField.DestinationCount, 4, true),
        new(CommunicationField.TextLength, 4, true),
        new(CommunicationField.StatusKey, 2, false),
    ];

    private static readonly Slot[] OutputEntry =
    [
        new(CommunicationField.ErrorKey, 1, false),
        new(CommunicationField.SymbolicDestination, 12, false),
    ];

    /// <summary>The positions in order: the second option's series of data-names corresponds to them "taken in order" (GR2, GR24).</summary>
    internal static IReadOnlyList<Slot> Header(CommunicationFormat format) => format switch
    {
        CommunicationFormat.Input => Input,
        CommunicationFormat.InputOutput => InputOutput,
        _ => OutputHeader,
    };

    /// <summary>The positions of one occurrence of the output table (GR16 d), none for the other formats.</summary>
    internal static IReadOnlyList<Slot> Entry(CommunicationFormat format) => format is CommunicationFormat.Output ? OutputEntry : [];

    /// <summary>The field a data-name clause defines, or null for the DESTINATION TABLE OCCURS clause.</summary>
    internal static CommunicationField? FieldOf(Core.CdClauseContext c) =>
        c.QUEUE() is not null ? CommunicationField.Queue
        : c.SUB_QUEUE_1() is not null ? CommunicationField.SubQueue1
        : c.SUB_QUEUE_2() is not null ? CommunicationField.SubQueue2
        : c.SUB_QUEUE_3() is not null ? CommunicationField.SubQueue3
        : c.DATE() is not null ? CommunicationField.MessageDate
        : c.TIME() is not null ? CommunicationField.MessageTime
        : c.SOURCE() is not null ? CommunicationField.SymbolicSource
        : c.TERMINAL() is not null ? CommunicationField.SymbolicTerminal
        : c.TEXT() is not null ? CommunicationField.TextLength
        : c.END() is not null ? CommunicationField.EndKey
        : c.STATUS() is not null ? CommunicationField.StatusKey
        : c.TABLE() is not null ? null
        : c.COUNT() is not null ? (c.DESTINATION() is not null ? CommunicationField.DestinationCount : CommunicationField.MessageCount)
        : c.ERROR() is not null ? CommunicationField.ErrorKey
        : CommunicationField.SymbolicDestination;

    /// <summary>The clause as the rules name it, for a message.</summary>
    internal static string ClauseWords(CommunicationField? field) => field switch
    {
        CommunicationField.Queue => "SYMBOLIC QUEUE",
        CommunicationField.SubQueue1 => "SYMBOLIC SUB-QUEUE-1",
        CommunicationField.SubQueue2 => "SYMBOLIC SUB-QUEUE-2",
        CommunicationField.SubQueue3 => "SYMBOLIC SUB-QUEUE-3",
        CommunicationField.MessageDate => "MESSAGE DATE",
        CommunicationField.MessageTime => "MESSAGE TIME",
        CommunicationField.SymbolicSource => "SYMBOLIC SOURCE",
        CommunicationField.SymbolicTerminal => "SYMBOLIC TERMINAL",
        CommunicationField.TextLength => "TEXT LENGTH",
        CommunicationField.EndKey => "END KEY",
        CommunicationField.StatusKey => "STATUS KEY",
        CommunicationField.MessageCount => "MESSAGE COUNT",
        CommunicationField.DestinationCount => "DESTINATION COUNT",
        CommunicationField.ErrorKey => "ERROR KEY",
        CommunicationField.SymbolicDestination => "SYMBOLIC DESTINATION",
        _ => "DESTINATION TABLE OCCURS",
    };

    internal static string FormatWords(CommunicationFormat format) => format switch
    {
        CommunicationFormat.Input => "Format 1, an input CD",
        CommunicationFormat.Output => "Format 2, an output CD",
        _ => "Format 3, an input-output CD",
    };
}

/// <summary>
/// ⛔ THE COMMUNICATION SECTION (ISO/IEC 1989:2002 Annex A.1; docs/rearchitecture/DESIGN-communication-facility.md §3; kb/Work PB4677). Each CD
/// becomes a <see cref="CommunicationDescription"/> whose <see cref="CommunicationDescription.Area"/> is the record A.1.2.3 prints as the entry's
/// "IMPLICIT DESCRIPTION", built here from <see cref="CommunicationFormats"/>; every record the program writes after the CD joins one redefinition
/// class with it (the FD's implicit record area, <see cref="RedefinitionKind.ImplicitCommunicationRecord"/>). The class's anchor is the FIRST written
/// record when there is one, because "only the first redefinition may contain VALUE clauses" (A.1.2.2 SR5, SR9, SR14) and a class's seed is its
/// anchor's; CCVS-85 CM201M writes `02 FILLER PIC 9999 VALUE 1` there. The items are static like working-storage's (ISO/IEC 1989:2002 8.6.3).
/// </summary>
internal sealed class CommunicationSectionBinder(DataBinder host)
{
    private EditionContext Edition => host.Edition;

    /// <summary>Bind the section's entries, in source order, and then the rule over all of them, SR2's INITIAL clause.</summary>
    internal void BindSection(Core.ProgramUnitContext program, CsNameScope rootNames)
    {
        if (program.dataDivision()?.communicationSection() is not { } section) return;
        if (host.UnitIsFunction)
            using (Edition.At(section))
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, "a function definition contains a communication section; "
                    + "ISO/IEC 1989:2002 Annex A: \"The communication facility may be used only in a program definition.\"");
        foreach (var entry in section.communicationDescriptionEntry())
            using (Edition.At(entry))
                BindEntry(entry, rootNames);
        ScreenInitialClause(program, section);
    }

    private void BindEntry(Core.CommunicationDescriptionEntryContext entry, CsNameScope rootNames)
    {
        string name = entry.cdName().GetText();
        host.DeclareUserWord(name, UserWordKind.CdName);
        var mode = entry.cdMode();
        var format = mode.INPUT() is not null ? CommunicationFormat.Input
            : mode.OUTPUT() is not null ? CommunicationFormat.Output
            : CommunicationFormat.InputOutput;
        var names = ReadClauses(entry, name, format, out int? destinationTable, out var indexNames);
        var records = host.BindEntries(entry.dataDescriptionEntry(), rootNames, EntrySection.Communication);
        host.State.Forest.AddWorkingStorageRoots(records);
        var written = records;
        bool anyClause = entry.cdClause().Length > 0 || entry.cdSeriesName().Length > 0;
        // SR4 ("If neither option for specifying the interface area is used, a level 01 data description entry shall follow the CD entry") and its
        // output twin SR8 ("If none of the optional clauses of the CD entry is specified"). Nothing else would give the program a name for the area.
        if (!anyClause && written.Count == 0)
            Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{name}' specifies none of its clauses and no level 01 data description entry "
                + "follows it; ISO/IEC 1989:2002 A.1.2.2 "
                + (format is CommunicationFormat.Output
                    ? "SR8: \"If none of the optional clauses of the CD entry is specified, a level 01 data description entry shall follow the CD entry.\""
                    : "SR4: \"If neither option for specifying the interface area is used, a level 01 data description entry shall follow the CD entry.\""));
        var area = BuildArea(name, format, names, destinationTable, indexNames, rootNames);
        host.State.Forest.AddWorkingStorageRoots([area]);
        DataItem anchor = written.Count > 0 ? written[0] : area;
        foreach (var record in written.Skip(1).Append(area))
            if (!ReferenceEquals(record, anchor) && record.RedefinesTarget is null)
                record.SetRedefinition(anchor, RedefinitionKind.ImplicitCommunicationRecord);
        host.State.Communication.Declare(new CommunicationDescription(name, format, mode.INITIAL_() is not null, area, written));
    }

    /// <summary>Read the clauses or the series of data-names into the field they name, refusing what the entry's format does not print (A.1.2.1): the
    /// clause of another format, a clause written twice, and a series of the wrong length; and SR6, SR10, SR15's uniqueness within the entry.</summary>
    private Dictionary<CommunicationField, string> ReadClauses(Core.CommunicationDescriptionEntryContext entry, string cdName, CommunicationFormat format,
        out int? destinationTable, out List<string> indexNames)
    {
        destinationTable = null;
        indexNames = [];
        var header = CommunicationFormats.Header(format);
        var printed = header.Concat(CommunicationFormats.Entry(format)).Select(s => s.Field).ToHashSet();
        var names = new Dictionary<CommunicationField, string>();
        var seen = new HashSet<CommunicationField?>();
        foreach (var clause in entry.cdClause())
        {
            using var _ = Edition.At(clause);
            var field = CommunicationFormats.FieldOf(clause);
            bool inFormat = field is { } f ? printed.Contains(f) : format is CommunicationFormat.Output;
            if (!inFormat)
            {
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{cdName}': the {CommunicationFormats.ClauseWords(field)} clause is not a clause of "
                    + $"{CommunicationFormats.FormatWords(format)}; ISO/IEC 1989:2002 A.1.2.1 prints it in another format.");
                continue;
            }
            if (!seen.Add(field))
            {
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{cdName}': the {CommunicationFormats.ClauseWords(field)} clause is written twice; "
                    + "ISO/IEC 1989:2002 A.1.2.1 prints each clause in brackets, once.");
                continue;
            }
            if (field is { } named)
            {
                if (clause.dataName().FILLER() is not null)
                    Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{cdName}': the {CommunicationFormats.ClauseWords(field)} clause names FILLER; ISO/IEC 1989:2002 A.1.2.2 "
                        + "SR6 and SR15 let a data-name be replaced by the reserved word FILLER \"within this series\", the option of the series of data-names, and a clause prints a data-name.");
                else
                    Record(named, clause.dataName().GetText());
            }
            else
            {
                // integer-1: IntegerOperandValue and IntegerOperandPass refuse a zero and a value past the host's limit.
                if (host.Constants.IntegerOperandValue(clause.integerOperand(), $"CD '{cdName}' DESTINATION TABLE OCCURS") is { } size and >= 1) destinationTable = size;
                if (clause.indexNameList() is { } list)
                    foreach (var word in list.cobolWord())
                    {
                        indexNames.Add(word.GetText());
                        host.DeclareUserWord(word.GetText(), UserWordKind.IndexName);
                    }
            }
        }
        if (entry.cdSeriesName() is { Length: > 0 } series)
        {
            if (format is CommunicationFormat.Output)
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{cdName}': a series of data-names is not a part of {CommunicationFormats.FormatWords(format)}; "
                    + "ISO/IEC 1989:2002 A.1.2.1 prints that option in Formats 1 and 3.");
            else if (series.Length != header.Count)
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{cdName}': the series names {series.Length} data-names and "
                    + $"{CommunicationFormats.FormatWords(format)} prints {header.Count}; ISO/IEC 1989:2002 A.1.2.3: the series, \"taken in order\", corresponds to the clauses.");
            else
                for (int i = 0; i < series.Length; i++)
                    if (series[i].dataName().FILLER() is null)
                    {
                        using var seriesAt = Edition.At(series[i]);
                        Record(header[i].Field, series[i].dataName().GetText());
                    }
        }
        return names;

        // Record the data-name a clause or a series member gives a field. A name another field already carries is refused AT
        // THE OCCURRENCE that repeats it (kb/Work PB5090): the clause or the series member the cursor stands on.
        void Record(CommunicationField field, string name)
        {
            if (names.Any(n => n.Key != field && CobolNames.Same(n.Value, name)))
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{cdName}' names the data-name '{name}' more than once; "
                    + (Edition.DialectLevel < 2002 ? "ISO §1985:XIV.2.2.3 " : "ISO/IEC 1989:2002 A.1.2.2 ")
                    + (format switch
                    {
                        CommunicationFormat.Input => "SR6",
                        CommunicationFormat.Output => "SR10",
                        _ => "SR15",
                    }) + (Edition.DialectLevel < 2002
                        ? format == CommunicationFormat.Output
                            ? ": the data-names \"must be unique within a CD entry\"."
                            : ": the data-names \"must be unique within the CD entry\"."
                        : ": the data-names \"shall be unique within the CD entry\"."));
            names[field] = name;
        }
    }

    /// <summary>The record A.1.2.3 prints as the "IMPLICIT DESCRIPTION": an unnamed level 1 group of elementary items, named by the clauses that name them
    /// and unnamed otherwise. The output table is flat without a DESTINATION TABLE OCCURS clause: SR11 "one error key and one symbolic destination area are
    /// assumed. In this case, subscripting is not permitted when referencing these data items", and SR12 otherwise.</summary>
    private DataItem BuildArea(string cdName, CommunicationFormat format, Dictionary<CommunicationField, string> names, int? destinationTable,
        List<string> indexNames, CsNameScope rootNames)
    {
        var area = new DataItem
        {
            Level = 1,
            DeclaredAt = Edition.Cursor,
            CobolName = null,   // the cd-name is no data-name, so the area itself is unreferenceable; its items are qualified by it (8.4.1.1.1)
            ClrName = CsNames.Allocate($"_cdArea{DataItem.Sanitize(cdName)}", rootNames),
        };
        area.Uid = host.TakeUid();
        area.RootSection = EntrySection.Communication;
        host.State.Forest.AddRoot(area);
        foreach (var slot in CommunicationFormats.Header(format))
            AddSlot(area, 2, slot, names);
        if (format is not CommunicationFormat.Output) return area;
        var parent = area;
        int level = 2;
        if (destinationTable is { } occurs)
        {
            parent = new DataItem
            {
                Level = 2,
                DeclaredAt = Edition.Cursor,
                CobolName = null,
                ClrName = $"_filler{host.TakeFillerOrdinal()}",
                Occurs = occurs,
                Parent = area,
            };
            parent.Uid = host.TakeUid();
            parent.IndexNames.AddRange(indexNames);
            area.AddMember(parent);
            host.RegisterImpliedName(parent);
            level = 3;
        }
        foreach (var slot in CommunicationFormats.Entry(format))
            AddSlot(parent, level, slot, names);
        return area;
    }

    private void AddSlot(DataItem parent, int level, CommunicationFormats.Slot slot, Dictionary<CommunicationField, string> names)
    {
        string? name = names.GetValueOrDefault(slot.Field);
        var item = new DataItem
        {
            Level = level,
            DeclaredAt = Edition.Cursor,
            CobolName = name,
            ClrName = name is null ? $"_filler{host.TakeFillerOrdinal()}" : DataItem.Sanitize(name),
            Pic = slot.Numeric
                ? new PicInfo(PicCategory.Numeric, Usage.Display, Length: slot.Size, Digits: slot.Size, Scale: 0, Signed: false)
                : new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: slot.Size, Digits: 0, Scale: 0, Signed: false),
            Parent = parent,
        };
        item.Uid = host.TakeUid();
        parent.AddMember(item);
        host.RegisterImpliedName(item);
        if (name is not null) host.DeclareUserWord(name, UserWordKind.DataName);
    }

    /// <summary>A.1.2.2 SR2: "Within a single program, the INITIAL clause may be specified in only one CD entry. The INITIAL clause shall not be used in a program
    /// that specifies the USING phrase of the procedure division header."</summary>
    private void ScreenInitialClause(Core.ProgramUnitContext program, Core.CommunicationSectionContext section)
    {
        var initial = section.communicationDescriptionEntry().Where(e => e.cdMode().INITIAL_() is not null).ToList();
        foreach (var second in initial.Skip(1))
            using (Edition.At(second))
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"CD '{second.cdName().GetText()}' specifies the INITIAL clause, and so does "
                    + $"'{initial[0].cdName().GetText()}'; ISO/IEC 1989:2002 A.1.2.2 SR2: \"Within a single program, the INITIAL clause may be specified in only one CD entry.\"");
        if (initial.Count > 0 && program.procedureDivision()?.usingClause() is { } usingPhrase)
            using (Edition.At(usingPhrase))
                Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"the procedure division header specifies the USING phrase and CD '{initial[0].cdName().GetText()}' the INITIAL "
                    + "clause; ISO/IEC 1989:2002 A.1.2.2 SR2: \"The INITIAL clause shall not be used in a program that specifies the USING phrase of the procedure division header.\"");
    }

    /// <summary>The rules that need the entries' sizes and VALUE clauses, asked once the forest is complete: A.1.2.2 SR5 and SR14, a record of an input or input-output
    /// CD "shall describe a record of exactly" 87 or 33 "alphanumeric character positions"; and SR5, SR9, SR14, "only the first redefinition may contain VALUE clauses".</summary>
    internal void CheckRecords()
    {
        foreach (var cd in host.State.Communication.Declared)
        {
            if (cd.Format is not CommunicationFormat.Output)
            {
                int required = cd.Format is CommunicationFormat.Input ? 87 : 33;
                string rule = cd.Format is CommunicationFormat.Input ? "SR5" : "SR14";
                foreach (var record in cd.Records)
                    if (record.ByteWidth != required)
                        using (Edition.At(record))
                            Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"record '{record.CobolName ?? "FILLER"}' of CD '{cd.Name}' describes {record.ByteWidth} character "
                                + $"positions; ISO/IEC 1989:2002 A.1.2.2 {rule}: it \"shall describe a record of exactly {required} alphanumeric character positions\".");
            }
            foreach (var record in cd.Records.Skip(1))
                if (FirstWithValue(record) is { } valued)
                    using (Edition.At(valued))
                        Edition.Error(DiagnosticCatalog.CommunicationEntryRule, $"'{valued.CobolName ?? "FILLER"}' has a VALUE clause in a record of CD '{cd.Name}' that is not its first; "
                            + "ISO/IEC 1989:2002 A.1.2.2: \"only the first redefinition may contain VALUE clauses\".");
        }
    }

    private static DataItem? FirstWithValue(DataItem item)
    {
        if (item.RawValue is not null || item.TableValues is not null) return item;
        foreach (var child in item.Children)
            if (FirstWithValue(child) is { } found)
                return found;
        return null;
    }
}
