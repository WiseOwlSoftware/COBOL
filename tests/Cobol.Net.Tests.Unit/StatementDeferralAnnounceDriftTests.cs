// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PB236 INVARIANT: a statement the grammar ACCEPTED and the binder REFUSED never leaves the compiler
/// silent. <c>BoundUnsupported</c> used to be the carrier for THREE incompatible jobs — a construct WiseOwl COBOL
/// has not built, an ill-formed OPERAND, and an illegal PLACEMENT — and <c>StatementEmitter</c> rendered all
/// three as the same <c>NotImplemented.Run(...)</c>. Two consequences, both measured: the two user-error jobs
/// told the programmer THE COMPILER was incomplete when in fact THE SOURCE was wrong, and on a path the flow
/// skipped they said NOTHING AT ALL, so illegal source compiled, ran to normal completion, and shipped.
/// <para>The separation is structural. The user-error jobs report their own rule and bind to
/// <c>BoundNop</c>; what is left on the carrier is the DEFERRAL, and it is announced from the ONE place it
/// cannot be forgotten — <c>StatementBinder.BindStatement</c>, the single funnel every statement passes
/// through, which already positions the diagnostic cursor (kb/Work PB82). A new <c>BoundUnsupported</c>
/// written in any future verb binder inherits COBOL1756 by construction; nobody has to remember it, which
/// is exactly what the per-site habit failed at (two of START's five refusals carried a diagnostic and three
/// did not).</para>
/// <para>⛔ WHY THIS IS A GATE AND NOT A GREEN TICK. Three of the four facts here are the ones that would go
/// wrong: the announce must NOT fail the compile (a deferral is WiseOwl COBOL's gap, not the source's error, so a
/// program whose unimplemented statement is never reached still has a defined meaning and still runs); it must
/// NOT fire on programs it has no business touching; and a violated SYNTAX RULE must draw an ERROR naming the
/// rule, never this warning. A test that asserted only "some diagnostic appeared" would pass in every one of
/// those failure modes (feedback_green_gates_arent_evidence).</para>
/// </summary>
public sealed class StatementDeferralAnnounceDriftTests
{
    /// <summary>INSPECT … FOR ALL LINAGE-COUNTER OF file is a pure job-1 deferral: LINAGE-COUNTER (§8.4.3.14) is
    /// runtime-sourced and the resolver routes it only where a statement binder reads it
    /// (<c>DeferredShape.UnroutedSpecialRegister</c>), and INSPECT's search operand is not one of them. The source is
    /// the standard's own: §8.4.3.14.4 GR1 makes LINAGE-COUNTER "a temporary unsigned integer data item of class and
    /// category numeric" and leaves its usage to the implementor, and §14.9.22.3 SR2 admits an elementary item of
    /// usage display. It sits behind a GO TO so the RUN never reaches it — before PB236 that made the staged loud
    /// unobservable at EVERY stage.
    /// <para>⚠ THE EXEMPLAR IS WHATEVER LEGAL SOURCE THE COMPILER STILL DEFERS, AND IT HAS CHANGED BEFORE: ENTRY until
    /// kb/Work PB909 (a PIN OF THE WRONG KIND, kb/Work PB938 — ISO/IEC 1989 defines no ENTRY statement, now refused
    /// COBOL2269), a table SORT over a REDEFINES view until kb/Work PB1175 and keyed on one until PB599, and a
    /// SEARCH ALL of an OCCURS DYNAMIC table nested under another table until kb/Work PB3365 built its per-occurrence
    /// capacity path. When this one is built, replace it with the next (<c>DeferredShape</c>'s census and the open
    /// notes naming COBOL1756 are the places to look), never with an extension.</para></summary>
    private const string DeferralProgram = """
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236ANNOUNCE.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT LF1 ASSIGN TO "pb236.lst".
DATA DIVISION.
FILE SECTION.
FD LF1 LINAGE IS 10 LINES.
01 LR PIC X(5).
WORKING-STORAGE SECTION.
01 S PIC X(8).
01 N PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    GO TO SKIPPER.
    INSPECT S TALLYING N FOR ALL LINAGE-COUNTER OF LF1.
SKIPPER.
    DISPLAY "DONE".
    STOP RUN.
""";

    private const string ConformingProgram = """
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236NOANNOUNCE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 SRC.
   05 A PIC 9(3) VALUE 1.
01 DST.
   05 A PIC 9(3) VALUE 2.
PROCEDURE DIVISION.
MAIN.
    ADD CORRESPONDING SRC TO DST.
    DISPLAY DST.
    STOP RUN.
""";

    [Fact]
    public void Deferral_IsAnnounced_AsAWarning_AndTheProgramStillCompiles()
    {
        var (ok, errors, warnings) = Compile(DeferralProgram);
        // The compile SUCCEEDS: a gap in WiseOwl COBOL may not reject a program the standard gives a meaning to.
        Assert.True(ok, string.Join("\n", errors));
        Assert.Contains(warnings, w => w.Contains("COBOL1756", StringComparison.Ordinal));
    }

    /// <summary>The announce carries the STATEMENT's own source position — it rides the ONE diagnostic cursor,
    /// so a deferral is as locatable as any error. A bare "not implemented" with no location was the other
    /// cost of the run-time-only posture.</summary>
    [Fact]
    public void Deferral_Warning_CarriesTheStatementPosition()
    {
        var (_, _, warnings) = Compile(DeferralProgram);
        string w = warnings.Single(x => x.Contains("COBOL1756", StringComparison.Ordinal));
        Assert.Contains("pb236.cob(17,", w, StringComparison.Ordinal);   // the INSPECT line, not the program
        Assert.Contains("LINAGE-COUNTER", w, StringComparison.Ordinal);
    }

    [Fact]
    public void ConformingProgram_DrawsNoDeferralWarning()
    {
        var (ok, errors, warnings) = Compile(ConformingProgram);
        Assert.True(ok, string.Join("\n", errors));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOL1756", StringComparison.Ordinal));
    }

    /// <summary>The other half of the separation: a violated SYNTAX RULE is an ERROR naming the rule, never
    /// the deferral warning. `ADD CORRESPONDING` over an elementary operand violates ISO §14.9.2.3 SR6, and
    /// the binder has had the predicate and the citation all along — only the STAGE was wrong.</summary>
    [Fact]
    public void SyntaxRuleViolation_IsAnError_NotTheDeferralWarning()
    {
        var (ok, errors, warnings) = Compile(ConformingProgram.Replace(
            "ADD CORRESPONDING SRC TO DST.", "ADD CORRESPONDING A OF SRC TO DST."));
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOL1757", StringComparison.Ordinal)
                                     && e.Contains("14.9.2.3 SR6", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, w => w.Contains("COBOL1756", StringComparison.Ordinal));
    }

    private static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) Compile(string source)
    {
        string dir = ScratchDirectory.Create("CobolNet_PB236_");
        try
        {
            string src = Path.Combine(dir, "pb236.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb236.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Errors, r.Warnings);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
