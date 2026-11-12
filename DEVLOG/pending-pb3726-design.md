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
that would silently stop a test running, and an exact ratchet refuses any new in-file self-test. Each conversion deletes
its script's `--self-test` handler, and the last step narrows discovery to `selftests/`. The hot scripts are split by
job: `gate.py` in two steps (its modules, then the `Gate` class), plus `inert_lane.py`, `land_lane.py`, `enqueue.py` and
`prune_worktrees.py`. The design assigns every definition of today's files to a module. `docs/CONFORMANCE.md` first gets one
reader. Then its storage becomes one file per entry under `docs/conformance/` (the rows of sections 7 and 8 included), and the
whole document is a rendered view. The three design documents stay single files. `PublicAPI.Unshipped.txt` keeps the
analyzer's order, so plain git merges two branches' symbols.

The register: 17 new notes, PB5395 to PB5411 (16 steps and PB5410, an analysis that hands the compiler-side hold
concentrations to the architecture review). Ten existing step notes (PB3729 to PB3738) carry a dated re-design
addendum and a new tier and order. PB3726 gains `PB3726/2026-11-12-design.md`, which lists the decisions and the order.
