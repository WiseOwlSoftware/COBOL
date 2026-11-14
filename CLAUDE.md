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
Each was earned by a correction. These eight are the SSOT; `PROMPT.md` holds the standing doctrine (mission, architectural
commitments, the four required reviews) and does not restate them. Other documents cite them by number.

1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL_2023.md`) defines correct behavior for EVERY case.** Read it and cite
   the exact §/GR for any semantics, syntax, or output question. The NIST goldens and the GnuCOBOL differential are
   regression NETS with known holes — never authority. **Where there are several implementation options, the precedence
   is: the ISO spec if it controls → otherwise GnuCOBOL → if GnuCOBOL has nothing, IBM Enterprise COBOL or Micro Focus**
   (`kb/Work/R13`). This settles implementor LATITUDE only — never GnuCOBOL's non-ISO extensions — and the choice is
   documented in `docs/CONFORMANCE.md`. When a general-format DIAGRAM is load-bearing, render the PDF page
   (`scripts/render-spec-page.py <page>`): the OCR'd diagrams were systematically lossy toward falsely-restrictive syntax.
   **A citation names its edition: `§<edition>:<locator>[ <rule>]`** — `§2023:13.18.35.4 GR1`, `§1985:VI.5.3.4 GR3`
   (kb/Work PB4887; the format and its one parser are `scripts/spec/citation.py`). A bare `§` is a section of a repository
   document, never the standard.
   **⛔ VALIDATE EVERY CITATION MECHANICALLY — `python scripts/spec/cite.py --check "§2023:<clause>[ <rule>]" "<text>"`.**
   The failure mode is not inventing a citation, it is INHERITING one: a note or design doc carries a §, its quoted text is
   genuinely in the standard, and the clause NUMBER is never re-derived before it propagates into code comments, goldens
   and the DEVLOG. A citation you did not run `--check` on is not a citation. It checks every part, in every edition: the
   clause, the rule's kind, ordinal and sub-item (`GR6c`), and that a quotation of four or more words lies inside that
   rule; a failure names the part and, where it can, the right designator (kb/Work PB5317).
2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/DESIGN.md` §0.5 lists them) plus the spec.
   Follow the doc; do not improvise. A design correction updates the doc in the same change set.
3. **Implement the COMPLETE feature to spec + design — never scope to what a test references.** Tests verify, they
   do not scope. Deferral, a GAP, or rejecting legal source is debt, taken only by a recorded decision note.
4. **Fix the root architectural cause.** No workarounds, no papering over, no relabeling a bug a "quirk", never
   change valid COBOL to dodge a compiler bug. Every bug is a pattern — sweep for its siblings, and fix a new issue found
   in a file already open in the same change unless it is a redesign of its own (then it becomes a `kb/Work/` note).
   **⛔ NO WRAPPERS, SHIMS, FORWARDERS, ALIASES OR COMPATIBILITY LAYERS, anywhere: compilers, runtime, tests,
   scripts, tooling, skills.** There is NO backward-compatibility requirement. When something moves, is renamed or
   is replaced, change EVERY caller and delete the old one in the same change.
5. **RE-DESIGN AND RE-ARCHITECT WHEN NECESSARY — a stated scope is an estimate, never a ceiling.** This compiler
   is production quality and must stay supportable for MORE THAN A DECADE, so when a note, finding or design doc says
   "one clause, no structural change" and implementation proves otherwise, the answer is the restructuring — never the
   smallest diff that fits the estimate, and never a hand-maintained list where a structure belongs. **Prefer the shape
   that makes the NEXT case automatic over the one that makes this case small**, and pair it with a drift test so
   "automatic" stays true. Correcting the estimate is part of the work: update the design doc and say so. (Rule 3
   forbids shrinking the FEATURE; this forbids shrinking the DESIGN.)
6. **Keep the docs CURRENT in the same change set.** Every doc except the DEVLOG describes the CURRENT compiler.
   The historical narrative lives ONLY in `DEVLOG/`, one file per entry, read newest first (`python scripts/devlog.py
   render`). Write the entry as `DEVLOG/pending-<slug>.md` (first line `## <Title>`); the landing numbers and stamps it.
   **One entry per landing on main**; the landing checks refuse a landing without it.
7. **Work autonomously.** Commit AND push every checkpoint, with a forensic commit message. Grammar changes need no
   approval. Decide what the Ownership section makes yours; ask the owner only what it keeps theirs.
