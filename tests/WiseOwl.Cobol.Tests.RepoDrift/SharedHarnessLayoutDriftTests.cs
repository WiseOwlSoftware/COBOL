// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ THE LINKED SHARED HARNESS (<c>tests/_shared/</c>) KEEPS DESIGN-architecture-review.md section 8.2 RULES 1-3
/// (kb/Work PB3200).
///
/// <para><b>Why it needs its own rule.</b> <c>tests/Directory.Build.props</c> links every file of the folder into every
/// test project, so rule 1's "project root namespace plus folder" names a different namespace for each project that
/// compiles one file, and no declaration can satisfy them all. Section 8.2 (Tests) gives the folder its own root: it is
/// ONE namespace named for its repository path, <see cref="Root"/>, and each subfolder extends it. The census cannot see
/// it (it inventories the <c>src/</c> projects alone), which is how the folder came to hold two namespaces, a file named
/// for none of the 13 types it declared (<c>GateLegs.cs</c>) and a drift audit in its mechanism's file.</para>
///
/// <para><b>The rules, over every committed C# file under <c>tests/_shared/</c>.</b>
/// <list type="number">
///   <item>Rule 1: the file's namespace is <see cref="Root"/> plus its folders below <c>tests/_shared/</c>, one
///     namespace per file.</item>
///   <item>Rule 2: a file declares at most one PUBLIC top-level type, except a closed family: every other public type
///     it declares names the file's own type in its base list (<c>ITestPartitionSlot</c> and its slot ladder).</item>
///   <item>Rule 3: the file is named for a top-level type it declares.</item>
/// </list>
/// Each rule is first driven by a planted file that MUST break it, so a check that silently stopped looking turns this
/// test red rather than green.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class SharedHarnessLayoutDriftTests
{
    /// <summary>The namespace of <c>tests/_shared/</c> itself.</summary>
    private const string Root = "WiseOwl.Cobol.Tests.Shared";

    private const string Folder = "tests/_shared/";

    /// <summary>The shared harness's C# sources, COMMITTED (kb/Work PB3814): never a walk of the directory from the file
    /// system.</summary>
    private static readonly TreeScope SharedSources = new(Under: ["tests/_shared/"], Kinds: [".cs"]);

    [Fact]
    public void EverySharedHarnessFile_KeepsRulesOneToThree()
    {
        var files = TrackedTree.Files(SharedSources);
        Assert.True(files.Count >= 30, $"tests/_shared/ holds {files.Count} committed C# files: the scope did not load");
        Assert.Contains(files, f => f.StartsWith(Folder + "Gate/", StringComparison.Ordinal));

        var findings = files.SelectMany(f => Findings(f, File.ReadAllText(TestRepo.At(f.Split('/'))))).ToList();
        Assert.True(findings.Count == 0,
            "tests/_shared/ breaks DESIGN-architecture-review.md section 8.2 (Tests: the linked shared harness, "
            + "kb/Work PB3200):\n  " + string.Join("\n  ", findings));
    }

    [Theory]
    [InlineData("tests/_shared/Gate/Thing.cs", "namespace WiseOwl.Cobol.Tests.Shared;\ninternal sealed class Thing;",
        "rule 1")]
    [InlineData("tests/_shared/Thing.cs", "namespace WiseOwl.Cobol.Tests.Shared.Gate;\ninternal sealed class Thing;",
        "rule 1")]
    [InlineData("tests/_shared/Thing.cs",
        "namespace WiseOwl.Cobol.Tests.Shared { internal sealed class Thing; }\nnamespace Other { internal sealed class Other; }",
        "rule 1")]
    [InlineData("tests/_shared/Thing.cs",
        "namespace WiseOwl.Cobol.Tests.Shared;\npublic sealed class Thing;\npublic enum Kind { A }", "rule 2")]
    [InlineData("tests/_shared/Things.cs", "namespace WiseOwl.Cobol.Tests.Shared;\ninternal sealed class Thing;",
        "rule 3")]
    public void APlantedBreak_IsFound(string path, string text, string rule) =>
        Assert.Contains(Findings(path, text), f => f.Contains(rule, StringComparison.Ordinal));

    [Theory]
    [InlineData("tests/_shared/Gate/Thing.cs",
        "namespace WiseOwl.Cobol.Tests.Shared.Gate;\npublic sealed class Thing;\ninternal sealed class Helper;")]
    [InlineData("tests/_shared/ISlot.cs",
        "namespace WiseOwl.Cobol.Tests.Shared;\npublic interface ISlot;\npublic readonly struct Slot0 : ISlot;")]
    public void AConformingFile_IsNotFound(string path, string text) => Assert.Empty(Findings(path, text));

    /// <summary>Every rule <paramref name="text"/> breaks as the file <paramref name="path"/>.</summary>
    private static IEnumerable<string> Findings(string path, string text)
    {
        CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
        string[] folders = path[Folder.Length..].Split('/')[..^1];
        string expected = string.Join('.', [Root, .. folders]);
        var namespaces = unit.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString()).Distinct(StringComparer.Ordinal).ToList();
        if (namespaces.Count != 1 || namespaces[0] != expected)
        {
            yield return $"{path}: rule 1, declares namespace {string.Join(", ", namespaces.DefaultIfEmpty("(none)"))}; "
                         + $"its folder is {expected}";
        }

        var types = unit.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .Where(t => t.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax).ToList();
        string own = Path.GetFileNameWithoutExtension(path);
        if (!types.Any(t => t.Identifier.ValueText == own))
        {
            yield return $"{path}: rule 3, declares no type named {own} (it declares "
                         + $"{string.Join(", ", types.Select(t => t.Identifier.ValueText))})";
        }

        var published = types.Where(t => t.Modifiers.Any(SyntaxKind.PublicKeyword)).ToList();
        var strays = published.Where(t => t.Identifier.ValueText != own
                                          && !(t.BaseList?.Types.Any(b => BaseName(b.Type) == own) ?? false))
            .Select(t => t.Identifier.ValueText).ToList();
        if (published.Count > 1 && strays.Count > 0)
        {
            yield return $"{path}: rule 2, declares public {string.Join(", ", strays)} beside {own} and outside its family";
        }
    }

    private static string BaseName(TypeSyntax type) => type switch
    {
        QualifiedNameSyntax q => q.Right.Identifier.ValueText,
        SimpleNameSyntax s => s.Identifier.ValueText,
        _ => type.ToString(),
    };
}
