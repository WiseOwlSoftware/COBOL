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

## ⛔🔀 RESUME AT — PHASE-09 (M2 OO rearchitect & complete)

**PHASE-08 IS COMPLETE (2026-07-15, DEVLOG 841–843 — three verdict-gated commits, battery green at each).**
The runtime is role-foldered (`Values/{Numeric,Text,Tables}`, `Verbs/`, `Control/Signals/`, `IO/Sharing/`;
`ProgramRegistry.cs` split one-concern-per-file); `Pow10` is the ONE table-driven power-of-ten (six loop copies
deleted); `RecordFraming` is the ONE on-disk framing; the abstract `FileConnector` base (ISO §9.1.13 status
machine + the §14.9.30/§14.9.35 read-position pair) underlies `Sequential/Relative/IndexedConnector`; ONE
polymorphic `FileRegistry` replaced the `Keyed*` fallthrough dispatch (deleted, singular pattern); **`RunUnit`
(ambient `AsyncLocal`, LAZY `Current`) owns ALL run-unit-lifetime state** — `ProgramTable` / `ExceptionEngine` /
`ExternalTable` / `ModuleStack` / `SwitchStore` / `FileRegistry` / `IClock Clock` — and every pre-P8 static name
(`ProgramRegistry`/`ExceptionState`/`ExternalStore`/`CobolModule`/`ExternalSwitches`/`CobolFile`/`AcceptSource`)
survives as a thin emitted-surface shim over `RunUnit.Current`, so the emitted C# is byte-stable (ZERO
compiler-side changes in the phase; the G8 namespace flip is the one remaining DESIGN-runtime-library item).
Surviving statics are genuinely immutable only (`ExceptionCatalog`, `Pow10`, `SystemClock.Instance`).
Next: **PHASE-09** (`docs/rearchitecture/PHASE-09-m2-oo-rearchitect-and-complete.md` — read its STATUS line
first; the P6→P9 seam note: OO bind bodies still live on the emitter behind `IOoBindHost`+`BindSession`, and the
flagged-latent `OoReparent{Class,Factory}Data` class-env mis-bind (DEVLOG 738) is P9's to fix).

**Working discipline in force:** BATCHED cycles (multiple sub-steps per battery run) with PIPELINING (batch N's
conformance runs on the prebuilt binaries in the background while batch N+1's edits are authored in the
worktree; the INDEX is staged at battery launch so the committed tree is EXACTLY the tested tree); commits are
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
- **F ◐ (NOW)** — PHASE 08–16: **PHASE-08 ✅ (runtime reorg — `RunUnit`/`FileConnector`/`FileRegistry`/`Pow10`/
  role folders; see the RESUME banner)**; next PHASE-09 (M2 OO) → 10–14 (feature waves + matrix closure) →
  15 (G8 legacy cut) → 16 (CIL backend).

**Done:** Phases 00–08 (migration safety net · frontend rename · `Cobol.Net.Editions` leaf + diagnostic registry ·
version-conformance pipeline [the two-arm `VersionConformancePass` is the SOLE edition gate] · frontend consolidation
· the unified data model · the Real Binder · the visitor/god-class/`Place`/FUNCTION-arg decomposition · the
runtime-library reorg onto `RunUnit`).
D10 SUBSCRIPT-mode
removal is RELOCATED → PHASE 15 §"CUT 2.5" (D10.4 mostly pre-empted by P7 Step 12). ⚠ Flagged latent (not
blocking): `OoReparent{Class,Factory}Data` mis-bind the class-level env (CURRENCY/ALPHABET/SELECT) — a dedicated
fix ~PHASE 09.

**Battery (keep green + pushed at EVERY commit):** 3256 conformance · 281 unit · 33 characterization (32 snapshots
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
