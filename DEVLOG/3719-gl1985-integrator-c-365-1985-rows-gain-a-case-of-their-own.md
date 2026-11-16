## Entry 3719 — 2026-11-15 19:58 PST — gl1985 integrator c: 365 1985 rows gain a case of their own (the widen-failed wave, 42 slugs), GAP 780 -> 416

**The lane's shape.** The third integration of the 1985 golden lane (kb/Work PB5675's rule: a KEPT 1985 row closes only
on a test-ref of its own, a suite case that runs at 85 of the kind its rule needs). Its 374 rows are the `widen-failed`
rows of kb/Work PB5730: an existing case of the row's 2023 targets could not be placed at 85, so each row needed a new
case citing its own 1985 id. By kind: GR 266, SR 51, RV 23, FMT 20, AR 14. The writers' dispositions: 364
`new-golden` (in 254 accepted cases and 2 refusals), 3 `defect-suspected`, 5 `not-closable`, 1
`not-closable-accepted`, 1 `already-witnessed`. No writer listed a `deferred` id.

**Validation.** Every draft ran on this branch's own build at `--std 85`, stdin closed, each in a new run directory: 254
accepted cases equal their `.out` byte for byte (after the host's CR LF became LF) and 2 refusals fail with their
sidecar's code at the violating line (`g85_bf09_07` COBOL0001, `g85_bf11_02` COBOL1961). Their two sidecars existed
only in the writers' `refusals` maps and were written as files. `generate.py --check` and `case_collisions.py --check`
GREEN; no PROGRAM-ID, test id or copybook name collided before or after the merge of main.

**Records.** `record_verdicts.py --edition 1985` (witness-only records): rows changed 365, witnesses +369 test-ref, GAP
844 -> 480 on the branch's base; after merging main (PB5730's 64 rows) the 1985 GAP reads 780 on main and 416 here
(`1985 2274 rules · 416 GAP (374 inherited, no own-edition witness · 14 never adjudicated · 28 open)`). The 365th
witnessed row, `1985-GR-VI.6.19.4-2`, stays GAP because its chain target `GR-14.9.25.4-1` is DIVERGES under kb/Work
PB5678. `1985-SR-VI.3.3.3-1` was reported `already-witnessed`, but its store record held no test-ref: it gains its
existing own refusal case `1985-SR-VI_3_3_3-1` (a diagnosable SR). `witness-batch --edition 1985` wrote 0 records before
and after the merge. LINEAGE READS GREEN.

**The refuter** overturned 38 rows: 17 does-not-exercise-rule, 9 not-spec-derived, 6 citation, 3 expected-value, 2
convention, 1 nondeterministic. The fixers applied all 38 corrections; one fixer refused only the refuter's suggested redesign (a SUM witness for
`1985-GR-XIII.3.14.4-2`) and returned that row not closable.

**Suspected compiler defects** (each re-probed on this build, wrong at `--std 2023` too):
- kb/Work PB5780: a reference-modified numeric item compares as a number (`IF N3 (1:3) = 5` true), blocking
  `1985-GR-IV.4.3.8.4.4-5`.
- kb/Work PB5781: `FUNCTION MIN (A (- B))` is refused, the parenthesised second argument parsed as a subscript on A,
  blocking `1985-SR-VI.6.2.3-4`.
- kb/Work PB5782: a numeric CONTROL item going from -0 to +0 causes a control break, blocking `1985-GR-XIII.3.7.4-3`.

**Not closable, and where each waits.** `1985-GR-VI.6.25.4-12` and `1985-GR-VI.6.27.4-21` (the result is undefined)
take kb/Work PB5757's reason, and `1985-GR-XIII.3.14.4-2` (no program can fail on it) PB5758's; both notes are on
golden-1985-1, not yet on main, so these rows join them once it lands. `1985-GR-X.4.5.4-3` (`not-closable-accepted`:
only a refusal can show EXTERNAL does not imply GLOBAL, and a GR needs an accepted case) has its own decision, kb/Work
PB5783. `1985-GR-XIII.4.5.4-10` and `-18` go back to the lane: their reason (no portable read-back of a report file)
does not hold under DOC-A.1-159 with DOC-A.1-146 (a), the read-back `g85_bf13_06` lands with.

**Held back.** `g85_bf31_06` and `g85_bf31_07` (PB5783's witnesses, correct, out of the tree); the three defect
repros; the withdrawn `g85_bf10_03`, `g85_bf21_10` and `g85_bf25_04`. `docs/conformance/`: the ten determinations
these goldens rest on (DOC-A.1-8, 103, 129, 146, 159, 191, 195, 196, 205, 208) name them under `Pinned by`.

