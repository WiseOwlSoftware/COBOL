// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN ARGUMENT THE BINDER HOLDS FOR §8.4.3.2.4 GR2's ORDER KEEPS THE DESCRIPTION THE UNHELD ARGUMENT HAS (kb/Work PB4124).
/// GR2 "evaluates the arguments individually in the order specified ... from left to right" and says nothing else, so a later
/// argument that activates a user-defined function makes the binder hold each earlier argument in a compiler temporary
/// (<c>ArgumentOrder.Window.Settle</c>), and the call must answer what it answers without the activation. The selecting
/// functions (MAX, MIN, MEDIAN, ABS, MEAN) return a COPY of an argument's item (§15.59.4, §15.63.4, §15.61.4; docs/CONFORMANCE.md
/// DOC-A.1-92), so what <c>DISPLAY</c>, <c>FUNCTION LENGTH</c> and <c>FUNCTION BYTE-LENGTH</c> show of the result is the
/// selected argument's description; before the fix the held copy was described by the temporary it was held in (8 digits for
/// <c>A * B</c>, 38 for a nested function), so <c>LENGTH (MAX (A * B, 3))</c> was 4 and <c>LENGTH (MAX (A * B, UDF (A)))</c> 8.
/// <para>The assertion is the AGREEMENT of the two forms for every argument shape the description is derived from - an arithmetic
/// expression, a data item, a literal, a nested selecting function (which keeps the item it copied), DATE-OF-INTEGER (fixed digits),
/// and the functions described by their value - not a number the standard leaves to the implementor (§15.4.1).</para>
/// </summary>
public sealed class HeldFunctionArgumentDescriptionTests : WiseOwlCobolTestBase
{
    /// <summary>Argument shapes, each with whether its value is negative (a MAX against the 0 filler would select the filler).</summary>
    private static readonly (string Name, string Argument, bool Negative)[] Shapes =
    [
        ("expr", "WS-A * WS-B", false),
        ("exprbig", "WS-C * WS-C * WS-C", false),
        ("exprdiv", "WS-C / 4", false),
        ("exprneg", "0 - WS-A", true),
        ("nestmax", "FUNCTION MAX(WS-C, 5)", false),
        ("nestmaxlit", "FUNCTION MAX(WS-Z, 5)", false),
        ("nestabs", "FUNCTION ABS(WS-S)", false),
        ("nestmin", "FUNCTION MIN(WS-C, WS-A * WS-B)", false),
        ("nest2", "FUNCTION MAX(FUNCTION MAX(WS-C, 5), 6)", false),
        ("nestabsmax", "FUNCTION ABS(FUNCTION MAX(WS-S, WS-A))", false),
        ("int", "FUNCTION INTEGER(WS-S)", true),
        ("sqrt", "FUNCTION SQRT(WS-C)", false),
        ("dateof", "FUNCTION DATE-OF-INTEGER(WS-D)", false),
        ("field", "WS-C", false),
        ("sfield", "WS-S", true),
        ("median", "FUNCTION MEDIAN(WS-C, 5, 7)", false),
        ("mean1", "FUNCTION MEAN(WS-C)", false),
        ("mean3", "FUNCTION MEAN(WS-C, 5, 7)", false),
        ("numval", "FUNCTION NUMVAL(\"12.5\")", false),
        ("plus", "WS-A * WS-B + WS-S", false),
        ("lit", "21", false),
        ("litneg", "-21.50", true),
        // kb/Work PB4162: a table(ALL) argument is held as a table of its elements, and a nested call naming one is held as the call it is
        ("all", "WS-TA (ALL)", false),
        ("allmax", "FUNCTION MAX(WS-TA (ALL), 5)", false),
        ("allmax2", "FUNCTION MAX(FUNCTION MAX(WS-TA (ALL), 5), 6)", false),
        ("allsum", "FUNCTION SUM(WS-TA (ALL))", false),
        ("allmean", "FUNCTION MEAN(WS-TA (ALL))", false),
        ("allabs", "FUNCTION ABS(FUNCTION MIN(WS-TB (ALL)))", false),
    ];

    /// <summary>The outer selection, the filler that never wins it (a literal, and the same value held behind an activation), and
    /// whether it can select a negative argument.</summary>
    private static readonly (string Name, string Template, string Literal, string Item, bool SelectsNegative)[] Outers =
    [
        ("MAX", "MAX({0}, {1})", "0", "WS-LO", false),
        ("MIN", "MIN({0}, {1})", "99999999", "WS-HI", true),
        ("MEDIAN", "MEDIAN({0}, 0, {1})", "99999999", "WS-HI", true),
    ];

