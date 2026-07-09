# DESIGN — Version-Conformance Pipeline (superset parse · edition-agnostic bind · one gating pass)

> **STATUS: DESIGN.** How COBOL.NET enforces edition (85 / 2002 / 2014 / 2023) conformance. The gating pass runs
> **over the bound tree, post-bind**; version *identity* is declared **local to the grammar** (construct-id annotation),
> version *numbers* stay single-sourced in `constructs.json`. This doc is the canonical design for the
> version-conformance pipeline; the version-gating
> *framework* primitives (`EditionInfo`, `IDiagnosticSink`, `ConstructRegistry`, `constructs.json`) remain owned by
> [`DESIGN-edition-framework.md`](DESIGN-edition-framework.md). Cross-refs: [`DESIGN-frontend-grammar.md`](DESIGN-frontend-grammar.md),
> [`DESIGN-binder-bound-tree.md`](DESIGN-binder-bound-tree.md). SSOT for settled invariants stays `docs/COBOLNET_DESIGN.md`.

## 0. Design rationale

Edition conformance is **one concern with one owner** — so it is one mechanism with clean, error-gated phase
boundaries, not logic spread across the grammar, the binder, and a post-hoc recogniser. Four fragmentation anti-patterns
this design rules out, each a way version gating decays if it is *not* a single bound-tree pass:

| Anti-pattern | Why it fails |
| --- | --- |
| A post-hoc reverse-signature *recogniser* that re-diagnoses a failed parse | It *guesses* a construct's identity from token/rule/adjacency signatures — inherently heuristic; it mis-fires on legitimate §8.9 user words + garbled syntax. |
| Edition `{isYYYY()}?` *predicates* scattered through the grammar | Gating logic duplicated per rule instead of one version table; and a bare predicate can only fail to a generic parse error, never name the required edition. (Four physical per-edition grammars are worse — ~95% duplicated and still unable to say "requires COBOL-2002" without cross-referencing a newer grammar.) |
| `ConstructRegistry.Check` calls *embedded in the binder* | Couples version conformance to name/type binding; no single place owns the policy. |
| Gating discovered *during* code emission (bind and emit fused) | Codegen runs on an errored tree and the output is discarded — wasted work and a phase-boundary violation. |

The single mechanism defined below — **superset parse + a construct-id annotation local to the grammar +
edition-agnostic bind + one `VersionConformancePass` over the bound tree + emit-if-clean** — is the direct answer to
all four (`feedback_singular_pattern`: one canonical mechanism per job, with error-gated phase boundaries).

## 1. Target architecture

```
  superset parse        grammar recognizes the UNION of all editions; NO edition {isYYYY()}? gates
        │               (a forward, identity-carrying lookahead survives ONLY where a construct is
        │                genuinely ambiguous across editions — the 2 load-bearing cases in §4).
        │               each version-gated rule STAMPS its construct-id onto the parse node (§2.2).
        ▼
     Bind               edition-AGNOSTIC. Produces the BoundProgram. Never sees an EditionInfo; contains
        │               ZERO ConstructRegistry.Check calls. Bound nodes carry a `.Syntax` back-reference.
        │               ── HALT if bind produced errors ──
        ▼
  VersionConformancePass   ONE dedicated walk over the bound tree. The SINGLE owner of all edition gating:
        │                  · syntactic gates → read the grammar-stamped construct-id (via `.Syntax`) → Check
        │                  · semantic gates  → inspect the resolved bound fact (is-group, operand-shape) → Check
        │                  strict: reject (COBOLNET0900/0902/0903).  permissive: accept-inert / warn.
        │               ── HALT if the pass produced errors ──   ← "no codegen on compile errors"
        ▼
     Emit               PURE codegen from a VALID bound tree. No diagnostics possible here except an ICE.
        ▼
     Roslyn
```

The pass is **table-driven** from `constructs.json`/`ConstructRegistry` (already the single version table) and is trivially testable in isolation: feed it a bound tree + an `EditionInfo`, assert the diagnostics — no binder, no emitter.

## 2. Mechanisms

### 2.1 Superset parse
Remove the edition `{isYYYY()}?` predicates so every construct parses at every `--std`. The per-construct feasibility
analysis (§4) confirms **5 of 7** residue gates are *pure gating* (safe to remove outright); **2** are load-bearing for
disambiguation and keep a predicate — but **forward** (§2.3), not a reverse guess. This is the standard multi-version
model (Roslyn `LanguageVersion`, Clang/GCC `-std=`): parse a permissive superset, enforce the language level in a
later phase.

### 2.2 Grammar construct-id annotation (identity, local to the grammar)
So the pass need not re-recognize constructs via a node-type switch, each version-gated rule declares its own identity
**where the construct is defined**:

