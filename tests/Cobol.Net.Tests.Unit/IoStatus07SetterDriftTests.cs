// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ I-O STATUS '07' HAS EXACTLY TWO SETTERS, THE ONE OPEN DISPATCH'S NO REWIND ARM AND THE ONE CLOSE DISPATCH, AND NO
/// OTHER INPUT-OUTPUT STATEMENT, ORGANIZATION OR EMITTER WRITES IT, AT ANY EDITION (kb/Work PB1515). ISO §9.1.13.2
/// item 6: <i>"An OPEN or CLOSE statement is successfully executed but a CLOSE statement with the NO REWIND,
/// REEL/UNIT, or FOR REMOVAL phrase or an OPEN statement with the NO REWIND phrase references a physical file on a
/// non-reel/unit medium"</i>, and Annex E.2 item 16: <i>"The setting of I-O Status '07' is now restricted to the
/// execution of the OPEN and CLOSE statements."</i> The repository holds no text of an earlier edition and E.2 names
/// neither the other statements nor the condition they reported, so docs/CONFORMANCE.md §3 records the determination
/// that the restriction holds at every edition (the surveyed implementation sets '07' from CLOSE alone); this test is
/// the structure that keeps the determination true.
/// <para>Every write goes through <see cref="FileStatusCode.PhraseOnNonReelMedium"/>, and a second spelling of the
/// value is a second setter nobody is guarding, so the test reads both: the named constant may be used only inside
/// <c>FileRegistry.CloseByFormat</c> (Table 14's symbols e and g, §14.9.6.4 GR3) and
/// <c>FileRegistry.NoRewindPhraseEffect</c> (§14.9.27.4 GR11), and the string <c>"07"</c> appears in the runtime and the
/// code generator nowhere but the constant's own declaration. A new medium or organization that needs the status
/// changes those two dispatches, never a third site.</para>
/// </summary>
public sealed class IoStatus07SetterDriftTests
{
    private static readonly string[] ScannedRoots =
    [
        TestRepo.Src("Cobol.Net.Runtime"),
        TestRepo.Src("Cobol.Net.Compiler", "CodeGen"),
    ];

    private static IEnumerable<(string File, SyntaxNode Root)> Sources() =>
        ScannedRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (File: Path.GetFileName(path),
                             Root: (SyntaxNode)CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(path)).GetRoot()));

    private static string EnclosingMember(SyntaxNode node) =>
        node.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault() switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            { } other => other.Kind().ToString(),
            null => node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault()?.Identifier.Text
                    ?? node.Ancestors().OfType<BaseTypeDeclarationSyntax>().First().Identifier.Text,
        };

    /// <summary>The named constant is read in exactly the two dispatches §9.1.13.2 item 6 names statements for, and in
    /// both — a setter that moved, was deleted or was added fails here with its location.</summary>
    [Fact]
    public void TheNamedConstant_IsUsedOnlyByTheCloseDispatchAndTheOpenNoRewindArm()
    {
        var sites = Sources()
            .SelectMany(s => s.Root.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.Text == nameof(FileStatusCode.PhraseOnNonReelMedium))
                .Select(id => $"{s.File}:{EnclosingMember(id)}"))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            ["FileRegistry.cs:CloseByFormat", "FileRegistry.cs:NoRewindPhraseEffect"],
            sites);
    }

    /// <summary>No spelling of the value outside the constant: a literal <c>"07"</c> assigned to a connector's status,
    /// or rendered into emitted C#, would be a setter the test above cannot see.</summary>
    [Fact]
    public void TheValue_IsNeverSpelledOutsideItsConstant()
    {
        var spellings = Sources()
            .SelectMany(s => s.Root.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.StringLiteralToken) && t.ValueText == FileStatusCode.PhraseOnNonReelMedium)
                .Select(t => $"{s.File}:{EnclosingMember(t.Parent!)}"))
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["FileStatus.cs:FileStatusCode"], spellings);
    }
}
