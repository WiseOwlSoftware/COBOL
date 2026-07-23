// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
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
            values[i] = StrUnstrOperand(
                phrases[i].dataReference(), phrases[i].literal(), phrases[i].figurativeConstant(), "STRING sending operand");
            if (values[i] is BoundAllLiteral)   // SR2 — literal-1 shall not be an ALL figurative
                values[i] = new BoundOperandError("STRING sending ALL literal (ISO §14.9.43.3 SR2)");
            if (phrases[i].delimitedByPhrase() is not { } dp) continue;
            hasPhrase[i] = true;
            if (dp.SIZE() is not null) { bySize[i] = true; continue; }
            var d = StrUnstrOperand(dp.dataReference(), dp.literal(), dp.figurativeConstant(), "STRING delimiter");
            // SR2 — literal-2 shall not be an ALL figurative; the grammar's (ALL)? token is not in the ISO format.
            if (dp.ALL() is not null || d is BoundAllLiteral)
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
        if (into is RefModPlace)
            return new BoundUnsupported("STRING INTO a reference-modified receiver (ISO §14.9.43.3 SR4)");
        if (into.Item.Pic is { Category: PicCategory.NumericEdited }
            or { Category: PicCategory.Alphanumeric, EditMask: not null })
            return new BoundUnsupported("STRING INTO an edited receiver (ISO §14.9.43.3 SR5)");

        Place? pointer = null;
        if (st.stringWithPointer()?.dataReference() is { } pd)
        {
            if (ctx.Refs.Resolve(pd) is not { } pp || !StrUnstrIsInteger(pp))
                return new BoundUnsupported(
                    $"STRING WITH POINTER '{pd.GetText()}' (an elementary numeric integer item without P, ISO §14.9.43.3 SR7)");
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
        if (ctx.Refs.Resolve(un.dataReference()) is not { } source)
            return new BoundUnsupported($"UNSTRING source '{un.dataReference().GetText()}'");
        // SR2 — identifier-1 (the sender) shall be category alphanumeric or national (a fixed-length group and a
        // reference-modified slice are alphanumeric-image senders and remain permitted). A numeric item — INCLUDING
        // usage DISPLAY, whose zoned image would otherwise be examined as characters — a numeric-edited item, or a
        // boolean item is not a permitted sender.
        if (source.Item.Pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited or PicCategory.Boolean })
            return new BoundUnsupported(
                $"UNSTRING sender '{un.dataReference().GetText()}' is category {source.Item.Pic.Category} " +
                "(category alphanumeric or national required, ISO §14.9.48.3 SR2)");

        var delims = new List<BoundUnstringDelimiter>();
        if (un.unstringDelimiterPhrase() is { } dp)
            foreach (var item in dp.unstringDelimiterItem())
            {
                bool all = item.ALL() is not null;
                var v = StrUnstrOperand(item.dataReference(), item.literal(), item.figurativeConstant(), "UNSTRING delimiter");
                if (v is BoundAllLiteral allLit) { v = new BoundStringLiteral(allLit.Literal); all = true; }
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
                if (target is not RefModPlace && !target.Item.IsGroup && !UnstringReceiverAllowed(target.Item.Pic))
                    return new BoundUnsupported(
                        $"UNSTRING INTO '{drefs[0].GetText()}' — a usage-display alphabetic/alphanumeric/numeric or " +
                        "usage-national national/numeric receiver is required; edited, COMP, packed, index, and float " +
                        "receivers are not permitted (ISO §14.9.48.3 SR4)");
                bool hasDelim = t.DELIMITER() is not null, hasCount = t.COUNT() is not null;
                if ((hasDelim || hasCount) && un.unstringDelimiterPhrase() is null)
                    return new BoundUnsupported("UNSTRING DELIMITER IN / COUNT IN without DELIMITED BY (ISO §14.9.48.3 SR7)");
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
                    if (ctx.Refs.Resolve(drefs[next]) is not { } c6 || !StrUnstrIsInteger(c6))
                        return new BoundUnsupported(
                            $"UNSTRING COUNT IN '{drefs[next].GetText()}' (an integer item without P, ISO §14.9.48.3 SR5)");
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
            if (ctx.Refs.Resolve(pd) is not { } pp || !StrUnstrIsInteger(pp))
                return new BoundUnsupported(
                    $"UNSTRING WITH POINTER '{pd.GetText()}' (an elementary numeric integer item without P, ISO §14.9.48.3 SR6)");
            pointer = pp;
        }
        Place? tallying = null;
        if (un.unstringTallying()?.dataReference() is { } td)
        {
            if (ctx.Refs.Resolve(td) is not { } tp || !StrUnstrIsInteger(tp))
                return new BoundUnsupported(
                    $"UNSTRING TALLYING IN '{td.GetText()}' (an integer item without P, ISO §14.9.48.3 SR5)");
            tallying = tp;
        }

        List<BoundStatement>? onOvf = null, notOvf = null;
        if (un.unstringOnOverflow() is { } ov)
            (onOvf, notOvf) = PhraseBlocks.Split(ov.statementBlock(), PhraseBlocks.StartsWithNot(ov), b => host.BindBlocks([b]));
        return new BoundUnstringStmt(source, delims, receivers, pointer, tallying, onOvf, notOvf);
    }

    /// <summary>Bind a STRING/UNSTRING operand position (exactly one of a data reference, a literal, or a
    /// figurative constant per the grammar). A figurative word is the implicit ONE-character item (STRING GR2 /
    /// UNSTRING GR7); the callers screen the ALL-literal figurative per their SRs.</summary>
    private BoundOperand StrUnstrOperand(
        Core.DataReferenceContext? dref, Core.LiteralContext? lit, Core.FigurativeConstantContext? fig, string role)
        => dref is not null ? host.Expr.FieldOperand(dref)
        : lit is not null ? host.Expr.LiteralOperand(lit)
        : fig is not null ? ExpressionBinder.FigurativeOperand(fig)
        : new BoundOperandError(role);

    /// <summary>True for an elementary fixed-point INTEGER item with no P scaling — the shape STRING SR7 /
    /// UNSTRING SR5–SR6 require of the POINTER / COUNT IN / TALLYING items (a V or P picture yields a non-zero
    /// scale; P is the signed-scale encoding, §13.18.40).</summary>
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
        : p.Item.IsGroup ? p.Item.ImageWidth
        : p.Item.Pic is { Category: PicCategory.Numeric } pic ? pic.Digits
        : p.Item.Pic?.Length ?? 0;
}
