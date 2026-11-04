// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Frontend.Generated;
using Xunit;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN OO SYMBOL'S ORIGIN IS A TYPE, NOT THE NULLNESS OF ITS CONTEXT (kb/Work PB4459), AND NO SYMBOL CARRIES A PARSE CONTEXT (kb/Work
/// PB2401). A class, an interface or a method is <see cref="OoSymbolOrigin.Source"/> exactly when a CLASS-ID, INTERFACE-ID or METHOD-ID
/// definition built it, and that definition is in the bind session's <see cref="SourceMap"/>, never on the symbol; a standard (ISO §16.2),
/// imported or generated (ISO §13.18.42 PROPERTY accessor) method has none, and asking the map for one is a loud internal error — so no
/// reader can hand a missing context to the diagnostic cursor (<c>edition.At</c>), which would leave the cursor where an earlier construct
/// put it — and the map refuses to record a definition for a symbol that is not <see cref="OoSymbolOrigin.Source"/>. The
/// positioned-diagnostic witnesses are <c>OoClassTablePositionTests</c>' IMPLEMENTS-GENERATED-ACCESSOR and IMPLEMENTS-STANDARD-METHOD
/// sources.
/// </summary>
public sealed class OoMethodSymbolOriginTests
{
    public static TheoryData<OoSymbolOrigin> NonSourceOrigins => new() { OoSymbolOrigin.Standard, OoSymbolOrigin.Imported, OoSymbolOrigin.Generated };

    [Fact]
    public void ASourceMethod_HasItsDefinitionInTheSourceMap()
    {
        var definition = new CobolParserCore.MethodDefinitionContext(null, 0);
        var written = new OoMethodSymbol("M", hasUsing: false, hasReturning: false, OoSymbolOrigin.Source) { ClrName = "M" };
        var sources = new SourceMap();
        sources.Add(written, definition);
        Assert.Equal(OoSymbolOrigin.Source, written.Origin);
        Assert.Same(definition, sources.Of(written));
        Assert.False(written.HasFixedClrName);
    }

    [Fact]
    public void ASourceClassAndInterface_HaveTheirDefinitionsInTheSourceMap()
    {
        var classDefinition = new CobolParserCore.ClassDefinitionContext(null, 0);
        var interfaceDefinition = new CobolParserCore.InterfaceDefinitionContext(null, 0);
        var cls = new OoClassSymbol("C", "C", OoSymbolOrigin.Source);
        var iface = new OoInterfaceSymbol("I", "I", OoSymbolOrigin.Source);
        var sources = new SourceMap();
        sources.Add(cls, classDefinition, expansion: null);
        sources.Add(iface, interfaceDefinition, expansion: null);
        Assert.Same(classDefinition, sources.Of(cls));
        Assert.Same(interfaceDefinition, sources.Of(iface));
        Assert.Null(sources.ExpansionOf(cls));
        Assert.Null(sources.ExpansionOf(iface));
    }

    [Fact]
    public void ASourceSymbolTheMapNeverSaw_IsALoudFault()
    {
        var missing = new OoMethodSymbol("M", hasUsing: false, hasReturning: false, OoSymbolOrigin.Source) { ClrName = "M" };
        var fault = Assert.Throws<InvalidOperationException>(() => new SourceMap().Of(missing));
        Assert.Contains("not in the bind session's source map", fault.Message, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(NonSourceOrigins))]
    public void ANonSourceMethod_HasNoDefinition_AndSaysWhichOriginItIs(OoSymbolOrigin origin)
    {
        var method = new OoMethodSymbol("M", hasUsing: false, hasReturning: true, origin) { ClrName = "M" };
        var sources = new SourceMap();
        Assert.Equal(origin, method.Origin);
        var fault = Assert.Throws<InvalidOperationException>(() => sources.Of(method));
        Assert.Contains(origin.ToString().ToLowerInvariant(), fault.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => sources.Add(method, new CobolParserCore.MethodDefinitionContext(null, 0)));
        Assert.Equal(origin is not OoSymbolOrigin.Generated, method.HasFixedClrName);
    }

    [Theory]
    [InlineData(OoSymbolOrigin.Standard)]
    [InlineData(OoSymbolOrigin.Imported)]
    public void ANonSourceClass_HasNoDefinition_AndTheMapRefusesOne(OoSymbolOrigin origin)
    {
        var cls = new OoClassSymbol("C", "C", origin);
        var sources = new SourceMap();
        var fault = Assert.Throws<InvalidOperationException>(() => sources.Of(cls));
        Assert.Contains(origin.ToString().ToLowerInvariant(), fault.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => sources.Add(cls, new CobolParserCore.ClassDefinitionContext(null, 0), expansion: null));
    }
}
