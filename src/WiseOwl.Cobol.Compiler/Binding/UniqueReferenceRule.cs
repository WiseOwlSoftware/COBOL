// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// ⛔ THE RULE THAT A REFERENCE IDENTIFIES ONE RESOURCE, IN THE EDITION IN FORCE (kb/Work PB4950): a message that says a
/// reference identifies no resource, or the wrong kind of one, cites <see cref="Citation"/> and quotes <see cref="Quote"/>
/// through <see cref="EditionInfo.Cite"/>, so a program compiled at <c>--std 85</c> reads the 1985 rule and one compiled
/// later reads §2023:8.4.2.1. The ONE place the two wordings are written: every undefined-name, wrong-kind-of-name and
/// unidentified-procedure message asks it.
/// <para>The 1985 rule is §1985:IV.4.3.8, Uniqueness of Reference: "In order to use a resource, a statement in a COBOL
/// program must contain a reference which uniquely identifies that resource".</para>
/// </summary>
internal static class UniqueReferenceRule
{
    /// <summary>The rule's citation in the edition form, without the "ISO " lead-in.</summary>
    public static string Citation(this EditionInfo edition) =>
        edition.Cite("§1985:IV.4.3.8", "§2023:8.4.2.1");

    /// <summary>The words the rule is quoted by. <paramref name="inOrderTo"/> selects the longer 2023 sentence that opens
    /// "In order to use a resource,".</summary>
    public static string Quote(this EditionInfo edition, bool inOrderTo = false) =>
        edition.Cite(inOrderTo
                ? "In order to use a resource, a statement in a COBOL program must contain a reference which uniquely identifies that resource"
                : "a statement in a COBOL program must contain a reference which uniquely identifies that resource",
            inOrderTo ? "In order to use a resource, a statement shall contain a reference that uniquely identifies that resource"
                      : "a statement shall contain a reference that uniquely identifies that resource");

    /// <summary><c>ISO §2023:8.4.2.1: "a statement shall contain …"</c>, the colon form every message of the colon arm uses.</summary>
    public static string Quoted(this EditionInfo edition) => $"{EditionInfo.Designated(edition.Citation())}: \"{edition.Quote()}\"";

    /// <summary><c>§2023:8.4.2.1 — "In order to use a resource, …"</c> (no "ISO" lead-in), the dash form of the procedure-name messages.</summary>
    public static string QuotedDash(this EditionInfo edition) => $"{edition.Citation()} — \"{edition.Quote(inOrderTo: true)}\"";
}
