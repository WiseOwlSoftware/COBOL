// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Editions;

/// <summary>A written exception-name the targeted edition does not have: the catalog descriptor to report under and the
/// message. Both halves of the report ride together so every channel that prints it (the front end's
/// <c>DiagnosticBag</c>, the binder's <c>EditionContext</c>) prints the same text under the same code.</summary>
public readonly record struct ExceptionNameViolation(DiagnosticDescriptor Descriptor, string Message);

/// <summary>
/// ⛔ THE ONE HOME OF "IS THIS WRITTEN WORD AN EXCEPTION-NAME THE TARGETED EDITION HAS" (kb/Work PB2740, R05). ISO
/// §2023:7.3.25.3 SR2 — "Exception-name-1 shall be one of the exception names listed in 14.6.13.1, Exception conditions" —
/// is a SYNTAX rule of the <c>&gt;&gt;TURN</c> directive, and §2023:7.2.1 requires a compiler directive to be "syntactically
/// correct in the initial source text and library text", the false path of a <c>&gt;&gt;IF</c> included. Two channels
/// therefore have to ask the same question and print the same answer: the directive stage
/// (<c>TurnDirectiveProcessor.ParseTurn</c>, which a COMPILED line and an OMITTED-branch line both reach) and the
/// binder's written-name funnel (<c>EcNameResolution.TryResolve</c>: USE, RAISE, RAISING, the PERFORM WHEN list). The
/// rule used to live only in the binder, which reads compiled events alone, so
/// <c>&gt;&gt;IF 1 = 0 / &gt;&gt;TURN EC-NOSUCH CHECKING ON / &gt;&gt;END-IF</c> compiled clean while the same directive
/// outside the <c>&gt;&gt;IF</c> was COBOL0711.
/// <para>The rule lives BELOW both channels (the front end cannot reference the binder), in the assembly that owns the
/// diagnostic catalog, so the next written-name site calls it and cannot write a third copy. What stays in the binder
/// is the part that is not syntax: the level test of the RAISE contexts, the declined-module postures and the
/// §2023:15.33 width advisory (<c>EcNameResolution.Advise</c>) — they need the compile's options, and an omitted branch is
/// never compiled.</para>
/// </summary>
public static class ExceptionNameRule
{
    /// <summary>Resolve <paramref name="raw"/> against the §2023:14.6.13.1 catalog (and the open EC-USER- family,
    /// §2023:14.6.13.1.1). False with <see cref="DiagnosticCatalog.EcNameUnknown"/> (COBOL0711) when it is neither;
    /// <paramref name="where"/> names the construct that wrote it (<c>&gt;&gt;TURN</c>, <c>RAISE</c>, …).</summary>
    public static bool TryGet(string raw, string where, out EcInfo info, out ExceptionNameViolation violation)
    {
        if (ExceptionCatalog.TryGet(raw, out info))
        {
            violation = default;
            return true;
        }
        violation = new ExceptionNameViolation(DiagnosticCatalog.EcNameUnknown,
            $"{where}: '{raw}' is not an exception-name of ISO/IEC 1989 §2023:14.6.13.1 (and not a valid EC-USER- name; "
            + "this implementation defines no EC-IMP- name, DOC-A.1-99)");
        return false;
    }

    /// <summary>The per-name edition window: the 2023-only families (VERSION_CHANGE_REFERENCE rows 40/61) are not
    /// exception-names at <paramref name="dialectLevel"/> 2002/2014. Null when the name exists there, else
    /// <see cref="DiagnosticCatalog.EcNameIntroducedLater"/> (COBOL0878).</summary>
    /// <remarks>The availability is the name's <see cref="EditionWindow"/>'s (kb/Work PB2620): the catalog carries an
    /// introduction edition only, so the one verdict it can give is not-yet-introduced, an error at every severity axis.</remarks>
    public static ExceptionNameViolation? Window(EcInfo info, int dialectLevel, string where) =>
        new EditionWindow(info.IntroducedIn).StatusAt(dialectLevel) == ConstructAvailability.NotYetIntroduced
            ? new ExceptionNameViolation(DiagnosticCatalog.EcNameIntroducedLater,
                $"{where}: exception-name {info.Name} was introduced by ISO/IEC 1989:{info.IntroducedIn} — it "
                + $"requires --std {info.IntroducedIn} or later (targeting COBOL-{dialectLevel})")
            : null;

