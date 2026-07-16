// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SPECIAL-NAMES {mnemonic-name → DEVICE-NAME} registry (ISO §12.3.7 Format 4: <c>device-name-1
/// IS mnemonic-name-3</c>), built once per program unit by walking the parse tree UP from the referencing
/// statement (no bound environment-division model exists yet). Every ENCLOSING program unit contributes too —
/// SPECIAL-NAMES declarations are visible to contained source units (§12.3.7.4 GR1), the nearest declaration
/// winning (<c>TryAdd</c>, innermost first). An entry carrying a switch ON/OFF phrase is a SWITCH clause
/// (Format 3), not a device association, and is skipped; an entry without <c>IS mnemonic</c> declares nothing
/// a consumer can reference. (P7 Step 10h — moved off the ACCEPT partial onto <see cref="BinderContext"/>:
/// ACCEPT-FROM and the WRITE SR13 / ADVANCING zero-advance legs share the ONE per-unit map.)</summary>
internal sealed class MnemonicRegistry
{
    private Dictionary<string, string>? _map;

    /// <summary>The per-unit map, computed lazily from any parse node INSIDE the unit.</summary>
    public IReadOnlyDictionary<string, string> Of(IParseTree at)
    {
        if (_map is not null) return _map;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (IParseTree? n = at; n is not null; n = n.Parent)
        {
            if (n is not Core.ProgramUnitContext pu) continue;
            var paragraphs = DataBinder.EnvDivisions(pu)
                .SelectMany(env => env.configurationSection()?.configurationParagraph() ?? []);
            foreach (var para in paragraphs)
                foreach (var entry in para.specialNamesParagraph()?.specialNameEntry() ?? [])
                    if (entry.implementorSwitchEntry() is { } e
                        && e.switchOnClause() is null && e.switchOffClause() is null
                        && e.cobolWord(1) is { } alias)
                        map.TryAdd(alias.GetText(), e.cobolWord(0).GetText().ToUpperInvariant());
        }
        return _map = map;
    }
}
