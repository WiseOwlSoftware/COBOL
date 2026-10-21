## PB3479 — the file harness declares OPEN EXTEND over sequential access, as ISO 14.9.27.3 SR2 requires

`FileHarness.Register` (kb/Work PB3471's merge of the nine private copies) defaults a RELATIVE or INDEXED connector to dynamic
access, where the copies it replaced passed access mode 0. Every theory that crosses the open modes therefore opened EXTEND over a
declaration the compiler refuses (COBOLNET2372, §14.9.27.3 SR2: "The EXTEND phrase shall be specified only if the access mode of
the file connector referenced by file-name-1 is sequential"), and the Table 19 and '61' answers it pinned were measured on a
program no one can write. Re-probe: `cobol.exe` on `ACCESS MODE IS DYNAMIC` plus `OPEN EXTEND` answers COBOLNET2372 on this build.

The note named four classes; the sweep found five. `PermanentErrorOutranksRecordLockTests` opens INPUT and I-O only (no EXTEND
cell, nothing to fix); `RelativeEngineTests` (three theories over `Opens`: I-O, EXTEND, OUTPUT) was the one it missed.

Fix shape: the rule is written once. `FileHarness.AccessFor(mode)` answers sequential for EXTEND and dynamic for the rest, and
`FileHarness.Open` (new) fails a test that opens EXTEND over a connector `Register` declared with RANDOM or DYNAMIC access, so
the next theory that crosses the modes cannot reintroduce the state silently. The mode-crossing theories of the five classes
declare `access: AccessFor(mode)` per connector and open through `Open`; `IndexedEngineTests`' private copy of the same
conditional calls `AccessFor`. `FileHarnessExtendDeclarationTests` pins the mapping, opens every organization x mode on the
declaration it gives, and shows the guard failing. `Table20WriteOpenModeTests` stays as it is: it opens DYNAMIC and RANDOM
connectors EXTEND on purpose, to test the runtime's own '48'. Seed's OUTPUT opens are legal under dynamic access. No inventory row
is claimed; `closes_rows: []`.
