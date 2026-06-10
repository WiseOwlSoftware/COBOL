// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Bound;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The COBOL.NET bound semantic tree (COBOLNET_DESIGN §2). The binder resolves every reference to a Place, every
//  literal to typed text, and every condition/expression to a bound node ONCE — so the backend (and future
//  desugar passes + the G4 PC dispatcher) walk this tree WITHOUT re-touching the ANTLR parse tree. No bound node
//  holds a raw parse context. Control-flow *emission* (sequential paragraph calls now, the dispatcher at G4) is the
//  backend's concern; this tree faithfully represents the program's paragraph/statement structure.
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>A bound program unit: its paragraphs in source order (the entry runs them in sequence until G4).</summary>
public sealed record BoundProgram(IReadOnlyList<BoundParagraph> Paragraphs);

/// <summary>A bound paragraph: its COBOL name and its SENTENCES (each a statement list — the separator-period
/// boundaries are semantic: NEXT SENTENCE transfers to the point after the current sentence, ISO §14.9.19 GR6).
/// Its pc index is its position in <see cref="BoundProgram.Paragraphs"/> — the G4 PC dispatcher transfers control
/// by that index.</summary>
public sealed record BoundParagraph(string CobolName, IReadOnlyList<IReadOnlyList<BoundStatement>> Sentences)
{
    /// <summary>All statements in order (sentence boundaries flattened) — for consumers that don't care.</summary>
    public IEnumerable<BoundStatement> Statements => Sentences.SelectMany(s => s);
}

// ── Numeric expressions (scale-tracked at render time by the backend) ──────────────────────────────────────────

/// <summary>A bound numeric expression — a tree of resolved operands and operators (no parse context).</summary>
public abstract record BoundExpr;

/// <summary>A numeric literal, kept as raw source text (e.g. <c>"3.5"</c>, <c>"-12"</c>); the backend scales it.</summary>
public sealed record BoundNumLiteral(string Text) : BoundExpr;

/// <summary>A reference to a numeric data item.</summary>
public sealed record BoundNumRef(Place Place) : BoundExpr;

/// <summary>A binary arithmetic node (<c>+ - * /</c>).</summary>
public sealed record BoundBinary(BoundExpr Left, char Op, BoundExpr Right) : BoundExpr;

/// <summary>Arithmetic negation.</summary>
public sealed record BoundNegate(BoundExpr Operand) : BoundExpr;

/// <summary>Exponentiation (<c>base ** exp</c>).</summary>
public sealed record BoundPower(BoundExpr Base, BoundExpr Exp) : BoundExpr;

/// <summary>An INDEXED BY index-name read as its 1-based occurrence number (the C# <c>long</c> index field,
/// COBOLNET_DESIGN §3.5). Valid in SET senders, SEARCH, relation conditions, and subscripts (ISO §13.18.38).</summary>
public sealed record BoundIndexRef(string IndexField) : BoundExpr;

/// <summary>An operand the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundExprError(string Feature) : BoundExpr;

// ── General operands (DISPLAY / MOVE source / comparison) — render as string or number per context ─────────────

/// <summary>A bound operand usable where either a string image or a numeric value may be required.</summary>
public abstract record BoundOperand;

/// <summary>A non-numeric (alphanumeric) literal, already decoded to its character value.</summary>
public sealed record BoundStringLiteral(string Value) : BoundOperand;

/// <summary>A numeric literal operand, kept as raw source text.</summary>
public sealed record BoundNumericLiteral(string Text) : BoundOperand;

/// <summary>A reference to a data item (its category decides string-vs-numeric rendering).</summary>
public sealed record BoundFieldOperand(Place Place) : BoundOperand;

/// <summary>A computed numeric expression used as an operand (e.g. a comparison operand <c>A + B</c>).</summary>
public sealed record BoundComputedOperand(BoundExpr Expr) : BoundOperand;

/// <summary>A figurative constant operand (ISO §8.3.1.2). <paramref name="Kind"/> ∈ {Z=ZERO, S=SPACE, H=HIGH-VALUE,
/// L=LOW-VALUE, Q=QUOTE, N=NULL}; its value is materialized against the receiving / other operand's category and
/// width (a single occurrence in DISPLAY, the receiver width in MOVE, the other operand's width in a comparison).</summary>
public sealed record BoundFigurative(char Kind) : BoundOperand;

