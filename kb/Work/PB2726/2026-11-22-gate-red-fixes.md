## Gate 1 RED, three RepoDrift reds the move exposed (wave 20262135, group E)

Gate run `TestResults/build-local/000001-20261122T191911Z` on 07d70c2637: Conformance 29,080/29,080, Unit 42,689/42,689
and Characterization 37/37 green; RepoDrift red in three tests, each the code having moved from a `.g4` (which those
sweeps do not read as C#) into `Parsing/CobolLexerBase.cs` (which they do):

- `InlineEditionComparisonDriftTests`: `ApplyEditionSpelling` wrote the Annex C edge inline (`_caseMappingEdition >= 2023`),
  a second copy of `CobolNames.HasEditionSpelling` ("the single question the lexer's spelling ... asks"). It now asks that
  predicate; identical for every edition.
- `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`: 16 rows (14 of 2023, one of 1985, one of 2002)
  cited `CobolLexer.g4#<member>`; re-sited to `Parsing/CobolLexerBase.cs#<member>` by record_verdicts batches
  `E:\COBOL-coord\scratch\w20262135\w20262135e\pb2726-resite-{2023,1985,2002}.json` (+16/+2/+1 added, the same retired,
  GAP 4 → 4, 0 → 0, 1173 → 1173).
- `SpecTraceabilityInventoryDriftTests.NoRowsProse_NamesAGrammarRule_ARetirementRecordsAsDeleted`: an old retirement mark
  names `CobolLexer.g4#PreviousIsFunctionName`, which the sweep then called deleted. It is moved, not deleted: the
  sweep's `GrammarSources` now reads the two recognizer superclasses (their C# tokens) beside the grammars.

**Sites (added):** `tests/WiseOwl.Cobol.Tests.RepoDrift/SpecTraceabilityInventoryDriftTests.cs` (`GrammarSources`,
`NoRowsProse_NamesAGrammarRule_ARetirementRecordsAsDeleted`), the 16 inventory store files above.
