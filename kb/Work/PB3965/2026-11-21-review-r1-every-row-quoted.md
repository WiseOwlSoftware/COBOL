# PB3965 - review R1-1 answered: every construct row's edge is quoted; a drift test refuses the labels (wave 20262132 group h)

Review round 1 (`E:/COBOL-coord/scratch/w20262132/review-e-h.jsonl`, R1-1) found rows still labelled derived or
provisional: the closing grep of item 1 matched four spellings. The full sweep, a regex over the `description`,
`citation`, `vcr` and `rule1985` of every row (`deriv`, `provisional`, `decision 1`, `ratified decision`,
`holds no <year>`, `not quoted`, `vendor transcription`, any case), found **87 more rows** (among them
`picture-string-50-2002`, `start-first-last-2002`, `standard-compare-2002`).

Six read-only adjudicators (slices D to I; brief `E:/COBOL-coord/scratch/w20262132h/pb3965/BRIEF-2.md`, records
`.../pb3965/out2/<row-id>.json` with each `cite.py --check` verdict line) re-derived each from the held 1985, 2002,
2014 and 2023 texts. **All 87 CONFIRMED**: no `introducedIn` or `removedIn` moves. With item 2's two rows and
item 1's 33, **122 rows** are re-cited.

- **The drift test:** `tests/WiseOwl.Cobol.Tests.Unit/ConstructEdgeProvenanceDriftTests.cs`
  (`EveryEdge_IsQuoted_NeverLabelledDerivedOrProvisional`, plus a test of the pattern itself). It is GREEN on the
  head. With `picture-string-50-2002` put back to its base text it fails: `Failed: 1, Passed: 1`.
- **The sibling sites, in the same change:**
  - change-reference rows 7.6, 7.7, 7.19, 7.26, 7.34, 7.37, 7.38, 7.39 and 7.41. For 7.6 the 1985 MULTIPLE FILE TAPE format
    is now quoted from §1985:VII.2.11.2. For 7.7 the OBJECT-COMPUTER figure is quoted from §1985:VI.4.4.2.
  - the COBOL5944 catalog text, which explains the message and is not the message itself.
  - comments in `ClauseCardinality`, `OptionsBinder`, `IntrinsicBinder`, `VersionConformancePass`,
    `RelationalOperatorGate`, `MoveValidity` and `CobolData.g4` (comments only).
  - `NUMERIC_DESIGN`, `CONTROL_FLOW_DESIGN`, `DATA_MODEL_DESIGN`, `FILES_DESIGN`,
    `DESIGN-frontend-grammar`, `conformance/4/LOCALE`, `PHASE4_RECONCILIATION`.
  - the comment of `pb725-flag-85-removed-2023.cob`.
  - the regenerated `ConstructRegistry.g.cs`.
  - the Characterization snapshot `char_neg_typedef85.85.diag.txt`, whose COBOL0900 message carries the TYPEDEF rows'
    new citations (`WISEOWL_COBOL_UPDATE_SNAPSHOTS=1`; only that snapshot moved).
- **FILES_DESIGN called RECORD IS VARYING a 2002 introduction.** That is wrong: §1985:VII.3.8.2 prints it as Format 2,
  and §1985:VII.3.8.4 GR4 says "Format 2 is used to specify variable length records". The compiler is right: a probe at
  `--std 85` compiles it (exit 0, `E:/COBOL-coord/scratch/w20262132h/rv85.cob`). The doc is corrected.
- **EDITION GATES** is GREEN after three changes:
  - `function-definition-2002` cites `§2023:11.5` and `§2023:10.7.2`.
  - `standard-compare-2002` cites `§2023:15.85`.
  - `initialize-then-replacing-2002` is now proven by its new SR/GR citation, so its HELD_OPEN entry in
    `edition_gates.py` is deleted, as the audit required.
- **CITATIONS** has 0 findings. The baseline shrinks from 6021 to 6019 through `--write-baseline`, and one
  `§2002:F.2 items 129` is rewritten in the form the parser reads.
- **What stays, on purpose:** the phrases are historical narrative in `docs/REARCHITECTURE_PLAN.md` (its risk-register
  mitigation text), `docs/COMPLETION_ROADMAP_COUNCIL.md`, `docs/rearchitecture/evidence/` and `DEVLOG/`, and in
  change-reference row 7.48's account of an overturned edge. None of them is a construct row's claim.

