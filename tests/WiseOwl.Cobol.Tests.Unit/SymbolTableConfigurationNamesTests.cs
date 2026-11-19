// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Symbols;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The symbol table's configuration-section names tell the source element that declared a name from the ones it came down
/// through (ISO §2023:8.4.6.1; §2023:8.4.6.2.1 3; kb/Work PB3347; docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b,
/// slice 4"): a switch condition-name declared in the SAME element as a level-88 of the spelling competes with it under
/// §2023:8.4.2.2.1, one declared in a nearer element hides it, one a tier farther out yields to it. The expected depths are
/// the containment distances §2023:8.4.6.2.1 3) counts: 0 for the element's own declaration, n for the n-th container's.
/// </summary>
public sealed class SymbolTableConfigurationNamesTests
{
    private static SymbolTable NewTable() => new DataBinder().Symbols;

    [Fact]
    public void ADeclaredNameIsOwn_AnInheritedNameIsNot()
    {
        var container = NewTable();
        container.DeclareSwitchCondition("S-ON", "SWITCH-1", isOn: true);
        var containee = NewTable();
        containee.DeclareSwitchCondition("OWN", "SWITCH-2", isOn: true);
        containee.InheritConfigurationNames(container);

        Assert.True(containee.DeclaresConfigurationName(NameClass.SwitchCondition, "S-ON"));
        Assert.Equal(1, containee.SwitchConditionDepth("S-ON"));   // only the container declared it
        Assert.Equal(0, containee.SwitchConditionDepth("OWN"));
        Assert.Equal(0, container.SwitchConditionDepth("S-ON"));
        Assert.Equal(-1, containee.SwitchConditionDepth("NOPE"));
    }

    [Fact]
    public void AContaineesOwnDeclarationWinsAndStaysOwn()
    {
        var container = NewTable();
        container.DeclareSwitchCondition("S-ON", "SWITCH-1", isOn: true);
        var containee = NewTable();
        containee.DeclareSwitchCondition("S-ON", "SWITCH-2", isOn: false);
        containee.InheritConfigurationNames(container);   // the containee's own declaration wins (§2023:12.3.4 GR1)

        Assert.Equal(0, containee.SwitchConditionDepth("S-ON"));
    }

    [Fact]
    public void ADepthCountsEveryLevelADeclarationCameDown()
    {
        var outer = NewTable();
        outer.DeclareSymbolicCharacter("S-ON", "A", national: false);
        var middle = NewTable();
        middle.InheritConfigurationNames(outer);
        var inner = NewTable();
        inner.InheritConfigurationNames(middle);

        Assert.Empty(outer.InheritedSymbolicDepths);
        Assert.Equal(1, middle.InheritedSymbolicDepths.Single(e => e.Key == "S-ON").Value);
        Assert.Equal(2, inner.InheritedSymbolicDepths.Single(e => e.Key == "S-ON").Value);
    }

    [Fact]
    public void ANameIsCaseInsensitive_ForTheDepthQuestionToo()
    {
        var table = NewTable();
        table.DeclareSwitchCondition("S-On", "SWITCH-1", isOn: true);
        Assert.Equal(0, table.SwitchConditionDepth("s-ON"));
    }

    [Fact]
    public void ANearerDeclarationHidesAnInheritedSymbolicCharacter_AndHidesNothingElse()
    {
        var container = NewTable();
        container.DeclareSymbolicCharacter("SC-A", "A", national: false);
        container.DeclareSymbolicCharacter("SC-B", "B", national: false);
        var containee = NewTable();
        containee.InheritConfigurationNames(container);

        // A name declared at depth 0 hides one inherited from depth 1 (§2023:8.4.6.2.1 3) a)); SC-B has no nearer declaration.
        containee.HideShadowedSymbolics((name, depth) => name == "SC-A" && depth > 0);

        Assert.True(containee.HidesSymbolic("sc-a"));
        Assert.False(containee.HidesSymbolic("SC-B"));
        Assert.True(containee.DeclaresConfigurationName(NameClass.SymbolicCharacter, "SC-A"));   // hidden, never removed
    }
}
