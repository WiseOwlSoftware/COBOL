// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

using Core = CobolParserCore;

/// <summary>The conditional phrases of ISO §14.5.1 Table 12 that come as a <c>[NOT]</c> pair (and SEARCH's AT END),
/// one value per phrase keyword: the grammar spells all of them in ONE rule, <c>conditionalPhraseHead</c>.</summary>
public enum ConditionalPhraseFamily
{
    /// <summary><c>[NOT] ON SIZE ERROR</c> — ADD, COMPUTE, DIVIDE, MULTIPLY, SUBTRACT (§14.7.5).</summary>
    SizeError,

    /// <summary><c>[NOT] ON EXCEPTION</c> — ACCEPT, CALL, DELETE (file), DISPLAY, RECEIVE, SEND.</summary>
    Exception,

    /// <summary><c>[NOT] ON OVERFLOW</c> — STRING, UNSTRING; and the 85–2014 CALL's <c>ON OVERFLOW</c>.</summary>
    Overflow,

    /// <summary><c>[NOT] INVALID KEY</c> — DELETE, READ, REWRITE, START, WRITE.</summary>
    InvalidKey,

    /// <summary><c>[NOT] AT END</c> — READ, RETURN; and SEARCH's <c>AT END</c>.</summary>
    AtEnd,

    /// <summary><c>[NOT] AT END-OF-PAGE</c> (or EOP) — WRITE.</summary>
    EndOfPage,
}

/// <summary>One written conditional phrase: its <see cref="ConditionalPhraseFamily"/> and whether it is the NOT twin.
/// Two phrases are "the same phrase" in §14.5.3.3's sense ("If all permitted occurrences of a phrase have already been
/// specified …") exactly when their kinds are equal.</summary>
public readonly record struct ConditionalPhraseKind(ConditionalPhraseFamily Family, bool Negated)
{
    /// <summary>The phrase's keywords as the standard prints them, optional words included: <c>NOT ON SIZE ERROR</c>,
    /// <c>AT END</c>, <c>INVALID KEY</c>.</summary>
    public string Words => (Negated ? "NOT " : "") + Family switch
    {
        ConditionalPhraseFamily.SizeError => "ON SIZE ERROR",
        ConditionalPhraseFamily.Exception => "ON EXCEPTION",
        ConditionalPhraseFamily.Overflow => "ON OVERFLOW",
        ConditionalPhraseFamily.InvalidKey => "INVALID KEY",
        ConditionalPhraseFamily.AtEnd => "AT END",
        ConditionalPhraseFamily.EndOfPage => "AT END-OF-PAGE",
        _ => throw new ArgumentOutOfRangeException(nameof(Family), Family, null),
    };
}

/// <summary>
/// ⛔ THE ONE READER OF THE <c>conditionalPhrases</c> RULE (kb/Work PB3830). Every statement that owns Table 12 [NOT]
/// phrases holds one <see cref="Core.ConditionalPhrasesContext"/>; this class classifies its phrases and hands a binder
/// the imperative-statement of the phrase it asks for, so no binder walks the phrase's tokens or its position.
/// </summary>
public static class ConditionalPhrases
{
    /// <summary>The family a phrase keyword names, or null for a token that names none (the optional words ON, AT and
    /// KEY, ERROR, and anything else). The ONE token → family map, read by the parse-time predicate (from the token
    /// stream) and by <see cref="KindOf"/> (from the tree) alike.</summary>
    public static ConditionalPhraseFamily? FamilyOfHeadToken(int tokenType) => tokenType switch
    {
        CobolLexer.SIZE => ConditionalPhraseFamily.SizeError,
        CobolLexer.EXCEPTION => ConditionalPhraseFamily.Exception,
        CobolLexer.OVERFLOW => ConditionalPhraseFamily.Overflow,
        CobolLexer.INVALID => ConditionalPhraseFamily.InvalidKey,
        CobolLexer.END => ConditionalPhraseFamily.AtEnd,
        CobolLexer.END_OF_PAGE or CobolLexer.EOP => ConditionalPhraseFamily.EndOfPage,
        _ => null,
    };

