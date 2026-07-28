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
| `docs/rearchitecture/EVAL-antlr-leverage-and-traversal.md` | DESIGN | **Architecture evaluation (owner-requested, 2026-07-11): are we leveraging ANTLR4 / reinventing the wheel?** Verdict: the `CST→bound-tree→passes→emit` design is SOUND (mirrors Roslyn), but the traversal machinery is not — NO shared bound-tree visitor (205 duplicated `case Bound` arms across ~5 bespoke walkers → the `UsageCollectionPass` completeness bugs), a 50-arm hand-rolled binder dispatch + 334 `GetText()` pokes, zero ANTLR listener use, no `SymbolTable`. Recommendation = TOOLING-FIRST resequence (source-generated visitor [P7 Step 6] + `SymbolTable` [P6] forward); drives `COBOLNET_REARCHITECTURE_PLAN.md §4.1`. [[project_leverage_antlr_roslyn_tooling]]. |
| `docs/rearchitecture/DESIGN-version-conformance-pipeline.md` | DESIGN | **Version conformance (edition gating) — the canonical mechanism doc:** superset parse (committed-match construct-id annotation; the two load-bearing forward-detects) · edition-agnostic bind · ONE `VersionConformancePass` over the bound tree (the sole edition-gating funnel — all 88 compiler-embedded `ConstructRegistry.Check` sites + the §8.9 reserved-word funnel) · emit-if-clean; §5 execution stages 1–3 (residue migration → delete the recogniser → the pipeline skeleton). |
| `docs/rearchitecture/DESIGN-compile-time-expressions.md` | DESIGN | **Compile-time expression evaluation (§7.3.6 arithmetic / §7.3.7+§8.8.2 boolean / §7.3.8 constant-conditional) — the ONE shared evaluator (ledger C2):** the `CompileTimeExpressionEvaluator` used by BOTH the frontend conditional-compilation stage (directive operands, fragment-parsed) and the CONSTANT-entry binder; the `BooleanExpressionResolver` (§8.8.2 rule-7b precedence incl. the context-inherited shift, shared with runtime COMPUTE-Format-2); the ANTLR fragment/cce grammar + the `PrimeDirectiveExpr` lexer flag; GR5/GR3 at the public boundary; code-preserving diagnostics + per-consumer citations. ANTLR-for-all-parsing (no hand-rolled parser); no deferrals. |
| `docs/rearchitecture/DESIGN-flag-directives.md` | DESIGN | **FLAG-02 / FLAG-14 migration-flagging directives (§7.3.14 / §7.3.15) — IN PROGRESS (P13 Wave D):** the ONE shared flagging subsystem — a frontend-collected per-option `FlagState` (the `>>TURN`/`>>REF-MOD-ZERO-LENGTH` toggle-fold template), a dedicated post-bind `FlagConformancePass` (sibling to `VersionConformancePass`, reusing the drift-proof `StatementChildren()` traversal), and the two frontend-inline detectors (compile-time-arithmetic, `>>EVALUATE`) that have no bound residue. Carries the adversarially-verified per-option census (FLAG-14 ALL+12, FLAG-02 ALL+5) mapped to GR4 sub-rules + Annex E.2 items + detection sites, the directive-word edition gates, diagnostics COBOLNET1620/1621 (Warning), and the 5-increment plan. Drives VCR **Table 5**. |
| `docs/rearchitecture/DESIGN-cobol-words-directive.md` | DESIGN | **`>>COBOL-WORDS` directive (§7.3.10; Annex D.12; E.3.3 item 12) — IMPLEMENTED (P13 Wave D):** the per-compilation-group reserved/context/intrinsic word-table modification. The `CobolWordsMap` override carrier (Editions), a post-lex `CobolWordsRewriter` (EQUATE/UNDEFINE/SUBSTITUTE token retyping) + the map-aware lexer data-name gate, a composed `ReservedWordSet` (RESERVE/UNDEFINE), and `IntrinsicBinder` synonym resolution for function names. All four options for reserved, context-sensitive, AND intrinsic-function words; SR1–SR5 enforced; COBOLNET0900 gate + COBOLNET1623. |
| `docs/rearchitecture/DESIGN-cc-in-copy.md` | DESIGN | **CC-directives-inside-COPY (§7.2.1) — IMPLEMENTED (P13 Wave D):** the merged interleaved text-manipulation driver — `ConditionalCompilationProcessor.ProcessWithCopy` fuses the CC branch-selection state machine with COPY expansion so directives INSIDE copybooks are processed (Step 1 incorporate → Step 2 CC over the expanded group), while a main-source `>>IF` still gates a COPY and an omitted-branch COPY is never expanded (false-branch missing copybook raises no error). Shared directive state threaded through recursive COPY; legacy `Process`/COPY byte-identical. |
| `docs/DIAGNOSTICS.md` | LEDGER | **Generated — do not hand-edit.** The index of the first-class diagnostic descriptors: code · stable id · severity · ISO § · suppress key · title. Source of truth = `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs`; regen `pwsh scripts/gen-diagnostics-doc.ps1`; drift-guarded by `DiagnosticRegistryDriftTests`. |
| `docs/rearchitecture/DESIGN-ec-oo-superbatch.md` | DESIGN | **The EC-infra + OO super-batch coordinated fix plan (DESIGN, not yet implemented):** the 13 remaining EC/OO conformance findings (CA9/10/11/12/V57 · CA21/22/V58 · CA29/30/V55 · CA37/38) that share the EC dispatch scaffold. §1 maps the scaffold (EcBinder/EcEmitter/ExceptionState) + the canonical fatal/nonfatal EC-gate recipe; §2 the synthesis — the shared fix pattern, 5 tracks (E/C/D parallel-safe, serial EC chain A + CA12 last), an 11-step commit plan, risks + 3 owner decisions; §3 per-finding re-verification + delta + anchors. From anchor re-scout `wf_d20dadb7-de9`. |
| `docs/rearchitecture/DESIGN-spec-conformance-review.md` | DESIGN | **The FULL implementation↔spec conformance review (P14 Step-0, spec-first) — the definition of DONE (D13):** decision-complete methodology for the EXHAUSTIVE, traceable review of every normative ISO rule → implementation → verified verdict → spec-derived test. Phase A = the spec-rule catalog (denominator); Phase B = map+verify (the traceability inventory, the GAP burn-down); Phase C = close DIVERGES(§24)/NOT-IMPLEMENTED/untested-CONFORMS. Motivated by the differential-blindness lesson; the two audits below are its first installment. |
| `docs/rearchitecture/DESIGN-SPEC-RECONCILIATION.md` | LEDGER | **Design-doc↔spec audit SSOT** (`wf_480d50f5` + correction `wf_16d53d4e`): the 54 doc↔spec conflicts across 13 docs (doc side CORRECTED spec-faithful) + the 6 spec-wrong-AND-implemented code-bugs routed to §24 V54–V59. |
| `docs/rearchitecture/CODE-SPEC-AUDIT.md` | LEDGER | **Code↔spec audit SSOT** (`wf_4ce42db6`, 14 behavioral areas): CA1–CA38 candidate conformance bugs — the implementation checked against the ISO spec rule-by-rule (the first spec-first oracle). Verify-then-fix; feeds §24. |
| `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md` | LEDGER | **The VERIFIED, fix-ready conformance queue** (`wf_29a15db2` verified V54–V59 + CA1–CA38 against spec+code): 44 CONFIRMED (2 blocker/30 major/10 minor/2 nit), each with a decision-complete spec-derived FIX + a spec-derived GOLDEN; 0 refuted; 2 owner-decided (CA14 uniform-introduction-policy; V59 Tier-C byte[] canonical — both APPROVED 2026-07-22). **the active work-list** (fix top-down by severity, batched by area, each landing with a golden). **30 landed / 16 remain as of 2026-07-23** — see the doc's LANDED header for the live tally. |

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
| `specs/ISO_COBOL.md` | SPEC | The ISO/IEC 1989:2023 COBOL standard, transcribed. The authority for all syntax/semantics/behavior — cite the §. **PUBLIC** (in this repo). Opens with a Preface carrying ISO's acknowledgment, and closes with an **Addendum listing every correction with the printed form**, so each is reversible. **PAGES ARE GONE** — a page is a typesetting artifact with no meaning in Markdown, so every cross-reference is an intra-document `#section-N-N` link and anything page-keyed must be re-keyed onto the clause hierarchy. Figures are GENERATED from the printed page, never hand-drawn. |
| `specs-private/…​.pdf` | SPEC | The licensed PDF, **private submodule** — per-copy licence, not redistributable. `git submodule update --init specs-private`. Needed only by the tools that MEASURE the printed page. |
| `docs/rearchitecture/spec-reconciliation/PDF-TEXT-LAYER.md` | REFERENCE | Why the PDF's text was unreadable (missing `/ToUnicode`, NOT obfuscation) and how it was recovered. Read before touching `pdf_deobfuscate.py`. |
| `docs/rearchitecture/spec-reconciliation/FIGURE-STYLE.md` | DESIGN | How a general format is DRAWN — glyph family, brackets vs braces, bar spacing, minimum row count, and the `line-height: 1` requirement. Settled by rendering, not reasoning; several rules are counter-intuitive. Read before changing `render_figure.py`. |
| `docs/rearchitecture/spec-reconciliation/TRANSCRIPTION-STATE.md` | PLAN | **Start here for transcription work.** What is done, what is next (Figure D.6), the gate commands with their last-known numbers, and the six rules for drawing an Annex D figure. Plan §0 stays the project SSOT; this is the detail behind its spec bullets. |
| `docs/rearchitecture/spec-reconciliation/REPAIR-PLAN.md` | PLAN | The transcription repair batches, their order and mechanism, and the measure-don't-squint rules. |
| `scripts/spec/relist_index.py` + `measure_index_levels.py` | TOOL | Rewrites the index as a nested list. Sub-entry LEVELS come from the printed indentation, measured into the committed `data/index-levels.json` so the transform works in the public repo without the PDF; alphabetical order is the fallback where the measurement cannot name an entry unambiguously. Gated on word conservation. |
| `scripts/spec/link_table_figure_lists.py` | TOOL | Anchors every `**Table N — …**` / `**Figure N — …**` caption and GENERATES the front-matter lists from them. Generated, because the hand-maintained figure list had 12 of 15 entries pointing at the wrong clause. |
| `scripts/spec/strip_page_rules.py` | TOOL | Removes the `---` rules left behind by de-paging (758 of them). Gated on: no word changes, no heading created or destroyed, and never removing a rule under a paragraph — `---` under TEXT is a setext H2 marker. Front matter is exempt. |
| `scripts/spec/repairs/annex_d_*.py` | TOOL | The three Annex D figure generators — `_flowcharts` (VARYING charts, D.1), `_truth_charts` (condition evaluation, D.7–D.10), `_structure` (the two that are structure rather than flow — D.3's nested schematic and D.6's page layout). ⚠ **Each needs the collision guard**: `put` refuses to overwrite a non-blank cell, with a separate `junction()` for the one legitimate case. It has caught seven defects that would have rendered as plausible pictures. Border WEIGHT is notation in D.3 (heavy/light/dashed = group/unit/element); in D.6 VERTICAL DISTANCE is, so its rows are placed from the measured printed y, and its `<…>` notation is escaped at write time or a renderer eats it. |
| `scripts/spec/resolve_index_folios.py` | TOOL | Maps a printed folio to the clause its page begins, for the index references de-paging left as bare numbers. ⚠ Skips the front matter (its TOC is full of `heading … number`) and prefers a heading printed ON the page — getting either wrong silently produces confident, wrong links. |
| `scripts/spec/lint_rendering.py` | GATE | The only check that measures whether the transcription is LEGIBLE rather than faithful — unbalanced emphasis outside code, `<pre>` missing `line-height:1`, an unescaped `<` inside a `<pre>` (raw HTML — a renderer drops the tag and the words with it), an undrawn figure, a column header repeating inside a table body, ragged rows, caption-as-heading, unbalanced tags, dangling links. Needs no PDF. Run it on every change to `specs/ISO_COBOL.md`; it takes a file argument so it can be pointed at an older revision to confirm it still fails. |

## Derived knowledge base (Obsidian vault at `kb/`)

> **Not authoritative.** A cross-linked Obsidian "second brain" derived (paraphrased) from `docs/*` + the source. The
> docs above remain the SSOT — **the doc wins on any conflict**. Contains **no verbatim ISO text** (that stays in the
> `specs/ISO_COBOL.md` submodule). The notes are tracked; Obsidian's volatile `.obsidian/` state is gitignored.

| Path | Type | Subject |
|---|---|---|
| `kb/` | DERIVED | MOCs, lookup tables (keywords · grammar · semantic-rules · IR mapping · runtime mapping · diagnostics · construct catalogue), diagrams, reverse indexes (ISO-clause→phase, runtime-class→IR), data-flow traces, and the live top-level `Remaining Work Tracker.md`. Setup/role: `kb/Context/Vault & Docs Integration.md`. Refresh notes when the docs they cite change; each carries a `last_updated` stamp. |
