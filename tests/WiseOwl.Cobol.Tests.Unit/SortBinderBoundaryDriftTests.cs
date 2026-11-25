// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A PHRASE SORT AND MERGE BOTH PRINT IS BOUND IN ONE PLACE (kb/Work PB2341; docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3, census R0-0046). ISO §2023:14.9.40 (SORT, Format 1) and §2023:14.9.24 (MERGE) print the same KEY, COLLATING SEQUENCE,
/// USING, GIVING and OUTPUT PROCEDURE phrases and state twin syntax rules over the files they name (SORT SR5/SR9/SR10/SR11,
/// MERGE SR3/SR10/SR11/SR12). <c>SortMergePhraseBinder</c> binds each of them once for both verbs, and <c>SortBinder</c> binds
/// the four statements and asks it. The types cannot say that a second copy of a shared rule does not grow back inside the
/// statement binder, so this test says it: each shared member is declared only in <c>SortMergePhraseBinder.cs</c>, and the
/// statement binder reaches the phrases only through that one collaborator.
/// </summary>
public sealed class SortBinderBoundaryDriftTests
{
    /// <summary>The members both verbs share — the phrases, the SD record and the screens over the statement's files.</summary>
    public static TheoryData<string> SharedMembers() =>
    [
        "ScreenDeclarativePlacement", "MarkRefusedSortMerge", "RecordLessSd", "SortRecordOf", "SortAddFileKeys",
        "SortBindCollating", "SortMapIoFiles", "ScreenIndexedGivingKey", "ScreenTransferRecordSizes", "ScreenSameClauses",
        "ScreenMultipleFileReels", "CheckSegmentation", "SortRange", "SortVaryingOf", "SortRootOf",
    ];

    /// <summary>The verb binders: a statement binder is where a shared phrase rule would grow back. (A member of the same name
    /// elsewhere is another rule: <c>FileControlBinder.ScreenSameClauses</c> screens the SAME clause itself.)</summary>
    private static IEnumerable<(string File, string Text)> VerbBinderSources() =>
        Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f).Replace('\\', '/'), File.ReadAllText(f)));

    [Theory]
    [MemberData(nameof(SharedMembers))]
    public void TheSharedMember_IsDeclaredOnlyInThePhraseBinder(string member)
    {
        // A declaration: an access modifier, then the return type, then the name and its parameter list.
        var declaration = new Regex(@"^\s*(?:internal|private|public)\b[^\n;=]*?\b" + member + @"\(", RegexOptions.Multiline);
        var homes = VerbBinderSources().Where(s => declaration.IsMatch(s.Text)).Select(s => s.File).ToList();
        Assert.True(homes.Count == 1 && homes[0].EndsWith("/Binding/Procedure/Verbs/SortMergePhraseBinder.cs", StringComparison.Ordinal),
            $"{member} must be declared once, in SortMergePhraseBinder.cs (kb/Work PB2341); declared in: "
            + (homes.Count == 0 ? "nowhere — the pattern no longer matches the code" : string.Join(", ", homes)));
    }

    [Fact]
    public void TheStatementBinder_AsksThePhrasesThroughItsOneCollaborator()
    {
        string sort = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "Binding", "Procedure", "Verbs", "SortBinder.cs"));
        Assert.Contains("private readonly SortMergePhraseBinder _phrases = new(ctx, host);", sort, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"_phrases\.SortAddFileKeys\([^;]*SortKeyRules\.FileSort"), sort);
        Assert.Matches(new Regex(@"_phrases\.SortAddFileKeys\([^;]*SortKeyRules\.Merge"), sort);
    }
}
