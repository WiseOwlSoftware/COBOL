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

/// <summary>A bound program unit: its paragraphs in source order — declarative sections FIRST (sharing the one
/// pc space, COBOLNET_DESIGN §14.5), then the nondeclarative body starting at <paramref name="EntryPc"/>
/// (ISO §14.2.3 GR1 — execution begins with the first nondeclarative procedure). <paramref name="Declaratives"/>
/// carries the program's USE AFTER STANDARD ERROR/EXCEPTION sections (ISO §14.9.49; empty/null when none).</summary>
public sealed record BoundProgram(
    IReadOnlyList<BoundParagraph> Paragraphs,
    int EntryPc = 0,
    IReadOnlyList<BoundDeclarative>? Declaratives = null,
    EcFeatures? Ec = null,
    IReadOnlyList<BoundMethod>? Methods = null);

/// <summary>One bound METHOD of a class body (ISO §11.7; OO deep-dive — the emit-into-a-type spine): its
/// contiguous pc range in the class's ONE dispatch space. The emitted public method runs
/// <c>__Dispatch(EntryPc, EndPc)</c> — the exit bound IS the method's LAST paragraph, so falling off the end
/// is the implicit method return, never a run into a sibling method's paragraphs (the legacy trap-#4 guard,
/// ported from CilEmitter's exit-bounded ranges). <paramref name="EntryPc"/> &gt; <paramref name="EndPc"/> ⇔
/// an empty method body (emitted as an empty C# method — no dispatch call at all).</summary>
public sealed record BoundMethod(string CobolName, string CsName, int EntryPc, int EndPc);

/// <summary>What of the EC exception-condition model (ISO §14.6.13; COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN) a
/// bound program actually USES — the emitter's gating summary: every piece of EC machinery (the int-returning
/// <c>__RunUse</c>, <c>__EcDispatch</c>, <c>__IoCheckEc</c>, the entry-wrapper fatal catch, the
/// <c>CobolNet.Runtime.Exceptions</c> using) is emitted ONLY when the group uses the feature, so an EC-free
/// program's generated source is byte-identical to a pre-EC build (the zero-scaffolding invariant, SSOT §18.16).</summary>
public sealed record EcFeatures(
    bool HasChecked,      // any statement bound under enabled >>TURN checking (a BoundEcChecked exists)
    bool HasIoChecked,    // any I-O statement with an enabled EC-I-O name (needs the generated __IoCheckEc)
    bool HasRaise,        // a RAISE statement (§14.9.29)
    bool HasResume,       // a RESUME statement (§14.9.33)
    bool HasF3,           // a USE AFTER EXCEPTION CONDITION declarative (§14.9.49 F3 — needs __EcDispatch)
    bool HasEcFunctions,  // a FUNCTION EXCEPTION-STATUS/-LOCATION/-STATEMENT reference (§15.28–15.33)
    bool HasRaising)      // a GOBACK/EXIT … RAISING (§14.9.18 / §14.9.14)
{
    /// <summary>Any EC-model feature present (drives the group-level <c>_ecActive</c> gate).</summary>
    public bool Any => HasChecked || HasIoChecked || HasRaise || HasResume || HasF3 || HasEcFunctions || HasRaising;
}

/// <summary>One USE declarative section (ISO §14.9.49): its inclusive pc range, the §14.9.49.4 GR7 handler exit
/// pc (== <paramref name="EndPc"/> except the CCVS termination-tail accommodation — see the binder), and its
/// trigger scope. Format 1 (AFTER STANDARD ERROR/EXCEPTION): file-scoped (GR3a/GR5, <paramref name="Files"/>
/// non-empty) or open-mode-scoped (GR3b/GR6b–e, <paramref name="ModeIndex"/> = the runtime <c>FileOpenMode</c>
/// ordinal). Format 2 (BEFORE REPORTING): <paramref name="ReportGroup"/> names the report group the procedure
/// runs just before (GR8 — wired into the report engine's per-group hook at emission). <paramref name="Global"/>
/// is parsed and recorded; cross-program dispatch (GR4) is the post-CALL wave.</summary>
public sealed record BoundDeclarative(
    string SectionName,
    int StartPc,
    int EndPc,
    int HandlerEndPc,
    IReadOnlyList<FileModel> Files,
    int? ModeIndex,
    bool Global,
    ReportGroupModel? ReportGroup = null,
    IReadOnlyList<(string Ec, FileModel? File)>? EcEntries = null,
    string? EoClassCsName = null);
