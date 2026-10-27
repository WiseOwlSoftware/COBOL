// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// A configuration-section table tells the source element that declared a name from the ones it came down through
/// (ISO §8.4.6.1; §8.4.6.2.1 3; kb/Work PB3347): a switch condition-name declared in the SAME element as a level-88 of
/// the spelling competes with it under §8.4.2.2.1, one declared in a nearer element hides it, one a tier farther out
/// yields to it.
/// </summary>
public sealed class ConfigurationNamesTests
{
    private static ConfigurationNames<int> NewTable() => new(UserWordKind.ConditionName, static (_, _, _) => { });

    [Fact]
    public void ADeclaredNameIsOwn_AnInheritedNameIsNot()
    {
        var container = NewTable();
        container.TryAdd("S-ON", 1);
        var containee = NewTable();
        containee.TryAdd("OWN", 2);
        containee.InheritFrom(container);

        Assert.True(containee.Declares("S-ON"));
        Assert.Equal(1, containee.ContainmentDepth("S-ON"));   // only the container declared it
        Assert.Equal(0, containee.ContainmentDepth("OWN"));
        Assert.Equal(0, container.ContainmentDepth("S-ON"));
        Assert.Equal(-1, containee.ContainmentDepth("NOPE"));
    }

    [Fact]
    public void AContaineesOwnDeclarationWinsAndStaysOwn()
    {
        var container = NewTable();
        container.TryAdd("S-ON", 1);
        var containee = NewTable();
        containee.TryAdd("S-ON", 2);
        containee.InheritFrom(container);   // the containee's own declaration wins (§12.3.4 GR1)

        Assert.Equal(0, containee.ContainmentDepth("S-ON"));
        Assert.True(containee.TryPeek("S-ON", out int value));
        Assert.Equal(2, value);
    }

    [Fact]
    public void SetReplacesAnInheritedEntryWithAnOwnOne()
    {
        var container = NewTable();
        container.TryAdd("S-ON", 1);
        var containee = NewTable();
        containee.InheritFrom(container);
        Assert.Equal(1, containee.ContainmentDepth("S-ON"));

        containee.Set("S-ON", 3);

        Assert.Equal(0, containee.ContainmentDepth("S-ON"));
    }

    [Fact]
    public void ADepthCountsEveryLevelADeclarationCameDown()
    {
        var outer = NewTable();
        outer.TryAdd("S-ON", 1);
        var middle = NewTable();
        middle.InheritFrom(outer);
        var inner = NewTable();
        inner.InheritFrom(middle);

        Assert.Equal(0, outer.ContainmentDepth("S-ON"));
        Assert.Equal(1, middle.ContainmentDepth("S-ON"));
        Assert.Equal(2, inner.ContainmentDepth("S-ON"));
        Assert.Equal(2, inner.InheritedDepths.Single(e => e.Key == "S-ON").Value);
    }

    [Fact]
    public void ANameIsCaseInsensitive_ForTheDepthQuestionToo()
    {
        var table = NewTable();
        table.TryAdd("S-On", 1);
        Assert.Equal(0, table.ContainmentDepth("s-ON"));
    }
}
