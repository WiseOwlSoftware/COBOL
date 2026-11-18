// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Listing;

/// <summary>
/// What listing a compilation is asked for (DESIGN-listing.md sections 4.2 and 8). <c>CompilerDriver.Options.Listing</c> is a
/// <see cref="ListingRequest"/>, or null for no listing and no listing work. Every format renders the one document the builder
/// made (<see cref="ListingOutput"/>), so the formats cannot disagree. A request is never one the listing would fail: its
/// constructor, each <c>init</c> accessor and so every <c>with</c> expression ask the request's rules
/// (<see cref="PageLengthProblem"/>, <see cref="PageWidthProblem"/>, <see cref="FormatsProblem"/>, <see cref="SectionsProblem"/>) and throw
/// <see cref="ArgumentException"/> on a problem.
/// </summary>
/// <param name="OutputBase">Where the listing goes: <c>BASE</c> plus each format's extension (<see cref="ListingOutput.Extension"/>);
/// null for the output assembly's path without its extension; <see cref="StandardOutput"/> for no file (the caller prints the
/// one format requested itself).</param>
/// <param name="PageLength">Lines per page, the heading included; 0 for no pagination (only a page-eject line or a new
/// outermost program starts a page).</param>
/// <param name="PageWidth">Columns, <see cref="MinimumPageWidth"/> through <see cref="MaximumPageWidth"/>.</param>
/// <param name="Formats">The formats to write, at least one (<see cref="FormatsProblem"/>).</param>
public sealed record ListingRequest(string? OutputBase = null, int PageLength = ListingRequest.DefaultPageLength,
    int PageWidth = ListingRequest.DefaultPageWidth, ListingFormats Formats = ListingFormats.Text)
{
    private readonly string? _outputBase = Valid(FormatsProblem(OutputBase, Formats), nameof(Formats), OutputBase);
    private readonly int _pageLength = Valid(PageLengthProblem(PageLength), nameof(PageLength), PageLength);
    private readonly int _pageWidth = Valid(PageWidthProblem(PageWidth), nameof(PageWidth), PageWidth);
    private readonly ListingFormats _formats = Formats;

    /// <summary>Where the listing goes (the record's <c>OutputBase</c> parameter); with the formats, one problem at most
    /// (<see cref="FormatsProblem"/>).</summary>
    public string? OutputBase { get => _outputBase; init => _outputBase = Valid(FormatsProblem(value, _formats), nameof(OutputBase), value); }

    /// <summary>Lines per page (the record's <c>PageLength</c> parameter; <see cref="PageLengthProblem"/>).</summary>
    public int PageLength { get => _pageLength; init => _pageLength = Valid(PageLengthProblem(value), nameof(PageLength), value); }

    /// <summary>Columns (the record's <c>PageWidth</c> parameter; <see cref="PageWidthProblem"/>).</summary>
    public int PageWidth { get => _pageWidth; init => _pageWidth = Valid(PageWidthProblem(value), nameof(PageWidth), value); }

    /// <summary>The formats to write: never none, and one alone when the listing goes to standard output.</summary>
    public ListingFormats Formats { get => _formats; init => _formats = Valid(FormatsProblem(_outputBase, value), nameof(Formats), value); }

    private readonly ListingSectionSet _sections = ListingSectionSet.Standard;

    /// <summary>The sections to produce (<c>--listing-sections</c>; DESIGN-listing.md section 8): never none
    /// (<see cref="SectionsProblem"/>); <see cref="ListingSectionSet.Standard"/> unless set.</summary>
    public ListingSectionSet Sections { get => _sections; init => _sections = Valid(SectionsProblem(value), nameof(Sections), value); }

    /// <summary><paramref name="value"/>, or the <see cref="ArgumentException"/> its <paramref name="problem"/> is.</summary>
    private static T Valid<T>(string? problem, string parameter, T value) =>
        problem is null ? value : throw new ArgumentException(problem, parameter);

    /// <summary>The <see cref="OutputBase"/> that writes no file: the command line prints the text listing to standard output.</summary>
    public const string StandardOutput = "-";

    /// <summary>The default lines per page.</summary>
    public const int DefaultPageLength = 60;

    /// <summary>The default (and widest fully used) page width.</summary>
    public const int DefaultPageWidth = 132;

    /// <summary>The narrowest page: the source columns need 97, and the heading's instant and page number 40 (DESIGN-listing.md
    /// section 6.1: what yields when the first heading line does not fit).</summary>
    public const int MinimumPageWidth = 100;

    /// <summary>The widest page.</summary>
    public const int MaximumPageWidth = 255;

    /// <summary>The fewest lines a paginated page may have: its heading and one row.</summary>
    public const int MinimumPageLength = TextHeadingLines + 1;

    /// <summary>The lines of a text page's heading (DESIGN-listing.md section 6.1): the identification line, the section line,
    /// a blank line and the column ruler.</summary>
    public const int TextHeadingLines = 4;

    /// <summary>Whether the listing is written to files (false for <see cref="StandardOutput"/>).</summary>
    public bool WritesFiles => OutputBase != StandardOutput;

    /// <summary>Why <paramref name="pageLength"/> is no page length, or null when it is one: 0, or at least
    /// <see cref="MinimumPageLength"/>.</summary>
    public static string? PageLengthProblem(int pageLength) =>
        pageLength == 0 || pageLength >= MinimumPageLength ? null
            : string.Create(CultureInfo.InvariantCulture,
                $"a page length is 0 (no pagination) or at least {MinimumPageLength} (got {pageLength})");

    /// <summary>Why <paramref name="formats"/> are no formats for a listing written to <paramref name="outputBase"/>, or null
    /// when they are: at least one known format, and exactly one when the listing goes to standard output
    /// (<see cref="StandardOutput"/>), where two formats would interleave in one stream.</summary>
    public static string? FormatsProblem(string? outputBase, ListingFormats formats)
    {
        var each = ListingOutput.Each(formats);
        if ((formats & ~ListingOutput.All) != 0)
            return string.Create(CultureInfo.InvariantCulture, $"a listing format is one of {ListingOutput.Spellings} (got {(int)formats})");
        if (each.Count == 0) return "a listing is written in at least one format";
        if (outputBase == StandardOutput && each.Count > 1)
            return "a listing written to standard output (-) is written in one format (got "
                + string.Join(", ", each.Select(ListingOutput.Spelling)) + ")";
        return null;
    }

    /// <summary>Why <paramref name="sections"/> are no sections for a listing, or null when they are: at least one.</summary>
    public static string? SectionsProblem(ListingSectionSet sections) =>
        sections.IsEmpty ? "a listing has at least one section" : null;

    /// <summary>Why <paramref name="pageWidth"/> is no page width, or null when it is one.</summary>
    public static string? PageWidthProblem(int pageWidth) =>
        pageWidth is >= MinimumPageWidth and <= MaximumPageWidth ? null
            : string.Create(CultureInfo.InvariantCulture,
                $"a page width is {MinimumPageWidth} through {MaximumPageWidth} (got {pageWidth})");
}

/// <summary>
/// ⛔ THE ONE SPELLING OF THE COMPILATION INSTANT in a listing (DESIGN-listing.md section 5): ISO 8601 local time with its
/// offset, <c>2026-11-17 14:03:22 -05:00</c>. The page heading and the DATE-COMPILED replacement both use it, so they cannot
/// disagree; WHEN-COMPILED keeps its own 21-character form of the same instant.
/// </summary>
public static class ListingTimestamp
{
    /// <summary>The instant as the listing prints it.</summary>
    public static string Format(DateTimeOffset instant) =>
        instant.ToString("yyyy'-'MM'-'dd HH':'mm':'ss zzz", CultureInfo.InvariantCulture);

    /// <summary>The paragraph a DATE-COMPILED paragraph "is replaced during compilation with": <c>DATE-COMPILED. current date.</c>
    /// (§1985:VI.3.4.4 GR1), the current date being the compilation instant.</summary>
    public static string DateCompiledParagraph(DateTimeOffset instant) => "DATE-COMPILED. " + Format(instant) + ".";
}
