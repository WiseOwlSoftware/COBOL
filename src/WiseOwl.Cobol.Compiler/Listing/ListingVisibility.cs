// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ ONE PASS DECIDES WHAT IS PRINTED AND WHERE A PAGE STARTS (DESIGN-listing.md section 4.5): renderers never re-decide it.
/// <list type="bullet">
/// <item>Every line is listed. (The LISTING directive's state, §2023:7.3.18.3 GR2-GR5, and COPY SUPPRESS, §2023:7.2.3.4 GR4,
/// GR5, join this pass in step 4a; at <c>--std 85</c> no directive exists, so every line is listed.)</item>
/// <item>A page starts before a listed page-eject comment line: "The slant in the indicator area causes page ejection prior
/// to printing the comment line in the listing of the source program" (§1985:IV.7.2.4); a '*' line prints "at the next
/// available line position" and starts nothing.</item>
/// <item>A page starts before the first line of each outermost program after the first, so each page heading names one
/// program (§1985:VI.3.3.4 GR1; DESIGN-listing.md section 6.1).</item>
/// </list>
/// </summary>
internal static class ListingVisibility
{
    /// <summary>Decide <see cref="LineDraft.Listed"/> and <see cref="LineDraft.PageBreakBefore"/> for every line;
    /// <paramref name="outermostStarts"/> are the first listing lines of the outermost programs, in any order.</summary>
    public static void Apply(IReadOnlyList<LineDraft> lines, IEnumerable<int> outermostStarts)
    {
        var starts = outermostStarts.Order().Skip(1).ToHashSet();
        foreach (var line in lines)
        {
            line.Listed = true;
            line.PageBreakBefore = line.Listed && (line.Kind == PhysicalLineKind.PageEjectComment || starts.Contains(line.Number));
        }
    }
}
