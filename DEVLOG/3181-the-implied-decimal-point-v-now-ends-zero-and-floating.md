## Entry 3181 — 2026-11-05 18:35 PST — The implied decimal point V now ends zero and floating suppression (PB4799)

`CobolEdit.Format` removed `V` from the mask before the Pass 2 suppression walk in `Render`, so zero suppression and
floating insertion ran past an implied decimal point: `PIC ZZVZZ` given 0.05 showed `[   5]`, `**V**` `[***5]`,
`$$V$$` `[  $5]`. Section 13.18.40.5 rules 6 a) and 7 a) end the walk at "the decimal point position", and 6 b) / 7 b)
treat a nonzero value as if suppression were "defined only to the left of the decimal point position"; the special
insertion `.` already did this. Re-probe on the build reproduced all three.

Fix: `ImpliedPointAt(picture)` returns the index in the V-less pattern where the point falls, and the walk breaks there.
The fraction digits after V now show: `[  05]`, `[**05]`, `[ $05]`. The floating landing (`FindFloatingPlacement`) needed
no change: after the walk the positions right of the point hold digits, so the suppressed run ends before them.
Zero values are unchanged (rule 7 b blanks the whole field).

Golden: `13.18.40.5-Editing-rules/pb4799_implied_point_ends_suppression` (editions 85 to 2023; V in a numeric-edited
PICTURE exists in all four, so there is no refused-below program). Sweep: the only other place that strips V is
`DeEdit`, which reads digits and does not suppress. No inventory row claimed the note, so `closes_rows` is empty with a
reason. Self-review lenses (architecture, full code, performance, duplication) found nothing further; the walk adds one
integer comparison per mask symbol.

