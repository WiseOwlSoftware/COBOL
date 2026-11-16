**2026-11-16, decided and landed (wave 20262068, group A): a separately compiled EXTERNAL describer is consulted from its activation.**

Re-probe on the branch build: the two-unit witness `conformance:wiseowl/cases/VII.4.7@1985-WRITE-STATEMENT/pb5766_external_advancing_separate_unit`
(PB5766B compiled apart, PB5766A opens, writes P1 and P2 plain, CALLs PB5766B, writes P3) gives the note's welded bytes,
`P1__P2__ LF LF A2__ LF P3__ LF`, read back as one 20-byte record with status 00 (Passed in the targeted run).

Of the note's two fix directions, the documented determination is taken: the external-repository direction cannot
work, because a program compiled apart is found at its CALL and nothing the run unit holds at the OPEN names it, so the
print-file fact could only be guessed. Which files support vertical positioning is the implementor's (Annex A.3 item 37,
`docs/conformance/4/WRITE-WITHOUT-ADVANCING.md`), and the determination already had arm (c), the first ADVANCING write;
it now states the separately compiled describer's arm in words (consulted from `FileRegistry.Register` at its activation,
or from its first ADVANCING write). No code change. Row `1985-GR-VII.4.7.4-16` re-recorded PARTIAL to CONFORMS
(batch `E:/COBOL-coord/scratch/w20262068a/batch-pb5766.json`, 1985 GAP 37 to 36). The 2023 row `GR-14.9.51.4-25` keeps
its CONFORMS, which this determination now covers.
