// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Architecture;

/// <summary>
/// ⛔ A SOURCE ELEMENT'S DATA BINDER IS SEEDED WITH THE SESSION'S FACTS IN ONE PLACE (kb/Work PB2626). The class table, the
/// element's census, &gt;&gt;REF-MOD-ZERO-LENGTH, &gt;&gt;COBOL-WORDS, the token retypes, the storage-claim walk, &gt;&gt;LEAP-SECOND at the
/// element and the &gt;&gt;DEFINE timeline reach every program unit and every OO element through
/// <c>BindSession.NewDataBinder</c>. Four hand-written initializers had to be kept in step, and the one that missed
/// &gt;&gt;LEAP-SECOND (kb/Work PB1820) gave a method the wrong SECONDS-FROM-FORMATTED-TIME. This test refuses a
/// <c>new DataBinder(</c> anywhere else in the compiler, with one exception: <c>OoExpansion</c>'s parameter-name scope, which
/// binds no source element's data and exists only to declare a parameterized definition's formals (§2023:12.3.8.3 SR12/SR13).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DataBinderSeedDriftTests
{
    private static readonly Regex Construction = new(@"\bnew\s+DataBinder\s*\(", RegexOptions.Compiled);

    [Fact]
    public void ADataBinderIsBuiltOnlyByTheSessionSeed()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        string parameterScope = Path.Combine("Oo", "OoExpansion.cs");
        var offenders = new List<string>();
        int exempt = 0;
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, file);
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].Split("//", 2)[0];
                if (!Construction.IsMatch(code)) continue;
                if (rel == parameterScope) { exempt++; continue; }
                offenders.Add($"{rel}:{i + 1}: {code.Trim()}");
            }
        }
        Assert.True(exempt == 1, $"OoExpansion's parameter-name scope binder moved ({exempt} constructions there): update this guard");
        Assert.True(offenders.Count == 0,
            "a DataBinder is constructed outside BindSession.NewDataBinder (kb/Work PB2626), so it misses the session's facts:\n  "
            + string.Join("\n  ", offenders));

        string session = File.ReadAllText(Path.Combine(root, "Binding", "BindSession.cs"));
        Assert.Contains("internal DataBinder NewDataBinder(", session, StringComparison.Ordinal);
        foreach (var (file, caller) in new[] { (Path.Combine("Binding", "BinderDriver.cs"), "BindUnitData"), (Path.Combine("Oo", "OoDriver.cs"), "ElementBinder") })
            Assert.True(File.ReadAllText(Path.Combine(root, file)).Contains("session.NewDataBinder(", StringComparison.Ordinal),
                $"{caller} in {file} no longer builds its binder through BindSession.NewDataBinder");
    }
}
