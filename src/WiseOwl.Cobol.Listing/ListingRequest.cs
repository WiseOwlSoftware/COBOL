// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace WiseOwl.Cobol.Listing;

/// <summary>
/// What listing a compilation is asked for (DESIGN-listing.md sections 4.2 and 8). <c>CompilerDriver.Options.Listing</c> is a
/// <see cref="ListingRequest"/>, or null for no listing and no listing work. Step 1 writes the paginated text listing.
/// </summary>
/// <param name="OutputBase">Where the listing goes: <c>BASE.lst</c>; null for the output assembly's path without its
/// extension; <see cref="StandardOutput"/> for no file (the caller prints the text listing itself).</param>
/// <param name="PageLength">Lines per page, the heading included; 0 for no pagination (only a page-eject line or a new
/// outermost program starts a page).</param>
/// <param name="PageWidth">Columns, <see cref="MinimumPageWidth"/> through <see cref="MaximumPageWidth"/>.</param>
public sealed record ListingRequest(string? OutputBase = null, int PageLength = ListingRequest.DefaultPageLength,
    int PageWidth = ListingRequest.DefaultPageWidth)
{
    /// <summary>The <see cref="OutputBase"/> that writes no file: the command line prints the text listing to standard output.</summary>
    public const string StandardOutput = "-";

    /// <summary>The extension of the text listing file.</summary>
    public const string TextExtension = ".lst";

    /// <summary>The default lines per page.</summary>
    public const int DefaultPageLength = 60;

    /// <summary>The default (and widest fully used) page width.</summary>
    public const int DefaultPageWidth = 132;

    /// <summary>The narrowest page: the source columns need 97, the heading a little more.</summary>
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
