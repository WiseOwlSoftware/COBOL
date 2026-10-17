// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ EVERY I-O NOT PHRASE IS RENDERED THROUGH THE ONE GATED RENDERER (kb/Work PB3237, PB1120). §14.6.13.1.4
/// 2)/3): once a WHEN phrase or a USE declarative has handled the nonfatal condition a SUCCESSFUL I-O statement
/// raised (EC-I-O-WARNING on a '0x' status), "the imperative-statement in [the NOT] phrase is not executed". The
/// statement's hook answers that as <c>UseHookResult.NotPhraseGate</c>, and the phrase's body is emitted only by
/// <c>SequentialIoEmitter.EmitNotPhrase</c> (a lone NOT phrase) or <c>EmitPhrasePair</c> (the phrase / NOT-phrase
/// pair) — both take the gate. PB1120 gated the INVALID KEY, AT END and END-OF-PAGE arms by hand and left DELETE
/// FILE's NOT ON EXCEPTION on <c>Successful(status)</c> alone, a second copy of the pair dispatch with no gate; this
/// guard makes a NEW NOT phrase read in the I-O emitters reach a renderer that has one.</summary>
public sealed class NotPhraseGateDriftTests
{
    private static readonly Regex NotPhraseRead = new(@"\b(NotOnException|NotInvalid|NotAtEnd|NotAtEop)\b(?!\s+is\s+(not\s+)?null)", RegexOptions.Compiled);
    private static readonly string[] IoEmitters = ["KeyedIoEmitter.cs", "SequentialIoEmitter.cs"];

    [Fact]
    public void EveryNotPhraseReadInTheIoEmitters_ReachesAGatedRenderer()
    {
        string verbs = TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs");
        var offenders = new List<string>();
        int reads = 0;
        foreach (string name in IoEmitters)
        {
            string file = Path.Combine(verbs, name);
            Assert.True(File.Exists(file), $"{name} moved: {file}");
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal) || code.StartsWith("///", StringComparison.Ordinal)) continue;
                if (!NotPhraseRead.IsMatch(code)) continue;
                reads++;
                // The renderer call is on the read's line or within the two lines after a pattern-binding `is { } x`.
                string window = string.Join('\n', lines.Skip(i).Take(3));
                if (!window.Contains("EmitNotPhrase(", StringComparison.Ordinal) && !window.Contains("EmitPhrasePair(", StringComparison.Ordinal))
                    offenders.Add($"{name}:{i + 1}: {code}");
            }
        }
        Assert.True(reads > 0, "the guard saw no NOT-phrase read: the property names or the emitter files moved");
        Assert.True(offenders.Count == 0,
            "An I-O NOT phrase is rendered without the NotPhraseGate (§14.6.13.1.4 2)/3, kb/Work PB3237) — "
            + "render it through SequentialIoEmitter.EmitNotPhrase / EmitPhrasePair with the hook's NotPhraseGate:\n"
            + string.Join("\n", offenders));
    }
}
