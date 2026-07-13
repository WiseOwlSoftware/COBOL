# DESIGN — Version-Conformance Pipeline (superset parse · edition-agnostic bind · one gating pass)

> **STATUS: DESIGN — ✅ IMPLEMENTED (PHASE-03; completed by Exec Step E).** The two-arm
> `VersionConformancePass` is the SOLE edition gate; the binder is edition-agnostic save the documented
> exception ledger (§1.1 — the UDF Check, the catalog-driven per-name windows, the two behavioral reads, the
> owner-disposition SYNC-on-group site). Exec Step E folded the last ~19 inline binder gates into the pass;
> all 9 PHASE-03 exit criteria hold.
> How COBOL.NET enforces edition (85 / 2002 / 2014 / 2023) conformance. The gating pass runs **post-bind** as a
> two-arm walk — one arm over the bound tree (semantic gates), one over the raw parse tree (syntactic gates + the
> §8.9 reserved-word funnel); version *identity* is recovered by the pass itself (bound-node type/attribute or
> parse-tree re-recognition — no `.Syntax` back-reference, no grammar annotation), while version *numbers* stay
> single-sourced in `constructs.json`. This doc is the canonical design for the
> version-conformance pipeline; the version-gating
> *framework* primitives (`EditionInfo`, `IDiagnosticSink`, `ConstructRegistry`, `constructs.json`) remain owned by
> [`DESIGN-edition-framework.md`](DESIGN-edition-framework.md). Cross-refs: [`DESIGN-frontend-grammar.md`](DESIGN-frontend-grammar.md),
> [`DESIGN-binder-bound-tree.md`](DESIGN-binder-bound-tree.md). SSOT for settled invariants stays `docs/COBOLNET_DESIGN.md`.

> **TWO-ARM ARCHITECTURE (the load-bearing invariant).** NO `.Syntax`/raw parse context is added to any bound node
> — the `BoundTree.cs` invariant STANDS. The pass (`VersionConformancePass.cs`) is **TWO-ARM**:
> (1) a BOUND-tree arm that re-identifies gates by bound-node TYPE or a resolved semantic ATTRIBUTE (the
> genuinely-semantic gates — MOVE-category, the file-org/USAGE/pointer-conditioned statement gates, and the
> DATA/PICTURE/OO attribute gates over every source-declared item); (2) a **presence-based PARSE-tree arm**
> (`ParseArm`, over `GroupBindContext.Tree`, running AFTER bind so a construct's semantic errors also accumulate) for
> the SYNTACTIC introduction/removal/phrase/expression/literal gates + the §8.9 reserved-word funnel — it ABSORBED
> the former `EditionValidator`. **WHY the split:** an INTRODUCTION/removal gate must fire on the construct's
> syntactic RECOGNITION, NOT its bound node — a bound-arm gate silently DROPS the 0900 whenever a below-edition
> construct ALSO has a semantic error (it binds to `BoundUnsupported`/`BoundNop`, so the distinctive node is never
> produced). The pipeline's end-state goals (superset parse · edition-agnostic bind · ONE pass · emit-if-clean · no
> `ReservedWordEditionHints`) hold.

## 0. Design rationale

Edition conformance is **one concern with one owner** — so it is one mechanism with clean, error-gated phase
boundaries, not logic spread across the grammar, the binder, and a post-hoc recogniser. Four fragmentation anti-patterns
this design rules out, each a way version gating decays if it is *not* a single post-bind pass:

| Anti-pattern | Why it fails |
| --- | --- |
| A post-hoc reverse-signature *recogniser* that re-diagnoses a failed parse | It *guesses* a construct's identity from token/rule/adjacency signatures — inherently heuristic; it mis-fires on legitimate §8.9 user words + garbled syntax. |
| Edition `{isYYYY()}?` *predicates* scattered through the grammar | Gating logic duplicated per rule instead of one version table; and a bare predicate can only fail to a generic parse error, never name the required edition. (Four physical per-edition grammars are worse — ~95% duplicated and still unable to say "requires COBOL-2002" without cross-referencing a newer grammar.) |
| `ConstructRegistry.Check` calls *embedded in the binder* | Couples version conformance to name/type binding; no single place owns the policy. |
| Gating discovered *during* code emission (bind and emit fused) | Codegen runs on an errored tree and the output is discarded — wasted work and a phase-boundary violation. |