    /// <summary>⛔ THE CONDITIONS THAT HAVE A DIFFERENT NAME BEFORE THE EDITION THAT INTRODUCED THEIRS (kb/Work PB6107): the 2002 text states
    /// one omitted-argument condition for a called program, an activated function and an invoked method, <c>EC-PROGRAM-ARG-OMITTED</c>
    /// (§2002:8.4.2.2.3 GR7, §2002:14.8.22.3 GR10, §2002:14.8.4.3 GR12), and one not-found condition for a program and a user function,
    /// <c>EC-PROGRAM-NOT-FOUND</c> (§2002:8.4.2.2.3 GR5); §2014 (§2014:E.2 item 18) and §2023 split each by element kind
    /// (<c>EC-FUNCTION-ARG-OMITTED</c> §2023:8.4.3.2.4 GR8, <c>EC-OO-ARG-OMITTED</c> §2023:14.9.23.4 GR10, <c>EC-FUNCTION-NOT-FOUND</c>
    /// §2023:8.4.3.2.4 GR6b). Keyed by the later name, valued by the name the editions before <see cref="EcInfo.IntroducedIn"/> use.
    /// Read only by <see cref="NameInEdition"/>.</summary>
    private static readonly FrozenDictionary<string, string> EarlierNames = new Dictionary<string, string>(CobolNames.Comparer)
    {
        ["EC-FUNCTION-ARG-OMITTED"] = "EC-PROGRAM-ARG-OMITTED",
        ["EC-OO-ARG-OMITTED"] = "EC-PROGRAM-ARG-OMITTED",
        ["EC-FUNCTION-NOT-FOUND"] = "EC-PROGRAM-NOT-FOUND",
    }.ToFrozenDictionary(CobolNames.Comparer);

    /// <summary>⛔ THE ONE ANSWER TO "WHICH EXCEPTION-NAME DOES THE EDITION'S TEXT RAISE FOR THIS CONDITION" (kb/Work PB6107):
    /// <paramref name="name"/> — the condition's name from 2014 or 2023 — when <paramref name="dialectLevel"/> is an edition that has
    /// it (its <see cref="EditionWindow"/> is the one <see cref="Window"/> asks), else the name the earlier edition raises for the same
    /// event (<c>EC-FUNCTION-NOT-FOUND</c> is <c>EC-PROGRAM-NOT-FOUND</c> at 2002). The binder declares and gates a raise, and the emitters
    /// write it, through this, so a program written for 2002 enables, catches and reads the name its own standard defines, and the next
    /// edition-dependent name is one row of <see cref="EarlierNames"/>.</summary>
    public static string NameInEdition(string name, int dialectLevel) =>
        EarlierNames.TryGetValue(name, out string? earlier) && ExceptionCatalog.TryGet(name, out var info)
        && new EditionWindow(info.IntroducedIn).StatusAt(dialectLevel) == ConstructAvailability.NotYetIntroduced
            ? earlier
            : name;

    /// <summary>The (later name, earlier name) pairs of <see cref="NameInEdition"/>, for the drift test that each earlier name is
    /// catalogued and introduced before its later one.</summary>
    public static IEnumerable<(string Later, string Earlier)> EditionRenames => EarlierNames.Select(kv => (kv.Key, kv.Value));

    /// <summary>The exception-name a reference to a formal of an element of <paramref name="element"/> raises in the edition
    /// <paramref name="dialectLevel"/>: <see cref="OmittedFormal.ConditionName"/> through <see cref="NameInEdition"/>. What the compiler
    /// enables (<c>&gt;&gt;TURN</c>) and declares per statement.</summary>
    public static string OmittedArgumentConditionName(ActivatedElementKind element, int dialectLevel) =>
        NameInEdition(OmittedFormal.ConditionName(element), dialectLevel);

    /// <summary>The kind whose raise a reference to a formal of <paramref name="element"/> makes in the edition
    /// <paramref name="dialectLevel"/> — the kind whose <see cref="OmittedFormal.ConditionName"/> is
    /// <see cref="OmittedArgumentConditionName"/>: the element's own from 2014, <see cref="ActivatedElementKind.Program"/> at 2002, whose
    /// one condition the function and the method share. The compiler emits THIS kind into every guard, so
    /// <see cref="OmittedFormal.Raise"/> reaches the helper and the checking flag of the condition the edition defines.</summary>
    public static ActivatedElementKind OmittedArgumentRaisedKind(ActivatedElementKind element, int dialectLevel)
    {
        string name = OmittedArgumentConditionName(element, dialectLevel);
        foreach (var kind in Enum.GetValues<ActivatedElementKind>())
            if (OmittedFormal.ConditionName(kind) == name)
                return kind;
        throw new ArgumentOutOfRangeException(nameof(element), element, $"no kind raises {name}");
    }
}