8. **⛔ THERE IS EXACTLY ONE WORK REGISTER — `kb/Work/` — AND YOU MAY NOT CREATE ANOTHER.** One note per item
   (`kind:` defect · analysis · adjudication · decision · upstream), tracked in git, frontmatter carrying `status`,
   `severity`, `area`, the harm flags, `inventory_rows` (the traceability-inventory rows the note CLAIMS while
   it is open) and `closes_rows` (the rows its landing CLOSED, with a `closes_rows_reason:` when it closed none);
   the forensic prose lives in the note body. Its views are `work.py` subcommands (`blocked`, `unharmed`, `analyses`,
   `landed`, `upstream`, `stats`). **Read it with `python scripts/spec/work.py next` and keep it CURRENT in the same
   change set as the work** — a landed fix flips its note's `status` AND writes `closes_rows` in the commit that lands
   it, and a newly found defect becomes a note before it becomes a DEVLOG paragraph. **A finding, lead or landing paragraph
   added to a note after it is filed is a NEW FILE beside it** (`kb/Work/<id>/<yyyy-mm-dd>-<slug>.md`, which `work.py` reads
   as part of the note; `python scripts/orchestrator/compact_notes.py` folds them back when nothing else is in flight): the
   note file itself is edited only for its own frontmatter and by its owner, so two branches never edit one file for an
   append and plain git merges everything, locally and in the merge queue (owner 2026-11-10, kb/Work PB5110: no custom merge drivers).
   ⛔ **Do not open a new list, table, tracker, checklist or "remaining work" section anywhere** — not in plan §0, not in
   a design doc, not in a new markdown file, not in a JSON sidecar. Five such registers had accumulated by 2026-08-04,
   three each declaring themselves canonical, and a WRONG-ANSWER defect sat in a prose paragraph where no work list
   could see it. The only other registers are the ones derived mechanically from the spec (the rule catalog → the
   traceability inventory → its generated burn-down) and `constructs/`; those are GENERATED or CI-owned.

**Subagents are a tool, used on judgment.** Nothing here says when or how to delegate. The `workstream` skill, its
templates and the orchestrator loop are available machinery, not a gate.

**Prove your work** (owner 2026-11-10: "You should always prove your work"). A claim that something is done, landed,
passing or the cause of a failure carries the evidence produced for it in this session: the command and its verdict line,
the merged pull request and commit, the count before and after. A check counts only if it could have failed, and a
subagent's report is a claim until sampled.

**Do not redo work whose inputs did not change.** Before a gate, a re-run, a wait or a dispatch, ask what it can show that
is not already known: a re-gate at an unchanged tree, a full gate after an inert-only commit, a gate on a branch that must
merge main again before it lands, and a hold not verified with `git diff` are each waste (kb/Work PB5099, PB5100, PB5110).

## Start here every session
1. **`kb/Work/` — THE WORK REGISTER, and the answer to "what do I do now".** Run `python scripts/spec/work.py next`
   (**Fix next**; `blocked`, `unharmed`, `analyses`, `landed` and `upstream` are the other views, each with `--json`). It
   ranks on what a defect DOES to a user's program, not on its severity label. ⛔ Never re-derive a worklist from prose.
2. **`docs/REARCHITECTURE_PLAN.md` §0** — live state, gates, open GAPs, decisions and the campaign narrative. It carries
   no worklist and must never regrow one (rule 8). Trust §0 over any status written anywhere else, including memory —
   but trust `kb/Work/` over §0 for what is OPEN.
3. **`pwsh scripts/session-probe.ps1`** — the mechanical state check (branch · dirty/unpushed · next-free
   diagnostic code · VCR todos · corpus counts · inventory GAP). Never hand-read state a script can compute. The
   `session-start` skill walks these three steps.
4. Before the session ends: update the `kb/Work/` notes you touched, update §0, add a DEVLOG entry.

## The project
WiseOwl COBOL (repo `WiseOwlSoftware/COBOL`; code `src/WiseOwl.Cobol.*` in namespaces `WiseOwl.Cobol.*`, solution
`WiseOwl.Cobol.sln`, exe `cobol`, NuGet `WiseOwl.COBOL`) compiles COBOL into **idiomatic typed-native C# built by Roslyn**: a
COBOL record IS a .NET `record struct`, an elementary item IS a native field. **There is NO byte `ProgramState`
substrate — never reintroduce one.** The legacy byte engine that had one is deleted from `main`;
`docs/rearchitecture/LEGACY-ARCHIVE.md` names the tag that preserves it.

