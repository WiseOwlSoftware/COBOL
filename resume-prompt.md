# COBOL.NET — Next-Session Kickoff Prompt

⛔🔥 **What this project is:** a blank-slate compiler translating COBOL → idiomatic typed-native C# compiled by
Roslyn — a COBOL record IS a .NET `record struct`, an elementary item IS a native field. **There is NO byte
`ProgramState` substrate — never reintroduce it, never "fall back" to the legacy byte engine** (`CobolSharp.Compiler`
is a differential oracle only, deleted at G8; a `byte[]` appears only at a genuine REDEFINES Tier-C / file boundary).

**Mission:** a commercial-quality, decades-sustainable, FULL ISO/IEC 1989:2023 COBOL compiler with correct support for
all prior editions (1985 / 2002 / 2014), validated as N per-edition compilers by the VERSION TEST MATRIX. Default
`--std` = 2023.

## Where to read (SSOT)
- **`docs/COBOLNET_DESIGN.md`** — the decision-complete design SSOT (locked invariants §1, data model, bound-tree
  pipeline [no lowered IR], native scaled-integer numerics, REDEFINES/files/OO/EC/intrinsics, §18 settled decisions,
  no-god-class structure). Deep-dives per subsystem are listed in its §0.5.
- **`docs/COBOLNET_REARCHITECTURE_PLAN.md`** — the go-forward roadmap (17-phase, resumable; **§0 RESUME PROTOCOL**;
  **§4.1 execution resequence**; §6 owner decisions D1–D12 ALL resolved). Obey its STATUS banner → the current
  `docs/rearchitecture/PHASE-NN-*.md` STATUS line → execute its numbered steps.
- **`specs/ISO_COBOL.md`** — the ISO spec is THE authority for ALL syntax AND behavior (submodule;
  `git submodule update --init`). See the NON-NEGOTIABLE rules below.
- **`DEVLOG.md`** — DESCENDING (newest entry first, under the preamble); add each entry at the TOP with a real
  `date "+%Y-%m-%d %H:%M %Z"` stamp. The full session history lives here (this banner stays lean).

## ⛔🔀 RESUME AT — EXEC STEP F: PHASE-08 (runtime library reorg)

**EXEC STEP E IS COMPLETE (2026-07-12).** The ~19 inline edition gates are FOLDED into the two-arm
`VersionConformancePass` — 20 new `constructs.json` rows (0804/0807/0815/0830–33/0845/0870–72/0876/0877/0879/
0880/0884/0885 + the dual-window arithmetic-standard-2014), each with a version-matrix fixture; the orphaned
`GateId` scaffolding is DELETED (generator section 3 + drift fact; `UsageGateId`→`UsageConstructId` rename);
the "edition-agnostic" claims are reconciled onto the ONE canonical **§1.1 gating-exception ledger**
(`DESIGN-version-conformance-pipeline.md`): the UDF Check + the catalog-driven per-name windows (D8 intrinsics,
EC names, PICTURE symbol rows, digit caps) + the two behavioral reads (keyword-omitted FUNCTION routing, MOVE
CORR pair window) + the owner-disposition SYNC-on-group site. Any OTHER `DialectLevel` comparison in
`Binding/**` is a defect — relocate it into the pass. Next: **Exec Step F = PHASE 08–16** (start at
`docs/rearchitecture/PHASE-08-runtime-library-reorg-rununit.md` — read its STATUS line first).

## (completed) EXEC STEP E: P2/P3 edition-gate remediation

