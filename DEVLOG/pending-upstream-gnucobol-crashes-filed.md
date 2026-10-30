## PB3908, PB3874: the two GnuCOBOL compiler crashes are filed upstream as bugs #1265 and #1266

The owner's public reply to the GnuCOBOL project lead promised the upstream findings through the SourceForge trackers,
starting with the two compiler crashes, and then asked for them to be filed. Before filing, every repro was re-run on
2026-10-30 on cobc 3.2.0 (`/usr/bin/cobc`) and on the 3.3-dev r5751 build (`~/opt/gnucobol-3.3-dev`, wrapper
`cobc-dev`; the patched exponentiation build `~/opt/gc-patch` was deliberately not used). The results match the
2026-10-29 records exactly:

- **PB3908 → [bugs #1265](https://sourceforge.net/p/gnucobol/bugs/1265/).** `EVALUATE IS-TWO WHEN N = 2` (a
  condition-name subject with a relation-condition object, legal per §14.9.13.3 SR 7 b) and Table 15) segfaults cobc
  on both versions (exit 139) under `-x`, `-fsyntax-only` and the default dialect; the invalid `WHEN N` variant passes
  `-fsyntax-only` silently and segfaults in codegen.
- **PB3874 → [bugs #1266](https://sourceforge.net/p/gnucobol/bugs/1266/).** `IF R1 = R2` on two universal object
  references reaches `unexpected category: 0` (tree.c:1606 on 3.2.0, exit 134; tree.c:1613 then SIGSEGV on 3.3-dev,
  exit 139). `SET WS-OBJ TO NULL` aborts the same way on 3.2.0 but gets an ordinary `invalid SET statement` on 3.3-dev,
  so the ticket leads with the relation condition, the trigger the development line still crashes on.

The owner shaped the tickets in review: each shows the compiler's verbatim output for every case on both versions,
not a description of it, and attaches the `.cob` repro files besides quoting the main program inline. Both notes now
carry `upstream_status: reported` and their `upstream_ref`; the rest of the upstream batch (about 18 reports) is still
`unreported` in `work.py upstream`.
