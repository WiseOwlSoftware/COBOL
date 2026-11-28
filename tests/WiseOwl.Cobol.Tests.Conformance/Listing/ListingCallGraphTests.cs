// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// ⛔ THE CALL GRAPH OF A PROGRAM THAT CALLS BY LITERAL (docs/rearchitecture/DESIGN-listing.md sections 6.7 and 6.7.1, "The program-name
/// literal"; kb/Work PB5925 step 7b review round 1, R1-2). At COBOL-85 every CALL and CANCEL names its program by literal-1 or by an
/// identifier (§1985:X.5.2.3 SR1, §1985:X.5.3.3 SR1: "Literal-1 must be a nonnumeric literal"), so a
/// literal target is the reference the graph is made of: the occurrence stands at the literal, of the entry of the program it names, the
/// contained SUB of this compilation here, and, where the group defines no such program, of the external program of that name. The
/// expected edges and positions are written from the program's own source lines below, at <c>--std 85</c>.
/// </summary>
public sealed class ListingCallGraphTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingcalls");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",                      // 1
        "       PROGRAM-ID. MAINP.",                            // 2
        "       DATA DIVISION.",                                // 3
        "       WORKING-STORAGE SECTION.",                      // 4
        "       01 X PIC 9 VALUE 1.",                           // 5
        "       PROCEDURE DIVISION.",                           // 6
        "       MAIN-P.",                                       // 7
        "           CALL \"SUB\" USING X.",                     // 8
        "           CALL \"EXTP\".",                            // 9
        "           CANCEL \"SUB\".",                           // 10
        "           STOP RUN.",                                 // 11
        "       IDENTIFICATION DIVISION.",                      // 12
        "       PROGRAM-ID. SUB.",                              // 13
        "       DATA DIVISION.",                                // 14
        "       LINKAGE SECTION.",                              // 15
        "       01 Y PIC 9.",                                   // 16
        "       PROCEDURE DIVISION USING Y.",                   // 17
        "       SUB-P.",                                        // 18
        "           EXIT PROGRAM.",                             // 19
        "       END PROGRAM SUB.",                              // 20
        "       END PROGRAM MAINP.",                            // 21
    ];

    private ListingCrossReference Compile()
    {
        string path = Path.Combine(_dir, "calls.cob");
        File.WriteAllText(path, string.Join("\n", Source));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, EditionInfo.Of(85), Path.Combine(_dir, "calls.dll"),
            Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json) { Sections = ListingSectionSet.All }));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        ListingJsonContract.AssertHolds(listing);
        return Assert.IsType<ListingCrossReference>(listing.CrossReference);
    }

    private static ListingSymbol Symbol(ListingCrossReference xref, string name) => Assert.Single(xref.Symbols, s => s.Name == name);

    /// <summary>The 1-based column the <paramref name="text"/> is written at on <paramref name="line"/>.</summary>
    private static int Column(int line, string text) => Source[line - 1].IndexOf(text, StringComparison.Ordinal) + 1;

    [Fact]
    public void ALiteralTarget_IsAnEdgeToTheProgramItNames_ContainedOrExternal()
    {
        var xref = Compile();
        int main = Symbol(xref, "MAINP").Id, sub = Symbol(xref, "SUB").Id, external = Symbol(xref, "EXTP").Id;
        Assert.Equal([(main, sub, 8, "CALL"), (main, external, 9, "CALL"), (main, sub, 10, "CANCEL")],
            xref.Calls.Select(e => (e.From ?? 0, e.To, e.Line ?? 0, e.Verb)));
        // The two literals, and the END PROGRAM header's name, which references the program it ends (§2023:8.4.6.3: a program-name may be referenced by the end program marker): no verb, no edge.
        Assert.Equal([(8, Column(8, "\"SUB\""), 5, "CALL"), (10, Column(10, "\"SUB\""), 5, "CANCEL"), (20, Column(20, "SUB"), 3, "")],
            Symbol(xref, "SUB").References.Where(r => r.Use != ListingUse.Defined).Select(r => (r.Line ?? 0, r.Column ?? 0, r.Length, r.Verb ?? "")));
        Assert.False(Symbol(xref, "SUB").Unreferenced);   // the contained program is called, so it is no unreferenced definition
        // A program the group defines nowhere is the external program of that name: its kind says so, it has no definition, and it is no
        // "defined and never referenced" symbol.
        var ext = Symbol(xref, "EXTP");
        Assert.Equal(ListingSymbolKind.ExternalProgram, ext.Kind);
        Assert.DoesNotContain(ext.References, r => r.Use == ListingUse.Defined);
        Assert.Equal([(9, Column(9, "\"EXTP\""), 6, "CALL")], ext.References.Select(r => (r.Line ?? 0, r.Column ?? 0, r.Length, r.Verb ?? "")));
        Assert.False(ext.Unreferenced);
    }

    /// <summary>The source of <see cref="EveryArmOfTheProgramNameScope_IsTheEdgeTheRunTimeTakes"/> at <c>--std 85</c>: OUTER contains the
    /// COMMON program C and the program D; OUTER2 is a second outermost program of the group.</summary>
    private static readonly string[] ScopeSource =
    [
        "       IDENTIFICATION DIVISION.",                      // 1
        "       PROGRAM-ID. OUTER.",                            // 2
        "       PROCEDURE DIVISION.",                           // 3
        "       OUTER-P.",                                      // 4
        "           CALL \"D\".",                               // 5   arm 1: D is directly contained in OUTER
        "           STOP RUN.",                                 // 6
        "       IDENTIFICATION DIVISION.",                      // 7
        "       PROGRAM-ID. C IS COMMON PROGRAM.",              // 8
        "       PROCEDURE DIVISION.",                           // 9
        "       C-P.",                                          // 10
        "           CALL \"C\".",                               // 11  the COMMON exception: C, not recursive, does not reach itself
        "           EXIT PROGRAM.",                             // 12
        "       END PROGRAM C.",                                // 13
        "       IDENTIFICATION DIVISION.",                      // 14
        "       PROGRAM-ID. D.",                                // 15
        "       PROCEDURE DIVISION.",                           // 16
        "       D-P.",                                          // 17
        "           CALL \"C\".",                               // 18  arm 3: C is COMMON and directly contained in D's container
        "           CALL \"OUTER2\".",                          // 19  arm 4: an outermost program of the group
        "           EXIT PROGRAM.",                             // 20
        "       END PROGRAM D.",                                // 21
        "       END PROGRAM OUTER.",                            // 22
        "       IDENTIFICATION DIVISION.",                      // 23
        "       PROGRAM-ID. OUTER2.",                           // 24
        "       PROCEDURE DIVISION.",                           // 25
        "       O2-P.",                                         // 26
        "           CALL \"D\".",                               // 27  D is contained in OUTER and not COMMON: no arm reaches it
        "           EXIT PROGRAM.",                             // 28
        "       END PROGRAM OUTER2.",                           // 29
    ];

    private ListingCrossReference CompileScope(string[] source, int dialect)
    {
        string path = Path.Combine(_dir, $"scope{dialect}.cob");
        File.WriteAllText(path, string.Join("\n", source));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, EditionInfo.Of(dialect), Path.Combine(_dir, $"scope{dialect}.dll"),
            Listing: new ListingRequest(Formats: ListingFormats.Json) { Sections = ListingSectionSet.All }));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        ListingJsonContract.AssertHolds(listing);
        return Assert.IsType<ListingCrossReference>(listing.CrossReference);
    }

    private static int ProgramId(ListingCrossReference xref, string name) =>
        Assert.Single(xref.Symbols, s => s.Name == name && s.Kind != ListingSymbolKind.ExternalProgram).Id;

    private static int ExternalId(ListingCrossReference xref, string name) =>
        Assert.Single(xref.Symbols, s => s.Name == name && s.Kind == ListingSymbolKind.ExternalProgram).Id;

    /// <summary>⛔ kb/Work PB5965: the CALL graph's edges follow the ONE scope rule (<c>ProgramNameScope.FirstVisible</c>) the run time
    /// resolves by, arm by arm (§1985:X.1.3.8.1 SR2, §1985:II.6.4.1.2 SR3; §2023:8.4.6.3): a contained program (arm 1), a COMMON program
    /// of a container (arm 3) under its exception (a COMMON program that is not recursive does not reach its own name), an outermost
    /// program of the group (arm 4), and a contained program that is not COMMON, which another outermost program does not reach. A
    /// literal no arm reaches names the external program of that name. Each edge below fails if its arm is dropped or reordered.</summary>
    [Fact]
    public void EveryArmOfTheProgramNameScope_IsTheEdgeTheRunTimeTakes()
    {
        var xref = CompileScope(ScopeSource, 85);
        int outer = ProgramId(xref, "OUTER"), c = ProgramId(xref, "C"), d = ProgramId(xref, "D"), outer2 = ProgramId(xref, "OUTER2");
        Assert.Equal(
            [(outer, d, 5), (c, ExternalId(xref, "C"), 11), (d, c, 18), (d, outer2, 19), (outer2, ExternalId(xref, "D"), 27)],
            xref.Calls.Select(e => (e.From ?? 0, e.To, e.Line ?? 0)));
    }

    /// <summary>Arm 2, from COBOL-2002, which has the RECURSIVE attribute: a recursive program reaches its own name, and a COMMON program
    /// that is also recursive is reachable from within itself (§2023:8.4.6.3 SR2: "may reference the program-name only if the program
    /// possesses the recursive attribute").</summary>
    [Fact]
    public void ARecursiveProgram_ReachesItsOwnName()
    {
        string[] source =
        [
            "       IDENTIFICATION DIVISION.",                  // 1
            "       PROGRAM-ID. OUTER3.",                       // 2
            "       PROCEDURE DIVISION.",                       // 3
            "           CALL \"R\".",                           // 4  arm 1
            "           STOP RUN.",                             // 5
            "       IDENTIFICATION DIVISION.",                  // 6
            "       PROGRAM-ID. R IS RECURSIVE.",               // 7
            "       PROCEDURE DIVISION.",                       // 8
            "           CALL \"R\".",                           // 9  arm 2: R is recursive (and not COMMON, so arm 3 cannot reach it)
            "           CALL \"CR\".",                          // 10 arm 3
            "           EXIT PROGRAM.",                         // 11
            "       END PROGRAM R.",                            // 12
            "       IDENTIFICATION DIVISION.",                  // 13
            "       PROGRAM-ID. CR IS COMMON RECURSIVE.",       // 14
            "       PROCEDURE DIVISION.",                       // 15
            "           CALL \"CR\".",                          // 16 arm 2, and arm 3's exception lifted by RECURSIVE
            "           EXIT PROGRAM.",                         // 17
            "       END PROGRAM CR.",                           // 18
            "       END PROGRAM OUTER3.",                       // 19
        ];
        var xref = CompileScope(source, 2023);
        int outer = ProgramId(xref, "OUTER3"), r = ProgramId(xref, "R"), cr = ProgramId(xref, "CR");
        Assert.Equal([(outer, r, 4), (r, r, 9), (r, cr, 10), (cr, cr, 16)], xref.Calls.Select(e => (e.From ?? 0, e.To, e.Line ?? 0)));
    }

    /// <summary>⛔ kb/Work PB5963: a method or factory method contains no program and is contained in none, so the one arm of the scope of
    /// a program-name that reaches from its body is arm 4 (§2023:8.4.6.3 SR3: "that program-name may be referenced by statements included in
    /// any source element in the run unit"). Its CALL and CANCEL literals are edges to the group's outermost program, from the instance
    /// method's binder and from the factory method's alike; a contained program that is not COMMON is reached by no arm, so its name is
    /// the external program of that name. Both binders were built without the scope, and every literal of a method was an external program.</summary>
    [Fact]
    public void AMethodsLiteral_ReachesAnOutermostProgramOfTheGroup()
    {
        string[] source =
        [
            "       IDENTIFICATION DIVISION.",                  // 1
            "       PROGRAM-ID. MP.",                           // 2
            "       PROCEDURE DIVISION.",                       // 3
            "           STOP RUN.",                             // 4
            "       IDENTIFICATION DIVISION.",                  // 5
            "       PROGRAM-ID. INNER.",                        // 6
            "       PROCEDURE DIVISION.",                       // 7
            "           EXIT PROGRAM.",                         // 8
            "       END PROGRAM INNER.",                        // 9
            "       END PROGRAM MP.",                           // 10
            "       IDENTIFICATION DIVISION.",                  // 11
            "       CLASS-ID. OOM INHERITS FROM BASE.",         // 12
            "       ENVIRONMENT DIVISION.",                     // 13
            "       CONFIGURATION SECTION.",                    // 14
            "       REPOSITORY.",                               // 15
            "           CLASS BASE.",                           // 16
            "       IDENTIFICATION DIVISION.",                  // 17
            "       FACTORY.",                                  // 18
            "       PROCEDURE DIVISION.",                       // 19
            "       IDENTIFICATION DIVISION.",                  // 20
            "       METHOD-ID. FM.",                            // 21
            "       PROCEDURE DIVISION.",                       // 22
            "           CANCEL \"MP\".",                        // 23 arm 4 from the factory method's binder
            "       END METHOD FM.",                            // 24
            "       END FACTORY.",                              // 25
            "       IDENTIFICATION DIVISION.",                  // 26
            "       OBJECT.",                                   // 27
            "       PROCEDURE DIVISION.",                       // 28
            "       IDENTIFICATION DIVISION.",                  // 29
            "       METHOD-ID. M1.",                            // 30
            "       PROCEDURE DIVISION.",                       // 31
            "           CALL \"MP\".",                          // 32 arm 4 from the instance method's binder
            "           CALL \"INNER\".",                       // 33 INNER is contained in MP and not COMMON: no arm reaches it
            "       END METHOD M1.",                            // 34
            "       END OBJECT.",                               // 35
            "       END CLASS OOM.",                            // 36
        ];
        var xref = CompileScope(source, 2023);
        int mp = ProgramId(xref, "MP");
        Assert.Equal([(mp, 23, "CANCEL"), (mp, 32, "CALL"), (ExternalId(xref, "INNER"), 33, "CALL")],
            xref.Calls.Select(e => (e.To, e.Line ?? 0, e.Verb)));
        Assert.DoesNotContain(xref.Symbols, s => s.Name == "MP" && s.Kind == ListingSymbolKind.ExternalProgram);
    }

    [Fact]
    public void TheTextCrossReference_PrintsTheCallGraphRows()
    {
        Compile();
        string[] lines = File.ReadAllLines(Path.Combine(_dir, "calls.lst"));
        int start = Array.FindIndex(lines, l => l.TrimEnd() == "CALL GRAPH");
        Assert.True(start >= 0, "the CALL GRAPH heading is printed");
        var rows = lines[(start + 2)..].Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Where(p => p.Length >= 4 && p[2] is "CALL" or "CANCEL").ToList();
        Assert.Equal(["MAINP 8 CALL SUB", "MAINP 9 CALL EXTP", "MAINP 10 CANCEL SUB"], rows.Select(p => string.Join(' ', p[0], p[1], p[2], p[3])));
    }
}
