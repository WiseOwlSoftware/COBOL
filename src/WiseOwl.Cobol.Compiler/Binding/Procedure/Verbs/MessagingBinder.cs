// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE STATEMENTS OF THE ASYNCHRONOUS MESSAGING FACILITY (ISO §2023:14.9.38 SEND, §2023:14.9.31 RECEIVE; docs/rearchitecture/DESIGN-message-control.md §4.3;
/// kb/Work PB4743): one node per statement format (<see cref="BoundSendToServer"/>, <see cref="BoundSendResponse"/>, <see cref="BoundReceiveMessage"/>),
/// each carrying its exception phrases, built once the statement's syntax rules have been asked. The screens run in the design's order: the name shape of
/// every data-name operand (COBOL2024), SEND SR1 (literal-1), SEND SR4 and RECEIVE SR1 (message-tag items), RECEIVE SR3 (integer numeric), SEND SR3 and
/// RECEIVE SR2 (the message area), then RAISING's exception-name through the one funnel (<see cref="EcNameResolution"/>). A statement that draws a refusal
/// yields a refusal node, never a half-built one.
/// <para>⛔ A SEND'S FORMAT IS ASSIGNED ONCE (<see cref="ClassifySend"/>, kb/Work PB4090): §2023:14.9.38.2 prints the formats apart only by their operands and
/// their tail, and the grammar keeps one rule for both because which one a <c>SEND TO name FROM x</c> is depends on the declaration of <c>name</c>, which
/// no parse alternative reads (kb/Work PB4085).</para>
/// </summary>
internal sealed class MessagingBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The citation of SEND's general formats, the clause that prints the TO and RETURNING operands.</summary>
    private const string SendClause = "ISO §2023:14.9.38.2";

    /// <summary>The citation of RECEIVE's general format, the clause that prints its data-name-1 and data-name-2.</summary>
    private const string ReceiveClause = "ISO §2023:14.9.31.2";

    /// <summary>Bind <c>RECEIVE [FROM] data-name-1 GIVING identifier-1 data-name-2 [CONTINUE [AFTER] { arithmetic-expression-1 [SECONDS] | MESSAGE
    /// RECEIVED }] [ON EXCEPTION …] [NOT ON EXCEPTION …] [END-RECEIVE]</c> (ISO §2023:14.9.31.2). data-name-1 and data-name-2 are NAMES (asked their written shape
    /// before they resolve, kb/Work PB4118); the three operands are RECEIVING places, since the statement stores into each (§2023:14.9.31.4 GR1 a), GR2, GR3).</summary>
    internal BoundStatement BindReceive(Core.MessagingReceiveStatementContext r)
    {
        // The grammar gives the rule its three references in the format's order — data-name-1, identifier-1, data-name-2 (the roster test pins the count).
        var references = r.dataReference();
        BoundStatement? refusal = null;
        Place? from = null, into = null, length = null;
        if (ScreenName(references[0], "RECEIVE data-name-1", ReceiveClause)) from = Receiving(references[0], ref refusal);
        else refusal ??= BoundRejected.Reported(ctx.Edition);
        into = Receiving(references[1], ref refusal);
        if (ScreenName(references[2], "RECEIVE data-name-2", ReceiveClause)) length = Receiving(references[2], ref refusal);
        else refusal ??= BoundRejected.Reported(ctx.Edition);

        // SR1, SR3, then SR2 (the screens' order, §2023:4.3).
        if (from is not null && !IsMessageTag(from))
            refusal ??= RefuseOperandClass(references[0], "RECEIVE data-name-1", "a message-tag data item", from,
                "ISO §2023:14.9.31.3 SR1", "Data-name-1 shall be the name of a message-tag data item.");
        if (length is not null && !IsIntegerNumeric(length))
            refusal ??= RefuseOperandClass(references[2], "RECEIVE data-name-2", "an integer data item of class numeric", length,
                "ISO §2023:14.9.31.3 SR3", "Data-name-2 shall be an integer data item of the class numeric.");
        if (into is not null && ScreenMessageArea(references[1], into, receiving: true) is { } areaRefusal) refusal ??= areaRefusal;

        BoundExpr? seconds = r.arithmeticExpression() is { } expression ? host.Expr.BindExpr(expression) : null;
        var wait = seconds is not null ? ReceiveWait.Seconds : r.messageKeyword() is not null ? ReceiveWait.Poll : ReceiveWait.Forever;
        var (onException, notOnException) = BindExceptionPhrases(r.conditionalPhrases());

        if (refusal is not null || from is null || into is null || length is null) return refusal ?? BoundRejected.Reported(ctx.Edition);
        return new BoundReceiveMessage(from, into, length, wait, seconds, onException, notOnException);
    }

    /// <summary>Bind <c>SEND [TO] { literal-1 | message-server-name-1 | data-name-2 } FROM identifier-1 [RETURNING data-name-1 | RAISING …] [ON EXCEPTION …]
    /// [NOT ON EXCEPTION …] END-SEND</c> (ISO §2023:14.9.38.2) as the format <see cref="ClassifySend"/> assigns.
    /// <para>⛔ THE TO OPERAND IS READ BY THE STATEMENT'S FORMAT (kb/Work PB4090). A name after TO is <i>message-server-name-1</i> in Format 1 — "the name of
    /// a message server run unit" (§2023:14.9.38.3 SR2), a processor-defined name that is no data item, so it is NOT resolved and an undefined one is no error — or
    /// <i>data-name-2</i> in Format 2, a message-tag data item (SR4), a data reference like any other.</para></summary>
    internal BoundStatement BindSend(Core.MessagingSendStatementContext s)
    {
        var to = DataReferencesAfter(s, Core.SEND, Core.TO).FirstOrDefault();
        // ⛔ THE TO OPERAND IS A NAME IN BOTH FORMATS (kb/Work PB4118): ask how it was WRITTEN before ClassifySend reads what it is. A subscripted,
        // reference-modified or special-register operand is neither a message-server-name-1 nor a data-name-2 (ScreenName); it was reported once and is read
        // no further.
        bool toIsName = to is null || ScreenName(to, "SEND TO operand", SendClause);
        // ⛔ A CONSTANT-NAME AFTER TO IS literal-1 (kb/Work PB4743, review R1-1): "the effect of specifying constant-name-1 ... is as if literal-1 ... were written" (ISO §2023:13.10.4 GR1; §2023:13.10.3 SR2 admits
        // the name "anywhere that a format specifies a literal of the class and category of constant-name-1"), so the one alias resolution is asked before the word is taken for a message-server-name-1.
        var toAlias = to is not null && toIsName ? host.Expr.LiteralAliasOf(to) : null;
        var format = ClassifySend(s, to, toIsName, toAlias, out _);
        BoundStatement? refusal = toIsName ? null : BoundRejected.Reported(ctx.Edition);
        if (format is SendFormat.Unclassified) refusal ??= BoundRejected.Reported(ctx.Edition);

        var from = BindSendFrom(s, ref refusal);

        BoundStatement bound;
        if (format is SendFormat.MessageServerResponse)
        {
            Place? toPlace = to is not null && toIsName ? Sending(to, ref refusal) : null;
            if (toPlace is not null && !IsMessageTag(toPlace))
                refusal ??= RefuseOperandClass(to!, "SEND TO data-name-2", "a message-tag data item", toPlace,
                    "ISO §2023:14.9.38.3 SR4", "Data-name-1 and data-name-2 shall be the name of a message-tag data item.");
            EcInfo? raising = null;
            if (s.cobolWord() is { } exceptionName)
            {
                // Format 2's RAISING EXCEPTION exception-name-1 (§2023:14.9.38.2) is a WRITTEN exception-name, so it resolves through the ONE funnel every written
                // name takes. §2023:14.9.38.3 has no level-3 rule, so no level-3 site is passed (a level-1 or level-2 name binds, DESIGN-message-control.md Q6).
                if (EcNameResolution.TryResolve(ctx.Edition, exceptionName.GetText(), $"SEND RAISING EXCEPTION '{exceptionName.GetText()}'", out var info))
                    raising = info;
                else refusal ??= BoundRejected.Reported(ctx.Edition);
            }
            var (onException, notOnException) = BindExceptionPhrases(s.conditionalPhrases());
            if (ScreenSendRequiredPhrases(s, format, false)) refusal ??= BoundRejected.Reported(ctx.Edition);
            if (refusal is not null || from is null || toPlace is null) return refusal ?? BoundRejected.Reported(ctx.Edition);
            bound = new BoundSendResponse(toPlace, from, raising, s.LAST() is not null, onException, notOnException);
        }
        else
        {
            Place? returning = null;
            if (DataReferencesAfter(s, Core.RETURNING).FirstOrDefault() is { } returningReference)
            {
                if (ScreenName(returningReference, "SEND RETURNING data-name-1", SendClause)) returning = Receiving(returningReference, ref refusal);
                else refusal ??= BoundRejected.Reported(ctx.Edition);
                if (returning is not null && !IsMessageTag(returning))
                    refusal ??= RefuseOperandClass(returningReference, "SEND RETURNING data-name-1", "a message-tag data item", returning,
                        "ISO §2023:14.9.38.3 SR4", "Data-name-1 and data-name-2 shall be the name of a message-tag data item.");
            }
            BoundOperand? serverLiteral = null;
            bool literalWritten = s.literal() is not null || toAlias is not null;
            if (s.literal() is { } literal) serverLiteral = ScreenServerLiteral(host.Expr.LiteralOperand(literal), literal, literal.GetText(), ref refusal);
            else if (toAlias is not null) serverLiteral = ScreenServerLiteral(host.Expr.AliasOperand(toAlias), to!, toAlias.Word, ref refusal);
            var (onException, notOnException) = BindExceptionPhrases(s.conditionalPhrases());
            if (ScreenSendRequiredPhrases(s, format, literalWritten)) refusal ??= BoundRejected.Reported(ctx.Edition);
            string? serverName = !literalWritten && to is not null && toIsName ?DataBinder.WrittenText(to) : null;
            if (refusal is not null || from is null || returning is null || (serverLiteral is null && serverName is null))
                return refusal ?? BoundRejected.Reported(ctx.Edition);
            bound = new BoundSendToServer(serverName, serverLiteral, from, returning, onException, notOnException);
        }
        return bound;
    }

    /// <summary>⛔ SEND SR1 ON THE LITERAL-1 OPERAND, WRITTEN OR SUBSTITUTED (ISO §2023:14.9.38.3 SR1: "Literal-1 shall be an alphanumeric or national literal."). The
    /// literal's own category is the question (a literal is no data item, so no lattice class is asked): a plain, hexadecimal or N"" literal passes, and so
    /// does a figurative constant — §2023:8.3.3.6.3 SR1 admits one "whenever 'literal' appears in a format" unless the literal is restricted to numeric, and this
    /// rule restricts it to alphanumeric or national, which a figurative constant is in an alphanumeric context (kb/Work PB4743, review R1-3). A numeric or
    /// boolean literal does not. An operand already refused where it was bound is reported once, there.</summary>
    /// <returns>the operand, or null after the refusal was recorded.</returns>
    private BoundOperand? ScreenServerLiteral(BoundOperand operand, ParserRuleContext at, string written, ref BoundStatement? refusal)
    {
        switch (operand)
        {
            case BoundStringLiteral { Category: PicCategory.Alphanumeric or PicCategory.National }:
            case BoundAllLiteral { Category: PicCategory.Alphanumeric or PicCategory.National }:
            case BoundFigurative:
                return operand;
            case BoundOperandError:
                refusal ??= BoundRejected.Reported(ctx.Edition);
                return null;
        }
        using var scope = ctx.Edition.At(at);
        ctx.Edition.Error(DiagnosticCatalog.MessagingOperandClass,
            $"SEND TO {written}: literal-1 is not an alphanumeric or national literal; ISO §2023:14.9.38.3 SR1: "
            + "\"Literal-1 shall be an alphanumeric or national literal.\"");
        refusal ??= BoundRejected.Reported(ctx.Edition);
        return null;
    }

    /// <summary>⛔ SEND's identifier-1 IS ANY IDENTIFIER (ISO §2023:14.9.38.2; §2023:8.4.3.1.2 Format 1 is the function-identifier,
    /// and Format 4 the inline method invocation; both are barred only from a RECEIVING operand, §2023:8.4.3.2.3 SR1, §2023:8.4.3.4.3 SR1), so the operand is a <see cref="BoundOperand"/>: a data item's place, or a
    /// function's or method's value, as every other sending identifier is (kb/Work PB4743, review R1-2). SR3 asks about "the data description entries of
    /// identifier-1", which only a data item has, so only a place is walked (<see cref="ScreenMessageArea"/>).</summary>
    private BoundOperand? BindSendFrom(Core.MessagingSendStatementContext s, ref BoundStatement? refusal)
    {
        BoundOperand? operand = null;
        if (s.functionCall() is { } function) operand = host.Intrinsic.IntrinsicOperand(function);
        else if (s.inlineMethodInvocation() is { } invocation) operand = host.Oo.OoInlineInvocationOperand(invocation);
        else if (DataReferencesAfter(s, Core.FROM).FirstOrDefault() is { } reference)
        {
            // A repository function written without FUNCTION is a function-identifier too (§2023:8.4.3.2 SR2), read as FieldOperand reads it.
            if (host.Intrinsic.KeywordOmittedFunction(reference) is { } keywordOmitted) operand = IntrinsicBinder.OperandOf(keywordOmitted, functionValue: true);
            else if (Sending(reference, ref refusal) is { } place)
            {
                if (ScreenMessageArea(reference, place, receiving: false) is { } areaRefusal) refusal ??= areaRefusal;
                operand = new BoundFieldOperand(place);
            }
        }
        if (operand is not BoundOperandError) return operand;
        refusal ??= BoundRejected.Reported(ctx.Edition);
        return null;
    }

    private Place? Sending(Core.DataReferenceContext dref, ref BoundStatement? refusal)
    {
        var answer = host.Expr.ResolveSending(dref);
        if (answer.Place is { } place) return place;
        refusal ??= answer.Refusal(ctx.Edition);
        return null;
    }

    private Place? Receiving(Core.DataReferenceContext dref, ref BoundStatement? refusal)
    {
        if (host.Expr.ResolveReceiving(dref) is { } place) return place;
        refusal ??= BoundRejected.Reported(ctx.Edition);
        return null;
    }

    /// <summary>The message-tag test every screen asks (ISO §2023:13.18.60.4 GR9: the class and category of a message-tag data item are message-tag): a whole item,
    /// never a reference-modified window.</summary>
    private static bool IsMessageTag(Place place) => place.DenotedItem is not null && ItemCategory.IsMessageTag(place.Item);

    /// <summary>RECEIVE SR3's test, "an integer data item of the class numeric", through the ONE integer classifier.</summary>
    private static bool IsIntegerNumeric(Place place) => IntrinsicResultType.IsIntegerOperand(new BoundFieldOperand(place));

    /// <summary>Report an operand that is not of the class its syntax rule requires (COBOL6265), quoting the rule's text.</summary>
    private BoundStatement RefuseOperandClass(Core.DataReferenceContext operand, string face, string required, Place actual, string rule, string sentence)
    {
        using var at = ctx.Edition.At(operand);
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.MessagingOperandClass,
            $"{face} '{DataBinder.WrittenText(operand)}' (class {IntrinsicArgumentRules.ClassNameOf(actual)}) is not {required}; {rule}: \"{sentence}\"");
    }

    /// <summary>⛔ SEND SR3 AND RECEIVE SR2, ONE WALK OVER identifier-1 AND ITS SUBORDINATES. "The data description entries of identifier-1 or any data items
    /// subordinate to it shall not contain the ANY LENGTH clause, the BASED clause, the DYNAMIC-LENGTH clause, the OBJECT-REFERENCE clause, the OCCURS clause
    /// with the DEPENDING ON phrase where the depending on data item is not within identifier-1, the POINTER clause, the FUNCTION-POINTER clause or the
    /// PROGRAM-POINTER clause" — and RECEIVE's list adds the CONSTANT RECORD clause. That one is never reached here: a RECEIVE writes identifier-1, so the
    /// receiving funnel (<see cref="ExpressionBinder.ResolveReceiving(Core.DataReferenceContext)"/>) has already refused every item of a CONSTANT RECORD
    /// (COBOL1548, §2023:13.18.15.3 SR2), before this screen sees a place. The message is the item's image, so each forbidden clause is one whose storage is no part
    /// of an image (a pointer, an object reference), has no fixed extent (ANY LENGTH, DYNAMIC-LENGTH, an ODO whose count the receiving run unit does not
    /// hold) or is not the message area's own (BASED).</summary>
    private BoundStatement? ScreenMessageArea(Core.DataReferenceContext operand, Place place, bool receiving)
    {
        // ⛔ A REFERENCE-MODIFIED identifier-1 (kb/Work PB4743, review R1-4): §2023:8.4.3.3.4 GR5 makes the slice "a unique data item that is a subset of the data
        // item referenced by identifier-1" with NO subordinates, and GR6 gives it its base's class, category and usage, so its one data description entry is the
        // base item's own: the walk asks that entry (a BASED, DYNAMIC-LENGTH, ANY LENGTH, pointer or ODO base item draws the refusal, as the whole item does)
        // and not the base's subordinates, which the slice does not have.
        var root = place.DenotedItem ?? place.Item;
        var items = place.DenotedItem is null ? [root] : DataItem.SelfAndDescendantsOf(root);
        foreach (var item in items)
        {
            string? clause = ForbiddenClause(item, root);
            if (clause is null) continue;
            string statement = receiving ? "RECEIVE" : "SEND";
            string rule = receiving ? "ISO §2023:14.9.31.3 SR2" : "ISO §2023:14.9.38.3 SR3";
            using var at = ctx.Edition.At(operand);
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.MessagingMessageArea,
                $"{statement} {(receiving ? "GIVING" : "FROM")} '{DataBinder.WrittenText(operand)}': "
                + (ReferenceEquals(item, root) ? "the item" : $"its subordinate '{item.CobolName}'") + $" has {clause}; {rule}: \"The data description entries of "
                + "identifier-1 or any data items subordinate to it shall not contain " + (receiving
                    ? "the ANY LENGTH clause, the BASED clause, the CONSTANT RECORD clause, the DYNAMIC-LENGTH clause, the FUNCTION-POINTER clause, the "
                      + "OBJECT-REFERENCE clause, the OCCURS clause with the DEPENDING ON phrase where the depending on data item is not within identifier-1, "
                      + "the POINTER clause, or the PROGRAM-POINTER clause"
                    : "the ANY LENGTH clause, the BASED clause, the DYNAMIC-LENGTH clause, the OBJECT-REFERENCE clause, the OCCURS clause with the DEPENDING ON "
                      + "phrase where the depending on data item is not within identifier-1, the POINTER clause, the FUNCTION-POINTER clause or the "
                      + "PROGRAM-POINTER clause") + ".\"");
        }
        // A message tag lives in a managed slot of the group that holds it and is no part of the item's image (DOC-A.1-212): a FIXED group's tags travel beside its image
        // (MessagingEmitter, AsyncMessaging.TagsOf; DESIGN-message-control.md section 4.4). A VARIABLE-LENGTH group is received by its contiguous decomposition
        // (FromContiguousImage), which has no tag-skipping form, so its tag would be lost silently: refused (kb/Work PB4744's lead).
        if (place.DenotedItem is not null && root.IsGroup && DataItem.HasVariableLengthSubordinate(root)
            && DataItem.DescendantsOf(root).FirstOrDefault(i => i.IsElementary && ItemCategory.IsMessageTag(i)) is { } tag)
        {
            using var at = ctx.Edition.At(operand);
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.MessagingTagInMessageArea,
                $"{(receiving ? "RECEIVE GIVING" : "SEND FROM")} '{DataBinder.WrittenText(operand)}': its subordinate '{tag.CobolName}' is a message tag in a variable-length group");
        }
        return null;
    }

    /// <summary>The forbidden clause <paramref name="item"/> carries, spelled as the rule names it, or null. A depending-on item outside identifier-1 is found
    /// from the table's <see cref="OccursSpec"/> (<see cref="OdoModel.IsWithin"/>).</summary>
    private static string? ForbiddenClause(DataItem item, DataItem root) => item switch
    {
        { IsAnyLength: true } => "the ANY LENGTH clause",
        { IsBased: true } => "the BASED clause",
        { IsDynamicLength: true } => "the DYNAMIC-LENGTH clause",
        { Pic.Category: PicCategory.ObjectReference } => "the OBJECT-REFERENCE clause",
        { Pic.Category: PicCategory.Pointer } => "the POINTER clause",
        { Pic.Category: PicCategory.FunctionPointer } => "the FUNCTION-POINTER clause",
        { Pic.Category: PicCategory.ProgramPointer } => "the PROGRAM-POINTER clause",
        { OccursSpec: { Depending: { } depending } } when !OdoModel.IsWithin(depending, root) =>
            $"an OCCURS clause DEPENDING ON '{depending.CobolName}', which is not within identifier-1",
        _ => null,
    };

    /// <summary>Which of §2023:14.9.38.2's two general formats a written SEND is. <see cref="Unclassified"/> is a statement whose format a diagnostic has already
    /// refused — it carries a phrase of each (COBOL5488), or its TO operand is no name and nothing else says which format it is (COBOL2024) — so no
    /// per-format rule is asked of it.</summary>
    private enum SendFormat { ToMessageServer, MessageServerResponse, Unclassified }

    /// <summary>⛔ THE ONE PLACE A WRITTEN SEND IS ASSIGNED ITS FORMAT (kb/Work PB4090). §2023:14.9.38.2 prints the two formats apart only by their operands and
    /// their tail: Format 1 is <c>TO { literal-1 | message-server-name-1 } FROM identifier-1 RETURNING data-name-1</c>, Format 2 <c>TO data-name-2 FROM
    /// identifier-1 [RAISING …]</c>. So the evidence of Format 1 is a literal-1 operand and a RETURNING phrase, and of Format 2 a RAISING phrase and a TO name
    /// that is a message-tag data item (§2023:14.9.38.3 SR4: data-name-2 "shall be the name of a message-tag data item"). Evidence for both is COBOL5488; evidence
    /// for neither is Format 1 — a bare name after TO that is no message-tag item can only be message-server-name-1 (SR2), whose statement then lacks its
    /// RETURNING phrase (<see cref="ScreenSendRequiredPhrases"/>). The name is read SILENTLY (<see cref="ReferenceResolver.ProbeDeclaration"/>): as
    /// message-server-name-1 it is no data item, and reporting it undefined would refuse legal source.
    /// <para>⛔ THE OPERAND'S WRITTEN SHAPE COMES BEFORE ITS DECLARATION (kb/Work PB4118). A message-server-name-1 is a bare name, so a TO operand written with
    /// a qualifier (<c>C OF B</c>) cannot be one: it is a data-name-2, the one reading on which §2023:8.4.2.2.2 Format 1 admits a qualifier, and is Format 2's
    /// evidence exactly as a message-tag item is. An operand <paramref name="toIsName"/> refused (a subscript, a reference modifier, a special register:
    /// neither format prints one) says nothing about the format; if nothing else does, the statement is <see cref="SendFormat.Unclassified"/> and is not also
    /// asked for the RETURNING phrase it may not need.</para></summary>
    private SendFormat ClassifySend(Core.MessagingSendStatementContext s, Core.DataReferenceContext? to, bool toIsName,
        ExpressionBinder.LiteralAlias? toAlias, out bool toIsMessageTag)
    {
        toIsMessageTag = to is not null && toIsName && toAlias is null && ctx.Refs.ProbeDeclaration(to) is { } declaration
            && ItemCategory.IsMessageTag(declaration);
        bool toIsQualified = to is not null && toIsName && ReferenceResolver.ReadWritten(to).Qualifiers.Count > 0;
        List<string> format1 = [], format2 = [];
        if (s.literal() is not null) format1.Add("a literal-1 operand");
        else if (toAlias is not null) format1.Add($"the TO operand '{toAlias.Word}', {toAlias.Described} (ISO §2023:13.10.3 SR2)");
        if (s.RETURNING() is not null) format1.Add("the RETURNING phrase");
        if (s.RAISING() is not null) format2.Add("the RAISING phrase");
        if (toIsMessageTag) format2.Add($"the TO operand '{DataBinder.WrittenText(to!)}', a message-tag data item");
        else if (toIsQualified)
            format2.Add($"the TO operand '{DataBinder.WrittenText(to!)}', which is qualified (a message-server-name-1 is a bare name)");

        if (format1.Count > 0 && format2.Count > 0)
        {
            ctx.Edition.Error(DiagnosticCatalog.SendPhrasesOfTwoFormats,
                $"SEND is written with {string.Join(" and ", format1)}, which only Format 1 (to-message-server) prints, "
                + $"and with {string.Join(" and ", format2)}, which only Format 2 (message-server-response) prints "
                + "(ISO §2023:14.9.38.2; §2023:14.9.38.3 SR2, SR4)");
            return SendFormat.Unclassified;
        }
        if (format1.Count == 0 && format2.Count == 0 && !toIsName) return SendFormat.Unclassified;
        return format2.Count > 0 ? SendFormat.MessageServerResponse : SendFormat.ToMessageServer;
    }

    /// <summary>⛔ A NAME OPERAND OF SEND OR RECEIVE IS ASKED ITS WRITTEN SHAPE BEFORE IT IS READ (kb/Work PB4118). SEND's TO operand (message-server-name-1 or
    /// data-name-2, §2023:14.9.38.2), its RETURNING data-name-1 and RECEIVE's data-name-1 and data-name-2 (§2023:14.9.31.2) are all printed as NAMES: no subscript
    /// (§2023:8.4.2.3.1), no reference modification (§2023:8.4.3.3.3 NOTE) and no special register (§2023:8.4.3.1.2 Format 10 / 11 are identifiers). The grammar's one
    /// <c>dataReference</c> alternative parses every such shape. The question is the ONE data-name-n shape screen, <see cref="DataBinder.ScreenDataNameShape"/>
    /// (COBOL2024), the same one the SORT and MERGE keys and the file clauses ask; a qualifier is a name's own form (§2023:8.4.2.2.2 Format 1) and passes.</summary>
    /// <returns>true when the operand is written as a name; false after reporting it.</returns>
    /// <param name="clause">the ONE clause whose general format prints this operand — <see cref="SendClause"/> or <see cref="ReceiveClause"/> — quoted in the
    /// refusal, so a RECEIVE operand is never told SEND's format and a SEND operand never RECEIVE's (kb/Work PB4169).</param>
    private bool ScreenName(Core.DataReferenceContext dref, string face, string clause) =>
        DataBinder.ScreenDataNameShape(dref, face, ctx.Edition,
            $"a name is what the statement's general format prints here ({clause})");

    /// <summary>⛔ §2023:14.9.38.2 prints `END-SEND` on its own line with NO brackets in BOTH formats, and Format 1's `RETURNING data-name-1` line with none either,
    /// while it brackets the exception phrases around them (PDF page 756, printed folio 726). Brackets are the only convention that makes a portion omissible
    /// (§2023:5.2.6.2), so both are required wherever their format is written. The grammar keeps them optional so the omission is NAMED here (COBOL1850, the one
    /// screen of an unbracketed phrase, <see cref="StatementValidation.ScreenOmittedRequiredPhrase"/>); its rule's <c>audit:required-word</c> line names this
    /// member (kb/Work PB2024). RETURNING is required wherever the statement IS Format 1 (<see cref="ClassifySend"/>): a literal-1 operand, and a name after TO
    /// that is no message-tag data item — message-server-name-1 (§2023:14.9.38.3 SR2) — neither of which Format 2 prints (kb/Work PB4090). An
    /// <see cref="SendFormat.Unclassified"/> statement has drawn its refusal and is asked no more.</summary>
    /// <returns>true when a required phrase is omitted, after reporting it.</returns>
    private bool ScreenSendRequiredPhrases(Core.MessagingSendStatementContext s, SendFormat format, bool literalWritten)
    {
        bool omitted = ctx.Validation.ScreenOmittedRequiredPhrase(s.END_SEND() is null, "END-SEND", "SEND",
            "both of its general formats print END-SEND on a line of its own without brackets", SendClause);
        if (format is not SendFormat.ToMessageServer) return omitted;
        bool literal = literalWritten;
        omitted |= ctx.Validation.ScreenOmittedRequiredPhrase(s.RETURNING() is null, "RETURNING",
            literal ? "SEND TO literal-1" : "SEND TO message-server-name-1",
            (literal ? "a literal-1 operand" : "a TO name that is no message-tag data item, which is message-server-name-1")
            + " is Format 1's (to-message-server), whose general format prints the RETURNING data-name-1 line "
            + "without brackets", "ISO §2023:14.9.38.2 Format 1");
        return omitted;
    }

    private (IReadOnlyList<BoundStatement>? OnException, IReadOnlyList<BoundStatement>? NotOnException) BindExceptionPhrases(Core.ConditionalPhrasesContext phrases)
    {
        var (on, notOn) = PhraseBlocks.Bind(phrases, ConditionalPhraseFamily.Exception, b => host.BindBlocks([b]));
        return (on, notOn);
    }

    /// <summary>The data references written directly after one of <paramref name="keywords"/> — the grammar gives SEND's three operand slots one
    /// <c>dataReference</c> rule, so the slot is read from the keyword that opens it.</summary>
    private static IEnumerable<Core.DataReferenceContext> DataReferencesAfter(ParserRuleContext statement, params int[] keywords)
    {
        for (int i = 1; i < statement.ChildCount; i++)
            if (statement.GetChild(i) is Core.DataReferenceContext dref
                && statement.GetChild(i - 1) is ITerminalNode { Symbol.Type: var type }
                && keywords.Contains(type))
                yield return dref;
    }
}
