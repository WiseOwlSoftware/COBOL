// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>
/// ⛔ ONE PASS DECIDES WHAT IS PRINTED AND WHERE A PAGE STARTS (DESIGN-listing.md section 4.5): renderers never re-decide it.
/// <list type="bullet">
/// <item>The LISTING state starts ON (§2023:7.3.18.3 GR2) and follows <see cref="DirectiveEffects.States"/>, the driver's own
/// state at each change (a LISTING directive, GR4 and GR5; a POP; the end of a library text, §2023:7.2.3.4 GR5). A line is
/// listed when the state is ON.</item>
/// <item>A LISTING directive line is listed whatever the state: "Each LISTING directive shall be listed, even if the listing is
/// being suppressed by a LISTING directive" (§2023:7.3.18.3 GR3).</item>
/// <item>A line of library text a COPY ... SUPPRESS statement incorporated is not listed: "library text incorporated as a
/// result of COPY statement processing is not listed" (§2023:7.2.3.4 GR4); GR3's exception names suppression by a LISTING
/// directive only, so it is not listed even when it is a LISTING directive. The COPY statement's own lines follow the state.</item>
/// <item>A page starts before a listed page-eject comment line: "The slant in the indicator area causes page ejection prior
/// to printing the comment line in the listing of the source program" (§1985:IV.7.2.4); a '*' line prints "at the next
/// available line position" and starts nothing. A listed PAGE directive line likewise: "a PAGE directive shall cause page
/// ejection followed by listing of the PAGE directive" (§2023:7.3.19.4 GR2); one under LISTING OFF is a source line that is not
/// listed, so it ejects nothing (DESIGN-listing.md section 2.3).</item>
/// <item>A page starts before the first line of each outermost program after the first, so each page heading names one
/// program (§1985:VI.3.3.4 GR1; DESIGN-listing.md section 6.1).</item>
/// </list>
/// At <c>--std 85</c> no directive exists, so every line is listed and only '/' lines and program starts break pages.
/// </summary>
internal static class ListingVisibility
{
    /// <summary>Decide <see cref="LineDraft.Listed"/> and <see cref="LineDraft.PageBreakBefore"/> for every line;
    /// <paramref name="outermostStarts"/> are the first listing lines of the outermost programs, in any order.</summary>
    public static void Apply(IReadOnlyList<LineDraft> lines, IEnumerable<int> outermostStarts, DirectiveEffects effects)
    {
        var starts = outermostStarts.Order().Skip(1).ToHashSet();
        bool on = true;
        int next = 0;
        foreach (var line in lines)
        {
            while (next < effects.States.Count && effects.States[next].From <= line.Number) on = effects.States[next++].On;
            line.Listed = !effects.Suppressed.Contains(line.Number) && (on || effects.DirectiveLines.Contains(line.Number));
            line.PageBreakBefore = line.Listed && (line.Kind == PhysicalLineKind.PageEjectComment
                || effects.PageLines.Contains(line.Number) || starts.Contains(line.Number));
        }
    }
}
