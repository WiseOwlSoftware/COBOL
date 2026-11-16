## Entry 3797 — 2026-11-16 15:22 PST — READ KEY refuses a REDEFINES of the record key in the key's own record (PB5877)

kb/Work PB5877: `RecordLayout.KeyIndexOfKeyItem`, the one place READ's KEY (§2023:14.9.30.3 SR11; §1985:IX.4.5.3 SR2)
asks whether an item IS a record key, matched the implicit key of §2023:12.4.5.12.4 GR4 (§1985:IX.2.7.4 GR3) by
offset, width and class in any record of the file, the key's own record included. `05 IX-KR REDEFINES IX-KEY` in the
key's own 01 therefore passed as a key. Those rules extend the key to all OTHER record description entries only, so an
item in the key's own entry that is not the key is no record key.

Re-probe on this build before the fix (the golden's program, `--std 85`, with the fix line disabled): the program
compiled and ran (exit 1, the missing data file). With the fix: exit 65, `COBOL0864`, at `--std 85` and `--std 2023`.

Fix: the positional arm adds `!ReferenceEquals(RootOf(key), RootOf(operand))`; `RootOf` also replaces the inline
parent walk in `IsInRecordOfFile` (one walk). START keeps admitting such an item as a §2023:14.9.41.3 SR6 b) generic
key, which is a different rule. The same-width alphanumeric item in another 01 stays accepted.

Golden: `cases/14.9.30-READ/pb5877-read-key-redefines-own-record`, refused at all four editions
(`wiseowl/diagnostics/pb5877-read-key-redefines-own-record.json`). Row `1985-SR-IX.4.5.3-2` gained the test-refs; its
verdict is the code read's, left to the registrar.

