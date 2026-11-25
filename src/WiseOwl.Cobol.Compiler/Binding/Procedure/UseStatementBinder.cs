// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Symbols;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>A USE statement's trigger scope (ISO §2023:14.9.49; kb/Work PB2345, docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3): Format 1's files or open mode with GLOBAL, Format 2's report group, Format 3's exception-name/file entries and
/// Format 4's class or interface, each operand screened by the rules that bar a repeat across the USE statements of one
/// procedure division (§2023:14.9.49.3 SR7, SR8, SR9, SR14). One instance per procedure division:
/// <see cref="ProcedureTableBuilder"/> collects the declarative sections into the pc space and takes a fresh one for each
/// method's declaratives.</summary>
internal sealed class UseStatementBinder(BinderContext ctx)
{
    // ── The USE duplicate-operand screens (ISO §2023:14.9.49.3 SR7, SR8, SR9, SR14) ──────────────────────────────
    // Four instances of ONE sentence — "the same X shall not be specified in more than one USE statement within
    // the same procedure division" — so ONE mechanism enforces all four (ConstructOperandRegister{TKey} holds
    // the construct-vs-operand boundary those rules draw; the USE STATEMENT is the construct here, the Format-2
    // COLLATING SEQUENCE CLAUSE is the construct for its second consumer). ⛔ PER PROCEDURE DIVISION: a
    // ProcedureTableBuilder is built per bound unit, and a CLASS unit's one builder collects EVERY method's
    // procedure division (kb/Work PB1010 — a method definition takes Format 1, §2023:14.2.2 SR10), so
    // CollectMethodDeclaratives re-makes these four for each method's declaratives: a file named in one
    // method's USE is not "more than one USE statement within the same procedure division" as another's.
    // SR8 and SR14 key the FILE a file-name identified (a FileModel is equal only to itself), never its spelling, and SR14's
    // exception-name is the catalog's own spelling (EcNameResolution answers it), so neither compares written names: that
    // comparison is the symbol table's (docs/rearchitecture/DESIGN-listing.md section 6.7.1).
    private readonly ConstructOperandRegister<int> _useModes = new();                                      // SR7
    private readonly ConstructOperandRegister<FileModel> _useFiles = new();                                // SR8
    private readonly ConstructOperandRegister<ReportGroupModel> _useReportGroups = new();                   // SR9
    private readonly ConstructOperandRegister<(string Ec, FileModel File)> _useEcPairs = new();             // SR14

    /// <summary>Close the USE statement just bound: its operands become the procedure division's, so a repeat
    /// in a LATER USE statement is the violation SR7/SR8/SR9/SR14 name and a repeat inside the statement itself
    /// never was. Called from the ONE place a USE statement is bound (<see cref="ProcedureTableBuilder"/>'s declarative section walk).</summary>
    public void DeclEndUseStatement()
    {
        _useModes.EndConstruct();
        _useFiles.EndConstruct();
        _useReportGroups.EndConstruct();
        _useEcPairs.EndConstruct();
    }

    /// <summary>One USE statement's bound trigger scope: Format 1's files/mode (+GLOBAL), Format 2's report
    /// group, Format 3's (exception-name, file) entries, or Format 4's single operand (ISO §2023:14.9.49).</summary>
    internal readonly record struct DeclScope(
        IReadOnlyList<FileModel> Files, int? ModeIndex, bool Global, ReportGroupModel? Report,
        IReadOnlyList<BoundExceptionSelector>? EcEntries = null, BoundEoOperand? Eo = null);

