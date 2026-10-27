// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.IO;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// <see cref="ConsoleInputReader"/>: the end-of-input signal of an interactive Windows console (kb/Work PB3294). The
/// console's own line read reports Ctrl+Z at the start of a line as end of input and discards that line; reading the
/// console through <c>ReadConsoleW</c> (what carries characters outside its input code page) delivers U+001A as a
/// character instead, and this reader restores the signal. The expected values are the console's measured behaviour
/// (a pseudoconsole run of the same keystrokes through the <c>ReadFile</c> arm: a lone Ctrl+Z, and Ctrl+Z followed by
/// text, both read as end-of-input), restated here over a <see cref="StringReader"/> so the rule is pinned on every
/// platform. The device end of it is witnessed on a real console by <c>WindowsConsoleDeviceTests</c>.
/// </summary>
public sealed class ConsoleInputReaderTests
{
    private const string Z = "\u001A";

    [Fact]
    public void Ctrl_Z_at_the_start_of_a_line_is_end_of_input_and_discards_the_line()
    {
        using var reader = new ConsoleInputReader(new StringReader("ab\r\n" + Z + "\r\n" + Z + "tail\r\ncd\r\n"));

        Assert.Equal("ab", reader.ReadLine());
        Assert.Null(reader.ReadLine());        // a lone Ctrl+Z line
        Assert.Null(reader.ReadLine());        // Ctrl+Z then text: the whole line is discarded
        Assert.Equal("cd", reader.ReadLine()); // the next read asks the console again, as the console's own read did
        Assert.Null(reader.ReadLine());        // and the real end of the stream is still the end
    }

    [Fact]
    public void Ctrl_Z_elsewhere_in_a_line_is_an_ordinary_character()
    {
        using var reader = new ConsoleInputReader(new StringReader("ab" + Z + "cd\n" + "x" + Z + "\n" + "q"));

        Assert.Equal("ab" + Z + "cd", reader.ReadLine());
        Assert.Equal("x" + Z, reader.ReadLine());
        Assert.Equal("q", reader.ReadLine());
    }

    [Fact]
    public void Peek_agrees_with_Read_at_the_start_of_a_line()
    {
        using var reader = new ConsoleInputReader(new StringReader("a\n" + Z + "\nb"));

        Assert.Equal('a', reader.Peek());
        Assert.Equal('a', reader.Read());
        Assert.Equal('\n', reader.Read());
        Assert.Equal(-1, reader.Peek());       // at a line start, Ctrl+Z reads as the end, and peeking changes nothing
        Assert.Equal(-1, reader.Read());
        Assert.Equal('b', reader.Read());
    }
}
