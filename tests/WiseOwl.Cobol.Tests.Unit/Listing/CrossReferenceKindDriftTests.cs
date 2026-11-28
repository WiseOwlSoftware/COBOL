// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Compiler.Listing;
using WiseOwl.Cobol.Compiler.Symbols;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The cross-reference's two maps from the symbol table to the listing (docs/rearchitecture/DESIGN-listing.md section 6.7) are total
/// both ways: every <see cref="NameClass"/> has its <see cref="ListingSymbolKind"/> and every kind but the copy member (a view of the
/// inclusions, no entry of the table) is some class's, each once; every <see cref="SymbolUse"/> has its <see cref="ListingUse"/>, one
/// to one. A member added to either enum without the other is red, as <c>PhysicalLineKindDriftTests</c> holds the line kinds.
/// </summary>
public sealed class CrossReferenceKindDriftTests
{
    [Fact]
    public void TheKindMap_IsTotal_BothWays()
    {
        var mapped = Enum.GetValues<NameClass>().Select(CrossReferenceBuilder.KindOf).ToList();
        Assert.Equal(mapped.Count, mapped.Distinct().Count());
        Assert.Equal(Enum.GetValues<ListingSymbolKind>().Where(k => k != ListingSymbolKind.CopyMember).Order(), mapped.Order());
        Assert.All(Enum.GetValues<NameClass>(), c => Assert.Equal(c.ToString(), CrossReferenceBuilder.KindOf(c).ToString()));
    }

    [Fact]
    public void TheUseMap_IsTotal_BothWays()
    {
        var mapped = Enum.GetValues<SymbolUse>().Select(CrossReferenceBuilder.UseOf).ToList();
        Assert.Equal(Enum.GetValues<ListingUse>(), mapped);
        Assert.All(Enum.GetValues<SymbolUse>(), u => Assert.Equal(u.ToString(), CrossReferenceBuilder.UseOf(u).ToString()));
    }

    /// <summary>The text listing and the page name every kind and mark a use with one switch each (<see cref="Listing.Text.CrossReferenceText"/>),
    /// which throws on a member it does not know.</summary>
    [Fact]
    public void EveryKindAndUse_HasItsWord()
    {
        Assert.Equal(Enum.GetValues<ListingSymbolKind>().Length,
            Enum.GetValues<ListingSymbolKind>().Select(Cobol.Listing.Text.CrossReferenceText.KindWord).Distinct().Count());
        Assert.All(Enum.GetValues<ListingUse>(), u => Assert.NotNull(Cobol.Listing.Text.CrossReferenceText.UseMark(u)));
    }
}
