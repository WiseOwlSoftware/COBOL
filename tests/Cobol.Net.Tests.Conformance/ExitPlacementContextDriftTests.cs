// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE §14.9.14.3 PLACEMENT MATRIX — the drift net under kb/Work PB403's mechanism: every EXIT format's
/// enclosing-CONTEXT rule, asked of the ONE bind-position probe (<c>BinderContext.Enclosing</c>), with the
/// expected verdict COMPUTED FROM THE RULE TEXT and never from an oracle.
///
/// <para>The rules, each <c>cite.py --check</c>ed:
/// <list type="bullet">
///   <item>SR7 — "An EXIT PROGRAM statement may be specified only in a program procedure division"; the source
///         elements that may carry a Format-1/2 procedure division are §14.2.2 SR10's five.</item>
///   <item>SR8 — "The EXIT PERFORM statement may be specified only in an inline or exception-checking PERFORM
///         statement. The CYCLE phrase shall not be specified within an exception-checking PERFORM
///         statement."</item>
///   <item>SR9 — "The EXIT statement with the SECTION phrase may be specified only in a section."</item>
///   <item>SR10 — "The EXIT statement with the PARAGRAPH phrase may be specified only in a paragraph." ⛔ It
///         admits no violating program: §14.4.3 makes sentences written after a section header (with no
///         paragraph-name) a paragraph too, so the only checkable half is that the admitted case is
///         ADMITTED — which is what the SR10 fact below pins.</item>
/// </list></para>
///
/// <para><b>Why a matrix and not four tests.</b> Before PB403 the placement family was four half-answers in four
/// places: SR7 tested ONE of the four non-program source elements, SR8's first sentence was tested nowhere (and
/// the accepted program emitted a bare C# <c>break;</c> into the pc dispatcher, which re-ran the paragraph
/// FOREVER), SR8's second sentence was tested by a parse-subtree walk owned by the PERFORM binder, and SR9 was
/// tested at the EXIT site. Each row below names the enclosing context it varies, so adding a sixth placement
/// rule with no asker shows up as a missing row rather than as silence.</para>
///
/// <para>The last class of facts pins the EXTRACTION itself: RESUME's §14.9.33.3 SR1/SR2 verdicts are unchanged
/// by moving its "am I in a WHEN phrase / which declarative?" question onto the shared probe. RESUME was the one
/// verb in the family that already asked, so it is the control.</para>
/// </summary>
public sealed class ExitPlacementContextDriftTests
{
    private const string ExitPlacement = "COBOLNET0827";   // the EXIT-statement placement family
    private const string ExitPerformCycleInF3 = "COBOLNET1604";   // SR8 sentence 2
    private const string ResumePlacement = "COBOLNET0712";
    private const string ResumeOperandInWhen = "COBOLNET1610";
    private const string ReturnInGlobalDecl = "COBOLNET2102";   // §14.9.14.3 SR2 / §14.9.18.3 SR1
    private const string RaisingLastOutOfPlace = "COBOLNET2103";   // §14.9.14.3 SR6 / §14.9.18.3 SR5

    // ── SR8 sentence 1: "only in an inline or exception-checking PERFORM statement" ──────────────────────

