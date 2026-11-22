// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The exception-handling regions of the object code (DESIGN-listing.md section 6.9, kb/Work PB5951) on a real compilation. The
/// program's statements compile to each kind the compiler writes: an OPEN's mode scope is a <c>finally</c>; an ADD ON SIZE ERROR's
/// size check is two protected blocks, one inside the other, each with two <c>catch</c> clauses; and a reference modification under
/// <c>&gt;&gt;TURN EC-BOUND-REF-MOD CHECKING ON</c> (§2023:7.3.25.1: the directive "is used to turn checking for specified exception
/// conditions on or off") is a <c>filter</c> nested in a <c>finally</c>. The EXPECTED clauses are read here from the assembly with
/// <c>System.Reflection.Metadata</c> (each body's <c>ExceptionRegions</c>, ECMA-335 II.25.4.6), and the expected lines' depths from the
/// blocks' containment (ECMA-335 I.12.4.2.7); nothing is read off a listing to be compared with itself.
/// </summary>
public sealed class ListingExceptionRegionTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingregions");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private static readonly string[] Source =
    [
        "       IDENTIFICATION DIVISION.",
        "       PROGRAM-ID. REGIONS.",
        "       ENVIRONMENT DIVISION.",
        "       INPUT-OUTPUT SECTION.",
        "       FILE-CONTROL.",
        "           SELECT OUT-FILE ASSIGN TO \"regions.dat\".",
        "       DATA DIVISION.",
        "       FILE SECTION.",
        "       FD OUT-FILE.",
        "       01 OUT-REC PIC X(10).",
        "       WORKING-STORAGE SECTION.",
        "       01 N PIC 9(4) VALUE 1.",
        "       01 X PIC X(4) VALUE \"ABCD\".",
        "       01 Y PIC X.",
        "       PROCEDURE DIVISION.",
        "       P1.",
        "           OPEN OUTPUT OUT-FILE",
        "           ADD 1 TO N ON SIZE ERROR DISPLAY \"SIZE\" END-ADD",
        "           CLOSE OUT-FILE",
        "       >>TURN EC-BOUND-REF-MOD CHECKING ON",
        "           MOVE X(N:1) TO Y",
        "           STOP RUN.",
    ];

    /// <summary>A clause as the test reads it from the body: the model's shape, the catch type named from the metadata here.</summary>
    private static ListingExceptionRegion Clause(MetadataReader metadata, ExceptionRegion region) => new(
        Enum.Parse<ListingRegionKind>(region.Kind.ToString()),
        region.TryOffset, region.TryOffset + region.TryLength, region.HandlerOffset, region.HandlerOffset + region.HandlerLength,
        region.Kind == ExceptionRegionKind.Filter ? region.FilterOffset : null,
        region.Kind == ExceptionRegionKind.Catch ? TypeName(metadata, region.CatchType) : null);

    /// <summary>A catch type's full name: its namespace and name, a nested type after its enclosing type and a '/'.</summary>
    private static string TypeName(MetadataReader metadata, EntityHandle handle)
    {
        if (handle.Kind == HandleKind.TypeReference)
        {
            var reference = metadata.GetTypeReference((TypeReferenceHandle)handle);
            string name = metadata.GetString(reference.Name);
            if (reference.ResolutionScope.Kind == HandleKind.TypeReference) return TypeName(metadata, (TypeReferenceHandle)reference.ResolutionScope) + "/" + name;
            string ns = metadata.GetString(reference.Namespace);
            return ns.Length == 0 ? name : ns + "." + name;
        }
        var definition = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
        string own = metadata.GetString(definition.Name);
        if (!definition.GetDeclaringType().IsNil) return TypeName(metadata, definition.GetDeclaringType()) + "/" + own;
        string space = metadata.GetString(definition.Namespace);
        return space.Length == 0 ? own : space + "." + own;
    }

    /// <summary>Each method's clauses, by metadata token, read from the assembly.</summary>
    private static Dictionary<int, List<ListingExceptionRegion>> ClausesOf(string dll)
    {
        using var pe = new PEReader(File.OpenRead(dll));
        var metadata = pe.GetMetadataReader();
        var clauses = new Dictionary<int, List<ListingExceptionRegion>>();
        foreach (var handle in metadata.MethodDefinitions)
        {
            var definition = metadata.GetMethodDefinition(handle);
            if (definition.RelativeVirtualAddress == 0) continue;
            clauses[MetadataTokens.GetToken(handle)] =
                [.. pe.GetMethodBody(definition.RelativeVirtualAddress).ExceptionRegions.Select(r => Clause(metadata, r))];
        }
        return clauses;
    }

    /// <summary>The lines a body's clauses must print, each block's opening and closing line with its depth: a protected block per
    /// distinct try range (named by the first clause protecting it), each filter block, each handler; the depth is the number of the
    /// body's other blocks that contain the block (ECMA-335 I.12.4.2.7: two blocks are disjoint or nested).</summary>
    private static List<ListingRegionMark> ExpectedLines(IReadOnlyList<ListingExceptionRegion> clauses)
    {
        var blocks = Blocks(clauses);
        var lines = new List<ListingRegionMark>();
        foreach (var b in blocks)
        {
            int depth = blocks.Count(o => o != b && o.Start <= b.Start && b.End <= o.End);
            lines.Add(new ListingRegionMark(b.Start, b.Open, b.Region, depth));
            lines.Add(new ListingRegionMark(b.End, b.Close, b.Region, depth));
        }
        return lines;
    }

    /// <summary>Whether <paramref name="stretch"/> holds the code <paramref name="line"/> bounds: the instruction at its offset for a
    /// line that opens a block, the instruction before its offset (the block's last) for one that closes a block.</summary>
    private static bool Owns(ListingObjectRange stretch, ListingRegionMark line) => ListingRegionBoundaries.Opens(line.Boundary)
        ? stretch.Start <= line.Offset && line.Offset < stretch.End
        : stretch.Start < line.Offset && line.Offset <= stretch.End;

    /// <summary>A body's blocks: a protected block per distinct try range (named by the first clause protecting it), each filter
    /// block, each handler.</summary>
    private static List<(int Start, int End, ListingRegionBoundary Open, ListingRegionBoundary Close, int Region)> Blocks(
        IReadOnlyList<ListingExceptionRegion> clauses)
    {
        var blocks = new List<(int Start, int End, ListingRegionBoundary Open, ListingRegionBoundary Close, int Region)>();
        for (int i = 0; i < clauses.Count; i++)
        {
            var c = clauses[i];
            if (!blocks.Any(b => b.Open == ListingRegionBoundary.TryStart && b.Start == c.TryStart && b.End == c.TryEnd))
                blocks.Add((c.TryStart, c.TryEnd, ListingRegionBoundary.TryStart, ListingRegionBoundary.TryEnd, i + 1));
            if (c.FilterStart is int filter)
                blocks.Add((filter, c.HandlerStart, ListingRegionBoundary.FilterStart, ListingRegionBoundary.FilterEnd, i + 1));
            blocks.Add((c.HandlerStart, c.HandlerEnd, ListingRegionBoundary.HandlerStart, ListingRegionBoundary.HandlerEnd, i + 1));
        }
        return blocks;
    }

    [Fact]
    public void EachMethodsClauses_AreTheBodysClauses_AndEachPrintsAsILDASMPrintsIt()
    {
        string path = Path.Combine(_dir, "regions.cob");
        File.WriteAllText(path, string.Join("\n", Source));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, EditionInfo.Latest, Path.Combine(_dir, "regions.dll"),
            Listing: new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json | ListingFormats.Html)
            { Sections = ListingSectionSet.Of(ListingSection.Source, ListingSection.ObjectCode) }));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var listing = result.Listing!;
        ListingJsonContract.AssertHolds(listing);
        var map = Assert.IsType<ListingObjectCodeMap>(listing.ObjectCode);
        var expected = ClausesOf(map.Assembly);

        // The model holds each method's clauses as the body lists them, the catch types named.
        foreach (var method in map.Methods) Assert.Equal(expected[method.Token], method.Regions);

        // The program compiles to every kind the test names, nested.
        var all = map.Methods.SelectMany(m => m.Regions).ToList();
        Assert.Contains(all, r => r.Kind == ListingRegionKind.Catch);
        Assert.Contains(all, r => r.Kind == ListingRegionKind.Filter);
        Assert.Contains(all, r => r.Kind == ListingRegionKind.Finally);
        Assert.Contains(map.Methods, m => m.Regions.Any(r => m.Regions.Any(o => (o.TryStart, o.TryEnd) != (r.TryStart, r.TryEnd)
            && ((o.TryStart <= r.TryStart && r.TryEnd <= o.TryEnd) || (o.HandlerStart <= r.TryStart && r.TryEnd <= o.HandlerEnd)))));

        // Every line of every method prints once, in one of the method's stretches, at its offset and depth; within a stretch in offset
        // order.
        foreach (var method in map.Methods)
        {
            var stretches = map.Ranges.Where(r => r.Method == method.Id).ToList();
            Assert.All(stretches, s => Assert.Equal(s.Regions!.Select(m => m.Offset).Order(), s.Regions!.Select(m => m.Offset)));
            Assert.Equal(ExpectedLines(method.Regions).OrderBy(Key), stretches.SelectMany(s => s.Regions!).OrderBy(Key));

            // Each line prints in the stretch whose code it bounds (section 6.9): an opening line in the stretch holding its offset, a
            // closing line in the stretch holding the block's last instruction; only a line no stretch owns prints elsewhere. And a
            // stretch that follows the one before it directly begins at the number of blocks containing its start.
            var blocks = Blocks(method.Regions);
            foreach (var s in stretches)
                foreach (var line in s.Regions!)
                    Assert.True(Owns(s, line) || !stretches.Any(o => Owns(o, line)),
                        $"{method.Name}: the {line.Boundary} line at IL_{line.Offset:X4} prints in IL_{s.Start:X4}-IL_{s.End:X4}, "
                        + "not in the stretch whose code it bounds");
            var ordered = stretches.OrderBy(s => s.Start).ToList();
            for (int i = 1; i < ordered.Count; i++)
                if (ordered[i - 1].End == ordered[i].Start)
                    Assert.Equal(blocks.Count(b => b.Start <= ordered[i].Start && ordered[i].Start < b.End), ordered[i].Depth);
        }

        // The text listing prints each line as ILDASM spells it: one .try per protected block, a catch row naming each catch clause's
        // type, a filter and a finally row per clause of those kinds, an end-handler row per clause.
        string[] rows = [.. File.ReadAllLines(Path.Combine(_dir, "regions.lst")).Select(r => r.Trim())];
        Assert.Equal(all.Select(r => (r.TryStart, r.TryEnd)).Distinct().Count(), rows.Count(r => r == ".try {"));
        Assert.Equal(all.Where(r => r.Kind == ListingRegionKind.Catch).Select(r => "catch " + r.CatchType + " {").Order(),
            rows.Where(r => r.StartsWith("catch ", StringComparison.Ordinal)).Order());
        Assert.Equal(all.Count(r => r.Kind == ListingRegionKind.Filter), rows.Count(r => r == "filter {"));
        Assert.Equal(all.Count(r => r.Kind == ListingRegionKind.Filter), rows.Count(r => r == "{ // handler"));
        Assert.Equal(all.Count(r => r.Kind == ListingRegionKind.Finally), rows.Count(r => r == "finally {"));
        Assert.Equal(all.Count, rows.Count(r => r == "} // end handler"));

        // The page prints the same lines, one row each, once: under each statement's line in the source section, which the object-code
        // section's entry links to (DESIGN-listing.md section 7.2, kb/Work PB5941's one copy of the instructions).
        string page = File.ReadAllText(Path.Combine(_dir, "regions.lst.html"));
        Assert.Equal(map.Ranges.Sum(r => r.Regions!.Count), page.Split("<tr class=\"rg\"").Length - 1);
    }

    private static (int, ListingRegionBoundary, int, int) Key(ListingRegionMark m) => (m.Offset, m.Boundary, m.Region, m.Depth);
}
