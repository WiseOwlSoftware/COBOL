// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A <c>FileStatusCode</c> SUMMARY THAT NAMES THE STATEMENTS A STATUS IS SET BY NAMES THEM ALL, AND THE CLASS
/// SUMMARY PROMISES NO MORE THAN THAT (kb/Work PB671, branch review R1-1). The class summary once said "Each
/// constant's summary names every statement that sets it" while 05, 41 and 42 named only OPEN or CLOSE; the standard
/// lists DELETE FILE for 05 (§9.1.13.2 item 4 b) and 41 (§9.1.13.7 item 1) and UNLOCK for 42 (§9.1.13.7 item 2), and a
/// maintainer tracing every producer of a status from the summary would have missed those verbs. The statements for
/// each status below are read back from the standard's own item (so a spec repair reaches this table or fails here)
/// and must each appear in the constant's summary; the class summary may not claim exhaustiveness for the rest.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class FileStatusCodeDocDriftTests
{
    private static readonly string[] Spec = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
    private static readonly string Source = File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", "FileStatus.cs"));

    /// <summary>Status, constant name and the statements §9.1.13's item for that status names.</summary>
    public static TheoryData<string, string, string[]> Documented => new()
    {
        { "05", "OptionalFileNotFound", ["OPEN", "DELETE FILE"] },
        { "37", "PermissionDenied", ["OPEN", "DELETE FILE"] },
        { "41", "FileAlreadyOpen", ["OPEN", "DELETE FILE"] },
        { "42", "FileNotOpen", ["CLOSE", "UNLOCK"] },
        { "43", "NoSuccessfulReadBeforeDeleteRewrite", ["DELETE RECORD", "REWRITE"] },
    };

    private static string SpecItem(string status)
    {
        int start = Array.FindIndex(Spec, l => Regex.IsMatch(l, @"^\d+\\\) I-O status = " + status + @"\b"));
        Assert.True(start >= 0, $"spec item for I-O status {status} not found");
        int end = Array.FindIndex(Spec, start + 1, l => Regex.IsMatch(l, @"^\d+\\\) I-O status = |^#"));
        Assert.True(end > start);
        return string.Join("\n", Spec[start..end]);
    }

    private static string Summary(string constant)
    {
        Match m = Regex.Match(Source, @"/// <summary>(?<s>(?:(?!</summary>).|\n)*)</summary>\s*public const string " + constant + " =",
            RegexOptions.None);
        Assert.True(m.Success, $"summary of {constant} not found");
        return m.Groups["s"].Value;
    }

    [Theory]
    [MemberData(nameof(Documented))]
    public void TheSummary_NamesEveryStatementTheStandardsItemNames(string status, string constant, string[] statements)
    {
        string item = SpecItem(status);
        string summary = Summary(constant);
        foreach (string statement in statements)
        {
            Assert.True(item.Contains(statement, StringComparison.Ordinal),
                $"the standard's item for I-O status {status} no longer names {statement}; update the table");
            Assert.True(summary.Contains(statement, StringComparison.Ordinal),
                $"FileStatusCode.{constant} ('{status}') does not name {statement}, which §9.1.13 lists for it");
        }
    }

    [Fact]
    public void TheClassSummary_DoesNotPromiseEveryStatement()
    {
        Assert.DoesNotMatch(new Regex(@"names every\s+(///\s*)?statement", RegexOptions.NonBacktracking), Source);
    }
}
