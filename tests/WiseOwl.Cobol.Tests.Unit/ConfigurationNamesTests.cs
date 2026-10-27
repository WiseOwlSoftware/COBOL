// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// A configuration-section table tells a name THIS source element declares from one a CONTAINING element handed down
/// (ISO §8.4.6.1; kb/Work PB3347): a switch condition-name the element declares itself competes with its own level-88
/// of the spelling under §8.4.2.2.1, while an inherited one yields to it.
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
        Assert.False(containee.DeclaresOwn("S-ON"));   // only the container declared it
        Assert.True(containee.DeclaresOwn("OWN"));
        Assert.True(container.DeclaresOwn("S-ON"));
    }

    [Fact]
    public void AContaineesOwnDeclarationWinsAndStaysOwn()
    {
        var container = NewTable();
        container.TryAdd("S-ON", 1);
        var containee = NewTable();
        containee.TryAdd("S-ON", 2);
        containee.InheritFrom(container);   // the containee's own declaration wins (§12.3.4 GR1)

        Assert.True(containee.DeclaresOwn("S-ON"));
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
        Assert.False(containee.DeclaresOwn("S-ON"));

        containee.Set("S-ON", 3);

        Assert.True(containee.DeclaresOwn("S-ON"));
    }

    [Fact]
    public void ANameIsCaseInsensitive_ForTheOwnQuestionToo()
    {
        var table = NewTable();
        table.TryAdd("S-On", 1);
        Assert.True(table.DeclaresOwn("s-ON"));
    }
}
