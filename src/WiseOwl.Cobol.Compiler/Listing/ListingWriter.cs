// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using WiseOwl.Cobol.Listing.Text;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>What the listing's exit produced: the document, the file written (null when the request writes none or the host
/// refused it) and the error the host's refusal is reported as.</summary>
internal sealed record ListingWritten(ListingDocument Document, string? File, string? Error);

/// <summary>
/// ⛔ THE LISTING'S ONE EXIT from a compilation (DESIGN-listing.md section 4.6): <c>CompilerDriver.Compile</c> asks it once the
/// body has returned, whatever return the body took. With no source text deposited there is no listing (null); with source,
/// the listing is built (<see cref="ListingBuilder"/>) and written as <c>BASE.lst</c> unless the request writes no file. A
/// listing the host refuses to write is one error and changes no outcome.
/// </summary>
internal static class ListingWriter
{
    public static ListingWritten? Write(ListingFacts facts, ListingOutcome outcome)
    {
        if (facts.Frontend is null) return null;
        var document = ListingBuilder.Build(facts, outcome);
        if (!facts.Request.WritesFiles) return new ListingWritten(document, null, null);
        string path = (facts.Request.OutputBase ?? Path.ChangeExtension(facts.OutputDll, null)) + ListingRequest.TextExtension;
        try
        {
            File.WriteAllText(path, TextListingRenderer.Render(document, facts.Request), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new ListingWritten(document, null, $"cannot write the listing '{path}': {e.Message}");
        }
        return new ListingWritten(document, Path.GetFullPath(path), null);
    }
}