    /// <summary>Every position that IS "an inline or exception-checking PERFORM statement" — imperative-statement-1
    /// of either format, a WHEN / WHEN OTHER handler body, and the FINALLY phrase (§14.9.14.4 GR4 gives an EXIT
    /// PERFORM written in any of them the same meaning: the implicit CONTINUE at the PERFORM's end).</summary>
    [Theory]
    [InlineData("PBEP01", "PERFORM 2 TIMES\n    EXIT PERFORM\nEND-PERFORM")]
    [InlineData("PBEP02", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n    EXIT PERFORM\nEND-PERFORM")]
    [InlineData("PBEP03", "PERFORM VARYING W-N FROM 1 BY 1 UNTIL W-N > 2\n    EXIT PERFORM\nEND-PERFORM")]
    // A nested inline PERFORM: the EXIT PERFORM belongs to the INNER one (§14.9.14.4 GR5 a) "the most closely
    // preceding, and as yet unterminated, inline PERFORM statement") — either way it is inside one.
    [InlineData("PBEP04", "PERFORM 2 TIMES\n    PERFORM 2 TIMES\n        EXIT PERFORM\n    END-PERFORM\nEND-PERFORM")]
    // Exception-checking (Format 3) PERFORM — imperative-statement-1.
    [InlineData("PBEP05", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n    EXIT PERFORM\n  WHEN EC-SIZE\n    CONTINUE\nEND-PERFORM")]
    // … a WHEN handler body (imperative-statement-2).
    [InlineData("PBEP06", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    EXIT PERFORM\nEND-PERFORM")]
    // … a WHEN OTHER handler body (imperative-statement-3).
    [InlineData("PBEP07", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    CONTINUE\n  WHEN OTHER\n    EXIT PERFORM\nEND-PERFORM")]
    // … the FINALLY phrase (imperative-statement-5).
    [InlineData("PBEP08", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    CONTINUE\n  FINALLY\n    EXIT PERFORM\nEND-PERFORM")]
    public void ExitPerform_InsideAPerform_IsAccepted(string pid, string body)
    {
        // ⛔ THE COMPILE MUST SUCCEED, not merely lack the placement code: `Compile` returns an EMPTY diagnostic
        // list on success, so an absence-only assertion is satisfied by a program that failed for some other
        // reason entirely. Every ACCEPTED row in this class asserts Ok.
        var (ok, diagnostics) = EditionHarness.Compile(MainProgram(pid, body), 2023);
        EditionHarness.AssertNoDiagnostic(diagnostics, ExitPlacement);
        EditionHarness.AssertNoDiagnostic(diagnostics, ExitPerformCycleInF3);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>Every position that is NOT one — each is a §14.9.14.3 SR8 violation, and each of them compiled
    /// CLEAN before PB403. The first row is the one that HUNG: the emitted <c>break;</c> left the dispatcher's
    /// <c>switch (__pc)</c> without advancing <c>__pc</c>, so the paragraph ran again, forever.</summary>
    [Theory]
    // An ordinary paragraph with no PERFORM anywhere in the program.
    [InlineData("PBEQ01", "DISPLAY \"A\"\nEXIT PERFORM")]
    // An inline PERFORM has CLOSED before the EXIT PERFORM — "as yet unterminated" is the whole point.
    [InlineData("PBEQ02", "PERFORM 2 TIMES\n    CONTINUE\nEND-PERFORM\nEXIT PERFORM")]
    // Inside the CONTROL phrase's reach but outside the body: an out-of-line PERFORM does not lexically contain
    // the statements it runs, so a paragraph it performs is not "in" it (this is the sibling the SR8 text
    // excludes by saying INLINE).
    [InlineData("PBEQ03", "PERFORM SUB-PARA 2 TIMES")]
    public void ExitPerform_OutsideEveryPerform_IsRejected(string pid, string body)
    {
        string src = MainProgram(pid, body) + "SUB-PARA.\n    EXIT PERFORM.\n";
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(src, 2023), ExitPlacement);
    }

    /// <summary>The declarative arm of the same rule — a declarative procedure is not a PERFORM either, and the
    /// bind cursor is in a different pc region there, so it is the row that proves the probe is not accidentally
    /// reading the nondeclarative stack.</summary>
    [Fact]
    public void ExitPerform_InADeclarativeProcedure_IsRejected() =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(DeclarativeProgram("PBEQ04", "EXIT PERFORM"), 2023),
            ExitPlacement);

    // ── SR8 sentence 2: "The CYCLE phrase shall not be specified within an exception-checking PERFORM" ────

    /// <summary>CYCLE is measured against the NEAREST enclosing PERFORM, because that is the one it would cycle
    /// (§14.9.14.4 GR5 b). So it is legal in an ordinary inline PERFORM even when that PERFORM is itself written
    /// inside an exception-checking one — the row that a "does any ancestor check exceptions?" reading would get
    /// wrong.</summary>
    [Theory]
    [InlineData("PBEC01", "PERFORM VARYING W-N FROM 1 BY 1 UNTIL W-N > 3\n    EXIT PERFORM CYCLE\nEND-PERFORM")]
    [InlineData("PBEC02", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n    PERFORM 2 TIMES\n        EXIT PERFORM CYCLE\n    END-PERFORM\n  WHEN EC-SIZE\n    CONTINUE\nEND-PERFORM")]
    public void ExitPerformCycle_InAnInlinePerform_IsAccepted(string pid, string body)
    {
        var (ok, diagnostics) = EditionHarness.Compile(MainProgram(pid, body), 2023);
        EditionHarness.AssertNoDiagnostic(diagnostics, ExitPlacement);
        EditionHarness.AssertNoDiagnostic(diagnostics, ExitPerformCycleInF3);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>Inside an exception-checking PERFORM — in imperative-statement-1 or in a handler — CYCLE is
    /// refused: a Format-3 PERFORM is not a loop, so there is no cycle for control to take.</summary>
    [Theory]
    [InlineData("PBEC03", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n    EXIT PERFORM CYCLE\n  WHEN EC-SIZE\n    CONTINUE\nEND-PERFORM")]
    [InlineData("PBEC04", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    EXIT PERFORM CYCLE\nEND-PERFORM")]
    public void ExitPerformCycle_InAnExceptionCheckingPerform_IsRejected(string pid, string body) =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(MainProgram(pid, body), 2023), ExitPerformCycleInF3);

    /// <summary>Outside every PERFORM the FIRST sentence decides — the statement has no PERFORM to exit at all,
    /// so it is the placement diagnostic and not the CYCLE one.</summary>
    [Fact]
    public void ExitPerformCycle_OutsideEveryPerform_IsThePlacementRefusal()
    {
        var diagnostics = EditionHarness.GetDiagnostics(MainProgram("PBEC05", "EXIT PERFORM CYCLE"), 2023);
        EditionHarness.AssertHasDiagnostic(diagnostics, ExitPlacement);
        EditionHarness.AssertNoDiagnostic(diagnostics, ExitPerformCycleInF3);
    }

    // ── SR7: "An EXIT PROGRAM statement may be specified only in a program procedure division" ───────────

    /// <summary>SR7's admitted position. ⛔ THE ASSERTION IS THAT THE COMPILE SUCCEEDS, not merely that
    /// COBOLNET0827 is absent: a program refused for some OTHER reason would satisfy an absence-only assertion
    /// and the row would be green for the wrong reason.</summary>
    [Fact]
    public void ExitProgram_InAProgramProcedureDivision_IsAccepted()
    {
        var (ok, diagnostics) = EditionHarness.Compile(MainProgram("PBXP01", "EXIT PROGRAM"), 2023);
        EditionHarness.AssertNoDiagnostic(diagnostics, ExitPlacement);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>⚠ A KNOWN GAP PINNED SO THAT CLOSING IT IS LOUD. §14.2.2 SR10's fifth source element is a program
    /// prototype definition (ISO §11.10.2 Format 2 — <c>PROGRAM-ID. name IS PROTOTYPE.</c>), and this compiler
    /// cannot parse one: <c>programIdAttribute</c> in <c>CobolIdentification.g4</c> admits only COMMON / INITIAL /
    /// RECURSIVE / GLOBAL, so PROTOTYPE is met as a reserved word (COBOLNET0901) — while the FUNCTION-ID twin has
    /// its <c>(IS? PROTOTYPE)?</c> tail. One construct, two arms, one built.
    /// <para>This fact asserts the CURRENT refusal, not a desired one. It exists because
    /// <c>SourceElementKind.ProgramPrototype</c> is the arm of §14.9.14.3 SR7 that nothing can exercise today, and
    /// a modelled-but-unreachable arm is an unverified one: when the grammar arm lands, this fact goes RED and
    /// whoever lands it has to come here and pin SR7's program-prototype verdict for real.</para></summary>
    [Fact]
    public void ProgramPrototypeDefinition_IsNotYetWritable_SoSr7sPrototypeArmIsUnexercised()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PBXP02P IS PROTOTYPE.
            PROCEDURE DIVISION.
            P-MAIN.
                EXIT PROGRAM.
            END PROGRAM PBXP02P.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. PBXP02.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "OK".
                STOP RUN.
            END PROGRAM PBXP02.

            """;
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(src, 2023), "COBOLNET0901");
    }

    /// <summary>The three §14.2.2 SR10 source elements that are NOT a program: a function definition, a function
    /// prototype definition and a method definition. Exactly ONE of the three (the method) was refused before
    /// PB403 — a function definition's EXIT PROGRAM was accepted AND handed the program-activation machinery
    /// (<c>__asCalled</c> + <c>ProgramReturn</c>).</summary>
    [Theory]
    [InlineData("UXF01", false)]
    [InlineData("UXF02", true)]
    public void ExitProgram_InAFunctionProcedureDivision_IsRejected(string fname, bool prototype)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            FUNCTION-ID. {fname}{(prototype ? " IS PROTOTYPE" : "")}.
            DATA DIVISION.
            LINKAGE SECTION.
            01 R-VAL PIC 9(4).
            PROCEDURE DIVISION RETURNING R-VAL.
            F-MAIN.
                EXIT PROGRAM.
            END FUNCTION {fname}.

            IDENTIFICATION DIVISION.
            PROGRAM-ID. P{fname}.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "OK".
                STOP RUN.
            END PROGRAM P{fname}.

            """;
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(src, 2023), ExitPlacement);
    }

    // ── SR9 / SR10: the Format-4 procedure rules ─────────────────────────────────────────────────────────

    /// <summary>SR9's only violating position: a paragraph that precedes every section header (§14.4.2 — a
    /// section is a section header plus its paragraphs), so there is no current section.</summary>
    [Fact]
    public void ExitSection_OutsideEverySection_IsRejected() =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(MainProgram("PBES01", "EXIT SECTION"), 2023),
            ExitPlacement);

    /// <summary>SR10 admits no violating program — §14.4.3 makes the sentences written directly after a section
    /// header a (paragraph-name-omitted) PARAGRAPH — so the checkable half is that EXIT PARAGRAPH is accepted
    /// there. A screen for SR10 could only ever reject this legal program.</summary>
    [Fact]
    public void ExitParagraph_InAParagraphNameOmittedParagraph_IsAccepted()
    {
        string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PBEP10.
            PROCEDURE DIVISION.
            S-ONE SECTION.
                DISPLAY "A".
                EXIT PARAGRAPH.
                DISPLAY "B".
            S-TWO SECTION.
                STOP RUN.

            """;
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(src, 2023);
        Assert.True(ok, detail);
        // §14.9.14.4 GR6 — control passes to an implicit CONTINUE after the paragraph's last explicit statement.
        Assert.Equal("A", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    // ── The extraction's own control: RESUME's verdicts are unchanged ────────────────────────────────────

    /// <summary>§14.9.33.3 SR1 — RESUME "may be specified only in a declarative or in an imperative statement in
    /// a WHEN phrase of an exception-checking PERFORM statement. In the latter case, the NEXT STATEMENT phrase
    /// shall be specified." RESUME was the ONE verb of the placement family that already asked "where am I
    /// bound?", so these rows are the regression net for moving that question onto the shared probe: the WHEN
    /// frame must be visible from a nested statement, and must NOT leak into imperative-statement-1 or
    /// FINALLY.</summary>
    [Theory]
    // In a WHEN phrase — admitted, and admitted from inside a statement nested in it.
    [InlineData("PBRS01", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    RESUME AT NEXT STATEMENT\nEND-PERFORM", null)]
    [InlineData("PBRS02", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    IF W-N > 0\n        RESUME AT NEXT STATEMENT\n    END-IF\nEND-PERFORM", null)]
    // imperative-statement-1 of the same PERFORM is NOT a WHEN phrase.
    [InlineData("PBRS03", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n    RESUME AT NEXT STATEMENT\n  WHEN EC-SIZE\n    CONTINUE\nEND-PERFORM", ResumePlacement)]
    // … nor is the FINALLY phrase.
    [InlineData("PBRS04", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    CONTINUE\n  FINALLY\n    RESUME AT NEXT STATEMENT\nEND-PERFORM", ResumePlacement)]
    // … nor an ordinary paragraph.
    [InlineData("PBRS05", "RESUME AT NEXT STATEMENT", ResumePlacement)]
    public void Resume_PlacementVerdicts_AreUnchangedByTheSharedProbe(string pid, string body, string? expected)
    {
        var (ok, diagnostics) = EditionHarness.Compile(MainProgram(pid, body), 2023);
        if (expected is null) Assert.True(ok, string.Join("\n", diagnostics));
        else EditionHarness.AssertHasDiagnostic(diagnostics, expected);
    }

    /// <summary>SR1's second sentence, through the same frame: in a WHEN phrase the operand shall be NEXT
    /// STATEMENT, so RESUME AT procedure-name is refused there — and the declarative form still is not.</summary>
    [Fact]
    public void Resume_AtProcedureNameInAWhenPhrase_IsRejected() =>
        EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            MainProgram("PBRS06", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    RESUME AT MAIN-PARA\nEND-PERFORM"), 2023),
            ResumeOperandInWhen);

    /// <summary>The declarative arm of RESUME's SR1, and §14.9.33.3 SR2's GLOBAL refusal beside it — the two
    /// verdicts that come from the probe's declarative lookup, which is now the ONE copy of that lookup.</summary>
    [Theory]
    [InlineData("PBRS07", false, null)]
    [InlineData("PBRS08", true, "COBOLNET0713")]
    public void Resume_InADeclarative_FollowsTheGlobalPhrase(string pid, bool global, string? expected)
    {
        var (ok, diagnostics) = EditionHarness.Compile(DeclarativeProgram(pid, "RESUME AT NEXT STATEMENT", global), 2023);
        if (expected is null) Assert.True(ok, string.Join("\n", diagnostics));
        else EditionHarness.AssertHasDiagnostic(diagnostics, expected);
    }

    // ── §14.9.14.3 SR2 / §14.9.18.3 SR1 — the GLOBAL-declarative prohibition, over TWO verbs ─────────────
    //
    // "The {EXIT | GOBACK} statement shall not be specified in a declarative procedure for which the GLOBAL
    // phrase is specified in the associated USE statement." One sentence, printed twice under two ordinals, and
    // enforced for NEITHER verb before kb/Work PB404/PB409 — while the byte-identical RESUME program above WAS
    // refused, from a declarative lookup sitting ten lines away in the same file. Both verbs now ask the ONE
    // screen (PlacementRules.RefusedInGlobalDeclarative), each citing its own clause and ordinal.

    /// <summary>The non-GLOBAL declarative is the CONTROL: the rule is about the GLOBAL phrase, not about
    /// declaratives, so the same statement in a plain declarative is legal and must stay legal.</summary>
    [Theory]
    [InlineData("PBGD01", "GOBACK", false, null)]
    [InlineData("PBGD02", "GOBACK", true, ReturnInGlobalDecl)]
    [InlineData("PBGD03", "EXIT PROGRAM", false, null)]
    [InlineData("PBGD04", "EXIT PROGRAM", true, ReturnInGlobalDecl)]
    public void ReturnStatementInADeclarative_FollowsTheGlobalPhrase(
        string pid, string verb, bool global, string? expected)
    {
        var (ok, diagnostics) = EditionHarness.Compile(DeclarativeProgram(pid, verb, global), 2023);
        if (expected is null) Assert.True(ok, string.Join("\n", diagnostics));
        else EditionHarness.AssertHasDiagnostic(diagnostics, expected);
    }

    // ── The LAST phrase's position: §14.9.14.3 SR6 and §14.9.18.3 SR5 ───────────────────────────────────
    //
    // ⚠ The two clauses word the SAME rule differently, and each fragment below is quoted from its own:
    //   §14.9.14.3 SR6 — "The LAST phrase may be specified only in a declarative procedure or a WHEN phrase
    //                     in a PERFORM statement."
    //   §14.9.18.3 SR5 — "The LAST phrase may be specified only in a declarative procedure or WHEN phrase of
    //                     a PERFORM statement."
    //
    // The same two positions §14.9.33.3 SR1 names for RESUME, over the RAISING LAST phrase of two more verbs,
    // through the SAME EnclosingContext predicate (InDeclarativeOrPerformWhen). Before kb/Work PB410 the rule
    // was enforced nowhere it applies; the WHEN-phrase half of the admitted set is pinned in
    // GobackStatusArmParityTests, where both arms of the §14.9.18.4 GR2/GR4 fork are compared.

    /// <summary>A declarative procedure IS one of the two admitted positions, for both verbs.</summary>
    [Theory]
    [InlineData("PBRL01", "GOBACK RAISING LAST EXCEPTION")]
    [InlineData("PBRL02", "EXIT PROGRAM RAISING LAST EXCEPTION")]
    public void RaisingLastInADeclarative_IsAccepted(string pid, string body)
    {
        var (ok, diagnostics) = EditionHarness.Compile(DeclarativeProgram(pid, body), 2023);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>Every position that is NEITHER a declarative NOR a WHEN phrase — an ordinary paragraph, an
    /// exception-checking PERFORM's imperative-statement-1, and its FINALLY phrase (the frame RESUME's SR1
    /// excludes for the same reason). Each compiled CLEAN before PB410, and the accepted EXIT form was compiled
    /// into a live ExceptionState.SetPropagatingLast call.</summary>
    [Theory]
    [InlineData("PBRL03", "GOBACK RAISING LAST EXCEPTION")]
    [InlineData("PBRL04", "EXIT PROGRAM RAISING LAST EXCEPTION")]
    [InlineData("PBRL05", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n    GOBACK RAISING LAST EXCEPTION\n  WHEN EC-SIZE\n    CONTINUE\nEND-PERFORM")]
    [InlineData("PBRL06", "PERFORM UNTIL W-N > 2\n    ADD 1 TO W-N\n  WHEN EC-SIZE\n    CONTINUE\n  FINALLY\n    GOBACK RAISING LAST EXCEPTION\nEND-PERFORM")]
    public void RaisingLastOutsideBothPositions_IsRejected(string pid, string body)
        => EditionHarness.AssertHasDiagnostic(
            EditionHarness.GetDiagnostics(MainProgram(pid, body), 2023), RaisingLastOutOfPlace);

    // ── §14.9.18.4 GR6 — the RUN-TIME half of the pair, over LEGAL source ────────────────────────────────

    /// <summary>"If a GOBACK statement is executed within the RANGE of a declarative procedure whose USE
    /// statement contains the GLOBAL phrase and that USE statement is specified in the same program as the
    /// GOBACK statement, the EC-FLOW-GLOBAL-GOBACK exception condition is set to exist." The GOBACK here is
    /// written in an ORDINARY paragraph that the global declarative PERFORMs — SR1 does not reach it, so this is
    /// legal source and GR6 alone governs it. Table 13 makes the condition Fatal and there is no applicable USE,
    /// so §14.6.13.1.3 rule 7 terminates the run unit abnormally; the declarative's statement AFTER the PERFORM
    /// must therefore not run. With checking OFF the condition is not raised at all (§14.6.13.1.1) and the
    /// GOBACK proceeds — which in this main program is a STOP (§14.9.18.4 GR3), so the output is the same
    /// prefix and the exit code is what distinguishes them.</summary>
    [Theory]
    [InlineData("PBG601", ">>TURN EC-FLOW-GLOBAL-GOBACK CHECKING ON\n", false)]
    [InlineData("PBG602", "", true)]
    public void GobackWithinTheRangeOfAGlobalDeclarative_RaisesOnlyWhenChecked(string pid, string turn, bool ok)
    {
        string src = $"""
            {turn}IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "{pid}-absent.dat" ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(10).
            PROCEDURE DIVISION.
            DECLARATIVES.
            D-SEC SECTION.
                USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON F.
            D-PARA.
                DISPLAY "IN-DECL".
                PERFORM GB-P.
                DISPLAY "BACK-IN-DECL".
            END DECLARATIVES.
            MAIN-SEC SECTION.
            MAIN-PARA.
                OPEN INPUT F.
                DISPLAY "AFTER-OPEN".
                STOP RUN.
            GB-P.
                DISPLAY "IN-GB-P".
                GOBACK.

            """;
        var (exit, stdout, detail) = new CobolNetCompiler(2023).CompileAndRunExit(src);
        Assert.Equal(ok, exit == 0);
        // Either way the declarative's PERFORM reaches GB-P and the GOBACK never returns to it: raised, the
        // fatal condition unwinds; unraised, a main program's GOBACK is a STOP (§14.9.18.4 GR3).
        Assert.StartsWith("IN-DECL", stdout.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.DoesNotContain("BACK-IN-DECL", stdout, StringComparison.Ordinal);
        if (!ok) Assert.Contains("EC-FLOW-GLOBAL-GOBACK", detail + stdout, StringComparison.Ordinal);
    }

    // ── Source shapes ────────────────────────────────────────────────────────────────────────────────────

    private static string MainProgram(string pid, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-N PIC 9(2) VALUE 0.
        PROCEDURE DIVISION.
        MAIN-PARA.
            {body.Replace("\n", "\n    ")}.
            STOP RUN.

        """;

    private static string DeclarativeProgram(string pid, string body, bool global = false) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "{pid}.dat" ORGANIZATION IS SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 F-REC PIC X(10).
        WORKING-STORAGE SECTION.
        01 W-N PIC 9(2) VALUE 0.
        PROCEDURE DIVISION.
        DECLARATIVES.
        D-SEC SECTION.
            USE{(global ? " GLOBAL" : "")} AFTER STANDARD ERROR PROCEDURE ON F.
        D-PARA.
            {body.Replace("\n", "\n    ")}.
        END DECLARATIVES.
        MAIN-SEC SECTION.
        MAIN-PARA.
            STOP RUN.

        """;
}