    /// <summary>The kind of a parsed phrase.</summary>
    public static ConditionalPhraseKind KindOf(Core.ConditionalPhraseContext phrase)
    {
        var head = phrase.conditionalPhraseHead();
        for (int i = 0; i < head.ChildCount; i++)
            if (head.GetChild(i) is ITerminalNode t && FamilyOfHeadToken(t.Symbol.Type) is { } family)
                return new(family, phrase.NOT() is not null);
        throw new InvalidOperationException(
            $"conditionalPhraseHead '{head.GetText()}' holds no phrase keyword — the rule changed shape and "
            + nameof(FamilyOfHeadToken) + " must follow it");
    }

    /// <summary>The kind of the phrase the token stream holds next, or null when it holds none. Read-only over the
    /// stream — safe in ANTLR's repeated predictions.</summary>
    internal static ConditionalPhraseKind? KindAhead(ITokenStream stream) =>
        KindOfLeadingTokens(stream.LA(1), stream.LA(2), stream.LA(3));

    /// <summary>The ONE reading of a phrase's leading token types — <c>NOT</c>? then <c>ON</c>/<c>AT</c>? then the
    /// keyword — for the parse-time predicate and the error strategy alike; null when they open no phrase.</summary>
    internal static ConditionalPhraseKind? KindOfLeadingTokens(int first, int second, int third)
    {
        bool negated = first == CobolLexer.NOT;
        int next = negated ? second : first, after = negated ? third : second;
        int keyword = next is CobolLexer.ON or CobolLexer.AT ? after : next;
        return FamilyOfHeadToken(keyword) is { } family ? new(family, negated) : null;
    }

    /// <summary>The kinds of the phrases <paramref name="phrases"/> holds, in written order.</summary>
    public static IEnumerable<ConditionalPhraseKind> KindsOf(Core.ConditionalPhrasesContext? phrases) =>
        phrases is null ? [] : phrases.conditionalPhrase().Select(KindOf);

    /// <summary>The written phrase of <paramref name="kind"/>, or null. §14.5.3.3 lets each be written at most once, so
    /// there is never a second.</summary>
    public static Core.ConditionalPhraseContext? Find(Core.ConditionalPhrasesContext? phrases, ConditionalPhraseKind kind) =>
        phrases?.conditionalPhrase().FirstOrDefault(p => KindOf(p) == kind);

    /// <summary>Whether any phrase of <paramref name="family"/> (either polarity) is written.</summary>
    public static bool HasFamily(Core.ConditionalPhrasesContext? phrases, ConditionalPhraseFamily family) =>
        KindsOf(phrases).Any(k => k.Family == family);

    /// <summary>The first written phrase of <paramref name="family"/> (either polarity), or null — the one a diagnostic
    /// about the family as a whole points at.</summary>
    public static Core.ConditionalPhraseContext? FirstOf(Core.ConditionalPhrasesContext? phrases, ConditionalPhraseFamily family) =>
        phrases?.conditionalPhrase().FirstOrDefault(p => KindOf(p).Family == family);
}

