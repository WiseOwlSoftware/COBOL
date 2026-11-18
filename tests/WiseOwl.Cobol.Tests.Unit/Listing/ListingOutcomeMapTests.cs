// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The compilation's outcome as the listing names it (DESIGN-listing.md section 4.6): every outcome that read the source maps
/// to the listing outcome of the same name, so a new outcome cannot reach the listing's exit unnamed. A backend error has no
/// source that reaches it (it is a compiler defect), so this map is its listing's witness; <c>SourceNotFound</c> read no
/// source and has no listing.
/// </summary>
public sealed class ListingOutcomeMapTests
{
    [Fact]
    public void EveryOutcomeThatReadTheSource_IsTheListingOutcomeOfTheSameName()
    {
        foreach (var outcome in Enum.GetValues<CompilerDriver.Outcome>())
        {
            if (outcome == CompilerDriver.Outcome.SourceNotFound)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => CompilerDriver.ListingOutcomeOf(outcome));
                continue;
            }
            Assert.Equal(outcome.ToString(), CompilerDriver.ListingOutcomeOf(outcome).ToString());
        }
        Assert.Equal(Enum.GetValues<CompilerDriver.Outcome>().Length - 1, Enum.GetValues<ListingOutcome>().Length);
    }
}
