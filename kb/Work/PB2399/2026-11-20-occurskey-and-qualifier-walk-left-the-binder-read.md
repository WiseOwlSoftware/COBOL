## Two of this note's binder reads ended with listing step 6b remainder slice 3 (kb/Work PB5932, branch `listing-step-6b-r3`)

- **`DataBinder.WrittenQualified` from `OccursKey` (OdoModel.cs).** `OccursKey` keeps its data-name-2 as a `WrittenDataName` (the model's
  written words, `Binding/Model/WrittenDataName.cs`), and `OccursKey.Written` is `Operand.Written`: the model type reads nothing of the
  binder. `ArchitectureEdgesDriftTests` measured the pair gone; `docs/rearchitecture/architecture-edges.json` lost the tolerated pair
  `WiseOwl.Cobol.Binding.Model.OccursKey -> WiseOwl.Cobol.Binding.DataBinder` and the row's `walkUses` fell 18 -> 17 (the ratchet).
- **`DataBinder.QualifierWalk`.** The one §2023:8.4.2.2.3 SR4 walk is now `SymbolTable.QualifierWalk`, private to the symbol table
  (`Binding/SymbolTable/SymbolTable.DataNameCandidates.cs`), with every caller that reached it (the data-name, condition-name and
  index-name chain matchers, the report section's sum counters and entries). No model type reads it; the work this note planned for it
  (a model item query) is no longer owed, since the walk is the table's name equality, not an item fact.

What remains of this note is unchanged by the slice.
