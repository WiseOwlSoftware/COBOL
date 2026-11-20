// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>The object-code map and the object code of the text listing (DESIGN-listing.md section 6.9): read from the model alone,
/// which read them from the assembly and its PDB.</summary>
public static class ObjectCodeText
{
    /// <summary>The object-code map's rows: the assembly, the methods (id, metadata token, IL size, type and name), then one row per
    /// stretch of a statement's code in listing order (its listing line and column, its method and its IL offset range).</summary>
    public static IEnumerable<string> MapRows(ListingObjectCodeMap map, int width)
    {
        foreach (string piece in TextListingRenderer.Indented(AssemblyLabel + "  " + map.Assembly, 0, 10, width)) yield return piece;
        yield return "";
        yield return string.Create(CultureInfo.InvariantCulture, $"{MethodHeadings[0],6} {MethodHeadings[1],-10} {MethodHeadings[2],6}  {MethodHeadings[3]}");
        foreach (var method in map.Methods)
            foreach (string piece in TextListingRenderer.Indented(string.Create(CultureInfo.InvariantCulture,
                $"{method.Id,6} {Token(method.Token),-10} {method.CodeSize,6}  {MethodName(method)}"), 0, 26, width))
                yield return piece;
        yield return "";
        yield return string.Create(CultureInfo.InvariantCulture, $"{RangeHeadings[0],6} {RangeHeadings[1],4} {RangeHeadings[2],6}  {RangeHeadings[3]}");
        foreach (var range in map.Ranges)
            yield return string.Create(CultureInfo.InvariantCulture,
                $"{range.Line,6} {range.Column,4} {range.Method,6}  {RangeText(range)}");
    }

    /// <summary>The object code's rows: for each stretch of a statement's code in listing order, a row naming its listing line, column,
    /// method and IL range, followed by the statement's line as written when the document holds the source section, then one row per
    /// IL instruction (its offset, name and operand), indented under it.</summary>
    public static IEnumerable<string> CodeRows(ListingObjectCodeMap map, IReadOnlyList<ListingLine> lines, int width)
    {
        var methods = map.Methods.ToDictionary(m => m.Id);
        var text = lines.ToDictionary(l => l.Number, l => l.Text);
        if (map.Ranges.Count == 0) yield return NoCodeNote;
        bool first = true;
        foreach (var range in map.Ranges)
        {
            if (!first) yield return "";
            first = false;
            string heading = string.Create(CultureInfo.InvariantCulture, $"{range.Line,6} {range.Column,4}  {StretchText(range, methods[range.Method])}");
            foreach (string piece in TextListingRenderer.Indented(heading, 0, 13, width)) yield return piece;
            if (text.TryGetValue(range.Line, out string? source))
                foreach (string piece in TextListingRenderer.Indented(source.TrimEnd(), 13, 13, width)) yield return piece;
            var regions = methods[range.Method].Regions;
            foreach (var line in CodeLines(range))
            {
                int indent = 13 + RegionIndent * line.Depth;
                string row = line.Instruction is { } instruction ? InstructionText(instruction) : RegionText(line.Mark!, regions);
                foreach (string piece in TextListingRenderer.Indented(row, indent, line.Instruction is null ? indent : indent + 23, width))
                    yield return piece;
            }
        }
    }

    /// <summary>The columns each level of exception-handling nesting indents a line of the object code by.</summary>
    public const int RegionIndent = 2;

    /// <summary>⛔ THE ONE MERGE of a stretch's instructions and its exception-handling lines (kb/Work PB5951), which both renderers print:
    /// each region line before the first instruction at or after its offset (after the last instruction when none is), each line at the
    /// depth the builder gave it (<see cref="ListingRegionMark.Depth"/>), and each instruction at the depth the lines before it leave,
    /// from the stretch's own <see cref="ListingObjectRange.Depth"/>: one deeper after an opening line, the closing line's own depth
    /// after a closing one. Nothing here orders or nests a region; the builder did.</summary>
    public static IEnumerable<ObjectCodeLine> CodeLines(ListingObjectRange range)
    {
        var marks = range.Regions ?? [];
        int next = 0, depth = range.Depth;
        foreach (var instruction in range.Instructions ?? [])
        {
            for (; next < marks.Count && marks[next].Offset <= instruction.Offset; next++)
            {
                yield return new ObjectCodeLine(marks[next].Depth, null, marks[next]);
                depth = DepthAfter(marks[next]);
            }
            yield return new ObjectCodeLine(depth, instruction, null);
        }
        for (; next < marks.Count; next++) yield return new ObjectCodeLine(marks[next].Depth, null, marks[next]);
    }

