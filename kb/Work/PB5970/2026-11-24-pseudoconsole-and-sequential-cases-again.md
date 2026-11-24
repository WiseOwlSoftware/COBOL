# PB5970 addendum (2026-11-24): two more gate runs timed out under load and passed alone

**Sources:** `E:/COBOL-coord/scratch/reports/w20262163b-PB5701-report.md` (New defects / leads) and
`E:/COBOL-coord/scratch/reports/w20262163h-PB6126-report.md` (New defects, Lead).

- PB5701's run: NIST/Conformance harness timeouts under load on `OBSQ4A`, `OBSQ5A`, `SQ214A` and
  `pb3350_open_reversed_varying_span_85`, recorded as non-observations in the environment, not compiler results. Three are the
  sequential-file family the earlier addendum (`2026-11-24-five-sequential-nist-cases-and-analyzer-once.md`) measured; the
  `pb3350_open_reversed_varying_span_85` Conformance case is not NIST and is new to this note.
- PB6126's run (gate 000002): `WindowsConsoleDeviceTests.The_cobol_commands_listing_on_the_console_shows_its_characters_as_themselves(html)`
  timed out at 120000 ms (a pseudoconsole child process) under load and passed on the re-gate. The report places it in the area of PB3294.

**What this adds.** The timeouts are not confined to sequential-file NIST cases: a pseudoconsole-child test and a Conformance case with
a reversed OPEN are in the same class (a harness wait under concurrent gates that no CPU measure explains). The root-cause step the
earlier addendum named (a dump of a hung process before the kill) applies to these two as well. Not probed in this filing.
