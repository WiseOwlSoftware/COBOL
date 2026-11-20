// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ NO DOCUMENTATION COMMENT UNDER <c>src/</c> OR <c>tests/</c> HOLDS TWO <c>summary</c> ELEMENTS OR TWO <c>remarks</c>
/// ELEMENTS (kb/Work PB5932, listing step 6b-r4 review R1-2). A member inserted between an existing doc comment and the
/// member that comment documents leaves ONE comment block with two summaries above the new member: the new member's
/// documentation shows the old member's contract, and the old member shows none. Nothing else catches it: the C# compiler
/// accepts the shape without a warning, and the comment reads correctly line by line. The review of that branch found two
/// such insertions; the sweep that followed found 45 more across the compiler, the runtime, the front end and the tests,
/// each a contract attributed to the wrong member or a summary left behind by a deleted one.
/// <para>
/// The comments are read by Roslyn, as the documentation generator reads them (<see cref="DocumentationMode.Parse"/>):
/// a top-level <c>summary</c> or <c>remarks</c> element of one <see cref="DocumentationCommentTriviaSyntax"/>, never a
/// text match, so a <c>&lt;summary&gt;</c> written inside a <c>code</c> element or quoted in prose is not one.
/// <see cref="TheSweepFiresOnAPlantedSecondSummary"/> fires the failure branch; <see cref="TheSweepReadsTheTree"/>
/// requires the sweep to have read the tree.
/// </para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DocCommentShapeDriftTests
{
    /// <summary>The files swept: every tracked C# source of the compiler, the runtime, the front end and the tests.</summary>
    private static readonly TreeScope CSharpSources = new(Under: ["src/", "tests/"], Kinds: [".cs"]);

    /// <summary>The elements one documentation comment holds at most once.</summary>
    private static readonly string[] SingleElements = ["summary", "remarks"];

    /// <summary>The one sweep of the committed tree both tree tests judge, read once per test assembly.</summary>
    private static readonly Lazy<SweepResult> s_tree = new(() => Sweep(TestRepo.Root, TrackedTree.Files(CSharpSources)));

    /// <summary>What a sweep saw: each offender as <c>path:line: element ×count</c>, and the files read.</summary>
    internal sealed record SweepResult(IReadOnlyList<string> Offenders, int FilesScanned);

    /// <summary>No documentation comment under <c>src/</c> or <c>tests/</c> holds a second <c>summary</c> or
    /// <c>remarks</c> element.</summary>
    [Fact]
    public void NoDocCommentHoldsASingleElementTwice()
    {
        SweepResult r = s_tree.Value;

        Assert.True(r.Offenders.Count == 0,
            $"{r.Offenders.Count} documentation comment(s) hold a summary or remarks element twice ({r.FilesScanned} files read):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, r.Offenders.Select(o => "    " + o))
            + Environment.NewLine
            + "A second summary usually means a member was inserted between a doc comment and the member it documents: move "
            + "the first summary down to its own member (or delete it when its member is gone), and merge two descriptions "
            + "of one member into one element.");
    }

    /// <summary>The sweep read the tree it claims to have swept, so an empty enumeration cannot pass for a clean tree.</summary>
    [Fact]
    public void TheSweepReadsTheTree()
    {
        Assert.True(s_tree.Value.FilesScanned >= 2000,
            $"the doc-comment sweep read only {s_tree.Value.FilesScanned} C# file(s) under {TestRepo.Root}: the enumeration is broken.");
    }

    /// <summary>The sweep fires on a member under two summaries and on one with two remarks, and passes one summary
    /// with its remarks, a <c>summary</c> written inside a <c>code</c> element, and a <c>summary</c> named in a plain
    /// comment.</summary>
    [Fact]
    public void TheSweepFiresOnAPlantedSecondSummary()
    {
        string scratch = ScratchDirectory.Create("wiseowl-cobol-doc-shape-");
        try
        {
            Directory.CreateDirectory(Path.Combine(scratch, "src"));
            File.WriteAllText(Path.Combine(scratch, "src", "Planted.cs"), string.Join("\n",
                "class C",
                "{",
                "    /// <summary>The old member's contract.</summary>",
                "    /// <summary>The inserted member's.</summary>",
                "    void Inserted() { }",
                "",
                "    void Old() { }",
                "",
                "    /// <summary>One.</summary>",
                "    /// <remarks>First.</remarks>",
                "    /// <remarks>Second.</remarks>",
                "    void TwoRemarks() { }",
                "",
                "    /// <summary>Fine.</summary>",
                "    /// <remarks><code>/// <summary>quoted</summary></code></remarks>",
                "    void Clean() { }",
                "",
                "    // <summary>a plain comment</summary> <summary>twice</summary>",
                "    void Plain() { }",
                "}",
                ""));

            SweepResult r = Sweep(scratch, ["src/Planted.cs"]);

            Assert.Equal(new[] { "src/Planted.cs:3: summary ×2", "src/Planted.cs:9: remarks ×2" }, r.Offenders);
            Assert.Equal(1, r.FilesScanned);
        }
        finally
        {
            ScratchDirectory.Remove(scratch);
        }
    }

    /// <summary>Read each of <paramref name="paths"/> (repo-root-relative) beneath <paramref name="root"/> and report every
    /// documentation comment holding one of <see cref="SingleElements"/> more than once.</summary>
    internal static SweepResult Sweep(string root, IEnumerable<string> paths)
    {
        var options = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse);
        var offenders = new List<string>();
        int scanned = 0;
        foreach (string path in paths)
        {
            string full = Path.Combine(root, path);
            if (!File.Exists(full)) continue;
            scanned++;
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(full), options);
            foreach (var doc in tree.GetRoot().DescendantTrivia(descendIntoTrivia: false)
                         .Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>())
            {
                foreach (string element in SingleElements)
                {
                    int count = doc.Content.OfType<XmlElementSyntax>().Count(e => e.StartTag.Name.LocalName.Text == element);
                    if (count > 1)
                        offenders.Add($"{path}:{doc.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {element} ×{count}");
                }
            }
        }

        return new SweepResult(offenders, scanned);
    }
}
