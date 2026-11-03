// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime.Collation;

namespace WiseOwl.Cobol.Binding;

using WiseOwl.Cobol.Runtime;
using Core = CobolParserCore;

/// <summary>
/// The binder of one source element's INPUT-OUTPUT SECTION (ISO §12.4): the FILE-CONTROL paragraph's SELECT entries
/// (§12.4.5), whose clauses it captures into <see cref="FileModel"/>s before the FD records bind, and the I-O-CONTROL
/// paragraph (§12.4.6; <c>FileControlBinder.IoControl.cs</c>), bound against those files (DESIGN-architecture-review
/// §8.3 (1), the census's R0-0001 target "FileControlBinder"). The data binder constructs one per unit
/// (<see cref="DataBinder.FileControl"/>) and drives it from the top of its declaration half; the files it declares live
/// on the data binder (<see cref="DataBinder.Files"/>, <see cref="DataBinder.FilesByName"/>), where the FD, the report
/// section and the file resolution reach them, and it asks the data binder for what is not a file concern: the §8.3.2.2
/// declaration funnel, the clause-operand screens, the constant readings and the data-item resolution.
/// </summary>
public sealed partial class FileControlBinder
{
    private readonly DataBinder _host;

    private EditionContext Edition { get; }

    internal FileControlBinder(DataBinder host, EditionContext edition)
    {
        _host = host;
        Edition = edition;
    }

    /// <summary>The files this unit's OWN file control entries select, by file-name — the subjects of ISO §12.4.5.2
    /// SR2 ("A given file-name may be specified in only one SELECT clause within a factory, function, object, or
    /// program") and SR3 (each needs an FD or SD in the same file section). It is not <see cref="FilesByName"/>,
    /// which also holds the models an FD or SD synthesizes, and it leaves out a class-level FILE-CONTROL, which
    /// §12.4.3 SR1 forbids outright (kb/Work PB1076) and which belongs to no factory or object (kb/Work PB1077).</summary>
    internal Dictionary<string, FileModel> SelectedFiles { get; } = new(CobolNames.Comparer);

    /// <summary>The SUPPRESS WHEN operands as written, keyed by their clause (captured in <see cref="BindFileControl"/>, read by
    /// <see cref="DataBinder.ReadSuppressWhen"/> once the data division's constant-names are declared).</summary>
    internal Dictionary<AlternateKeyClause, Core.ValueClauseOperandContext> SuppressWhenOperands { get; } = [];

