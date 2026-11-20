## Lead: a reserved word used as a user word under --permissive is a removal with no edition flag (found by kb/Work PB5951's lead sweep)

Listing step pb5947's review lead R1-2 (`review-listing-step-pb5947.jsonl`) found the table of flagged constructs held complete
only for the three band codes. Branch `listing-step-pb5951` fixed the live instance it named (the MERGE in a SORT procedure, row
`merge-in-sort-merge-proc-removed-2023`, now reported through `ConstructRegistry.VerdictAt` with its flag) and made
`EditionFlagDriftTests` read every row's pinned removal code from the registry. One code stays outside the scan: COBOL0901
(`EditionCodes.ReservedWord`). The registry's fifteen rows with a removal edition that pin it (the `user-word-*` rows and the RECEIVE pair, each a word a later edition reserved: COMMIT, XOR,
PROTOTYPE, ...), but no report reads those rows: `VersionConformancePass.ReportReservedWord` reports every reserved word through
`ReservedWordSet.UserWordVerdictAt`, whose `Removed` verdict (the word was a user word at an earlier edition, or a >>COBOL-WORDS
RESERVE took it) is a warning under `--permissive` and carries no `EditionFlag`.

Scenario: `--std 2023 --permissive`, a data item named `XOR` (a user word through 2014, reserved at 2023, row
`user-word-xor-2023`): the compile warns COBOL0901 and the listing's table of flagged constructs omits it.

To do (the root): the reserved-word verdict carries the flag of the word's row (`EditionFlag.Of(row, Removed)` for a word the
registry has a row for; for a word a >>COBOL-WORDS directive reserved, a flag whose construct names the directive), reported through
the same `EditionDiagnostic.Flag`, and the scan then takes COBOL0901 from the rows like every other pinned code.
