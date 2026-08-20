// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

// The entire STRING/UNSTRING surface bound here — including NOT ON OVERFLOW and END-STRING/END-UNSTRING — is
// COBOL-85 (both verbs' phrases were complete by 1985); no edition gate applies. The post-85 deltas (class
// national / boolean operands, zero-length-item rules, dynamic-length SIZE — §14.9.43.4 GR1) concern data shapes
// the current data model cannot describe, and the EC-OVERFLOW-STRING / EC-OVERFLOW-UNSTRING names (2002+, GR8b /
// GR16b) await the EC model; the ON/NOT ON OVERFLOW control flow itself is edition-invariant.

/// <summary>The STRING/UNSTRING verb binder (P7 Step 10d — a real collaborator over
/// <see cref="BinderContext"/>, extracted from the <c>StatementBinder.StringUnstring</c> partial; the
/// operand spine is reached through transitional host edges until 10q). The SR rejections here are
/// CONTROL FLOW (each aborts the statement with a placeholder), so none lift to StatementValidation.
/// The five bound records stayed in <c>Binding/Bound/BoundStringUnstring.cs</c>.</summary>
internal sealed class StringUnstringBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind STRING (ISO §14.9.43). Each <c>stringSendingPhrase</c> carries ONE sender and optionally the
    /// DELIMITED phrase that closes its run; the phrase governs every phraseless sender back to the previous
    /// phrase, so the binder back-propagates each phrase to its run, and a trailing phraseless run — legal only
    /// immediately preceding INTO — is DELIMITED BY SIZE (SR9). The receiver is screened per SR4 (not
    /// reference-modified) and SR5 (not edited, no JUSTIFIED — JUSTIFIED is not yet modeled, so only the edited
    /// arm is checkable); the pointer per SR7 (elementary integer, no P — a non-zero scale covers both V and P).</summary>
    public BoundStatement BindString(Core.StringStatementContext st)
    {
        var phrases = st.stringSendingPhrase();
        int n = phrases.Length;
        var values = new BoundOperand[n];
        var delims = new BoundOperand?[n];
        var bySize = new bool[n];
        var hasPhrase = new bool[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = StrUnstrOperand(phrases[i].strUnstrOperand(), "STRING sending operand");
            if (values[i] is BoundAllLiteral { BeginsWithAll: true })   // SR2 — literal-1 shall not be a figurative beginning with the word ALL (a bare symbolic character is not — PB110)
                values[i] = new BoundOperandError("STRING sending ALL literal (ISO §14.9.43.3 SR2)");
            if (phrases[i].delimitedByPhrase() is not { } dp) continue;
            hasPhrase[i] = true;
            if (dp.SIZE() is not null) { bySize[i] = true; continue; }
            var d = StrUnstrOperand(dp.strUnstrOperand(), "STRING delimiter");
            // SR2 — literal-2 shall not be an ALL figurative; the grammar's (ALL)? token is not in the ISO format.
            if (dp.ALL() is not null || d is BoundAllLiteral { BeginsWithAll: true })
                d = new BoundOperandError("STRING DELIMITED BY ALL literal (ISO §14.9.43.3 SR2)");
            delims[i] = d;
        }
        // Back-propagate each DELIMITED phrase over its preceding phraseless run (the general format attaches the
        // phrase AFTER the run); senders left with no phrase form the trailing run ⇒ SIZE (SR9).
        for (int i = n - 2; i >= 0; i--)
            if (!hasPhrase[i]) { delims[i] = delims[i + 1]; bySize[i] = bySize[i + 1]; hasPhrase[i] = hasPhrase[i + 1]; }
        var sendings = new List<BoundStringSending>(n);
        for (int i = 0; i < n; i++)
            sendings.Add(new BoundStringSending(values[i], delims[i], bySize[i] || delims[i] is null));

        if (ctx.Refs.Resolve(st.stringIntoPhrase().dataReference()) is not { } into)
            return new BoundUnsupported($"STRING INTO '{st.stringIntoPhrase().dataReference().GetText()}'");
        string intoText = st.stringIntoPhrase().dataReference().GetText();
        // §14.9.43.3 SR4–SR6, SR11 — bind-time rejections (kb/Work PB88: each was a run-time loud stage on ILLEGAL
        // source, the wrong-stage family; the statement compiled clean and died when control reached it).
        if (into is RefModPlace)
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not be reference-modified (ISO §14.9.43.3 SR4)");
        if (into.Item.Pic is { Category: PicCategory.NumericEdited }
            or { Category: PicCategory.Alphanumeric, EditMask: not null })
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not reference an edited data item (ISO §14.9.43.3 SR5)");
        if (into.Item.Justified)
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not be described with the JUSTIFIED clause (ISO §14.9.43.3 SR5)");
        if (StrongTypeModel.IsStrongGroup(into.Item))
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not reference a strongly-typed group item (ISO §14.9.43.3 SR6)");
        if (into.Item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(into.Item))
            return Reject($"STRING INTO '{intoText}': identifier-3 shall not specify a variable-length group (ISO §14.9.43.3 SR11; §8.5.1.12)");
        // ⛔ DA7 — SR1 at BIND time. This check previously existed ONLY in StringEmitter as a run-time loud stage,
        // so `STRING … INTO <a COMP item>` compiled clean and crashed at the statement. SR1: "all identifiers,
        // except identifier-4, shall be described implicitly or explicitly as usage display or national" —
        // identifier-4 is the POINTER, so the INTO receiver is covered. A GROUP receiver is EXEMPT: usage is an
        // elementary property, and §14.9.43.4 GR3a defines the transfer into identifier-3 "in accordance with the
        // MOVE statement rules for alphanumeric-to-alphanumeric moves", which admit a group — including one holding
        // a BINARY/PACKED leaf (V59). Edition-invariant: SR1 is unchanged at 85/2002/2014/2023.
        if (!into.Item.IsGroup && into is not RefModPlace && !into.Item.StoreAsImage
            && into.Item.Pic is { } ip && ip.Usage is not (Usage.Display or Usage.National))
        {
            ctx.Edition.Error(DiagnosticCatalog.CharacterOperandUsage,
                $"STRING INTO '{st.stringIntoPhrase().dataReference().GetText()}' is an elementary item of USAGE "
                + $"{ip.Usage}, which has no character image; SR1 requires every identifier except the POINTER to be "
                + "usage display or national (ISO §14.9.43.3 SR1)");
            return new BoundUnsupported(
                $"STRING INTO receiver of USAGE {ip.Usage} (ISO §14.9.43.3 SR1 — usage display or national only)");
        }

        Place? pointer = null;
        if (st.stringWithPointer()?.dataReference() is { } pd)
        {
            if (ctx.Refs.Resolve(pd) is not { } pp)
                return new BoundUnsupported($"STRING WITH POINTER '{pd.GetText()}'");   // undefined — the resolver reported it
            if (!StrUnstrIsInteger(pp))
                return Reject($"STRING WITH POINTER '{pd.GetText()}': identifier-4 shall be an elementary numeric integer "
                    + "data item without the symbol P (ISO §14.9.43.3 SR7)");
            pointer = pp;
        }

        List<BoundStatement>? onOvf = null, notOvf = null;
        if (st.stringOnOverflow() is { } ov)
            (onOvf, notOvf) = PhraseBlocks.Split(ov.statementBlock(), PhraseBlocks.StartsWithNot(ov), b => host.BindBlocks([b]));
        return new BoundStringStmt(sendings, into, pointer, onOvf, notOvf);
    }

    /// <summary>Bind UNSTRING (ISO §14.9.48). The sender must be category alphanumeric or national (SR2 — a
    /// numeric [INCLUDING usage DISPLAY], numeric-edited, or boolean sender is rejected); each INTO target's data references arrive in
    /// order [receiver, DELIMITER IN?, COUNT IN?] keyed off the DELIMITER/COUNT tokens, both legal only under a
    /// DELIMITED phrase (SR7); COUNT IN / TALLYING are integer items (SR5) and the pointer per SR6. The grammar's
    /// ambiguous <c>DELIMITED ALL "0"</c> may parse as the figurative ALL-literal — SR1 forbids that figurative
    /// here, so the binder reads it as the UNSTRING ALL phrase over the plain literal.</summary>
    public BoundStatement BindUnstring(Core.UnstringStatementContext un)
    {
        // DA4: the sender is an identifier, so it may be a function-identifier (§14.9.48.2 + §8.4.3.1.2 Format 1).
        string senderText = un.strUnstrSender().GetText();
        var source = StrUnstrSender(un.strUnstrSender(), $"UNSTRING source '{senderText}'");
        if (source is BoundOperandError) return new BoundUnsupported($"UNSTRING source '{senderText}'");
        // SR2 — identifier-1 (the sender) shall be category alphanumeric or national (a fixed-length group and a
        // reference-modified slice are alphanumeric-image senders and remain permitted). A numeric item — INCLUDING
        // usage DISPLAY, whose zoned image would otherwise be examined as characters — a numeric-edited item, or a
        // boolean item is not a permitted sender. The screen reads the CATEGORY off whichever shape arrived: a
        // field's PICTURE, or an intrinsic's §15.2 result category.
        if (UnstringSenderCategory(source) is { } badCat)
            return Reject($"UNSTRING sender '{senderText}' is category {badCat} "
                + "(identifier-1 shall reference a data item of category alphanumeric or national, ISO §14.9.48.3 SR2)");
        if (source is BoundFieldOperand { Place.Item: { IsGroup: true } sg } && ReferenceResolver.HasVariableLengthSubordinate(sg))
            return Reject($"UNSTRING sender '{senderText}' shall not reference a variable-length group (ISO §14.9.48.3 SR10; §8.5.1.12)");

        var delims = new List<BoundUnstringDelimiter>();
        if (un.unstringDelimiterPhrase() is { } dp)
            foreach (var item in dp.unstringDelimiterItem())
            {
                bool all = item.ALL() is not null;
                var v = StrUnstrOperand(item.strUnstrOperand(), "UNSTRING delimiter");
                if (v is BoundAllLiteral allLit) { v = new BoundStringLiteral(allLit.Literal) { Category = allLit.Category }; all = true; }
                delims.Add(new BoundUnstringDelimiter(v, all));
            }

        var receivers = new List<BoundUnstringReceiver>();
        foreach (var ip in un.unstringIntoPhrase())
            foreach (var t in ip.unstringIntoTarget())
            {
                var drefs = t.dataReference();
                if (ctx.Refs.Resolve(drefs[0]) is not { } target)
                    return new BoundUnsupported($"UNSTRING INTO '{drefs[0].GetText()}'");
                // SR4 — identifier-4 shall be (usage display + category alphabetic/alphanumeric/numeric) or (usage
                // national + category national/numeric). A fixed-length group (SR10) and a reference-modified slice
                // are alphanumeric-image receivers and are exempt; edited, COMP/packed/COMP-5, index, and float
                // receivers are not permitted.
                // ⛔ DA7: a COMPILE-TIME diagnostic. The verdict was already correct and already computed HERE,
                // but returning only BoundUnsupported let the illegal program compile clean and throw when control
                // reached the UNSTRING. Groups stay exempt — §14.9.43.4 GR3a's alphanumeric-MOVE semantics carry a
                // group receiver, including one holding a BINARY/PACKED leaf (V59).
                if (target is not RefModPlace && !target.Item.IsGroup && !UnstringReceiverAllowed(target.Item.Pic))
                {
                    ctx.Edition.Error(DiagnosticCatalog.CharacterOperandUsage,
                        $"UNSTRING INTO '{drefs[0].GetText()}' requires a usage-display alphabetic/alphanumeric/"
                        + "numeric or usage-national national/numeric receiver; edited, COMP, packed, index and "
                        + "float receivers have no character image (ISO §14.9.48.3 SR4)");
                    return new BoundUnsupported(
                        $"UNSTRING INTO '{drefs[0].GetText()}' — a usage-display alphabetic/alphanumeric/numeric or " +
                        "usage-national national/numeric receiver is required; edited, COMP, packed, index, and float " +
                        "receivers are not permitted (ISO §14.9.48.3 SR4)");
                }
                bool hasDelim = t.DELIMITER() is not null, hasCount = t.COUNT() is not null;
                if ((hasDelim || hasCount) && un.unstringDelimiterPhrase() is null)
                    return Reject("UNSTRING DELIMITER IN / COUNT IN: the DELIMITED BY phrase shall be specified when either "
                        + "is written (ISO §14.9.48.3 SR7)");
                if (target.Item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(target.Item))
                    return Reject($"UNSTRING INTO '{drefs[0].GetText()}' shall not reference a variable-length group (ISO §14.9.48.3 SR10; §8.5.1.12)");
                int next = 1;
                Place? delimIn = null, countIn = null;
                if (hasDelim)
                {
                    if (ctx.Refs.Resolve(drefs[next]) is not { } d5)
                        return new BoundUnsupported($"UNSTRING DELIMITER IN '{drefs[next].GetText()}'");
                    delimIn = d5;
                    next++;
                }
                if (hasCount)
                {
                    if (ctx.Refs.Resolve(drefs[next]) is not { } c6)
                        return new BoundUnsupported($"UNSTRING COUNT IN '{drefs[next].GetText()}'");   // undefined — reported by the resolver
                    if (!StrUnstrIsInteger(c6))
                        return Reject($"UNSTRING COUNT IN '{drefs[next].GetText()}': identifier-6 shall reference an integer "
                            + "data item without the symbol P (ISO §14.9.48.3 SR5)");
                    countIn = c6;
                }
                receivers.Add(new BoundUnstringReceiver(target, delimIn, countIn, StrUnstrReceiveSize(target)));
            }
        if (delims.Count == 0 && receivers.Any(r => r.NoDelimSize < 0))
            return new BoundUnsupported(
                "UNSTRING without DELIMITED BY into a reference-modified receiver (no static reception size, ISO §14.9.48.4 GR11b)");

        Place? pointer = null;
        if (un.unstringWithPointer()?.dataReference() is { } pd)
        {
            if (ctx.Refs.Resolve(pd) is not { } pp)
                return new BoundUnsupported($"UNSTRING WITH POINTER '{pd.GetText()}'");   // undefined — reported by the resolver
            if (!StrUnstrIsInteger(pp))
                return Reject($"UNSTRING WITH POINTER '{pd.GetText()}': identifier-7 shall be an elementary numeric integer "
                    + "data item without the symbol P (ISO §14.9.48.3 SR6)");
            pointer = pp;
        }
        Place? tallying = null;
        if (un.unstringTallying()?.dataReference() is { } td)
        {
            if (ctx.Refs.Resolve(td) is not { } tp)
                return new BoundUnsupported($"UNSTRING TALLYING IN '{td.GetText()}'");   // undefined — reported by the resolver
            if (!StrUnstrIsInteger(tp))
                return Reject($"UNSTRING TALLYING IN '{td.GetText()}': identifier-8 shall reference an integer data item "
                    + "without the symbol P (ISO §14.9.48.3 SR5)");
            tallying = tp;
        }

        List<BoundStatement>? onOvf = null, notOvf = null;
        if (un.unstringOnOverflow() is { } ov)
            (onOvf, notOvf) = PhraseBlocks.Split(ov.statementBlock(), PhraseBlocks.StartsWithNot(ov), b => host.BindBlocks([b]));
        return new BoundUnstringStmt(source, delims, receivers, pointer, tallying, onOvf, notOvf);
    }

    /// <summary>Bind a STRING/UNSTRING SENDING operand position (exactly one of a function-identifier, a data
    /// reference, a literal, or a figurative constant per the grammar). A figurative word is the implicit
    /// ONE-character item (STRING GR2 / UNSTRING GR7); the callers screen the ALL-literal figurative per their SRs.
    /// <para>
    /// ⛔ DA4 — the function-identifier arm. §14.9.43.2 and §14.9.48.2 write these operands as identifier-N, and
    /// §8.4.3.1.2 Format 1 makes <c>function-identifier-1</c> a FORMAT of an identifier, so a function is
    /// admissible in every one of them; §8.4.3.2.3 SR1 excludes it only from a RECEIVING operand, which is why the
    /// INTO phrases do not route through here. Before this, <c>STRING FUNCTION ORD(C) DELIMITED BY SIZE INTO A</c>
    /// did not even PARSE ("no viable alternative at input 'FUNCTION'"), so DA2's string-context renderer could
    /// never be reached — the rejection happened a whole stage earlier.
    /// </para>
    /// <para>ONE helper serves all four sending positions (STRING identifier-1 and identifier-2, UNSTRING
    /// identifier-1 and the DELIMITED BY … OR … items), so this arm is written once rather than four times. The
    /// grammar names the two shapes — <c>strUnstrOperand</c> (identifier or literal) and its strict subset
    /// <c>strUnstrSender</c> (identifier only) — so this takes ONE context parameter rather than a row of
    /// mutually-exclusive nullable ones that a caller could transpose.</para>
    /// </summary>
    private BoundOperand StrUnstrOperand(Core.StrUnstrOperandContext? op, string role)
        => op is null ? new BoundOperandError(role)
        : op.strUnstrSender() is { } snd ? StrUnstrSender(snd, role)
        : op.literal() is { } lit ? host.Expr.LiteralOperand(lit)
        : op.figurativeConstant() is { } fig ? host.Expr.FigurativeOperand(fig)
        : new BoundOperandError(role);

    /// <summary>Bind the narrower SENDER shape — an identifier only (a function-identifier or a data reference),
    /// no literal. This is what §14.9.48.2's `UNSTRING identifier-1` admits, and
    /// <see cref="StrUnstrOperand"/> delegates its two identifier arms here so the shapes cannot drift apart.</summary>
    private BoundOperand StrUnstrSender(Core.StrUnstrSenderContext? snd, string role)
        => snd?.functionCall() is { } fn ? IntrinsicBinder.OperandOf(host.Intrinsic.BindIntrinsic(fn))
        : snd?.dataReference() is { } dref ? ScreenedField(dref, role)
        : new BoundOperandError(role);

    /// <summary>A sending data reference, screened for an INDEX-NAME (kb/Work R16): STRING/UNSTRING are none
    /// of §13.18.38.3 r7's five index-name contexts, and this funnel serves all four sending positions — the
    /// one place, so the diagnostic cannot cover three slots and miss the fourth. Before, the reference
    /// compiled clean and aborted at run time in the string channel.</summary>
    private BoundOperand ScreenedField(Core.DataReferenceContext dref, string role)
    {
        var op = host.Expr.FieldOperand(dref);
        return host.Expr.ScreenIndexNameOperand(op, dref.GetText(), role)
            ? new BoundOperandError($"{role}: the index-name '{dref.GetText()}' (ISO §13.18.38.3 r7)")
            : op;
    }

    /// <summary>ISO §14.9.48.3 SR2 — the OFFENDING category of an UNSTRING sender, or <see langword="null"/> when
    /// it is a permitted one. Reads the category off whichever operand shape the sender is: a FIELD's PICTURE
    /// (a group has none and is an alphanumeric-image sender, so it passes), or a numeric/boolean intrinsic's
    /// §15.2 RESULT category. A string-class function (UPPER-CASE, TRIM, SUBSTITUTE …) is a permitted sender;
    /// FUNCTION ORD or a boolean function is not, for exactly the reason a numeric ITEM is not.</summary>
    private static PicCategory? UnstringSenderCategory(BoundOperand source) => source switch
    {
        BoundFieldOperand f when f.Place.Item.Pic is
            { Category: PicCategory.Numeric or PicCategory.NumericEdited or PicCategory.Boolean } pic => pic.Category,
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } when ic.ResultCategory is
            PicCategory.Numeric or PicCategory.NumericEdited or PicCategory.Boolean => ic.ResultCategory,
        _ => null,
    };

    /// <summary>True for an elementary fixed-point INTEGER item with no P scaling — the shape STRING SR7 /
    /// UNSTRING SR5–SR6 require of the POINTER / COUNT IN / TALLYING items (a V or P picture yields a non-zero
    /// scale; P is the signed-scale encoding, §13.18.40).</summary>
    /// <summary>A STRING / UNSTRING syntax-rule violation: reported at BIND (COBOLNET1651 — the compile fails) and
    /// staged loud as the backstop the caller returns (kb/Work PB88: the stage alone let illegal source compile
    /// clean and die at run time). ONE helper, so no rule site can forget the diagnostic half again.</summary>
    private BoundStatement Reject(string message)
    {
        ctx.Edition.Error(DiagnosticCatalog.StringUnstringOperandRule, message);
        return new BoundUnsupported(message);
    }

    private static bool StrUnstrIsInteger(Place p) =>
        p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Scale: 0 };

    /// <summary>ISO §14.9.48.3 SR4 — the permitted UNSTRING INTO (identifier-4) receiver categories: usage display
    /// with category alphabetic/alphanumeric/numeric (alphabetic folds into <see cref="PicCategory.Alphanumeric"/>;
    /// <c>EditMask: null</c> excludes alphanumeric-edited), OR usage national with category national/numeric. A
    /// numeric receiver must be non-float and usage display or national — so a numeric-edited, COMP/packed/COMP-5,
    /// index, boolean, or float receiver is rejected. National is SR4-LEGAL and stays allowed (the emitter defers it
    /// separately). Callers exempt a group (SR10) and a reference-modified slice before consulting this.</summary>
    private static bool UnstringReceiverAllowed(PicInfo? pic) => pic is
        { Category: PicCategory.Alphanumeric, EditMask: null }
        or { Category: PicCategory.National }
        or { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display or Usage.National };

    /// <summary>The UNSTRING no-DELIMITED reception size (ISO §14.9.48.4 GR11b): the receiving area's size in
    /// character positions — a group's image width, an alphanumeric/edited item's character length, and a numeric
    /// item's DIGIT positions (GR11b excludes a separate sign position; an over-punched sign occupies none). A
    /// reference-modified receiver has no static size (−1; rejected by the caller when it would be needed).</summary>
    private static int StrUnstrReceiveSize(Place p) =>
        p is RefModPlace ? -1
        : p.Item.IsGroup ? p.Item.AsIfPic?.Length ?? p.Item.ImageWidth   // a bit / national group: its as-if positions (D20/PB79)
        : p.Item.Pic is { Category: PicCategory.Numeric } pic ? pic.Digits
        : p.Item.Pic?.Length ?? 0;
}