    /// <summary>The depth the lines after <paramref name="mark"/> stand at: one deeper than an opening line, a closing line's own.</summary>
    public static int DepthAfter(ListingRegionMark mark) => ListingRegionBoundaries.Opens(mark.Boundary) ? mark.Depth + 1 : mark.Depth;

    /// <summary>An instruction as both renderers' object code prints it: its offset, its name and its operand.</summary>
    public static string InstructionText(ListingInstruction instruction) =>
        string.Create(CultureInfo.InvariantCulture, $"{Offset(instruction.Offset)}  {instruction.OpCode,-12} {instruction.Operand}").TrimEnd();

    /// <summary>A line of a method's exception-handling structure as ILDASM prints it (ECMA-335 II.19): <c>.try {</c>,
    /// <c>} // end .try</c>, <c>catch Type {</c>, <c>filter {</c>, <c>} // end filter</c>, a filter's <c>{ // handler</c>,
    /// <c>finally {</c>, <c>fault {</c> and <c>} // end handler</c>. <paramref name="regions"/> is the method's
    /// <see cref="ListingMethod.Regions"/>, which <see cref="ListingRegionMark.Region"/> indexes.</summary>
    public static string RegionText(ListingRegionMark mark, IReadOnlyList<ListingExceptionRegion> regions)
    {
        var region = regions[mark.Region - 1];
        return mark.Boundary switch
        {
            ListingRegionBoundary.TryStart => ".try {",
            ListingRegionBoundary.TryEnd => "} // end .try",
            ListingRegionBoundary.FilterStart => "filter {",
            ListingRegionBoundary.FilterEnd => "} // end filter",
            ListingRegionBoundary.HandlerStart => region.Kind switch
            {
                ListingRegionKind.Catch => "catch " + region.CatchType + " {",
                ListingRegionKind.Filter => "{ // handler",
                ListingRegionKind.Finally => "finally {",
                ListingRegionKind.Fault => "fault {",
                _ => throw new InvalidOperationException($"an exception-handling clause of kind {region.Kind}"),
            },
            ListingRegionBoundary.HandlerEnd => "} // end handler",
            _ => throw new InvalidOperationException($"an exception-handling boundary {mark.Boundary}"),
        };
    }

    /// <summary>A method as the object-code rows name it: its type and its name, ECMA-335's <c>Type::Name</c>.</summary>
    public static string MethodName(ListingMethod method) => method.Type + "::" + method.Name;

    /// <summary>An IL offset as an IL listing spells it: <c>IL_</c> and four or more hexadecimal digits.</summary>
    public static string Offset(int offset) => string.Create(CultureInfo.InvariantCulture, $"IL_{offset:X4}");

    /// <summary>A method's metadata token as both renderers print it: <c>0x</c> and eight hexadecimal digits.</summary>
    public static string Token(int token) => string.Create(CultureInfo.InvariantCulture, $"0x{token:X8}");

    /// <summary>A stretch's IL offset range as both renderers print it: <c>IL_start-IL_end</c>.</summary>
    public static string RangeText(ListingObjectRange range) => Offset(range.Start) + "-" + Offset(range.End);

    /// <summary>A stretch of a statement's code as both renderers head it: its method (<see cref="MethodName"/>) and its IL range.</summary>
    public static string StretchText(ListingObjectRange range, ListingMethod method) => MethodName(method) + "  " + RangeText(range);

    /// <summary>The label of the assembly the object-code map was read from.</summary>
    public const string AssemblyLabel = "ASSEMBLY";

    /// <summary>The object-code map's method table headings: the method's id, metadata token, IL size and name.</summary>
    public static readonly IReadOnlyList<string> MethodHeadings = ["METHOD", "TOKEN", "SIZE", "NAME"];

    /// <summary>The object-code map's stretch table headings: the listing line, column, method and IL range.</summary>
    public static readonly IReadOnlyList<string> RangeHeadings = ["LINE", "COL", "METHOD", "IL RANGE"];

    /// <summary>What the object code section says when no statement compiled to object code.</summary>
    public const string NoCodeNote = "No statement of the listing compiled to object code.";
}

/// <summary>One line of a stretch's object code as the renderers print it (<see cref="ObjectCodeText.CodeLines"/>): an instruction or a
/// line of the method's exception-handling structure, at its nesting depth.</summary>
/// <param name="Depth">The line's nesting depth (<see cref="ListingRegionMark.Depth"/>).</param>
/// <param name="Instruction">The instruction, or null for a region line.</param>
/// <param name="Mark">The region line, or null for an instruction.</param>
public readonly record struct ObjectCodeLine(int Depth, ListingInstruction? Instruction, ListingRegionMark? Mark);