    /// <summary>Bind the USE statement's trigger scope (ISO §2023:14.9.49): Format 1's file list or open mode; the
    /// GLOBAL phrase drives the cross-program GR4b dispatch (the emitter's <c>__RunGlobalUse</c> containment
    /// walk). <c>ON file-name</c> resolves against <c>SymbolTable.VisibleFiles</c>, which includes containers' GLOBAL FDs
    /// (§2023:13.18.27.4 GR1; IC234A's contained USE names the outer's GLOBAL file).
    /// Format 2 (BEFORE REPORTING, SR9) names a report group — the section becomes the group's
    /// before-reporting hook, invoked by the report engine just before the group is produced (GR8; wired in
    /// <c>CSharpEmitter.ReportWriter.cs</c>). The same group shall not appear in two such statements (SR9).</summary>
    public DeclScope? DeclBindUse(Core.UseStatementContext use, string sectionName)
    {
        bool global = use.GLOBAL() is not null;
        if (use.exceptionSelectionOperand() is { Length: > 0 } ecEntries)
            return DeclBindUseF3(ecEntries, sectionName);
        if (use.OBJECT() is not null || use.EO() is not null)
        {
            // Format 4 (§2023:14.9.49.2 — a brace group requiring exactly one of TWO alternatives,
            // {object-class-name-1 | interface-name-1}; SR15 EO ≡ EXCEPTION OBJECT). GR3: for an OBJECT raise,
            // F4 selection REPLACES the F1/F3 tiers (the generated __EcObjDispatch, D-EO7).
            // use-after-exception-object-2002: the pass owns the edition gate (Exec Step E).
            ctx.EcState.F3 = true;   // the ONE "EC declaratives present" feature bit — F4 rides the same group gate
            string cname = use.cobolWord().GetText();
            // SR16 and SR17 are ONE rule with two operand kinds, and both scope the name to the REPOSITORY
            // paragraph — never to the compilation group, which is what OoClassTable holds. Both halves go
            // through the §2023:8.4.6.4 funnel; before kb/Work PB365 this site called `OoClasses.Find` directly, so a
            // program with NO REPOSITORY compiled clean (SR16 unenforced) and the interface alternative had no
            // resolution path at all and died on SR16's diagnostic (SR17 unimplemented).
            var r = ctx.Data.OoClasses!.Names.Resolve(ctx.Edition, use, use.cobolWord(),
                Compiler.Oo.OoNameWant.Either,
                $"declarative section '{sectionName}': USE AFTER EXCEPTION OBJECT",
                DiagnosticCatalog.UseExceptionObjectName.Code,
                EditionInfo.Designated(ctx.Edition.Edition.Cite("§2002:14.8.45.2 SR16/SR17", "§2023:14.9.49.3 SR16/SR17",
                    y2002: "§2002:14.8.45.2 SR16/SR17", y2014: "§2014:14.9.45.2 SR16/SR17")));
            if (!r.Ok) return null;
            // The bound operand carries the RESOLVED SYMBOL, never a C# type name: how many emitted types one
            // COBOL name selects is GR14's question and the emitter's answer (kb/Work PB366 — a class is TWO).
            return new DeclScope([], null, global, null, Eo: r.Interface is { } ifc
                ? new BoundEoInterface(ifc)          // SR17 / GR14 b) — the IMPLEMENTS test
                : new BoundEoClass(r.Class!));       // SR16 / GR14 a) — class-or-subclass, factory or instance
        }
        if (use.REPORTING() is not null)
        {
            // Format 2: USE [GLOBAL] BEFORE REPORTING identifier-1 — identifier-1 references a report group
            // (SR9), optionally qualified by its report-name (§2023:8.4.2.2.2 Format 1's file-report-qualifier).
            // Resolution is the ONE funnel's: it collects EVERY candidate and diagnoses an ambiguous reference
            // (§2023:8.4.2.2.1 / §2023:8.4.2.2.3 SR1) where this arm used to `return` inside the loop on the first match
            // and therefore could never observe a second (kb/Work PB365).
            var (head, _) = ReportGroupResolution.Parts(use.reportGroupReference());
            var match = ReportGroupResolution.Resolve(ctx.Data, use.reportGroupReference(),
                $"declarative section '{sectionName}': USE BEFORE REPORTING", out _, out var group, NameReach.Visible);
            if (match == ReportGroupResolution.Match.Ambiguous) return null;   // COBOL1920 is reported; no group to record (kb/Work PB5542)
            if (match == ReportGroupResolution.Match.None || group is null)
            {
                ctx.Edition.Error(DiagnosticCatalog.DeclarativeUseRule, $"declarative section '{sectionName}': USE BEFORE REPORTING "
                    + $"'{head}' does not name a report group (ISO §2023:14.9.49.3 SR9)");
                return null;
            }
            // SR9: the same identifier-1 shall not appear in more than one USE BEFORE REPORTING
            // statement within the same procedure division. Format 2 names exactly one group per
            // statement, so only the Duplicate verdict is reachable here.
            if (_useReportGroups.Register(group) == ConstructOperand.Duplicate)
            {
                using var duplicateAt = ctx.Edition.At(use.reportGroupReference());   // the repeating operand (kb/Work PB5090)
                ctx.Edition.Error(DiagnosticCatalog.DeclarativeUseRule, $"declarative section '{sectionName}': report group "
                    + $"'{head}' already has a USE BEFORE REPORTING procedure in this procedure "
                    + "division (ISO §2023:14.9.49.3 SR9)");
            }
            return new DeclScope([], null, global, group);
        }
        var target = use.useOnTarget();
        if (target is null) return null;

        // Mode scope (GR3b/GR6b–e) — the index IS the runtime FileOpenMode ordinal, mapped by the one mapping the
        // PERFORM Format-3 WHEN EXCEPTION mode operand shares.
        int? mode = OpenModeOperand.ExceptionModeIndex(target.openMode());
        if (mode is { } m)
        {
            // SR7: the INPUT, OUTPUT, I-O and EXTEND phrases may each be specified only once in the
            // declaratives portion of a given procedure division. Format 1's brace group takes exactly one
            // alternative per statement, so only the Duplicate verdict is reachable here.
            if (_useModes.Register(m) == ConstructOperand.Duplicate)
            {
                using var duplicateAt = ctx.Edition.At(target);   // the repeating USE statement's operand (kb/Work PB5090)
                ctx.Edition.Error(DiagnosticCatalog.DeclarativeUseRule, $"declarative section '{sectionName}': this open mode already "
                    + $"has a USE procedure in this procedure division ({EditionInfo.Designated(new EditionCite("§2023:14.9.49.3 SR7", "§1985:VII.4.6.3 SR6").At(ctx.Edition.Edition.Year, FileOrganization.Sequential))})");
            }
            return new DeclScope([], m, global, null);
        }

        var files = new List<FileModel>();
        foreach (var fn in target.fileName())
        {
            if (ResolveUseFile(fn, sectionName) is not { } file) continue;
            string fname = fn.GetText();
            // SR8: "The same file-name shall not appear in more than one USE AFTER EXCEPTION statement within
            // the same procedure division." ONE statement writing the name twice is not more than one — the
            // Format-1 figure's `{ file-name-1 } …` permits the repetition and GR6 a) associates the one
            // procedure with the file however many times it is written — so the repeat binds ONCE and is
            // silent. It must not bind twice: the emitted `switch (__f)` would carry two identical case
            // labels (DispatchEmitter / EcEmitter EmitUseTiers).
            switch (_useFiles.Register(file))
            {
                case ConstructOperand.Repeated:
                    continue;
                case ConstructOperand.Duplicate:
                    using (ctx.Edition.At(fn))   // the file-name that repeats (kb/Work PB5090)
                        ctx.Edition.Error(DiagnosticCatalog.DeclarativeUseRule, $"declarative section '{sectionName}': file '{fname}' "
                            + $"already has a USE procedure in this procedure division ({EditionInfo.Designated(new EditionCite("§2023:14.9.49.3 SR8", "§1985:VII.4.6.3 SR3", "§1985:VIII.4.8.3 SR3", "§1985:IX.4.8.3 SR3").At(ctx.Edition.Edition.Year, file.Organization))})");
                    continue;   // §2023:14.9.49.4 GR3 recovery — the FIRST declarative keeps the file
            }
            files.Add(file);
        }
        return new DeclScope(files, null, global, null);
    }

