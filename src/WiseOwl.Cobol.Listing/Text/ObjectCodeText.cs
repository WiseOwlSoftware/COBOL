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
        foreach (string piece in TextListingRenderer.Indented("ASSEMBLY  " + map.Assembly, 0, 10, width)) yield return piece;
        yield return "";
        yield return string.Create(CultureInfo.InvariantCulture, $"{"METHOD",6} {"TOKEN",-10} {"SIZE",6}  NAME");
        foreach (var method in map.Methods)
            foreach (string piece in TextListingRenderer.Indented(string.Create(CultureInfo.InvariantCulture,
                $"{method.Id,6} {Token(method.Token),-10} {method.CodeSize,6}  {MethodName(method)}"), 0, 26, width))
                yield return piece;
        yield return "";
        yield return string.Create(CultureInfo.InvariantCulture, $"{"LINE",6} {"COL",4} {"METHOD",6}  IL RANGE");
        foreach (var range in map.Ranges)
            yield return string.Create(CultureInfo.InvariantCulture,
                $"{range.Line,6} {range.Column,4} {range.Method,6}  {Offset(range.Start)}-{Offset(range.End)}");
    }

    /// <summary>The object code's rows: for each stretch of a statement's code in listing order, a row naming its listing line, column,
    /// method and IL range, followed by the statement's line as written when the document holds the source section, then one row per
    /// IL instruction (its offset, name and operand), indented under it.</summary>
    public static IEnumerable<string> CodeRows(ListingObjectCodeMap map, IReadOnlyList<ListingLine> lines, int width)
    {
        var methods = map.Methods.ToDictionary(m => m.Id);
        var text = lines.ToDictionary(l => l.Number, l => l.Text);
        if (map.Ranges.Count == 0) yield return "No statement of the listing compiled to object code.";
        bool first = true;
        foreach (var range in map.Ranges)
        {
            if (!first) yield return "";
            first = false;
            string heading = string.Create(CultureInfo.InvariantCulture,
                $"{range.Line,6} {range.Column,4}  {MethodName(methods[range.Method])}  {Offset(range.Start)}-{Offset(range.End)}");
            foreach (string piece in TextListingRenderer.Indented(heading, 0, 13, width)) yield return piece;
            if (text.TryGetValue(range.Line, out string? source))
                foreach (string piece in TextListingRenderer.Indented(source.TrimEnd(), 13, 13, width)) yield return piece;
            foreach (var instruction in range.Instructions ?? [])
            {
                string row = string.Create(CultureInfo.InvariantCulture, $"{Offset(instruction.Offset)}  {instruction.OpCode,-12} {instruction.Operand}");
                foreach (string piece in TextListingRenderer.Indented(row.TrimEnd(), 13, 36, width)) yield return piece;
            }
        }
    }

    /// <summary>A method as the object-code rows name it: its type and its name, ECMA-335's <c>Type::Name</c>.</summary>
    public static string MethodName(ListingMethod method) => method.Type + "::" + method.Name;

    /// <summary>An IL offset as an IL listing spells it: <c>IL_</c> and four or more hexadecimal digits.</summary>
    public static string Offset(int offset) => string.Create(CultureInfo.InvariantCulture, $"IL_{offset:X4}");

    private static string Token(int token) => string.Create(CultureInfo.InvariantCulture, $"0x{token:X8}");
}