// EoClassCsName: Format 4 (USE AFTER EXCEPTION OBJECT class-name, §14.9.49 — the EC-OO wave): the emitted
// C# class the generated __EcObjDispatch matches with `is` (GR14a: the object's class OR a subclass).
// EcEntries: the Format-3 scope (ISO §14.9.49.2 — USE AFTER {EXCEPTION CONDITION | EC} {ec-name [FILE f]…}…):
// each pair is one (exception-name, optional file) selection entry, consumed by the generated __EcDispatch
// selector's GR3c–g tiers. Null for Format 1/2 declaratives; an F3 declarative has empty Files / null ModeIndex,
// so the F1 __IoCheck switches naturally exclude it.

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

/// <summary>The LINAGE-COUNTER special register of <paramref name="File"/> (ISO §8.4.3.14): a READ-ONLY unsigned
/// integer the I-O control system alone modifies (§13.18.34 GR7b) — the current line within the page body. It is
/// runtime-sourced (the connector's counter, COBOLNET_DESIGN's register-attaches-to-its-subsystem rule), never a
/// synthesized storage item; SR2 bars it from receiving positions (receiving resolution already fails loud).</summary>
public sealed record BoundLinageCounterRef(FileModel File) : BoundExpr;

/// <summary>A report's LINE-COUNTER or PAGE-COUNTER register (ISO §8.4.3.15): an unsigned integer the Report
/// Writer Control System alone maintains (GR1–GR4) — runtime-sourced from the report's engine instance (the
/// register-attaches-to-its-subsystem rule, the <see cref="BoundLinageCounterRef"/> precedent), never a storage
/// item. SR3 bars LINE-COUNTER from receiving positions (receiving resolution rejects at bind).</summary>
public sealed record BoundReportCounterRef(ReportModel Report, bool IsPage) : BoundExpr;

/// <summary>A report SUM counter read (ISO §13.18.54.4 GR4 — the counter is the source item of its printable
/// entry): an unscaled integer at <paramref name="Scale"/>, runtime-sourced from the report engine. Produced
/// only by the report-section compose emission (sum counters are report-section names, unreachable from
/// PROCEDURE DIVISION references in this slice).</summary>
public sealed record BoundReportSumRef(ReportModel Report, string Id, int Scale) : BoundExpr;

/// <summary>An operand the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundExprError(string Feature) : BoundExpr;

/// <summary>A resolved intrinsic-function call (ISO §15; COBOLNET_INTRINSICS_DESIGN D2): the catalog row (already
/// category-resolved for the polymorphic MAX/MIN families) plus the typed bound arguments — table(ALL) expanded,
/// the §15.68.3 r3 default currency injected — never a pre-rendered C# fragment. <paramref name="Args"/> are
/// <see cref="BoundOperand"/>s, NOT <see cref="BoundExpr"/>s: the string-argument functions (NUMVAL, ORD,
/// LOWER-CASE …) take alphanumeric operands the expression tree cannot represent; numeric argument expressions
/// wrap as <see cref="BoundComputedOperand"/> (the documented deviation from the deep-dive's original sketch —
/// recorded there). <paramref name="Collate"/> marks a CHAR/ORD call bound under a NON-identity PROGRAM COLLATING
/// SEQUENCE (§15.15.4 r2 / §15.70.4) — the backend then passes its collating-weights table; when false the field
/// does not even exist (hazard H5).</summary>
public sealed record BoundIntrinsicCall(
    IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) : BoundExpr;

// ── General operands (DISPLAY / MOVE source / comparison) — render as string or number per context ─────────────

/// <summary>A bound operand usable where either a string image or a numeric value may be required.</summary>
public abstract record BoundOperand;

