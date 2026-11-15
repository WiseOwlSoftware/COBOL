// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// kb/Work PB5600 — the <c>&gt;&gt;FLAG-85</c> (§2002:7.2.11) and <c>&gt;&gt;FLAG-NATIVE-ARITHMETIC</c> (§2002:7.2.12)
/// directives: the operand parse (<see cref="FlagDirectiveLine"/>), the per-option fold (<see cref="FlagState"/>), and one
/// positive and one negative witness per GR4 sub-rule through <see cref="CompilerDriver"/> at <c>--std 2002</c>, where the
/// warning mechanism of §2002:7.2.11.3 GR1 / §2002:7.2.12.3 GR1 is required. Each expected verdict is the printed rule's.
/// </summary>
public sealed class Flag85DirectiveTests
{
    // ── The operand (§2002:7.2.11.1: { ALL | |option ...| } { ON | OFF }; §2002:7.2.12.1: { ON | OFF }) ─────────────────

    [Theory]
    [InlineData("", true, 0)]                       // bare: ALL ON (both un-underlined, §2002:5.1.5.2)
    [InlineData("ALL", true, 0)]
    [InlineData("OFF", false, 0)]                   // a lone OFF is ALL OFF
    [InlineData("NUMVAL", true, 1)]                 // an option list with ON omitted
    [InlineData("NUMVAL OFF", false, 1)]
    [InlineData("MOVE SET zero-length ON", true, 3)] // any order, Annex C case fold
    [InlineData("CORRESPONDING DE-EDITING DIVIDE FUNCTION-ARGUMENT MOVE NUMVAL SET STANDARD-1 STANDARD-2 ZERO-LENGTH", true, 10)]
    public void Flag85_Operand_Parses(string operand, bool on, int optionCount)
    {
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.Flag85, operand, out var options, out bool parsedOn, out string? error), error);
        Assert.Equal(on, parsedOn);
        Assert.Equal(optionCount, options.Count);
    }

    [Theory]
    [InlineData("ALL ON JUNK")]
    [InlineData("NUMVAL NUMVAL")]
    [InlineData("ALL NUMVAL")]
    [InlineData("BOGUS")]
    [InlineData("ON NUMVAL")]
    [InlineData("ZERO-LENGTHS")]
    public void Flag85_Operand_IsRefused(string operand) =>
        Assert.False(FlagDirectiveLine.TryParse(FlagDirective.Flag85, operand, out _, out _, out string? error) || error is null);

    [Theory]
    [InlineData("", true)]
    [InlineData("ON", true)]
    [InlineData("OFF", false)]
    public void FlagNativeArithmetic_Operand_Parses(string operand, bool on)
    {
        Assert.True(FlagDirectiveLine.TryParse(FlagDirective.FlagNativeArithmetic, operand, out var options, out bool parsedOn, out _));
        Assert.Equal(on, parsedOn);
        Assert.Empty(options);
    }

    [Theory]
    [InlineData("ALL ON")]
    [InlineData("ALL")]
    [InlineData("NUMVAL")]
    [InlineData("FLAG-NATIVE-ARITHMETIC")]
    [InlineData("ON JUNK")]
    public void FlagNativeArithmetic_Operand_IsRefused(string operand) =>
        Assert.False(FlagDirectiveLine.TryParse(FlagDirective.FlagNativeArithmetic, operand, out _, out _, out string? error) || error is null);

    [Fact]
    public void TheFourDirectiveWords_ResolveToTheirDirective_AndRows()
    {
        foreach (var d in Enum.GetValues<FlagDirective>())
        {
            Assert.Equal(d, FlagDirectiveLine.OfWord(FlagDirectiveLine.DirectiveWord(d).ToLowerInvariant()));
            Assert.NotNull(WiseOwl.Cobol.Editions.ConstructRegistry.Find(FlagDirectiveLine.Row(d)));
            Assert.NotEmpty(FlagOptions.OptionsOf(d));
        }
        Assert.Null(FlagDirectiveLine.OfWord("FLAG-86"));
    }

    // ── The fold (§2002:7.2.11.3 GR2, GR3, GR5; §2002:7.2.12.3 GR2, GR3, GR5) ──────────────────────────────────────────

    [Fact]
    public void Fold_OffByDefault_OnAfterTheDirective_AllOffResets()
    {
        var s = FlagState.Build([
            new FlagEvent(2, FlagDirective.Flag85, true, [FlagOption.Flag85Numval]),
            new FlagEvent(6, FlagDirective.Flag85, false, [])]);   // ALL OFF: "turns off all flagging options"
        Assert.False(s.IsOnAt(1, FlagOption.Flag85Numval));        // GR5: the default is off
        Assert.False(s.IsOnAt(2, FlagOption.Flag85Numval));        // the directive's own line is not text that follows it
        Assert.True(s.IsOnAt(3, FlagOption.Flag85Numval));
        Assert.False(s.IsOnAt(3, FlagOption.Flag85Move));          // only the named option
        Assert.False(s.IsOnAt(7, FlagOption.Flag85Numval));
    }

    [Fact]
    public void Fold_TheFourDirectivesDoNotShareOptions()
    {
        var s = FlagState.Build([new FlagEvent(1, FlagDirective.FlagNativeArithmetic, true, [])]);
        Assert.True(s.IsOnAt(2, FlagOption.FlagNativeArithmeticOperation));
        Assert.All(FlagOptions.OptionsOf(FlagDirective.Flag85), o => Assert.False(s.IsOnAt(2, o)));
        Assert.All(FlagOptions.OptionsOf(FlagDirective.Flag14), o => Assert.False(s.IsOnAt(2, o)));
    }

    // ── The warning mechanism, end to end at --std 2002 (§2002:7.2.11.3 GR1, GR4; §2002:7.2.12.3 GR1, GR2) ──────────────

    private const string Data =
        "       01 N PIC 9(5).\n" +
        "       01 S PIC S9(5).\n" +
        "       01 US PIC 9(5).\n" +
        "       01 ED PIC ZZ,ZZ9.\n" +
        "       01 LONGA PIC X(40).\n" +
        "       01 SHORTA PIC X(31).\n" +
        "       01 T1.\n" +
        "          05 R1 OCCURS 3.\n" +
        "             10 TA PIC 9.\n" +
        "             10 TB1 PIC 9.\n" +
        "       01 T2.\n" +
        "          05 R2 OCCURS 3.\n" +
        "             10 TA PIC 9.\n" +
        "             10 TB1 PIC 9.\n" +
        "       01 I PIC 9 VALUE 1.\n" +
        "       01 FLG PIC X.\n" +
        "          88 FLG-ON VALUE \"A\".\n" +
        "       01 VFLG.\n" +
        "          88 VF-OK VALUE \"A\".\n" +
        "          05 VF-CNT PIC 9.\n" +
        "          05 VF-ITEM PIC X OCCURS 0 TO 5 DEPENDING ON VF-CNT.\n" +
        "       01 ZN PIC 9.\n" +
        "       01 ZG.\n" +
        "          05 ZT PIC X OCCURS 0 TO 3 DEPENDING ON ZN.\n" +
        "       01 NG.\n" +
        "          05 NT PIC X OCCURS 1 TO 3 DEPENDING ON ZN.\n" +
        "       01 B1 USAGE BINARY-LONG.\n" +
        "       01 F1 USAGE FLOAT-LONG.\n";

    private static string Program(string directive, string body, bool standard = false, string mode = "STANDARD") =>
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. PB5600T.\n" +
        (standard ? "       OPTIONS.\n           ARITHMETIC IS " + mode + ".\n" : "") +
        "       DATA DIVISION.\n" +
        "       WORKING-STORAGE SECTION.\n" + Data +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN.\n" +
        (directive.Length == 0 ? "" : "       " + directive + "\n") +
        "           " + body + "\n" +
        "           STOP RUN.\n";

    private static IReadOnlyList<string> Compile(string source, int dialect = 2002)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Flag85_");
        try
        {
            string src = Path.Combine(dir, "flag85.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "flag85.dll"), DialectLevel: dialect, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            return r.Warnings;
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>§2002:7.2.2 SR3: in fixed form "a compiler directive shall be written in the program-text area and may be
    /// followed only by space characters and an optional inline comment" — the same line in the area is accepted, a directive
    /// from column 1 (the sequence number and indicator areas) and a directive with a word after its format are refused.</summary>
    [Theory]
    [InlineData("       >>FLAG-85 ALL ON  *> an inline comment", true)]
    [InlineData(">>FLAG-85 ALL ON", false)]
    [InlineData("       >>FLAG-85 ALL ON JUNK", false)]
    [InlineData("       >>FLAG-NATIVE-ARITHMETIC ON JUNK", false)]
    public void FixedForm_DirectiveLine_FollowsSr3(string line, bool accepted)
    {
        string source = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB5600F.\n       PROCEDURE DIVISION.\n"
            + line + "\n           STOP RUN.\n";
        string dir = ScratchDirectory.Create("WiseOwlCobol_Flag85_");
        try
        {
            string src = Path.Combine(dir, "flag85.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "flag85.dll"), DialectLevel: 2002, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            Assert.Equal(accepted, r.Success);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static int Flags(string code, IReadOnlyList<string> warnings) =>
        warnings.Count(w => w.Contains("warning " + code, StringComparison.Ordinal));

    /// <summary>One GR4 sub-rule: the option's flagged statement draws exactly one COBOL7390 with the option's word and its
    /// GR4 citation under <c>&gt;&gt;FLAG-85 option</c>, none under OFF, none without the directive, and the control
    /// statement (what the sub-rule does NOT cover) none under ALL.</summary>
    [Theory]
    [InlineData("CORRESPONDING", "ADD CORRESPONDING R1 (I) TO R2 (2).", "ADD CORRESPONDING R1 (1) TO R2 (2).")]
    [InlineData("CORRESPONDING", "SUBTRACT CORR R1 (I) FROM R2 (2).", "SUBTRACT CORR R1 (1) FROM R2 (2).")]
    [InlineData("CORRESPONDING", "MOVE CORRESPONDING R1 (I) TO R2 (2).", "MOVE CORRESPONDING R1 (3) TO R2 (2).")]
    [InlineData("DE-EDITING", "MOVE ED TO N.", "MOVE N TO ED.")]
    [InlineData("DIVIDE", "DIVIDE S BY 3 GIVING US REMAINDER N.", "DIVIDE US BY 3 GIVING S REMAINDER N.")]
    [InlineData("DIVIDE", "DIVIDE 3 INTO S GIVING US REMAINDER N.", "DIVIDE US BY 3 GIVING US REMAINDER N.")]
    [InlineData("FUNCTION-ARGUMENT", "COMPUTE N = FUNCTION MAX(1 FUNCTION RANDOM (2)).", "COMPUTE N = FUNCTION RANDOM (2).")]
    [InlineData("FUNCTION-ARGUMENT", "COMPUTE N = FUNCTION MAX(1 FUNCTION RANDOM (2)).", "COMPUTE N = FUNCTION ABS(FUNCTION RANDOM (2)).")]
    [InlineData("MOVE", "MOVE LONGA TO N.", "MOVE SHORTA TO N.")]
    [InlineData("MOVE", "MOVE \"12345678901234567890123456789012\" TO N.", "MOVE \"1234567890123456789012345678901\" TO N.")]
    [InlineData("NUMVAL", "COMPUTE N = FUNCTION NUMVAL(SHORTA).", "COMPUTE N = FUNCTION MAX(1 2).")]
    [InlineData("NUMVAL", "COMPUTE N = FUNCTION NUMVAL-C(SHORTA).", "COMPUTE N = FUNCTION ABS(-3).")]
    [InlineData("SET", "SET VF-OK TO TRUE.", "SET FLG-ON TO TRUE.")]
    [InlineData("ZERO-LENGTH", "DISPLAY ZG.", "DISPLAY NG.")]
    [InlineData("ZERO-LENGTH", "DISPLAY ZG ZG.", "DISPLAY N.")]
    public void Flag85_EachOption_FlagsItsConstruct_AndOnlyThat(string option, string flagged, string control)
    {
        var on = Compile(Program($">>FLAG-85 {option} ON", flagged));
        Assert.Equal(1, Flags("COBOL7390", on));   // once per statement
        Assert.Contains(on, w => w.Contains($">>FLAG-85 {option}", StringComparison.Ordinal));
        Assert.Contains(on, w => w.Contains("ISO §2002:7.2.11.3 GR4", StringComparison.Ordinal) || w.Contains("(1985↔2002)", StringComparison.Ordinal));

        Assert.Equal(0, Flags("COBOL7390", Compile(Program($">>FLAG-85 {option} OFF", flagged))));   // GR3
        Assert.Equal(0, Flags("COBOL7390", Compile(Program("", flagged))));                           // GR5: off by default
        Assert.Equal(0, Flags("COBOL7390", Compile(Program(">>FLAG-85 ALL ON", control))));          // not what GR4 covers
        Assert.Equal(1, Flags("COBOL7390", Compile(Program(">>FLAG-85 ALL ON", flagged))));          // GR4 a: ALL applies every option
    }

    [Fact]
    public void Flag85_AnotherOptionDoesNotFlag()
    {
        Assert.Equal(0, Flags("COBOL7390", Compile(Program(">>FLAG-85 MOVE ON", "COMPUTE N = FUNCTION NUMVAL(SHORTA)."))));
    }

    [Fact]
    public void Flag85_AllOff_TurnsEveryOptionOff_GR2()
    {
        string source = Program(">>FLAG-85 ALL ON\n       >>FLAG-85 ALL OFF", "COMPUTE N = FUNCTION NUMVAL(SHORTA).");
        Assert.Equal(0, Flags("COBOL7390", Compile(source)));
    }

    [Fact]
    public void Flag85_Standard1AndStandard2_FlagTheirAlphabetClause()   // GR4 i, j
    {
        static string Source(string directive, string clause) =>
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB5600A.\n       ENVIRONMENT DIVISION.\n" +
            "       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n" +
            (directive.Length == 0 ? "" : "       " + directive + "\n") +
            "           " + clause + ".\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           STOP RUN.\n";
        var one = Compile(Source(">>FLAG-85 STANDARD-1 ON", "ALPHABET A1 IS STANDARD-1"));
        Assert.Equal(1, Flags("COBOL7390", one));
        Assert.Contains(one, w => w.Contains(">>FLAG-85 STANDARD-1", StringComparison.Ordinal));
        var two = Compile(Source(">>FLAG-85 STANDARD-2 ON", "ALPHABET A2 IS STANDARD-2"));
        Assert.Equal(1, Flags("COBOL7390", two));
        Assert.Contains(two, w => w.Contains(">>FLAG-85 STANDARD-2", StringComparison.Ordinal));
        Assert.Equal(0, Flags("COBOL7390", Compile(Source(">>FLAG-85 STANDARD-2 ON", "ALPHABET A1 IS STANDARD-1"))));
        Assert.Equal(0, Flags("COBOL7390", Compile(Source(">>FLAG-85 ALL ON", "ALPHABET A1 IS NATIVE"))));
        Assert.Equal(0, Flags("COBOL7390", Compile(Source(">>FLAG-85 ALL OFF", "ALPHABET A1 IS STANDARD-1"))));
    }

    [Fact]
    public void Flag85_ZeroLength_FlagsAReadThatCouldReturnAZeroLengthItem()   // GR4 k
    {
        static string Source(string varying) =>
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB5600R.\n       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT F ASSIGN TO \"zl.dat\".\n" +
            "       DATA DIVISION.\n       FILE SECTION.\n" +
            $"       FD F RECORD VARYING FROM {varying} TO 10 DEPENDING ON RL.\n" +
            "       01 FR PIC X(10).\n       WORKING-STORAGE SECTION.\n       01 RL PIC 99.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n           OPEN INPUT F.\n" +
            "       >>FLAG-85 ZERO-LENGTH ON\n" +
            "           READ F.\n           STOP RUN.\n";
        Assert.Equal(1, Flags("COBOL7390", Compile(Source("0"))));
        Assert.Equal(0, Flags("COBOL7390", Compile(Source("1"))));
    }

    /// <summary>§2002:7.2.12.3 GR2: ON, standard arithmetic in effect, an operation with an operand of a usage that
    /// takes native arithmetic (§2002:8.8.1.3) is flagged (COBOL7391); GR3: OFF flags nothing; GR4: under native
    /// arithmetic nothing is flagged; an operation with no such operand is not flagged.</summary>
    [Theory]
    [InlineData("ADD B1 TO N.")]
    [InlineData("SUBTRACT B1 FROM N.")]
    [InlineData("MULTIPLY B1 BY N.")]
    [InlineData("DIVIDE B1 INTO N.")]
    [InlineData("COMPUTE N = B1 + 1.")]
    [InlineData("COMPUTE N = F1 * 2.")]
    [InlineData("IF N > B1 + 2 DISPLAY \"X\" END-IF.")]
    public void FlagNativeArithmetic_FlagsAnOperationUsingNativeArithmetic_UnderStandardArithmetic(string operation)
    {
        Assert.Equal(1, Flags("COBOL7391", Compile(Program(">>FLAG-NATIVE-ARITHMETIC ON", operation, standard: true))));
        Assert.Equal(1, Flags("COBOL7391", Compile(Program(">>FLAG-NATIVE-ARITHMETIC", operation, standard: true))));   // ON is the default
        Assert.Equal(0, Flags("COBOL7391", Compile(Program(">>FLAG-NATIVE-ARITHMETIC OFF", operation, standard: true))));   // GR3
        Assert.Equal(0, Flags("COBOL7391", Compile(Program("", operation, standard: true))));                                // GR5
        Assert.Equal(0, Flags("COBOL7391", Compile(Program(">>FLAG-NATIVE-ARITHMETIC ON", operation, standard: false))));   // GR4: native is in effect
    }

    [Theory]
    [InlineData("ADD 1 TO N.")]
    [InlineData("COMPUTE N = N + 1.")]
    [InlineData("ADD US TO N.")]
    [InlineData("MOVE B1 TO N.")]          // a MOVE is no arithmetic operation
    public void FlagNativeArithmetic_DoesNotFlagAnOperationWithNoNativeOperand(string operation) =>
        Assert.Equal(0, Flags("COBOL7391", Compile(Program(">>FLAG-NATIVE-ARITHMETIC ON", operation, standard: true))));

    [Fact]
    public void FlagNativeArithmetic_IsASeparateDirective_FromFlag85_And_NamesOnlyItsDirective()
    {
        Assert.Equal(0, Flags("COBOL7391", Compile(Program(">>FLAG-85 ALL ON", "ADD B1 TO N.", standard: true))));
        Assert.Equal(0, Flags("COBOL7390", Compile(Program(">>FLAG-NATIVE-ARITHMETIC ON", "ADD B1 TO N.", standard: true))));
        // The message names the directive only: the format admits no operand word (review finding R1-3).
        Assert.DoesNotContain("FLAG-NATIVE-ARITHMETIC FLAG-NATIVE-ARITHMETIC", string.Join("\n",
            Compile(Program(">>FLAG-NATIVE-ARITHMETIC ON", "ADD B1 TO N.", standard: true))));
    }

    /// <summary>§2014:7.3.13.3 GR2 enables flagging only "If ARITHMETIC IS STANDARD has been specified or implied": under
    /// STANDARD-DECIMAL (STANDARD-BINARY is declined at bind, COBOL0806) no operation uses native arithmetic (§2014:8.8.1.2), so nothing is flagged at
    /// --std 2014 (review finding R1-1).</summary>
    [Theory]
    [InlineData("STANDARD-DECIMAL", 0)]
    [InlineData("STANDARD", 1)]
    public void FlagNativeArithmetic_At2014_FlagsOnlyUnderArithmeticIsStandard(string mode, int expected) =>
        Assert.Equal(expected, Flags("COBOL7391", Compile(
            Program(">>FLAG-NATIVE-ARITHMETIC ON", "COMPUTE N = B1 + 1.", standard: true, mode: mode), 2014)));
}
