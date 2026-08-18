// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

using CobolNet.Binding.Model;

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
    IReadOnlyList<BoundMethod>? Methods = null,
    IReadOnlyList<BoundDebugSubject>? DebugSubjects = null,
    // The exception-checking (Format-3) PERFORM handler pc-ranges (imp-2/3/4) appended above the main pc space
    // (ISO §14.9.28.4 GR17). F3HandlerBasePc is the first such pc (null when the unit has none — the byte-identity
    // gate: a non-F3 unit adds zero synthetic pcs and needs no fall-through wall); F3HandlerOwners[pc - base] is the
    // owning PERFORM's PerformId (the EXIT-PERFORM / handler-region context). Set by the pc-range synthesis wave.
    int? F3HandlerBasePc = null,
    IReadOnlyList<int>? F3HandlerOwners = null);

/// <summary>One <c>USE FOR DEBUGGING ON procedure-name / ALL PROCEDURES</c> subject procedure (X3.23-1985 debug
/// module; deleted 2002, absent ISO 2023 — modeled only at <c>--std 85</c>, VCR Table 7 row 7.17). The emitter
/// injects, at <paramref name="SubjectPc"/>'s dispatcher entry, a call that space-fills DEBUG-ITEM, sets
/// DEBUG-LINE (<paramref name="SourceLine"/>) / DEBUG-NAME (<paramref name="SubjectName"/>) / DEBUG-CONTENTS (the
/// transfer cause), then runs the debugging declarative body over the bounded pc range
/// [<paramref name="SectionStartPc"/>..<paramref name="SectionEndPc"/>]. A subject is always a NONdeclarative
/// procedure (pc ≥ EntryPc): a debugging declarative is never debugged, matching ALL PROCEDURES's exclusion of
/// the debugging sections themselves (DB101A "USE PROCEDURE NOT EXECUTED"). The data-name / file-name / cd-name
/// subject kinds and the SORT/MERGE-procedure cause taxonomy are staged (rejected COBOLNET1571 at bind).</summary>
public sealed record BoundDebugSubject(
    int SubjectPc,
    string SubjectName,
    int SourceLine,
    int SectionStartPc,
    int SectionEndPc);

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
    bool HasRaising,      // a GOBACK/EXIT … RAISING (§14.9.18 / §14.9.14)
    bool HasF3Perform = false)  // an exception-checking (Format-3) PERFORM (§14.9.28 F3 — installs __EcPerform + the frame stack)
{
    /// <summary>Any EC-model feature present (drives the group-level <c>_ecActive</c> gate). HasF3Perform is
    /// included so the int-returning <c>__RunUse</c> (the pc-range handler invoker the interceptor depends on) is
    /// emitted even for an F3 PERFORM whose imp-1 uses no OTHER EC feature (e.g. <c>PERFORM CONTINUE WHEN
    /// EC-BOUND-SUBSCRIPT CONTINUE END-PERFORM</c>) — §14.9.28.4.</summary>
    public bool Any => HasChecked || HasIoChecked || HasRaise || HasResume || HasF3 || HasEcFunctions || HasRaising || HasF3Perform;
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
public sealed record BoundParagraph(string CobolName, IReadOnlyList<IReadOnlyList<BoundStatement>> Sentences,
    int SourceLine = 0)
{
    // SourceLine: the paragraph's LAST executable statement's source line — the X3.23-1985 DEBUG-LINE value when a
    // debug subject is reached by sequential FALL THROUGH (the causing statement is the one that completed and fell
    // through; DB101A "FALL-THROUGH-TEST" pins it to the preceding "MOVE 0 TO RESULT-FLAG.", :403-407). 0 when the
    // debug facility is inactive / the paragraph is empty (never read then). VCR Table 7 row 7.17.
    /// <summary>All statements in order (sentence boundaries flattened) — for consumers that don't care.</summary>
    public IEnumerable<BoundStatement> Statements => Sentences.SelectMany(s => s);
}

// ── Numeric expressions (scale-tracked at render time by the backend) ──────────────────────────────────────────

/// <summary>A bound numeric expression — a tree of resolved operands and operators (no parse context).</summary>
[BoundNode]
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

/// <summary>A report VARYING counter read (ISO §13.18.64.4 GR3/GR4 — the per-repetition counter persists
/// through its occurrence and acts as a source item): a scale-0 integer, <paramref name="CsName"/> naming the
/// compose-local counter variable. Produced only by the report-section compose emission (a counter is
/// referable only within its entry, §13.18.64.3 SR2).</summary>
public sealed record BoundReportVaryingRef(string CsName) : BoundExpr;

/// <summary>An OCCURS DEPENDING table's CURRENT extent (ISO §13.18.38 GR8 — "the rules of the OCCURS clause for a
/// sending data item", which §15.50.4 r4b / §15.14.4 r2b invoke; kb/Work PB61): <paramref name="FixedWidth"/> plus
/// data-name-1's value × <paramref name="ElemWidth"/>, the value clamped to [<paramref name="MinOccurs"/>,
/// <paramref name="MaxOccurs"/>] with EC-BOUND-ODO set outside them (§13.18.38.4 GR7). A scale-0 integer,
/// runtime-sourced from <paramref name="Depending"/> (data-name-1's place). Produced only by the LENGTH /
/// BYTE-LENGTH variable-length-group builder; the backend renders <c>CobolTable.OdoExtent</c> — the SAME helper
/// the group's sending image slices with, so the two extents cannot disagree.</summary>
public sealed record BoundOdoExtent(Place Depending, int MinOccurs, int MaxOccurs, int FixedWidth, int ElemWidth) : BoundExpr;

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
    IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) : BoundExpr
{
    /// <summary>CHAR-NATIONAL / ORD-over-a-national-argument bound under a NON-native NATIONAL program collating
    /// sequence (an <c>ALPHABET … FOR NATIONAL</c> literal phrase; §15.16.4 / §15.70.4 r2) — the backend then
    /// passes the emitted <c>__COLLATE_NAT</c> table; when false the field does not even exist (the H5 twin).
    /// Never true together with <see cref="Collate"/> — each call reads exactly one class's sequence.</summary>
    public bool CollateNat { get; init; }

    /// <summary>A reference modification applied to this function's RESULT — <c>FUNCTION CURRENT-DATE (1:4)</c>,
    /// <c>FUNCTION UPPER-CASE("abc") (1:2)</c>, and their keyword-omitted twins (fix-queue PB8). ISO §8.4.3.1.2
    /// Format 3 composes an identifier from an identifier plus a reference-modifier, and §8.4.3.3.3 SR2 admits a
    /// function-identifier as identifier-1; §8.4.3.1.4 GR1 (f)→(g) fixes the order — the argument list binds to
    /// the name first, THEN the reference modifier applies to the identifier on its left.
    /// <para>⛔ A RIDER ON THIS NODE, NOT A WRAPPER AROUND IT, and that is a deliberate structural choice. The
    /// alphanumeric string channel is selected by pattern-matching <c>BoundComputedOperand { Expr:
    /// BoundIntrinsicCall }</c> at several sites (<c>OperandText.AsString</c>, the nested-argument visitor,
    /// <c>IntrinsicArgumentRules</c>, <c>EcBinder</c>); a wrapper node would have silently stopped matching at
    /// every one of them, and the failure mode is not a compile error but a DROPPED ref-mod — the exact silent
    /// wrong answer this fix exists to remove. The rider also keeps <see cref="ResultCategory"/> correct with no
    /// extra rule: §8.4.3.3.4 GR6 preserves class and category for the three categories SR2 admits (alphanumeric,
    /// boolean, national), so a ref-modified result has the SAME category as the unmodified one.</para>
    /// <para>Null for every unmodified call, which is nearly all of them. SR2 confines this to alphanumeric /
    /// boolean / national functions — all of which render through the ONE <c>IntrinsicRenderer.RenderString</c> —
    /// so exactly one emit site has to honour it, and a numeric-result call can never carry one.</para></summary>
    public RefModSpec? RefMod { get; init; }

    /// <summary>EXCEPTION-FILE / EXCEPTION-FILE-N with a file-connector-name argument (§15.28.4 r2 / §15.29.4 r2,
    /// COBOL-2023): the resolved FD <see cref="FileModel"/> the function reports the I-O status of. Non-null only
    /// for the arg form; the renderer passes its <c>FileKeyExpr</c> so the runtime reads the NAMED connector's
    /// status (not the last exception's). Null for the no-argument form and every other function.</summary>
    public FileModel? FileArg { get; init; }

    /// <summary>TRIM (§15.96.4): 0 = both leading and trailing (rule 3), 1 = LEADING (rule 1), 2 = TRAILING
    /// (rule 2) — the LEADING/TRAILING phrase keyword, extracted at bind time. Zero for every other function.</summary>
    public int TrimMode { get; init; }

    /// <summary>FIND-STRING (§15.37.2): the <c>LAST</c> phrase keyword — seek the LAST occurrence of argument-2
    /// (rule 1) rather than the first. False for every other function.</summary>
    public bool FindLast { get; init; }

    /// <summary>FIND-STRING (§15.37.2 / .4 rule 4): the <c>ANYCASE</c> phrase keyword — case-insensitive matching
    /// (as if both arguments were lowered per LOWER-CASE). False for every other function.</summary>
    /// <summary>The ANYCASE phrase was present — FIND-STRING's case-folded search (§15.37.4 r4) and the
    /// NUMVAL-C / TEST-NUMVAL-C case-folded currency match (§15.68.3 r4f) share the ONE flag.</summary>
    public bool Anycase { get; init; }

    /// <summary>SUBSTITUTE (§15.87.2): one mode flag per (argument-2, argument-3) pair — bit 0 = FIRST (rule 3.a),
    /// bit 1 = LAST (rule 3.b), bit 2 = ANYCASE (rule 5); 0 = replace ALL occurrences. <see cref="Args"/> holds
    /// [argument-1, from₁, to₁, from₂, to₂, …]; this list has one entry per pair. Null for every other function.</summary>
    public IReadOnlyList<int>? SubstituteModes { get; init; }

    /// <summary>FUNCTION LENGTH measured in BYTES over the argument's storage image (kb/Work PB61): §15.50.4 r6
    /// gives a DYNAMIC LENGTH elementary item its "current length … in bytes" — for a national item 2 × its
    /// character positions — where every other LENGTH runtime shape is a character-position count over the string
    /// image. The renderer's Length arm reads the storage channel when this is set.</summary>
    public bool LengthInBytes { get; init; }

    /// <summary>CONCAT (§15.18.4 r3): the returned value is ALPHABETIC — argument-1 is usage display and every
    /// argument is class alphabetic (a PIC A item, or a nested CONCAT carrying this same rider). The RESULT
    /// CATEGORY stays <see cref="PicCategory.Alphanumeric"/> — the deliberate PIC A fold (PicInfo) is not
    /// reopened, because this is per-CALL derived state, not a declarable category, and no §15.3 argument rule
    /// admits class alphabetic — so the rider carries Table 16's finer row to its ONE consumer
    /// (<c>Table16Operand</c>, whose IsAlphabetic axis already exists; fix-queue PB59 family 7 /
    /// RV-15.18.4-3). False for every other function.</summary>
    public bool ResultIsAlphabetic { get; init; }

    /// <summary>CONVERT (§15.19.2) source-format: 0 = ANY, 1 = ANUM/ALPHANUMERIC, 2 = HEX, 3 = NAT/NATIONAL.
    /// Zero for every other function (== ANY, unused there).</summary>
    public int ConvertSource { get; init; }

    /// <summary>CONVERT (§15.19.2) destination base-format: 1 = ANUM, 3 = NAT, 4 = BYTE. Zero for every other
    /// function.</summary>
    public int ConvertDest { get; init; }

    /// <summary>CONVERT (§15.19.2): the HEX destination modifier (§15.19.4 r2/r4). False for every other function.</summary>
    public bool ConvertDestHex { get; init; }

    /// <summary>MODULE-NAME (§15.65.2): the keyword selector — 0 = CURRENT (r7), 1 = ACTIVATING (r5/r6),
    /// 2 = NESTED (r8), 3 = STACK (r9), 4 = TOP-LEVEL (r10). Zero for every other function.</summary>
    public int ModuleNameKind { get; init; }
}

