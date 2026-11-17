# Rulings on the re-reads of slices 1985-248 and 1985-249 (batch 21)

Written 2026-11-17 by the Mythos lane session (Claude Opus 5.5, main thread). Read pass 20: run wf_00d2a9a2-682 of
`coderead-1985/coderead-sample.js` on a compiler built at main d77571151b (`E:/COBOL-wt/coderead85-tree-d`). Rows: the
four rows kb/Work PB5891 fixed (CHAR, FACTORIAL, MOD, RANDOM) and the four MEAN and MEDIAN rows whose code moved to
`Binding/Bound/FunctionValueLane.cs`. Reader: 8 CONFORMS. Refuter: 6 UPHELD, 2 DOUBTFUL (`1989-RV-A.2.24.4-1`,
`1989-RV-A.2.24.4-2`, MEAN). Every citation below answered `python scripts/spec/cite.py --check` OK in this session.

## 1989-RV-A.2.24.4-1 and -2 (MEAN): the doubt is REJECTED; both rows CONFORM

The refuter's doubt: `FUNCTION MEAN (-4.4 5.5 6.6 1.1 FUNCTION SQRT (4))` gives 2.1599, not 2.16. Once any argument is
approximate, MeanExact's binary64 body evaluates the whole mean in binary64. The same happens at 2023. The refuter
says it conforms only if the implementor's latitude covers it.

Ruled: it does. `§1985:IV.4.3.4` says "Each implementor will indicate the techniques used in evaluating integer and
numeric functions", and the representation of a numeric function is the implementor's. The technique is indicated
in the register entry `docs/conformance/7/DOC-A.1-92b.md`: when any argument is floating-point or an approximation,
the function's equivalent expression is evaluated in IEEE binary64, and the receiver takes the result by the
ordinary rules. CONFORMS. The entry names only its 2023 basis (`§2023:15.4.1`). It is to gain one sentence saying the
same determination is this implementation's indication under `§1985:IV.4.3.4` at `--std 85`.

## Defects found on the way (filed by records branch 6)
- kb/Work PB5892 (rejects legal source, every edition from 2014 on, where CONCAT is in the edition):
  `IntrinsicBinder` (about line 2193) refuses an unsigned P-scaled integer item in FUNCTION CONCAT
  (`nSigned || nScale != 0`) as not an integer. `FUNCTION CONCAT ("A" U)` over `U PIC 99P` is refused COBOL1627 at
  2023. Probe: `E:/COBOL-coord/scratch/my-20261115/w20262099/b/concat_p.cob`, run `w20262099/b/runs/concat_p-001` (the PB5891 implementer's).
  The test asks the spelling (scale 0), not the value (kb/Work PB272). The one question is
  `NumericLanes.IsIntegerByValue` (kb/Work PB5891).
- kb/Work PB5893 (wrong answer, every edition): an argument at a NUMERIC (not integer) position of a function, under a
  floating-point (COMP-1 or COMP-2) receiver, is evaluated in binary64 although every operand is fixed-point.
  `COMPUTE F = FUNCTION REM ((Y * 100 - X * 10.25) * 4, 1000)` with F COMP-2 stores 672 where a fixed-point
  receiver stores 41 (the PB5891 round-6 reviewer's lead R6-2: X PIC 9(31) VALUE 9999999999999999999999999999999,
  Y PIC 9(31) VALUE 2050000000000000000000000000000; program
  `E:/COBOL-coord/scratch/my-20261115/w20262097/review-w20262097-q/n2.cob` lines 9 and 10, runs `runs-br/n2-002` and
  `runs-main/n2-002` beside it: 672 on main and on the branch). DOC-A.1-92b's determination is about a
  floating-point ARGUMENT. Nothing in it makes the RECEIVER's usage the evaluation technique of a fixed-point
  argument. kb/Work PB5891 fixed this for integer positions (`IntrinsicRenderer.ReceiverFor`), and this is the
  sibling for the rest. The PB5891 implementer's lead (an argument classed integer only for a function's result type,
  such as ABS or MAX, is likewise evaluated in binary64 under a floating-point receiver) is the same defect.
- kb/Work PB5889 narrows (its owner's frontmatter): PB5891 admits every identity form whose terms cancel, so what is
  left is the forms that are integers only by a function's meaning (`S2 - FUNCTION MAX (S2)`,
  `S2 - FUNCTION ABS (S2)`, `FUNCTION MEAN (N, N) + 1`).

## Leads that are not yet anything
- The reader of slice 248: refusals at `--std 85` cite `§2023` clauses in their messages (the third slice to see it;
  nobody has listed them).
- The reader of slice 248: a negative or zero argument where 1985 makes the result undefined compiles. Nobody has
  ruled whether a literal outside the domain is refused at compile time (the slice-247 lead).
