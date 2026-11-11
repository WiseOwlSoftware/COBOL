## Entry 3472 — 2026-11-11 05:27 PST — PB5149 is discharged by the user-word census, and PB5147's stall gets a probe (PB5073 review R2-1)

Wave 20261998 group A finished the PB5073 branch (the declaration name-uniqueness screens, COBOL6862 and COBOL6863, landed in its own
entry) and took its two leads.

**Merge.** The unlanded PB5073 branch was merged into main: two conflicts, both additions on both sides (the diagnostic catalog
and the generated `diagnosticCodes` list of `tests/doc-facts/compiler-facts.json`), both kept.

**PB5073 review R2-1 and PB5147 (CM102M).** The review's measure was right: CM102M's `DELAY-FOR-30-SECS` is a CPU spin, so
a runner kill at 120 s with 31.5 s of CPU means one delay was done and about 88 s were spent blocked, which the 210 s budget
(PB5154) does not explain. Measured on this host with a temporary stopwatch: a run makes 544 durable device flushes (the
`McsStore.Transmit` flush each SEND pays) and 7 store syncs, 2.2 ms per flush, 1.2 s in all. At about 160 ms per flush
(a cloud volume's `FlushFileBuffers`) 544 flushes are the missing 88 s; that fits but is not shown. So the harness now
measures it: a timed-out observation carries `durable flush on the run volume: median N ms, worst M ms of 5 probes`
(`HostLoad.DurableFlushLine`, taken in the run's own directory before the kill). The next red confirms or rules out the
flush. The flush itself is the designed power-loss contract of the MCS device, so it was not changed.

**PB5149.** The note supposed a report-name or screen-name beside a data-name escaped the GLOBAL screen. The user-word census
(§2023:8.3.2.2) already refuses a report-name and a data-name of one spelling at every edition (COBOL2692); the SCREEN
SECTION is refused outright (COBOL1560); and a constant-name is not a data item (§2023:3.53; §2023:8.4.2.1 reaches only a
data item), which §2023:8.3.2.2 exception 3 lets share a data-name's spelling. Discharged, with the report-name arm pinned by
`pb5149-report-name-and-global-data-name` (refused at 85, 2002, 2014, 2023). Lead for the registrar: a reference to a name that
a constant and a data item share compiles and takes the constant, where §2023:8.4.2.2.1 asks for a unique reference.

**Review round.** PB5147 stays OPEN: the probe is a step, the runner stall's cause is still owed. PB5154 gained a
correction addendum (its budget states CM102M's own wait; it did not explain the kills), and the design doc's observer row
says the same. The probe times the directory the child really runs in (`HostLoad.RunDirectory`). The constant/data-item
reference lead is filed as PB5301.

**Rows.** None claimed, none closed (`closes_rows: []` on both).

**Self-review.** Architecture: the probe lives in the one observer's host-load instrument, no second mechanism. Full code: the
probe only describes a non-observation and never fails one (IO and permission errors say "not probed"); its file is
`DeleteOnClose`. Performance: five small flushes, taken only on a timeout. Duplication: none. Semgrep `verify.py` PASS, no count up.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 23,997/23,997 ran · Unit 40,755/40,755 ran · Characterization 37/37 ran (skipped 0) ... (run 000001-20261111T105532Z) ===` (fix gate).

