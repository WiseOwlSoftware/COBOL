# COBOL.NET — Next-Session Kickoff Prompt

⛔🔥 **What this project is (PIVOT, DEVLOG 457):** a blank-slate compiler translating COBOL → idiomatic typed-native C#
compiled by Roslyn — a COBOL record IS a .NET `record struct`, an elementary item IS a native field. **There is NO byte
`ProgramState` substrate — never reintroduce it, never "fall back" to the legacy byte engine** (`CobolSharp.Compiler` is
a differential oracle only, deleted at G8; a `byte[]` appears only at a genuine REDEFINES Tier-C / file boundary).

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
  `date "+%Y-%m-%d %H:%M %Z"` stamp. Full session history lives here (this banner stays lean).

## ⛔🔀 RESUME AT — EXEC STEP D: the rest of PHASE-07 (structural `Place` + god-class decomposition + the backend seam)
**Structural `Place` (no C# text in the bound tree) + the binder/emitter god-class decomposition + the
`ICodeGenBackend` seam** (`docs/rearchitecture/PHASE-07-*.md` — read its STATUS line first; its Step 6, the generated
visitor, was pulled forward and is DONE). P7 pickups noted in DEVLOG 773: SymbolTableBuilder-owned storage; route
`ReferenceResolver.ResolveUnqualified` + the StatementBinder condition lookup through the SymbolTable; the image-fact
caching (deferred from P5 Step 10 — the O(subtree) perf work belongs with the DataItem slimming).

**Execution order (§4.1, owner-directed TOOLING-FIRST, 2026-07-11):**
- **A ✅ DONE** — the source-generated exhaustive bound-tree visitor (PHASE-07 Step 6 + the 6h SYSTEMATIC AUDIT; DEVLOG
  755–766): the 7 generated `IBound*Visitor` + `Accept` + `BoundStatementTree.StatementChildren`; every
  completeness-critical dispatch converted; every switch ISO-§-classified.
- **B ✅ DONE** — P6 the Real Binder (DEVLOG 767–774, commits `8ac37480`→phase close, adversarially reviewed): a REAL
  Binder phase — `Binding/BinderDriver.Bind` → immutable `Binding/Model/BoundCompilation` (bound model relocated:
  `BoundUnit`/`OoClassUnit`); the middle-end is the DECLARED `BindPipeline.GroupTail` manifest (ProcedureBinding →
  UsageCollectionPass → StorageFormPass → the `VersionConformancePass` as NAMED terminal pass), ONE validated DAG with
  the resolve prefix + a DEBUG watermark gate (`DataBinder.Watermark`/`Require`); driver Phase 2 = Bind → gate →
  CheckOnly → EmitBound; 14 CodeGen-read binder collections sealed `IReadOnly` (zero CodeGen writes, grep-proven); the
  lookup QUADRUPLE deleted → the ONE scope-aware `SymbolTable` (`TryResolve`/`TryResolveIndex`/`IndexCellOf`, explicit
  `Scope`, per binder). OO bind bodies stay on the emitter behind `IOoBindHost`+`BindSession` (the documented P6→P9
  seam). Deviations recorded in the PHASE-06 STATUS ledger.
- **C ✅ DONE** — PHASE 05 complete (DEVLOG 776–785, commits `7b22f10a`→ phase close, CI-green): the `StoreAsImage`
  FLAG is DEAD — `Storage` (the ONE `StorageForm`) computed once by the group-tail `StorageFormPass` from COLLECTED
  facts (`ImageForcedItems` + `WholeGroupReferenced`), the name kept only as the read-only projection; `RecordLayout`
  = the ONE phase-free width/offset authority (6 geometry copies deleted; §13.18.44.3 SR8 now ENFORCED —
  COBOLNET1539, failing-first `KeyedOffsetSpecTests`); the data model in `Binding/Model/` (`PlaceDecorator` base;
  `StrongTypeModel` + `PictureAnalyzer` extracted — `PicInfo` a pure value record, the skeleton scaffolding +
  reference-identity sentinels replaced by `DataItem.Pending` + `PicInfo.Recovery`); the tier verdict single-sourced
  through `RedefinesClass.Classify`; ONE `UsageInheritancePass`; the §8.3.1.2 apostrophe-VALUE goldens PROVEN
  failing-first. All 7 exit criteria hold; deviations in the PHASE-05 ledger.
- **D ◐ (NOW)** — the rest of PHASE-07 (above).
- **E** — P2/P3 edition-gate remediation (task #13): fold the ~15 inline gates into the two-arm `VersionConformancePass`,
  delete orphaned `GateId`, correct the "edition-agnostic" over-claims.
- **F** — PHASE 08–16: runtime reorg, M2/M3/M4 feature waves, version-matrix closure, G8 legacy cut, CIL backend.

**Done:** Phases 00–06 ✅ (migration safety net · frontend rename · `Cobol.Net.Editions` leaf +
diagnostic registry · version-conformance pipeline [the two-arm `VersionConformancePass` is the SOLE edition gate] ·
frontend consolidation · the unified data model [closed 2026-07-11] · the Real Binder). D10
SUBSCRIPT-mode removal was RELOCATED → PHASE 15 §"CUT 2.5". ⚠ Flagged latent (not blocking):
`OoReparent{Class,Factory}Data` mis-bind the class-level env (CURRENCY/ALPHABET/SELECT) 0/1/2× — a dedicated fix
~PHASE 09 (DEVLOG 738).

**Battery (keep green + pushed at EVERY commit):** 3166 conformance · 281 unit · 32 characterization byte-exact · FULL
legacy guard NIST 353 MATCH. ⚠ Build `CobolSharp.sln` before `dotnet test --no-build` — a stale test-bin compiler DLL
hides greenfield regressions ([[feedback_fresh_build_before_no_build_test]]).

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
   the SAME change set — state the current design AND why the original was not followed (cite the SSOT §/DEVLOG).
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
