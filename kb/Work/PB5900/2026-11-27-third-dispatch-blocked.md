**2026-11-27, a third dispatch returned BLOCKED for this note's own hold.**

Wave 20262171 group B planned PB5900, its implementer read the note's NOT-TO-BE-FIXED-NOW hold (2014 waits under the owner's order
of 2026-11-15; 2002 waits for its plan's fix-train stage) and returned BLOCKED with no commits. This is the third such dispatch
(wave 20262165 group D was the first). The mechanism is filed as the open PB6183: a hold written only in prose has no
machine-readable form, so `work.py next` and `plan_wave.py` keep planning the note. Each wasted dispatch costs one implementer start.

**Needs.** Land PB6183's hold, or drop PB5900 from the planner by hand, before the next wave. Re-dispatch PB5900 only once the owner
lifts the 2014 hold (and the 2002 plan reaches its fix-train stage). The fix then is the 2014 and 2002 sibling of
`DialectBehavior.IoStatusValuesOutside1985Lists` and `IoStatus1985.Report`, judged from each edition's own I-O status clause
(§2014:9.1.12; `cite.py --find` shows no clause for I-O status 06, 09 or 71 at 2014).

Source: `E:/COBOL-coord/scratch/reports/w20262171b-PB5900-report.md`.
