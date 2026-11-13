## Golden lane 85p: the five warning-only 1985 declaratives rows get accepted witnesses; their warning pins wait (PB5461)

Five 1985 rows forbid a declarative procedure to reference a nondeclarative procedure. Four are the USE statement's GR2
in Sequential, Relative and Indexed I-O and in Report Writer; the fifth is the Debug module's §1985:XV.3.2.3 SR2. WiseOwl
accepts a violation of any of them with warning COBOL2375, and the PB4995 decision of 2026-11-12 found that correct:
under §1985:I.1.6 the results of violating a rule are undefined unless the standard says otherwise, and none of the
five says otherwise. What was missing was each row's witness.

**Placed.** I added four accepted 85 tests, one per USE GR2. Each USE procedure PERFORMs a declarative paragraph and
GO TOs the section's last paragraph. The I-O tests trigger the procedure with a READ at end (status 10); the Report
Writer test uses OPEN EXTEND of a missing file (status 35), and the procedure ends the run, because after a critical
error the implementor decides what happens. I derived each expected output from the 1985 text first, checked every
citation with `cite.py --check`, and only then ran the programs; all four matched. The SR row needed no new test: the
existing `1985-GR-XV_3_2_4-1` already PERFORMs a declarative from its debugging section and names the nondeclarative
MAIN-PARA only in USE FOR DEBUGGING, so it took the rule into its `rules`. I recorded the five test-refs with no verdict
(GAP 311 → 311). D-DECLREF in `docs/CONFORMANCE.md` now states the 1985 rules and their basis.

**What could not be placed.** The decision called for a `syntax` flagged line for the SR row and a WiseOwl-layer pin of
COBOL2375 for the four GR rows. On main, the generator refuses any flagged test that has no `refuse-at` refusal, and the
`wiseowl/diagnostics` layer holds refusals only. So the suite cannot yet pin a warning on a program WiseOwl accepts. A
probe flagged twin was refused by `generate.py --check`, and I removed it. I did not bend the format or pin a refusal.
kb/Work PB5461 records the gap, the reader that enforces it, and the five pins to place once the design's S6 forms
(`.flagged.jsonl` and the severity-bearing WiseOwl layer) land.
