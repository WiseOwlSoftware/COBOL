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
| `docs/COBOLNET_DESIGN.md` | LIVE | **The SSOT.** Decision-complete design for the rewrite: locked invariants (§1), cross-cutting (§14), settled decisions (§18), the G0–G8 build order, and pointers to the per-subsystem deep-dives. |
| `docs/COMPLETION_ROADMAP_COUNCIL.md` | LIVE | **RATIFIED — the EXECUTION ROADMAP for G7→G8** (validator waves → M2 OO/residuals → M3 → M4 → matrix closure → G8 in 3 cuts), phase-by-phase with exit criteria. Upholds the SSOT §16 spine + 7 ratified deltas (its §3); its §5 decision packet is owner-resolved (all council defaults; #1 = no standards acquisition — the in-repo 2023 spec is the sole ISO authority). Read via the plan's §0 banner (largely subsumed by the plan; retained for the ratified-decision record). |
| `docs/COBOLNET_REARCHITECTURE_PLAN.md` | LIVE | **THE ONE PLANNING DOCUMENT (owner-directed consolidation 2026-07-19) — read §0 FIRST every session; update §0 before ending.** Absorbed and replaced: `resume-prompt.md`, all 17 `PHASE-00..16-*.md` step-by-steps (live detail in its Part II; completed records in Part III), `PHASE-13-audit.md` (+ scout JSON), `PLAN-bindtime-gating-migration.md`. Contains: the §0 live resume state · mission + D13 (100% CONFORMING) · north-star + principles + the dual-backend mandate · the §3 execution model · the §4 phase index · §6 owner decisions D1–D20 · §7 backfill refinements · the §8 consolidated residue ledger · §9 verification/corpus mechanics · the §10 doc map. Still separate: the DESIGN-* deep-dives (design SSOTs), the review/scout LEDGERS (`PHASE-13-plan-vs-spec-review.md` = the fix SSOT via its §24 queue; `PHASE-11/12-scout-notes.md`), SURVEY/CRITIQUE (frozen analyses, delete at P15), DEVLOG (the only history). |
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
| `docs/rearchitecture/DESIGN-compile-time-expressions.md` | DESIGN | **Compile-time expression evaluation (§7.3.6 arithmetic / §7.3.7+§8.8.2 boolean / §7.3.8 constant-conditional) — the ONE shared evaluator (ledger C2):** the `CompileTimeExpressionEvaluator` used by BOTH the frontend conditional-compilation stage (directive operands, fragment-parsed) and the CONSTANT-entry binder; the `BooleanExpressionResolver` (§8.8.2 rule-7b precedence incl. the context-inherited shift, shared with runtime COMPUTE-Format-2); the ANTLR fragment/cce grammar + the `PrimeDirectiveExpr` lexer flag; GR5/GR3 at the public boundary; code-preserving diagnostics + per-consumer citations. ANTLR-for-all-parsing (no hand-rolled parser); no deferrals. |
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
| `docs/CONFORMANCE.md` | LIVE | **The §4.2.16 conformance record + implementor's user documentation.** Per-item disposition of the Annex A.3 processor-dependent elements (claimed / not claimed), the pinned §4.2.6 behavior determinations, and the four documented-non-support facilities (MCS, commit/rollback, VALIDATE, screen). The catalogue behind the COBOLNET1560-band §4.2.6 warnings. Keep in sync with the supported surface. |
| `docs/GnuCOBOL extensions.md` | LIVE (register) | **Non-ISO constructs COBOL.NET does not support**, found by running the GnuCOBOL testsuite through the compiler (plan §11 A4 / P14 Step 13). Exists so that adopting any of them is a deliberate future decision, not an accident; NOTHING in it is scheduled — the mission is ISO-2023 ×4 (D13) and every row is outside that target. ⚠ Rows carry a CONFIDENCE column: **NEEDS VERIFICATION** rows are unadjudicated and some may prove to be ISO constructs we wrongly reject (i.e. OUR bugs) — two such have already moved out of it. Also carries the opposite-direction table (source we wrongly ACCEPT), which seeds §11 A2. Never cite it as authority that something is non-ISO until its row says CONFIRMED. |

## Conformance feature catalog

| Doc | Type | Subject |
|---|---|---|
| `docs/ISO2023_CONFORMANCE_PLAN.md` | LEDGER | The M2/M3/M4 feature catalog (post-85 features to implement). ⚠ Its data-model-migration framing is obsolete (banner inside) — use it only for the feature list. |
| `docs/PHASE4_RECONCILIATION.md` | LEDGER | **The GREENFIELD-truth view of the M2/M3/M4 catalog** (the ratified Phase-4 entry audit): per-item LANDED/PARTIAL/STAGED-LOUD/NOT-STARTED/OBSOLETE with evidence + per-track wave sizing. SUPERSEDES the catalog's legacy ☑/◐ marks. Keep in sync as Phase-4 tracks land. |

## ISO specification (authoritative)

| Doc | Type | Subject |
|---|---|---|
| `specs/ISO_COBOL.md` | SPEC | The ISO/IEC 1989:2023 COBOL standard (private submodule). The authority for all syntax/semantics/behavior — cite the §. `git submodule update --init --recursive`. |