// ── General operands (DISPLAY / MOVE source / comparison) — render as string or number per context ─────────────

/// <summary>A bound operand usable where either a string image or a numeric value may be required.</summary>
[BoundNode]
public abstract record BoundOperand;

/// <summary>A non-numeric literal, already decoded to its character value. <paramref name="Category"/> carries
/// the literal's data category — Alphanumeric for a plain <c>"…"</c>, National for <c>N"…"</c> (§8.3.3.5),
/// Boolean for <c>B"…"</c> (§8.3.3.4) — the ONE literal node for all three (feedback_one_mechanism_per_job); the
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
    /// split consult (feedback_one_mechanism_per_job).</summary>
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
[BoundNode]
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

/// <summary>The four boolean shift operators (ISO §8.8.2, COBOL-2023): logical (fill boolean 0) or circular
/// (rotate), left or right.</summary>
public enum BoolShiftKind { Left, Right, LeftCircular, RightCircular }

/// <summary>A boolean shift/rotate (ISO §8.8.2 rule 8, COBOL-2023): shift <paramref name="Operand"/> by the
/// integer <paramref name="Count"/>. The result length equals the operand's length (rule 9 — the count contributes
/// no positions). NOTE the rule-7b context-sensitive precedence (a shift inheriting a preceding B-OR/B-XOR's
/// precedence) is a documented refinement — the grammar binds the shift tighter than B-AND (the unmixed default
/// case, which the Annex A Table A.2 oracle exercises).</summary>
public sealed record BoundBoolShift(BoundBoolExpr Operand, BoolShiftKind Kind, BoundExpr Count) : BoundBoolExpr;

