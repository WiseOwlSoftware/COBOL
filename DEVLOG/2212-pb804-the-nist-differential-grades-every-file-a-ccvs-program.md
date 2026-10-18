## Entry 2212 — 2026-10-18 08:53 PDT — PB804: the NIST differential grades every file a CCVS program writes, not the one it used to read

`NistDifferentialTests.RunNist` read exactly one string per program: the print file (`<test>.txt`), else the first
`*.txt` in the run directory, else stdout. It normalized that string and deleted the run directory. Whatever else the
program wrote was neither read nor kept. PB792 hid behind that for a battery cycle: RW104A lost a detail field's
`COLUMN 20` under `--permissive`, the damage was entirely in its second report file `xxxxx049.txt`, `rw104a.txt` stayed
byte-identical to its golden, and battery #61 was all green. The shell guards (`guard.sh`, `guard-run-group.sh`, CI's
`guard` job) had the same shape with a worse candidate list: `<test>.txt`, `print-file.txt` and stdout, any one of which
could rescue a wrong listing.

**Re-probe.** Every green and divergent program, run in its own directory on the fixing build: 150 of the 349 write at
least one file besides their report, 201 files and 8.7 MB in all. They are the `xxxxx0NN.txt` sequential files and the
`tf0NN.txt` relative, indexed and sort stores of the SQ, RL, IX, ST, SM and IC families and the OBSQ chain, the RW101A to
RW104A report-writer files, and the console output of NC109M, NC121M, NC204M and NC220M beside their print files. Two
full runs gave identical bytes for every file. No program produces `print-file.txt`, and no program relied on the
`any-*.txt` fallback.

**Fix, in both arms.**
- C#: `NistRunner` (new, the former private `RunNist`) returns the report plus every file the program WROTE, meaning
  created or modified since a snapshot taken after the compile and after every chain predecessor. `NistOutputs.Grade`
  compares each with `tests/nist/valid-files/<TEST>/<file>` byte for byte and requires the two sets equal: a file with
  no golden, a golden no longer written and one different byte are each red. The report is `<test>.txt`, else stdout;
  the `any-*.txt` fallback is deleted.
- Shell: `guard_files_apply` in `guard-verdict.sh`, called by `guard.sh` and `guard-run-group.sh`, applies the same rule
  to a program whose report matched, using a marker file for "written since". The report is one candidate
  (`guard_report_file`), and the `print-file.txt` candidate is gone. stdout and stderr moved out of the run directory so
  the shell's redirections are not counted as program output.
- The goldens are bytes (`-text` in `.gitattributes`): a sequential or relative file has no line terminator and an
  indexed store is binary. They pin WiseOwl COBOL's own file formats (ISO 13.18.43.4 GR2 leaves the record layout on the
  medium to the implementor); the standard-defined content is what the CCVS program reads back into its print file.
  Two things are the host's, so they are not compared as written: a TEXT file's newline (CR LF is taken as LF; the
  line delimiter of a line sequential file is the host's, DOC-A.1-114 and ISO 12.4.5.10.3 GR2, and the report-writer
  files are CR LF on Windows and LF on Linux) and `(stdout)`. A binary file is never normalized. A deliberate format
  change re-captures with `COBOLNET_UPDATE_NIST_FILES=1`, the way `COBOLNET_UPDATE_SNAPSHOTS` does.
- 95 of the goldens hold a NUL (an indexed or relative store's header and slots, a variable-length record's length
  prefix, a COMP field), and each is named for the file the program wrote, `<name>.txt`. `NulByteDriftTests` sweeps
  `.txt` as text, so its exemption list now takes a directory key, and `tests/nist/valid-files/` is one, with its
  reason. An entry is still dropped when nothing it covers holds a NUL, and a directory whose name only starts with the
  key's is still swept. The landing's Linux unit leg found the 95 (finisher): the Windows leg 1 never ran the sweep,
  because its recorded time is over the tier-0c cost cap (kb/Work PB3112).

**What the first Linux run found.** The Linux gate (conformance and guard legs) ran the new arm on the first commit
and went red twice, both real. RW101A to RW104A wrote LF where the Windows-captured goldens held CR LF: byte-exact
goldens of a line sequential file are not portable, hence the canonical form above. And the shell guard ran 15 programs
that `corpus.tsv` calls `pending` ("cataloged (not asserted)", yet each has a golden and the audit expects MATCH) which
the differential never ran, so they had no written-file goldens to grade against: OBSQ1A, SQ106A, SQ107A, SQ115A, SQ116A,
SQ121A, SQ134A and SQ218A to SQ224A and SQ227A. They match under the differential on Windows, so they are green rows now
(364 theory rows, up from 349) rather than a second, guard-only capture path.

**Falsification** (the deliverable the note asked for). Flipping one bit of RW104A's `xxxxx049.txt` golden reddens
exactly that case: 1 failed of 349, naming the file and byte 2000. `NistWrittenFilesTests` does it on every run and
covers each way a written file can be wrong (different byte, longer, shorter, no golden, golden not written, snapshot
selection, update mode, strays in the golden tree). `guard-verify.sh --witnesses` carries the twin cases for the shell
arm, including a chain member that leaves its predecessor's file alone; ten of them fail against the previous group
runner and pass against this one.

**Abnormal paths.** Per the campaign requirement, the net now sees what the SQ, RL, IX and ST programs leave in their
files after their I-O status and AT END and INVALID KEY tests, not only what they print about it, and the harness's own
failure modes (a file with no golden, a golden no longer written, a truncated or extended file) are each a witnessed red
in both arms, never a silent pass.

**Branch review round 1 (three findings, all fixed).** (1) The net skipped every `TERMINATES` row, the abnormal path the
campaign names: ST146A closes SQ-FS1, writes SQ-FS2 and ends its run unit on a fatal I-O status, and a regression
that lost or corrupted either file at that end left every gate green. `NistRunner` now returns the files a run that
exits non-zero left (no report to separate out, so the print file counts, and the console output whenever it printed),
`NistDifferentialTests` grades them, and the termination arm of both shell guards calls `guard_files_apply` with an
empty report name. ST146A has goldens (`xxxxx014.txt`, `xxxxx015.txt`, the partial `st146a.txt`); NC401M terminates
before writing a file and has none. `EveryGoldenDirectory_BelongsToAGreenProgram` no longer forbids them.
(2) `(stdout)` was graded by two different rules: the C# arm byte-for-byte after the host newline, the shell on the
acceptance basis, so a trailing blank line was red in C# and MATCH in CI. Both now use the acceptance basis
(`NistOutputs.StdoutBasis`), and a console of only blanks printed nothing in both. (3) The three `FILES NO-VERDICT`
arms of `guard_files_apply` had no witness; `guard-verify.sh` now drives each (missing run directory, missing marker,
failing `find`, failing canonicalization) through the function with the step shimmed, plus a control, and every new
case was shown red against a mutant (checks removed, pipefail dropped) or the previous runner.

**Register.** kb/Work PB804 landed, `closes_rows: []` with its reason (a process-only defect, no inventory row).

Review round 2 (R2-1): two doc comments still stated the pre-round-1 rules (`Grade` said every file is compared in its canonical form; `StdoutName` said the console counts only beside a print file). They now say the console is compared on the acceptance basis and is a written file beside a print file or after an abnormal end; the same wording is corrected in `guard-verdict.sh` and DESIGN-test-build-ci.md 3.2.1. Comment-only change.

