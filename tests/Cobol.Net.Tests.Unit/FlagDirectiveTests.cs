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

    // ── Incr 3: FLAG-14 i REF-MOD-ZERO-LENGTH (§7.3.15.4 GR4 i) — a ref-mod flagged when the >>REF-MOD-ZERO-LENGTH
    //    directive is UNSPECIFIED at the site AND EC-BOUND-REF-MOD checking is on. ──

    private static string RefModProgram(string directives) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGI.\n       DATA DIVISION.\n" +
        "       WORKING-STORAGE SECTION.\n       01 W PIC X(5) VALUE \"HELLO\".\n       01 R PIC X(3).\n" +
        "       PROCEDURE DIVISION.\n       MAIN.\n" + directives + "           MOVE W(2:2) TO R.\n           STOP RUN.\n";

    [Fact]
    public void Compile_RefModZeroLengthOn_Flags_WhenUnspecifiedAndEcOn()
    {
        var warnings = CompileWarnings(RefModProgram(
            "       >>TURN EC-BOUND-REF-MOD CHECKING ON\n       >>FLAG-14 REF-MOD-ZERO-LENGTH ON\n"));
        Assert.Contains(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("REF-MOD-ZERO-LENGTH", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RefModZeroLength_NotFlagged_WhenDirectiveExplicitlyOff()
    {
        // >>REF-MOD-ZERO-LENGTH OFF makes the directive explicitly specified — GR4 i requires the UNSPECIFIED state.
        var warnings = CompileWarnings(RefModProgram(
            "       >>TURN EC-BOUND-REF-MOD CHECKING ON\n       >>REF-MOD-ZERO-LENGTH OFF\n       >>FLAG-14 REF-MOD-ZERO-LENGTH ON\n"));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RefModZeroLength_NotFlagged_WhenEcBoundRefModOff()
    {
        var warnings = CompileWarnings(RefModProgram("       >>FLAG-14 REF-MOD-ZERO-LENGTH ON\n"));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    // ── Incr 2: the frontend-inline options (emitted in ConditionalCompilationProcessor, never reach the bound tree) ──

    // c EVALUATE (§7.3.15.4 GR4 c) — a >>EVALUATE directive carrying both a >>WHEN and a >>WHEN OTHER.
    private static string EvaluateDirectiveProgram(string otherArm) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGEV.\n       PROCEDURE DIVISION.\n       MAIN.\n" +
        "       >>DEFINE N AS 1\n       >>FLAG-14 EVALUATE ON\n       >>EVALUATE N\n       >>WHEN 1\n" +
        "           DISPLAY \"ONE\".\n" + otherArm + "       >>END-EVALUATE\n           STOP RUN.\n";

    [Fact]
    public void Compile_EvaluateOn_Flags_DirectiveWithWhenAndWhenOther()
    {
        var warnings = CompileWarnings(EvaluateDirectiveProgram("       >>WHEN OTHER\n           DISPLAY \"OTHER\".\n"));
        // Frontend-inline warnings carry a path(line,col) prefix (unlike the bound-option edition warnings), so match
        // on the code + option rather than a leading "warning".
        Assert.Contains(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal)
            && w.Contains(">>FLAG-14 EVALUATE", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_Evaluate_NotFlagged_WithoutWhenOther()
    {
        var warnings = CompileWarnings(EvaluateDirectiveProgram(""));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    // b COMPILE-TIME-ARITHMETIC-EXPRESSIONS (§7.3.15.4 GR4 b) — a compile-time arithmetic expression with an operator.
    private static string DefineArithmeticProgram(string expr) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGB.\n       PROCEDURE DIVISION.\n       MAIN.\n" +
        "       >>FLAG-14 COMPILE-TIME-ARITHMETIC-EXPRESSIONS ON\n       >>DEFINE X AS " + expr + "\n           STOP RUN.\n";

    [Fact]
    public void Compile_CompileTimeArithmeticOn_Flags_ExpressionWithOperator()
    {
        var warnings = CompileWarnings(DefineArithmeticProgram("1 + 2"));
        Assert.Contains(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("COMPILE-TIME-ARITHMETIC-EXPRESSIONS", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_CompileTimeArithmetic_NotFlagged_ForSoleLiteral()
    {
        // A bare single literal is not an arithmetic expression — no operator, nothing to flag.
        var warnings = CompileWarnings(DefineArithmeticProgram("5"));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    // ── Incr 1b: FLAG-14 j VALUE-EDITING (§7.3.15.4 GR4 j) — numeric-edited VALUE literal without editing symbols ──

    [Fact]
    public void Compile_ValueEditingOn_Flags_NumericLiteralOnNumericEdited()
    {
        // A numeric literal on a numeric-edited item carries no editing symbols (editing is now auto-supplied).
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-EDITING ON\n", "01 A PIC ZZZ9 VALUE 5."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("VALUE-EDITING", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueEditingOn_Flags_NonnumericLiteralWithoutEditingSymbols()
    {
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-EDITING ON\n", "01 B PIC ZZZ9 VALUE \"0005\"."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("VALUE-EDITING", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueEditing_NotFlagged_WhenLiteralContainsEditingSymbols()
    {
        // "1,234" already carries an editing symbol (the comma) — the edited form, not flagged.
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-EDITING ON\n", "01 C PIC Z,ZZ9 VALUE \"1,234\"."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueEditing_NotFlagged_OnFigurativeConstant()
    {
        // A figurative constant is not "a literal" for j (it is g/l/k territory).
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-EDITING ON\n", "01 D PIC ZZZ9 VALUE ZERO."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    // ── Incr 1b: FLAG-14 k VALUE-FIG-CON-LENGTH (§7.3.15.4 GR4 k) — figurative VALUE on an item with no length ──

    [Fact]
    public void Compile_ValueFigConLengthOn_Flags_FigurativeValueNoPictureNoUsage()
    {
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-FIG-CON-LENGTH ON\n", "01 A VALUE SPACE."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("VALUE-FIG-CON-LENGTH", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueFigConLengthOn_Flags_UsageDisplayWithoutPicture()
    {
        // USAGE DISPLAY without a PICTURE still has no specified length.
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-FIG-CON-LENGTH ON\n", "01 E USAGE DISPLAY VALUE SPACE."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("VALUE-FIG-CON-LENGTH", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueFigConLength_NotFlagged_WhenPicturePresent()
    {
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-FIG-CON-LENGTH ON\n", "01 C PIC XXX VALUE SPACE."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueFigConLength_NotFlagged_OnLengthImplyingUsage()
    {
        // COMP-2 implies a fixed length (8 bytes) — the item's length is specified.
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-FIG-CON-LENGTH ON\n", "01 D USAGE COMP-2 VALUE ZERO."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_ValueFigConLength_NotFlagged_OnGroupItem()
    {
        // A group's figurative VALUE is filled to the subordinates' length (§13.18.63 SR13) — length is specified.
        var warnings = CompileWarnings(ValueProgram("       >>FLAG-14 VALUE-FIG-CON-LENGTH ON\n",
            "01 G VALUE SPACE.\n          05 GA PIC X."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    // ── Incr 1c: FLAG-14 m WRITE-END-OF-PAGE (§7.3.15.4 GR4 m) — WRITE + file has LINAGE + no AT EOP ──

    private static string WriteProgram(bool linage, string directive, string writeStmt) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGWR.\n       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT OUTF ASSIGN \"of.txt\".\n" +
        "       DATA DIVISION.\n       FILE SECTION.\n" +
        (linage ? "       FD OUTF\n           LINAGE IS 60 LINES.\n" : "       FD OUTF.\n") +
        "       01 OUT-REC PIC X(80).\n       PROCEDURE DIVISION.\n       MAIN.\n           OPEN OUTPUT OUTF.\n" +
        directive + "           " + writeStmt + "\n           CLOSE OUTF.\n           STOP RUN.\n";

    [Fact]
    public void Compile_WriteEndOfPageOn_Flags_WriteWithoutEopOnLinageFile()
    {
        var warnings = CompileWarnings(WriteProgram(linage: true,
            "       >>FLAG-14 WRITE-END-OF-PAGE ON\n", "WRITE OUT-REC."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1621", StringComparison.Ordinal)
            && w.Contains("WRITE-END-OF-PAGE", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_WriteEndOfPage_NotFlagged_WhenEopPhraseIsPresent()
    {
        var warnings = CompileWarnings(WriteProgram(linage: true,
            "       >>FLAG-14 WRITE-END-OF-PAGE ON\n", "WRITE OUT-REC AT END-OF-PAGE CONTINUE END-WRITE."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_WriteEndOfPage_NotFlagged_OnNonLinageFile()
    {
        // The file has no LINAGE clause, so a WRITE to it does not "allow" an END-OF-PAGE phrase (§14.9.51).
        var warnings = CompileWarnings(WriteProgram(linage: false,
            "       >>FLAG-14 WRITE-END-OF-PAGE ON\n", "WRITE OUT-REC."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1621", StringComparison.Ordinal));
    }

    // ── Incr 1c: FLAG-02 f TERMINATE-WITH-VARYING (§7.3.14.4 GR4 f) — TERMINATE of a report with a VARYING clause ──

    private static string ReportProgram(bool varying, string directive) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGT.\n       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT RPT ASSIGN \"r.rpt\".\n" +
        "       DATA DIVISION.\n       FILE SECTION.\n       FD RPT REPORT IS R.\n" +
        "       WORKING-STORAGE SECTION.\n       01 WS-SEQ PIC 9 VALUE 0.\n" +
        "       REPORT SECTION.\n       RD R.\n       01 DET TYPE DE.\n          02 LINE PLUS 1.\n" +
        (varying
            ? "             03 COLUMNS ARE 1 5 PIC Z9 SOURCE IS RV\n                VARYING RV FROM WS-SEQ BY 1.\n"
            : "             03 COLUMN 1 PIC Z9 SOURCE IS WS-SEQ.\n") +
        "       PROCEDURE DIVISION.\n       MAIN.\n           OPEN OUTPUT RPT.\n           INITIATE R.\n" +
        "           GENERATE DET.\n" + directive + "           TERMINATE R.\n           CLOSE RPT.\n           STOP RUN.\n";

    [Fact]
    public void Compile_TerminateWithVaryingOn_Flags_ReportContainingVarying()
    {
        var warnings = CompileWarnings(ReportProgram(varying: true, "       >>FLAG-02 TERMINATE-WITH-VARYING ON\n"));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1620", StringComparison.Ordinal)
            && w.Contains("TERMINATE-WITH-VARYING", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_TerminateWithVarying_NotFlagged_ReportWithoutVarying()
    {
        var warnings = CompileWarnings(ReportProgram(varying: false, "       >>FLAG-02 TERMINATE-WITH-VARYING ON\n"));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    // ── Incr 3: FLAG-02 d MOVE-TO-SAME-NAME (§7.3.14.4 GR4 d) — a MOVE whose sending and receiving operands are the
    //    SAME data description entry, when that DDE is (1) category alphanumeric-edited, or (2) has a subordinate
    //    OCCURS…DEPENDING whose DEPENDING item is subordinate to it. ──

    private static string MoveProgram(string dataEntries, string directive, string moveStmt) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGMV.\n       DATA DIVISION.\n" +
        "       WORKING-STORAGE SECTION.\n" + dataEntries +
        "       PROCEDURE DIVISION.\n       MAIN.\n" + directive + "           " + moveStmt + "\n           STOP RUN.\n";

    [Fact]
    public void Compile_MoveToSameNameOn_Flags_AlphanumericEditedSelfMove()
    {
        // MOVE AE TO AE where AE is alphanumeric-edited (PIC X(3)BX(3), a 'B' insertion) — GR4 d condition (1).
        var warnings = CompileWarnings(MoveProgram(
            "       01 AE PIC X(3)BX(3).\n", "       >>FLAG-02 MOVE-TO-SAME-NAME ON\n", "MOVE AE TO AE."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1620", StringComparison.Ordinal)
            && w.Contains("MOVE-TO-SAME-NAME", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_MoveToSameNameOn_Flags_SubordinateOdoSelfMove()
    {
        // MOVE G TO G where G has a subordinate OCCURS…DEPENDING ON CNT and CNT is subordinate to G — GR4 d (2).
        var warnings = CompileWarnings(MoveProgram(
            "       01 G.\n          05 CNT PIC 9.\n          05 T OCCURS 1 TO 5 DEPENDING ON CNT PIC X.\n",
            "       >>FLAG-02 MOVE-TO-SAME-NAME ON\n", "MOVE G TO G."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1620", StringComparison.Ordinal)
            && w.Contains("MOVE-TO-SAME-NAME", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_MoveToSameName_NotFlagged_PlainAlphanumericSelfMove()
    {
        // MOVE PL TO PL where PL is a plain alphanumeric item (no edit mask, no subordinate ODO) — neither GR4 d arm.
        var warnings = CompileWarnings(MoveProgram(
            "       01 PL PIC X(7).\n", "       >>FLAG-02 MOVE-TO-SAME-NAME ON\n", "MOVE PL TO PL."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_MoveToSameName_NotFlagged_DifferentDescriptionEntries()
    {
        // MOVE AE TO AF — two DISTINCT (though identically-described) DDEs are not "the same data description entry".
        var warnings = CompileWarnings(MoveProgram(
            "       01 AE PIC X(3)BX(3).\n       01 AF PIC X(3)BX(3).\n",
            "       >>FLAG-02 MOVE-TO-SAME-NAME ON\n", "MOVE AE TO AF."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_MoveToSameName_NotFlagged_OdoDependingItemOutsideTheGroup()
    {
        // The DEPENDING item CNT is declared OUTSIDE G (§13.18.38 SR20 permits this), so it is NOT subordinate to
        // the moved DDE — GR4 d (2) requires the DEPENDING item be subordinate to the DDE.
        var warnings = CompileWarnings(MoveProgram(
            "       01 CNT PIC 9.\n       01 G.\n          05 T OCCURS 1 TO 5 DEPENDING ON CNT PIC X.\n",
            "       >>FLAG-02 MOVE-TO-SAME-NAME ON\n", "MOVE G TO G."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_MoveToSameName_NotFlagged_WhenDirectiveOff()
    {
        var warnings = CompileWarnings(MoveProgram(
            "       01 AE PIC X(3)BX(3).\n", "       >>FLAG-02 MOVE-TO-SAME-NAME OFF\n", "MOVE AE TO AE."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    // ── Incr 3: FLAG-02 e RANGE-EXCEPTION-FOR-INDEX (§7.3.14.4 GR4 e) — a Format-1 index-assignment (SET … TO) or
    //    Format-2 index-arithmetic (SET … UP/DOWN BY) whose receiver is an INDEX-NAME, flagged when EC-RANGE-INDEX
    //    checking is enabled. Only index-names range-check (§14.9.39.4 Format-1 GR2b copies a class-index DATA item
    //    unchanged), so a USAGE INDEX data item / plain numeric receiver is NOT flagged. ──

    private static string SetIndexProgram(string extraEntries, string directive, string setStmt) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. FLGSI.\n       DATA DIVISION.\n" +
        "       WORKING-STORAGE SECTION.\n       01 T.\n          05 E OCCURS 5 TIMES INDEXED BY IDX PIC X.\n" +
        extraEntries + "       PROCEDURE DIVISION.\n       MAIN.\n" + directive + "           " + setStmt +
        "\n           STOP RUN.\n";

    [Fact]
    public void Compile_RangeExceptionForIndexOn_Flags_SetIndexNameTo_WhenEcOn()
    {
        var warnings = CompileWarnings(SetIndexProgram("",
            "       >>TURN EC-RANGE-INDEX CHECKING ON\n       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX ON\n", "SET IDX TO 3."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1620", StringComparison.Ordinal)
            && w.Contains("RANGE-EXCEPTION-FOR-INDEX", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RangeExceptionForIndexOn_Flags_SetIndexNameUpBy_WhenEcOn()
    {
        var warnings = CompileWarnings(SetIndexProgram("",
            "       >>TURN EC-RANGE-INDEX CHECKING ON\n       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX ON\n", "SET IDX UP BY 1."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1620", StringComparison.Ordinal)
            && w.Contains("RANGE-EXCEPTION-FOR-INDEX", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RangeExceptionForIndexOn_Flags_ViaEcAllHierarchy()
    {
        // >>TURN EC-ALL CHECKING ON enables the level-3 EC-RANGE-INDEX through the exception hierarchy.
        var warnings = CompileWarnings(SetIndexProgram("",
            "       >>TURN EC-ALL CHECKING ON\n       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX ON\n", "SET IDX TO 3."));
        Assert.Contains(warnings, w => w.StartsWith("warning COBOLNET1620", StringComparison.Ordinal)
            && w.Contains("RANGE-EXCEPTION-FOR-INDEX", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RangeExceptionForIndex_NotFlagged_WhenEcRangeIndexOff()
    {
        // GR4 e fires only when EC-RANGE-INDEX checking is enabled (default OFF) — no >>TURN, no flag.
        var warnings = CompileWarnings(SetIndexProgram("",
            "       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX ON\n", "SET IDX TO 3."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RangeExceptionForIndex_NotFlagged_OnUsageIndexDataItemReceiver()
    {
        // A class-index DATA item (USAGE INDEX) receiver copies its value UNCHANGED (§14.9.39.4 Format-1 GR2b) and
        // never raises EC-RANGE-INDEX, so it is NOT flagged — only an index-NAME receiver range-checks.
        var warnings = CompileWarnings(SetIndexProgram("       01 IXD USAGE INDEX.\n",
            "       >>TURN EC-RANGE-INDEX CHECKING ON\n       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX ON\n", "SET IXD TO IDX."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RangeExceptionForIndex_NotFlagged_OnPlainNumericReceiver()
    {
        // SET of a plain numeric item is not an index-assignment into an index — no flag.
        var warnings = CompileWarnings(SetIndexProgram("       01 N PIC 9(4).\n",
            "       >>TURN EC-RANGE-INDEX CHECKING ON\n       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX ON\n", "SET N TO 3."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }

    [Fact]
    public void Compile_RangeExceptionForIndex_NotFlagged_WhenDirectiveOff()
    {
        var warnings = CompileWarnings(SetIndexProgram("",
            "       >>TURN EC-RANGE-INDEX CHECKING ON\n       >>FLAG-02 RANGE-EXCEPTION-FOR-INDEX OFF\n", "SET IDX TO 3."));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOLNET1620", StringComparison.Ordinal));
    }
}