The single mechanism defined below — **superset parse + construct identity recovered by the pass +
edition-agnostic bind + one two-arm `VersionConformancePass` (post-bind) + emit-if-clean** — is the direct answer to
all four (`feedback_singular_pattern`: one canonical mechanism per job, with error-gated phase boundaries).

## 1. Target architecture

```
  superset parse        grammar recognizes the UNION of all editions; NO edition {isYYYY()}? gates
        │               (a forward, identity-carrying lookahead survives ONLY where a construct is
        │                genuinely ambiguous across editions — the 2 load-bearing cases in §4).
        │               construct IDENTITY is recovered later, by the pass's parse-tree arm (§2.2).
        ▼
     Bind               edition-AGNOSTIC (the §1.1 exception ledger is the complete remainder). Produces the
        │               BoundProgram; ZERO ConstructRegistry.Check calls save the ONE documented UDF exception.
        │               NO bound node carries a `.Syntax`/parse back-reference (the BoundTree invariant).
        │               (No separate bind halt: the driver's ONE post-pass HasErrors gate covers both phases —
        │                bind + pass diagnostics ACCUMULATE, so a below-edition construct with a semantic error
        │                surfaces both; DEVLOG 14h.1.)
        ▼
  VersionConformancePass   ONE dedicated post-bind pass, in TWO arms. The SINGLE owner of all edition gating:
        │                  · syntactic gates → the parse-tree arm re-recognizes the construct → Check
        │                  · semantic gates  → inspect the resolved bound fact (is-group, operand-shape) → Check
        │                  strict: reject (COBOLNET0900/0902/0903).  permissive: accept-inert / warn.
        │               ── HALT if the pass produced errors ──   ← "no codegen on compile errors"
        ▼
     Emit               PURE codegen from a VALID bound tree. No diagnostics possible here except an ICE.
        ▼
     Roslyn
```

The pass is **table-driven** from `constructs.json`/`ConstructRegistry` (already the single version table) and is trivially testable in isolation: feed it a bound tree + an `EditionInfo`, assert the diagnostics — no binder, no emitter.

### 1.1 The gating-exception ledger (the COMPLETE list — everything else is pass-owned)

"Edition-agnostic bind" means: the binder makes NO hardcoded-year gating decision of its own. Exec Step E folded
the last ~19 inline `DialectLevel` diagnostic gates into the pass (each now a `constructs.json` row firing from
the correct arm, entering the version matrix with its negative witness). What legitimately REMAINS binder-side
is this closed, documented set:

1. **The ONE bind-time `Check`** — `UdfBinder` (UserFunctionInvocation2002): an intrinsic FUNCTION and a
   user-function call are syntactically identical, only the repository-resolved name set separates them, so the
   gate fires at bind on recognition (§2.4).
2. **Catalog-driven per-NAME windows** — version facts that live in their OWN tables, not constructs.json: the
   D8 intrinsic IntroducedIn/RemovedIn windows (`IntrinsicCatalog`, COBOLNET1502/1503, + the bespoke TRIM
   argument-2 window riding BindTrim), the `ExceptionCatalog` EC-name windows (COBOLNET0878 — EcBinder, the USE
   F3 name loop, `>>TURN`), the `PictureAnalyzer` picture-symbol rows (the ≥edition 0899 half of the W2 skeleton
   gate), and the §8.3.1.2 digit caps (`EditionContext.CheckDigitCapacity`, 0801/0802 — `EditionInfo.MaxDigits`
   is the table).
3. **The two sanctioned BEHAVIORAL edition reads** (they select semantics for VALID programs, no diagnostic):
   the <2002 keyword-omitted FUNCTION routing gate (`IntrinsicBinder` — §8.4.3.2 SR2 routing is inert below
   2002) and the ≥2002 MOVE CORRESPONDING pair-selection window (`CorrespondingBinder` — the Table-16 NE row).
