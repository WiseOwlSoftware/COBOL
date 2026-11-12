**Addendum (2026-11-12, branch witnesses-85k, the 1985 draft triage of kb/Work PB4995): still under-rejected.**

The re-run of this morning (kb/Work PB4995, `2026-11-12-rerun-85g.md`) left 1 draft(s) of this note's rule(s) mismatching. The triage read each rule and its draft again: the draft breaks the rule this note names and nothing else (its control compiles, exit 0), so the compiler is wrong and the root is this note's. Each repro is the draft unchanged, compiled on a Debug build of origin/main 09ce7dc601 (cobol 25.0.0, assemblies 5d83a43c44a7):

- `1985-GR-VI.6.4.3-3`: repro `E:/COBOL-coord/scratch/triage-85k/repro/1985-GR-VI.6.4.3-3.cob`, line 13 `MOVE CORRESPONDING R TO G2.`. `cobol <repro> --std 85`: exit 0, no diagnostic; the program runs and its first line of output is `P`.

No witness is placed and no verdict recorded for these rows; a row with a compiler defect takes its verdict from a later code read.