    /// <summary>Bind the FILE-CONTROL paragraph's SELECT clauses into <see cref="FileModel"/>s (assign target,
    /// organization, access mode, OPTIONAL, FILE STATUS). The FD records attach in <see cref="BindFileSection"/>.
    /// A unit carries at most ONE environment division: an OO definition's synthetic unit (<c>OoDriver.OoReparent</c>)
    /// carries only its own, and a class definition's configuration reaches its factory and instance definitions by
    /// inheritance (<see cref="ConfigurationStep"/>; kb/Work PB3154), never as a second division.</summary>
    internal void BindFileControl(Core.ProgramUnitContext program)
    {
        if (program.environmentDivision() is not { } env
            || env.inputOutputSection()?.fileControlParagraph() is not { } fc) return;
        // A class-level input-output section (the class definition's own binder binds it, OoDriver.BindClassData) is
        // outside §12.4.5.2's "factory, function, object, or program" and is forbidden by §12.4.3 SR1 (kb/Work
        // PB1076), so SR2 and SR3 do not screen its entries.
        bool screened = env.Parent is not Core.ClassDefinitionContext;
        foreach (var grp in fc.fileControlClauseGroup())
        {
            using var _ = Edition.At(grp);
            if (grp.fileName()?.GetText() is not { } name) continue;
            // ISO §12.4.5.2 SR2 — one SELECT per file-name. The first entry stands: binding the second used to
            // REPLACE it in FilesByName, so the program silently ran against the last ASSIGN target (kb/Work PB1077).
            if (screened && SelectedFiles.ContainsKey(name))
            {
                Edition.Error(DiagnosticCatalog.FileNameSelectedTwice, $"file-name '{name}' is specified in a second "
                    + "SELECT clause; ISO §12.4.5.2 SR2: \"A given file-name may be specified in only one SELECT clause "
                    + "within a factory, function, object, or program.\"");
                continue;
            }
            var file = new FileModel { CobolName = name, SelectName = name, HasSelect = true, Optional = grp.OPTIONAL() is not null };
            // The entry's OWN position: §12.4.5.1's required members and §12.4.5.2 SR10 are violated by the
            // ABSENCE of a clause, so there is nothing else to point at (kb/Work PB699).
            file.EntryAt = Edition.Cursor;
            Core.AccessModeClauseContext? accessAt = null;   // the ACCESS MODE clause cursor for §12.4.5.5.2 SR2, below
            Core.FileReserveClauseContext? reserveAt = null;   // the RESERVE clause cursor for §12.4.5.2 SR12, below
            foreach (var clauses in grp.fileControlClauses())
            {
                // ISO §12.4.5.3 GR3 — BOTH phrases of the ASSIGN clause, on both general-format arms
                // (`ASSIGN TO? assignTarget (USING dataReference)?` and the bare `ASSIGN USING dataReference`).
                // GR3 a is the static TO-phrase association; GR3 b is dynamic file assignment (§9.1.21) and applies
                // "when the USING phrase of the ASSIGN clause is specified" with NO condition on the TO phrase, so
                // both are captured and the runtime lets the USING content win at each OPEN/SORT/MERGE. Reading only
                // assignTarget() dropped the USING operand entirely AND left a bare-USING file's target defaulted to
                // the FILE-NAME, so its records landed in `<file-name>.txt` (kb/Work PB324).
                if (clauses.assignClause() is { } asg)
                {
                    file.AssignWritten = true;
                    // The TO phrase is a LIST (§12.4.5.1's ellipsis); which lists are allowable and what one names
                    // is the §12.4.5.2 SR5 determination, read in ONE place (AssignTargetRule, DOC-A.1-71).
                    file.AssignTarget = asg.assignTarget() is { Length: > 0 } tgts
                        ? AssignTargetRule.Resolve(Edition, _host.LiteralEnv, tgts, name)
                        : "";   // bare `ASSIGN USING …`: no device-name-1/literal-1 exists — UNASSOCIATED until an OPEN
                    if (asg.dataReference() is { } dyn)
                    {
                        (file.AssignUsingName, file.AssignUsingQualifiers) = _host.ClauseDataName(dyn, "ASSIGN USING");
                        using var __ = Edition.At(dyn);
                        file.AssignUsingAt = Edition.Cursor;   // §12.4.5.2 SR7 reports here, from ResolveFiles
                    }
                }
                else if (clauses.organizationClause() is { } org) { file.Organization = MapOrganization(org); file.OrganizationWritten = true; }
                else if (clauses.accessModeClause() is { } acc) { file.AccessMode = MapAccessMode(acc); file.AccessModeWritten = true; accessAt = acc; }
                // The base word PLUS its IN/OF qualifiers (`SQ-FS4-STATUS OF STATUS-GROUP`, SQ133A): a raw
                // GetText() would glue the qualifier into the lookup key, and keeping the base word ALONE
                // resolved the first same-named declaration in order (kb/Work PB489).
                else if (clauses.fileStatusClause()?.dataReference() is { } fs)
                {
                    (file.FileStatusName, file.FileStatusQualifiers) = _host.ClauseDataName(fs, "FILE STATUS");
                    using var __ = Edition.At(fs);
                    file.FileStatusAt = Edition.Cursor;   // where its post-build resolution reports
                }
                else if (clauses.recordKeyClause() is { } rkc)                                                       // ISO §12.4.5.12
                {
                    // ⛔ THE CLAUSE HAS TWO KEY FORMS — §12.4.5.12.2's brace — and each is captured by its own
                    // arm, the SOURCE form FIRST, so neither can fall through this chain unread (kb/Work PB358,
                    // PB1099): a null dataReference() taken alone would skip the clause and leave the file KEYLESS.
                    if (rkc.recordKeySourcePhrase() is { } rks)
                    {
                        file.RecordKeySource = CaptureRecordKeySource(rks, "RECORD KEY");
                        file.RecordKeyName = file.RecordKeySource.Name;
                        using var __ = Edition.At(rks.recordKeyName());
                        file.RecordKeyAt = Edition.Cursor;
                    }
                    else if (rkc.dataReference() is { } rk)
                    {
                        (file.RecordKeyName, file.RecordKeyQualifiers) = _host.ClauseDataName(rk, "RECORD KEY");
                        using var __ = Edition.At(rk);
                        file.RecordKeyAt = Edition.Cursor;   // §12.4.5.12.3 reports here, from ResolveFiles
                    }
                }
                else if (clauses.alternateKeyClause() is { } ak)                                                     // ISO §12.4.5.6
                {
                    // The SAME two-form choice as its prime-key twin above, taken in the SAME order and for the
                    // same reason — §12.4.5.6.2 prints the identical brace group, and Annex A.3 item 40 names
                    // both clauses in one sentence, so one omission here would be the two-arm defect shape
                    // (feedback_two_arm_dispatch) on a clause that literally IS the other arm.
                    var aks = ak.recordKeySourcePhrase();
                    if (aks is not null || ak.dataReference() is not null)
                    {
                        var source = aks is null ? null : CaptureRecordKeySource(aks, "ALTERNATE RECORD KEY");
                        var (an, aq) = source is null ? _host.ClauseDataName(ak.dataReference(), "ALTERNATE RECORD KEY")
                            : (source.Name, (IReadOnlyList<string>)[]);
                        using var __ = Edition.At(aks?.recordKeyName() ?? (Antlr4.Runtime.ParserRuleContext)ak.dataReference());
                        var clause = new AlternateKeyClause
                        {
                            Name = an, Qualifiers = aq, Duplicates = ak.DUPLICATES() is not null, Source = source,
                            At = Edition.Cursor,   // §12.4.5.6.3 reports here, from ResolveFiles
                        };
                        file.AlternateKeyNames.Add(clause);
                        // §12.4.5.6.2 SUPPRESS WHEN literal-1 — captured AS WRITTEN and read post-build by
                        // ReadSuppressWhen: a constant-name operand is declared in the DATA DIVISION, after this entry.
                        if (ak.alternateKeySuppressWhen()?.valueClauseOperand() is { } sw)
                            SuppressWhenOperands[clause] = sw;
                    }
                }
                else if (clauses.relativeKeyClause()?.dataReference() is { } rlk)
                {
                    (file.RelativeKeyName, file.RelativeKeyQualifiers) = _host.ClauseDataName(rlk, "RELATIVE KEY");   // ISO §12.4.5.13.3 SR3 — outside the record
                    using var __ = Edition.At(rlk);
                    file.RelativeKeyAt = Edition.Cursor;   // §12.4.5.13.3 reports here, from ResolveFiles
                }
                else if (clauses.sharingClause() is { } sh)   // §12.4.5.15 — edition gate: VersionConformancePass ParseArm.VisitSharingClause (14g.4, recognition — fires on the clause's presence, drop-proof on a SELECT error)
                    file.Sharing = MapSharing(sh.sharingMode());
                else if (clauses.lockModeClause() is { } lm)   // §12.4.5.9 — edition gate: VersionConformancePass ParseArm.VisitLockModeClause (14g.4)
                    file.LockMode = MapLockMode(lm);
                else if (clauses.fileCollatingSequenceClause() is { } col)   // §12.4.5.7 — introduction-gated post-bind; resolved in ResolveFileCollating
                    CaptureFileCollating(file, col);
                // RESERVE clause (ISO §12.4.5.14) — integer-1 is the connector's input-output area count
                // (§12.4.5.14.3 GR1, "If the RESERVE clause is specified, the number of input-output areas allocated
                // is equal to the value of integer-1"; kb/Work PB643): the emitter tells the connector
                // (SequentialIoEmitter.EmitReserveRegistration), whose reader, writer or store handle allocates them.
                // It is also part of an external file connector's entry identity — §12.4.5.3 GR1 d), "The same value
                // for integer-1 in the RESERVE clause" (kb/Work PB1079, the EC-EXTERNAL-FILE-MISMATCH fingerprint).
                // The areas are one host buffer, so integer-1 above HostFile.MaxInputOutputAreas is refused
                // (COBOL2897, §4.2.15: "A conforming implementation may place such limits").
                else if (clauses.fileReserveClause() is { } res)
                {
                    using var rsv = Edition.At(res);
                    reserveAt = res;
                    file.ReserveAreas = _host.Constants.IntegerOperandValue(res.integerOperand(), $"file '{name}' RESERVE clause")
                        ?? ConstantEntryBinder.RecoveredIntegerOperand;
                    if (file.ReserveAreas > WiseOwl.Cobol.Runtime.IO.HostFile.MaxInputOutputAreas)
                    {
                        Edition.Error(DiagnosticCatalog.ReserveAreasBeyondLimit,
                            $"file '{name}': RESERVE {file.ReserveAreas} AREAS exceeds this implementation's limit of "
                            + $"{WiseOwl.Cobol.Runtime.IO.HostFile.MaxInputOutputAreas} input-output areas (ISO §4.2.15)");
                        file.ReserveAreas = ConstantEntryBinder.RecoveredIntegerOperand;
                    }
                }
                // RECORD DELIMITER clause (ISO §12.4.5.11) — DECLINED, ACCEPT-INERT, and diagnosed by name at
                // EVERY edition on BOTH arms of its required choice (kb/Work PB292). STANDARD-1 is Annex A.3
                // item 26, a processor-dependent element whose §12.4.5.11.4 GR2 medium is a tape drive; a
                // feature-name-1 names nothing because §12.4.5.11.3 SR2's available-name set is this
                // implementation's to specify and is EMPTY (Annex A.1 item 150 is optional — docs/CONFORMANCE.md
                // §7). The clause does not change the I-O: §12.4.5.11.4 GR5's implementor method (the 4-byte
                // length prefix) frames every variable-length record whatever the clause says. §4.2.6 ¶3 makes
                // the WARNING the obligation this discharges; §12.4.5.11.4 GR1 keeps the framing out of the record
                // area, so the accept is inert in the program's own terms. The phrase written IS captured
                // (FileModel.RecordDelimiter), for the one reader that needs it: §12.4.5.3 GR1 c) makes "Either
                // the STANDARD-1 phrase or a consistent value of feature-name-1" part of an external file
                // connector's entry identity (kb/Work PB1079). This arm is LAST because every preceding clause
                // has a distinct leading token and `recordKeyClause` already back-tracks past `RECORD DELIMITER`.
                else if (clauses.recordDelimiterClause() is { } rd)
                {
                    file.RecordDelimiter = rd.STANDARD_1() is not null ? "STANDARD-1" : CobolNames.UpperFold(rd.cobolWord().GetText());
                    Edition.Declined(DiagnosticCatalog.RecordDelimiterUnsupported,
                        $"the RECORD DELIMITER clause on file '{name}' ({DataBinder.Spelled(rd)})");
                }
            }
            // §12.4.5.5.2 SR2 — "The DYNAMIC and RANDOM phrases shall not be specified for a sequential file."
            // A FILE CONTROL ENTRY rule, so it belongs HERE, beside the clauses it relates, and not on any verb:
            // it constrains the ENTRY whether or not the file is ever read, written or started (kb/Work PB692).
            // The general format says the same thing structurally — Format 3 (sequential) admits only
            // `[ ACCESS MODE IS SEQUENTIAL ]` — but a format is not a check, and nothing screened this.
            // ⛔ THE ORGANIZATION MAY BE UNWRITTEN. §12.4.5.10.3 GR6: "When the ORGANIZATION clause is not
            // specified, sequential organization with the RECORD SEQUENTIAL phrase is implied", so the bare
            // `SELECT F ASSIGN … ACCESS MODE IS RANDOM.` is under this rule too and is the shape a screen keyed
            // on a written clause would miss; `FileModel.Organization` already defaults to Sequential, so
            // `IsSequential` covers the omitted clause, RECORD SEQUENTIAL and LINE SEQUENTIAL in one predicate.
            // That LINE SEQUENTIAL is under "a sequential file" is DERIVED, not assumed: §12.4.5.10.3 GR2/GR3
            // put both sequential phrases in the ORGANIZATION clause, that clause is written only in the
            // Format-3 file control entry, and §12.4.5.2 SR11 says "Format 3 shall be specified only for a
            // sequential file or a report file".
            // Edition-invariant (all four): no `docs/VERSION_CHANGE_REFERENCE.md` row touches §12.4.5.5, and the
            // 1985 sequential-I-O file control entry likewise offered no ACCESS MODE but SEQUENTIAL.
            // WHY THIS MATTERS DOWNSTREAM: without it, `IsSequential && AccessMode != Sequential` is a REACHABLE
            // state, and the verbs then read a file that has no keys through their keyed rules — §14.9.30.3
            // SR8/SR9 imply no NEXT under RANDOM, so §14.9.30.4 GR19 turns the READ into a Format-2 random read
            // on a keyless connector. Every statement-level screen that tests `AccessMode == Random` on a
            // SEQUENTIAL-organization file is defence-in-depth from here on, never the only guard.
            // The predicate NAMES THE TWO PHRASES the rule names, rather than negating SEQUENTIAL: the rule is
            // "DYNAMIC and RANDOM shall not be specified", not "only SEQUENTIAL may be", and a mode the standard
            // adds later would then be screened by a rule that does not mention it.
            if (file.IsSequential && file.AccessMode is FileAccessMode.Random or FileAccessMode.Dynamic)
            {
                using var acc2 = Edition.At(accessAt);   // the ACCESS MODE clause itself (null ⇒ the entry cursor)
                Edition.Error(DiagnosticCatalog.AccessModeNotSequentialOnSequentialFile,
                    $"file '{name}': ACCESS MODE IS {file.AccessMode.ToString().ToUpperInvariant()} may not be "
                    + $"specified for a sequential file — this entry's organization is "
                    + file.OrganizationFace
                    + " (ISO §12.4.5.5.2 SR2)");
            }
            // §12.4.5.2 SR12 — "If the LINE SEQUENTIAL phrase of the ORGANIZATION clause is specified, the RESERVE
            // clause shall not be specified" (kb/Work PB781). A clause-pair exclusion decidable from the entry alone,
            // so it sits here beside its two siblings and not in FileControlKeyRules (FILES_DESIGN D19).
            // ⛔ The predicate is LINE SEQUENTIAL itself, NOT `IsSequential`: RECORD SEQUENTIAL and the omitted
            // clause (§12.4.5.10.3 GR6) are sequential too and keep RESERVE (NIST IX4014, SQ2034, SQ2264 …).
            // The ORGANIZATION clause may follow the RESERVE clause, so the screen waits for the whole entry.
            // Reachable at 2023 only: below it LINE SEQUENTIAL is refused by its introduction gate.
            if (file.Organization == FileOrganization.LineSequential && reserveAt is not null)
            {
                using var rsv2 = Edition.At(reserveAt);
                Edition.Error(DiagnosticCatalog.ReserveOnLineSequentialFile, $"file '{name}': the RESERVE clause "
                    + $"({DataBinder.WrittenText(reserveAt)}) may not be specified for a LINE SEQUENTIAL file (ISO §12.4.5.2 SR12)");
            }
            // §12.4.5.9.3 SR2: WITH LOCK ON MULTIPLE RECORDS shall not be specified for a sequentially-accessed
            // or sequential-organization file. The organization half is `FileModel.IsSequential` — the ONE
            // spelling of "this file has sequential organization" (§12.4.5.10.3 GR2/GR3/GR6), shared with the
            // §12.4.5.5.2 SR2 screen above and with every verb-side reroute, rather than a second open-coded
            // copy of the enum pair that a future organization would have to be added to twice.
            if (file.LockMode is { Multiple: true }
                && (file.AccessMode is FileAccessMode.Sequential || file.IsSequential))
                Edition.Error("COBOL1512", $"file '{name}': LOCK MODE … WITH LOCK ON MULTIPLE RECORDS may "
                    + "not be specified for a sequential-access or sequential-organization file (ISO §12.4.5.9.3 SR2)");
            // ⛔ §14.9.27.3 SR8 IS **NOT** CHECKED HERE, AND MUST NEVER BE RE-ADDED. It is a syntax rule of the
            // OPEN STATEMENT about file-name-1, which §14.9.27.2's general format defines as the OPEN
            // statement's operand: BOTH disjuncts of its antecedent presuppose an OPEN ("if the sharing phrase
            // is omitted from the OPEN statement and the ALL phrase is specified in the SHARING clause of the
            // file control entry for file-name-1 or if the ALL phrase is specified on the OPEN statement"), and
            // its leading conjunct ("When file-name-1 is not subject to an APPLY COMMIT clause") keys on the
            // I-O-CONTROL paragraph, which is bound LATER — so this site could not evaluate the antecedent even
            // if the rule reached it. The SHARING clause's own clause imposes nothing of the kind: §12.4.5.15.3
            // has exactly ONE general rule ("The SHARING clause specifies the sharing mode to be used for the
            // file unless it is overridden by the SHARING phrase of the OPEN statement") and NO syntax rules, so
            // §14.9.27.3 SR8 is the standard's only source of a LOCK MODE requirement anywhere.
            // A second copy here dropped the antecedent and rejected legal source — a file whose OPEN supplies
            // its own non-ALL sharing phrase, and a file never opened at all — while duplicating the diagnostic
            // for programs that DO violate it. THE ONE HOME IS StatementValidation.CheckOpenSharingAllOther,
            // called from SequentialIoBinder.BindOpen per file-name (kb/Work PB319, feedback_one_rule_one_place;
            // conformance:OpenSharingLockModeTests pins the single-diagnostic count).
            _host.AddFile(file);
            _host.DeclareUserWord(name, UserWordKind.FileName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB65, PB1083)
            _host.FilesByName[name] = file;
            if (screened) SelectedFiles[name] = file;
        }
    }

