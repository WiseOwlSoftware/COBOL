# WiseOwl COBOL — Claude Code Instructions

## Ownership
The owner delegated this codebase to Claude on 2026-11-10: "The entire wise owl cobol code base is yours. What it's for and
how it's built are your call; and you'll continue to build upon it", extended to "reorganization of the existing code base",
and "You decide when to delegate". So a decision about the codebase (design, scope, a deferral, doctrine, tooling and process
shape, a restructuring, what is built next, whether and how to use subagents) is Claude's: decide it, record it and its
reasoning as a `kind: decision` note in `kb/Work/`, and proceed. Owning it means owning its cost: money, tokens and the
owner's time spent on work that did not need doing is a defect of the session that spent it.

**Still the owner's, one bare question at a time:** money and quota beyond the plan in force (API credit, resets, a model
tier not yet approved), accounts and credentials, anything sent to people outside (ANSI, an upstream report, email),
publishing or licensing the product, and deleting history or branches others use.

**The quota plan in force (owner 2026-11-11, kb/Work PB5357):** no borrowed days on either account. `python
scripts/orchestrator/budget.py` at borrow 0 is the line: an account above it starts no worker, fleet or landing until the
line passes its reading, and each account spends its remainder by the late evening before its own weekly reset. A Mythos
or Fable dispatch on either account needs the owner's explicit approval for that dispatch.

## The rules
Other documents cite these by number. `PROMPT.md` holds the standing doctrine (mission, architecture, the four reviews).

1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL_2023.md`) defines correct behavior.** Cite the exact clause and rule for
   any semantics, syntax or output question. The NIST goldens and the GnuCOBOL differential are regression nets, not
   authority. For implementor latitude the precedence is: the ISO spec → GnuCOBOL → IBM Enterprise COBOL or Micro Focus
   (`kb/Work/R13`), never a GnuCOBOL extension; record the choice in `docs/conformance/`. When a syntax diagram matters,
   read the rendered PDF page (`scripts/render-spec-page.py <page>`): the OCR'd diagrams lean falsely restrictive.
   A citation names its edition, `§2023:8.4.2.3.4 GR1b` (a bare `§` is a section of a repository document), and is checked
   with `python scripts/spec/cite.py --check "§2023:<clause>[ <rule>]" "<text>"` before it goes anywhere: the usual error is
   a clause number inherited from a note and never re-derived.
2. **Implement each feature from its subsystem design doc** (`docs/DESIGN.md` §0.5 lists them) plus the spec. A design
   correction updates the doc in the same change.
3. **Implement the complete feature to spec and design**, not the part a test exercises. A deferral, a GAP or rejecting
   legal source is debt taken only by a recorded decision note.
4. **Fix the root cause.** No workarounds, and never change valid COBOL to avoid a compiler bug. Look for the bug's
   siblings. No wrappers, shims, forwarders, aliases or compatibility layers anywhere: there is no backward-compatibility
   requirement, so a move or rename changes every caller and deletes the old one in the same change.
5. **Re-design when the work shows it is needed.** A scope estimate is not a ceiling. This compiler must stay supportable
   for more than a decade: prefer the shape that makes the next case automatic, pair it with a drift test, and update the
   design doc when the estimate was wrong.
6. **Keep the docs current in the same change.** Every doc except the DEVLOG describes the current compiler. History lives
   only in `DEVLOG/`, one file per entry: write `DEVLOG/pending-<slug>.md` (first line `## <Title>`); the landing numbers it.
   One entry per landing on main.
7. **Work autonomously.** Commit and push every checkpoint with a forensic commit message. Ask the owner only what the
   Ownership section reserves for the owner.
8. **`kb/Work/` is the one work register.** One note per item (`kind:` defect · analysis · adjudication · decision ·
   upstream) with frontmatter for `status`, `severity`, `area`, harm flags, `inventory_rows` and `closes_rows`. Read it with
   `python scripts/spec/work.py next`; keep it current in the change that does the work (a landed fix flips its note's
   `status` and writes `closes_rows`). A later finding on a filed note is a new file beside it,
   `kb/Work/<id>/<yyyy-mm-dd>-<slug>.md`, so two branches never edit one file. Do not start another list, tracker or
   "remaining work" section anywhere; the only other registers are generated from the spec or owned by CI.

**Prove your work** (owner 2026-11-10). A claim that something is done, landed, passing or the cause of a failure carries
evidence produced in this session: the command and its verdict line, the merged pull request, the count before and after.
**Do not redo work whose inputs did not change** (a re-gate at an unchanged tree, a gate on a branch that must merge main
again first).

## Start here
`python scripts/session_probe.py` (state), `python scripts/spec/work.py next` (what to do), and `docs/REARCHITECTURE_PLAN.md`
§0 (live state and gates; it carries no worklist). The `session-start` skill walks them. Before a session ends, update the
notes you touched, §0, and the DEVLOG.

## The project
WiseOwl COBOL (repo `WiseOwlSoftware/COBOL`; code `src/WiseOwl.Cobol.*`, solution `WiseOwl.Cobol.sln`, exe `cobol`, NuGet
`WiseOwl.COBOL`) compiles COBOL into idiomatic typed-native C# built by Roslyn: a COBOL record is a .NET `record struct`, an
elementary item a native field. There is no byte `ProgramState` substrate; never reintroduce one.
**Mission (D13):** 100% conforming to ISO/IEC 1989:2023 per §2023:4.2.1, with correct support for 1985, 2002 and 2014,
validated per edition by the version test matrix (`docs/VERSION_TEST_MATRIX_DESIGN.md`). Done = every edition's
traceability inventory at zero GAP.

## Where things live
`docs/DOC_INDEX.md` maps the docs (one canonical doc per subsystem). Design: `docs/DESIGN.md` and
`docs/rearchitecture/DESIGN-*.md`. Doctrine: `PROMPT.md`. Specs: `specs/ISO_COBOL_2023.md`, `specs/ANSI_COBOL_1985.md`;
the licensed PDFs are the private submodule `specs-private`. Implementor environment variables are `WISEOWL_COBOL_*`.

## Testing and landing
The one gate is `pwsh scripts/build-local.ps1 -Priority BelowNormal`, run at a branch's head; it picks its lane from the
diff. A branch lands only through `python scripts/orchestrator/enqueue.py land <branch>`, which refuses a tip without a
GREEN record and queues the pull request in GitHub's merge queue. The `gate` skill and plan §9 carry the details. Classify a
merge-group red before changing anything; a flake is a `kb/Work` defect fixed at its root, never a retry. Build
`WiseOwl.Cobol.sln` before any `--no-build` run.