    /// <summary>Bind a Format-3 USE statement's scope (ISO §2023:14.9.49.2 — <c>USE AFTER {EXCEPTION CONDITION | EC}
    /// {exception-name-1 | exception-name-2 {FILE file-name-2}…}…</c>): validate every exception-name against
    /// the §2023:14.6.13.1 catalog (level 1/2/3 all legal — the §2023:14.9.49.4 GR3c–g tiers select by level), SR13 (a file-scoped
    /// name shall begin EC-I-O), SR14 (the same (exception-name-2, file-name-2) PAIR in more than one USE
    /// statement of this procedure division — a bare exception-name-1 is outside that rule and is bound as
    /// written), and the per-name edition window. The whole format is 2002+ (the EC model's introduction).</summary>
    private DeclScope? DeclBindUseF3(Core.ExceptionSelectionOperandContext[] entries, string sectionName)
    {
        // use-after-exception-condition-2002: the pass owns the edition gate (Exec Step E).
        ctx.EcState.F3 = true;
        var pairs = new List<BoundExceptionSelector>();
        // Each entry resolves through the one resolver a PERFORM Format-3 WHEN operand shares (§2023:14.9.28.4 GR17; kb/Work
        // PB6139). ⚠ Its name funnel gates the introduction edition at EVERY level where this site's former copy guarded
        // Level == 3 — a LEVEL-2 name of a 2023-only family (EC-MCS) in a USE entry slipped the old gate at 2002/2014.
        foreach (var entry in entries)
            ExceptionSelectionOperands.Resolve(ctx, entry, $"declarative section '{sectionName}'",
                refuseFilePairing: (_, info) =>
                    ctx.Edition.Error(DiagnosticCatalog.UseFileExceptionNameNotIo, $"declarative section '{sectionName}': FILE may be specified "
                        + $"only with an exception-name beginning 'EC-I-O' — '{info.Name}' does not (ISO §2023:14.9.49.3 SR13)"),
                resolveFile: fn => ResolveUseFile(fn, sectionName),
                select: (selector, _) =>
                {
                    // A BARE name is exception-name-1, not exception-name-2 (§2023:14.9.49.2's Format-3 figure puts
                    // exception-name-2 only in the `exception-name-2 { FILE file-name-2 } …` alternative, and
                    // §2023:14.9.49.4 GR3c–GR3d against GR3e–GR3g split on the same axis). SR14 governs PAIRS, so nothing
                    // screens a repeated exception-name-1 — GR3 gives it its OUTCOME instead: "The first
                    // declarative that satisfies the selection criteria is executed and no other declaratives are
                    // executed." It therefore never enters the SR14 register.
                    if (selector.File is null) pairs.Add(selector);
                    else AddPair(selector);
                });
        return new DeclScope([], null, Global: false, null, pairs);

        void AddPair(BoundExceptionSelector pair)
        {
            string ecName = pair.Ec!;
            FileModel file = pair.File!;
            // SR14: "The same pair of exception-name-2 and file-name-2 shall not be specified in more than one
            // USE statement within the same procedure division." The pair is the key (a bare exception-name-1
            // never gets here) and the STATEMENT is the boundary: the figure's inner `{ FILE file-name-2 } …`
            // and outer `…` both let ONE statement write a pair twice, which is not more than one statement.
            // The pair is the exception-name (the catalog's own spelling) and the FILE the file-name identified: two
            // file-names are one when they identify one file, so the comparison of written names stays the symbol table's.
            switch (_useEcPairs.Register((ecName, file)))
            {
                case ConstructOperand.Repeated:
                    return;   // written twice in this one statement — legal, and bound once
                case ConstructOperand.Duplicate:
                    ctx.Edition.Error(DiagnosticCatalog.UseExceptionFilePairRepeated, $"declarative section '{sectionName}': the exception-name/"
                        + $"file pair '{ecName} FILE {file.CobolName}' is already specified in another USE statement "
                        + "of this procedure division (ISO §2023:14.9.49.3 SR14)");
                    return;   // §2023:14.9.49.4 GR3 recovery — the FIRST declarative keeps the pair
            }
            pairs.Add(pair);
        }
    }