    /// <summary>Capture the SOURCE phrase of a RECORD KEY (ISO §12.4.5.12.2) or ALTERNATE RECORD KEY (§12.4.5.6.2)
    /// clause AS WRITTEN — record-key-name-1 and its data-name-2 operands in the order specified, each operand
    /// through the ONE file-clause capture (<see cref="ClauseDataName"/>, which refuses the shapes a data-name-n
    /// position does not admit) with the cursor its post-build rules report at. The key is resolved in
    /// <see cref="ResolveFiles"/>, as data-name-1's is: "Record-key-name-1 defines a record key consisting of the
    /// concatenation of all occurrences of data-name-2 in the order specified" (§12.4.5.12.4 GR2 / §12.4.5.6.4
    /// GR2; Annex A.3 item 40, provided — kb/Work PB1099 Q1, which retired the COBOL1954 refusal).
    /// <para>ONE capture for BOTH clauses: the two general formats print the SAME brace group, so a second copy
    /// would be the two-arm shape (feedback_two_arm_dispatch). record-key-name-1 is DECLARED here, through the one
    /// declaration funnel, because the phrase is what defines it (§8.3.2.2.24; kb/Work PB1083).</para></summary>
    private RecordKeySource CaptureRecordKeySource(Core.RecordKeySourcePhraseContext src, string clause)
    {
        string keyName = src.recordKeyName().GetText();
        using (Edition.At(src.recordKeyName()))
            _host.DeclareUserWord(keyName, UserWordKind.RecordKeyName);
        var operands = new List<KeySourceOperand>();
        foreach (var dref in src.dataReference())
        {
            var (n, q) = _host.ClauseDataName(dref, $"{clause} SOURCE");
            using var _ = Edition.At(dref);
            operands.Add(new KeySourceOperand { Name = n, Qualifiers = q, At = Edition.Cursor });
        }
        return new RecordKeySource { Name = keyName, Operands = operands };
    }

