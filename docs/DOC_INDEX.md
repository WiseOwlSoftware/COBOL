# COBOL.NET — Documentation Index & Maintenance Guide

> **The map of the `docs/` corpus** — every doc, its subject, and what to keep current. Referenced by `CLAUDE.md`.
> **Keep THIS file in sync:** when you add, retire, or materially change a doc, update its row here.

> ⛔🔥 **PIVOT (2026-06-08, DEVLOG 457–464): the project is now a blank-slate COBOL→C#/Roslyn rewrite (COBOL.NET).**
> The authoritative design is **`docs/COBOLNET_DESIGN.md`** (the decision-complete SSOT for the rewrite — pipeline,
> data model, native scaled-integer numerics, PC-dispatcher control flow, REDEFINES, files, OO, EC, intrinsics,
> project reorg/rename to Cobol.NET/`cobol.exe`, no-god-class structure, C# 14, G0–G8 build order);
> `docs/COBOLNET_ARCHITECTURE.md` is the brief overview. The byte-engine docs below (incl. the "Mono.Cecil/CIL
> backend" stack-of-record lines) are HISTORICAL — the new backend is **C# source via Roslyn**, numerics are native
> `long`/`Int128` (NO `decimal`/`BigInteger`), and the legacy `CobolSharp.*` is kept only as a differential oracle
> until cut-over (G8).
>
> **COBOL.NET design corpus** (the LIVE rewrite docs — one SSOT + a deep-dive per subsystem):
> `COBOLNET_DESIGN.md` (SSOT: invariants, cross-cutting, settled decisions, G0–G8 order) · `COBOLNET_ARCHITECTURE.md`
> (overview) · deep dives: `COBOLNET_PIPELINE_DESIGN.md` · `COBOLNET_DATA_MODEL_DESIGN.md` ·
> `COBOLNET_NUMERIC_DESIGN.md` · `COBOLNET_CONTROL_FLOW_DESIGN.md` · `COBOLNET_REDEFINES_DESIGN.md` ·
> `COBOLNET_STRING_OPS_DESIGN.md` · `COBOLNET_FILES_DESIGN.md` · `COBOLNET_INTERPROGRAM_DESIGN.md` ·
> `COBOLNET_OO_DESIGN.md` · `COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` · `COBOLNET_INTRINSICS_DESIGN.md` ·
> `COBOLNET_PROJECT_ORG_DESIGN.md`. **Keep each subsystem deep-dive as the one canonical doc for its subsystem.**

## How to use & maintain the docs
- **Start from `docs/MASTER_PLAN.md`** — the top-level SSOT + execution playbook. The **§1 LIVE plan/status SSOTs**
  drive the work and must be kept current every session (tick items, update status + DEVLOG).
- **Subsystem design-references (§5)** are *target designs*; each opens with a status banner stating its **real
  implementation status**. **When you change a subsystem, update its one canonical design-reference** and flip its
  banner status as the feature lands. There is exactly **one canonical per subsystem** — extend it, never fork a
  second doc for the same subsystem.
- **Adding a new subsystem/feature?** Create **one** doc in the right section, give it a status banner (subject ·
  real status · stack `.NET 10` / `C# 14` · backend CIL-only via Mono.Cecil · pointer to `MASTER_PLAN.md`), and **add a
  row here**.
- **Ledgers (§7)** are historical/status records — *append*, don’t rewrite history. **Spec text (§8)** is the ISO
  source — reference only.
- **Stack of record:** `.NET 10` / `C# 14`; backend **CIL-only via Mono.Cecil** (no custom VM); pointers = one
  `ManagedPointer`. Don’t reintroduce stale facts (`.NET 9`, `C# 13`, 8-byte pointer handle, `PointerRegistry`,
  `CobolDataPointer`, "CilEmitter god class").

**Type legend:** **LIVE** = grounded/binding, keep current · **DESIGN** = target design (banner shows real status) ·
**LEDGER** = historical/status record · **SPEC** = ISO reference.


## 1. Plan & status SSOTs — LIVE, keep current every session

| Doc | Type | Subject |
|---|---|---|
| `DEVLOG.md` | LIVE | Narrative of decisions, failures, and breakthroughs (the dev log). **DESCENDING order — newest entry FIRST** (latest `## Entry` is just below the preamble's `Ordering: DESCENDING` note; oldest at the end). Add new entries at the top. |
| `PROJECT_PLAN.md` | LIVE | Project status, KTDs (key technical decisions 1–5), and the per-session log. |
| `docs/ARCHITECTURE_ASSESSMENT.md` | LIVE | Evidence-based architecture audit + P0–P6 commercial-hardening roadmap (2026-06-03). |
| `docs/COBOL85_COMPLIANCE_PLAN.md` | LIVE | M1 (COBOL-85) 100% execution plan — the 3-axis model (baseline / flagging / spec-completeness). |
| `docs/DATA_MODEL_ARCHITECTURE.md` | LIVE | The typed-native data-model ADR (records→record struct, char→string, numeric→long/decimal, pointers→ManagedPointer) — SETTLED, do not re-litigate. |
| `docs/DATA_MODEL_REVIEW.md` | LIVE | Adversarial review of the data-model ADR (companion to DATA_MODEL_ARCHITECTURE). |
| `docs/ISO2023_CONFORMANCE_PLAN.md` | LIVE | THE conformance work-breakdown to full ISO-2023 (ranked M2/M3/M4 + execution waves) — the Phase-C SSOT; execute it, don’t re-audit. |
| `docs/MASTER_PLAN.md` | LIVE | THE top-level SSOT + autonomous-execution playbook (North Star, phased roadmap A–F, doc index, grounded assessment). Start here. |
| `docs/MULTIVERSION_ROADMAP.md` | LIVE | High-level milestone view of the 1985→2023 multi-version drive (M0→M4). |
| `docs/OO_IMPLEMENTATION_DESIGN.md` | LIVE | LIVE OO turnkey design (§6.6 slice-1 map) + the consolidated OO subsystem canonical; grammar done, semantic/emit pending. |
| `docs/RECORD_STRUCT_STORAGE_DESIGN.md` | LIVE | The 7-stage data-model migration roadmap (record-struct storage; pointers done; Stage 5/6 + byte-engine islanding ahead). |

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
| `docs/CobolSharp COBOL ACCEPT, DISPLAY, Console IO & Environment‑Variable Architecture.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp COBOL CALL Convention, Parameter Passing, LINKAGE & BY VALUE-BY REFERENCE Architecture.md` | DESIGN | Implementation status: LARGELY IMPLEMENTED (~85–90%). CALL literal + CALL identifier, USING with |
| `docs/CobolSharp COBOL CIL Backend & Code Generation Architecture.md` | DESIGN | This is a target-design document for the CIL backend (the final stage of the |
| `docs/CobolSharp COBOL COPY-REPLACE Preprocessor Architecture.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp COBOL Compiler Directive & Conditional Compilation Architecture.md` | DESIGN | Not yet implemented). This describes the target architecture for compiler directives. Current implementation status and integration point: refer… |
| `docs/CobolSharp COBOL Condition Names, Boolean Evaluation & Predicate Architecture.md` | DESIGN | [DESIGN REFERENCE] This document describes the authoritative architecture for condition-name evaluation and boolean operations. Implementation… |
| `docs/CobolSharp COBOL EVALUATE, Branching & Control‑Flow Semantics Architecture.md` | DESIGN | Type: Authoritative design/architecture reference for COBOL branching, control flow, EVALUATE |
| `docs/CobolSharp COBOL Expression Evaluation & Type System Architecture.md` | DESIGN | Define the authoritative rules for |
| `docs/CobolSharp COBOL File IO, FD-SD, Sequential-Indexed-Relative & Record‑Buffer Architecture.md` | DESIGN | RELATIVE/INDEXED organizations, OPEN/CLOSE/READ/WRITE/REWRITE/DELETE/START, file-status, keys, locking) |
| `docs/CobolSharp COBOL File Status, Error Handling & Exception Mapping Architecture.md` | DESIGN | Error Handling Architecture (authoritative target design, ~70% implemented) |
| `docs/CobolSharp COBOL INSPECT, STRING, UNSTRING & Text‑Processing Engine Architecture.md` | DESIGN | subsystem design reference for COBOL text-processing statements (INSPECT / STRING / UNSTRING) |
| `docs/CobolSharp COBOL Interop Architecture — .NET Types, Assemblies, INVOKE .NET & Type Mapping.md` | DESIGN | This is the authoritative TARGET design reference for COBOL ↔ .NET interop. Implementation status |
| `docs/CobolSharp COBOL Language Feature Support Matrix.md` | DESIGN | Provide a comprehensive, authoritative matrix of COBOL language features supported by CobolSharp, aligned with ISO/IEC 1989:2023 |
| `docs/CobolSharp COBOL MOVE, CORRESPONDING, INITIALIZE & Data‑Movement Architecture.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp COBOL National Character, Unicode & Locale‑Independent Text Architecture.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp COBOL Numeric Engine & Packed Decimal Architecture.md` | DESIGN | DIVIDE/COMPUTE, ROUNDED, SIZE ERROR, DISPLAY/COMP/COMP-3/COMP-5 numeric formats, packed-decimal encode/decode, |
| `docs/CobolSharp COBOL Optimizer & Intermediate Representation (IR) Architecture.md` | DESIGN | optimizer. Implementation status: the IR layer is REAL and current |
| `docs/CobolSharp COBOL PERFORM, Control‑Flow, Looping & Structured Execution Architecture.md` | DESIGN | This is a target-design / architecture reference for PERFORM and structured control‑flow lowering |
| `docs/CobolSharp COBOL Paragraph, Section & Program Structure Architecture.md` | DESIGN | This document specifies the architecture for COBOL program/section/paragraph structure. It is a design document; the subsystem is ~80–90%… |
| `docs/CobolSharp COBOL Program Lifecycle, STOP RUN, GOBACK & Runtime Termination Architecture.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp COBOL Program Registry, ENTRY Points & Multi‑Entry Dispatch Architecture.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp COBOL Runtime — ExecutionContext, StorageBlocks, ObjectTable, FileManager & Engine Integration Architecture.md` | DESIGN | runtime. It is an aspirational unified model, not a description of the code as built. Actual implementation |
| `docs/CobolSharp COBOL SORT-MERGE, File‑Based Pipeline & Collation Architecture.md` | DESIGN | [DESIGN REFERENCE] This document describes SORT/MERGE architecture. Implementation status: core SORT/MERGE ~85–90% complete; M1 spec-fix backlog… |
| `docs/CobolSharp COBOL Semantic Rules & Edge‑Case Behavior Specification.md` | DESIGN | the authoritative semantic rules and edge-case behaviors CobolSharp/COBOL.NET must implement |
| `docs/CobolSharp COBOL‑to‑C# Interop Cookbook.md` | DESIGN | Provide a practical, example‑driven guide for developers integrating COBOL code compiled by CobolSharp with C# and other .NET languages |
| `docs/CobolSharp Complete Developer Guide — Best Practices, Patterns, Anti‑Patterns & Performance Recipes.md` | DESIGN | Provide the authoritative developer guide for CobolSharp |
| `docs/CobolSharp Complete ISO Compatibility Matrix — Feature Coverage, Deviations & Extensions.md` | DESIGN | / aspirational matrix. This document describes a complete ISO/IEC 1989:2023 compatibility reference. Actual coverage and correctness should be… |
| `docs/CobolSharp Compliance & Governance Manual — Auditability, Traceability, Regulatory Controls & Long‑Term Retention.md` | DESIGN | Define the authoritative compliance and governance framework for CobolSharp |
| `docs/CobolSharp Concurrency Model — Cooperative Scheduling, Event Loops & Deterministic Single‑Thread Execution.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp Contributor & Maintainer Guide Architecture.md` | DESIGN | Define the internal processes, standards, and workflows that ensure CobolSharp remains |
| `docs/CobolSharp Cookbook — 100+ Ready‑to‑Use Patterns, Idioms & Recipes.md` | DESIGN | Production Patterns (target cookbook; implementation status varies by subsystem) |
| `docs/CobolSharp Debugger Architecture — Breakpoints, StorageBlock Inspection, Step Semantics & ExecutionContext Visualization.md` | DESIGN | Implementation status: DESIGN-ONLY (~0 lines). There is no debugger, PDB-emission, sequence-point, LSP, or DAP |
| `docs/CobolSharp Determinism Model — Cross‑Platform Guarantees, Encoding Rules & Reproducibility Architecture.md` | DESIGN | the determinism model for CobolSharp/COBOL.NET — cross-platform execution guarantees, encoding |
| `docs/CobolSharp End‑User Handbook — Practical Examples, Templates & Real‑World Workflows.md` | DESIGN | Provide a practical, example‑driven handbook for CobolSharp end‑users |
| `docs/CobolSharp Enterprise Deployment Guide — CI-CD, Version Pinning, Multi‑Tenant Hosting & Observability.md` | DESIGN | Define the authoritative enterprise‑grade deployment architecture for CobolSharp |
| `docs/CobolSharp Error Model — Declaratives, ExceptionState, USE AFTER & Structured Recovery Architecture.md` | DESIGN | subsystem: DECLARATIVES, USE statements, ExceptionState propagation, statement-level handlers |
| `docs/CobolSharp Formal Specification — Grammar, Type System, Operational Semantics & Memory Model (ISO‑Aligned, CIL‑Only).md` | DESIGN | This document is a formal specification essay authored as a target design. Actual implementation status varies by subsystem — refer to… |
| `docs/CobolSharp Future Extensions Roadmap — SQL, CICS, Distributed Files & Multi‑Tenant Runtime Architecture.md` | DESIGN | Extensions (Phase F+) (0% implemented; design-only for deterministic SQL/CICS/distributed I/O/multi-tenant runtime) |
| `docs/CobolSharp LSP  IDE Integration Architecture.md` | DESIGN | Provide a modern, language-server–driven development experience for COBOL |
| `docs/CobolSharp Master Architecture Document.md` | DESIGN | It describes the target end-to-end architecture of the compiler+runtime. Implementation reality (2026-06-07, |
| `docs/CobolSharp Memory Model — StorageBlocks, Offsets, REDEFINES, OCCURS & DEPENDING‑ON Architecture.md` | DESIGN | field offsets, REDEFINES overlays, OCCURS / OCCURS DEPENDING ON, PIC/USAGE encoding (DISPLAY, COMP, |
| `docs/CobolSharp Modernization and Migration Toolkit Architecture.md` | DESIGN | This document specifies the target architecture for a legacy-COBOL modernization and migration toolkit. It is a design-only specification with ~0… |
| `docs/CobolSharp Operational Runbook — Incident Response, Debugging in Production & Recovery Procedures.md` | DESIGN | Define the authoritative operational runbook for CobolSharp production systems |
| `docs/CobolSharp Packaging & Distribution Architecture.md` | DESIGN | This is a TARGET design for the CobolSharp packaging / build / release / distribution |
| `docs/CobolSharp Performance Architecture — IL Optimizations, StorageBlock Access Patterns & Engine Throughput.md` | DESIGN | Define the authoritative architecture for |
| `docs/CobolSharp Security Architecture — Sandboxing, Capability Restrictions, WASM Isolation & Safe Interop.md` | DESIGN | This document is a security-architecture essay authored as a target design (CIL-only). Implementation is PARTIAL: memory safety and file-path… |
| `docs/CobolSharp Standard Library — Intrinsics, FUNCTION Resolution, Runtime Helpers & Deterministic Semantics.md` | DESIGN | Implementation status: LARGELY IMPLEMENTED (~90%+). ~94 intrinsic FUNCTIONs are implemented and |
| `docs/CobolSharp Test Harness & Validation Architecture.md` | DESIGN | Test Harness Architecture (authoritative target design, ~70% implemented) |
| `docs/CobolSharp Testing & Verification Architecture — Unit Tests, Golden Files, Deterministic Snapshots & Runtime Validation.md` | DESIGN | [DESIGN REFERENCE] This document describes the testing and verification architecture. Current test status per MASTER_PLAN.md §2: 1196 unit / 509… |
| `docs/Cobolsharp COBOL JSON & XML Processing Architecture.md` | DESIGN | Implementation status: DESIGN-ONLY (Phase C). A permissive grammar overlay exists |
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
| `docs/TERMINAL-TEST-HARNESS.md` | DESIGN | Terminal Test Harness (target design for deterministic screen I/O testing; implementation status ~30% per MASTER_PLAN §2) |

## 7. Conformance / gap / status ledgers (historical — append, don’t rewrite)

| Doc | Type | Subject |
|---|---|---|
| `AUDIT_REPORT.md` | LEDGER | ⚠️ Status: Dated 2026-03-24 (pre-dates MASTER_PLAN 2026-06-07). Contains stale tech markers (.NET 9→10, C# 13→14,… |
| `MIGRATION_LEDGER.md` | LEDGER | The C#-modernization + framework-retarget record (net8→net9→net10 / C# 14); append, don’t rewrite. |
| `NIST_TEST_REPORT.md` | LEDGER | NIST CCVS test-status snapshot (historical; current counts live in MASTER_PLAN §2). |
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

## 8. ISO spec text — reference excerpts

| Doc | Type | Subject |
|---|---|---|
| `docs/spec-text/cobol-spec.md` | SPEC | Extracted ISO COBOL spec text (reference excerpt). |
| `docs/spec-text/full-spec.md` | SPEC | Extracted full ISO COBOL spec text (reference excerpt). |
| `docs/spec-text/section-14-procedure.md` | SPEC | Extracted ISO spec §14 (Procedure Division) text (reference excerpt). |

---

*Index generated 2026-06-07 (DEVLOG 443). The ISO spec also lives in `specs/ISO_COBOL.md` (submodule, authoritative).*
