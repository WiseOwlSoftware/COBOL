// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The APPLY COMMIT antecedent family (kb/Work PB666, PB3338): the syntax rules of the clauses and statements whose
/// antecedent is "a file subject to an APPLY COMMIT clause" are enforced in their own right (COBOL2974,
/// <c>DataBinder.ScreenApplyCommitSubject</c>) in BOTH lanes.
/// </summary>
/// <remarks>
/// <para>⛔ WHY THIS EXISTS. Until kb/Work PB3338 the APPLY COMMIT clause was declined (Annex A.4.3 item 2),
/// so the family was recorded CONFORMS as vacuously satisfied (kb/Work PB371) and only <c>--permissive</c>, where the
/// declined clause was a warning, reached the rules (PB666). Since PB3338 the clause is accepted for a RELATIVE or
/// INDEXED file, so every forbidden shape is a program a user can write in the strict lane. The corpus negatives pin the
/// strict lane's refusal by code; this class pins, for both lanes, that the refusal is the rule's own (its citation in
/// the message) and that it is the only refusal: every diagnostic is the rule's.</para>
/// <para>The sources ARE the corpus fixtures, read from disk, so the forbidden shape is written once. §12.4.6.4.4 GR3
/// (two files of one SAME clause both subject to commit and rollback) is a general rule, not one of these syntax
/// rules, and has no fixture here.</para>
/// </remarks>
public sealed class ApplyCommitSubjectRuleTests
{
    /// <summary>A negative fixture's program text without its <c>*&gt;</c> header comment lines.</summary>
    private static string Fixture(string name) =>
        string.Join('\n', File.ReadAllText(TestRepo.SuiteCase(name)).Split('\n')
            .Where(line => !line.TrimStart().StartsWith("*>", StringComparison.Ordinal)));

    /// <summary>Each fixture's forbidden shape, over a RELATIVE file, draws its rule in the strict lane and under
    /// <c>--permissive</c> alike, and nothing else of the program is refused (the APPLY COMMIT clause itself is accepted).</summary>
    [Theory]
    [InlineData("apply-commit-open-sharing", "§14.9.27.3 SR7", false)]
    [InlineData("apply-commit-open-sharing", "§14.9.27.3 SR7", true)]
    [InlineData("apply-commit-lock-mode", "§12.4.5.9.3 SR1", false)]
    [InlineData("apply-commit-lock-mode", "§12.4.5.9.3 SR1", true)]
    [InlineData("apply-commit-same-area", "§12.4.6.4.3 SR11", false)]
    [InlineData("apply-commit-same-area", "§12.4.6.4.3 SR11", true)]
    [InlineData("apply-commit-read-lock", "§14.9.30.3 SR5", false)]
    [InlineData("apply-commit-read-lock", "§14.9.30.3 SR5", true)]
    [InlineData("apply-commit-rewrite-lock", "§14.9.35.3 SR5", false)]
    [InlineData("apply-commit-rewrite-lock", "§14.9.35.3 SR5", true)]
    [InlineData("apply-commit-unlock", "§14.9.47.3 SR2", false)]
    [InlineData("apply-commit-unlock", "§14.9.47.3 SR2", true)]
    public void ForbiddenShape_IsRefusedByItsOwnRule(string fixture, string rule, bool permissive)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Fixture(fixture), 2023, permissive);
        Assert.False(ok, $"{fixture}: {rule} forbids this shape for a file subject to an APPLY COMMIT clause");
        Assert.Contains(errors, e => e.Contains("COBOL2974", StringComparison.Ordinal)
                                     && e.Contains(rule, StringComparison.Ordinal));
        Assert.All(errors.Where(e => e.Contains("COBOL", StringComparison.Ordinal)),
            e => Assert.Contains("COBOL2974", e, StringComparison.Ordinal));   // the clause is accepted: the rule is the only refusal
        Assert.DoesNotContain(warnings, w => w.Contains("COBOL17", StringComparison.Ordinal));   // and no declined-facility warning
    }

    /// <summary>THE ARM THAT PROVES THE PROBE CAN FAIL: the SR7 fixture with its I-O-CONTROL paragraph removed — the
    /// same OPEN … SHARING on a file no APPLY COMMIT clause names — compiles in both lanes, so the refusal above is the
    /// antecedent's and not the statement's. Without the clause §14.9.27.3 SR8 applies ("When file-name-1 is not subject
    /// to an APPLY COMMIT clause, … if the ALL phrase is specified on the OPEN statement, the LOCK MODE clause shall be
    /// specified"), so the arm writes the LOCK MODE clause the clause's file may not have (§12.4.5.9.3 SR1).</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithoutTheApplyCommitClause_TheSharingPhraseIsLegal(bool permissive)
    {
        string source = string.Join('\n', Fixture("apply-commit-open-sharing").Split('\n')
            .Where(line => !line.Contains("I-O-CONTROL", StringComparison.Ordinal)
                           && !line.Contains("APPLY COMMIT", StringComparison.Ordinal))
            .Select(line => line.Replace("ORGANIZATION IS RELATIVE.", "ORGANIZATION IS RELATIVE LOCK MODE IS AUTOMATIC.",
                StringComparison.Ordinal)));
        Assert.Contains("LOCK MODE IS AUTOMATIC", source, StringComparison.Ordinal);
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023, permissive);
        EditionHarness.AssertNoDiagnostic(errors, "COBOL2974");
        Assert.True(ok, string.Join("\n", errors));
    }
}
