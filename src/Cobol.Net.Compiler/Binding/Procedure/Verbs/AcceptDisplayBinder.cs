// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
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

    /// <summary>The implementor device-names a DISPLAY may transfer output TO (ISO §14.9.11.3 SR2 — a device
    /// "capable of receiving data from the program"; §12.3.7.3 rule 7/8 delegates the available names to the
    /// implementor, COBOLNET_DESIGN §12.3). SYSIN is the ACCEPT-side (input-only) name — a mnemonic bound to it fails
    /// SR2. SYSERR routes to standard error; CONSOLE / SYSOUT and the no-UPON default use the standard display device
    /// (standard output).</summary>
    private static readonly HashSet<string> DisplayOutputDevices = new(StringComparer.OrdinalIgnoreCase)
    {
        "CONSOLE", "SYSOUT", "SYSERR",
    };

        /// <summary>Bind ACCEPT (ISO §14.9.1). Format 1: no FROM (the implementor default device, GR5) or FROM a
    /// SPECIAL-NAMES mnemonic-name (SR2). Format 2: FROM a temporal source; the <c>YYYYMMDD</c>/<c>YYYYDDD</c>
    /// four-digit-year phrases are COBOL-2002+ and rejected below that edition (the version-gating rule).
    /// <para>Format 3 (screen) is the DECLINED Annex A.4.2 module and is refused by name here — see
    /// <see cref="ScreenFormatOf"/>. ⚠ This comment used to read "its syntax does not parse under the Format-1
    /// rule, so nothing silently degrades", and that was exactly wrong: <c>ACCEPT screen-name-1</c> is the
    /// MINIMAL format-3 statement and is token-identical to the format-1 device ACCEPT, so it parsed, bound,
    /// and read device input into a screen record with no diagnostic anywhere (kb/Work PB260).</para>
    /// The receiver resolves like any other (qualified / subscripted / ref-modified).</summary>
    public BoundStatement BindAccept(Core.AcceptStatementContext ac)
    {
        // Format 3 (screen) — Annex A.4.2 item 1, DECLINED. Refused BEFORE anything else so no format-1 rule is
        // applied to a statement that is not format 1.
        if (ScreenFormatOf(ac.screenTail(), ac.dataReference() is { } recv ? [recv] : []) is { } why)
        {
            ScreenFacility.ReportAcceptScreen(ctx.Edition, why);
            return new BoundNop();
        }

        // END-ACCEPT: the explicit scope terminator is a COBOL-2002 introduction (ISO §14.9.1 general formats; the
        // 1985 ACCEPT has none). The edition gate (EndAccept2002) moved to the post-bind VersionConformancePass
        // (Step 14e), reading BoundAccept.HasEndTerminator — computed once here and stamped on each ACCEPT node.
        bool endTerm = AcceptHasTerminator(ac);

        // An index-NAME receiver: not an identifier at all (ISO §8.4.3.1.2 — an index-name is none of the
        // identifier formats), and §13.18.38.3 r7 closes the contexts that may reference one. The context
        // diagnostic (COBOLNET1637), not the §8.4.2.1 UNDEFINED report the demanding resolve would produce.
        if (ac.dataReference() is { } dref && host.Expr.IndexFieldOf(dref) is not null)
        {
            ctx.Edition.Error("COBOLNET1637", $"ACCEPT receiver '{dref.GetText()}' is an index-name — an "
                + "index-name is not an identifier (ISO §8.4.3.1.2) and ACCEPT is not among the contexts that "
                + "may reference one (§13.18.38.3 r7); SET a data item to it first (§14.9.39)");
            return new BoundUnsupported($"ACCEPT into index-name '{dref.GetText()}'");
        }

        if (ctx.Refs.Resolve(ac.dataReference()) is not { } target)
            return new BoundUnsupported($"ACCEPT receiver '{ac.dataReference().GetText()}'");

        // Format 2 is FROM a temporal source; FROM omitted / FROM mnemonic is the Format 1 device transfer.
        bool temporal = ac.acceptSource() is { } tsrc && tsrc.dataReference() is null;

        // §14.9.1.3 SR1 (identifier-1) / SR3 (identifier-2): the excluded receiver CLASSES both formats share.
        // SR1 — "neither a strongly-typed group item nor a data item of class index, message-tag, object, or
        // pointer"; SR3 repeats the same class rows for the temporal receiver (message-tag has no declarable
        // shape in this data model). SR3's class-alphabetic/boolean exclusions are NOT listed here — they fall
        // out of the Table 16 ask below. A strongly-typed group temporal receiver fails GR6's MOVE-rules store
        // identically (§14.9.25.3 SR2 — the sender must be a group of the SAME type, and the conceptual
        // temporal sender is an untyped integer), so both formats screen it.
        var rItem = target.Item;
        string? excluded =
            rItem.Pic is { Usage: Usage.Index } ? "an index data item (class index)"
            : rItem.Pic?.Category is PicCategory.ObjectReference ? "a data item of class object"
            : rItem.Pic?.Category is PicCategory.Pointer or PicCategory.ProgramPointer ? "a data item of class pointer"
            : StrongTypeModel.IsStrongGroup(rItem) ? "a strongly-typed group item"
            : null;
        if (excluded is not null)
        {
            ctx.Edition.Error("COBOLNET0818", $"ACCEPT receiver '{rItem.CobolName}' is {excluded}, which "
                + (temporal ? "the temporal format excludes (ISO §14.9.1.3 SR3" + (StrongTypeModel.IsStrongGroup(rItem) ? "; §14.9.25.3 SR2 via §14.9.1.4 GR6" : "") + ")"
                            : "the device format excludes (ISO §14.9.1.3 SR1)"));
            return new BoundUnsupported($"ACCEPT into {excluded} '{rItem.CobolName}'");
        }

        // §14.9.1.4 GR6: the temporal value stores "according to the rules for the MOVE statement" — the
        // legality question is asked of the ONE Table 16 mechanism (PB53's MoveTable16; the conceptual sender
        // is an unsigned INTEGER of usage display, GR7–GR12). That makes SR3's class-alphabetic and
        // class-boolean exclusions AUTOMATIC (both are Table-16 'No' rows for an integer sender), along with
        // every other refused receiver category — no hand-rolled copy of the table to drift.
        if (temporal && MoveTable16.Refusal(new Table16Operand(PicCategory.Numeric), Table16Operand.Of(target)) is { } refusal)
        {
            ctx.Edition.Error("COBOLNET0818", $"ACCEPT receiver '{rItem.CobolName}': the temporal transfer "
                + $"stores by the MOVE rules (ISO §14.9.1.4 GR6 / §14.9.1.3 SR3) and this move is invalid — {refusal}");
            return new BoundUnsupported($"ACCEPT temporal into '{rItem.CobolName}'");
        }

        // SR6: "Neither identifier-1 nor identifier-2 shall reference a variable-length group" (§8.5.1.12 —
        // a group with a DYNAMIC LENGTH elementary item or dynamic-capacity table subordinate at any depth).
        if (rItem.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(rItem))
        {
            ctx.Edition.Error(DiagnosticCatalog.AcceptVariableLengthGroup, $"ACCEPT receiver '{rItem.CobolName}' references a "
                + "variable-length group (a DYNAMIC LENGTH item or dynamic-capacity table is subordinate to "
                + "it) — ISO §14.9.1.3 SR6");
            return new BoundUnsupported($"ACCEPT into variable-length group '{rItem.CobolName}'");
        }

        if (ac.acceptSource() is not { } src)
            return new BoundAccept(target, AcceptKind.Device) { HasEndTerminator = endTerm };   // GR5 — FROM omitted: the implementor default (stdin)

        if (src.dataReference() is { } mnemonic)
        {
            var accepted = BindAcceptFromMnemonic(target, mnemonic);
            return accepted is BoundAccept mba ? mba with { HasEndTerminator = endTerm } : accepted;
        }

        // Format 2 — temporal. accept-four-digit-year-2002: the pass owns the edition gate (Exec Step E).

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

    /// <summary>The classes a DISPLAY operand shall not present (kb/Work PB148): §14.9.11.3 SR1 excludes
    /// class message-tag (undeclarable here), object and pointer — the old fall-through printed a pointer's
    /// ManagedPointer.ToString() and an object's CLR text; §13.18.60.3 SR10's closed list keeps an index DATA
    /// item out of DISPLAY — the old numeric arm printed a zero-digit EMPTY image for it; and the
    /// grammar-carried NULL word (class pointer — NOT a §8.3.3.6.2 figurative) rejects through the same
    /// gate, where it previously printed U+0000.</summary>
    private static readonly CobolClass[] DisplayExcludedClasses =
        [CobolClass.Pointer, CobolClass.Object, CobolClass.Index];

    private const string DisplayClassCite =
        "ISO §14.9.11.3 SR1 (class object/pointer); §13.18.60.3 SR10 (an index data item may be referenced "
        + "only in SEARCH/SET, a relation condition, an intrinsic/method argument, or a USING phrase)";

    /// <summary>Is this ACCEPT / DISPLAY the SCREEN format (ISO §14.9.1.2 format 3 / §14.9.11.2 format 2 —
    /// Annex A.4.2 items 1 and 9)? Returns the REASON it was recognized, or null for the device/temporal formats.
    /// <para>⛔ THE TWO RECOGNITIONS ARE NOT EQUALLY VISIBLE, and that asymmetry is the whole defect kb/Work
    /// PB260 measured. A positioning or exception phrase can only be the screen format. But the MINIMAL screen
    /// statement — <c>ACCEPT screen-name-1</c> / <c>DISPLAY screen-name-1</c> — is token-identical to its device
    /// format, so it parsed and bound as one: the screen record's characters were written to standard output and
    /// device input was read into a screen record, with no diagnostic anywhere. Only the operand's DECLARATION
    /// tells them apart, which is why <see cref="DataBinder.ScreenNames"/> is consulted here.</para></summary>
    private string? ScreenFormatOf(Core.ScreenTailContext? tail, IReadOnlyList<Core.DataReferenceContext> operands)
    {
        if (tail is not null)
            return tail.screenExceptionPhrases() is not null
                ? "the AT / LINE / COLUMN positioning phrase with an ON EXCEPTION handler appears only in the "
                  + "screen format"
                : "the AT / LINE / COLUMN positioning phrase appears only in the screen format";
        // The operand scan costs nothing in the overwhelmingly common case: no SCREEN SECTION, no names, and
        // DISPLAY is the binder's hottest statement — so the empty-set test comes before the per-operand work.
        if (ctx.Data.ScreenNames.Count == 0) return null;
        foreach (var dref in operands)
        {
            string name = dref.cobolWord()?.GetText() ?? dref.GetText();
            if (ctx.Data.ScreenNames.Contains(name))
                return $"'{dref.GetText()}' is declared in the SCREEN SECTION, so this is the screen format even "
                     + "though it is spelled exactly like the device format";
        }
        return null;
    }

    public BoundStatement BindDisplay(Core.DisplayStatementContext display)
    {
        // Format 2 (screen) — Annex A.4.2 item 9, DECLINED. See ScreenFormatOf: `DISPLAY SG` used to print the
        // screen record and `DISPLAY SG COLUMN 5` used to bind as a THREE-operand device DISPLAY.
        if (ScreenFormatOf(display.screenTail(), display.dataReference()) is { } why)
        {
            ScreenFacility.ReportDisplayScreen(ctx.Edition, why);
            return new BoundNop();
        }

        var ops = new List<BoundOperand>();
        foreach (IParseTree child in StatementBinder.Children(display))
        {
            BoundOperand? op = child switch
            {
                Core.LiteralContext lit => host.Expr.LiteralOperand(lit),
                Core.DataReferenceContext dref => host.Expr.FieldOperand(dref),
                // DISPLAY FUNCTION … (ISO §8.4.4.1 — an identifier includes a function-identifier; §14.9.11.2).
                Core.FunctionCallContext fc => host.Intrinsic.IntrinsicOperand(fc),
                _ => null,
            };
            if (op is null) continue;
            string text = child.GetText();
            // DISPLAY is none of §13.18.38.3 r7's five index-name contexts (kb/Work R16 — this compiled
            // clean and aborted at run time before).
            if (child is Core.DataReferenceContext && host.Expr.ScreenIndexNameOperand(op, text, "DISPLAY"))
            {
                ops.Add(new BoundOperandError($"DISPLAY of the index-name '{text}' (ISO §13.18.38.3 r7)"));
                continue;
            }
            // The operand-class gate (kb/Work PB148) — every operand shape, literals and intrinsic results
            // included (FUNCTION MAX(IX1 IX2) is class index per §15.2 item 6).
            if (host.Expr.ScreenOperandClass(op, DisplayExcludedClasses, text, "DISPLAY", DisplayClassCite))
            {
                ops.Add(new BoundOperandError($"DISPLAY operand '{text}' of an excluded class"));
                continue;
            }
            ops.Add(op);
        }
        bool toStdErr = display.displayUpon() is { } upon && BindDisplayUpon(upon);
        return new BoundDisplay(ops, display.displayNoAdvancing() is not null, toStdErr);
    }

    /// <summary><c>DISPLAY … UPON mnemonic-name-1</c> (ISO §14.9.11.3 SR2): the mnemonic shall be declared in
    /// SPECIAL-NAMES and associated with a device CAPABLE OF RECEIVING data from the program. An undeclared name or an
    /// input-only device is a bind-time rejection — the legacy silently dropped the UPON phrase and always displayed on
    /// the standard device. Returns true iff the resolved device is SYSERR (standard error routing); every other
    /// output device — and every rejected case — uses the standard display device (§14.9.11.4 GR8).</summary>
    private bool BindDisplayUpon(Core.DisplayUponContext upon)
    {
        string name = upon.cobolWord().GetText();
        if (!ctx.Mnemonics.Of(upon).TryGetValue(name, out string? device))
        {
            ctx.Edition.Error("COBOLNET0817", $"DISPLAY UPON '{name}': not a mnemonic-name declared in SPECIAL-NAMES "
                + "(ISO §14.9.11.3 SR2 — mnemonic-name-1 shall be associated with an implementor device-name, "
                + "§12.3.7 Format 4 'device-name-1 IS mnemonic-name-3')");
            return false;
        }
        if (!DisplayOutputDevices.Contains(device))
        {
            ctx.Edition.Error("COBOLNET0817", $"DISPLAY UPON '{name}': device '{device}' is not capable of receiving "
                + "data (ISO §14.9.11.3 SR2; the output-capable implementor device-names are CONSOLE, SYSOUT, and "
                + "SYSERR, §12.3.7.3)");
            return false;
        }
        return device.Equals("SYSERR", StringComparison.OrdinalIgnoreCase);
    }
}
