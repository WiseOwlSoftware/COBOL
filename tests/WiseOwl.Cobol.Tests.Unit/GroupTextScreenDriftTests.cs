// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A SCREEN OF THE SOURCE TEXT WALKS EVERY TEXT THE GROUP COMPILES, NOT ONLY THE WRITTEN TREE (kb/Work PB4483). The text of an expansion
/// of a skeleton read from the repository is lexed from the recorded text and parsed apart (<c>OoExpansion</c>), so the written tree does
/// not hold it. Every parse-tree screen walked <c>GroupBindContext.Tree</c>, and an expansion of such a skeleton in the group's own assembly
/// was therefore never asked the edition pass's parse-tree arm (COBOL0903 for NEXT SENTENCE), the expression-formation, declined-facility
/// or closed-format screens, while the same expansion in an assembly of its own got them from its own compilation. The walks now go over
/// <c>GroupBindContext.Texts</c>, and this guard keeps the next screen on it: a <c>CursorFollowingVisitor</c> walk of the compiler is
/// positioned at a member of <c>Texts</c> (the loop variable <c>text</c>), and <c>GroupBindContext.Tree</c> is read nowhere else, unless
/// the walk is named below with the reason it is about the written source and not its text.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class GroupTextScreenDriftTests
{
    private static readonly Regex Walk = new(@"\.VisitPositioned\((?<root>[^)]*)\)", RegexOptions.Compiled);
    private static readonly Regex GroupTree = new(@"\bgroup\.Tree\b", RegexOptions.Compiled);

    /// <summary>File-name → why its walk is of the written tree alone.</summary>
    private static readonly Dictionary<string, string> WrittenSourceOnly = new(StringComparer.Ordinal)
    {
        ["FlagConformancePass.cs"] = "the >>FLAG directives bracket lines the user wrote; a skeleton's recorded CompileState does not carry "
            + "them, so the expansion's own compilation flags nothing either",
        ["LevelNumberPass.cs"] = "pre-bind, before any expansion exists; an error-only syntax screen the skeleton's own compilation passed "
            + "under the same state, and the substitution changes only class-name words the written tree's screen asked at the entry",
        ["IntegerOperandPass.cs"] = "pre-bind and error-only, as LevelNumberPass",
        ["ProcedureFormatPass.cs"] = "pre-bind and error-only, as LevelNumberPass",
    };

    [Fact]
    public void EveryTextScreenWalksTheGroupsTexts()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (name == "CursorFollowingVisitor.cs" || WrittenSourceOnly.ContainsKey(name)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Walk.Match(lines[i]) is { Success: true } walk && walk.Groups["root"].Value != "text")
                    offenders.Add($"{name}:{i + 1}: {lines[i].Trim()}");
                if (GroupTree.IsMatch(lines[i]))
                    offenders.Add($"{name}:{i + 1}: {lines[i].Trim()}");
            }
        }
        Assert.True(offenders.Count == 0,
            "A parse-tree screen walks the written tree only, so the text of an expansion of a skeleton read from the repository is never "
            + "screened by it: walk `foreach (var text in group.Texts) pass.VisitPositioned(text);`, or name the file in WrittenSourceOnly "
            + "with the reason its walk is about the written source. Offending sites:\n  " + string.Join("\n  ", offenders));
    }

    [Fact]
    public void TheWrittenSourceWalksStillExist()
    {
        var present = Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(".VisitPositioned(", StringComparison.Ordinal))
            .Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        var stale = WrittenSourceOnly.Keys.Where(k => !present.Contains(k)).ToList();
        Assert.True(stale.Count == 0, "Exempt files that no longer walk a tree (drop them): " + string.Join(", ", stale));
    }
}
