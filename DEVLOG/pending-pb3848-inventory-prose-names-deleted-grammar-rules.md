## PB3848: 37 inventory rows stop naming grammar rules the grammar no longer has, and a drift test refuses the next one

kb/Work PB3848 said ten traceability-inventory rows still named the per-statement phrase rules PB3830 deleted
(`arithmeticOnSizeError`, `readAtEnd`, `writeInvalidKey`). Re-measured on main 20fecec14: ten rows, as the note said, but
four of them (GR-14.5.3.2-2, GR-14.5.3.3-2, GR-14.5.3.3-4, FMT-14.9.26.2) name the rule only inside a `retired-witness:`
mark, which is the recorded history of PB3830's retirement and stays. The other six named it in prose: SR-14.9.2.3-5,
FMT-14.9.10.2, FMT-14.9.44.2, SR-14.9.30.3-9, GR-14.9.30.4-19 and SR-14.9.51.3-2. Each is restated against the grammar and
binders as they are (`conditionalPhrases`, `ConditionalPhrases.HasFamily(..., ConditionalPhraseFamily.AtEnd)`, the INVALID KEY
family of `conditionalPhrases`, `receiversOrSendingOperand`), and FMT-14.9.44.2's code-sites paragraph, whose line numbers and
five rule names were all stale, is rewritten symbol-keyed.

The note's second acceptance line asked for the sweep beyond these three names. It was run mechanically: every rule name any
revision of any `.g4` ever declared that the grammar no longer has (177 camel-case names, from `git log -p -- '*.g4'`),
matched as a whole word against every string field of all 4,214 rows outside the retirement marks. 24 more rows named one
(`addOperandList`, `divideIntoOperand`, `subtractFromOperand`, `moveSendingOperand`, `startInvalidKeyPhrase`, `occursBound`,
`repositoryAsPhrase`, `subscriptQualification`, `computerAttributes`, `genericClause` and others), several of them in the
present tense about code PB2114 (the one `sendingOperand`), PB2113 (the parsed subscript) and PB3830 had since rewritten.
All are restated in two `record_verdicts.py` batches; verdicts, editions and witnesses are unchanged, GAP stays 20, and
`audit_witness_loss.py --check` is green. The batches wrote one cosmetic change besides the notes: GR-14.9.12.4-2's `test-ref`
separator is respelled `; ` by the writer's normalization.

The third line asked whether a drift test should exist. It does:
`SpecTraceabilityInventoryDriftTests.NoRowsProse_NamesAGrammarRule_ARetirementRecordsAsDeleted` takes every grammar symbol
that any row's retirement mark records (`retired-witness: ...g4#Name`), drops the ones some committed `.g4` still mentions in
code, and refuses a row whose prose names a remaining one. It needs no list of its own, because the marks are the list, and
it reaches across rows, which the writer's PB762 rule (a retirement must restate the prose of the row that CITED the witness)
never could: a grammar restructure retires the witnesses of the citing rows and nothing else. Its first run found seven more
rows, all through lexer tokens the camel-case sweep cannot match (`SUB_HEXLIT`, `SUB_COLON`, `COMMA_SEP`, plus
`abbreviatedAndChain` and `linkageProcedureParameter`), which were restated in the second batch together with seven more
past-tense mentions, so no row's prose names a deleted rule now. `TheseChecks_ActuallyFail_OnAFabricatedInventory` drives
the check against planted rows and a planted grammar (a second row's prose, the retiring row's own prose, and four positive
controls: behind a mark, a rule that moved, a longer identifier, a rule no mark retires).

What the sweep cannot reach, and the note records: a single-word rule name (`identifier`, `literal`) is indistinguishable
from the English word, and a deleted rule that no row ever cited as a `.g4#Rule` witness has no mark for the test to read.
Line-number citations in older notes (`ArithmeticBinder.cs:94-139`) are staler still but are not rule names; they stay.

Self-review: architecture (the rule lives in the one inventory drift class beside the PB762 prose check, reusing `ProseOf`
and `CodeText`; the only new thing is the cross-row source of names, the marks), full code (the test cites no clause; the
prose it protects cites §5.2.6.4 and §14.9.x rules that were `cite.py --check`ed or kept verbatim), performance (one
regex over four thousand rows, six seconds in the targeted run), duplication (a `WholeWord` helper now serves
`SymbolResolves` and the new check, replacing two copies of one pattern). Gate: the first gate, on the code and inventory
head, was `LEG 1 ONLY (batched gating, PB2515): GREEN` with `Linux half (WSL clone of HEAD) GREEN` (run
000001-20261030T034837Z); the new test passed in its leg 1 Unit run.

No diagnostic codes used (COBOL5482-COBOL5484 returned). No conformance golden: no compiler behavior changed, and the
rule these rows cite is untouched.
