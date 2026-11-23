// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB3199 — the NIST run environment is <c>tests/nist/run-environment.tsv</c>, read by both legs
/// (<see cref="NistRunEnvironment"/>, <c>scripts/guard-run-group.sh</c>'s <c>guard_run_environment</c>). These facts
/// keep it the ONE place: it declares every variable <see cref="CutRunner.RunExit"/> sets for a program, so the CLI leg,
/// which has only the file, runs its programs in the environment the C# leg's programs get; the shell runner writes no
/// variable of its own; and both readers refuse the same malformed lines (the shell's half is
/// <c>scripts/guard-verify.sh --self-test</c>).
/// </summary>
public sealed partial class NistRunEnvironmentTests
{
    /// <summary>The locale pin and the MCS directory are what <see cref="CutRunner.RunExit"/> gives every program it runs;
    /// the shell leg used to give its programs <c>COBOL_SWITCH_1</c> alone, so a program that read the host's default
    /// locale was graded under two environments.</summary>
    [Fact]
    public void TheRunEnvironment_DeclaresEveryVariableTheHarnessSetsForAProgram()
    {
        var declared = NistRunEnvironment.Declared.ToDictionary(v => v.Key, v => v.Value);
        var expected = CutRunner.PinnedEnvironment
            .Select(p => (p.Key, Value: p.Value!))
            .Append((Key: CutRunner.McsDirectoryVariable, Value: NistRunEnvironment.RunDirectoryToken))
            .ToList();
        var missing = expected.Where(e => !declared.TryGetValue(e.Key, out string? v) || v != e.Value)
            .Select(e => $"  {e.Key}\t{e.Value}").ToList();
        Assert.True(missing.Count == 0,
            $"tests/nist/run-environment.tsv lacks (or gives another value to) what CutRunner.RunExit sets for every program:\n{string.Join("\n", missing)}");
        Assert.Equal("ON", declared["COBOL_SWITCH_1"]);
    }

    [Fact]
    public void For_GivesTheRunDirectoryWhereTheFileWritesTheToken()
    {
        var env = NistRunEnvironment.For("/runs/NC174A");
        Assert.Equal("/runs/NC174A", env[CutRunner.McsDirectoryVariable]);
        Assert.Equal(CutRunner.LocalePin, env[WiseOwl.Cobol.Runtime.LocaleState.UserDefaultVariable]);
        Assert.Equal(NistRunEnvironment.Declared.Count, env.Count);
    }

    [Fact]
    public void Parse_ReadsCommentsBlankLinesAndHostLineEnds()
    {
        var parsed = NistRunEnvironment.Parse(["# a comment", "", "A_1\tON\r", "B\t{run-directory}/x"]);
        Assert.Equal(new KeyValuePair<string, string>[] { new("A_1", "ON"), new("B", "{run-directory}/x") }, parsed);
    }

    [Theory]
    [InlineData("lower\tx", "is not NAME<TAB>VALUE")]
    [InlineData("NOVALUE", "is not NAME<TAB>VALUE")]
    [InlineData("A\t", "is not NAME<TAB>VALUE")]
    [InlineData("A\tb\tc", "is not NAME<TAB>VALUE")]
    [InlineData("1A\tx", "is not NAME<TAB>VALUE")]
    [InlineData("A\tx\nA\ty", "declares A a second time")]
    public void Parse_RefusesAMalformedLine_NamingIt(string text, string reason)
    {
        var e = Assert.Throws<FormatException>(() => NistRunEnvironment.Parse(text.Split('\n')));
        Assert.Contains(reason, e.Message, StringComparison.Ordinal);
    }

    /// <summary>The shell leg reads the file for every program it runs and exports nothing of its own: a variable
    /// written into the runner is the second copy kb/Work PB3199 removed.</summary>
    [Fact]
    public void TheShellRunner_ReadsTheFile_AndExportsNoVariableOfItsOwn()
    {
        string runner = File.ReadAllText(TestRepo.Scripts("guard-run-group.sh"));
        Assert.Contains("RUN_ENVIRONMENT=\"$ROOT/tests/nist/run-environment.tsv\"", runner, StringComparison.Ordinal);
        Assert.Contains("guard_run_environment \"$RUN_ENVIRONMENT\" \"$WORK\" || exit 1", runner, StringComparison.Ordinal);
        var ownExports = OwnExport().Matches(runner).Select(m => m.Value.Trim()).ToList();
        Assert.True(ownExports.Count == 0,
            $"scripts/guard-run-group.sh exports a variable of its own; declare it in tests/nist/run-environment.tsv:\n{string.Join("\n", ownExports)}");
    }

    /// <summary>An <c>export NAME=…</c> line (the reader's own <c>export "$name=…"</c> is not one).</summary>
    [GeneratedRegex(@"^[ \t]*export[ \t]+[A-Za-z_][A-Za-z0-9_]*=.*$", RegexOptions.Multiline)]
    private static partial Regex OwnExport();
}
