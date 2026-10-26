// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using CobolNet.Runtime;
using Core = CobolParserCore;

/// <summary>
/// The I-O-CONTROL paragraph (ISO §12.4.6): the SAME clause's membership model, its syntax rules
/// (§12.4.6.4.3 SR2-SR10) and the record-area sharing it links (§12.4.6.4.4 GR2).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Bind the I-O-CONTROL paragraph (ISO §12.4.6). A record-area SAME clause (Format 2) makes the listed
    /// files "share a memory area for processing the current logical record … equivalent to an implicit redefinition
    /// of the area with records aligned on the leftmost byte position" (§12.4.6.4 GR2) — modeled by chaining each
    /// listed file's FIRST record as a synthesized REDEFINES of the first LISTED file's first record, exactly the
    /// multi-01-under-one-FD mechanism (the singular-pattern rule): the tier machinery then aliases every record of
    /// every listed file over ONE backing, and READ/WRITE/RELEASE image distribution gives the
    /// record-of-the-most-recently-read-file semantics for free. A sort/merge file may appear in a record-area
    /// clause (SR6 — ST131A's <c>READ FILE3</c> then <c>RELEASE S3</c> with no FROM relies on it). The file-area
    /// (Format 1) and sort-merge-area (Format 3) formats are storage-economy permissions (GR1/GR4 — shared/reusable
    /// ALLOCATION plus open-mode constraints on the program, nothing a typed-native runtime must alias) — bound as
    /// conformant no-ops; MULTIPLE FILE TAPE is obsolete and parsed-and-ignored (grammar note), and so is the
    /// X3.23-1985 RERUN clause (a checkpoint HINT with no program-visible effect — a null rerun facility is
    /// conforming; deleted by ISO 2002, 0902-gated ≥2002 by the version-conformance pass, VCR Table 7 row 7.15) —
    /// both skip through the non-SAME <c>continue</c> below by design. The APPLY COMMIT clause (§12.4.6.3) is the one
    /// other arm that is not a no-op here: <see cref="BindApplyCommitClause"/> (kb/Work PB3338).
    /// EVERY SAME format also records its MEMBERSHIP on its files
    /// (<see cref="FileModel.SameClauses"/>, kb/Work PB1139), which the SORT and MERGE statements read for
    /// §14.9.40.3 SR10 / §14.9.24.3 SR11 (<c>SortBinder.ScreenSameClauses</c>). The clause's OWN syntax rules —
    /// the arity its format prints and §12.4.6.4.3 SR2, SR3 and SR5-SR10 — are <see cref="ScreenSameOperands"/> and
    /// <see cref="ScreenSameClauses"/> (kb/Work PB1087).</summary>
    private void BindIoControl(Core.ProgramUnitContext program)
    {
        var io = program.environmentDivision()?.inputOutputSection()?.ioControlParagraph();
        if (io is null) return;
        var sites = new List<SameSite>();
        foreach (var clause in io.ioControlClause())
        {
            using var _ = Edition.At(clause);
            if (clause.applyCommitClause() is { } apply)
            {
                BindApplyCommitClause(apply);
                continue;
            }
            if (clause.sameClause() is not { } same) continue;
            // ⛔ EVERY SAME FORMAT IS RECORDED ON ITS FILES (kb/Work PB1139): the clause's membership is what
            // §14.9.40.3 SR10 and §14.9.24.3 SR11 ask the SORT and MERGE statements about — "No pair of file-names in
            // the same SORT statement may be specified in the same SAME SORT AREA or SAME SORT-MERGE AREA clause" —
            // and until then only the RECORD AREA format was modelled (as a peer list), so those rules had nothing to read.
            // §12.4.6.4.3 SR1: SORT and SORT-MERGE are equivalent — ONE format, the written words kept for messages.
            var kind = same.RECORD() is not null ? SameClauseKind.RecordArea
                : same.SORT_MERGE() is not null || same.SORT() is not null ? SameClauseKind.SortMergeArea
                : SameClauseKind.Area;
            string written = "SAME" + (same.RECORD() is not null ? " RECORD" : same.SORT_MERGE() is not null ? " SORT-MERGE"
                : same.SORT() is not null ? " SORT" : "") + " AREA";
            var (members, complete) = ScreenSameOperands(same, written);
            var recorded = new SameClause(kind, members, written);
            foreach (var m in members) m.SameClauses.Add(recorded);
            sites.Add(new SameSite(same, recorded, complete));
        }
        ScreenSameClauses(sites);
        // §12.4.6.4.3 SR11 — asked after the WHOLE paragraph, because an APPLY COMMIT clause may follow the SAME
        // clause it constrains. One clause mixing the two kinds is one violation, reported at the clause.
        foreach (var site in sites)
            if (site.Clause.Members.FirstOrDefault(f => f.SubjectToApplyCommit) is { } subject
                && site.Clause.Members.FirstOrDefault(f => !f.SubjectToApplyCommit) is { } other)
            {
                using var _ = Edition.At(site.Ctx);
                ScreenApplyCommitSubject(subject, true,
                    $"{site.Clause.Written} naming '{subject.CobolName}' and '{other.CobolName}'",
                    "ISO §12.4.6.4.3 SR11: \"A file or record area that is subject to an APPLY COMMIT clause shall not be "
                    + "specified with another file or record area that is not subject to an APPLY COMMIT clause\"");
            }
        foreach (var site in sites)
            if (site.Clause.Kind == SameClauseKind.RecordArea) LinkSameRecordArea(site.Clause);
    }

    /// <summary>The source element's APPLY COMMIT clause (ISO §12.4.6.3; kb/Work PB3338), or null when its I-O-CONTROL
    /// paragraph writes none. The code generator registers it at the runtime element's activation (§12.4.6.3.4 GR5).</summary>
    public ApplyCommitModel? ApplyCommit { get; private set; }

    /// <summary>Whether the source element carries the RECURSIVE attribute (§11.10) — §12.4.6.3.3 SR5's antecedent (a
    /// function is always recursive, §8.6.6, and is asked through <see cref="UnitIsFunction"/>).</summary>
    public bool UnitIsRecursive { get; init; }

    /// <summary>Bind one APPLY COMMIT clause (ISO §12.4.6.3.2: <c>APPLY COMMIT ON [ [ file-name-1 ] [ identifier-1 ] ] …</c>)
    /// and the syntax rules that need no data division: SR1/SR2 (the files), SR4 (an operand at all), SR5 (a recursive
    /// source element). SR6 (a factory or instance definition, the only class-side definitions with an I-O-CONTROL
    /// paragraph) is <c>OoEnvironmentRules</c>'s alone.
    /// The identifier-1 operands are resolved, and SR3 and SR7-SR10 asked, by <see cref="ResolveApplyCommit"/> once the
    /// data division is bound.
    /// <para>The operand list repeats an all-optional pair, so the symbol table is the only discriminator: a BARE word
    /// naming a file of THIS element's FILE-CONTROL paragraph is file-name-1 (SR1; §8.4.2.2.2 gives a file-name no
    /// qualified format, kb/Work PB2040), anything else is identifier-1 — and an inherited GLOBAL file, which SR1 and
    /// SR11 both keep out of a contained element's clause, therefore resolves as an identifier and is refused there.</para>
    /// <para>A file of any organization on a mass storage device but a sort-merge or report file is accepted (§9.1.18.2:
    /// <i>"Apart from sort-merge files and report writer files, any type of file on a mass storage device may be
    /// specified"</i>): the keyed organizations keep the transaction in their store (DESIGN-runtime-library.md 2.2.1.10)
    /// and the sequential ones — record sequential, line sequential and the print stream — in the transaction journal
    /// (2.2.2, kb/Work PB3340).</para></summary>
    private void BindApplyCommitClause(Core.ApplyCommitClauseContext apply)
    {
        var clause = ApplyCommit ??= new ApplyCommitModel { At = Edition.Cursor };
        if (UnitIsRecursive || UnitIsFunction)
            ApplyCommitRule("the clause is written in a recursive source element"
                + (UnitIsFunction ? " (a function is always recursive, ISO §8.6.6)" : ""),
                "SR5", "This clause shall not be specified in a recursive source element.");
        // SR6 (a class, factory or interface definition) is OoEnvironmentRules', which sees the definition's kind: one
        // violation, one diagnostic.
        if (apply.dataReference().Length == 0)
            ApplyCommitRule("the clause names no file and no data item", "SR4",
                "At least one file or data-item shall be specified.");
        foreach (var operand in apply.dataReference())
        {
            using var at = Edition.At(operand);
            if (operand.cobolWord() is { } word && operand.dataReferenceSuffix().Length == 0
                && FilesByName.TryGetValue(word.GetText(), out var file))
            {
                if (file.IsSortMerge || file.IsReportFile)
                {
                    ApplyCommitRule($"'{file.CobolName}' is {(file.IsSortMerge ? "a sort-merge file" : "a report file")}",
                        "SR2", "File-name-1 shall not represent a sort-merge file, nor a file used by report writer.");
                    continue;
                }
                if (clause.Files.Contains(file)) continue;   // a repeated file-name adds nothing (GR1 is about the set)
                clause.Files.Add(file);
                file.SubjectToApplyCommit = true;
                // §12.4.5.9.3 SR1 — the LOCK MODE clause of the file control entry, bound before this paragraph.
                ScreenApplyCommitSubject(file, file.LockMode is not null, "LOCK MODE clause",
                    "ISO §12.4.5.9.3 SR1: \"This clause shall not be specified for a file that is the subject of "
                    + "an APPLY COMMIT clause for which there is an implicit LOCK MODE IS AUTOMATIC WITH LOCK ON "
                    + "MULTIPLE RECORDS applied automatically\"");
                continue;
            }
            var (name, quals) = ClauseDataName(operand, "APPLY COMMIT identifier-1");
            clause.Identifiers.Add((name, quals, Edition.Cursor));
        }
    }

    /// <summary>Post-build (the data division is bound and indexed), resolve the APPLY COMMIT clause's identifier-1
    /// operands and collect what §12.4.6.3.3 SR9 makes subject to it, asking the syntax rules that need a resolved
    /// item: SR3 (an unreferenced-modified, distinct, non-overlapping 01 or 77 item of the LINKAGE, WORKING-STORAGE or
    /// LOCAL-STORAGE SECTION; the reference-modified shape is already refused by <see cref="ClauseDataName"/>), SR7 (no
    /// object reference), SR8 (both or neither of two 01/77 items one of which depends on the other), SR9 (the
    /// implicit items' sections) and SR10 (no BASED item). Called at the end of <see cref="ResolveFiles"/>, whose FILE
    /// STATUS, LINAGE and RECORD DEPENDING resolutions SR9 reads.</summary>
    internal void ResolveApplyCommit()
    {
        if (ApplyCommit is not { } clause) return;
        var listed = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        foreach (var (name, quals, at) in clause.Identifiers)
        {
            if (ResolveClauseOperand(name, quals, "APPLY COMMIT identifier-1", at) is not { } item) continue;
            using var _ = Edition.At(at);
            if (item.Level is not (1 or 77) || item.Section is not (EntrySection.WorkingStorage
                    or EntrySection.LocalStorage or EntrySection.Linkage))
            {
                ApplyCommitRule($"'{item.CobolName}' is not a 01 or 77 level item of the linkage, working-storage or "
                    + "local-storage section", "SR3", "Multiple instances of Identifier-1 shall represent different "
                    + "non-overlapping 01 or 77 level data-items in the linkage, working-storage and local-storage sections.");
                continue;
            }
            if (!listed.Add(item) || clause.Items.Any(other => SharesStorage(item, other)))
            {
                ApplyCommitRule($"'{item.CobolName}' repeats or overlaps another identifier-1 of the clause", "SR3",
                    "Multiple instances of Identifier-1 shall represent different non-overlapping 01 or 77 level "
                    + "data-items in the linkage, working-storage and local-storage sections.");
                continue;
            }
            if (!ScreenApplyCommitItem(item, $"identifier-1 '{item.CobolName}'")) continue;
            clause.Items.Add(item);
        }
        // SR8 — "Where there is a dependency of one data item upon another, the 01 or 77 level data items that contain or
        // refer to them shall each either or neither be specified in the list of identifiers": the dependency the data
        // division can state between two 01/77 items is an OCCURS DEPENDING ON whose object lies in another record.
        foreach (var root in Roots.Where(r => r.Section is EntrySection.WorkingStorage or EntrySection.LocalStorage
                     or EntrySection.Linkage))
            foreach (var table in DataItem.DescendantsOf(root).Prepend(root))
                if (table.OccursSpec?.Depending is { } obj && !ReferenceEquals(obj.Root, root)
                    && obj.Root.Section is EntrySection.WorkingStorage or EntrySection.LocalStorage or EntrySection.Linkage
                    && listed.Contains(root) != listed.Contains(obj.Root))
                {
                    using var _ = Edition.At(clause.At);
                    ApplyCommitRule($"'{root.CobolName}' depends on '{obj.Root.CobolName}' (OCCURS DEPENDING ON "
                        + $"{obj.CobolName}), and the clause names only one of them", "SR8", "Where there is a dependency "
                        + "of one data item upon another, the 01 or 77 level data items that contain or refer to them "
                        + "shall each either or neither be specified in the list of identifiers, or have been specified in "
                        + "identifiers already subject to the same or another APPLY COMMIT clause.");
                }
        // SR9 — "the file status data items and data-items specified in the linage or record clauses of those file
        // descriptions are automatically subject to the APPLY COMMIT clause … and they shall only be specified in the
        // linkage, working-storage or local-storage sections, or in the case of the record clause also in the file section".
        // Over every file the clause names, sequential and keyed alike (§9.1.18.2; kb/Work PB3340).
        foreach (var file in clause.Files)
        {
            using var _ = Edition.At(clause.At);
            var implicitItems = new List<(DataItem Item, string What, bool FileSectionAllowed)>();
            if (file.FileStatusItem is { } fs) implicitItems.Add((fs, "FILE STATUS", false));
            if (file.Linage is { } lin)
                foreach (var op in new[] { lin.Body, lin.Footing, lin.Top, lin.Bottom })
                    if (op?.Item is { } li) implicitItems.Add((li, "LINAGE", false));
            if (file.VaryingDependingItem is { } dep) implicitItems.Add((dep, "RECORD DEPENDING", true));
            foreach (var (item, what, fileSectionAllowed) in implicitItems)
            {
                if (item.Section is not (EntrySection.WorkingStorage or EntrySection.LocalStorage or EntrySection.Linkage)
                    && !(fileSectionAllowed && item.Section is EntrySection.File))
                {
                    ApplyCommitRule($"the {what} item '{item.CobolName}' of file '{file.CobolName}' is described outside the "
                        + "linkage, working-storage and local-storage sections", "SR9", "… they shall only be specified in "
                        + "the linkage, working-storage or local-storage sections, or in the case of the record clause also "
                        + "in the file section.");
                    continue;
                }
                if (!ScreenApplyCommitItem(item, $"the {what} item '{item.CobolName}' of file '{file.CobolName}'")) continue;
                if (listed.Add(item) && !clause.Items.Any(i => DataItem.DescendantsOf(i).Contains(item)))
                    clause.Items.Add(item);
            }
        }
    }

    /// <summary>SR7 and SR10 over one item the clause saves: no object reference within it (ISO §12.4.6.3.3 SR7, and
    /// §9.1.18.6: "Data items containing object references are not permitted in APPLY COMMIT clauses") and no BASED
    /// item (SR10). Returns whether both hold.</summary>
    private bool ScreenApplyCommitItem(DataItem item, string what)
    {
        if (DataItem.DescendantsOf(item).Prepend(item).FirstOrDefault(i => i.Pic?.Usage == Usage.ObjectReference) is { } objRef)
        {
            ApplyCommitRule($"{what} contains the object reference '{objRef.CobolName}'", "SR7",
                "APPLY COMMIT clauses shall not include data items containing object-reference data items.");
            return false;
        }
        if (item.IsBased || item.Root.IsBased)
        {
            ApplyCommitRule($"{what} is subject to a BASED clause", "SR10", "No data items referenced by the FILE STATUS "
                + "clause, the LINAGE clause, the RECORD clause, or separately specified in the APPLY COMMIT clause shall "
                + "be subject to a BASED clause.");
            return false;
        }
        return true;
    }

    /// <summary>Whether two 01/77 items occupy any storage in common — the one 01-level way to overlap is a REDEFINES
    /// (§13.18.44), so the test follows each item's redefinition chain to the storage it starts.</summary>
    private static bool SharesStorage(DataItem a, DataItem b)
    {
        static DataItem Base(DataItem i) { while (i.RedefinesTarget is { } t) i = t; return i; }
        return ReferenceEquals(Base(a), Base(b));
    }

    /// <summary>⛔ THE ONE REPORT of the APPLY COMMIT clause's own syntax rules (COBOL4709; ISO §12.4.6.3.3).</summary>
    private void ApplyCommitRule(string what, string rule, string quoted) =>
        Edition.Error(DiagnosticCatalog.ApplyCommitClauseRule, $"APPLY COMMIT: {what} — ISO §12.4.6.3.3 {rule}: \"{quoted}\"");

    /// <summary>⛔ THE ONE REPORT FOR THE APPLY COMMIT ANTECEDENT FAMILY (kb/Work PB666) — the syntax rules of CLAIMED
    /// clauses and statements whose antecedent is "a file subject to an APPLY COMMIT clause": §12.4.5.9.3 SR1 (the
    /// LOCK MODE clause), §12.4.6.4.3 SR11 (a SAME clause mixing subject and non-subject files), §14.9.27.3 SR7 (OPEN's
    /// sharing phrase), §14.9.30.3 SR5 / §14.9.35.3 SR5 (READ's and REWRITE's lock phrases) and §14.9.47.3 SR2
    /// (UNLOCK). Each caller passes its own rule quoted with its citation; the fact is
    /// <see cref="FileModel.SubjectToApplyCommit"/>, recorded off this paragraph.
    /// <para>Each rule is enforced here in its own right (COBOL2974): since kb/Work PB3338 the APPLY COMMIT clause is
    /// accepted for the keyed organizations, so every one of these forbidden shapes is a program a user can write.
    /// (§12.4.6.4.4 GR3, the family's seventh member, is a general rule about an ACTIVE clause's sharing at run time,
    /// not a syntax rule.)</para></summary>
    /// <param name="file">The file the clause or statement names.</param>
    /// <param name="written">Whether the source wrote the construct the rule forbids for such a file.</param>
    /// <param name="what">The construct as written, for the message.</param>
    /// <param name="rule">The caller's own rule, quoted with its citation.</param>
    /// <returns>true when the rule holds; false after reporting.</returns>
    internal bool ScreenApplyCommitSubject(FileModel file, bool written, string what, string rule)
    {
        if (!(written && file.SubjectToApplyCommit)) return true;
        Edition.Error(DiagnosticCatalog.ApplyCommitSubjectRule, $"{what} — the file '{file.CobolName}' is named in an "
            + $"I-O-CONTROL APPLY COMMIT clause, and {rule}.");
        return false;
    }

    /// <summary>Only SAME RECORD AREA shares storage (Format 2) — SORT/SORT-MERGE AREA is Format 3, and SAME AREA
    /// (Format 1) gives no record-area effect either (docs/CONFORMANCE.md §7, A.1 items 168-169).
    /// <para>⛔ THE GROUP IS ONE STORAGE AREA FOR THE WHOLE RUNTIME ELEMENT, and that is also §14.9.6.4 GR7's
    /// answer for CLOSE. GR7: "If file-name-1 is specified in a SAME RECORD AREA clause, the record area
    /// is available to the runtime element if any of the file connectors referenced by the other
    /// file-names in that SAME RECORD AREA clause are open." The area emitted here is a live typed field
    /// of the program class, so a CLOSE of one member cannot take it away from a still-open sibling —
    /// GR7's availability branch holds STRUCTURALLY, not by omission (pinned by
    /// the conformance-suite test pb235_same_record_area_close). GR7's other branch — a successful CLOSE with no open
    /// member left makes the area UNAVAILABLE — carries no obligation a conforming program can observe:
    /// the standard defines nothing about referencing an unavailable record area (and Annex A.2 item 5
    /// makes the unsuccessful case outright undefined), so WiseOwl COBOL's determination is that the storage
    /// KEEPS ITS LAST CONTENT, documented at docs/CONFORMANCE.md §7, A.1 item 24 (kb/Work PB235).</para></summary>
    private static void LinkSameRecordArea(SameClause clause)
    {
        DataItem? anchor = null;
        foreach (var f in clause.Members)
        {
            if (f.Records.Count == 0) continue;
            // The area's CHARACTER half is linked here; an out-of-line record (FileModel.IsOutOfLineRecord —
            // D-FRA, kb/Work PB981) has no window over it and reaches the shared area through
            // FileModel.OutOfLineRecords, which is why every sharing file knows its peers
            // (FileModel.SameRecordAreaPeers, derived from the clause just recorded).
            if (f.CharacterAnchor is not { } fAnchor) continue;
            if (anchor is null) { anchor = fAnchor; continue; }
            // §12.4.6.4.4 GR2: "equivalent to an implicit redefinition of the area with records aligned on the
            // leftmost byte position" — implicit, like the FD's own records (kb/Work PB836).
            if (!ReferenceEquals(fAnchor, anchor) && fAnchor.RedefinesTarget is null)
                fAnchor.SetRedefinition(anchor, RedefinitionKind.SameRecordArea);
        }
    }

    /// <summary>One SAME clause as bound: its parse node (the position every rule reports at), its membership model
    /// and whether EVERY operand resolved to a file of this source element — a rule that asks what the clause
    /// contains (SR8) is silent about a clause whose own name resolution already failed.</summary>
    private readonly record struct SameSite(Core.SameClauseContext Ctx, SameClause Clause, bool Complete);

    /// <summary>⛔ THE OPERAND RULES OF ONE SAME CLAUSE (kb/Work PB1087), returning the files its operands resolve
    /// to and whether all of them did.
    /// <para>The format prints <c>file-name-1 { file-name-2 } …</c> in all three formats (§12.4.6.4.2), the braces
    /// REQUIRING a file-name-2, and §12.4.6.4.4 GR1/GR2 define the clause over "two or more files referenced by
    /// file-name-1, file-name-2" — so a clause naming fewer than two distinct files is not a legal form (the
    /// grammar admits it, superset-parse / bind-narrow). SR2: each operand "shall be specified in the FILE-CONTROL
    /// paragraph of the source element that contains this SAME clause" — <see cref="FilesByName"/> holds exactly
    /// this unit's own files here (a containing program's GLOBAL files merge in after the unit's data binds, so a
    /// SAME clause cannot name one) — so a name that is no file, or a data-name, is refused rather than dropped.
    /// SR3: the operands "shall not reference an external file connector".</para></summary>
    private (List<FileModel> Members, bool Complete) ScreenSameOperands(Core.SameClauseContext same, string written)
    {
        var members = new List<FileModel>();
        var seenNames = new HashSet<string>(CobolNames.Comparer);
        bool complete = true;
        foreach (var fn in same.fileName())
        {
            string name = fn.GetText();
            seenNames.Add(name);
            using var _ = Edition.At(fn);
            if (!FilesByName.TryGetValue(name, out var file))
            {
                complete = false;
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{written}: '{name}' is not a file specified in the FILE-CONTROL paragraph of this source element "
                    + "(ISO §12.4.6.4.3 SR2: \"File-name-1 and file-name-2 shall be specified in the FILE-CONTROL "
                    + "paragraph of the source element that contains this SAME clause.\")");
                continue;
            }
            if (file.IsExternal)
            {
                complete = false;
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{written}: file '{name}' references an external file connector (ISO §12.4.6.4.3 SR3: "
                    + "\"File-name-1 and file-name-2 shall not reference an external file connector.\")");
                continue;
            }
            if (!members.Contains(file)) members.Add(file);
        }
        if (seenNames.Count < 2)
        {
            using var _ = Edition.At(same);
            Edition.Error(DiagnosticCatalog.SameClauseRule,
                $"{written} names {(seenNames.Count == 0 ? "no file" : "only one file")}: every SAME format prints "
                + "\"file-name-1 { file-name-2 } …\" (ISO §12.4.6.4.2) and the clause is defined over \"two or more "
                + "files referenced by file-name-1, file-name-2\" (ISO §12.4.6.4.4 GR1, GR2)");
        }
        return (members, complete);
    }

    /// <summary>What a file IS to the SAME clause (§12.4.6.4.3 SR5-SR7 sort the file-names into three kinds).</summary>
    private enum SameFileKind { Report, SortMerge, Other }

    private static SameFileKind SameKindOf(FileModel f) =>
        f.IsSortMerge ? SameFileKind.SortMerge : f.IsReportFile ? SameFileKind.Report : SameFileKind.Other;

    /// <summary>⛔ THE OCCURRENCE TABLE: how many SAME clauses of one format a file of one kind may be named in
    /// (§12.4.6.4.3 SR5, SR6, SR7 — one rule shape, three rows) and the rule's own words. Both axes are switched
    /// BY AN UNREACHABLE-THROW arm only, so a new <see cref="SameFileKind"/> or <see cref="SameClauseKind"/> member
    /// fails loudly on its first SAME clause instead of falling into a silently unlimited cell.</summary>
    private static (int Max, int Sr, string Text) SameOccurrenceRule(SameFileKind kind, SameClauseKind format) => kind switch
    {
        SameFileKind.Report => (format switch
        {
            SameClauseKind.Area => 1,
            SameClauseKind.RecordArea => 0,
            SameClauseKind.SortMergeArea => 0,
            _ => throw new System.Diagnostics.UnreachableException(),
        }, 5, "a given file-name that represents a report file may be specified in one file-area format SAME clause "
            + "and shall not be specified in a record-area format or sort-merge-area format SAME clause"),
        SameFileKind.SortMerge => (format switch
        {
            SameClauseKind.Area => 0,
            SameClauseKind.RecordArea => 1,
            SameClauseKind.SortMergeArea => 1,
            _ => throw new System.Diagnostics.UnreachableException(),
        }, 6, "a given file-name that represents a sort or merge file may be specified in one record-area format SAME "
            + "clause and in one sort-merge-area SAME clause, and shall not be specified in a file-area format SAME clause"),
        SameFileKind.Other => (format switch
        {
            SameClauseKind.Area => 1,
            SameClauseKind.RecordArea => 1,
            SameClauseKind.SortMergeArea => int.MaxValue,
            _ => throw new System.Diagnostics.UnreachableException(),
        }, 7, "a given file-name that represents a file other than a report file or a sort or merge file may be "
            + "specified in one file-area format, in one record-area format, and in one or more sort-merge-area format "
            + "SAME clauses"),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private static string SameKindName(SameFileKind kind) => kind switch
    {
        SameFileKind.Report => "report file",
        SameFileKind.SortMerge => "sort or merge file",
        SameFileKind.Other => "file",
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private static string SameFormatName(SameClauseKind format) => format switch
    {
        SameClauseKind.Area => "file-area",
        SameClauseKind.RecordArea => "record-area",
        SameClauseKind.SortMergeArea => "sort-merge-area",
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    /// <summary>⛔ THE CROSS-CLAUSE RULES OF THE SAME CLAUSES OF ONE I-O-CONTROL PARAGRAPH (kb/Work PB1087;
    /// §12.4.6.4.3 SR5-SR10), over the membership every clause recorded: the per-file occurrence limits
    /// (<see cref="SameOccurrenceRule"/> — SR5, SR6, SR7), SR8 (a sort-merge-area clause names a sort or merge
    /// file), SR9 (a file-area clause that shares a file with a record-area clause is wholly inside it) and SR10 (a
    /// file-area clause is wholly inside every sort-merge-area clause that names one of its non-sort files). Both
    /// the SORT and the SORT-MERGE spelling are the one sort-merge-area format (SR1), which is what
    /// <see cref="SameClauseKind"/> says.</summary>
    private void ScreenSameClauses(IReadOnlyList<SameSite> sites)
    {
        var count = new Dictionary<(FileModel, SameClauseKind), int>();
        foreach (var site in sites)
            foreach (var f in site.Clause.Members)
            {
                var role = SameKindOf(f);
                var (max, sr, text) = SameOccurrenceRule(role, site.Clause.Kind);
                count[(f, site.Clause.Kind)] = count.GetValueOrDefault((f, site.Clause.Kind)) + 1;
                if (count[(f, site.Clause.Kind)] <= max) continue;
                using var _ = Edition.At(site.Ctx);
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{site.Clause.Written}: {SameKindName(role)} '{f.CobolName}' "
                    + (max == 0 ? $"is named in a {SameFormatName(site.Clause.Kind)} format clause, which its kind bars"
                                : $"is named in more than one {SameFormatName(site.Clause.Kind)} format clause")
                    + $" (ISO §12.4.6.4.3 SR{sr}: {text})");
            }

        // §13.18.27.3 SR2 (kb/Work PB1242): the GLOBAL clause is written on neither the file description entry nor a
        // record description entry of any file the record-area clause shares.
        foreach (var site in sites.Where(s => s.Clause.Kind == SameClauseKind.RecordArea))
            foreach (var f in site.Clause.Members)
            {
                string? where = f.IsGlobal ? "file description entry"
                    : f.Records.Any(r => r.HasGlobalClause) ? "record description entry" : null;
                if (where is null) continue;
                using var _ = Edition.At(site.Ctx);
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{site.Clause.Written}: the {where} of file '{f.CobolName}' includes the GLOBAL clause (ISO §13.18.27.3 "
                    + "SR2: \"If the SAME RECORD AREA clause is specified for several files, the record description "
                    + "entries or the file description entries for these files shall not include the GLOBAL clause.\")");
            }

        foreach (var site in sites)
        {
            if (site.Clause.Kind != SameClauseKind.SortMergeArea || !site.Complete
                || site.Clause.Members.Any(m => m.IsSortMerge)) continue;
            using var _ = Edition.At(site.Ctx);
            Edition.Error(DiagnosticCatalog.SameClauseRule,
                $"{site.Clause.Written} names no sort or merge file (ISO §12.4.6.4.3 SR8: \"At least one file-name "
                + "specified in a sort-merge-area format SAME clause shall represent a sort or merge file.\")");
        }

        foreach (var area in sites.Where(s => s.Clause.Kind == SameClauseKind.Area))
        {
            foreach (var record in sites.Where(s => s.Clause.Kind == SameClauseKind.RecordArea))
            {
                var missing = SameMissingFrom(area.Clause, record.Clause);
                if (missing.Count == 0 || missing.Count == area.Clause.Members.Count) continue;
                using var _ = Edition.At(record.Ctx);
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{record.Clause.Written} shares files with {area.Clause.Written} but omits {SameNames(missing)} "
                    + "(ISO §12.4.6.4.3 SR9: \"If one or more file-names specified in a file-area format SAME clause "
                    + "are also specified in a record-area format SAME clause, all of the file-names specified in the "
                    + "file-area format SAME clause shall also be specified in the record-area format SAME clause.\")");
            }
            var reported = new HashSet<SameSite>();
            foreach (var f in area.Clause.Members.Where(m => SameKindOf(m) == SameFileKind.Other))
                foreach (var sm in sites.Where(s => s.Clause.Kind == SameClauseKind.SortMergeArea && s.Clause.Members.Contains(f)))
                {
                    var missing = SameMissingFrom(area.Clause, sm.Clause);
                    if (missing.Count == 0 || !reported.Add(sm)) continue;
                    using var _ = Edition.At(sm.Ctx);
                    Edition.Error(DiagnosticCatalog.SameClauseRule,
                        $"{sm.Clause.Written} names '{f.CobolName}', which {area.Clause.Written} also names, but omits "
                        + $"{SameNames(missing)} (ISO §12.4.6.4.3 SR10: \"If a file-name that represents a file other "
                        + "than a sort or merge file is specified in a file-area format SAME clause and in one or more "
                        + "sort-merge-area format SAME clauses, all of the file-names specified in that file-area "
                        + "format SAME clause shall also be specified in those sort-merge-area format SAME clause(s).\")");
                }
        }
    }

    private static List<FileModel> SameMissingFrom(SameClause subset, SameClause superset) =>
        subset.Members.Where(m => !superset.Members.Contains(m)).ToList();

    private static string SameNames(IEnumerable<FileModel> files) =>
        string.Join(", ", files.Select(f => $"'{f.CobolName}'"));
}