**PHASE-07 IS COMPLETE (Steps 1–12).** Both god classes are dissolved; `Place` is structural (Step 11 —
`AccessPath` + `CodeGen/Roslyn/PlaceRenderer`, `PlaceNeutralityTests` locks G4); and **Step 12 landed the
FUNCTION-arg grammar**: `functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)?` — arguments are
REAL `arithmeticExpression` trees through the ONE `ExpressionBinder.BindExpr` (lexer FUNCTION suppression +
argument-region `SIGNED_*` twins per §8.7.1/§8.3.3.3.2 + the `FNARG_SEPARATOR` §8.3.5 separator token); the
hand-rolled recursive-descent arg parser is DELETED outright (the keyword-omitted D2 form re-parses its captured
text through the SAME `functionArgList` rule via `Frontend.Parsing.FunctionArgFragment`; `UdfBinder` binds through
the same `BindArgOperand`); the `IntrinsicRenderer` STATIC channel is DELETED (one instance channel over the ONE
`NumericRenderer` under `ReceiverContext.None`, `RuntimeApi`-routed, off the ratchet whitelist; the public render
entries are save/restore re-entrant). The legacy oracle consumes the reshaped CST via the thin
`MapFunctionArgTokens` shim (behavior-identical, NIST-proven). ONE documented deferral stands: subscript / ref-mod
/ RedefView-offset INDEX expressions stay transitional STRINGS, folding into D10 (PHASE 15; D10.4 is now mostly
pre-empted — residual scope recorded in `DESIGN-frontend-grammar.md §9.5` + PHASE-15 CUT 2.5). Read the PHASE-07
STATUS banner for the full as-landed record, then execute:

