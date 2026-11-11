// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
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
/// A.2.1.2, A.2.2.2, A.2.3.2, A.2.4.2, A.2.5.2 and A.2.6.2, and X3.23-1985's §1985:XIV.3 where its rules differ. The facility's edges are the edition pass's; the one
/// edition fact read here is <see cref="ClassRules2002"/>, the class rules ISO 2002 added on the RECEIVE and SEND identifier-1 (kb/Work PB5278).
/// </summary>
internal sealed class CommunicationBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The syntax rule that names the CDs a statement admits.</summary>
    private readonly record struct CdRule(string Statement, string Rule, string Sentence, CommunicationFormat[] Formats);

    private static readonly CdRule AcceptRule = new("ACCEPT MESSAGE COUNT", "A.2.1.2 SR1", "Cd-name-1 shall reference an input CD.", [CommunicationFormat.Input]);
    private static readonly CdRule PurgeRule = new("PURGE", "A.2.4.2 SR1", "Cd-name-1 shall reference an output CD or input-output CD.", [CommunicationFormat.Output, CommunicationFormat.InputOutput]);
    private static readonly CdRule ReceiveRule = new("RECEIVE", "A.2.5.2 SR1", "Cd-name-1 shall reference an input CD or input-output CD.", [CommunicationFormat.Input, CommunicationFormat.InputOutput]);
    private static readonly CdRule SendRule = new("SEND", "A.2.6.2 SR1", "Cd-name-1 shall reference an output CD or input-output CD.", [CommunicationFormat.Output, CommunicationFormat.InputOutput]);

    /// <summary>⛔ THE 2002 CLASS RULES ON RECEIVE'S AND SEND'S IDENTIFIER-1 ARE AN EDITION ROW (kb/Work PB5278). ISO/IEC 1989:2002 A.2.5.2 SR2 ("Identifier-1 shall
    /// reference a data item of category alphanumeric") and A.2.6.2 SR2 ("... of class alphanumeric") are new at 2002: X3.23-1985 prints no rule on RECEIVE's
    /// identifier-1 (§1985:XIV.3.5.3 has SR1 alone, on cd-name-1) and restricts SEND's only when it is a function-identifier (§1985:XIV.3.6.3 SR6), so at 85 a numeric,
    /// numeric-edited or alphabetic item is legal there and RECEIVE moves the message into its character positions (§1985:XIV.3.5.4 GR3).</summary>
    private bool ClassRules2002 => ctx.Edition.Edition.Year >= 2002;

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
        if (key is null) return new BoundCommunicationStatement(verb, cd, CommunicationAreaOf(cd)) { Path = path };
        var keyOperand = host.Move.SendingOperand(key.sendingOperand(), $"{statement} KEY operand");
        // §1985:XIV.3.2.3 SR4 and §1985:XIV.3.3.3 SR4: "Literal-1 or the content of the data item referenced by identifier-1 must be defined as alphanumeric"
        // (kb/Work PB5082); an alphanumeric function-identifier is an identifier there (§1985:IV.4.3.8.3.3 SR3, kb/Work PB5240). The phrase is 85's alone
        // (communication-key-phrase-removed-2002), so the rule has no later row.
        if (!IsAlphanumericKey(keyOperand))
            using (ctx.Edition.At(key.sendingOperand()))
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"{statement} KEY {DataBinder.WrittenText(key.sendingOperand())}: the operand is not "
                    + $"alphanumeric; §1985:{(verb is CommunicationVerb.Enable ? "XIV.3.3.3" : "XIV.3.2.3")} SR4: \"Literal-1 or the content of the data item referenced by "
                    + "identifier-1 must be defined as alphanumeric.\"");
        return new BoundCommunicationStatement(verb, cd, CommunicationAreaOf(cd)) { Path = path, Key = keyOperand };
    }

    /// <summary>SR4's "defined as alphanumeric": a data item of category alphanumeric (<see cref="IsCommunicationAlphanumericArea"/>), a nonnumeric literal or an
    /// alphanumeric function. A figurative constant whose class only its receiver decides, and an operand already refused, are not asked again.</summary>
    private static bool IsAlphanumericKey(BoundOperand key) => key switch
    {
        BoundFieldOperand { Place.Item: var item } => IsCommunicationAlphanumericArea(item),
        BoundOperandError => true,
        _ => IntrinsicArgumentRules.ClassOf(key) is null or CobolClass.Alphanumeric,
    };

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
        if (ClassRules2002 && !IsCommunicationAlphanumericArea(into.Item))
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
        {
            using var at = ctx.Edition.At(s.sendText());
            if (ClassRules2002)
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND FROM {DataBinder.WrittenText(s.sendText())}: the operand is not of class alphanumeric; "
                    + "ISO/IEC 1989:2002 A.2.6.2 SR2: \"Identifier-1 shall reference a data item of class alphanumeric\", and SR7 asks a function-identifier the same.");
            else if (s.sendText().functionCall() is not null)
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND FROM {DataBinder.WrittenText(s.sendText())}: the function is not an alphanumeric "
                    + "function; §1985:XIV.3.6.3 SR6: \"If identifier-1 is a function-identifier, it must reference an alphanumeric function.\"");
        }
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
                // "a one-character integer without an operational sign" (§1985:XIV.3.6.3 SR2): an integer has no digit position right of the point, so a trailing P
                // makes one, and "the scaling position character P is not counted in the size of the data item" (§1985:VI.5.9.4), so PIC 9P is one character
                // (kb/Work PB5289).
                if (!IsIntegerItem(indicatorItem, static pic => pic is { Digits: 1, Signed: false }))
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
        // SR5: integer-1 "may be zero", and the figurative ZERO is that zero with or without the optional word ALL (kb/Work PB5289).
        if (advancing.literal() is { } literal && FigurativeConstants.Classify(literal.GetText(), includeNull: false).Kind is not 'Z')
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
        // SR3/SR4 "an integer data item" (kb/Work PB5289): a trailing-P item is one; an index data item, a symbolic-character or constant-name (a literal, and
        // no mnemonic-name "defined in the SPECIAL-NAMES paragraph", §1985:XIV.3.6.3 SR4) and an index-name are not.
        if (!IsIntegerItem(host.Expr.FieldOperand(operand), static _ => true))
            ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND ADVANCING {DataBinder.WrittenText(operand)}: ISO/IEC 1989:2002 A.2.6.2 SR4: \"Identifier-3 shall reference an integer data item.\"");
    }

    /// <summary>Whether a bound SEND operand is an integer data item (<see cref="PicInfo.IsIntegerDescription"/>, which no index data item meets) whose picture also meets
    /// <paramref name="more"/>. An operand already refused is not refused again; a LINAGE-COUNTER, LINE-COUNTER or PAGE-COUNTER is an integer special register and passes;
    /// every other shape (an index-name, the literal a constant-name or symbolic-character stands for, a group) is no integer data item.</summary>
    private static bool IsIntegerItem(BoundOperand? operand, Func<PicInfo, bool> more) => operand switch
    {
        BoundFieldOperand { Place.Item.Pic: { IsIntegerDescription: true } pic } => more(pic),
        BoundOperandError or null => true,
        BoundComputedOperand { Expr: BoundIndexRef } => false,
        BoundComputedOperand => true,
        _ => false,
    };
}
