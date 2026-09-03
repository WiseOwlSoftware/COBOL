// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// THE witnesses for the <c>DOCUMENTED-NON-SUPPORT</c> traceability rows — the half of that verdict that is
/// easy to skip. <c>tests/version-matrix/inventory-schema.json</c> is explicit: "The notes field carries the
/// decision reference; closing still needs a test proving we <b>diagnose</b> (or, where the departure is a
/// reported VALUE rather than a rejected construct, that the documented behaviour is what actually happens)
/// rather than silently miscompile." A declined facility a program can still write and get a clean, silent
/// compile out of is not declined — it is undiagnosed, and nothing else in the battery can tell those apart.
///
/// <para>⛔ WHY THESE ARE NOT GOLDENS. The named non-support diagnostics are <b>warnings</b>
/// (COBOLNET1560 / COBOLNET1578-1580, docs/CONFORMANCE.md §1): the positive corpus asserts a program COMPILES
/// and byte-matches its <c>.out</c> and never looks at the warning channel, and the negative corpus requires
/// the compile to FAIL, which a warning does not. So a corpus golden can pin the INERTNESS half and only a
/// compile-diagnostic assertion can pin the DECLINE half. Each test below therefore reads the SAME source the
/// corpus runs — one source of truth for both halves — and adds the assertion the corpus structurally cannot
/// make. (feedback_green_test_can_hold_a_gap_open: a warning nothing asserts reads as implemented support.)</para>
///
/// <para>THE SPEC BASIS, per facility. Commit/rollback is PROCESSOR-DEPENDENT (Annex A.3 item 6) and §4.2.6
/// third paragraph makes the compile-time warning mechanism MANDATORY for one that is not supported, so the
/// COBOLNET1579 assertion is a conformance obligation, not a house style. Screen handling (Annex A.4.2) and
/// VALIDATE (Annex A.4.14) are OPTIONAL: §4.2.7 requires only that the decline be identified in user
/// documentation, so COBOLNET1560/COBOLNET1580 are this implementation's own stronger posture — and a posture
/// that is documented but unmeasured is exactly what these rows owe. kb/Work PB260 (screen) and PB261
/// (commit/rollback) own the debt.</para>
///
/// <para>⛔ WHAT IS DELIBERATELY NOT ASSERTED HERE, and why no test may be added to make it green: Annex A.4.2
/// <b>item 10</b> — "EC-SCREEN exception conditions in the RAISING phrase of the EXIT and GOBACK statements,
/// the RAISING phrase of the procedure division header, the USE statement, the WHEN phrase of the PERFORM
/// statement, the RAISE statement, and the TURN compiler directive" — is part of the SAME declined module, and
/// its six source positions need no SCREEN SECTION. Today they draw NOTHING: the five EC-SCREEN names are
/// declared in <c>ExceptionCatalog</c> (level-2 at :51, level-3 at :180-184) with zero setting sites and zero
/// non-support diagnostics, so <c>&gt;&gt;TURN EC-SCREEN-LINE-NUMBER CHECKING ON</c> and
/// <c>RAISE EC-SCREEN-FIELD-OVERLAP</c> compile clean against a facility that does not exist. That is PB260
/// finding 3 — "a defect to register, not a fixture to write around" — and it holds the rows whose content
/// needs that machinery (<c>GR-14.9.11.4-18</c> and its two level-2 legs, and <c>GR-14.9.11.4-19</c>) OPEN.
/// A test pinning the current silence would read as a decision (feedback_green_test_can_hold_a_gap_open).</para>
/// </summary>
public sealed class DocumentedNonSupportWitnessTests
{
    private const string ScreenNonSupport = "COBOLNET1560";     // SCREEN SECTION — A.4.2 / §4.2.7
    private const string CommitNonSupport = "COBOLNET1579";     // COMMIT/ROLLBACK — A.3 items 6-7 / §4.2.6 ¶3
    private const string ValidateNonSupport = "COBOLNET1580";   // VALIDATE — A.4.14 / §4.2.7 / F.2 item 5

    /// <summary>The corpus golden's source text — the witness and the corpus run the SAME file.</summary>
    private static string Golden(string edition, string name) =>
        File.ReadAllText(Path.Combine(TestRepo.Tests("conformance"), edition, name + ".cob"));

    /// <summary>The NEGATIVE-corpus source text — same one-source-of-truth rule as <see cref="Golden"/>. Read
    /// here so the refusal is asserted even while the entry is manifest-PENDING (its <c>.err</c> substring is
    /// not yet measured, and a negative entry may not ship an unmeasured <c>.err</c>).</summary>
    private static string Negative(string name) =>
        File.ReadAllText(Path.Combine(TestRepo.Tests("conformance"), "negative", name + ".cob"));

