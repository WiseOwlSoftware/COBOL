// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;

namespace WiseOwl.Cobol.Tests.Shared;

/// <summary>
/// A listing format's bytes as text, for a test that reads what the compiler would write (DESIGN-listing.md section 10). The product
/// streams each format to its file or to standard output (<see cref="ListingOutput.Write"/>, kb/Work PB5941) and holds no format as a
/// string, so a test takes the same bytes through the same one call and decodes them as the listing's own encoding,
/// <see cref="ListingOutput.Utf8"/>.
/// </summary>
public static class ListingRendering
{
    /// <summary>What <see cref="ListingOutput.Write"/> writes of <paramref name="document"/> in <paramref name="format"/> for
    /// <paramref name="request"/>.</summary>
    public static string Of(ListingFormats format, ListingDocument document, ListingRequest request) =>
        Decoded(bytes => ListingOutput.Write(format, document, request, bytes));

    /// <summary>The paginated text listing of <paramref name="document"/>.</summary>
    public static string Text(ListingDocument document, ListingRequest request) => Of(ListingFormats.Text, document, request);

    /// <summary>The JSON listing of <paramref name="document"/> (no request shapes it).</summary>
    public static string Json(ListingDocument document) => Of(ListingFormats.Json, document, new ListingRequest(Formats: ListingFormats.Json));

    /// <summary>The HTML page of <paramref name="document"/>.</summary>
    public static string Html(ListingDocument document, ListingRequest request) => Of(ListingFormats.Html, document, request);

    /// <summary>The bytes <paramref name="write"/> writes to a stream, decoded as the listing's encoding.</summary>
    public static string Decoded(Action<Stream> write)
    {
        using var bytes = new MemoryStream();
        write(bytes);
        return ListingOutput.Utf8.GetString(bytes.GetBuffer(), 0, checked((int)bytes.Length));
    }
}