/// <summary>A boolean expression the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundBoolError(string Feature) : BoundBoolExpr;

/// <summary>A boolean expression used as a RELATION operand (ISO §8.8.4.2.2) — the ONE carrier that lets a
/// boolean expression sit in a <see cref="BoundRelational"/> beside another boolean operand (item↔item compares
/// ride the SAME BoundRelational + renderer branch, never a parallel node; feedback_one_mechanism_per_job).</summary>
public sealed record BoundBoolOperand(BoundBoolExpr Expr) : BoundOperand;

// ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound condition — a side-effect-free predicate tree (COBOLNET_DESIGN §11).</summary>
[BoundNode]
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
public sealed record BoundCondition88(Place Parent, Condition88 Condition, bool CheckRangeInvalid = false) : BoundCondition;

/// <summary>An alphanumeric/national THRU-range membership test (the EVALUATE WHEN <c>lo THRU hi</c> form, and the
/// carrier for EC-RANGE-INVALID checking): <c>Left</c> is within [<c>Lo</c>, <c>Hi</c>] in the effective collating
/// sequence. When <paramref name="CheckInvalid"/> and <c>lo</c> collates after <c>hi</c> (§14.7.8 rule 2) the nonfatal
/// EC-RANGE-INVALID is set and the range is treated as empty — realized by the runtime <c>CobolString.ThruMember</c>
/// (the empty behaviour is already emergent from the inclusive-bound test, so only the EC-set is added).</summary>
public sealed record BoundRangeMembership(BoundOperand Left, BoundOperand Lo, BoundOperand Hi, bool CheckInvalid) : BoundCondition;

/// <summary>A sign condition: <paramref name="Expr"/> IS [NOT] {POSITIVE | NEGATIVE | ZERO}. <paramref
/// name="Format2Float"/> marks the ISO §8.8.4.7.3 Format 2 form — a bare (unparenthesized) standard
/// floating-point data-name — which tests the IEEE sign BIT (§8.8.4.7.4 GR2: +0.0 IS POSITIVE, −0.0 IS
/// NEGATIVE) rather than the Format-1 algebraic value.</summary>
public sealed record BoundSignCondition(BoundExpr Expr, char Kind, bool Negated, bool Format2Float = false) : BoundCondition;   // Kind: P/N/Z

/// <summary>A class condition: <paramref name="Operand"/> IS [NOT] {NUMERIC | ALPHABETIC | ALPHABETIC-UPPER |
/// ALPHABETIC-LOWER} (ISO §8.8.4.1.4). <paramref name="ClassKind"/> ∈ {N, A, U, L}.</summary>
public sealed record BoundClassCondition(BoundOperand Operand, char ClassKind, bool Negated) : BoundCondition;

