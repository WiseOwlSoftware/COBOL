// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ ONLY THE ROSLYN BACKEND REFERENCES ROSLYN (kb/Work PB4272; DESIGN-backend-abstraction §2.1, §3.4). The backend seam
/// (<c>CodeGen/ICodeGenBackend.cs</c>), the model, the binders and the driver name no <c>Microsoft.CodeAnalysis</c> type,
/// so a direct-IL backend implements <see cref="ICodeGenBackend"/> without loading Roslyn, and PB4273's model assembly can
/// be built with no Roslyn package. The facts the model used to ASK Roslyn — which characters an identifier may hold, and
/// the C# reserved keywords (<see cref="CsNameReservationDriftTests"/>) — it now states, and this class pins each
/// statement to the Roslyn the compiler builds against, as it pins the Roslyn backend's translation of its diagnostics
/// into the seam's <see cref="BackendDiagnostic"/> to the text Roslyn itself renders, so the restructuring changes no
/// byte a user sees.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RoslynFreeSeamDriftTests
{
    /// <summary>The Roslyn backend's files: everything under <c>CodeGen/</c> except the seam itself. Every other source
    /// file of the compiler is the backend-neutral side.</summary>
    private static bool IsRoslynBackendFile(string rel) =>
        rel.StartsWith("CodeGen/", StringComparison.Ordinal) && rel != "CodeGen/ICodeGenBackend.cs";

    /// <summary>The lines of <paramref name="source"/> that name the <c>Microsoft.CodeAnalysis</c> namespace in code (a
    /// using directive, an alias or a qualified name), read from its tokens, so a comment or a string that mentions the
    /// namespace is not a use.</summary>
    internal static IReadOnlyList<int> RoslynUses(string source)
    {
        var tokens = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantTokens().ToList();
        var lines = new List<int>();
        for (int i = 0; i + 2 < tokens.Count; i++)
            if (tokens[i].IsKind(SyntaxKind.IdentifierToken) && tokens[i].ValueText == "Microsoft"
                && tokens[i + 1].IsKind(SyntaxKind.DotToken)
                && tokens[i + 2].IsKind(SyntaxKind.IdentifierToken) && tokens[i + 2].ValueText == "CodeAnalysis")
                lines.Add(tokens[i].GetLocation().GetLineSpan().StartLinePosition.Line + 1);
        return lines;
    }

    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("WiseOwl.Cobol.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            if (rel.StartsWith("obj/", StringComparison.Ordinal) || rel.StartsWith("bin/", StringComparison.Ordinal))
                continue;
            yield return (rel, File.ReadAllText(f));
        }
    }

    [Fact]
    public void NoSourceOutsideTheRoslynBackend_NamesRoslyn()
    {
        var sources = CompilerSources().ToList();
        Assert.True(sources.Count > 200, $"the scan read only {sources.Count} compiler sources");
        // The probe sees what it guards: the Roslyn backend itself names Roslyn.
        Assert.NotEmpty(RoslynUses(sources.Single(s => s.Rel == "CodeGen/RoslynBackend.cs").Text));

        var bad = sources.Where(s => !IsRoslynBackendFile(s.Rel))
            .SelectMany(s => RoslynUses(s.Text).Select(line => $"{s.Rel}:{line}"))
            .ToList();
        Assert.True(bad.Count == 0,
            "a backend-neutral compiler source names Microsoft.CodeAnalysis (kb/Work PB4272): the seam, the model, the "
            + "binders and the driver reference no code-generation backend. State the fact in the model (as CsNames does) "
            + "or move the code into the Roslyn backend:\n  " + string.Join("\n  ", bad));

        string csproj = File.ReadAllText(TestRepo.Src("WiseOwl.Cobol.Compiler", "WiseOwl.Cobol.Compiler.csproj"));
        Assert.DoesNotContain("<Using Include=\"Microsoft.CodeAnalysis", csproj, StringComparison.Ordinal);
    }

    /// <summary>The scan fails for the right reason: each way a file can name Roslyn is found, and a mention in a comment, a
    /// documentation comment or a string is not.</summary>
    [Fact]
    public void TheScan_FindsEachFormOfUse_AndNoMention()
    {
        const string planted = """
            using Microsoft.CodeAnalysis.CSharp;
            using R = Microsoft.CodeAnalysis.Diagnostic;
            /// <summary>Microsoft.CodeAnalysis in a doc comment, <see cref="Microsoft.CodeAnalysis.Location"/>.</summary>
            static class P
            {
                // Microsoft.CodeAnalysis in a comment
                const string S = "Microsoft.CodeAnalysis";
                static bool K(char c) => Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsIdentifierStartCharacter(c);
                static global::Microsoft.CodeAnalysis.Location? L;
            }
            """;
        Assert.Equal([1, 2, 8, 9], RoslynUses(planted));
    }

    /// <summary>The model's identifier character classes are Roslyn's, for every UTF-16 code unit: a name
    /// <see cref="DataItem.Sanitize"/> builds is the one it built when it asked <c>SyntaxFacts</c>.</summary>
    [Fact]
    public void TheIdentifierCharacterClasses_AreRoslyns_ForEveryCodeUnit()
    {
        var bad = new List<string>();
        for (int i = char.MinValue; i <= char.MaxValue; i++)
        {
            char c = (char)i;
            if (CsNames.IsIdentifierStartCharacter(c) != SyntaxFacts.IsIdentifierStartCharacter(c))
                bad.Add($"start U+{i:X4}");
            if (CsNames.IsIdentifierPartCharacter(c) != SyntaxFacts.IsIdentifierPartCharacter(c))
                bad.Add($"part U+{i:X4}");
        }
        Assert.True(bad.Count == 0, $"{bad.Count} code unit(s) differ from Roslyn's identifier rule: " + string.Join(", ", bad.Take(40)));
        Assert.True(CsNames.IsIdentifierStartCharacter('_') && !CsNames.IsIdentifierStartCharacter('1')
            && CsNames.IsIdentifierPartCharacter('1') && !CsNames.IsIdentifierPartCharacter('-'));   // the probe sees what it guards
    }

    /// <summary>Each Roslyn diagnostic crosses the seam as a <see cref="BackendDiagnostic"/> that renders Roslyn's own line:
    /// an error and a warning in the source (the generated C# is parsed without a path, so the line begins
    /// <c>(line,col)</c>), and an error that names no position. A hidden diagnostic is not carried.</summary>
    [Fact]
    public void TheNeutralDiagnostics_RenderRoslynsText()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            using System.Text;
            class C
            {
                void M() { int unused; }
                int y = z;
            }
            """);
        var compilation = CSharpCompilation.Create("probe", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        var roslyn = compilation.GetDiagnostics();
        Assert.Contains(roslyn, d => d.Id == "CS0103" && d.Location.IsInSource);    // error in the source
        Assert.Contains(roslyn, d => d.Id == "CS0168" && d.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(roslyn, d => d.Id == "CS5001" && !d.Location.IsInSource);   // no Main: no position
        Assert.Contains(roslyn, d => d.Severity == DiagnosticSeverity.Hidden);      // the unnecessary using

        var carried = roslyn.Where(d => d.Severity != DiagnosticSeverity.Hidden).ToList();
        var neutral = RoslynBackend.Neutral(roslyn);
        Assert.Equal(carried.Select(d => d.ToString()), neutral.Select(d => d.ToString()));
        Assert.Equal(carried.Select(d => d.Severity == DiagnosticSeverity.Error), neutral.Select(d => d.IsError));
        Assert.Equal(carried.Select(d => d.Id), neutral.Select(d => d.Code));
        Assert.Null(neutral.Single(d => d.Code == "CS5001").Location);
        Assert.StartsWith("(5,13): error CS0103: ", neutral.Single(d => d.Code == "CS0103").ToString(), StringComparison.Ordinal);
    }
}
