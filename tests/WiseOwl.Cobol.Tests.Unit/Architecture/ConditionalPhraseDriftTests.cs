// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ A NOT PHRASE'S BODY IS EMITTED BY THE ONE CONDITIONAL-PHRASE RENDERER (kb/Work PB3238). The NOT ON SIZE ERROR, NOT ON
/// OVERFLOW, NOT INVALID KEY, NOT AT END, NOT AT END-OF-PAGE and NOT ON EXCEPTION imperatives run when the statement completes
/// successfully and no handled nonfatal condition interrupted it (§2023:14.6.13.1.4 3)), and <c>ConditionalPhrase</c> renders
/// that once. Five verb emitters used to write the pair themselves, with four ways of composing the NOT test, and one copy lost
/// its gate (PB3237's DELETE FILE). This test refuses a statement list emitted from a variable named for a NOT phrase anywhere
/// in code generation outside <c>ConditionalPhrase.cs</c>.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ConditionalPhraseDriftTests
{
    private static readonly Regex NotBody = new(@"EmitStatementList\(\s*[\w.?]*\b(?:[Nn]ot\w*)\b", RegexOptions.Compiled);

    [Fact]
    public void NotPhraseBodies_AreEmittedOnlyByConditionalPhrase()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler", "CodeGen");
        string owner = Path.Combine("Verbs", "ConditionalPhrase.cs");
        Assert.True(File.Exists(Path.Combine(root, owner)), "ConditionalPhrase moved: update this guard");
        var offenders = new List<string>();
        int inOwner = 0;
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file);
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].Split("//", 2)[0];
                if (!NotBody.IsMatch(code)) continue;
                if (rel == owner) { inOwner++; continue; }
                offenders.Add($"{rel}:{i + 1}: {code.Trim()}");
            }
        }
        Assert.True(inOwner > 0, "the guard saw no NOT-phrase body in ConditionalPhrase: its parameter was renamed");
        Assert.True(offenders.Count == 0,
            "a NOT phrase's body is emitted outside ConditionalPhrase (kb/Work PB3238; §2023:14.6.13.1.4 3)) — render it through "
            + "ConditionalPhrase.EmitPair / EmitNot with its NotPhraseCondition:\n  " + string.Join("\n  ", offenders));
    }
}
