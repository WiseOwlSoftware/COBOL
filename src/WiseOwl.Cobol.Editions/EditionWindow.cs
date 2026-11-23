// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Editions;

/// <summary>
/// ⛔ THE ONE EDITION WINDOW OF A CATALOGUED ELEMENT, AND THE ONE VERDICT IT GIVES (kb/Work PB2620). Three catalogs carry an
/// element's editions: the construct registry (<see cref="ConstructDialectStatus"/>), the intrinsic-function catalog
/// (<c>IntrinsicSig</c>, the D8 window) and the exception-name catalog (<c>EcInfo.IntroducedIn</c>). Each used to write the
/// window comparison itself, and the intrinsic arm decided its own severity, so a removed function was an error under
/// <c>--permissive</c> where every removed construct is a warning. Every catalog now hands its columns to this type, which
/// alone says where the element stands at an edition (<see cref="StatusAt"/>) and what the gate reports there
/// (<see cref="VerdictAt"/>, whose severity is <see cref="EditionSeverityPolicy"/>'s).
/// </summary>
/// <param name="IntroducedIn">The first edition that has the element (85/2002/2014/2023).</param>
/// <param name="RemovedIn">The first edition that removed it (null: never).</param>
/// <param name="ObsoleteIn">The first edition that marks it obsolete or archaic (null: never).</param>
public readonly record struct EditionWindow(int IntroducedIn, int? RemovedIn = null, int? ObsoleteIn = null)
{
    /// <summary>The availability verdict at <paramref name="edition"/> (the year as <c>--std</c> spells it).</summary>
    public ConstructAvailability StatusAt(int edition)
    {
        if (edition < IntroducedIn) return ConstructAvailability.NotYetIntroduced;
        if (RemovedIn is { } r && edition >= r) return ConstructAvailability.Removed;
        if (ObsoleteIn is { } o && edition >= o) return ConstructAvailability.Obsolete;
        return ConstructAvailability.Available;
    }

    /// <summary>True when the element is IN FORCE at <paramref name="edition"/>: introduced and not yet removed, whether or not
    /// flagged obsolete. An obsolete element is still a supported one (§1985:I.1.5.2.3: "A conforming implementation of Standard
    /// COBOL is required to support obsolete language elements"), so <see cref="ConstructAvailability.Obsolete"/> is in force:
    /// the ONE spelling of that rule for every site that branches on the edition fact.</summary>
    public bool IsInForceAt(int edition) =>
        StatusAt(edition) is ConstructAvailability.Available or ConstructAvailability.Obsolete;

    /// <summary>⛔ THE ONE REPORT VERDICT of a windowed element at <paramref name="edition"/>, or null when it is in force and
    /// unflagged: the availability, the severity <see cref="EditionSeverityPolicy"/> dictates for it (a removed element is an
    /// error strict and a warning under <c>--permissive</c>, whichever catalog holds it), the code the catalog reports that
    /// availability under (<paramref name="codes"/>: catalogs keep their own codes as data; an obsolete element is the one flag
    /// code <see cref="EditionCodes.ObsoleteFlag"/> in every catalog) and, for an obsolete or removed
    /// element, the edition flag (DESIGN-listing.md section 6.11) naming <paramref name="construct"/> as
    /// <paramref name="display"/>.</summary>
    public EditionVerdict? VerdictAt(EditionInfo edition, EditionGateCodes codes, string construct, string display)
    {
        var availability = StatusAt(edition.Year);
        if (availability == ConstructAvailability.Available) return null;
        var (code, flag) = availability switch
        {
            ConstructAvailability.NotYetIntroduced => (codes.NotYetIntroduced, (EditionFlag?)null),
            ConstructAvailability.Removed => (codes.Removed, new EditionFlag(EditionFlagKind.Removed, construct, display, RemovedIn!.Value)),
            ConstructAvailability.Obsolete => (EditionCodes.ObsoleteFlag, new EditionFlag(EditionFlagKind.Obsolete, construct, display, ObsoleteIn!.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(edition), availability, "an availability with no code"),
        };
        return new EditionVerdict(availability, EditionSeverityPolicy.For(availability, edition), code, flag);
    }
}

/// <summary>The codes one catalog reports the verdicts of its <see cref="EditionWindow"/> under (kb/Work PB2620): the codes are the
/// catalog's data, the verdict and its severity are the window's.</summary>
/// <param name="NotYetIntroduced">The code of an element the edition has not introduced.</param>
/// <param name="Removed">The code of an element the edition removed.</param>
public readonly record struct EditionGateCodes(string NotYetIntroduced, string Removed);
