# CobolSharp — Claude Code Instructions

## ⛔ NON-NEGOTIABLE PROCESS RULES (owner-emphasized — obey BEFORE writing any code)
These are the rules the owner most insists on (repeatedly corrected). Full text at the top of `PROMPT.md` and
`resume-prompt.md`; durable in memories `feedback_use_the_spec`, `feedback_follow_design_docs_and_spec`,
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
**Live kickoff = `resume-prompt.md`** (read it FIRST; its top RESUME banner + the §"NON-NEGOTIABLE PROCESS RULES"
block are current). The COBOL.NET rewrite SSOT is `docs/COBOLNET_DESIGN.md`.

## ⛔ TOP-LEVEL PLAN: `docs/COBOLNET_REARCHITECTURE_PLAN.md` is the go-forward roadmap (read `resume-prompt.md` FIRST → the plan → `docs/COBOLNET_DESIGN.md`)
The North Star is a **commercial-quality, decades-sustainable, full ISO/IEC 1989:2023 COBOL compiler with correct
support for all prior editions (1985 / 2002 / 2014)** — implemented with maximum autonomy + practical parallelism, no
back-compat. **The go-forward roadmap is `docs/COBOLNET_REARCHITECTURE_PLAN.md`** — a resumable, execution-grade
**17-phase** rearchitecture + 100%-ISO plan (clean architecture · all editions · a selectable Roslyn↔CIL backend)
that subsumes the feature/NIST drive as its phases 09–14; `resume-prompt.md`'s top banner points to it + its §0 resume
protocol. **Phases 00–07 are DONE (PHASE-07 closed: both god classes dissolved; `Place` structural via
`CodeGen.Roslyn.PlaceRenderer`; FUNCTION args parse as real `arithmeticExpression`s and the `IntrinsicRenderer`
static channel is deleted); Exec Step E (edition-gate remediation) is DONE — the two-arm `VersionConformancePass` is the ONE gating funnel with the §1.1 exception ledger; RESUME AT Exec Step F (PHASE-08 runtime reorg).** §6 owner decisions D1–D12 are
ALL resolved. Battery: 3166+ conformance · 282 unit · 33 characterization (32 snapshots byte-exact + ratchet) · legacy
guard 353 MATCH. **Always read `resume-prompt.md`'s top banner for the live resume point, never this snapshot.** The
SSOT for locked invariants / settled decisions is **`docs/COBOLNET_DESIGN.md`**; the four-editions mission is validated
by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md`) against the edition-change checklist
`docs/VERSION_CHANGE_REFERENCE.md`. The greenfield design lives in the `docs/COBOLNET_*` corpus (the pre-PIVOT
byte-engine plan/architecture docs were retired).

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
