// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE STATEMENT AND THE CLAUSE THAT GOVERNS IT TRAVEL AS ONE VALUE (kb/Work PB388; CLAUDE.md rule 1).
/// RAISE (§2023:14.9.29), the GOBACK RAISING phrase (§2023:14.9.18) and the EXIT PROGRAM / FUNCTION / METHOD RAISING
/// phrase (§2023:14.9.14) are bound by ONE code path — <see cref="Procedure.EcBinder.EcBindRaising"/> over
/// <see cref="EcNameResolution.TryResolve"/> — and every rule that path enforces is written down THREE TIMES in
/// the standard, once per statement, under a DIFFERENT ordinal each time:
/// <list type="table">
///   <item><term>level-3 exception-name</term><description>EXIT SR3 · GOBACK SR2 · RAISE SR1</description></item>
///   <item><term>EC-USER named in the PD RAISING phrase</term><description>EXIT SR3 · GOBACK SR2 (RAISE: none —
///     it has no such rule)</description></item>
///   <item><term>identifier-1 is an object reference</term><description>EXIT SR5 · GOBACK SR4 · RAISE SR2, with
///     the declared-class constraint at <c>a)</c> and the universal-reference one at <c>d)</c></description></item>
///   <item><term>the LAST phrase</term><description>EXIT SR6 · GOBACK SR5 (RAISE: no LAST phrase)</description></item>
///   <item><term>not in a GLOBAL declarative</term><description>EXIT SR2 · GOBACK SR1 (RAISE: none — it does not
///     return from the source element)</description></item>
/// </list>
/// Before this type the VERB was threaded through that path and the CLAUSE was a literal in each message, so
/// every one of them printed GOBACK's or RAISE's ordinal at an EXIT statement — a wrong rule number shown to the
/// user, on legal and illegal source alike, and the exact shape of feedback_two_arm_dispatch. The citation is a
/// property OF THE SITE; making it one field of the value the site already passes is what makes a message
/// physically unable to name another statement's rule.
///
/// <para>⚠ The ordinals are pinned to the standard by <c>EcRaiseSiteDriftTests</c>, which re-derives each one
/// from <c>spec-rule-catalog.json</c> by matching the rule TEXT — so an edition that renumbers §2023:14.9.14.3 fails
/// a test instead of silently misdirecting a reader.</para>
/// <para>⛔ A MESSAGE CITES THE RULE OF THE EDITION COMPILED AT (kb/Work PB4950, PB6175): <see cref="Cite"/> takes the edition and
/// a <see cref="EcRaiseRule"/>, never an ordinal of one edition. COBOL-2002 (§2002:14.8.13.2, 14.8.17.2, 14.8.28.2) and COBOL-2014
/// (§2014:14.9.13.2, 14.9.17.2, 14.9.28.2) number these rules as COBOL-2023 does, in clauses of their own; the one exception is
/// GOBACK at COBOL-2002, whose clause states only the GLOBAL-declarative rule (§2002:14.8.17.2 SR1) and whose GR1 has it operate
/// "as if executing an EXIT PROGRAM statement with the RAISING phrase" (§2002:14.8.17.3 GR1), so its RAISING phrase answers to
/// EXIT's rules there. COBOL-85 has none of these statements but EXIT PROGRAM, and no RAISING phrase: its construct gates refuse
/// them, and a message that also speaks cites the rule of COBOL-2002, which introduced them
/// (<see cref="EditionInfo.CiteRuleFrom2002"/>). <c>EcRaiseSiteDriftTests</c> re-derives the 2002 and 2014 ordinals from those
/// editions' texts too.</para>
/// </summary>
/// <param name="Verb">The statement as the user wrote it, and as every message names it ("RAISE", "GOBACK",
/// "EXIT PROGRAM", "EXIT FUNCTION", "EXIT METHOD").</param>
/// <param name="Clause">The COBOL-2023 SYNTAX RULE block that governs this statement's RAISING operands; the ordinals below
/// are its.</param>
/// <param name="Clause2002">The same block of COBOL-2002, which numbers the rules alike.</param>
/// <param name="Clause2014">The same block of COBOL-2014, which numbers the rules alike.</param>
/// <param name="Level3Rule">The rule requiring a level-3 exception-name (and, where it exists, requiring an
/// EC-USER name to appear in the procedure division header's RAISING phrase).</param>
/// <param name="ObjectRule">The rule constraining identifier-1 to an object reference. Its <c>a)</c> sub-item is
/// the declared-class-in-the-PD-header constraint and its <c>d)</c> sub-item the universal-reference one.</param>
/// <param name="LastRule">The rule restricting the LAST phrase to a declarative or a PERFORM WHEN phrase, or 0
/// for a statement that has no LAST phrase.</param>
/// <param name="GlobalDeclarativeRule">The rule forbidding the STATEMENT inside a declarative procedure whose USE
/// statement carries the GLOBAL phrase, or 0 for a statement that has no such rule. ⛔ It is a rule about the
/// STATEMENT, not about its RAISING phrase — carried here because it is the same (statement → clause → ordinal)
/// mapping this value exists to keep honest (kb/Work PB404, PB409), and a bare EXIT PROGRAM / GOBACK with no
/// RAISING phrase asks it too.</param>
/// <param name="GlobalDeclarativeRule1985">COBOL-85's own GLOBAL-declarative rule, for the one statement of these that edition
/// has (EXIT PROGRAM, §1985:X.5.4.3 SR2), or null.</param>
/// <param name="RaisingRulesAreExitProgramsAt2002">True for GOBACK: at COBOL-2002 its RAISING phrase answers to EXIT PROGRAM's
/// rules (§2002:14.8.17.3 GR1), its own clause stating none.</param>
internal readonly record struct EcRaiseSite(string Verb, string Clause, string Clause2002, string Clause2014, int Level3Rule,
                                            int ObjectRule, int LastRule, int GlobalDeclarativeRule = 0,
                                            string? GlobalDeclarativeRule1985 = null, bool RaisingRulesAreExitProgramsAt2002 = false)
{
    /// <summary>RAISE (ISO §2023:14.9.29.3). It has no RAISING phrase and therefore no EC-USER/LAST rules, and it does
    /// not return from the source element, so §2023:14.9.29.3 states no GLOBAL-declarative prohibition either.</summary>
    public static readonly EcRaiseSite Raise = new("RAISE", "14.9.29.3", "14.8.28.2", "14.9.28.2", Level3Rule: 1, ObjectRule: 2,
                                                   LastRule: 0);

    /// <summary>GOBACK RAISING (ISO §2023:14.9.18.3) — including the method-context GOBACK.</summary>
    public static readonly EcRaiseSite Goback = new("GOBACK", "14.9.18.3", "14.8.17.2", "14.9.17.2", Level3Rule: 2, ObjectRule: 4,
                                                    LastRule: 5, GlobalDeclarativeRule: 1, RaisingRulesAreExitProgramsAt2002: true);

    /// <summary>An EXIT statement's RAISING phrase (ISO §2023:14.9.14.3): EXIT PROGRAM, EXIT FUNCTION, EXIT METHOD.
    /// One clause, one set of ordinals — the format differs, the syntax rules do not.</summary>
    public static EcRaiseSite Exit(string verb) =>
        new(verb, "14.9.14.3", "14.8.13.2", "14.9.13.2", Level3Rule: 3, ObjectRule: 5, LastRule: 6, GlobalDeclarativeRule: 2,
            GlobalDeclarativeRule1985: verb == "EXIT PROGRAM" ? "§1985:X.5.4.3 SR2" : null);

    /// <summary>The statement named as a message's subject: "RAISE", or "GOBACK RAISING" / "EXIT PROGRAM
    /// RAISING" for the phrase forms.</summary>
    public string Context => this == Raise ? Verb : $"{Verb} RAISING";

    /// <summary>The COBOL-2023 ordinal of <paramref name="rule"/> in <see cref="Clause"/> (0: the site has no such rule).</summary>
    public int Ordinal(EcRaiseRule rule) => rule switch
    {
        EcRaiseRule.Level3 => Level3Rule,
        EcRaiseRule.Object => ObjectRule,
        EcRaiseRule.Last => LastRule,
        EcRaiseRule.GlobalDeclarative => GlobalDeclarativeRule,
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "no such EcRaiseRule"),
    };

    /// <summary>⛔ This site's citation of one of its rules AS THE EDITION COMPILED AT STATES IT (kb/Work PB4950, PB6175):
    /// <c>Cite(e, EcRaiseRule.Level3)</c> → "ISO §2023:14.9.14.3 SR3" at COBOL-2023, "ISO §2014:14.9.13.2 SR3" at 2014,
    /// "ISO §2002:14.8.13.2 SR3" at 2002 and at COBOL-85, which has no RAISING phrase (see the type's remarks). The
    /// GLOBAL-declarative rule of EXIT PROGRAM at COBOL-85 is that edition's own. <paramref name="sub"/> spells a sub-item
    /// ("a", "d").</summary>
    public string Cite(EditionInfo edition, EcRaiseRule rule, string sub = "")
    {
        int ordinal = Ordinal(rule);
        if (ordinal == 0) throw new ArgumentOutOfRangeException(nameof(rule), rule, $"{Verb} has no {rule} rule");
        string y2002 = RaisingRulesAreExitProgramsAt2002 && rule != EcRaiseRule.GlobalDeclarative
            ? $"§2002:{ExitProgram.Clause2002} SR{ExitProgram.Ordinal(rule)}{sub}, by §2002:14.8.17.3 GR1"
            : $"§2002:{Clause2002} SR{ordinal}{sub}";
        string y2014 = $"§2014:{Clause2014} SR{ordinal}{sub}";
        string later = $"§2023:{Clause} SR{ordinal}{sub}";
        return rule == EcRaiseRule.GlobalDeclarative && GlobalDeclarativeRule1985 is { } own1985
            ? edition.CiteRule(own1985, later, y2002, y2014)
            : edition.CiteRuleFrom2002(y2002, later, y2014);
    }

    /// <summary>EXIT PROGRAM, whose rules GOBACK's RAISING phrase answers to at COBOL-2002.</summary>
    private static EcRaiseSite ExitProgram => Exit("EXIT PROGRAM");
}

/// <summary>The rules an <see cref="EcRaiseSite"/> cites, by what they require rather than by any edition's ordinal.</summary>
internal enum EcRaiseRule
{
    /// <summary>A level-3 exception-name (and an EC-USER name in the procedure division header's RAISING phrase).</summary>
    Level3,
    /// <summary>Identifier-1 is an object reference (sub-items a) to d)).</summary>
    Object,
    /// <summary>The LAST phrase only in a declarative (or, from 2023, a PERFORM WHEN phrase).</summary>
    Last,
    /// <summary>The statement not in a declarative whose USE statement carries the GLOBAL phrase.</summary>
    GlobalDeclarative,
}
