**Review R1-1 (record arm, landed with the note):** `WRITE FR` / `REWRITE FR`, where FR is the record of a container's GLOBAL file and also
the program's own data item, was refused COBOL1757 "not a logical record of any file description entry" (`StatementValidation.ResolveRecordName`).
`Declarations.RecordNameHiddenBy` (beside `FileNameHiddenBy`, over `InheritedGlobals.RecordAreas`) now asks the one name-set resolver whether a
nearer declaration hides the GLOBAL record-name, and the refusal says it through `NameResolution.HidingClause` ("record-name").
**Sites:** src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.Declarations.cs:171 (`Declarations.RecordNameHiddenBy`), src/WiseOwl.Cobol.Compiler/Binding/Validation/StatementValidation.cs:808 (`StatementValidation.ResolveRecordName`).
**Witness:** `pb5980-own-data-item-hides-global-record-write-85` (REWRITE shares the arm). This supersedes the lead in the landed addendum.
