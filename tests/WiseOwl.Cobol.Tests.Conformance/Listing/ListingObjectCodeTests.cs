// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The COBOL to object-code map (DESIGN-listing.md section 6.9; step 8 of section 11) on a real compilation. The expected positions are
/// where the test's own source writes each statement; the expected methods and IL ranges are what the assembly and its PDB say, read here
/// independently with <c>System.Reflection.Metadata</c> (each visible sequence point's method, offset and the next one's); the expected
/// instructions are the method body's bytes at those offsets. Nothing is read off a listing to be compared with itself.
/// </summary>
public sealed class ListingObjectCodeTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingobject");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    /// <summary>Statements on their own lines, two on one line, a statement nested in an IF, an inline PERFORM whose step and test follow its
    /// body, and a copybook copied twice whose statement is two statements at one file position.</summary>
    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",                         // 1
        "       PROGRAM-ID. OBJMAP.",                              // 2
        "       DATA DIVISION.",                                   // 3
        "       WORKING-STORAGE SECTION.",                         // 4
        "       01 A PIC 9(4) VALUE 1.",                           // 5
        "       01 B PIC 9(4).",                                   // 6
        "       PROCEDURE DIVISION.",                              // 7
        "       P1.",                                              // 8
        "           MOVE A TO B  ADD 1 TO B",                      // 9: MOVE at 12, ADD at 25
        "           IF B > 1",                                     // 10
        "              DISPLAY \"BIG \" B",                        // 11: DISPLAY at 15
        "           END-IF",                                       // 12
        "           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3",    // 13
        "              ADD A TO B",                                // 14: ADD at 15
        "           END-PERFORM",                                  // 15
        "           COPY OBJCPY.",                                 // 16
        "           COPY OBJCPY.",                                 // 17
        "           STOP RUN.",                                    // 18
    ];

    private const string Copybook = "           ADD 2 TO B.\n";   // its line 1: ADD at 12

    /// <summary>Each statement the source writes: its file line and column.</summary>
    private static readonly (int Line, int Column)[] Statements = [(9, 12), (9, 25), (10, 12), (11, 15), (13, 12), (14, 15), (18, 12)];

    private CompilerDriver.Result Compile(ListingSectionSet sections, bool checkOnly = false, IEnumerable<string>? source = null)
    {
        string path = Path.Combine(_dir, "objmap.cob");
        File.WriteAllText(path, string.Join("\n", source ?? Source));
        File.WriteAllText(Path.Combine(_dir, "OBJCPY.cpy"), Copybook);
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, EditionInfo.Latest, Path.Combine(_dir, "objmap.dll"),
            CopyPaths: [_dir], CheckOnly: checkOnly,
            Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json) { Sections = sections }));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return result;
    }

    /// <summary>The assembly's own account: each visible sequence point (file, line, column), its method's token and the IL range to the
    /// method's next visible point, and the body's bytes. The PDB names each document relative to the main source's directory.</summary>
    private List<(string File, int Line, int Column, int Token, int Start, int End, byte[] Il)> Points(string dll)
    {
        using var pe = new PEReader(File.OpenRead(dll));
        var metadata = pe.GetMetadataReader();
        using var pdbStream = File.OpenRead(Path.ChangeExtension(dll, ".pdb"));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var pdb = provider.GetMetadataReader();
        var points = new List<(string, int, int, int, int, int, byte[])>();
        foreach (var handle in metadata.MethodDefinitions)
        {
            var definition = metadata.GetMethodDefinition(handle);
            if (definition.RelativeVirtualAddress == 0) continue;
            byte[] il = pe.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes()!;
            var visible = pdb.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(handle)))
                .GetSequencePoints().Where(p => !p.IsHidden).OrderBy(p => p.Offset).ToList();
            for (int i = 0; i < visible.Count; i++)
                points.Add((Path.GetFullPath(pdb.GetString(pdb.GetDocument(visible[i].Document).Name), _dir), visible[i].StartLine,
                    visible[i].StartColumn, MetadataTokens.GetToken(handle), visible[i].Offset,
                    i + 1 < visible.Count ? visible[i + 1].Offset : il.Length, il));
        }
        return points;
    }

    private static List<string> DocumentNames(string dll)
    {
        using var pdbStream = File.OpenRead(Path.ChangeExtension(dll, ".pdb"));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var pdb = provider.GetMetadataReader();
        return [.. pdb.Documents.Select(d => pdb.GetString(pdb.GetDocument(d).Name)).Order(StringComparer.Ordinal)];
    }

    [Fact]
    public void EachStatement_IsPlacedAtItsLine_WithTheMethodAndRangeThePdbHolds()
    {
        var result = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.ObjectMap));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = result.Listing!;
        Assert.Contains(new ListingSectionStatus(ListingSection.ObjectMap, true, null), listing.Sections);
        var map = Assert.IsType<ListingObjectCodeMap>(listing.ObjectCode);
        Assert.Equal(Path.Combine(_dir, "objmap.dll"), map.Assembly);
        Assert.All(map.Ranges, r => Assert.Null(r.Instructions));   // no object-code section asked: no instructions

        string source = Path.GetFullPath(Path.Combine(_dir, "objmap.cob"));
        var points = Points(map.Assembly);
        var methods = map.Methods.ToDictionary(m => m.Id);
        int ListingLine(int fileLine) => Assert.Single(listing.Lines, l => l.File == 1 && l.FileLine == fileLine).Number;

        // Every statement of the main source: one first range at its line and column, equal to the PDB's first point there.
        foreach (var (line, column) in Statements)
        {
            var expected = points.Where(p => p.File == source && p.Line == line && p.Column == column).ToList();
            Assert.NotEmpty(expected);
            var ranges = map.Ranges.Where(r => r.Line == ListingLine(line) && r.Column == column).ToList();
            Assert.Equal(expected.Select(p => (p.Token, p.Start, p.End)),
                ranges.Select(r => (methods[r.Method].Token, r.Start, r.End)));
        }
        // The PERFORM's step and test, after its body, are its own: a second range at its line, after the ADD it encloses.
        var perform = map.Ranges.Where(r => r.Line == ListingLine(13)).ToList();
        Assert.Equal(2, perform.Count);
        var nestedAdd = Assert.Single(map.Ranges, r => r.Line == ListingLine(14));
        Assert.True(perform[0].End <= nestedAdd.Start && nestedAdd.End <= perform[1].Start);
        // The PDB names the source relative to its own directory, so the assembly is the same wherever the tree lies.
        Assert.Equal(["OBJCPY.cpy", "objmap.cob"], DocumentNames(map.Assembly));
        // Nothing else of the main source has a range: the listing places no point the PDB does not hold.
        Assert.Equal(points.Count(p => p.File == source), map.Ranges.Count(r => listing.Lines[r.Line - 1].File == 1));
    }

    /// <summary>An EVALUATE's second WHEN is tested after the first WHEN's statements (kb/Work PB5925 review R1-1): that test is the
    /// EVALUATE's code, a range of its own at its line between the two nested MOVEs, never part of the first MOVE's range. The third WHEN
    /// (OTHER) tests nothing, so the EVALUATE has two ranges.</summary>
    [Fact]
    public void AnEvaluatesLaterWhenTest_IsTheEvaluatesOwnRange()
    {
        var listing = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.ObjectMap), source:
        [
            .. Source.Take(8),
            "           EVALUATE A",            // 9
            "             WHEN 1",              // 10
            "               MOVE 1 TO B",       // 11
            "             WHEN 2",              // 12
            "               MOVE 2 TO B",       // 13
            "             WHEN OTHER",          // 14
            "               MOVE 3 TO B",       // 15
            "           END-EVALUATE",          // 16
            "           STOP RUN.",             // 17
        ]).Listing!;
        var map = listing.ObjectCode!;
        int ListingLine(int fileLine) => Assert.Single(listing.Lines, l => l.File == 1 && l.FileLine == fileLine).Number;
        var evaluate = map.Ranges.Where(r => r.Line == ListingLine(9)).ToList();
        Assert.Equal(2, evaluate.Count);
        Assert.All(evaluate, r => Assert.Equal(12, r.Column));
        var first = Assert.Single(map.Ranges, r => r.Line == ListingLine(11));
        var second = Assert.Single(map.Ranges, r => r.Line == ListingLine(13));
        Assert.True(evaluate[0].End <= first.Start && first.End <= evaluate[1].Start && evaluate[1].End <= second.Start,
            $"EVALUATE {evaluate[0].Start}-{evaluate[0].End}, {evaluate[1].Start}-{evaluate[1].End}; MOVE 1 {first.Start}-{first.End}; "
            + $"MOVE 2 {second.Start}-{second.End}");
        Assert.Equal(evaluate.Count, Points(map.Assembly).Count(p => p.Line == 9 && p.Column == 12));
    }

    /// <summary>A statement whose code begins with storing its flag (an ADD ON SIZE ERROR's size-error flag, a CALL ON EXCEPTION's
    /// exception flag; kb/Work PB5925 review R2-1) owns that store: the range of the statement before it ends where the statement's first
    /// range begins, and holds no store to a local (a MOVE to a data item and a DISPLAY of a literal store to none). The ADD follows an
    /// EVALUATE's last WHEN; the CALL follows a STRING whose ON OVERFLOW holds a DISPLAY.</summary>
    [Fact]
    public void AStatementsFlagStore_IsInItsOwnRange_NotThePreviousStatements()
    {
        var listing = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.ObjectCode), source:
        [
            .. Source.Take(5),
            "       01 S PIC X(5).",                                         // 6
            "       01 B PIC 9(4).",                                         // 7
            "       PROCEDURE DIVISION.",                                    // 8
            "       P1.",                                                    // 9
            "           EVALUATE A",                                         // 10
            "             WHEN 1 MOVE 1 TO B",                               // 11
            "             WHEN OTHER MOVE 3 TO B",                           // 12: MOVE at 25
            "           END-EVALUATE",                                       // 13
            "           ADD 9999 TO B ON SIZE ERROR DISPLAY \"SE\" END-ADD",  // 14
            "           STRING \"ABCDEFG\" DELIMITED BY SIZE INTO S",        // 15
            "               ON OVERFLOW DISPLAY \"OV\" END-STRING",          // 16: DISPLAY at 28
            "           CALL \"NOSUCH\" ON EXCEPTION DISPLAY \"EXC\" END-CALL", // 17
            "           STOP RUN.",                                          // 18
        ]).Listing!;
        var map = listing.ObjectCode!;
        int ListingLine(int fileLine) => Assert.Single(listing.Lines, l => l.File == 1 && l.FileLine == fileLine).Number;
        var move = Assert.Single(map.Ranges, r => r.Line == ListingLine(12) && r.Column == 25);
        var add = map.Ranges.Where(r => r.Line == ListingLine(14) && r.Column == 12).OrderBy(r => r.Start).First();
        var display = Assert.Single(map.Ranges, r => r.Line == ListingLine(16) && r.Column == 28);
        var call = map.Ranges.Where(r => r.Line == ListingLine(17) && r.Column == 12).OrderBy(r => r.Start).First();
        Assert.Equal((move.Method, move.End), (add.Method, add.Start));
        Assert.Equal((display.Method, display.End), (call.Method, call.Start));
        Assert.All(new[] { move, display }, r => Assert.DoesNotContain(r.Instructions!, i => i.OpCode.StartsWith("stloc", StringComparison.Ordinal)));
    }

    /// <summary>A copybook copied twice: its one statement is two statements at one file position, and each copy's range is placed in its
    /// own block, the first copy's code before the second's (DESIGN-listing.md section 6.9).</summary>
    [Fact]
    public void ACopybookCopiedTwice_PlacesEachCopysCode_InItsOwnBlock()
    {
        var listing = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.ObjectMap)).Listing!;
        var map = listing.ObjectCode!;
        string copybook = Path.GetFullPath(Path.Combine(_dir, "OBJCPY.cpy"));
        var expected = Points(map.Assembly).Where(p => p.File == copybook && p.Line == 1 && p.Column == 12).OrderBy(p => p.Start).ToList();
        Assert.Equal(2, expected.Count);
        var blocks = listing.Inclusions.Select(i => Assert.Single(listing.Lines, l => l.Inclusion == i.Id && l.FileLine == 1).Number).ToList();
        Assert.Equal(2, blocks.Count);
        var ranges = blocks.Select(b => Assert.Single(map.Ranges, r => r.Line == b)).ToList();
        Assert.Equal(expected.Select(p => (p.Start, p.End)), ranges.Select(r => (r.Start, r.End)));
        Assert.All(ranges, r => Assert.Equal(12, r.Column));
    }

    /// <summary>With the object-code section, each range holds its instructions: they begin at its start and lie within it, and the
    /// first instruction's opcode is the body's byte at that offset; the text prints them under each statement.</summary>
    [Fact]
    public void TheObjectCode_ListsEachRangesInstructions()
    {
        var result = Compile(ListingSectionSet.Of(ListingSection.Source, ListingSection.ObjectCode));
        var map = result.Listing!.ObjectCode!;
        Assert.DoesNotContain(result.Listing.Sections, s => s.Section == ListingSection.ObjectMap);
        var bodies = Points(map.Assembly).ToDictionary(p => (p.Token, p.Start), p => p.Il);
        var methods = map.Methods.ToDictionary(m => m.Id);
        foreach (var range in map.Ranges)
        {
            var instructions = Assert.IsAssignableFrom<IReadOnlyList<ListingInstruction>>(range.Instructions);
            Assert.NotEmpty(instructions);
            Assert.Equal(range.Start, instructions[0].Offset);
            Assert.All(instructions, i => Assert.InRange(i.Offset, range.Start, range.End - 1));
            byte first = bodies[(methods[range.Method].Token, range.Start)][range.Start];
            Assert.Equal(first == 0xFE, instructions[0].OpCode is "ceq" or "cgt" or "clt" or "cgt.un" or "clt.un" or "ldftn" or "ldvirtftn"
                or "ldarg" or "ldarga" or "starg" or "ldloc" or "ldloca" or "stloc" or "localloc" or "endfilter" or "unaligned." or "volatile."
                or "tail." or "initobj" or "constrained." or "cpblk" or "initblk" or "rethrow" or "sizeof" or "refanytype" or "readonly.");
        }
        string text = File.ReadAllText(Path.Combine(_dir, "objmap.lst"));
        Assert.Contains("OBJECT CODE", text, StringComparison.Ordinal);
        Assert.Contains(ObjectCodeText.Offset(map.Ranges[0].Start) + "  " + map.Ranges[0].Instructions![0].OpCode, text, StringComparison.Ordinal);
    }

    /// <summary>A compilation that wrote no assembly has no object code, and the section says why (DESIGN-listing.md section 4.3).</summary>
    [Fact]
    public void NoAssembly_NoObjectCode_AndTheSectionSaysWhy()
    {
        var sections = ListingSectionSet.Of(ListingSection.ObjectMap, ListingSection.ObjectCode);
        var checkOnly = Compile(sections, checkOnly: true).Listing!;
        Assert.Null(checkOnly.ObjectCode);
        Assert.Equal([new ListingSectionStatus(ListingSection.ObjectMap, false, "check-only compile: no code generated"),
                      new ListingSectionStatus(ListingSection.ObjectCode, false, "check-only compile: no code generated")], checkOnly.Sections);
        var refused = Compile(sections, source: [.. Source.Take(8), "           MOVE NOSUCH TO B.", "           STOP RUN."]).Listing!;
        Assert.Null(refused.ObjectCode);
        Assert.All(refused.Sections, s => Assert.Equal("the bind refused the program", s.Reason));
    }

    /// <summary>The object code is read only when a section asks for it: the standard listing holds none.</summary>
    [Fact]
    public void TheStandardListing_HoldsNoObjectCode()
    {
        var result = Compile(ListingSectionSet.Standard);
        Assert.True(result.Success);
        Assert.Null(result.Listing!.ObjectCode);
        Assert.True(File.Exists(Path.Combine(_dir, "objmap.pdb")));   // the PDB is written all the same: it is the debugger's
    }
}
