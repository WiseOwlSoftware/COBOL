using CobolSharp.Compiler.Common;
using Xunit;

namespace CobolSharp.Tests.Unit.Common;

public class SourceTextTests
{
    [Fact]
    public void SourceText_TracksLines()
    {
        var source = SourceText.From("line 1\nline 2\nline 3");
        Assert.Equal(3, source.LineCount);
    }

    [Fact]
    public void SourceText_GetLineNumber()
    {
        var source = SourceText.From("abc\ndef\nghi");
        Assert.Equal(0, source.GetLineNumber(0)); // 'a'
        Assert.Equal(0, source.GetLineNumber(2)); // 'c'
        Assert.Equal(1, source.GetLineNumber(4)); // 'd'
        Assert.Equal(2, source.GetLineNumber(8)); // 'g'
    }

    [Fact]
    public void SourceText_GetColumn()
    {
        var source = SourceText.From("abc\ndef");
        Assert.Equal(0, source.GetColumn(0)); // 'a'
        Assert.Equal(2, source.GetColumn(2)); // 'c'
        Assert.Equal(0, source.GetColumn(4)); // 'd'
        Assert.Equal(2, source.GetColumn(6)); // 'f'
    }

    [Fact]
    public void SourceText_GetLocation()
    {
        var source = SourceText.From("abc\ndef", "test.cob");
        var loc = source.GetLocation(5); // 'e'
        Assert.Equal(1, loc.Line);
        Assert.Equal(1, loc.Column);
        Assert.Equal("test.cob", loc.FileName);
    }

    [Fact]
    public void SourceText_GetLineText()
    {
        var source = SourceText.From("first\nsecond\nthird");
        Assert.Equal("first", source.GetLineText(0));
        Assert.Equal("second", source.GetLineText(1));
        Assert.Equal("third", source.GetLineText(2));
    }

    [Fact]
    public void SourceText_HandlesWindowsLineEndings()
    {
        var source = SourceText.From("abc\r\ndef\r\nghi");
        Assert.Equal(3, source.LineCount);
        Assert.Equal("abc", source.GetLineText(0));
        Assert.Equal("def", source.GetLineText(1));
        Assert.Equal("ghi", source.GetLineText(2));
    }

    [Fact]
    public void TextSpan_Contains()
    {
        var span = new TextSpan(5, 10);
        Assert.True(span.Contains(5));
        Assert.True(span.Contains(14));
        Assert.False(span.Contains(4));
        Assert.False(span.Contains(15));
    }

    [Fact]
    public void TextSpan_FromBounds()
    {
        var span = TextSpan.FromBounds(3, 7);
        Assert.Equal(3, span.Start);
        Assert.Equal(4, span.Length);
        Assert.Equal(7, span.End);
    }
}
