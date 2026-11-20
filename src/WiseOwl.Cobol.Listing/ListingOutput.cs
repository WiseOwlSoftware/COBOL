// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Listing.Html;
using WiseOwl.Cobol.Listing.Json;
using WiseOwl.Cobol.Listing.Text;

namespace WiseOwl.Cobol.Listing;

/// <summary>The formats a listing is written in (DESIGN-listing.md sections 4.2 and 8). A format joins with its renderer: a format
/// the compiler cannot write is not a member.</summary>
[Flags]
public enum ListingFormats
{
    /// <summary>The paginated text listing for people and printers (<c>BASE.lst</c>).</summary>
    Text = 1,

    /// <summary>The versioned JSON document with its published schema (<c>BASE.lst.json</c>): the stable programming
    /// interface.</summary>
    Json = 2,

    /// <summary>The hyperlinked HTML page (<c>BASE.lst.html</c>): one self-contained file, each diagnostic, inclusion, data map
    /// entry and statement's object code linked to its line.</summary>
    Html = 4,
}

/// <summary>
/// ⛔ THE ONE TABLE OF LISTING FORMATS (DESIGN-listing.md sections 7 and 8): each format's command-line spelling, its file
/// extension and its renderer. The compiler's listing exit writes files through it and the command line prints to standard
/// output through it, so a format added to <see cref="ListingFormats"/> is added here once and reaches both; every switch below
/// throws on a member it does not know, and the Unit assembly's <c>ListingOutputTests</c> calls each one for every member.
/// </summary>
public static class ListingOutput
{
    /// <summary>Every format.</summary>
    public const ListingFormats All = ListingFormats.Text | ListingFormats.Json | ListingFormats.Html;

    /// <summary>The single formats of <paramref name="formats"/>, in the order of <see cref="ListingFormats"/>.</summary>
    public static IReadOnlyList<ListingFormats> Each(ListingFormats formats) =>
        [.. Enum.GetValues<ListingFormats>().Where(f => (formats & f) != 0)];

    /// <summary>The command-line spelling of one format (<c>--listing-format</c>).</summary>
    public static string Spelling(ListingFormats format) => format switch
    {
        ListingFormats.Text => "text",
        ListingFormats.Json => "json",
        ListingFormats.Html => "html",
        _ => throw Unknown(format),
    };

    /// <summary>Every spelling, as a help text lists them: <c>text|json|html</c>.</summary>
    public static string Spellings => string.Join("|", Enum.GetValues<ListingFormats>().Select(Spelling));

    /// <summary>The format <paramref name="spelling"/> names (ordinal, ignoring case), or false.</summary>
    public static bool TryParse(string spelling, out ListingFormats format)
    {
        foreach (var f in Enum.GetValues<ListingFormats>())
            if (string.Equals(Spelling(f), spelling, StringComparison.OrdinalIgnoreCase)) { format = f; return true; }   // not a COBOL word: a command-line format name
        format = default;
        return false;
    }

    /// <summary>The extension one format's file takes after the listing's base.</summary>
    public static string Extension(ListingFormats format) => format switch
    {
        ListingFormats.Text => ".lst",
        ListingFormats.Json => ".lst.json",
        ListingFormats.Html => ".lst.html",
        _ => throw Unknown(format),
    };

    /// <summary>The encoding of every format: UTF-8 with no byte order mark (DESIGN-listing.md section 7.1; the JSON text RFC 8259
    /// section 8.1 requires; the charset the HTML page declares).</summary>
    public static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Write one format's rendering of <paramref name="document"/>, as <paramref name="request"/> asks for it, to
    /// <paramref name="output"/> in <see cref="Utf8"/>, and flush it; the stream stays open.
    /// <para>⛔ STREAMED (kb/Work PB5941; DESIGN-listing.md section 9): each renderer writes to the stream as it renders, so no format is
    /// ever held whole in memory, as a string or a buffer, on its way to its file. A listing of every section is several times the
    /// source's size (the object code), and holding it whole, then copying it into a string, then encoding it, cost three times its
    /// size per format.</para>
    /// </summary>
    public static void Write(ListingFormats format, ListingDocument document, ListingRequest request, Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        switch (format)
        {
            case ListingFormats.Text:
            {
                using var writer = Writer(output);
                TextListingRenderer.Write(document, request, writer);
                break;
            }
            case ListingFormats.Json:
                JsonListingWriter.Write(document, output);
                break;
            case ListingFormats.Html:
            {
                using var writer = Writer(output);
                HtmlListingRenderer.Write(document, request, writer);
                break;
            }
            default:
                throw Unknown(format);
        }
        output.Flush();
    }

    /// <summary>The character writer of a text format over <paramref name="output"/>: <see cref="Utf8"/>, LF line ends written
    /// by the renderers themselves, the stream left open for its owner to close.</summary>
    private static StreamWriter Writer(Stream output) =>
        new(output, Utf8, bufferSize: WriterBufferSize, leaveOpen: true);

    /// <summary>The characters a text format's writer buffers before it encodes them to the stream.</summary>
    private const int WriterBufferSize = 16 * 1024;

    private static ArgumentOutOfRangeException Unknown(ListingFormats format) =>
        new(nameof(format), format, "not one listing format");
}
