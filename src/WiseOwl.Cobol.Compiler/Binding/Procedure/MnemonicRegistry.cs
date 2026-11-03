// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding.Procedure;

using WiseOwl.Cobol.Runtime;

/// <summary>The READER of the SPECIAL-NAMES {mnemonic-name → implementor system-name} map (ISO §12.3.7.2: <c>switch-name-1
/// IS mnemonic-name-1</c> | <c>feature-name-1 IS mnemonic-name-2</c> | <c>device-name-1 IS mnemonic-name-3</c>), which is a
/// field of the unit's <see cref="ConfigurationSlice"/> (<see cref="ConfigurationSlice.TryGetMnemonic"/>): the unit's own
/// entries first, then its container's — SPECIAL-NAMES declarations are visible to contained source units (§12.3.7.4 GR1),
/// the nearest declaration winning — and, for a METHOD, its half's, which carries the class definition's (§12.3.3 SR2: a
/// method declares no configuration section; §12.3.4 GR1). <see cref="ConfigurationStep"/> produces the inheritance; this
/// type used to walk the parse tree upward from the referencing statement for the purpose (kb/Work PB135, PB2296), a second
/// mechanism for the same rule.
/// <para>⛔ The value is the <see cref="ImplementorName"/> ROW, so every consumer asks the row's KIND, never the
/// spelling of the entry (kb/Work PB862). The map used to hold every two-word entry WITHOUT an ON/OFF phrase as a
/// "device", so <c>SWITCH-1 IS SW1</c> made SW1 a legal WRITE ADVANCING operand and <c>WIBBLE WOBBLE</c> a
/// device named WIBBLE. §12.3.7.3 SR5/SR6/SR7 key the use of a mnemonic on the TYPE of the name it is
/// associated with — SET for a switch, WRITE for a feature, ACCEPT/DISPLAY for a device — and §8.3.2.3.1 makes
/// that type a property of the name. An entry naming an unavailable name contributes nothing (the SPECIAL-NAMES
/// binder reported it, COBOL2241); a switch entry with no mnemonic declares nothing a statement can
/// reference.</para>
/// (P7 Step 10h — moved off the ACCEPT partial onto <see cref="BinderContext"/>: ACCEPT-FROM, DISPLAY-UPON and
/// the WRITE SR13 / SR16 / ADVANCING legs share the ONE per-unit map; since kb/Work PB454 so do SET Format 3's
/// switch operand (§14.9.39.3 SR5) and the condition binder's "that is a switch mnemonic" diagnostic.)
/// <para>⛔ A LOOKUP IS ONE OF TWO NAMED OPERATIONS (kb/Work PB3153), as for the data binder's other
/// configuration-section names (<see cref="ConfigurationNames{T}"/>): <see cref="TryUse"/> resolves a reference the
/// binder commits to and records it as a §8.3.2.2 use of the word as a mnemonic-name in the referring source
/// element's census (<see cref="DataBinder.RecordConfigurationNameUse"/>) — a contained program or a method uses
/// names its container declares, so no declaration of its own ever announced them; <see cref="TryPeek"/> reads
/// without recording, for a diagnostic that names what the word is.</para></summary>
internal sealed class MnemonicRegistry(DataBinder data)
{
    /// <summary>Resolve the mnemonic-name <paramref name="name"/> written at <paramref name="at"/>, recording the use
    /// (§8.3.2.2). False, recording nothing, when no SPECIAL-NAMES entry in scope declares it.</summary>
    public bool TryUse(Antlr4.Runtime.RuleContext at, string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ImplementorName row)
    {
        if (!data.State.Configuration.TryGetMnemonic(name, out row)) return false;
        data.RecordConfigurationNameUse(at, name, UserWordKind.MnemonicName);
        return true;
    }

    /// <summary>The row <paramref name="name"/> names, read WITHOUT recording a use: a diagnostic's question.</summary>
    public bool TryPeek(string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ImplementorName row) =>
        data.State.Configuration.TryGetMnemonic(name, out row);
}
