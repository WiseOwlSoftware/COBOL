// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>⛔ ONE TABLE FROM AN I-O STATUS TO ITS CONDITION (kb/Work PB810). Every emitted phrase branch and both
/// generated after-verb hooks decide "is this the at end condition / the invalid key condition / a successful
/// completion" from the status's first character, and that decision is §9.1.13.1's table, written ONCE in
/// <c>CodeGen/Verbs/IoStatusClass.cs</c>. PB810 found the READ arms justifying the same comparison from two
/// different clauses — one of them a rule about a DIFFERENT condition (§9.1.14, invalid key) — because each site
/// wrote its own; this guard makes a new site ask the table instead.</summary>
public sealed class IoStatusClassDriftTests
{
    /// <summary>A first-character comparison of a status against a class digit, the shape every site used.</summary>
    private static readonly Regex StatusClassTest = new(@"\[0\]\s*[!=]=\s*'\d'", RegexOptions.Compiled);

    private const string TableHome = "IoStatusClass.cs";

    [Fact]
    public void OnlyIoStatusClass_ClassifiesAnIoStatus()
    {
        string codegen = TestRepo.Src("Cobol.Net.Compiler", "CodeGen");
        Assert.True(Directory.Exists(codegen), $"CodeGen moved: {codegen} is not a directory.");
        Assert.True(File.Exists(Path.Combine(codegen, "Verbs", TableHome)), "the status-class table moved");

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(codegen, "*.cs", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFileName(file), TableHome, StringComparison.Ordinal)) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal)) continue;   // prose may quote the shape
                if (StatusClassTest.IsMatch(code))
                    offenders.Add($"{Path.GetRelativePath(codegen, file)}:{i + 1}: {code}");
            }
        }
        Assert.True(offenders.Count == 0,
            "An I-O status is classified outside IoStatusClass (§9.1.13.1's table, kb/Work PB810) — "
            + "call IoStatusClass.Successful/AtEnd/InvalidKey/Unsuccessful instead:\n" + string.Join("\n", offenders));
    }

    /// <summary>The table's three phrase-driving classes, pinned to the values §9.1.13.2, §9.1.13.4 and
    /// §9.1.13.5 give them — and '46' (§9.1.13.7 6), a logic error) in none of the phrase classes.</summary>
    [Theory]
    [InlineData("00", true, false, false)]
    [InlineData("04", true, false, false)]
    [InlineData("10", false, true, false)]
    [InlineData("14", false, true, false)]
    [InlineData("23", false, false, true)]
    [InlineData("46", false, false, false)]
    [InlineData("30", false, false, false)]
    public void TheTable_MatchesTheStandardsClasses(string status, bool ok, bool atEnd, bool invalid)
    {
        static bool Eval(string cond, string st) => cond switch
        {
            _ when cond == $"st[0] == '0'" => st[0] == '0',
            _ when cond == $"st[0] == '1'" => st[0] == '1',
            _ when cond == $"st[0] == '2'" => st[0] == '2',
            _ => throw new InvalidOperationException("unexpected rendering: " + cond),
        };
        Assert.Equal(ok, Eval(CobolNet.CodeGen.IoStatusClass.Successful("st"), status));
        Assert.Equal(atEnd, Eval(CobolNet.CodeGen.IoStatusClass.AtEnd("st"), status));
        Assert.Equal(invalid, Eval(CobolNet.CodeGen.IoStatusClass.InvalidKey("st"), status));
    }
}
