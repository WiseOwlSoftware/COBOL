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

### Folded in: the entry first written for this work as number 3435 (2026-11-10 07:06 PST), which never landed

**PB5073: a GLOBAL data-name and an index-name are asked for uniqueness at their declaration**

Re-probe on the Debug build: a GLOBAL data-name shared with another item of its element (`01 DUP-NAME GLOBAL` beside `02 DUP-NAME`),
two `01 DUP-ITEM GLOBAL` entries, two tables `INDEXED BY IX`, and one entry `INDEXED BY IX IX` all compiled and ran at `--std 85`
and at 2023; only a REFERENCE to a duplicated index-name drew COBOL1639.

Mechanism: nothing asked the declarations, only references. New `DataBinder.NameUniqueness.cs` (`ScreenNameUniqueness`), run once in
`Bind` after the unit's own names are all declared:

- COBOL6862, every edition. §2023:8.4.2.1 (the same sentence as §1985:IV.4.3.8.1; §1985:X.4.6.3 SR2 states it for two GLOBAL
  items): a name the GLOBAL clause makes global is no other data item's name in the element. The global entry is the record that
  carries the clause (or whose FD does); the subordinates of two global records stay a qualification matter, as
  `pb1047-two-globals-one-element-ambiguous` pins (§2023:8.4.6.2.1 3) b) 1.). Reported at the later entry of the pair. A TYPE
  or SAME AS copy IS a data item of the element ("as though ... coded in place", §2023:13.18.57.4 GR1, §2023:13.18.49.4 GR1;
  branch review R1-1 caught a first version that exempted copies), so the GLOBAL screen is `CheckGlobalNameUniquenessPass`,
  run right after ExpandTypes on the composed forest; a GLOBAL name inherited from a container stays out. Golden
  `pb5073-global-data-name-duplicated-by-type-copy` (refused at 2023). The index-name screen stays in `Bind`.
- COBOL6863, `--std 85` only. §1985:VI.5.8.3 SR14: an index-name is a unique word within the program. 2002 and later qualify an
  index-name (Annex F item 108), so two tables may each declare one spelling and only an unqualified reference is ambiguous.

The first gate (two reds) found one existing test written against an illegal program: `ApplyCommitEnclosingFileTests.
TwoGlobalFilesRecordsOfOneName_AreToldApartByTheFileQualifier` declared records named R in two GLOBAL files, a record of a GLOBAL
file possessing the global attribute. It now names F1's record R1, and asserts the same-name pair draws COBOL6862. The facts file
`tests/doc-facts/compiler-facts.json` was regenerated for the two codes.

Goldens: `pb5073_global_name_unique_beside_qualified_locals` (85 to 2023) and refused `pb5073-global-data-name-duplicated` (all
four editions); `pb5073_index_name_twice_qualified` (2002 and later) and refused `pb5073-index-name-declared-twice-85`.
No inventory row is closed (the 1985 rows do not exist yet; the 2023 catalog has no row for the §2023:8.4.2.1 sentence, so the
GLOBAL goldens witness SR-13.18.27.3-1, the nearest row).


Finisher (merge-group bounce of PR #1465, run 42841304077): the one red, CCVS `CM102M_ReportsNoFailure` timing out at 120 s on a
GitHub Windows runner, was not this change. CM102M waits 90 s on its own clock by design (three 30-second ACCEPT FROM TIME
delays), which left 30 s of the one-unit harness budget for load. The budget rule now adds a stated own-clock wait to the
units (`ObservationBudget.OneProcessWaiting`, kb/Work PB5154), and CM102M states its 90 s.
