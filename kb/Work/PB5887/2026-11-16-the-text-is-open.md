# Supplement to my ruling on kb/Work PB5887: the 1985 REPLACE text is OPEN, and the precedence decides it

Written 2026-11-16 about 19:55 PST by the Mythos lane session (Claude Mythos 5.2); filed here, with its register
sentence in `docs/conformance/3/D-85-REPLACE-DEBUGGING-LINES.md`, by the fix round of wave 20262097 group C.

## What was wrong with the ruling as filed
The section of `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-17.md` that became kb/Work PB5887 rested on
`§1985:VI.7.3` alone ("After all COPY and REPLACE statements have been processed, a debugging line will be
considered to have all the characteristics of a comment line, if the WITH DEBUGGING MODE clause is not specified").
That sentence says what a debugging line of the RESULT is; it does not say whether a debugging line written in
pseudo-text-2 is still a debugging line in the result. kb/Work PB5382 (landed 2026-11-12) had answered that question
on purpose, the other way, and I ruled without reading its reasons. The conclusion stands; the reason is this one.

## The text admits two readings
- `§1985:XII.3.4 GR11` (REPLACE): "If additional lines are introduced into the source program as a result of the
  processing of REPLACE statements, the indicator area of the introduced lines contains the same character as the
  line on which the text being replaced begins". READING P (kb/Work PB5382's): every line a replacement brings in is
  an introduced line, so a debugging line of pseudo-text-2 whose replaced text begins on a source line becomes a
  source line and runs.
- READING Q: the sentence speaks of the ADDITIONAL lines a placement creates. `§1985:XII.2.4 GR11` (COPY) is the
  same sentence, and there it stands beside `§1985:XII.2.4 GR9`: "only those text words that are specified on
  debugging lines where the debugging line is within pseudo-text-2 appear on debugging lines in the resultant
  program". If the sentence meant what reading P says, COPY's two rules would contradict each other; so in COPY it
  cannot mean that, and the same words in REPLACE need not. REPLACE has no sentence of its own for a debugging line
  of pseudo-text-2: `§1985:XII.3.4 GR8` only permits it ("Debugging lines are permitted in pseudo-text").
No 1985 sentence chooses between P and Q for REPLACE. (`§1985:VI.7.3` was checked in the batch-17 rulings; the
four quotations of clause XII answered `python scripts/spec/cite.py --check` OK in the fix round that filed this.)

## The precedence (CLAUDE.md rule 1): GnuCOBOL answers Q
MEASURED by this session, GnuCOBOL 3.2.0 in WSL (`E:/COBOL-coord/scratch/my-20261115/b17-check/gc-replace/`):
`rep_dbg_ctl.cob` compiled `-std=cobol85 -fixed` prints `END` alone; compiled with `-fdebugging-line` it prints
`DIRECT-DLINE`, `VIA-REPLACE`, `END`. So GnuCOBOL keeps a debugging line of pseudo-text-2 a debugging line.

## So
1. The fix of wave 20262097 group C is right in what it does (one rule for COPY and REPLACE at 1985; the flag
   `KeepsPseudoTextDebuggingLines` deleted; `rep_dbg_ctl` prints `END`; the case
   `pb5382-replace-debugging-introduced-lines` now expects 1, ONE, THREE).
2. It owes, in its fix round: a determination in `docs/conformance/` section 3 (key `D-85-REPLACE-DEBUGGING-LINES`
   or the name the section's form gives) that states the two readings, that the text does not choose, GnuCOBOL's
   measured answer, and the behaviour; the case's comment and the row `1985-GR-XII.3.4-11`'s notes say the same and
   name the determination; this file as a dated file beside kb/Work PB5887 and a two-line dated file beside kb/Work
   PB5382 (its reading of GR11 for REPLACE at 1985 is replaced by a recorded determination, and why).
3. What GR11 still decides, unchanged: a replacement whose replaced text begins ON a debugging line puts every line
   it introduces on a debugging line (the first half of the case: `XSTMT`), and a literal that would continue onto
   a debugging line is an error.
