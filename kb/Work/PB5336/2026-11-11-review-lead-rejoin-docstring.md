# 2026-11-11 - review lead R1-1: `rejoin_lines` states a condition it does not test

Branch review w20262003a-review-r1 (fix-pb5332-2002-repairs at d55ee199ba, landed as #1543; findings
E:\COBOL-coord\scratch\review-w20262003-a.jsonl) recorded a low lead in the same generator this note fixes:
`scripts/spec/render_figure.py` `rejoin_lines` (line 329 at the landing) says a word moves only to a neighbouring row
"that holds only words no enclosure holds", and the code never tests that condition. No figure in any edition is
affected today. The fix that lands this note either adds the test (with a self-test case that fails without it) or drops
the clause from the docstring, whichever matches what the generator must do.
