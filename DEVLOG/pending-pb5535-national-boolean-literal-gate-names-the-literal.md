## PB5535: the edition gate of a national or boolean literal names the literal

**The defect.** At `--std 85`, `MOVE N"AB" TO X` reported `COBOL0900: national data (PICTURE symbol N / USAGE NATIONAL) requires
COBOL-2002 … the national literal N"AB"`, and `B"1"` named boolean data (PICTURE symbol 1 / USAGE BIT). The program declares no such
item. `LiteralScreen.GateFormat` gated the literal token under the data rows `national-data-2002` and `boolean-data-2002`, because
the edition introduced both at once. Re-probed on this tree before the fix with all four spellings (N, NX, B, BX).

**The fix.** Two construct catalogue rows of their own, `national-literal-2002` and `boolean-literal-2002`. Their edges are quoted:
§2023:8.3.3.5.1 and §2023:8.3.3.4.1 make each literal its own class, and §1985:IV.4.2.2.2 says "Every literal belongs to one of two
types, nonnumeric or numeric". `GateFormat` reports under them, so the message now reads `the national literal N"..." requires
COBOL-2002`. The data rows still gate data items (`VersionConformancePass.GateData`). The registry files were regenerated
(`scripts/gen-constructs.ps1`), and the hexadecimal-literal and VALUE-implied-PICTURE rows' descriptions and the `pb425` golden's
comment now name the literal rows.

**Tests.** `NationalBooleanLiteralTests.LiteralEditionGate_NamesTheLiteral` reads the diagnostic text, because the bug is only in the
words: the four spellings at 85 name the literal row and not the data construct, and they compile at 2002. Each new row's
version-matrix source runs at every edition. Inventory: six national-literal rows had their notes restated through a
`record_verdicts` batch to name the new row. Their verdicts are unchanged, and the GAP count stays 0.
