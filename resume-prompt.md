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

## ⛔🔀 RESUME AT — PHASE-11 (deferred-intrinsics backlog to zero + the Tier-C REDEFINES confined-byte codec)

**PHASE-10 IS COMPLETE (2026-07-17, DEVLOG 854–870 — 14 battery-gated commits `7436a1ef`→`a0fd3f68`+close, CI
green on each; the P10 doc's STATUS banner carries the exit-criteria confirmation + the NAMED forward-residue
ledger).** Every M2 non-OO track is landed on the greenfield substrate or staged loud by name. The waves, in
landing order (each spec-first, line-reviewed, full-battery-gated): NATIONAL intrinsics (the ONE `Repertoire`
translator) · EC `-N` twins + CHAR-NATIONAL · `&`-CONCAT (compile-time fold, `ConcatFolder`) · ALLOCATE
INITIALIZED (§14.9.3 GR7 through the ONE InitializeBinder) · UDF category-RETURNING (§8.4.3.2.4 GR1) ·
CONSTANT entries + CONSTANT RECORD (§13.10/§13.18.15 — the compile-time substitution table
`DataBinder.Constants.cs`; CONSTANT+AS joined the §8.9 interval machinery, AS nameSlot-ONLY [FU-1]) ·
PROGRAM-POINTER + qualified/subscripted ADDRESS OF (the `ProgramPointer` outermost-identity carrier on the ONE
`ProgramTable`; `ResolveForAddressOf` on the ONE cell-offset formula) · FILE-LOCK (§9.1.16 record locks on
EVERY organization [sequential = ordinal identity]; RETRY on the mutating verbs; DELETE FILE **62** = the
sharing conflict §9.1.13.9 item 2 — the audit had mislabeled it; the ONE `LockGoverned` neutrality hinge) ·
UDF BY VALUE + PER-EVALUATION activation (§14.2.3 GR10 detached cells on the ONE CALL ABI;
`BoundUdfEvaluated` IIFE windows — C# short-circuit IS the COBOL rule; 1509 narrowed to 3 named shapes) ·
RECURSIVE-WS (§13.5.4 GR1 static WS for `Recursive&&!Initial` incl. every FUNCTION, `__ResetStatics` at
registration/CANCEL; program LOCAL-STORAGE was silently UNBOUND — now binds per §13.6.4 GR1) ·
ARITH-STANDARD (the six SDIDI residuals: `CobolDec.Pow`, decimal128 range ECs, float→SDIDI operands, MEAN;
the ARITHMETIC gates RE-TIMED to 2002 with the full 0900/0903/0807 lifecycle; the ENTRY-CONVENTION funnel
bug fixed) · TYPEDEF residue (SAME AS §13.18.49 on the ONE `CloneItem`; EXTERNAL type §13.18.22 LIVE [the
1534 stage's § citation was wrong]; strong groups compare element-wise §8.8.4.2.12; ExpandType was DROPPING
template root clauses — fixed via the shared `CopyEntryDescription`) · RW-2002 (PRESENT WHEN §13.18.41 +
VARYING §13.18.64 — whose SR1 pulled multiple/relative COLUMN in; the absent-entry line-collapse model;
1559) · ALPHABET-NATIONAL (FOR NATIONAL + UCS-4/UTF-8/UTF-16 on the ONE Collating subsystem; the sparse
`NationalCollatingTable`; **UCS-4 ≡ NATIVE proven via §8.5.1.4** — surrogate pairs are two character
positions by spec, so the divergence is unreachable; GR7 Table 6 coded-set-only rejections).
**⚠ THE P10 RECURRING LESSON: the Step-1 audit itself drifted in ~5 of 6 re-checked claims** — wrong §
citations (EXTERNAL-type "GR5", RW "§13.18.44"), already-landed "gaps" (strong relations, ARITH consumption,
per-occurrence UDF activation), a mislabeled status code (62 vs 71) — EVERY wave must re-scout its anchors
spec-first before implementing. Battery at close: **3467 conformance · 292 unit · 33 characterization
byte-identical · legacy 1196+636 · NIST 353 MATCH.**
**NEXT: PHASE-11** (`docs/rearchitecture/PHASE-11-intrinsics-backlog-tierc-codec.md`, STATUS: NOT STARTED —
read its §0/steps; it can run parallel to P12 per its header): every remaining `IntrinsicBind.Deferred`
catalog row → Runtime (BYTE-LENGTH unblocks the staged `CONSTANT AS BYTE-LENGTH OF`; the EC-N 2023 arg
forms live there too) + the Tier-C REDEFINES confined-byte codec. The P10 forward residues (per-shape 1510,
OPTIONAL formals, recursive-WS stages, OO class-unit BASED, INITIALIZE-over-pointers, line-seq 06/09/71,
keyed GR10a FPI, cross-run-unit sharing, multiple-LINE, narrowed-1509 shapes, signed-leaf strong ordering,
MAX/MIN-under-collating) are ledgered IN the P10 doc's STATUS banner; B-SHIFT/BX + STANDARD-BINARY are
2023/2014 items for P12/P13.

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
  explicit `Scope`, per binder). the P6-era `IOoBindHost` OO seam is
  DELETED (P9 Step 4 — `Oo/OoDriver` owns the bind bodies).
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
- **F ◐ (NOW)** — PHASE 08–16: **PHASE-08 ✅** (runtime reorg onto `RunUnit`) · **PHASE-09 ✅** (M2 OO
  rearchitect + completion; see the RESUME banner); next PHASE-10 (M2 residual catalog) → 11–14 (feature
  waves + matrix closure) → 15 (G8 legacy cut) → 16 (CIL backend).

**Done:** Phases 00–09 (migration safety net · frontend rename · `Cobol.Net.Editions` leaf + diagnostic registry ·
version-conformance pipeline [the two-arm `VersionConformancePass` is the SOLE edition gate] · frontend consolidation
· the unified data model · the Real Binder · the visitor/god-class/`Place`/FUNCTION-arg decomposition · the
runtime-library reorg onto `RunUnit` · the M2 OO rearchitect+completion).
D10 SUBSCRIPT-mode
removal is RELOCATED → PHASE 15 §"CUT 2.5" (D10.4 mostly pre-empted by P7 Step 12). 
**Battery (keep green + pushed at EVERY commit):** 3275 conformance · 281 unit · 33 characterization (32 snapshots
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
