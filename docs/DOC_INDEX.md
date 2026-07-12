# COBOL.NET — Documentation Index

> **The map of the surviving docs.** Referenced by `CLAUDE.md`. **Keep in sync:** when you add, retire, or materially
> change a doc, update its row here. There is exactly **one canonical doc per subsystem** — extend it, never fork a second.
>
> ⛔🔥 **The project is a blank-slate COBOL→C#/Roslyn rewrite (COBOL.NET, `src/Cobol.Net.*`).** The authoritative design
> is **`docs/COBOLNET_DESIGN.md`** (the decision-complete SSOT). The pre-PIVOT byte-engine plan + architecture docs
> (MASTER_PLAN, PROJECT_PLAN, the DATA_MODEL_* / RECORD_STRUCT migration docs, the ~50 `CobolSharp …` architecture/guide
> docs, the CIL-emitter/binder decomposition docs, the byte-engine plans/audits, and the extracted spec excerpts) were
> **removed** as obsolete + misleading. Backend = **C# source via Roslyn (primary)**
> behind the backend-neutral `ICodeGenBackend` over ONE bound tree (`--backend roslyn|cil`; a Cecil/CIL backend is
> future-additive with its OWN private structure→branch lowering — NO shared lowered IR); numerics are native scaled
> `long`/`Int128` (+ IEEE `float`/`double` for COMP-1/2; no `decimal`/`BigInteger`); the legacy `CobolSharp.*` engine
> survives in `src/` ONLY as a
> differential oracle until cut-over (G8). The ISO spec is the submodule **`specs/ISO_COBOL.md`** (authoritative — the
> extracted excerpts were removed as redundant).

**Type legend:** **LIVE** = binding, keep current · **DESIGN** = target design (banner shows real status) ·
**LEDGER** = catalog/record · **SPEC** = ISO reference.

## Start here (live kickoff + plan)