4. **The owner-disposition SYNCHRONIZED-on-group site** (`DataBinder`, row sync-on-group-2023): deliberately a
   manual `Edition.Removed`-severity emission (error strict / WARNING permissive) because the `Check`
   introduction verdict errors on BOTH axes — the owner chose accept-inert continuity (P3 step 10; the row's
   description records it).

Any OTHER `DialectLevel` comparison appearing in `Binding/**` is a defect: relocate it into the pass.

## 2. Mechanisms

### 2.1 Superset parse
Remove the edition `{isYYYY()}?` predicates so every construct parses at every `--std`. The per-construct feasibility
analysis (§4) confirms **5 of 7** residue gates are *pure gating* (safe to remove outright); **2** are load-bearing for
disambiguation and keep a predicate — but **forward** (§2.3), not a reverse guess. This is the standard multi-version
model (Roslyn `LanguageVersion`, Clang/GCC `-std=`): parse a permissive superset, enforce the language level in a
later phase.

### 2.2 Construct identity (recovered by the pass, not stamped in the grammar)
So the pass can gate without the grammar carrying any edition facts, it recovers each construct's identity itself,
after parsing — never by a grammar action or a side table keyed on the parse node:

- **Syntactic gates → the parse-tree arm re-recognizes the construct.** The `ParseArm` visitor descends the raw
  compilation unit and, at each version-gated grammar rule, recognizes the construct from its OWN parse context (its
  rule type and tokens) and routes it through the one `Check`. Superset parsing (§2.1) is what makes this reliable:
  with the alternative no longer edition-gated, the rule is matched **deterministically**, so the parse node is
  present exactly when the construct is genuinely present. No hoisted `{…}?` gating predicate is needed — such a
  predicate is evaluated *speculatively* by ANTLR during failing prediction (the trap that killed the earlier
  forward-stamp attempt); recognition here happens post-parse over a fully-built tree.
- **The pass names the construct-ID, the table owns the version number.** A gate says only *"this node is
  `RaisingClause2002`"*; `constructs.json`/`ConstructRegistry` remains the sole owner of *"that construct requires 2002 /
  was removed at 2023."* No version facts in two places → no drift. The parse (or bound) shape says *what*, the table
  says *which edition*.
