## PB3726 re-designed: the pipeline-scripts restructure measured again and decided against today's tree

No code changed; this entry covers the design in `docs/rearchitecture/DESIGN-orchestrator-loop.md` section 16 and the
kb/Work step notes that implement it.

The owner decided on 2026-10-25 to restructure the pipeline scripts so parallel changes stop colliding, with the design
by a Mythos model. A first design landed then, and three of its steps landed (PB3637, PB3727, PB3728). The pipeline
was rebuilt after that design, and on 2026-11-10 the owner deleted every merge driver (PB5110). Two of the design's
subjects no longer existed (`run_gate_legs.py`, the keyed-table and PublicAPI drivers), and its arm checks named C# tests
that had moved to `scripts/self_test_arms.py`. So section 16 was measured again on `origin/main` `3798495df1` and rewritten.

What the new measurement shows, over the 14 days to that commit (the commands are in section 16): 792 pull requests landed
and 174 touched `scripts/`. The hottest scripts are `test_plan_wave.py` (26 landings), `gate.py` (22; its 944-line
`Gate` class took 131 of the file's 367 hunks), `inert_lane.py` (18) and `land_lane.py` (16). `enqueue.py`'s self-test
took 170 of its 298 hunks. The dispatch ledger held groups out of a wave 638 times in the window. `docs/CONFORMANCE.md`
caused 161 of those holds, and every script together caused 42. The three design documents PB3726 named caused 14, 0 and
1 holds and no conflict brief.

The decisions. Every script's tests move out of the script into `<root>/selftests/<path>/<name>/`, one `case_`
function per case and one file per fixture world. A layout rule that the self-test runner calls first refuses the shapes
that would silently stop a test running, and a set of the files that still hold an in-file self-test, one path per line,
refuses any new one; each conversion removes its own lines, so two branches never collide on a count. Each conversion
deletes its script's `--self-test` handler, and the last step narrows discovery to `selftests/`. The hot scripts are split
by job: `gate.py` in two steps (its modules, then the `Gate` class), plus `inert_lane.py`, `land_lane.py`, `enqueue.py`
and `prune_worktrees.py`. The design assigns every definition of today's files to a module. `docs/CONFORMANCE.md` first
gets one reader, then is made regular in place (50 of its 125 section 3 entries had no key, and four sections had prose
after their entries), then becomes one file per entry under `docs/conformance/`. The whole document is a gitignored,
stamped view that a gate audit refuses to see edited, every instruction that says where to write a determination
(CLAUDE.md rule 1 among them) moves to `docs/conformance/`, and the package carries the rendered statement that 62
compiler and CLI strings name. The three design documents stay single files. `PublicAPI.Unshipped.txt` keeps one
ordinal order, held by an audit, so plain git merges two branches' symbols.

An adversarial review the same day returned SOUND WITH FIXES with thirteen findings, six blocking; all thirteen
reproduced and all are answered in section 16 (the list is `kb/Work/PB3726/2026-11-12-design-review.md`). The main
changes it caused: four compiler tests that run or read script self-tests are now in the steps that move their scripts;
case files import the collector through one `PYTHONPATH` the runner sets, and the static import readers and the
scratch-repository audit see into `selftests/`; the steps run strictly serially, the case layout first; and the `Gate`
class split decides seven cross-boundary calls.

The register: PB5395 to PB5411 (16 steps and PB5410, an analysis that hands the compiler-side hold concentrations to
the architecture review) and, after the review, PB5428 to PB5441 (14 steps). The ten step notes filed in 2026-10
(PB3729 to PB3738) are retired, each superseded by a fresh note: their bodies claimed files the pipeline has deleted,
and `work.py` reads the union of a note's Sites blocks, so no addendum could withdraw them. PB4053 now waits on PB5430.
PB3726 gains `2026-11-12-design.md`, `2026-11-12-design-review.md` and `2026-11-12-design-steps-after-review.md` (the
final order: 30 steps in one chain).