| row | verdict | checks OK |
|---|---|---|
| `accept-four-digit-year-2002` | CONFIRMED | 10 |
| `address-identifier-2002` | CONFIRMED | 15 |
| `aligned-clause-2002` | CONFIRMED | 8 |
| `allocate-2002` | CONFIRMED | 6 |
| `any-length-clause-2002` | CONFIRMED | 9 |
| `arithmetic-standard-2002` | CONFIRMED | 10 |
| `based-clause-2002` | CONFIRMED | 9 |
| `boolean-of-integer-2002` | CONFIRMED | 11 |
| `boolean-operators-2002` | CONFIRMED | 12 |
| `byte-length-2002` | CONFIRMED | 8 |
| `call-as-phrase-2002` | CONFIRMED | 10 |
| `call-returning-2002` | CONFIRMED | 9 |
| `concat-operator-2002` | CONFIRMED | 10 |
| `constant-entry-2002` | CONFIRMED | 10 |
| `currency-picture-symbol-2002` | CONFIRMED | 7 |
| `dynamic-length-structure-2014` | CONFIRMED | 9 |
| `end-accept-2002` | CONFIRMED | 8 |
| `end-display-2002` | CONFIRMED | 7 |
| `exception-file-n-2002` | CONFIRMED | 11 |
| `exit-paragraph-2002` | CONFIRMED | 10 |
| `exit-perform-2002` | CONFIRMED | 12 |
| `exit-section-2002` | CONFIRMED | 11 |
| `file-collating-clause-2002` | CONFIRMED | 7 |
| `file-sharing-clause-2002` | CONFIRMED | 9 |
| `flag-85-directive-window` | CONFIRMED | 6 |
| `flag-native-arithmetic-directive-window` | CONFIRMED | 5 |
| `floating-point-literal-2002` | CONFIRMED | 2 |
| `format-clause-2002` | CONFIRMED | 5 |
| `free-2002` | CONFIRMED | 9 |
| `from-literal-2002` | CONFIRMED | 12 |
| `function-definition-2002` | CONFIRMED | 12 |
| `function-prototype-2002` | CONFIRMED | 12 |
| `goback-bare-2002` | CONFIRMED | 8 |
| `goback-returning-2002` | CONFIRMED | 9 |
| `group-usage-clause-2002` | CONFIRMED | 9 |
| `initialize-filler-2002` | CONFIRMED | 9 |
| `initialize-then-replacing-2002` | CONFIRMED | 9 |
| `initialize-to-default-2002` | CONFIRMED | 9 |
| `initialize-to-value-2002` | CONFIRMED | 10 |
| `invoke-2002` | CONFIRMED | 9 |
| `locale-functions-2002` | CONFIRMED | 16 |
| `locale-time-from-seconds-2014` | CONFIRMED | 10 |
| `lock-mode-clause-2002` | CONFIRMED | 12 |
| `move-alphanumeric-figurative-removed-2023` | CONFIRMED | 8 |
| `not-equal-operator-2014` | CONFIRMED | 10 |
| `options-arithmetic-native-2002` | CONFIRMED | 9 |
| `options-default-rounded-2014` | CONFIRMED | 6 |
| `options-paragraph-2002` | CONFIRMED | 7 |
| `order-table-2002` | CONFIRMED | 17 |
| `pic-external-float-2002` | CONFIRMED | 8 |
| `pic-wide-19-digits-2002` | CONFIRMED | 10 |
| `picture-string-50-2002` | CONFIRMED | 6 |
| `pointer-arithmetic-2002` | CONFIRMED | 11 |
| `procedure-returning-2002` | CONFIRMED | 8 |
| `program-id-recursive-2002` | CONFIRMED | 8 |
| `read-previous-2002` | CONFIRMED | 9 |
| `record-lock-phrase-2002` | CONFIRMED | 15 |
| `replace-also-last-2002` | CONFIRMED | 13 |
| `report-present-when-2002` | CONFIRMED | 8 |
| `report-varying-2002` | CONFIRMED | 9 |
| `retry-phrase-2002` | CONFIRMED | 13 |
| `same-as-clause-2002` | CONFIRMED | 12 |
| `select-when-clause-2002` | CONFIRMED | 8 |
| `set-address-2002` | CONFIRMED | 15 |
| `set-condition-false-2002` | CONFIRMED | 9 |
| `standard-compare-2002` | CONFIRMED | 14 |
| `start-first-last-2002` | CONFIRMED | 13 |
| `start-with-length-2002` | CONFIRMED | 9 |
| `stop-run-status-2002` | CONFIRMED | 6 |
| `table-sort-2002` | CONFIRMED | 9 |
| `test-numval-2002` | CONFIRMED | 16 |
| `type-clause-2002` | CONFIRMED | 11 |
| `typedef-def-2002` | CONFIRMED | 9 |
| `unlock-statement-2002` | CONFIRMED | 11 |
| `usage-float-extended-2002` | CONFIRMED | 12 |
| `usage-float-long-2002` | CONFIRMED | 9 |
| `usage-float-short-2002` | CONFIRMED | 9 |
| `usage-object-reference-active-class-2002` | CONFIRMED | 11 |
| `usage-object-reference-factory-of-2002` | CONFIRMED | 9 |
| `usage-object-reference-only-2002` | CONFIRMED | 10 |
| `user-word-extended-letter-2002` | CONFIRMED | 9 |
| `user-word-underscore-2002` | CONFIRMED | 6 |
| `validate-statement-2002` | CONFIRMED | 7 |
| `validation-clauses-2002` | CONFIRMED | 11 |
| `value-implied-picture-2002` | CONFIRMED | 8 |
| `value-table-format-2002` | CONFIRMED | 6 |
| `write-rewrite-file-phrase-2002` | CONFIRMED | 17 |
