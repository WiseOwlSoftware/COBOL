## Filed the PB1528 report's review leads as PB4167 and a third PB4070 witness

The lead-filing clerk (units/enqueue.md step 5) filed the review leads of the PB1528 report and one more sighting of an open defect. No code changed.

- PB4167 (defect, TRIVIAL, process only): the PB1528 branch (PR #851) deletes the grammar rule `screenTail`, yet the `pb260-accept-screen-at-phrase.cob` witness header and the `FMT-14.9.11.2` inventory row's notes still describe it. From the PB1528 report, R1-1 and R1-2, one mechanism, one note.
- PB4070 extended: a third witness, conflict finisher run 000010 (`wf_f1d1c695-17b-15`), held alive about 90 minutes after `FINISHER RESULT: DONE`, 11 result lines, about $6.2. It carries the finisher's own diagnosis from the PB3845 report (lead 3) and its extra point: sweep `land_lane.py` and every other headless launcher for the same missing `COBOL_LOOP_UNIT` export.