/// <summary>The figurative <c>ALL "literal"</c> (ISO §8.3.3.6.4 Format 6): the multi-character <paramref name="Literal"/>
/// repeated to the associated width (the receiver in a MOVE, the other operand in a comparison — GR2) or used once in a
/// length-unspecified context such as DISPLAY (GR3c).</summary>
public sealed record BoundAllLiteral(string Literal) : BoundOperand;

/// <summary>An operand the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundOperandError(string Feature) : BoundOperand;

// ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound condition — a side-effect-free predicate tree (COBOLNET_DESIGN §11).</summary>
public abstract record BoundCondition;

/// <summary>A relational comparison <c>left op right</c> (<paramref name="Op"/> is the mapped C# operator).</summary>
public sealed record BoundRelational(BoundOperand Left, string Op, BoundOperand Right) : BoundCondition;

/// <summary>A logical combination (<c>&amp;&amp;</c> / <c>||</c> / <c>^</c>) of sub-conditions.</summary>
public sealed record BoundLogical(string Op, IReadOnlyList<BoundCondition> Operands) : BoundCondition;

/// <summary>Logical negation.</summary>
public sealed record BoundNot(BoundCondition Operand) : BoundCondition;

/// <summary>A level-88 condition-name membership test over its (already-resolved) conditional variable place.</summary>
public sealed record BoundCondition88(Place Parent, Condition88 Condition) : BoundCondition;

/// <summary>A sign condition: <paramref name="Expr"/> IS [NOT] {POSITIVE | NEGATIVE | ZERO}.</summary>
public sealed record BoundSignCondition(BoundExpr Expr, char Kind, bool Negated) : BoundCondition;   // Kind: P/N/Z

/// <summary>A class condition: <paramref name="Operand"/> IS [NOT] {NUMERIC | ALPHABETIC | ALPHABETIC-UPPER |
/// ALPHABETIC-LOWER} (ISO §8.8.4.1.4). <paramref name="ClassKind"/> ∈ {N, A, U, L}.</summary>
public sealed record BoundClassCondition(BoundOperand Operand, char ClassKind, bool Negated) : BoundCondition;

/// <summary>A condition the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundConditionError(string Feature) : BoundCondition;

// ── Statements ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound statement.</summary>
public abstract record BoundStatement;

/// <summary>An unsupported / unresolved statement — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundUnsupported(string Feature) : BoundStatement;

/// <summary><c>STOP RUN</c> / <c>GOBACK</c> (this slice: both unwind the paragraph chain).</summary>
public sealed record BoundStop : BoundStatement;

/// <summary><c>DISPLAY</c> of a sequence of operands (each rendered as its display image).</summary>
public sealed record BoundDisplay(IReadOnlyList<BoundOperand> Operands, bool NoAdvancing) : BoundStatement;

/// <summary><c>MOVE source TO targets</c> (single sending operand).</summary>
public sealed record BoundMove(BoundOperand Source, IReadOnlyList<Place> Targets) : BoundStatement;

// The arithmetic verbs, each a small explicit node: the source operands are bound numeric expressions, the
// receivers are resolved Places paired with a rounding mode (the ROUNDED phrase, ISO §14.7.4). The in-place forms
// (TO/FROM/BY/INTO) read+write each target; the GIVING forms only write. The backend renders the value at the
// target's scale and stores via CobolNum, rounding per the receiver's mode.

/// <summary>An arithmetic resultant identifier: the receiving <see cref="Place"/> and the rounding mode its ROUNDED
/// phrase selects (ISO §14.7.4 — no phrase → <see cref="CobolRounding.Truncation"/>; bare <c>ROUNDED</c> →
/// <see cref="CobolRounding.NearestAwayFromZero"/>; <c>ROUNDED MODE IS x</c> → the named mode).</summary>
public sealed record Receiver(Place Place, CobolRounding Rounding);

