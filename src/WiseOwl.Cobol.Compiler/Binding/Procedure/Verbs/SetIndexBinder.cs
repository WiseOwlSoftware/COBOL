// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SET Formats 1 and 2, index-name and integer assignment and UP BY / DOWN BY (ISO §2023:14.9.39.3 SR2; §1985:VI.6.23): one binder of the SET format families <see cref="SetBinder"/> dispatches to (kb/Work
/// PB2314; DESIGN-architecture-review §8.3, R0-0019). <see cref="SetFormatSelection"/> chose the format before this binder runs.</summary>
internal sealed class SetIndexBinder(BinderContext ctx, StatementBinder host, SetFormatSelection fmt)
{
    /// <summary>SET Format 1, <c>SET receivers… TO { arithmetic-expression-1 | index-name-2 | identifier-2 }</c> (ISO §2023:14.9.39.2),
    /// once <see cref="SetBinder"/> has selected the format from the receiving list and the classified
    /// <paramref name="sender"/>: the sending alternative (SR2), the COBOL-85 amount gate, each receiver's screen, and one
    /// <see cref="BoundSetTo"/>.</summary>
    internal BoundStatement BindIndexAssignment(Core.DataReferenceContext[] recvs, SetSender sender, Core.ArithmeticExpressionContext amount)
    {
        // §2023:14.9.39.3 SR2 — the sending alternative, classified ONCE for every receiver (kb/Work PB212).
        if (IndexAssignmentSenderOf(recvs, sender) is not { } alternative) return BoundRejected.Reported(ctx.Edition);
        GateCobol85IndexAmount(amount, upDown: false);
        var targets = new List<BoundSetTarget>();
        bool admitted = true;
        foreach (var dref in recvs)
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{WrittenSource.Of(dref)}'");
            admitted &= ScreenIndexAssignmentReceiver(t, WrittenSource.Of(dref), alternative, sender.Text);
            targets.Add(t);
        }
        if (!admitted) return BoundRejected.Reported(ctx.Edition);
        // §2023:14.9.39.4 GR2: "item identification of the data item referenced by identifier-1 is done immediately before the
        // value of that data item is changed", so SetEmitter places an object-property receiver's accessors around ITS
        // store (kb/Work PB2078): `SET IX, BAL OF AR(IX) TO 2` reaches AR(2).
        ctx.Data.Procedure.ClaimInterleavedReceiver(targets.OfType<SetPlaceTarget>().Select(t => t.Place));
        return new BoundSetTo(targets, FormatOneValueOf(sender, amount));
    }

    /// <summary>The value Format 1 stores, as an expression. A function-identifier or inline invocation sender is
    /// ALREADY bound (classifying it bound it, once), so it must not be bound again: an index function is
    /// identifier-2's value as it stands, and any other identifier is arithmetic-expression-1 and takes the SAME
    /// §2023:8.8.1.1 numeric-class screen the expression spine applies to it — <see cref="ExpressionBinder.ScreenIdentifierOperand"/>,
    /// the half <c>BindIdentifierOperand</c> shares. Everything else (a data reference, a literal, an expression) binds
    /// as an r7 window, as before (kb/Work R29).</summary>
    private BoundExpr FormatOneValueOf(SetSender sender, Core.ArithmeticExpressionContext amount) =>
        sender is { Bound: { } bound, Syntax: { } syntax }
            ? sender.IdentifierClass is CobolClass.Index
                ? bound
                : host.Expr.ScreenIdentifierOperand(bound, syntax, OperandContext.ArithmeticIndexWindow)
            : host.Expr.BindIndexWindowExpr(amount);   // SET is an r7 window (kb/Work R29)

    /// <summary>Format 1's sending brace, <c>{ arithmetic-expression-1 | index-name-2 | identifier-2 }</c> (ISO
    /// §2023:14.9.39.2, the rendered figure: three alternatives, one required).</summary>
    private enum IndexAssignmentSender { ArithmeticExpression, IndexName, IndexDataItem }

    /// <summary>Which of Format 1's three sending alternatives the sender IS — or null, after reporting, when it is
    /// none of them.
    /// <para>⚠ DETERMINATION (kb/Work PB212 / PB388's SR2 question, settled from the rendered §2023:14.9.39.2 Format-1
    /// figure): SR2 — "Identifier-2 shall reference a data item of class index" — governs the identifier-2
    /// ALTERNATIVE, not every bare identifier. A single numeric identifier is an arithmetic expression (§2023:8.8.1.1:
    /// "An arithmetic expression may be an identifier referencing a numeric data item"), so <c>SET IX TO N</c> over an
    /// integer <c>N</c> is arithmetic-expression-1 and legal against an index-name receiver. The strict reading —
    /// that an integer sender is refused because it is not of class index — is rejected: it would make
    /// arithmetic-expression-1 unable to be a single data item. What SR2 DOES refuse is a bare identifier that
    /// is neither of class index nor numeric: it is no alternative of the brace. That refusal is written HERE, in
    /// SR2's own words, rather than left to the §2023:8.8.1.1 arithmetic screen to reach incidentally.</para>
    /// <para>⛔ A SENDER IS CLASSIFIED BY ITS CLASS, NOT BY HOW IT IS WRITTEN (kb/Work PB1399). identifier-2 is "a data
    /// item of class index" (§2023:14.9.39.3 SR2) and §2023:8.4.3.1.3 SR1 lets it be any identifier format, so an INDEX
    /// FUNCTION — §2023:15.2 GR6: "These are of the class and category index" — is identifier-2 exactly as an index data
    /// item is. The classifier used to return <see cref="IndexAssignmentSender.ArithmeticExpression"/> for every
    /// sender that was not a bare data reference, which put an index function through §2023:14.9.39.3 SR3 / the §2023:8.8.1.1
    /// numeric screen. A function-identifier or invocation of any OTHER class stays arithmetic-expression-1, whose
    /// §2023:8.8.1.1 class screen then answers for it (<see cref="FormatOneValueOf"/>).</para></summary>
    private IndexAssignmentSender? IndexAssignmentSenderOf(IReadOnlyList<Core.DataReferenceContext> recvs, SetSender sender)
    {
        if (sender.Ref is not { } senderDref)   // a literal, an expression, or a function-identifier / invocation
            return sender.IdentifierClass is CobolClass.Index
                ? IndexAssignmentSender.IndexDataItem : IndexAssignmentSender.ArithmeticExpression;
        string senderText = sender.Text;
        switch (fmt.KindOf(senderDref))
        {
            case SetOperandKind.IndexName: return IndexAssignmentSender.IndexName;
            case SetOperandKind.IndexDataItem: return IndexAssignmentSender.IndexDataItem;
            case SetOperandKind.OtherDataItem
                when ctx.Refs.Probe(senderDref) is { } sp && !IntrinsicArgumentRules.IsArithmeticOperandClass(sp.Item):
                ctx.Edition.Error(DiagnosticCatalog.SetIndexAssignmentOperand,
                    $"SET {SetFormatSelection.Written(recvs)} TO '{senderText}': the sender is "
                    + ctx.Edition.Edition.Cite(
                        "not identifier-2 — §1985:VI.6.23.3 SR2: \"Identifier-1 and identifier-2 must each reference an index data item or an "
                        + "elementary item described as an integer\" — nor integer-1",
                        "neither identifier-2 — ISO §2023:14.9.39.3 SR2: \"Identifier-2 shall reference a data item of class index\" — nor a numeric "
                        + "operand of arithmetic-expression-1 (ISO §2023:8.8.1.1)"));
                return null;
            default: return IndexAssignmentSender.ArithmeticExpression;
        }
    }

    /// <summary>§2023:14.9.39.3 SR1, SR3 and SR4 over ONE receiving operand of Format 1, as the receiver-class ×
    /// sender-alternative table §2023:14.9.39.4 GR2 implies: it defines the statement for an index-name receiver from
    /// any of the three alternatives (GR2 a)), and for a data-item receiver only from the alternatives its class
    /// admits — so every other pairing is a syntax-rule violation, never a statement the emitter should store.
    /// <list type="bullet">
    ///   <item>SR1 is the RECEIVER's class — the <see cref="OperandPositions.SetIndexAssignmentReceiver"/> row of
    ///         the ONE operand-class screen. Refused in both lanes: every refused class either threw at run time
    ///         or stored a value (PIC 9(4)V99 received 5 as 000500) GR2 defines for no such receiver.</item>
    ///   <item>SR3 — a class-index receiver: arithmetic-expression-1 "shall not be specified".</item>
    ///   <item>SR4 — a numeric receiver: "index-name-2 shall be specified" — so neither arithmetic-expression-1
    ///         nor an index-data-item identifier-2.</item>
    /// </list>
    /// SR3 and SR4 go through the <see cref="EditionContext.Removed"/> seam: an error under strict, and under
    /// <c>--permissive</c> a warning with the value stored as before — the documented leniency for source that
    /// assigns a number to an integer or index data item with SET.</summary>
    private bool ScreenIndexAssignmentReceiver(BoundSetTarget t, string text, IndexAssignmentSender sender, string senderText)
    {
        if (t is not SetPlaceTarget { Place: var p }) return true;   // index-name-1 — GR2 a) admits every sender
        if (!OperandClassScreen.Screen(ctx.Edition, OperandPositions.SetIndexAssignmentReceiver, p, text)) return false;
        bool indexReceiver = (OperandClassScreen.ClassesOf(p) & OperandClasses.IndexDataItem) != 0;
        // The rule each pairing breaks, per edition (§1985:VI.6.23.4 GR5's table names the general rule of each "No": an index data
        // item receiver is GR3b's, an integer data item receiver GR3c's); a later edition's wording is one more Cite argument.
        (string Later, string Y1985)? rule = (indexReceiver, sender) switch
        {
            (true, IndexAssignmentSender.ArithmeticExpression) =>
                ("§2023:14.9.39.3 SR3: \"If identifier-1 references a data item of class index, arithmetic-expression-1 shall not be "
                    + "specified\"",
                 "§1985:VI.6.23.4 GR3b: \"If identifier-1 references an index data item, it may be set equal to either the content of "
                    + "index-name-2 or identifier-2 where identifier-2 also references an index data item\" (GR5's table: no integer "
                    + "literal or integer data item may be sent to an index data item)"),
            (false, IndexAssignmentSender.ArithmeticExpression or IndexAssignmentSender.IndexDataItem) =>
                ("§2023:14.9.39.3 SR4: \"If identifier-1 references a numeric data item, index-name-2 shall be specified\"",
                 "§1985:VI.6.23.4 GR3c: \"If identifier-1 does not reference an index data item, it may be set only to an occurrence "
                    + "number that corresponds to the value of index-name-2. Neither identifier-2 nor integer-1 can be used in this case.\""),
            _ => null,
        };
        if (rule is not { } broken) return true;
        ctx.Edition.Removed(DiagnosticCatalog.SetIndexAssignmentOperand.Code,
            $"SET '{text}' TO '{senderText}' violates " + ctx.Edition.Edition.CiteRule(broken.Y1985, broken.Later));
        return ctx.Edition.Permissive;
    }

    /// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §2023:14.9.39 Format 2), with Formats 10 and 14
    /// sharing the same grammar shape — selected from the WHOLE receiving list (kb/Work PB449).
    /// <para>⛔ FORMAT 2's RECEIVING BRACE IS <c>{ index-name-3 } …</c>, AND THAT IS THE WHOLE LIST: §2023:14.9.39.2
    /// prints no <c>identifier</c> alternative for it and §2023:14.9.39.4 GR4 is written "For each occurrence of
    /// index-name-3". So the three UP/DOWN formats between them admit an index-name, a data-pointer (SR23) and a
    /// dynamic-capacity register (SR29) — and NOTHING else. <c>SET WS-N UP BY 4</c> over a <c>PIC 9(4)</c> used
    /// to compile and answer 5; it now draws COBOL2112, because no printed format admits it.</para></summary>
    internal BoundStatement BindSetUpDown(Core.SetIndexStatementContext ud)
    {
        var recvs = SetFormatSelection.Receivers(ud.setReceivingIdentifier());
        var kinds = fmt.KindsOf(recvs);
        var amount = ud.arithmeticExpression();
        bool down = ud.DOWN() is not null;
        switch (SetFormatSelection.Select(kinds, SetDirections.UpDown, out bool exact))
        {
            case SetFormat.F10:   // data-pointer arithmetic — PtrBinder screens EVERY operand against SR23
                return host.Ptr.BindSetUpDown(ud);
            case SetFormat.F14:
                return host.Set.Capacity.BindSetCapacity(recvs, kinds, amount, down ? SetCapacityKind.DownBy : SetCapacityKind.UpBy);
            case SetFormat.F2 when !exact:   // an index-name mixed with something Format 2 does not admit
                fmt.ReportNotAdmitted(recvs, kinds, SetFormat.F2);
                return BoundRejected.Reported(ctx.Edition);
            case SetFormat.F2:
                break;
            default:
                fmt.ReportNoFormat(recvs, kinds, SetDirections.UpDown, WrittenSource.Of(amount));
                return BoundRejected.Reported(ctx.Edition);
        }
        GateCobol85IndexAmount(amount, upDown: true);
        var targets = new List<BoundSetTarget>();
        foreach (var dref in recvs)
        {
            if (SetTargetOf(dref) is not { } t) return new BoundUnsupported($"SET receiver '{WrittenSource.Of(dref)}'");
            targets.Add(t);
        }
        return new BoundSetUpDown(targets, host.Expr.BindIndexWindowExpr(amount), down);
    }

    /// <summary>⛔ THE X3.23-1985 SENDING OPERANDS OF FORMATS 1 AND 2 (kb/Work PB5074). §1985:VI.6.23.2 prints Format 1's
    /// sender as <c>{ index-name-2 | identifier-2 | integer-1 }</c> and Format 2's amount as <c>{ identifier-3 | integer-2 }</c>,
    /// held to §1985:VI.6.23.3 SR2 ("Identifier-1 and identifier-2 must each reference an index data item or an elementary
    /// item described as an integer"), SR3 ("Identifier-3 must reference an elementary numeric integer") and SR4
    /// ("Integer-1 and integer-2 may be signed. Integer-1 must be positive."), with §1985:III.2's "neither signed nor zero
    /// unless explicitly allowed". ISO/IEC 1989:2002 prints arithmetic-expression-1 and arithmetic-expression-2 in those
    /// positions (14.8.35.1), so any other operand, a non-integer literal or item, a zero or negative integer-1, an
    /// expression, is the 2002 form: row <c>set-arithmetic-expression-2002</c>, or <c>set-up-down-index-amount-2002</c> for an
    /// index-name or index data item as Format 2's amount (kb/Work PB5593), asked once per statement, after the
    /// format is selected (a statement no format admits is refused already).</summary>
    internal void GateCobol85IndexAmount(Core.ArithmeticExpressionContext amount, bool upDown)
    {
        if (Cobol85AmountRefusal(amount, upDown) is not { } row
            || ConstructRegistry.IsAvailable(ctx.Edition.Edition, row)) return;
        using var _ = ctx.Edition.At(amount);
        ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition.Sink, row,
            $"the SET {(upDown ? "UP BY or DOWN BY" : "TO")} operand {WrittenSource.Of(amount)}");
    }

    /// <summary>The construct row that names why <paramref name="amount"/> is not one of the 1985 operands
    /// <see cref="GateCobol85IndexAmount"/> names — an integer literal with the sign its position admits, or a data reference to
    /// an elementary numeric integer, and, for Format 1's sender only, an index-name (index-name-2) or an index data item
    /// (identifier-2, §1985:VI.6.23.3 SR2) (an item the reference does not resolve, or one of another class, is refused by its own rule and is not asked here) — or null when it is
    /// one. An index-name or index data item as Format 2's amount has a row of its own (<see cref="Constructs.SetUpDownIndexAmount2002"/>,
    /// kb/Work PB5593), so the refusal names the operand it refuses and not the three forms
    /// (<see cref="Constructs.SetArithmeticExpression2002"/>) it is not.</summary>
    private string? Cobol85AmountRefusal(Core.ArithmeticExpressionContext amount, bool upDown)
    {
        if (amount.additiveExpression().multiplicativeExpression() is not [var term]
            || term.powerExpression() is not [var power] || power.unaryExpression() is not [var unary])
            return Constructs.SetArithmeticExpression2002;
        if (unary.primaryExpression()?.dataReference() is not { } dref)
            return Cobol85OperandForms.IsInteger(unary, upDown ? Cobol85IntegerSign.Any : Cobol85IntegerSign.Positive)
                ? null : Constructs.SetArithmeticExpression2002;
        // §1985:VI.6.23.3 SR3 — "Identifier-3 must reference an elementary numeric integer": an index-name and an index
        // data item are neither (kb/Work PB5421). Format 1's sender admits an index-name (the index-name-2 brace) and an index data
        // item (identifier-2, SR2), so only the UP/DOWN amount asks.
        if (upDown && fmt.KindOf(dref) is SetOperandKind.IndexName or SetOperandKind.IndexDataItem)
            return Constructs.SetUpDownIndexAmount2002;
        return ctx.Refs.ProbeDeclaration(dref) is not { Pic: { Category: PicCategory.Numeric, Scale: > 0 } }
            ? null : Constructs.SetArithmeticExpression2002;
    }

    /// <summary>A Format-1 / Format-2 SET receiving operand: an INDEXED BY index-name (its <c>long</c> field) or a
    /// resolvable data item (an index data item or an integer item — the emitter dispatches on its usage).
    /// <para>It applies NO class screen, and that is deliberate: it is shared with PERFORM VARYING's induction
    /// variable, whose class rule is a different one (§2023:14.9.28.3 SR2). Each caller asks the ONE operand-class
    /// screen for its OWN position — <see cref="BindIndexAssignment"/> for §2023:14.9.39.3 SR1 (kb/Work PB212), the varying
    /// phrase for §2023:14.9.28.3 SR2 / SR5 a). The FORMAT question is not carried here either —
    /// <see cref="SetFormatSelection"/> has already established that every receiver belongs to Format 1's or
    /// Format 2's brace.</para></summary>
    internal BoundSetTarget? SetTargetOf(Core.DataReferenceContext dref) =>
        host.Expr.IndexTargetOf(dref) is { } ix ? ix
        : host.Expr.ResolveReceivingAtStore(dref) is { } p ? new SetPlaceTarget(p)   // a SET receiver IS a receiving operand, identified as it is set (GR2; PB5386)
        : null;
}
