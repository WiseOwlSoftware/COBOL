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

## ⛔ TOP-LEVEL PLAN: `docs/COBOLNET_REARCHITECTURE_PLAN.md` is the go-forward roadmap (read `resume-prompt.md` FIRST → the plan → `docs/COBOLNET_DESIGN.md`)
The North Star is a **commercial-quality, decades-sustainable, full ISO/IEC 1989:2023 COBOL compiler with correct
support for all prior editions (1985 / 2002 / 2014)** — implemented with maximum autonomy + practical parallelism, no
back-compat. **The go-forward roadmap (DEVLOG 665) is `docs/COBOLNET_REARCHITECTURE_PLAN.md`** — a resumable,
execution-grade **17-phase** rearchitecture + 100%-ISO plan (clean architecture · all editions · a selectable
Roslyn↔CIL backend) that SUBSUMES the prior feature/NIST drive as its phases 09–14; `resume-prompt.md`'s top banner
points to it + its §0 resume protocol. **EXECUTION NOT STARTED (Phase 00 next); ~12 owner decisions in its §6.** The
SSOT for locked invariants / settled decisions remains **`docs/COBOLNET_DESIGN.md`**, and the four-editions mission is
validated by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md`) against the edition-change checklist
`docs/VERSION_CHANGE_REFERENCE.md`.
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
cut-over (G8). Tests may break mid-transition; 100% green at completion. **STATE (DEVLOG 575, 2026-06-11):
⛔🎉 PHASE 1 COMPLETE — G0–G6 ✅, the COBOL-85 corpus drive is CLOSED. Every golden-bearing NIST program is
locked byte-exact (318 = 93 NC + 29 ST + 32 RL + 40 IX + 23 IC + 69 SQ + 42 IF + 15 SM + 4 RW + 2 OBSQ);
final census 357/403 GREEN with zero diffs (residue golden-less by NIST design); 1026 conformance + 16 unit;
legacy guard 353 MATCH + 11 `LEGACY_DIVERGENT` (ISO-re-baselined goldens — citations in `scripts/guard.sh`),
0 regressions. Landed in Phase 1: COPY/SM, the full §15 intrinsic catalog, the Tier-C record codec, LINAGE,
the IC residue (EXTERNAL/GLOBAL FDs, cross-assembly CALL), and the Report Writer. The EC exception model
(§11/§14.6.13 — >>TURN, RAISE/RESUME, USE F3, RAISING propagation, the status→EC bridges, EXCEPTION-*
functions) is DONE (DEVLOG 577; 1074 conformance + 29 unit). NEXT (SSOT §16): G7 per-edition correctness
(EditionValidator + M2 OO/2002 → M3 2014 → M4 2023) → G8.
The CURRENT state + next steps live in `resume-prompt.md`'s top STATE banner — always read it, never this
snapshot.** ⛔ **MISSION now =
full ISO-2023 AND all prior editions (85/2002/2014), validated by the VERSION TEST MATRIX (test as N per-edition
compilers): `docs/VERSION_CHANGE_REFERENCE.md` (130-row edition-change checklist) + `docs/VERSION_TEST_MATRIX_DESIGN.md`
(matrix design, Phase 0 done). Default `--standard` is now COBOL-2023. Memories `feedback_version_test_matrix`,
`feedback_version_targeted_semantics`.** **See `resume-prompt.md` (live SSOT) for the current STATE + the two-track
RESUME AT.**
