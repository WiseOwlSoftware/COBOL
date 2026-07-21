// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> migration-flagging subsystem (ISO §7.3.14 / §7.3.15;
/// Increment 0 — the core + the two syntactic detectors READ-PREVIOUS and I-O-STATUS-07/CLOSE). Covers the ONE
/// directive-line parser (<see cref="FlagDirectiveLine"/>), the per-option source-line fold
/// (<see cref="FlagState"/>), and the end-to-end warning emission through <see cref="CompilerDriver"/>. Design
/// SSOT: <c>docs/rearchitecture/DESIGN-flag-directives.md</c>.
/// </summary>
public sealed class FlagDirectiveTests
{
    // ── The directive-line parser (§7.3.14.2 / §7.3.15.2) ───────────────────────────────────────────────────

    [Fact]
    public void Parse_Flag14_SingleOption_On()
    {
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.Flag14, "READ-PREVIOUS ON",
            out var options, out bool on, out string? error));
        Assert.Null(error);
        Assert.True(on);
        Assert.Equal([FlagOption.Flag14ReadPrevious], options);
    }

    [Fact]
    public void Parse_Flag14_MultipleOptions_AnyOrder_Off()
    {
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.Flag14, "EVALUATE READ-PREVIOUS OFF",
            out var options, out bool on, out _));
        Assert.False(on);
        Assert.Contains(FlagOption.Flag14ReadPrevious, options);
        Assert.Contains(FlagOption.Flag14Evaluate, options);
    }

    [Fact]
    public void Parse_All_YieldsEmptyOptionSet_TheFanOutMarker()
    {
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.Flag14, "ALL ON", out var options, out bool on, out _));
        Assert.True(on);
        Assert.Empty(options);   // empty ⇒ ALL fan-out at fold time
    }

    [Fact]
    public void Parse_Flag02_OnIsImplicit_WhenOnOffOmitted()
    {
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.Flag02, "I-O-STATUS-07",
            out var options, out bool on, out _));
        Assert.True(on);   // FLAG-02: ON is the implicit default (§7.3.14.2)
        Assert.Equal([FlagOption.Flag02IoStatus07], options);
    }

    [Fact]
    public void Parse_Flag14_MissingOnOff_IsError()
    {
        Assert.False(FlagDirectiveLine.TryParse(FlagDirective.Flag14, "READ-PREVIOUS", out _, out _, out string? error));
        Assert.NotNull(error);   // FLAG-14 requires the ON/OFF choice
    }

    [Fact]
    public void Parse_UnknownOption_IsError()
    {
        Assert.False(FlagDirectiveLine.TryParse(FlagDirective.Flag14, "BOGUS ON", out _, out _, out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Parse_AllCombinedWithOption_IsError()
    {
        Assert.False(FlagDirectiveLine.TryParse(FlagDirective.Flag14, "ALL READ-PREVIOUS ON", out _, out _, out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Parse_SameWord_ResolvesPerDirective()
    {
        // I-O-STATUS-07 is a valid option of BOTH directives, with different meanings — the parse is scoped.
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.Flag14, "I-O-STATUS-07 ON", out var f14, out _, out _));
        Assert.Equal([FlagOption.Flag14IoStatus07], f14);
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.Flag02, "I-O-STATUS-07 ON", out var f02, out _, out _));
        Assert.Equal([FlagOption.Flag02IoStatus07], f02);
    }

    // ── The per-option source-line fold (§7.3.14.4 / §7.3.15.4 GR2/GR3/GR5) ─────────────────────────────────

    [Fact]
    public void Fold_DefaultsOff_AndIsStickyForwardStrictlyAfterTheDirective()
    {
        var state = FlagState.Build([new FlagEvent(5, FlagDirective.Flag14, true, [FlagOption.Flag14ReadPrevious])]);
        Assert.False(state.IsOnAt(4, FlagOption.Flag14ReadPrevious));   // before the directive
        Assert.False(state.IsOnAt(5, FlagOption.Flag14ReadPrevious));   // ON the directive line — strict < (applies to FOLLOWING text)
        Assert.True(state.IsOnAt(6, FlagOption.Flag14ReadPrevious));    // after
    }

    [Fact]
    public void Fold_All_FansOutToEveryOptionOfThatDirectiveOnly()
    {
        var state = FlagState.Build([new FlagEvent(3, FlagDirective.Flag14, true, [])]);   // ALL ON
        Assert.True(state.IsOnAt(5, FlagOption.Flag14ReadPrevious));
        Assert.True(state.IsOnAt(5, FlagOption.Flag14ValueZero));
        Assert.False(state.IsOnAt(5, FlagOption.Flag02IoStatus07));   // the OTHER directive is untouched
    }

    [Fact]
    public void Fold_AllOff_ResetsAPreviouslyEnabledOption()
    {
        var state = FlagState.Build(
        [
            new FlagEvent(2, FlagDirective.Flag14, true, [FlagOption.Flag14ReadPrevious]),
            new FlagEvent(4, FlagDirective.Flag14, false, []),   // ALL OFF — the GR2 reset
        ]);
        Assert.True(state.IsOnAt(3, FlagOption.Flag14ReadPrevious));
        Assert.False(state.IsOnAt(5, FlagOption.Flag14ReadPrevious));
    }

    [Fact]
    public void Fold_OtherDirectiveEvent_DoesNotAffectThisOption()
    {
        var state = FlagState.Build([new FlagEvent(3, FlagDirective.Flag02, true, [])]);   // FLAG-02 ALL ON
        Assert.False(state.IsOnAt(5, FlagOption.Flag14ReadPrevious));
    }

    // ── End-to-end: the warning emits through the compiler (Increment 0 detectors) ──────────────────────────

    private const string ReadPreviousProgram =
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. FLAGRP.\n" +
        "       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n" +
        "       FILE-CONTROL.\n" +
        "           SELECT F ASSIGN TO \"f.dat\"\n" +
        "               ORGANIZATION IS INDEXED\n" +
        "               ACCESS MODE IS DYNAMIC\n" +
        "               RECORD KEY IS F-KEY.\n" +
        "       DATA DIVISION.\n" +
        "       FILE SECTION.\n" +
        "       FD F.\n" +
        "       01 F-REC.\n" +
        "          05 F-KEY PIC X(4).\n" +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN.\n" +
        "           OPEN INPUT F.\n" +
        "{DIRECTIVE}" +
        "           READ F PREVIOUS RECORD\n" +
        "               AT END CONTINUE\n" +
        "           END-READ.\n" +
        "           CLOSE F.\n" +
        "           STOP RUN.\n";

    private static IReadOnlyList<string> CompileWarnings(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Flag_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "flag.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "flag.dll"), DialectLevel: 2023, CheckOnly: true));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            return r.Warnings;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    [Fact]
    public void Compile_Flag14ReadPreviousOn_EmitsCobolnet1621()
    {
        var warnings = CompileWarnings(ReadPreviousProgram.Replace("{DIRECTIVE}", "       >>FLAG-14 READ-PREVIOUS ON\n"));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_NoDirective_EmitsNoFlagWarning()
    {
        var warnings = CompileWarnings(ReadPreviousProgram.Replace("{DIRECTIVE}", ""));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_Flag14ReadPreviousOff_EmitsNoFlagWarning()
    {
        var warnings = CompileWarnings(ReadPreviousProgram.Replace("{DIRECTIVE}", "       >>FLAG-14 READ-PREVIOUS OFF\n"));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_Flag02IoStatus07On_EmitsCobolnet1620_OnCloseNoRewind()
    {
        // A CLOSE WITH NO REWIND under >>FLAG-02 I-O-STATUS-07 ON is flagged (§7.3.14.4 GR4 c).
        string program = ReadPreviousProgram
            .Replace("{DIRECTIVE}", "")
            .Replace("           CLOSE F.\n", "       >>FLAG-02 I-O-STATUS-07 ON\n           CLOSE F WITH NO REWIND.\n");
        var warnings = CompileWarnings(program);
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1620", StringComparison.Ordinal));
    }

    // ── Incr 1a: the VALUE-clause data options g NUM-ED-ZERO-FIGCONST + l VALUE-ZERO (§7.3.15.4 GR4 g/l) ──

    private static string ValueProgram(string directives, string entry) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGVAL.\n       DATA DIVISION.\n" +
        "       WORKING-STORAGE SECTION.\n" + directives + "       " + entry + "\n" +
        "       PROCEDURE DIVISION.\n       MAIN.\n           STOP RUN.\n";

    [Fact]
    public void Compile_NumEdZeroFigconstOn_Flags_NumericEditedValueZero()
    {
        var warnings = CompileWarnings(ValueProgram(
            "       >>FLAG-14 NUM-ED-ZERO-FIGCONST ON\n", "01 NE PIC ZZ9.99 VALUE ZERO."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("NUM-ED-ZERO-FIGCONST", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueZeroOn_Flags_NumericEditedValueZero()
    {
        var warnings = CompileWarnings(ValueProgram(
            "       >>FLAG-14 VALUE-ZERO ON\n", "01 NE PIC ZZ9.99 VALUE ZERO."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("VALUE-ZERO", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_NumEdZero_NotFlagged_OnPlainNumericItem()
    {
        // PIC 999 is category numeric (NOT numeric-edited) — GR4 g/l do not reach it.
        var warnings = CompileWarnings(ValueProgram(
            "       >>FLAG-14 NUM-ED-ZERO-FIGCONST ON\n", "01 PLAIN PIC 999 VALUE ZERO."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_NumEdZero_NotFlagged_WhenValueIsNumericLiteral()
    {
        // A numeric-edited item whose VALUE is a numeric literal (not the figurative ZERO) is not flagged.
        var warnings = CompileWarnings(ValueProgram(
            "       >>FLAG-14 NUM-ED-ZERO-FIGCONST ON\n", "01 NE PIC ZZ9 VALUE 5."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }
}
