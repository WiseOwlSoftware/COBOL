// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The SIGN clause beside BLANK WHEN ZERO at --std 85 (kb/Work PB5790). §1985:VI.5.4.4 GR2 makes a numeric PICTURE written
/// with BLANK WHEN ZERO "considered to be numeric edited", but the SIGN clause's subject rule asks the PICTURE: "may be
/// specified only for a numeric data description entry whose PICTURE contains the character 'S'" (§1985:VI.5.12.3 SR1;
/// the Report Writer's §1985:XIII.3.17.3 SR1, whose Table 1 of §1985:XIII.3.9.3 SR11 permits the two clauses together).
/// So the profile keeps the fact that its PICTURE is signed numeric, the one subject test reads it, and a SEPARATE sign
/// takes its own position in the edited image (§1985:VI.5.12.4 GR6a). The 2002 and later texts refuse 'S' beside the
/// clause, so nothing changes there. The goldens <c>pb5790-sign-separate-blank-when-zero-85</c> and
/// <c>pb5790-report-sign-separate-blank-when-zero-85</c> are the end-to-end witnesses.
/// </summary>
public sealed class SignBesideBlankWhenZero85Tests
{
    private static (PicInfo Pic, EditionContext Ed) Analyze(int edition, string picture, SignSpec? sign)
    {
        var ed = new EditionContext(edition);
        return (PictureAnalyzer.Analyze(picture, Usage.Display, ed, "data item 'T'", sign, blankWhenZero: true), ed);
    }

    [Theory]
    [InlineData(true, "+99999")]
    [InlineData(false, "99999+")]
    public void At85_SeparateSign_IsASubjectOfTheSignClause_AndTakesItsOwnPosition(bool leading, string mask)
    {
        var (pic, ed) = Analyze(85, "S9(5)", new SignSpec(Leading: leading, Separate: true));
        Assert.False(ed.HasErrors);
        Assert.Equal(PicCategory.NumericEdited, pic.Category);
        Assert.True(pic.BlankWhenZeroSigned);
        Assert.True(pic.PictureIsSignedNumeric);
        Assert.Equal(mask, pic.EditMask);
        Assert.Equal(6, pic.Length);
        Assert.Null(DataBinder.SignClauseElementaryDefect(pic, "S9(5)", "data description entry", ed.Edition, reportGroup: false));
        Assert.Null(DataBinder.SignClauseElementaryDefect(pic, "S9(5)", "report group description entry", ed.Edition, reportGroup: true));
    }

    [Fact]
    public void At85_EmbeddedSign_HasNoPositionInTheEditedImage()
    {
        var (pic, ed) = Analyze(85, "S9(3)", new SignSpec(Leading: true, Separate: false));
        Assert.False(ed.HasErrors);
        Assert.True(pic.PictureIsSignedNumeric);
        Assert.Equal(3, pic.Length);
        Assert.Null(DataBinder.SignClauseElementaryDefect(pic, "S9(3)", "data description entry", ed.Edition, reportGroup: false));
    }

    [Fact]
    public void At85_AnUnsignedPicture_IsStillNoSubject()
    {
        var (pic, ed) = Analyze(85, "9(5)", new SignSpec(Leading: true, Separate: true));
        Assert.False(pic.BlankWhenZeroSigned);
        Assert.False(pic.PictureIsSignedNumeric);
        Assert.Equal(5, pic.Length);
        Assert.NotNull(DataBinder.SignClauseElementaryDefect(pic, "9(5)", "data description entry", ed.Edition, reportGroup: false));
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void From2002_TheSignedPictureBesideTheClause_IsRefusedByItsOwnRule(int edition)
    {
        var (pic, ed) = Analyze(edition, "S9(5)", new SignSpec(Leading: true, Separate: true));
        Assert.True(ed.HasErrors);
        Assert.False(pic.BlankWhenZeroSigned);
    }
}
