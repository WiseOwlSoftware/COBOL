# More readers of NumericDisplayForm than PB6172 lists

PB6172 names `AcceptDisplayEmitter`, `EditionContext` and `CliOptions` as the readers a move to compiler.model changes. A grep
of `src` for `NumericDisplayForm` (2026-11-24) finds these files too, and each changes in the same change (CLAUDE.md rule 4):

- `src/WiseOwl.Cobol.Compiler/CodeGen/Emit/EmitCore.cs:103` (`public NumericDisplayForm NumericDisplay`)
- `src/WiseOwl.Cobol.Compiler/CodeGen/Emit/OperandText.cs:482-489` (`AsDeviceString`, the `form is NumericDisplayForm.Decimal` tests; the doc comment at 473-475)
- `src/WiseOwl.Cobol.Compiler/Binding/Model/DataBindResult.cs:64` (`NumericDisplay`)
- `src/WiseOwl.Cobol.Compiler/Binding/Repository/RepositoryVocabulary.cs:172-182` (`Persisted`, `FromPersisted`; the doc at 15)
- `src/WiseOwl.Cobol.Compiler/CompilerDriver.cs:52-53, 88` (the options record's `NumericDisplay`, written `Binding.NumericDisplayForm`)
- `src/WiseOwl.Cobol.Cli/Program.cs:137-142, 339` (through `NumericDisplayFormOption`)
- `src/WiseOwl.Cobol.Compiler/OptionsSectionBuilder.cs:61` (`NumericDisplayFormOption.OptionSpellings`; the Binding type `NumericDisplayFormOption` moves or is re-pointed with the enum)
- `src/WiseOwl.Cobol.Runtime/Repository/RepositoryEnums.cs:9` (a comment naming it)

**Target ratchet.** EmitCore and OperandText sit in the `CodeGen.Emit -> Binding` row, which the move also shrinks; the note's
Target ratchets only `CodeGen -> Binding` back to 68. Re-measure both rows after the move and ratchet each.