/// <summary>A non-numeric literal, already decoded to its character value. <paramref name="Category"/> carries
/// the literal's data category — Alphanumeric for a plain <c>"…"</c>, National for <c>N"…"</c> (§8.3.3.5),
/// Boolean for <c>B"…"</c> (§8.3.3.4) — the ONE literal node for all three (feedback_singular_pattern); the
/// category drives MOVE legality (§14.9.25.3 Table 16), relation-class checks, and store fills.</summary>
public sealed record BoundStringLiteral(string Value) : BoundOperand
{
    /// <summary>The literal's data category (default Alphanumeric — every pre-2002 site is untouched).</summary>
    public PicCategory Category { get; init; } = PicCategory.Alphanumeric;
}

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
public sealed record BoundAllLiteral(string Literal) : BoundOperand
{
    /// <summary>True when the literal is one or more digit characters — the shape of ISO §14.9.25.3 SR5's sole
    /// surviving figurative→numeric MOVE ("an ALL "literal" figurative constant (containing only digits) … to an
    /// integer numeric item"). The ONE definition both the binder's edition gates and the emitter's value/image
    /// split consult (feedback_singular_pattern).</summary>
    public bool IsDigitOnly => Literal.Length > 0 && Literal.All(c => c is >= '0' and <= '9');

    /// <summary>The literal's data category — always Alphanumeric today: <c>ALL N"…"</c>/<c>ALL B"…"</c>
    /// (§8.3.3.6.3 SR2) are grammar residue (figurativeConstant admits only ALL STRINGLIT/HEXLIT); the
    /// property exists so that leg lands on the <see cref="BoundStringLiteral.Category"/> shape.</summary>
    public PicCategory Category { get; init; } = PicCategory.Alphanumeric;
}

/// <summary>An operand the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundOperandError(string Feature) : BoundOperand;

// ── Boolean expressions (ISO §8.8.2; Phase-4 track (a) increment 2) — a SEPARATE value channel from the numeric
//    BoundExpr and the DISPLAY/MOVE BoundOperand: a boolean value IS a '0'/'1' string (D-B1), combined by the
//    B-AND/B-OR/B-XOR/B-NOT operators. It never enters the numeric channel (NumericRenderer) or the string
//    channel (OperandText) — the emitter routes it through BooleanRenderer over the runtime CobolBool. ─────────

/// <summary>A bound boolean expression (COBOLNET_DESIGN §11 / ISO §8.8.2).</summary>
public abstract record BoundBoolExpr;

/// <summary>A boolean literal <c>B"1010"</c>, decoded to its '0'/'1' bit string.</summary>
public sealed record BoundBoolLiteral(string Bits) : BoundBoolExpr;

/// <summary>A reference to a category-boolean data item (including a static ref-mod of one).</summary>
public sealed record BoundBoolRef(Place Place) : BoundBoolExpr;

/// <summary>The figurative <c>ALL B"…"</c> (and figurative ZERO, normalized to <c>ALL B"0"</c> at bind) — a
/// positionless pattern that materializes to the OTHER operand's length (ISO §8.3.3.6.4 GR2). <c>B-NOT ALL …</c>
/// constant-folds to the flipped pattern at bind (ALL is positionless).</summary>
public sealed record BoundBoolAll(string Bits) : BoundBoolExpr;

/// <summary>A binary boolean operation (<paramref name="Op"/> ∈ <c>'&amp;'</c> B-AND / <c>'|'</c> B-OR /
/// <c>'^'</c> B-XOR), positionwise with rule-9 right-zero-extension and rule-10 result length (§8.8.2).</summary>
public sealed record BoundBoolBinary(BoundBoolExpr Left, char Op, BoundBoolExpr Right) : BoundBoolExpr;

/// <summary>Boolean negation (B-NOT) — length preserved (ISO §8.8.2 rule 10).</summary>
public sealed record BoundBoolNot(BoundBoolExpr Operand) : BoundBoolExpr;

/// <summary>A boolean expression the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundBoolError(string Feature) : BoundBoolExpr;

/// <summary>A boolean expression used as a RELATION operand (ISO §8.8.4.2.2) — the ONE carrier that lets a
/// boolean expression sit in a <see cref="BoundRelational"/> beside another boolean operand (item↔item compares
/// ride the SAME BoundRelational + renderer branch, never a parallel node; feedback_singular_pattern).</summary>
public sealed record BoundBoolOperand(BoundBoolExpr Expr) : BoundOperand;

// ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound condition — a side-effect-free predicate tree (COBOLNET_DESIGN §11).</summary>
public abstract record BoundCondition;

/// <summary>A relational comparison <c>left op right</c> (<paramref name="Op"/> is the mapped C# operator).</summary>
public sealed record BoundRelational(BoundOperand Left, string Op, BoundOperand Right) : BoundCondition;

/// <summary>A logical combination (<c>&amp;&amp;</c> / <c>||</c> / <c>^</c>) of sub-conditions.</summary>
public sealed record BoundLogical(string Op, IReadOnlyList<BoundCondition> Operands) : BoundCondition;

/// <summary>Logical negation.</summary>
public sealed record BoundNot(BoundCondition Operand) : BoundCondition;

/// <summary>A simple boolean condition (ISO §8.8.4.3): a boolean expression of length 1 used as a condition —
/// true iff its value is boolean 1 (GR1). Negation composes via <see cref="BoundNot"/>.</summary>
public sealed record BoundBooleanCondition(BoundBoolExpr Expr) : BoundCondition;

/// <summary>A level-88 condition-name membership test over its (already-resolved) conditional variable place.</summary>
public sealed record BoundCondition88(Place Parent, Condition88 Condition) : BoundCondition;

/// <summary>A sign condition: <paramref name="Expr"/> IS [NOT] {POSITIVE | NEGATIVE | ZERO}.</summary>
public sealed record BoundSignCondition(BoundExpr Expr, char Kind, bool Negated) : BoundCondition;   // Kind: P/N/Z

/// <summary>A class condition: <paramref name="Operand"/> IS [NOT] {NUMERIC | ALPHABETIC | ALPHABETIC-UPPER |
/// ALPHABETIC-LOWER} (ISO §8.8.4.1.4). <paramref name="ClassKind"/> ∈ {N, A, U, L}.</summary>
public sealed record BoundClassCondition(BoundOperand Operand, char ClassKind, bool Negated) : BoundCondition;

/// <summary>A USER-DEFINED class condition (ISO §8.8.4.1.4 with a SPECIAL-NAMES class-name, §12.3.7): true when
/// the operand consists entirely of <paramref name="Members"/> (the clause's literals expanded at bind time).</summary>
public sealed record BoundUserClassCondition(BoundOperand Operand, string Members, bool Negated) : BoundCondition;

/// <summary>A condition the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundConditionError(string Feature) : BoundCondition;

// ── Statements ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound statement.</summary>
public abstract record BoundStatement;

/// <summary>An unsupported / unresolved statement — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundUnsupported(string Feature) : BoundStatement;

/// <summary><c>STOP RUN</c> / <c>GOBACK</c> (this slice: both unwind the paragraph chain).</summary>
public sealed record BoundStop : BoundStatement;

/// <summary>STOP literal (X3.23-1985 §14 Format 2, deleted 2002; edition-gated ≥2002 by the validator): "the
/// literal is communicated to the operator" and, on resume, "execution continues with the next executable
/// statement". The greenfield realization (implementor latitude on the operator interaction): write the
/// literal to the operator channel — stderr, never the program's output stream — and continue immediately.
/// Replaces the silent bind-as-STOP-RUN mis-bind (the DEVLOG-578 latent bug; P2.6).</summary>
public sealed record BoundStopLiteral(string Text) : BoundStatement;

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

/// <summary><c>COMPUTE boolean-targets = boolean-expression</c> (ISO §14.9.8 Format 2). Each receiver is an
/// elementary boolean item; the stored value is resized to <paramref name="Gr3Width"/> = the number of boolean
/// positions in the LARGEST boolean ITEM referenced in the expression (GR3 — literal-only larger sides don't
/// count), left-aligned / right-zero-filled / right-truncated (§14.6.8.6). No ROUNDED, no SIZE ERROR (F2).</summary>
public sealed record BoundComputeBoolean(BoundBoolExpr Rhs, IReadOnlyList<Place> Targets, int Gr3Width) : BoundStatement;

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

