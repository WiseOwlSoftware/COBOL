# COBOL.NET — Documentation Index & Maintenance Guide

> **The map of the `docs/` corpus** — every doc, its subject, and what to keep current. Referenced by `CLAUDE.md`.
> **Keep THIS file in sync:** when you add, retire, or materially change a doc, update its row here.

> ⛔🔥 **PIVOT (2026-06-08, DEVLOG 457–464): the project is now a blank-slate COBOL→C#/Roslyn rewrite (COBOL.NET).**
> The authoritative design is **`docs/COBOLNET_DESIGN.md`** (the decision-complete SSOT for the rewrite — pipeline,
> data model, native scaled-integer numerics, PC-dispatcher control flow, REDEFINES, files, OO, EC, intrinsics,
> project reorg/rename to Cobol.NET/`cobol.exe`, no-god-class structure, C# 14, G0–G8 build order);
> `docs/COBOLNET_ARCHITECTURE.md` is the brief overview. (The obsolete byte-engine architecture/plan docs were DELETED
> 2026-06-09, DEVLOG 523.) The backend is **C# source via Roslyn**, numerics are native `long`/`Int128` (NO
> `decimal`/`BigInteger`), and the legacy `CobolSharp.*` engine is kept only as a differential oracle until cut-over (G8).
>
> **COBOL.NET design corpus** (the LIVE rewrite docs — one SSOT + a deep-dive per subsystem):
> `COBOLNET_DESIGN.md` (SSOT: invariants, cross-cutting, settled decisions, G0–G8 order) · `COBOLNET_ARCHITECTURE.md`
> (overview) · deep dives: `COBOLNET_PIPELINE_DESIGN.md` · `COBOLNET_DATA_MODEL_DESIGN.md` ·
> `COBOLNET_NUMERIC_DESIGN.md` · `COBOLNET_CONTROL_FLOW_DESIGN.md` · `COBOLNET_REDEFINES_DESIGN.md` ·
> `COBOLNET_STRING_OPS_DESIGN.md` · `COBOLNET_FILES_DESIGN.md` · `COBOLNET_INTERPROGRAM_DESIGN.md` ·
> `COBOLNET_OO_DESIGN.md` · `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` · `COBOLNET_INTRINSICS_DESIGN.md` ·
> `COBOLNET_PROJECT_ORG_DESIGN.md`. **Keep each subsystem deep-dive as the one canonical doc for its subsystem.**

## How to use & maintain the docs
- **Start from `resume-prompt.md`** (the live session kickoff — two-track RESUME AT) over the SSOT
  **`docs/COBOLNET_DESIGN.md`** (the greenfield rewrite). ⛔ The pre-PIVOT byte-engine plan + architecture docs
  (`PROJECT_PLAN.md`, `docs/MASTER_PLAN.md`, the `docs/DATA_MODEL_*` / `docs/RECORD_STRUCT_STORAGE_DESIGN.md` set, and
  the ~50 `docs/CobolSharp …` byte-engine architecture/guide docs) were **DELETED** (DEVLOG 523) — they were obsolete
  and misleading. The greenfield design lives in the `COBOLNET_*` corpus below.
- **Subsystem design-references (§5)** are *target designs*; each opens with a status banner stating its **real
  implementation status**. **When you change a subsystem, update its one canonical design-reference** and flip its
  banner status as the feature lands. There is exactly **one canonical per subsystem** — extend it, never fork a
  second doc for the same subsystem.
- **Adding a new subsystem/feature?** Create **one** doc in the right section, give it a status banner (subject ·
  real status · stack `.NET 10` / `C# 14` · pointer to `docs/COBOLNET_DESIGN.md`), and **add a row here**.
- **Ledgers (§7)** are historical/status records — *append*, don’t rewrite history. **Spec text (§8)** is the ISO
  source — reference only.
- **Stack of record (greenfield):** `.NET 10` / `C# 14`; backend = **C# source compiled by Roslyn** (the greenfield
  `src/Cobol.Net.*`; the legacy Mono.Cecil/CIL byte engine survives only as a differential oracle until G8). Don’t
  reintroduce stale facts (`.NET 9`, `C# 13`).

**Type legend:** **LIVE** = grounded/binding, keep current · **DESIGN** = target design (banner shows real status) ·
**LEDGER** = historical/status record · **SPEC** = ISO reference.


## 1. Plan & status SSOTs — LIVE, keep current every session

