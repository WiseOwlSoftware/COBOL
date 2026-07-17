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

## ⛔🔀 RESUME AT — PHASE-10 (M2 residual catalog: national/boolean, pointers, UDF, file-2002, RW/CONSTANT/concat)

**PHASE-09 IS COMPLETE (2026-07-16, DEVLOG 844–853 — 8 verdict-gated commits; all 8 exit criteria hold; the
PHASE-09 doc's STATUS block carries the full checkoff + deviations + deferral ledger).** OO lives in `Oo/`
(`CobolNet.Compiler.Oo`): the PURE `OoClassTable`, `OoConformance` (AdapterPairs RETURNED →
`BoundCompilation.OoAdapters`), the phase-explicit `OoMethodBinding`, `OoDriver` owning the OO bind bodies
(**`IOoBindHost` is DELETED**; `CSharpEmitter` = only `Bind`/`EmitBound`), `NamingConvention` (accessor names +
`__FACTORY`/`__Instance`/`__New` + the `::EXT::`/`::INST::`/`::FACT::` bands — the runtime's `::EXT::` read is a
documented WIRE CONTRACT), and the former ambient flags gone (`ActiveMethodScope` scoped by `BindPositionScope`;
`OoIsClassUnit`/`OoCurrentClass`/`OoInFactory` compiler-enforced `init`). Feature closes: multi-base INHERITS
parses (§11.3.2 repetition) + rejects LOUDLY (COBOLNET0849); **ANY LENGTH §13.18.2 on the method + contained-
program + function legs** (COBOLNET1542 SR family; RETURNING leg staged loud via `any-length-returning`); the
§4.2.2 interface conformance leg proven (`oo_interface_conformance` + the 0828 lossy-projection negative); the
14 legacy OoTests re-landed (`OoPortedTests` — 4 ported + 10 covered, audited in-file); the DEVLOG-738 latent
class-env shadow bug FIXED (`DataBinder.EnvDivisions` outermost-first over EVERY former singular env read;
golden `oo_class_env`). Deferrals (all in `docs/ISO2023_CONFORMANCE_PLAN.md`): GOBACK status-phrase → P13;
`>>PROPAGATE` directive semantics → P13. SPEC CORRECTION: USE Formats 3/4 carry NO [GLOBAL] phrase (§14.9.49.2).
**PHASE-10 IS IN PROGRESS (DEVLOG 854–858+):** Step-1 reconciliation audit DONE (the 13-track verdict table +
evidence is IN the P10 doc under §Step-1 AUDIT RESULT); WAVES LANDED (each spec-first, agent-implemented,
human-reviewed, full-battery-gated, CI-green): the NATIONAL wave (DISPLAY-OF §15.26 + NATIONAL-OF §15.66 on
the ONE `Repertoire` translator; the N-literal Latin-1 0814 guard lifted), the EC `-N` wave (EXCEPTION-FILE-N
§15.29 + EXCEPTION-LOCATION-N §15.31 + CHAR-NATIONAL §15.16; ORD-over-national corrected to §15.70.4 r2; the
2023 connector-arg form staged → P13), the CONCAT wave (§8.8.3 as a COMPILE-TIME literal fold inside
`nonNumericLiteral` — `ConcatFolder` is the ONE chokepoint; 1540/1541/1545), the ALLOCATE wave (§14.9.3
GR7 INITIALIZED = the spec's own INITIALIZE-equivalence lowered through the EXISTING InitializeBinder;
GR6 CHARACTERS = binary zeros), the UDF category-RETURNING wave (§8.4.3.2.4 GR1 — 1510 lifted for
alnum/group/edited/national; per-shape residues stay 1510), the CONSTANT wave (§13.10 constant entries
= the COMPILE-TIME substitution table `DataBinder.Constants.cs` + §13.18.15 CONSTANT RECORD; 1547/1548/1549;
CONSTANT + AS joined the §8.9 interval-word machinery — AS is nameSlot-ONLY, the FU-1 ledger; DEVLOG 861),
and the POINTERS wave (Steps 6+7, DEVLOG 862 — USAGE PROGRAM-POINTER end-to-end on the ONE ProgramTable
[`ProgramPointer` carrier = the outermost identity; `setEntryStatement` + CALL-through-pointer + SameTarget
relations; FUNCTION-POINTER superset-parsed, staged 0899, 2014 interval]; qualified/subscripted ADDRESS OF
lifted via `ResolveForAddressOf` on the ONE cell-offset formula; class-unit BASED + INITIALIZE-over-pointers
= named residues).
Read the P10 doc's checkboxes for the live step state; remaining waves:
UDF BY VALUE+RECURSIVE · file-sharing extension · line-seq 62 ·
ARITH-STANDARD consumption · B-SHIFT · TYPEDEF SAME AS · ALPHABET-national.
(RW-2002 landed 2026-07-16 — Step 13: PRESENT WHEN + VARYING + multiple/relative COLUMN on the existing RWCS;
COBOLNET1559 SR family; golden `2002/rw_present_when`; multiple-LINE repetition staged 0899 with the
report-group OCCURS family.)

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
