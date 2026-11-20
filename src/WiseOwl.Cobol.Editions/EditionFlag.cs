// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Editions;

/// <summary>What an edition flag says of the construct it names (DESIGN-listing.md section 6.11).</summary>
public enum EditionFlagKind
{
    /// <summary>The construct is obsolete or archaic in the edition in force (COBOL0903; ISO §2023:4.2.12, §2023:4.2.13): still
    /// conforming, flagged by a warning.</summary>
    Obsolete,
    /// <summary>The edition in force removed the construct (COBOL0902, or the code its registry row pins): an error, or a
    /// warning that keeps the meaning the construct had under <c>--permissive</c>.</summary>
    Removed,
    /// <summary>The construct is a nonstandard extension this compiler accepts (COBOL2894 under <c>--flag-extensions</c>; ISO
    /// §2023:4.2.10).</summary>
    Extension,
}

/// <summary>
/// ⛔ THE FACT THAT A DIAGNOSTIC IS AN EDITION FLAG (DESIGN-listing.md section 6.11; kb/Work PB5947), carried on the diagnostic
/// from the one place the flag is raised: <see cref="ConstructRegistry.VerdictAt"/> for an obsolete or removed construct of the
/// construct registry, <c>EditionContext.Extension</c> for a nonstandard extension of the extension register. A reader that
/// groups the flags (the listing's table of flagged constructs by edition) reads this record, never the code or the message, so
/// a construct added to either register is flagged and grouped by construction.
/// </summary>
/// <param name="Kind">Obsolete, removed or extension.</param>
/// <param name="Construct">The id of the register row that names the construct (<see cref="ConstructDialectStatus.Id"/> or
/// <see cref="NonstandardExtension.Id"/>).</param>
/// <param name="Description">The construct as a user would name it (the row's <c>Display</c>).</param>
/// <param name="Edition">The edition, as <c>--std</c> spells its year, whose rule makes the construct what <paramref name="Kind"/>
/// says: the edition that made it obsolete, the edition that removed it, or, for an extension, the edition in force, whose
/// standard does not define it.</param>
public sealed record EditionFlag(EditionFlagKind Kind, string Construct, string Description, int Edition)
{
    /// <summary>The flag of registry row <paramref name="row"/> at <paramref name="verdict"/>: obsolete as of its
    /// <see cref="ConstructDialectStatus.ObsoleteIn"/>, removed in its <see cref="ConstructDialectStatus.RemovedIn"/>; null for a
    /// verdict that flags nothing (in force, or not yet introduced, which is a refusal and no flag).</summary>
    public static EditionFlag? Of(ConstructDialectStatus row, ConstructAvailability verdict) => verdict switch
    {
        ConstructAvailability.Obsolete => new(EditionFlagKind.Obsolete, row.Id, row.Display,
            row.ObsoleteIn ?? throw new InvalidOperationException($"construct '{row.Id}' is obsolete at no edition")),
        ConstructAvailability.Removed => new(EditionFlagKind.Removed, row.Id, row.Display,
            row.RemovedIn ?? throw new InvalidOperationException($"construct '{row.Id}' is removed at no edition")),
        ConstructAvailability.Available or ConstructAvailability.NotYetIntroduced => null,
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "an availability with no flag decision"),
    };

    /// <summary>The flag of extension register row <paramref name="row"/>, used in a compilation of the edition whose year
    /// <c>--std</c> spells as <paramref name="edition"/>.</summary>
    public static EditionFlag Of(NonstandardExtension row, int edition) =>
        new(EditionFlagKind.Extension, row.Id, row.Display, edition);
}

/// <summary>The construct registry's answer for one construct at one edition, when it reports anything
/// (<see cref="ConstructRegistry.VerdictAt"/>): the severity, the code and, for an obsolete or removed construct, the flag.</summary>
/// <param name="Availability">The construct's availability at the edition: never <see cref="ConstructAvailability.Available"/>.</param>
/// <param name="Severity">Error or warning, from <see cref="EditionSeverityPolicy"/>.</param>
/// <param name="Code">The code the report carries.</param>
/// <param name="Flag">The edition flag, or null for a construct the edition has not introduced.</param>
public readonly record struct EditionVerdict(ConstructAvailability Availability, EditionSeverity Severity, string Code, EditionFlag? Flag);
