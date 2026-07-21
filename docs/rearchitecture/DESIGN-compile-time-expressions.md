# DESIGN — Compile-time expression evaluation (§7.3.6 / §7.3.7 / §7.3.8)

> **Status: DESIGN (in implementation, P13 Wave D).** Canonical deep-dive for the ONE shared compile-time
> expression evaluator (frontend conditional-compilation stage + CONSTANT-entry binder), the ONE boolean
> precedence resolver (compile-time + runtime), and the ANTLR grammar that parses every compiler-directive
> expression. Design SSOT for review-ledger item **C2** (`PHASE-13-plan-vs-spec-review.md §24`). Keep CURRENT
> (describes the compiler as built); do not narrate the doc's own revision history — that belongs in `DEVLOG.md`.

## 1. Scope and the defect being closed

A *compile-time expression* is an arithmetic (§7.3.6), boolean (§7.3.7), or constant-conditional (§7.3.8)
expression evaluated by the compiler, never at run time. Admitted in:

| Consumer | Spec | Operand kinds |
|---|---|---|
| `>>DEFINE cv AS …` | §7.3.11 | arithmetic-expr, boolean-expr, literal, PARAMETER, OFF |
| `>>EVALUATE …` / `>>WHEN …` | §7.3.13 | arithmetic-expr, boolean-expr, literal (subject/object/THRU range) |
| `>>IF cce` / `>>ELSE` / `>>END-IF` | §7.3.16 → §7.3.8 | constant-conditional-expression |
| `>>DISPLAY …` | §7.3.12 | arithmetic-expr, boolean-expr, literal, PARAMETER (listing sink) |
| `01 c CONSTANT AS arithmetic-expr` | §13.10.4 GR4 | arithmetic-expr (numeric only) |

**The defect (ledger C2, MAJOR — silent wrong value).** Two unrelated code paths: the **binder**
(`DataBinder.Constants.cs EvalConstExpr`) had a complete, battery-tested §7.3.6 arithmetic evaluator, reachable
only from CONSTANT-entry binding (numeric only). The **frontend** (`ConditionalCompilationProcessor`) resolved
each directive operand by a **single token** — `>>DEFINE X AS 1 + 2` bound `X = 1`, boolean operands were never
evaluated, no diagnostic raised.