/// <summary>
/// ⛔ ISO §14.5.3.3's PHRASE ASSOCIATION, DECIDED HERE AND NOWHERE ELSE (kb/Work PB3830): "Any phrase encountered is the
/// next phrase of the most-recently preceding unterminated statement with which that phrase may be syntactically
/// associated. If all permitted occurrences of a phrase have already been specified for a given statement, a subsequent
/// occurrence of that phrase is not syntactically associated with that statement."
///
/// <para><b>How the grammar uses it.</b> <c>conditionalPhrases</c> is <c>({conditionalPhraseAssociates($ctx)}?
/// conditionalPhrase)*</c>, so the innermost open statement is asked first (ANTLR's greedy loop: "most-recently
/// preceding") and takes the phrase exactly when <see cref="Associates"/> says it may; otherwise its loop ends, its
/// statement ends, and the next enclosing owner is asked. A phrase may be syntactically associated with a statement when
/// it belongs to one of the statement's printed general formats TOGETHER WITH everything the statement already wrote
/// — the phrases (READ's AT END and INVALID KEY are two formats, §14.9.30.2) AND the other syntax elements that only
/// some of its formats print (<see cref="Selector"/>: READ NEXT, PREVIOUS or ADVANCING ON LOCK is Format 1 only and
/// KEY IS Format 2 only, §14.9.30.2; WRITE ADVANCING is Format 1 only, §14.9.51.2; a COMPUTE boolean-expression that
/// is no arithmetic expression is Format 2, which prints no SIZE ERROR phrase, §14.9.8.2) — and is not one of the
/// phrases already written.
/// <see cref="Owners"/> holds the general formats, one row per owning grammar rule. (Branch review R1-1 of PB3830: a
/// table of phrase brackets alone let an inner <c>READ g NEXT</c> take the enclosing READ's NOT INVALID KEY.)</para>
///
/// <para><b>The superset reading, last of all — editions only.</b> The grammar parses the superset of every edition
/// (COBOLNET_DESIGN D7), so a phrase that only an EARLIER edition's format prints (CALL's ON OVERFLOW, removed at 2023)
/// is still associated with its statement, for VersionConformancePass to refuse by name (COBOL0882) — but ONLY when no
/// enclosing open statement may take the phrase at the target edition, so the superset never moves a legal program's
/// phrase to the wrong statement. The superset relaxes the EDITION and nothing else: a phrase no format of any edition
/// admits beside what the statement already wrote (a NOT ON OVERFLOW on a CALL; a READ's INVALID KEY after its AT END,
/// or after its NEXT) associates with nothing and is a syntax error, COBOL2269 (<see cref="UnownedPhraseMessage"/>).</para>
/// </summary>
public static class ConditionalPhraseAssociation
{
    /// <summary>A syntax element of the owning statement that some of its general formats print and others do not, so
    /// that writing it rules those others out: <paramref name="Rule"/> is the element's grammar rule, and
    /// <paramref name="Words"/> names it as the standard prints it, for COBOL2269's message.
    /// <para>⛔ The element is looked for among the owner's OWN syntax only: its child rules, and the alternative each
    /// bracket child chose (one level down — READ's <c>readLockContentionPhrase</c> → <c>readAdvancingOnLock</c>), never
    /// inside an expression or a nested statement (a function argument may hold a boolean-expression).
    /// <c>ConditionalPhraseDriftTests</c> holds every selector to that depth in the grammar.</para>
    /// <para>⛔ The test is on what the SOURCE wrote, never on which alternative the parser took:
    /// <see cref="AlsoPrinted"/> answers true for a written element that ALSO reads as syntax of the formats the
    /// selector would rule out, and such an element selects nothing. (Branch review R2-1 of PB3830: a sole figurative
    /// ZERO parses through COMPUTE's <c>booleanExpression</c> alternative but is a §8.8.1.1 arithmetic expression, so
    /// <c>COMPUTE X = ZERO ON SIZE ERROR …</c> was refused and a nested one's phrase moved outward.)</para></summary>
    public sealed record Selector(int Rule, string Words)
    {
        /// <summary>Whether the written element (a context of <see cref="Rule"/>) also reads as syntax the formats
        /// that do not print this selector print, so writing it rules none of them out. Default: never.</summary>
        public Func<ParserRuleContext, bool> AlsoPrinted { get; init; } = static _ => false;
    }

    /// <summary>One printed general format's phrase bracket: the phrases it admits, the construct that bounds the
    /// editions printing the format (null: every edition), and <see cref="Unprinted"/>, the selectors it does NOT
    /// print — a statement that wrote one of them is not written in this format, so this format's phrases are not
    /// its phrases.</summary>
    public sealed record Format(FrozenSet<ConditionalPhraseKind> Phrases, string? Construct = null)
    {
        public IReadOnlyList<Selector> Unprinted { get; init; } = [];
    }