| Doc | Type | Subject |
|---|---|---|
| `DEVLOG.md` | LIVE | Narrative of decisions, failures, and breakthroughs (the dev log). **DESCENDING order — newest entry FIRST** (latest `## Entry` is just below the preamble's `Ordering: DESCENDING` note; oldest at the end). Add new entries at the top. |
| `docs/ARCHITECTURE_ASSESSMENT.md` | LIVE | Evidence-based architecture audit + P0–P6 commercial-hardening roadmap (2026-06-03). |
| `docs/COBOL85_COMPLIANCE_PLAN.md` | LIVE | M1 (COBOL-85) 100% execution plan — the 3-axis model (baseline / flagging / spec-completeness). |
| `docs/ISO2023_CONFORMANCE_PLAN.md` | LEDGER | M2/M3/M4 FEATURE CATALOG (still useful — the post-85 features to implement). ⚠ Its data-model-migration framing is OBSOLETE (banner inside); live plan = `resume-prompt.md` + `docs/COBOLNET_DESIGN.md`. |
| `docs/MULTIVERSION_ROADMAP.md` | LIVE | High-level milestone view of the 1985→2023 multi-version drive (M0→M4). |
| `docs/OO_IMPLEMENTATION_DESIGN.md` | LIVE | LIVE OO turnkey design (§6.6 slice-1 map) + the consolidated OO subsystem canonical; grammar done, semantic/emit pending. |

## 2. Doctrine & process — rules of engagement

| Doc | Type | Subject |
|---|---|---|
| `CLAUDE.md` | LIVE | It is the single SSOT + autonomous-execution playbook to reach the North Star — a commercial-quality, |
| `CONSTRAINTS.md` | LIVE | This document captures the full set of anti-patterns, migration phases, process rituals, |
| `PROMPT.md` | LIVE | C# 14, .NET 10, Multi‑Session Continuity, Explicit Anti‑Patterns |
| `README.md` | LIVE | A COBOL-85 compiler targeting .NET, built from the ISO/IEC 1989:1985 specification |

## 3. Architecture decomposition guides (M001/M003/M004 — done)

| Doc | Type | Subject |
|---|---|---|
| `docs/PARSER-ARCHITECTURE-REVIEW.md` | LIVE | Historical pre-ANTLR hand-written-parser review (superseded by the ANTLR4 grammar; kept for context). |
| `docs/batch3-architectural-summary.md` | LIVE | Items: M407 (CURRENCY SIGN WITH PICTURE SYMBOL), M411 (SCREEN SECTION) |
| `docs/binder/Binder-Decomposition.md` | LIVE | Prerequisite: M001 (IrExpression) — complete |
| `docs/boundtree/BoundTreeBuilder-Decomposition.md` | LIVE | Prerequisites: M001 (IrExpression) — complete, M002 (Binder) — complete, M003 (CilEmitter → 11 emitters) — complete |
| `docs/cilemitter/CilEmitter-Decomposition.md` | LIVE | Prerequisites: M001 (IrExpression) — complete, M002 (Binder decomposition) — complete |
| `docs/ir/IR-Expression-Contract.md` | LIVE | The IR layer has no expression representation. Arithmetic expressions, subscripts, |

## 4. Frontend — grounded design & grammar reference

| Doc | Type | Subject |
|---|---|---|
| `ANTLR4_VSCODE_IMPORT_BUG.md` | LIVE | CobolLexer.g4 # lexer grammar (defines all tokens) |
| `ANTLR4_VSCODE_IMPORT_BUG_RESPONSE.md` | LIVE | 14 grammar files (3,225 lines total), split across two directories |
| `GRAMMAR_AUDIT.md` | LIVE | Spec: ISO/IEC 1989:1985 (COBOL-85) primary, ISO/IEC 1989:2023 reference |
| `docs/ANTLR4-GRAMMAR-ARCHITECTURE.md` | LIVE | This is the authoritative grammar/parsing doc. The ANTLR4 front-end is implemented |
| `docs/ANTLR4-RATIONALE.md` | LIVE | an imperative statement (COBOL-85) |
| `docs/BINDER-DESIGN.md` | LIVE | The binder sits between the parse tree (ANTLR4) and downstream phases (type system, CIL codegen) |
| `docs/CATEGORY-RULES.md` | LIVE | Reference for the category lattice and compatibility matrices implemented in |
| `docs/DATA-DIVISION-LAYOUT-DESIGN.md` | LIVE | Reference design for production-quality OCCURS, REDEFINES, RENAMES, and USAGE |
| `docs/GRAMMAR-AUDIT.md` | LIVE | Complete audit of every grammar production in sections 1-8 of GRAMMAR-REFERENCE.md against Parser.cs |
| `docs/GRAMMAR-REFERENCE.md` | LIVE | Extracted from the spec for lexer/parser implementation. Page references are to the PDF (physical = logical + 30) |
| `docs/SCOPE-RULES.md` | LIVE | This document extracts every rule about scope termination, sentences, periods, and |
| `docs/SEMANTIC-ANALYSIS-ARCHITECTURE.md` | LIVE | CobolSharp semantic analyzer / symbol-table / type-system subsystem. Implementation status |

## 5. Subsystem design-references — one canonical each; banner shows real status; update as features land

| Doc | Type | Subject |
|---|---|---|
| `docs/IL-BYTECODE-GENERATION-DESIGN.md` | DESIGN | Transform the fully-resolved semantic model into a clean, deterministic IL/bytecode representation |
| `docs/REPLACE Preprocessor, Source Mapping & Compilation Pipeline Architecture.md` | DESIGN | Define the authoritative architecture for |
| `docs/Resume-Prompt-For-Docs.md` | DESIGN | You are continuing a long‑form, numbered architecture series for a real software project named CobolSharp |
| `docs/The Internal Compiler API Reference Architecture.md` | DESIGN | [DESIGN REFERENCE] This document defines the internal compiler API surface (stable internal contract). It is not a public API. Implementation… |
| `docs/USER-GUIDE.md` | DESIGN | End-user quick-start: install, compile (`cobol`), and run; common errors. |
| `resume-prompt.md` | DESIGN | North Star (owner, emphatic + repeated): a commercial-quality, production-level, decades-sustainable, full |

## 5b. LIVE per-feature plans

| Doc | Type | Subject |
|---|---|---|
| `docs/M407-currency-sign-picmode-refactor.md` | LIVE | Subsystem: Grammar / Semantics / Runtime |
| `docs/REPORT_WRITER_CONTROL_DESIGN.md` | LIVE | CONTROL break detection + CONTROL HEADING/FOOTING presentation (#6.4), then SUM accumulators (#6.5). Implement |
| `docs/REPORT_WRITER_ROADMAP.md` | LIVE | Implementation status: IMPLEMENTED / largely complete — not "not yet implemented" (that opening claim below is |
| `docs/collating-baseline-finding.md` | LIVE | and reflog b1626f1. Repro: /e/tmp/repro/SPCMP.cob, GRP.cob._ |
| `docs/collating-gap2-turnkey.md` | LIVE | _Authored 2026-05-29. Gap 1 (SORT/MERGE collating) + the numeric-key fix are DONE and committed |
| `docs/collating-subsystem-plan.md` | LIVE | _Authored 2026-05-29 (audit session). Supersedes the "bypassed everywhere" framing in |
| `docs/dialect-strictness.md` | LIVE | CobolSharp's long-term goal is to support multiple COBOL standards selectable from the |

## 6. Terminal / SCREEN SECTION subsystem (design set, Phase C)

| Doc | Type | Subject |
|---|---|---|
| `docs/M411-screen-section-grammar-island.md` | DESIGN | [DESIGN REFERENCE — NOT YET IMPLEMENTED] Status: Design complete, awaiting implementation authorization (dated 2026-03-30). This document… |
| `docs/TERMINAL-ABSTRACTION-DESIGN.md` | DESIGN | Top-level architecture document for the CobolSharp terminal subsystem. Provides a |
| `docs/TERMINAL-ACCEPT-INPUT-LOOP.md` | DESIGN | This document specifies the detailed state-machine semantics for the ACCEPT input loop (SCREEN SECTION). Implementation status and test coverage… |
| `docs/TERMINAL-BUFFER-ATTRIBUTE-MODEL.md` | DESIGN | TerminalBuffer is the in-memory representation of the screen |
| `docs/TERMINAL-CRT-STATUS-MAPPING.md` | DESIGN | Defines the mapping from TerminalInputResult to the COBOL CRT STATUS data item |
| `docs/TERMINAL-CURSOR-ENCODING.md` | DESIGN | The CURSOR clause in SPECIAL-NAMES binds a COBOL data item to the terminal cursor |
| `docs/TERMINAL-DEVICE-BACKEND.md` | DESIGN | ITerminalDevice abstracts the actual I/O mechanism (console, GUI, headless). It knows |
| `docs/TERMINAL-MULTI-FIELD-NAVIGATION.md` | DESIGN | A form consisting of multiple screen items |
| `docs/TERMINAL-RUNTIME-CLASS-LAYOUT.md` | DESIGN | Runtime class layout for the terminal/SCREEN subsystem (CobolSharp.Runtime.Terminal types). |
| `docs/TERMINAL-RUNTIME-ENTRY-POINTS.md` | DESIGN | Defines how bound ACCEPT/DISPLAY statements call into TerminalSession, bridging the |
| `docs/TERMINAL-SESSION-API.md` | DESIGN | Implementation status: Design-only; part of complete 12-doc Terminal/SCREEN SECTION design per Phase C). This specifies the TerminalSession API… |
| `docs/TERMINAL-TEST-HARNESS.md` | DESIGN | Terminal Test Harness (target design for deterministic screen I/O testing; implementation status ~30%) |

## 7. Conformance / gap / status ledgers (historical — append, don’t rewrite)

| Doc | Type | Subject |
|---|---|---|
| `AUDIT_REPORT.md` | LEDGER | ⚠️ Status: Dated 2026-03-24 (pre-dates the greenfield pivot (DEVLOG 457)). Contains stale tech markers (.NET 9→10, C# 13→14,… |
| `MIGRATION_LEDGER.md` | LEDGER | The C#-modernization + framework-retarget record (net8→net9→net10 / C# 14); append, don’t rewrite. |
| `NIST_TEST_REPORT.md` | LEDGER | NIST CCVS test-status snapshot (historical; current counts live in DEVLOG / resume-prompt). |
| `docs/BATCH5-IMPLEMENTATION-PLAN.md` | LEDGER | M429: Screen I/O runtime (Terminal abstraction, ACCEPT/DISPLAY screen forms) |
| `docs/BATCH5-LEDGER-TASKS-M429-M431.md` | LEDGER | implementation-ready, with test names and method signatures. Zero ambiguity |
| `docs/BATCH5-OVERLENIENT-GRAMMAR-APPROVAL.md` | LEDGER | rationale, diff, and approval notes. These proposals must be validated by ANTLR and |
| `docs/BATCH5-OVERLENIENT-GRAMMAR-DELTAS.md` | LEDGER | and patch-ready. These must be reviewed by ANTLR and COBOL expert agents before |
| `docs/CONFORMANCE.md` | LEDGER | Character set: ASCII (UTF-8). EBCDIC supported via codepage conversion |
| `docs/EXCLUSION_LEDGER.md` | LEDGER | classified into exactly one documented exclusion class. real-GAPs = 0 — the M1 baseline axis is |
| `docs/FLAG_MANIFESTS.md` | LEDGER | constructs it presents and the diagnostic a conforming flagger must emit. Drives the WS-FLAG harness |
| `docs/FUTURES.md` | LEDGER | Cross-check the category compatibility matrix against actual NIST test execution |
| `docs/SPEC_FIX_BACKLOG.md` | LEDGER | conformance tests for under-tested live COBOL-85 features; these are the 30 features that did NOT pass |
| `docs/SPEC_FIX_RECIPES_M1.md` | LEDGER | spec, version-classified M1='85 vs M2=2002+, with file list + fix sketch + test shape). This is the |
| `docs/SPEC_GAP_INVENTORY.md` | LEDGER | test exercises each. untested/partial features are the WS-SPEC authoring scope. passing rows are |
| `docs/spec-gaps.md` | LEDGER | output), not inferred from diagnostic strings or comments. Several long-standing "not supported" |
| `docs/VERSION_CHANGE_REFERENCE.md` | LEDGER | LIVE version-gating checklist: every edition-to-edition change of standard COBOL documented in the ISO/IEC 1989:2023 spec (130 rows: Annex E.2/E.3 2014→2023 deltas, Annex F archaic/obsolete, FLAG-02/FLAG-14 GR4, inline §-NOTES). Gate behavior-changes by DialectLevel; new features ≥ their edition; flag obsolete/archaic. 85→2002 & 2002→2014 deltas under-documented here — confirm vs older standards. |
| `docs/VERSION_TEST_MATRIX_DESIGN.md` | DESIGN | Proposed design (not yet built): test COBOL.NET as N per-edition compilers via a (construct × target-edition) matrix driven by VERSION_CHANGE_REFERENCE.md — expected outcome computed from introducedIn/removedIn/behaviorVariants; 3 invariants (continuity, introduction-gating, behavior-correctness); ports the legacy DialectConfig/DialectStrictnessChecks model to the greenfield; phased rollout. |

## 8. ISO spec text — reference excerpts

| Doc | Type | Subject |
|---|---|---|
| `docs/spec-text/cobol-spec.md` | SPEC | Extracted ISO COBOL spec text (reference excerpt). |
| `docs/spec-text/full-spec.md` | SPEC | Extracted full ISO COBOL spec text (reference excerpt). |
| `docs/spec-text/section-14-procedure.md` | SPEC | Extracted ISO spec §14 (Procedure Division) text (reference excerpt). |

---

*Index generated 2026-06-07 (DEVLOG 443). The ISO spec also lives in `specs/ISO_COBOL.md` (submodule, authoritative).*
