// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>A diagnostic of the INTO and FROM phrases cites the rule as the edition being compiled numbers it (kb/Work PB5352):
/// at COBOL-85 the general rule of the file's own I-O module (ANSI X3.23-1985 restates READ once per organization), from 2002
/// the 2023 syntax rule. The conformance refusals pb5352-* pin the message; this pins the selection for every organization.</summary>
public sealed class EditionCiteTests
{
    [Theory]
    [InlineData(FileOrganization.Sequential, "§1985:VII.4.4.4 GR7")]
    [InlineData(FileOrganization.LineSequential, "§1985:VII.4.4.4 GR7")]
    [InlineData(FileOrganization.Relative, "§1985:VIII.4.5.4 GR7")]
    [InlineData(FileOrganization.Indexed, "§1985:IX.4.5.4 GR7")]
    public void ReadInto_At85_CitesTheModuleOfTheFilesOrganization(FileOrganization organization, string cite) =>
        Assert.Equal(cite, IntoPhraseRules.Read.Admissibility.At(85, organization));

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ReadInto_From2002_CitesThe2023SyntaxRule(int dialectLevel) =>
        Assert.Equal("ISO §2023:14.9.30.3 SR1", IntoPhraseRules.Read.Admissibility.At(dialectLevel, FileOrganization.Indexed));

    [Fact]
    public void ReturnInto_At85_CitesTheSortMergeModuleWhateverTheOrganization() =>
        Assert.Equal("§1985:XI.4.3.4 GR5", IntoPhraseRules.Return.Admissibility.At(85, FileOrganization.Relative));

    [Fact]
    public void ARuleWithNo1985Arm_IsCitedByIts2023NumberAt85() =>
        Assert.Equal("ISO §2023:14.9.30.3 SR2", new EditionCite("ISO §2023:14.9.30.3 SR2").At(85, FileOrganization.Indexed));

    [Theory]
    [InlineData(FileOrganization.Sequential, "§1985:VII.4.7.4 GR3")]
    [InlineData(FileOrganization.Relative, "§1985:VIII.4.9.4 GR3")]
    [InlineData(FileOrganization.Indexed, "§1985:IX.4.9.4 GR3")]
    public void WriteFrom_At85_CitesTheModulesMoveRule(FileOrganization organization, string cite) =>
        Assert.Equal(cite, WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs.FromPhraseRules.Write.PhraseAt(85, organization).Cite);

    /// <summary>kb/Work PB5486: the FROM function-class rule is a 1985 rule too (ANSI X3.23a-1989 added it to each FROM syntax rule),
    /// so its message cites the module of the file's organization at 85 and the 2023 rule from 2002.</summary>
    [Theory]
    [InlineData("WRITE", FileOrganization.Sequential, "§1985:VII.4.7.3 SR1")]
    [InlineData("WRITE", FileOrganization.Relative, "§1985:VIII.4.9.3 SR1")]
    [InlineData("WRITE", FileOrganization.Indexed, "§1985:IX.4.9.3 SR1")]
    [InlineData("REWRITE", FileOrganization.Sequential, "§1985:VII.4.5.3 SR1")]
    [InlineData("REWRITE", FileOrganization.Relative, "§1985:VIII.4.6.3 SR1")]
    [InlineData("REWRITE", FileOrganization.Indexed, "§1985:IX.4.6.3 SR1")]
    [InlineData("RELEASE", FileOrganization.Sequential, "§1985:XI.4.2.3 SR3")]
    public void FromFunctionRule_At85_CitesTheModulesSyntaxRule(string verb, FileOrganization organization, string cite)
    {
        var rules = FromRules(verb);
        string at85 = rules.FunctionRuleCite!.At(85, organization);
        Assert.StartsWith(cite + " — ", at85);
        Assert.EndsWith("\"If identifier-1 is a function-identifier, it must reference an alphanumeric function.\"", at85);
        Assert.DoesNotContain("§1985", rules.FunctionRuleCite.At(2002, organization));
    }

    /// <summary>⛔ kb/Work PB6072: from 2002 the FROM function-class rule and the MOVE rule of each verb are cited by the number and
    /// words of the edition compiled, not the 2023 locator with a 2002 number beside it. The 2002 REWRITE and WRITE FILE texts restrict an
    /// INTRINSIC function and the 2002 WRITE text writes "alphanumeric, or national", so the quotation must be each edition's own.</summary>
    [Theory]
    [InlineData("WRITE", 2002, "ISO §2002:14.8.47.2 SR4 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric, or national function.\"")]
    [InlineData("WRITE", 2014, "ISO §2014:14.9.47.2 SR4 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or national function.\"")]
    [InlineData("WRITE", 2023, "ISO §2023:14.9.51.3 SR4 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or national function.\"")]
    [InlineData("REWRITE", 2002, "ISO §2002:14.8.33.2 SR9 — \"If identifier-1 references an intrinsic function and the FILE phrase is not specified, identifier-1 shall reference an alphanumeric, boolean, or national function.\"")]
    [InlineData("REWRITE", 2014, "ISO §2014:14.9.33.2 SR8 — \"If identifier-1 references a function and the FILE phrase is not specified, identifier-1 shall reference an alphanumeric, boolean, or national function.\"")]
    [InlineData("REWRITE", 2023, "ISO §2023:14.9.35.3 SR9 — \"If identifier-1 references a function and the FILE phrase is not specified, identifier-1 shall reference an alphanumeric, boolean, or national function.\"")]
    [InlineData("RELEASE", 2002, "ISO §2002:14.8.30.2 SR2 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or national function.\"")]
    [InlineData("RELEASE", 2014, "ISO §2014:14.9.30.2 SR2 — \"If identifier-1 is a function-identifier, it shall reference an alphanumeric or national function.\"")]
    public void FromFunctionRule_From2002_IsTheEditionsOwnRule(string verb, int dialectLevel, string expected) =>
        Assert.Equal(expected, FromRules(verb).FunctionRuleCite!.At(dialectLevel, FileOrganization.Sequential));

    [Theory]
    [InlineData("WRITE", 2002, "ISO §2002:14.8.47.3 GR3a; §2002:14.8.47.2 SR6")]
    [InlineData("WRITE", 2014, "ISO §2014:14.9.47.3 GR3a; §2014:14.9.47.2 SR6")]
    [InlineData("REWRITE", 2002, "ISO §2002:14.8.33.3 GR6a; §2002:14.8.33.2 SR6")]
    [InlineData("REWRITE", 2014, "ISO §2014:14.9.33.3 GR6a; §2014:14.9.33.2 SR5")]
    [InlineData("RELEASE", 2002, "ISO §2002:14.8.30.3 GR4a; §2002:14.8.30.2 SR3")]
    [InlineData("RELEASE", 2014, "ISO §2014:14.9.30.3 GR4a; §2014:14.9.30.2 SR3")]
    public void FromMoveRule_From2002_IsTheEditionsOwnRule(string verb, int dialectLevel, string expected) =>
        Assert.Equal(expected, FromRules(verb).MoveRule.At(dialectLevel, FileOrganization.Sequential));

    private static WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs.FromPhraseRules FromRules(string verb) => verb switch
    {
        "WRITE" => WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs.FromPhraseRules.Write,
        "REWRITE" => WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs.FromPhraseRules.Rewrite,
        _ => WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs.FromPhraseRules.Release,
    };
}
