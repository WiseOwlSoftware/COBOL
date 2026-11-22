// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB2649: the native figurative carriers (<see cref="CobolString.CompareFig(string?,string?,bool,char)"/>,
/// <see cref="CobolString.ThruMemberFig(string?,string?,string?,bool,bool,char)"/>) walk the §2023:8.3.3.6.4 GR2 sized
/// figurative without building it, while <see cref="CobolString.FigToWidth"/> stays the one materializing form (the
/// compile-time fold and the collating-sequence arms read it). These theories pin the two to one answer: over seeds of
/// every length (empty, one character, an ALL literal shorter, equal to and longer than the associated operand) and
/// associated widths from 0 up, the non-materializing walk answers exactly as the materialized string compared by
/// <see cref="CobolString.Compare(string?,string?,char)"/>.
/// </summary>
public sealed class FigurativeReaderDriftTests
{
    private static readonly string[] Seeds = ["", " ", "\0", "ÿ", "0", "AB", "ABC", "ZZZZZZZZZZ", "A B"];

    private static IEnumerable<string> Associated()
    {
        var rng = new Random(2649);
        const string alphabet = " \0AB0Zÿ";
        for (int w = 0; w <= 12; w++)
            for (int k = 0; k < 6; k++)
            {
                var chars = new char[w];
                for (int i = 0; i < w; i++) chars[i] = alphabet[rng.Next(alphabet.Length)];
                yield return new string(chars);
            }
        // The degenerate associated operands: every character equal to a seed's, so the walk runs to the end.
        foreach (var seed in Seeds)
            for (int w = 0; w <= 7; w++) yield return CobolString.FigToWidth(seed, w)[..w];
    }

    /// <summary>CompareFig on either side answers what the materialized figurative compared by Compare answers.</summary>
    [Fact]
    public void CompareFig_AnswersAsTheMaterializedFigurative()
    {
        int cases = 0;
        foreach (var seed in Seeds)
            foreach (var other in Associated())
                foreach (var pad in new[] { ' ', '0' })
                {
                    string sized = CobolString.FigToWidth(seed, other.Length);
                    Assert.Equal(Math.Sign(CobolString.Compare(sized, other, pad)),
                                 Math.Sign(CobolString.CompareFig(seed, other, figIsLeft: true, pad)));
                    Assert.Equal(Math.Sign(CobolString.Compare(other, sized, pad)),
                                 Math.Sign(CobolString.CompareFig(other, seed, figIsLeft: false, pad)));
                    cases++;
                }
        Assert.True(cases > 1000, $"the theory walked only {cases} cases");
    }

    /// <summary>ThruMemberFig answers what ThruMember answers over the materialized ends (ranges that are not
    /// inverted: an inverted range raises EC-RANGE-INVALID on both, which another test pins).</summary>
    [Fact]
    public void ThruMemberFig_AnswersAsTheMaterializedEnds()
    {
        int cases = 0;
        foreach (var read in Associated())
            foreach (var lo in Seeds)
                foreach (var hi in Seeds)
                    foreach (var (loFig, hiFig) in new[] { (true, true), (true, false), (false, true) })
                    {
                        string l = loFig ? CobolString.FigToWidth(lo, read.Length) : lo;
                        string h = hiFig ? CobolString.FigToWidth(hi, read.Length) : hi;
                        if (CobolString.Compare(l, h) > 0) continue;
                        Assert.Equal(CobolString.ThruMember(read, l, h),
                                     CobolString.ThruMemberFig(read, lo, hi, loFig, hiFig));
                        cases++;
                    }
        Assert.True(cases > 1000, $"the theory walked only {cases} cases");
    }
}
