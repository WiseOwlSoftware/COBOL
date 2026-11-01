// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB4304 — ISO §13.18.40.3 SR4's number is the EDITION's: 30 at COBOL-85, 50 in the 2002 text's own SR4
/// (its Annex F.2 item 94, "increased from 30 to 50"), 63 from COBOL-2014 and in the 2023 text. The two edges are the
/// construct registry rows <c>picture-string-50-2002</c> and <c>picture-string-63-2014</c>
/// (docs/VERSION_CHANGE_REFERENCE.md row 7.46); these tests ask the one bound, <see cref="PictureAnalyzer.MaxStringLength"/>,
/// and the one analyzer prologue that applies it, from both sides of every boundary.
/// </summary>
public sealed class PictureStringLengthTests
{
    [Theory]
    [InlineData(85, 30)]
    [InlineData(2002, 50)]
    [InlineData(2014, 63)]
    [InlineData(2023, 63)]
    public void MaxStringLength_IsTheEditions(int edition, int expected) =>
        Assert.Equal(expected, PictureAnalyzer.MaxStringLength(edition));

    private static string[] Diagnose(string picture, int edition, bool permissive = false)
    {
        var ed = new EditionContext(edition, permissive);
        PictureAnalyzer.Analyze(picture, Usage.Display, ed, "data item 'T'");
        return ed.Diagnostics.ToArray();
    }

    [Theory]
    [InlineData(85, 30, 31)]
    [InlineData(2002, 50, 51)]
    [InlineData(2014, 63, 64)]
    [InlineData(2023, 63, 64)]
    public void TheBoundary_IsLegalAndOneOverIsCobol2146_AtEveryEdition(int edition, int atBound, int overBound)
    {
        Assert.Empty(Diagnose(new string('X', atBound), edition));
        var over = Diagnose(new string('X', overBound), edition);
        var diagnostic = Assert.Single(over);
        Assert.Contains("COBOL2146", diagnostic);
        Assert.Contains($"{overBound} characters long", diagnostic);
        Assert.Contains($"the maximum is {atBound}", diagnostic);
    }

    [Theory]
    [InlineData(85, true)]
    [InlineData(2002, true)]
    [InlineData(2014, false)]
    [InlineData(2023, false)]
    public void ALengthErrorBelowItsEdge_NamesWhereTheBoundBegins(int edition, bool named)
    {
        var diagnostic = Assert.Single(Diagnose(new string('X', 64), edition));
        Assert.Equal(named, diagnostic.Contains("(63 from COBOL-2014)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(85, 31)]
    [InlineData(2002, 51)]
    public void ALengthErrorIsAnErrorUnderPermissiveToo(int edition, int written) =>
        Assert.Single(Diagnose(new string('X', written), edition, permissive: true));

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void TheCountIsOverTheWrittenString_NotTheExpansion(int edition) =>
        Assert.Empty(Diagnose("X(30000)", edition));
}
