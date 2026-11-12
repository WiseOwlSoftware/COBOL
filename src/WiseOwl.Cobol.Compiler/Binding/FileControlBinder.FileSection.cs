// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// The FILE SECTION half of the file binder (ISO §2023:13.4.5 FD, §2023:13.4.6 SD; DESIGN-architecture-review §8.3 (1); kb/Work PB2296 M7):
/// binds the FD and SD entries into the storage forest and onto the <see cref="FileModel"/>s the SELECT entries declared
/// (<see cref="BindFileSection"/>, with the RECORD, BLOCK CONTAINS and LINAGE clauses and the implied record of a record-less
/// FD), and screens that every SELECT has its description (<see cref="ScreenSelectsHaveDescriptions"/>, §2023:12.4.5.2 SR3). The
/// post-build half is <c>FileControlBinder.Resolve.cs</c>.
/// </summary>
public sealed partial class FileControlBinder
{
    /// <summary>ISO §2023:12.4.5.2 SR3 — <i>"For each file-name specified in a SELECT clause, there shall be a file
    /// description entry or a sort-merge file description entry in the file section of the factory, function,
    /// object, or program in which the SELECT clause is specified."</i> Asked once every FD and SD has attached
    /// (both set <see cref="FileModel.HasFd"/>), and separately from <see cref="BindFileSection"/> because a unit
    /// with no FILE SECTION at all has the same obligation. A SELECT with no description used to compile clean,
    /// and its first OPEN aborted the run unit as "a compiler defect" (kb/Work PB1077). The opposite direction, an
    /// FD or SD that no SELECT names, is reported where the entry binds.</summary>
    internal void ScreenSelectsHaveDescriptions()
    {
        foreach (var (name, file) in SelectedFiles)
        {
            if (file.HasFd) continue;
            using var _ = Edition.At(file.EntryAt);
            Edition.Error(DiagnosticCatalog.SelectWithoutFileDescription, $"file '{name}' is specified in a SELECT "
                + "clause but has no file description entry (FD) or sort-merge file description entry (SD); ISO "
                + "§2023:12.4.5.2 SR3");
        }
    }