    private static Format Pair(ConditionalPhraseFamily f, string? construct = null) =>
        new(new[] { new ConditionalPhraseKind(f, false), new ConditionalPhraseKind(f, true) }.ToFrozenSet(), construct);

    private static Format Positive(ConditionalPhraseFamily f, string? construct = null) =>
        new(new[] { new ConditionalPhraseKind(f, false) }.ToFrozenSet(), construct);

    // The selectors of the owners whose formats print different phrase brackets within ONE grammar rule.
    private static readonly Selector ReadDirection = new(Core.RULE_readDirection, "NEXT or PREVIOUS");
    private static readonly Selector ReadAdvancingOnLock = new(Core.RULE_readAdvancingOnLock, "ADVANCING ON LOCK");
    private static readonly Selector ReadKey = new(Core.RULE_readKey, "KEY IS");
    private static readonly Selector WriteAdvancing = new(Core.RULE_writeBeforeAfter, "BEFORE or AFTER ADVANCING");
    // A sole figurative ZERO parses as a booleanExpression yet is a Format 1 arithmetic expression (§8.8.1.1), so it
    // selects neither COMPUTE format: ComputeFormats is the one reading, shared with ArithmeticBinder's re-route.
    private static readonly Selector BooleanCompute = new(Core.RULE_booleanExpression, "a boolean-expression")
    {
        AlsoPrinted = static e => ComputeFormats.SoleFigurativeZero((Core.BooleanExpressionContext)e) is not null,
    };

