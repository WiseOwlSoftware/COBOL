// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;

using Core = WiseOwl.Cobol.Frontend.Generated.CobolParserCore;

namespace WiseOwl.Cobol.Binding;

/// <summary>The three general formats of the file description entry (ISO/IEC 1989:2023 §2023:13.4.5.2, rendered from
/// the printed pages, folios 342-343) as a SET, so one table row can name every format that contains a clause.
/// §2023:13.4.5.3 binds each entry to exactly one of them: SR5 "Format 1 is the file description entry for a sequential
/// file", SR7 "Format 2 is the file description entry for a relative file or an indexed file", SR8 "Format 3 is the
/// file description entry for a report file".</summary>
[Flags]
internal enum FileDescriptionFormat
{
    None = 0,

    /// <summary>Format 1 (sequential) — record sequential or line sequential organization, no REPORT clause.</summary>
    Sequential = 1,

    /// <summary>Format 2 (relative-or-indexed).</summary>
    RelativeOrIndexed = 2,

    /// <summary>Format 3 (report) — §2023:9.1.22: "A report file is an output file having sequential organization whose
    /// file description entry contains a REPORT clause."</summary>
    Report = 4,

    All = Sequential | RelativeOrIndexed | Report,
}

/// <summary>One clause alternative of <c>fileDescriptionClause</c>: its parse-tree context, its name as a diagnostic
/// says it, the formats that contain it, and — for a clause that belongs to ANOTHER entry — which one.</summary>
/// <param name="Context">The generated context type of the alternative (the table's key).</param>
/// <param name="Name">The clause as the standard names it ("LINAGE clause").</param>
/// <param name="Formats">The §2023:13.4.5.2 formats whose printed clause list contains it.</param>
/// <param name="HomeEntry">Null for a file description clause; otherwise the entry the clause belongs to, for a
/// clause the grammar parses in the FD position only so it can be refused BY NAME (the file control entry's
/// clauses: a user who writes <c>FD F ORGANIZATION IS RELATIVE</c> is told where the clause goes, not handed a
/// token error).</param>
/// <param name="DeletedAt2002">A COBOL-85 FD clause ISO/IEC 1989:2002 deleted (LABEL RECORDS, DATA RECORDS,
/// VALUE OF). Its edition from 2002 is decided by its removal gate, so its <paramref name="Formats"/> name every format it stood
/// in in 1985 and it is left out of the "admits only" sentence, which quotes the current §2023:13.4.5.2.</param>
/// <param name="IntroducedIn">The edition that introduced the clause (85 unless stated): the "admits only" sentence of a COBOL-85
/// diagnostic lists what the 1985 file description entries held, which has neither this clause nor, where it was deleted, lacks
/// the <paramref name="DeletedAt2002"/> ones (kb/Work PB4950).</param>
/// <param name="Formats1985">The 1985 entries that print the clause, where they differ from <paramref name="Formats"/>: ANSI
/// X3.23-1985 states one entry per module (§1985:VII.3.2.2, VIII.3.2.2, IX.3.2.2, XIII.3.2.2, and with EXTERNAL and GLOBAL
/// §1985:X.4.2.2's three formats), and its report-file entry prints no DATA RECORDS clause (kb/Work PB6027). Null: the same.</param>
internal sealed record FileDescriptionClauseRow(
    Type Context, string Name, FileDescriptionFormat Formats, string? HomeEntry = null, bool DeletedAt2002 = false,
    int IntroducedIn = 85, FileDescriptionFormat? Formats1985 = null)
{
    /// <summary>The formats whose printed clause list contains the clause in <paramref name="edition"/>'s text.</summary>
    public FileDescriptionFormat FormatsIn(EditionInfo edition) => edition.Has(2002) ? Formats : Formats1985 ?? Formats;

    /// <summary>The clause's edition window: introduced in <see cref="IntroducedIn"/>, removed by 2002 when
    /// <see cref="DeletedAt2002"/>. Whether the clause is in force at an edition is the window's answer (kb/Work PB2620).</summary>
    public EditionWindow Window => new(IntroducedIn, DeletedAt2002 ? 2002 : null);
}

/// <summary>⛔ THE ONE PLACE THE FILE DESCRIPTION ENTRY'S FORMAT IS DECIDED AND ITS CLAUSE ADMISSIBILITY IS ASKED
/// (kb/Work PB1238, PB1081, PB865).
/// <para>Before it, no code decided which §2023:13.4.5.2 format an FD was, so no rule keyed on the format existed:
/// BindFileSection bound every clause against every organization. LINAGE on a relative file bound a LINAGE page no
/// relative connector ever drives, CODE-SET on an indexed file compiled clean, LINAGE rode a report FD, and the
/// five file-control-entry alternatives the grammar carries in the FD position (ORGANIZATION, ACCESS MODE, RECORD
/// KEY, ALTERNATE RECORD KEY, FILE STATUS) had NO binder arm at all — <c>FD F ORGANIZATION IS RELATIVE</c> compiled
/// to a sequential connector and <c>FD F FILE STATUS IS FS</c> left FS unwired, silently.</para>
/// <para>The table is keyed by the parse-tree context TYPE of each <c>fileDescriptionClause</c> alternative, so it
/// is enumerable: <c>FileDescriptionFormatDriftTests</c> reflects over the generated parser and fails when a new
/// alternative lands with no row here — the next clause is decided by construction, never bound by default.</para>
/// </summary>
internal static class FileDescriptionFormats
{
    private const string FileControlEntry = "the file control entry (ISO §2023:12.4.5.1)";

