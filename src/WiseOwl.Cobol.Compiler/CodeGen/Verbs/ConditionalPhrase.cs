// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Bound;

namespace WiseOwl.Cobol.Compiler.CodeGen.Verbs;

/// <summary>When a statement's NOT phrase runs, as the C# the emitted branch tests (<see cref="ConditionalPhrase"/>).
/// <paramref name="Success"/> is the statement's successful-completion test, null when reaching the NOT phrase IS the success
/// (a READ's NOT AT END emitted inside its own success arm). <paramref name="ComplementsPhrase"/> says the success test is
/// exactly the negation of the conditional phrase's test, so the pair renders as <c>if … else</c>. <paramref name="Gate"/> is
/// the further test that no WHEN phrase or USE declarative handled a nonfatal condition the statement raised
/// (§2023:14.6.13.1.4 GR3, "If a conditional phrase with a NOT phrase is specified in the interrupted statement, the
/// imperative-statement in that phrase is not executed"), null when nothing can interrupt the statement that way.
/// <paramref name="Why"/>, when set, is the trailing comment the NOT phrase's own test carries.</summary>
internal readonly record struct NotPhraseCondition(string? Success, bool ComplementsPhrase, string? Gate, string? Why = null)
{
    /// <summary>The NOT phrase's whole test, or null when it runs unconditionally.</summary>
    public string? Test => (Success, Gate) switch
    {
        (null, null) => null,
        ({ } s, null) => s,
        (null, { } g) => g,
        ({ } s, { } g) => $"{s} && {g}",
    };

    /// <summary>The test as a block header, with its comment.</summary>
    public string Header(string keyword) => $"{keyword} ({Test}){(Why is null ? "" : $"   // {Why}")}";
}

/// <summary>
/// ⛔ THE ONE RENDERING OF A STATEMENT'S CONDITIONAL PHRASE AND ITS NOT PHRASE (kb/Work PB3238; PB3237, PB1120, PB606).
/// ON SIZE ERROR, ON OVERFLOW, INVALID KEY, AT END, ON EXCEPTION and their NOT twins all follow one rule: the conditional
/// phrase's imperative runs when the statement's condition holds, the NOT phrase's when the statement completes
/// successfully AND no handled nonfatal condition interrupted it (§2023:14.6.13.1.4 GR3). Each verb emitter used to write the
/// <c>if (on) { … } else …</c> shape itself, five times with four different ways of composing the NOT test, and the DELETE
/// FILE copy lost its gate. A verb now says what its tests ARE (<paramref name="phraseCond"/>, a
/// <see cref="NotPhraseCondition"/>) and this class renders them; <c>ConditionalPhraseDriftTests</c> refuses a NOT phrase's
/// body emitted anywhere else.
/// </summary>
internal static class ConditionalPhrase
{
    /// <summary>The pair: <paramref name="phrase"/> under <paramref name="phraseCond"/>, then <paramref name="notPhrase"/> under
    /// <paramref name="not"/> — an <c>else</c> when the NOT test is the bare complement of the phrase test, an <c>else if</c>
    /// otherwise; either phrase may be absent.</summary>
    public static void EmitPair(CodeWriter w, StatementEmitter statements, string phraseCond,
        IReadOnlyList<BoundStatement>? phrase, IReadOnlyList<BoundStatement>? notPhrase, NotPhraseCondition not)
    {
        if (phrase is null)
        {
            if (notPhrase is not null) EmitNot(w, statements, notPhrase, not);
            return;
        }
        using (w.Block($"if ({phraseCond})")) statements.EmitStatementList(phrase);
        if (notPhrase is null) return;
        if (not is { ComplementsPhrase: true, Gate: null })
            using (w.Block("else")) statements.EmitStatementList(notPhrase);
        else if (not.Test is null)
            throw new InvalidOperationException("a NOT phrase beside its conditional phrase needs a success test that is not the complement");
        else
            using (w.Block(not.Header("else if"))) statements.EmitStatementList(notPhrase);
    }

    /// <summary>A NOT phrase alone: its body under its test, or bare when the test is empty, so a statement nothing can
    /// interrupt emits the list with no branch.</summary>
    public static void EmitNot(CodeWriter w, StatementEmitter statements, IReadOnlyList<BoundStatement> notPhrase, NotPhraseCondition not)
    {
        if (not.Test is null) { statements.EmitStatementList(notPhrase); return; }
        using (w.Block(not.Header("if"))) statements.EmitStatementList(notPhrase);
    }
}
