// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Listing;

/// <summary>Where the value of a row of the options section comes from.</summary>
public enum ListingOptionOrigin
{
    /// <summary>An option the compilation was not given a value for: its default.</summary>
    Default,
    /// <summary>An option given a value other than its default.</summary>
    Specified,
    /// <summary>No option: a fact of the compiler or of the host the compilation ran on (a version, the operating system).</summary>
    Host,
}

/// <summary>One row of the options section (DESIGN-listing.md section 6.4).</summary>
/// <param name="Name">The option as the command line spells it (<c>--std</c>), or the name of the fact (<c>source</c>,
/// <c>compiler</c>).</param>
/// <param name="Value">Its value, as the command line spells it where it has a spelling; for a default that is derived from
/// another value (the output path), the value derived.</param>
/// <param name="Origin">Default, specified, or a fact of the compiler or the host.</param>
public sealed record ListingOption(string Name, string Value, ListingOptionOrigin Origin);

/// <summary>The statistics section (DESIGN-listing.md section 6.8). It holds no timing, so two listings of one source differ
/// only where the compilations did.</summary>
/// <param name="Lines">Every line of the listing, counted.</param>
/// <param name="Files">The lines of each file read, counted, in the order of the files: a library text copied twice counts
/// each copy.</param>
/// <param name="Diagnostics">The diagnostics the compilation reported, counted by severity, every severity once.</param>
/// <param name="Parse">The programs, statements and verbs the parse read, or null when the source was not parsed.</param>
public sealed record ListingStatistics(ListingLineCounts Lines, IReadOnlyList<ListingFileStatistics> Files,
    IReadOnlyList<ListingSeverityCount> Diagnostics, ListingParseStatistics? Parse);

/// <summary>A count of listing lines.</summary>
/// <param name="Lines">The lines.</param>
/// <param name="Kinds">The lines of each kind, every kind once, in the order of <see cref="ListingLineKind"/>.</param>
/// <param name="Omitted">The lines a false conditional-compilation branch omitted (<see cref="ListingLine.Omitted"/>).</param>
/// <param name="Copied">The lines of library text a COPY statement copied (<see cref="ListingLine.Inclusion"/> set).</param>
/// <param name="Unlisted">The lines the source section does not print (<see cref="ListingLine.Listed"/> false): a LISTING
/// directive or COPY SUPPRESS suppressed them.</param>
public sealed record ListingLineCounts(int Lines, IReadOnlyList<ListingKindCount> Kinds, int Omitted, int Copied, int Unlisted);

/// <summary>The lines of one kind.</summary>
/// <param name="Kind">The kind.</param>
/// <param name="Count">The lines of that kind.</param>
public sealed record ListingKindCount(ListingLineKind Kind, int Count);

/// <summary>The lines of one file.</summary>
/// <param name="File">The <see cref="ListingFile.Id"/>.</param>
/// <param name="Lines">Its lines, counted: each listing line read from the file, so each inclusion of a library text counts.</param>
public sealed record ListingFileStatistics(int File, ListingLineCounts Lines);

/// <summary>The diagnostics of one severity.</summary>
/// <param name="Severity">The severity.</param>
/// <param name="Count">The diagnostics of that severity.</param>
public sealed record ListingSeverityCount(ListingSeverity Severity, int Count);

/// <summary>What the parse read: programs, statements and verbs (DESIGN-listing.md section 6.8). Read off the parse tree, so
/// the verbs are counted as the programmer wrote them, and they are there when the bind failed.</summary>
/// <param name="Programs">The source units of the program map (<see cref="ListingDocument.Programs"/>).</param>
/// <param name="Statements">The statements, a statement nested in another counted as well.</param>
/// <param name="Verbs">Each verb, by the word that begins its statements, in the ordinal order of that word.</param>
public sealed record ListingParseStatistics(int Programs, int Statements, IReadOnlyList<ListingVerb> Verbs);

/// <summary>One verb of the verb counts (IBM's VBREF).</summary>
/// <param name="Verb">The word that begins the statements, in upper case (<c>MOVE</c>, <c>GO</c>, <c>NEXT</c>).</param>
/// <param name="Count">The statements it begins.</param>
/// <param name="Lines">The listing line each of those statements begins on, in listing order, one entry per statement (a line
/// that begins two of them appears twice); a statement on no line the listing holds has none.</param>
public sealed record ListingVerb(string Verb, int Count, IReadOnlyList<int> Lines);
