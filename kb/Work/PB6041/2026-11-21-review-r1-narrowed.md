## Review R1-1: the suppression narrowed to the operand that could be the refused key

The branch review of train-1985-f (round 1, R1-1, medium) found the cascade fix over-broad: any refused RECORD KEY or
ALTERNATE RECORD KEY clause made READ skip §2023:14.9.30.3 SR11 and START skip §2023:14.9.41.3 SR6 for EVERY operand. So
`READ IX-FILE KEY IS WS-N` on a WORKING-STORAGE item drew no COBOL0864, and the START drew no COBOL0862, although no repair
of the clause makes WS-N a key (probe `E:/COBOL-coord/scratch/w20262132/review-train-1985-f/probeB.cob`).

Every key a repaired clause can name lies in a record description entry of the file (§1985:IX.2.7.3 SR2,
§1985:IX.2.5.3 SR2; §2023:12.4.5.12.3 SR2, §2023:12.4.5.6.3 SR2). So do GR4's implicit keys and START's generic keys.
`FileModel.CouldBeRefusedRecordKey(operand)` asks that with `RecordLayout.IsInRecordOfFile`, and only such an operand is
spared the rule. GR15's substituted prime key asks `PrimeKeyClauseRefused`, the RECORD KEY clause alone.

The sweep over the other arms:
- RELATIVE KEY: sound. START's SR4/SR5 identity is about the one relative key item, so a refused relative key leaves no
  key to judge any operand against.
- FILE STATUS and RELATIVE KEY agreement of external describers (`ExternalFileConnectorRules`): they skipped the whole
  group when one describer was refused. They now compare the describers that were not refused, so a disagreement among
  the others, or an omission while a refused one wrote the clause, is still reported.
- LINAGE: sound. A refused operand is undecided per operand (`FileDescriptionIdentity.Undecided`).
- RECORD KEY and ALTERNATE RECORD KEY agreement (`FileEntryIdentity`, COBOL6847): this was a missed cascade. A refused key
  clause in one describer was compared by its written name against the other's description. Each key clause is now an
  entry of the segment, and a refused entry is undecided (`FileEntrySegment.Entries`). The number of keys and each decided
  key are still compared.

Tests: `RefusedFileClauseOperandTests`, seven new cases (15 runs), beside the 30 of the first fix.
