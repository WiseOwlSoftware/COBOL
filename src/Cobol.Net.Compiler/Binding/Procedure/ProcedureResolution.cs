// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>Which uniqueness rule a procedure-name reference broke. The two are separated because their
/// REPAIRS differ, and a diagnostic that cannot tell them apart tells the user to do something impossible
/// (kb/Work PB466).</summary>
internal enum ProcedureAmbiguityRule
{
    /// <summary>ISO §8.4.2.2.1 rule 1 is false and no rule 2-6 excuse applies, so §8.4.2.2.3 SR1 requires
    /// qualification: more than one procedure in the source element carries the spelling, and the reference
    /// wrote no qualifier (or wrote one that is itself duplicated).</summary>
    Qualification,

    /// <summary>ISO §8.4.2.2.3 SR7 — "If explicitly referenced, a paragraph-name shall not be duplicated within
    /// a section." No qualifier exists that could separate the two, since §8.4.2.2.2 format 4 offers a
    /// paragraph-name only its section-name and both declarations share it.</summary>
    InSectionDuplicate,
}

/// <summary>What KIND of statement writes a procedure-name reference — the one fact ISO §14.9.49.3 SR3 and SR4
/// turn on. The declaratives boundary may be crossed by a reference only in two directions and only by one verb
/// each: SR3 ("Within a declarative procedure, there shall be no reference to any nondeclarative procedures except
/// in a RESUME statement") lets a RESUME reach OUT of a declarative, and SR4 ("Procedure-names within a
/// declarative section may be referenced in a different declarative section or in a nondeclarative procedure only
/// with a PERFORM statement") lets a PERFORM reach IN.
/// <para>⛔ <see cref="Other"/> IS THE DEFAULT, AND THAT IS THE RULE (kb/Work PB362): every statement except those
/// two is restricted, so a verb added later that takes a procedure-name inherits both restrictions without being
/// taught them. Only the two exempt verbs have to say who they are.</para></summary>
internal enum ProcedureReferenceKind
{
    /// <summary>Any statement other than PERFORM and RESUME — GO TO (both formats), ALTER, and the SORT/MERGE
    /// INPUT and OUTPUT PROCEDURE phrases today.</summary>
    Other,

    /// <summary>A PERFORM statement's procedure-name-1 / procedure-name-2 — SR4's exemption.</summary>
    Perform,

    /// <summary>A RESUME AT procedure-name-1 — SR3's exemption.</summary>
    Resume,
}

/// <summary>An unresolvable procedure-name reference, with everything the ONE reporting step needs to name the
/// rule and the candidates. Carried out of the resolver rather than reported inside it, because the resolver is
/// also the QUIET prescan path.</summary>
/// <param name="Rule">Which uniqueness rule failed.</param>
/// <param name="Name">The spelling as written — the paragraph-name, or the qualifying section-name when the
/// qualifier itself is what is duplicated.</param>
/// <param name="Candidates">Every declaration carrying that spelling, described for the message in declaration
/// order.</param>
/// <param name="Section">For <see cref="ProcedureAmbiguityRule.InSectionDuplicate"/>, the section that declares
/// the name twice — the scope SR7 is written about. Null for the rule-1 case, whose scope is the whole source
/// element.</param>
internal readonly record struct ProcedureAmbiguity(
    ProcedureAmbiguityRule Rule, string Name, IReadOnlyList<string> Candidates, string? Section = null);

/// <summary>A procedure-name reference that RESOLVED — the whole answer, not a naked pc pair: the inclusive pc
/// <paramref name="Range"/> the reference denotes, plus the <paramref name="Section"/> that OWNS the denoted
/// procedure (the section itself for a section-name; the containing section for a paragraph; null for a
/// paragraph written outside every section).
/// <para>⛔ THE SECTION TRAVELS WITH THE RESOLUTION (kb/Work PB433). A pc pair has already discarded which
/// section the name came from, and several rules are written about exactly that — ISO §14.9.28.3 SR11 ("When
/// procedure-name-1 and procedure-name-2 are both specified and either is the name of a procedure in the
/// declaratives portion of the procedure division, both shall be procedure-names in the same declarative
/// section") and the analogous constraints on GO TO, ALTER and the SORT/MERGE procedure phrases. A check bolted
/// on after the fact must re-derive the section from the numbers, and every later rule re-derives it
/// again.</para></summary>
internal readonly record struct ResolvedProcedure(PcRange Range, SectionInfo? Section)
{
    /// <summary>Is the denoted procedure in the DECLARATIVES portion (ISO §14.3)? A paragraph outside every
    /// section cannot be: the declaratives portion consists of sections (§14.3 format).</summary>
    public bool IsDeclarative => Section is { IsDeclarative: true };
}

/// <summary>The outcome of resolving one procedure-name reference: the resolved procedure, or nothing plus (when
/// the reference identified MORE than one procedure rather than none) the reason.
/// <para>⛔ Both null is "no procedure of that name" — the COBOLNET1639 case. A non-null
/// <paramref name="Ambiguity"/> is the opposite failure, and the two must not be collapsed: telling a user that
/// no paragraph carries a name their program declares twice sends them looking for the wrong thing.</para></summary>
internal readonly record struct ProcedureResolution(ResolvedProcedure? Procedure, ProcedureAmbiguity? Ambiguity)
{
    /// <summary>The pc range alone — what the QUIET prescan path and the range-only operands want.</summary>
    public PcRange? Range => Procedure?.Range;
}
