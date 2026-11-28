// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Compiler.Binding.Procedure.Verbs;

using Core = CobolParserCore;

/// <summary>The SET Formats 7 (without an ADDRESS phrase), 8, 9 and 17: a data-, function-, program-pointer or message-tag receiver assigned NULL or another item of its category (ISO §2023:14.9.39.3 SR17, SR20, SR21, SR22, SR35), and the implicit SET of INITIALIZE ... REPLACING (§2023:14.9.20.4 GR4): one binder of the SET format families <see cref="SetBinder"/> dispatches to (kb/Work
/// PB2314; DESIGN-architecture-review §8.3, R0-0019). <see cref="SetFormatSelection"/> chose the format before this binder runs.</summary>
internal sealed class SetPointerBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The formats this binder binds from a <c>SET … TO</c> statement: 7 (without an ADDRESS phrase), 8, 9 and 17.</summary>
    internal static bool Binds(SetFormat format) => format is SetFormat.F7 or SetFormat.F8 or SetFormat.F9 or SetFormat.F17;

    /// <summary>Bind a <c>SET receivers… TO NULL</c> (<paramref name="toNull"/>) or <c>SET receivers… TO value</c>
    /// (<paramref name="amount"/>, classified as <paramref name="sender"/>) that <see cref="SetBinder"/> selected as
    /// <paramref name="format"/>, one of <see cref="Binds"/>'s. Each format's diagnostic quotes the sender as the program spelled
    /// it (kb/Work PB388): the NULL word, or the TO operand's source extent (kb/Work PB4209).</summary>
    internal BoundStatement BindTo(SetFormat format, Core.DataReferenceContext[] recvs, SetSender? sender,
        Core.PredefinedNullContext? toNull, Core.ArithmeticExpressionContext? amount)
    {
        string senderText = toNull is not null ? toNull.GetText()
            : WrittenSource.Of(amount ?? throw new ArgumentNullException(nameof(amount), "a SET TO value names its sending operand"));
        bool isNull = toNull is not null;
        bool selfSuper = sender?.Predefined is not null;
        return format switch
        {
            SetFormat.F7 => BindSetPointer(recvs, sender, isNull, selfSuper, senderText),
            SetFormat.F9 => BindSetProgramPointer(recvs, sender, isNull, selfSuper, senderText),
            SetFormat.F8 => BindSetFunctionPointer(recvs, sender, isNull, selfSuper, senderText),
            SetFormat.F17 => BindMessageTagSet(recvs, sender, isNull, senderText),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "not a format SetPointerBinder binds"),
        };
    }

    /// <summary>⛔ AN INDEX-NAME OPERAND IS A CATEGORY ERROR, NEVER AN UNDEFINED NAME (kb/Work PB388), and it
    /// reaches SIX operand positions, not one. The carrier re-routes — data-pointer (§2023:14.9.39 Format 7),
    /// function-pointer (Format 8), program-pointer (Format 9) — are selected by whichever operand HAS a
    /// carrier category, so an index-name can stand on the other side of any of them. Each position then
    /// resolved through <c>ctx.Refs.Resolve</c>, which answers for a DATA ITEM only, and the FIRST thing the
    /// user read was COBOL1639: "'IX' is not defined — no declaration in this source element gives the name
    /// 'IX'" — false about a name INDEXED BY declared, and it sends the reader hunting a typo. §2023:13.18.38.3 SR7
    /// closes the list of contexts that may reference an index-name and the SET statement IS one of them, as
    /// Format 1 (§2023:14.9.39.3 SR1). So the statement is still refused — for the category, which is the real
    /// reason. ONE helper, asked FIRST at every one of the six positions.</summary>
    private bool SetIndexNameOperand(Core.DataReferenceContext? dref, string position, string carrier,
                                     string cite)
    {
        if (dref is null || host.Expr.IndexFieldOf(dref) is null) return false;
        ctx.Edition.Error(DiagnosticCatalog.PointerOperandShape,
            $"SET '{WrittenSource.Of(dref)}': an index-name cannot be the {position} of a {carrier} SET — that operand "
            + $"shall be of category {carrier} ({cite}). An index-name operand belongs to Format 1, whose "
            + "receiver is a data item of class index or an integer data item ("
            + ctx.Edition.Edition.CiteRule("§1985:VI.6.23.3 SR2", "§2023:14.9.39.3 SR1") + ")");
        return true;
    }

    /// <summary>⛔ THE ONE RESOLUTION OF A CARRIER FORMAT'S SENDER TO THE DATA ITEM IT REFERENCES (Formats 7, 8, 9 —
    /// kb/Work PB1929). A data reference resolves through the sending chokepoint (an index-name is refused by
    /// <see cref="SetIndexNameOperand"/> first); a function-identifier or inline method invocation already IS the
    /// data item §2023:8.4.3.2.1 / §2023:8.4.3.4.4 GR1 say it references, so its returned temporary is the answer with no
    /// second bind. A null place with a null <paramref name="refusal"/> is an identifier with NO item of its own — an
    /// intrinsic's computed value — which the caller's own category rule then refuses by name.</summary>
    private Place? CarrierSenderPlace(SetSender sender, string carrierWord, string cite, out BoundStatement? refusal)
    {
        refusal = null;
        if (sender.Ref is not { } senderRef) return sender.TemporaryItem;
        if (SetIndexNameOperand(senderRef, "sending operand", carrierWord, cite))
        {
            refusal = BoundRejected.Reported(ctx.Edition);
            return null;
        }
        var answer = host.Expr.ResolveSending(senderRef);
        if (answer.Place is { } place) return place;
        refusal = answer.Refusal(ctx.Edition);   // the resolver's answer (kb/Work PB1030)
        return null;
    }

    /// <summary>The rule that governs a data-pointer SET (Format 7) in the edition in force (kb/Work PB4950): §2023:14.9.39.3 SR17;
    /// COBOL-85's SET statement (§1985:VI.6.23.2) has no data-pointer format, so there it names that standard's formats.</summary>
    private string DataPointerSetRule(string format2023) => ctx.Edition.Edition.Cite(
        "§1985:VI.6.23.2, whose SET statement has no data-pointer format",
        $"ISO §2023:{format2023}, §2023:14.9.39.3 SR17");

    /// <summary>SET data-pointer assignment (§2023:14.9.39 Format 7; Phase-4b increment 1): every target shall
    /// be USAGE POINTER (COBOL0869 otherwise); the sender is the NULL figurative or another data pointer
    /// (SELF/SUPER are object-only — 0869). A pointer-item sender is screened per receiver for §2023:14.9.39.3 SR19's
    /// restricted-data-pointer rule through PtrBinder's one screen (kb/Work PB548); the <c>ADDRESS OF</c>
    /// spellings reach Format 7 through <c>PtrBinder.BindSetAddress</c> (<c>SetToStatementCst.SpellsAddressPhrase</c>).</summary>
    private BoundStatement BindSetPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, SetSender? sender,
        bool toNull, bool senderIsSelfSuper, string? senderText = null)
    {
        if (senderIsSelfSuper)
        {
            using var atWord = ctx.Edition.At(sender?.Predefined);   // the refusal is about the SELF / SUPER word (kb/Work PB4652)
            // ⛔ NAME THE RECEIVERS AND THE WORD THE PROGRAM WROTE (kb/Work PB388's elision sweep). This read
            // `SET … TO SELF/SUPER`, and the diagnostic renderer transliterates U+2026 to ASCII, so the user
            // was shown `SET . TO SELF/SUPER` — neither the receivers they wrote nor the sender they wrote.
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText ?? "SELF/SUPER"}: SELF and SUPER "
                + "are object references, not data pointers "
                + "(ISO §2023:14.9.39.2 Format 7 — the sender of a data-pointer SET is NULL or another pointer; "
                + "SELF and SUPER belong to Format 5, the object-reference assignment)");
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (SetIndexNameOperand(t, "receiving operand", "data-pointer", DataPointerSetRule("14.9.39 Format 7"))) return BoundRejected.Reported(ctx.Edition);
            if (host.Expr.ResolveReceiving(t) is not { } tp || tp.Item.Pic?.Category is not PicCategory.Pointer)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET '{WrittenSource.Of(t)}': the receiving operand of a data-pointer SET shall be of category "
                    + $"data-pointer ({DataPointerSetRule("14.9.39 Format 7")})");
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            // ⛔ A SENDER THAT IS NOT A REFERENCE IS SR17's BUSINESS, NOT A FEATURE GAP (kb/Work PB456). The
            // format is chosen by the RECEIVERS, so a literal or an expression sender now ARRIVES here with
            // sender null — and identifier-6 "shall be of category data-pointer", which a literal is not.
            // This used to return BoundUnsupported, unreachably: the caller's re-route declined for exactly
            // this shape and dropped the statement into the Format-1 arithmetic store instead.
            if (sender is not { IsIdentifierOperand: true })
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: "
                    + "identifier-6 shall be of category data-pointer — a data-pointer sender is the predefined "
                    + "address NULL, another USAGE POINTER item, a function-identifier or inline method invocation "
                    + "of category data-pointer, or ADDRESS OF an identifier, never a literal or "
                    + $"an arithmetic expression ({DataPointerSetRule("14.9.39.2 Format 7")})");
            }
            var sp = CarrierSenderPlace(sender, "data-pointer", DataPointerSetRule("14.9.39 Format 7"), out var senderRefusal);
            if (senderRefusal is not null) return senderRefusal;
            if (sp?.Item.Pic?.Category is not PicCategory.Pointer)
            {
                // ⛔ NAME THE RECEIVERS (kb/Work PB388). The message opened `SET … TO 'x'` and the diagnostic
                // renderer transliterates U+2026 to ASCII, so what the user actually read was `SET . TO 'WS-N'`
                // — a statement nobody wrote. The receivers are in hand. The "ADDRESS OF senders are a later
                // increment" tail went with it: `SET p TO ADDRESS OF x` has bound through PtrBinder's sender
                // form since Phase-4b increment 2, so the message named a non-support that no longer exists.
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO "
                    + $"'{sender.Text}': a data-pointer sender shall be the predefined address NULL, "
                    + "another USAGE POINTER item, a function-identifier or inline method invocation of category "
                    + "data-pointer, or ADDRESS OF an identifier (ISO §2023:14.9.39 Format 7)");
            }
            source = sp;
            // ⛔ THE THIRD ARM (kb/Work PB548). §2023:14.9.39.3 SR19 — "If identifier-5 references a restricted
            // data-pointer, identifier-6 shall be the predefined address NULL or shall reference a data-pointer
            // restricted to the same type" — and its unnumbered continuation ("If identifier-6 references a
            // restricted data-pointer, either identifier-5 shall reference a data-pointer restricted to the same
            // type …") are one rule about each (receiver, sender) pair, asked through PtrBinder's ONE screen, the
            // same call BindSetAddress makes for its identifier-5 receivers. This route used to check only the
            // category, so `SET P TO R` stored an unrestricted address into a restricted data-pointer. TO NULL
            // (the branch not taken here) is admitted by SR19's own words.
            var senderRestriction = StrongTypeModel.PointerRestriction(sp.Item);
            for (int i = 0; i < targets.Count; i++)
            {
                if (!host.Ptr.ScreenPointerReceiverRestriction(targets[i], WrittenSource.Of(targetRefs[i]),
                        senderRestriction, sender.Text, addressSender: false))
                    return BoundRejected.Reported(ctx.Edition);
            }
        }
        // Every receiver on THIS route is identifier-5 (§2023:14.9.39.3 SR17) — the `ADDRESS OF data-name-1`
        // spelling of the printed brace reaches Format 7 through PtrBinder.BindSetAddress instead, and
        // both routes build the SAME receiver list so GR12 and GR13 stay one loop in the emitter.
        return new BoundSetPointer([.. targets.Select(t => new BoundPointerReceiver(t, null))], source, toNull);
    }

    /// <summary>SET message-tag assignment (ISO §2023:14.9.39.2 Format 17; SR35 — "Data-name-4 and data-name-5 shall be
    /// message-tag data items"; kb/Work PB4740): <c>SET data-name-4 TO { data-name-5 | NULL }</c> over the
    /// <c>MessageTag</c> carrier, the Format-9 twin. The sender is NULL or another message-tag item.
    /// <para>⛔ DATA-NAME-4 IS THE RECEIVER IN BOTH RULES (the owner-approved reading, docs/CONFORMANCE.md D-SET17;
    /// DESIGN-message-control.md §0 Q1). The printed format makes data-name-4 the receiver, while §2023:14.9.39.4 GR40 says
    /// "If data-name-5 is specified, the content of data-name-4 is moved to data-name-5" and GR41 "If NULL is specified,
    /// the content of data-name-5 shall be set to that of the null value", which names an operand that is not
    /// written when NULL is. The format, every other SET format and §2023:14.9.39.1 ("setting the content of a message-tag
    /// data item to NULL or another message-tag") agree on the receiver, so the two rules are read as misprints.</para>
    /// <para>The printed format has ONE receiving operand (no repetition, unlike Formats 7, 8 and 9), so a list of
    /// receivers is refused.</para>
    /// <para>⛔ BELOW 2023 THE STATEMENT DOES NOT REACH HERE WHEN THE GATE REFUSED IT (kb/Work PB916, PB4771, PB4836, PB4837):
    /// <c>StatementBinder.BindStatement</c> binds a statement naming a gated message-tag item to a no-op for every verb and
    /// every SET format (<c>StatementBinder.NamesGatedMessageTag</c>), so the SR35 class refusals below speak only where the usage
    /// exists, or where the operand is imported and so ungated (§2023:8.4.3.9.2).</para></summary>
    private BoundStatement BindMessageTagSet(
        IReadOnlyList<Core.DataReferenceContext> receivers, SetSender? sender, bool toNull, string senderText)
    {
        string written = SetFormatSelection.Written(receivers);
        if (receivers.Count != 1)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                $"SET {written} TO {senderText}: a message-tag SET names exactly one receiving data-name-4 "
                + "(ISO §2023:14.9.39.2 Format 17 prints no repetition of the receiving operand)");
        var t = receivers[0];
        if (SetIndexNameOperand(t, "receiving operand", "message-tag", "ISO §2023:14.9.39 Format 17, §2023:14.9.39.3 SR35")) return BoundRejected.Reported(ctx.Edition);
        if (host.Expr.ResolveReceiving(t) is not { } target || target.Item.Pic?.Category is not PicCategory.MessageTag)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                $"SET '{WrittenSource.Of(t)}': the receiving operand of a message-tag SET shall be a message-tag data item "
                + "(ISO §2023:14.9.39.3 SR35 — \"Data-name-4 and data-name-5 shall be message-tag data items\")");
        Place? source = null;
        if (!toNull)
        {
            if (sender is not { IsIdentifierOperand: true })
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {written} TO {senderText}: data-name-5 shall be a message-tag data item — the sender of a "
                    + "message-tag SET is NULL or another message-tag item, never a literal or an arithmetic expression "
                    + "(ISO §2023:14.9.39.2 Format 17, §2023:14.9.39.3 SR35)");
            var sp = CarrierSenderPlace(sender, "message-tag", "ISO §2023:14.9.39 Format 17, §2023:14.9.39.3 SR35", out var senderRefusal);
            if (senderRefusal is not null) return senderRefusal;
            if (sp?.Item.Pic?.Category is not PicCategory.MessageTag)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {written} TO '{sender.Text}': data-name-5 shall be a message-tag data item or NULL "
                    + "(ISO §2023:14.9.39.3 SR35 — \"Data-name-4 and data-name-5 shall be message-tag data items\")");
            source = sp;
        }
        return new BoundSetMessageTag(target, source, toNull);
    }

    /// <summary>SET program-pointer assignment (ISO §2023:14.9.39 Format 9; SR21 — every target AND the sender
    /// shall be category program-pointer; SR22 — a RESTRICTED receiver's prototype and the sender's shall have the same
    /// signature): <see cref="BindSetPrototypedPointer"/> over the ProgramPointer carrier. The sender is NULL or another
    /// program-pointer; SELF/SUPER are object references (0869).</summary>
    private BoundStatement BindSetProgramPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, SetSender? sender,
        bool toNull, bool senderIsSelfSuper, string? senderText = null) =>
        BindSetPrototypedPointer(PrototypedPointerFormat.ProgramPointer, targetRefs, sender, toNull, senderIsSelfSuper, senderText);

    /// <summary>SET function-pointer assignment (ISO §2023:14.9.39.2 Format 8; §2023:14.9.39.3 SR20 — every target AND the
    /// sender shall be category function-pointer, the sender may be the predefined address NULL, and "the
    /// function-prototypes associated with identifier-12 and identifier-13 shall have the same signature"):
    /// <see cref="BindSetPrototypedPointer"/> over the FunctionPointer carrier. kb/Work PB452 + PB817.</summary>
    private BoundStatement BindSetFunctionPointer(
        IReadOnlyList<Core.DataReferenceContext> targetRefs, SetSender? sender,
        bool toNull, bool senderIsSelfSuper, string? senderText = null) =>
        BindSetPrototypedPointer(PrototypedPointerFormat.FunctionPointer, targetRefs, sender, toNull, senderIsSelfSuper, senderText);

    /// <summary>⛔ THE ONE BINDING OF A SET OF A POINTER THAT A PROTOTYPE MAY RESTRICT — Format 9 (program-pointer, SR21/SR22) and
    /// Format 8 (function-pointer, SR20) of ISO §2023:14.9.39 are one statement over two carriers (kb/Work PB2155, census R0-0053;
    /// kb/Work PB817: "SR20 and SR22 are ONE rule over TWO carriers; write it once"). Every receiver and the sender shall be of
    /// <paramref name="format"/>'s category; the sender is NULL or an identifier of that category, never SELF/SUPER (Format 5's)
    /// nor a literal or an arithmetic expression (kb/Work PB456); and each receiver and the sender are held to the same-signature
    /// rule by <see cref="PointerAssignmentReason"/>, which knows the one asymmetry (SR22 binds only a RESTRICTED program-pointer
    /// receiver, SR20 every function-pointer). <paramref name="format"/> carries what the two formats spell differently.</summary>
    private BoundStatement BindSetPrototypedPointer(PrototypedPointerFormat format,
        IReadOnlyList<Core.DataReferenceContext> targetRefs, SetSender? sender,
        bool toNull, bool senderIsSelfSuper, string? senderText)
    {
        if (senderIsSelfSuper)
        {
            using var atWord = ctx.Edition.At(sender?.Predefined);   // the refusal is about the SELF / SUPER word (kb/Work PB4652)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText ?? "SELF/SUPER"}: SELF and SUPER "
                + $"are object references, not {format.Plural} "
                + $"(ISO §2023:14.9.39.2 {format.Format} — the sender of a {format.Kind} SET is NULL, another "
                + $"{format.Kind}, or {format.AddressSender}; SELF and SUPER belong to Format 5, "
                + "the object-reference assignment)");
        }
        string rules = $"ISO §2023:14.9.39 {format.Format}, §2023:14.9.39.3 {format.Rule}";
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (SetIndexNameOperand(t, "receiving operand", format.Kind, rules)) return BoundRejected.Reported(ctx.Edition);
            if (host.Expr.ResolveReceiving(t) is not { } tp || tp.Item.Pic?.Category != format.Carrier)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET '{WrittenSource.Of(t)}': the receiving operand of a {format.Kind} SET shall be USAGE "
                    + $"{UsageFamilies.UsageWord(format.Usage)} (ISO §2023:14.9.39.3 {format.ReceiverRule})");
            }
            targets.Add(tp);
        }
        Place? source = null;
        if (!toNull)
        {
            if (sender is not { IsIdentifierOperand: true })   // SR20 / SR21 over a literal / expression sender (kb/Work PB456)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: "
                    + $"{format.SenderIdentifier} shall be of category {format.Kind} — a {format.Kind} sender is NULL, "
                    + $"another USAGE {UsageFamilies.UsageWord(format.Usage)} item, a function-identifier or inline method invocation of "
                    + $"category {format.Kind}, or {format.AddressSender}, "
                    + $"never a literal or an arithmetic expression (ISO §2023:14.9.39.2 {format.Format}, §2023:14.9.39.3 {format.Rule})");
            }
            var sp = CarrierSenderPlace(sender, format.Kind, rules, out var senderRefusal);
            if (senderRefusal is not null) return senderRefusal;
            if (sp?.Item.Pic?.Category != format.Carrier)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PointerOperandShape,
                    // ⛔ NAME THE RECEIVERS (kb/Work PB388's sweep): the renderer transliterates U+2026 to ASCII, so
                    // `SET … TO 'x'` reached the user as `SET . TO 'x'` — a statement nobody wrote.
                    $"SET {SetFormatSelection.Written(targetRefs)} TO "
                    + $"'{sender.Text}': a {format.Kind} sender shall be NULL, another "
                    + $"USAGE {UsageFamilies.UsageWord(format.Usage)} item, a function-identifier or inline method invocation of category "
                    + $"{format.Kind}, or {format.AddressSender} "
                    + $"(ISO §2023:14.9.39.3 {format.ReceiverRule} / {format.AddressIdentifierClause})");
            }
            source = sp;
            // SR20's last sentence and SR22, over every receiver: the associated prototypes shall have the same signature.
            // §2023:13.18.60.4 GR25/GR26 make "associated" a compile-time fact — every restricted pointer carries the prototype
            // its unbracketed TO phrase names.
            foreach (var t in targets)
                if (PointerAssignmentReason(t.Item, sp.Item) is { } why)
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.PrototypePointerSignature,
                        $"SET '{t.Item.CobolName}' TO '{sp.Item.CobolName}': {why}");
        }
        return format.Bind(targets, source, toNull);
    }

    /// <summary>What SET Format 9 and Format 8 (<see cref="BindSetPrototypedPointer"/>) spell differently: the carrier's
    /// category, its word and its usage, the format and its rule, the sender identifier's name, the address-identifier sender
    /// (§2023:8.4.3.13 program-address-identifier, §2023:8.4.3.12 function-address-identifier) and the bound statement.</summary>
    private sealed record PrototypedPointerFormat(
        PicCategory Carrier, string Kind, Usage Usage, string Format, string Rule, string ReceiverRule,
        string SenderIdentifier, string AddressSender, string AddressIdentifierClause,
        Func<IReadOnlyList<Place>, Place?, bool, BoundStatement> Bind)
    {
        public static readonly PrototypedPointerFormat ProgramPointer = new(PicCategory.ProgramPointer,
            "program-pointer", Usage.ProgramPointer, "Format 9", "SR21", "SR21, Format 9", "identifier-8",
            "an ENTRY program-address-identifier", "§2023:8.4.3.13",
            static (targets, source, toNull) => new BoundSetProgramPointer(targets, source, toNull));

        public static readonly PrototypedPointerFormat FunctionPointer = new(PicCategory.FunctionPointer,
            "function-pointer", Usage.FunctionPointer, "Format 8", "SR20", "SR20", "identifier-13",
            "an ADDRESS OF FUNCTION function-address-identifier", "§2023:8.4.3.12",
            static (targets, source, toNull) => new BoundSetFunctionPointer(targets, source, toNull));

        /// <summary>The carrier named in prose as a plural noun ("program pointers").</summary>
        public string Plural => Kind.Replace('-', ' ') + "s";
    }

    /// <summary>⛔ THE ONE §2023:14.9.39.3 VERDICT FOR A PROGRAM- OR FUNCTION-POINTER IDENTIFIER STORED INTO A POINTER
    /// RECEIVER — the SET of one (receiver, sender) pair whose two items are already of one pointer category. Asked by
    /// SET Format 8 and Format 9 themselves and by <see cref="ScreenImplicitSet"/> (INITIALIZE … REPLACING); the
    /// BY CONTENT / BY VALUE argument lane, which §2023:14.8.2.3.3 makes "the same as if a SET statement were performed in
    /// the activating runtime element with the argument as the sending operand and the corresponding formal parameter
    /// as the receiving operand", asks the same relations of the run-time description
    /// (<c>ActivationRelations.MaySet</c>; kb/Work PB1063), not this member. Null when the SET is valid.
    /// <list type="bullet">
    /// <item>program-pointer — SR22: a RESTRICTED receiver needs a sender "restricted" to a program-prototype of the
    /// same signature (an unrestricted sender is associated with none); an unrestricted receiver takes any
    /// program-pointer (SR21);</item>
    /// <item>function-pointer — SR20: "The function-prototypes associated with identifier-12 and identifier-13 shall
    /// have the same signature".</item>
    /// </list>
    /// ⛔ THE DATA-POINTER ARM IS NOT HERE: SR19's three sentences are one rule over the ONE restriction model
    /// (<see cref="StrongTypeModel.PointerRestriction"/>), written once as
    /// <c>PtrBinder.ScreenPointerReceiverRestriction</c>, which the Format-7 routes and
    /// <see cref="ScreenImplicitSet"/> all ask (kb/Work PB3966: a second copy of it lived here, reachable only by
    /// the order of <see cref="ScreenImplicitSet"/>'s own guards).</summary>
    internal string? PointerAssignmentReason(DataItem receiver, DataItem sender)
    {
        switch (receiver.Pic?.Category)
        {
            case PicCategory.ProgramPointer:
                string? targetProto = receiver.Pic.RestrictedPrototypeName;
                if (targetProto is null) return null;   // an UNRESTRICTED receiver — SR22's condition is not met
                // An UNRESTRICTED sender is a violation in its own right, not a vacuous pass: SR22 requires the
                // prototypes "associated with identifier-7 and identifier-8" to have the same signature, and an
                // unrestricted program-pointer is associated with none. (§2023:14.8.2.3.2 says the same thing in the
                // argument-passing direction and says it explicitly — "if either is a restricted pointer, both
                // shall be restricted and of the same type".) A prototype name the unit's tables do not hold was
                // refused where it was declared, and raises no second fault here (StatementBinder.SignaturesDiffer).
                string? senderProto = sender.Pic?.RestrictedPrototypeName;
                return senderProto is null
                       || StatementBinder.SignaturesDiffer(ctx.Symbols.ProgramSignatureOf(receiver), ctx.Symbols.ProgramSignatureOf(sender))
                    ? $"the receiving program-pointer is restricted to program-prototype '{targetProto}' and the sender "
                      + $"{(senderProto is null ? "is unrestricted" : $"to '{senderProto}'")}, so the associated "
                      + "program-prototypes do not have the same signature (ISO §2023:14.9.39.3 SR22; §2023:13.18.60.4 GR25)"
                    : null;
            case PicCategory.FunctionPointer:
                string? rp = receiver.Pic.RestrictedPrototypeName, sp = sender.Pic?.RestrictedPrototypeName;
                return !StatementBinder.SignaturesDiffer(ctx.Symbols.FunctionSignatureOf(receiver), ctx.Symbols.FunctionSignatureOf(sender)) ? null
                    : $"the receiving function-pointer is restricted to function-prototype '{rp}' and the sender to "
                      + $"'{sp}', which do not have the same signature — the function-prototypes associated with "
                      + "identifier-12 and identifier-13 shall have the same signature (ISO §2023:14.9.39.3 SR20; §2023:13.18.60.4 GR26)";
            default:
                return null;
        }
    }

    /// <summary>⛔ THE §2023:14.9.39.3 VERDICT FOR A STORE THAT IS AN IMPLICIT SET, NOT A WRITTEN ONE — INITIALIZE …
    /// REPLACING <c>{DATA|PROGRAM|FUNCTION}-POINTER | OBJECT-REFERENCE</c> DATA BY identifier-2 (kb/Work PB3817).
    /// §2023:14.9.20.4 GR4 makes the store, per receiving operand, "SET receiving-operand TO sending-operand", so the
    /// receiver's own restriction is asked at the receiver: §2023:13.18.60.4 GR23/GR25/GR26 say a restricted pointer "shall
    /// contain only the predefined address NULL or the address of" its type, and SR19/SR20/SR22 are where a SET is held
    /// to it; the object-reference receiver's class description is held by SR10/SR12 (<c>OoConformance</c>).
    /// The phrase-level category check (SR4) cannot ask it, because the receivers of
    /// <c>INITIALIZE g REPLACING PROGRAM-POINTER</c> are the group's program-pointers, each restricted (or not) on its own.
    /// The data-pointer arm is <see cref="PtrBinder.ScreenPointerReceiverRestriction"/>, the Format-7 route's own; the
    /// program-pointer and function-pointer arms are <see cref="PointerAssignmentReason"/>, the Format-8/9 routes'; the
    /// object-reference arm is <c>OoBinder.ScreenObjectRefAssignment</c>, the Format-5 route's.
    /// ⛔ ALL FOUR ARMS NAME THE STATEMENT AND THE RECEIVER the same way (<paramref name="statement"/>, then
    /// "the implicit SET 'receiver' TO 'sender' (ISO §2023:14.9.20.4 GR4)"), because the receiver is a SUBORDINATE of the
    /// INITIALIZE operand the user wrote and is otherwise unfindable (kb/Work PB3966).
    /// A NULL sender never reaches here (it is a <c>BoundPredefinedNull</c>, not a place). True when the store is valid;
    /// otherwise the error is reported and false returned.</summary>
    internal bool ScreenImplicitSet(Place receiver, Place sender, string statement)
    {
        string recvName = receiver.Item.CobolName ?? "FILLER";
        string sendName = sender.Item.CobolName ?? "FILLER";
        string implicitSet = $"{statement} — the implicit SET '{recvName}' TO '{sendName}' (ISO §2023:14.9.20.4 GR4)";
        if (receiver.Item.Pic?.Category is PicCategory.Pointer)
            return host.Ptr.ScreenPointerReceiverRestriction(receiver, recvName,
                StrongTypeModel.PointerRestriction(sender.Item), sendName, addressSender: false, implicitSet);
        if (receiver.Item.Pic is { Category: PicCategory.ObjectReference } rpic
            && sender.Item.Pic is { Category: PicCategory.ObjectReference } spic)
            return host.Oo.ScreenObjectRefAssignment(spic, rpic, implicitSet);
        if (PointerAssignmentReason(receiver.Item, sender.Item) is not { } why) return true;
        ctx.Edition.Error(DiagnosticCatalog.PrototypePointerSignature, $"{implicitSet}: {why}");
        return false;
    }
}
