## PB5337: GENERATE report-name of a one-detail report runs that detail's USE BEFORE REPORTING procedure

kb/Work PB5337 (wave 20262020, group F). §1985:XIII.3.20.4 GR11 processes a GENERATE report-name of a report with exactly one detail "as though a GENERATE data-name statement were being executed": a) the subtotalling, b) the detail's USE BEFORE REPORTING procedure, d) nothing more. PB5249 had given the summary form its subtotalling; the procedure still never ran.

**Re-probe.** On the base build (45199b9c36) a one-detail report with `USE BEFORE REPORTING DE-1` printed no USE line for `GENERATE R1` at `--std 85` and `--std 2023`; GENERATE DE-1 ran it.

**Fix.** `CobolReport.GenerateCore`'s summary arm processes the sole detail through the one group prologue, `BeginGroup(detail, print: false)`: the group's own sum additions, its declarative, its end-of-group reset, and no presentation. One prologue serves the printed and the unprinted processing, so a later step of group processing reaches both forms. The GROUP INDICATE occasion stays owed, since no presentation happened and the report-name form is not "a GENERATE ... issued for the current detail group".

**Edition.** The note proposed a dialect behavior. The 2023 text gives the same answer (§2023:14.9.16.4 GR2's summary reporting is the GENERATE data-name processing without the printing; §2023:14.9.49.4 GR9 runs the procedure on each occasion the group is processed), the 1985 inventory record says "Same processing", and GnuCOBOL runs the procedure too. So every edition behaves this way, and the choice is recorded in docs/CONFORMANCE.md beside PB5249's.

**Goldens.** `14.9.49-USE/pb5337_generate_report_name_runs_detail_use` (all four editions: the procedure sees the subtotal, its SUPPRESS does not carry over, LINE-COUNTER stays 0), `.../pb5337_generate_report_name_several_details` (2002 on: no detail's procedure runs) and `13.18.54-SUM/pb5337_generate_report_name_sums_detail_entry` (2002 on: the processed detail's entry adds into a footing's SUM counter, which the summary form used to skip as well).

**Rows.** None re-verdicted: the 2023 rows stay CONFORMS and the inventory has no 1985 rows yet (PB5094).
