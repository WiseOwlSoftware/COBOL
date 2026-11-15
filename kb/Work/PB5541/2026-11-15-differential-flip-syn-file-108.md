# 2026-11-15 - the nightly's one differential flip, syn_file:108, is PB5541's intended refusal

**What flipped.** Nightly run 43734875156 at main 40362f08f1 (the GnuCOBOL differential, 1,323 cases) reported one
per-case flip: `syn_file:108` (the corpus group titled "Duplicated FD"), `WE_ACCEPT_THEY_REJECT -> AGREE_REJECT`. Our
diagnostic is COBOL4250 at the second FD (`prog.cob(14,8): error COBOL4250`, line 26 of the run's `gnucobol.log`).
GnuCOBOL rejects the case, so the verdict moved from "we accept, they reject" to "both reject".

**Why it is PB5541's intended effect.** kb/Work/PB5541.md landed at 4245f1e63f on 2026-11-14: `DescriptionRepeated`
refuses a second FD or SD of one file-name under COBOL4250 at both arms of `BindFileSection`. The manifest row
(`tests/external/gnucobol-verdict-baseline.tsv`) predates that fix and still recorded the old acceptance. The
compiler is right; the row was stale. The case is a duplicate FD of one file-name, the shape PB5541 refuses.

**Citation.** ISO §2023:8.4.2.1 (General): "In order to use a resource, a statement shall contain a reference that
uniquely identifies that resource." The clause has no numbered rule; it states the uniqueness requirement for a
user-defined name that names a resource, and a file-name described by two FD entries names its resource twice.
`python scripts/spec/cite.py --check "§2023:8.4.2.1" "In order to use a resource, a statement shall contain a reference that uniquely identifies that resource"`
printed `OK  §2023:8.4.2.1  (General)  the text is in the clause`.

**Record.** The row was re-recorded from the nightly's own `gnucobol-report.json` (its 1,323 cases) through
`gnucobol_differential.write_baseline`, with no re-run; the diff moved exactly two lines, the `# Last recorded:` line
and the `syn_file:108` row. Witness for the refusal: `conformance-suite/wiseowl/diagnostics/pb5541-fd-declared-twice.json`.
