## Entry 3726 — 2026-11-15 22:54 PST — Eight notes and two finding files from the Mythos lane's batch-4 rulings of 2026-11-15, among them a compiler crash on legal source (PB5801)

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed at `--std 85` (and `--std 2023`), and the leads.

- PB5801 (defect, severity BLOCKER, crashes): a level 66 item named as FILE STATUS, LINAGE, RECORD VARYING DEPENDING ON, RELATIVE KEY or a report CONTROL ends the compiler with an unhandled `System.InvalidOperationException` ("no Place was built for data item"), at every edition, on legal source (`§1985:VI.5.11.4 GR2`). Five uses, five programs; SORT and MERGE keys, SUM operands and RECORD KEY were not probed and are leads. The 2023 rows `SR-12.4.5.8.3-2`, `SR-13.18.34.3-2`, `SR-13.18.43.3-6`, `SR-12.4.5.13.3-2` and `SR-13.18.16.3-6` are named for the registrar.
- PB5802 (defect): at `--std 85` ASSIGN TO NULL is refused (COBOL1639), though NULL is not reserved in 1985.
- PB5803 (defect): at `--std 85` pseudo-text that begins a line with `>>` is refused as a compiler directive, which 1985 does not have.
- PB5804 (defect): at `--std 85` CLASS-ID, INTERFACE-ID, FUNCTION-ID and REPOSITORY are refused as a computer-name.
- PB5805 (adjudication, open): may the integer of WRITE ADVANCING carry a written sign at `--std 85` (`+3` accepted, `-0` refused)? The text does not decide it; rule 1's order and a 1985 conformance entry settle it.
- PB5806 (defect, rated medium by the checker): at `--std 85` GROUP INDICATE is accepted on an entry with no COLUMN clause; the fix reverses half of the landed PB5243.
- PB5807 (defect, severity MAJOR, wrong answer): a group item named in a report SOURCE clause prints `047` where a MOVE of the same group gives `4X7`, at every edition.
- PB5808 (defect): a USE BEFORE REPORTING procedure may change a control data item through a REDEFINES of it, at every edition.

Two dated finding files beside existing notes: `kb/Work/PB5775/2026-11-15-two-more-rows.md` (rows `1985-SR-XIII.4.9.3-2` and `1985-SR-XIII.3.18.3-1` join the report-name homonym mechanism) and the source copied unchanged as `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-4.md`. The finding files for PB5756, PB5757 and PB5758 were skipped: those notes were not on main when this batch was filed.

**The first population is done: 504 KEPT rows read at 1985 (plus the sample's 60).** Of the 564: 50 contradicted (the sample 6, batch 1 9, batch 2 10, batch 3 11, batch 4 14), about 9 %; 4 more undecided on open readings (PB5789, PB5799, PB5800, PB5805); 4 rejected or decided as conforming.