/// <summary>A USER-DEFINED class condition (ISO §8.8.4.1.4 with a SPECIAL-NAMES class-name, §12.3.7): true when
/// the operand consists entirely of <paramref name="Members"/> (the clause's literals expanded at bind time).</summary>
public sealed record BoundUserClassCondition(BoundOperand Operand, string Members, bool Negated) : BoundCondition;

/// <summary>A condition the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundConditionError(string Feature) : BoundCondition;

/// <summary>A condition carrying the user-defined-function <paramref name="Activations"/> its
/// <paramref name="Inner"/> predicate consumes, for a CONDITIONALLY or REPEATEDLY evaluated window: each
/// activation runs ONCE PER EVALUATION of this condition — a function-identifier "references a temporary data
/// item whose value is determined when the function is referenced at runtime" and its arguments are evaluated
/// "at the beginning of the evaluation of the function-identifier" (ISO §8.4.3.2.4 GR1 :6963 / GR6a :6995), and
/// functions in conditions are evaluated "if and when the conditions containing them are evaluated"
/// (§8.8.4.13 r2). The binder attaches the statement-pending activations here instead of the once-per-statement
/// hoist at every such window: a PERFORM UNTIL / VARYING UNTIL condition (re-evaluated per iteration, §14.9.28),
/// a SEARCH / SEARCH ALL WHEN condition (per pass, §14.9.37), an EVALUATE selection-object term (per WHEN
/// consideration, §14.9.13), and a non-first AND/OR combined-condition operand (§8.8.4.13 r1 short-circuit).
/// The renderer emits an immediately-invoked <c>Func&lt;bool&gt;</c> so the activations execute exactly when the
/// C# condition text evaluates — including inside loop headers and short-circuited <c>&amp;&amp;</c>/<c>||</c>
/// chains. This is the ONE deliberate exception to the "side-effect-free predicate tree" contract above; the
/// activations are the SAME lowering the hoist uses (one mechanism).
/// <para>⚠ <paramref name="Activations"/> is <see cref="BoundStatement"/>, not <see cref="BoundCallProgram"/>,
/// because the statement-pending list it drains from carries BOTH pre-op kinds (see
/// <c>DataBinder.PendingPreOps</c>): a user-function activation AND a D18 function-bearing subscript's §15.4
/// temporary store (fix-queue PB17). The window rule is identical for both — §8.8.4.13 r2 governs "functions",
/// not one spelling of them — so widening the carrier is what makes the subscript case ride the machinery the
/// UDF case already proved, instead of a parallel one.</para></summary>
public sealed record BoundUdfEvaluated(IReadOnlyList<BoundStatement> Activations, BoundCondition Inner)
    : BoundCondition;

// ── Statements ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound statement.</summary>
[BoundNode]
public abstract record BoundStatement;

/// <summary>An unsupported / unresolved statement — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundUnsupported(string Feature) : BoundStatement;

/// <summary>The STOP RUN / GOBACK termination status phrase (ISO §14.9.42 / §14.9.18.2 — COBOL-2002 on STOP,
/// COBOL-2023 on GOBACK): <c>WITH {ERROR | NORMAL} STATUS [value]</c>. <paramref name="Error"/> selects the OS
/// error-vs-normal termination indication (§14.9.42.4 GR2/GR3 · §14.9.18.4 GR7/GR8); <paramref name="Value"/> is
/// the status VALUE passed to the operating system (§14.9.42.4 GR5 · §14.9.18.4 GR10), null when no <c>STATUS</c>
/// operand is given. On .NET the single observable is the process exit code (<see cref="Runtime.RunUnit.ExitStatus"/>
/// → <c>Environment.ExitCode</c>): the value wins when present, else ERROR ⇒ 1 / NORMAL ⇒ 0 (the documented
/// implementor mapping — <c>docs/CONFORMANCE.md</c> §4.2.16).</summary>
public sealed record TerminationStatus(bool Error, BoundExpr? Value);

/// <summary><c>STOP RUN [ WITH {NORMAL|ERROR} STATUS [value] ]</c> (ISO §14.9.42): terminate the run unit,
/// passing <see cref="Status"/> to the operating system as the process exit code. The edition gate
/// (StopRunStatus2002) reads the PARSE tree in the post-bind <see cref="Validation.VersionConformancePass"/>,
/// not this node.</summary>
public sealed record BoundStop(TerminationStatus? Status = null) : BoundStatement;

/// <summary>STOP literal (X3.23-1985 §14 Format 2, deleted 2002; edition-gated ≥2002 by the validator): "the
/// literal is communicated to the operator" and, on resume, "execution continues with the next executable
/// statement". The greenfield realization (implementor latitude on the operator interaction): write the
/// literal to the operator channel — stderr, never the program's output stream — and continue immediately.
/// Replaces the silent bind-as-STOP-RUN mis-bind (the DEVLOG-578 latent bug; P2.6).</summary>
public sealed record BoundStopLiteral(string Text) : BoundStatement;

/// <summary><c>DISPLAY</c> of a sequence of operands (each rendered as its display image). <paramref name="ToStdErr"/>
/// carries the UPON device routing (ISO §14.9.11.3 SR2 / §14.9.11.4 GR8): a mnemonic-name bound to the SYSERR
/// implementor device-name routes to the process standard-error stream; CONSOLE / SYSOUT and the no-UPON default use
/// the standard display device (standard output). Default false keeps the no-UPON emission byte-identical.</summary>
public sealed record BoundDisplay(IReadOnlyList<BoundOperand> Operands, bool NoAdvancing, bool ToStdErr = false) : BoundStatement;

