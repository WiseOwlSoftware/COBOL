// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ AT COBOL-85 A COMPUTER-NAME IS NO IMPLEMENTOR-NAME (kb/Work PB5795; COBOL7624). "Computer-name is a system-name."
/// (§1985:VI.4.3.3 SR1, §1985:VI.4.4.3 SR1), and §1985:IV.4.2.2.1.2: "Within a given implementation these three types of
/// system-names form disjoint sets; a given system-name may belong to one and only one of them." The implementor-names are
/// the rows of this implementation's two tables, so one name of each kind stands for its table: a device-name, a
/// feature-name, a switch-name and an ALPHABET code-name, each in both paragraphs. From 2002 the disjointness covers
/// device-name, feature-name and switch-name only (§2002:8.3.1.1.2, §2023:8.3.2.3.1), so the same program compiles.
/// </summary>
public sealed class ComputerNameSystemNameTests
{
    [Theory]
    [InlineData("SOURCE-COMPUTER", "CONSOLE", "device-name")]
    [InlineData("OBJECT-COMPUTER", "C01", "feature-name")]
    [InlineData("SOURCE-COMPUTER", "UPSI-0", "switch-name")]
    [InlineData("OBJECT-COMPUTER", "ASCII", "code-name")]
    public void At85_AnImplementorNameIsRefusedAsAComputerName_AndFrom2002Accepted(string paragraph, string word, string kind)
    {
        string source = Program(paragraph, word);
        var (ok85, errors85, _) = EditionHarness.CompileFull(source, 85);
        Assert.False(ok85);
        Assert.Contains(errors85, e => e.Contains("error COBOL7624", StringComparison.Ordinal)
            && e.Contains($"the {paragraph} computer-name {word} is one of this implementation's {kind}s", StringComparison.Ordinal));
        var (ok2002, errors2002, _) = EditionHarness.CompileFull(source, 2002);
        Assert.True(ok2002, string.Join(" | ", errors2002));
    }

    /// <summary>The control: a computer-name that is no implementor-name compiles at 85, so the refusal above is the
    /// name's, not the paragraph's.</summary>
    [Fact]
    public void At85_AComputerNameThatIsNoImplementorName_Compiles()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program("OBJECT-COMPUTER", "MYBOX"), 85);
        Assert.True(ok, string.Join(" | ", errors));
    }

    private static string Program(string paragraph, string word) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CNSN85.\n       ENVIRONMENT DIVISION.\n"
        + $"       CONFIGURATION SECTION.\n       {paragraph}. {word}.\n       PROCEDURE DIVISION.\n       P1.\n"
        + "           DISPLAY \"RAN\".\n           STOP RUN.\n";
}