    /// <summary>Bind the FILE SECTION's FD records into the storage forest (they emit as Program fields, like
    /// WORKING-STORAGE), attach them to their <see cref="FileModel"/>, and model the shared record area: multiple
    /// <c>01</c>s under one FD occupy ONE area (ISO §2023:9.1.2), so each secondary record is synthesized as a REDEFINES of
    /// the first — the existing tier machinery then makes them alias one backing (the singular-pattern rule).</summary>
    internal void BindFileSection(Core.ProgramUnitContext program, CsNameScope rootNames)
    {
        var fs = program.dataDivision()?.fileSection();
        if (fs is null) return;
        foreach (var fd in fs.fileDescriptionEntry())
        {
            using var _ = Edition.At(fd);
            if (fd.fileName()?.GetText() is not { } name) continue;
            var records = _host.BindEntries(fd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!_host.State.Files.FilesByName.TryGetValue(name, out var file))
            {
                // ISO §2023:13.4.5.3 SR1 — "File-name-1 shall be specified in a file control entry." The model is still
                // built, so the entry's records resolve and the compile reports this one error rather than a
                // cascade of undefined names (kb/Work PB1237; it used to compile clean and run I-O against an
                // empty assignment).
                Edition.Error(DiagnosticCatalog.FileDescriptionWithoutSelect, $"file description entry '{name}' names "
                    + "a file that no file control entry specifies; ISO §2023:13.4.5.3 SR1: \"File-name-1 shall be "
                    + "specified in a file control entry.\"");
                file = new FileModel { CobolName = name, SelectName = name };
                _host.State.Files.Declare(name, file);
            }
            file.HasFd = true;
            file.Records.AddRange(records);
            // §2023:13.18.33.4 GR3: "Multiple level 1 entries subordinate to a FD or SD entry represent implicit
            // redefinitions of the same area" — an IMPLICIT redefinition, not a REDEFINES clause (kb/Work PB836).
            LinkImplicitRecordArea(file);
            // §2023:13.4.5.3 SR5/SR7/SR8: the entry's format is decided ONCE, before any clause binds — from the SELECT's
            // organization and whether a REPORT clause is written ANYWHERE in the entry (SR2: any order). A clause
            // its format does not contain is refused and NOT bound (kb/Work PB1238).
            var clauses = fd.fileDescriptionClauses()?.fileDescriptionClause() ?? [];
            var format = FileDescriptionFormats.Of(file.Organization, clauses.Any(c => c.reportClause() is not null));
            foreach (var clause in clauses)
                if (!FileDescriptionClauseAdmitted(clause, format, file, name))
                    continue;
                else if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, file);   // RECORD VARYING / m TO n → FileModel.Varying (ISO §2023:13.18.43)
                else if (clause.blockContainsClause() is { } bcc)
                    BindBlockContainsClause(bcc, name, file);   // ISO §2023:13.18.10 — the integers are screened; the values are what the 1985 agreement rule compares
                else if (clause.codeSetClause() is { } cs)
                    BindCodeSetClause(cs, file, records);   // ISO §2023:13.18.13 (kb/Work PB110)
                else if (clause.formatClause() is { } fmt)
                    // FORMAT clause (ISO §2023:13.18.24) — Annex A.4.8 item 1), an OPTIONAL element this
                    // implementation does not claim (docs/CONFORMANCE.md §5). Recognized so it can be REFUSED
                    // BY NAME rather than parse-erroring or, worse, binding inert: §2023:13.18.24.4 GR1 makes the
                    // clause change the on-medium representation, so an inert compile writes the wrong bytes.
                    // Every occurrence reports (this loop runs per clause per FD).
                    Edition.Declined(DiagnosticCatalog.FormatSelectWhenUnclaimed,
                        $"the FORMAT clause on file description entry '{name}' ({DataBinder.Spelled(fmt)})");
                else if (clause.linageClause() is { } lc)
                    BindLinageClause(lc, file);   // LINAGE logical-page model → FileModel.Linage (ISO §2023:13.18.34)
                else if (clause.dataRecordsClause() is { } drc)
                    ScreenDataRecords(drc, records, name);   // §1985:VII.3.5.3 SR1 (kb/Work PB5070)
                else if (clause.valueOfClause() is { } voc)
                    CaptureValueOf(voc, file);   // §1985:VII.3.9.3 SR1, SR2, screened post-build (kb/Work PB5070)
                else if (clause.labelRecordsClause() is { } lr)
                    CaptureLabelRecords(lr, file);   // §1985:VII.3.6.3 GR4, compared across an external connector's entries (kb/Work PB5300)
                else if (clause.reportClause() is { } rep)
                    // REPORT(S) clause (ISO §2023:13.18.46): the FD hosts these reports — a report FILE (§2023:9.1.22,
                    // legally record-less). Names resolve to ReportModels post-build (ResolveReports).
                    foreach (var rn in rep.reportName())
                    {
                        using var at = Edition.At(rn);   // the name's own position, where the correspondence rules report
                        file.ReportNames.Add(new ReportClauseName(rn.GetText(), Edition.Cursor));
                    }
                else if (clause.fileGlobalExternalClause() is { } ge)
                {
                    // FD IS EXTERNAL / IS GLOBAL (ISO §2023:13.18.22 / §2023:13.18.27.4 GR1): EXTERNAL ⇒ one run-unit file
                    // connector + external record data (GR4a/GR4b), externalized as the FD name (GR5); GLOBAL ⇒
                    // the file-name and record-names are global names, inherited by contained programs.
                    if (ge.EXTERNAL() is not null)
                    {
                        file.IsExternal = true;
                        // GR5's two sentences, in order: literal-1 when the AS phrase is written, the FD's own
                        // file-name otherwise (kb/Work PB511). The literal is a character-string, not a COBOL
                        // word, so its case is kept, while the file-name is written in its one upper-case spelling
                        // (CobolNames.UpperFold: §2023:8.1.3.2's Annex C fold, kb/Work PB1965). §2023:13.18.22.3 SR3 screens it through the ONE shared
                        // externalized-name screen (COBOL2156), which also FORMS it — leading and trailing
                        // spaces removed (DOC-A.1-68, kb/Work PB1539).
                        file.ExternalName =
                            (ge.externalizedNamePhrase() is { } asPhrase
                                ? ExternalizedName.Screen(asPhrase, _host.ReadConstant, Edition,
                                    DiagnosticCatalog.ExternalClauseAsLiteral,
                                    $"file '{name}' IS EXTERNAL AS {ExternalizedName.Written(asPhrase)}", "literal-1",
                                    "ISO §2023:13.18.22.3 SR3", _host.LiteralEnv, rejectZeroLength: true)
                                : null)
                            ?? CobolNames.UpperFold(name);
                    }
                    if (ge.GLOBAL() is not null) file.IsGlobal = true;
                }
            // §2023:13.4.5.3 SR8 — "Format 3 is the file description entry for a report file. No record description
            // entries or constant entries shall be associated with the file description entry for a report file."
            // Asked after the clause loop because the REPORT clause may follow any other clause (kb/Work PB1171:
            // such a record used to compile, and a WRITE of it injected a line into the report output).
            if (file.IsReportFile && fd.dataDescriptionEntry() is { Length: > 0 } entries)
                Edition.Error(DiagnosticCatalog.ReportFileRecordEntry, $"file description entry '{name}' specifies a "
                    + $"REPORT clause and has {entries.Length} record description or constant "
                    + $"entr{(entries.Length == 1 ? "y" : "ies")} subordinate to it — ISO §2023:13.4.5.3 SR8: \"No record "
                    + "description entries or constant entries shall be associated with the file description entry "
                    + "for a report file.\" Remove them; the report's lines come from its REPORT SECTION description.");
            // §2023:13.16.3 SR7's FD half — every WRITTEN record of an EXTERNAL / GLOBAL FD has a data-name
            // (DataBinder.ClausePlacement.cs; kb/Work PB518). Before the implied record, which is not written.
            ScreenFileRecordEntryNames(file);
            // ⛔ LAST, because it reads the RECORD, REPORT(S) and (via the SELECT) ORGANIZATION clauses: a file
            // description entry with NO record description entries still has a record area, and it is created
            // HERE rather than worked around at each of its consumers (kb/Work PB345).
            if (file.Records.Count == 0) MaterializeImpliedRecord(file, name, rootNames);
        }

