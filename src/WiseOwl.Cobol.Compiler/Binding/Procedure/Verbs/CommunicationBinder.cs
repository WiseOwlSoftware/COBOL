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
/// A.2.1.2, A.2.2.2, A.2.3.2, A.2.4.2, A.2.5.2 and A.2.6.2, and X3.23-1985's §1985:XIV.3 where its rules differ. The facility's edges are the edition pass's; the one
/// edition fact read here is <see cref="Prints2002"/>, which text the program's edition prints: the class rules ISO 2002 added on the RECEIVE and SEND identifier-1
/// (kb/Work PB5278), and the numbering and wording every diagnostic quotes (<see cref="SyntaxRule"/>, kb/Work PB5343).
/// </summary>
internal sealed class CommunicationBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>⛔ ONE SYNTAX RULE AS EACH EDITION PRINTS IT (kb/Work PB5343). X3.23-1985's §1985:XIV.3 and ISO/IEC 1989:2002's Annex A.2 state the same rules
    /// under different numbers and words: 85 writes "must" where 2002 writes "shall", and 2002 inserted SEND's SR2, so SEND's 85 SR2 to SR6 are its SR3 to SR7. A
    /// diagnostic quotes the rule of the edition the program targets (<see cref="Cite"/>), never 2002's at <c>--std 85</c>. Every quotation is checked with
    /// <c>scripts/spec/cite.py --check</c> in both editions.</summary>
    private readonly record struct SyntaxRule(string Rule1985, string Sentence1985, string Rule2002, string Sentence2002);

    /// <summary>The syntax rule that names the CDs a statement admits.</summary>
    private readonly record struct CdRule(string Statement, SyntaxRule Rule, CommunicationFormat[] Formats);

    private static readonly CdRule AcceptRule = new("ACCEPT MESSAGE COUNT", new("XIV.3.1.3 SR1", "Cd-name-1 must reference an input CD.",
        "A.2.1.2 SR1", "Cd-name-1 shall reference an input CD."), [CommunicationFormat.Input]);
    private static readonly CdRule PurgeRule = new("PURGE", new("XIV.3.4.3 SR1", "Cd-name-1 must reference an output CD or input-output CD.",
        "A.2.4.2 SR1", "Cd-name-1 shall reference an output CD or input-output CD."), [CommunicationFormat.Output, CommunicationFormat.InputOutput]);
    private static readonly CdRule ReceiveRule = new("RECEIVE", new("XIV.3.5.3 SR1", "Cd-name-1 must reference an input CD or input-output CD.",
        "A.2.5.2 SR1", "Cd-name-1 shall reference an input CD or input-output CD."), [CommunicationFormat.Input, CommunicationFormat.InputOutput]);
    private static readonly CdRule SendRule = new("SEND", new("XIV.3.6.3 SR1", "Cd-name-1 must reference an output CD or input-output CD.",
        "A.2.6.2 SR1", "Cd-name-1 shall reference an output CD or input-output CD."), [CommunicationFormat.Output, CommunicationFormat.InputOutput]);

    /// <summary>DISABLE (statement 2: §1985:XIV.3.2.3, A.2.2.2) and ENABLE (statement 3: §1985:XIV.3.3.3, A.2.3.2) print the same three path rules, SR1 to SR3,
    /// in both editions.</summary>
    private static SyntaxRule PathRule(int statement, int rule, string cd, string phrase) =>
        new($"XIV.3.{statement}.3 SR{rule}", $"Cd-name-1 must reference {cd} when the {phrase} phrase is specified.",
            $"A.2.{statement}.2 SR{rule}", $"Cd-name-1 shall reference {cd} when the {phrase} phrase is specified.");

    private static readonly SyntaxRule SendIdentifier2 = new("XIV.3.6.3 SR2", "Identifier-2 must reference a one-character integer without an operational sign.",
        "A.2.6.2 SR3", "Identifier-2 shall reference a one-character integer without an operational sign.");
    private static readonly SyntaxRule SendIdentifier3 = new("XIV.3.6.3 SR3", "Identifier-3 must reference an integer data item.",
        "A.2.6.2 SR4", "Identifier-3 shall reference an integer data item.");
    private static readonly SyntaxRule SendMnemonic = new("XIV.3.6.3 SR4", "When the mnemonic-name phrase is used, the name is identified with a particular feature "
        + "specified by the implementor.", "A.2.6.2 SR5", "When mnemonic-name-1 is specified, the name is associated with a feature-name specified by the implementor.");
    private static readonly SyntaxRule SendZero = new("XIV.3.6.3 SR5", "Integer-1 or the value of the data item referenced by identifier-3 may be zero.",
        "A.2.6.2 SR6", "Integer-1 or the value of the data item referenced by identifier-3 may be zero.");
    private static readonly SyntaxRule SendFunction = new("XIV.3.6.3 SR6", "If identifier-1 is a function-identifier, it must reference an alphanumeric function.",
        "A.2.6.2 SR7", "If identifier-1 is a function-identifier, it shall reference an alphanumeric function.");

    /// <summary>⛔ WHICH TEXT THE PROGRAM'S EDITION PRINTS: ISO/IEC 1989:2002's Annex A.2 from 2002 (where 2014 and 2023 refuse the facility whole, COBOL0902), else
    /// X3.23-1985's §1985:XIV.3. It decides two things. (1) The 2002 class rules on RECEIVE's and SEND's identifier-1 (kb/Work PB5278): A.2.5.2 SR2 ("Identifier-1
    /// shall reference a data item of category alphanumeric") and A.2.6.2 SR2 ("... of class alphanumeric") are new at 2002: X3.23-1985 prints no rule on RECEIVE's
    /// identifier-1 (§1985:XIV.3.5.3 has SR1 alone) and restricts SEND's only when it is a function-identifier (§1985:XIV.3.6.3 SR6), so at 85 a numeric,
    /// numeric-edited or alphabetic item is legal there and RECEIVE moves the message into its character positions (§1985:XIV.3.5.4 GR3). (2) The rule a diagnostic
    /// quotes (<see cref="Cite"/>).</summary>
    private bool Prints2002 => ctx.Edition.Edition.Year >= 2002;

    /// <summary>The rule as the program's edition prints it, located and quoted.</summary>
    private string Cite(SyntaxRule rule) => Prints2002 ? $"§2002:{rule.Rule2002}: \"{rule.Sentence2002}\"" : $"§1985:{rule.Rule1985}: \"{rule.Sentence1985}\"";

    /// <summary>SEND's general format in the program's edition.</summary>
    private string SendFormat => Prints2002 ? "§2002:A.2.6.1" : "§1985:XIV.3.6.2";

    /// <summary>The cd-name a statement writes, or null after reporting it. A name that is no cd-name and a cd-name of the wrong format are the same code, each quoting the rule.</summary>
    private CommunicationDescription? ResolveCommunicationCd(Core.CobolWordContext word, CdRule rule)
    {
        using var _ = ctx.Edition.At(word);
        string name = word.GetText();
        if (!ctx.Data.State.Communication.ByName.TryGetValue(name, out var cd))
        {
            ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementCdName, $"{rule.Statement}: '{name}' is not the cd-name of a communication description entry in this "
                + $"source element; {Cite(rule.Rule)}");
            return null;
        }
        if (rule.Formats.Contains(cd.Format)) return cd;
        ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementCdName, $"{rule.Statement}: '{name}' is {CommunicationFormats.FormatWords(cd.Format)}; {Cite(rule.Rule)}");
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
        BindPath(CommunicationVerb.Enable, "ENABLE", 3, s.communicationPath(), s.cobolWord(), s.communicationKeyPhrase());

    /// <summary>A.2.2: the DISABLE statement.</summary>
    internal BoundStatement BindDisable(Core.DisableStatementContext s) =>
        BindPath(CommunicationVerb.Disable, "DISABLE", 2, s.communicationPath(), s.cobolWord(), s.communicationKeyPhrase());

    /// <param name="clause">The statement's number in both editions' clause (<see cref="PathRule"/>).</param>
    private BoundStatement BindPath(CommunicationVerb verb, string statement, int clause, Core.CommunicationPathContext written, Core.CobolWordContext word, Core.CommunicationKeyPhraseContext? key)
    {
        var (path, rule) = written.OUTPUT() is not null
            ? (CommunicationPath.Output, new CdRule(statement, PathRule(clause, 3, "an output CD", "OUTPUT"), [CommunicationFormat.Output]))
            : written.I_O() is not null
            ? (CommunicationPath.InputOutputTerminal, new CdRule(statement, PathRule(clause, 2, "an input-output CD", "I-O TERMINAL"), [CommunicationFormat.InputOutput]))
            : (written.TERMINAL() is not null ? CommunicationPath.InputTerminal : CommunicationPath.Input,
               new CdRule(statement, PathRule(clause, 1, "an input CD", "INPUT"), [CommunicationFormat.Input]));
        if (ResolveCommunicationCd(word, rule) is not { } cd) return BoundRejected.Reported(ctx.Edition);
        if (key is null) return new BoundCommunicationStatement(verb, cd, CommunicationAreaOf(cd)) { Path = path };
        var keyOperand = host.Move.SendingOperand(key.sendingOperand(), $"{statement} KEY operand");
        // §1985:XIV.3.2.3 SR4 and §1985:XIV.3.3.3 SR4: "Literal-1 or the content of the data item referenced by identifier-1 must be defined as alphanumeric"
        // (kb/Work PB5082); an alphanumeric function-identifier is an identifier there (§1985:IV.4.3.8.3.3 SR3, kb/Work PB5240). The phrase is 85's alone
        // (communication-key-phrase-removed-2002), so the rule has no later row.
        if (!IsAlphanumericKey(keyOperand))
            using (ctx.Edition.At(key.sendingOperand()))
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"{statement} KEY {DataBinder.WrittenText(key.sendingOperand())}: the operand is not "
                    + $"alphanumeric; §1985:XIV.3.{clause}.3 SR4: \"Literal-1 or the content of the data item referenced by identifier-1 must be defined as alphanumeric.\"");
        return new BoundCommunicationStatement(verb, cd, CommunicationAreaOf(cd)) { Path = path, Key = keyOperand };
    }

    /// <summary>SR4's "defined as alphanumeric": a reference of category alphanumeric (<see cref="IsCommunicationAlphanumericArea(Place)"/>), a nonnumeric literal
    /// or an alphanumeric function. A figurative constant whose class only its receiver decides, and an operand already refused, are not asked again.</summary>
    private static bool IsAlphanumericKey(BoundOperand key) => key switch
    {
        BoundFieldOperand { Place: var place } => IsCommunicationAlphanumericArea(place),
        BoundOperandError => true,
        _ => IntrinsicArgumentRules.ClassOf(key) is null or CobolClass.Alphanumeric,
    };

    /// <summary>A.2.4: <c>PURGE cd-name-1</c>.</summary>
    internal BoundStatement BindPurge(Core.PurgeStatementContext s) =>
        ResolveCommunicationCd(s.cobolWord(), PurgeRule) is { } cd
            ? new BoundCommunicationStatement(CommunicationVerb.Purge, cd, CommunicationAreaOf(cd))
            : BoundRejected.Reported(ctx.Edition);

    /// <summary>A.2.5: the RECEIVE statement. SR2: "Identifier-1 shall reference a data item of category alphanumeric", asked of the reference
    /// (<see cref="IsCommunicationAlphanumericArea(Place)"/>).</summary>
    internal BoundStatement BindReceive(Core.CommunicationReceiveStatementContext s)
    {
        var cd = ResolveCommunicationCd(s.cobolWord(), ReceiveRule);
        if (host.Expr.ResolveReceiving(s.receivingIdentifier()) is not { } into) return BoundRejected.Reported(ctx.Edition);
        if (Prints2002 && !IsCommunicationAlphanumericArea(into))
            using (ctx.Edition.At(s.receivingIdentifier()))
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"RECEIVE INTO {DataBinder.WrittenText(s.receivingIdentifier())}: the item is not of category alphanumeric; "
                    + "§2002:A.2.5.2 SR2: \"Identifier-1 shall reference a data item of category alphanumeric.\"");
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

    /// <summary>Whether a reference is alphanumeric for RECEIVE's identifier-1 and the KEY operand, asked of the REFERENCE (kb/Work PB5384): a reference-modified
    /// operand is the unique data item it creates, whose category a numeric or numeric edited base rewrites to alphanumeric ("the categories numeric, numeric
    /// edited, and alphanumeric edited are considered class and category alphanumeric", §1985:IV.4.3.8.4.4 GR5; §2023:8.4.3.3.4 GR6), read by the one reader of
    /// that rule (<see cref="Table16Operand.Of(Place)"/>, whose rider keeps an alphabetic slice alphabetic). Any other reference is its item.</summary>
    private static bool IsCommunicationAlphanumericArea(Place place) => place is RefModPlace
        ? Table16Operand.Of(place) is { Category: PicCategory.Alphanumeric, IsAlphabetic: false }
        : IsCommunicationAlphanumericArea(place.Item);

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
            if (Prints2002)
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND FROM {DataBinder.WrittenText(s.sendText())}: the operand is not of class alphanumeric; "
                    + $"§2002:A.2.6.2 SR2: \"Identifier-1 shall reference a data item of class alphanumeric\", and SR7 asks a function-identifier the same.");
            else if (s.sendText().functionCall() is not null)
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND FROM {DataBinder.WrittenText(s.sendText())}: the function is not an alphanumeric "
                    + $"function; {Cite(SendFunction)}");
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
                // makes one, and "the scaling position character P is not counted in the size of the data item" (§1985:VI.5.9.4 GR8), so PIC 9P is one character
                // (kb/Work PB5289).
                if (!IsIntegerItem(indicatorItem, static pic => pic is { Digits: 1, Signed: false }))
                    using (ctx.Edition.At(ind))
                        ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND WITH {DataBinder.WrittenText(ind.dataReference())}: {Cite(SendIdentifier2)}");
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

    /// <summary>The ADVANCING phrase's operands (SR3 to SR5 at 85, SR4 to SR6 at 2002). This MCS's devices are oriented to variable-length messages, so vertical
    /// positioning "is not applicable" and "the MCS will ignore" it (A.2.6.3 GR12; docs/CONFORMANCE.md); the statement still has to be a legal one, so its operands
    /// are asked and nothing of them is kept.</summary>
    private void ScreenSendAdvancing(Core.SendAdvancingPhraseContext advancing)
    {
        if (advancing.literal() is { } literal)
            ScreenSendAdvancingLiteral(literal);
        if (advancing.dataReference() is not { } operand) return;
        using var at = ctx.Edition.At(operand);
        if (operand.cobolWord() is { } word && ctx.Mnemonics.TryPeek(word.GetText(), out _) && !ctx.Refs.DescribesOwnItem(operand)
            && ctx.Mnemonics.TryUse(advancing, word.GetText(), out var system))
        {
            if (system.Kind != SystemNameKind.Feature || (advancing.LINE() ?? advancing.LINES()) is not null)
                ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND ADVANCING {DataBinder.WrittenText(advancing)}: {Cite(SendMnemonic)}, and "
                    + $"{SendFormat} prints no LINE or LINES after it.");
            return;
        }
        // "an integer data item" (kb/Work PB5289): a trailing-P item is one; an index data item, a symbolic-character (a figurative constant, and
        // no mnemonic-name "defined in the SPECIAL-NAMES paragraph", §1985:XIV.3.6.3 SR4), an index-name and a reference-modified item are not.
        if (!IsIntegerItem(host.Expr.FieldOperand(operand), static _ => true))
            ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND ADVANCING {DataBinder.WrittenText(operand)}: {Cite(SendIdentifier3)}");
    }

    /// <summary>Integer-1, which "may be zero" (SR5 at 85, SR6 at 2002), and the figurative ZERO is that zero (kb/Work PB5289): the bound literal answers which constant
    /// it is, whatever its spelling. ⛔ THE ALL HALF HAS AN EDITION EDGE (kb/Work PB5342): integer-1 is a literal restricted to a numeric literal, where from 2002 "the
    /// only figurative constant permitted is ZERO (ZEROS, ZEROES) without the ALL phrase" (§2002:8.3.1.2.5.2 SR1a, §2023:8.3.3.6.3 SR1a). ALL before ZERO is
    /// redundant at 85, so the one gate of that row asks it (<see cref="FigurativeAssociation.GateAllZeroAsNumeric"/>, VCR Table 7 row 7.28).</summary>
    private void ScreenSendAdvancingLiteral(Core.LiteralContext literal)
    {
        using var at = ctx.Edition.At(literal);
        if (host.Expr.LiteralOperand(literal) is not BoundFigurative { Kind: 'Z' })
            ctx.Edition.Error(DiagnosticCatalog.CommunicationStatementOperand, $"SEND ADVANCING {ConcatFolder.Spelling(literal)}: {SendFormat} prints integer-1 here, and "
                + $"{Cite(SendZero)}");
        else if (literal.nonNumericLiteral()?.figurativeConstant() is { } zero && !ExpressionBinder.IsNumericRestrictedZero(zero))
            FigurativeAssociation.GateAllZeroAsNumeric(ctx.Edition.Edition, ctx.Edition.Sink, $"figurative constant '{ConcatFolder.Spelling(zero)}' as SEND ADVANCING integer-1");
    }

    /// <summary>Whether a bound SEND operand is an integer data item whose description also meets <paramref name="more"/>, judged from the operand as bound
    /// (kb/Work PB5384). A data item asks the one integer classifier (<see cref="IntrinsicResultType.IsIntegerOperand(BoundOperand)"/>), which refuses an index data
    /// item and a reference-modified item (an alphanumeric or national unique data item, §2023:8.4.3.3.4 GR6, §1985:IV.4.3.8.4.4 GR5). A LINAGE-COUNTER, LINE-COUNTER
    /// or PAGE-COUNTER is an integer special register, asked through its implicit description, whose size is the LINAGE page size's (§1985:VII.1.3.7). A constant-name
    /// of an unsigned integer is integer-1. An operand already refused is not refused again; every other shape (an index-name, a function, a symbolic-character, a
    /// group) is no integer data item.</summary>
    private static bool IsIntegerItem(BoundOperand? operand, Func<PicInfo, bool> more) => operand switch
    {
        BoundOperandError or null => true,
        BoundFieldOperand { Place.Item.Pic: { } pic } field => IntrinsicResultType.IsIntegerOperand(field) && more(pic),
        // A constant-name of an integer value stands for integer-1 (§2023:13.10.3 SR2: "anywhere that a format specifies a
        // literal of the class and category of constant-name-1").
        BoundNumericLiteral number => number.Text.All(char.IsAsciiDigit),
        BoundComputedOperand { Expr: var register } => AlgebraicRanges.CounterRegisterDescription(register) is { IsIntegerDescription: true } pic && more(pic),
        _ => false,
    };
}
