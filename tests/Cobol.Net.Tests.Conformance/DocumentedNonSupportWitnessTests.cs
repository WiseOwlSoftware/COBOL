// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// THE witnesses for the <c>DOCUMENTED-NON-SUPPORT</c> traceability rows whose facility is ACCEPT-INERT — the
/// half of that verdict that is easy to skip. <c>tests/version-matrix/inventory-schema.json</c> is explicit:
/// "The notes field carries the decision reference; closing still needs a test proving we <b>diagnose</b> (or,
/// where the departure is a reported VALUE rather than a rejected construct, that the documented behaviour is
/// what actually happens) rather than silently miscompile." A declined facility a program can still write and
/// get a clean, silent compile out of is not declined — it is undiagnosed, and nothing else in the battery can
/// tell those apart.
///
/// <para>⛔ WHY THESE ARE NOT GOLDENS. The facilities left in this class are declined <b>ACCEPT-INERT</b>, so
/// their named diagnostics are <b>warnings</b> (COBOLNET1578-1580, docs/CONFORMANCE.md §1): the positive corpus
/// asserts a program COMPILES and byte-matches its <c>.out</c> and never looks at the warning channel, and the
/// negative corpus requires the compile to FAIL, which a warning does not. So a corpus golden can pin the
/// INERTNESS half and only a compile-diagnostic assertion can pin the DECLINE half. Each test below therefore
/// reads the SAME source the corpus runs — one source of truth for both halves — and adds the assertion the
/// corpus structurally cannot make. (feedback_green_test_can_hold_a_gap_open: a warning nothing asserts reads
/// as implemented support.)</para>
///
/// <para>⛔ SCREEN HANDLING (Annex A.4.2) IS NO LONGER WITNESSED HERE, and its absence is the point. It used to
/// be the largest client of this class, pinned as an accept-inert WARNING. kb/Work PB260 measured that posture
/// and refuted it: A.4.1 — "An implementation shall accept the syntax and provide the functionality for an
/// optional element only when support for that language element is claimed by the implementor" — is a
/// prohibition on ACCEPTING, so an unclaimed A.4 module is REFUSED, not warned. The whole module is now an
/// ERROR by name (COBOLNET1560 data/environment surface, COBOLNET1707 procedure surface) through the one funnel
/// <see cref="CobolNet.Binding.ScreenFacility"/>, which puts its witnesses where a refusal belongs: the NEGATIVE
/// corpus, 41 programs under <c>tests/conformance/negative/a42-*</c>, each <c>.err</c> naming the construct as
/// well as the code. <c>ScreenFacilityConstructDriftTests</c> holds that coverage complete against
/// <c>CobolScreen.g4</c>. Adding a screen test back here would need it to COMPILE, which is exactly what the
/// module may no longer do.</para>
///
/// <para>THE SPEC BASIS, per remaining facility. Commit/rollback is PROCESSOR-DEPENDENT (Annex A.3 item 6) and
/// §4.2.6 third paragraph makes the compile-time warning mechanism MANDATORY for one that is not supported, so
/// the COBOLNET1579 assertion is a conformance obligation, not a house style. VALIDATE (Annex A.4.14) is
/// OPTIONAL and additionally OBSOLETE (§4.2.13; Annex F.2 item 5): §4.2.7 requires only that the decline be
/// identified in user documentation, so COBOLNET1580 is this implementation's own stronger posture — and a
/// posture that is documented but unmeasured is exactly what these rows owe. kb/Work PB261 owns the
/// commit/rollback debt.</para>
/// </summary>
public sealed class DocumentedNonSupportWitnessTests
{
    private const string CommitNonSupport = "COBOLNET1579";     // COMMIT/ROLLBACK — A.3 items 6-7 / §4.2.6 ¶3
    private const string ValidateNonSupport = "COBOLNET1580";   // VALIDATE — A.4.14 / §4.2.7 / F.2 item 5

    /// <summary>The corpus golden's source text — the witness and the corpus run the SAME file.</summary>
    private static string Golden(string edition, string name) =>
        File.ReadAllText(Path.Combine(TestRepo.Tests("conformance"), edition, name + ".cob"));

    /// <summary>The recognize-and-warn contract: at every named edition the source COMPILES (the facility is
    /// declined ACCEPT-INERT, not refused) and the NAMED non-support diagnostic rides the non-failing
    /// channel.</summary>
    private static void AssertRecognizedAndNamed(string source, string code, params int[] editions)
    {
        foreach (int edition in editions)
        {
            var (ok, errors, warnings) = EditionHarness.CompileFull(source, edition);
            Assert.True(ok, $"--std {edition}: an ACCEPT-INERT declined facility must still COMPILE "
                + $"(docs/CONFORMANCE.md §1): {string.Join("\n", errors)}");
            EditionHarness.AssertHasDiagnostic(warnings, code);
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
