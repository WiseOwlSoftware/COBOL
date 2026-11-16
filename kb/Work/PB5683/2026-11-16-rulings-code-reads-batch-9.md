# Rulings on the non-agreeing rows of the 1985 code reads, batch 9, the last (the Mythos lane, Monday 2026-11-16 01:50 PST)

**The batch.** Slices 1985-165 to 1985-185 of `E:/COBOL-coord/scratch/coderead-1985/`, the last 122 KEPT rows of
the second population, read on the code at `--std 85` (main 1df234fa72) by the Mythos lane's five runs of the
operator's workflow (wf_9e4a47c2-904, wf_a0667955-e08, wf_fe81610f-ba8, wf_1ae9910c-f18, wf_25136791-3e6). 107 agree
with the inherited verdict. Fifteen do not (`coderead-1985/non-agreeing-165-185.json`). With this batch every one
of the population's 1,106 rows has been read and refuted.

**How each was checked.** An independent checker (`my-20261115/BRIEF-contradictions-1985.md`) ran its own
fixed-form programs at `--std 85` and `--std 2023` on a compiler built at main d7d3f99539:
`my-20261115/contradictions-b9/rulings.json`, programs under `contradictions-b9/src/`. I ran two myself at `--std 85`
with the tracked probe runner. `intarg.cob` printed `FACT(WN*0.5) 00000001` and `MOD(A/B,3) 00000000` with no
diagnostic (WN is 3, A is 7, B is 2). `relbin.cob` printed `S2 SQRT(4) EQ 2.00000000000000001`.

**Ruling: six mechanisms. Three UPHELD as contradictions (two new notes, one existing), two rows REJECTED, one
row confirmed with a defect beside it, one NOT DECIDED.**
Cause (a) = wrong at 85 only; cause (b) = wrong the same way at 2023.

| # | 1985 rows | Ruling | Harm | What happens | Note |
|---|---|---|---|---|---|
| 1 | `1989-AR-A.2.14.3-1`, `1989-AR-A.2.28.3-1`, `1989-AR-A.2.35.3-1`, `1989-AR-A.2.9.3-1` | UPHELD (a) | under_rejects | At 85 an integer argument that is a product (`WN * 0.5`), a quotient of two items inside a larger argument list (`A / B`), or an expression holding a numeric function (`FUNCTION SQRT (N) + 64`) compiles and is silently truncated: FACTORIAL, MOD, RANDOM, CHAR. A quotient written alone is refused. See below. I ran it. | PB5832 |
| 2 | `1989-FMT-A.2.12.2`, `1989-FMT-A.2.13.2` | REJECTED | none for these rows | The general formats of DATE-OF-INTEGER and DAY-OF-INTEGER are right. The integer requirement is their argument rules', which mechanism 1 covers. | (none) |
| 3 | `1989-AR-A.2.23.3-1`, `1989-AR-A.2.24.3-1`, `1989-AR-A.2.25.3-1`, `1989-AR-A.2.26.3-1`, `1989-AR-A.2.27.3-1` | UPHELD (b) | rejects_legal_source, under_rejects, wrong_answer, crashes | A reference-modified element under an ALL subscript (`N-EL (ALL) (1:1)`) is class alphanumeric by the 1985 text, and the argument-class screen classes it as its numeric element. MAX and MIN refuse legal lists; MEAN, MEDIAN and MIDRANGE compute from digit slices; an illegal MAX mix compiles and fails at run time. Not the mechanism of PB5780 or PB5813. | PB5833 |
| 4 | `1985-SR-XIII.3.7.3-1`, `1985-SR-XIII.4.3.3-1` | UPHELD (b) | under_rejects | The mechanism of kb/Work PB5775: report names are resolved apart from data names, so an unqualified CONTROL name that a Report Section item also spells binds silently to WORKING-STORAGE, and `GENERATE DTL2` compiles when DTL2 names both a detail group and a data item. | joins PB5775 |
| 5 | `1989-FMT-A.2.34.2` | NOT DECIDED | under_rejects under one reading | `PRESENT-VALUE (T (ALL))` supplies argument-1 and the whole argument-2 series from one table. The text admits two readings of where ALL may stand. GnuCOBOL was not measured (it is not on this host). | PB5834 |
| 6 | `1989-RV-A.2.8.4-1` | The ROW is confirmed; a DEFECT outside it is UPHELD (a) | wrong_answer | ATAN's value is inside its range. What fails is the relation condition: at 85 a comparison of a numeric function with an 18-digit literal is made in binary floating point, so `IF FUNCTION SQRT (4) = 2.00000000000000001` is TRUE. The 1985 rule for comparing numeric operands has no row in the 1985 catalog. I ran it. | PB5835 |

**Mechanism 1, ruled, with three more rows.** `§1989:A.2.2 AR4` requires of an integer argument "An arithmetic
expression which will always result in an integer value" (cite.py OK), and `§1989:A.2.3 GR2` says "A numeric
function may not be referenced where an integer operand is required" (cite.py OK). The landed kb/Work PB1526 left
this screen open by design because a run-time check (EC-ARGUMENT-FUNCTION) catches a non-integer value; that check
exists only from 2002, and the checker's `intec23.cob` shows it firing at 2023. At 85 nothing catches it. So the
2023 verdicts stand and 1985 lacks its own arm, as kb/Work PB5208 already ruled for the power shape of the same
screen. The checker also found the stored agreement on three rows of earlier slices wrong on the same mechanism:
`1989-AR-A.2.12.3-1`, `1989-AR-A.2.13.3-1` and `1989-AR-A.2.2-4`. I rule them contradicted with the four above:
their reader and refuter agreed without trying a product or a function term.

**Mechanism 6, ruled.** The row `1989-RV-A.2.8.4-1` states ATAN's range, and the value is inside it
(`ATAN (HUGE) + 0 < 1.57079632679489662` is true). The fault is in the relation condition, which at 85 applies the
2023 rule for native arithmetic with no edition arm. It is a defect at 85 and is filed. It contradicts no
inventory row: the checker found no row for `§1985:VI.6.3.1.1.1` in the 1985 catalog, and the note carries that as
a catalog gap, with the two sentences of kb/Work PB5826 that have none either.

**What follows.**
- Rows of mechanisms 1 (seven rows with the three more), 3 and 4 do not close on their inherited verdict. The rows
  of mechanisms 2 and 6 are confirmed. The row of mechanism 5 waits.
- The 2023 rows of mechanisms 3 and 4 are re-recorded non-conforming by a registrar (the checker's `rulings.json`
  names them: five argument rules of the 2023 functions, and `SR-13.18.16.3-2` and `SR-14.9.16.3-1`).
- THE POPULATION IS READ. Counted from the rulings files by script
  (`my-20261115/store-import/rulings-map.json`, batches 1 to 8: 69 rows contradicted, 12 confirmed against the
  refuter, 9 not decided, 1 confirmed with a defect beside it) and this batch (14 contradicted with the three more
  rows, 2 confirmed against the refuter, 1 not decided, 1 confirmed with a defect beside it): 83 of 1,106 rows
  contradicted, 7.5 %. The sample of 60 rows before it had 5 contradicted and 1 not decided. My running tally in
  the earlier rulings files was two rows high (it read 76 after batch 8 where the tables count 69 and the sample
  5): the tables are the record.