- **Exec Step E — edition-gate remediation (plan §4.1 / task #13):** fold the ~15 inline binder edition gates into
  the two-arm `VersionConformancePass`, delete the orphaned `GateId` scaffolding, and correct the
  "edition-agnostic" over-claims in the P2/P3 docs.

**Working discipline in force:** BATCHED cycles (multiple sub-steps per battery run) with PIPELINING (batch N's
conformance runs on the prebuilt binaries in the background while batch N+1's edits are authored); commits are
verdict-gated as separate actions, never `&&`-chained. P7 pickups still queued: SymbolTableBuilder-owned storage;
route `ReferenceResolver.ResolveUnqualified` + the StatementBinder condition lookup through the `SymbolTable`; the
image-fact caching (the O(subtree) perf work).

**Execution order (§4.1, TOOLING-FIRST):**
- **A ✅** — the source-generated exhaustive bound-tree visitor (PHASE-07 Step 6): the 7 generated `IBound*Visitor` +
  `Accept` + `BoundStatementTree.StatementChildren`; every completeness-critical dispatch converted; every switch
  ISO-§-classified.
- **B ✅** — the Real Binder (P6): `Binding/BinderDriver.Bind` → immutable `Binding/Model/BoundCompilation`; the
  middle-end is the declared `BindPipeline.GroupTail` manifest (ProcedureBinding → UsageCollectionPass →
  StorageFormPass → the `VersionConformancePass` terminal pass), ONE validated DAG; 14 CodeGen-read binder
  collections sealed `IReadOnly`; the ONE scope-aware `SymbolTable` (`TryResolve`/`TryResolveIndex`/`IndexCellOf`,
  explicit `Scope`, per binder). OO bind bodies stay on the emitter behind `IOoBindHost`+`BindSession` (the P6→P9
  seam).
- **C ✅** — PHASE 05 the unified data model: the `StoreAsImage` FLAG is gone — `Storage` (the ONE `StorageForm`) is
  computed once by the group-tail `StorageFormPass` from collected facts, the name kept as the read-only projection;
  `RecordLayout` is the ONE phase-free width/offset authority (§13.18.44.3 SR8 enforced, COBOLNET1539); the data
  model in `Binding/Model/` (`PlaceDecorator` base; `StrongTypeModel` + `PictureAnalyzer`; sentinels → `DataItem.
  Pending`); the tier verdict single-sourced through `RedefinesClass.Classify`; ONE `UsageInheritancePass`.
- **D ✅** — PHASE-07 complete (Steps 1–12: both god classes dissolved; structural `Place`; FUNCTION-arg grammar +
  the `IntrinsicRenderer` static-channel deletion).
- **E ✅** — edition-gate remediation complete: the ~19 inline gates folded into the two-arm
  `VersionConformancePass` (20 registry rows, all in the matrix); `GateId` deleted; claims reconciled onto the
  §1.1 gating-exception ledger.
- **F ◐ (NOW)** — PHASE 08–16: runtime reorg, M2/M3/M4 feature waves, version-matrix closure, G8 legacy cut, CIL backend.

**Done:** Phases 00–07 (migration safety net · frontend rename · `Cobol.Net.Editions` leaf + diagnostic registry ·
version-conformance pipeline [the two-arm `VersionConformancePass` is the SOLE edition gate] · frontend consolidation
· the unified data model · the Real Binder · the visitor/god-class/`Place`/FUNCTION-arg decomposition).
D10 SUBSCRIPT-mode
removal is RELOCATED → PHASE 15 §"CUT 2.5" (D10.4 mostly pre-empted by P7 Step 12). ⚠ Flagged latent (not
blocking): `OoReparent{Class,Factory}Data` mis-bind the class-level env (CURRENCY/ALPHABET/SELECT) — a dedicated
fix ~PHASE 09.

**Battery (keep green + pushed at EVERY commit):** 3166 conformance · 282 unit · 33 characterization (32 snapshots
byte-exact + the RuntimeApi ratchet) · FULL legacy guard NIST 353 MATCH. ⚠ Build `CobolSharp.sln` before `dotnet
test --no-build` — a stale test-bin compiler DLL hides greenfield regressions.

---

## ⛔ NON-NEGOTIABLE PROCESS RULES (owner-emphasized — obey BEFORE writing any code)
Durable copies: `feedback_use_the_spec`, `feedback_follow_design_docs_and_spec`, `feedback_spec_scopes_not_tests`.
1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL.md`) defines the correct behavior for EVERY case.** Whenever any
   question of semantics / syntax / output / edge-case arises, READ the spec — the SPECIFIC governing §/GR, not the
   nearest general sentence — and CITE it in code + DEVLOG. Never guess, never infer behavior from the legacy oracle or
   a green corpus (they are regression nets with known non-conformances, NOT authority). **A "faithful, byte-neutral"
   refactor proves NO-REGRESSION, never CORRECTNESS — validate the logic against the §.**
2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/COBOLNET_DESIGN.md` §0.5 lists them). Read
   the doc, FOLLOW it; do NOT improvise. The deep-dives are decision-complete SSOTs; `COBOLNET_DESIGN.md` wins for locked
   invariants (§1), cross-cutting (§14), settled decisions (§18), build order (§16).
3. **Implement the COMPLETE feature to the spec + design — NEVER scope to what a test references.** The corpus VERIFIES;
   it does NOT bound what to build. Legitimate STAGING is by spec/design structure, never by test coverage.
4. **Keep the deep-dive docs CURRENT.** When the SSOT (or a new decision/finding) supersedes a deep-dive, UPDATE it in
   the SAME change set to state the current design. Docs describe the CURRENT compiler — the historical narrative lives
   only in `DEVLOG.md`.
5. **Never work around or hack — REDESIGN/rearchitect for the best possible design; leverage the tooling.** Any new tree
   traversal uses the ONE generated/shared visitor (bound tree: `IBound*Visitor`/`StatementChildren`) or the ANTLR
   generated visitor/listener (CST), never a fresh bespoke `switch`. After finishing work, retroactively audit related
   prior work for similar shortcuts. ([[feedback_no_workarounds_root_cause]], [[project_path_a_leverage_tooling]],
   [[feedback_singular_pattern]].)

**Standing operating rules (in memory — still in force):** guard-green (the CobolNet differential+unit suites for
greenfield work) before EVERY commit; a `tests/…` conformance test + a DEVLOG entry ship in the SAME commit as each
feature; commit AND push every checkpoint (never ask "should I continue/push"); run autonomously and continue
immediately when work is pending (don't stop to ask, don't ScheduleWakeup to wait); adversarially-review non-trivial
features. (Memories: `feedback_fully_autonomous_push`, `feedback_continue_dont_wait`,
`feedback_conformance_tests_per_feature`, `feedback_devlog_per_commit`, `feedback_complete_dotnet_migration_no_byte`,
`feedback_commercial_quality_north_star`.)
