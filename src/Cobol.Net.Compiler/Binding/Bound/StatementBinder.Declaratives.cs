// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// The DECLARATIVES half of the binder (ISO §14.2.4 / §14.9.49 USE): each declarative section joins the ONE pc
/// space (COBOLNET_DESIGN §14.5 — qualified-name resolution and SR4's explicit PERFORM into a declarative
/// paragraph then work unchanged), its USE sentence binds into a <see cref="BoundDeclarative"/> scope (never a
/// bound statement — USE is declaration, SR1), and the §14.9.49.4 GR7 handler exit pc is computed here (with the
/// CCVS termination-tail accommodation, see <see cref="DeclHandlerEndPc"/>).
/// </summary>
public sealed partial class StatementBinder
{
    private int _entryPc;
    private readonly List<BoundDeclarative> _declaratives = [];
    private readonly HashSet<string> _declScopedFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> _declScopedModes = [];
    private readonly HashSet<ReportGroupModel> _declReportGroups = [];   // §14.9.49 SR9 — one Format-2 USE per group

    /// <summary>Collect one declarative section into the pc space: the USE sentence (SR1 — the section's first
    /// sentence), an anonymous paragraph for any further leading sentences (the CCVS handler-before-the-first-
    /// paragraph shape, e.g. SQ103A), then the named paragraphs.</summary>
    private void DeclCollectSection(Core.DeclarativeSectionContext sec, HashSet<string> used)
    {
        string name = sec.sectionName().GetText();
        var info = new SectionInfo(name, _paras.Count);

        // SR1: the first sentence consists of exactly one USE statement.
        var leading = sec.sentence();
        DeclScope? scope = null;
        if (leading.Length == 0
            || leading[0].statement() is not { Length: 1 } first
            || first[0].useStatement() is not { } use)
            data.Edition.Error("COBOLNET0897", $"declarative section '{name}': the first sentence shall consist "
                + "of a single USE statement (ISO §14.2.4 / §14.9.49 SR1)");
        else if (use.DEBUGGING() is not null)
        {
            // X3.23-1985 USE FOR DEBUGGING (the '85 debug facility, deleted by ISO 2002 — 0902-gated ≥2002 by
            // the version-conformance pass, VCR Table 7 row 7.17). Accepted-inert at 85 per the '85 rules: WITHOUT
            // SOURCE-COMPUTER … WITH DEBUGGING MODE the whole debugging section is compiled as if it were
            // comment lines (skip it — nothing binds, its names leave the pc space); WITH the switch the
            // section IS compiled, but the object-time debug switch (implementor-defined) is permanently OFF
            // here, so no trigger ever fires (scope stays null — no BoundDeclarative). The DEBUG-ITEM register
            // family is not implemented (the full debug facility is deferred with the golden-less DB series).
            if (!data.DebuggingModeDeclared) return;
        }
        else
            scope = DeclBindUse(use, name);

        // Leading sentences past the USE form an anonymous paragraph at the section start (handler bodies that
        // CCVS writes directly under the section header).
        if (leading.Length > 1)
            AddParagraph(name, leading.Skip(1).ToArray(), info, used);
        foreach (var p in sec.declarativeParagraph())
            AddParagraph(p.paragraphName().GetText(), p.sentence(), info, used);
        // An empty handler still needs ONE pc so the bounded dispatch has a range (a no-op paragraph).
        if (_paras.Count == info.StartPc)
            AddParagraph(name, [], info, used);

        info.EndPc = _paras.Count - 1;
        _sections.TryAdd(info.Name, info);

        if (scope is { } s)
            _declaratives.Add(new BoundDeclarative(
                name, info.StartPc, info.EndPc, DeclHandlerEndPc(sec, info), s.Files, s.ModeIndex, s.Global, s.Report,
                s.EcEntries, s.EoClassCsName));
    }

