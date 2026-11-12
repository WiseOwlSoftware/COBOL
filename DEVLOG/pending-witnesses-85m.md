## PB4995: first witnesses for 19 1985 rules that never had a draft (13 placed, 6 mismatches on 5 notes)

The locator's category F named 20 open 1985 rows that had no draft in any evidence. One of them, `1985-GR-XII.2.4-12`, got
a test-ref this morning from witnesses-85g, so I dropped it. For each of the other 19, I wrote a draft from the rule's 1985
text alone: 11 output programs, 7 refusals (a violating line and a legal control) and 1 acceptance of a form 1985 allows
and 2023 refuses. `cite.py --check` passed on all 52 citations.

`run_drafts.py`, in a drafts pass of its own (`E:/COBOL-coord/scratch/draft-85m/state`), judged the drafts on origin/main
09ce7dc601. The first run had 12 MATCH and 7 MISMATCH. One mismatch was a draft error: the draft named a file `RF`, which
is a reserved word. I renamed the file and left the expectation unchanged. The second run had 13 MATCH and 6 MISMATCH.
`place_witnesses.py` placed the 13 matches (11 output tests and 2 refusals, COBOL1746 and COBOL0900).
`record_verdicts.py --edition 1985` recorded 13 witness-only test-refs. The 1985 GAP stayed at 321.

The six mismatches are compiler suspects, and each draft held when I checked it against the text again. I appended the
repros to PB5115 (BLOCK CONTAINS 4 TO 4 refused at 85), PB5116 (TYPE CH with no operand accepted), PB5118 (PIC V(1)
accepted) and PB5108 (MOVE FUNCTION MAX accepted). I filed two new notes. PB5445 covers a RECORD KEY or ALTERNATE RECORD
KEY that names a group containing an ODO table: it compiles at 85, and the 1985 rule refuses it. PB5446 covers `cite.py`
suggesting a designator its own parser refuses. The details are in `kb/Work/PB4995/2026-11-12-drafts-85m.md`.
