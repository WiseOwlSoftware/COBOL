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
/// ⛔ A BOUND NODE BUILT AFTER BINDING IS NEVER CHECKED BY A BIND PASS. That sentence is the whole of kb/Work
/// PB348: the implicit MOVE of every <c>… FROM</c> / <c>… INTO</c> phrase was constructed in the EMITTER —
/// <c>move.Emit(new BoundMove(from, [rl.Record]))</c> — downstream of every bind-time MOVE screen AND of the
/// storage facts the emitter's own output consumes.
///
/// <para><b>Measured cost, not a theory.</b> ISO §14.9.32.4 GR4 makes <c>RELEASE record-name-1 FROM x</c>
/// exactly <c>MOVE x TO record-name-1</c> followed by the same RELEASE without FROM. It was not:
/// <c>RELEASE SRT-REC FROM WS-NUM</c> (a PIC 9(3) sender into a PIC A(8) record) compiled clean where the
/// identical explicit MOVE drew COBOLNET0819; and <c>RELEASE SRT-NUM FROM QUOTE</c> ABORTED THE RUN with an
/// unhandled NotImplementedCobolFeatureException before the record was ever released, because
/// <c>MarkImageForced</c> — a fact <c>StorageFormPass</c> consumes — is collected by the binder and the
/// emitter-built move never passed through it. Same source, same SD, opposite result.</para>
///
/// <para><b>The invariant.</b> Every <c>BoundMove</c> the I-O and sort emitters render was built by
/// <c>MoveBinder</c>, so it carries the §14.9.25.3 syntax rules and the collected storage facts. Adding an I-O
/// verb with a FROM or INTO phrase and synthesizing its move at emission fails HERE, at the shape, rather than
/// silently in a program nobody has written yet.</para>
///
/// <para><b>What this can and cannot see.</b> It reads comment-stripped source, so a construction NAMED in
/// prose (this subject is discussed in several of these files) is not mistaken for one performed in code. It
/// cannot see a bound node built by a helper the emitter calls in another assembly, and it says nothing about
/// the three NON-phrase implicit moves that remain emitter-built — MOVE CORRESPONDING's per-pair moves,
/// INITIALIZE's §14.9.20 GR4 stores and GOBACK RETURNING's §14.9.18.4 GR2 move — which are a different rule
/// each and are named below so that their number can only go down.</para>
/// </summary>
public sealed class ImplicitMoveConstructionDriftTests
{
    private const string Node = "BoundMove";

    /// <summary>The emitters that render a <c>… FROM</c> or <c>… INTO</c> phrase. Every one of them constructed
    /// a <c>BoundMove</c> before PB348; none of them may again.</summary>
    public static TheoryData<string> PhraseEmitters() => new()
    {
        "SequentialIoEmitter.cs",   // WRITE/REWRITE … FROM, READ … INTO — sequential + line-sequential
        "KeyedIoEmitter.cs",        // WRITE/REWRITE … FROM, READ … INTO — relative + indexed
        "SortEmitter.cs",           // RELEASE … FROM, RETURN … INTO
    };

    [Theory]
    [MemberData(nameof(PhraseEmitters))]
    public void NoFromOrIntoEmitter_ConstructsItsOwnBoundMove(string emitterFile)
    {
        string src = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", emitterFile)));
        Assert.False(Regex.IsMatch(src, $@"\bnew\s+{Node}\s*\("),
            $"{emitterFile} constructs a {Node}. The implicit MOVE of a FROM / INTO phrase is BOUND — "
            + "MoveBinder.BindFromPhrase / BindIntoPhrase — so that the §14.9.25.3 syntax rules and the "
            + "MarkImageForced / MarkRefModStoreImage storage facts apply to it. A move built here escapes both, "
            + "which is how RELEASE … FROM QUOTE came to abort the run unit (kb/Work PB348).");
    }

    /// <summary>The implicit moves that are still emitter-built, each keyed to the rule that defines it. They
    /// are NOT FROM/INTO phrases and are outside PB348's mechanism; they are enumerated so that a NEW
    /// emitter-built move anywhere in CodeGen fails this test rather than joining them unremarked. An entry
    /// leaves this table when its move moves to bind time; nothing may be added without a note that says why.
    /// </summary>
    private static readonly Dictionary<string, string> EmitterBuiltMoves = new(StringComparer.Ordinal)
    {
        ["CorrespondingEmitter.cs"] = "MOVE CORRESPONDING — the per-pair moves of ISO §14.9.25.4, whose pairs "
            + "§14.6.3 selects; the pair list is bound, the moves over it are not",
        ["InitializeEmitter.cs"] = "INITIALIZE — the implicit MOVE of ISO §14.9.20.4 GR4 for each initialized "
            + "item, built from the bound InitializeStore actions",
        ["CallEmitter.cs"] = "GOBACK RETURNING — the move of ISO §14.9.18.4 GR2 into the activation's RETURNING "
            + "item, whose place is emitter state (the header item of the program being emitted)",
    };

    [Fact]
    public void EveryEmitterBuiltBoundMove_IsOneOfTheKnownNonPhraseMoves()
    {
        var found = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(
                     TestRepo.Src("Cobol.Net.Compiler", "CodeGen"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            int n = Regex.Matches(StripComments(File.ReadAllText(file)), $@"\bnew\s+{Node}\s*\(").Count;
            if (n > 0) found[Path.GetFileName(file)] = n;
        }

        var unexpected = found.Keys.Where(f => !EmitterBuiltMoves.ContainsKey(f)).ToList();
        Assert.True(unexpected.Count == 0,
            $"CodeGen constructs a {Node} in {string.Join(", ", unexpected)}, which is not one of the known "
            + "non-phrase implicit moves. A move built after binding is checked by no bind pass and carries none "
            + "of the storage facts codegen consumes (kb/Work PB348). Bind it — MoveBinder.BindMoveOf is the one "
            + "entry — or, if it genuinely cannot be bound, add it to EmitterBuiltMoves with the rule it "
            + "implements and open a kb/Work note for it.");
    }

    /// <summary>Drop line and block comments so a construction NAMED in prose is never mistaken for one
    /// performed in code — several of these files discuss this very subject at length.</summary>
    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
