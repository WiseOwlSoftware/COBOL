// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>
/// A TOKEN THE MARGIN CUT IN TWO IS NAMED WHETHER OR NOT THE PARSE FAILS (kb/Work PB4999; ISO §2023:6.3.1 "Margin R is
/// immediately to the right of the rightmost character position of the program-text area", Annex A.1 item 158). Text past
/// margin R is not program text and the reference format drops it, so a word, a literal or a number that runs across the
/// margin is read SHORT. When the shortened token still parses, nothing is wrong to the grammar and the program means
/// something else: <c>Y ** 0.7</c> with its <c>7</c> at position 73 reads <c>Y ** 0.</c>. <see cref="CobolErrorListener"/>'s
/// note (COBOL5788) is given only BESIDE a syntax error, so this screen is the other arm of the one question "was this line's
/// program text cut where it may have been program text?": it asks it of every token of the compiled text, through the same
/// <see cref="IMarginRCutLocator"/> and the same <see cref="LocatedMarginRCut.EndsAtMarginR"/> decision (the token's last
/// character stands at position 72 and the cut text begins at position 73 with no space between them), never a second scan.
/// <para>What stays silent is what <c>ReferenceFormatMap</c> already decided is no cut (a blank tail, a comment line, an inline
/// comment's rest, a tag that recurs: the locator answers null) and a line whose text past margin R begins after a space,
/// the ordinary sequence number or identification tag. A token on a line the compiler never read (a false conditional-compilation
/// branch, a debugging line without debugging mode) is no token of the stream's default channel, so it is not asked about.</para>
/// </summary>
public static class MarginRCutScreen
{
    /// <summary>The warnings for every token of <paramref name="tokens"/> that ends at margin R with cut text right after it,
    /// except on a line <paramref name="reported"/> already holds a COBOL5788 for: the syntax-error note names that cut, and
    /// one cut is named once. In source order.</summary>
    public static List<Diagnostic> Warnings(IEnumerable<IToken> tokens, IMarginRCutLocator cuts, IReadOnlyList<Diagnostic> reported)
    {
        var warnings = new List<Diagnostic>();
        foreach (var token in tokens)
        {
            if (token.Channel != TokenConstants.DefaultChannel || token.Type == TokenConstants.EOF
                || token.StopIndex < token.StartIndex)
                continue;
            // A token continued across lines (ISO §2023:6.3.5) is one token of the resultant line whose characters stand on
            // several physical lines, and any segment may end at margin R: every character is asked about, not only the last.
            for (int offset = 0; offset <= token.StopIndex - token.StartIndex; offset++)
            {
                if (cuts.MarginRCutAt(token.Line, token.Column + offset) is not { EndsAtMarginR: true } cut)
                    continue;
                var at = cut.CutLocation;
                if (reported.Any(d => d.Code == DiagnosticCatalog.ProgramTextPastMarginR.Code && d.Location.Equals(at))) continue;
                string code = DiagnosticCatalog.TokenCutAtMarginR.Code;
                if (warnings.Any(d => d.Code == code && d.Location.Equals(at))) continue;
                warnings.Add(new Diagnostic(code, DiagnosticSeverity.Warning,
                    $"{cut.Cut.Account} The token '{WrittenSource.Of(token)}' ends at position {cut.Cut.LastProgramTextPosition} with the "
                    + "cut text right after it and no space between, so the compiler read the token as far as it was written within "
                    + $"the program-text area, which may not be the token meant. {cut.Cut.Remedy}", at, TextSpan.Empty));
            }
        }
        return warnings;
    }
}
