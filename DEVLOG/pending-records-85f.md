## PB4996: the 1985 re-read 2 merged; 72 verdicts recorded (1985 GAP 443 to 381) and 13 defect notes filed

The re-read wave read 92 1985 rules in 11 slices (`E:/COBOL-coord/scratch/coderead-85/out-reread2/rr-01..11.json`) and ran one refuter case for each CONFORMS. Readers and probes used the pinned tree at origin/main 8147f008b3, whose `src/` matches this branch's base. `coderead-85/merge-pipe-3.py` is `merge-pipe-2.py` with the directory and slice-name prefix as arguments. Its merge found 82 closing verdicts: 62 upheld, 10 overturned and 10 doubtful. The other 10 verdicts are non-conforming.

`record_verdicts.py --edition 1985` recorded the 62 upheld CONFORMS ("rows changed : 62", "GAP : 443 → 381") and then the 10 non-conforming reads, each under an open note ("rows changed : 10", "PARTIAL 10", "GAP : 381 → 381"). PB5242 holds four of the 10 rows; the other six fall under new notes. The 1985 store now has 670 adjudicated rows.

The 20 overturned and doubtful rows are recorded as nothing. They are held for decision in `coderead-85/held-7.json`, because `held-5.json` and `held-6.json` already hold earlier waves. Every non-conforming root's repro was rerun on the pinned compiler (`coderead-85/reprobe-85f/`), and all reproduce. Each new root got one note, PB5375 to PB5387. Nine carry the wrong-answer flag, because a conforming program prints a wrong value or status:
- PB5375: INSPECT of a P-scaled item.
- PB5376: DISABLE or ENABLE with a destination named twice.
- PB5377: RECEIVE INTO a group with its own ODO object.
- PB5378: the SEND size measure.
- PB5380: an iterative PERFORM re-entering an independent segment in its initial state.
- PB5381: the terminal lock surviving CANCEL.
- PB5382: lines introduced on a debugging line.
- PB5385: the debug register of a late-subscripted receiver.
- PB5386: RETURN INTO subscript timing.

PB5202, PB5242 and PB5188 get addenda. The table, the held questions and the leads not filed are in `kb/Work/PB4996/2026-11-12-reread-2.md`.
