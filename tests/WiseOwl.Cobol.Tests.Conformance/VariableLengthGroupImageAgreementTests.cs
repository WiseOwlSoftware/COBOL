// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE IMAGE OF A VARIABLE-LENGTH GROUP IS ITS STORAGE (kb/Work PB3441; CONFORMANCE.md A.1 items 57 and 63): for every
/// shape of variable-length group, in a declared record and in a cell-backed (EXTERNAL) one, the characters DISPLAY shows,
/// <c>FUNCTION LENGTH</c> (ISO §15.50.4 rule 7), <c>FUNCTION BYTE-LENGTH</c> (§15.14.4 rule 6) and the bytes CONVERT's ANY
/// source takes (§15.19.3 rule 7, §15.19.4 rule 2: the digits of the bits of argument-1; <c>--permissive</c>, because a
/// group is not a legal ANY source under strict conformance) are one number of positions, and the hexadecimal digits are
/// those of the displayed characters. The group image once held a dynamic-length NATIONAL item one character a position
/// (and the elements of a national table the same) while a FIXED national member was its UTF-16BE byte pairs, so DISPLAY
/// showed a width LENGTH did not count, CONVERT ANY aborted, and a MOVE of such a group stored the character image as the
/// table's byte image (a corrupted table). The shapes below are the member kinds the one composer
/// (<c>GroupImageCodec.VarParts</c>) classifies, each beside a national member, so a NEXT kind of member that is not
/// storage-faithful fails here.
/// <para>The data holds no line terminator, so each displayed line is one image.</para>
/// </summary>
public sealed class VariableLengthGroupImageAgreementTests
{
    /// <summary>One shape: its member descriptions (<c>{p}</c> is the shape's unique name prefix) and the statements that
    /// give each dynamic member a nonempty, distinct content.</summary>
    private sealed record Shape(string Name, string Members, string Moves);

    private static readonly Shape[] Shapes =
    [
        new("a dynamic-length national item beside a numeric", """
          05 {p}A PIC N DYNAMIC LENGTH LIMIT IS 8.
          05 {p}N PIC 9(2).
""", """
           MOVE N"xy" TO {p}A
           MOVE 6 TO {p}N
"""),
        new("a dynamic-capacity table of national elements", """
          05 {p}T PIC N OCCURS DYNAMIC CAPACITY IN {p}C INITIALIZED.
          05 {p}N PIC 9.
""", """
           SET {p}C TO 2
           MOVE N"p" TO {p}T(1)
           MOVE N"q" TO {p}T(2)
           MOVE 7 TO {p}N
"""),
        new("a national table of two-position elements beside an alphanumeric dynamic item and a fixed national", """
          05 {p}A PIC X DYNAMIC LENGTH LIMIT IS 8.
          05 {p}F PIC N(2).
          05 {p}T PIC N(2) OCCURS DYNAMIC CAPACITY IN {p}C FROM 1.
""", """
           MOVE "ab" TO {p}A
           MOVE N"fg" TO {p}F
           MOVE N"hi" TO {p}T(1)
           MOVE N"jk" TO {p}T(2)
"""),
        new("a nested group holding a dynamic national item", """
          05 {p}I.
             10 {p}A PIC N DYNAMIC LENGTH LIMIT IS 4.
             10 {p}B PIC 9.
          05 {p}X PIC X.
""", """
           MOVE N"lm" TO {p}A
           MOVE 3 TO {p}B
           MOVE "z" TO {p}X
"""),
        new("a fixed table of groups each holding a dynamic national item", """
          05 {p}E OCCURS 2.
             10 {p}A PIC N DYNAMIC LENGTH LIMIT IS 4.
             10 {p}B PIC X.
""", """
           MOVE N"n" TO {p}A(1)
           MOVE "u" TO {p}B(1)
           MOVE N"op" TO {p}A(2)
           MOVE "v" TO {p}B(2)
"""),
        new("an OCCURS DEPENDING table of national elements beside a dynamic national item", """
          05 {p}K PIC 9.
          05 {p}A PIC N DYNAMIC LENGTH LIMIT IS 4.
          05 {p}T PIC N OCCURS 1 TO 4 DEPENDING ON {p}K.
""", """
           MOVE 3 TO {p}K
           MOVE N"rs" TO {p}A
           MOVE N"1" TO {p}T(1)
           MOVE N"2" TO {p}T(2)
           MOVE N"3" TO {p}T(3)
"""),
        new("a table of national-form NUMERIC elements beside an alphanumeric dynamic item", """
          05 {p}T PIC 9(2) USAGE NATIONAL OCCURS DYNAMIC CAPACITY IN {p}C INITIALIZED.
          05 {p}A PIC X DYNAMIC LENGTH LIMIT IS 4.
""", """
           SET {p}C TO 2
           MOVE 12 TO {p}T(1)
           MOVE 34 TO {p}T(2)
           MOVE "tt" TO {p}A
"""),
        new("a dynamic national item AFTER a fixed national member", """
          05 {p}F PIC N(2).
          05 {p}A PIC N DYNAMIC LENGTH LIMIT IS 4.
""", """
           MOVE N"uv" TO {p}F
           MOVE N"w" TO {p}A
"""),
        new("a dynamic-capacity table of groups each holding a dynamic national item", """
          05 {p}E OCCURS DYNAMIC CAPACITY IN {p}C INITIALIZED.
             10 {p}A PIC N DYNAMIC LENGTH LIMIT IS 4.
             10 {p}B PIC X.
""", """
           SET {p}C TO 2
           MOVE N"n" TO {p}A(1)
           MOVE "u" TO {p}B(1)
           MOVE N"op" TO {p}A(2)
           MOVE "v" TO {p}B(2)
"""),
        new("an OCCURS DEPENDING table of groups each holding a dynamic national item", """
          05 {p}K PIC 9.
          05 {p}E OCCURS 1 TO 3 DEPENDING ON {p}K.
             10 {p}A PIC N DYNAMIC LENGTH LIMIT IS 4.
             10 {p}B PIC X.
""", """
           MOVE 2 TO {p}K
           MOVE N"q" TO {p}A(1)
           MOVE "u" TO {p}B(1)
           MOVE N"rs" TO {p}A(2)
           MOVE "v" TO {p}B(2)
"""),
        new("the alphanumeric control", """
          05 {p}A PIC X DYNAMIC LENGTH LIMIT IS 8.
          05 {p}T PIC X(2) OCCURS DYNAMIC CAPACITY IN {p}C INITIALIZED.
          05 {p}N PIC 9.
""", """
           MOVE "dyn" TO {p}A
           SET {p}C TO 2
           MOVE "ee" TO {p}T(1)
           MOVE "ff" TO {p}T(2)
           MOVE 4 TO {p}N
"""),
    ];

