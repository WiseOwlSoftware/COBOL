// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.IO;

/// <summary>
/// THE END-OF-INPUT SIGNAL OF AN INTERACTIVE WINDOWS CONSOLE, KEPT WHEN THE CONSOLE IS READ THROUGH ITS WIDE-CHARACTER
/// INTERFACE (kb/Work PB3294). A Windows console in line-input mode reports Ctrl+Z as the end of its input when the
/// line starts with it: a read returns zero bytes, and <see cref="TextReader.ReadLine"/> returns <c>null</c>, as it does
/// at the end of a pipe or a file. <see cref="StandardStreams.EnsureUtf8"/> puts an interactive console's standard input
/// on <c>ReadConsoleW</c> (so that characters outside its input code page survive), and that interface hands the
/// character U+001A to the program instead, so without this reader an <c>ACCEPT</c> typed as Ctrl+Z, Enter would store
/// that character, and a program reading until end-of-input would never see the end (measured against the console's own
/// <c>ReadFile</c> arm on Windows 11, through a pseudoconsole).
/// </summary>
/// <remarks>
/// The rule is the console's own, restated over the characters: <b>U+001A as the first character of a line is end of
/// input, and the rest of that line is discarded</b> (the console discards it too). U+001A anywhere else in a line is
/// an ordinary character. Only <see cref="Read()"/> and <see cref="Peek"/> are overridden; every other read of
/// <see cref="TextReader"/> (<c>ReadLine</c>, <c>ReadToEnd</c>, the buffer reads) is built on them, so there is one
/// place the rule lives. After the signal the next read starts a fresh line and asks the console again, which is what
/// the console's own read did.
/// </remarks>
internal sealed class ConsoleInputReader(TextReader console) : TextReader
{
    private const char EndOfInputKey = '\u001A';

    private bool _atLineStart = true;

    /// <inheritdoc/>
    public override int Peek()
    {
        int next = console.Peek();
        return _atLineStart && next == EndOfInputKey ? -1 : next;
    }

    /// <inheritdoc/>
    public override int Read()
    {
        int next = console.Read();
        if (_atLineStart && next == EndOfInputKey)
        {
            // End of input: discard the rest of the line, through its terminator, as the console does.
            int c;
            while ((c = console.Read()) != -1 && c != '\n') { }
            return -1;
        }
        _atLineStart = next is '\n' or -1;
        return next;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) console.Dispose();
        base.Dispose(disposing);
    }
}