- **Mechanism = a committed-match ACTION, never a gating predicate.** A hoisted `{…}?` predicate is evaluated
  *speculatively* by ANTLR during failing prediction — the exact trap that killed the earlier forward-stamp attempt
  (DEVLOG 679). A rule-body **action** `{ MarkConstruct(Constructs.RaisingClause2002); }` runs *only* when the rule is
  actually matched. Superset parsing (§2.1) is what makes this reliable: with the alternative no longer edition-gated,
  the rule is matched **deterministically**, so the action fires exactly when the construct is genuinely present.
- **Annotate the construct-ID, not the version number.** The grammar declares only *"this node is
  `RaisingClause2002`"*; `constructs.json`/`ConstructRegistry` remains the sole owner of *"that construct requires 2002 /
  was removed at 2023."* No version facts in two places → no drift. Grammar says *what*, the table says *which edition*.
- **Storage:** a side table keyed by `ParserRuleContext` (or a custom context field), read by the pass through the
  bound node's `.Syntax` back-reference.
- **Scope:** this is for *syntactic* gates where identity is knowable at parse. It is optional per construct — where a
  bound-node *type* is already self-identifying (e.g. a distinct `BoundUnlockStatement`), the pass may switch on the
  type instead. Use the annotation where the construct does **not** surface as its own distinctive bound node.

