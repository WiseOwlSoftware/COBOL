## Entry 4021 — 2026-11-23 17:20 PST — A crosswalk rebuild keeps every adjudicated entry (PB6131)

`python conformance-suite/harness/crosswalk.py --build` used to write each program's file from the mechanical map alone.
On main ddf3400a61 it replaced all 11 adjudicated NC115A entries (`basis: adjudicated: ...`, the PB3178 INSPECT pilot) with
`open`/`ambiguous` mechanical ones. `--check` holds adjudicated entries only to the program's facts, so it said nothing.

The builder now merges by unit name (`crosswalk.merge`). An entry with any basis other than `mechanical` is written back
verbatim. A kept adjudication whose recorded facts (lines, FEATURE, ANSI-REFERENCE, flag) the program now contradicts, or
whose unit is gone, is kept and reported, and the build's verdict reads `BUILD RED`. A unit adjudicated twice stops the
build before any file is written. `--check` runs the same merge and refuses a file in which a rebuild would change an
adjudicated entry. The decision and its rejected alternatives are in kb/Work PB6131.

Rebuilding the whole crosswalk now prints `BUILD GREEN: wrote 459 file(s) ..., 11 adjudicated entries kept verbatim,
0 conflict(s)` and changes no file. The files were already LF: the build writes bytes through `write_file`. An AST sweep of
`conformance-suite/` found no text-mode writer without `newline=`. Self-test cases 18-21 pin the merge, the conflict
reports, the loud failure and LF output. Design §6.3 and the suite README describe the rebuild.

