// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
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
        Assert.Equal(cite, WiseOwl.Cobol.Binding.Procedure.FromPhraseRules.Write.PhraseAt(85, organization).Cite);

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
        Assert.StartsWith("ISO §2023:", rules.FunctionRuleCite.At(2002, organization));
    }

    private static WiseOwl.Cobol.Binding.Procedure.FromPhraseRules FromRules(string verb) => verb switch
    {
        "WRITE" => WiseOwl.Cobol.Binding.Procedure.FromPhraseRules.Write,
        "REWRITE" => WiseOwl.Cobol.Binding.Procedure.FromPhraseRules.Rewrite,
        _ => WiseOwl.Cobol.Binding.Procedure.FromPhraseRules.Release,
    };
}
