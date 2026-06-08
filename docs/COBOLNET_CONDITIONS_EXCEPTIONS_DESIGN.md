# COBOL.NET — Conditions & Exception Model (subsystem design)

> **Status: DESIGN / decision-complete.** The implementation-ready design for the *conditions-exceptions*
> subsystem of the greenfield COBOL→C# (Roslyn) compiler (`src/CobolNet`). Scope: `IF`/`ELSE`/`END-IF`;
> `EVALUATE` (all forms); level-88 condition-names + `SET cond TO TRUE/FALSE`; class / sign / relational /
> abbreviated-combined conditions + operator precedence; the COBOL-2002 EC exception model (EC-* hierarchy,
> `>>TURN`, `RAISE`/`RESUME`, `USE …EXCEPTION/ERROR` declaratives, `EXCEPTION-OBJECT`, the conditional phrases
> `ON SIZE ERROR` / `AT END` / `INVALID KEY` / `ON OVERFLOW` / `ON EXCEPTION`). Companion to
> `docs/COBOLNET_ARCHITECTURE.md`. Cites ISO/IEC 1989:2023 (`specs/ISO_COBOL.md`).

All ISO citations are to ISO/IEC 1989:2023 unless noted. The proven semantics are mined from the legacy
`CobolSharp.Compiler/CodeGen/Lowering/ConditionLowerer.cs` and `CobolSharp.Runtime/PicRuntime.cs` (which pass
364 NIST programs) — the byte *implementation* is rejected, but the *behavior* is the oracle.

---

## 0. Where this lives in the pipeline

`CSharpEmitter` currently has a thin `RenderCondition`/`RenderComparison` pair (DEVLOG 460) that emits a C#
boolean expression for a relational/AND/OR/NOT condition, with class/sign/condition-name/abbreviated forms as
`false /* TODO */` fallbacks. This subsystem replaces those fallbacks with full behavior and adds the
exception model.

Two distinct C# code shapes are produced:

1. **Conditions are pure C# boolean expressions** — `RenderCondition(node) → string`. They are referentially
   transparent (no side effects), so they compose into `if (...)`, `while (!(...))`, `?:`, EVALUATE arms, and
   level-88 boolean properties. This is the existing model; we extend it.
2. **The exception model is stateful runtime + emitted guards.** EC checking, `>>TURN` state, the per-element
   exception-status indicators, the last-exception register, `EXCEPTION-OBJECT`, declarative dispatch, and
   `RAISE`/`RESUME` are runtime concerns carried by `CobolNet.Runtime` + emitted `try`/guard scaffolding that
   only appears when a program actually uses the feature (zero-overhead otherwise).

---

## 1. IF / ELSE / END-IF

### 1.1 Mapping
COBOL `IF cond [THEN] s1… [ELSE s2…] END-IF` → C#:

```csharp
if (<RenderCondition(cond)>) { <s1…> } else { <s2…> }     // else omitted when no ELSE branch
```

The existing `EmitIf` already splits THEN/ELSE at the `ELSE` token and recurses. **Keep it.** Two corrections
to make it decision-complete:

