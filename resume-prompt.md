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

## ⛔🔀 RESUME AT — EXEC STEP D: PHASE-07 **Step 11** (structural `Place`), then Step 12

**Both god classes are dissolved.** PHASE-07 Steps 1–10 are COMPLETE — the EMITTER (Step 9) and the BINDER (Step 10)
are decomposed. Read the PHASE-07 STATUS line first
(`docs/rearchitecture/PHASE-07-visitor-dispatch-emitter-decomposition.md`), then execute:

- **Step 11 — structural `Place` + `PlaceRenderer` (highest risk; per-SUBTYPE battery gating).** Introduce the
  structural `Place` shapes (`DESIGN-codegen-backend.md §2.3`) — `MemberPlace`/`RefModPlace`/`RedefViewPlace`/
  `NumericImagePlace`/`RenamesPlace`/`CapacityRegisterPlace` over an `AccessPath` of `AccessSegment`s with `BoundExpr`
  subscripts — and move the ENTIRE current `Place.cs` (`Binding/Model/Place.cs`) render logic into
  `CodeGen/Roslyn/PlaceRenderer`; `Place` carries structure, not C# strings. Do it ONE subtype at a time behind the
  P0 differential/snapshot harness (output MUST be byte-identical pre/post each subtype).
- **Step 12 — FUNCTION args as real expressions** through the ONE `ExpressionBinder`/`BindExpr` (the lexer-mode
  blocker + the space-separated-argument hazard; the `ArgPrimary` recursive-descent parser is DELETED; a shared
  `.g4` change ⇒ FULL legacy guard).

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
- **D ◐ (NOW)** — the rest of PHASE-07 (Steps 11–12 above).
- **E** — P2/P3 edition-gate remediation: fold the ~15 inline gates into the two-arm `VersionConformancePass`,
  delete orphaned `GateId`, correct the "edition-agnostic" over-claims.
- **F** — PHASE 08–16: runtime reorg, M2/M3/M4 feature waves, version-matrix closure, G8 legacy cut, CIL backend.

**Done:** Phases 00–06 (migration safety net · frontend rename · `Cobol.Net.Editions` leaf + diagnostic registry ·
version-conformance pipeline [the two-arm `VersionConformancePass` is the SOLE edition gate] · frontend consolidation
· the unified data model · the Real Binder) and PHASE-07 Steps 1–10 (both god classes dissolved). D10 SUBSCRIPT-mode
removal is RELOCATED → PHASE 15 §"CUT 2.5". ⚠ Flagged latent (not blocking): `OoReparent{Class,Factory}Data`
mis-bind the class-level env (CURRENCY/ALPHABET/SELECT) — a dedicated fix ~PHASE 09.

**Battery (keep green + pushed at EVERY commit):** 3166 conformance · 281 unit · 33 characterization (32 snapshots
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
