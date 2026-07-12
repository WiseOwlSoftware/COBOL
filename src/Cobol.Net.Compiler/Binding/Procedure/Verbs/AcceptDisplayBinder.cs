// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The ACCEPT/DISPLAY verb binder (P7 Step 10h — DETACHED from the partial class the Step-5
/// rename left it as; absorbs <c>BindDisplay</c>, making the Step-5 filename honest on the binder side
/// too). The SPECIAL-NAMES mnemonic registry moved to <see cref="BinderContext.Mnemonics"/> — the WRITE
/// SR13 / zero-advance consumers in SequentialIoBinder share it. The <c>BoundAccept</c>/<c>AcceptKind</c>
/// nodes stayed in <c>Binding/Bound/BoundAccept.cs</c>; the 0815 four-digit-year gate moved VERBATIM
/// (Exec Step E folds it).</summary>
internal sealed class AcceptDisplayBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The implementor device-names an ACCEPT may take input from (ISO §12.3.7.3 items 7–8 — the
    /// implementor specifies the available device-names; COBOLNET_DESIGN §12.3): both name the process standard
    /// input. SYSOUT / SYSERR are the DISPLAY-side (output-only) names — a mnemonic bound to one fails SR2.</summary>
    private static readonly HashSet<string> AcceptInputDevices = new(StringComparer.OrdinalIgnoreCase)
    {
        "CONSOLE", "SYSIN",
    };

        /// <summary>Bind ACCEPT (ISO §14.9.1). Format 1: no FROM (the implementor default device, GR5) or FROM a
    /// SPECIAL-NAMES mnemonic-name (SR2). Format 2: FROM a temporal source; the <c>YYYYMMDD</c>/<c>YYYYDDD</c>
    /// four-digit-year phrases are COBOL-2002+ and rejected below that edition (the version-gating rule). Format 3
    /// (screen ACCEPT) needs the SCREEN SECTION subsystem — its syntax does not parse under the Format-1 rule, so
    /// nothing silently degrades. The receiver resolves like any other (qualified / subscripted / ref-modified).</summary>
    public BoundStatement BindAccept(Core.AcceptStatementContext ac)
    {
        // END-ACCEPT: the explicit scope terminator is a COBOL-2002 introduction (ISO §14.9.1 general formats; the
        // 1985 ACCEPT has none). The edition gate (EndAccept2002) moved to the post-bind VersionConformancePass
        // (Step 14e), reading BoundAccept.HasEndTerminator — computed once here and stamped on each ACCEPT node.
        bool endTerm = AcceptHasTerminator(ac);

        if (ctx.Refs.Resolve(ac.dataReference()) is not { } target)
            return new BoundUnsupported($"ACCEPT receiver '{ac.dataReference().GetText()}'");

        // SR1/SR3: the receiver shall not be of class index (nor object / pointer / message-tag — classes the data
        // model does not yet admit). The SR3 class-alphabetic/boolean exclusions await their PicCategory split.
        if (target.Item.Pic is { Usage: Usage.Index })
        {
            ctx.Edition.Error("COBOLNET0818", $"ACCEPT receiver '{target.Item.CobolName}' is an index data item "
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
        if ((src.YYYYMMDD() ?? src.YYYYDDD()) is { } phrase && ctx.Edition.DialectLevel < 2002)
            ctx.Edition.Error("COBOLNET0815", $"ACCEPT FROM {(src.DATE() is not null ? "DATE YYYYMMDD" : "DAY YYYYDDD")} "
                + $"— the {phrase.GetText()} (four-digit-year) phrase was introduced by ISO/IEC 1989:2002 (§14.9.1); "
                + $"it requires --std 2002 or later (targeting COBOL-{ctx.Edition.DialectLevel})");

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
        if (!ctx.Mnemonics.Of(mnemonic).TryGetValue(name, out string? device))
        {
            ctx.Edition.Error("COBOLNET0817", $"ACCEPT FROM '{name}': not a mnemonic-name declared in SPECIAL-NAMES "
                + "(ISO §14.9.1.3 SR2 — mnemonic-name-1 shall be associated with an implementor device-name, "
                + "§12.3.7 Format 4 'device-name-1 IS mnemonic-name-3')");
            return new BoundUnsupported($"ACCEPT FROM undeclared mnemonic '{name}'");
        }
        if (!AcceptInputDevices.Contains(device))
        {
            ctx.Edition.Error("COBOLNET0817", $"ACCEPT FROM '{name}': device '{device}' is not capable of input "
                + "(ISO §14.9.1.3 SR2; the input-capable implementor device-names are CONSOLE and SYSIN, §12.3.7.3)");
            return new BoundUnsupported($"ACCEPT FROM non-input device mnemonic '{name}'");
        }
        return new BoundAccept(target, AcceptKind.Device);
    }

    /// <summary>True when the statement carries an explicit <c>END-ACCEPT</c>. Detected by token scan so the
    /// binder works identically whether or not the superset grammar exposes a dedicated accessor for it.</summary>
    private static bool AcceptHasTerminator(Core.AcceptStatementContext ac)
    {
        for (int i = 0; i < ac.ChildCount; i++)
            if (ac.GetChild(i) is ITerminalNode t && t.Symbol.Type == CobolLexer.END_ACCEPT) return true;
        return false;
    }

    public BoundStatement BindDisplay(Core.DisplayStatementContext display)
    {
        var ops = new List<BoundOperand>();
        foreach (IParseTree child in StatementBinder.Children(display))
            switch (child)
            {
                case Core.LiteralContext lit: ops.Add(host.Expr.LiteralOperand(lit)); break;
                case Core.DataReferenceContext dref: ops.Add(host.Expr.FieldOperand(dref)); break;
                // DISPLAY FUNCTION … (ISO §8.4.4.1 — an identifier includes a function-identifier; §14.9.11.2).
                case Core.FunctionCallContext fc: ops.Add(host.Intrinsic.IntrinsicOperand(fc)); break;
            }
        return new BoundDisplay(ops, display.displayNoAdvancing() is not null);
    }
}
