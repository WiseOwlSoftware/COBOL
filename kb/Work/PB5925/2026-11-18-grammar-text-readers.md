## Lead: about fifty tests read the grammar's text with readers of their own (listing step 7a)

Step 7a put a classifying label on every name slot of the grammar (`refDataReference1=dataReference`). Thirteen facts of nine
Unit test classes read a rule's shape from the `.g4` text with regular expressions and went red on the labels
(`PointerUsageOperandDriftTests`, `DeclinedFacilityDriftTests`, `PrintedFormatAlternativeDriftTests`, `SendingOperandDriftTests`,
`UserWordSlotDriftTests`, `ClassConditionTableDriftTests`, `PartialExpressionSpineDriftTests`, `ReceivingOperandFormDriftTests`,
`TrailingInPhraseDriftTests`). They now read through the one shared reader, `tests/_shared/GrammarSource.cs`
(`GrammarSource.Read`, labels removed).

About fifty other test files under `tests/` read a `.g4` file with `File.ReadAllText` or `File.ReadAllLines` and a private
comment stripper of their own (59 files mention a grammar path and read a file; the count is
`grep -rl '\.g4\|"Grammar"' tests --include=*.cs | xargs grep -l 'ReadAllText\|ReadAllLines\|ReadLines'`). They pass today,
because what they match does not stand next to a slot, but the next label added beside what one of them matches turns it red,
and a test asserting that something is ABSENT could pass vacuously. The Python side already has one reader,
`scripts/spec/grammar_source.py`, with `private_readers` refusing a second.

Lead: route every test's grammar read through `GrammarSource` (with comment stripping as one of its functions, replacing the
dozen private `StripComments` copies), and hold it with a drift test like `ScratchDirectoryDriftTests`: no test file outside
`GrammarSource.cs` reads a path under `src/WiseOwl.Cobol.Frontend/Grammar` with a `File.Read*` call.
