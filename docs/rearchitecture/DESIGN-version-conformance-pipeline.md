# DESIGN — Version-Conformance Pipeline (superset parse · edition-agnostic bind · one gating pass)

> **STATUS: DESIGN / PROPOSED (2026-07-08).** Owner-driven redesign of how COBOL.NET enforces edition (85 / 2002 /
> 2014 / 2023) conformance. Decisions locked in this session: the gating pass runs **over the bound tree, post-bind**;
> version *identity* is declared **local to the grammar** (construct-id annotation), version *numbers* stay
> single-sourced in `constructs.json`. This doc is the canonical design for the pipeline redesign; the version-gating
> *framework* primitives (`EditionInfo`, `IDiagnosticSink`, `ConstructRegistry`, `constructs.json`) remain owned by
> [`DESIGN-edition-framework.md`](DESIGN-edition-framework.md). Cross-refs: [`DESIGN-frontend-grammar.md`](DESIGN-frontend-grammar.md),
> [`DESIGN-binder-bound-tree.md`](DESIGN-binder-bound-tree.md). SSOT for settled invariants stays `docs/COBOLNET_DESIGN.md`.

## 0. Why (the four observations that motivated this)

The redesign was prompted by four owner critiques, which are one root cause:

| Observation | Underlying defect |
| --- | --- |
| "the error mechanism is ad hoc, not formally designed" | `ReservedWordEditionHints` *guesses* a construct's identity from a **failed** parse (token/rule/adjacency signatures) — inherently heuristic, and it mis-fires on legitimate §8.9 user words + garbled syntax (DEVLOG 708). |
| "four grammars, or a better design?" | edition gating is scattered as `{isYYYY()}?` **grammar predicates** instead of one version table applied uniformly. Four physical grammars would be ~95% duplicated *and* still couldn't say "requires COBOL-2002" without cross-referencing a newer grammar. |
| "why wait until binding? use a tree walker" | gating is **smeared through the binder** (`ConstructRegistry.Check` at ~24 sites) plus a half-pass (`EditionValidator`) — no single dedicated pass owns it. |
| "no reason to generate code when there are compile errors" | **bind and emit are fused**: `CompilerDriver.cs:114` runs `CSharpEmitter.Emit` (which binds → `BoundProgram` → *renders C#*) and only at `:115` checks `edition.Diagnostics` and discards the output. Codegen runs on an errored tree. |

**Root cause:** the pipeline has **no clean, error-gated phase boundaries**, and version conformance is split across **three** mechanisms (grammar predicates, the post-hoc guesser, and binder-embedded `Check` calls). This is the `feedback_singular_pattern` anti-pattern (multiple mechanisms for one job) plus a phase-separation failure.

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

### 2.4 The VersionConformancePass
- Input: the `BoundProgram` + the target `EditionInfo` + an `IDiagnosticSink`. Output: diagnostics; no tree mutation in
  strict mode. In **permissive** mode it applies the accept-inert policy (warn, and where a removed construct has no emit
  path, elide it) — the natural home for the migration-mode "filtering."
- For each visited bound node: if its `.Syntax` carries a stamped construct-id → `ConstructRegistry.Check(edition, sink,
  id, where)`. For the handful of semantically-conditioned gates → compute the resolved fact from the bound node and
  Check. Both funnel through the one `Check`.
- Replaces both `EditionValidator` (absorbed) and the ~24 binder-embedded `Check` calls (relocated here).

### 2.5 Bind/emit phase separation (the "no codegen on errors" fix)
`CompilerDriver` runs `bind → conformance-pass → (halt if errors) → emit`. `CSharpEmitter` is split so that **binding**
(producing the `BoundProgram`) and **emission** (rendering C# from a valid `BoundProgram`) are distinct, and the driver
gates between them. Emission is never reached when parse, bind, or the conformance pass produced an error.

## 3. What this deletes / keeps

**Deleted:** `ReservedWordEditionHints.cs` in full (all 7 reverse-signature arms + the heuristic helpers
`PrecededByOperand` / `NextWithin` / `PrecededByAnyBeforeDot` / `InRule`) once every arm is migrated. The interim
positional-signature patch from DEVLOG 708 (this session) is **superseded** and reverted — it was a heuristic on a
heuristic.

**Kept (orthogonal — NOT introduction gates):**
- The **§8.9 reserved-word funnel** (`EditionValidator`/its successor `CheckedTokenTypes`): rejects a *genuine user-word*
  spelling at/above the edition where the word became reserved (`COBOLNET0901`). Inverse gate (word→reserved), unaffected.
- The **VALUE/PROPERTY boundary guards** (`CobolData.g4:382/389`): value-operand-loop disambiguation, not an edition gate.
- The **vendor JSON/XML `COBOL0313` disposition**: a dialect/vendor-extension disposition, not an ISO edition gate.

## 4. Per-construct residue classification (feasibility analysis, this session)

| # | Construct (id) | Predicate role | Mechanism | Recognition / detect point | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | `LogicalXorOperator2023` (XOR / EXCLUSIVE-OR) | gating-only | bind-time → pass | `LogicalXorExpressionContext` (guard `ChildCount>1`) | low |
| 2 | `BooleanOperators2002` (B-AND/B-OR/B-XOR/B-NOT + condition ENTRY + COMPUTE F2) | **both** | hybrid: pass for operator tiers + COMPUTE F2; **forward-detect** `boolExprAhead()` for the condition ENTRY | `BindBoolExpr` (guard `HasBoolOp`) | medium |
| 3 | `FileSharingClause2002` (SELECT + OPEN SHARING) | gating-only | bind-time → pass (both sites) | SELECT clause; OPEN phrase | medium (OPEN name-list collision) |
| 4 | `RetryPhrase2002` (RETRY on OPEN/READ/WRITE/REWRITE/DELETE) | **both** | hybrid: pass for the 5 verb sites; **forward-detect** `retryPhraseAhead()` for OPEN | `BindRetry` | medium |
| 5 | `UnlockStatement2002` | gating-only | bind-time → pass | `BoundUnlockStatement` (own token) | low |
| 6 | `ProcedureRaising2002` (PD … RAISING) | gating-only | bind-time → pass | PD linkage bind (`raisingClause() is not null`) | low |
| 7 | `PropertyClause2002` (data-desc PROPERTY) | gating-only | bind-time → pass | data-description clause loop | low |

No construct requires the guesser to survive. #2-ENTRY and #4-OPEN retain a *grammar* predicate, but forward and
identity-carrying. Disambiguation counterexamples that make the two load-bearing: `IF B-AND = 5` with `01 B-AND PIC 9`
at `--std 85` (#2); `OPEN INPUT RETRY FOREVER.` with a file named RETRY at `--std 85` (#4).

## 5. Migration plan (incremental, each step guardable)

Any `.g4` edit ⇒ ANTLR regen + the **FULL** legacy guard (not `guard-fast`). Pure code steps ⇒ greenfield conformance +
unit, then the full legacy guard before commit. One construct/phase per commit, each with a DEVLOG entry + a
version-matrix continuity/introduction check + negative fixtures.

- **Stage 0 — skeleton (biggest architectural win, land first).** Split `CSharpEmitter` into bind vs. emit; introduce
  `VersionConformancePass` over the bound tree; **relocate the existing ~24 binder `Check` calls into it**; make the
  binder edition-agnostic; wire the driver `bind → pass → (halt) → emit`. No behavior change (same diagnostics, now
  from one pass) — proven byte-identical by the full legacy guard + INV-1 sweep. Delivers "no codegen on errors" +
  "dedicated pass" immediately, with the residue guesser still in place (unchanged).
- **Stage 1 — fold the residue into the pass (Batch A→C, ascending risk).** Per §4: **A** (low) UNLOCK, PROPERTY,
  PD-RAISING, XOR, SELECT-SHARING → remove the grammar predicate, stamp the construct-id / gate in the pass, delete the
  arm. **B** (medium) OPEN-SHARING with adversarial characterization fixtures (byte-identical parse). **C** (medium)
  RETRY (5 verb sites bind-gated + `retryPhraseAhead()` forward for OPEN) and the boolean family (7a pure-gating tiers +
  COMPUTE F2; 7b the `boolExprAhead()` ENTRY generalization — highest scrutiny, it is the shared comparison DFA that
  regressed in DEVLOG 621).
- **Stage 2 — delete `ReservedWordEditionHints.cs`** and its helpers; grep-sweep lingering refs; update
  `DESIGN-frontend-grammar.md`, `DOC_INDEX.md`, DEVLOG.

RETRY has **no bind-time introduction gate today** (carried solely by grammar+guesser) — Stage 1-C is net-new
correctness coverage, to be called out in its DEVLOG entry.

## 6. Risks

- **ANTLR optional-block / DFA greediness** on the two OPEN name-list sites (SHARING, RETRY) and the shared comparison
  DFA (boolean ENTRY): the one place static reasoning cannot fully certify. Contained by sequencing these last, shipping
  the pre-specified adversarial characterization fixtures (byte-identical parse before/after), and the full legacy guard
  after each regen. Pre-designed fallback for the OPEN sites: retain a forward `openSharingAhead()` / `retryPhraseAhead()`
  — a config flip, not a redesign.
- **Bound-node `.Syntax` back-reference** must exist for every version-gated node so the pass can read the annotation.
  Where a construct has no distinctive bound node *and* no `.Syntax` link, it stays a bound-node-type/semantic check.

## 7. SSOT / doc impact

- `docs/COBOLNET_DESIGN.md` — record the pipeline as `parse → bind (edition-agnostic) → version-conformance pass →
  emit`, and that edition gating is a single bound-tree pass (superseding "bind-time Check at recognition points").
- `DESIGN-edition-framework.md` — add a pointer to this doc; note the pass is the single consumer of `ConstructRegistry`.
- `DESIGN-frontend-grammar.md` — the superset-parse + construct-id-annotation convention (action-not-predicate).
- `DESIGN-binder-bound-tree.md` — the binder is edition-agnostic; bound nodes carry `.Syntax`.
- `DOC_INDEX.md` + `docs/COBOLNET_REARCHITECTURE_PLAN.md` — index this doc; slot the migration as a phase.
