// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DECLARED GROUP AND A CELL-BACKED GROUP HAND <c>CobolContiguousLayout.Of</c> THE SAME LAYOUT CODE FOR A DYNAMIC-CAPACITY TABLE
/// (kb/Work PB6138). <c>FileModel.LayoutCodeOf</c> is the one reader: the bare unit code of the table's elements
/// (<c>CobolDynStructure.PlainCode</c>: 0 for one image character a position, 32 for a national element, two bytes). The declared
/// lane (<c>GroupImageCodec.ContiguousLayout</c>) once passed 0 for a table, so the same national table padded with the
/// one-unit space in one lane and the national space pair in the other. This pins the code the declared lane emits.
/// </summary>
public sealed class DynamicTableLayoutCodeTests
{
    private static string EmitCSharp(string element)
    {
        string dir = ScratchDirectory.Create("pb6138");
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, $"""
                IDENTIFICATION DIVISION.
                PROGRAM-ID. PB6138P.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 G.
                   05 T PIC {element} OCCURS DYNAMIC.
                PROCEDURE DIVISION.
                MAIN-PARA.
                    STOP RUN.
                """);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, EditionInfo.Of(2023), Path.Combine(dir, "p.dll"), SourceFormat: TestSourceFormat.Inline));
            Assert.True(r.Success, string.Join("; ", r.Errors));
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The layout-code span of the one <c>CobolContiguousLayout.Of</c> call: its third span argument.</summary>
    private static string LayoutCodes(string cs)
    {
        var m = Regex.Match(cs, @"CobolContiguousLayout\.Of\(\d+, \[[^\]]*\], \[[^\]]*\], \[(?<codes>[^\]]*)\]");
        Assert.True(m.Success, "no CobolContiguousLayout.Of call in the emitted C#:" + Environment.NewLine + cs);
        return m.Groups["codes"].Value;
    }

    [Fact]
    public void NationalDynamicTable_DeclaredLayoutPassesTheNationalUnitCode()
        => Assert.Equal("32", LayoutCodes(EmitCSharp("N")));

    [Fact]
    public void AlphanumericDynamicTable_DeclaredLayoutPassesCodeZero()
        => Assert.Equal("0", LayoutCodes(EmitCSharp("X")));
}
