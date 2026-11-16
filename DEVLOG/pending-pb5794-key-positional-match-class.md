## READ and START KEY refuse an item of another class at a record key's positions (PB5794)

kb/Work PB5794: `RecordLayout.KeyIndexOfKeyItem`, the one place READ's KEY (§2023:14.9.30.3 SR11) and START's
operand (§2023:14.9.41.3 SR6 a) ask whether an item IS a record key, matched the implicit key of §2023:12.4.5.12.4 GR4
by offset and byte width only. A `PIC 9(4)` item at the positions of an `X(4)` key, in a second record description of
the file, was therefore accepted and read by the prime key (`FS=00`), at every edition.

Re-probe on the unfixed tree (main's build): `k1.cob` (READ) and `k2.cob` (START) both ran, `FS=00 REC=2222REC02`,
at `--std 85`. The fix: the positional arm now also asks `SameClassCategoryUsage` (the one predicate START's generic
key arm, b) 2., already used), so an item of another class, category or usage is no key. After the fix `k1` is refused
with COBOL0864, `k2` and the shorter-item control `k3` with COBOL0862, at `--std 85` and `--std 2023`. The
same-width ALPHANUMERIC twin (`k4.cob`) stays accepted: that is the implicit-key reading kb/Work PB354 chose, recorded
in PB5794 as a lead nobody has ruled.

Goldens: `cases/14.9.30-READ/pb5794-read-key-numeric-twin` and `cases/14.9.41-START/pb5794-start-key-numeric-twin`,
each refused at all four editions (`wiseowl/diagnostics/pb5794-*.json`). Rows `SR-14.9.30.3-11` and `SR-14.9.41.3-6`
re-verdicted PARTIAL to CONFORMS (GAP 32 to 30). The two 1985 rows the note names are left to the registrar.
