## Entry 3041 — 2026-11-03 12:16 PST — Three GnuCOBOL reports filed (bugs #1286, #1287, #1288) and the 1985 CALL vendor spellings decided (PB4503)

The owner reviewed and posted the three upstream reports that the differential adjudication of PB4346 found: bugs #1286
(PB4500, `-Wprototypes` gated on `-Winitial-value` in `tree.c`, with a patch against 3.3-dev r5751 and a new testsuite
case; make check 1373 behaved as expected, 0 failed), #1287 (PB4499, OMITTED in a CALL with no prototype under the ISO
dialects) and #1288 (PB4501, `-std=cobol85` accepts a procedure division or section that does not begin with a
paragraph-name). The duplicate search used the PB3838 tier-6 ticket cache, now kept at
`E:/COBOL-coord/scratch/sf-tickets` with the 27 tickets fetched since; each note's upstream state and posted text are
recorded.

The PB4499 research on COBOL-85 (IBM lists OMITTED, BY VALUE and RETURNING in CALL among its extensions over ISO
1989:1985; HP documents OMITTED and BY VALUE as HP extensions) showed two faults in WiseOwl's own handling at
`--std 85`, filed as PB4503 with the owner's decision that `--permissive` accepts the vendor spellings at 85 too.

