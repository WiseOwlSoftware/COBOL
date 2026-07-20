# CobolSharp — Claude Code Instructions

## ⛔ NON-NEGOTIABLE PROCESS RULES (owner-emphasized — obey BEFORE writing any code)
These are the rules the owner most insists on (repeatedly corrected). Full text at the top of `PROMPT.md`; durable in memories `feedback_use_the_spec`, `feedback_follow_design_docs_and_spec`,
`feedback_spec_scopes_not_tests`.
1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL.md`) defines correct behavior for EVERY case** — read it and cite
   the § for any semantics/syntax/output question; the legacy oracle is a regression net, NOT authority.
2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/COBOLNET_DESIGN.md` §0.5 lists them) +
   the spec — FOLLOW the doc, do not improvise.
3. **Implement the COMPLETE feature to the spec + design — NEVER scope to what a test references** (tests VERIFY, not
   SCOPE).
4. **Keep the docs CURRENT** — when a change supersedes a deep-dive, update it in the same change set to state the
   current design. Every doc **except `DEVLOG.md`** describes the CURRENT compiler; the historical narrative (how we
   got here) lives only in `DEVLOG.md`.
**Live kickoff = `docs/COBOLNET_REARCHITECTURE_PLAN.md` §0 — THE ONE PLAN** (owner-directed consolidation
2026-07-19: it absorbed `resume-prompt.md` + all 17 phase docs + the P13 audit; read its §0 FIRST and update §0
before ending every session). The COBOL.NET rewrite design SSOT is `docs/COBOLNET_DESIGN.md`.

## ⛔ TOP-LEVEL PLAN: `docs/COBOLNET_REARCHITECTURE_PLAN.md` is THE ONE PLANNING DOCUMENT (read its §0 resume banner FIRST → the worklists → `docs/COBOLNET_DESIGN.md` for design)
The North Star is a **commercial-quality, decades-sustainable COBOL compiler, 100% CONFORMING to ISO/IEC
1989:2023 per §4.2.16 with correct support for all prior editions (1985/2002/2014)** — owner decision D13;
optional modules may remain documented non-support; the definition of done is the PHASE-14 Step-0 traceability
inventory at zero GAP. **The go-forward plan is `docs/COBOLNET_REARCHITECTURE_PLAN.md` — THE ONE PLANNING
DOCUMENT** (17 phases; Roslyn↔CIL dual backend; §6 owner decisions D1–D20 all resolved). **Do NOT trust any
status snapshot here or in memory — the plan's §0 banner is the ONLY live resume point** (phases 00–12 done;
P13 in progress; then P14 [Step 0 = the traceability inventory] → P15 legacy retirement → P16 CIL backend).
The SSOT for locked invariants / settled decisions is **`docs/COBOLNET_DESIGN.md`**; the four-editions mission
is validated by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` + `docs/VERSION_CHANGE_REFERENCE.md`);
the verified defect/fix queue is the review ledger `docs/rearchitecture/PHASE-13-plan-vs-spec-review.md` §24.

## 🗺 DOC MAP: read `docs/DOC_INDEX.md` to navigate the docs
The index of all docs — each doc's subject, type (LIVE/DESIGN/LEDGER/SPEC), and a maintenance guide. **Consult it to
find the right doc; keep it in sync** when you add, retire, or materially change a doc. There is exactly one canonical
doc per subsystem — extend it, never fork a second. A new subsystem ⇒ a new doc (with a status banner) + a new index
row.

Read PROMPT.md before making any code change. It contains architectural doctrine and development rules. Every rule
exists because it was violated and corrected. They are non-negotiable.

Read DEVLOG.md for context on recent decisions, failures, and design rationale. **DEVLOG.md is in DESCENDING order —
newest entry FIRST** (the latest `## Entry` is immediately below the preamble's `> **Ordering: DESCENDING**` note;
the oldest is at the end). Add a new entry at the TOP, directly under that note, with a real date+time stamp in the
header — `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title` (from `date "+%Y-%m-%d %H:%M %Z"`). (Memory `feedback_devlog`.)

specs/ISO_COBOL.md contains the definitive ISO/IEC 1989:2023 COBOL specification (in the CobolSharp-private
submodule). Refer to it for all specification, behavior, syntax, and semantic questions. It is the authoritative
source — do not guess or assume COBOL semantics without consulting it. Initialize the submodule with:
`git submodule update --init --recursive`

## ⛔🔥 The project: a blank-slate rewrite → COBOL.NET (COBOL → C# via Roslyn)
COBOL.NET is a compiler (`src/Cobol.Net.*`, exe `cobol`) translating COBOL → **idiomatic typed-native C# source
compiled by Roslyn** — a COBOL record IS a .NET `record struct`, an elementary item IS a native field. **There is NO
byte `ProgramState` substrate; never fall back to the legacy byte engine.** **READ `docs/COBOLNET_DESIGN.md` FIRST** —
the decision-complete SSOT (bound-tree pipeline [NO lowered IR], `Place` lvalue, native scaled-integer numerics,
PC-dispatcher control flow, REDEFINES, files, OO, EC, intrinsics, Cobol.NET / `cobol.exe` naming, no-god-class
structure, C# 14, §18 settled decisions, G0–G8 order); `COBOLNET_ARCHITECTURE.md` is the brief overview. Memory:
[[feedback_complete_dotnet_migration_no_byte]], [[feedback_fully_autonomous_push]]. The legacy `CobolSharp.Compiler`
is kept ONLY as a reference + differential oracle until cut-over (G8). **MISSION = full ISO-2023 AND all prior editions
(85/2002/2014), validated by the VERSION TEST MATRIX (test as N per-edition compilers):
`docs/VERSION_CHANGE_REFERENCE.md` + `docs/VERSION_TEST_MATRIX_DESIGN.md`. The default `--std` is COBOL-2023.**
Memories `feedback_version_test_matrix`, `feedback_version_targeted_semantics`.
