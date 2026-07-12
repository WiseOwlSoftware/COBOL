// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;
using CobolNet.Editions;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary><c>SET {mnemonic-name-1}… TO {ON|OFF} …</c> (ISO §14.9.39 Format 3), resolved to implementor
/// switch-names: GR5 — each switch associated with a mnemonic-name is modified so that a condition-name associated
/// with it evaluates on (ON phrase) / off (OFF phrase). Both the mnemonic list and the outer group repeat (a
/// compound <c>SET A TO ON B TO OFF</c> is ONE statement); assignments apply in source order.</summary>
public sealed record BoundSetSwitches(IReadOnlyList<(string Name, bool On)> Switches) : BoundStatement;

/// <summary>A switch-status condition (ISO §8.8.4.6.2 — just <c>condition-name-1</c>): true when the external
/// switch <paramref name="ImplementorName"/> is set to the position the condition-name posits (GR1) — ON when
/// <paramref name="TestsOn"/>, OFF otherwise. A simple condition (§8.8.4.4), freely combinable with AND/OR/NOT.</summary>
public sealed record BoundSwitchCondition(string ImplementorName, bool TestsOn) : BoundCondition;

/// <summary>One <c>ALTER proc-1 TO [PROCEED TO] proc-2</c> entry, fully resolved: assign <paramref name="NewPc"/>
/// into the target paragraph's alterable-GO-TO field (COBOLNET_CONTROL_FLOW_DESIGN D4 — a mutable static int per
/// altered paragraph, NOT the legacy slot table).</summary>
public sealed record BoundAlterEntry(string AlterField, int NewPc);

/// <summary><c>ALTER</c> (ANSI X3.23-1985 ¶VI.6 — obsolete there; DELETED by ISO/IEC 1989:2002; the 2023 standard
/// has no ALTER and its GO TO has only Formats 1–2, §14.9.17): each entry replaces the GO TO target of the named
/// single-GO-TO paragraph. The most recent ALTER executed governs subsequent transfers.</summary>
public sealed record BoundAlter(IReadOnlyList<BoundAlterEntry> Entries) : BoundStatement;

/// <summary>A GO TO inside an ALTER-target paragraph (written-target or target-less): transfer to the CURRENT
/// value of the paragraph's <paramref name="AlterField"/> (D4 — <c>__pc = _alter_X; break;</c>).
/// <paramref name="DefaultPc"/> initializes the field: the written GO TO target's pc, or −1 for a target-less
/// <c>GO TO.</c> not yet ALTERed — executing it then is undefined (ANSI-85), realized as −1 = dispatcher exit.</summary>
public sealed record BoundGoToAlterable(string AlterField, int DefaultPc) : BoundStatement;

public sealed partial class StatementBinder
{
    // ── ALTER + the 85-only target-less GO TO (ANSI X3.23-1985; deleted by ISO/IEC 1989:2002) ────────────────

    /// <summary>ALTER-target paragraph pc → its generated <c>_alter_&lt;para&gt;</c> field (D4), from the lazy
    /// whole-program prepass (<see cref="AlterEnsureScan"/>).</summary>
    private Dictionary<int, string>? _alterSwFields;

    /// <summary>Paragraph parse context → its pc, so a GO TO locates its OWNING paragraph without re-walking.</summary>
    private Dictionary<Core.SentenceContext, int>? _alterSwParaPc;   // sentence -> owning paragraph pc

    /// <summary>The whole-program ALTER prepass (port of the legacy <c>ScanAlterTargets</c> BEHAVIOR): collect
    /// every <c>ALTER proc-1 TO …</c> target paragraph and assign it its D4 field, BEFORE any GO TO binds — a GO TO
    /// in a target paragraph must bind alterable regardless of whether its ALTER appears earlier or later in the
    /// source. Runs once, on first demand (after <c>CollectParagraphs</c>, so the procedure table is complete);
    /// each target name resolves with the ALTER site's own section context (ISO §8.4.2.2 in-section-first).</summary>
    private void AlterEnsureScan()
    {
        if (_alterSwFields is not null) return;
        _alterSwFields = new Dictionary<int, string>();
        _alterSwParaPc = new Dictionary<Core.SentenceContext, int>();
        for (int i = 0; i < _paras.Count; i++)
            foreach (var sent in _paras[i].Sentences) _alterSwParaPc[sent] = i;

        SectionInfo? saved = _currentSection;
        for (int i = 0; i < _paras.Count; i++)
            foreach (var al in _paras[i].Sentences.SelectMany(AlterStatementsIn))
                foreach (var entry in al.alterEntry())
                {
                    if (entry.procedureName() is not { Length: >= 2 } names) continue;
                    _currentSection = _paraSection[i];
                    // proc-1 names a PARAGRAPH (a section resolves to a multi-pc range and is excluded; the
                    // sole-GO-TO shape check happens at the ALTER's own bind, where it can fail loud).
                    if (ResolveProcedure(names[0]) is { } t && t.Start == t.End)
                        // Method "P_<name>" → field "_alter_<name>" (D4); COBOL names cannot start with '_',
                        // so the field can never collide with a data item's emitted field.
                        _alterSwFields.TryAdd(t.Start, "_alter_" + _paras[t.Start].Method[2..]);
                }
        _currentSection = saved;
    }