| Doc | Type | Subject |
|---|---|---|
| `resume-prompt.md` | LIVE | **Read FIRST.** The session kickoff: mission (full ISO-2023 + all prior editions), current STATE, the two-track RESUME AT (version-correctness + the feature/NIST corpus drive), and the NON-NEGOTIABLE PROCESS RULES. |
| `docs/COBOLNET_DESIGN.md` | LIVE | **The SSOT.** Decision-complete design for the rewrite: locked invariants (§1), cross-cutting (§14), settled decisions (§18), the G0–G8 build order, and pointers to the per-subsystem deep-dives. |
| `docs/COMPLETION_ROADMAP_COUNCIL.md` | LIVE | **RATIFIED — the EXECUTION ROADMAP for G7→G8** (validator waves → M2 OO/residuals → M3 → M4 → matrix closure → G8 in 3 cuts), phase-by-phase with exit criteria. Upholds the SSOT §16 spine + 7 ratified deltas (its §3); its §5 decision packet is owner-resolved (all council defaults; #1 = no standards acquisition — the in-repo 2023 spec is the sole ISO authority). Read via `resume-prompt.md`'s STATE banner. |
| `docs/COBOLNET_REARCHITECTURE_PLAN.md` | LIVE | **The rearchitecture & 100%-ISO master roadmap.** A resumable, execution-grade plan: north-star 5-assembly architecture, 11 principles, the dual-backend §3 mandate (Roslyn↔CIL), a 17-phase dependency-ordered index (status checkboxes), an owner-decisions table, and §7 backfill refinements. Step-by-steps in `docs/rearchitecture/PHASE-00..16-*.md`; designs in `docs/rearchitecture/DESIGN-*.md`; execution ledgers in `docs/rearchitecture/PLAN-*.md` (e.g. `PLAN-bindtime-gating-migration.md` — moving edition introduction-gating to bind-time `ConstructRegistry.Check`); as-is survey/critique in `docs/rearchitecture/{SURVEY,CRITIQUE}-*.md`. **Phases 00–06 DONE** (P03 the version-conformance pipeline — the two-arm `VersionConformancePass` is the SOLE edition gate, binder edition-agnostic save the documented UDF exception; P04 frontend consolidation, D10 relocated → P15 §CUT 2.5; P06 the Real Binder — `BinderDriver`→immutable `BoundCompilation`, the declared `GroupTail` manifest with the conformance pass as NAMED terminal, sealed `IReadOnly` binder views, the DEBUG watermark gate, and the ONE scope-aware `SymbolTable` replacing the lookup quadruple). Per the §4.1 TOOLING-FIRST resequence: **Exec A ✅** (the source-generated exhaustive bound-tree visitor, PHASE-07 Step 6) · **Exec B ✅** (P06) · **Exec C ✅** (P05 the unified data model — the `StoreAsImage` FLAG deleted, `Storage` computed once by `StorageFormPass` with the name kept as the read-only projection; `RecordLayout` the ONE width/offset authority, §13.18.44.3 SR8 enforced COBOLNET1539; `Binding/Model/` + `PictureAnalyzer`/`StrongTypeModel`; sentinels → `DataItem.Pending`; `RedefinesClass.Classify`) · **◐ Exec D (NOW) = the rest of PHASE-07** — Steps 1–10 DONE (both god classes dissolved: the emitter → `ProgramEmitter`/`DispatchEmitter`/`StatementEmitter` + per-verb `CodeGen/Verbs/*Emitter` over the static `RuntimeApi`; the binder → `StatementBinder` dispatch + composition root + the `BinderContext` spine + per-verb `Binding/Procedure/Verbs/*Binder` + `Binding/Validation/StatementValidation`); RESUME AT Step 11 (structural `Place`) → Step 12 (FUNCTION-arg grammar). |
| `CLAUDE.md` | LIVE | Project instructions / agent playbook (points here + to the SSOT). |
| `docs/DOC_INDEX.md` | LIVE | This file — the doc map + maintenance guide (one row per surviving doc; keep in sync). |
| `DEVLOG.md` | LIVE | Narrative log of decisions/failures/breakthroughs. **DESCENDING — newest `## Entry` first**, with a `YYYY-MM-DD HH:MM TZ` stamp. |
| `CONSTRAINTS.md` | LIVE | Doctrine: anti-patterns, process rituals (some examples are byte-engine-era — the rules generalize). |
| `PROMPT.md` | LIVE | Doctrine + the anti-pattern catalog (multi-session continuity; C# 14 / .NET 10 **or later** — a .NET 11 upgrade is pre-authorized when it helps the goals). |
| `README.md` | LIVE | Repo front page. |

## COBOL.NET design corpus — one canonical deep-dive per subsystem

| Doc | Type | Subject |
|---|---|---|
| `docs/COBOLNET_ARCHITECTURE.md` | DESIGN | Brief overview of the greenfield architecture (companion to the SSOT). |
| `docs/COBOLNET_PIPELINE_DESIGN.md` | DESIGN | The compile pipeline (6 phases; edition gating is its OWN pass): Frontend (preprocess → superset ANTLR parse) → Bind (edition-agnostic; bound tree = ALL semantics) → `VersionConformancePass` (the sole edition gate; HALT on errors — emit is unreachable on an errored tree) → Desugar → `ICodeGenBackend` (Roslyn C#-emit primary; CIL future-additive) → Roslyn compile — emitters only render. |
| `docs/COBOLNET_DATA_MODEL_DESIGN.md` | DESIGN | Typed-native data model: groups→`record struct`, elementary→native fields, OCCURS→`T[]`, OCCURS DYNAMIC→out-of-line `CobolDynTable<T>` (D9), TYPEDEF/TYPE clause→a template registry + subtree clone (D17), `Place`/`ReferenceResolver`. |
| `docs/COBOLNET_NUMERIC_DESIGN.md` | DESIGN | Native scaled-integer numerics (`CobolNum`): scale/round, `TryStore`, ON SIZE ERROR, signed-DISPLAY. |
| `docs/COBOLNET_CONTROL_FLOW_DESIGN.md` | DESIGN | The PC dispatcher (`__Dispatch`): GO TO / DEPENDING / PERFORM (THRU/TIMES/UNTIL) / EXIT. |
| `docs/COBOLNET_REDEFINES_DESIGN.md` | DESIGN | REDEFINES/RENAMES — the 4-tier one-canonical-backing model. |
| `docs/COBOLNET_STRING_OPS_DESIGN.md` | DESIGN | INSPECT / STRING / UNSTRING / reference-modification (`CobolStrings`). |
| `docs/COBOLNET_FILES_DESIGN.md` | DESIGN | File I/O — connectors, FILE STATUS, OPEN/CLOSE/READ/WRITE/REWRITE state machines (§9.1.13). |
| `docs/COBOLNET_INTERPROGRAM_DESIGN.md` | DESIGN | CALL / interprogram linkage / nested programs. |
| `docs/COBOLNET_OO_DESIGN.md` | DESIGN | OO — classes/methods/INVOKE as typed-native .NET. |
| `docs/COBOLNET_OO_SLICE_BRIEFS.md` | LEDGER | OO — workflow-regenerated implementation briefs for the remaining slices (FACTORY / OVERRIDE-FINAL / universal / EC-OO / INTERFACE+PROPERTY); decisions fold into the OO deep-dive per slice. |
| `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` | DESIGN | Conditions + the EC exception model / declaratives. |
| `docs/COBOLNET_INTRINSICS_DESIGN.md` | DESIGN | Intrinsic FUNCTION resolution + semantics. |
| `docs/COBOLNET_REPORT_WRITER_DESIGN.md` | DESIGN | Report Writer — the `CobolReport` RWCS engine, compose-at-presentation lines, counters, CONTROL/SUM, USE BEFORE REPORTING (closes the SSOT's RW seam flag). |
| `docs/COBOLNET_PROJECT_ORG_DESIGN.md` | DESIGN | Project/folder organization + the Cobol.NET / `cobol.exe` naming (G0 executed; G8 namespace big-bang pending). |
| `docs/COBOLNET_VALIDATION_DESIGN.md` | DESIGN | **Diagnostics + the §8.9 reserved-word funnel:** the strict/permissive channels + `Removed()` seam, the 0900–0903 band, the four-source reserved-word tables + the cobolWord funnel, the ConstructDialectStatus registry + drift disciplines, the corpus runner shells, and the measurable G7 exit criteria. Edition GATING itself lives in the ONE `VersionConformancePass` — the canonical mechanism doc is `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`. |
| `docs/rearchitecture/EVAL-antlr-leverage-and-traversal.md` | DESIGN | **Architecture evaluation (owner-requested, 2026-07-11): are we leveraging ANTLR4 / reinventing the wheel?** Verdict: the `CST→bound-tree→passes→emit` design is SOUND (mirrors Roslyn), but the traversal machinery is not — NO shared bound-tree visitor (205 duplicated `case Bound` arms across ~5 bespoke walkers → the `UsageCollectionPass` completeness bugs), a 50-arm hand-rolled binder dispatch + 334 `GetText()` pokes, zero ANTLR listener use, no `SymbolTable`. Recommendation = TOOLING-FIRST resequence (source-generated visitor [P7 Step 6] + `SymbolTable` [P6] forward); drives `COBOLNET_REARCHITECTURE_PLAN.md §4.1`. [[project_path_a_leverage_tooling]]. |
| `docs/rearchitecture/DESIGN-version-conformance-pipeline.md` | DESIGN | **Version conformance (edition gating) — the canonical mechanism doc:** superset parse (committed-match construct-id annotation; the two load-bearing forward-detects) · edition-agnostic bind · ONE `VersionConformancePass` over the bound tree (the sole edition-gating funnel — all 88 compiler-embedded `ConstructRegistry.Check` sites + the §8.9 reserved-word funnel) · emit-if-clean; §5 execution stages 1–3 (residue migration → delete the recogniser → the pipeline skeleton). |
| `docs/DIAGNOSTICS.md` | LEDGER | **Generated — do not hand-edit.** The index of the first-class diagnostic descriptors: code · stable id · severity · ISO § · suppress key · title. Source of truth = `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs`; regen `pwsh scripts/gen-diagnostics-doc.ps1`; drift-guarded by `DiagnosticRegistryDriftTests`. |

## Version-correctness (multi-edition)

> ⛔ **Cross-cutting obligation:** `cobol.exe` is FOUR compilers in one (`--std` 1985/2002/2014/2023). Every subsystem
> deep-dive above must state, for each edition-varying construct it designs, BOTH the per-edition behavior AND the
> diagnostic emitted by every edition that lacks it (not-yet-introduced or removed), keyed to the rows of
> `VERSION_CHANGE_REFERENCE.md`. A deep-dive that designs a post-1985 feature without its pre-introduction diagnostic
> is incomplete.

| Doc | Type | Subject |
|---|---|---|
| `docs/VERSION_CHANGE_REFERENCE.md` | LEDGER | Checklist of every edition-to-edition change documented in the 2023 spec (Annex E.2/E.3, Annex F, FLAG-02/14, NOTES) — drives version-gating. **Status is DERIVED:** each row carries a `<!-- gate:id -->`/`ref-only`/`pin-to-spec`/`todo` anchor; the "Gating status index" is a GENERATED block (`pwsh scripts/gen-vcr.ps1`; `VcrDriftTests` guards it). Edit rows/prose by hand, never the generated block. |
| `docs/VERSION_TEST_MATRIX_DESIGN.md` | DESIGN | Test the compiler as N per-edition compilers: the (construct × edition) matrix, the 3 invariants, the rollout (Phase 0 done). |

## Conformance feature catalog

| Doc | Type | Subject |
|---|---|---|
| `docs/ISO2023_CONFORMANCE_PLAN.md` | LEDGER | The M2/M3/M4 feature catalog (post-85 features to implement). ⚠ Its data-model-migration framing is obsolete (banner inside) — use it only for the feature list. |
| `docs/PHASE4_RECONCILIATION.md` | LEDGER | **The GREENFIELD-truth view of the M2/M3/M4 catalog** (the ratified Phase-4 entry audit): per-item LANDED/PARTIAL/STAGED-LOUD/NOT-STARTED/OBSOLETE with evidence + per-track wave sizing. SUPERSEDES the catalog's legacy ☑/◐ marks. Keep in sync as Phase-4 tracks land. |

## ISO specification (authoritative)

| Doc | Type | Subject |
|---|---|---|
| `specs/ISO_COBOL.md` | SPEC | The ISO/IEC 1989:2023 COBOL standard (private submodule). The authority for all syntax/semantics/behavior — cite the §. `git submodule update --init --recursive`. |
