## Entry 3742 — 2026-11-16 02:02 PST — Eight notes and three finding files from the Mythos lane's batch-9 rulings of 2026-11-16

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed, and a Sites block.

- PB5832 (defect, MAJOR): at `--std 85` an integer argument of an intrinsic function that is a product, an item quotient inside an argument list, or an expression holding a numeric function compiles and is silently truncated; seven 1985 rows, and the 2023 run-time check that makes the open screen safe does not exist in 1985.
- PB5833 (defect, MAJOR): a reference-modified element under an ALL subscript is classed as its numeric element by the argument-class screen, so MAX and MIN refuse legal lists, MEAN, MEDIAN and MIDRANGE compute from digit slices, and an illegal mix compiles and fails at run time (every edition).
- PB5835 (defect, MAJOR): at `--std 85` a relation between a numeric function and an 18-digit literal is compared in binary floating point, so `IF FUNCTION SQRT (4) = 2.00000000000000001` is true; the row `1989-RV-A.2.8.4-1` itself is confirmed, and the 1985 rule for comparing numeric operands has no row in the 1985 catalog.
- PB5834 (adjudication, open): may ALL stand for any argument of a function whose definition allows repetition, or only for the repeated argument (`PRESENT-VALUE (T (ALL))`)? The 1989 text admits both readings.
- PB5836 and PB5837 (defects, process only): the branch survey reads a finished hand-dispatched branch as IN FLIGHT while the dispatching session lives (the worktree lock carries the session's process id), and after a conflict finisher it does not offer the branch.
- PB5838 (defect, process only): an own verdict that comes from a read cannot close on a derivation, and the widening planner plans a rule whose stored row is already OK.
- PB5840 (analysis, process only): the 2002 text dropped the 1985 sentence that treats sequential mass storage files as files on tape; its annex of changes and `docs/VERSION_CHANGE_REFERENCE.md` do not record it. A fact, not a task: the 2002 edition's work is frozen by the owner.

Three dated finding files beside existing notes: `kb/Work/PB5775/2026-11-16-two-more-rows.md` (the rows `1985-SR-XIII.3.7.3-1` and `1985-SR-XIII.4.3.3-1`), `kb/Work/PB4950/2026-11-16-more-instances-at-85.md` (the diagnostics that cite a 2023 rule at `--std 85`, COBOL2973, COBOL2198 and COBOL1627), and the ruling copied unchanged as `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-9.md`.

**The population is read.** The ruling counts 83 of 1,106 rows of the 1985 code reads contradicted (7.5 %), from the rulings files by script.