/// <summary><c>MOVE source TO targets</c> (single sending operand).</summary>
public sealed record BoundMove(BoundOperand Source, IReadOnlyList<Place> Targets) : BoundStatement
{
    /// <summary>Per-target dispatch kinds, classified ONCE at construction by the single authority
    /// (<see cref="MoveClassifier"/>, P7 Step 7) — a computed record property, so EVERY construction (the
    /// binder's real MOVE and the emitter's synthetic implicit MOVEs alike) carries them; the emitter renders
    /// by kind and re-derives nothing.</summary>
    public IReadOnlyList<MoveKind> Kinds { get; } = MoveClassifier.Classify(Source, Targets);
}

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
[BoundNode]
public abstract record BoundPerformControl;
/// <summary>Run the body once.</summary>
public sealed record PerformOnce : BoundPerformControl;
/// <summary>Run the body <paramref name="Count"/> times.</summary>
public sealed record PerformTimes(BoundOperand Count) : BoundPerformControl;
/// <summary>Run the body until <paramref name="Until"/> (TEST BEFORE → while; <paramref name="TestAfter"/> → do/while).</summary>
public sealed record PerformUntil(BoundCondition Until, bool TestAfter) : BoundPerformControl;
/// <summary>PERFORM … UNTIL EXIT (ISO §14.9.28.4 GR11, COBOL-2023): an unconditional infinite loop (a condition
/// that never becomes true → <c>while(true)</c>). Escape is the programmer's responsibility — an inline loop by
/// EXIT PERFORM, an out-of-line loop by GOBACK/STOP RUN (NOTE 4).</summary>
public sealed record PerformForever : BoundPerformControl;

/// <summary>One VARYING/AFTER level of a PERFORM Format 4 (ISO §14.9.28): the induction variable (an index-name or
/// data item — SET-style target), its FROM initialization, BY augment (1 when the phrase is omitted, GR12), and
/// UNTIL condition. FROM/BY stay EXPRESSIONS — they are re-evaluated at every setting/augmenting operation and the
/// conditions at every test (GR12 item identification; changes inside the body have immediate effect).</summary>
public sealed record VaryingLevel(BoundSetTarget Var, BoundExpr From, BoundExpr By, BoundCondition Until);

/// <summary><c>PERFORM … VARYING v FROM f BY b UNTIL c [AFTER …]…</c> (ISO §14.9.28 Format 4, GR13): nested
/// induction loops, leftmost level outermost.</summary>
public sealed record PerformVarying(IReadOnlyList<VaryingLevel> Levels, bool TestAfter,
    bool CheckIndexRange = false) : BoundPerformControl;

/// <summary>An inline <c>PERFORM … END-PERFORM</c> (a real loop over a bound body).</summary>
public sealed record BoundInlinePerform(BoundPerformControl Control, IReadOnlyList<BoundStatement> Body) : BoundStatement;

/// <summary>An out-of-line <c>PERFORM p [THRU q] [control]</c> — the resolved pc range [<paramref name="StartPc"/>,
/// <paramref name="EndPc"/>] (inclusive; a single paragraph has StartPc == EndPc), run per the control via the G4
/// dispatcher (a recursive bounded <c>Dispatch(StartPc, EndPc)</c>).</summary>
// SourceLine (on the transfer nodes below): the source line of the transferring statement — the X3.23-1985
// DEBUG-LINE value when the transfer reaches a debug subject (VCR Table 7 row 7.17; the causing statement, DB101A —
// PERF-ITERATION-TEST pins the PERFORM line :611-617, GO-TO-TEST the GO TO line :482-489, on every iteration). 0
// when the debug facility is inactive (never read then).
public sealed record BoundOutOfLinePerform(int StartPc, int EndPc, BoundPerformControl Control, int SourceLine = 0) : BoundStatement;

/// <summary><c>GO TO p</c> — set the program counter to <paramref name="TargetPc"/> (ISO §14.9.20 Format 1).</summary>
public sealed record BoundGoTo(int TargetPc, int SourceLine = 0) : BoundStatement;

/// <summary><c>GO TO p1 p2 … DEPENDING ON sel</c> — transfer to <c>Targets[sel-1]</c>; out-of-range falls through
/// to the next statement (ISO §14.9.20 Format 2).</summary>
public sealed record BoundGoToDepending(BoundOperand Selector, IReadOnlyList<int> Targets, int SourceLine = 0) : BoundStatement;

/// <summary><c>EXIT PARAGRAPH</c> — transfer to the end of the current paragraph (fall through to the next).</summary>
public sealed record BoundExitParagraph(int SourceLine = 0) : BoundStatement;

/// <summary><c>EXIT SECTION</c> (ISO §14.9.14 Format 4, GR7) — transfer control to the unnamed empty paragraph
/// following the LAST paragraph of the current section (pc <paramref name="SectionEndPc"/> + 1), "preceding any
/// return mechanisms for that section". When the enclosing bounded dispatch was entered with its exit AT the section
/// end (a PERFORM SECTION / PERFORM … THRU the section end / SORT-or-USE procedure / the top-level end wall), that
/// section return mechanism fires — realized in the emitter by an explicit <c>return</c> when <c>__exitPc</c> equals
/// the section end (the mid-section fall-through the bounded dispatch's own <c>__atExit</c> tail-check cannot see).</summary>
public sealed record BoundExitSection(int SectionEndPc, int SourceLine = 0) : BoundStatement;

/// <summary><c>EXIT PERFORM [CYCLE]</c> — break (or continue, when CYCLE) the nearest inline PERFORM loop.</summary>
public sealed record BoundExitPerform(bool Cycle) : BoundStatement;

