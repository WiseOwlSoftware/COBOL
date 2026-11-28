// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>
/// ⛔ THE JOINS OF THE SYMBOL TABLE'S SECTIONS ONTO THE SOURCE LINES (docs/rearchitecture/DESIGN-listing.md section 4.2): made once
/// here and stored on each line, so no renderer joins two sections and the text and any other format cannot disagree about a
/// line. Step 6 builds the margin of each definition line (<see cref="ListingLine.Margin"/>); step 7b's joins of the cross-reference
/// (each line's names and a statement line's margin) are <see cref="CrossReferenceBuilder.MarkLines"/>, made after these, so a
/// definition line keeps the data map's margin.
/// </summary>
internal static class LineAnnotationBuilder
{
    /// <summary>The margin of each line that declares a data item (DESIGN-listing.md section 6.2): the first data item the data map
    /// places on the line, its offset, length and usage.</summary>
    public static void MarkMargins(ListingDataMap map, IReadOnlyList<LineDraft> drafts)
    {
        foreach (var item in map.Items)
            if (item is { Kind: ListingDataItemKind.Data, Line: { } line } && drafts[line - 1].Margin is null)
                drafts[line - 1].Margin = new ListingMargin(item.Id, item.Offset, item.Length, item.Usage, []);
    }
}