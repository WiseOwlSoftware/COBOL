// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Listing.Text;

/// <summary>The program map of the text listing (DESIGN-listing.md section 6.5): one row per source unit, indented by its
/// nesting.</summary>
public static class ProgramMapText
{
    /// <summary>The program map's rows (DESIGN-listing.md section 6.5): each source unit in text order, its id, kind, first and
    /// last listing line, and its name indented two columns per level of nesting, so a contained unit stands under the unit
    /// that contains it, and under a bound program its attributes (<see cref="AttributesText"/>). After a parse that recovered from a syntax error (<paramref name="parse"/>) a last row says the map holds
    /// only the units the recovery kept.</summary>
    public static IEnumerable<string> Rows(IReadOnlyList<ListingProgram> programs, ListingParse parse, int width)
    {
        yield return string.Create(CultureInfo.InvariantCulture, $"{Headings[0],6}  {Headings[1],-9} {Headings[2],6}  {Headings[3],6}  {Headings[4]}");
        foreach (var p in programs)
        {
            foreach (string row in TextListingRenderer.Indented(string.Create(CultureInfo.InvariantCulture,
                         $"{p.Id,6}  {KindWord(p.Kind),-9} {p.FirstLine,6}  {p.LastLine,6}  {new string(' ', 2 * p.Depth)}{p.Name}"),
                         0, NameColumn + 2 * p.Depth, width))
                yield return row;
            if (p.Attributes is { } a)
                foreach (string row in TextListingRenderer.Indented(AttributesText(a), NameColumn + 2 * p.Depth + 2,
                             NameColumn + 2 * p.Depth + 4, width))
                    yield return row;
        }
        if (parse is not ListingParse.Recovered) yield break;
        yield return "";
        foreach (string row in TextListingRenderer.Indented(RecoveredNote, 0, 2, width))
            yield return row;
    }

    /// <summary>The row under a bound program (DESIGN-listing.md section 6.5): its COMMON, INITIAL, RECURSIVE and PROTOTYPE
    /// attributes, the name it is externalized as, and the .NET type it became.</summary>
    public static string AttributesText(ListingProgramAttributes attributes)
    {
        var words = new List<string>();
        if (attributes.Common) words.Add("COMMON");
        if (attributes.Initial) words.Add("INITIAL");
        if (attributes.Recursive) words.Add("RECURSIVE");
        if (attributes.Prototype) words.Add("PROTOTYPE");
        words.Add($"AS '{attributes.ExternalizedName}'");
        words.Add($".NET {attributes.NativeType}");
        return string.Join("  ", words);
    }

    /// <summary>The program map's headings: the unit's id, kind, first and last listing line, and name.</summary>
    public static readonly IReadOnlyList<string> Headings = ["ID", "KIND", "FIRST", "LAST", "NAME"];

    /// <summary>What the program map says after a parse that recovered from a syntax error.</summary>
    public static readonly string RecoveredNote = TextListingRenderer.RecoveredNote("the source units");

    /// <summary>The 0-based column a program row's name starts at, before its nesting indent.</summary>
    public const int NameColumn = 34;

    /// <summary>The word a source unit's kind is printed as.</summary>
    public static string KindWord(ListingProgramKind kind) => kind switch
    {
        ListingProgramKind.Program => "program",
        ListingProgramKind.Function => "function",
        ListingProgramKind.Class => "class",
        ListingProgramKind.Interface => "interface",
        ListingProgramKind.Factory => "factory",
        ListingProgramKind.Object => "object",
        ListingProgramKind.Method => "method",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "a source unit kind with no word"),
    };
}