**Mandate.** ONE compile-time expression evaluator shared by both consumers (the C2 verifier correction: "*lift/
reuse that evaluator rather than build a new one*"). ONE parser — ANTLR — for all directive-expression syntax.
ONE boolean precedence resolver shared by the compile-time and runtime paths. No operand silently mis-bound;
anything unrepresentable is rejected **loudly**. No deferrals.

## 2. Two jobs, cleanly separated

The conditional-compilation stage (§7.2 text-manipulation) has two different jobs:

1. **Line selection (NOT parsing).** Walking `>>IF/>>ELSE/>>END-IF/>>EVALUATE/>>WHEN` nesting to decide which
   physical lines survive. "text-1/text-2" may be any source lines — including un-expanded `COPY` and, in omitted
   branches, non-COBOL — so it MUST be a pre-parse text stage (§7.2); the main grammar cannot own it. Stays a
   small line-inclusion state machine in `ConditionalCompilationProcessor`.
2. **Expression / condition parsing + evaluation (100% ANTLR).** Every `>>DEFINE` operand, `>>IF` cce, and
   `>>EVALUATE`/`>>WHEN` operand is **fragment-parsed by ANTLR** and **evaluated by the ONE shared evaluator**
   over the parse tree. There is no hand-rolled tokenizer or condition parser — the ANTLR grammar is the single
   source of truth for directive-expression syntax.

```
   line-inclusion state machine (text selection)
        │  hands each directive's expression/cce TEXT to ↓
        ▼
   ANTLR fragment parse (CobolLexer + CobolParserCore, DEFAULT mode, ZeroTokenRewriter applied)
        │  → compileTimeOperandFragment / constantConditionalExpressionFragment tree
        ▼
   CompileTimeExpressionEvaluator (walks CobolParserCore.*Context; boolean via BooleanExpressionResolver)
        │  injected: name resolver · code-preserving diag sink · operand-source clause · decimalPointIsComma
        ▼
   CtValue (numeric / alphanumeric / national / boolean) — or a loud diagnostic, never a wrong value
```

## 3. Assembly layering (done)

The evaluator must live in **Frontend** so both callers reach it (`Editions ← Frontend ← Compiler`; Frontend
cannot reference Compiler). Its lexical dependencies were relocated down accordingly — the singular lexical
utilities, now reachable by both layers:

* **`CobolLiteral`** (the ONE literal codec) relocated Compiler → `Frontend/Common` (namespace `CobolNet.Common`
  unchanged; all Compiler `using`s still resolve). **Done, build-verified.**
* **`NumericLiteral.Normalize`** — the §12.3.7 GR14a numeric-literal normalizer extracted to `Frontend/Common`;
  `DataBinder.NormalizeNumericLiteral` delegates to it and keeps emitting COBOLNET0895 byte-identically. **Done,
  build-verified.**

`PicCategory` stays in Compiler — the evaluator uses its own `CtCategory`; the binder adapts at the call boundary.

## 4. The ANTLR grammar (one source of truth for syntax)

Isolated fragment entry rules, reachable only from the frontend fragment-parse (referenced by nothing in
`compilationUnit` — zero blast radius, the `functionArgListFragment` precedent). They reuse existing operand
sub-rules — no duplicated expression grammar:

```antlr
compileTimeOperandFragment : compileTimeOperand EOF ;
compileTimeOperand
    : {boolExprAhead()}? booleanExpression      // a genuine boolean expression (B-operator/BOOLLIT present)
    | arithmeticExpression                       // numeric operand (single numeric literal too — GR5 in eval)
    | nonNumericLiteral                          // string / national / hex literal operand
    ;
constantConditionalExpressionFragment : constantConditionalExpression EOF ;
constantConditionalExpression : cceOr ;
cceOr      : cceAnd ( OR cceAnd )* ;
cceAnd     : cceNot ( AND cceNot )* ;
cceNot     : NOT cceNot | ccePrimary ;
ccePrimary : LPAREN constantConditionalExpression RPAREN
           | definedCondition
           | cceRelationOrBoolean ;
definedCondition : cobolWord IS? NOT? DEFINED ;                       // DEFINED: primed-lexer token (below)
cceRelationOrBoolean
    : {boolExprAhead()}? booleanExpression                            // §8.8.4.3 simple boolean condition (len-1)
    | compileTimeOperand ( IS? NOT? comparisonOperator compileTimeOperand )? ;   // §8.8.4.2 relation
```

* **Operand-kind disambiguation** uses the existing `boolExprAhead()` predicate (the mechanism the source
  `primaryCondition` rule already uses): `booleanExpression` is entered only when a real B-operator/BOOLLIT is
  present; otherwise arithmetic (incl. a single numeric literal) or a non-numeric literal. The evaluator
  dispatches on **which operand sub-node parsed**, not a token guess — necessary because `booleanExpression →
  valueOperand` would otherwise match every arithmetic/non-numeric operand.
* **`DEFINED`** is not reserved in the source language — a token only inside the fragment via a primed lexer flag
  `PrimeDirectiveExpr()` (`DEFINED : {_primeDirectiveExpr}? 'DEFINED' ;`), the `PrimeFunctionArgs()` pattern.
  Zero global blast radius.
* **Relops** reuse `comparisonOperator`; the non-numeric `=`/`<>` restriction (§7.3.8.2 SR1a.2) is enforced in
  the evaluator. Abbreviated combined relations are not admitted (§7.3.8.2 SR1d).

### 4.1 Fragment-parse mechanics (identical lexing to the main parse)

* **DEFAULT lexer mode** — NOT `PrimeFunctionArgs`: §7.3.6 has no argument-juxtaposition, so `1 - 2` is
  subtraction.
* **`PrimeDirectiveExpr()`** — a lexer flag (the `PrimeFunctionArgs()` pattern) with two effects specific to the
  directive-expression context: it makes `DEFINED` a token (context-sensitive — reserved nowhere else), and it
  makes every `(` a grouping `LPAREN` (subscript mode is never pushed). The second is required: the subscript-vs-
  grouping lexer decision treats a `(` after any word that *could* be a data-name as a subscript, and the boolean
  operators (`B-AND` etc.) are legal data-names below 2023 — so without this flag `A B-AND (…)` mis-lexes the
  parenthesized group in SUBSCRIPT mode (confirmed by token dump). Directive operands never subscript, so every
  `(` is unambiguously a group. (The same latent subscript-vs-grouping ambiguity affects `(` after a boolean
  operator in the *main* parse — a pre-existing runtime `COMPUTE` Format-2 limitation, tracked separately.)
* **`ZeroTokenRewriter`** applied to the fragment stream (as `Frontend.LexAndParse`), so figurative `ZERO` in an
  arithmetic operand becomes `ZERO_ARITH`.
* **Edition** `EditionInfo.Of(EditionInfo.Latest)` (not literal 2023) — the operand parses at the newest edition
  (the whole-`>>`-facility introduction gate below 2002 is ledger C15). Recorded coupling: `Latest` also relaxes
  fixed-point digit capacity (§8.3.1.2), closed with C15 when the real edition is threaded.
* An error-flag listener (the `FunctionArgFragment.SyntaxErrorFlag` pattern) turns any syntax error into a loud
  `COBOLNET1619`; a partial parse is never evaluated.

## 5. The shared evaluator API — GR5/GR3 owned at the public boundary

```csharp
public sealed class CompileTimeExpressionEvaluator(
    Func<string, CtValue?> resolveName,     // a name → its bound value, or null if undefined
    ICtDiagnostics diag,                     // CODE-preserving sink (§5.2) — not a bare Action<string>
    CtOperandVocabulary vocab,               // per-consumer operand-source clause / noun (§5.2)
    bool decimalPointIsComma)                // §12.3.7 GR14a normalization (binder: real; frontend: false, §5.3)
{
    // Public operand boundary — applies §7.3.11.4 GR5 reclassification + §7.3.6.3 GR3 truncation ITSELF.
    public CtNumber? EvaluateArithmeticOperand(CobolParserCore.ArithmeticExpressionContext e, string where);
    // A boolean operand → its bit string (via BooleanExpressionResolver, §6). A shift count goes through
    // EvaluateArithmeticOperand, so it is GR3-truncated and rule-5 integer-validated.
    public BitString? EvaluateBoolean(CobolParserCore.BooleanExpressionContext e, string where);
}
public readonly record struct CtNumber(bool WasSingleLiteral, decimal Value);   // GR3-truncated unless WasSingleLiteral
```

* **GR5 + GR3 live INSIDE `EvaluateArithmeticOperand`.** The raw-`decimal` recursion (`EvalArith`, the lift of
  `EvalConstExpr`) stays private — intermediates correctly un-truncated (§7.3.6.3 GR1). At the boundary: a single
  numeric literal (private `SoleNumericLiteral` probe) is kept exact (GR5 / §13.10.3 SR1 — `AS 0.25` → `0.25`);
  otherwise the final result is truncated to its integer part (GR3 / INTEGER-PART §15.49). No consumer re-does
  this — the probe/truncate rule lives in one place, not copied at each operand site — and the boolean shift
  count is correct because it calls this boundary.

### 5.1 Arithmetic semantics (§7.3.6) — lifted from the binder

The private recursion is the existing `EvalConstExpr`, unchanged in logic: `+ - * /` and unary sign over the
grammar precedence tiers (§8.8.1/§7.3.6.3 GR1); SR1a exponentiation reject; SR1b operand-is-fixed-point-literal-
or-numeric-name (floating-point/E-form rejected); SR1c div-by-zero reject; SR2 intermediates ride .NET
`System.Decimal` (96-bit, 28–29 significant digits — **not** IEEE-754 decimal128; the lifted overflow message +
the V41 `CONFORMANCE.md §3` note are corrected accordingly); overflow reported, never wrapped.

### 5.2 Diagnostics — code-preserving, per-consumer citations

`ICtDiagnostics.Report(CtDiagCode code, string message)` preserves the diagnostic CODE (a bare `Action<string>`
sink would drop it): the binder maps each `CtDiagCode` to its `DiagnosticCatalog` descriptor (COBOLNET0895 GR14a,
the `ConstantEntryRule` messages) → byte-identical binder codes; the frontend maps to **COBOLNET1619**
(directive-expression violation). `CtOperandVocabulary` supplies the per-consumer noun + governing citation so no
message mis-cites: the evaluator's shared text cites only the shared §7.3.6.2 SR1a/b/c and §8.8.2; the operand-
source clause ("previously defined numeric **constant-name**, §13.10.3 SR2/GR1" for the binder; "previously
defined numeric **compilation variable**, §7.3.11.4 GR1" for the frontend) is injected. The binder's rejection
text may differ by that one clause from today (CONSTANT goldens updated same commit); the code is unchanged.

### 5.3 Numeric-literal normalization

The one chokepoint is `CobolNet.Common.NumericLiteral.Normalize` (Frontend, §3) — §12.3.7 GR14a. The binder
passes its real `DecimalPointIsComma` and routes the issue to COBOLNET0895; the **frontend passes `false`** —
directives are processed before SPECIAL-NAMES is bound (§7.2), so a directive numeric literal is always
dot-decimal (a stated, spec-grounded limitation).

## 6. Boolean semantics (§7.3.7 → §8.8.2) — complete, via ONE shared precedence resolver

A compile-time boolean value is a **bit string** — an immutable `BitString` with **value equality**
(`IEquatable`, length-sensitive) so SR2 redefinition and cce `=`/`<>` compare correctly. Operands: boolean
literals `B"1010"` (decoded via `CobolLiteral`), figurative `ZERO`/`ALL B"…"`, grouped sub-expressions,
previously-defined boolean names.

**Precedence is implemented correctly, including the context-inherited shift precedence (rule 7b), in ONE shared
mechanism.** A CFG cannot express context-dependent precedence, so `BooleanExpressionResolver.Resolve<T>`
(Frontend) flattens a `booleanExpression` into its lexical operand/operator sequence and precedence-climbs:

* Binary precedence `B-AND`(3) > `B-XOR`(2) > `B-OR`(1); `B-NOT` is the unary factor level (tightest, rule 7b
  1st); parentheses recurse as a fresh level (rule 7a); equal precedence left-to-right (rule 7c).
* A **shift** takes the precedence of the operator immediately preceding it in the sequence, or `B-AND` if none
  (rule 7b tail). So `A B-AND B B-SHIFT-L 2` → `(A B-AND B) B-SHIFT-L 2`; `A B-OR B B-SHIFT-L 2` →
  `(A B-OR B) B-SHIFT-L 2`; `A B-SHIFT-L 2 B-AND C` → `(A B-SHIFT-L 2) B-AND C`. Verified by shunting-yard trace.
* `Resolve<T>` is **generic over the combine operations** (leaf / not / binary / shift callbacks), so the SAME
  grouping serves the compile-time evaluator (`T = BitString`, folds) and the runtime `COMPUTE` Format-2 boolean
  binder (`T = BoundBoolExpr`, builds). This is the singular fix.

**This removes the prior `COBOLNET1569` reject** in `ConditionBinder` (`ShiftMixedWithBinary`), which refused the
legal mixed shift-with-binary form and told the user to parenthesize — a conformance gap that rejected valid
source. `ConditionBinder`'s tier-walk (`BindBoolExpr/Xor/And/Shift`) is refactored onto `Resolve<T>`; the mixed
form is now accepted and evaluated per rule 7b. Existing COBOLNET1569 tests flip from reject to accept-and-verify.

Operator/operand semantics (§8.8.2):

* **`B-NOT`** — complement, length preserved (`ALL` folds — flip the pattern).
* **Binary `B-AND`/`B-OR`/`B-XOR`** (rules 9/10) — bit-by-bit from the left; unequal length ⇒ shorter
  right-extended with boolean zeros; result length = the larger operand; zero-length ⇒ zero-length (rule 9
  NOTE 2).
* **Shift `-L/-R/-LC/-RC`** (rule 8) — second operand is an **integer operand**: evaluated via
  `EvaluateArithmeticOperand` (GR3-truncated) and **rule-5-validated** (a fractional value rejected); `count==0`
  identity; a **negative** count rejected (§8.8.2 defines only counts ≥ 1). Logical (zero-fill) vs circular (wrap);
  result length = first operand; `count ≥ length` degenerates correctly.
* **Rule 4/5 adjacency** — both binary operands `ALL literal`, or an `ALL literal` first shift operand, rejected
  (reuse COBOLNET1511 in the binder; COBOLNET1619 in the frontend).
* **Figurative operand length (compile-time rule).** A figurative `ZERO`/`ALL literal` operand takes the **other
  operand's length** in a binary op; a standalone figurative boolean value, or a binary op with both operands
  figurative, has indeterminate length and is rejected. Documented + unit-tested.

## 7. The constant-conditional-expression (§7.3.8) — evaluated over the ANTLR tree

`EvaluateCce(constantConditionalExpression)` walks the cce tree:

* **`cceOr`/`cceAnd`/`cceNot`** — logical combination (§8.8.4.9); both sides evaluated **unconditionally** (a
  formation error in any operand is always reportable per §7.3.8, regardless of branch truth). Test-pinned.
* **`definedCondition`** — `IS [NOT] DEFINED` per §7.3.8.4.4.
* **Relation** (§8.8.4.2 / §7.3.8.2 SR1a) — evaluate both operands to `CtValue`s: **SR1a.1** reject a
  category mismatch; **SR1a.2** for non-numeric operands only `=`/`<>` are valid; **comparison** (§7.3.8.3 GR2)
  numeric by value, non-numeric by binary character/bit value, length-sensitive (unequal ⇒ not equal), no
  collating.
* **Bare boolean condition** (§8.8.4.3) — enforce SR1 (length 1, else reject) + GR1 (true iff the single bit is
  `1`); leading `NOT` per GR2. The bit-string→truth bridge.

## 8. EVALUATE directive rules (§7.3.13)

`>>EVALUATE` Format 1/2 run in the line state machine; operands via the shared evaluator; formation rules
enforced: **SR11** all subjects/objects same category; **SR12** THROUGH ⇒ every subject/object numeric; **GR4**
selection — without THRU `subject = object`, with THRU the inclusive numeric range `[object, object3]` (GR4b);
Format 2 evaluates each WHEN's cce (§7.3.8). Single-numeric-literal reclassification (GR2) is automatic (§5).

## 9. Compilation-variable value model

```csharp
enum CtCategory { Numeric, Alphanumeric, National, Boolean }
sealed record CtValue(CtCategory Category, decimal Number, string Text, BitString? Bits);
```

Member-wise record equality is **replaced by a hand-written `Equals`** dispatching on `Category` (Numeric →
`Number` only, so `AS 1` / `AS 01` / `AS 1.0` are the same value and SR2 does not fire on spelling;
Alnum/National → `Text`; Boolean → `Bits` value-equality). `AS PARAMETER` (GR4, landed) and the SR2/COBOLNET1618
redefinition check (landed) use this model.

## 10. Legacy-oracle safety

`ConditionalCompilationProcessor.Process` has two callers: greenfield `Frontend.cs` and the legacy differential
oracle `Compilation.cs:345` (frozen until G8). The new ANTLR evaluation path is **greenfield-only**, selected by
an explicit parameter the greenfield caller sets; the legacy caller keeps its exact current behavior (frozen-
oracle doctrine — compile-time expressions are a 2002+ feature the 85-era oracle never needs). **Pre-check:** grep
the legacy differential corpus for any `>>` directive with a compile-time operand (expected none — 85 predates
conditional compilation). **Gate:** the full legacy guard (`scripts/guard.sh`) `=== ALL GREEN ===` before commit
(not guard-fast). The runtime `ConditionBinder` rule-7b refactor (§6) is guarded the same way; its COBOLNET1569
tests flip to accept-and-verify.

## 11. Test plan (ships in the change set)

* **Unit — `BooleanExpressionResolver`**: rule-7b groupings (`A B-AND B B-SHIFT-L 2`, `A B-OR B B-SHIFT-L 2`,
  `A B-SHIFT-L 2 B-AND C`, isolated shift, consecutive shifts) via both the BitString and BoundBoolExpr
  instantiations.
* **Unit — shared evaluator**: arithmetic (`2+3*4`, `(2+3)*4-6/2`, unary sign, div-by-0, `**` reject, non-literal
  reject, GR5 `0.25` no-truncation, GR3 truncation); boolean (`B-AND`/`B-OR`/`B-XOR` bit results, unequal-length
  extension, `B-NOT`, shift logical + circular L/R, `count≥length`, fractional/negative-count reject,
  `ALL`-adjacency reject, figurative-length rule).
* **Unit — frontend directive** (extend `ConditionalCompilationDefineTests`): `>>DEFINE X AS 1 + 2` ⇒ 3;
  `>>EVALUATE 1 + 1` selects `WHEN 2`; `>>IF A + 1 = B`; boolean DEFINE + `>>IF`; `>>IF NAME = "ABC"`;
  `A IS NOT = 1`; `((A = 1))`; category-mismatch reject; THRU-non-numeric reject; loud COBOLNET1619; existing
  PARAMETER/OFF/OVERRIDE/SR2-1618 still green.
* **Runtime COMPUTE-F2** — the mixed shift-with-binary forms now accepted and evaluated per rule 7b (was
  COBOLNET1569); goldens for the grouping.
* **Conformance** — a 2002 `>>DEFINE`/`>>IF`/`>>EVALUATE` program (arithmetic + boolean operands), `.out` golden.
* **Regression** — CONSTANT-entry goldens (`constant_entry.cob`) for the operand-source-clause wording;
  characterization; full legacy guard green.