    /// <summary>Every <c>fileDescriptionClause</c> alternative except the <c>unrecognizedDataClause</c> error
    /// production (refused by <c>ClosedFormatPass</c>, COBOL1970), in the order §2023:13.4.5.2 prints them. The
    /// Format columns are the rendered §2023:13.4.5.2 lists.</summary>
    public static readonly IReadOnlyList<FileDescriptionClauseRow> Rows =
    [
        new(typeof(Core.ExternalClauseContext), "IS EXTERNAL clause", FileDescriptionFormat.All),
        new(typeof(Core.GlobalClauseContext), "IS GLOBAL clause", FileDescriptionFormat.All),
        new(typeof(Core.FormatClauseContext), "FORMAT clause", FileDescriptionFormat.Sequential, IntroducedIn: 2002),
        new(typeof(Core.BlockContainsClauseContext), "BLOCK CONTAINS clause", FileDescriptionFormat.All),
        new(typeof(Core.RecordClauseContext), "RECORD clause", FileDescriptionFormat.All),
        new(typeof(Core.LinageClauseContext), "LINAGE clause", FileDescriptionFormat.Sequential),
        new(typeof(Core.CodeSetClauseContext), "CODE-SET clause",
            FileDescriptionFormat.Sequential | FileDescriptionFormat.Report),
        new(typeof(Core.ReportClauseContext), "REPORT clause", FileDescriptionFormat.Report),
        new(typeof(Core.LabelRecordsClauseContext), "LABEL RECORDS clause", FileDescriptionFormat.All,
            DeletedAt2002: true),
        new(typeof(Core.DataRecordsClauseContext), "DATA RECORDS clause", FileDescriptionFormat.All,
            DeletedAt2002: true,
            // §1985:XIII.3.2.2 and §1985:X.4.2.2 Format 3 print the report-file entry without it (kb/Work PB6027).
            Formats1985: FileDescriptionFormat.Sequential | FileDescriptionFormat.RelativeOrIndexed),
        new(typeof(Core.ValueOfClauseContext), "VALUE OF clause", FileDescriptionFormat.All, DeletedAt2002: true),
        new(typeof(Core.OrganizationClauseContext), "ORGANIZATION clause", FileDescriptionFormat.None,
            FileControlEntry),
        new(typeof(Core.AccessModeClauseContext), "ACCESS MODE clause", FileDescriptionFormat.None,
            FileControlEntry),
        new(typeof(Core.RecordKeyClauseContext), "RECORD KEY clause", FileDescriptionFormat.None, FileControlEntry),
        new(typeof(Core.AlternateKeyClauseContext), "ALTERNATE RECORD KEY clause", FileDescriptionFormat.None,
            FileControlEntry),
        new(typeof(Core.FileStatusClauseContext), "FILE STATUS clause", FileDescriptionFormat.None,
            FileControlEntry),
    ];

    /// <summary><see cref="Rows"/> by context type.</summary>
    public static readonly FrozenDictionary<Type, FileDescriptionClauseRow> ByContext =
        Rows.ToFrozenDictionary(r => r.Context);

    /// <summary>The format an FD is written in, decided ONCE from the file control entry's organization and
    /// whether the entry carries a REPORT clause (ISO §2023:13.4.5.3 SR5/SR7/SR8). A relative or indexed file is
    /// Format 2 whatever else the FD says — §2023:9.1.22 makes a report file sequential, so a REPORT clause on it is a
    /// Format 2 violation, not a Format 3 entry.</summary>
    public static FileDescriptionFormat Of(FileOrganization organization, bool hasReportClause) =>
        organization is FileOrganization.Relative or FileOrganization.Indexed ? FileDescriptionFormat.RelativeOrIndexed
        : hasReportClause ? FileDescriptionFormat.Report
        : FileDescriptionFormat.Sequential;

    /// <summary>The row for one written clause, or null for the <c>unrecognizedDataClause</c> error production (which
    /// its own pass refuses).</summary>
    public static FileDescriptionClauseRow? RowOf(Core.FileDescriptionClauseContext clause) =>
        clause.ChildCount > 0 && ByContext.TryGetValue(clause.GetChild(0).GetType(), out var row) ? row : null;

