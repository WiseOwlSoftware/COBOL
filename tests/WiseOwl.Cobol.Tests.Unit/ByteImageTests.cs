// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>The one same-length byte patch every compiled-module fixture uses (kb/Work PB4470, PB4460): it replaces every
/// occurrence, counts them so a caller can refuse a vacuous patch, never re-searches what it wrote, and refuses a patch that
/// would move an offset or change nothing.</summary>
public sealed class ByteImageTests
{
    private static byte[] B(string text) => System.Text.Encoding.ASCII.GetBytes(text);

    [Fact]
    public void ReplaceEvery_ReplacesEveryOccurrence_AndCountsThem()
    {
        byte[] image = B("..9.0.0.0..9.0.0.0..9.0.0.0..");
        int replaced = ByteImage.ReplaceEvery(image, B("9.0.0.0"), B("0.0.0.9"));
        Assert.Equal(3, replaced);
        Assert.Equal("..0.0.0.9..0.0.0.9..0.0.0.9..", System.Text.Encoding.ASCII.GetString(image));
    }

    [Fact]
    public void ReplaceEvery_ReportsZero_WhenTheTextIsAbsent_AndLeavesTheImage()
    {
        byte[] image = B("nothing to see");
        Assert.Equal(0, ByteImage.ReplaceEvery(image, B("absent"), B("ABSENT")));
        Assert.Equal("nothing to see", System.Text.Encoding.ASCII.GetString(image));
    }

    [Fact]
    public void ReplaceEvery_NeverSearchesTheTextItWrote()
    {
        // "AB" -> "BA" over "ABAB": scanning from the start after each write would find the "AB" that the first write made
        // out of "BA"+"B" and replace it again; a forward scan sees two occurrences and stops.
        byte[] image = B("ABAB");
        Assert.Equal(2, ByteImage.ReplaceEvery(image, B("AB"), B("BA")));
        Assert.Equal("BABA", System.Text.Encoding.ASCII.GetString(image));
    }

    [Fact]
    public void ReplaceEvery_FindsAnOccurrenceAtTheVeryEnd()
    {
        byte[] image = B("xxAB");
        Assert.Equal(1, ByteImage.ReplaceEvery(image, B("AB"), B("CD")));
        Assert.Equal("xxCD", System.Text.Encoding.ASCII.GetString(image));
    }

    [Theory]
    [InlineData("AB", "ABC")]   // a different length would move every offset after it
    [InlineData("AB", "AB")]    // nothing changes
    [InlineData("", "")]        // nothing to find
    public void ReplaceEvery_RefusesAPatchThatMovesOrChangesNothing(string from, string to)
    {
        Assert.Throws<ArgumentException>(() => ByteImage.ReplaceEvery(B("ABAB"), B(from), B(to)));
    }

    [Fact]
    public void PatchFile_WritesBackOnlyWhenSomethingChanged()
    {
        string dir = ScratchDirectory.Create("byteimage");
        try
        {
            string file = Path.Combine(dir, "module.bin");
            File.WriteAllBytes(file, B("a9.0.0.0b"));
            var stamp = File.GetLastWriteTimeUtc(file);

            Assert.Equal(0, ByteImage.PatchFile(file, B("absent"), B("ABSENT")));
            Assert.Equal(stamp, File.GetLastWriteTimeUtc(file));

            Assert.Equal(1, ByteImage.PatchFile(file, B("9.0.0.0"), B("8.0.0.0")));
            Assert.Equal("a8.0.0.0b", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(file)));
        }
        finally
        {
            ScratchDirectory.Remove(dir);
        }
    }
}