/// <summary>A no-op statement: bare <c>EXIT</c>, <c>CONTINUE</c>, or <c>EXIT PROGRAM</c> in the main program.</summary>
public sealed record BoundNop : BoundStatement;

/// <summary>CONTINUE AFTER arithmetic-expression-1 SECONDS (ISO §14.9.9, COBOL-2023): a timed pause.
/// <paramref name="Seconds"/> is the interval; a value below zero is forced to 0 (GR1a) and, when
/// <paramref name="CheckLessThanZero"/> (EC-CONTINUE-LESS-THAN-ZERO checking was enabled at this statement),
/// sets the nonfatal EC-CONTINUE-LESS-THAN-ZERO (GR1b) before continuing; otherwise execution suspends for the
/// interval (GR1). Fractional seconds truncate (implicit COMPUTE without ROUNDED).</summary>
public sealed record BoundContinueAfter(BoundExpr Seconds, bool CheckLessThanZero) : BoundStatement;

/// <summary>A fixed pre/main/post statement group emitted in order — the carrier for bind-time desugars
/// that wrap ONE source statement in synthesized neighbors (first client: object-property references,
/// ISO §8.4.3.9.4 GR1–GR3 — the pre-GET / statement / post-SET triple over a compiler temp; deep-dive
/// D-P2). NOT a control-flow construct: no pc identity of its own, both backends render the children
/// consecutively; a child that transfers control (GO TO/EXIT) behaves exactly as if written in line.</summary>
public sealed record BoundSequence(IReadOnlyList<BoundStatement> Steps) : BoundStatement;

/// <summary><c>NEXT SENTENCE</c> (ISO §14.9.19 GR6 / §14.9.37 — archaic per Annex F.1, legal at every edition):
/// transfer to the implicit CONTINUE following the current sentence's separator period.</summary>
public sealed record BoundNextSentence(int SourceLine = 0) : BoundStatement;

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
/// emitter renders <c>ManagedPointer.At(cell, classOffset)</c> over the item's forced/EXTERNAL storage cell.
/// <paramref name="OccursDisplacement"/> carries a SUBSCRIPTED operand's occurrence displacement —
/// <c>(idx − 1) × width [+ …]</c> character positions added to the item's class offset (the occurrences lie
/// end-to-end in the ONE cell image, §8.4.3.11 GR1 — the address OF THE OCCURRENCE); null for an
/// unsubscripted operand. It is the D10 transitional rendered-index carrier (see
/// <c>AccessPath</c>/<c>FixedTableSegment</c>) — a <c>BoundExpr</c> when PHASE 15 removes SUBSCRIPT mode.</summary>
public sealed record BoundAddressOf(DataItem Item, string? OccursDisplacement = null);

/// <summary><c>SET ADDRESS OF based-item TO pointer</c> (ISO §14.9.39 Format 7; SR18 — the receiver shall be
/// BASED; GR12–13 — the address VALUE is assigned, a snapshot): <c>__addr_B = pointer</c>.</summary>
public sealed record BoundSetAddressOfBased(DataItem Based, Place Source) : BoundStatement;

/// <summary><c>SET program-pointer… TO {NULL | program-pointer}</c> (ISO §14.9.39 Format 9; SR21 — both sides
/// category program-pointer; P10 Step 7): a straight carrier copy, the data-pointer Format-4 twin.</summary>
public sealed record BoundSetProgramPointer(IReadOnlyList<Place> Targets, Place? Source, bool ToNull) : BoundStatement;

/// <summary><c>SET program-pointer… TO ENTRY {literal | identifier}</c> (ISO §14.9.39 Format 9 with the
/// §8.4.3.13 program-address-identifier sender): resolve the named OUTERMOST program through the run-unit
/// ProgramTable at statement time. Exactly one of <paramref name="NameLiteral"/> (the decoded literal) /
/// <paramref name="NamePlace"/> (a runtime name read, GR1a) is set. Not locatable → GR4: the targets take
/// NULL and EC-PROGRAM-NOT-FOUND is set to exist (the emitter's checking-gated block).</summary>
public sealed record BoundSetEntry(IReadOnlyList<Place> Targets, string? NameLiteral, Place? NamePlace) : BoundStatement;

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
[BoundNode]
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

/// <summary>How SET Format 14 changes a dynamic table's current capacity (ISO §13.18.38 Format 4 / §14.9.39 GR29;
/// data-model D9).</summary>
public enum SetCapacityKind { To, UpBy, DownBy }

/// <summary><c>SET dynamic-capacity-register {TO | UP BY | DOWN BY} amount</c> (ISO §14.9.39 SET Format 14; the
/// COBOL-2014 OCCURS DYNAMIC feature, data-model D9). The register is a VIEW over its owning table, so the emitter
/// calls the table's <c>SetCapacity</c>/<c>CapacityUpBy</c>/<c>CapacityDownBy</c> (via <paramref name="Table"/>, the
/// whole-table access path) — raising or lowering the current capacity, seeding new occurrences (§8.5.1.9.5), clamped
/// to the minimum, and raising EC-FLOW-SEARCH if a SEARCH of that same table is active (GR31).</summary>
public sealed record BoundSetCapacity(AccessPath Table, BoundExpr Amount, SetCapacityKind Kind) : BoundStatement;