    /// <summary>⛔ THE OWNERS: every grammar rule that writes <c>conditionalPhrases</c>, with its general formats' phrase
    /// brackets (the §14.9 general formats, read against Table 12 by <c>ConditionalPhraseDriftTests</c>).</summary>
    public static readonly FrozenDictionary<int, IReadOnlyList<Format>> Owners = new Dictionary<int, IReadOnlyList<Format>>
    {
        // §14.9.2 / §14.9.44 / §14.9.26 / §14.9.12 / §14.9.8: every format prints [| ON SIZE ERROR | NOT ON SIZE ERROR |].
        [Core.RULE_addStatement] = [Pair(ConditionalPhraseFamily.SizeError)],
        [Core.RULE_subtractStatement] = [Pair(ConditionalPhraseFamily.SizeError)],
        [Core.RULE_multiplyStatement] = [Pair(ConditionalPhraseFamily.SizeError)],
        [Core.RULE_divideStatement] = [Pair(ConditionalPhraseFamily.SizeError)],
        // §14.9.8.2: Format 1 (arithmetic-compute) prints the pair; Format 2 (boolean-compute) prints no phrase, so a
        // COMPUTE whose right side is a boolean-expression and no arithmetic expression owns none. A right side both
        // formats print — a sole identifier (parsed as Format 1) or a sole figurative ZERO (parsed as Format 2,
        // BooleanCompute.AlsoPrinted) — may be either format, so it owns the pair, and ArithmeticBinder re-routes it
        // by the receiver's class, naming the phrase COBOL1511 when the statement proves to be Format 2.
        [Core.RULE_computeStatement] = [Pair(ConditionalPhraseFamily.SizeError) with { Unprinted = [BooleanCompute] }],
        // §14.9.4.2 Formats 1 and 2: [| ON EXCEPTION | NOT ON EXCEPTION |]; the 85–2014 format's [ ON OVERFLOW ] alone
        // (ISO/IEC 1989:2002 §14.8.4.1 Format 1 — no NOT ON OVERFLOW at any edition), removed at 2023 (Annex E.2 1c).
        [Core.RULE_callStatement] =
        [
            Pair(ConditionalPhraseFamily.Exception),
            Positive(ConditionalPhraseFamily.Overflow, Constructs.CallOnOverflowRemoved2023),
        ],
        // §14.9.31.2, §14.9.38.2, §14.9.10.2 Format 2 (file), and the screen formats of ACCEPT (§14.9.1.2 Format 3)
        // and DISPLAY (§14.9.11.2 Format 2), whose phrase pair follows their positioning phrase (screenTail).
        [Core.RULE_mcsReceiveStatement] = [Pair(ConditionalPhraseFamily.Exception)],
        [Core.RULE_mcsSendStatement] = [Pair(ConditionalPhraseFamily.Exception)],
        [Core.RULE_deleteFileStatement] = [Pair(ConditionalPhraseFamily.Exception)],
        [Core.RULE_screenTail] = [Pair(ConditionalPhraseFamily.Exception)],
        // §14.9.30.2: Format 1 (sequential) { NEXT | PREVIOUS } … [ ADVANCING ON LOCK | … ] [| AT END | NOT AT END |];
        // Format 2 (random) … [ KEY IS … ] [| INVALID KEY | NOT INVALID KEY |]. NEXT is the default alternative of
        // Format 1's braces (§5.2.6.3), so a READ that writes none of the selectors may be either format.
        [Core.RULE_readStatement] =
        [
            Pair(ConditionalPhraseFamily.AtEnd) with { Unprinted = [ReadKey] },
            Pair(ConditionalPhraseFamily.InvalidKey) with { Unprinted = [ReadDirection, ReadAdvancingOnLock] },
        ],
        // §14.9.51.2: Format 1 (sequential) ADVANCING and the END-OF-PAGE pair, Format 2 (random) the INVALID KEY pair
        // and no ADVANCING.
        [Core.RULE_writeStatement] =
        [
            Pair(ConditionalPhraseFamily.EndOfPage),
            Pair(ConditionalPhraseFamily.InvalidKey) with { Unprinted = [WriteAdvancing] },
        ],
        // §14.9.35.2, §14.9.10.2 Format 1 (record), §14.9.41.2.
        [Core.RULE_rewriteStatement] = [Pair(ConditionalPhraseFamily.InvalidKey)],
        [Core.RULE_deleteStatement] = [Pair(ConditionalPhraseFamily.InvalidKey)],
        [Core.RULE_startStatement] = [Pair(ConditionalPhraseFamily.InvalidKey)],
        // §14.9.34.2: AT END [ NOT AT END ], SR4 either order.
        [Core.RULE_returnStatement] = [Pair(ConditionalPhraseFamily.AtEnd)],
        // §14.9.43.2, §14.9.48.2.
        [Core.RULE_stringStatement] = [Pair(ConditionalPhraseFamily.Overflow)],
        [Core.RULE_unstringStatement] = [Pair(ConditionalPhraseFamily.Overflow)],
        // §14.9.37.2, both formats: [ AT END imperative-statement-1 ] and no NOT AT END.
        [Core.RULE_searchStatement] = [Positive(ConditionalPhraseFamily.AtEnd)],
        [Core.RULE_searchAllStatement] = [Positive(ConditionalPhraseFamily.AtEnd)],
    }.ToFrozenDictionary();

    /// <summary>§14.5.3.3: whether the phrase <paramref name="next"/> is the next phrase of the statement that owns
    /// <paramref name="phrases"/> — the parse-time predicate's whole decision.</summary>
    public static bool Associates(Core.ConditionalPhrasesContext phrases, ConditionalPhraseKind next, EditionInfo edition)
    {
        if (MayAssociate(phrases, next, edition)) return true;
        if (!MayAssociate(phrases, next, edition: null)) return false;
        // The superset reading: only when no enclosing open statement may take the phrase by §14.5.3.3 itself.
        for (RuleContext? a = phrases.Parent?.Parent; a is not null; a = a.Parent)
            if (a is Core.ConditionalPhrasesContext outer && MayAssociate(outer, next, edition))
                return false;
        return true;
    }

    /// <summary>The §14.5.3.3 test proper: <paramref name="next"/> is not yet written, and one of the owner's general
    /// formats printed at <paramref name="edition"/> (at ANY edition when null — the superset reading) admits it together
    /// with every phrase already written, and prints every <see cref="Selector"/> the statement wrote.</summary>
    public static bool MayAssociate(Core.ConditionalPhrasesContext phrases, ConditionalPhraseKind next, EditionInfo? edition)
    {
        var written = ConditionalPhrases.KindsOf(phrases).ToList();
        return !written.Contains(next)
            && AdmittingFormats(phrases, written, next).Any(f =>
                f.Construct is null || edition is not { } e || ConstructRegistry.IsAvailable(e, f.Construct));
    }

