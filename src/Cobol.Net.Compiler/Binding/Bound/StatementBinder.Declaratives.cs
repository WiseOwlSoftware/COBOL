// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

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

    /// <summary>Collect one declarative section into the pc space: the USE sentence (SR1 — the section's first
    /// sentence), an anonymous paragraph for any further leading sentences (the CCVS handler-before-the-first-
    /// paragraph shape, e.g. SQ103A), then the named paragraphs.</summary>
    private void DeclCollectSection(Core.DeclarativeSectionContext sec, HashSet<string> used)
    {
        string name = sec.sectionName().GetText();
        var info = new SectionInfo(name, _paras.Count);

        // SR1: the first sentence consists of exactly one USE statement.
        var leading = sec.sentence();
        (IReadOnlyList<FileModel> Files, int? ModeIndex, bool Global)? scope = null;
        if (leading.Length == 0
            || leading[0].statement() is not { Length: 1 } first
            || first[0].useStatement() is not { } use)
            data.Edition.Error("COBOLNET0897", $"declarative section '{name}': the first sentence shall consist "
                + "of a single USE statement (ISO §14.2.4 / §14.9.49 SR1)");
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
                name, info.StartPc, info.EndPc, DeclHandlerEndPc(sec, info), s.Files, s.ModeIndex, s.Global));
    }

    /// <summary>Bind the USE statement's trigger scope (ISO §14.9.49): Format 1's file list or open mode; the
    /// GLOBAL phrase recorded (cross-program dispatch GR4 is the post-CALL wave). Format 2 (BEFORE REPORTING)
    /// is the Report Writer module — diagnosed, never silent.</summary>
    private (IReadOnlyList<FileModel> Files, int? ModeIndex, bool Global)? DeclBindUse(
        Core.UseStatementContext use, string sectionName)
    {
        bool global = use.GLOBAL() is not null;
        if (use.REPORTING() is not null)
        {
            data.Edition.Error("COBOLNET0898", $"declarative section '{sectionName}': USE BEFORE REPORTING "
                + "(ISO §14.9.49 Format 2 — Report Writer) is not yet implemented");
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
            return ([], m, global);
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
        return (files, null, global);
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