        // SD entries (ISO §2023:13.4.6): a sort-merge file's records bind through the SAME entry path as FD records —
        // they emit as Program fields and multi-01 records share ONE area (synthesized REDEFINES, ISO §2023:9.1.2).
        // The SD format admits only the record clause (§2023:13.4.6); DATA RECORDS is an obsolete '85 element DELETED
        // by ISO/IEC 1989:2002 — accepted-inert at 85 (every NIST SD writes it), rejected ≥2002.
        foreach (var sd in fs.sortMergeDescriptionEntry())
        {
            using var _ = Edition.At(sd);
            if (sd.fileName()?.GetText() is not { } sdName) continue;
            var sdRecords = _host.BindEntries(sd.dataDescriptionEntry(), rootNames, EntrySection.File);
            if (!_host.State.Files.FilesByName.TryGetValue(sdName, out var sdFile))
            {
                // ISO §2023:13.4.6.3 SR1 — the SD twin of the FD arm above (kb/Work PB1290).
                Edition.Error(DiagnosticCatalog.FileDescriptionWithoutSelect, $"sort-merge file description entry "
                    + $"'{sdName}' names a file that no file control entry specifies; ISO §2023:13.4.6.3 SR1: \"File-name-1 "
                    + "shall be specified in a file control entry.\"");
                sdFile = new FileModel { CobolName = sdName, SelectName = sdName };
                _host.State.Files.Declare(sdName, sdFile);
            }
            sdFile.HasFd = true;
            sdFile.IsSortMerge = true;   // referenced only by SORT/MERGE/RELEASE/RETURN (§2023:13.4.6 SR3/SR4)
            // ⛔ The SD arm does NOT inherit §2023:13.4.5.3 SR3's permission to omit the record descriptions — that is a
            // FORMATS 1 AND 2 rule, and §2023:13.4.6.3 SR2 requires them here. Until kb/Work PB345 a record-less SD
            // compiled and its SORT announced itself a COMPILER GAP (COBOL1756, "SORT 'S1' without an SD
            // record — not implemented") before aborting the run unit: the compiler apologising for the source's
            // error. It reads THIS entry's own records, so it needs nothing the clause loop below sets — unlike
            // the FD arm's MaterializeImpliedRecord, which reads the RECORD and REPORT(S) clauses and runs last.
            if (sdRecords.Count == 0)
                Edition.Error(DiagnosticCatalog.FileDescriptionRecordRequired, $"sort-merge file description entry "
                    + $"'{sdName}': one or more record description entries shall be associated with the sort-merge "
                    + "file description entry (ISO §2023:13.4.6.3 SR2) — a SORT or MERGE key is a data item within a "
                    + "record of this file (§2023:14.9.40.3 SR6 a), so there is nothing for the KEY phrase to name.");
            sdFile.Records.AddRange(sdRecords);
            LinkImplicitRecordArea(sdFile);   // §2023:13.18.33.4 GR3 — the SD twin of the FD arm (kb/Work PB836)
            foreach (var clause in sd.sortMergeDescriptionClauses()?.sortMergeDescriptionClause() ?? [])
            {
                if (clause.recordClause() is { } rc)
                    BindRecordClause(rc, sdFile);
                else if (clause.dataRecordsClause() is { } drc)
                    ScreenDataRecords(drc, sdRecords, sdName);   // §1985:VII.3.5.3 SR1, the SD's own records (kb/Work PB5070)
                // (The SD DATA RECORDS 0873 gate MIGRATED to the version-conformance pass parse-arm (VisitDataRecordsClause) — one
                // enforcement site covering FD AND SD via the shared grammar rule; P2.6 / Table-7 row 7.1.)
            }
        }
    }

    /// <summary>Whether one written FD clause belongs in this entry, reporting it when it does not (kb/Work PB1238).
    /// Two rules, both keyed on facts decided before the clause loop: the §2023:13.4.5.2 format's clause list
    /// (<see cref="FileDescriptionFormats"/>, COBOL2604 — including the file control entry's clauses, which no
    /// FD format contains) and §2023:13.4.5.3 SR4's LINE SEQUENTIAL ban on BLOCK CONTAINS and RECORD CONTAINS
    /// (COBOL2605). The <c>unrecognizedDataClause</c> error production has no row and is admitted here: its own pass
    /// refuses it by name (COBOL1970).</summary>
    private bool FileDescriptionClauseAdmitted(
        Core.FileDescriptionClauseContext clause, FileDescriptionFormat format, FileModel file, string fdName)
    {
        if (FileDescriptionFormats.RowOf(clause) is not { } row) return true;
        using var _ = Edition.At(clause);
        if (row.HomeEntry is { } home)
        {
            Edition.Error(DiagnosticCatalog.FileDescriptionClauseFormat, $"the {row.Name} ({DataBinder.WrittenText(clause)}) is "
                + $"written in the file description entry for '{fdName}', but it is a clause of {home}, and no ISO "
                + "§2023:13.4.5.2 file description format contains it — write it in the SELECT entry for "
                + $"'{fdName}'. It is not bound here, so it has no effect on the file.");
            return false;
        }
        if ((row.Formats & format) == 0)
        {
            Edition.Error(DiagnosticCatalog.FileDescriptionClauseFormat, $"the {row.Name} ({DataBinder.WrittenText(clause)}) is not "
                + $"a clause of {FileDescriptionFormats.Caption(format)} of the file description entry: file "
                + $"'{fdName}' is {file.OrganizationFace}"
                + (format == FileDescriptionFormat.Report ? ", and its entry specifies a REPORT clause" : "")
                + $", and {FileDescriptionFormats.BindingRule(format)}. §2023:13.4.5.2 prints that format with only "
                + $"the {FileDescriptionFormats.Admitted(format)}.");
            return false;
        }
        // §2023:13.4.5.3 SR4 (FORMATS 1 AND 3 — the formats a LINE SEQUENTIAL file can be written in): "neither the
        // BLOCK CONTAINS clause nor the RECORD CONTAINS clause shall be specified". RECORD CONTAINS is the record
        // clause's Format 1 and Format 3 (§2023:13.18.43.2, the arms spelled RECORD CONTAINS); the VARYING arm stays
        // legal, as §2023:14.9.51.4 GR22 describes a line sequential file whose RECORD clause has the DEPENDING phrase.
        if (file.Organization == FileOrganization.LineSequential
            && (clause.blockContainsClause() is not null || clause.recordClause() is { } rc && rc.VARYING() is null))
        {
            Edition.Error(DiagnosticCatalog.LineSequentialBlockOrRecordContains, $"file '{fdName}' is LINE SEQUENTIAL "
                + $"and its file description entry specifies {DataBinder.WrittenText(clause)} — ISO §2023:13.4.5.3 SR4: \"If the LINE "
                + "SEQUENTIAL phrase of the ORGANIZATION clause … is specified neither the BLOCK CONTAINS clause nor "
                + "the RECORD CONTAINS clause shall be specified.\" A line sequential record ends at its line "
                + "delimiter; remove the clause (RECORD IS VARYING remains available).");
            return false;
        }
        return true;
    }

    /// <summary>ISO §2023:14.9.30.4 GR6's IMPLIED record description, made a REAL record at bind time (kb/Work PB345).
    /// <para>§2023:13.4.5.3 SR3 expressly contemplates a file description entry with no record description entries —
    /// "When no record description entries are specified: a) a RECORD clause shall be specified …" — and SR7
    /// confines the <i>one or more record description entries</i> requirement to INDEXED files, so a sequential or
    /// relative FD written without a level-01 is legal source. GR6 then says what its record area IS: a READ INTO
    /// on such a file "proceeds as though there were one record description entry describing an alphanumeric group
    /// item of the maximum size established by the RECORD clause". This method materializes exactly that entry —
    /// an unnamed level-01 group over one FILLER of the RECORD clause's maximum size.</para>
    /// <para>⛔ IT IS SYNTHESIZED HERE, NOT WORKED AROUND AT THE CONSUMERS, and that is the whole fix. Before it,
    /// <see cref="FileModel.AreaRecord"/> answered null and FIVE consumers each carried their own null arm:
    /// <c>SequentialIoEmitter.EmitFileRegistration</c> registered the connector only when the FD hosted a REPORT,
    /// <c>KeyedIoEmitter.EmitRegistration</c> returned outright, and the READ record-area store and implicit INTO
    /// move were both guarded on it — so every I-O verb on a legal record-description-less file either aborted the
    /// run unit ("file connector … was never registered") or, had registration alone been repaired, would have
    /// read '00' and left identifier-1 unchanged. With the record real, registration, the area store and the INTO
    /// move all fall out of the ordinary path, for BOTH organizations, from ONE place.</para>
    /// <para>A GROUP and not an elementary PIC X(n): the distinction is observable. §2023:14.9.25.4 makes a MOVE whose
    /// sending operand is a group item a group (alphanumeric) move, so <c>READ … INTO</c> a numeric receiver copies
    /// bytes; an elementary alphanumeric sender would instead convert. GR6 says "group item", so a group it is.</para>
    /// <para>The record is kept OFF <see cref="ForestSlice.ByName"/>: the entry is IMPLIED, so it has no record-name and the
    /// program cannot reference it (which is also why §2023:13.4.5.3 SR3 b/c require the FILE … FROM and INTO phrases on
    /// the verbs). It IS on <see cref="ForestSlice.Roots"/>, because it is real storage that has to emit.</para></summary>
    private void MaterializeImpliedRecord(FileModel file, string fdName, CsNameScope rootNames)
    {
        // §2023:13.4.5.3 SR8: "No record description entries or constant entries shall be associated with the file
        // description entry for a report file." — a report file's area is the report engine's line, and
        // §2023:13.18.43.3 SR1 excludes it from the RECORD-clause requirement by name. Nothing to synthesize.
        if (file.IsReportFile) return;
        // §2023:13.4.5.3 SR7 withdraws SR3's permission for an INDEXED file. Rejecting is the whole repair: a prime
        // RECORD KEY has to be locatable in a record image (§2023:12.4.5.12.3 SR2), which is why the emitter's
        // fallback for an unlocatable key was a run-time abort — the compiler announcing the SOURCE's error.
        if (file.Organization == FileOrganization.Indexed)
        {
            Edition.Error(DiagnosticCatalog.FileDescriptionRecordRequired, $"file description entry '{fdName}': "
                + "for an indexed file, one or more record description entries shall be associated with the file "
                + "description entry (ISO §2023:13.4.5.3 SR7) — §2023:13.4.5.3 SR3's permission to omit them is withdrawn "
                + "here, because the prime RECORD KEY shall reference a data item within a record description "
                + "entry associated with this file (§2023:12.4.5.12.3 SR2).");
            return;
        }
        // "the maximum size established by the RECORD clause" (§2023:14.9.30.4 GR6): format 1's integer-1, format 3's
        // integer-5, or format 2's integer-3. A format-2 clause with no TO phrase establishes none — §2023:13.18.43.4
        // GR10 defers to "the greatest number of bytes described for a record in that file" and this file
        // describes none — and neither does an absent clause, which §2023:13.4.5.3 SR3 a) / §2023:13.18.43.3 SR1 forbid.
        int? max = file.RecordClause?.Upper;   // the one reading of the clause's upper operand, in all three formats (PB2519)
        if (max is not > 0)
        {
            Edition.Error(DiagnosticCatalog.RecordLessFdNoRecordSize, $"file description entry '{fdName}': it "
                + "specifies no record description entries, so a RECORD clause establishing a maximum record size "
                + "shall be specified (ISO §2023:13.4.5.3 SR3 a / §2023:13.18.43.3 SR1) — §2023:14.9.30.4 GR6 sizes this file's "
                + "implied record description by \"the maximum size established by the RECORD clause\", and "
                + $"{(max is null ? "no clause here establishes one" : $"the maximum established here is {max}")}. "
                + "Write RECORD CONTAINS n CHARACTERS, or RECORD IS VARYING IN SIZE TO n CHARACTERS.");
            return;
        }
        var record = new DataItem
        {
            Level = 1,
            DeclaredAt = Edition.Cursor,
            CobolName = null,   // implied, so unnamed and unreferenceable — never RegisterName'd
            ClrName = CsNames.Allocate($"_impliedRecord{DataItem.Sanitize(fdName)}", rootNames),
        };
        record.Uid = _host.TakeUid();
        record.RootSection = EntrySection.File;   // §2023:14.9.30.4 GR6's implied record is a FILE SECTION record
        var area = new DataItem
        {
            Level = 2,
            DeclaredAt = Edition.Cursor,
            CobolName = null,
            ClrName = $"_filler{_host.TakeFillerOrdinal()}",
            Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: max.Value, Digits: 0, Scale: 0,
                Signed: false),
            Parent = record,
        };
        area.Uid = _host.TakeUid();
        record.AddMember(area);
        _host.State.Forest.AddRoot(record);
        file.Records.Add(record);
        file.ImpliedRecord = record;   // real storage, but no record description entry: WrittenRecords leaves it out (kb/Work PB1219)
    }

    /// <summary>Bind a RECORD clause's variable-length forms into <see cref="FileModel.Varying"/> (ISO §2023:13.18.43:
    /// <c>RECORD IS VARYING [FROM m] [TO n] [DEPENDING ON d]</c> and <c>RECORD CONTAINS m TO n</c> describe
    /// variable-length records; the fixed Format-1 <c>RECORD CONTAINS n</c> leaves it null). Shared by the FD and
    /// SD loops — ONE binding for the clause. The DEPENDING operand is a qualified-data-name like every other
    /// clause operand (<see cref="DataBinder.ClauseDataName"/>) and resolves post-build in <see cref="ResolveFiles"/>; it
    /// kept only the base word until kb/Work PB489, so a qualified one bound another item of that name.</summary>
    private void BindRecordClause(Core.RecordClauseContext rc, FileModel file)
    {
        // ⛔ The clause's OWN position, captured here for §2023:13.18.43.3's syntax rules, which are screened
        // post-build by RecordClauseRules (SR3/SR4 compare against §2023:13.18.43.4 GR8 byte counts over a forest
        // that is not complete yet). The RecordKeyAt pattern; set for EVERY format, on BOTH the FD and SD arms,
        // because it is set HERE and not at the two call sites.
        using var _ = Edition.At(rc);
        file.RecordClauseAt = Edition.Cursor;
        string recWhere = $"file '{file.CobolName}' RECORD clause";
        if (rc.VARYING() is null && rc.TO() is null)
        {
            // The fixed Format-1 RECORD CONTAINS integer-1 (ISO §2023:13.18.43.2). It SIZES THE FILE — §2023:13.18.43.4
            // GR6: "Integer-1 specifies the number of bytes contained in each record in the file" — through
            // FileModel.RecordWidth, for an FD and an SD alike (kb/Work PB1276), and a report file's line width
            // prefers it (REPORT_WRITER_DESIGN §4).
            // A constant-name stands for integer-1 (§2023:13.10.3 SR2) and is read by the ONE integer-n reader; a refused
            // one is reported there and leaves the width unset, so it cascades into no size diagnostic.
            if (rc.integerOperand() is { Length: > 0 } fixedOps)
                file.RecordContains = _host.Constants.IntegerOperandValue(fixedOps[0], recWhere);
            return;
        }
        var lits = rc.integerOperand();
        int? lo = lits.Length > 0 ? _host.Constants.IntegerOperandValue(lits[0], recWhere) : null;
        int? hi = lits.Length > 1 ? _host.Constants.IntegerOperandValue(lits[1], recWhere) : null;
        if (rc.TO() is not null && lits.Length == 1) { hi = lo; lo = null; }
        string? dep = null;
        IReadOnlyList<string> depQuals = [];
        Editions.DiagnosticCursor depAt = default;
        if (rc.dataReference() is { } d)
        {
            (dep, depQuals) = _host.ClauseDataName(d, "RECORD DEPENDING ON");
            // The OPERAND's own position, inside the clause cursor kb/Work PB721 pushed at the method top.
            using var _dep = Edition.At(d);
            depAt = Edition.Cursor;
        }
        // FORMAT 2 vs FORMAT 3: the WORD VARYING is the discriminator, and it is load-bearing — §2023:13.18.43.4 GR16
        // (the READ/RETURN INTO sending size) sits under the FORMAT 2 heading and §2023:14.9.30.4 GR4 b) designates
        // the implied move an alphanumeric group move only for a RECORD IS VARYING clause (kb/Work PB339).
        bool varyingClause = rc.VARYING() is not null;
        file.Varying = new VaryingRecordInfo(lo, hi, dep, VaryingClause: varyingClause)
        {
            DependingQualifiers = depQuals, DependingAt = depAt,
            // Format 3's integers bound a WRITE/REWRITE only where the edition states GR19 (2002 on) (kb/Work PB5197).
            BoundsEnforced = varyingClause
                || DialectBehaviors.IsActive(DialectBehavior.RecordContainsRangeBindsRecordSize, Edition.DialectLevel),
        };
    }

    /// <summary>Screen a BLOCK CONTAINS clause's integers (ISO §2023:13.18.10.2: <c>BLOCK CONTAINS [integer-1 TO] integer-2
    /// {CHARACTERS | RECORDS}</c>). The clause models nothing — the physical block is implementor-defined — but each
    /// <c>integer-n</c> is an operand a constant-name may stand for (§2023:13.10.3 SR2), and a constant that is not an integer,
    /// is zero or is undefined is the program's error whether or not the clause changes the I-O, so each goes through
    /// THE integer-n reader like every other position (kb/Work PB1948). The pair is ORDERED: §2023:13.18.10.3 SR1 (kb/Work PB1217).</summary>
    private void BindBlockContainsClause(Core.BlockContainsClauseContext bc, string fileName, FileModel file)
    {
        using var _ = Edition.At(bc);
        var values = new List<long?>();
        foreach (var operand in bc.integerOperand())
            values.Add(_host.Constants.IntegerOperandValue(operand, $"file '{fileName}' BLOCK CONTAINS clause"));
        // ⛔ §2023:13.18.10.3 SR1 — "If integer-1 is specified, integer-2 shall be greater than integer-1." The ORDERED-PAIR
        // rule RECORD CONTAINS / RECORD IS VARYING already states for their pairs (RecordClauseRules, SR5 / SR9), under
        // the same code: equal bounds break it as surely as inverted ones (kb/Work PB1217). An operand whose reading was
        // refused (null) is already reported, so the pair is judged only when both are known.
        if (values is [{ } integer1, { } integer2] && integer2 <= integer1)
            Edition.Error(DiagnosticCatalog.FileClauseSizeRange, $"file '{fileName}': the BLOCK CONTAINS clause states "
                + $"{integer1} TO {integer2}; integer-2 shall be greater than integer-1 (ISO §2023:13.18.10.3 SR1)");
        // §1985:VII.3.3.3 GR5: an external connector's BLOCK CONTAINS clauses "must have the same value for integer-1 and integer-2" (kb/Work PB5300).
        if (values.All(v => v is not null))
            file.FdClauses[FileDescriptionIdentity.Block] = new(string.Join("..", values), Edition.Cursor);
    }

    /// <summary>Bind a LINAGE clause into <see cref="FileModel.Linage"/> (ISO §2023:13.18.34: <c>LINAGE IS
    /// {data-name-1 | integer-1} LINES [WITH FOOTING AT {data-name-2 | integer-2}] [LINES AT TOP
    /// {data-name-3 | integer-3}] [LINES AT BOTTOM {data-name-4 | integer-4}]</c>). Each operand is a fixed
    /// literal (GR6a) or a data-name (GR6b — read at the evaluation points), resolved post-build by
    /// <see cref="ResolveLinage"/>. Absent FOOTING/TOP/BOTTOM phrases stay null (GR1 — margins zero; no footing
    /// ⇒ no end-of-page condition independent of page overflow).
    /// <para>⛔ A DATA-NAME OPERAND KEEPS ITS WHOLE WRITTEN REFERENCE. It used to keep
    /// <c>d.cobolWord()?.GetText()</c> — the FIRST word — "the FILE STATUS capture pattern", which is wrong
    /// wherever the name can be QUALIFIED: §2023:13.18.34.2 prints <i>data-name-1</i>, a qualified-data-name
    /// (§2023:8.4.2.2.2 Format 1), so <c>LINAGE IS SZ OF GRP-B LINES</c> is legal and — with two SZs — the only legal
    /// spelling, and the IN/OF qualifier was thrown away and the first same-named item bound instead. The whole
    /// logical page was then built on another item's value with no diagnostic at any stage (kb/Work PB489,
    /// measured: EOP / LINAGE-COUNTER 1 where the standard requires NO-EOP / 5). For the special-register
    /// alternative the first cobolWord is the FILE-NAME QUALIFIER, so the clause recorded the file name as its
    /// data-name; <see cref="DataBinder.KeyReference"/> refuses that spelling outright.</para></summary>
    private void BindLinageClause(Core.LinageClauseContext lc, FileModel file)
    {
        LinageOperand Operand(Core.DataReferenceContext? d, Core.IntegerLiteralContext? i)
        {
            if (i is not null)
            {
                // A literal operand keeps its cursor too: §2023:13.18.34.3 SR3 reports AT integer-2 (ResolveLinage).
                using var lit = Edition.At(i);
                return new LinageOperand(WiseOwl.Cobol.Validation.IntegerOperandRules.HostValue(i), null) { At = Edition.Cursor };
            }
            // "data-name-1 or integer-1": a bare word that names a CONSTANT is the integer-1 arm — the parse cannot tell
            // it from a data-name (§2023:13.10.3 SR2; kb/Work PB1948) — and the constant's integer is a literal operand
            // from here on (§2023:13.18.34.3 SR3 and GR6a read it as the written literal).
            if (_host.Constants.ConstantOf(d!) is not null)
            {
                using var cst = Edition.At(d!);
                return new LinageOperand(
                    _host.Constants.ConstantOperandValue(d!, "LINAGE clause operand") ?? ConstantEntryBinder.RecoveredIntegerOperand, null) { At = Edition.Cursor };
            }
            var (name, quals) = _host.ClauseDataName(d!, "LINAGE clause operand");
            using var _ = Edition.At(d!);
            return new LinageOperand(null, name) { Qualifiers = quals, At = Edition.Cursor };
        }
        file.Linage = new LinageInfo(
            Operand(lc.dataReference(), lc.integerLiteral()),
            lc.linageFootingPhrase() is { } f ? Operand(f.dataReference(), f.integerLiteral()) : null,
            lc.linageLinesAtTopPhrase() is { } t ? Operand(t.dataReference(), t.integerLiteral()) : null,
            lc.linageLinesAtBottomPhrase() is { } b ? Operand(b.dataReference(), b.integerLiteral()) : null);
    }

    /// <summary>⛔ THE ONE LINKER of a file's implicitly shared record area — §2023:13.18.33.4 GR3: "Multiple level 1
    /// entries subordinate to a FD or SD entry represent implicit redefinitions of the same area". Every record
    /// with a CHARACTER WINDOW becomes an implicit redefinition of the first such record (the FD and SD arms used
    /// to spell this loop twice). An OUT-OF-LINE record (<see cref="FileModel.IsOutOfLineRecord"/> — a
    /// dynamic-length record, a variable-length group, a pointer-class record) is NOT linked: it has no window
    /// over the character area (§2023:8.5.1.10.3 lets its storage be "located elsewhere"; a pointer has no character
    /// image, CONFORMANCE.md A.1 item 216), and it shares the area at the transfer boundary instead
    /// (<see cref="FileModel.OutOfLineRecords"/> — determination D-FRA, kb/Work PB981). Linking it is what used to
    /// stage such an FD loud as COBOL0899: the class model has no shape for a member without a window.</summary>
    private static void LinkImplicitRecordArea(FileModel file)
    {
        if (file.CharacterAnchor is not { } anchor) return;
        foreach (var record in file.Records)
            if (!ReferenceEquals(record, anchor) && record.RedefinesTarget is null
                && !FileModel.IsOutOfLineRecord(record))
                record.SetRedefinition(anchor, RedefinitionKind.ImplicitFileRecord);
    }

    /// <summary>Bind an FD CODE-SET clause (ISO §2023:13.18.13; kb/Work PB110 — it was parsed as the '85 one-name form
    /// and read by nothing): both formats — <c>IS alphabet-name-1 [alphabet-name-2]</c> and the FOR ALPHANUMERIC /
    /// FOR NATIONAL phrases (one or both, any order, each at most once — §2023:5.2.6.4). SR1/SR2: each name shall
    /// reference an alphabet defining a coded character set of its class (a LOCALE alphabet is COBOL1669 through
    /// the ONE resolver; a class mismatch or an undeclared name is COBOL1672). SR3: with record description
    /// entries and no SELECT WHEN, one class only, every elementary item of that usage, signed items SIGN SEPARATE.
    /// GR2/GR6: the on-medium coded character set, decided by <see cref="CodedCharacterSet.Medium"/> — the ONE
    /// classifier, so this binder names no set. <c>Identity</c> (NATIVE; UTF-16 on the D-N1 substrate — the
    /// whole-channel identity sets) converts as the identity and is CLAIMED, and nothing is emitted for it.
    /// <c>Translated</c> (an implementor code-name whose GR7 i correspondence with the native set is a
    /// single-byte code — EBCDIC, kb/Work PB793 — and the ISO/IEC 646 sets STANDARD-1/2 (GR7 c) and the ASCII
    /// code-name, whose identity correspondence covers only 128 characters, kb/Work PB1542) is CLAIMED and
    /// carried to the connector, which applies GR6 a/b at the medium boundary and refuses, at the WRITE or
    /// REWRITE, a record character outside the set. <c>NotProvided</c> (a literal-phrase
    /// alphabet's remapped ordinals; UTF-8 / UCS-4 as variable-width medium encodings) is the DOCUMENTED A.3
    /// item 27 non-support (COBOL1672 — "dependent upon a device capable of supporting the specified code";
    /// CONFORMANCE.md §2 row 27), never a silent identity.</summary>
    private void BindCodeSetClause(Core.CodeSetClauseContext cs, FileModel file, IReadOnlyList<DataItem> records)
    {
        using var _ = Edition.At(cs);
        var (alnumName, natName) = ChoiceIndicators.AlphabetPair(Edition, cs.codeSetForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord().GetText(), cs.cobolWord(),
            $"file '{file.CobolName}' CODE-SET", "13.18.13.2");
        var alnumSet = alnumName is null ? null : _host.SpecialNames.CodedCharacterSetOf(cs, alnumName, $"CODE-SET {alnumName}",
            "ISO §2023:13.18.13.3 SR1 — alphabet-name-1 shall reference an alphabet that defines an alphanumeric coded character set");
        var natSet = natName is null ? null : _host.SpecialNames.CodedCharacterSetOf(cs, natName, $"CODE-SET {natName}",
            "ISO §2023:13.18.13.3 SR2 — alphabet-name-2 shall reference an alphabet that defines a national coded character set");
        if (alnumName is not null && alnumSet is null || natName is not null && natSet is null) return;
        if (alnumSet is { National: true })
        {
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET {alnumName}: alphabet-name-1 shall "
                + "reference an alphabet that defines an ALPHANUMERIC coded character set — this alphabet is defined "
                + "FOR NATIONAL (ISO §2023:13.18.13.3 SR1)");
            return;
        }
        if (natSet is { National: false })
        {
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET {natName}: alphabet-name-2 shall "
                + "reference an alphabet that defines a NATIONAL coded character set — this alphabet is alphanumeric "
                + "(ISO §2023:13.18.13.3 SR2)");
            return;
        }
        // SR3 — record description entries and no SELECT WHEN (none exists in this grammar): one class only.
        if (records.Count > 0 && alnumName is not null && natName is not null)
            Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, "CODE-SET: if any record description entries are "
                + "associated with the file and no SELECT WHEN clauses are specified, either alphabet-name-1 or "
                + "alphabet-name-2 may be specified, but not both (ISO §2023:13.18.13.3 SR3)");
        // GR2/GR6 — which conversion the set asks for. ONE question, asked of the SET (CodedCharacterSet.Medium)
        // rather than answered from a list of phrase names here: an identity correspondence over the whole
        // one-byte channel converts byte-for-byte and needs nothing; a single-byte code whose correspondence this
        // processor has (§2023:12.3.7.4 GR7 c / GR7 i — including ISO/IEC 646's 128-character identity, which the
        // connector needs in order to REFUSE what the set lacks, kb/Work PB1542) is carried to the connector,
        // which replaces each character per GR6 a/b; a set whose
        // medium form is neither is the documented A.3 item 27 non-support — refused loudly, never a silent
        // identity (that would be a wrong answer for an EBCDIC-shaped alphabet).
        foreach (var (set, name) in new[] { (alnumSet, alnumName), (natSet, natName) })
        {
            if (set is null) continue;
            if (set.Medium is CodeSetMedium.NotProvided)
                Edition.Error(DiagnosticCatalog.CodeSetClauseViolation, $"CODE-SET {name}: the {set.Phrase} coded "
                    + "character set's on-medium representation differs from the native encoding, and this processor "
                    + "does not provide alternate device code sets (Annex A §2023:A.3 item 27 — documented non-support, "
                    + "CONFORMANCE.md §2 row 27); NATIVE, STANDARD-1, STANDARD-2, UTF-16 and the ASCII code-name "
                    + "convert as the identity, and the EBCDIC code-name converts through its GR7 i correspondence");
            else if (set.Medium is CodeSetMedium.Translated)
                // GR1/GR2 — the alphabet the medium's data is represented in, from the successful OPEN onwards.
                file.CodeSetCorrespondence = set.MediumCorrespondence;
        }
        // SR3 a/b — the selected class's usage over every elementary record item; signed numeric SIGN SEPARATE.
        // ⛔ DEFERRED, not written here (kb/Work PB536): both sentences ask what an item IS DESCRIBED WITH, and
        // a group-level SIGN (§2023:13.18.52.4 GR1) / USAGE (§2023:13.18.60.4 GR1) clause reaches the leaf only in a later
        // pass, so a screen run from this FD walk refused the legal `05 G SIGN IS LEADING SEPARATE. 10 N PIC
        // S9(4).`. The CLASS the clause selected is all that travels; the screen is the declared pass
        // CheckCodeSetRecordItemsPass, at PassPhase.SignResolved.
        if (alnumName is not null || natName is not null)
            file.CodeSetRecordClass = natName is not null;
        // §1985:VII.3.4.4 GR3: an external connector's CODE-SET clauses "must have the same character set": the SET each name denotes (a name is the program's
        // own and two programs may name one set differently), not the name (kb/Work PB5300).
        if (alnumSet is not null || natSet is not null)
            file.FdClauses[FileDescriptionIdentity.CodeSet] = new($"{alnumSet?.Phrase}/{natSet?.Phrase}", Edition.Cursor);
    }

    /// <summary>§2023:13.16.3 SR7's SECOND half — "…or for record descriptions associated with a file description entry
    /// that contains the EXTERNAL or GLOBAL clause" — the one row whose subject is the FD rather than the entry.
    /// Called once per FD after its clause loop, when <see cref="FileModel.IsExternal"/> / <c>IsGlobal</c> are
    /// known and before an implied record is materialized (that record is the compiler's, not written).</summary>
    private void ScreenFileRecordEntryNames(FileModel file)
    {
        if (!file.IsExternal && !file.IsGlobal) return;
        var rule = ClausePlacementRules.Rules.First(r => r.Kind is ClausePlacementKind.DataNameRequired);
        string clause = file.IsExternal ? (file.IsGlobal ? "EXTERNAL and GLOBAL" : "EXTERNAL") : "GLOBAL";
        foreach (var record in file.Records)
        {
            if (record.CobolName is not null) continue;
            using var _ = Edition.At(record);
            Edition.Error(rule.Code, $"a record description entry of file '{file.CobolName}' has no data-name, and "
                + $"the file description entry contains the {clause} clause. \"{rule.Sentence}\" (ISO {rule.Rule})");
        }
    }
}