/// <summary>SET [SIZE OF] data-name TO n (ISO §14.9.39 Format 16, COBOL-2023): set the current length of the
/// dynamic-length elementary item at <paramref name="Target"/> to <paramref name="Amount"/> characters. Growing
/// space-fills the added positions (GR39); shrinking drops the trailing ones; a value above <paramref name="Limit"/>
/// (the LIMIT character count, −1 = unbounded) clamps, a negative value yields 0 (GR37/GR38). When
/// <paramref name="CheckStorage"/> (EC-STORAGE-NOT-AVAIL checking was enabled at this statement — captured from the
/// TurnState at bind time) the clamp/negative legs also set the nonfatal EC-STORAGE-NOT-AVAIL (GR37/GR38). A
/// self-identifying node — the VersionConformancePass bound-tree arm gates it (SetDynLengthSize2023) for both the
/// explicit SIZE OF form and the bare re-routed form.</summary>
public sealed record BoundSetSize(Place Target, BoundExpr Amount, int Limit, bool CheckStorage) : BoundStatement;

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
    bool FromStart = false, Place? DependItem = null, string? DynTable = null,
    bool CheckSearchIndex = false, bool CheckSearchNoMatch = false) : BoundStatement;

// ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ───────────────────────────────────────────────────────────────────

/// <summary>How a file is opened (ISO §14.9.25). Maps 1:1 to the runtime <c>FileOpenMode</c>.</summary>
public enum BoundOpenMode { Input, Output, Extend, IO }

/// <summary>How a closed file is finalized (ISO §14.9.7): a plain close, <c>WITH LOCK</c> (no reopen), or a
/// <c>REEL/UNIT</c> phrase (a no-op on a disk medium, leaves the file open).</summary>
public enum BoundCloseKind { Normal, WithLock, ReelUnit }

/// <summary>The record-lock phrase on a READ/WRITE/REWRITE (ISO §14.9.30 etc.): explicit WITH LOCK, WITH NO
/// LOCK (never lock), or IGNORING LOCK (read despite another connector's lock — READ only). None = the file's
/// effective LOCK MODE governs (AUTOMATIC locks on READ, MANUAL does not).</summary>
public enum BoundRecordLock { None, WithLock, WithNoLock, IgnoringLock }

/// <summary>A RETRY phrase (ISO §14.7.9): retry a locked operation N TIMES, FOR N SECONDS, or FOREVER. In the
/// single-run-unit model the n-TIMES count is a real bounded loop over the connector registry; SECONDS/FOREVER
/// are documented no-ops (no competing process ever releases — named residue).</summary>
public enum RetryKind { Times, Seconds, Forever }
public sealed record RetrySpec(RetryKind Kind, BoundExpr? Amount);

/// <summary><c>UNLOCK file [RECORD[S]]</c> (ISO §14.9.47): release all record locks this connector holds on the
/// file; always succeeds when the file is open (else I-O status 42).</summary>
public sealed record BoundUnlock(FileModel File, bool Records) : BoundStatement;

/// <summary><c>OPEN {INPUT|OUTPUT|I-O|EXTEND} file …</c> — each opened file with its mode (ISO §14.9.25). An
/// unsupported organization (relative/indexed in the sequential slice) carries a loud <paramref name="Unsupported"/>
/// reason so the file opens to a runtime not-implemented guard.</summary>
public sealed record BoundOpen(IReadOnlyList<(FileModel File, BoundOpenMode Mode, string? Unsupported)> Files) : BoundStatement
{
    /// <summary>An OPEN SHARING phrase (ISO §14.9.27), overriding each file's SELECT SHARING clause for this
    /// OPEN; null = use the file-control clause. Applies to every file in the statement.</summary>
    public SharingMode? SharingOverride { get; init; }

    /// <summary>An OPEN RETRY phrase (ISO §14.7.9), or null. The n-TIMES count bounds a re-attempt loop over
    /// the connector registry; SECONDS/FOREVER are single-run-unit no-ops (residue).</summary>
    public RetrySpec? Retry { get; init; }
}

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
    string? Unsupported, IReadOnlyList<BoundStatement>? AtEop = null, IReadOnlyList<BoundStatement>? NotAtEop = null) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.51 Format 1 — WITH LOCK / WITH NO LOCK), or None;
    /// on a sharing-active file WITH LOCK locks the record written (GR11), single locking releases the
    /// connector's prior lock (GR10).</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.51 GR16), or null.</summary>
    public RetrySpec? Retry { get; init; }
    /// <summary>The AFTER-ADVANCING phrase of a COBOL-2023 combined <c>WRITE … BEFORE ADVANCING … AFTER ADVANCING …</c>
    /// (ISO §14.9.51 GR25e/GR25f): when non-null, <see cref="Advancing"/> holds the BEFORE phrase and this holds the
    /// AFTER phrase, and the record is presented once at the current line then advanced by BOTH amounts (both after
    /// presentation). PAGE is forbidden in the combined form (SR17). Null = the classic single-phrase WRITE.</summary>
    public BoundAdvancing? AfterAdvancing { get; init; }
}

/// <summary><c>READ file [NEXT] [INTO x] [AT END …][NOT AT END …]</c> (ISO §14.9.30): a sequential read that
/// distributes the record image into the FD record (and, with INTO, MOVEs it to <paramref name="Into"/>). The AT END
/// / NOT AT END imperatives branch on the at-end condition.</summary>
public sealed record BoundRead(
    FileModel File, Place? Into, IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd, string? Unsupported) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.30 Format 1 — the GR7–GR12 lock rules are ALL-FORMATS
    /// rules, so they bind on the sequential organization too), or None.</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.30 GR9), or null.</summary>
    public RetrySpec? Retry { get; init; }
    /// <summary>ADVANCING ON LOCK (§14.9.30 GR22): skip-scan records locked by another connector.</summary>
    public bool AdvancingOnLock { get; init; }
}

