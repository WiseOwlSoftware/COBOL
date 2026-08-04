# COBOL.NET — Claude Code Instructions

## ⛔ Non-negotiable process rules
Owner-emphasized, each earned by a correction. These seven are the SSOT; `PROMPT.md` holds the standing doctrine
(mission, architectural commitments, the four required reviews) and does not restate them.

1. **The ISO/IEC 1989:2023 spec (`specs/ISO_COBOL.md`) defines correct behavior for EVERY case.** Read it and cite
   the exact §/GR for any semantics, syntax, or output question. The legacy oracle, the NIST goldens and the
   GnuCOBOL differential are regression NETS with known holes — never authority. When a general-format DIAGRAM is
   load-bearing, render the PDF page (`scripts/render-spec-page.py <page>`): the OCR'd diagrams were systematically
   lossy toward falsely-restrictive syntax.
   **⛔ VALIDATE EVERY CITATION MECHANICALLY — `python scripts/spec/cite.py --check <clause> "<text>"`.** The
   failure mode is not inventing a citation, it is INHERITING one: a queue entry or design doc carries a §, its
   quoted text is genuinely in the standard, and the clause NUMBER is never re-derived before it propagates into
   code comments, goldens and the DEVLOG. Two CA10 citations were wrong exactly this way. A citation you did not
   run `--check` on is not a citation.
2. **Implement each feature FROM its subsystem deep-dive design doc** (`docs/COBOLNET_DESIGN.md` §0.5 lists them)
   plus the spec. Follow the doc; do not improvise. A design correction updates the doc in the same change set.
3. **Implement the COMPLETE feature to spec + design — never scope to what a test references.** Tests verify, they
   do not scope. Deferral, a GAP, or rejecting legal source is debt, and only an explicit owner decision.
4. **Fix the root architectural cause.** No workarounds, no papering over, no relabeling a bug a "quirk", never
   change valid COBOL to dodge a compiler bug. Every bug is a pattern — sweep for its siblings.
5. **RE-DESIGN AND RE-ARCHITECT WHEN NECESSARY — a stated scope is an estimate, never a ceiling.** This compiler
   is production quality and must stay supportable for MORE THAN A DECADE, so when a queue entry, finding or
   design doc says "one clause, no structural change" and implementation proves otherwise, the answer is the
   restructuring — never the smallest diff that fits the estimate, and never a hand-maintained list where a
   structure belongs. **Prefer the shape that makes the NEXT case automatic over the one that makes this case
   small**, and pair it with a drift test so "automatic" stays true. Correcting the estimate is part of the work:
   update the design doc and say so. (Rule 3 forbids shrinking the FEATURE; this forbids shrinking the DESIGN.)
6. **Keep the docs CURRENT in the same change set.** Every doc except `DEVLOG.md` describes the CURRENT compiler.
   The historical narrative lives ONLY in `DEVLOG.md`, which is DESCENDING — add each new entry at the TOP, under
   the ordering note, headed `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title` (stamp from `date "+%Y-%m-%d %H:%M %Z"`).
7. **Work autonomously.** Commit AND push every checkpoint, with a forensic commit message and a DEVLOG entry.
   Grammar changes are pre-authorized. Prompt only for genuine owner decisions — one at a time, as a bare question.

## Start here every session
1. **`docs/COBOLNET_REARCHITECTURE_PLAN.md` §0** — THE ONE PLANNING DOCUMENT and the ONLY live-state SSOT: the
   worklist, the gates, the open GAPs. Trust §0 over any status written anywhere else, including memory.
2. **`pwsh scripts/session-probe.ps1`** — the mechanical state check (branch · dirty/unpushed · next-free
   diagnostic code · VCR todos · corpus counts · inventory GAP). Never hand-read state a script can compute.
3. Update §0 and add a DEVLOG entry before the session ends.

## The project
COBOL.NET (`src/Cobol.Net.*`, exe `cobol`) compiles COBOL into **idiomatic typed-native C# built by Roslyn**: a
COBOL record IS a .NET `record struct`, an elementary item IS a native field. **There is NO byte `ProgramState`
substrate — never fall back to the legacy byte engine.** The legacy `CobolSharp.Compiler` survives only as a
differential oracle until the P15 cut-over, and that differential is opt-in
(`COBOLSHARP_LEGACY_DIFFERENTIAL=1`).

**Mission (owner decision D13):** a commercial-quality, decades-sustainable compiler that is **100% conforming to
ISO/IEC 1989:2023 per §4.2.16, with correct support for 1985/2002/2014** — validated as four per-edition compilers
by the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` + `docs/VERSION_CHANGE_REFERENCE.md`; the default
`--std` is COBOL-2023). Done = the PHASE-14 Step-0 traceability inventory at zero GAP.

**The product bar is broader than conformance.** The owner requires four review dimensions — **architecture · full
code review · performance · duplication and efficiency** — as continuous criteria on every change, and as a
comprehensive pass once the design settles (`PROMPT.md` §4).

## Where things live
- **Plan / live state:** `docs/COBOLNET_REARCHITECTURE_PLAN.md` — §0 live state · §3 execution model · §8
  forward-residue ledger · §9 verification commands + corpus mechanics · §11 analysis backlog · §12 risk register.
- **Design SSOT:** `docs/COBOLNET_DESIGN.md` plus the `docs/rearchitecture/DESIGN-*.md` deep-dives.
- **The work register:** **`kb/Work/`** — ONE note per item (defect · analysis · adjudication · decision),
  tracked in git, with the forensic prose in the note body. `kb/Work.base` is the view; **`Fix next`** ranks
  on what a defect DOES to a user's program, not on its severity label. `python scripts/spec/work.py next`
  prints it and session-probe shows it every session. ⛔ It replaced FIVE overlapping registers, three of
  which each claimed to be canonical; `CONFORMANCE-FIX-QUEUE.md` is now a pointer.
- **History:** `DEVLOG.md`, and nowhere else. **Doctrine:** `PROMPT.md`.
- **Doc map:** `docs/DOC_INDEX.md` — consult it to find the right doc and keep it in sync. Exactly one canonical
  doc per subsystem: extend it, never fork a second.
- **Spec:** `specs/ISO_COBOL.md` (private submodule — `git submodule update --init --recursive`).

## Testing
Per commit, run only the WAVE-LOCAL filtered gate (~2 min). Run the FULL Conformance suite plus the GnuCOBOL
differential once per accumulated batch, pre-merge. Build `CobolSharp.sln` — not a single project — before any
`--no-build` run. Commands and the current battery baseline are in plan §0 "Gates" and §9.