    /// <summary>Capture one file-control COLLATING SEQUENCE clause as written (ISO §12.4.5.7) — resolved to per-key
    /// weight tables post-build in <see cref="ResolveFileCollating"/> (the keys are not yet bound here). Format 2 is
    /// OF-led; Format 1 is the FOR-split or the IS alphabet-name-1 [alphabet-name-2] form.</summary>
    private void CaptureFileCollating(FileModel file, Core.FileCollatingSequenceClauseContext ctx)
    {
        using var _ = Edition.At(ctx);
        if (ctx.OF() is not null)   // Format 2 (key-level): OF {key}… IS alphabet-name-3
        {
            // Each key through the ONE data-name-n capture (kb/Work PB1075): qualified names are legal (§8.4.2.2.1),
            // and SR6's "shall not be subscripted" is refused by name there.
            var keys = ctx.dataReference()
                .Select(d => _host.ClauseDataName(d, "COLLATING SEQUENCE OF"))
                .Select(k => new CollatingKeyOperand { Name = k.Base, Qualifiers = k.Quals })
                .ToList();
            file.KeyLevelCollating.Add(new KeyLevelCollatingClause(keys, ctx.cobolWord(0).GetText(), Edition.Cursor));
            _host.SpecialNames.UseAlphabetName(ctx, ctx.cobolWord(0).GetText());   // the clause's §8.3.2.2 use (kb/Work PB3153)
            return;
        }
        if (file.FileLevelCollatingCount++ == 0) file.FileLevelCollatingAt = Edition.Cursor;   // §12.4.5.7.3 SR3 — at most one file-level clause
        file.FileLevelCollating = ChoiceIndicators.AlphabetPair(Edition, ctx.collatingForPhrase(),
            f => f.NATIONAL() is not null, f => f.cobolWord().GetText(), ctx.cobolWord(),
            $"file '{file.CobolName}' COLLATING SEQUENCE", "12.4.5.7.2 Format 1");
        // The written names are this clause's §8.3.2.2 uses (kb/Work PB3153); SR1/SR2 screen them post-build.
        if (file.FileLevelCollating is var (alnum, nat))
            foreach (var name in new[] { alnum, nat }.OfType<string>()) _host.SpecialNames.UseAlphabetName(ctx, name);
    }

