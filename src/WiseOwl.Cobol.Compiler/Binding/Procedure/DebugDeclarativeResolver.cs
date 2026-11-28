// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>X3.23-1985 USE FOR DEBUGGING (VCR Table 7 row 7.17; kb/Work PB2345, docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3): the debugging declaratives <see cref="ProcedureTableBuilder"/> collected, resolved to their trigger SUBJECTS (a
/// paragraph or section of the pc space) and TARGETS (a data-name, ALL REFERENCES OF, a file-name, a cd-name) once every
/// procedure is in the pc space, each operand held to the §1985:XV.3.2.3 syntax rules, and the DEBUG-CONTENTS width they
/// need. The table owns the subject and target lists the resolver appends to; it reads the pc space and the one
/// procedure-name resolution through the table.</summary>
internal sealed class DebugDeclarativeResolver(BinderContext ctx, ProcedureTableBuilder table,
    List<BoundDebugSubject> subjects, List<BoundDebugTarget> targets, int contentsWidth)
{
    /// <summary>Resolve the collected debug declaratives to trigger SUBJECTS now that the whole pc space exists
    /// (X3.23-1985 debug module, VCR Table 7 row 7.17). <c>ALL PROCEDURES</c> and a bare procedure-name bind to
    /// real trigger points at procedures outside every debugging section (a debugging declarative is never debugged
    /// — the '85 ALL PROCEDURES exclusion, DB101A "USE PROCEDURE NOT EXECUTED"): a paragraph-name is its pc, a
    /// section-name its first pc (<see cref="AddDebugSectionSubject"/>). The data-name (incl. ALL REFERENCES OF), file-name and
    /// cd-name operands become <see cref="ProcedureTableBuilder.DebugTargets"/> (<see cref="AddDebugDataTarget"/>, <see cref="AddDebugCdTarget"/>;
    /// the triggers are planned per statement, CONTROL_FLOW_DESIGN D20); a file-name operand is a target too
    /// (<see cref="AddDebugFileTarget"/>, general rule 2, kb/Work PB4811). A SORT/MERGE INPUT/OUTPUT
    /// procedure subject needs nothing here: the SORT or MERGE statement sets the cause where it transfers control
    /// (§1985:XV.3.2.4 GR19, <c>SortEmitter</c>).</summary>
    /// <returns>The DEBUG-CONTENTS width the targets need (unchanged when nothing is pending).</returns>
    public int FinalizeDebug(IReadOnlyList<(SectionInfo Section, Core.UseStatementContext Use)> pending)
    {
        if (pending.Count == 0) return _debugContentsWidth;
        // §1985:XV.3.2.3 SR7 and SR8 relate the operands of EVERY USE FOR DEBUGGING statement of the program, so the first
        // ALL PROCEDURES operand is found before any operand is recorded (kb/Work PB5081).
        var allProcedures = pending.SelectMany(p => p.Use.useDebugTarget()).FirstOrDefault(t => t.PROCEDURES() is not null);
        foreach (var (section, use) in pending)
        {
            foreach (var t in use.useDebugTarget())
            {
                // FinalizeDebug runs after every walker, so the operand positions its own diagnostics (kb/Work
                // PB2034: a refusal names the file and line the user edits).
                using var _ = ctx.Edition.At(t);
                if (t.PROCEDURES() is not null)          // ALL PROCEDURES → every procedure-name outside a debugging section
                {
                    if (ReferenceEquals(t, allProcedures)) AddAllProceduresSubjects(section);
                    else ctx.Edition.Error(DiagnosticCatalog.DebugAllProceduresConflict, $"declarative section '{section.Name}': "
                        + "USE FOR DEBUGGING ON ALL PROCEDURES is written a second time in the program; §1985:XV.3.2.3 SR7: \"The "
                        + "ALL PROCEDURES phrase can appear only once in a program.\"");
                }
                else if (t.ALL() is not null)            // ALL [REFERENCES] [OF] identifier-1 → an all-references data-name target
                {
                    // ALL, not REFERENCES, marks the arm: REFERENCES and OF are the '85 format's optional words,
                    // so `ALL W-A` is this operand too and never a procedure-name subject (kb/Work PB4237). The
                    // message names the operand, so two ALL operands of one USE are two diagnostics (DB201A).
                    // ⛔ THE OPERAND IS RESOLVED BEFORE IT IS CALLED A DATA ITEM (kb/Work PB4363): identifier-1 is a
                    // data item, and a word that names a procedure or names nothing is not the legal-but-unmodeled
                    // subject COBOL1571 tells the user about.
                    var adr = t.dataReference();
                    string anm = DebugOperandAsWritten(adr);
                    if (!NamesDataItem(adr))
                    {
                        if (ResolveDebugProcedure(adr, section, report: false) is { Procedure: not null } or { Ambiguity: not null })
                        {
                            ctx.Edition.Error(DiagnosticCatalog.DebugAllOperandIsProcedure, $"declarative section "
                                + $"'{section.Name}': USE FOR DEBUGGING ON ALL REFERENCES OF '{anm}' names a paragraph or "
                                + "section, and identifier-1 of ALL [REFERENCES OF] identifier-1 is a data item ("
                                + ctx.Edition.Edition.CiteRule("§1985:XV.3.2.2", "§2023:14.4.1") + ": a "
                                + $"procedure-name refers to a paragraph or section); write ON {anm} for a procedure-name subject");
                            continue;
                        }
                        if (ReportUnresolvedDebugOperand(adr, section)) continue;
                    }
                    AddDebugDataTarget(adr, section, DebugTargetKind.AllReferences);
                }
                else if (t.dataReference() is { } dr)    // bare name: procedure-name, else file/data/cd (staged)
                {
                    string nm = DebugOperandAsWritten(dr);
                    // ⛔ THE SAME §2023:8.4.2.2 RESOLUTION AS EVERY OTHER PROCEDURE-NAME REFERENCE, through the same
                    // method (kb/Work PB466) — USE FOR DEBUGGING ON procedure-name is an explicit reference, so
                    // an ambiguous spelling owes the same diagnostic here as it does at GO TO. The referencing
                    // section is THIS declarative section, which is what §2023:8.4.2.2.1 SR6 is measured against.
                    // ⛔ ONE PROCEDURE-NAME IS ONE TRIGGER POINT (§1985:XV.3.2.4 GR3 a: "Immediately before each execution
                    // of the named procedure"): a paragraph is its own pc, and a SECTION is executed when control
                    // reaches its FIRST paragraph, so it is one trigger there, named by the section. Treating the section
                    // as every one of its paragraphs ran the debugging section once per paragraph under the paragraph's
                    // name (kb/Work PB5083, GR9 and GR16).
                    var subject = ResolveDebugProcedure(dr, section, report: true);
                    if (subject.Ambiguity is { } dambiguity)
                        ctx.Validation.RejectAmbiguousProcedureName(dambiguity, "USE FOR DEBUGGING ON");
                    else if (subject.Procedure is { } named)
                        AddNamedDebugSubject(named, nm, section, allProcedures);
                    else if (NamesDataItem(dr))
                        AddDebugDataTarget(dr, section, DebugTargetKind.DataName);
                    else if (NamesCommunicationDescription(dr))
                        AddDebugCdTarget(dr, section);
                    else if (NamesFile(dr))
                        AddDebugFileTarget(dr, section);
                    else if (!ReportUnresolvedDebugOperand(dr, section))
                        ctx.Edition.Error(DiagnosticCatalog.DebugSubFacilityStaged, $"declarative section '{section.Name}': USE FOR DEBUGGING "
                            + $"ON '{nm}' names an operand no rule classifies as a procedure-name, data-name, file-name or cd-name "
                            + "(X3.23-1985 USE FOR DEBUGGING general format; VCR Table 7 row 7.17)");
                }
            }
        }

        // General rule 15: DEBUG-CONTENTS is "large enough to contain the data required" — the widest data-name or cd-name target,
        // and, when a paragraph is a subject, the widest procedure-name-2 of an ALTER (GR17 c: the name is the contents).
        if (subjects.Any(s => !s.IsSection))
            foreach (var para in table.Paragraphs)
                foreach (var alter in para.Sentences.SelectMany(sentence => ParseTreeWalk.SelfAndDescendants(sentence)).OfType<Core.AlterStatementContext>())
                    foreach (var entry in alter.alterEntry())
                        if (entry.procedureName() is { Length: >= 2 } names)
                            _debugContentsWidth = Math.Max(_debugContentsWidth, ProcedureNameShape.AsWritten(names[1]).Length);
        ctx.Data.Symbols.ActivateDebugRegisters(_debugContentsWidth);
        return _debugContentsWidth;
    }

    /// <summary>The operand of a USE FOR DEBUGGING target as a message quotes it: the word and its OF/IN qualifiers
    /// as written, <c>P1 OF S1</c>. <c>GetText()</c> of the node would glue the words (<c>P1OFS1</c>, the class
    /// kb/Work PB4252 and PB4277 record), and a subscript or reference modifier is left off as it always was.</summary>
    private static string DebugOperandAsWritten(Core.DataReferenceContext operand)
    {
        // A counter register (LINAGE-COUNTER, LINE-COUNTER, PAGE-COUNTER) is a keyword token, and its cobolWord() is the QUALIFIER:
        // X3.23-1985 USE FOR DEBUGGING GR13 (§1985:XV.3.2.4) names the word and all its qualifiers separated by IN or OF.
        if (operand.GetChild(0) is Antlr4.Runtime.Tree.ITerminalNode keyword)
            return operand.cobolWord() is { } file ? $"{keyword.GetText()} {operand.GetChild(1).GetText()} {file.GetText()}" : keyword.GetText();
        string written = operand.cobolWord()?.GetText() ?? operand.GetText();
        foreach (var suffix in operand.dataReferenceSuffix())
            if (suffix.qualification() is { } q)
                written += " " + q.GetChild(0).GetText() + " " + q.cobolWord().GetText();
        return written;
    }

    /// <summary>A USE FOR DEBUGGING operand read as a <c>procedure-name</c> (kb/Work PB4363). The grammar spells the operand as a
    /// dataReference because the phrase also admits file-, cd- and data-names; the table reads its procedure-name shapes,
    /// <c>word</c> and <c>word OF section</c> (ISO §2023:14.4.1: "a paragraph-name (that may be qualified) or a section-name"), and
    /// any other suffix (a subscript, a reference modifier, a property) is no procedure-name and identifies nothing
    /// (<c>SymbolTable.ProcedureWords</c>). Both operand arms of <see cref="FinalizeDebug"/> ask it, so a qualified procedure-name is a
    /// procedure-name in either: the ALL REFERENCES OF arm as a probe, the subject arm as the reference.</summary>
    private ProcedureResolution ResolveDebugProcedure(Core.DataReferenceContext operand, SectionInfo section, bool report) =>
        table.Identified(operand, section, report);

    /// <summary>Whether the operand names a data item the program declares, or a special register — the §2023:8.4.2.2
    /// lookup alone, asked silently (<see cref="ReferenceResolver.ProbeDeclaration"/>), so the answer is about the
    /// DECLARATION. The reference SHAPE screens that <see cref="ReferenceResolver.Probe"/> applies are left out on
    /// purpose: the debug identifier of a table element is written without its subscripts (the DEBUG-SUB-n items
    /// report them, X3.23-1985), so that operand is the data item the trigger watches, not one the
    /// COBOL2270 "shall be subscripted" rule (ISO §2023:8.4.2.3.3 SR5) judges (kb/Work PB4363, review round 1).</summary>
    private bool NamesDataItem(Core.DataReferenceContext operand) =>
        operand.Register() != SpecialRegister.None || ctx.Refs.ProbeDeclaration(operand) is not null
        || SumCounterOf(operand) is not null;

    /// <summary>The register of the REPORT SECTION sum counter the operand names, or null: the item every reference to the counter carries
    /// (<see cref="ReportSumCounterPlace.RegisterItem"/>), which is what the trigger watches. X3.23-1985 USE FOR DEBUGGING syntax rule 9
    /// admits it by name: "Identifier-1 must not reference any data item defined in the Report Section except sum counters" (kb/Work PB4858).
    /// A silent probe, like <see cref="NamesDataItem"/>.</summary>
    private DataItem? SumCounterOf(Core.DataReferenceContext operand) =>
        operand.Register() == SpecialRegister.None && operand.cobolWord() is not null
        && ctx.Refs.Counters.SumCounterFamilyFor(operand, report: false) is (_, { } family)
            ? family.Register : null;

    /// <summary>Whether the operand is a bare file-name of this program (the file subject of USE FOR DEBUGGING).</summary>
    private bool NamesFile(Core.DataReferenceContext operand) =>
        operand.dataReferenceSuffix().Length == 0 && operand.cobolWord() is { } word
        && ctx.Symbols.IdentifyFile(word, report: false) is not null;

    /// <summary>Whether the operand is a bare cd-name of this program (the cd-name subject of USE FOR DEBUGGING: X3.23-1985's debug
    /// module triggers on the ENABLE, DISABLE, PURGE, RECEIVE and SEND of a cd-name, kb/Work PB4713). A cd-name is this source
    /// element's alone (ISO/IEC 1989:2002 8.4.5), so the element's own slice is the whole scope.</summary>
    private bool NamesCommunicationDescription(Core.DataReferenceContext operand) =>
        operand.dataReferenceSuffix().Length == 0 && operand.cobolWord() is { } word
        && ctx.Symbols.Identify(word, NameClass.CommunicationDescription, report: false) is not null;

    /// <summary>An operand that is no procedure, data item, file or cd-name is told why. A qualified word whose HEAD is a
    /// paragraph or section the program declares (<c>P1 OF NOSEC</c>) is a procedure-name that identifies nothing,
    /// and is reported as every other procedure-name operand is (<c>RejectProcedureName</c>, COBOL1639): the data
    /// resolver's "no declaration gives the name P1" would send the user hunting for a declaration that is there.
    /// Any other operand is asked of the ONE reference resolver, which says why — "'X' is not defined" for a name
    /// nothing declares (COBOL1639, ISO §2023:8.4.2.1: "a statement shall contain a reference that uniquely identifies
    /// that resource"), the ambiguity for a name several declarations share (§2023:8.4.2.2.3 SR1). True when it
    /// reported; false when it found nothing to say, and the caller falls back to the staged COBOL1571 rather than
    /// let an operand through in silence.</summary>
    private bool ReportUnresolvedDebugOperand(Core.DataReferenceContext operand, SectionInfo section)
    {
        if (operand.dataReferenceSuffix().Length > 0 && operand.cobolWord() is { } head
            && table.Identified(head, section, report: false) is { Procedure: not null } or { Ambiguity: not null })
        {
            ctx.Validation.RejectProcedureName(DebugOperandAsWritten(operand), head.GetText(), "USE FOR DEBUGGING ON", "",
                ctx.CurrentMethodScope is not null);
            return true;
        }
        int recorded = ctx.Edition.ErrorsRecorded;
        ctx.Refs.Resolve(operand);
        return ctx.Edition.ErrorsRecorded != recorded;
    }

    /// <summary>Record the paragraph subject at <paramref name="pc"/> (its name, its source line and the debugging
    /// section's invokable pc range). A paragraph-name omitted (§2023:14.4.3) is no procedure-name for a subject
    /// to name. A procedure is named once: <see cref="AddNamedDebugSubject"/> refuses a repeat (syntax rule 6).</summary>
    private void AddDebugSubject(int pc, SectionInfo debugging, string? written = null)
    {
        if (pc < 0 || pc >= table.Paragraphs.Count || table.Paragraphs[pc].Cobol.Length == 0) return;
        AddDebugSubject(new BoundDebugSubject(
            pc, written ?? table.Paragraphs[pc].Cobol, table.ParaLines[pc], debugging.StartPc, debugging.EndPc));
    }

    /// <summary>Record a SECTION subject: one trigger point at the section's first pc, named by the section
    /// (§1985:XV.3.2.4 GR13: DEBUG-NAME is "the name that caused the debugging section to be executed"), so control
    /// entering the section anywhere else does not run the debugging section (GR9).</summary>
    private void AddDebugSectionSubject(SectionInfo subject, SectionInfo debugging)
    {
        int pc = subject.StartPc;
        AddDebugSubject(new BoundDebugSubject(
            pc, subject.Name, table.ParaLines[pc], debugging.StartPc, debugging.EndPc, IsSection: true, SubjectEndPc: subject.EndPc));
    }

    private void AddDebugSubject(BoundDebugSubject subject) => subjects.Add(subject);

    /// <summary>Record the procedure-name subject <paramref name="named"/> (procedure-name-1 of the USE FOR DEBUGGING format), held to
    /// the three syntax rules that speak of one (kb/Work PB5081): §1985:XV.3.2.3 SR5, "Procedure-names defined within debugging
    /// sections must not appear within USE FOR DEBUGGING statements."; SR8, no procedure-name-1 in a program with ALL PROCEDURES
    /// (<paramref name="allProcedures"/>); and SR6, a procedure-name "may appear in only one USE FOR DEBUGGING statement and may
    /// appear only once in that statement" — the repeat the subject list used to swallow silently. A paragraph is one trigger
    /// point and a section another, at its first paragraph (§1985:XV.3.2.4 GR3 a, kb/Work PB5083), so P and the section that
    /// begins with P are two procedure-names.</summary>
    private void AddNamedDebugSubject(ResolvedProcedure named, string written, SectionInfo debugging, Core.UseDebugTargetContext? allProcedures)
    {
        if (named.Section is { IsDebugging: true } home)
        {
            ctx.Edition.Error(DiagnosticCatalog.DebuggingProcedureReferenced, $"declarative section '{debugging.Name}': USE FOR "
                + $"DEBUGGING ON '{written}' names a procedure of the debugging section '{home.Name}'; §1985:XV.3.2.3 SR5: "
                + "\"Procedure-names defined within debugging sections must not appear within USE FOR DEBUGGING statements.\"");
            return;
        }
        if (allProcedures is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.DebugAllProceduresConflict, $"declarative section '{debugging.Name}': USE FOR "
                + $"DEBUGGING ON '{written}' names a procedure-name in a program whose USE FOR DEBUGGING ON ALL PROCEDURES already "
                + "makes it a subject; §1985:XV.3.2.3 SR8: \"When the ALL PROCEDURES phrase is specified, procedure-name-1 must "
                + "not be specified in any USE FOR DEBUGGING statement.\"");
            return;
        }
        int pc = named.Range.Start;
        if (subjects.Any(s => s.SubjectPc == pc && s.IsSection == named.IsSection))
        {
            ctx.Edition.Error(DiagnosticCatalog.DebugTargetRepeated, $"declarative section '{debugging.Name}': USE FOR DEBUGGING "
                + $"ON '{written}' names a procedure that another USE FOR DEBUGGING operand names; X3.23-1985 USE FOR DEBUGGING "
                + "syntax rule 6 allows a procedure-name in one USE FOR DEBUGGING statement, once");
            return;
        }
        // GR13: DEBUG-NAME holds the name that caused the section to run, "All qualifiers of the name ... separated
        // by the word 'IN' or 'OF'": a paragraph subject written P OF S is named so, as the USE statement wrote it.
        if (!named.IsSection) AddDebugSubject(pc, debugging, written);
        else if (named.Section is { } namedSection) AddDebugSectionSubject(namedSection, debugging);
    }

    /// <summary>ALL PROCEDURES (§1985:XV.3.2.4 GR4): "the effects described in general rule 3 ... for every
    /// procedure-name in the program, except those appearing within a debugging section" - each paragraph and each
    /// section, the declarative sections that are not debugging sections included (a USE procedure is executed, GR21).</summary>
    private void AddAllProceduresSubjects(SectionInfo debugging)
    {
        for (int pc = 0; pc < table.Paragraphs.Count; pc++)
        {
            var owner = table.ParaSections[pc];
            if (owner is { IsDebugging: true }) continue;
            AddDebugSubject(pc, debugging);
            if (owner is { } sec && sec.StartPc == pc) AddDebugSectionSubject(sec, debugging);
        }
    }

    /// <summary>Record one data-name or ALL REFERENCES OF target. The operand is resolved to its DATA ITEM (a qualified name is the one
    /// item its qualifiers pick, §2023:8.4.2.2), written without subscripts or a reference modifier (syntax rules 10 and 12: the trigger watches
    /// the item, and DEBUG-SUB-n report which occurrence a statement referenced) and named once (syntax rule 6). A report sum counter is its
    /// register item (syntax rule 9). LINAGE-COUNTER, LINE-COUNTER and PAGE-COUNTER are special-register data items nothing in the format
    /// excludes (kb/Work PB4858), runtime-sourced and no place: the target names the register (<see cref="CounterRegisterOf"/>).</summary>
    private void AddDebugDataTarget(Core.DataReferenceContext operand, SectionInfo section, DebugTargetKind kind)
    {
        string written = DebugOperandAsWritten(operand);
        string phrase = kind is DebugTargetKind.AllReferences ? $"ON ALL REFERENCES OF '{written}'" : $"ON '{written}'";
        DataItem? item;
        BoundExpr? register = null;
        if (operand.Register() is var special and not SpecialRegister.None)
        {
            (item, register) = CounterRegisterOf(operand, special);
            if (register is null) return;   // the resolver reported why
        }
        else
            item = ctx.Refs.ProbeDeclaration(operand) ?? SumCounterOf(operand)
                ?? throw new InvalidOperationException($"USE FOR DEBUGGING {phrase}: NamesDataItem said the operand names a data item, and none resolves");
        if (operand.dataReferenceSuffix().Any(s => s.subscriptPart() is not null || s.refModPart() is not null
                || s.qualification() is { } q && (q.subscriptPart().Length > 0 || q.refModPart().Length > 0)))
        {
            ctx.Edition.Error(DiagnosticCatalog.DebugTargetSubscripted, $"declarative section '{section.Name}': USE FOR DEBUGGING {phrase} is "
                + "subscripted, indexed or reference modified; X3.23-1985 USE FOR DEBUGGING syntax rules 10 and 12 write identifier-1 without "
                + "subscripts or indexes and without a reference modifier");
            return;
        }
        if (targets.Any(t => item is not null && ReferenceEquals(t.Item, item) || register is not null && t.NamesRegister(register)))
        {
            ctx.Edition.Error(DiagnosticCatalog.DebugTargetRepeated, $"declarative section '{section.Name}': USE FOR DEBUGGING {phrase} names a "
                + "data item that another USE FOR DEBUGGING operand names; X3.23-1985 USE FOR DEBUGGING syntax rule 6 allows an identifier in "
                + "one USE FOR DEBUGGING statement, once");
            return;
        }
        targets.Add(new BoundDebugTarget(kind, written, item, null, section.StartPc, section.EndPc, Register: register));
        // A data item's widest reference is every occurrence of it, a whole table (rule 24 c; kb/Work PB4855); a counter register's image is at
        // most 10 digits, inside the minimum.
        if (register is null) _debugContentsWidth = RecordLayout.SaturatedBound(Math.Max(_debugContentsWidth, RecordLayout.AllOccurrencesImageWidth(item!)));
    }

    /// <summary>The counter register a USE FOR DEBUGGING operand names, and the data item a store to it names: the file whose LINAGE clause
    /// LINAGE-COUNTER belongs to (§2023:8.4.3.14), or the report whose LINE-COUNTER or PAGE-COUNTER it is (§2023:8.4.3.15), by the ONE resolution
    /// every counter reference takes (<see cref="CounterResolver"/>; the qualification rules of §2023:8.4.2.2.3 SR8 to SR10). Only PAGE-COUNTER can
    /// be stored into (its receiving <see cref="ReportPageCounterPlace"/> names the report's counter item); LINAGE-COUNTER and LINE-COUNTER are
    /// changed by the I-O and report engines alone, which no statement "explicitly references". A null register: the resolver has reported.</summary>
    private (DataItem? Item, BoundExpr? Register) CounterRegisterOf(Core.DataReferenceContext operand, SpecialRegister special)
    {
        if (special is SpecialRegister.LinageCounter)
            return (null, ctx.Refs.Counters.LinageFileOf(operand) is { } file ? new BoundLinageCounterRef(file) : null);
        bool isPage = special is SpecialRegister.PageCounter;
        return ctx.Refs.Counters.ReportOf(operand) is { } report
            ? (isPage ? report.PageCounterRegister : null, new BoundReportCounterRef(report, isPage, ctx.Data.State.Reports.Depth(report)))
            : (null, null);
    }

    /// <summary>Record one cd-name target (X3.23-1985 USE FOR DEBUGGING general rule 8): the communication description the operand names, whose
    /// area DEBUG-CONTENTS holds (general rule 23 e).</summary>
    private void AddDebugCdTarget(Core.DataReferenceContext operand, SectionInfo section)
    {
        string written = DebugOperandAsWritten(operand);
        var cd = (CommunicationDescription)ctx.Symbols.Identify(operand.cobolWord()!, NameClass.CommunicationDescription, report: true)!.Symbol;
        if (targets.Any(t => t.CdKey == cd.Key))
        {
            ctx.Edition.Error(DiagnosticCatalog.DebugTargetRepeated, $"declarative section '{section.Name}': USE FOR DEBUGGING ON '{written}' names a "
                + "cd-name that another USE FOR DEBUGGING operand names; X3.23-1985 USE FOR DEBUGGING syntax rule 6 allows a cd-name in one "
                + "USE FOR DEBUGGING statement, once");
            return;
        }
        targets.Add(new BoundDebugTarget(DebugTargetKind.CdName, written, null, cd.Key, section.StartPc, section.EndPc));
        _debugContentsWidth = Math.Max(_debugContentsWidth, cd.Area.ImageWidth);
    }

    /// <summary>Record one file-name target (X3.23-1985 USE FOR DEBUGGING general rule 2): the file the operand names, whose largest record
    /// widens DEBUG-CONTENTS (general rules 15 and 23 c). Named once (syntax rule 6).</summary>
    private void AddDebugFileTarget(Core.DataReferenceContext operand, SectionInfo section)
    {
        string written = DebugOperandAsWritten(operand);
        if (ctx.Symbols.IdentifyFile(operand.cobolWord()!, report: true) is not { } file) return;   // NamesFile said it is one
        if (targets.Any(t => ReferenceEquals(t.File, file)))
        {
            ctx.Edition.Error(DiagnosticCatalog.DebugTargetRepeated, $"declarative section '{section.Name}': USE FOR DEBUGGING ON '{written}' names a "
                + "file-name that another USE FOR DEBUGGING operand names; X3.23-1985 USE FOR DEBUGGING syntax rule 6 allows a file-name in "
                + "one USE FOR DEBUGGING statement, once");
            return;
        }
        targets.Add(new BoundDebugTarget(DebugTargetKind.FileName, written, null, null, section.StartPc, section.EndPc, file));
        _debugContentsWidth = Math.Max(_debugContentsWidth, file.AreaRecord?.ImageWidth ?? 0);
    }

    /// <summary>The widest DEBUG-CONTENTS any target needs, as <see cref="FinalizeDebug"/> collects them (seeded with the table's).</summary>
    private int _debugContentsWidth = contentsWidth;
}