/// <summary>A fixed pre/main/post statement group emitted in order — the carrier for bind-time desugars
/// that wrap ONE source statement in synthesized neighbors (first client: object-property references,
/// ISO §8.4.3.9.4 GR1–GR3 — the pre-GET / statement / post-SET triple over a compiler temp; deep-dive
/// D-P2). NOT a control-flow construct: no pc identity of its own, both backends render the children
/// consecutively; a child that transfers control (GO TO/EXIT) behaves exactly as if written in line.</summary>
public sealed record BoundSequence(IReadOnlyList<BoundStatement> Steps) : BoundStatement;

/// <summary><c>NEXT SENTENCE</c> (ISO §14.9.19 GR6 / §14.9.37 — archaic per Annex F.1, legal at every edition):
/// transfer to the implicit CONTINUE following the current sentence's separator period.</summary>
public sealed record BoundNextSentence : BoundStatement;

/// <summary><c>SET condition-name+ TO TRUE</c> — each names a level-88 whose first VALUE is stored into its
/// (already-resolved) parent place.</summary>
public sealed record BoundSetConditions(IReadOnlyList<(Place Parent, Condition88 Condition)> Sets) : BoundStatement;

/// <summary>SET data-pointer assignment (ISO §14.9.39 Format 4 — SET pointer TO {NULL | pointer};
/// Phase-4b increment 1): copy the NULL singleton or the source pointer into each target in order.
/// <paramref name="ToNull"/> ⇔ the sender is the NULL figurative (renders <c>ManagedPointer.Null</c>);
/// <paramref name="Address"/> ⇔ the sender is <c>ADDRESS OF identifier</c> (increment 2 — ONE node per job,
/// never a parallel SET-pointer node).</summary>
public sealed record BoundSetPointer(
    IReadOnlyList<Place> Targets, Place? Source, bool ToNull, BoundAddressOf? Address = null) : BoundStatement;

/// <summary><c>ADDRESS OF identifier</c> as a pointer VALUE (ISO §8.4.3.11 GR1; Phase-4b increment 2): for a
/// BASED item the value IS its implicit data-address pointer (§8.6.5 :8791); for a cell-backed record the
/// emitter renders <c>ManagedPointer.At(cell, classOffset)</c> over the item's forced/EXTERNAL storage cell.</summary>
public sealed record BoundAddressOf(DataItem Item);

/// <summary><c>SET ADDRESS OF based-item TO pointer</c> (ISO §14.9.39 Format 7; SR18 — the receiver shall be
/// BASED; GR12–13 — the address VALUE is assigned, a snapshot): <c>__addr_B = pointer</c>.</summary>
public sealed record BoundSetAddressOfBased(DataItem Based, Place Source) : BoundStatement;

/// <summary><c>SET pointer… {UP|DOWN} BY integer</c> (ISO §14.9.39 Format 10; 2002+): the address moves by
/// bytes (GR20 — character positions in this model); NULL → EC-DATA-PTR-NULL at runtime (GR18).</summary>
public sealed record BoundSetPointerUpDown(IReadOnlyList<Place> Targets, BoundExpr Amount, bool Down) : BoundStatement;

/// <summary><c>ALLOCATE</c> (ISO §14.9.3): form 1 — <paramref name="Chars"/> characters (GR1 rounds a
/// fractional request UP; GR2 ≤0 ⇒ NULL, no EC) RETURNING <paramref name="Returning"/> (SR2 — required with
/// CHARACTERS); form 2 — storage sized for the BASED <paramref name="Based"/> (GR3), its implicit pointer set
/// (GR4a) and <paramref name="Returning"/> also set when present (GR4b). <paramref name="Initialized"/>: GR6
/// binary-zero fill (form 1) / the GR7 INITIALIZE lowering (form 2).</summary>
public sealed record BoundAllocate(
    DataItem? Based, BoundExpr? Chars, bool Initialized, Place? Returning) : BoundStatement;