    /// <summary>Resolve each INDEXED key's collating-weight table from the file's §12.4.5.7 COLLATING SEQUENCE
    /// clauses (post-build — the keys are bound by now). Per §12.4.5.7.4 the sequence for a key is, in order: (GR6)
    /// a Format-2 clause naming it; else (GR2/GR3) the Format-1 default for the key's CLASS; else (GR4/GR5) the
    /// native sequence of that class (null weights = ordinal) — see <see cref="ResolveKeyCollating"/>. The clause's
    /// syntax rules are screened HERE, as written and before any key is resolved: SR3 (one file-level clause),
    /// SR1/SR2 (the Format-1 alphabets' classes), SR4/SR5 (Format-2 names are declared keys), SR7 (alphabet-name-3
    /// against each named key's class) and SR8 (a key in at most one clause).
    /// <para>⛔ SR1/SR2 ARE RULES ABOUT THE CLAUSE, NOT ABOUT THE KEYS IT REACHES (kb/Work PB1074). They used to be
    /// checked lazily, inside the per-key resolution, so an alphabet-name-1 that every key's Format-2 clause
    /// shadowed was never looked at (`COLLATING SEQUENCE IS NOSUCH` compiled clean) and alphabet-name-2 was never
    /// read at all.</para></summary>
    internal void ResolveFileCollating(FileModel file)
    {
        using var fileLevelAt = Edition.At(file.ClauseOrEntryAt(file.FileLevelCollatingAt));   // the file-level clause (kb/Work PB2034)
        if (file.FileLevelCollatingCount > 1)
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': at most one file-level "
                + "COLLATING SEQUENCE clause may be specified in one file control entry (ISO §12.4.5.7.3 SR3)");
        if (file.FileLevelCollating is null && file.KeyLevelCollating.Count == 0) return;   // no clause → native