/// <summary>An ON SIZE ERROR phrase on an arithmetic statement (ISO §14.7.5): the imperative run when a size error
/// occurs (<paramref name="OnError"/>) and/or the imperative run when none does (<paramref name="NotOnError"/>);
/// either may be absent. A null <c>SizeError</c> on an arithmetic node means the statement has no phrase (the checked
/// path is not emitted — its behavior is unchanged).</summary>
public sealed record SizeErrorPhrase(IReadOnlyList<BoundStatement>? OnError, IReadOnlyList<BoundStatement>? NotOnError);

/// <summary><c>ADD addends TO targets</c> — each target ← target + Σ addends.</summary>
public sealed record BoundAddTo(IReadOnlyList<BoundExpr> Addends, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;
/// <summary><c>ADD addends GIVING targets</c> — each target ← Σ addends.</summary>
public sealed record BoundAddGiving(IReadOnlyList<BoundExpr> Addends, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;
/// <summary><c>SUBTRACT minuends FROM targets</c> — each target ← target − Σ minuends.</summary>
public sealed record BoundSubtractFrom(IReadOnlyList<BoundExpr> Minuends, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;
/// <summary><c>SUBTRACT minuends FROM from GIVING targets</c> — each target ← from − Σ minuends.</summary>
public sealed record BoundSubtractGiving(IReadOnlyList<BoundExpr> Minuends, BoundExpr From, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;
/// <summary><c>MULTIPLY a BY targets</c> — each target ← target × a.</summary>
public sealed record BoundMultiplyBy(BoundExpr A, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;
/// <summary><c>MULTIPLY a BY b GIVING targets</c> — each target ← a × b.</summary>
public sealed record BoundMultiplyGiving(BoundExpr A, BoundExpr B, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;
/// <summary><c>DIVIDE divisor INTO targets</c> — each target ← target ÷ divisor.</summary>
public sealed record BoundDivideInto(BoundExpr Divisor, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;
/// <summary><c>DIVIDE divisor INTO dividend GIVING targets</c> / <c>DIVIDE dividend BY divisor GIVING targets</c>
/// — each target ← dividend ÷ divisor.</summary>
public sealed record BoundDivideGiving(BoundExpr Dividend, BoundExpr Divisor, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;

/// <summary><c>DIVIDE … GIVING quotient REMAINDER remainder</c> (ISO §14.9.12 Formats 4–5): one quotient receiver;
/// the remainder = dividend − (intermediate quotient × divisor), where the intermediate quotient is TRUNCATED at
/// the quotient receiver's scale even when the stored quotient is ROUNDED (GR7).</summary>
public sealed record BoundDivideRemainder(
    BoundExpr Dividend, BoundExpr Divisor, Receiver Quotient, Place Remainder, SizeErrorPhrase? SizeError) : BoundStatement;

/// <summary><c>COMPUTE targets = rhs</c>.</summary>
public sealed record BoundCompute(BoundExpr Rhs, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement;

/// <summary><c>IF cond THEN then-stmts [ELSE else-stmts]</c>.</summary>
public sealed record BoundIf(
    BoundCondition Condition,
    IReadOnlyList<BoundStatement> Then,
    IReadOnlyList<BoundStatement> Else) : BoundStatement;

/// <summary>How a PERFORM repeats its body.</summary>
public abstract record BoundPerformControl;
/// <summary>Run the body once.</summary>
public sealed record PerformOnce : BoundPerformControl;
/// <summary>Run the body <paramref name="Count"/> times.</summary>
public sealed record PerformTimes(BoundOperand Count) : BoundPerformControl;
/// <summary>Run the body until <paramref name="Until"/> (TEST BEFORE → while; <paramref name="TestAfter"/> → do/while).</summary>
public sealed record PerformUntil(BoundCondition Until, bool TestAfter) : BoundPerformControl;

/// <summary>One VARYING/AFTER level of a PERFORM Format 4 (ISO §14.9.28): the induction variable (an index-name or
/// data item — SET-style target), its FROM initialization, BY augment (1 when the phrase is omitted, GR12), and
/// UNTIL condition. FROM/BY stay EXPRESSIONS — they are re-evaluated at every setting/augmenting operation and the
/// conditions at every test (GR12 item identification; changes inside the body have immediate effect).</summary>
public sealed record VaryingLevel(BoundSetTarget Var, BoundExpr From, BoundExpr By, BoundCondition Until);

/// <summary><c>PERFORM … VARYING v FROM f BY b UNTIL c [AFTER …]…</c> (ISO §14.9.28 Format 4, GR13): nested
/// induction loops, leftmost level outermost.</summary>
public sealed record PerformVarying(IReadOnlyList<VaryingLevel> Levels, bool TestAfter) : BoundPerformControl;

/// <summary>An inline <c>PERFORM … END-PERFORM</c> (a real loop over a bound body).</summary>
public sealed record BoundInlinePerform(BoundPerformControl Control, IReadOnlyList<BoundStatement> Body) : BoundStatement;

/// <summary>An out-of-line <c>PERFORM p [THRU q] [control]</c> — the resolved pc range [<paramref name="StartPc"/>,
/// <paramref name="EndPc"/>] (inclusive; a single paragraph has StartPc == EndPc), run per the control via the G4
/// dispatcher (a recursive bounded <c>Dispatch(StartPc, EndPc)</c>).</summary>
public sealed record BoundOutOfLinePerform(int StartPc, int EndPc, BoundPerformControl Control) : BoundStatement;

/// <summary><c>GO TO p</c> — set the program counter to <paramref name="TargetPc"/> (ISO §14.9.20 Format 1).</summary>
public sealed record BoundGoTo(int TargetPc) : BoundStatement;

/// <summary><c>GO TO p1 p2 … DEPENDING ON sel</c> — transfer to <c>Targets[sel-1]</c>; out-of-range falls through
/// to the next statement (ISO §14.9.20 Format 2).</summary>
public sealed record BoundGoToDepending(BoundOperand Selector, IReadOnlyList<int> Targets) : BoundStatement;

/// <summary><c>EXIT PARAGRAPH</c> — transfer to the end of the current paragraph (fall through to the next).</summary>
public sealed record BoundExitParagraph : BoundStatement;

/// <summary><c>EXIT PERFORM [CYCLE]</c> — break (or continue, when CYCLE) the nearest inline PERFORM loop.</summary>
public sealed record BoundExitPerform(bool Cycle) : BoundStatement;

/// <summary>A no-op statement: bare <c>EXIT</c>, <c>CONTINUE</c>, or <c>EXIT PROGRAM</c> in the main program.</summary>
public sealed record BoundNop : BoundStatement;

/// <summary><c>NEXT SENTENCE</c> (ISO §14.9.19 GR6 / §14.9.37 — archaic per Annex F.1, legal at every edition):
/// transfer to the implicit CONTINUE following the current sentence's separator period.</summary>
public sealed record BoundNextSentence : BoundStatement;

/// <summary><c>SET condition-name+ TO TRUE</c> — each names a level-88 whose first VALUE is stored into its
/// (already-resolved) parent place.</summary>
public sealed record BoundSetConditions(IReadOnlyList<(Place Parent, Condition88 Condition)> Sets) : BoundStatement;

// ── SET index assignment / arithmetic (ISO §14.9.39 Formats 1–2; COBOLNET_DESIGN §3.5/§12.3) ──────────────────

/// <summary>A SET receiving operand, dispatched by kind (the design's §12.3 rule).</summary>
public abstract record BoundSetTarget;
/// <summary>An INDEXED BY index-name receiver — its C# <c>long</c> occurrence-number field.</summary>
public sealed record SetIndexTarget(string IndexField) : BoundSetTarget;
/// <summary>A data-item receiver: an index data item (USAGE INDEX — receives the value unchanged, §14.9.39 GR2b)
/// or an integer data item (receives the occurrence number via its own PICTURE store, GR2c).</summary>
public sealed record SetPlaceTarget(Place Place) : BoundSetTarget;

/// <summary><c>SET receivers… TO value</c> (ISO §14.9.39 Format 1): the sender (an occurrence number — in the
/// §3.5 model an index IS its 1-based occurrence number) is determined ONCE (GR2), then stored per receiver kind.</summary>
public sealed record BoundSetTo(IReadOnlyList<BoundSetTarget> Targets, BoundExpr Value) : BoundStatement;

/// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2): the amount is determined ONCE
/// (GR3), then each index is incremented/decremented by it (GR4).</summary>
public sealed record BoundSetUpDown(IReadOnlyList<BoundSetTarget> Targets, BoundExpr Amount, bool Down) : BoundStatement;

// ── SEARCH (ISO §14.9.37 Format 1 — serial search) ─────────────────────────────────────────────────────────────

/// <summary>One WHEN arm of a serial SEARCH: its condition and imperative statements (evaluated in source order;
/// the first true arm runs and ends the search, ISO §14.9.37.4 GR5).</summary>
public sealed record BoundSearchWhen(BoundCondition Condition, IReadOnlyList<BoundStatement> Statements);

/// <summary><c>SEARCH table [VARYING …] [AT END …] WHEN…</c> (ISO §14.9.37 Format 1): a serial scan from the
/// CURRENT setting of <paramref name="IndexField"/> (the table's first index, or the VARYING same-table index).
/// Each pass: past-end → AT END; else the WHEN conditions in order; none true → the index (and
/// <paramref name="AlsoVaried"/>, a different-table index or data item, GR8) increments by 1.</summary>
public sealed record BoundSearch(
    string IndexField, long Count, BoundSetTarget? AlsoVaried,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundSearchWhen> Whens) : BoundStatement;

// ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ───────────────────────────────────────────────────────────────────

/// <summary>How a file is opened (ISO §14.9.25). Maps 1:1 to the runtime <c>FileOpenMode</c>.</summary>
public enum BoundOpenMode { Input, Output, Extend, IO }

/// <summary>How a closed file is finalized (ISO §14.9.7): a plain close, <c>WITH LOCK</c> (no reopen), or a
/// <c>REEL/UNIT</c> phrase (a no-op on a disk medium, leaves the file open).</summary>
public enum BoundCloseKind { Normal, WithLock, ReelUnit }

/// <summary><c>OPEN {INPUT|OUTPUT|I-O|EXTEND} file …</c> — each opened file with its mode (ISO §14.9.25). An
/// unsupported organization (relative/indexed in the sequential slice) carries a loud <paramref name="Unsupported"/>
/// reason so the file opens to a runtime not-implemented guard.</summary>
public sealed record BoundOpen(IReadOnlyList<(FileModel File, BoundOpenMode Mode, string? Unsupported)> Files) : BoundStatement;

/// <summary><c>CLOSE file [WITH LOCK | REEL/UNIT] …</c> (ISO §14.9.7).</summary>
public sealed record BoundClose(IReadOnlyList<(FileModel File, BoundCloseKind Kind)> Files) : BoundStatement;

/// <summary>A <c>WRITE … {BEFORE|AFTER} ADVANCING {n LINES | PAGE}</c> phrase (ISO §14.9.46): print-control output.
/// <paramref name="Page"/> = ADVANCING PAGE (a form feed); otherwise <paramref name="Lines"/> is the line count
/// (a literal or data-name, default 1). <paramref name="Before"/> distinguishes BEFORE from AFTER.</summary>
public sealed record BoundAdvancing(bool Before, bool Page, BoundOperand? Lines);

/// <summary><c>WRITE record [FROM x] [ADVANCING …]</c> (ISO §14.9.46): <paramref name="Record"/> is the record area
/// place (its image is written); a FROM operand first MOVEs into the record. <paramref name="Advancing"/> null = a
/// plain (data) WRITE. <paramref name="Unsupported"/> set (loud) when the owning file's organization is unsupported.</summary>
public sealed record BoundWrite(FileModel File, Place Record, BoundOperand? From, BoundAdvancing? Advancing, string? Unsupported) : BoundStatement;

/// <summary><c>READ file [NEXT] [INTO x] [AT END …][NOT AT END …]</c> (ISO §14.9.30): a sequential read that
/// distributes the record image into the FD record (and, with INTO, MOVEs it to <paramref name="Into"/>). The AT END
/// / NOT AT END imperatives branch on the at-end condition.</summary>
public sealed record BoundRead(
    FileModel File, Place? Into, IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd, string? Unsupported) : BoundStatement;

/// <summary><c>REWRITE record [FROM x]</c> (ISO §14.9.35): replace the last-read record with the record area's image.</summary>
public sealed record BoundRewrite(FileModel File, Place Record, BoundOperand? From, string? Unsupported) : BoundStatement;
