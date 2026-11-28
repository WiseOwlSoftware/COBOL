// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Compiler.Binding.Model;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;

using Core = CobolParserCore;

/// <summary>The SET Formats 14 and 16, a dynamic table's capacity and a dynamic-capacity item's size (ISO §2023:14.9.39.2): one binder of the SET format families <see cref="SetBinder"/> dispatches to (kb/Work
/// PB2314; DESIGN-architecture-review §8.3, R0-0019). <see cref="SetFormatSelection"/> chose the format before this binder runs.</summary>
internal sealed class SetCapacityBinder(BinderContext ctx, StatementBinder host, SetFormatSelection fmt)
{
    /// <summary>SET Format 14 (ISO §2023:14.9.39.2; OCCURS DYNAMIC, data-model D9) — <c>SET data-name-2
    /// {TO | UP BY | DOWN BY} {integer-1 | arithmetic-expression-4}</c> changes the table's current capacity.
    /// Reached only when <see cref="SetFormatSelection"/> has already selected Format 14 from the whole receiving
    /// list, so the only receiver rules left are SR29 and the format's CARDINALITY: <c>SET data-name-2</c> is printed
    /// WITHOUT an ellipsis, so the register is the sole receiver. ⛔ BOTH ARE ASKED OF THE WHOLE LIST BEFORE ANY
    /// RECEIVER IS READ (kb/Work PB3225): the selection picks this format as the nearest row for a list holding a
    /// register in ANY position, so a leading non-register used to be read as "the register could not be
    /// addressed" — a not-implemented warning and a run-unit abort for source the standard refuses — while the
    /// same operands in the other order drew the cardinality error. The verdict is the list's, never its order's.
    /// <para>⛔ THE LITERAL ALTERNATIVE IS SCREENED HERE (§2023:14.9.39.3 SR30; kb/Work PB458). Only the
    /// arithmetic-expression alternative is left to §2023:14.9.39.4 GR29/GR30's run-time condition and clamp; integer-1
    /// is a SYNTAX rule and its violation is a refusal. SR30's capacity bounds are conditioned on the SET's own
    /// TO alternative — UP BY / DOWN BY write a DELTA, which "not less than the minimum capacity" cannot be
    /// about — so those two take the nonnegative half alone.</para></summary>
    internal BoundStatement BindSetCapacity(
        IReadOnlyList<Core.DataReferenceContext> targets, IReadOnlyList<SetOperandKind> kinds,
        Core.ArithmeticExpressionContext amount, SetCapacityKind kind)
    {
        // §2023:14.9.39.3 SR29 and the format's one-operand brace, over the whole list (kb/Work PB3225) — before any
        // receiver is read, as BindSetSize does for Format 16.
        if (targets.Count != 1)
        {
            fmt.ReportNotSoleReceiver(targets, kinds, SetFormat.F14);
            return BoundRejected.Reported(ctx.Edition);
        }
        // A PURE capacity-register peek (NOT refs.Resolve, which would route an OO `prop OF obj` first target through
        // the property hook and enqueue a spurious pending op — OCCURS DYNAMIC review #7). The selection classified
        // this very reference as a CAPACITY register through the same peek, so a miss is a broken invariant.
        if (ctx.Refs.CapacityRegisterFor(targets[0], report: true) is not { } cap)
            throw new InvalidOperationException(
                $"SET Format 14 was selected for '{WrittenSource.Of(targets[0])}', which is not a CAPACITY register (kb/Work PB3225)");
        // The target NAMES a register but breaks one of its reference rules (§2023:13.18.38.3 SR30/SR31, §2023:8.4.2.2.3 SR4,
        // §2023:8.4.3.3.3 SR1; kb/Work PB457). This is still SET Format 14 — SetFormatSelection chose the format from the
        // receiving list, and a CAPACITY register is what this receiver IS — so the format is CONSUMED here and the
        // reference's own rule is stated by the ONE screen that owns it (Refs.Resolve → CapacityPlaceOf). Nothing is
        // stacked on top of that diagnostic: the receiver is implemented; the reference is illegal.
        // ⚠ ResolveSending, deliberately: the receiving chokepoint REFUSES a CAPACITY register (COBOL1523 —
        // "except in a SET statement Format 14"), and this IS Format 14; the call exists only for the resolver's
        // own reference diagnostic.
        // An AMBIGUOUS register (§2023:8.4.2.2.3 SR1; kb/Work PB2988) is the same screen's: refused, or under --permissive
        // warned and resolved to the first declared register, whose place the resolver then hands back.
        CapacityRegisterPlace place;
        if (cap.Fault is not ReferenceResolver.CapacityRefFault.None || cap.Place is null)
        {
            if (host.Expr.ResolveSending(targets[0]).Place is not CapacityRegisterPlace resolved)
                return new BoundUnsupported("SET receiving operand");
            place = resolved;
        }
        else place = cap.Place;

        if (SetLiteralAmount.Of(amount) is { } integer1)
        {
            // §2023:13.18.38.4 GR16/GR17 name the two capacities SR30 compares against: "Integer-4 is the minimum
            // capacity of the table. If integer-4 is absent, a value of zero is assumed for it" and "Integer-5
            // is the expected capacity".
            var spec = cap.Table.OccursSpec;
            bool to = kind == SetCapacityKind.To;
            var bound = new SetAmountBound(
                Min: to ? spec?.InitialCap ?? 0 : 0,
                Max: to ? spec?.ExpectedMax : null,
                MinWhat: to && spec?.InitialCap is { } min and > 0
                    ? $"nonnegative and not less than the minimum capacity ({min}) defined in the corresponding OCCURS clause"
                    : "nonnegative",
                MaxWhat: $"not greater than the expected capacity ({spec?.ExpectedMax}) defined in the corresponding OCCURS clause",
                Operand: "integer-1", Rule: "ISO §2023:14.9.39.3 SR30");
            if (SetLiteralAmount.Violation(integer1, bound) is { } why)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLiteralAmountOutOfRange,
                    $"SET '{cap.Register.CobolName}' {SetCapacityKinds.Text(kind)} {integer1}: {why} ({bound.Rule})");
            }
        }
        return new BoundSetCapacity(place.Table, host.Expr.BindIndexWindowExpr(amount), kind);
    }

    /// <summary>⛔ BOTH ARMS OF FORMAT 16 ARE ONE BIND (kb/Work PB458). <c>[ SIZE OF ]</c> is a bracket, so
    /// <c>SET SIZE OF D TO n</c> and <c>SET D TO n</c> are the SAME format with the same rules — and the
    /// SIZE-OF-absent arm used to be a separate peek that refused to resolve anything carrying a
    /// <c>dataReferenceSuffix</c>. That test was aimed at subscripts, but a suffix ALSO carries QUALIFICATION,
    /// so <c>SET D OF G TO 2</c> — legal source (§2023:8.4.2.2.3 SR2: "a name may be qualified even though it does
    /// not need qualification"; §2023:13.16.3 SR18 leaves an OCCURS clause impossible here, so the subscript half had
    /// nothing legal to exclude) — fell into the Format-1 numeric store and died at run time. The ONE receiver
    /// resolution is <see cref="ExpressionBinder.ResolveReceiving"/>, the same one the explicit arm always used;
    /// the format is selected before either arm is entered, so no peek is needed to keep a speculative resolve
    /// off the OO property hook.</summary>
    internal BoundStatement BindSetSize(IReadOnlyList<Core.DataReferenceContext> targets,
                                       IReadOnlyList<SetOperandKind> kinds, Core.ArithmeticExpressionContext amount)
    {
        // §2023:14.9.39.2 Format 16 prints ONE data-name-3, with no ellipsis: a second receiver is no Format 16.
        if (targets.Count != 1)
        {
            fmt.ReportNotSoleReceiver(targets, kinds, SetFormat.F16);
            return BoundRejected.Reported(ctx.Edition);
        }
        return BindSetSize(targets[0], amount, explicitSizeOf: false);
    }

    /// <summary>SET [SIZE OF] data-name-3 TO n (ISO §2023:14.9.39 Format 16, COBOL-2023): set the current length of a
    /// dynamic-length elementary item. data-name-3 shall itself be dynamic-length (SR33 → COBOL1568). The 2023
    /// introduction gate is on the <see cref="BoundSetSize"/> node (VersionConformancePass semantic arm), covering
    /// both the explicit SIZE OF form and the bare re-routed form. Whether EC-STORAGE-NOT-AVAIL checking is enabled
    /// at this statement is NOT captured here: §2023:14.9.39.4 GR37/GR38's nonfatal condition rides the ambient
    /// (EC-STORAGE-NOT-AVAIL → StorageNotAvailChecking) pair the EcBinder already adds to every statement in a
    /// checking-on region, which is also what carries it into the §2023:14.6.13.1.4 #3 selection (kb/Work PB367b).
    /// <para>This is the bind over the SOLE receiver, reached directly by the explicit <c>SIZE OF</c> arm (the
    /// grammar admits exactly one receiver there) and by the bare arm once its list is known to be one.</para></summary>
    internal BoundStatement BindSetSize(Core.DataReferenceContext dref, Core.ArithmeticExpressionContext amount,
                                       bool explicitSizeOf)
    {
        if (host.Expr.ResolveReceiving(dref) is not { } p)
            return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
        if (!p.Item.IsDynamicLength)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetSizeTargetNotDynamicLength,
                $"SET SIZE OF '{p.Item.CobolName}': data-name-3 shall be a dynamic-length elementary item "
                + "(ISO §2023:14.9.39.3 SR33, Format 16)");
        }
        // §2023:14.9.39.3 SR34 over the LITERAL alternative — "Integer-2 shall be non-negative, and shall be equal to
        // or less than the maximum size of data-name-3, as specified in 8.5.1.10" — the maximum §2023:8.5.1.10.1
        // defines and DataItem.DynMaxSize carries. §2023:14.9.39.4 GR37/GR38's EC-STORAGE-NOT-AVAIL and clamp are the
        // rules for arithmetic-expression-5 and stay exactly where they are (kb/Work PB458).
        if (SetLiteralAmount.Of(amount) is { } integer2)
        {
            var bound = new SetAmountBound(0, p.Item.DynMaxSize, "non-negative",
                $"equal to or less than the maximum size of '{p.Item.CobolName}' ({p.Item.DynMaxSize} character "
                + "positions, ISO §2023:8.5.1.10.1)", "integer-2", "ISO §2023:14.9.39.3 SR34");
            if (SetLiteralAmount.Violation(integer2, bound) is { } why)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.SetLiteralAmountOutOfRange,
                    $"SET {(explicitSizeOf ? "SIZE OF " : "")}'{p.Item.CobolName}' TO {integer2}: {why} ({bound.Rule})");
            }
        }
        return new BoundSetSize(p, host.Expr.BindIndexWindowExpr(amount), p.Item.DynMaxSize);
    }
}
