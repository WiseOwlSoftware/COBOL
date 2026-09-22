// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB871 — the ELEMENTARY CHARACTER RECEIVING STORE lives in ONE place, <c>CodeGen/Verbs/ReceivingStore.cs</c>.
/// ISO §8.5.1.10.4 is written over "a receiving operand", not over MOVE: every verb that transfers "according to the
/// rules for the MOVE statement" owes a dynamic-length receiver its whole new value. The defect was that the
/// dynamic arm existed only inside <c>MoveEmitter</c>, and UNSTRING and ACCEPT each built the fixed-width arm
/// themselves — at the PICTURE's one position (§13.18.19.3 SR1). This pins the structure, not a case: an emitter
/// under <c>CodeGen/Verbs</c> that builds either store directly is a new private copy of the rule.
/// </summary>
public sealed class ReceivingStoreDriftTests
{
    /// <summary>The one file allowed to spell the two stores. Deliberately NO other entry: <c>OoEmitter</c>'s BY
    /// CONTENT boolean argument copy was the last private copy, and it is "the MOVE store for the formal's
    /// category" by its own comment (§14.8.2.3.3 rule 2d), so it calls the home too.</summary>
    private static readonly HashSet<string> Allowed = new() { "ReceivingStore.cs" };

    [Fact]
    public void NoVerbEmitter_BuildsTheElementaryCharacterStoreItself()
    {
        var dir = TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs");
        var rx = new Regex(@"RuntimeApi\.(StrStoreAligned|DynStore)\(");
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(dir, "*.cs"))
        {
            string name = Path.GetFileName(file);
            if (Allowed.Contains(name)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (rx.IsMatch(lines[i]) && !lines[i].TrimStart().StartsWith("//"))
                    offenders.Add($"{name}:{i + 1}: {lines[i].Trim()}");
        }
        Assert.True(offenders.Count == 0,
            "An emitter builds the elementary character receiving store itself instead of calling "
            + "ReceivingStore.Characters — a dynamic-length receiver would take the PICTURE's width there "
            + "(ISO §8.5.1.10.4; kb/Work PB871):\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void TheOneHome_StillCarriesBothArms()
    {
        // Make the scan above able to FAIL: if the home stops spelling the two stores, the allow-list entry is
        // stale and the drift test would pass vacuously over a rule that moved somewhere unscanned.
        string home = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", "ReceivingStore.cs"));
        Assert.Contains("RuntimeApi.DynStore(", home);
        Assert.Contains("RuntimeApi.StrStoreAligned(", home);
    }
}