        // ⛔ THIS IS §12.4.5.2 SR8's FIRST SENTENCE over the collating-sequence-clause operand, not §12.4.5.7.1.
        // The clause appears ONLY in §12.4.5.1's Format 1 (indexed), so writing it specifies Format 1, and SR8 —
        // "Format 1 shall be specified only for an indexed file" — is the sentence that forbids it elsewhere.
        // §12.4.5.7.1 is the clause's GENERAL paragraph ("…for an indexed file"), which is descriptive prose and
        // states no obligation; the citation shipped here was inherited from it (CLAUDE.md rule 1, kb/Work
        // PB742). The rule's other operands — the RECORD KEY, ALTERNATE RECORD KEY and RELATIVE KEY clauses —
        // are rows of FileControlKeyRules; this one stays here because the test is also the GUARD that stops the
        // rest of this method (there are no keys on a non-indexed file to resolve alphabets for), and splitting
        // a guard from its report would leave the resolution running on an entry it has already refused.
        if (file.Organization != FileOrganization.Indexed)
        {
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': a file-control COLLATING "
                + "SEQUENCE clause is a Format-1 clause and shall be specified only for an indexed file — this "
                + $"file is {file.OrganizationFace} (ISO §12.4.5.2 SR8)");
            return;
        }

        // SR4/SR5: every Format-2 operand shall name a declared RECORD KEY or ALTERNATE RECORD KEY of this file — SR4
        // a data-name-1 key, SR5 a SOURCE-form key by its record-key-name. A data-name operand is a qualified
        // data-name (kb/Work PB1075), so it is RESOLVED — §8.4.2.2 over the file's key items — and compared by ITEM:
        // two files' keys may share a name, and only qualification tells them apart.
        var fileKeys = new List<RecordKey>();
        if (file.PrimeKey is { } pk) fileKeys.Add(pk);
        fileKeys.AddRange(file.AlternateKeys.Select(a => a.Key));
        // SR8: "Neither data-name-1 nor record-key-name-1 shall be specified in more than one COLLATING SEQUENCE
        // clause." The boundary is the CLAUSE, and the screen is per OPERAND *within* it: §12.4.5.7.2's Format-2
        // figure is `OF { data-name-1 | record-key-name-1 } … IS alphabet-name-3`, and by §5.2.7 the ellipsis
        // applies to the brace group, so ONE clause may list the same key twice — and one clause is never "more
        // than one". Hence the shared ConstructOperandRegister (the clause is the construct), never a HashSet
        // hoisted out of the clause loop: that screened per operand across ALL clauses at once and rejected
        // `COLLATING SEQUENCE OF IX-KEY IX-KEY IS ALPHA-X` with a diagnostic the program itself falsified
        // (kb/Work PB703; PB364 is the same shape on §14.9.49.3's USE rules, the register's first consumer).
        // GR6 is unaffected by the repeat: ResolveKeyCollating takes the FIRST clause naming the key, and a key
        // listed twice in one clause is named by that one clause, so it resolves to that clause's alphabet-name-3.
        var namedIn = new ConstructOperandRegister<RecordKey>(ReferenceEqualityComparer.Instance);
        foreach (var (keys, alphabet3, at) in file.KeyLevelCollating)
        {
            using var _ = Edition.At(at);
            foreach (var k in keys)
            {
                string written = DataBinder.WrittenQualified(k.Name, k.Qualifiers);
                if (_host.RefusedClauseOperands.Contains(k.Name)) continue;   // SR6 / shape — refused at capture
                k.Key = CollatingKeyNamed(file, k, fileKeys, written);
                if (k.Key is null) continue;
                if (namedIn.Register(k.Key) is ConstructOperand.Duplicate)   // an EARLIER clause named it
                    Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': key '{written}' is "
                        + "named in more than one COLLATING SEQUENCE clause (ISO §12.4.5.7.3 SR8)");
            }
            namedIn.EndConstruct();   // the clause is complete — a repeat from here on is across clauses
            // SR7 — alphabet-name-3 against the class of EACH key the clause names: "When the class of data-name-1
            // or record-key-name-1 is national, alphabet-name-3 shall reference an alphabet that defines a national
            // collating sequence; otherwise, alphabet-name-3 shall reference an alphabet that defines an
            // alphanumeric collating sequence." An undeclared alphabet breaks both halves and is said once.
            if (!_host.SpecialNames.IsAlphabetName(alphabet3))
            {
                Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING SEQUENCE "
                    + $"OF {string.Join(' ', keys.Select(k => DataBinder.WrittenQualified(k.Name, k.Qualifiers)))} IS '{alphabet3}' — alphabet-name-3 names no alphabet declared in SPECIAL-NAMES "
                    + "(ISO §12.4.5.7.3 SR7)");
                continue;
            }
            foreach (var k in keys.Where(k => k.Key is not null).DistinctBy(k => k.Key!, ReferenceEqualityComparer.Instance))
                if (k.Key is { } key && ItemCategory.IsAlphanumericOrNational(key.CategorySource)
                    && _host.SpecialNames.CollatingAlphabetFault(alphabet3, IsNationalKey(key)) is { } fault)
                    Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING "
                        + $"SEQUENCE OF '{DataBinder.WrittenQualified(k.Name, k.Qualifiers)}' IS '{alphabet3}' — "
                        + $"'{DataBinder.WrittenQualified(k.Name, k.Qualifiers)}' is of class "
                        + $"{(IsNationalKey(key) ? "national" : "alphanumeric")}, so alphabet-name-3 shall reference an "
                        + $"alphabet that defines {(IsNationalKey(key) ? "a national" : "an alphanumeric")} collating "
                        + $"sequence; {fault} (ISO §12.4.5.7.3 SR7)");
        }

        // SR1/SR2 — the Format-1 alphabet names, screened as WRITTEN, whatever keys they end up reaching.
        if (file.FileLevelCollating is var (alnum, nat))
        {
            using var _ = Edition.At(file.FileLevelCollatingAt);
            if (alnum is not null && _host.SpecialNames.CollatingAlphabetFault(alnum, national: false) is { } f1)
                Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING SEQUENCE "
                    + $"'{alnum}' — alphabet-name-1 shall reference an alphabet that defines an alphanumeric collating "
                    + $"sequence; {f1} (ISO §12.4.5.7.3 SR1)");
            if (nat is not null && _host.SpecialNames.CollatingAlphabetFault(nat, national: true) is { } f2)
                Edition.Error(DiagnosticCatalog.FileCollatingAlphabet, $"file '{file.CobolName}': COLLATING SEQUENCE "
                    + $"'{nat}' — alphabet-name-2 shall reference an alphabet that defines a national collating "
                    + $"sequence; {f2} (ISO §12.4.5.7.3 SR2)");
        }

        file.PrimeKeyCollation = ResolveKeyCollating(file, file.PrimeKey);
        foreach (var alt in file.AlternateKeys)
            file.AlternateKeyCollations.Add(ResolveKeyCollating(file, alt.Key));
    }

    /// <summary>A record key's CLASS for §12.4.5.7 — national when the key is category national (a PIC N item or a
    /// GROUP-USAGE NATIONAL group), alphanumeric otherwise. Only keys §12.4.5.12.3 SR2 / §12.4.5.6.3 SR2 admit
    /// (alphanumeric or national) reach a caller.</summary>
    private static bool IsNationalKey(RecordKey key) => !ItemCategory.IsAlphanumeric(key.CategorySource);

    /// <summary>The RECORD KEY or ALTERNATE RECORD KEY one key-level COLLATING SEQUENCE operand names — ISO
    /// §12.4.5.7.3 SR4: "shall be a name specified as the data-name in an ALTERNATE RECORD KEY clause or in a
    /// RECORD KEY clause in the file control entry", SR5: "Record-key-name-1 shall be a name specified as a
    /// record-key-name in an ALTERNATE RECORD KEY clause or in a RECORD KEY clause in the file control entry" — or
    /// null, REPORTED, when it names none (kb/Work PB1075, PB1099).
    /// <para>⛔ BOTH READINGS IN ONE COUNTED SET (kb/Work PB1099 review R1-2). The operand position admits
    /// data-name-1 or record-key-name-1, and §8.3.2.2 lets one word be both, so the record-key-names this entry
    /// declares (<see cref="FileModel.RecordKeyNameCandidates"/>, which reads §8.4.2.2.2 Format 6's file-name
    /// qualifier) and the qualified data-names resolved by §8.4.2.2 and narrowed to this file's data-name-1 key
    /// items are ONE candidate set: qualification is what separates two files' same-named keys (the case that used
    /// to be a parse error), and a reference that still leaves two readings is ambiguous (§8.4.2.2.3 SR1) — never
    /// "the record-key-name first". A key clause whose own operand failed to resolve has already been reported, so
    /// an operand naming it is not reported a second time as "not a key"
    /// (<see cref="FileModel.DeclaresUnresolvedKeyNamed"/>: one fault, one verdict).</para></summary>
    private RecordKey? CollatingKeyNamed(FileModel file, CollatingKeyOperand k, IReadOnlyList<RecordKey> keys,
                                         string written)
    {
        var readings = file.RecordKeyNameCandidates(k.Name, k.Qualifiers)
            .Plus(_host.QualifiedCandidates(k.Name, k.Qualifiers, Model.Scope.Program)
                .Where(i => keys.Any(key => ReferenceEquals(key.Item, i)))
                .Select(KeyNameReading (i) => new KeyNameReading.DataName(i)));
        if (readings.Count > 0)
            return _host.UniqueOrReportAmbiguous(readings, $"file '{file.CobolName}': COLLATING SEQUENCE OF", written, out _) switch
            {
                KeyNameReading.RecordKeyName rk => file.KeyAt(rk.KeyIndex),
                KeyNameReading.DataName dn => keys.Single(key => ReferenceEquals(key.Item, dn.Item)),
                _ => null,
            };
        if (!file.DeclaresUnresolvedKeyNamed(k.Name, k.Qualifiers))
            Edition.Error(DiagnosticCatalog.FileCollatingKey, $"file '{file.CobolName}': COLLATING SEQUENCE OF "
                + $"'{written}' — '{written}' is not a RECORD KEY or ALTERNATE RECORD KEY of this file "
                + "(ISO §12.4.5.7.3 SR4/SR5)");
        return null;
    }

    /// <summary>Resolve one key's collating sequence BY THE KEY'S CLASS (§12.4.5.7.4): a Format-2 alphabet naming
    /// the key wins (GR6); else the Format-1 alphabet of the key's class — alphabet-name-1 for a key of class
    /// alphanumeric (GR2), alphabet-name-2 for a key of class national (GR3); else that class's native sequence
    /// (GR4/GR5 — null, ordinal). An alphabet whose class the screen above refused resolves to native (the compile
    /// has already failed).
    /// <para>⛔ KEY-CLASS-BLIND UNTIL kb/Work PB1074: every key took <c>keyLevel ?? FileLevelCollating.Alnum</c>, so an
    /// alphanumeric alphabet re-ordered — and re-judged the uniqueness of — a NATIONAL key (a spurious '22' on a
    /// legal WRITE), and <c>FOR NATIONAL IS alphabet-name-2</c> was stored and never read, silently.</para>
    /// <para>An ALPHANUMERIC alphabet resolves to its <see cref="AlphabetDef"/> — a literal-phrase table or, per
    /// owner decision Q3 (determination L8), a LOCALE sequence. A NATIONAL alphabet that is the native national
    /// sequence (NATIVE, UCS-4 — <see cref="NationalAlphabetDef.IsIdentity"/>) resolves to native, which is exactly
    /// GR3. One defined by literals or LOCALE is Annex A.3 item 41's processor-dependent capability, which this
    /// implementation does not provide: it is DECLINED by name (COBOL1584), and refused rather than accepted
    /// inert because an inert compile would order the key — and judge its uniqueness — by a different sequence
    /// than the one written (docs/CONFORMANCE.md §2 rows 41–42).</para></summary>
    private AlphabetDef? ResolveKeyCollating(FileModel file, RecordKey? key)
    {
        if (key is null || !ItemCategory.IsAlphanumericOrNational(key.CategorySource)) return null;   // SR2 has refused it
        bool national = IsNationalKey(key);
        string? alphabet = null;
        var where = file.FileLevelCollatingAt;
        // GR6 — the FIRST key-level clause naming this key ITEM (operands resolved in ResolveFileCollating, kb/Work
        // PB1075; they were compared by spelling, so a qualified operand could not name its key).
        foreach (var clause in file.KeyLevelCollating)
            if (clause.Keys.Any(k => ReferenceEquals(k.Key, key))) { alphabet = clause.Alphabet; where = clause.At; break; }
        alphabet ??= national ? file.FileLevelCollating?.Nat : file.FileLevelCollating?.Alnum;   // GR3 / GR2
        if (alphabet is null) return null;                                                       // GR5 / GR4

        if (!national)
            return _host.State.Configuration.Alphabets.TryPeek(alphabet, out var def) && !def.IsIdentity ? def : null;   // used at capture
        using var _ = Edition.At(where);
        if (_host.State.Configuration.NationalAlphabets.TryPeek(alphabet, out var nd) && nd.HasCollatingSequence && !nd.IsIdentity)
            Edition.Declined(DiagnosticCatalog.FileCollatingNationalUnsupported, $"file '{file.CobolName}': national "
                + $"record key '{key.Face}' under COLLATING SEQUENCE '{alphabet}' ({nd.Phrase})");
        return null;
    }

    /// <summary>Map the <c>sharingMode</c> production to the model enum. ONE mapper for BOTH of its writers —
    /// the file control entry's SHARING clause (ISO §12.4.5.15) and the OPEN statement's sharing phrase
    /// (§14.9.27.2), which are the same grammar production and therefore the same three words. It was written
    /// twice, byte-identically, until kb/Work PB319 pulled the second copy out of
    /// <c>SequentialIoBinder</c> (feedback_one_rule_one_place): a fourth mode word would otherwise have to be
    /// added in two places, and only one of them would have been.</summary>
    internal static SharingMode MapSharing(Core.SharingModeContext m) =>
        m.READ() is not null ? SharingMode.ReadOnly
        : m.NO() is not null ? SharingMode.NoOther
        : SharingMode.AllOther;   // ALL OTHER

    /// <summary>Map the LOCK MODE clause (ISO §12.4.5.9): MANUAL / AUTOMATIC + single/MULTIPLE granularity.</summary>
    private static LockModeInfo MapLockMode(Core.LockModeClauseContext lm) =>
        new(lm.AUTOMATIC() is not null ? LockKind.Automatic : LockKind.Manual,
            lm.lockOnPhrase()?.multipleKeyword() is not null);

    /// <summary>The ORGANIZATION clause (ISO §12.4.5.10.3 GR2–GR5) → the bound organization. The clause is
    /// optional and so is the absent-clause case: §12.4.5.10.3 GR6 — "When the ORGANIZATION clause is not
    /// specified, sequential organization with the RECORD SEQUENTIAL phrase is implied" — is the null arm.
    /// <para>THE ONE RECOGNITION SITE for line sequential, which is a COBOL-2023 introduction (kb/Work PB688);
    /// its edition gate is <c>VersionConformancePass.ParseArm.VisitOrganizationClause</c>, never here — this
    /// binder is edition-agnostic.</para></summary>
    private static FileOrganization MapOrganization(Core.OrganizationClauseContext org)
    {
        var t = org.organizationType();
        if (t is null) return FileOrganization.Sequential;
        if (t.LINE() is not null) return FileOrganization.LineSequential;
        if (t.RELATIVE() is not null) return FileOrganization.Relative;
        if (t.INDEXED() is not null) return FileOrganization.Indexed;
        // `RECORD? SEQUENTIAL` — with or without the optional word RECORD (§12.4.5.10.3 GR3), record sequential.
        return FileOrganization.Sequential;
    }

    private static FileAccessMode MapAccessMode(Core.AccessModeClauseContext acc)
    {
        var m = acc.accessMode();
        if (m?.RANDOM() is not null) return FileAccessMode.Random;
        if (m?.DYNAMIC() is not null) return FileAccessMode.Dynamic;
        return FileAccessMode.Sequential;
    }
}
