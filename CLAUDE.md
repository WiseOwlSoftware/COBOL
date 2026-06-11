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
4. **Keep the deep-dive docs CURRENT** — when superseded, update the deep-dive in the same change set with the current
   design AND why the original wasn't followed.
**Live kickoff = `resume-prompt.md`** (read it FIRST; its top STATE banner + the §"NON-NEGOTIABLE PROCESS RULES" block
are current). The COBOL.NET rewrite SSOT is `docs/COBOLNET_DESIGN.md` (the PIVOT below).

## ⛔ TOP-LEVEL PLAN: read `resume-prompt.md` FIRST, then `docs/COBOLNET_DESIGN.md`
The North Star is a **commercial-quality, decades-sustainable, full ISO/IEC 1989:2023 COBOL compiler with correct
support for all prior editions (1985 / 2002 / 2014)** — implemented with maximum autonomy + practical parallelism, no
back-compat. The live plan is **`resume-prompt.md`** (its two-track RESUME AT: the version-correctness framework + the
feature/NIST corpus drive) over the SSOT **`docs/COBOLNET_DESIGN.md`**, validated by the VERSION TEST MATRIX
(`docs/VERSION_TEST_MATRIX_DESIGN.md`) against the edition-change checklist `docs/VERSION_CHANGE_REFERENCE.md`.
⛔ The pre-PIVOT **byte-engine plan + architecture docs were DELETED** (DEVLOG 523, as obsolete/misleading):
`PROJECT_PLAN.md`, `docs/MASTER_PLAN.md`, the `docs/DATA_MODEL_*` / `docs/RECORD_STRUCT_STORAGE_DESIGN.md` set, and the
~50 `docs/CobolSharp …` byte-engine architecture/guide docs. The greenfield design lives in the `docs/COBOLNET_*`
corpus. (memory `feedback_commercial_quality_north_star`.)

## 🗺 DOC MAP: read `docs/DOC_INDEX.md` to navigate the docs
The index of all docs (~126) — each doc's subject, type (LIVE/DESIGN/LEDGER/SPEC), and a maintenance guide. **Consult
it to find the right doc; keep it in sync** when you add, retire, or materially change a doc. There is exactly one
canonical doc per subsystem — extend it, never fork a second. A new subsystem ⇒ a new doc (with a status banner) + a
new index row.

Read PROMPT.md before making any code change. It contains architectural doctrine and development
rules derived from 13+ sessions of building this compiler. Every rule exists because it was
violated and corrected. They are non-negotiable.

Read DEVLOG.md for context on recent decisions, failures, and design rationale. **DEVLOG.md is in DESCENDING order —
newest entry FIRST** (the latest `## Entry` is immediately below the preamble's `> **Ordering: DESCENDING**` note;
the oldest is at the end). Add a new entry at the TOP, directly under that note, with a real date+time stamp in the
header — `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title` (from `date "+%Y-%m-%d %H:%M %Z"`). (Memory `feedback_devlog`.)

specs/ISO_COBOL.md contains the definitive ISO/IEC 1989:2023 COBOL specification (in the
CobolSharp-private submodule). Refer to it for all specification, behavior, syntax, and semantic
questions. It is the authoritative source — do not guess or assume COBOL semantics without
consulting it. Initialize the submodule with: `git submodule update --init --recursive`

## ⛔🔥 PIVOT (2026-06-08, DEVLOG 457): blank-slate rewrite → COBOL.NET (COBOL → C# via Roslyn)
The owner directed a **blank-slate rewrite**: a NEW compiler (`src/CobolNet`, exe `cobol`) translating COBOL →
**idiomatic typed-native C# source compiled by Roslyn** — a COBOL record IS a .NET `record struct`, an elementary
item IS a native field. **NO byte `ProgramState` substrate; never fall back to the legacy byte engine.** **READ
`docs/COBOLNET_DESIGN.md` FIRST** — the decision-complete SSOT (bound-tree pipeline [NO lowered IR], `Place` lvalue,
native scaled-integer numerics, PC-dispatcher control flow, REDEFINES, files, OO, EC, intrinsics, project
reorg/rename to Cobol.NET/cobol.exe, no-god-class structure, C# 14, §18 settled decisions, G0–G8 order);
`COBOLNET_ARCHITECTURE.md` is the brief overview. Memory: [[feedback_complete_dotnet_migration_no_byte]],
[[feedback_fully_autonomous_push]]. Legacy `CobolSharp.Compiler` kept ONLY as a reference + differential oracle until
cut-over (G8). Tests may break mid-transition; 100% green at completion. **STATE (DEVLOG 559, 2026-06-10): G0–G6
✅ + ALL FOUR wave-2 families (ODO, SORT/MERGE, KeyedIO, CALL/inter-program with multi-unit instance-class
emission), DECIMAL-POINT IS COMMA / CURRENCY SIGN, and the USE DECLARATIVES subsystem. The differential harness
locks 226 NIST programs byte-match (90/95 NC + 14 ST + 44 RL/IX + 18 IC + 61 SQ); 776 conformance + 15 unit
green. The CURRENT state + next steps live in `resume-prompt.md`'s top STATE banner — always read it, never this
snapshot.** ⛔ **MISSION now =
full ISO-2023 AND all prior editions (85/2002/2014), validated by the VERSION TEST MATRIX (test as N per-edition
compilers): `docs/VERSION_CHANGE_REFERENCE.md` (130-row edition-change checklist) + `docs/VERSION_TEST_MATRIX_DESIGN.md`
(matrix design, Phase 0 done). Default `--standard` is now COBOL-2023. Memories `feedback_version_test_matrix`,
`feedback_version_targeted_semantics`.** **See `resume-prompt.md` (live SSOT) for the current STATE + the two-track
RESUME AT.**