    /// <summary>The module of ANSI X3.23-1985 that holds the file description entry of a file written in <paramref name="format"/>:
    /// the sequential I-O module VII, the relative VIII, the indexed IX, and the Report Writer XIII for a report file. COBOL-85
    /// states one entry per module where the 2023 text has the three formats of one clause.</summary>
    private static string Module1985(FileDescriptionFormat format, FileOrganization organization) => format switch
    {
        FileDescriptionFormat.Report => "XIII",
        FileDescriptionFormat.RelativeOrIndexed => organization is FileOrganization.Indexed ? "IX" : "VIII",
        _ => "VII",
    };

    /// <summary>The kind of file a 1985 entry describes ("relative"), as the module's function paragraph words it.</summary>
    private static string Kind1985(FileDescriptionFormat format, FileOrganization organization) => format switch
    {
        FileDescriptionFormat.Report => "report",
        FileDescriptionFormat.RelativeOrIndexed => organization is FileOrganization.Indexed ? "indexed" : "relative",
        _ => "sequential",
    };

    /// <summary>⛔ HOW A DIAGNOSTIC NAMES THE FORMAT'S ENTRY in the edition in force (kb/Work PB4950): <see cref="Caption(FileDescriptionFormat)"/>
    /// from COBOL-2002 on, and at COBOL-85 the form of the entry in the module that holds the file ("the relative-file form").</summary>
    public static string Caption(EditionInfo edition, FileDescriptionFormat format, FileOrganization organization) =>
        edition.Cite($"the {Kind1985(format, organization)}-file form", Caption(format));

    /// <summary>The citation of the clause list the format prints, in the edition in force: the general format of §2023:13.4.5.2 or
    /// the general format of the file description entry, §1985:VII.3.2.2 / VIII.3.2.2 / IX.3.2.2 / XIII.3.2.2.</summary>
    public static string FormatCitation(EditionInfo edition, FileDescriptionFormat format, FileOrganization organization) =>
        edition.Cite($"§1985:{Module1985(format, organization)}.3.2.2", "§2023:13.4.5.2");

    /// <summary>The entry the clause belongs to, as a message names it, in the edition in force: the file control entry of §2023:12.4.5.1
    /// or, at COBOL-85, of the module that holds the file (§1985:VII.2.3, VIII.2.3, IX.2.3).</summary>
    public static string HomeEntryOf(EditionInfo edition, FileDescriptionClauseRow row, FileDescriptionFormat format, FileOrganization organization) =>
        edition.Cite($"the file control entry (§1985:{Module1985(format, organization)}.2.3)", row.HomeEntry!);

    /// <summary>How a diagnostic names a format: "Format 2 (relative-or-indexed)", as §2023:13.4.5.2 captions it.</summary>
    public static string Caption(FileDescriptionFormat format) => format switch
    {
        FileDescriptionFormat.Sequential => "Format 1 (sequential)",
        FileDescriptionFormat.RelativeOrIndexed => "Format 2 (relative-or-indexed)",
        FileDescriptionFormat.Report => "Format 3 (report)",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "not a single §2023:13.4.5.2 format"),
    };

    /// <summary>The §2023:13.4.5.3 syntax rule that binds an entry to <paramref name="format"/>, quoted.</summary>
    public static string BindingRule(FileDescriptionFormat format) => format switch
    {
        FileDescriptionFormat.Sequential =>
            "ISO §2023:13.4.5.3 SR5: \"Format 1 is the file description entry for a sequential file\"",
        FileDescriptionFormat.RelativeOrIndexed =>
            "ISO §2023:13.4.5.3 SR7: \"Format 2 is the file description entry for a relative file or an indexed file\"",
        FileDescriptionFormat.Report =>
            "ISO §2023:13.4.5.3 SR8: \"Format 3 is the file description entry for a report file\"",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "not a single §2023:13.4.5.2 format"),
    };

    /// <summary>The §2023:13.4.5.3 syntax rule that binds an entry to <paramref name="format"/>, quoted, in the edition in force:
    /// at COBOL-85 the function paragraph of the module's file description entry, which states the kind of file it describes.</summary>
    public static string BindingRule(EditionInfo edition, FileDescriptionFormat format, FileOrganization organization) =>
        edition.Cite($"§1985:{Module1985(format, organization)}.3.2.1: \"The file description entry furnishes information "
            + $"concerning the physical structure, identification, and {(Kind1985(format, organization) == "report" ? "report" : "record")}-names "
            + $"pertaining to a{(Kind1985(format, organization) == "indexed" ? "n" : "")} "
            + $"{Kind1985(format, organization)} file\"",
            BindingRule(format));

    /// <summary>The clauses <paramref name="format"/> admits, in the printed order, for the diagnostic's "admits
    /// only …" sentence — derived from the table, never restated. At COBOL-85 it lists the clauses the 1985 entry printed: the
    /// ones COBOL-2002 deleted, and not the ones it introduced.</summary>
    public static string Admitted(EditionInfo edition, FileDescriptionFormat format) =>
        string.Join(", ", Rows
            .Where(r => (r.FormatsIn(edition) & format) != 0
                && r.Window.IsInForceAt(edition.Year))
            .Select(r => r.Name));
}
