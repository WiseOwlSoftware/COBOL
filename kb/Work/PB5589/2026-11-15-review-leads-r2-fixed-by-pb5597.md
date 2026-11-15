**Addendum (2026-11-15): the branch review's leads R2-1 to R2-4 are fixed at their roots by kb/Work PB5597.** R2-4 (no
pass of an audit run produced `next.answers`, so a run holding one flagged pair was refused whole): `audit_batch.py
import-reads` turns the reads, which answer the flags, into an audit run, `dispositions` carries the answers into the
change, and `change-batch` refuses each failing record alone. R2-3 (the communication-reach `confined` pattern held a JSON
backspace): the pattern is fixed and a control character in a trigger parameter is refused when the table loads. R2-2 (an
answer matched by trigger name only): `next.answers` names the flag's detail it answers, and a changed detail is
unanswered. R2-1 (a new behaviour would turn every stored record red): `record_verdicts.py` asks the screen of every change
record it writes, and a stored record's later flag is listed as owed a re-read, never red. Design: section 10.5's flag
paragraphs and section 10.6 of DESIGN-spec-conformance-review.md.