/// <summary><c>REWRITE record [FROM x]</c> (ISO §14.9.35): replace the last-read record with the record area's image.</summary>
public sealed record BoundRewrite(FileModel File, Place Record, BoundOperand? From, string? Unsupported) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.35 — WITH LOCK / WITH NO LOCK; the GR11/GR12 lock
    /// rules are ALL-FORMATS rules), or None.</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.35 GR11), or null.</summary>
    public RetrySpec? Retry { get; init; }
}

// ── Report Writer verbs (ISO §14.9.21 / §14.9.16 / §14.9.46; COBOLNET_REPORT_WRITER_DESIGN §5) ────────────────

/// <summary><c>INITIATE report-name…</c> (ISO §14.9.21): each report's counters/sum counters reset and the
/// report becomes active (GR1/GR4); a multi-name statement unrolls in written order (GR5).</summary>
public sealed record BoundInitiate(IReadOnlyList<ReportModel> Reports) : BoundStatement;

/// <summary><c>GENERATE {detail | report-name}</c> (ISO §14.9.16): detail reporting prints one instance of
/// <paramref name="Detail"/> after control-break/page-fit processing (GR1); a null detail is SUMMARY reporting
/// (GR2 — the report-name form, same processing with no detail printed).</summary>
public sealed record BoundGenerate(ReportModel Report, ReportGroupModel? Detail) : BoundStatement;

/// <summary><c>SUPPRESS PRINTING</c> (ISO §14.9.45): inhibit the PRINTING of the current instance of the report
/// group named by the lexically-enclosing USE BEFORE REPORTING procedure (GR1/SR1). <paramref name="Report"/> is
/// that group's owning report, resolved at bind time (the target group is a static, lexical property — GR1). The
/// per-instance suppression itself is a RUNTIME effect (GR2): the emitted call sets a one-shot flag the report
/// engine consumes at the next group presentation, inhibiting print lines, page advance, NEXT GROUP, and
/// LINE-COUNTER changes (GR3 a–d) — but NOT sum-counter accumulation (GR7) or the end-of-group sum reset (GR2;
/// only PRESENT WHEN / OCCURS DEPENDING absence skips the reset, §13.18.54.4 GR10).</summary>
public sealed record BoundSuppress(ReportModel Report) : BoundStatement;

/// <summary><c>TERMINATE report-name…</c> (ISO §14.9.46): final control footings + report footing, report →
/// inactive (GR3); unrolls in written order (GR4); does NOT close the file (GR6).</summary>
public sealed record BoundTerminate(IReadOnlyList<ReportModel> Reports) : BoundStatement;

// ── The EC exception-condition model (ISO §14.6.13 / §14.9.29 / §14.9.33 / §14.9.49 F3;
//    COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12) ─────────────────────────────────────────────────────────────

/// <summary>The per-statement EC checking decision, computed at BIND time from the compile-time TurnState
/// (deep-dive D10 — bound nodes carry no parse context, so the line-anchored TURN fold happens in the binder and
/// its RESULT travels on the bound tree; the emitter renders guards from this record only).</summary>
/// <param name="Enabled">The enabled (level-3 exception-name, file, with-location) triples RELEVANT to this
/// statement's kind — EC-SIZE-* for arithmetic, EC-I-O-* per referenced file connector, EC-OVERFLOW-* for
/// STRING/UNSTRING, EC-PROGRAM-* for CALL/CANCEL, EC-ARGUMENT-FUNCTION for intrinsic-bearing statements. Never
/// empty (an empty decision binds NO wrapper — the zero-scaffolding rule). ⛔ <b>WithLocation is PER
/// (name, file) PAIR, never per statement</b> — §15.32.3 r1 / §15.30.3 r1 key the 63-spaces/one-space answer on
/// "the TURN directive … that enabled checking for THE EXCEPTION CONDITION associated with the last exception
/// status": one condition enabled WITH LOCATION must not make a sibling condition's raise record location
/// information (kb/Work R06 — the former per-statement bool did exactly that).</param>
/// <param name="StatementName">The uppercase statement name (§15.32.3 r2, Table 12).</param>
/// <param name="Location">The pre-rendered §15.30.3 r2 location string ("element; para[ OF section]; line").</param>
public sealed record EcStatementInfo(
    IReadOnlyList<(string Ec, FileModel? File, bool WithLocation)> Enabled,
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
    Place? ObjectSource = null,
    // kb/Work R07 — the RAISING statement's §15.32.3 r2 / §15.30.3 r2 operands, per-condition like BoundRaise's
    // (§7.3.25.4 GR7 keys them on the TURN governing THIS name at THIS line). StatementName is the Table 12
    // name: GOBACK, or EXIT (EXIT PROGRAM / FUNCTION / METHOD are formats of the EXIT statement).
    bool WithLocation = false, string? StatementName = null, string? Location = null);
// ObjectSource: the GOBACK/EXIT … RAISING identifier-1 leg (§14.9.18.3 SR4; the EC-OO wave) — exactly one
// of EcName / IsLast / ObjectSource is set. Objects are NOT TURN-gated (§7.3.25 takes names only), so the
// Enabled/Fatal fields are meaningless on this leg (the §14.6.13.1.5 activator rules decide fatality).

/// <summary>RAISE identifier-1 (ISO §14.9.29; §14.6.13.1.5 — the EC-OO wave): raise an exception OBJECT.
/// <paramref name="Source"/> null ⇔ SELF (renders <c>this</c>). NEVER fatal by itself (GR2): the F4
/// declarative runs if one matches, else execution continues with the next statement.</summary>
public sealed record BoundRaiseObject(Place? Source) : BoundStatement;
