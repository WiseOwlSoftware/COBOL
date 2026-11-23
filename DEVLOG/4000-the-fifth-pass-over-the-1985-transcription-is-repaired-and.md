## Entry 4000 — 2026-11-23 11:39 PST — The fifth pass over the 1985 transcription is repaired, and two box-marking faults are now linted (PB6051)

The fifth independent pass (run wf_10fdbb6c-b56) left 16 claims, 4 confirmed, all applied after reading each page rendered.
Page 532's FD RECORD clause set `[DEPENDING ON data-name-1]` as a fourth brace alternative instead of the IS VARYING
alternative's continuation; the sweep of every brace and bracket found the same fault in the SD's RECORD clause (559), the
SUM clause's `[RESET ON]` (210, 632) and the RECORD clause's Format 2 (404), and their figure verdicts now hold the printed
form. Page 240's two figures inside the box around 3.4 gained their box sides, and so did page 387's RECORD DELIMITER format.
Page 419 had a box edge read as an underline. Page 32's Qualification rows were a column off; a measurement of every
summary-of-elements row against the scan found Amendment 1's intrinsic function summary (835, and its copy on 59) flat too.
`heading_levels.check_box_markup` now refuses an underline that crosses a box side and a figure between boxed lines with no
box sides. Two KEPT rows owe a code read (1985-FMT-VII.2.8.2, 1985-GR-VII.4.4.4-8). PB6051 stays open; it closes on mechanical checks and a PDF text diff, not another full pass (kb/Work PB6135).

