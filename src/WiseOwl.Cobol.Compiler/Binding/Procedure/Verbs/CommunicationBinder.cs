// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Communication;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ THE STATEMENTS OF THE COMMUNICATION FACILITY (ISO/IEC 1989:2002 Annex A.2; docs/rearchitecture/DESIGN-communication-facility.md §4; kb/Work PB4677): one
/// <see cref="BoundCommunicationStatement"/> per statement, carrying the CD's implicit description as its area. Each method asks the statement's syntax rules:
/// A.2.1.2, A.2.2.2, A.2.3.2, A.2.4.2, A.2.5.2 and A.2.6.2. The facility's edges are the edition pass's, so nothing here reads an edition.
/// </summary>
internal sealed class CommunicationBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The syntax rule that names the CDs a statement admits.</summary>
    private readonly record struct CdRule(string Statement, string Rule, string Sentence, CommunicationFormat[] Formats);

    private static readonly CdRule AcceptRule = new("ACCEPT MESSAGE COUNT", "A.2.1.2 SR1", "Cd-name-1 shall reference an input CD.", [CommunicationFormat.Input]);
    private static readonly CdRule PurgeRule = new("PURGE", "A.2.4.2 SR1", "Cd-name-1 shall reference an output CD or input-output CD.", [CommunicationFormat.Output, CommunicationFormat.InputOutput]);
    private static readonly CdRule ReceiveRule = new("RECEIVE", "A.2.5.2 SR1", "Cd-name-1 shall reference an input CD or input-output CD.", [CommunicationFormat.Input, CommunicationFormat.InputOutput]);
    private static readonly CdRule SendRule = new("SEND", "A.2.6.2 SR1", "Cd-name-1 shall reference an output CD or input-output CD.", [CommunicationFormat.Output, CommunicationFormat.InputOutput]);

    /// <summary>The cd-name a statement writes, or null after reporting it. A name that is no cd-name and a cd-name of the wrong format are the same code, each quoting the rule.</summary>
    private CommunicationDescription? ResolveCommunicationCd(Core.CobolWordContext word, CdRule rule)
    {
        using var _ = ctx.Edition.At(word);
        string name = word.GetText();
        if (!ctx.Data.State.Communication.ByName.TryGetValue(name, out var cd))
        {
            ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementCdName, $"{rule.Statement}: '{name}' is not the cd-name of a communication description entry in this "
                + $"source element; ISO/IEC 1989:2002 {rule.Rule}: \"{rule.Sentence}\"");
            return null;
        }
        if (rule.Formats.Contains(cd.Format)) return cd;
        ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementCdName, $"{rule.Statement}: '{name}' is {CommunicationFormats.FormatWords(cd.Format)}; ISO/IEC 1989:2002 {rule.Rule}: \"{rule.Sentence}\"");
        return null;
    }

    /// <summary>The place of the CD's implicit description, which <see cref="CommunicationSectionBinder"/> always builds.</summary>
    private Place CommunicationAreaOf(CommunicationDescription cd) =>
        ctx.Refs.Places.ResolveItem(cd.Area) ?? throw new InvalidOperationException($"CD '{cd.Name}' has no place for its record area");

    /// <summary>A.2.1: <c>ACCEPT cd-name-1 MESSAGE COUNT</c>.</summary>
    internal BoundStatement BindAcceptMessageCount(Core.AcceptMessageCountStatementContext s) =>
        ResolveCommunicationCd(s.cobolWord(), AcceptRule) is { } cd
            ? new BoundCommunicationStatement(CommunicationVerb.AcceptMessageCount, cd, CommunicationAreaOf(cd))
            : BoundRejected.Reported(ctx.Edition);

    /// <summary>A.2.3: the ENABLE statement.</summary>
    internal BoundStatement BindEnable(Core.EnableStatementContext s) =>
        BindPath(CommunicationVerb.Enable, "ENABLE", "A.2.3.2", s.communicationPath(), s.cobolWord(), s.communicationKeyPhrase());

    /// <summary>A.2.2: the DISABLE statement.</summary>
    internal BoundStatement BindDisable(Core.DisableStatementContext s) =>
        BindPath(CommunicationVerb.Disable, "DISABLE", "A.2.2.2", s.communicationPath(), s.cobolWord(), s.communicationKeyPhrase());

    private BoundStatement BindPath(CommunicationVerb verb, string statement, string clause, Core.CommunicationPathContext written, Core.CobolWordContext word, Core.CommunicationKeyPhraseContext? key)
    {
        var (path, rule) = written.OUTPUT() is not null
            ? (CommunicationPath.Output, new CdRule(statement, $"{clause} SR3", "Cd-name-1 shall reference an output CD when the OUTPUT phrase is specified.", [CommunicationFormat.Output]))
            : written.I_O() is not null
            ? (CommunicationPath.InputOutputTerminal, new CdRule(statement, $"{clause} SR2", "Cd-name-1 shall reference an input-output CD when the I-O TERMINAL phrase is specified.", [CommunicationFormat.InputOutput]))
            : (written.TERMINAL() is not null ? CommunicationPath.InputTerminal : CommunicationPath.Input,
               new CdRule(statement, $"{clause} SR1", "Cd-name-1 shall reference an input CD when the INPUT phrase is specified.", [CommunicationFormat.Input]));
        if (ResolveCommunicationCd(word, rule) is not { } cd) return BoundRejected.Reported(ctx.Edition);
        BoundOperand? keyOperand = key?.literal() is { } literal ? host.Expr.LiteralOperand(literal)
            : key?.dataReference() is { } item ? host.Expr.FieldOperand(item)
            : null;
        return new BoundCommunicationStatement(verb, cd, CommunicationAreaOf(cd)) { Path = path, Key = keyOperand };
    }

    /// <summary>A.2.4: <c>PURGE cd-name-1</c>.</summary>
    internal BoundStatement BindPurge(Core.PurgeStatementContext s) =>
        ResolveCommunicationCd(s.cobolWord(), PurgeRule) is { } cd
            ? new BoundCommunicationStatement(CommunicationVerb.Purge, cd, CommunicationAreaOf(cd))
            : BoundRejected.Reported(ctx.Edition);

    /// <summary>A.2.5: the RECEIVE statement. SR2: "Identifier-1 shall reference a data item of category alphanumeric", which a group is unless its usage says otherwise.</summary>
    internal BoundStatement BindReceive(Core.CommunicationReceiveStatementContext s)
    {
        var cd = ResolveCommunicationCd(s.cobolWord(), ReceiveRule);
        if (host.Expr.ResolveReceiving(s.receivingIdentifier()) is not { } into) return BoundRejected.Reported(ctx.Edition);
        if (!IsCommunicationAlphanumericArea(into.Item))
            using (ctx.Edition.At(s.receivingIdentifier()))
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"RECEIVE INTO {DataBinder.WrittenText(s.receivingIdentifier())}: the item is not of category alphanumeric; "
                    + "ISO/IEC 1989:2002 A.2.5.2 SR2: \"Identifier-1 shall reference a data item of category alphanumeric.\"");
        var noData = s.receiveNoDataPhrase() is { } no ? host.BindBlocks([no.statementBlock()]) : null;
        var withData = s.receiveWithDataPhrase() is { } yes ? host.BindBlocks([yes.statementBlock()]) : null;
        if (cd is null) return BoundRejected.Reported(ctx.Edition);
        return new BoundCommunicationStatement(CommunicationVerb.Receive, cd, CommunicationAreaOf(cd))
        {
            Segment = s.SEGMENT() is not null,
            Into = into,
            NoData = noData,
            WithData = withData,
        };
    }

    private static bool IsCommunicationAlphanumericArea(DataItem item) =>
        item.IsGroup ? item.GroupUsage is GroupUsage.None : item.Pic is { Category: PicCategory.Alphanumeric, IsAlphabetic: false };

    /// <summary>A.2.6: the SEND statement.</summary>
    internal BoundStatement BindSend(Core.CommunicationSendStatementContext s)
    {
        var cd = ResolveCommunicationCd(s.cobolWord(), SendRule);
        BoundOperand? text = s.sendText() is { } written
            ? written.functionCall() is { } call ? host.Intrinsic.IntrinsicOperand(call) : host.Expr.FieldOperand(written.dataReference())
            : null;
        if (text is not null && IntrinsicArgumentRules.ClassOf(text) is not (null or CobolClass.Alphanumeric))
            using (ctx.Edition.At(s.sendText()))
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND FROM {DataBinder.WrittenText(s.sendText())}: the operand is not of class alphanumeric; "
                    + "ISO/IEC 1989:2002 A.2.6.2 SR2: \"Identifier-1 shall reference a data item of class alphanumeric\", and SR7 asks a function-identifier the same.");
        var indicator = SendIndicator.None;
        BoundOperand? indicatorItem = null;
        if (s.sendIndicator() is { } ind)
        {
            if (ind.ESI() is not null) indicator = SendIndicator.EndOfSegment;
            else if (ind.EMI() is not null) indicator = SendIndicator.EndOfMessage;
            else if (ind.EGI() is not null) indicator = SendIndicator.EndOfGroup;
            else
            {
                indicatorItem = host.Expr.FieldOperand(ind.dataReference());
                if (indicatorItem is BoundFieldOperand { Place.Item: { Pic: { Category: PicCategory.Numeric, Digits: not 1 } or { Category: PicCategory.Numeric, Scale: not 0 } or { Category: PicCategory.Numeric, Signed: true } or { Category: not PicCategory.Numeric } or null } })
                    using (ctx.Edition.At(ind))
                        ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND WITH {DataBinder.WrittenText(ind.dataReference())}: "
                            + "ISO/IEC 1989:2002 A.2.6.2 SR3: \"Identifier-2 shall reference a one-character integer without an operational sign.\"");
            }
        }
        if (s.sendAdvancingPhrase() is { } advancing) ScreenSendAdvancing(advancing);
        if (cd is null) return BoundRejected.Reported(ctx.Edition);
        return new BoundCommunicationStatement(CommunicationVerb.Send, cd, CommunicationAreaOf(cd))
        {
            Text = text,
            Indicator = indicator,
            IndicatorItem = indicatorItem,
        };
    }

    /// <summary>The ADVANCING phrase's operands (SR4, SR5). This MCS's devices are oriented to variable-length messages, so vertical positioning "is not applicable" and "the MCS
    /// will ignore" it (A.2.6.3 GR12; docs/CONFORMANCE.md); the statement still has to be a legal one, so its operands are asked and nothing of them is kept.</summary>
    private void ScreenSendAdvancing(Core.SendAdvancingPhraseContext advancing)
    {
        if (advancing.literal() is { } literal && !(CobolNames.Same(literal.GetText(), "ZERO") || CobolNames.Same(literal.GetText(), "ZEROS") || CobolNames.Same(literal.GetText(), "ZEROES")))
            using (ctx.Edition.At(literal))
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND ADVANCING {literal.GetText()}: ISO/IEC 1989:2002 A.2.6.1 prints integer-1 here, and A.2.6.2 SR6 says it may be zero.");
        if (advancing.dataReference() is not { } operand) return;
        using var at = ctx.Edition.At(operand);
        if (operand.cobolWord() is { } word && ctx.Mnemonics.TryPeek(word.GetText(), out _) && !ctx.Refs.DescribesOwnItem(operand)
            && ctx.Mnemonics.TryUse(advancing, word.GetText(), out var system))
        {
            if (system.Kind != SystemNameKind.Feature || (advancing.LINE() ?? advancing.LINES()) is not null)
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND ADVANCING {DataBinder.WrittenText(advancing)}: ISO/IEC 1989:2002 A.2.6.2 SR5: \"When mnemonic-name-1 "
                    + "is specified, the name is associated with a feature-name specified by the implementor\", and A.2.6.1 prints no LINE or LINES after it.");
            return;
        }
        if (host.Expr.FieldOperand(operand) is BoundFieldOperand { Place.Item: var item } && item.Pic is not { Category: PicCategory.Numeric, Scale: 0 })
            ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND ADVANCING {DataBinder.WrittenText(operand)}: ISO/IEC 1989:2002 A.2.6.2 SR4: \"Identifier-3 shall reference an integer data item.\"");
    }
}
