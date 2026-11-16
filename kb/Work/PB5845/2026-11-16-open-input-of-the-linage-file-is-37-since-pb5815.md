The golden `conformance-suite/cases/VII.4.3@1985-OPEN-STATEMENT/pb5845_linage_open_refused_85` changed in two of its lines, OPEN-5 and CLOSE-3.
Its OPEN-5 (OPEN INPUT of the LINAGE file F1) now expects 37 where it said 00, and its CLOSE-3 expects 42 where it said 00; each is preceded by its USE-1 line.
The change was made on branch worktree-wf_df842f2c-278-1 (wave 20262068 group A) on the operator's decision of 2026-11-16 12:35.

Why: pull request 1811, which carries kb/Work PB5815, was bounced by the merge group (run 43945694880).
The one red case was this one, on Linux shard 3/5 and Windows shard 2/7.
It passed on main alone and on the branch alone; it failed only on the tree that holds both landings.
PB5815 was ruled a wrong answer in every edition: a file written with ADVANCING or LINAGE must not open INPUT or I-O with 00.
Whether such a file supports read operations is the implementor's determination, `docs/conformance/7/DOC-A.1-146.md` (c'): it does not.
§1985:VII.1.3.5 GRL2.3 makes that 37: "The INPUT phrase is specified but the file will not support read operations" (cite.py --check OK).
The two notes do not compete in the order of the OPEN checks, because they ask about different modes.
This note's LINAGE value check (90) is made only for OPEN OUTPUT: §1985:VII.3.7.4 GR6 reads the values "at the time the file is opened by the execution of an OPEN statement with the OUTPUT phrase" (cite.py --check OK).
PB5815's read-capability check (37) is made only for INPUT and I-O.
The 00 lines recorded main's behaviour before PB5815; the 1985 text requires neither 00 nor 37 there.
37 still shows what the line is for: the LINAGE values are not checked at OPEN INPUT, because the answer is not 90.
After the refused OPEN the file is not open, so its CLOSE is 42 (§1985:VII.1.3.5 GRL2.4 b).
The case header says so in one added paragraph.
No inventory record or lineage cites the old 00 lines.
