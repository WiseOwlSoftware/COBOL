// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE OPERANDS OF A STATEMENT THE COMPILER ACCEPTS INERT ARE STILL IDENTIFIERS (kb/Work PB3568). A DECLINED
/// facility's STATEMENT — VALIDATE (§14.9.50, Annex A.4.14) and the MCS SEND / RECEIVE (§14.9.38, §14.9.31, Annex A.3
/// item 4) — binds to a no-op, so the facility does nothing; but declining a facility licenses its BEHAVIOUR to be
/// absent, never its SYNTAX RULES: an undefined name, a reference-modified operand and a pointer operand compiled clean
/// because nothing ever resolved them (GnuCOBOL refused the first, and was right). This collaborator is the ONE place
/// the declined statements' identifiers are read, so the next declined statement is one more method here, never one
/// more unbound parse subtree. It builds no node and stores nothing: the resolution is for its DIAGNOSTICS.
/// <para>⚠ A DEFERRED reference shape (<see cref="RefOutcome.Deferred"/>: a legal reference whose storage place this
/// compiler has not built) is taken back off the unbuilt-operand ledger here. The ledger exists so the statement funnel
/// can announce "not implemented — reaching this statement aborts the run unit"; an inert statement never reaches the
/// storage, so the announcement would be false, and a legal operand is not refused for a place nothing reads.</para>
/// </summary>
internal sealed class DeclinedFacilityBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind the operands of <c>VALIDATE { identifier-1 } …</c> (ISO §14.9.50.2): each identifier-1 is
    /// resolved and screened against §14.9.50.3 SR1, SR2, SR4 and SR5. SR6 (more than one identifier in an
    /// exception-checking PERFORM) is the EC binder's (COBOL1607) and SR3 (a VALIDATE-STATUS clause referencing the
    /// operand) reads a data-division clause the declined facility refuses by name (COBOL1708), so neither is
    /// asked here.</summary>
    public void BindValidate(Core.ValidateFacilityStatementContext v)
    {
        int unbuiltMark = ctx.Edition.UnbuiltMark;
        foreach (var dref in v.dataReference()) ScreenValidateOperand(dref);
        ctx.Edition.TakeUnbuiltSince(unbuiltMark);
    }

    /// <summary>Bind the identifiers of <c>RECEIVE [FROM] data-name-1 GIVING identifier-1 data-name-2 [CONTINUE AFTER
    /// … SECONDS] [ON EXCEPTION …] [NOT ON EXCEPTION …]</c> (ISO §14.9.31.2): the three data references resolve, the
    /// CONTINUE AFTER expression binds as an arithmetic expression, and the imperative statements of the exception
    /// phrases bind as the statements they are — dead under the declined facility, but still source that must
    /// compile.</summary>
    public void BindReceive(Core.McsReceiveStatementContext r)
    {
        int unbuiltMark = ctx.Edition.UnbuiltMark;
        foreach (var dref in r.dataReference()) _ = host.Expr.ResolveSending(dref);
        if (r.arithmeticExpression() is { } seconds) _ = host.Expr.BindExpr(seconds);
        BindExceptionPhrases(r.conditionalPhrases());
        ctx.Edition.TakeUnbuiltSince(unbuiltMark);
    }

    /// <summary>Bind the identifiers of <c>SEND [TO] { literal-1 | message-server-name-1 | data-name-2 } FROM
    /// identifier-1 [RETURNING data-name-1 | RAISING …] [ON EXCEPTION …] [NOT ON EXCEPTION …]</c> (ISO §14.9.38.2): the
    /// FROM and RETURNING operands are data references and resolve.
    /// <para>⛔ THE TO OPERAND IS READ BY THE STATEMENT'S FORMAT (kb/Work PB4090). A name after TO is
    /// <i>message-server-name-1</i> in Format 1 — "the name of a message server run unit" (§14.9.38.3 SR2), a
    /// processor-defined name that is no data item, so it is NOT resolved and an undefined one is no error — or
    /// <i>data-name-2</i> in Format 2, a message-tag data item (SR4), a data reference like any other. The grammar's one
    /// <c>dataReference</c> alternative cannot tell them apart; <see cref="ClassifySend"/> does, from the phrases
    /// written and the declaration the name silently resolves to, and only a Format 2 name is resolved here.</para></summary>
    public void BindSend(Core.McsSendStatementContext s)
    {
        int unbuiltMark = ctx.Edition.UnbuiltMark;
        var to = DataReferencesAfter(s, Core.SEND, Core.TO).FirstOrDefault();
        var format = ClassifySend(s, to, out bool toIsMessageTag);
        foreach (var dref in DataReferencesAfter(s, Core.FROM, Core.RETURNING))
            _ = host.Expr.ResolveSending(dref);
        // A message-tag data-name-2 has no place to build (its profile is the COBOL1943 recovery shape); its entry drew
        // that refusal and the name is a declared item. Any other Format 2 name resolves, so an undefined one is COBOL1639.
        if (format is SendFormat.MessageServerResponse && to is not null && !toIsMessageTag)
            _ = host.Expr.ResolveSending(to);
        // Format 2's RAISING EXCEPTION exception-name-1 (§14.9.38.2) is a WRITTEN exception-name, so it resolves through
        // the ONE funnel every written name takes (EcNameResolution — an unknown name is COBOL0711, an EC-MCS name
        // keeps the accept-inert warning of the declined facility); it was never read.
        if (s.cobolWord() is { } exceptionName)
            _ = EcNameResolution.TryResolve(ctx.Edition, exceptionName.GetText(),
                $"SEND RAISING EXCEPTION '{exceptionName.GetText()}'", out _);
        BindExceptionPhrases(s.conditionalPhrases());
        ScreenSendRequiredPhrases(s, format);
        ctx.Edition.TakeUnbuiltSince(unbuiltMark);
    }

    /// <summary>Which of §14.9.38.2's two general formats a written SEND is. <see cref="Mixed"/> is a statement that
    /// carries a phrase of each (COBOL5488): it is in neither, so no per-format rule is asked of it.</summary>
    private enum SendFormat { ToMessageServer, MessageServerResponse, Mixed }

    /// <summary>⛔ THE ONE PLACE A WRITTEN SEND IS ASSIGNED ITS FORMAT (kb/Work PB4090). §14.9.38.2 prints the two
    /// formats apart only by their operands and their tail: Format 1 is <c>TO { literal-1 | message-server-name-1 }
    /// FROM identifier-1 RETURNING data-name-1</c>, Format 2 <c>TO data-name-2 FROM identifier-1 [RAISING …]</c>. So
    /// the evidence of Format 1 is a literal-1 operand and a RETURNING phrase, and of Format 2 a RAISING phrase and a
    /// TO name that is a message-tag data item (§14.9.38.3 SR4: data-name-2 "shall be the name of a message-tag data
    /// item"). Evidence for both is COBOL5488; evidence for neither is Format 1 — a bare name after TO that is no
    /// message-tag item can only be message-server-name-1 (SR2), whose statement then lacks its RETURNING phrase
    /// (<see cref="ScreenSendRequiredPhrases"/>). The name is read SILENTLY
    /// (<see cref="ReferenceResolver.ProbeDeclaration"/>): as message-server-name-1 it is no data item, and reporting
    /// it undefined would refuse legal source.</summary>
    private SendFormat ClassifySend(Core.McsSendStatementContext s, Core.DataReferenceContext? to, out bool toIsMessageTag)
    {
        toIsMessageTag = to is not null && ctx.Refs.ProbeDeclaration(to) is { } declaration
            && ItemCategory.IsMessageTag(declaration);
        List<string> format1 = [], format2 = [];
        if (s.literal() is not null) format1.Add("a literal-1 operand");
        if (s.RETURNING() is not null) format1.Add("the RETURNING phrase");
        if (s.RAISING() is not null) format2.Add("the RAISING phrase");
        if (toIsMessageTag) format2.Add($"the TO operand '{DataBinder.WrittenText(to!)}', a message-tag data item");

        if (format1.Count > 0 && format2.Count > 0)
        {
            ctx.Edition.Error(DiagnosticCatalog.SendPhrasesOfTwoFormats,
                $"SEND is written with {string.Join(" and ", format1)}, which only Format 1 (to-message-server) prints, "
                + $"and with {string.Join(" and ", format2)}, which only Format 2 (message-server-response) prints "
                + "(ISO §14.9.38.2; §14.9.38.3 SR2, SR4)");
            return SendFormat.Mixed;
        }
        return format2.Count > 0 ? SendFormat.MessageServerResponse : SendFormat.ToMessageServer;
    }

    /// <summary>⛔ §14.9.38.2 prints `END-SEND` on its own line with NO brackets in BOTH formats, and Format 1's
    /// `RETURNING data-name-1` line with none either, while it brackets the exception phrases around them (PDF page
    /// 756, printed folio 726). Brackets are the only convention that makes a portion omissible (§5.2.6.2), so both are
    /// required wherever their format is written — the facility is declined, its syntax rules are not (the class
    /// remark). The grammar keeps them optional so the omission is NAMED here (COBOL1850, the one screen of an
    /// unbracketed phrase, <see cref="StatementValidation.ScreenOmittedRequiredPhrase"/>); its rule's
    /// <c>audit:required-word</c> line names this member (kb/Work PB2024). RETURNING is required wherever the
    /// statement IS Format 1 (<see cref="ClassifySend"/>): a literal-1 operand, and a name after TO that is no
    /// message-tag data item — message-server-name-1 (§14.9.38.3 SR2) — neither of which Format 2 prints (kb/Work
    /// PB4090). A <see cref="SendFormat.Mixed"/> statement has drawn COBOL5488 and is asked no more.</summary>
    private void ScreenSendRequiredPhrases(Core.McsSendStatementContext s, SendFormat format)
    {
        ctx.Validation.ScreenOmittedRequiredPhrase(s.END_SEND() is null, "END-SEND", "SEND",
            "both of its general formats print END-SEND on a line of its own without brackets", "ISO §14.9.38.2");
        if (format is not SendFormat.ToMessageServer) return;
        bool literal = s.literal() is not null;
        ctx.Validation.ScreenOmittedRequiredPhrase(s.RETURNING() is null, "RETURNING",
            literal ? "SEND TO literal-1" : "SEND TO message-server-name-1",
            (literal ? "a literal-1 operand" : "a TO name that is no message-tag data item, which is message-server-name-1")
            + " is Format 1's (to-message-server), whose general format prints the RETURNING data-name-1 line "
            + "without brackets", "ISO §14.9.38.2 Format 1");
    }

    private void BindExceptionPhrases(Core.ConditionalPhrasesContext phrases) =>
        _ = PhraseBlocks.Bind(phrases, ConditionalPhraseFamily.Exception, b => host.BindBlocks([b]));

    /// <summary>The data references written directly after one of <paramref name="keywords"/> — the grammar gives
    /// SEND's three operand slots one <c>dataReference</c> rule, so the slot is read from the keyword that opens it.</summary>
    private static IEnumerable<Core.DataReferenceContext> DataReferencesAfter(ParserRuleContext statement, params int[] keywords)
    {
        for (int i = 1; i < statement.ChildCount; i++)
            if (statement.GetChild(i) is Core.DataReferenceContext dref
                && statement.GetChild(i - 1) is ITerminalNode { Symbol.Type: var type }
                && keywords.Contains(type))
                yield return dref;
    }

    /// <summary>One identifier-1 of VALIDATE against §14.9.50.3. The reference-modifier (SR1) is read off the written
    /// reference, before resolution, so a numeric item's reference modification — a shape the resolver defers — is
    /// refused too; the rest ask the resolved place. An unresolvable reference was reported by the resolver.</summary>
    private void ScreenValidateOperand(Core.DataReferenceContext dref)
    {
        string written = DataBinder.WrittenText(dref);
        if (dref.dataReferenceSuffix().Any(suffix => suffix.refModPart() is not null))
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandForm,
                $"VALIDATE '{written}' — identifier-1 shall not be reference-modified (ISO §14.9.50.3 SR1)");
        }

        // An unresolvable reference was reported by the resolver (an undefined name, a subscript on a non-table item);
        // a deferred shape has no place to screen and is no error (the class remark on a deferred shape).
        if (host.Expr.ResolveSending(dref).Place is not { } place) return;

        if (place.DenotedItem is not { } item) return;   // a reference-modified place: SR1 spoke above

        if (item.Section is null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandForm,
                $"VALIDATE '{written}' — identifier-1 shall reference a data item described in the file, linkage, "
                + "local-storage, or working-storage section (ISO §14.9.50.3 SR1)");
        }

        if (IntrinsicArgumentRules.ClassOfPlace(place) is CobolClass.Index or CobolClass.Object or CobolClass.Pointer)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandClass,
                $"VALIDATE '{written}' — identifier-1 is of class {IntrinsicArgumentRules.ClassNameOf(place)}, and "
                + "VALIDATE shall not reference a data item of class index, message-tag, object, or pointer "
                + "(ISO §14.9.50.3 SR2)");
        }

        if (item.Level == 66)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandDescription,
                $"VALIDATE '{written}' — identifier-1 shall not reference a 66-level entry (ISO §14.9.50.3 SR4)");
        }

        if (item.IsAnyLength)
        {
            ctx.Edition.Error(DiagnosticCatalog.ValidateOperandDescription,
                $"VALIDATE '{written}' — identifier-1 shall not be described with the ANY LENGTH clause "
                + "(ISO §14.9.50.3 SR5)");
        }
    }
}
