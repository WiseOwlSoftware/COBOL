## Entry 3718 — 2026-11-15 19:55 PST — gl1985 integrator b: 173 1985 rows gain a case of their own (waves 3 and 4, 24 slugs), 1985 GAP 844 to 671

The 1985 golden lane (gl1985) writes a new conformance case, accepted at `--std 85` only and citing the row's own
1985 catalog id, for each KEPT 1985 row that kb/Work PB5675's own-witness rule left open and no widening reaches. This
is the second integration: 24 input files of waves 3 and 4, 184 rows.

**The rows.** By kind: GR 112, AR 25, RV 20, SR 14, FMT 13. By why the widening failed (the set of reasons over a row's
lacking 2023 targets): band 89, no-case 60, other 20, band+no-case 8, refusal-not-evidence 5, no-case+other 1,
other+refusal-not-evidence 1. Dispositions: 173 new-golden, 11 not-closable, no defect-suspected.

**What landed.** 131 cases (128 accepted at 85, 3 refusals) and two copybooks, in 86 `@1985` case folders (16 of them new). Each
accepted case was run on this branch's own build at `--std 85` and printed its `.out` exactly (the console's CRLF
normalized, nothing else); each refusal failed with its sidecar's code at the line its derivation names (`g85_f22_03`
COBOL0862 at 30, `g85_f22_04` COBOL0860 at 26, `g85_f38_03` COBOL1504 at 17). The two refusal-only records are SR rows
whose input says `diagnosable`; every other record has an accepted case, and every record's case names the row's own
id in its `rules`. `record_verdicts.py --edition 1985`: rows changed 173, +175 test-ref, **GAP 844 to 671**. The cases
`g85_f15_01`, `g85_f15_02` and `g85_f15_03` pin DOC-A.1-105's failed-OPEN arm at 85 and are named in its Pinned by cell.

**The refuters** overturned 19 verdicts: wave 3 twelve (11 does-not-exercise-rule, 1 convention), wave 4 seven (4
citation, 2 does-not-exercise-rule, 1 expected-value). Twenty fixes were applied (wave 4 fixed a sibling citation beside its seven); several wave 3 fixes narrowed a
header's claim (the second OPEN after a failed OPEN is a fresh OPEN, not a witness of persistence) with the `.out`
unchanged, and each fixed header and `.out` was checked on disk against its fix report. One fix was refused: the
refuter held that OPEN OUTPUT WITH NO REWIND of a disk report file must answer 00 (`1985-GR-XIII.4.5.4-9`), the fixer
held the compiler's 07 conforming under §1985:B.2.2, §1985:VII.1.3.5 and §1985:C.3. That disagreement is the
dispatcher's to decide; the row lands nothing here.

**Not closable (11).** `1985-GR-VI.6.24.4-2` (STOP RUN's implicit CLOSE shows only after the run unit ends) is filed
as kb/Work PB5765, a harness defect: a case is judged on its standard output alone. Ten wait for the decision notes
kb/Work PB5756, PB5757 and PB5758, which integrator a's branch golden-1985-1 files and which were not on main at this
landing: the unpopulatable-antecedent rows `1989-RV-A.2.11.3-2`, `-3`, `1989-RV-A.2.22.4-3` and `1989-RV-A.2.46.3-3`
(PB5756's reason); the rows that state no behavior, `1985-GR-XI.4.4.4-4` and `1985-GR-XI.4.1.4-10` (PB5757's); the rows
every program meets at WiseOwl's determinations, `1989-RV-A.2.19.4-3`, `1985-GR-VII.4.3.4-22` and
`1985-GR-XIII.4.5.4-15` (PB5758's); and the disputed `1985-GR-XIII.4.5.4-9`. No draft was held back and the writers
deferred nothing.

