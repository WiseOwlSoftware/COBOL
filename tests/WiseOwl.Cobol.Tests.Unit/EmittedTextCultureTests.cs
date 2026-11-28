// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The generated C# is the same text on every compiling host, whatever its culture (kb/Work PB3239). The emitters interpolate
/// integers into C# source, and some are negative: the presence slot of an unconditional rolled SUM total is −1
/// (§2023:13.18.54.4 GR11). A culture whose negative sign is U+2212 MINUS SIGN (sv-SE on .NET 10) wrote that character into
/// the source, which is no C# token (CS1056), so the program failed to compile on such a host. <c>CompilerDriver.Compile</c>
/// now runs the backend under <c>InvariantCultureScope</c>. The culture here is a clone of the invariant culture whose negative
/// sign is U+2212, so the test means the same on every host, ICU or not.
/// </summary>
public sealed class EmittedTextCultureTests : WiseOwlCobolTestBase
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. CULTRS01.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT PRT ASSIGN TO "cultrs01.txt".
               DATA DIVISION.
               FILE SECTION.
               FD PRT REPORT IS R.
               WORKING-STORAGE SECTION.
               01 K PIC X VALUE "A".
               01 V PIC 99 VALUE 1.
               REPORT SECTION.
               RD R CONTROLS ARE FINAL K.
               01 D TYPE DETAIL LINE PLUS 1.
                  05 COLUMN 1 PIC 99 SOURCE V.
               01 TYPE CONTROL FOOTING K LINE PLUS 1.
                  05 KS COLUMN 1 PIC 999 SUM V.
               01 TYPE CONTROL FOOTING FINAL LINE PLUS 1.
                  05 COLUMN 1 PIC 9999 SUM KS.
               PROCEDURE DIVISION.
                   OPEN OUTPUT PRT
                   INITIATE R
                   GENERATE D
                   TERMINATE R
                   CLOSE PRT
                   STOP RUN.
            """;

    [Fact]
    public void GeneratedSource_IsTheSame_UnderACultureWithAUnicodeMinusSign()
    {
        string invariant = Generate("invariant", CultureInfo.InvariantCulture);
        Assert.Contains(", -1,", invariant, StringComparison.Ordinal);   // the program reaches a negative interpolation

        var minus = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        minus.NumberFormat.NegativeSign = "−";
        string underMinus = Generate("minus", minus);

        Assert.DoesNotContain('−', underMinus);
        Assert.Equal(invariant, underMinus);
    }

    private string Generate(string tag, CultureInfo culture)
    {
        string dir = Path.Combine(TempDir, tag);
        Directory.CreateDirectory(dir);
        string srcPath = Path.Combine(dir, "cultrs01.cob");
        File.WriteAllText(srcPath, Source);
        var host = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, EditionInfo.Of(2023), SourceFormat: TestSourceFormat.Inline));
            Assert.True(result.Success, string.Join("\n", result.Errors));
            return File.ReadAllText(result.GeneratedCsPath!).Replace(dir, "<dir>", StringComparison.Ordinal);
        }
        finally { CultureInfo.CurrentCulture = host; }
    }
}
