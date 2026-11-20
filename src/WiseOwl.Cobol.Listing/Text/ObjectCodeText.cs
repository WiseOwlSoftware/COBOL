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
            foreach (var instruction in range.Instructions ?? [])
            {
                // One string per instruction (kb/Work PB5941): the row is made indented, and cut only when it is wider than the page.
                string row = instruction.Operand is { } operand
                    ? string.Create(CultureInfo.InvariantCulture, $"{InstructionIndent}{new IlOffset(instruction.Offset)}  {instruction.OpCode,-12} {operand}")
                    : string.Create(CultureInfo.InvariantCulture, $"{InstructionIndent}{new IlOffset(instruction.Offset)}  {instruction.OpCode}");
                if (row.Length <= width) yield return row;
                else foreach (string piece in TextListingRenderer.Indented(row[InstructionIndent.Length..], InstructionIndent.Length, 36, width)) yield return piece;
            }
        }
    }

    /// <summary>The indent of an instruction row under its stretch's heading.</summary>
    private const string InstructionIndent = "             ";

    /// <summary>A method as the object-code rows name it: its type and its name, ECMA-335's <c>Type::Name</c>.</summary>
    public static string MethodName(ListingMethod method) => method.Type + "::" + method.Name;

    /// <summary>An IL offset as an IL listing spells it: <c>IL_</c> and four or more hexadecimal digits (<see cref="IlOffset"/>).</summary>
    public static string Offset(int offset) => new IlOffset(offset).ToString();

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

/// <summary>⛔ THE ONE SPELLING OF AN IL OFFSET, <c>IL_</c> and four or more hexadecimal digits, as a value that formats itself into a
/// row or a page without a string of its own (kb/Work PB5941: the object code prints one per IL instruction).</summary>
/// <param name="Offset">The offset in its method's IL body.</param>
public readonly record struct IlOffset(int Offset) : ISpanFormattable
{
    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        destination.TryWrite(CultureInfo.InvariantCulture, $"IL_{Offset:X4}", out charsWritten);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>The offset as an IL listing spells it.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{this}");
}
