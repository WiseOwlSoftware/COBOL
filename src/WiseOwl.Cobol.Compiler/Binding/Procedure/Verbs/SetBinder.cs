// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The SET verb binder (P7 Step 10m — a real collaborator over <see cref="BinderContext"/>): the
/// seventeen-format dispatch of ISO §2023:14.9.39.2. A format a RESERVED WORD identifies is chosen by its own
/// grammar alternative in <see cref="BindSet"/> (3 switch, 4 condition, 6 attribute, 11/12 locale, 13
/// last-exception, 15 content, and the ENTRY / ADDRESS OF / SIZE OF phrases); the formats that SHARE a token
/// shape — 1, 2, 5, 7, 8, 9, 10, 14, 16 — are selected SEMANTICALLY, and that selection is
/// <see cref="SetFormatSelection"/>'s alone.
/// <para>⛔ THE CHAIN OF PER-FORMAT PEEKS IS GONE (kb/Work PB449 + PB456). Each candidate format used to try in
/// contract order — the F10 pointer peek first, then the F14 CAPACITY-register peek, then the F5 object
/// re-route — and each read <c>receivers[0]</c> (and, for the TO direction, the sender) and returned null
/// otherwise. Three consequences, all measured: the same operands gave different verdicts in different orders,
/// a receiving list no printed format admits fell through to Format 1/2 arithmetic, and a re-route that
/// declined left NOTHING behind it. Now the format is decided once, from the whole receiving list, and each
/// format's own binder is reached having already been chosen — so what those binders screen is the RULE
/// (SR8/SR17/SR20/SR21/SR23), uniformly over every operand.</para>
/// <para>⛔ THIS TYPE SELECTS; EACH FORMAT FAMILY'S BINDER BINDS (kb/Work PB2314; DESIGN-architecture-review §8.3, R0-0019):
/// <see cref="SetContentBinder"/> (Format 15), <see cref="SetLocaleBinder"/> (11, 12), <see cref="SetPointerBinder"/> (7, 8, 9, 17),
/// <see cref="SetAddressBinder"/> (the ENTRY, ADDRESS OF FUNCTION and ADDRESS OF PROGRAM senders), <see cref="SetIndexBinder"/>
/// (1, 2), <see cref="SetCapacityBinder"/> (14, 16) and <see cref="SetConditionBinder"/> (4). Switches go via the .AlterSwitches
/// host edge (10n), objects via the OO host edge (10s). <c>SetBinderFamilyDriftTests</c> keeps the dispatch the only thing
/// declared here.</para></summary>
internal sealed class SetBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>⛔ THE ONE FORMAT SELECTOR (kb/Work PB449 + PB456). Every shape-sharing SET format is chosen
    /// HERE, from the WHOLE receiving list, before any format's own binder runs — see
    /// <see cref="SetFormatSelection"/> for why a per-format peek at <c>receivers[0]</c> could not work.</summary>
    private readonly SetFormatSelection _fmt = new(ctx, host);

    /// <summary>⛔ THE ONE CLASSIFIER OF A `SET … TO` SENDER (kb/Work PB1399 + PB1929): a data reference, a
    /// function-identifier, an inline method invocation, or none of them — read once, so every format asks its own
    /// syntax rule of the same <see cref="SetSender"/>.</summary>
    private readonly SetSenders _senders = new(host);

    // ⛔ ONE BINDER PER SET FORMAT FAMILY (kb/Work PB2314; DESIGN-architecture-review §8.3, R0-0019): this type selects the
    // format and each family's binder binds it. SetBinderFamilyDriftTests keeps the dispatch the only thing bound here.
    private SetContentBinder? _content;
    /// <summary>The binder of Format 15.</summary>
    private SetContentBinder Content => _content ??= new(ctx, host);
    private SetLocaleBinder? _locale;
    /// <summary>The binder of Formats 11 and 12.</summary>
    private SetLocaleBinder Locale => _locale ??= new(ctx, host);
    private SetPointerBinder? _pointer;
    /// <summary>The binder of Formats 7 (no ADDRESS phrase), 8, 9 and 17, and INITIALIZE's implicit SET.</summary>
    internal SetPointerBinder Pointer => _pointer ??= new(ctx, host);
    private SetAddressBinder? _address;
    /// <summary>The binder of the ENTRY, ADDRESS OF FUNCTION and ADDRESS OF PROGRAM senders.</summary>
    internal SetAddressBinder Address => _address ??= new(ctx, host);
    private SetIndexBinder? _index;
    /// <summary>The binder of Formats 1 and 2.</summary>
    internal SetIndexBinder Index => _index ??= new(ctx, host, _fmt);
    private SetCapacityBinder? _capacity;
    /// <summary>The binder of Formats 14 and 16.</summary>
    internal SetCapacityBinder Capacity => _capacity ??= new(ctx, host, _fmt);
    private SetConditionBinder? _condition;
    /// <summary>The binder of Format 4.</summary>
    private SetConditionBinder Condition => _condition ??= new(ctx, host);

    /// <summary>Refuse every SET receiving operand written in an identifier format that is never a receiving operand,
    /// each by its own rule (<see cref="ExpressionBinder.ResolveReceiving(Core.SetReceivingIdentifierContext)"/>), and
    /// the statement with them; null when every receiver is a data reference. A <c>setStatement</c> alternative is one
    /// format rule, so its receivers are that rule's own children.</summary>
    private BoundStatement? RefuseNonReceivable(Core.SetStatementContext set)
    {
        if (set.ChildCount == 0 || set.GetChild(0) is not Antlr4.Runtime.ParserRuleContext format) return null;
        bool refused = false;
        foreach (var r in format.GetRuleContexts<Core.SetReceivingIdentifierContext>())
            if (r.dataReference() is null)
            {
                _ = host.Expr.ResolveReceiving(r);
                refused = true;
            }
        foreach (var r in format.GetRuleContexts<Core.SetToReceiverContext>())
            if (r.nonReceivableIdentifier() is { } n)
            {
                _ = host.Expr.RefuseNonReceivable(n);
                refused = true;
            }
        return refused ? BoundRejected.Reported(ctx.Edition) : null;
    }

    /// <summary>Bind a SET statement, dispatching by format (ISO §2023:14.9.39; DESIGN §12.3). The COBOL-85
    /// surface — Format 1 index/value assignment, Format 2 UP/DOWN BY, Format 4 condition-name TO TRUE — binds here;
    /// the later-edition formats (switches need SPECIAL-NAMES, pointers/objects their 2002 subsystems, TO FALSE the
    /// 2002 FALSE phrase) fail loud by NAME until their subsystem lands.</summary>
    public BoundStatement BindSet(Core.SetStatementContext set)
    {
        // ⛔ A RECEIVING OPERAND WRITTEN IN AN IDENTIFIER FORMAT THAT IS NEVER ONE (kb/Work PB1551): SELF, SUPER, NULL, an
        // object-view, a function- or program-address-identifier. Every SET receiving list parses them (the grammar's
        // `setReceivingIdentifier`, and setToStatement's `setToReceiver`) so each is refused by its OWN rule at the
        // receiving chokepoint — once, here, before a format is selected from a list that holds a non-reference.
        if (RefuseNonReceivable(set) is { } nonReceivable) return nonReceivable;
        // F11 set-locale / F12 save-locale (§2023:14.9.39; Annex A.4.9 item 9) — IMPLEMENTED since kb/Work PB64 T1 (it
        // used to be refused by name with COBOL1518 — kb/Work PB92 before that: "'LOCALE' is not defined").
        if (set.setLocaleStatement() is { } sl)
        {
            bool save = sl.setLocaleSource() is null;   // F12 has no Format-11 TO operand (the grammar splits the two formats)
            return save ? Locale.BindSaveLocale(sl) : Locale.BindSetLocale(sl);
        }
        // F6 attribute (§2023:14.9.39; Annex A.4.2 item 24) — DECLINED: the screen module. It had NO grammar
        // alternative at all before kb/Work PB260, so `SET SG ATTRIBUTE HIGHLIGHT ON` was a generic COBOL0001
        // parse error pointing at ATTRIBUTE rather than a named refusal naming the facility.
        if (set.setScreenAttributeStatement() is { } sat)
        {
            ScreenFacility.ReportSetAttribute(ctx.Edition, WrittenSource.Of(sat.dataReference()));
            return BoundRejected.Reported(ctx.Edition);
        }
        if (set.setLastExceptionStatement() is not null) return host.Ec.BindSetLastException();   // F13 (ISO §2023:14.9.39; 2002+)
        if (set.setContentStatement() is { } sc) return Content.BindSetContent(sc);   // F15 numeric-content (2014; kb/Work PB452)
        if (set.setEntryStatement() is { } se) return Address.BindSetEntry(se);   // F9 + §2023:8.4.3.13 ENTRY sender (P10 Step 7)
        if (set.setSizeStatement() is { } ss)
            return Capacity.BindSetSize(ss.setReceivingIdentifier().dataReference(), ss.arithmeticExpression(), explicitSizeOf: true);   // F16 explicit SIZE OF (2023)
        if (set.setToStatement() is { } st)
        {
            var sender = st.setToSender();
            // ⛔ FORMAT 7 BY ITS SPELLING (kb/Work PB4231). An `ADDRESS OF data-name-1` receiver or a data-address-
            // identifier sender is printed by Format 7 alone, so such a statement goes to Format 7's own binder before
            // any receiver is resolved, and no other format ever meets the phrase. Its arithmetic-arm sender is read by
            // the ONE classifier, as every other SET … TO sender is. Every other shape is selected from the receiving
            // list (SetFormatSelection).
            if (st.SpellsAddressPhrase())
            {
                SetSender? addressSender = sender.arithmeticExpression() is { } arith ? _senders.Classify(arith) : null;
                if (addressSender is not null && SetSenders.IsRefused(addressSender)) return BoundRejected.Reported(ctx.Edition);
                return host.Ptr.BindSetAddress(st, addressSender);   // F7 both directions + ADDRESS OF senders
            }
            var recvs = SetFormatSelection.Receivers(st.setToReceiver());
            return sender.predefinedNull() is { } toNull ? BindSetToNull(recvs, toNull) : BindSetTo(recvs, sender.arithmeticExpression());
        }
        if (set.setIndexStatement() is { } ud) return Index.BindSetUpDown(ud);
        if (set.setBooleanStatement() is { } b) return Condition.BindSetCondition(b);
        if (set.setSwitchStatement() is { } sw) return host.Alter.SwitchBindSet(sw);   // Format 3 — external switches (ISO §2023:14.9.39)
        if (set.setFunctionAddressStatement() is { } sfa) return Address.BindSetFunctionAddress(sfa);   // F8 + §2023:8.4.3.12 sender (kb/Work PB452)
        if (set.setProgramAddressStatement() is { } spa) return Address.BindSetProgramAddress(spa);     // F9 + §2023:8.4.3.13 sender (kb/Work PB549)
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, $"SET '{WrittenSource.Of(set)}': the statement matches no SET format (ISO §2023:14.9.39.2)");
    }

    /// <summary><c>SET receivers… TO NULL</c> — the predefinedNull arm of <c>setToSender</c>, which no other arm
    /// derives (kb/Work PB4231). NULL is the sender of Formats 5, 7, 8, 9 and 17 at once, so the format is SELECTED from
    /// the whole receiving list (§2023:14.9.39.2; kb/Work PB449) and the chosen format's own syntax rule then refuses any
    /// operand it does not admit — SR17 (Format 7), SR20 (Format 8), SR21 (Format 9), SR8 (Format 5). This used to
    /// sniff <c>dataReference(0)</c> alone, which made <c>SET P1 X TO NULL</c> and <c>SET X P1 TO NULL</c> two different
    /// verdicts. ⛔ WITHOUT THE FUNCTION-POINTER ARM a declarable function-pointer falls through into the OO Format-5
    /// bind and, through BindSetTo, into the Format-1 arithmetic store — the silent wrong answer PB817 filed as "the
    /// other end, which a fixer will otherwise miss".</summary>
    private BoundStatement BindSetToNull(Core.DataReferenceContext[] recvs, Core.PredefinedNullContext toNull)
    {
        // ⛔ SET IS AN SR1 CONTEXT, SO IT ASKS THE EDITION TOO (kb/Work PB1673, review w20261769b R1-2). The predefined
        // NULL exists only where §2023:8.9 reserves the word (COBOL 2002 on); at COBOL-85 an undeclared NULL token names
        // nothing declared, the §2023:8.4.2.1 undefined reference every other NULL position draws there
        // (ExpressionBinder.NullAdmittingOperand, PredefinedNullRule.Report) — never the object-reference SET's SR8
        // of a facility the edition does not have.
        if (UnreservedKeywordReference.IsFreeAt("NULL", ctx.Edition))
        {
            UnreservedKeywordReference.Report(ctx.Edition, "NULL");
            return BoundRejected.Reported(ctx.Edition);
        }
        return SetFormatSelection.Select(_fmt.KindsOf(recvs), SetDirections.To, out _) switch
        {
            // Formats 7, 9, 8 and 17. ⛔ F17 IS THE SECOND ARM OF THE SAME DISPATCH (kb/Work PB453; feedback_two_arm_dispatch):
            // `SET MT TO NULL` arrives HERE rather than through BindSetTo, and it was this arm that produced the §2023:14.9.39.3 SR8
            // object-reference diagnostic the note measured.
            SetFormat pointer when SetPointerBinder.Binds(pointer) => Pointer.BindTo(pointer, recvs, null, toNull, amount: null),
            // Format 5, and the residual: NULL is a sender of no other format, so a receiving list that selects
            // Format 1/14/16 here is refused by SR8 inside — the diagnostic that names what the statement was
            // trying to be.
            _ => host.Oo.OoBindSetObjectRef(recvs, null, senderNull: true, senderSelf: false, senderSuper: false),
        };
    }

    /// <summary><c>SET receivers… TO value</c>, the arithmeticExpression arm of <c>setToSender</c>. ⛔ THE FORMAT IS
    /// SELECTED ONCE, FROM EVERY RECEIVING OPERAND (§2023:14.9.39.2; <see cref="SetFormatSelection"/>, kb/Work PB449 +
    /// PB456). Formats 1, 5, 7, 8, 9, 14 and 16 all reach this grammar rule, and each of their receiving braces is
    /// written <c>{ … } …</c>, so the whole list decides — never <c>receivers[0]</c>, and never the SENDER.
    /// <para>The sender used to be half the sniff, and that is what let <c>SET U TO 5</c> escape every rule:
    /// the Format-5 re-route's precondition was "the sender is exactly one bare data reference", a literal is
    /// not one, so the re-route declined and an object reference landed in the Format-1 arithmetic store. Now
    /// the receivers select Format 5 whatever the sender looks like, and SR9 refuses the literal by name.</para></summary>
    private BoundStatement BindSetTo(Core.DataReferenceContext[] recvs, Core.ArithmeticExpressionContext amount)
    {
        var kinds = _fmt.KindsOf(recvs);
        // As written: GetText() prints a bare ZERO as its rewritten ZERO_ARITH token "0" (kb/Work PB4209, PB3961).
        string senderText = WrittenSource.Of(amount);
        // ⛔ THE FORMATS THAT TAKE THE AMOUNT AS AN EXPRESSION OF THEIR OWN are decided from the receiving list alone,
        // BEFORE the sender is classified: classifying binds a sole function-identifier (and registers its
        // activation), and F14/F16 bind the same expression themselves — binding it here as well would run a user
        // function twice. No sender can change which of these formats the statement is (their rows name no sending
        // kind), so nothing is lost by asking them first.
        switch (SetFormatSelection.Select(kinds, SetDirections.To, out _))
        {
            case SetFormat.F14:   // OCCURS DYNAMIC capacity change (data-model D9)
                return Capacity.BindSetCapacity(recvs, kinds, amount, SetCapacityKind.To);
            case SetFormat.F16:   // the SIZE-OF-absent bare form; the explicit form enters at BindSetSize
                return Capacity.BindSetSize(recvs, kinds, amount);
            case null:
                _fmt.ReportNoFormat(recvs, kinds, SetDirections.To, senderText);
                return BoundRejected.Reported(ctx.Edition);
        }
        // The sender AS AN IDENTIFIER of any of §2023:8.4.3.1.2's formats (§2023:14.9.39.2's identifier-2/-4/-6/-8/-13
        // positions) — a data reference, a function-identifier or an inline method invocation; null of all three
        // for a literal or an expression, which each format's own syntax rule then refuses (kb/Work PB1399/PB1929).
        var sender = _senders.Classify(amount);
        if (SetSenders.IsRefused(sender)) return BoundRejected.Reported(ctx.Edition);
        // ⛔ SelectForTo, not Select: the receiving list decides, EXCEPT against Format 1's `identifier-1`
        // catch-all brace, where a sender of class object or of category data-/program-/function-pointer is
        // admissible in no Format-1 sending position (§2023:8.8.1.1 / §2023:14.9.39.3 SR2) and names exactly one other
        // format. `SET N4 TO U` is that statement: Format 1's brace admits N4, so the sender used to reach the
        // ARITHMETIC screen (COBOL0844, "'U' is not a numeric operand") while the rule the program broke was
        // §2023:14.9.39.3 SR8. It now selects Format 5 and SR8 refuses N4 by name.
        switch (_fmt.SelectForTo(kinds, sender, out _))
        {
            // Formats 7, 9, 8 and 17 (the receivers select F17, or a message-tag SENDER over Format 1's catch-all brace).
            // SELF / SUPER senders arrive here too (the expression tier carries them, kb/Work PB1425), by name.
            case SetFormat pointer when SetPointerBinder.Binds(pointer):
                return Pointer.BindTo(pointer, recvs, sender, toNull: null, amount);
            case SetFormat.F5:
                return host.Oo.OoBindSetObjectRef(recvs, sender, senderNull: false, senderSelf: sender.IsSelf,
                    senderSuper: sender.IsSuper, senderText);
            case SetFormat.F1:
                break;
            default:
                _fmt.ReportNoFormat(recvs, kinds, SetDirections.To, senderText);
                return BoundRejected.Reported(ctx.Edition);
        }
        return Index.BindIndexAssignment(recvs, sender, amount);
    }

}
