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
/// ⛔ THE SENDING OPERAND OF AN <c>INTO</c> PHRASE IS ONE RULE, AND IT HAS TO STAY IN ONE PLACE. ISO §14.9.30.4
/// GR4 b) and §14.9.34.4 GR5 b) say the same sentence about READ and RETURN — the sender is THE CURRENT RECORD,
/// sized by the RECORD clause (§13.18.43.4 GR16), and designated an alphanumeric group move when the file
/// description entry carries a RECORD IS VARYING clause — and the compiler renders that phrase from THREE arms:
/// the sequential READ, the keyed READ, and the sort/merge RETURN.
/// <para>All three used to build the implicit MOVE themselves, and all three were wrong the same way for years:
/// each sent the whole space-padded record area, which is observationally identical to the current record for a
/// LEFT-justified receiver and destroys the data for a JUSTIFIED one (kb/Work PB339). A per-arm fix would have
/// left the next arm — a new I-O verb, or PB348 moving these MOVEs to bind time — free to re-derive it a fourth
/// time. So the builder is singular, and this is the test that keeps it singular.</para>
/// <para><b>What this can and cannot see.</b> It counts CONSTRUCTIONS of the operand across the compiler's
/// comment-stripped sources and asserts there is exactly one, in the method that owns the rule; and it asserts
/// each of the three INTO emitters reaches that method. It cannot see an arm that calls the builder and then
/// ignores the result — the four-edition golden <c>pb339_into_current_record</c> covers the behaviour.</para>
/// </summary>
public sealed class IntoSenderDriftTests
{
    private const string Operand = "BoundCurrentRecord";
    private const string Builder = "IntoSender";

    /// <summary>The emitter that OWNS the rule, and the file its one construction must live in.</summary>
    private const string Owner = "SequentialIoEmitter.cs";

    /// <summary>Every emitter that renders an <c>INTO</c> phrase (ISO §14.9.30 READ, §14.9.34 RETURN). Adding a
    /// verb with an INTO phrase means adding its emitter here — and routing it through the builder.</summary>
    public static TheoryData<string> IntoEmitters() => new()
    {
        "SequentialIoEmitter.cs",   // READ … INTO, sequential + line-sequential organizations
        "KeyedIoEmitter.cs",        // READ … INTO, relative + indexed organizations
        "SortEmitter.cs",           // RETURN … INTO
    };

    [Fact]
    public void TheCurrentRecordOperand_IsConstructedInExactlyOnePlace()
    {
        var sites = new List<string>();
        foreach (string file in Directory.EnumerateFiles(
                     TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            // The generated parser/visitor tree is a build output, never a rule site.
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            string src = StripComments(File.ReadAllText(file));
            int n = Regex.Matches(src, $@"\bnew\s+{Operand}\s*\(").Count;
            for (int i = 0; i < n; i++) sites.Add(Path.GetFileName(file));
        }

        Assert.True(sites.Count == 1,
            $"ISO §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b) name ONE sending operand for every INTO phrase, so "
            + $"'new {Operand}' belongs at exactly one site — {Builder}. Found {sites.Count}: "
            + string.Join(", ", sites.Distinct().OrderBy(s => s, StringComparer.Ordinal)));
        Assert.Equal(Owner, sites[0]);
    }

    [Theory]
    [MemberData(nameof(IntoEmitters))]
    public void EveryIntoEmitter_BuildsItsSenderThroughTheOneBuilder(string emitterFile)
    {
        string src = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Verbs", emitterFile)));
        Assert.True(Regex.IsMatch(src, $@"\b{Builder}\s*\("),
            $"{emitterFile} renders an INTO phrase but never calls {Builder} — §14.9.30.4 GR4 b)'s current record "
            + "would be re-derived here, which is exactly how the padded record area survived in three arms.");
    }

    /// <summary>Drop line and block comments so a rule NAMED in prose is never mistaken for a rule APPLIED in
    /// code. String literals are left alone: none of the tokens searched for here appears inside one.</summary>
    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
