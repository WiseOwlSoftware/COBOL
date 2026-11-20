// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Listing.Building;

/// <summary>What the listing's exit produced: the document, the files written (none when the request writes none or the host
/// refused each) and the errors the host's refusals are reported as, one per file refused.</summary>
internal sealed record ListingWritten(ListingDocument Document, IReadOnlyList<string> Files, IReadOnlyList<string> Errors);

/// <summary>
/// ⛔ THE LISTING'S ONE EXIT from a compilation (DESIGN-listing.md section 4.6): <c>CompilerDriver.Compile</c> asks it once the
/// body has returned, whatever return the body took. With no source text deposited there is no listing (null); with source,
/// the listing is built ONCE (<see cref="ListingBuilder"/>) and each requested format renders that one document
/// (<see cref="ListingOutput.Write"/>), streamed into <c>BASE</c> plus the format's extension, unless the request writes no file. A
/// listing file the host refuses to write is one error and changes no outcome; the other formats are still written.
/// </summary>
internal static class ListingWriter
{
    public static ListingWritten? Write(ListingFacts facts, ListingOutcome outcome)
    {
        if (facts.Frontend is null) return null;
        var document = ListingBuilder.Build(facts, outcome);
        if (!facts.Request.WritesFiles) return new ListingWritten(document, [], []);
        string outputBase = facts.Request.OutputBase ?? Path.ChangeExtension(facts.OutputDll, null);
        List<string> files = [], errors = [];
        foreach (var format in ListingOutput.Each(facts.Request.Formats))
        {
            string path = outputBase + ListingOutput.Extension(format);
            try
            {
                using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, FileBufferSize))   // not a compilation input: the listing the compilation writes
                    ListingOutput.Write(format, document, facts.Request, file);
                files.Add(Path.GetFullPath(path));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                errors.Add($"cannot write the listing '{path}': {e.Message}");
            }
        }
        return new ListingWritten(document, files, errors);
    }

    /// <summary>The bytes the file stream buffers before it writes: each format streams to its file (kb/Work PB5941).</summary>
    private const int FileBufferSize = 64 * 1024;
}
