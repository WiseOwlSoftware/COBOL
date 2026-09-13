// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE STATEMENT AND THE CLAUSE THAT GOVERNS IT TRAVEL AS ONE VALUE (kb/Work PB388; CLAUDE.md rule 1).
/// RAISE (§14.9.29), the GOBACK RAISING phrase (§14.9.18) and the EXIT PROGRAM / FUNCTION / METHOD RAISING
/// phrase (§14.9.14) are bound by ONE code path — <see cref="Procedure.EcBinder.EcBindRaising"/> over
/// <see cref="EcNameResolution.TryResolve"/> — and every rule that path enforces is written down THREE TIMES in
/// the standard, once per statement, under a DIFFERENT ordinal each time:
/// <list type="table">
///   <item><term>level-3 exception-name</term><description>EXIT SR3 · GOBACK SR2 · RAISE SR1</description></item>
///   <item><term>EC-USER named in the PD RAISING phrase</term><description>EXIT SR3 · GOBACK SR2 (RAISE: none —
///     it has no such rule)</description></item>
///   <item><term>identifier-1 is an object reference</term><description>EXIT SR5 · GOBACK SR4 · RAISE SR2, with
///     the declared-class constraint at <c>a)</c> and the universal-reference one at <c>d)</c></description></item>
///   <item><term>the LAST phrase</term><description>EXIT SR6 · GOBACK SR5 (RAISE: no LAST phrase)</description></item>
/// </list>
/// Before this type the VERB was threaded through that path and the CLAUSE was a literal in each message, so
/// every one of them printed GOBACK's or RAISE's ordinal at an EXIT statement — a wrong rule number shown to the
/// user, on legal and illegal source alike, and the exact shape of feedback_two_arm_dispatch. The citation is a
/// property OF THE SITE; making it one field of the value the site already passes is what makes a message
/// physically unable to name another statement's rule.
///
/// <para>⚠ The ordinals are pinned to the standard by <c>EcRaiseSiteDriftTests</c>, which re-derives each one
/// from <c>spec-rule-catalog.json</c> by matching the rule TEXT — so an edition that renumbers §14.9.14.3 fails
/// a test instead of silently misdirecting a reader.</para>
/// </summary>
/// <param name="Verb">The statement as the user wrote it, and as every message names it ("RAISE", "GOBACK",
/// "EXIT PROGRAM", "EXIT FUNCTION", "EXIT METHOD").</param>
/// <param name="Clause">The SYNTAX RULE block that governs this statement's RAISING operands.</param>
/// <param name="Level3Rule">The rule requiring a level-3 exception-name (and, where it exists, requiring an
/// EC-USER name to appear in the procedure division header's RAISING phrase).</param>
/// <param name="ObjectRule">The rule constraining identifier-1 to an object reference. Its <c>a)</c> sub-item is
/// the declared-class-in-the-PD-header constraint and its <c>d)</c> sub-item the universal-reference one.</param>
/// <param name="LastRule">The rule restricting the LAST phrase to a declarative or a PERFORM WHEN phrase, or 0
/// for a statement that has no LAST phrase.</param>
internal readonly record struct EcRaiseSite(string Verb, string Clause, int Level3Rule, int ObjectRule,
                                            int LastRule)
{
    /// <summary>RAISE (ISO §14.9.29.3). It has no RAISING phrase and therefore no EC-USER/LAST rules.</summary>
    public static readonly EcRaiseSite Raise = new("RAISE", "14.9.29.3", Level3Rule: 1, ObjectRule: 2, LastRule: 0);

    /// <summary>GOBACK RAISING (ISO §14.9.18.3) — including the method-context GOBACK.</summary>
    public static readonly EcRaiseSite Goback = new("GOBACK", "14.9.18.3", Level3Rule: 2, ObjectRule: 4, LastRule: 5);

    /// <summary>An EXIT statement's RAISING phrase (ISO §14.9.14.3): EXIT PROGRAM, EXIT FUNCTION, EXIT METHOD.
    /// One clause, one set of ordinals — the format differs, the syntax rules do not.</summary>
    public static EcRaiseSite Exit(string verb) =>
        new(verb, "14.9.14.3", Level3Rule: 3, ObjectRule: 5, LastRule: 6);

    /// <summary>The statement named as a message's subject: "RAISE", or "GOBACK RAISING" / "EXIT PROGRAM
    /// RAISING" for the phrase forms.</summary>
    public string Context => this == Raise ? Verb : $"{Verb} RAISING";

    /// <summary>This site's citation of one of its own rules — <c>Cite(Level3Rule)</c> → "ISO §14.9.14.3 SR3".
    /// <paramref name="sub"/> spells a sub-item ("a", "d").</summary>
    public string Cite(int rule, string sub = "") => $"ISO §{Clause} SR{rule}{sub}";
}