### 2.3 Forward-detection (only where disambiguation is load-bearing)
Two constructs cannot be ungated outright without mis-parsing a valid below-edition program (§4 #2, #4). They keep a
grammar predicate, but generalized from a pure edition gate to an **identity-carrying forward detector**, following the
existing `boolExprAhead()` precedent (`CobolParserCoreBase.cs:83`):

```
( {is<E>() || <construct>Ahead()}? <constructRule> )?
```

`<construct>Ahead()` returns true **at all editions** iff the tokens genuinely form the construct in operator/phrase
position. When it fires below edition E, the construct is *recognized* (not mis-parsed as user names) and stamped, so
the **same** conformance pass emits the exact diagnostic. Forward-detection is not a parallel diagnostic path — it only
steers the parse so the single pass can do the diagnosing. Contrast with the residue: the guesser infers identity
*after* a failed parse; forward-detection *proves* identity *during* the parse, then defers the verdict to the one pass.

The OPEN site's predicate is `{is2002() || retryPhraseAhead()}?`. **`retryPhraseAhead()` canonical spec:** true iff,
with RETRY at the lookahead in the OPEN-clause position, the following tokens form a complete retry tail
(arithmetic-expression `TIMES` | `FOR`? arithmetic-expression `SECONDS` | `FOREVER`) **AND** at least one further
candidate file-name token remains before the sentence terminator (`openFileSpec+` must stay satisfiable).
Consequences:
- `OPEN INPUT RETRY FOREVER.` (no trailing name) stays a two-file-name list at 85;
- `OPEN INPUT RETRY 5 TIMES F.` forward-detects (an integer can never be a file name);
- the genuinely ambiguous `OPEN INPUT RETRY FOREVER F.` resolves to file names below 2002 (RETRY is a legal §8.9 user
  word there) and to the phrase at ≥2002 (`is2002()` is true; the §8.9 funnel reserves RETRY).

Fail-safe: a missed real gate degrades to a neutral parse error, never a wrong edition claim.

### 2.4 The VersionConformancePass
- Input: the `BoundProgram` + the target `EditionInfo` + an `IDiagnosticSink`. Output: diagnostics; no tree mutation in
  strict mode. In **permissive** mode it applies the accept-inert policy (warn, and where a removed construct has no emit
  path, elide it) — the natural home for the migration-mode "filtering."
- For each visited bound node: if its `.Syntax` carries a stamped construct-id → `ConstructRegistry.Check(edition, sink,
  id, where)`. For the handful of semantically-conditioned gates → compute the resolved fact from the bound node and
  Check. Both funnel through the one `Check`.
- Absorbs and DELETES `EditionValidator` (its §8.9 reserved-word funnel moves into the pass, §3) and funnels ALL 88
  compiler-embedded `ConstructRegistry.Check` call sites into the pass: `DataBinder*` / `StatementBinder*` /
  `OoClassTable` / `PicInfo` / `OdoModel` + `EditionValidator`'s own + the one emitter-side site in
  `CSharpEmitter.Call.cs` (the anti-pattern-#4 emission-time gate, relocated here by name).

### 2.5 Bind/emit phase separation (the "no codegen on errors" fix)
`CompilerDriver` runs `bind → conformance-pass → (halt if errors) → emit`. `CSharpEmitter` is split so that **binding**
(producing the `BoundProgram`) and **emission** (rendering C# from a valid `BoundProgram`) are distinct, and the driver
gates between them. Emission is never reached when parse, bind, or the conformance pass produced an error.

**Verdict surfaces include the pass.** `CheckOnly` / `check-batch` = stop after bind **+ the conformance pass** — the
pass is part of the verdict; only emit is skipped. `CheckOnly`, `check-batch`, `EditionHarness`, and the INV-1
continuity + INV-1-strong legs are re-pointed so their verdicts include pass diagnostics (the strict edition band
codes come from the pass, so a bind-only verdict would silently drop every edition diagnostic).

## 3. What this deletes / keeps

**Deleted:** `ReservedWordEditionHints.cs` in full (all 7 reverse-signature arms + the heuristic helpers
`PrecededByOperand` / `NextWithin` / `PrecededByAnyBeforeDot` / `InRule`) once every arm is migrated. No positional or
signature heuristic survives it in any form.

**Deleted:** `EditionValidator` — absorbed into the `VersionConformancePass` (§2.4); its §8.9 reserved-word funnel
moves into the pass.

**Kept (orthogonal — NOT introduction gates):**
- The **§8.9 reserved-word funnel**: rejects a *genuine user-word* spelling at/above the edition where the word became
  reserved (`COBOLNET0901`). Inverse gate (word→reserved), unchanged in behavior; it lives inside the
  `VersionConformancePass` once `EditionValidator` is absorbed.
- The **VALUE/PROPERTY boundary guards** (`CobolData.g4:382/389`): value-operand-loop disambiguation, not an edition gate.
- The **vendor JSON/XML `COBOL0313` disposition**: a dialect/vendor-extension disposition, not an ISO edition gate. It
  relocates to `CobolErrorStrategy` as a **token-keyed vendor hint** — it is a parse-error re-diagnosis of
  hard-reserved tokens, not an ISO edition gate — in the same commit that deletes `ReservedWordEditionHints` (§5
  Stage 2). No signature table is resurrected for it.

## 4. Per-construct residue classification (feasibility analysis)

| # | Construct (id) | Predicate role | Mechanism | Recognition / detect point | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | `LogicalXorOperator2023` (XOR / EXCLUSIVE-OR) | gating-only | bind-time → pass | `LogicalXorExpressionContext` (guard `ChildCount>1`) | low |
| 2 | `BooleanOperators2002` (B-AND/B-OR/B-XOR/B-NOT + condition ENTRY + COMPUTE F2) | **both** | hybrid: pass for operator tiers + COMPUTE F2; **forward-detect** `boolExprAhead()` for the condition ENTRY | `BindBoolExpr` (guard `HasBoolOp`) | medium |
| 3 | `FileSharingClause2002` (SELECT + OPEN SHARING) | gating-only | bind-time → pass (both sites) | SELECT clause; OPEN phrase | medium (OPEN name-list collision) |
| 4 | `RetryPhrase2002` (RETRY on OPEN/READ/WRITE/REWRITE/DELETE) | **both** | **6 predicate sites** (`openClause:232`, `readStatement:291`, `writeStatement:343`, `rewriteStatement:384`, `deleteStatement:407`, `deleteFileStatement:425` in `Core/CobolIO.g4`) — hybrid: bind-time → pass for the 5 statement sites; **forward-detect** `retryPhraseAhead()` (§2.3) for OPEN | `BindRetry` | medium |
| 5 | `UnlockStatement2002` | gating-only | bind-time → pass | `BoundUnlockStatement` (own token) | low |
| 6 | `ProcedureRaising2002` (PD … RAISING) | gating-only | bind-time → pass | PD linkage bind (`raisingClause() is not null`) | low |
| 7 | `PropertyClause2002` (data-desc PROPERTY) | gating-only | bind-time → pass | data-description clause loop | low |

No construct requires the guesser to survive. #2-ENTRY and #4-OPEN retain a *grammar* predicate, but forward and
identity-carrying. Disambiguation counterexamples that make the two load-bearing: `IF B-AND = 5` with `01 B-AND PIC 9`
at `--std 85` (#2); `OPEN INPUT RETRY FOREVER.` with files named RETRY and FOREVER at `--std 85` (#4 — stays a
two-file-name list under the `retryPhraseAhead()` canonical spec, §2.3).

## 5. Migration plan (incremental, each step guardable)

Any `.g4` edit ⇒ ANTLR regen + the **FULL** legacy guard (not `guard-fast`). Pure code steps ⇒ greenfield conformance +
unit, then the full legacy guard before commit. One construct/phase per commit, each with a DEVLOG entry + a
version-matrix continuity/introduction check + negative fixtures.

The pipeline lands **residue-first**: the residue gates migrate onto bind-time `Check`s one construct per commit
(each individually guardable), the recogniser then has nothing left to recognise and is deleted, and the skeleton
finally funnels every bind-time `Check` into the one pass.

- **Stage 1 — residue migration (batches, ascending risk).** Per §4: **A** (low) UNLOCK, PROPERTY, PD-RAISING, XOR →
  remove the grammar predicate, gate bind-time via `ConstructRegistry.Check`, delete the recogniser arm. **B**
  (medium) SHARING — the SELECT clause + OPEN phrase sites together (one construct, one commit), with adversarial
  characterization fixtures for the OPEN name-list collision (byte-identical parse). **C** (medium) RETRY — the six
  grammar predicate sites of §4 #4: the five statement sites drop their predicate and gate bind-time via
  `Check(RetryPhrase2002)` at the retry-binding site; the OPEN site becomes `{is2002() || retryPhraseAhead()}?`
  (§2.3) — and the boolean family (7a: the operator tiers + COMPUTE F2 are pure gating — drop the predicates + one
  `Check(BooleanOperators2002)` in `BindBoolExpr` guarded by `HasBoolOp`; 7b: the boolean-condition ENTRY generalizes
  `boolExprAhead()` to fire at all editions with operand-adjacency — highest scrutiny, it is the shared comparison
  DFA that regressed in DEVLOG 621).
  *Execution note:* **LANDED** (DEVLOG 709–713): UNLOCK #5, PROPERTY #7, PD-RAISING #6, XOR #1, SHARING #3 (SELECT +
  OPEN in ONE commit; the OPEN name-list collision proven byte-safe). **REMAINING:** Batch C = RETRY #4 + the boolean
  family #2.
- **Stage 2 — delete `ReservedWordEditionHints.cs`** entirely, with its helpers; the vendor JSON/XML `COBOL0313`
  disposition relocates to `CobolErrorStrategy` as a token-keyed vendor hint in the same commit (§3); grep-sweep
  lingering refs; update `DESIGN-frontend-grammar.md`, `DOC_INDEX.md`, DEVLOG.
- **Stage 3 — the pipeline skeleton.** Build the `VersionConformancePass` over the bound tree; funnel ALL 88
  compiler-embedded `ConstructRegistry.Check` call sites into it (`DataBinder*` / `StatementBinder*` / `OoClassTable` /
  `PicInfo` / `OdoModel` + `EditionValidator`'s own + the one emitter-side site in `CSharpEmitter.Call.cs`); absorb
  and DELETE `EditionValidator` (its §8.9 reserved-word funnel moves into the pass); make the binder edition-agnostic
  (zero `Check`s); ensure bound nodes carry the `.Syntax` back-reference the pass reads; split bind/emit in
  `CompilerDriver` (bind → pass → HALT on errors → emit); re-point `CheckOnly` / `check-batch` / `EditionHarness` /
  the INV-1 continuity + INV-1-strong legs so their verdicts include pass diagnostics (§2.5). No behavior change
  (same diagnostics, now from one pass) — proven byte-identical by the full legacy guard + INV-1 sweep. Delivers
  "no codegen on errors" + "dedicated pass".

RETRY has **no bind-time introduction gate today** (carried solely by the grammar predicates) — Stage 1-C is net-new
correctness coverage, to be called out in its DEVLOG entry.

## 6. Risks

- **ANTLR optional-block / DFA greediness** on the two OPEN name-list sites (SHARING, RETRY) and the shared comparison
  DFA (boolean ENTRY): the one place static reasoning cannot fully certify. Contained by sequencing these last, shipping
  the pre-specified adversarial characterization fixtures (byte-identical parse before/after), and the full legacy guard
  after each regen. The OPEN RETRY site keeps its forward `retryPhraseAhead()` by design (§2.3/§4); pre-designed
  fallback for the OPEN SHARING site: retain an analogous forward `openSharingAhead()` — a config flip, not a redesign.
- **Bound-node `.Syntax` back-reference** must exist for every version-gated node so the pass can read the annotation.
  Where a construct has no distinctive bound node *and* no `.Syntax` link, it stays a bound-node-type/semantic check.

## 7. SSOT / doc impact

- `docs/COBOLNET_DESIGN.md` — records the pipeline as `parse → bind (edition-agnostic) → version-conformance pass →
  emit`, and that edition gating is a single bound-tree pass.
- `DESIGN-edition-framework.md` — add a pointer to this doc; note the pass is the single consumer of `ConstructRegistry`.
- `DESIGN-frontend-grammar.md` — the superset-parse + construct-id-annotation convention (action-not-predicate).
- `DESIGN-binder-bound-tree.md` — the binder is edition-agnostic; bound nodes carry `.Syntax`.
- `DOC_INDEX.md` + `docs/COBOLNET_REARCHITECTURE_PLAN.md` — index this doc; slot the migration as a phase.