- **`CONTINUE` in a branch** → emit nothing (an empty C# block is fine; ISO §14.9.9 "no operation").
- **`NEXT SENTENCE`** (§14.9.19, obsolete) is *not* `CONTINUE`: it transfers control to the statement after
  the next period (sentence end), bypassing the rest of the current IF. In the structured-subset emitter this
  is the one IF construct that does **not** map to a plain `if`. Approach: when an IF (or any sentence) contains
  `NEXT SENTENCE`, lower the enclosing **sentence** as a labeled block and emit `goto <after_sentence_label>;`.
  This is rare (NC tests use it); flag `COBOLNET0701 NEXT SENTENCE` and lower via the goto-label path. (It is
  *not* the same as falling out of the IF, which is why a bare `break`/return is wrong.)

### 1.2 Nested IF / dangling ELSE
COBOL binds `ELSE` to the nearest unmatched `IF` (the grammar's `(ELSE statementBlock*)?` already does this at
parse time). Because each IF emits a fully-braced C# block, **C#'s dangling-else is structurally impossible** —
the braces disambiguate. No special handling.

---

## 2. Conditions — the expression translator

### 2.1 Precedence (ISO §8.8.4.1, §8.8.4.9)
COBOL condition precedence, tightest first: **`NOT` > `AND` > `XOR` > `OR`**. The ANTLR grammar already encodes
this as a rule cascade (`logicalOrExpression → logicalXorExpression → logicalAndExpression →
unaryLogicalExpression → primaryCondition`), so **`RenderCondition` walks the cascade and the precedence is
preserved by construction** — we never re-parenthesize by precedence ourselves. C#'s own `bool` precedence is
`!` > `&` > `^` > `|` > `&&` > `||` (so `^` binds *tighter* than `&&`/`||`), which does **not** match COBOL's
`AND > XOR > OR`. **Decision: fully parenthesize every binary node we emit** (`(a && b)`, `(a || b)`, `(a ^ b)`)
so the emitted tree's grouping is exactly the COBOL tree's grouping, independent of C# operator precedence. Cost:
a few extra parens in generated source; benefit: provable correctness, no reliance on a subtle C# precedence
table. (Rejected: rely on C# precedence — the `^`-vs-`&&` ordering mismatch is a real trap.)

### 2.1.1 Short-circuit vs eager evaluation of AND/OR (a deliberate divergence from the oracle)
COBOL does not specify operand evaluation order or short-circuiting for `AND`/`OR`; the legacy `ConditionLowerer`
(lines 188–196) evaluates **both** operands into temporaries *eagerly*, then combines — non-short-circuit. C#
`&&`/`||` short-circuit. **Decision: emit short-circuiting `&&`/`||`** (idiomatic, faster, and the safer
default — it suppresses a fault in the right operand when the left already decides the result). **Divergence
from the oracle:** the only observable difference is when the right operand has a side effect or could fault and
the left operand guards it — the classic `IF I > 0 AND TABLE(I) = X` idiom, where eager evaluation would touch
`TABLE(I)` even when `I <= 0`. In the typed model a bad subscript throws (EC-BOUND is OFF by default), so the
two strategies *can* differ observably. **Empirical check (performed):** a scan of the NIST corpus
(`tests/nist/programs`) found **zero** cases of the guard-then-same-variable-subscript idiom — the 44 `AND
<subscripted>` occurrences use a subscript independent of the guard (e.g. `IF SUB4 = 6 AND WZ-X-CHAR(SUB2) =
SPACE`), so short-circuit is corpus-safe. (Rejected: eager evaluation matching the legacy — it would force
hoisting both operands into temporaries, defeating the clean `&&`/`||` output, for a behavior no conformance
test depends on. If a future corpus program *does* rely on eager evaluation, the fix is local: hoist that
operand before the `&&`.)

### 2.2 Node-by-node (extends the existing `RenderCondition`)

| COBOL grammar node | C# rendering |
|---|---|
| `logicalOrExpression` (no abbreviated chain) | `(a || b || …)` |
| `logicalXorExpression` | `(a ^ b)` (C# `^` on `bool` is logical XOR) |
| `logicalAndExpression` (no abbreviated relation) | `(a && b)` |
| `unaryLogicalExpression` with `NOT` | `(!(p))` |
| `primaryCondition` → `comparisonExpression` | §2.3 |
| `primaryCondition` → `booleanLiteral` | `true` / `false` |
| `primaryCondition` → `( condition )` | `(…)` |

### 2.3 `comparisonExpression` — the dispatch
The grammar's `comparisonExpression` has three alternatives. The translator dispatches on which is present:

1. **`operand IS? NOT? className`** → class condition (§2.5).
2. **`operand IS? NOT? (POSITIVE|NEGATIVE|ZERO)`** → sign condition (§2.6).
3. **`operand (comparisonOperator operand)?`** → relational comparison, OR (when the trailing operator group
   is absent) a **bare operand** which is either (a) a level-88 **condition-name** reference (§3), or (b) a
   `PIC 1`/boolean data item used as a condition, or (c) a switch-condition (mnemonic ON/OFF). The binder
   decides which by resolving the data-name's category; see §2.4.

### 2.4 Bare-operand resolution (the key disambiguation)
A bare `comparisonOperand` that is a single data reference is resolved by name:
- resolves to a **level-88 condition-name** → emit the condition-name boolean property (§3).
- resolves to a **`PIC 1`/boolean** elementary item → emit `item != default(bool)` i.e. just `item` (truthy).
- resolves to a **mnemonic switch** (SPECIAL-NAMES `ON STATUS`/`OFF STATUS`) → emit the switch test (§2.8).
- otherwise → it is a numeric item used as an implicit `≠ 0` condition (legacy `ConditionLowerer` line 220–228:
  `IrPicCompareLiteral(loc, 0, NotEqual)`); emit `(<num> != 0)`. Flag `COBOLNET0702` only if the item is
  alphanumeric (truthiness of alphanumeric is not standard; legacy treats non-blank as true — match it for the
  NIST corpus: `!string.IsNullOrWhiteSpace(item)`).

### 2.5 Relational comparison (ISO §8.8.4.1)
This already exists (`RenderComparison`) and is largely correct; the decision-complete rules:

- **Numeric comparison** when *both* operands are numeric (numeric item / numeric literal / arithmetic
  expression / figurative ZERO). Algebraic value comparison: render both as scaled `long`, align to the larger
  scale (the existing `NumX`/`Align`), then `(<l> <op> <r>)`. Operands of unequal precision/scale are compared
  by value (ISO §8.8.4.1.1) — alignment handles it; **no truncation** (comparison is exact). Mixed
  fixed/float: promote to `double` and compare (a later slice once float arithmetic lands).
- **Alphanumeric comparison** when *either* operand is non-numeric (string literal, alphanumeric item, edited
  item, figurative SPACE/HIGH-VALUE/LOW-VALUE/QUOTE/ALL): use `CobolString.Compare(a, b) <op> 0` with COBOL
  space-extension of the shorter operand (ISO §8.8.4.1.2) and the program collating sequence when present
  (§2.7). `CobolString.Compare` must implement: pad shorter with spaces to the longer length, compare
  position-by-position under the (native or custom) collating weights, return sign.
- **Numeric vs alphanumeric** edge (e.g. `PIC 9` item vs string literal `"5"`): legacy parses the literal as a
  number when the field is numeric (lines 424–432). Match: if one side is a numeric item and the other a numeric
  string literal, compare numerically; else compare as text. Flag `COBOLNET0703` for a genuinely incompatible
  pair (numeric item vs non-numeric literal) — fatal per ISO category rules but commonly accepted; keep it
  lenient + dialect-gated like the legacy.
- **Figurative ZERO vs numeric** → numeric 0 (ISO §8.3.1.2; legacy lines 385–393).
- **Pointer relation** (`p = q`, `p = NULL`, only `=`/`NOT =`, ISO §8.8.4.1.4) → `ManagedPointer` reference
  identity: `ReferenceEquals(p, q)` / `p is null`. (Legacy `TryLowerPointerComparison`.)
- **Constant-fold** literal-vs-literal comparisons to `true`/`false` at emit time (legacy lines 447–457) — keeps
  generated C# clean and matches what mainstream COBOL compilers do.
- **Group / record-struct comparison** (`IF GROUP-A = GROUP-B`, or a group vs an alphanumeric literal) compares
  the whole group **as a single alphanumeric value** (ISO §8.8.4.1.2). In the typed model a group is a `record
  struct`, so this needs the **whole-group character image** — the deferred G6 facility
  (`docs/COBOLNET_ARCHITECTURE.md` §3). Until G6, materialize the group to its character image via a
  `record-struct → string` projection and route through `CobolString.Compare`; flag `COBOLNET0708`. This is the
  one IF/EVALUATE operand kind that cannot be done with native field comparison alone.

#### Operator mapping
The existing `MapOperator` is correct (it dodged a double-negation bug, DEVLOG 460). Keep it. It maps every
symbolic + word form (`=`, `<>`, `<`, `>`, `<=`, `>=`, `EQUAL [TO]`, `GREATER [THAN]`, `LESS`, `NOT GREATER`,
`GREATER OR EQUAL`, …) and a leading `NOT` inverts the base relation. **Add a unit-test matrix** over every
grammar alternative of `comparisonOperator` (≈18 forms) — this is the single highest-risk function.

### 2.6 Sign condition (ISO §8.8.4.4)
`operand IS [NOT] {POSITIVE | NEGATIVE | ZERO}` ≡ a numeric comparison against 0:
- POSITIVE → `(<num> > 0)`  ·  NEGATIVE → `(<num> < 0)`  ·  ZERO → `(<num> == 0)`
- a leading `NOT` wraps in `!(…)`.

The operand is evaluated as a scaled `long` (or float); compare the **algebraic value**. (Legacy
`LowerSignCondition` builds exactly this.) Note ZERO means *equal to zero*, and `NOT POSITIVE` means `≤ 0`
(i.e. `!(>0)`), which is **not** the same as NEGATIVE (it includes 0) — the `!(…)` wrap gets this right.

### 2.7 Program collating sequence
When `OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE alphabet-name` (or a SPECIAL-NAMES ALPHABET) is active, all
alphanumeric comparisons use the custom weights, and `HIGH-VALUE`/`LOW-VALUE` remap to the max/min-weight
characters (legacy `MakeFigurativeStringWithSequence` + `FindCharWithMin/MaxWeight`). **Mapping:** the emitter
emits the collating table as a `byte[256]` (or `int[]` weight vector) static field; `CobolString.Compare`
overloads take an optional weight table. This is a self-contained runtime concern; the condition translator just
passes the program's table name when one is configured. (Deferred until the collating subsystem lands in
CobolNet, but the API seam — `CobolString.Compare(a, b, weights?)` — is fixed now so call sites never change.)

### 2.8 Switch (mnemonic) condition
SPECIAL-NAMES `SWITCH-n ON STATUS IS sw-on OFF STATUS IS sw-off` defines condition-names over a runtime switch.
`IF sw-on` → test the switch state. **Mapping:** runtime switches are a `bool[]` (or named `bool` fields) on
the program; the condition-name boolean property reads it. Identical shape to level-88 (§3). `SET sw TO ON/OFF`
sets the bool.

### 2.9 Abbreviated combined conditions (ISO §8.8.4.2)
`IF A > B AND < C OR = D` — after the first relation, subsequent relations may elide the subject (and optionally
the operator). The grammar captures these as `abbreviatedRelation : comparisonOperator comparisonOperand` and
`abbreviatedAndChain`. The current emitter **drops them** (the AND/OR walkers only fire when the abbreviated
lists are empty). Decision-complete handling:

**Expansion at emit time.** Walk `logicalOrExpression` / `logicalAndExpression` keeping a *current subject* and
*current operator* from the most recent full relation. For each `abbreviatedRelation`:
- `op operand` → expand to `currentSubject op operand` (new operator, same subject).
- `operand` alone (bare, lands in the `unaryLogicalExpression` path) → expand to
  `currentSubject currentOperator operand` (same subject, same operator).
A leading `NOT` on an abbreviated relation negates *that relation only*, not the subject (ISO §8.8.4.2 — `NOT`
is part of the operator). Reset the current subject/operator whenever a full (non-abbreviated)
`comparisonExpression` is encountered. Emit each expanded relation through `RenderComparison`, then combine with
`&&`/`||` per the grammar structure.

Worked example: `IF A = B OR C OR > D`
- full: `A = B`  → subject=A, op=`=`
- abbrev bare `C` → `A = C`
- abbrev `> D` → `A > D`
- result: `((A==B) || (A==C) || (A>D))` (numeric/alpha per A's category).

This is the single most error-prone condition feature; it ships with a dedicated test set (NC tests
`NC135A`-style abbreviated forms exercise it).

---

## 3. Level-88 condition-names → C# bool properties

### 3.1 Data-model representation
A level-88 entry is `88 cond-name VALUE v1 [v2 …] | v1 THRU v2 [WHEN SET TO FALSE vf]` subordinate to a
conditional variable. **Mapping:** each condition-name becomes a **C# expression-bodied `static bool`
property** (or `bool` for an instance/OO field) over the parent field — *not* a stored bool, because the truth
value is derived from the conditional variable's current value (ISO §8.8.4.5).

`DataItem` gains a `List<ConditionName> ConditionNames` on the parent, and a new record:
```csharp
public sealed record ConditionName(string CobolName, string CsName,
    IReadOnlyList<CondValue> TrueValues,   // each is a single value or a THRU range
    CondValue? FalseValue);                // WHEN SET TO FALSE literal, if any
public readonly record struct CondValue(string FromLiteral, string? ThruLiteral, bool IsAll);
```

### 3.2 The boolean property
```cobol
01 WS-STATE  PIC 9.
   88 ACTIVE   VALUE 1.
   88 PENDING  VALUE 2 THRU 4.
   88 DONE     VALUE 5 9 WHEN SET TO FALSE 0.
```
→
```csharp
private static long WS_STATE = 0L;
private static bool ACTIVE  => WS_STATE == 1L;
private static bool PENDING => WS_STATE >= 2L && WS_STATE <= 4L;
private static bool DONE    => WS_STATE == 5L || WS_STATE == 9L;
```
Rules:
- **Single value** → `parent == v` (numeric: scale-aligned `long`; alphanumeric: `CobolString.Compare(parent,
  v)==0` with space extension; ALL literal: repeat to the parent width, legacy lines 743–752).
- **THRU range** → `parent >= from && parent <= to` (numeric) or the collating-aware string `>=`/`<=`
  (alphanumeric). For alphanumeric ranges the bound literals are space-extended to the parent width.
- **Multiple values/ranges** → OR them: `(c1 || c2 || …)`.
- **Negation** (`IF NOT cond-name`) → handled by the enclosing `unaryLogicalExpression` `NOT` (no special case
  in the property; the property is the positive test).
- A condition-name may itself be **qualified/subscripted** (`cond-name OF group (i)`). When the parent is a
  table element, the property cannot be parameterless; emit a **method** `bool COND(long i) => parent[i-1] == v;`
  and call sites pass the subscript. (Legacy resolves the parent location via `cn.ParentExpression`.) Flag
  `COBOLNET0704` for the subscripted case until tables land; the method shape is fixed now.

### 3.3 `SET cond-name TO TRUE / FALSE` (ISO §14.9.39 GR 6 / §13.18.63 GR 20)
- **TO TRUE** → move the **first** literal of the condition-name's VALUE clause into the conditional variable
  (ISO §14.9.39 GR 6): `WS_STATE = CobolNum.Store(<v1-unscaled>, <v1-scale>, _P_WS_STATE);` (numeric) or the
  `CobolString.Store` equivalent. For a THRU range, the first literal is the range *start*.
- **TO FALSE** → move the `WHEN SET TO FALSE` literal (`vf`) into the conditional variable (ISO §23456). If no
  FALSE phrase, `SET cond TO FALSE` is a **syntax error** → diagnostic `COBOLNET0705` (ISO: the FALSE phrase is
  required for SET TO FALSE). Multiple condition-names in one SET (`SET A B TO TRUE`) → emit each assignment.

The existing grammar parses `SET dataReference+ TO (TRUE_|FALSE_)` (`setBooleanStatement`). The emitter resolves
each `dataReference` to a `ConditionName`, then emits the move of v1 (TRUE) or vf (FALSE).

---

## 4. Class conditions (ISO §8.8.4.3)

`operand IS [NOT] {NUMERIC | ALPHABETIC | ALPHABETIC-LOWER | ALPHABETIC-UPPER | user-class}`.

### 4.1 The clean problem: no byte buffer
The legacy tests operate on the field's *bytes*. In the typed model the value is a `string`/`long`/`decimal`.
The class condition still operates on the **character image** of the item, so:

- For a `string` (alphanumeric) field, the image **is** the field — test the chars directly.
- For a numeric `long`/`decimal` field, NUMERIC is **always true** by construction (a `long` cannot hold a
  non-digit) — *but* ISO §14.6.13.2 says the test exists to detect *incompatible data* that arrived via a
  REDEFINES/group MOVE. In the typed model, a numeric field can never be incompatible, so `IS NUMERIC` on a
  pure numeric item folds to `true` and `IS NOT NUMERIC` to `false` (and the compiler may warn `COBOLNET0706`
  that the test is constant). The genuinely interesting NUMERIC test is on an **alphanumeric** item holding
  digits (`PIC X(5)` value `"12345"`) — that hits the char-scan path.
  **Dependency:** this fold is valid only while a numeric field has no alphanumeric view. Once REDEFINES/overlay
  (G6) lets a numeric item alias alphanumeric storage, `IS NUMERIC` on that item can legitimately be false and
  must scan the overlaid character image — revisit the fold when G6 lands (do not let it become silently wrong).

### 4.2 Runtime API (`CobolNet.Runtime.CobolClass`)
A new static class mirrors the legacy `PicRuntime` predicates but over `string` (and the typed sign-image for
signed numeric-as-alphanumeric edge cases):
```csharp
public static class CobolClass
{
    public static bool IsNumeric(string s);            // every char 0-9, plus an optional overpunch/separate sign
    public static bool IsNumericDisplay(string s, NumProfile p);  // sign-aware per the PICTURE (overpunch/separate)
    public static bool IsAlphabetic(string s);         // {A-Z, a-z, space} only (ISO §8.8.4.4 closed set)
    public static bool IsAlphabeticLower(string s);    // {a-z, space}
    public static bool IsAlphabeticUpper(string s);    // {A-Z, space}
    public static bool IsUserClass(string s, ReadOnlySpan<char> validChars);  // user CLASS
}
```
Behavior is ported verbatim from `PicRuntime.IsNumericClass/IsAlphabeticClass/…` (legacy lines 2379–2464),
**but over UTF-16 chars instead of bytes** — which is the whole point of the rewrite. Crucial fidelity points:
- ALPHABETIC is the **closed Latin set + space**, *not* `char.IsLetter` (legacy comment line 2436 — Unicode
  letters must be rejected). Keep the explicit ranges.
- NUMERIC on a *signed numeric-display* item accepts the overpunch (`{`,`A`–`I`,`}`,`J`–`R`) or separate
  (`+`/`-`) sign at the sign position (legacy lines 2386–2424). For the typed model this only matters when the
  field is stored as an alphanumeric display image; pure `long`/`decimal` use the always-true fold.
- Spaces are **not** digits → `IS NUMERIC` is false for a field with embedded/trailing spaces (legacy line 2383).

### 4.3 Emission
```cobol
IF WS-CODE IS NUMERIC            →  if (CobolClass.IsNumeric(WS_CODE)) …      (WS-CODE is PIC X)
IF WS-NAME IS ALPHABETIC         →  if (CobolClass.IsAlphabetic(WS_NAME)) …
IF WS-NUM IS NOT NUMERIC         →  if (!true) …   → folds, with COBOLNET0706  (WS-NUM is PIC 9)
```
A leading `NOT` wraps in `!(…)`. **User class** (`CLASS HEX-DIGITS IS "0" THRU "9" "A" THRU "F"` in
SPECIAL-NAMES) compiles to a `validChars` set (expand THRU ranges) passed to `IsUserClass`.

---

## 5. EVALUATE (ISO §14.9.13) — all forms

EVALUATE is the richest condition construct. The grammar:
`EVALUATE subject (ALSO subject)* (WHEN group (ALSO group)* | WHEN OTHER) body …`.

### 5.1 Strategy: lower to a chained `if/else if/else`, NOT C# `switch`
**Decision: emit `if (m1) {…} else if (m2) {…} … else {…}`** where each `mK` is the full match expression for
that WHEN phrase. **Rejected: C# `switch`** — COBOL WHEN arms can be ranges, conditions, multiple subjects,
ANY, partial expressions, and arbitrary expressions per subject; they are *not* constant case labels. The
if/else-if chain is exactly the ISO semantics (§14.9.13.4 GR4: "process each WHEN phrase from left to right …
the first that matches"), reads cleanly, and the C# compiler optimizes dense integer chains anyway. (A *future*
peephole may detect a single-subject all-single-integer-value EVALUATE and emit a `switch` for prettiness; not
v1.)

### 5.2 Subjects evaluated once (ISO §14.9.13.4 GR3)
"At the beginning of execution, each selection subject is evaluated and assigned a value." Side-effecting
subjects (a function call, an arithmetic expression) must be evaluated **exactly once**. **Mapping:** hoist each
subject into a local before the chain:
```csharp
var _e0 = <subject0 as long/decimal/string/bool>;
var _e1 = <subject1 …>;
if (<match0>) {…} else if (<match1>) {…} else {…}
```
A subject that is a bare identifier/literal needs no hoist (no side effect) — emit it inline for readability;
hoist only expressions/function calls. (Correctness: hoisting is always safe; the inline shortcut is a
readability optimization for the common case.)

### 5.3 Subject classification (§14.9.13.4 GR3, §14.9.13.3 GR6)
Each subject is one of: **TRUE/FALSE** (a truth value), a **boolean expression**, a **numeric value**
(identifier/literal/arith-expr), or an **alphanumeric value**. The classification picks how each WHEN item is
compared. Per GR3a, a numeric or 1-char boolean identifier subject is treated as the *identifier* (a value), not
re-parsed as an expression. The binder records, per subject, its `EvaluateSubjectKind ∈ {Truth, Numeric,
Alpha, Boolean}` and its hoisted local + scale.

### 5.4 WHEN item → match expression (§14.9.13.4 GR4a)
For each subject *k* and its WHEN item, build a C# bool:

| WHEN item (grammar `evaluateWhenItem`) | match for subject `_eK` |
|---|---|
| `ANY` | `true` |
| `valueOperand` (single value), subject is value | `_eK == value` (numeric: scale-aligned; alpha: `CobolString.Compare(_eK,v)==0`) |
| `valueRange` (`v1 THRU v2`) | `_eK >= v1 && _eK <= v2` (numeric or collating-aware string) |
| `condition` (subject is TRUE/FALSE) | the truth value of the condition `== _eK` (where `_eK` is `true`/`false`) |
| `TRUE`/`FALSE` as item, subject is a condition | `_eK == true/false` |
| partial-expression (item begins with a relational/class/sign operator) | prepend the subject → full condition, render it |
| leading `NOT` on the group | negate the whole group's combined match |

The grammar's `evaluateWhenGroup : NOT? evaluateWhenItem+` — the `NOT` negates the conjunction of items in the
group (per ISO the NOT applies to the object). **A `WHEN a ALSO b ALSO c` matches iff every per-subject pair
matches** (AND across subjects, §14.9.13.4 GR4b). **Multiple `WHEN` phrases sharing one body** (the grammar's
`evaluateWhenPhrase+`) are **OR**ed (§14.9.13.3, legacy comment: "WHEN a WHEN b … imperative" = a OR b).

So one WHEN *clause*'s match = `OR over its WHEN phrases ( AND over subjects ( per-subject match ) )`.

#### Partial expressions (§14.9.13.3 GR5/8, the subtle one)
`EVALUATE X ALSO Y  WHEN > 5 ALSO "A" THRU "M"` — the first object `> 5` is a *partial expression*: prepend the
subject → `X > 5`. Detection: a WHEN item whose `condition`/`comparisonExpression` leftmost token is a
relational operator (`>`, `<`, `=`, `NOT`, `IS`), a class name without an identifier, or a sign word. The binder
recognizes the partial form and synthesizes `subjectK <partial>`. (Grammar already admits `condition` as a
WHEN item; the partial case is the condition with an elided subject — bind it by injecting the subject.)

### 5.5 WHEN OTHER and no-match (§14.9.13.4 GR5)
- A matched WHEN → its body, then jump to end (the `else if` chain gives this for free — only one arm runs).
- `WHEN OTHER` → the final `else { … }`.
- No WHEN matched and no WHEN OTHER → EVALUATE does nothing (no final `else`).

### 5.6 Worked example
```cobol
EVALUATE WS-DAY ALSO TRUE
  WHEN 1 THRU 5 ALSO WS-OPEN     DISPLAY "WEEKDAY-OPEN"
  WHEN 6 7      ALSO ANY         DISPLAY "WEEKEND"
  WHEN OTHER                     DISPLAY "?"
END-EVALUATE.
```
→
```csharp
var _e0 = WS_DAY;            // long
var _e1 = true;             // TRUE subject
if (((_e0 >= 1L && _e0 <= 5L) && (WS_OPEN == _e1)) /* WS-OPEN is an 88-name → bool */ )
    System.Console.WriteLine("WEEKDAY-OPEN");
else if (((_e0 == 6L || _e0 == 7L) && true))
    System.Console.WriteLine("WEEKEND");
else
    System.Console.WriteLine("?");
```

---

## 6. The COBOL-2002/2023 exception model (EC-*)

This is the deepest part. **Foundational decision: EC checking is OFF by default** (ISO §14.6.13.1.1: "By
default, checking is not enabled for any exception condition"), so a program that never enables an EC and never
uses an exception phrase emits **zero** exception scaffolding — the typed-native fast path stays clean. The
machinery turns on only where the source asks for it.

### 6.1 The EC catalog (ISO Table 13)
Encode Table 13 as a generated `enum ExceptionCondition` (level-3 names) plus a static hierarchy map
(level-3 → level-2 → EC-ALL) and a per-name **fatality** (Fatal / NonFatal / Imp). This lives in
`CobolNet.Runtime/Exceptions/ExceptionCatalog.cs` (generated from the table, single source of truth). The full
level-2 set: EC-ARGUMENT, EC-BOUND, EC-CONTINUE, EC-DATA, EC-EXTERNAL, EC-FLOW, EC-FUNCTION, EC-I-O, EC-IMP,
EC-LOCALE, EC-MCS, EC-OO, EC-ORDER, EC-OVERFLOW, EC-PROGRAM, EC-RAISING, EC-RANGE, EC-REPORT, EC-SCREEN,
EC-SIZE, EC-SORT-MERGE, EC-STORAGE, EC-USER, EC-VALIDATE; level-1 EC-ALL. Each carries its level-3 children and
fatality from Table 13 (e.g. EC-SIZE-OVERFLOW = Fatal, EC-I-O-AT-END = NonFatal, EC-USER-* = NonFatal,
EC-BOUND-SUBSCRIPT = Fatal). User exceptions are `EC-USER-<suffix>` (always nonfatal, §24505); implementor
`EC-IMP-<suffix>`.

### 6.2 Runtime state (`CobolNet.Runtime.Exceptions.ExceptionState`)
Per ISO §14.6.13.1.1 there are three conceptual entities — model them as runtime fields:

- **Last exception status** (run-unit-wide): the last level-3 EC raised (or "exception object raised", or
  "none"). Backs the `EXCEPTION-STATUS` / `EXCEPTION-FILE` / `EXCEPTION-LOCATION` / `EXCEPTION-STATEMENT`
  intrinsic functions (ISO §15.28–15.33). A `[ThreadStatic]`/run-unit singleton:
  ```csharp
  public static class ExceptionState
  {
      public static string? LastExceptionName;   // e.g. "EC-SIZE-OVERFLOW", or null
      public static object? ExceptionObject;     // EXCEPTION-OBJECT predefined ref (§8.4.3.6), null normally
      public static string? LastExceptionFile;   // for EXCEPTION-FILE
      public static string? LastExceptionStatement, LastExceptionLocation;
      public static void Clear() => LastExceptionName = null;  // SET LAST EXCEPTION TO OFF, run-unit reset
  }
  ```
- **Per-statement exception-status indicators**: ISO says all are *cleared at the start of every statement*.
  We do **not** materialize 100+ indicators; instead each EC is *detected at the point it would occur* (inside
  the emitted guard for that statement) and only when checking is enabled — so "set/cleared" is implicit in the
  guard's control flow. The only persistent register is the *last exception status* above.
- **EXCEPTION-OBJECT** (§8.4.3.6): the predefined object reference set by `RAISE identifier` / `…RAISING obj`;
  null after an exception-name RAISE. Stored in `ExceptionState.ExceptionObject`.

### 6.3 `>>TURN` directive (ISO §7.2.x TURN, §4970) — compile-time
`>>TURN ec-name-1 [ec-name-2…] CHECKING ON [WITH LOCATION] | CHECKING OFF` enables/disables checking for the
named ECs **for the source text that follows in the compilation group** until overridden. Default is
`>>TURN EC-ALL CHECKING OFF` (§5000). **This is a compile-time state, not runtime** — it decides *whether the
emitter emits a guard at all* for a given statement. EC-I-O-WARNING can only be toggled explicitly (§5006).

**Mapping:** the preprocessor/frontend (reused) surfaces `>>TURN` directives with their source positions. A new
`TurnState` walks the procedure division in source order, maintaining the set of enabled ECs (expanding EC-ALL
→ all, a level-2 name → its level-3 children, §5002–5004) at each statement. When the emitter lowers a
statement that *can* raise EC-x, it consults `TurnState.IsEnabled(EC-x, atThisStatement)`; only then does it
emit the runtime check. `WITH LOCATION` (§5022) makes EXCEPTION-LOCATION/STATEMENT data available — when set,
the emitted guard passes `(__FILE__, line, "VERB")` into `ExceptionState`.

This is the elegant part of the C#-native design: **EC checking that's OFF compiles to nothing**, so there is
no per-statement runtime branch in the overwhelmingly common case.

### 6.4 Conditional phrases (the "explicit-handler" path — works WITHOUT `>>TURN`)
`ON SIZE ERROR`, `AT END`, `INVALID KEY`, `ON OVERFLOW`, `ON EXCEPTION` are statement-attached imperative
phrases. **These do NOT require `>>TURN`** — they are the classic COBOL-85/2002 handler form and are *always
active when written* (ISO §14.6.13.1.4 GR1: "If a conditional phrase without the NOT phrase is specified in the
interrupted statement, the imperative statement associated with that conditional phrase is executed"). They are
the primary mechanism the NIST corpus uses; `>>TURN`/declaratives are the secondary, M2+ mechanism.

Each maps to a `try`/result-flag around the operation:

#### 6.4.1 ON SIZE ERROR (arithmetic, ISO §14.7.5)
The size-error condition arises when a result exceeds the receiver's PICTURE capacity, or division by zero
(EC-SIZE-ZERO-DIVIDE), or exponentiation-rule violation (EC-SIZE-EXPONENTIATION). **The store path already
routes through `CobolNum.Store`** — extend it to return whether truncation of *non-zero high-order digits*
occurred (true size error, not mere fractional rounding). Mapping:
```cobol
ADD A TO B ON SIZE ERROR DISPLAY "OVF" NOT ON SIZE ERROR DISPLAY "OK" END-ADD
```
→
```csharp
bool _se = false;
{ long _v = …;  B = CobolNum.StoreChecked(_v, _scale, _P_B, out _se); }
if (_se) { System.Console.WriteLine("OVF"); }
else     { System.Console.WriteLine("OK"); }
```
Key rules:
- **On size error, the receiver is left UNCHANGED** (ISO §14.7.5: "the resultant identifier values… are not
  altered"). So `StoreChecked` must compute the would-be value, detect overflow, and **not write** the field
  when overflow occurs and an ON SIZE ERROR phrase is present. Implement: compute the candidate, test capacity,
  write only if no error (or no phrase). This means the emitted code stages the value and conditionally assigns.
- **Multiple receivers**: each is tested; the ON SIZE ERROR phrase runs if *any* receiver overflowed; the
  receivers that didn't overflow ARE updated (ISO §14.7.5). The emitted code accumulates an OR of per-receiver
  flags.
- **Division by zero** → size error (ISO §14.7.5 + EC-SIZE-ZERO-DIVIDE). `CobolNum.Divide` signals it.
- **No ON SIZE ERROR phrase + overflow** → result truncated to the PICTURE (low-order digits kept) silently,
  *unless* EC-SIZE checking is `>>TURN`ed on, in which case the EC path (§6.6) fires. This is the bridge between
  the two mechanisms: the phrase is the local handler; `>>TURN` is the global one.

`ROUNDED` interacts: rounding happens *before* the size-error test (ISO §14.7.5) — round to scale, then check
the integer-part capacity. `CobolNum.Store` already takes a `CobolRounding`; thread it through `StoreChecked`.

#### 6.4.2 AT END / INVALID KEY / ON OVERFLOW / ON EXCEPTION
These attach to I-O (`READ`, `WRITE`, `START`, `RETURN`…), `STRING`/`UNSTRING`, and `CALL`/`INVOKE`
respectively. The shape is identical — a status flag from the operation drives a branch:
```csharp
var _st = CobolFile.Read(f, …);      // sets I-O status
if (CobolStatus.IsAtEnd(_st)) { <AT END body> } else { <NOT AT END body> }
```
- **AT END** ↔ EC-I-O-AT-END ↔ I-O status `1x` (ISO §9.1.13, Table 13). When AT END phrase present and the
  at-end condition exists, **no other applicable exception processing runs** (ISO §11409) — the phrase wins.
- **INVALID KEY** ↔ EC-I-O-INVALID-KEY ↔ status `2x`.
- **ON OVERFLOW** (STRING/UNSTRING) ↔ EC-OVERFLOW-STRING / EC-OVERFLOW-UNSTRING (nonfatal).
- **ON EXCEPTION** (CALL/INVOKE/DELETE FILE/XML/JSON) ↔ the relevant EC; for CALL it fires when the program
  isn't found (EC-PROGRAM-NOT-FOUND).
- Each has a `NOT` form (`NOT AT END`, `NOT INVALID KEY`, `NOT ON OVERFLOW`, `NOT ON EXCEPTION`) → the `else`
  branch (runs on success). `NOT` runs only when the operation *succeeded* (ISO §14.6.13.1.4 GR1 + the
  statement rules). These mirror the legacy `LowerConditionalBranch` (lines 826–855).

These are the **explicit applicable exception processing statements** (ISO §9.1.12) — they take precedence over
USE declaratives for the same condition.

### 6.5 USE … EXCEPTION/ERROR declaratives (ISO §14.9.49)
`DECLARATIVES. d-name SECTION. USE [GLOBAL] AFTER STANDARD {EXCEPTION | ERROR} PROCEDURE ON {file… | INPUT |
OUTPUT | I-O | EXTEND}. <body> END DECLARATIVES.` — a declarative is a handler invoked when a matching exception
is raised and no explicit phrase handled it (ISO §14.6.13.1.4 GR3, §9.1.12).

**Mapping:**
- A declarative SECTION becomes a normal paragraph-method (it's in the control-flow port; CobolNet's
  paragraph-method model from G4 already covers declarative sections — they are just sections in the
  DECLARATIVES region, never fallen-into by normal flow).
- A **declarative registry** maps `(EC / file / open-mode) → declarative-method`. Built at emit time from the
  USE statements. At runtime, when an I-O (or other) operation raises an EC that is *not* handled by an explicit
  phrase, the runtime dispatch (`ExceptionDispatch.Invoke(ec, file)`) selects the matching declarative (first
  match: file-specific > open-mode > exception-name; ISO §9.1.12 "first one in the list that matches") and calls
  its method.
- **USE GLOBAL** (§14.9.49) — visible to contained programs; the registry is keyed per program with a parent
  fallback chain. (Legacy `GlobalUseDeclarativeRegistry`, DEVLOG 233–234; port the shape.)
- **Normal completion** of a declarative → for a nonfatal EC, execution resumes after the offending statement
  (ISO §14.6.13.1.4 GR3); for a fatal EC, the run unit terminates abnormally unless a `RESUME` redirected
  (ISO §14.6.13.1.3 GR5). `RESUME` (§6.7) is how the declarative overrides this.

**Decision: the declarative call is injected at the I-O/operation site**, not via a global try/catch. The
emitted I-O helper returns a status; the emitter, knowing a USE declarative applies to that file/mode, emits
`if (status indicates error && no explicit phrase) ExceptionDispatch.Invoke(...);`. This keeps control flow
explicit and debuggable and matches the legacy proven dispatch. (Rejected: a single program-wide `try/catch`
that re-dispatches — it loses the "resume after the statement" semantics and the precise applicable-statement
selection.)

### 6.6 The EC path proper (when `>>TURN … ON`)
When checking is enabled for EC-x at a statement and the condition occurs:
1. Set `ExceptionState.LastException = EC-x` (and EXCEPTION-OBJECT = null), record file/location/statement when
   `WITH LOCATION`.
2. **Order of handling** (ISO §14.6.13.1.4 nonfatal / §14.6.13.1.3 fatal):
   a) an explicit conditional phrase on the statement (without NOT) handles it (→ §6.4);
   b) else an enclosing **exception-checking PERFORM** `WHEN ec-x` (M2 feature, ISO §14.9.28; the modern
      declarative-free handler) handles it;
   c) else a matching **USE declarative** runs (→ §6.5);
   d) else (nonfatal) execution continues as if nothing happened; (fatal) the run unit terminates abnormally
      (or PROPAGATE propagates).
3. Implement (a)/(c) inline at the site (as above). (b) — exception-checking PERFORM — is a self-contained M2
   slice: wrap `imperative-statement-1` in a `try { } catch (CobolException e) when (Matches(e, whenList))` and
   the WHEN bodies become catch arms; `RESUME NEXT STATEMENT` becomes leaving the try normally. This is the one
   place a real C# `try/catch` is the right tool (the WHEN phrase explicitly traps).

Fatal vs nonfatal drives **termination**: a fatal EC with no handler → `throw new CobolFatalException(EC-x)`
caught by the run-unit boundary in `Main` → abnormal termination message + nonzero exit. Nonfatal with no
handler → continue (the guard just records last-exception and falls through).

### 6.7 RAISE / RESUME (ISO §14.9.29 / §14.9.33)
- **`RAISE EXCEPTION ec-name`** (§14.9.29 GR1): set the EC to exist + EXCEPTION-OBJECT=null, then run the
  handling sequence (§6.6). For a nonfatal EC with no handler, RAISE acts as CONTINUE (§29759). Emission:
  `CobolException.Raise(EC-x);` (a runtime call that records last-exception and either invokes the matching
  declarative or, if fatal & unhandled, throws `CobolFatalException`).
- **`RAISE identifier`** (object, §14.9.29 GR2): `ExceptionState.ExceptionObject = obj;` then run the OO
  exception path — if an applicable declarative exists run it (resume after RAISE on normal completion), else
  continue after the RAISE. Emission: `CobolException.RaiseObject(obj);`.
- **`RESUME AT NEXT STATEMENT`** (§14.9.33 GR2): only valid in a declarative or an exception-checking PERFORM
  WHEN; transfers control to an implicit CONTINUE after the statement that raised the exception. **Mapping:** in
  the declarative-call model, the declarative method returns a `ResumeAction` enum (`{Default, NextStatement,
  Procedure(name)}`); the call site at the offending statement inspects it: `NextStatement` → fall through to
  after the statement (the normal flow already does this for nonfatal; for fatal it suppresses termination);
  `Procedure(name)` → `goto` that procedure's label (ISO §14.9.33 GR3: "as if GO TO procedure-name").
  `RESUME` in a **GLOBAL** declarative ≡ CONTINUE (§30319). A bare `RESUME AT proc` in a declarative sets
  `ResumeAction.Procedure` and returns. (This is why declaratives return a value rather than just running.)
- **`EXIT … RAISING ec/obj`** and **`GOBACK RAISING`** (§24469): set the EC/object then unwind to the activating
  element (propagation). `GOBACK RAISING LAST EXCEPTION` re-raises `ExceptionState.LastException`. Maps to
  setting state + the existing `StopRun`/return path carrying the raised EC to the caller's INVOKE/CALL site,
  where it becomes an EC for *that* statement (ISO §14.6.13.1.5). M2/OO slice.

### 6.8 EXCEPTION-OBJECT and the EXCEPTION-* functions
- `EXCEPTION-OBJECT` (§8.4.3.6) — a predefined object reference; reads `ExceptionState.ExceptionObject`. Usable
  as an INVOKE receiver / SET source. Maps to the field directly.
- `FUNCTION EXCEPTION-STATUS` → `ExceptionState.LastExceptionName ?? "        "` (8-space when none).
- `FUNCTION EXCEPTION-FILE`/`-LOCATION`/`-STATEMENT` → the recorded strings (populated only under
  `WITH LOCATION` for location/statement; file always for I-O ECs).

---

## 7. Diagnostics (new `COBOLNET07xx` band for this subsystem)
| Code | Meaning |
|---|---|
| COBOLNET0701 | `NEXT SENTENCE` lowered via goto-label (obsolete; informational) |
| COBOLNET0702 | Alphanumeric item used as a bare truthiness condition (legacy-compatible) |
| COBOLNET0703 | Numeric vs non-numeric literal comparison (dialect-gated leniency) |
| COBOLNET0704 | Subscripted/qualified condition-name (table slice deferred) |
| COBOLNET0705 | `SET cond TO FALSE` without a `WHEN SET TO FALSE` phrase (error) |
| COBOLNET0706 | Class condition on a typed item folds to a constant (informational) |
| COBOLNET0707 | EC raised/checked for an unimplemented level-3 EC (where ISO permits not raising) |
| COBOLNET0708 | Whole-group (record-struct) comparison via the deferred character-image projection (G6 dependency) |

---

## 8. Owner-level open questions
1. **EC default + dialect.** ISO default is EC-ALL OFF. The North Star is full 2023. Confirm: ship EC checking
   OFF by default (NIST-faithful, fast), enabled only by `>>TURN`/phrases — and the conformance corpus drives
   the EC-on paths. (Recommended; matches ISO §5000.)
2. **Fatal-EC termination policy.** ISO §14.6.13.1.3 lets the implementor continue or terminate an unhandled
   fatal EC. Recommend: terminate the run unit with a diagnostic + nonzero exit (commercial-quality, safest);
   confirm this is the desired implementor choice.
3. **`PROPAGATE` directive** (§4808) and **exception-checking PERFORM `WHEN`** (§14.9.28) are post-'85 (M2/M3).
   Confirm they are in scope for full-2023 (they are listed in the plan) and may land after the declarative/phrase
   path. The seams (declarative-returns-ResumeAction, runtime ExceptionState) are designed to admit them without
   rework.
4. **VALIDATE / EC-VALIDATE** is an **obsolete feature** in 2023 (Table 13 note). Implement for conformance or
   skip as obsolete? (Recommend: implement minimally for the conformance corpus, mark obsolete.)
