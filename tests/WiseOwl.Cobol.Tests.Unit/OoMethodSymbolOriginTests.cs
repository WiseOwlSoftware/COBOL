// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Generated;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN OO METHOD SYMBOL'S ORIGIN IS A TYPE, NOT THE NULLNESS OF ITS CONTEXT (kb/Work PB4459), as <see cref="OoClassSymbol"/>'s is. A method
/// is <see cref="OoSymbolOrigin.Source"/> exactly when a METHOD-ID definition built it; a standard (ISO §16.2), imported or generated
/// (ISO §13.18.42 PROPERTY accessor) method has none, and reading <see cref="OoMethodSymbol.Ctx"/> on it is a loud internal error — so no
/// reader can hand a missing context to the diagnostic cursor (<c>edition.At</c>), which would leave the cursor where an earlier construct
/// put it. The positioned-diagnostic witnesses are <c>OoClassTablePositionTests</c>' IMPLEMENTS-GENERATED-ACCESSOR and
/// IMPLEMENTS-STANDARD-METHOD sources.
/// </summary>
public sealed class OoMethodSymbolOriginTests
{
    public static TheoryData<OoSymbolOrigin> NonSourceOrigins => new() { OoSymbolOrigin.Standard, OoSymbolOrigin.Imported, OoSymbolOrigin.Generated };

    [Fact]
    public void ASourceMethod_IsBuiltFromItsDefinition_AndAnOriginConstructorRefusesSource()
    {
        var definition = new CobolParserCore.MethodDefinitionContext(null, 0);
        var written = new OoMethodSymbol("M", hasUsing: false, hasReturning: false, definition) { ClrName = "M" };
        Assert.Equal(OoSymbolOrigin.Source, written.Origin);
        Assert.Same(definition, written.Ctx);
        Assert.False(written.HasFixedClrName);
        Assert.Throws<ArgumentException>(() => new OoMethodSymbol("M", hasUsing: false, hasReturning: false, OoSymbolOrigin.Source) { ClrName = "M" });
    }

    [Theory]
    [MemberData(nameof(NonSourceOrigins))]
    public void ANonSourceMethod_HasNoDefinition_AndSaysWhichOriginItIs(OoSymbolOrigin origin)
    {
        var method = new OoMethodSymbol("M", hasUsing: false, hasReturning: true, origin) { ClrName = "M" };
        Assert.Equal(origin, method.Origin);
        var fault = Assert.Throws<InvalidOperationException>(() => method.Ctx);
        Assert.Contains(origin.ToString().ToLowerInvariant(), fault.Message, StringComparison.Ordinal);
        Assert.Equal(origin is not OoSymbolOrigin.Generated, method.HasFixedClrName);
    }
}
