## The enqueue unit's lead sweep: 43 uncollected reports read, 16 mechanisms filed as PB6060-PB6076, 3 findings

The landing lane queued 61 branches; 43 of them had a report. The enqueue unit's lead clerk read each report's "New defects" section
and its review `LEAD` lines (the 2026-11-22 sweep), grouped them by mechanism, and checked each against kb/Work and against the code
on main before filing.

Result: about 100 leads in 43 reports. Filed as new notes: PB6060 (no CLI top-level guard), PB6061 (hook Rule 7 leaders), PB6062
(run_probe false timeout), PB6063 (code_reads 1989 anchor), PB6064 (reformat position guess), PB6065 (lineage/verdict writers
ignore row state), PB6066 (inventory code-location checks), PB6067 (generator free-form refuse-at, 2002 widening, test-ref
headers), PB6068 (COBOL7081 cascade guard), PB6069 (MOD and FACTORIAL under STANDARD arithmetic), PB6070 (PB5785 record-extent
leads), PB6071 (OoEmitter group actual), PB6072 (cross-edition citations), PB6073 (operand-class leads), PB6074 (unwitnessed changes
and stale prose), PB6075 (report-writer readings), PB6076 (two flakes). Findings beside existing notes: PB5977 (third unreadable
semgrep file), PB5697 (threshold recipe reads the wrong field), PB5770 (skip-scan lock state). Already covered, no filing: the
semgrep failure (PB5977, PB5502), the Linux push self-test (PB5390), the tooling-gate hang and sibling timeouts (PB5828), the
ArchOracle flake (PB5862), the nightly job-limits false RED (PB5841), the LINKAGE clause-operand reference (PB5876), the infix-word
lists (PB5793), the 1985 implementor-name overlap (PB5884), the OpenedModeView comment (fixed on main), the free-form-at-85
cases (PB6033, PB6037). Ids PB6077-PB6079 were left unused. No code changed.