    /// <summary>One USE statement's bound trigger scope: Format 1's files/mode (+GLOBAL), Format 2's report
    /// group, or Format 3's (exception-name, file) entries (ISO §14.9.49).</summary>
    private readonly record struct DeclScope(
        IReadOnlyList<FileModel> Files, int? ModeIndex, bool Global, ReportGroupModel? Report,
        IReadOnlyList<(string Ec, FileModel? File)>? EcEntries = null, string? EoClassCsName = null);

    /// <summary>Bind the USE statement's trigger scope (ISO §14.9.49): Format 1's file list or open mode; the
    /// GLOBAL phrase drives the cross-program GR4b dispatch (the emitter's <c>__RunGlobalUse</c> containment
    /// walk). <c>ON file-name</c> resolves against <c>FilesByName</c>, which includes containers' GLOBAL FDs
    /// (§13.18.30 — merged by <c>CallBindUnit</c>; IC234A's contained USE names the outer's GLOBAL file).
    /// Format 2 (BEFORE REPORTING, SR9) names a report group — the section becomes the group's
    /// before-reporting hook, invoked by the report engine just before the group is produced (GR8; wired in
    /// <c>CSharpEmitter.ReportWriter.cs</c>). The same group shall not appear in two such statements (SR9).</summary>
    private DeclScope? DeclBindUse(Core.UseStatementContext use, string sectionName)
    {
        bool global = use.GLOBAL() is not null;
        if (use.useEcEntry() is { Length: > 0 } ecEntries)
            return DeclBindUseF3(ecEntries, sectionName);
        if (use.OBJECT() is not null || use.EO() is not null)
        {
            // Format 4 (§14.9.49.2 — ONE class/interface operand; SR15 EO ≡ EXCEPTION OBJECT). GR3: for an
            // OBJECT raise, F4 selection REPLACES the F1/F3 tiers (the generated __EcObjDispatch, D-EO7).
            if (data.Edition.DialectLevel < 2002)
                data.Edition.Error("COBOLNET0876",
                    "USE AFTER EXCEPTION OBJECT is the COBOL-2002+ exception-object declarative "
                    + $"(ISO §14.9.49) — it requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");
            _ecF3 = true;   // the ONE "EC declaratives present" feature bit — F4 rides the same group gate
            string cname = use.cobolWord().GetText();
            if (OoClasses?.Find(cname) is not { } cls)
            {
                data.Edition.Error("COBOLNET0859",
                    $"declarative section '{sectionName}': USE AFTER EXCEPTION OBJECT '{cname}' does not "
                    + "name a class of the compilation group (ISO §14.9.49.3 SR16; interface entries are "
                    + "the interface-RAISING refinement)");
                return null;
            }
            return new DeclScope([], null, global, null, EoClassCsName: cls.CsName);
        }
        if (use.REPORTING() is not null)
        {
            // Format 2: USE [GLOBAL] BEFORE REPORTING identifier-1 — identifier-1 references a report group
            // (SR9), optionally qualified by its report-name (the procedureName's OF/IN tail).
            var pn = use.procedureName();
            string head = pn.GetChild(0).GetText();
            string? qualifier = pn.ChildCount >= 3 ? pn.GetChild(2).GetText() : null;
            foreach (var report in data.Reports)
            {
                if (qualifier is not null && !report.Name.Equals(qualifier, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (report.Groups.FirstOrDefault(g =>
                        head.Equals(g.Name, StringComparison.OrdinalIgnoreCase)) is { } group)
                {
                    if (!_declReportGroups.Add(group))
                        data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': report group "
                            + $"'{head}' already has a USE BEFORE REPORTING procedure (ISO §14.9.49 SR9)");
                    return new DeclScope([], null, global, group);
                }
            }
            data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE BEFORE REPORTING "
                + $"'{head}' does not name a report group (ISO §14.9.49 SR9)");
            return null;
        }
        var target = use.useOnTarget();
        if (target is null) return null;

        // Mode scope (GR3b/GR6b–e) — the index IS the runtime FileOpenMode ordinal (the compiler references
        // the runtime enum; both sides stay aligned by construction).
        int? mode = target.INPUT() is not null ? (int)Runtime.IO.FileOpenMode.Input
            : target.OUTPUT() is not null ? (int)Runtime.IO.FileOpenMode.Output
            : target.EXTEND() is not null ? (int)Runtime.IO.FileOpenMode.Extend
            : target.I_O() is not null ? (int)Runtime.IO.FileOpenMode.IO
            : null;
        if (mode is { } m)
        {
            if (!_declScopedModes.Add(m))
                data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': this open mode already "
                    + "has a USE procedure in this source element (ISO §14.9.49 SR7)");
            return new DeclScope([], m, global, null);
        }

