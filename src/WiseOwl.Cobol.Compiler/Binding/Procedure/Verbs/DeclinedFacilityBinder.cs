// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding.Bound;
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
    /// identifier-1 [RETURNING data-name-1 | RAISING …] [ON EXCEPTION …] [NOT ON EXCEPTION …]</c> (ISO §14.9.38.2):
    /// the FROM and RETURNING operands are data references and resolve. ⚠ THE TO OPERAND IS NOT RESOLVED, and that is
    /// the standard's, not an omission: Format 1 names a <i>message-server-name-1</i> — "the name of a message server
    /// run unit" (§14.9.38.3 SR2), a processor-defined name that is no data item — and Format 2 a message-tag
    /// <i>data-name-2</i>; the grammar's one <c>dataReference</c> alternative cannot tell them apart, and resolving a
    /// server name as a data name would refuse legal source.</summary>
    public void BindSend(Core.McsSendStatementContext s)
    {
        int unbuiltMark = ctx.Edition.UnbuiltMark;
        foreach (var dref in DataReferencesAfter(s, Core.FROM, Core.RETURNING))
            _ = host.Expr.ResolveSending(dref);
        // Format 2's RAISING EXCEPTION exception-name-1 (§14.9.38.2) is a WRITTEN exception-name, so it resolves through
        // the ONE funnel every written name takes (EcNameResolution — an unknown name is COBOL0711, an EC-MCS name
        // keeps the accept-inert warning of the declined facility); it was never read.
        if (s.cobolWord() is { } exceptionName)
            _ = EcNameResolution.TryResolve(ctx.Edition, exceptionName.GetText(),
                $"SEND RAISING EXCEPTION '{exceptionName.GetText()}'", out _);
        BindExceptionPhrases(s.conditionalPhrases());
        ctx.Edition.TakeUnbuiltSince(unbuiltMark);
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
