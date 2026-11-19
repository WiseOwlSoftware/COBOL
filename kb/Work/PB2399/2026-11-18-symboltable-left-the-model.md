## SymbolTable's part is answered by moving the table, not by re-pointing it (listing step 6b, kb/Work PB5932)

This note's first item, "SymbolTable indexes the item forest, not the DataBinder (SymbolTable.cs:47-49)", is answered the other
way: listing step 6b moved `SymbolTable` out of `compiler.model` into `compiler.binding`
(`src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/`, namespace `WiseOwl.Cobol.Binding`), because the one store of names owns tables
of binder types and resolves through the binder (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, as built so
far"). The tolerated pairs `Model.SymbolTable -> DataBindState`, `-> DataBinder` (this note's) and `-> ReportScope` (PB2420's)
are gone from `docs/rearchitecture/architecture-edges.json` (the row's walkUses 21 -> 18), and the path this note names is the
table's new file. The note's other items are unchanged.