    /// <summary>The owner's formats (at any edition) that print <paramref name="next"/> beside every phrase in
    /// <paramref name="written"/> and print every selector the owner wrote.</summary>
    private static IEnumerable<Format> AdmittingFormats(Core.ConditionalPhrasesContext phrases,
        IReadOnlyCollection<ConditionalPhraseKind> written, ConditionalPhraseKind next)
    {
        var owner = Owner(phrases);
        return FormatsOf(owner.RuleIndex).Where(f => f.Phrases.Contains(next) && written.All(f.Phrases.Contains)
            && !f.Unprinted.Any(s => Writes(owner, s)));
    }

    /// <summary>Whether the owner's own syntax writes <paramref name="selector"/>: holds its element — a child rule of
    /// the owner, or the alternative a child bracket rule chose (see <see cref="Selector"/>) — and that element is not
    /// one the other formats also print (<see cref="Selector.AlsoPrinted"/>). The phrases and their imperative
    /// statements are never looked into.</summary>
    private static bool Writes(ParserRuleContext owner, Selector selector) =>
        Element(owner, selector.Rule) is { } element && !selector.AlsoPrinted(element);

    /// <summary>The owner's own element of grammar rule <paramref name="rule"/>, or null. Loops, not LINQ: it runs at
    /// every phrase-keyword lookahead, so it allocates nothing.</summary>
    private static ParserRuleContext? Element(ParserRuleContext owner, int rule)
    {
        if (owner.children is not { } children) return null;
        foreach (var c in children)
        {
            if (c is not ParserRuleContext child || child is Core.ConditionalPhrasesContext) continue;
            if (child.RuleIndex == rule) return child;
            if (child.children is not { } grandchildren) continue;
            foreach (var g in grandchildren)
                if (g is ParserRuleContext chosen && chosen.RuleIndex == rule) return chosen;
        }
        return null;
    }

    /// <summary>Whether some general format of the owning rule, at some edition, prints the phrase <paramref name="kind"/>.</summary>
    public static bool Prints(int ownerRule, ConditionalPhraseKind kind) =>
        FormatsOf(ownerRule).Any(f => f.Phrases.Contains(kind));