        var files = new List<FileModel>();
        foreach (var fn in target.fileName())
        {
            string fname = fn.GetText();
            if (!data.FilesByName.TryGetValue(fname, out var file))
            {
                data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE names unknown "
                    + $"file '{fname}' (ISO §14.9.49)");
                continue;
            }
            if (file.IsSortMerge)
            {
                data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE may not name the "
                    + $"sort/merge file '{fname}' (ISO §14.9.49 SR2)");
                continue;
            }
            if (!_declScopedFiles.Add(fname))
                data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': file '{fname}' already "
                    + "has a USE procedure in this source element (ISO §14.9.49 SR8)");
            files.Add(file);
        }
        return new DeclScope(files, null, global, null);
    }

    /// <summary>Bind a Format-3 USE statement's scope (ISO §14.9.49.2 — <c>USE AFTER {EXCEPTION CONDITION | EC}
    /// {exception-name-1 | exception-name-2 {FILE file-name-2}…}…</c>): validate every exception-name against
    /// the §14.6.13.1 catalog (level 1/2/3 all legal — the GR3c–g tiers select by level), SR13 (a file-scoped
    /// name shall begin EC-I-O), SR14 (no duplicate (ec, file) pair across the USE statements of one procedure
    /// division), and the per-name edition window. The whole format is 2002+ (the EC model's introduction).</summary>
    private DeclScope? DeclBindUseF3(Core.UseEcEntryContext[] entries, string sectionName)
    {
        if (data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0877",
                "USE AFTER EXCEPTION CONDITION (Format 3) is the COBOL-2002+ exception-condition declarative "
                + $"(ISO §14.9.49) — it requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");
        _ecF3 = true;
        var pairs = new List<(string Ec, FileModel? File)>();
        foreach (var entry in entries)
        {
            string raw = entry.cobolWord().GetText();
            if (!Runtime.Exceptions.ExceptionCatalog.TryGet(raw, out var info))
            {
                data.Edition.Error("COBOLNET0711", $"declarative section '{sectionName}': '{raw}' is not an "
                    + "exception-name of ISO/IEC 1989 §14.6.13.1 (and not a valid EC-USER-/EC-IMP- name)");
                continue;
            }
            if (info.Level == 3 && info.IntroducedIn > data.Edition.DialectLevel)
            {
                data.Edition.Error("COBOLNET0878", $"exception-name {info.Name} was introduced by ISO/IEC "
                    + $"1989:{info.IntroducedIn} — it requires --std {info.IntroducedIn} or later "
                    + $"(targeting COBOL-{data.Edition.DialectLevel})");
                continue;
            }
            var fileNames = entry.fileName();
            if (fileNames.Length > 0 && !Runtime.Exceptions.ExceptionCatalog.IsIoName(info.Name))
            {
                data.Edition.Error("COBOLNET0715", $"declarative section '{sectionName}': FILE may be specified "
                    + $"only with an exception-name beginning 'EC-I-O' — '{info.Name}' does not (ISO §14.9.49.3 SR13)");
                continue;
            }
            if (fileNames.Length == 0)
            {
                AddPair(info.Name, null);
                continue;
            }
            foreach (var fn in fileNames)
            {
                string fname = fn.GetText();
                if (!data.FilesByName.TryGetValue(fname, out var file))
                {
                    data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE names unknown "
                        + $"file '{fname}' (ISO §14.9.49)");
                    continue;
                }
                if (file.IsSortMerge)
                {
                    data.Edition.Error("COBOLNET0897", $"declarative section '{sectionName}': USE may not name "
                        + $"the sort/merge file '{fname}' (ISO §14.9.49.3 SR2)");
                    continue;
                }
                AddPair(info.Name, file);
            }
        }
        return new DeclScope([], null, Global: false, null, pairs);

        void AddPair(string ec, FileModel? file)
        {
            // SR14: the same (exception-name, file-name) pair shall not appear in more than one USE statement
            // within the same procedure division (the set spans sections — _declEcPairs is per division).
            if (!_declEcPairs.Add(ec + "|" + (file?.CobolName ?? "")))
                data.Edition.Error("COBOLNET0716", $"declarative section '{sectionName}': the exception-name/"
                    + $"file pair '{ec}{(file is null ? "" : " FILE " + file.CobolName)}' is already specified in "
                    + "another USE statement of this procedure division (ISO §14.9.49.3 SR14)");
            else
                pairs.Add((ec, file));
        }
    }

    /// <summary>The pc the bounded handler dispatch ends at (§14.9.49.4 GR7 — normally the section's last
    /// paragraph). CCVS ACCOMMODATION (documented deviation, the legacy's empirically-validated SQ212A rule):
    /// some CCVS programs place an UNREFERENCED termination tail (CLOSE-FILES → footer → STOP RUN) inside the
    /// declarative section after a trivial exit paragraph; the NIST golden requires the handler to RETURN at
    /// that exit paragraph (the tail stays in pc space — an explicit GO TO still reaches it on the fatal path).
    /// Rule: the LAST paragraph whose statements are all bare EXIT/CONTINUE that is still followed by a
    /// paragraph containing STOP RUN / EXIT PROGRAM / GOBACK ⇒ HandlerEndPc = that exit paragraph's pc. It must
    /// be the LAST such (the boundary adjoining the tail): the handler body's own PERFORM … THRU exit points
    /// (SQ212A's FAIL-ROUTINE-EX1 before EXIT-PARA) are also trivial-exit paragraphs, and bounding at an
    /// earlier one lets a handler GO TO past it fall through into the termination tail.</summary>
    private int DeclHandlerEndPc(Core.DeclarativeSectionContext sec, SectionInfo info)
    {
        var paras = sec.declarativeParagraph();
        int firstNamedPc = info.EndPc - paras.Length + 1;   // leading anonymous paragraph (if any) precedes
        for (int i = paras.Length - 2; i >= 0; i--)
        {
            if (!DeclIsTrivialExit(paras[i])) continue;
            for (int j = i + 1; j < paras.Length; j++)
                if (DeclTerminatesRunUnit(paras[j]))
                    return firstNamedPc + i;
        }
        return info.EndPc;
    }

    private static bool DeclIsTrivialExit(Core.DeclarativeParagraphContext p)
    {
        var sentences = p.sentence();
        if (sentences.Length == 0) return true;   // an empty named paragraph is a pure exit point
        foreach (var s in sentences)
            foreach (var st in s.statement())
            {
                if (st.continueStatement() is not null) continue;
                if (st.exitStatement() is { } e
                    && e.PARAGRAPH() is null && e.PERFORM() is null && e.SECTION() is null
                    && e.PROGRAM() is null && e.METHOD() is null && e.FUNCTION() is null)
                    continue;
                return false;
            }
        return true;
    }

    private static bool DeclTerminatesRunUnit(Core.DeclarativeParagraphContext p)
    {
        foreach (var s in p.sentence())
            foreach (var st in s.statement())
                if (st.stopStatement() is not null || st.gobackStatement() is not null
                    || st.exitStatement() is { } e && e.PROGRAM() is not null)
                    return true;
        return false;
    }
}