/// <summary><c>FREE pointer…</c> (ISO §14.9.15): per-operand left to right (GR2); each operand runs the GR1
/// three-way (release-and-null / NULL no-op / EC-STORAGE-NOT-ALLOC nonfatal, reported through the
/// TurnState-gated status block).</summary>
public sealed record BoundFree(IReadOnlyList<Place> Operands) : BoundStatement;

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
/// <paramref name="AlsoVaried"/>, a different-table index or data item, GR8) increments by 1.
/// <paramref name="FromStart"/> marks <c>SEARCH ALL</c> (Format 2): the initial index setting is IGNORED (GR9 —
/// the technique is implementor-specified; this implementation scans from occurrence 1, conformant for the
/// key-ordered tables Format 2 requires).</summary>
public sealed record BoundSearch(
    string IndexField, long Count, BoundSetTarget? AlsoVaried,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundSearchWhen> Whens,
    bool FromStart = false, string? DependCount = null) : BoundStatement;

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
/// plain (data) WRITE. <paramref name="Unsupported"/> set (loud) when the owning file's organization is unsupported.
/// <paramref name="AtEop"/>/<paramref name="NotAtEop"/> are the END-OF-PAGE / NOT END-OF-PAGE imperatives (ISO
/// §14.9.51 GR27b/GR28 — run after the SUCCESSFUL write, branching on the end-of-page condition; SR19 requires
/// the file to have a LINAGE clause).</summary>
public sealed record BoundWrite(FileModel File, Place Record, BoundOperand? From, BoundAdvancing? Advancing,
    string? Unsupported, IReadOnlyList<BoundStatement>? AtEop = null, IReadOnlyList<BoundStatement>? NotAtEop = null) : BoundStatement;

/// <summary><c>READ file [NEXT] [INTO x] [AT END …][NOT AT END …]</c> (ISO §14.9.30): a sequential read that
/// distributes the record image into the FD record (and, with INTO, MOVEs it to <paramref name="Into"/>). The AT END
/// / NOT AT END imperatives branch on the at-end condition.</summary>
public sealed record BoundRead(
    FileModel File, Place? Into, IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd, string? Unsupported) : BoundStatement;

/// <summary><c>REWRITE record [FROM x]</c> (ISO §14.9.35): replace the last-read record with the record area's image.</summary>
public sealed record BoundRewrite(FileModel File, Place Record, BoundOperand? From, string? Unsupported) : BoundStatement;

// ── Report Writer verbs (ISO §14.9.21 / §14.9.16 / §14.9.46; COBOLNET_REPORT_WRITER_DESIGN §5) ────────────────

/// <summary><c>INITIATE report-name…</c> (ISO §14.9.21): each report's counters/sum counters reset and the
/// report becomes active (GR1/GR4); a multi-name statement unrolls in written order (GR5).</summary>
public sealed record BoundInitiate(IReadOnlyList<ReportModel> Reports) : BoundStatement;

/// <summary><c>GENERATE {detail | report-name}</c> (ISO §14.9.16): detail reporting prints one instance of
/// <paramref name="Detail"/> after control-break/page-fit processing (GR1); a null detail is SUMMARY reporting
/// (GR2 — the report-name form, same processing with no detail printed).</summary>
public sealed record BoundGenerate(ReportModel Report, ReportGroupModel? Detail) : BoundStatement;

/// <summary><c>TERMINATE report-name…</c> (ISO §14.9.46): final control footings + report footing, report →
/// inactive (GR3); unrolls in written order (GR4); does NOT close the file (GR6).</summary>
public sealed record BoundTerminate(IReadOnlyList<ReportModel> Reports) : BoundStatement;

// ── The EC exception-condition model (ISO §14.6.13 / §14.9.29 / §14.9.33 / §14.9.49 F3;
//    COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12) ─────────────────────────────────────────────────────────────

