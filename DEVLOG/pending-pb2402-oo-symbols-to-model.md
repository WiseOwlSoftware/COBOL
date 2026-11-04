## The backend seam, wave 20261851 group A: the OO symbols and their closure move to compiler.model (PB2402)

**Carried in.** This branch continues the PB2293 campaign: the predecessor branches' PB4319, PB2398 and PB2401 commits were
cherry-picked onto main `a9f2c2897`, and the PB4509 branch (`worktree-wf_3feef266-b02-2`, PR 1086, its DEVLOG already
stamped 3056) was merged with the merge drivers. The first gate on the combined head was RED on one Unit test: the edges
file's `Binding.Bound → Binding` row said 49 walked pairs where the walk measures 48 (main removed one after the
predecessor's base). The ratchet lowered it; the re-gate was GREEN.

**The prerequisite Extract.** The re-probe confirmed the predecessor's what-if: `OoMethodDataScope`, the method's model
name scope, held two members of binder working state, its §8.3.2.2 census (`Words`) and its constant tables (`Constants`,
whose declared entries hold four parse contexts each). Moved with the symbols they would have added `Binding.Model →
Binding` pairs. They now live binder-side. The census is the class table's, one per method symbol for the whole
compilation (`OoClassTable.CensusOf(OoMethodSymbol)`, beside `_elementWords`), so the binder declaring a method's words and
every reference made in it still record into one object; `OoDataBinder.EnterMethodDeclarations` takes the symbol and
selects it. The constants are the class half's `ConstantEntryBinder._methodConstants`, keyed by the method's name scope.
The one "does this method declare the word" predicate (ISO §11.7.4 GR5) split at the same line: `OoMethodDataScope.IndexesDeclare`
asks the scope's five name indexes, `ConstantEntryBinder.MethodDeclares` the constants and the words the data division
defines before any entry binds, and `DataBinder.MethodDeclares`, THE answer, asks both; every `SymbolTable` method tier,
`TryFindTypeDecl` and `ActiveMethodDeclares` ask it. `GlobalNameClassDriftTests` holds all three (reflection over the
scope's indexes, text over both halves). Oracle: `=== ARCH-ORACLE: IDENTICAL — 16464 cases (baseline 6e13573d5abe) ===`.

**The move.** `OoClassSymbol`, `OoInterfaceSymbol`, `OoMethodSymbol`, `NamingConvention`, `OoMethodBinding`, `OoFormal`,
`OoMethodDataScope` and `OptionsModel.cs`'s types are in `Binding/Model` (namespace `WiseOwl.Cobol.Binding.Model`), and
`StandardMethod` left `OoStandardClasses.cs` for its own file; every caller's `using` and qualified name changed, and the
`using WiseOwl.Cobol.Compiler.Oo` lines that named nothing left were removed. The walk showed only falls, as the design
predicted: `Binding.Model → Compiler.Oo` 5 → 2 (`AdapterPair` and `OoClassTable`, PB2403), `Binding.Bound → Compiler.Oo`
6 → 1 (`StatementBinder → OoClassTable`, PB2347), `CodeGen → Compiler.Oo` 28 → 13, and the OPTIONS-model pairs off four
`→ Binding` rows. The ratchet lowered them and PB2402 left every `removedBy`. The symbols are now in the backend contract's
scanned namespace, so their text members were classified (DESIGN-backend-abstraction §6). Five whose names read as C#
were renamed for what they carry: `FactoryOrInstanceClrNames`, `ImplementedClrNames` (CLR type names), `RunUnitKey`,
`HalfKey` (run-unit keys) and `ImportedFrom.AssemblyFile`. `NamingConvention.FactoryInstanceField`, a storage member named
by text, joined the L4 row. Inventory: 33 rows re-sited to the moved files and two to the renamed members, by
record_verdicts batches (GAP 1 → 1). Oracle: `=== ARCH-ORACLE: IDENTICAL — 16464 cases (baseline 5471f0e35f08) ===`.
Docs: OO_DESIGN, INTRINSICS_DESIGN, DESIGN-architecture-review §8.1, DESIGN-backend-abstraction L6.
