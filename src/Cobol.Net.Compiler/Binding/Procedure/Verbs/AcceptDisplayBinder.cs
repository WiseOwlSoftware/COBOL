// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;
using CobolNet.Editions;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary><c>ACCEPT identifier [FROM source]</c> (ISO §14.9.1). <see cref="AcceptKind.Device"/> is Format 1 —
/// the hardware-device transfer (a plain <c>ACCEPT</c>, GR5, or <c>FROM mnemonic-name</c> resolved through the
/// SPECIAL-NAMES device registry, §12.3.7 Format 4): data REPLACES the receiver's content, stored ALIGNED LEFT
/// with additional transfers requested / excess ignored by size (GR1–GR4 — explicitly NOT the MOVE rules). Every
/// other kind is Format 2 — a temporal source read from the system clock as a conceptual UNSIGNED INTEGER USAGE
/// DISPLAY item and transferred BY THE MOVE RULES (GR6), of conceptual width 6/8/5/7/8/1 (GR7–GR12).</summary>
public sealed record BoundAccept(Place Target, AcceptKind Kind) : BoundStatement
{
    /// <summary>True when the ACCEPT was written with the explicit END-ACCEPT scope terminator (ISO §14.9.1
    /// general formats — COBOL-2002; the 1985 ACCEPT has none). The edition gate (EndAccept2002) reads this in the
    /// post-bind <see cref="Validation.VersionConformancePass"/> (rearch PHASE-03 Step 14e); the terminator has no
    /// semantic effect, so only its presence is recorded.</summary>
    public bool HasEndTerminator { get; init; }
}

/// <summary>The data source of an ACCEPT (ISO §14.9.1): the Format 1 device, or one of the Format 2 temporal
/// sources — DATE (YYMMDD, GR7), DATE YYYYMMDD (GR8, 2002+), DAY (YYDDD, GR9), DAY YYYYDDD (GR10, 2002+),
/// TIME (HHMMSScc, GR11), DAY-OF-WEEK (1=Monday…7=Sunday, GR12).</summary>
public enum AcceptKind { Device, Date, DateYYYYMMDD, Day, DayYYYYDDD, DayOfWeek, Time }

public sealed partial class StatementBinder
{
    /// <summary>The implementor device-names an ACCEPT may take input from (ISO §12.3.7.3 items 7–8 — the
    /// implementor specifies the available device-names; COBOLNET_DESIGN §12.3): both name the process standard
    /// input. SYSOUT / SYSERR are the DISPLAY-side (output-only) names — a mnemonic bound to one fails SR2.</summary>
    private static readonly HashSet<string> AcceptInputDevices = new(StringComparer.OrdinalIgnoreCase)
    {
        "CONSOLE", "SYSIN",
    };

    private Dictionary<string, string>? _acceptMnemonics;

    /// <summary>Bind ACCEPT (ISO §14.9.1). Format 1: no FROM (the implementor default device, GR5) or FROM a
    /// SPECIAL-NAMES mnemonic-name (SR2). Format 2: FROM a temporal source; the <c>YYYYMMDD</c>/<c>YYYYDDD</c>
    /// four-digit-year phrases are COBOL-2002+ and rejected below that edition (the version-gating rule). Format 3
    /// (screen ACCEPT) needs the SCREEN SECTION subsystem — its syntax does not parse under the Format-1 rule, so
    /// nothing silently degrades. The receiver resolves like any other (qualified / subscripted / ref-modified).</summary>
    private BoundStatement BindAccept(Core.AcceptStatementContext ac)
    {
        // END-ACCEPT: the explicit scope terminator is a COBOL-2002 introduction (ISO §14.9.1 general formats; the
        // 1985 ACCEPT has none). The edition gate (EndAccept2002) moved to the post-bind VersionConformancePass
        // (Step 14e), reading BoundAccept.HasEndTerminator — computed once here and stamped on each ACCEPT node.
        bool endTerm = AcceptHasTerminator(ac);

        if (refs.Resolve(ac.dataReference()) is not { } target)
            return new BoundUnsupported($"ACCEPT receiver '{ac.dataReference().GetText()}'");

        // SR1/SR3: the receiver shall not be of class index (nor object / pointer / message-tag — classes the data
        // model does not yet admit). The SR3 class-alphabetic/boolean exclusions await their PicCategory split.
        if (target.Item.Pic is { Usage: Usage.Index })
        {
            data.Edition.Error("COBOLNET0818", $"ACCEPT receiver '{target.Item.CobolName}' is an index data item "
                + "(class index), which neither ACCEPT format permits (ISO §14.9.1.3 SR1/SR3)");
            return new BoundUnsupported($"ACCEPT into index item '{target.Item.CobolName}'");
        }

        if (ac.acceptSource() is not { } src)
            return new BoundAccept(target, AcceptKind.Device) { HasEndTerminator = endTerm };   // GR5 — FROM omitted: the implementor default (stdin)

        if (src.dataReference() is { } mnemonic)
        {
            var accepted = BindAcceptFromMnemonic(target, mnemonic);
            return accepted is BoundAccept mba ? mba with { HasEndTerminator = endTerm } : accepted;
        }

        // Format 2 — temporal. The four-digit-year phrases are COBOL-2002+ (the 1985 §14.9.1 formats list only the
        // bare DATE / DAY / DAY-OF-WEEK / TIME); reject below 2002, never silently accept-and-misbehave.
        if ((src.YYYYMMDD() ?? src.YYYYDDD()) is { } phrase && data.Edition.DialectLevel < 2002)
            data.Edition.Error("COBOLNET0815", $"ACCEPT FROM {(src.DATE() is not null ? "DATE YYYYMMDD" : "DAY YYYYDDD")} "
                + $"— the {phrase.GetText()} (four-digit-year) phrase was introduced by ISO/IEC 1989:2002 (§14.9.1); "
                + $"it requires --std 2002 or later (targeting COBOL-{data.Edition.DialectLevel})");

        AcceptKind kind =
            src.DATE() is not null ? (src.YYYYMMDD() is not null ? AcceptKind.DateYYYYMMDD : AcceptKind.Date)
            : src.TIME() is not null ? AcceptKind.Time
            : src.DAY_OF_WEEK() is not null ? AcceptKind.DayOfWeek
            : src.DAY() is not null ? (src.YYYYDDD() is not null ? AcceptKind.DayYYYYDDD : AcceptKind.Day)
            : AcceptKind.Device;   // unreachable by grammar; Device keeps the bind total
        return new BoundAccept(target, kind) { HasEndTerminator = endTerm };
    }

