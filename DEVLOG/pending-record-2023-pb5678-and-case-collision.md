## The 2023 MOVE GR1 row recorded DIVERGES for PB5678 (2023 GAP 0 → 1, a corrected record and no compiler regression); PB5694, one case directory and the CASE COLLISIONS audit

**The 2023 GAP now reads 1, and the compiler did not regress.** kb/Work PB5678, filed earlier today, reports that a MOVE
of an alphanumeric-edited or national-edited sender to two or more receivers edits the sender a second time. The 2023
row of the same rule, `GR-14.9.25.4-1` (§2023:14.9.25.4 GR1, checked with `cite.py --check`: `OK §2023:14.9.25.4 GR1
(General rules) the text is in GR1`), was recorded CONFORMS. On a Debug build of main d615406082, the repro
(`E:/COBOL-coord/scratch/gl2002/integrate-2/repro/g2mvd2.cob`) printed this at `--std 2023`:

- `2 [A    ][A    ]`, where the rule requires `[A B  ][A B  ]`
- `3 [A//  ][A//  ]`, where it requires `[A/B  ][A/B  ]`
- `4 [C    ][C    ]`, where it requires `[C D  ][C D  ]`

The row was re-recorded DIVERGES naming PB5678 (`record_verdicts.py`: `GR-14.9.25.4-1: CONFORMS → DIVERGES`, `GAP : 0 →
1`). Its witnesses and code sites stay, and its notes keep the earlier prose as the history of the sentences that do
conform. PB5678's `inventory_rows` gains the row, and PB394's `closes_rows` loses it, with a reason, because `work.py
check` refuses a closed-row claim on a GAP row. The one 1985 sibling, `1985-GR-VI.6.19.4-2`, is a KEPT lineage record
that chains to `GR-14.9.25.4-1`, so it was not hand-edited. The 1985 view follows the 2023 row, and its GAP went from 32
to 33. The alphanumeric lines of the repro are wrong at `--std 85` as well. Both rows close when PB5678's fix lands. The
finding is recorded in `kb/Work/PB5678/2026-11-15-the-2023-row.md`.

**PB5694: two case directories that differed only in letter case.** Main held
`conformance-suite/cases/8.4.2.2@2002-FUNCTION-IDENTIFIER` (six files) and `8.4.2.2@2002-Function-identifier` (one
file, pb5640's). Windows treats them as one directory and Linux as two. The PB5640 review round and the golden-2002-2
lane each created the directory today with its own casing. The pb5640 case moved into the upper-case directory, the
form 84 of the 88 `@2002-` directories use, through a temporary name. The two references in kb/Work PB5669 now use that
spelling, and `git grep` finds no other use of the old one.

The new audit `scripts/case_collisions.py --check` is `CASE COLLISIONS` in `gate_audits.AUDITS`, so every lane runs
it. It is also a step of CI's `audits` job, which reads the merged tree where such pairs first meet, and a line of
`battery.sh`. It refuses any two tracked names, files or directories, that the simple one-to-one case mapping makes
equal, and it has no allow-list. Its self-test plants both spellings in a scratch index and must be RED. On
d615406082's tree, the rule finds exactly this one pair among 41,590 paths. The audit is documented in
DESIGN-test-build-ci.md, beside the conflict-marker sweep.
