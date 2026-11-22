// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The cross-reference is sorted by the Annex C fold of each name (DESIGN-listing.md section 6.7, <c>CobolNames.UpperFold</c>), a copy member's
/// text-name included (kb/Work PB5925 step 7b review round 1, R1-4): <c>COPY payrec.</c>, a text-name written as a lowercase user-defined
/// word, is the member PAYREC, so its row stands among the names that begin with P, never after the upper-case names an ordinal comparison of
/// the raw text puts it after. The row keeps the name as written.
/// </summary>
public sealed class ListingCopyMemberOrderTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingcopyorder");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    [Fact]
    public void ACopyMember_SortsByTheFoldOfItsTextName_AndKeepsItsWrittenName()
    {
        string path = Path.Combine(_dir, "cpyord.cob");
        File.WriteAllText(Path.Combine(_dir, "payrec.cpy"), "       01 WS-A PIC 9 VALUE 1.\n");
        File.WriteAllText(path, string.Join("\n",
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. CPYORD.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "           COPY payrec.",
            "       01 ZZ-TOTAL PIC 9.",
            "       PROCEDURE DIVISION.",
            "           DISPLAY WS-A ZZ-TOTAL.",
            "           STOP RUN.",
        ]));
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(path, EditionInfo.Of(2023), Path.Combine(_dir, "cpyord.dll"),
            CopyPaths: [_dir], Listing: new ListingRequest(Formats: ListingFormats.Json) { Sections = ListingSectionSet.All }));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        var xref = Assert.IsType<ListingCrossReference>(Assert.IsType<ListingDocument>(result.Listing).CrossReference);
        Assert.Equal(["CPYORD", "payrec", "WS-A", "ZZ-TOTAL"], xref.Symbols.Select(s => s.Name));
        Assert.Equal(ListingSymbolKind.CopyMember, xref.Symbols[1].Kind);
    }
}