    // ⛔ NO HANDLER-END HEURISTIC LIVES HERE (kb/Work PB367 — the shape-derived `DeclHandlerEndPc` /
    // `DeclIsTrivialExit` / `DeclTerminatesRunUnit` trio is DELETED, not disabled). It walked the section's
    // paragraphs backwards and, whenever ANY later paragraph terminated the run unit, ended the bounded USE
    // dispatch at the last trivial EXIT/CONTINUE paragraph before it — so a user declarative written in that
    // ordinary shape (a common exit point, then a paragraph that STOPs the run) executed only in part, on every
    // invocation path (§2023:14.9.49.4 GR7/GR12/GR13 and the Format-2 BEFORE REPORTING hook alike). The standard
    // leaves no room for it: §2023:14.9.49.3 SR1 makes the use procedure the WHOLE remainder of the section, §2023:14.4.2
    // ends the section only at the next section header or END DECLARATIVES, §2023:14.6.3 GR1 puts the implied
    // transfer back to the controlling USE at "the last statement" of "the last procedure in the range", and
    // §2023:14.9.14.4 GR7's NOTE names USE among the return mechanisms that sit AFTER the section's last paragraph.

    /// <summary>The file a USE statement's written file-name identifies (Format 1's file-name-1 and Format 3's file-name-2 alike), or
    /// null after reporting a name that identifies no file (§2023:14.9.49) or a sort or merge file (§2023:14.9.49.3 SR2).</summary>
    private FileModel? ResolveUseFile(Core.FileNameContext fn, string sectionName)
    {
        string fname = fn.GetText();
        if (ctx.Symbols.Identify(fn, NameClass.File, report: true) is not { Symbol: FileModel file })
        {
            ctx.Edition.Error(DiagnosticCatalog.DeclarativeUseRule, $"declarative section '{sectionName}': USE names unknown "
                + $"file '{fname}' (ISO §2023:14.9.49)");
            return null;
        }
        if (file.IsSortMerge)
        {
            ReportUseNamesSortMergeFile(sectionName, fname);
            return null;
        }
        return file;
    }

    /// <summary>⛔ THE ONE REFUSAL OF A USE STATEMENT THAT NAMES A SORT OR MERGE FILE (both USE formats that name files), citing the rule of
    /// the edition in force (kb/Work PB4950): §2023:14.9.49.3 SR2 from 2002; COBOL-85's USE statements state no such syntax rule, and
    /// its ground is the sort-merge concept, "The sort file itself is referred to and accessed only by the SORT statement"
    /// (§1985:II.2.2.2.1.1; the merge file by the MERGE statement, §1985:II.2.2.2.1.2).</summary>
    private void ReportUseNamesSortMergeFile(string sectionName, string fname) =>
        ctx.Edition.Error(DiagnosticCatalog.DeclarativeUseRule, $"declarative section '{sectionName}': USE may not name the "
            + $"sort/merge file '{fname}' (" + ctx.Edition.Edition.CiteRule(
                "§1985:II.2.2.2.1.1 and §1985:II.2.2.2.1.2: a sort or merge file is referred to and accessed only by the SORT or MERGE statement",
                "§2023:14.9.49.3 SR2") + ")");
}
