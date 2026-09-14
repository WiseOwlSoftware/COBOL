// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE TWO ARMS OF THE §14.9.18.4 GR2/GR4 FORK OWE THE STATUS PHRASE THE SAME SYNTAX RULES — the parity fact,
/// asserted by compiling the SAME statement in a program and in a method and comparing the verdicts.
/// <para>WHY IT IS WRITTEN THIS WAY. kb/Work PB411's second mechanism was not a missing rule, it was a missing
/// CALLER: <c>CallBinder.BindGoback</c> forked to the OO binder when the GOBACK was inside a METHOD, and that
/// binder read only the RETURNING and RAISING phrases — so the status phrase was discarded before any rule could
/// see it. Every rule in this file already passed on the program arm; testing them again there would have proved
/// nothing. Comparing the arms is what makes the defect visible, and what keeps the next phrase from being
/// dropped on one of them (<c>feedback_two_arm_dispatch</c>). The measured pre-fix state: the four rows below
/// each compiled CLEAN inside a method — three syntax rules and an edition gate, all four unenforced.</para>
/// <para>THE RULES ARE CONTEXT-FREE AND THE SPEC SAYS SO. §14.9.18.3's syntax rules carry no qualifier of any
/// kind. The context restriction the standard DOES write appears only in the general rules — GR7, GR8, GR9 and
/// GR10 each open "If the GOBACK … is executing in a main program" — and that is about the phrase's EFFECT, not
/// its legality. A method is never a main program, so <see cref="TheMethodArmsStatus_IsInertNotErroneous"/> pins
/// the other half: a legal status phrase in a method compiles, runs, and indicates nothing.</para>
/// </summary>
public sealed class GobackStatusArmParityTests
{
    /// <summary>The same GOBACK statement in a PROGRAM (the §14.9.18.4 GR2 activation-return arm).</summary>
    private static string InProgram(string goback) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB411ARMP.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-GRP.
           05 WS-A PIC X VALUE "4".
           05 WS-B PIC X VALUE "1".
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY "IN-PROG".
            {goback}
        """;

    /// <summary>The same GOBACK statement in a METHOD (the §14.9.18.4 GR4 method-return arm), reached through an
    /// INVOKE so the method is genuinely bound and emitted rather than merely parsed.</summary>
    private static string InMethod(string goback) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB411ARMM.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        REPOSITORY.
            CLASS CB411A.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 O USAGE OBJECT REFERENCE CB411A.
        PROCEDURE DIVISION.
        MAIN.
            INVOKE CB411A "NEW" RETURNING O.
            INVOKE O "PING".
            DISPLAY "AFTER-INVOKE".
            STOP RUN.
        END PROGRAM PB411ARMM.

        IDENTIFICATION DIVISION.
        CLASS-ID. CB411A.
        IDENTIFICATION DIVISION.
        OBJECT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-GRP.
           05 WS-A PIC X VALUE "4".
           05 WS-B PIC X VALUE "1".
        PROCEDURE DIVISION.
        METHOD-ID. PING.
        PROCEDURE DIVISION.
        MAIN-P.
            DISPLAY "IN-METHOD".
            {goback}
        END METHOD PING.
        END OBJECT.
        END CLASS CB411A.
        """;

    [Theory]
    // §14.9.18.3 SR8 — "Literal-1 shall not be a zero-length literal."
    [InlineData("GOBACK WITH ERROR STATUS \"\".", 2023, "COBOLNET1704")]
    // §14.9.18.3 SR7 — "If literal-1 is numeric, it shall be an integer."
    [InlineData("GOBACK WITH ERROR STATUS 1.5.", 2023, "COBOLNET1704")]
    // §14.9.18.3 SR6 — a BIT group is neither an integer item nor a display/national one (§13.18.29.4 GR1 b).
    [InlineData("GOBACK WITH ERROR STATUS WS-BITG.", 2023, "COBOLNET1704")]
    // The COBOL-2023 introduction of the phrase itself (§14.9.18.2; Annex E.3.3 item 32).
    [InlineData("GOBACK WITH ERROR STATUS 5.", 2014, "COBOLNET0900")]
    [InlineData("GOBACK WITH ERROR STATUS 5.", 2002, "COBOLNET0900")]
    public void AStatusRuleBrokenInAMethod_IsRejectedExactlyAsInAProgram(string goback, int edition, string code)
    {
        const string bitGroup = """
            01 WS-BITG GROUP-USAGE IS BIT.
               05 WS-BB PIC 1(8) USAGE BIT VALUE B"00000111".
            """;
        var (progOk, progErrors, _) = EditionHarness.CompileFull(
            InProgram(goback).Replace("01 WS-GRP.", bitGroup + "\n01 WS-GRP."), edition);
        var (methOk, methErrors, _) = EditionHarness.CompileFull(
            InMethod(goback).Replace("01 WS-GRP.", bitGroup + "\n01 WS-GRP."), edition);

        Assert.False(progOk, $"the program arm must reject '{goback}' at {edition}");
        Assert.False(methOk,
            $"the METHOD arm accepted '{goback}' at {edition} while the program arm rejected it. §14.9.18.3's "
            + "syntax rules carry no context qualifier and §14.9.18.2 is the ONE general format both arms are "
            + "written in — the fork may differ in what it DOES with a phrase, never in whether the phrase's "
            + "rules run (kb/Work PB411).");
        EditionHarness.AssertHasDiagnostic(progErrors, code);
        EditionHarness.AssertHasDiagnostic(methErrors, code);
    }

    /// <summary>THE OTHER HALF OF THE RULE, and the reason the fix is a SCREEN and not a new behaviour: a legal
    /// status phrase in a method is accepted and then has no effect. §14.9.18.4 GR7/GR8/GR9/GR10 give the phrase
    /// its whole force only "in a main program", and a method is never one — so the INVOKE returns normally, the
    /// statement after it runs, and the run unit's own STOP RUN decides the termination indication (0).
    /// <para>The operand is an ALPHANUMERIC GROUP, which is mechanism 1 of the same note: §8.5.2.1 — "An
    /// alphanumeric group item is treated as though it had a usage of display" — makes it one of the two shapes
    /// SR6 admits, and it was rejected on both verbs before the fix.</para></summary>
    [Fact]
    public void TheMethodArmsStatus_IsInertNotErroneous()
    {
        var (exit, stdout, detail) = new CobolNetCompiler(2023)
            .CompileAndRunExit(InMethod("GOBACK WITH ERROR STATUS WS-GRP."));
        Assert.Equal("IN-METHOD" + Environment.NewLine + "AFTER-INVOKE", stdout.ReplaceLineEndings().TrimEnd());
        Assert.Equal(0, exit);
    }
}