- **Semantic gates → the bound-tree arm keys on the resolved fact.** Where a bound-node *type* is already
  self-identifying (e.g. a distinct `BoundUnlockStatement`), or the identity is a RESOLVED bound attribute (a MOVE's
  source × receiver picture, an item's USAGE / PICTURE category, a file-org/pointer condition) rather than mere
  presence, the bound-tree arm switches on the bound node instead. Use the bound arm where the construct's identity
  is a semantic fact; use the parse arm where it is the construct's syntactic recognition.

### 2.3 Forward-detection (only where disambiguation is load-bearing)
Two constructs cannot be ungated outright without mis-parsing a valid below-edition program (§4 #2, #4). They keep a
grammar predicate, but generalized from a pure edition gate to an **identity-carrying forward detector**, following the
existing `boolExprAhead()` precedent (`CobolParserCoreBase.cs:83`):

```
( {is<E>() || <construct>Ahead()}? <constructRule> )?
```

`<construct>Ahead()` returns true **at all editions** iff the tokens genuinely form the construct in operator/phrase
position. When it fires below edition E, the construct is *recognized* (not mis-parsed as user names), so
the **same** conformance pass emits the exact diagnostic. Forward-detection is not a parallel diagnostic path — it only
steers the parse so the single pass can do the diagnosing. Contrast with the residue: the guesser infers identity
*after* a failed parse; forward-detection *proves* identity *during* the parse, then defers the verdict to the one pass.

The OPEN site's predicate is `{is2002() || retryPhraseAhead()}?`. **`retryPhraseAhead()` canonical spec:** true iff,
with RETRY at the lookahead in the OPEN-clause position, the tail after RETRY is an **UNAMBIGUOUS numeric** retry tail
(`n TIMES` | `FOR`? `n SECONDS`, where `n` contains an integer literal — an integer can never be a file name, so RETRY
must be the phrase keyword) **AND** at least one further candidate file-name token remains before the sentence
terminator (`openFileSpec+` must stay satisfiable). A bare `RETRY FOREVER` (FOREVER is a §8.10 user-legal word) or a
`RETRY <name>` count is *genuinely ambiguous* below 2002 and is EXCLUDED — those defer to `is2002()`. Consequences:
- `OPEN INPUT RETRY FOREVER.` (no numeric tail, no trailing name) stays a two-file-name list at 85;
- `OPEN INPUT RETRY 5 TIMES F.` forward-detects (the integer `5` disambiguates; `F` remains);
- the genuinely ambiguous `OPEN INPUT RETRY FOREVER F.` resolves to file names below 2002 (RETRY/FOREVER are legal user
  words there) and to the phrase at ≥2002 (`is2002()` is true; the §8.9 funnel reserves RETRY).

Fail-safe: a missed real gate (an ambiguous below-2002 tail) degrades to a neutral parse error, never a wrong edition
claim. The other five RETRY sites name their file BEFORE the phrase, so they carry no ambiguity and are gated by the
parse-tree arm on the RETRY phrase's recognition (`VisitRetryPhrase` → `Check(RetryPhrase2002)`), no forward detect needed.

### 2.4 The VersionConformancePass
- Input: the bound run unit (`GroupBindContext` — the bound programs + their raw parse `Tree`) + the target
  `EditionInfo` + an `IDiagnosticSink`. Output: diagnostics; no tree mutation in strict mode. In **permissive** mode
  it applies the accept-inert policy (warn, and where a removed construct has no emit path, elide it) — the natural
  home for the migration-mode "filtering."
- The parse-tree arm re-recognizes each syntactic construct and calls `ConstructRegistry.Check(edition, sink, id,
  where)`; the bound-tree arm computes the resolved fact from the bound node for the semantically-conditioned gates
  and Checks. Both funnel through the one `Check`.
- Absorbs the former `EditionValidator` (its §8.9 reserved-word funnel is the parse arm's `VisitCobolWord`, §3) and is
  the SINGLE home for every edition `Check`: the binder is edition-agnostic (the former `DataBinder*` /
  `StatementBinder*` / `OoClassTable` / `PicInfo` / `OdoModel` sites now fire from one of the pass's two arms), and the
  former emission-time gate (the anti-pattern #4) is gone. The ONE surviving bind-time `Check` is the documented
  UDF-invocation exception (`UdfBinder.cs`): an intrinsic FUNCTION and a user-function call are syntactically
  identical — only the repository-resolved name set separates them — so it fires at bind, on recognition, before
  operand binding.

### 2.5 Bind/emit phase separation (the "no codegen on errors" fix)
`CompilerDriver` runs `bind → conformance-pass → (halt if errors) → emit`. **Binding** (producing the `BoundProgram`)
and **emission** (rendering C# from a valid `BoundProgram`) are distinct phases and the driver gates between them.
Emission is never reached when parse, bind, or the conformance pass produced an error.

**Verdict surfaces include the pass.** `CheckOnly` / `check-batch` = stop after bind **+ the conformance pass** — the
pass is part of the verdict; only emit is skipped. `CheckOnly`, `check-batch`, `EditionHarness`, and the INV-1
continuity + INV-1-strong legs include pass diagnostics in their verdicts (the strict edition band
codes come from the pass, so a bind-only verdict would silently drop every edition diagnostic).

## 3. What this deletes / keeps

**Deleted:** `ReservedWordEditionHints.cs` in full (all 7 reverse-signature arms + the heuristic helpers
`PrecededByOperand` / `NextWithin` / `PrecededByAnyBeforeDot` / `InRule`). No positional or
signature heuristic survives it in any form.

**Deleted:** `EditionValidator` — absorbed into the `VersionConformancePass` (§2.4); its §8.9 reserved-word funnel is
the pass's parse-tree arm (`VisitCobolWord`).

**Kept (orthogonal — NOT introduction gates):**
- The **§8.9 reserved-word funnel**: rejects a *genuine user-word* spelling at/above the edition where the word became
  reserved (`COBOLNET0901`). Inverse gate (word→reserved), unchanged in behavior; it lives inside the
  `VersionConformancePass` (the parse-tree arm's `VisitCobolWord`).
- The **VALUE/PROPERTY boundary guards** (`CobolData.g4:382/389`): value-operand-loop disambiguation, not an edition gate.
- The **vendor JSON/XML `COBOL0313` disposition**: a dialect/vendor-extension disposition, not an ISO edition gate. It
  lives in `CobolErrorStrategy` as a **token-keyed vendor hint** — a parse-error re-diagnosis of
  hard-reserved tokens, not an ISO edition gate. No signature table is resurrected for it.

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
  DFA most prone to regression).
- **Stage 2 — delete `ReservedWordEditionHints.cs`** entirely, with its helpers; the vendor JSON/XML `COBOL0313`
  disposition relocates to `CobolErrorStrategy` as a token-keyed vendor hint in the same commit (§3); grep-sweep
  lingering refs; update `DESIGN-frontend-grammar.md`, `DOC_INDEX.md`, DEVLOG.
- **Stage 3 — the pipeline skeleton.** Build the two-arm `VersionConformancePass`; funnel every
  compiler-embedded `ConstructRegistry.Check` call site into it (`DataBinder*` / `StatementBinder*` / `OoClassTable` /
  `PicInfo` / `OdoModel` + `EditionValidator`'s own + the former emitter-side gate); absorb
  and DELETE `EditionValidator` (its §8.9 reserved-word funnel becomes the parse arm's `VisitCobolWord`); make the
  binder edition-agnostic (zero `Check`s save the documented UDF exception); no `.Syntax` back-reference is added to
  any bound node (the parse-tree arm re-recognizes the syntactic gates instead); split bind/emit in
  `CompilerDriver` (bind → pass → HALT on errors → emit); point `CheckOnly` / `check-batch` / `EditionHarness` /
  the INV-1 continuity + INV-1-strong legs so their verdicts include pass diagnostics (§2.5). No behavior change
  (same diagnostics, now from one pass) — proven byte-identical by the full legacy guard + INV-1 sweep. Delivers
  "no codegen on errors" + "dedicated pass".

## 6. Risks

- **ANTLR optional-block / DFA greediness** on the two OPEN name-list sites (SHARING, RETRY) and the shared comparison
  DFA (boolean ENTRY): the one place static reasoning cannot fully certify. Contained by sequencing these last, shipping
  the pre-specified adversarial characterization fixtures (byte-identical parse before/after), and the full legacy guard
  after each regen. The OPEN RETRY site keeps its forward `retryPhraseAhead()` by design (§2.3/§4); pre-designed
  fallback for the OPEN SHARING site: retain an analogous forward `openSharingAhead()` — a config flip, not a redesign.
- **Parse-arm ↔ bound-arm assignment** must be correct for every version-gated construct: an introduction/removal or
  otherwise purely-syntactic gate belongs on the parse-tree arm (so it fires on recognition even when the construct
  also fails to bind), and only a genuinely-semantic gate (identity = a resolved bound attribute) belongs on the
  bound-tree arm. No `.Syntax`/parse back-reference is added to a bound node to bridge the two (the BoundTree
  invariant); a misassigned syntactic gate would silently drop its diagnostic on a semantic-error path.

## 7. SSOT / doc impact

- `docs/COBOLNET_DESIGN.md` — records the pipeline as `parse → bind (edition-agnostic) → version-conformance pass →
  emit`, and that edition gating is a single bound-tree pass.
- `DESIGN-edition-framework.md` — add a pointer to this doc; note the pass is the single consumer of `ConstructRegistry`.
- `DESIGN-frontend-grammar.md` — the superset-parse + construct-id-annotation convention (action-not-predicate).
- `DESIGN-binder-bound-tree.md` — the binder is edition-agnostic; no bound node carries a `.Syntax`/parse back-reference.
- `DOC_INDEX.md` + `docs/COBOLNET_REARCHITECTURE_PLAN.md` — index this doc; slot the migration as a phase.