    /// <summary>Every <c>alterStatement</c> context under <paramref name="node"/> (ALTER is an imperative
    /// statement — it may sit inside IF branches etc., so the scan is a full subtree walk).</summary>
    private static IEnumerable<Core.AlterStatementContext> AlterStatementsIn(IParseTree node)
    {
        if (node is Core.AlterStatementContext al) { yield return al; yield break; }
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var inner in AlterStatementsIn(node.GetChild(i)))
                yield return inner;
    }

    /// <summary>The pc of the paragraph lexically containing <paramref name="node"/> (walk to the enclosing
    /// <c>paragraphDefinition</c>), or null outside any collected paragraph. Requires <see cref="AlterEnsureScan"/>.</summary>
    private int? AlterOwningPc(IParseTree node)
    {
        for (IParseTree? n = node; n is not null; n = n.Parent)
            if (n is Core.SentenceContext p && _alterSwParaPc!.TryGetValue(p, out int pc))
                return pc;
        return null;
    }

    /// <summary>Bind a resolved single-target GO TO: in an ALTER-target paragraph it transfers to the CURRENT
    /// altered target with the WRITTEN target as the field's initial value (ANSI-85 ALTER GR — until an ALTER
    /// executes, the written GO TO governs); otherwise it is the plain §14.9.17 Format 1 transfer.</summary>
    internal BoundStatement AlterGoTo(Core.GoToStatementContext g, int writtenTarget)
    {
        AlterEnsureScan();
        return AlterOwningPc(g) is { } pc && _alterSwFields!.TryGetValue(pc, out var field)
            ? new BoundGoToAlterable(field, writtenTarget)
            : new BoundGoTo(writtenTarget);
    }

    /// <summary>Bind the target-less <c>GO TO.</c> (ANSI X3.23-1985 only — obsolete there, DELETED by ISO/IEC
    /// 1989:2002; §14.9.17 of the 2023 standard requires procedure-name-1): it may appear only in a single-GO-TO
    /// paragraph referenced by an ALTER and must be ALTERed before execution, else execution is UNDEFINED — bound
    /// as the alterable transfer with default −1 (dispatcher exit) when ALTERed somewhere, or a constant −1
    /// transfer when no ALTER ever names the paragraph (the legacy's NIST-proven realization of "undefined").</summary>
    internal BoundStatement AlterBindBareGoTo(Core.GoToStatementContext g)
    {
        if (g.dataReference() is not null)   // `GO TO DEPENDING ON x` with NO procedure-names is malformed, not bare
            return new BoundUnsupported("GO TO DEPENDING without procedure-names (ISO §14.9.17 Format 2)");
        // The bare-GO-TO removal gate (BareGotoRemoved2002) fires on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitGoToStatement, no procedure-name && no DEPENDING — this exact condition); Step 14h.4a.
        // At 85 the construct is an OBSOLETE element: accepted with no failing diagnostic (the obsolete-element
        // flag awaits the EditionContext warning channel — it must not fail the 85 compile).
        AlterEnsureScan();
        return AlterOwningPc(g) is { } pc && _alterSwFields!.TryGetValue(pc, out var field)
            ? new BoundGoToAlterable(field, -1)
            : new BoundGoTo(-1);
    }

    /// <summary>Bind <c>ALTER {proc-1 TO [PROCEED TO] proc-2}…</c> (ANSI X3.23-1985; 85-ONLY — rejected at 2002+
    /// as a deleted element). Each proc-1 shall be a paragraph consisting of a single sentence that is exactly one
    /// GO TO Format 1 (with or without a written target); execution replaces that GO TO's transfer target with
    /// proc-2 (a section proc-2 transfers to its first paragraph, the §14.9.17 GR1 GO TO rule).</summary>
    private BoundStatement BindAlter(Core.AlterStatementContext al)
    {
        // ALTER was REMOVED by ISO 2002; the edition gate moved to the post-bind VersionConformancePass
        // (PHASE-03 Step 14b), firing on the self-identifying BoundAlter node. At 85: obsolete element, accepted
        // with no failing diagnostic (warning channel pending, as above).
        AlterEnsureScan();
        var entries = new List<BoundAlterEntry>();
        foreach (var entry in al.alterEntry())
        {
            if (entry.procedureName() is not { Length: >= 2 } names)
                return new BoundUnsupported($"ALTER entry '{entry.GetText()}' (malformed)");
            if (ResolveProcedure(names[0]) is not { } target || target.Start != target.End)
                return new BoundUnsupported($"ALTER target '{names[0].GetText()}' (not a known paragraph)");
            if (!AlterIsSoleGoToParagraph(target.Start))
                return new BoundUnsupported($"ALTER target '{names[0].GetText()}' is not a paragraph consisting "
                    + "of a single GO TO sentence (ANSI X3.23-1985 ALTER syntax rule)");
            if (ResolveProcedure(names[1]) is not { } dest)
                return new BoundUnsupported($"ALTER new destination '{names[1].GetText()}' (unknown procedure)");
            entries.Add(new BoundAlterEntry(_alterSwFields![target.Start], dest.Start));
        }
        return new BoundAlter(entries);
    }

    /// <summary>True when paragraph <paramref name="pc"/> consists of a SINGLE sentence whose only statement is a
    /// GO TO Format 1 — written target or target-less, never DEPENDING (the ANSI-85 ALTER shape requirement).</summary>
    private bool AlterIsSoleGoToParagraph(int pc)
    {
        var sentences = _paras[pc].Sentences;
        if (sentences.Length != 1) return false;
        var stmts = sentences[0].statement();
        return stmts.Length == 1 && stmts[0].goToStatement() is { } g
            && g.dataReference() is null && g.procedureName().Length <= 1;
    }

    // ── SPECIAL-NAMES external switches: SET Format 3 + the switch-status condition (ISO §12.3.7) ────────────

    /// <summary>The switch-status condition a bare unsubscripted reference names (ISO §8.8.4.6; §8.4.4.2 Format 1
    /// SR1 — the condition-name shall be associated with a switch-name in SPECIAL-NAMES), else null. The CALLER
    /// must have tried level-88 resolution FIRST (<see cref="ConditionOf"/>): a name defined as both resolves as
    /// the level-88 (legacy resolution order; NC211A regression guard).</summary>
    private BoundCondition? SwitchCondOf(Core.DataReferenceContext dref)
    {
        if (dref.dataReferenceSuffix().Length != 0) return null;   // a status condition-name takes no subscript/qualifier
        return dref.cobolWord()?.GetText() is { } name && data.SwitchConditions.TryGetValue(name, out var sc)
            ? new BoundSwitchCondition(sc.ImplementorName, sc.IsOn)
            : null;
    }

    /// <summary>Bind <c>SET {{mnemonic-name-1}… TO {ON|OFF}}…</c> (ISO §14.9.39 Format 3). The grammar is FLAT
    /// (<c>SET (dataReference+ TO (ON|OFF))+</c>), so the groups are reassembled by token position: the references
    /// whose stop precedes a TO belong to that TO's group, and the group's position is the ON or OFF token between
    /// this TO and the next. Every receiver must name a settable external switch's mnemonic (SR5) — an unresolvable
    /// name fails loud, never a silent skip.</summary>
    private BoundStatement SwitchBindSet(Core.SetSwitchStatementContext sw)
    {
        var drefs = sw.dataReference();
        var tos = sw.TO();
        var ons = sw.ON();
        var switches = new List<(string Name, bool On)>();
        int refIdx = 0, onIdx = 0;
        for (int t = 0; t < tos.Length; t++)
        {
            int toPos = tos[t].Symbol.TokenIndex;
            int nextToPos = t + 1 < tos.Length ? tos[t + 1].Symbol.TokenIndex : int.MaxValue;
            bool on = onIdx < ons.Length
                && ons[onIdx].Symbol.TokenIndex > toPos && ons[onIdx].Symbol.TokenIndex < nextToPos;
            if (on) onIdx++;
            for (; refIdx < drefs.Length && drefs[refIdx].Stop.TokenIndex < toPos; refIdx++)
            {
                string name = drefs[refIdx].cobolWord()?.GetText() ?? drefs[refIdx].GetText();
                if (!data.SwitchMnemonics.TryGetValue(name, out var implName))
                    return new BoundUnsupported(
                        $"SET '{name}' TO ON/OFF — not a SPECIAL-NAMES external-switch mnemonic (ISO §14.9.39 F3 SR5)");
                switches.Add((implName, on));
            }
        }
        return new BoundSetSwitches(switches);
    }
}
