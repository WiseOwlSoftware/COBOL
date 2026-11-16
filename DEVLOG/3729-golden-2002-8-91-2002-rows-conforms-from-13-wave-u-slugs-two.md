## Entry 3729 — 2026-11-15 23:13 PST — golden-2002-8: 91 2002 rows CONFORMS from 13 wave-u slugs, two defects filed (GAP 1805 to 1714 on main with golden-2002-7)

The eighth landing of the 2002 golden lane (gl2002, kb/Work PB5597): the wave-u slugs g2002u-13-16-31 (LEVEL-NUMBER) through g2002u-14-8-3 (ALLOCATE), rows a 2002 read left as needs-own-witness or undecided. Each row closes on a witness that runs at `--std 2002`.

**Shape.** The 13 writer reports hold 108 rows: 87 new-golden, 4 existing-golden, 9 defect-suspected, 7 not-closable and 1 needs-own-witness. The refuters overturned 29 rows in 10 slugs, and the fixers applied all 29. No writer deferred anything.

**Landed.** 127 new cases and one widened case:
- 47 new cases accepted at [2002] and 80 refusals with `refuse-at: ["2002"]`.
- `g2src01_source_cd_compute_and_operands` gains 2002-FMT-13.16.51.1, and two of its entries now write SOURCE and SOURCES without the optional IS and ARE.

No validate stage ran, so I validated every draft on this worktree's build at `--std 2002`: run.py PASS 48 · FAIL 0, and all 80 refusals were refused with their expected codes. Mechanical repairs, with no expectation changed:
- Seven PROGRAM-ID collisions were renumbered.
- CRLF was normalised to LF.
- `g2-value-cd-initial-image` and `g2-value-ws-report-no-editing` land with the six PB5577 rows removed from their `rules`. A case that names a row counts the row as witnessed, and those six rows stay open.

`record_verdicts.py --edition 2002`: CONFORMS 91, all 91 closed the GAP; **GAP 1899 → 1808** on the base 5e49d46678, and **1805 → 1714** once main (with golden-2002-7) is merged in. docs/conformance gains these pins:
- DOC-A.1-128, DOC-A.1-197, DOC-A.1-213 and DOC-A.1-215 gain their 2002 pins.
- DOC-A.1-173 states its `--std 2002` determination (§2002:13.16.49.3 GR5), from the select-when writer.

**Defects (each repro re-run on this build before filing).**
- PB5760 (new; 2002-GR-13.16.41.3-14, -19): RESUME AT NEXT STATEMENT from an EC-SORT-MERGE-RELEASE declarative ends the SORT. The rest of the input procedure and the output procedure never run (also at 2023).
- PB5761 (new; 2002-SR-14.8.3.2-4): a pointer record typed through TYPE under an output CD, or as a second FD record, is refused COBOL0899. PB5604 missed the TYPE arm.
- PB5577 gains six VALUE rows (2002-SR-13.16.61.2-2, -4, -5, -6, -24 and 2002-GR-13.16.61.3-10), whose format 5 leg is refused COBOL1708.
- PB5578 gains 2002-GR-13.16.13.3-4: a record-less CODE-SET FD converts the binary item of a WRITE FILE ... FROM operand.
- PB4950: the VALUE literal refusals cite §2023:13.18.63.3 at 2002.
- Already filed: the SET index UP/DOWN BY lead is PB5674, and the pb1036 fatal-EC golden is in PB5612.

**Held out of the tree.**
- Every draft of a defect-suspected row: 23 value-p2 refusals and g2002-record-size-outside-range.
- The needs-own-witness draft for 2002-GR-13.16.39.3-3: items a) to f) of the PRESENT WHEN GR3 are unwitnessed.

**Not closable (7).**
- 2002-GR-14.8.1.3-1, -3 and -4: a device ACCEPT needs device data, and the harness runs every case with stdin closed.
- 2002-GR-14.6.8.2-1, -2 and -4: RETRY needs a cross-run-unit witness, so the case is withheld.
- 2002-GR-14.8.3.3-5: storage exhaustion (EC-STORAGE-NOT-AVAIL) cannot be probed on this host (host safety).


**The merge group's red, root-fixed here (PB5820).** The first merge group of pull request 1759 failed `SlotImageRoundTripTests.DataPointerOf_APointerDisplacedBeyondTheWindow_ReadsBackAsItself_NeverAsAnotherArea` on Windows shard 4/7. That test had passed at all three of this branch's local gates, and this landing touches no runtime code. The cause was a runtime defect.
- `SlotImageTable.Note` held a far pointer's `FarDisplacement` entry only by a `WeakReference`, and nothing else references that entry. Any collection between imaging a far pointer and decoding it turned the token foreign.
- A `GC.Collect()` placed between the two steps reproduces it on every run.
- The fix: `PointerImage.ICellLifeEntry` (`CellLife`, `FarDisplacement`) is held strongly while its cell life lasts and swept once the life ends. The freed-cell retention test now images a far pointer too.
- The plan section 0 GAP sentence was dropped on the operator's instruction: every 2002 landing's figure conflicted with the next.