**Mission (D13):** a commercial-quality, decades-sustainable compiler that is **100% conforming to ISO/IEC 1989:2023 per
§2023:4.2.1, with correct support for 1985/2002/2014** — validated as four per-edition compilers by the VERSION TEST
MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` + `docs/VERSION_CHANGE_REFERENCE.md`; the default `--std` is COBOL-2023).
Done = every edition's traceability inventory at zero GAP.

**The product bar is broader than conformance.** Four review dimensions — **architecture · full code review · performance
· duplication and efficiency** — are continuous criteria on every change (`PROMPT.md` §4).

## Where things live
- **Plan / live state:** `docs/REARCHITECTURE_PLAN.md` — §0 live state · §3 execution model · §8 forward-residue ledger ·
  §9 verification commands + corpus mechanics · §11 analysis backlog · §12 risk register.
- **Design SSOT:** `docs/DESIGN.md` plus the `docs/rearchitecture/DESIGN-*.md` deep-dives.
- **Names:** the code carries the product's name, `WiseOwl.Cobol`; the product's earlier names live only in history, and
  `scripts/rename_retired_names.py --check` refuses them elsewhere. Implementor environment variables are `WISEOWL_COBOL_*`.
- **The work register:** `kb/Work/` (rule 8). `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md` is only a pointer to it.
- **History:** `DEVLOG/` (one file per entry), and nowhere else. **Doctrine:** `PROMPT.md`.
- **Doc map:** `docs/DOC_INDEX.md` — consult it to find the right doc and keep it in sync. Exactly one canonical doc per
  subsystem: extend it, never fork a second.
- **Spec:** `specs/ISO_COBOL_2023.md` and `specs/ANSI_COBOL_1985.md` (tracked). The licensed PDFs are the private
  submodule `specs-private` (`git submodule update --init --recursive`) — needed only to render a page.

## Testing
THE POPULATION IS THE PLAN (kb/Work PB4323). **The gate has ONE shape: `pwsh scripts/build-local.ps1 -Priority
BelowNormal`** (`scripts/gate.py`), run at a branch's head before it lands. It decides a lane from the diff's paths:
- **inert** (`docs/`, `kb/`, `DEVLOG/` and every `*.md`, except the files `inert_lane.py`'s `CLASSIFICATION.read` lists:
  the ISO text, the oracle, and the data files under docs/ that compiler tests read): the tree audits, about 40 s;
- **tooling** (`scripts/`, `.github/`, `tools/`, `.claude/`, any `*.py`/`*.ps1`/`*.sh`, except the tooling a compiler test
  or the build reads, `CODE_READS`): the audits and the self-tests whose recorded inputs it touches, with no build;
- **code** (everything else): the build once and, on Windows, the WHOLE population of every test target in
  `scripts/build_targets.json` (each keyed by its declared inputs: one up to date at its key does not run).
There is no impact map, no tier, no selected leg and no Linux half per change; the inner loop (`dotnet test --filter` by
hand) claims nothing. **A branch lands through ONE script, `python scripts/orchestrator/enqueue.py land <branch>`**: it
refuses a tip no GREEN record covers, merges `origin/main` in, stamps the DEVLOG entry, runs the landing checks, publishes
this host's passes to the store ref and queues one pull request in GitHub's merge queue. **The merge group runs every target
on Linux (Release) and, on Windows (Debug), each target with no pass at the merged tree's key on the store ref the landing
publishes** (kb/Work PB5235), and it is the ONLY run of the merged tree, of Linux and of Release. A nightly runs the CLI NIST guard, every self-test on both OSes and the GnuCOBOL
differential. Build `WiseOwl.Cobol.sln` (not a single project) before any `--no-build` run. Commands and the current
baseline are in plan §0 "Gates" and §9.

⛔ **THE RULE: every case the merge group runs on Windows already ran green at the pushed head, on this host, under the
same `toolchain.env`.** It asks that each case ran green at that tree, not that all ran in one run (kb/Work PB5099). The
merge group is therefore the FIRST run of (a) the merged tree, (b) Linux, (c) Release with a cold compile cache, (d) the
Windows self-tests a change's files reach, and (e) GitHub's runners; a merge-group red is one of those five, a flake, or
infrastructure — never an untested case. Classify every red against the branch's gate record before anything is changed:
an infrastructure or runner red is re-queued once; a merge red or a genuine red is fixed at its root on the branch; a
flake is a `kb/Work` defect fixed at its root, never retried inside the run. A red of class (b), (c) or (e) is also a
finding against the pipeline's record of that class (`kb/Work/PB1957`). **CI and every local host run the SAME tool
versions, the LATEST releases**: one toolchain manifest (`toolchain.env`), everything installs from it.
