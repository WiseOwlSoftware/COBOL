// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WiseOwl.Cobol;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Building;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ A BACKEND DIAGNOSTIC ON A STATEMENT'S LINE IS PLACED ON ITS LISTING LINE (DESIGN-listing.md section 6.9; kb/Work PB5925 review R1-3).
/// The Roslyn backend's line directives name each COBOL file relative to the document root, the main source's directory
/// (<see cref="SourceDocuments.NameOf"/>); a Roslyn diagnostic on such a line crosses the seam naming the file by its full path
/// (<see cref="SourceDocuments.PathOf"/>), and the listing finds that file whichever name the compilation read it by. Held here on a
/// source compiled by its full path, copying a library text from a sibling directory (a document name with <c>..</c> in it): the Roslyn
/// diagnostics are Roslyn's own, from C# whose directives the one decider names, and the listing is the one builder's.
/// </summary>
public sealed class BackendDiagnosticPlacementTests : WiseOwlCobolTestBase
{
    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",          // 1
        "       PROGRAM-ID. PLACEMENT.",            // 2
        "       DATA DIVISION.",                    // 3
        "       WORKING-STORAGE SECTION.",          // 4
        "       01 B PIC 9(4).",                    // 5
        "       PROCEDURE DIVISION.",               // 6
        "       P1.",                               // 7
        "           MOVE 1 TO B.",                  // 8: MOVE at 12
        "           COPY PLACECPY.",                // 9
        "           STOP RUN.",                     // 10
    ];

    [Fact]
    public void ABackendDiagnostic_OnAMappedLine_IsPlacedOnItsListingLine_AndNamesTheFileByItsFullPath()
    {
        string work = Path.Combine(TempDir, "w"), library = Path.Combine(TempDir, "lib");
        Directory.CreateDirectory(work);
        Directory.CreateDirectory(library);
        string source = Path.Combine(work, "placement.cob"), copybook = Path.Combine(library, "PLACECPY.cpy");
        File.WriteAllText(source, string.Join("\n", Source));
        File.WriteAllText(copybook, "           ADD 2 TO B.\n");   // its line 1: ADD at 12

        var request = new ListingRequest(Formats: ListingFormats.Text) { Sections = ListingSectionSet.Of(ListingSection.Source, ListingSection.Diagnostics) };
        var options = new CompilerDriver.Options(source, Path.Combine(work, "placement.dll"), CopyPaths: [library], Listing: request);
        var inputs = new CompilationInputs { DigestsFiles = true };
        var facts = new ListingFacts(request, inputs, options.SourcePath, options.OutputDll(),
            Editions.EditionInfo.Of(options.DialectLevel, options.Permissive), () => DateTimeOffset.UnixEpoch, []);
        var frontend = CompilerDriver.NewFrontend(options, inputs, recordsListing: true);
        Assert.NotNull(frontend.Parse(source, new DiagnosticBag()));
        facts.DepositFrontend(frontend);

        // C# with an error on a line mapped to each file, its directive naming the file as the backend does.
        string root = SourceDocuments.RootOf(source);
        Assert.Equal("../lib/PLACECPY.cpy", SourceDocuments.NameOf(copybook, root));
        string csharp = "class C\n{\nstatic void M()\n{\n"
            + $"#line (8, 12) - (8, 22) 1 \"{SourceDocuments.NameOf(source, root)}\"\nint x = unknownA;\n"
            + $"#line (1, 12) - (1, 21) 1 \"{SourceDocuments.NameOf(copybook, root)}\"\nint y = unknownB;\n"
            + "#line hidden\n}\n}\n";
        var roslyn = CSharpCompilation.Create("probe", [CSharpSyntaxTree.ParseText(csharp)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).GetDiagnostics();
        var neutral = RoslynBackend.Neutral(roslyn, root).Where(d => d.Code == "CS0103").ToList();
        Assert.Equal(2, neutral.Count);
        Assert.Equal([Path.GetFullPath(source), Path.GetFullPath(copybook)], neutral.Select(d => d.Location!.Value.FileName));
        Assert.Equal([7, 0], neutral.Select(d => d.Location!.Value.Line));   // 0-based: the MOVE's line 8, the copybook's line 1

        facts.DepositDiagnostics(neutral);
        var listing = ListingBuilder.Build(facts, ListingOutcome.BackendError);
        var placed = listing.Diagnostics.Where(d => d.Code == "CS0103").ToList();
        Assert.Equal(2, placed.Count);
        int Line(int file, int fileLine) => Assert.Single(listing.Lines, l => l.File == file && l.FileLine == fileLine).Number;
        int copyFile = Assert.Single(listing.Files, f => f.Role == ListingFileRole.Library).Id;
        Assert.Equal([Line(1, 8), Line(copyFile, 1)], placed.Select(d => d.Line));
        Assert.Equal([1, copyFile], placed.Select(d => d.File));
        Assert.Equal([8, 1], placed.Select(d => d.FileLine));
    }
}
