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

## ⛔🔀 RESUME AT — EXEC STEP D: PHASE-07 **Step 10** (binder decomposition), then 11 → 12
**Read the PHASE-07 STATUS line first** (`docs/rearchitecture/PHASE-07-visitor-dispatch-emitter-decomposition.md`)
— it carries the per-substep hashes and the AS-BUILT PLAN + 9n as-landed notes in its §Step 9. State as of
2026-07-11 (DEVLOG 786–810, tree `72ec6633`, battery green):
- **P7 Steps 1–8 ✅** (ICodeGenBackend seam · AssemblyPackager · immutable `EmitContext`+`ReceiverContext` ·
  `RuntimeApi`+`FigurativeConstants` + the bare-`Cobol*.` RATCHET (`RuntimeApiGuardTests`) · ACCEPT renames ·
  generated visitor (pulled forward) · `MoveKind` on `BoundMove` · Step 8 = done-by-P5).
- **Step 9 ✅ COMPLETE (9a–9n + the 9-final ratchet sweep; DEVLOG 794–810)** — the emitter god class is
  DISSOLVED: `ProgramEmitter` (run-unit orchestration; owns `NameAllocator` + the `DispatchState`/`EcState`/
  `CallUnitState` state model + the LIVE `Current` root) → `UnitEmitters` (the PER-UNIT composition root —
  acyclic ctor order, the cyclic edges verbs↔`StatementEmitter`↔`EcEmitter` + KeyedIo↔SeqIo property-wired) →
  `DispatchEmitter` + `StatementEmitter` (the 79-Visit generated `IBoundStatementVisitor<bool>`) + 18
  `Verbs/*Emitter` + `EcEmitter` + the `DataDivision/` five behind `DataEmitter`. Emission reads ZERO bind-host
  session state (OoEmitter takes `comp.OoClasses`/`comp.InterfaceData`, reads the per-unit set via `Current`
  LIVE — class units re-new it mid-run). **`CSharpEmitter` = the thin bind-host facade ONLY** (Bind/EmitBound +
  `IOoBindHost` + the OO BIND half — the recorded deviation, until P9). The ratchet whitelist is down to the
  FOUR pre-Step-11/12 renderers (IntrinsicRenderer 47 · NumericRenderer 17 · ConditionRenderer 17 ·
  OperandText 7 — they route at Steps 11/12); recorded counting refinement: comment lines stripped,
  `CobolRounding.`/`CobolPassMode.` typed-enum accesses excluded (emitted forms route via
  `RoundingText`/`PassModeText`).
