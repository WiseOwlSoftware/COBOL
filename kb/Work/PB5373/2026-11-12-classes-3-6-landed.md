**2026-11-12: classes 3 to 6 of this decision are recorded on eight of the 21 rows (wave 20262008, group C).**

Each row below was re-probed on the branch's build, read against its rule, and recorded through `record_verdicts.py --edition 1985` (batch `E:/COBOL-coord/scratch/w20262008c/batch-1.json`; 1985 GAP 381 to 373). Seven are CONFORMS and one is DOCUMENTED-NON-SUPPORT.

| Row | Class | Verdict | Witness |
|---|---|---|---|
| `1985-GR-VII.2.8.4-6` | 3 | DOCUMENTED-NON-SUPPORT | `DocumentedNonSupportWitnessTests.RecordDelimiterStandard1_NamedNonSupportWarning_PinnedToSpec` (and the feature-name arm): COBOL1778 at `--std 85` |
| `1985-GR-IX.1.3.4-6` | 4 | CONFORMS | `pb690_file_character_set_85` (an indexed WRITE of U+20AC answers 91) |
| `1985-GR-VII.4.7.4-14` | 4 | CONFORMS | new `SequentialWriteBoundaryTests` (`/dev/full`, Linux only): 34, USE ran once, record area unaffected |
| `1985-GR-XIV.2.2.4-25` | 4 | CONFORMS | new `ScheduledProgramTests.TheScheduledProgram_FirstReceiveReturnsTheSchedulingMessage_...` |
| `1985-GR-XIV.2.2.4-30` | 4 | CONFORMS | the same test (status 21 after data-name-3 changes) and `MessageControlTests.ScheduledTerminal_ChangedBeforeTheFirstStatement_IsUnknown` |
| `1985-GR-VI.6.24.4-3` | 4 | CONFORMS | `MessageControlTests.AMessagePartlyReceivedByARunUnitThatEnded_IsNotCounted` and the new `APortionSentAndNotEnded_IsPurgedWhenTheRunUnitEnds_...` |
| `1985-GR-VI.4.4.4-1` | 5 | CONFORMS | new `1985-GR-VI_4_4_4-1` (an arbitrary computer-name and MEMORY SIZE 1 WORDS are accepted and the program runs) |
| `1985-SR-XIII.3.17.3-2` | 6 | CONFORMS | new refusal `1985-SR-XIII_3_17_3-2`: COBOL2422 (the rule's own code) at the entry's line, beside COBOL2198 |

**Left open (13 rows):** classes 1 and 2 (nine rows, and `1985-GR-VI.3.4.4-1`, which is class 2 because WiseOwl produces no source listing, `docs/CONFORMANCE.md` section 3 on >>LISTING) wait for kb/Work PB5374's tooling; the three ENTER rows wait for kb/Work PB5308.

**Sites:** `docs/CONFORMANCE.md` (section 3, the row 'A program scheduled by the MCS'),
`conformance-suite/cases/XIII.3.17@1985-SIGN-CLAUSE/` (`1985-SR-XIII_3_17_3-2`),
`conformance-suite/wiseowl/diagnostics/` (`1985-SR-XIII_3_17_3-2.json`),
`conformance-suite/cases/VI.4.4@1985-OBJECT-COMPUTER-PARAGRAPH/` (`1985-GR-VI_4_4_4-1`),
`tests/WiseOwl.Cobol.Tests.Conformance/` (`ScheduledProgramTests`, `SequentialWriteBoundaryTests`),
`tests/WiseOwl.Cobol.Tests.Unit/` (`MessageControlTests`), `tests/version-matrix/inventory-1985/` (the eight rows above).
