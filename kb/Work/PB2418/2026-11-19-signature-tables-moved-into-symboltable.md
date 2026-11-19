**2026-11-19, the two signature tables this note plans to extract already left BinderDriver (found by listing step 6b's closing slice, kb/Work PB5932, carrying the slice-7 review lead R1-2).**

This note's title and its "Seam" paragraph plan to move `BinderDriver.UserFunctionsOf` and `BinderDriver.ProgramPrototypesOf` into a
new `compiler.binding` type, `UnitSignatureTables`. Neither member exists on main any longer: listing step 6b moved each into the one
store of names with its table, as the name class it resolves.

- `ProgramPrototypesOf` is `SymbolTable.ResolveProgramPrototypes` (`src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.Repository.cs`;
  step 6b slice 6, DESIGN-listing.md section 6.7.1, "Step 6b, slice 6").
- `UserFunctionsOf` is `SymbolTable.ResolveFunctionPrototypes` (`src/WiseOwl.Cobol.Compiler/Binding/SymbolTable/SymbolTable.Functions.cs`;
  step 6b slice 7, the same section's "Step 6b, slice 7").

So that part of the extraction is discharged, and in the binding layer already: no `UnitSignatureTables` type is to be made (it would
be a second store of the prototype names, which `NameTableOwnershipDriftTests` refuses). What this note still plans is the other two
moves, `ExternalItemIdentity` to a `DataItem` member and the procedure bind to `ProcedureBindPass`; an implementer of it starts
from those two and reads the prototype signatures through the two `SymbolTable` members above.
