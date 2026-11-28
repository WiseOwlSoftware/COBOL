## R2 batch 1c (pin 9e65164eb3a9): 1 finding on this note

### `reference-resolution-2--architecture--f2#4` (finding, extract, votes upheld/upheld/upheld)

**Finding:** The symbol table calls BinderDriver.ContainingDefinitions four times, a BinderDriver static that PB2418 does not list among the members other types call, so PB2320 moving BinderDriver to compiler.pipeline would create a binding-to-pipeline edge

**Rule:** DESIGN-architecture-review 8.1: compiler.binding may not depend on compiler.pipeline (architecture-edges.json lists no such pair; layer 12 is a composition root and gets BinderDriver after 8.3). PB2418 is the note that moves the BinderDriver members other code calls out of BinderDriver before PB2320, and its list names ExternalItemIdentity, UserFunctionsOf, ProgramPrototypesOf and BindProcedures only

**Scenario:** ContainingDefinitions (BinderDriver.cs:1087-1090) only walks BoundUnit.Parent, a model query. SymbolTable reaches it from DeclareContainingDefinitions (Repository.cs:57), ResolveProgramPrototypes (Repository.cs:161, 186) and the function-prototype resolution (Functions.cs:88). When PB2320 moves BinderDriver to WiseOwl.Cobol (compiler.pipeline), each call becomes a cross-layer pair the edge ratchet refuses. The PB2320 implementer then finds an unplanned extraction mid-wave, or the pair is tolerated with no remover.

**Target:** ContainingDefinitions becomes a member of BoundUnit in compiler.model (the unit and its containing units, nearest first), and every caller, BinderDriver included, changes in the same change (rule 4, no forwarder). Record it as an item of PB2418 so PB2320 stays unblocked by an unlisted call.

**Finding sites (the pin's):** src/WiseOwl.Cobol.Compiler/Binding/BinderDriver.cs:1087-1090 (BinderDriver.ContainingDefinitions); src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.Repository.cs:57 (SymbolTable.DeclareContainingDefinitions); src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.Repository.cs:161-186 (SymbolTable.ResolveProgramPrototypes); src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.Functions.cs:88 (SymbolTable function-prototype resolution)

The finding says this note does not list BinderDriver.ContainingDefinitions among the members other types call (SymbolTable calls it four times), so the extract that precedes PB2320 must carry it too. It is recorded here and not filed as a note of its own. The sites are the pin's.
