// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ EVERY I-O NOT PHRASE IS RENDERED THROUGH THE ONE GATED RENDERER, GIVEN THE STATEMENT'S OWN HOOK
/// (kb/Work PB3237, PB1120). §14.6.13.1.4 2)/3): once a WHEN phrase or a USE declarative has handled the nonfatal
/// condition a SUCCESSFUL I-O statement raised (EC-I-O-WARNING on a '0x' status), "the imperative-statement in [the
/// NOT] phrase is not executed". The statement's hook answers that as <c>UseHookResult.NotPhraseGate</c>, and the
/// phrase's body is emitted only by <c>SequentialIoEmitter.EmitNotPhrase</c> (a lone NOT phrase),
/// <c>EmitPhrasePair</c> (the phrase / NOT-phrase pair) or <c>EmitInvalid</c> (the INVALID KEY pair) — each takes the
/// <c>UseHookResult</c> WHOLE, never its gate string, so a caller cannot pass <c>null</c> or leave a default and a
/// dropped hook does not compile. PB1120 gated the INVALID KEY, AT END and END-OF-PAGE arms by hand and left DELETE
/// FILE's NOT ON EXCEPTION on <c>Successful(status)</c> alone. These guards close what the type system leaves open:
/// a NOT phrase read that bypasses the renderers, a renderer call that passes anything but the statement's
/// <c>hook</c> variable, and a <c>UseHookResult</c> built anywhere but <c>EmitUseHook</c> (a hand-made
/// <c>new UseHookResult(false, null)</c> or <c>default</c> is the dropped hook again, spelled to compile).</summary>
public sealed class NotPhraseGateDriftTests
{
    private static readonly Regex NotPhraseRead = new(@"\b(NotOnException|NotInvalid|NotAtEnd|NotAtEop)\b(?!\s+is\s+(not\s+)?null)", RegexOptions.Compiled);
    private static readonly Regex RendererCall = new(@"(?<!void\s)\b(EmitNotPhrase|EmitPhrasePair|EmitInvalid)\(([^;{]*);", RegexOptions.Compiled);
    private static readonly Regex HookArgument = new(@"(?<![.\w])hook(?![.\w])", RegexOptions.Compiled);
    private static readonly Regex HookConstruction = new(@"\bnew\s+(\w+\.)?UseHookResult\s*\(|\bdefault\s*\(\s*(\w+\.)?UseHookResult\s*\)", RegexOptions.Compiled);
    private static readonly string[] IoEmitters = ["KeyedIoEmitter.cs", "SequentialIoEmitter.cs"];

    private static string[] CodeLines(string file)
    {
        Assert.True(File.Exists(file), $"moved: {file}");
        return File.ReadAllLines(file).Select(l =>
        {
            int c = l.IndexOf("//", StringComparison.Ordinal);
            return c < 0 ? l : l[..c];
        }).ToArray();
    }

    [Fact]
    public void EveryNotPhraseReadInTheIoEmitters_ReachesAGatedRenderer()
    {
        string verbs = TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs");
        var offenders = new List<string>();
        int reads = 0;
        foreach (string name in IoEmitters)
        {
            string[] lines = CodeLines(Path.Combine(verbs, name));
            for (int i = 0; i < lines.Length; i++)
            {
                if (!NotPhraseRead.IsMatch(lines[i])) continue;
                reads++;
                // The renderer call is on the read's line or within the two lines after a pattern-binding `is { } x`.
                string window = string.Join('\n', lines.Skip(i).Take(3));
                if (!window.Contains("EmitNotPhrase(", StringComparison.Ordinal) && !window.Contains("EmitPhrasePair(", StringComparison.Ordinal))
                    offenders.Add($"{name}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(reads > 0, "the guard saw no NOT-phrase read: the property names or the emitter files moved");
        Assert.True(offenders.Count == 0,
            "An I-O NOT phrase is rendered without the NotPhraseGate (§14.6.13.1.4 2)/3, kb/Work PB3237) — "
            + "render it through SequentialIoEmitter.EmitNotPhrase / EmitPhrasePair / EmitInvalid with the statement's hook:\n"
            + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryRendererCallInTheIoEmitters_PassesTheStatementsHook()
    {
        string verbs = TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs");
        var offenders = new List<string>();
        int calls = 0;
        foreach (string name in IoEmitters)
        {
            string text = string.Join('\n', CodeLines(Path.Combine(verbs, name)));
            foreach (Match m in RendererCall.Matches(text))
            {
                calls++;
                if (!HookArgument.IsMatch(m.Groups[2].Value))
                    offenders.Add($"{name}: {m.Groups[1].Value}({m.Groups[2].Value.Trim()})");
            }
        }
        Assert.True(calls >= 10, $"the guard saw only {calls} renderer calls: the renderer names or the emitter files moved");
        Assert.True(offenders.Count == 0,
            "A NOT-phrase renderer is called without the statement's `hook` variable (§14.6.13.1.4 2)/3, kb/Work PB3237) — "
            + "pass the UseHookResult EmitUseHook returned for THIS statement:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void AUseHookResultIsBuiltOnlyByEmitUseHook()
    {
        string compiler = TestRepo.Src("Cobol.Net.Compiler");
        string owner = Path.Combine(compiler, "CodeGen", "Verbs", "SequentialIoEmitter.cs");
        var offenders = new List<string>();
        int inOwner = 0;
        foreach (string file in Directory.EnumerateFiles(compiler, "*.cs", SearchOption.AllDirectories))
        {
            string[] lines = CodeLines(file);
            // EmitUseHook is the span from its signature to the first closing brace at the member's indentation.
            int start = -1, end = -1;
            if (string.Equals(file, owner, StringComparison.OrdinalIgnoreCase))
            {
                start = Array.FindIndex(lines, l => l.Contains("UseHookResult EmitUseHook(", StringComparison.Ordinal));
                Assert.True(start >= 0, "EmitUseHook moved: update this guard");
                end = Array.FindIndex(lines, start, l => l == "    }");
                Assert.True(end > start, "EmitUseHook's closing brace moved: update this guard");
            }
            for (int i = 0; i < lines.Length; i++)
            {
                if (!HookConstruction.IsMatch(lines[i])) continue;
                if (start >= 0 && i >= start && i <= end) { inOwner++; continue; }
                offenders.Add($"{Path.GetRelativePath(compiler, file)}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(inOwner > 0, "the guard saw no UseHookResult construction in EmitUseHook: the type or method moved");
        Assert.True(offenders.Count == 0,
            "A UseHookResult is built outside EmitUseHook (kb/Work PB3237) — a hand-made one carries no NotPhraseGate, "
            + "the dropped hook again; call EmitUseHook for the statement:\n" + string.Join("\n", offenders));
    }
}
