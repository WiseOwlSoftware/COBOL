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

/// <summary>The outcome of resolving one procedure-name reference: a pc range, or nothing plus (when the
/// reference identified MORE than one procedure rather than none) the reason.
/// <para>⛔ Both null is "no procedure of that name" — the COBOLNET1639 case. A non-null
/// <paramref name="Ambiguity"/> is the opposite failure, and the two must not be collapsed: telling a user that
/// no paragraph carries a name their program declares twice sends them looking for the wrong thing.</para></summary>
internal readonly record struct ProcedureResolution(PcRange? Range, ProcedureAmbiguity? Ambiguity);