    private const string Header = """
        IDENTIFICATION DIVISION.
        FUNCTION-ID. HFADBUMP.
        DATA DIVISION.
        LINKAGE SECTION.
        01 L-X PIC 9(8).
        01 L-R PIC 9(8).
        PROCEDURE DIVISION USING L-X RETURNING L-R.
        P.
            MOVE L-X TO L-R.
            GOBACK.
        END FUNCTION HFADBUMP.
        IDENTIFICATION DIVISION.
        PROGRAM-ID. HFADMAIN.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            FUNCTION HFADBUMP
            FUNCTION ALL INTRINSIC.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-A PIC 9(4) VALUE 7.
        01 WS-B PIC 9(4) VALUE 3.
        01 WS-C PIC 9(8) VALUE 21.
        01 WS-D PIC 9(8) VALUE 100000.
        01 WS-S PIC S9(3)V99 VALUE -12.50.
        01 WS-Z PIC 9(4) VALUE 0.
        01 WS-LO PIC 9(8) VALUE 0.
        01 WS-HI PIC 9(8) VALUE 99999999.
        01 WS-N PIC 9(4) VALUE 0.
        01 WS-TA-REC.
           05 WS-TA PIC 9(8) OCCURS 3 VALUE 21.
        01 WS-TB-REC.
           05 WS-TB PIC S9(3)V99 OCCURS 2 VALUE -12.50.
        PROCEDURE DIVISION.
        MAIN.

        """;

    /// <summary>The labels of the pairs that are written, in source order.</summary>
    private static (string Source, List<string> Labels) Matrix()
    {
        var source = new StringBuilder(Header);
        var labels = new List<string>();
        foreach (var (name, argument, negative) in Shapes)
            foreach (var (outer, template, literal, item, selectsNegative) in Outers)
            {
                if (negative && !selectsNegative) continue;   // MAX (-x, 0) selects the filler, whose item differs by construction
                labels.Add($"{name}-{outer}");
                foreach (string kind in new[] { "U", "F" })
                {
                    string filler = kind == "U" ? literal : $"FUNCTION HFADBUMP({item})";
                    string call = "FUNCTION " + string.Format(template, argument, filler);
                    source.AppendLine($"    DISPLAY \"{name}-{outer}-D-{kind} \" {call}");
                    source.AppendLine($"    MOVE FUNCTION LENGTH({call}) TO WS-N");
                    source.AppendLine($"    DISPLAY \"{name}-{outer}-L-{kind} \" WS-N");
                    source.AppendLine($"    MOVE FUNCTION BYTE-LENGTH({call}) TO WS-N");
                    source.AppendLine($"    DISPLAY \"{name}-{outer}-B-{kind} \" WS-N");
                }
            }
        source.AppendLine("    STOP RUN.");
        source.AppendLine("END PROGRAM HFADMAIN.");
        return (source.ToString(), labels);
    }

    /// <summary>⛔ A FATAL EXCEPTION OF AN EARLIER ARGUMENT IS RAISED BEFORE A LATER ARGUMENT'S ACTIVATION (kb/Work PB4124, review
    /// round 1). ISO §8.4.3.2.4 GR2 evaluates the arguments left to right; DATE-OF-INTEGER (0) raises EC-ARGUMENT-FUNCTION
    /// (§14.6.13.1.3), which is fatal (§14.6.13.1.6), so the user-defined function in argument-2 must never run. The nested call
    /// is described by its fixed digits, which no argument decides, so it is held AS A VALUE at its place and not read after
    /// the activation by holding only its (stable) arguments.</summary>
    [Theory]
    [InlineData("FUNCTION DATE-OF-INTEGER(WS-D)")]
    [InlineData("FUNCTION DATE-OF-INTEGER(0)")]
    [InlineData("FUNCTION DAY-OF-INTEGER(WS-D)")]
    public void AFatalExceptionOfAnEarlierArgumentIsRaisedBeforeALaterActivation(string earlier)
    {
        string source = $$"""
            >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
            IDENTIFICATION DIVISION.
            FUNCTION-ID. HFADRAN.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L-X PIC 9(8).
            01 L-R PIC 9(8).
            PROCEDURE DIVISION USING L-X RETURNING L-R.
            P.
                DISPLAY "UDF RAN"
                MOVE L-X TO L-R.
                GOBACK.
            END FUNCTION HFADRAN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HFADORD.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION HFADRAN
                FUNCTION ALL INTRINSIC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-D PIC S9(8) VALUE 0.
            01 WS-C PIC 9(8) VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "START"
                DISPLAY FUNCTION MAX({{earlier}}, FUNCTION HFADRAN(WS-C))
                DISPLAY "END"
                STOP RUN.
            END PROGRAM HFADORD.
            """;
        var (ok, stdout, _) = CompileAndRun(source, dialectLevel: 2023);
        Assert.False(ok);   // the fatal exception ended the run
        Assert.Equal("START", stdout);   // neither the activation ("UDF RAN") nor the DISPLAY after it ran
    }

    [Fact]
    public void AHeldArgumentIsDescribedAsTheUnheldArgumentIs()
    {
        var (source, labels) = Matrix();
        var (ok, stdout, detail) = CompileAndRun(source, dialectLevel: 2023);
        Assert.True(ok, detail);

        var shown = stdout.Split("\r\n").Select(line => line.Split(' ', 2)).Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);
        Assert.Equal(labels.Count * 6, shown.Count);   // DISPLAY, LENGTH and BYTE-LENGTH, unheld and held: none vacuous
        foreach (string label in labels)
            foreach (string measure in new[] { "D", "L", "B" })
            {
                string[] parts = label.Split('-');
                string unheld = shown[$"{parts[0]}-{parts[1]}-{measure}-U"];
                string held = shown[$"{parts[0]}-{parts[1]}-{measure}-F"];
                Assert.True(unheld == held, $"{label} {measure}: unheld '{unheld}', held behind an activation '{held}'");
            }
    }
}