    /// <summary>The whole program for one lane: <paramref name="external"/> makes every group an EXTERNAL record (the cell
    /// lane, <c>StorageCell</c>), else a plain WORKING-STORAGE record (the declared lane, a generated record struct).</summary>
    private static string Program(bool external)
    {
        var data = new StringBuilder();
        var code = new StringBuilder();
        for (int i = 0; i < Shapes.Length; i++)
        {
            string p = $"G{i}";
            data.Append(CultureInfo.InvariantCulture, $"       01 {p}{(external ? " EXTERNAL" : "")}.\n");
            data.Append(Shapes[i].Members.Replace("{p}", p, StringComparison.Ordinal));
            code.Append(Shapes[i].Moves.Replace("{p}", p, StringComparison.Ordinal));
            code.Append(CultureInfo.InvariantCulture, $"           DISPLAY \"W{i}=<\" {p} \">\"\n");
            code.Append(CultureInfo.InvariantCulture, $"           DISPLAY \"L{i}=\" FUNCTION LENGTH ({p})\n");
            code.Append(CultureInfo.InvariantCulture, $"           DISPLAY \"B{i}=\" FUNCTION BYTE-LENGTH ({p})\n");
            code.Append(CultureInfo.InvariantCulture, $"           DISPLAY \"H{i}=\" FUNCTION CONVERT ({p} ANY ANUM HEX)\n");
        }
        return $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3441AGREE{(external ? "X" : "D")}.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
{data}       PROCEDURE DIVISION.
       MAIN-P.
{code}           STOP RUN.
""";
    }

    /// <summary>Every shape, in both lanes: the displayed width, LENGTH, BYTE-LENGTH and half the CONVERT ANY digit count are
    /// one number, and the digits are the hexadecimal of the displayed characters (each an image character, a byte).</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisplayedWidth_Length_ByteLengthAndConvertAny_AreOneImage(bool external)
    {
        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Program(external), 2023, permissive: true);
        Assert.True(ranOk, detail);
        var lines = stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        string Line(string key) => lines.Single(l => l.StartsWith(key, StringComparison.Ordinal))[key.Length..];
        for (int i = 0; i < Shapes.Length; i++)
        {
            string shape = $"{(external ? "cell-backed" : "declared")} group: {Shapes[i].Name}";
            string shown = Line($"W{i}=<");
            Assert.EndsWith(">", shown, StringComparison.Ordinal);
            string image = shown[..^1];
            string hex = Line($"H{i}=");
            Assert.True(image.Length > 0, shape);
            Assert.Equal(image.Length, int.Parse(Line($"L{i}="), CultureInfo.InvariantCulture));
            Assert.Equal(image.Length, int.Parse(Line($"B{i}="), CultureInfo.InvariantCulture));
            Assert.Equal(string.Concat(image.Select(c => ((int)c).ToString("X2", CultureInfo.InvariantCulture))), hex);
        }
    }

    /// <summary>The cell-backed lane's image is the declared lane's, shape by shape: one program, the same members and
    /// values, the same DISPLAYed characters whichever storage holds the record.</summary>
    [Fact]
    public void ACellBackedGroupShowsTheImageADeclaredOneShows()
    {
        var declared = EditionHarness.CompileAndRun(Program(external: false), 2023, permissive: true);
        var cell = EditionHarness.CompileAndRun(Program(external: true), 2023, permissive: true);
        Assert.True(declared.Ok, declared.Detail);
        Assert.True(cell.Ok, cell.Detail);
        Assert.Equal(declared.Stdout, cell.Stdout);
    }
}
