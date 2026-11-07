// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE PARSE CONTEXT OF EACH SOURCE ELEMENT, HELD BY THE BINDER (kb/Work PB2398, PB2401; DESIGN-backend-abstraction §1.1,
/// §1.3 L6; DESIGN-architecture-review §8.1). The bound compilation is the backend's whole input and carries no parse
/// context, and neither does a symbol of the class table, so the definition a <see cref="BoundUnit"/>, a class, an interface
/// or a method was built from lives here, on the bind session, read only by the binders and the bind-time validation passes:
/// a unit's is added by <c>BinderDriver.CollectUnits</c> as each unit is made, and a CLASS-ID's, INTERFACE-ID's and METHOD-ID's
/// by <c>OoClassTable.Build</c> as each symbol is made, with what made a class or an interface an expansion (ISO §2023:12.3.8.4 GR5,
/// GR8). A symbol is its own key: its <see cref="OoSymbolOrigin"/> says whether it has a definition (only
/// <see cref="OoSymbolOrigin.Source"/> does), and asking for the definition of one that has none is a loud internal error, never
/// a null a caller forgot to test (kb/Work PB4459).
/// </summary>
internal sealed class SourceMap
{
    private readonly Dictionary<BoundUnit, Core.ProgramUnitContext> _units = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<OoClassSymbol, Core.ClassDefinitionContext> _classes = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<OoInterfaceSymbol, Core.InterfaceDefinitionContext> _interfaces = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<OoMethodSymbol, Core.MethodDefinitionContext> _methods = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, ExpansionOrigin> _expansions = new(ReferenceEqualityComparer.Instance);

    /// <summary>Record that <paramref name="unit"/> was made from <paramref name="definition"/> (a contained unit's is the
    /// synthetic program unit <c>BinderDriver.Reparent</c> builds).</summary>
    public void Add(BoundUnit unit, Core.ProgramUnitContext definition) => _units.Add(unit, definition);

    /// <summary>The program unit <paramref name="unit"/> was bound from.</summary>
    public Core.ProgramUnitContext Of(BoundUnit unit) => _units.TryGetValue(unit, out var definition)
        ? definition
        : throw new InvalidOperationException($"the unit '{unit.Name}' is not in the bind session's source map (BinderDriver.CollectUnits adds every unit it makes)");

    /// <summary>The unit's own program-name token, for the diagnostic position. A CONTAINED unit's context is the
    /// SYNTHETIC programUnit context Reparent builds (<c>new ProgramUnitContext(null!, -1)</c>), whose Start is null — so
    /// positioning on it silently produced a diagnostic with NO source location at all, while the outermost arm of the same
    /// message carried one. One accessor, every arm.</summary>
    public ParserRuleContext NameOf(BoundUnit unit)
    {
        var definition = Of(unit);
        var body = definition.identificationDivision()?.identificationBody();
        return (ParserRuleContext?)(body?.programIdParagraph()?.programName()
            ?? body?.functionIdParagraph()?.programName()) ?? definition;
    }

    /// <summary>Record that the <see cref="OoSymbolOrigin.Source"/> class <paramref name="symbol"/> was built from the CLASS-ID
    /// <paramref name="definition"/>, and what made it an expansion (<paramref name="expansion"/>; null for a class written as a class).</summary>
    public void Add(OoClassSymbol symbol, Core.ClassDefinitionContext definition, ExpansionOrigin? expansion)
    {
        RequireSource(symbol.Origin, "class", symbol.Name);
        _classes.Add(symbol, definition);
        if (expansion is not null) _expansions.Add(symbol, expansion);
    }

    /// <summary>Record that the <see cref="OoSymbolOrigin.Source"/> interface <paramref name="symbol"/> was built from the INTERFACE-ID
    /// <paramref name="definition"/>, and what made it an expansion (<paramref name="expansion"/>; null for an interface written as one).</summary>
    public void Add(OoInterfaceSymbol symbol, Core.InterfaceDefinitionContext definition, ExpansionOrigin? expansion)
    {
        RequireSource(symbol.Origin, "interface", symbol.Name);
        _interfaces.Add(symbol, definition);
        if (expansion is not null) _expansions.Add(symbol, expansion);
    }

    /// <summary>Record that the <see cref="OoSymbolOrigin.Source"/> method or prototype <paramref name="symbol"/> was written as the
    /// METHOD-ID <paramref name="definition"/>.</summary>
    public void Add(OoMethodSymbol symbol, Core.MethodDefinitionContext definition)
    {
        RequireSource(symbol.Origin, "method", symbol.Name);
        _methods.Add(symbol, definition);
    }

    /// <summary>The CLASS-ID definition <paramref name="symbol"/> was built from. Every class of the compilation group has one; the
    /// STANDARD class and an IMPORTED one do not, and no consumer of a group class's source should reach either, because neither is
    /// in <see cref="OoClassTable.Classes"/>.</summary>
    public Core.ClassDefinitionContext Of(OoClassSymbol symbol) => _classes.TryGetValue(symbol, out var definition)
        ? definition
        : throw NoDefinition(symbol.Origin, "class", symbol.Name);

    /// <summary>The INTERFACE-ID definition <paramref name="symbol"/> was built from; an IMPORTED interface has none (ISO §16 defines
    /// no standard interface).</summary>
    public Core.InterfaceDefinitionContext Of(OoInterfaceSymbol symbol) => _interfaces.TryGetValue(symbol, out var definition)
        ? definition
        : throw NoDefinition(symbol.Origin, "interface", symbol.Name);

    /// <summary>The METHOD-ID definition <paramref name="symbol"/> was written as: what binding its data and body reads, and the
    /// position a diagnostic about it stands at. A standard, a generated and an imported method have none, so a reader that may meet
    /// either asks <see cref="OoMethodSymbol.Origin"/> first.</summary>
    public Core.MethodDefinitionContext Of(OoMethodSymbol symbol) => _methods.TryGetValue(symbol, out var definition)
        ? definition
        : throw NoDefinition(symbol.Origin, "method", symbol.Name);

    /// <summary>What made the class <paramref name="symbol"/> an EXPANSION of a parameterized definition (ISO §2023:12.3.8.4 GR5): the
    /// skeleton, the REPOSITORY entry and the actuals; null for a class written as a class and for an imported one.</summary>
    public ExpansionOrigin? ExpansionOf(OoClassSymbol symbol) => _expansions.GetValueOrDefault(symbol);

    /// <summary>What made the interface <paramref name="symbol"/> an EXPANSION (ISO §2023:12.3.8.4 GR8); null for an interface written
    /// as an interface and for an imported one.</summary>
    public ExpansionOrigin? ExpansionOf(OoInterfaceSymbol symbol) => _expansions.GetValueOrDefault(symbol);

    private static void RequireSource(OoSymbolOrigin origin, string what, string name)
    {
        if (origin is not OoSymbolOrigin.Source)
            throw new ArgumentException($"{what} '{name}' is {origin.ToString().ToLowerInvariant()}: only a Source {what} has a definition");
    }

    private static InvalidOperationException NoDefinition(OoSymbolOrigin origin, string what, string name) => new(origin switch
    {
        OoSymbolOrigin.Source => $"{what} '{name}' is not in the bind session's source map (OoClassTable.Build adds every symbol it makes)",
        OoSymbolOrigin.Standard => $"{what} '{name}' is standard (ISO §16) and has no source definition",
        OoSymbolOrigin.Imported => $"{what} '{name}' is imported from the external repository and has no source definition",
        _ => $"{what} '{name}' is {origin.ToString().ToLowerInvariant()} and has no source definition",
    });
}