/// <summary>The per-statement EC checking decision, computed at BIND time from the compile-time TurnState
/// (deep-dive D10 — bound nodes carry no parse context, so the line-anchored TURN fold happens in the binder and
/// its RESULT travels on the bound tree; the emitter renders guards from this record only).</summary>
/// <param name="Enabled">The enabled (level-3 exception-name, file) pairs RELEVANT to this statement's kind —
/// EC-SIZE-* for arithmetic, EC-I-O-* per referenced file connector, EC-OVERFLOW-* for STRING/UNSTRING,
/// EC-PROGRAM-* for CALL/CANCEL, EC-ARGUMENT-FUNCTION for intrinsic-bearing statements. Never empty (an empty
/// decision binds NO wrapper — the zero-scaffolding rule).</param>
/// <param name="WithLocation">The enabling TURN carried WITH LOCATION (§7.3.25.4 GR7) — the raise site then
/// records <paramref name="StatementName"/>/<paramref name="Location"/> into the last-exception state.</param>
/// <param name="StatementName">The uppercase statement name (§15.32.3 r2, Table 12).</param>
/// <param name="Location">The pre-rendered §15.30.3 r2 location string ("element; para[ OF section]; line").</param>
public sealed record EcStatementInfo(
    IReadOnlyList<(string Ec, FileModel? File)> Enabled,
    bool WithLocation,
    string StatementName,
    string Location);

/// <summary>A statement bound under ENABLED exception-condition checking (>>TURN … CHECKING ON in scope at its
/// line, §7.3.25.4 GR6): the emitter sets the statement EC context, emits <paramref name="Inner"/> with the
/// per-raise-point guards, and clears it. Absent wherever checking is off — checking-off emits NOTHING new.</summary>
public sealed record BoundEcChecked(BoundStatement Inner, EcStatementInfo Info) : BoundStatement;

/// <summary><c>RAISE EXCEPTION exception-name-1</c> (ISO §14.9.29; SR1 — level-3 only, validated at bind).
/// The TURN decision is baked in at bind time (§14.6.13.1.1: an exception condition is raised only when checking
/// is enabled): <paramref name="Enabled"/> false + nonfatal ⇒ the statement is a no-op (§14.6.13.1.4 first
/// sentence — "execution continues as if the exception did not occur"); false + fatal ⇒ the implementor-defined
/// §14.6.13.1.3 #8 case — this implementation terminates loudly (§1.4). The RAISE identifier-1 (exception object)
/// form binds loud until the OO wave.</summary>
public sealed record BoundRaise(
    string EcName, bool Fatal, bool Enabled, bool WithLocation, string Location) : BoundStatement;

/// <summary><c>RESUME AT {NEXT STATEMENT | procedure-name}</c> (ISO §14.9.33): unwinds the active declarative
/// via the runtime ResumeSignal; <paramref name="TargetPc"/> is the resolved NONdeclarative pc (SR3), or the
/// NextStatement sentinel (−2) — the raise site then falls through past the raising statement (GR2).</summary>
public sealed record BoundResume(int TargetPc) : BoundStatement;

/// <summary><c>SET LAST EXCEPTION TO OFF</c> (ISO §14.9.39 Format 13): clears the run-unit last exception
/// status (§14.6.13.1.1).</summary>
public sealed record BoundSetLastException : BoundStatement;

/// <summary>The bound RAISING phrase of GOBACK / EXIT PROGRAM (ISO §14.9.18.2 / §14.9.14.2 Format 2): either a
/// level-3 <paramref name="EcName"/> (with its catalog <paramref name="Fatal"/>ity and the bind-time TURN
/// <paramref name="Enabled"/> decision at the statement's line) or <paramref name="IsLast"/> (RAISING LAST
/// EXCEPTION — re-stages the current last exception status). The identifier (exception-object) form binds loud
/// until the OO wave.</summary>
public sealed record BoundRaising(string? EcName, bool IsLast, bool Fatal, bool Enabled,
    Place? ObjectSource = null);
// ObjectSource: the GOBACK/EXIT … RAISING identifier-1 leg (§14.9.18.3 SR4; the EC-OO wave) — exactly one
// of EcName / IsLast / ObjectSource is set. Objects are NOT TURN-gated (§7.3.25 takes names only), so the
// Enabled/Fatal fields are meaningless on this leg (the §14.6.13.1.5 activator rules decide fatality).

/// <summary>RAISE identifier-1 (ISO §14.9.29; §14.6.13.1.5 — the EC-OO wave): raise an exception OBJECT.
/// <paramref name="Source"/> null ⇔ SELF (renders <c>this</c>). NEVER fatal by itself (GR2): the F4
/// declarative runs if one matches, else execution continues with the next statement.</summary>
public sealed record BoundRaiseObject(Place? Source) : BoundStatement;
