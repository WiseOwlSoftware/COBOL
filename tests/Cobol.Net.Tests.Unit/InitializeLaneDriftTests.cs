// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE TWO INVARIANTS THAT KEEP THE INITIALIZE EXPANSION AUTOMATIC (kb/Work PB418, CLAUDE.md rule 5).
/// <para>The INITIALIZE lane expands ISO §14.9.20.4 GR4's "series of implicit MOVE or SET statements" at BIND
/// time, so two things about it can rot silently, and both did.</para>
/// <para><b>1. The VALUE carrier.</b> A receiver's sending-operand under the VALUE phrase (GR6a3) and its
/// qualification under GR5c1b/GR5c1c are the SAME question — "what does this item's VALUE clause give THIS
/// occurrence" — and <c>DataItem.ValueAt</c> is the one reader that answers it across both carriers (Format 1's
/// <c>RawValue</c> and Format 2's <c>TableValuePlan</c>). The binder used to read <c>RawValue</c> directly, so
/// the Format-2 carrier was invisible: <c>ALL TO VALUE</c> emitted no store for a table element and
/// <c>ALL TO VALUE THEN TO DEFAULT</c> wrote SPACES over the values the statement exists to restore. A THIRD
/// carrier would repeat that exactly, and nothing would fail.</para>
/// <para><b>2. The action vocabulary.</b> <c>InitializeEmitter.EmitAction</c> switches over
/// <see cref="T:CobolNet.Binding.Bound.InitializeAction"/> with NO default arm — an action the binder composes
/// and the emitter does not know is dropped WITHOUT a diagnostic, and the store the standard requires simply
/// never happens. The vocabulary grew (PB393's loop, PB418's per-occurrence select) and will grow again.</para>
/// </summary>
public sealed class InitializeLaneDriftTests
{
    private static readonly string BinderPath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "InitializeBinder.cs");

    private static readonly string BoundPath =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound", "BoundInitialize.cs");

    private static readonly string EmitterPath =
        TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "InitializeEmitter.cs");

    /// <summary>The file's code with every comment removed — a doc comment naming a symbol is documentation, not
    /// a read of it, and this file's subjects are named in several of them.</summary>
    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    /// <summary>ISO §14.9.20.4 GR5c1b/GR5c1c and GR6a3 are answered for BOTH VALUE carriers by
    /// <c>DataItem.ValueAt</c>, so the INITIALIZE binder must never reach past it to a carrier property. Reading
    /// one directly is how the Format-2 carrier went unseen (kb/Work PB418 / PB499); it is also how a third one
    /// would.</summary>
    [Fact]
    public void InitializeBinder_ReadsValueOnlyThroughTheOneCarrierAgnosticReader()
    {
        string code = CodeOf(BinderPath);

        Assert.True(code.Contains(".ValueAt(", StringComparison.Ordinal),
            "InitializeBinder no longer calls DataItem.ValueAt — §14.9.20.4 GR6a3's sending operand has to come "
            + "from the one carrier-agnostic reader, or a VALUE carrier it does not know about is silently lost.");

        // TableValuePlan is the Format-2 carrier the per-occurrence arm legitimately walks (GR5c1c needs the
        // occurrence tuples themselves, which no scalar reader can give it). RawValue and TableValues are not:
        // they are the raw carriers ValueAt exists to hide.
        foreach (string carrier in new[] { ".RawValue", ".TableValues" })
            Assert.False(code.Contains(carrier, StringComparison.Ordinal),
                $"InitializeBinder reads DataItem{carrier} directly. Ask DataItem.ValueAt(subscripts) instead — "
                + "it is the ONE reader of \"what initializes this item at this occurrence\" and the reason the "
                + "declaration lane and the INITIALIZE lane cannot disagree (§14.9.20.4 GR6a3).");
    }

    /// <summary>Every <c>InitializeAction</c> the bound tree declares has an arm in the emitter. The emitter's
    /// switch has no default, so a missing arm is a store that the standard requires and the program never
    /// performs — with a green build and a green gate.</summary>
    [Fact]
    public void EveryInitializeAction_HasAnEmitterArm()
    {
        string emitter = CodeOf(EmitterPath);
        var missing = new List<string>();
        foreach (string action in DeclaredActions())
            if (!Regex.IsMatch(emitter, $@"case\s+{Regex.Escape(action)}\b"))
                missing.Add(action);

        Assert.True(missing.Count == 0,
            "InitializeEmitter.EmitAction has no arm for: " + string.Join(", ", missing)
            + ". Its switch carries no default, so the action is dropped silently and the implicit MOVE/SET "
            + "§14.9.20.4 GR4 requires never happens. Add the arm, and the matching visit in "
            + "UsageCollectionPass.InitAct and BoundStores.InitStores.");
    }

    /// <summary>The action records declared in <c>BoundInitialize.cs</c> — read from the source rather than by
    /// reflection so the test names the file a contributor has to edit, and so a record added but not yet
    /// referenced anywhere still counts.</summary>
    private static IEnumerable<string> DeclaredActions() =>
        Regex.Matches(CodeOf(BoundPath), @"record\s+(?<name>\w+)\s*\([^)]*\)\s*:\s*InitializeAction\b")
            .Select(m => m.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    /// <summary>The scan above is only evidence if it can see anything at all — a regex that matches nothing
    /// makes the previous test a silent green (feedback_green_gates_arent_evidence).</summary>
    [Fact]
    public void TheActionScan_FindsTheActionsThatExist()
    {
        var actions = DeclaredActions().ToList();
        Assert.True(actions.Count >= 5,
            "the InitializeAction scan found only " + actions.Count + " record(s) in BoundInitialize.cs ("
            + string.Join(", ", actions) + ") — the declaration shape changed and EveryInitializeAction_"
            + "HasAnEmitterArm is no longer looking at anything.");
        foreach (string expected in new[]
                 { "InitializeStore", "InitializeLoop", "InitializeSetNull", "InitializeErrorAction",
                   "InitializeOccurrenceSelect" })
            Assert.Contains(expected, actions);
    }
}