    /// <summary>The recognize-and-warn contract: at every named edition the source COMPILES (the facility is
    /// declined, not rejected) and the NAMED non-support diagnostic rides the non-failing channel.</summary>
    private static void AssertRecognizedAndNamed(string source, string code, params int[] editions)
    {
        foreach (int edition in editions)
        {
            var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
            Assert.True(ok, $"--std {edition}: a declined facility is recognize-and-warn, so the program must "
                + $"still COMPILE (docs/CONFORMANCE.md §1): {string.Join("\n", errors)}");
            EditionHarness.AssertHasDiagnostic(warnings, code);
        }
    }

    /// <summary>SCREEN SECTION declared → COBOLNET1560, at every edition the screen rows name. The fixture
    /// carries the entry shapes the declined general rules govern: SG-OUT's FROM/VALUE elementary items are
    /// §14.9.11.4 GR13's transfer subject, SG-IN carries a TO item, and the LINE/COLUMN clauses are what
    /// GR12/GR14-GR16 position — none of it separately diagnosed, because A.4.1 makes the module's syntax and
    /// general rules optional with the module.
    /// <para>⚠ It does NOT carry §14.9.1.3 SR4's forbidden shape. SR4 constrains a REFERENCE ("screen-name-1
    /// may reference a group item containing screen items with FROM or VALUE clauses only if the group also
    /// contains screen items with TO or USING clauses"), and this fixture references no screen-name at all, so
    /// no declaration in it can violate SR4. That shape is written by <c>pb260_accept_screen_reference</c>,
    /// where an ACCEPT actually references it.</para></summary>
    [Fact]
    public void ScreenFacility_NamedNonSupportWarning_PinnedToSpec() =>
        AssertRecognizedAndNamed(Golden("2023", "pb260_screen_facility_witness"),
            ScreenNonSupport, 2002, 2014, 2023);

    /// <summary>§13.17.3 SR9 (LOCALE phrase + SIGN clause in one screen description entry) is subsumed by the
    /// A.4.2 item 20 decline: the pair is accepted, and the only diagnostic is the facility's own named one.</summary>
    [Fact]
    public void ScreenLocaleSignPair_AcceptedUnderTheDecline_PinnedToSpec() =>
        AssertRecognizedAndNamed(Golden("2023", "pb260_screen_locale_sign_witness"),
            ScreenNonSupport, 2002, 2014, 2023);

