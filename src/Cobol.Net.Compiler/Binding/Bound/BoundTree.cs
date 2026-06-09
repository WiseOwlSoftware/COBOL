// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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

/// <summary>A bound paragraph: its COBOL name, the C# method name it emits as, and its statements.</summary>
public sealed record BoundParagraph(string CobolName, string Method, IReadOnlyList<BoundStatement> Statements);

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
// receivers are resolved Places. The in-place forms (TO/FROM/BY/INTO) read+write each target; the GIVING forms
// only write. The backend renders the value at the target's scale and stores via CobolNum.

/// <summary><c>ADD addends TO targets</c> — each target ← target + Σ addends.</summary>
public sealed record BoundAddTo(IReadOnlyList<BoundExpr> Addends, IReadOnlyList<Place> Targets) : BoundStatement;
/// <summary><c>ADD addends GIVING targets</c> — each target ← Σ addends.</summary>
public sealed record BoundAddGiving(IReadOnlyList<BoundExpr> Addends, IReadOnlyList<Place> Targets) : BoundStatement;
/// <summary><c>SUBTRACT minuends FROM targets</c> — each target ← target − Σ minuends.</summary>
public sealed record BoundSubtractFrom(IReadOnlyList<BoundExpr> Minuends, IReadOnlyList<Place> Targets) : BoundStatement;
/// <summary><c>SUBTRACT minuends FROM from GIVING targets</c> — each target ← from − Σ minuends.</summary>
public sealed record BoundSubtractGiving(IReadOnlyList<BoundExpr> Minuends, BoundExpr From, IReadOnlyList<Place> Targets) : BoundStatement;
/// <summary><c>MULTIPLY a BY targets</c> — each target ← target × a.</summary>
public sealed record BoundMultiplyBy(BoundExpr A, IReadOnlyList<Place> Targets) : BoundStatement;
/// <summary><c>MULTIPLY a BY b GIVING targets</c> — each target ← a × b.</summary>
public sealed record BoundMultiplyGiving(BoundExpr A, BoundExpr B, IReadOnlyList<Place> Targets) : BoundStatement;
/// <summary><c>DIVIDE divisor INTO targets</c> — each target ← target ÷ divisor.</summary>
public sealed record BoundDivideInto(BoundExpr Divisor, IReadOnlyList<Place> Targets) : BoundStatement;
/// <summary><c>DIVIDE divisor INTO dividend GIVING targets</c> / <c>DIVIDE dividend BY divisor GIVING targets</c>
/// — each target ← dividend ÷ divisor.</summary>
public sealed record BoundDivideGiving(BoundExpr Dividend, BoundExpr Divisor, IReadOnlyList<Place> Targets) : BoundStatement;

/// <summary><c>COMPUTE targets = rhs</c>.</summary>
public sealed record BoundCompute(BoundExpr Rhs, IReadOnlyList<Place> Targets) : BoundStatement;

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

/// <summary>An inline <c>PERFORM … END-PERFORM</c> (a real loop over a bound body).</summary>
public sealed record BoundInlinePerform(BoundPerformControl Control, IReadOnlyList<BoundStatement> Body) : BoundStatement;

/// <summary>An out-of-line <c>PERFORM p [THRU q] [control]</c> — the resolved target methods, run per the control.
/// (Emission is a sequential call chain until the G4 dispatcher; the structure is captured here.)</summary>
public sealed record BoundOutOfLinePerform(IReadOnlyList<string> TargetMethods, BoundPerformControl Control) : BoundStatement;

/// <summary><c>SET condition-name+ TO TRUE</c> — each names a level-88 whose first VALUE is stored into its
/// (already-resolved) parent place.</summary>
public sealed record BoundSetConditions(IReadOnlyList<(Place Parent, Condition88 Condition)> Sets) : BoundStatement;