    /// <summary><c>ACCEPT … FROM mnemonic-name-1</c> (ISO §14.9.1 Format 1, SR2): the mnemonic shall be declared in
    /// SPECIAL-NAMES and associated with a device CAPABLE OF INPUT. An undeclared name or an output-only device is
    /// a bind-time rejection — the legacy silently treated every FROM word as the console; the spec says reject.</summary>
    private BoundStatement BindAcceptFromMnemonic(Place target, Core.DataReferenceContext mnemonic)
    {
        string name = mnemonic.cobolWord()?.GetText() ?? mnemonic.GetText();
        if (!AcceptMnemonics(mnemonic).TryGetValue(name, out string? device))
        {
            data.Edition.Error("COBOLNET0817", $"ACCEPT FROM '{name}': not a mnemonic-name declared in SPECIAL-NAMES "
                + "(ISO §14.9.1.3 SR2 — mnemonic-name-1 shall be associated with an implementor device-name, "
                + "§12.3.7 Format 4 'device-name-1 IS mnemonic-name-3')");
            return new BoundUnsupported($"ACCEPT FROM undeclared mnemonic '{name}'");
        }
        if (!AcceptInputDevices.Contains(device))
        {
            data.Edition.Error("COBOLNET0817", $"ACCEPT FROM '{name}': device '{device}' is not capable of input "
                + "(ISO §14.9.1.3 SR2; the input-capable implementor device-names are CONSOLE and SYSIN, §12.3.7.3)");
            return new BoundUnsupported($"ACCEPT FROM non-input device mnemonic '{name}'");
        }
        return new BoundAccept(target, AcceptKind.Device);
    }

    /// <summary>The SPECIAL-NAMES {mnemonic-name → DEVICE-NAME} registry (ISO §12.3.7 Format 4: <c>device-name-1
    /// IS mnemonic-name-3</c>), built once per program unit by walking the parse tree UP from the referencing
    /// statement (no bound environment-division model exists yet). Every ENCLOSING program unit contributes too —
    /// SPECIAL-NAMES declarations are visible to contained source units (§12.3.7.4 GR1), the nearest declaration
    /// winning (<c>TryAdd</c>, innermost first). An entry carrying a switch ON/OFF phrase is a SWITCH clause
    /// (Format 3), not a device association, and is skipped; an entry without <c>IS mnemonic</c> declares nothing
    /// ACCEPT can reference.</summary>
    private IReadOnlyDictionary<string, string> AcceptMnemonics(IParseTree at)
    {
        if (_acceptMnemonics is not null) return _acceptMnemonics;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (IParseTree? n = at; n is not null; n = n.Parent)
        {
            if (n is not Core.ProgramUnitContext pu) continue;
            var paragraphs = pu.environmentDivision()?.configurationSection()?.configurationParagraph() ?? [];
            foreach (var para in paragraphs)
                foreach (var entry in para.specialNamesParagraph()?.specialNameEntry() ?? [])
                    if (entry.implementorSwitchEntry() is { } e
                        && e.switchOnClause() is null && e.switchOffClause() is null
                        && e.cobolWord(1) is { } alias)
                        map.TryAdd(alias.GetText(), e.cobolWord(0).GetText().ToUpperInvariant());
        }
        return _acceptMnemonics = map;
    }

    /// <summary>True when the statement carries an explicit <c>END-ACCEPT</c>. Detected by token scan so the
    /// binder works identically whether or not the superset grammar exposes a dedicated accessor for it.</summary>
    private static bool AcceptHasTerminator(Core.AcceptStatementContext ac)
    {
        for (int i = 0; i < ac.ChildCount; i++)
            if (ac.GetChild(i) is ITerminalNode t && t.Symbol.Type == CobolLexer.END_ACCEPT) return true;
        return false;
    }
}