- **Step 10 ◐ IN PROGRESS — batches 10a–10e ✅ (DEVLOG 811–815):** 10a `34410d7f` (SymbolTable residuals) ·
  10b `a01b5c77` (`PhraseBlocks`) · 10c `2f4acded` (`BinderContext` + `StatementValidation` [Check*
  convention] + `Verbs/InspectBinder`) · 10d `706ac699` (`Verbs/{Evaluate,StringUnstring}Binder`) · 10e
  `1980f941` (the MOVE-family tier: `Verbs/{Move,Corresponding,Initialize}Binder` — MoveBinder absorbs
  .MoveFigurative with the MarkImageForced choreography byte-preserved; the ONE `_corrCounter` /
  `_initializeLoopVar` owners; CheckStrongMove/CheckComposite/INITIALIZE lifts; the `:219` ≥2002 window =
  the sanctioned in-binder BEHAVIORAL edition read). 10f `bf98ba2d` (`Verbs/ReportWriterBinder` ctx-only; `ResolveReceiving` hoisted to
  the core receiving spine; the 1523 catalog normalization deferred to 10t) · 10g `9c0f257a`
  (`Verbs/FileLockBinder` + `Verbs/PtrBinder` — tri-state/peek/ByName-bypass verbatim). 10h `b28907b7`
  (the I/O cycle: `Verbs/{SequentialIo,KeyedIo}Binder` + `MnemonicRegistry` on ctx + the AcceptDisplayBinder
  DETACH incl. BindDisplay; OPEN SR8 + WRITE SR19/18/13 lifts). 10i `0bae9f55` (`Verbs/SortBinder`) · 10j `4a4091ee`
  (`Verbs/CallBinder` — Oo/Ec host hooks outlive per plan). 10k `924e5978`
  (`Verbs/{Intrinsic,Udf}Binder` — the GR12 pair; CompileClock → IntrinsicBinder; injection surface stays
  host-side; PendingCount mark/drain seam). 10l `c49e7ab7` (`Verbs/ControlFlowBinder` — ONE class,
  recorded deviation from the three-class sketch). 10m `394003ef` (`Verbs/{Set,Search}Binder`). 10n ✅ (`Verbs/SetAlterBinder` — the last plain
  verb). 10o `a733bb30` (`Verbs/ConditionBinder` — the whole
  channel merged; deviations recorded: host forwarders [one-shot flip at 10t]; SR pure-lift deferred to the
  10t sweep) · 10p `5034a3c2` (`Verbs/ArithmeticBinder` — the five arithmetic verbs; BindSizeError stays a
  HOST phrase family past 10q) · 10q `b7dfe0b4` (`Binding/Procedure/ExpressionBinder` — the expression-spine
  flip; 15 host forwarders; core 405 lines) · 10r `140d2e86` (`Verbs/EcBinder` + `EcBindState` on ctx +
  `ctx.BindCursor`; the Exceptions partial DELETED; ConfigureEc a host PUBLIC forwarder until 10t;
  intrinsic-presence walks VERBATIM — generated-visitor conversion FLAGGED) · 10s `de9901cc`
  (`Verbs/OoBinder` — the OO half LAST; ride-alongs → records-only `Bound/BoundOo.cs`;
  `SectionInfo`/`OoMethodScope` promoted top-level; the ambient quadruple REPLACED by the scoped
  `ctx.EnterMethodScope` token in both bind loops; BindMethodRoster + emitter-set OO props +
  InMethod/OoScopeHint/_paraMethod stay HOST until 10t/P9). **RESUME AT 10t** (FINAL WIRING —
  ProcedureTableBuilder [+ the Declaratives half] · thin StatementBinder · ALL host edges/forwarders
  deleted + collaborator re-points [BinderDriver/CSharpEmitter.Oo ConfigureEc] · the deferred 1511/
  relational-SR pure-lifts (10o dev-b) · the 1523 catalog-descriptor normalization · the FULL doc sweep:
  topology reconciliation [.Boolean→ConditionBinder at Verbs/, .Exceptions→EcBinder, ControlFlowBinder
  ONE-class deviation, ExpressionBinder at Procedure/ root], stale clone/pin rows, DOC_INDEX, memory)
  → 10s (OoBinder LAST, behind the OO goldens + method-scope tests) → 10t (FINAL wiring:
  ProcedureTableBuilder + thin StatementBinder + host-edge deletion + the full doc sweep incl. the deferred
  SR pure-lifts and the 1523 catalog normalization) per the PHASE-07 §Step 10 **AS-BUILT PLAN block**
  (census `wf_b788936b-ca2`, scratchpad p7-step10-census.json, session 61dab794). Standing rules for
  the batches: SR checks lift to `StatementValidation` as PURE checks only (error+placeholder control flow
  stays in the verb binder); the residual ~19 inline edition gates move VERBATIM (pass-folding = Exec Step
  E); ProcedureTableBuilder at the 10t FINAL wiring (the `OoMethodScope` entanglement); OO LAST (10s)
  behind the method-scope tests (R1). Then **Step 11** (structural `Place` — per-SUBTYPE battery gating) ·
  **Step 12** (FUNCTION args as real expressions — the lexer-mode blocker + space-separated-argument
  hazard; FULL legacy guard). The premise audit (`wf_8ace7f29-a1d`, scratchpad p7-audit.md, session
  3fbfd282-efa2-47fa-924c-31094eb1ed46) maps Steps 10–12's sites.
**Working discipline in force (DEVLOG 803/807):** BATCHED cycles (multiple sub-steps per battery run) with
PIPELINING (batch N's conformance runs on the prebuilt binaries in the background while batch N+1's edits are
authored); commits verdict-gated as separate actions (never `&&`-chained — the 783/792 lesson); ratchet
exact-sum accounting per mechanical split. P7 pickups still queued (DEVLOG 773): SymbolTableBuilder-owned
storage; route `ReferenceResolver.ResolveUnqualified` + the StatementBinder condition lookup through the
SymbolTable; the image-fact caching (the O(subtree) perf work).

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

**Battery (keep green + pushed at EVERY commit):** 3166 conformance · 281 unit · 33 characterization (32 snapshots byte-exact + the RuntimeApi ratchet) · FULL
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
