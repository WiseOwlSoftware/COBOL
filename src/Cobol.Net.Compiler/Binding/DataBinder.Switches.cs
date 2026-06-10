// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The SPECIAL-NAMES external-switch registry (ISO §12.3.7, version-invariant 85→2023): the switch-name clause
/// associates an implementor-defined external switch with an optional mnemonic-name (Option 1) and/or ON/OFF
/// status condition-names (either Option). The mnemonic is referenced only in SET (SR5); the condition-names are
/// interrogated as switch-status conditions (GR2 / §8.8.4.6); SET alters the status (GR3 / §14.9.39 F3).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>SPECIAL-NAMES switch mnemonic-names (case-insensitive) → the implementor switch-name they set
    /// (ISO §12.3.7 Option 1; SR5 — a mnemonic-name may be specified only in a SET statement). An Option 2 entry
    /// (no mnemonic) registers the switch-name itself, accepting <c>SET switch-name</c> — legacy-parity leniency
    /// (the conforming program cannot SET an Option 2 switch at all, so no conforming program changes meaning).</summary>
    public Dictionary<string, string> SwitchMnemonics { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Switch-status condition-names (case-insensitive) → (implementor switch-name, posited-ON) (ISO
    /// §12.3.7 GR2; §8.4.4.2 Format 1 SR1 — the condition-name shall be associated with a switch-name in
    /// SPECIAL-NAMES). Consulted by the condition binder AFTER level-88 resolution (NC211A defines a name as BOTH;
    /// the level-88 wins).</summary>
    public Dictionary<string, (string ImplementorName, bool IsOn)> SwitchConditions { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>User-defined CLASS names (case-insensitive) → the EXPANDED member-character set (ISO §12.3.7
    /// class-name clause: each literal lists its characters; a THRU pair contributes every character between the
    /// two ordinals in the NATIVE collating sequence, in either order). Consulted by the class-condition binder
    /// (§8.8.4.1.4 — true when the operand consists entirely of members).</summary>
    public Dictionary<string, string> UserClasses { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Populate the switch registry from the SPECIAL-NAMES paragraph's switch-name clauses (ISO §12.3.7
    /// general format: <c>switch-name-1 [IS mnemonic-name-1] [ON [STATUS] [IS] condition-name-1]
    /// [OFF [STATUS] [IS] condition-name-2]</c>; the NIST-85 surface also writes <c>ON IS cond</c> with no STATUS —
    /// both shapes are one grammar rule). Any switch-name is accepted (SR8 — the available names are
    /// implementor-specified; see <c>ExternalSwitches</c> for the documented item-191 contract).</summary>
    private void SwitchBindSpecialNames(Core.ProgramUnitContext program)
    {
        var cfg = program.environmentDivision()?.configurationSection();
        if (cfg is null) return;
        foreach (var para in cfg.configurationParagraph())
        {
            if (para.specialNamesParagraph() is not { } sn) continue;
            foreach (var entry in sn.specialNameEntry())
            {
                if (entry.classDefinitionClause() is { } cd) { SwitchBindClass(cd); continue; }
                if (entry.implementorSwitchEntry() is not { } sw) continue;
                var ids = sw.cobolWord();   // [0] = switch-name; [1] = mnemonic-name when Option 1
                if (ids.Length == 0) continue;
                string? onName = sw.switchOnClause()?.cobolWord()?.GetText();
                string? offName = sw.switchOffClause()?.cobolWord()?.GetText();
                // Only a genuine switch clause registers: a mnemonic (Option 1) or ≥1 status condition (Option 2)
                // — the §12.3.7 format requires at least one of the three phrases.
                if (ids.Length < 2 && onName is null && offName is null) continue;

                string implName = ids[0].GetText();
                SwitchMnemonics.TryAdd(ids.Length >= 2 ? ids[1].GetText() : implName, implName);
                if (onName is not null) SwitchConditions.TryAdd(onName, (implName, true));
                if (offName is not null) SwitchConditions.TryAdd(offName, (implName, false));
            }
        }
    }

    /// <summary>One <c>CLASS class-name IS {literal [THRU literal]}…</c> clause (ISO §12.3.7): expand each value
    /// item to its member characters — a multi-character literal lists each character; a THRU pair contributes the
    /// contiguous native-collating range between the two single-character ordinals, ASCENDING OR DESCENDING (the
    /// clause's GR allows either order — NC174A's <c>"D" THROUGH "A"</c> equals <c>"A" THRU "D"</c>).</summary>
    private void SwitchBindClass(Core.ClassDefinitionClauseContext cd)
    {
        string name = cd.cobolWord(0).GetText();
        var members = new System.Text.StringBuilder();
        foreach (var item in cd.classValueSet().classValueItem())
        {
            var lits = item.literal();
            string lo = LiteralChars(lits[0]);
            if (lits.Length >= 2)
            {
                string hi = LiteralChars(lits[1]);
                if (lo.Length == 1 && hi.Length == 1)
                {
                    char a = lo[0], b = hi[0];
                    if (a > b) (a, b) = (b, a);
                    for (char c = a; c <= b; c++) members.Append(c);
                    continue;
                }
            }
            members.Append(lo);
        }
        UserClasses.TryAdd(name, members.ToString());
    }

    /// <summary>The character content of a class-definition literal: a quoted literal's characters, or — for an
    /// unsigned integer literal — the character at that ORDINAL position of the native collating sequence
    /// (1-based, ISO §12.3.7; ordinal n ⇒ char code n−1 over the 8-bit native sequence).</summary>
    private static string LiteralChars(Core.LiteralContext lit)
    {
        string text = lit.GetText();
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
            return text[1..^1].Replace("\"\"", "\"");
        return int.TryParse(text, out int ordinal) && ordinal >= 1 && ordinal <= 256
            ? ((char)(ordinal - 1)).ToString()
            : text;
    }
}
