// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using System.Text;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE BYTES OF A SOURCE OR LIBRARY TEXT FILE BECOME CHARACTERS IN ONE PLACE, AND NO BYTE IS EVER REPLACED BY U+FFFD
/// (docs/CONFORMANCE.md §7, Annex A.1 items 25 and 26; ISO §8.1.3.2 GR1 and GR2; kb/Work PB1397).
/// <para>Before this class's subject existed, <c>File.ReadAllText</c> read every file and substituted U+FFFD for each byte
/// it could not decode, so a Latin-1 <c>"é"</c> in a literal became <c>FUNCTION ORD</c> 65534 with no diagnostic. The
/// determination (written in DOC-A.1-25 and DOC-A.1-26): a byte-order mark names the encoding; a marked file not
/// well-formed in it is the error COBOL5434 naming the file and the byte offset; an unmarked file is UTF-8, or ISO/IEC
/// 8859-1 with the warning COBOL5435 when it is not well-formed UTF-8.</para>
/// <para>The expected values here are computed from the encodings' own definitions (RFC 3629 for UTF-8, RFC 2781 for
/// UTF-16, ISO/IEC 10646 for UTF-32 and ISO/IEC 8859-1's identity with U+0000..U+00FF), never from a run.</para>
/// </summary>
public sealed class SourceTextDecoderTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("WiseOwlCobol_PB1397_");

    public void Dispose() { ScratchDirectory.Remove(_dir); }

    private static byte[] Concat(byte[] mark, byte[] body) => [.. mark, .. body];

    [Fact] // A UTF-8 file with no mark is read as UTF-8: 0xC3 0xA9 is U+00E9.
    public void UnmarkedWellFormedUtf8_IsUtf8()
    {
        var d = SourceTextDecoder.Decode("Aé€Z"u8);
        Assert.Equal(SourceTextReading.Utf8, d.Reading);
        Assert.Equal("Aé€Z", d.Text);
        Assert.Equal(-1, d.FaultOffset);
    }

    [Fact] // An unmarked file that is not UTF-8 is Latin-1: every byte is the character with that code point, none is U+FFFD.
    public void UnmarkedNonUtf8_IsLatin1_EveryByteItsOwnCodePoint()
    {
        byte[] all = [.. Enumerable.Range(0, 256).Select(i => (byte)i)];
        var d = SourceTextDecoder.Decode(all);
        Assert.Equal(SourceTextReading.Latin1, d.Reading);
        Assert.Equal("ISO-8859-1", d.EncodingName);
        Assert.Equal(all.Select(b => (char)b), d.Text!.ToCharArray());
        Assert.DoesNotContain('�', d.Text);
        Assert.Equal(0x80, d.FaultOffset);   // 0x80 is the first byte of the sequence that is not UTF-8
    }

    [Fact] // The offset is the first byte that UTF-8 does not define: "AB" then a lone 0xE9 stands at offset 2.
    public void UnmarkedNonUtf8_ReportsTheOffsetOfTheFirstBadByte()
    {
        var d = SourceTextDecoder.Decode([0x41, 0x42, 0xE9, 0x43]);
        Assert.Equal(SourceTextReading.Latin1, d.Reading);
        Assert.Equal("ABéC", d.Text);
        Assert.Equal(2, d.FaultOffset);
    }

    [Fact] // A UTF-8 mark: the mark is not source text.
    public void Utf8Mark_IsStripped()
    {
        var d = SourceTextDecoder.Decode(Concat([0xEF, 0xBB, 0xBF], "A"u8.ToArray()));
        Assert.Equal(SourceTextReading.Marked, d.Reading);
        Assert.Equal("UTF-8", d.EncodingName);
        Assert.Equal("A", d.Text);
    }

    [Fact] // A UTF-8 mark names UTF-8, so a Latin-1 byte after it has no fallback: the error, at its offset in the FILE (mark included).
    public void Utf8Mark_WithInvalidContent_IsMalformed()
    {
        var d = SourceTextDecoder.Decode([0xEF, 0xBB, 0xBF, 0x41, 0xE9, 0x42]);
        Assert.Equal(SourceTextReading.Malformed, d.Reading);
        Assert.Null(d.Text);
        Assert.Equal("UTF-8", d.EncodingName);
        Assert.Equal(4, d.FaultOffset);
    }

    [Fact] // A truncated multi-byte UTF-8 sequence at the end of a marked file is malformed too.
    public void Utf8Mark_WithTruncatedSequence_IsMalformed()
    {
        var d = SourceTextDecoder.Decode([0xEF, 0xBB, 0xBF, 0x41, 0xE2, 0x82]);
        Assert.Equal(SourceTextReading.Malformed, d.Reading);
        Assert.Equal(4, d.FaultOffset);
    }

    [Theory] // The four UTF-16/32 marks each select their encoding and each is stripped.
    [InlineData("UTF-16LE", new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0xAC, 0x20 })]
    [InlineData("UTF-16BE", new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0x20, 0xAC })]
    [InlineData("UTF-32LE", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 0xAC, 0x20, 0x00, 0x00 })]
    [InlineData("UTF-32BE", new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x41, 0x00, 0x00, 0x20, 0xAC })]
    public void WideMarks_SelectTheirEncoding(string name, byte[] file)
    {
        var d = SourceTextDecoder.Decode(file);
        Assert.Equal(SourceTextReading.Marked, d.Reading);
        Assert.Equal(name, d.EncodingName);
        Assert.Equal("A€", d.Text);
    }

    [Fact] // U+10000 is a surrogate PAIR in UTF-16 (D800 DC00) and one unit in UTF-32: both well-formed, both the same text.
    public void SupplementaryCharacter_IsWellFormedInUtf16AndUtf32()
    {
        Assert.Equal("\U00010000", SourceTextDecoder.Decode([0xFF, 0xFE, 0x00, 0xD8, 0x00, 0xDC]).Text);
        Assert.Equal("\U00010000", SourceTextDecoder.Decode([0xFF, 0xFE, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00]).Text);
    }

    [Theory] // A lone surrogate is not a Unicode scalar value: malformed, at the byte where the surrogate STARTS.
    [InlineData(new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0x00, 0xD8, 0x42, 0x00 }, 4)]   // LE: high surrogate then 'B'
    [InlineData(new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0x00, 0xDC, 0x42, 0x00 }, 4)]   // LE: low surrogate on its own
    [InlineData(new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0xD8, 0x00, 0x00, 0x42 }, 4)]   // BE: high surrogate then 'B'
    [InlineData(new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0x00, 0xD8 }, 4)]               // LE: high surrogate at the end
    [InlineData(new byte[] { 0xFF, 0xFE, 0x41, 0x00, 0x42 }, 4)]                     // LE: a truncated final unit
    public void Utf16Mark_WithLoneSurrogateOrTruncatedUnit_IsMalformed(byte[] file, int offset)
    {
        var d = SourceTextDecoder.Decode(file);
        Assert.Equal(SourceTextReading.Malformed, d.Reading);
        Assert.Null(d.Text);
        Assert.Equal(offset, d.FaultOffset);
    }

    [Theory] // A UTF-32 unit beyond U+10FFFF, or a surrogate code point, is no scalar value.
    [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00 }, 8)]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x41, 0x00, 0x00, 0x00, 0x00, 0xD8, 0x00, 0x00 }, 8)]
    [InlineData(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x11, 0x00, 0x00 }, 4)]
    public void Utf32Mark_WithInvalidUnit_IsMalformed(byte[] file, int offset)
    {
        var d = SourceTextDecoder.Decode(file);
        Assert.Equal(SourceTextReading.Malformed, d.Reading);
        Assert.Equal(offset, d.FaultOffset);
    }

    [Fact] // An empty file is well-formed UTF-8 with no text.
    public void EmptyFile_IsEmptyUtf8()
    {
        var d = SourceTextDecoder.Decode([]);
        Assert.Equal(SourceTextReading.Utf8, d.Reading);
        Assert.Equal("", d.Text);
    }

    // ---- the reads that go through CompilationInputs: the diagnostics, once per file per bag ------------------------

    private string Write(string name, byte[] bytes)
    {
        string path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact] // The main source: Latin-1 draws the warning COBOL5435 at the file and the text keeps U+00E9.
    public void MainSource_Latin1_WarnsAndKeepsTheCharacter()
    {
        var inputs = new CompilationInputs();
        var bag = new DiagnosticBag();
        string path = Write("lat.cob", [.. "DISPLAY \"A"u8, 0xE9, .. "B\"."u8]);
        Assert.Equal("DISPLAY \"AéB\".", inputs.ReadSourceText(path, bag));
        var w = Assert.Single(bag.Diagnostics);
        Assert.Equal("COBOL5435", w.Code);
        Assert.False(w.IsError);
        Assert.Equal(path, w.Location.FileName);
        Assert.Contains("offset 10", w.Message);
        Assert.Contains(Path.GetFullPath(path), inputs.FilesRead);
    }

    [Fact] // The main source: a marked, malformed file is COBOL5434 and yields no text (so no parse).
    public void MainSource_MarkedMalformed_IsAnErrorAndYieldsNoText()
    {
        var inputs = new CompilationInputs();
        var bag = new DiagnosticBag();
        string path = Write("bad.cob", [0xEF, 0xBB, 0xBF, .. "DISPLAY \"A"u8, 0xE9, .. "B\"."u8]);
        Assert.Null(inputs.ReadSourceText(path, bag));
        var e = Assert.Single(bag.Diagnostics);
        Assert.Equal("COBOL5434", e.Code);
        Assert.True(e.IsError);
        Assert.Contains("offset 13", e.Message);
    }

    [Fact] // Frontend.Parse stops at the malformed main source: no tree, the one error.
    public void FrontendParse_OfAMarkedMalformedSource_ReturnsNoTreeAndTheError()
    {
        string path = Write("bad2.cob", [0xEF, 0xBB, 0xBF, 0x41, 0xE9]);
        var bag = new DiagnosticBag();
        Assert.Null(new CnFrontend { DialectLevel = 2023 }.Parse(path, bag));
        Assert.Equal(["COBOL5434"], bag.Diagnostics.Select(d => d.Code));
    }

    private (string Text, DiagnosticBag Diags) RunCopy(string mainText)
    {
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, dialectLevel: 2023, permissive: false);
        string text = ConditionalCompilationProcessor.ProcessWithCopy(mainText, copy,
            CnFrontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        return (text, bag);
    }

    [Fact] // A library text read as Latin-1 warns ONCE however many COPY statements bring it in, and the text keeps U+00E9.
    public void LibraryText_Latin1_WarnsOncePerFile()
    {
        Write("lat.cpy", [.. "01 V PIC X(1) VALUE \""u8, 0xE9, .. "\".\n"u8]);
        var (text, diags) = RunCopy("COPY lat.\nCOPY lat.\n");
        Assert.Contains("VALUE \"é\"", text);
        Assert.DoesNotContain('�', text);
        var w = Assert.Single(diags.Diagnostics);
        Assert.Equal("COBOL5435", w.Code);
        Assert.EndsWith("lat.cpy", w.Location.FileName, StringComparison.OrdinalIgnoreCase);   // the library search probes the upper-case suffix first
    }

    [Fact] // A marked malformed library text is COBOL5434 once, and the COPY adds no second diagnostic and splices no text.
    public void LibraryText_MarkedMalformed_IsOneErrorAndNoSplice()
    {
        Write("bad.cpy", [0xEF, 0xBB, 0xBF, .. "01 V PIC X VALUE \""u8, 0xE9, .. "\".\n"u8]);
        var (text, diags) = RunCopy("COPY bad.\nCOPY bad.\n");
        var e = Assert.Single(diags.Diagnostics);
        Assert.Equal("COBOL5434", e.Code);
        Assert.True(e.IsError);
        Assert.DoesNotContain("PIC X", text);
    }

    [Fact] // The control: a well-formed UTF-8 library text draws nothing.
    public void LibraryText_Utf8_DrawsNothing()
    {
        Write("ok.cpy", Encoding.UTF8.GetBytes("01 V PIC X(1) VALUE \"é\".\n"));
        var (text, diags) = RunCopy("COPY ok.\n");
        Assert.Contains("VALUE \"é\"", text);
        Assert.Empty(diags.Diagnostics);
    }
}
