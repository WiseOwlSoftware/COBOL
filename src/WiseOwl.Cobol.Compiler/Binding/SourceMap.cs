// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE PARSE CONTEXT OF EACH SOURCE ELEMENT, HELD BY THE BINDER (kb/Work PB2398; DESIGN-backend-abstraction §1.1,
/// §1.3 L6; DESIGN-architecture-review §8.1). The bound compilation is the backend's whole input and carries no parse
/// context, so the definition a <see cref="BoundUnit"/> was bound from lives here, on the bind session, read only by the
/// binders and the bind-time validation passes. Built once, by <c>BinderDriver.CollectUnits</c>, as each unit is made.
/// </summary>
internal sealed class SourceMap
{
    private readonly Dictionary<BoundUnit, Core.ProgramUnitContext> _units = new(ReferenceEqualityComparer.Instance);

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
}