    /// <summary>
    /// The COBOL2269 (statement-format-shape) message for a syntax error AT a conditional phrase that no statement
    /// open there may take, or null when <paramref name="offending"/> opens no such phrase. Read from the parser state:
    /// the candidates are the statements still open at <paramref name="context"/> and the ones that ended just before
    /// the phrase (the rightmost descendants — the parse ended them BECAUSE the phrase did not fit, §14.5.3.3 2) c) /
    /// 4) b)). The message names the innermost candidate whose formats print the phrase's family and says why it may not
    /// take this one: already written ("If all permitted occurrences of a phrase have already been specified …"), or
    /// printed by no format beside the phrases already written, or printed by none of its formats at all.
    /// </summary>
    public static string? UnownedPhraseMessage(ParserRuleContext? context, ITokenStream stream, IToken offending)
    {
        if (KindAt(stream, offending.TokenIndex) is not { } kind) return null;
        var candidates = new List<Core.ConditionalPhrasesContext>();
        for (var c = context; c is not null; c = c.Parent as ParserRuleContext)
        {
            if (c is Core.ConditionalPhrasesContext open) candidates.Add(open);
            for (var d = CobolErrorStrategy.LastRuleChild(c); d is not null; d = CobolErrorStrategy.LastRuleChild(d))
                if (d is Core.ConditionalPhrasesContext ended) candidates.Add(ended);
        }
        // Not this rule's error when a candidate may take the phrase: the parse failed for another reason.
        if (candidates.Any(p => MayAssociate(p, kind, edition: null)))
            return null;
        var owner = candidates.FirstOrDefault(p => FormatsOf(OwnerRule(p)).Any(f => f.Phrases.Any(k => k.Family == kind.Family)));
        if (owner is null) return null;
        var written = ConditionalPhrases.KindsOf(owner).ToList();
        string name = StatementName(owner);
        // The selectors that ruled out every format printing the phrase beside the phrases already written.
        var ownerContext = Owner(owner);
        var selectors = FormatsOf(ownerContext.RuleIndex)
            .Where(f => f.Phrases.Contains(kind) && written.All(f.Phrases.Contains))
            .SelectMany(f => f.Unprinted).Where(s => Writes(ownerContext, s))
            .Select(s => s.Words).Distinct().ToList();
        string why = written.Contains(kind)
            ? $"this {name} statement already has its {kind.Words} phrase, and ISO §14.5.3.3: \"If all permitted "
              + "occurrences of a phrase have already been specified for a given statement, a subsequent occurrence of "
              + "that phrase is not syntactically associated with that statement\""
            : selectors.Count > 0
                ? $"this {name} statement specifies {string.Join(" and ", selectors)}, and no general format of the "
                  + $"{name} statement that prints it also prints the {kind.Words} phrase, so the phrase may not be "
                  + "syntactically associated with this statement (ISO §14.5.3.3)"
            : Prints(OwnerRule(owner), kind)
                ? $"no general format of the {name} statement prints the {kind.Words} phrase beside the "
                  + string.Join(" and ", written.Select(w => w.Words)) + " phrase, so it may not be syntactically "
                  + "associated with this statement (ISO §14.5.3.3)"
                : $"no general format of the {name} statement prints a {kind.Words} phrase, so it may not be "
                  + "syntactically associated with this statement (ISO §14.5.3.3)";
        return $"{name} … {kind.Words}: {why}; and no other statement open here may take it. A phrase is the next phrase "
            + "of the most-recently preceding unterminated statement with which it may be syntactically associated "
            + "(ISO §14.5.3.3), and its imperative-statement ends at the next phrase of any containing statement.";
    }

    /// <summary>The conditional phrase the default-channel tokens from <paramref name="index"/> spell, or null.</summary>
    private static ConditionalPhraseKind? KindAt(ITokenStream stream, int index)
    {
        Span<int> types = [TokenConstants.EOF, TokenConstants.EOF, TokenConstants.EOF];
        int n = 0;
        for (int i = index; i < stream.Size && n < types.Length; i++)
            if (stream.Get(i) is { Channel: TokenConstants.DefaultChannel } t) types[n++] = t.Type;
        return ConditionalPhrases.KindOfLeadingTokens(types[0], types[1], types[2]);
    }

    /// <summary>The verb of the statement that owns <paramref name="phrases"/> (a screen ACCEPT / DISPLAY owns them
    /// through its positioning phrase).</summary>
    private static string StatementName(Core.ConditionalPhrasesContext phrases)
    {
        for (RuleContext? c = phrases.Parent; c is not null; c = c.Parent)
            if (c is ParserRuleContext r && Core.ruleNames[r.RuleIndex].EndsWith("Statement", StringComparison.Ordinal))
                return r.Start.Text.ToUpperInvariant();
        return "the";
    }

    private static ParserRuleContext Owner(Core.ConditionalPhrasesContext phrases) =>
        phrases.Parent as ParserRuleContext ?? throw new InvalidOperationException(
            "conditionalPhrases has no owning rule context");

    private static int OwnerRule(Core.ConditionalPhrasesContext phrases) => Owner(phrases).RuleIndex;

    private static IReadOnlyList<Format> FormatsOf(int ownerRule) =>
        Owners.TryGetValue(ownerRule, out var formats) ? formats
            : throw new InvalidOperationException(
                $"grammar rule '{Core.ruleNames[ownerRule]}' writes conditionalPhrases but has no row in "
                + $"{nameof(ConditionalPhraseAssociation)}.{nameof(Owners)} — give it its general formats' phrases");
}