    /// <summary>The MINIMAL legal DISPLAY Format 2 — <c>DISPLAY screen-name-1</c> with the AT phrase and the
    /// exception phrases omitted (§14.9.11.2 Format 2; every one of them is bracketed) — must not run to a
    /// clean completion. It parses as a one-operand Format-1 DISPLAY, so the compile-time signal is the
    /// section-level COBOLNET1560 and the STATEMENT is refused at run time, naming that same cause
    /// (kb/Work R32). Both halves are asserted here: silence at EITHER stage would be a silent miscompile of
    /// a construct docs/CONFORMANCE.md §4 item 4 says is not provided. The empty stdout is the load-bearing
    /// half — it is what separates a REFUSED screen transfer from one that silently went nowhere.</summary>
    [Fact]
    public void ScreenFormat2Statement_FailsLoudNamingTheDecline_PinnedToSpec()
    {
        string source = Golden("2023", "screen_section_reference");
        AssertRecognizedAndNamed(source, ScreenNonSupport, 2023);

        var (ok, stdout, detail) = EditionHarness.CompileAndRun(source, 2023);
        Assert.False(ok, "DISPLAY screen-name-1 is Annex A.4.2 item 9, declined — it must not complete "
            + "normally, which is the only outcome distinguishable from a screen transfer that silently "
            + "went nowhere (kb/Work PB260 finding 1).");
        Assert.Equal("", stdout);
        Assert.Contains(ScreenNonSupport, detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The ACCEPT twin: <c>ACCEPT screen-name-1</c> with the AT phrase omitted is the minimal legal
    /// Format 3 (§14.9.1.2), Annex A.4.2 item 1. It likewise compiles under COBOLNET1560 and is refused at run
    /// time rather than silently transferring nothing into the screen record.
    /// <para>The referenced group is deliberately the shape §14.9.1.3 SR4 forbids — FROM/VALUE screen items
    /// with NO TO or USING item — because SR4 constrains the REFERENCE, not the declaration: only a statement
    /// that references such a group can violate it. Under the decline the reference is ACCEPTED and SR4 is not
    /// enforced (A.4.1 carries the module licence to the syntax rule), but the facility is still NAMED.</para>
    /// <para>⚠ The run-time text is asserted only to be the ACCEPT receiver refusal, NOT to name COBOLNET1560:
    /// the ACCEPT arm builds its loud from the bare reference while the DISPLAY arm routes through the R32
    /// screen-section reason. That asymmetry is a registered finding, and this assertion deliberately pins
    /// only what the decline requires (a refusal), so it does not harden the gap into an expectation.</para></summary>
    [Fact]
    public void ScreenFormat3Accept_IsRefusedNotSilentlyDropped_PinnedToSpec()
    {
        string source = Golden("2023", "pb260_accept_screen_reference");
        AssertRecognizedAndNamed(source, ScreenNonSupport, 2023);

        var (ok, _, detail) = EditionHarness.CompileAndRun(source, 2023);
        Assert.False(ok, "ACCEPT screen-name-1 is Annex A.4.2 item 1, declined — it must be refused, never "
            + "executed as a no-op that leaves the receiver untouched with no word said.");
        Assert.Contains("ACCEPT receiver", detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>§14.9.1.3 SR5's OWN operands. Identifier-3 and identifier-4 are the AT LINE NUMBER / COLUMN
    /// NUMBER positions of ACCEPT Format 3 (§14.9.1.2 — AT and NUMBER are optional words and the whole AT
    /// phrase is one bracketed group), so the AT-OMITTED shape above measures nothing about them: it writes
    /// neither operand. This is the only source that does, and it writes the shape SR5 forbids (a SIGNED
    /// PIC S9(3) line operand).
    /// <para>The obligation here is weaker than the warning ones and is the module's, not §4.2.6's:
    /// docs/CONFORMANCE.md §5 states the posture for a <b>Not claimed</b> optional module as "a parse error or
    /// a named error is the conforming posture" (per A.4.1, optional-element syntax is accepted only when
    /// support is claimed). So a REFUSAL is what closes this, and the point of asserting it is that the
    /// alternative — silently reinterpreting the source as a Format-1 ACCEPT with the position operands
    /// dropped — would be a wrong answer no diagnostic mentions. The <c>.err</c> substring is not asserted:
    /// the emitted text is not yet measured, so the negative-corpus entry ships PENDING and this test carries
    /// the refusal without hardening a diagnostic nobody has read.</para></summary>
    [Fact]
    public void ScreenFormat3AcceptAtPhrase_IsRefusedNotReinterpreted_PinnedToSpec()
    {
        string source = Negative("pb260-accept-screen-at-phrase");
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
            Assert.False(ok, $"--std {edition}: ACCEPT screen-name-1 AT LINE NUMBER … COLUMN NUMBER … is "
                + "Annex A.4.2 item 1 and the module is Not claimed, so the source must be REFUSED — never "
                + "reinterpreted as a Format-1 ACCEPT with §14.9.1.3 SR5's operands silently dropped.");
            Assert.NotEmpty(errors);
        }
    }

    /// <summary>COMMIT / ROLLBACK → COBOLNET1579. This is the one assertion in this class the STANDARD itself
    /// compels: Annex A.3 item 6 makes the facility processor-dependent, and §4.2.6 ¶3 — "An implementation
    /// shall provide a warning mechanism at compile time to indicate use of syntactically-detectable
    /// processor-dependent language elements not supported by that implementation" — makes the compile-time
    /// warning mandatory. §14.9.7.4 GR3/GR4/GR5 ride the decline through A.4.1. kb/Work PB261.
    /// <para>⛔ AND IT IS THE ONLY EVIDENCE AVAILABLE, which is why the golden's <c>.out</c> cannot close these
    /// rows on its own: §14.9.7.4 GR1 and §14.9.36.4 GR1 both read "If this statement is executed when there is
    /// no active APPLY COMMIT clause, then it has the same effect as a CONTINUE statement with no additional
    /// phrases", so with no APPLY COMMIT clause in the program a conforming processor and a declining one are
    /// behaviourally IDENTICAL. The observable difference needs an APPLY COMMIT clause, and that clause does not
    /// parse (the sibling row GR-14.9.7.4-1) — so the compile-time name is the whole witness.</para></summary>
    [Fact]
    public void CommitRollbackFacility_NamedNonSupportWarning_PinnedToSpec() =>
        AssertRecognizedAndNamed(Golden("2023", "pb261_commit_facility_witness"), CommitNonSupport, 2023);

    /// <summary>VALIDATE → COBOLNET1580, at the editions the facility exists (2002+; at --std 85 VALIDATE is
    /// an ordinary user-defined word). §13.18.40.4 GR19's only trigger is the format validation stage of a
    /// VALIDATE statement, so the decline is what makes the rule unreachable.
    /// <para>⚠ The golden's <c>.out</c> pins INERTNESS only, never the decline: §14.9.50.4 GR5 ("the execution
    /// of the VALIDATE statement does not terminate and the content of the invalid data item does not change")
    /// and §13.18.17.4 GR1 ("The data item itself remains unchanged") make an UNCHANGED subject the conforming
    /// outcome too, so "SUBJECT UNCHANGED" discriminates nothing. Only this warning does.</para></summary>
    [Fact]
    public void ValidateFacility_NamedNonSupportWarning_PinnedToSpec() =>
        AssertRecognizedAndNamed(Golden("2023", "dns_validate_picture_locale_witness"),
            ValidateNonSupport, 2002, 2014, 2023);
}
