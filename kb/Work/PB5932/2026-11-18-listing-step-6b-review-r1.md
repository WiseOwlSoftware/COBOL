## Listing step 6b slice 1, review round 1: how each finding was fixed (2026-11-18)

- **R1-1 (BLOCKING, glued text).** Root: `SymbolTable.Identify` keyed every lookup by `written.GetText()`, which glues the tokens
  of a multi-token context, and `IntrinsicBinder.BindExceptionFileArg` handed it the whole argument when the argument was no
  sole data reference, so `FUNCTION EXCEPTION-FILE (A - B)` bound to the file `A-B`. Fixed in the table: a name is one word and
  a COBOL word is one token (`cobolWord`, CobolWords.g4), so `Identify` keys that token's text (`SymbolTable.WordOf`) and answers
  null for a context of several tokens. Every caller is covered at once (the sweep: `SortBinder.SubjectWord` hands the whole
  operand when it has no word, and now finds no file through it). `BindExceptionFileArg` also no longer passes a non-name: its
  `written` is a `CobolWordContext` or null, and null goes straight to COBOL1574. Witness: the new conformance case
  `pb5932-exception-file-glued-expression` (refused at 2023 with COBOL1574, as main refuses it). DESIGN-listing.md section 6.7.1
  carries the correction ("`Identify` keys one word").
- **R1-2 (LEAD, stale doc comments).** `NearerFileNameHides` has its summary back, and `IsSelected` has one; crefs and prose
  naming `DeclarationFactOf` now name `OtherThanData`, `NonDataDeclaration` now `DeclarationFact`, `InheritedGlobals.DropShadowed`
  as the file arm now `SymbolTable.HideShadowedFiles`, and "Resolve through VisibleFiles" now `SymbolTable.Identify`. Sweep: a
  `GenerateDocumentationFile` build of `WiseOwl.Cobol.Compiler` at the branch against the same build at its merge base 247b8dfdaa
  found six unresolved crefs the branch introduced, all fixed (four unqualified crefs in `SymbolTable.Declarations.cs` to
  members of `DataBinder` and `FileControlBinder`, `DataBinder.cs`'s cref to the moved `ConditionNameHiddenByNearerNonDataName`,
  and `InheritedGlobals.cs`'s cref to its deleted `Files`); three test comments that named the deleted
  `DataBinder.NonDataDeclarationOf` now name `SymbolTable.Declarations.OtherThanData`. The root, crefs that no build resolves,
  is kb/Work PB1322 (open); its new finding file records the repository-wide count.
