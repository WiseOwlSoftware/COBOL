## Listing step 6b-r3, review round 2: how each finding was fixed (2026-11-20)

- R2-1 (BLOCKING, link and clear out of order): `LinkRedefinitionsPass` now reports an unfound data-name-2 (COBOL1577 for a method's
  level-01 entry, otherwise COBOL1654) and clears the clause before it links the next item, as the one loop did before the split, so
  `DataItem.RedefinedEntry` never reads an unresolved redefiner as a redefinition; `ResolveRedefinesPass` screens linked pairs only.
  Pinned by `RedefinesTargetTests.LaterRedefiner_AfterAnUnresolvedOne_NamesTheNearestEntry` at 85 and 2023 (the reviewer's RDCASC
  program: one COBOL1654, no COBOL2739), shown red 2 of 2 with the clear moved back after the link loop. Sibling sweep: the only pass
  this branch split; `BitLayout.StartBitWithin`'s -1 comment and the walk's comment named the old order and are corrected.
  DESIGN-listing.md 6.7.1 (new correction paragraph) and DATA_MODEL_DESIGN.md corrected.
- R2-2 (BLOCKING, older-edition inventories named the moved resolver): the 15 anchors in 10 rows of inventory-1985 and inventory-2002
  re-pointed at `SymbolTable/SymbolTable.DataNameCandidates.cs` (`DataNameCandidates`, `IdentifyEntryClauseOperand`,
  `IdentifyRegionOperand`, `SubtreeCandidates`, `QualifierWalk`, `QualifierChainMatchesFrom`, `ConditionQualifierChainMatches`,
  `ClauseOperandsTakeImplicitQualifiers`). Root: `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` read the
  newest store alone; it now reads every edition's store. That found 16 more dead links in older-edition rows left by earlier
  changes (VisibleFiles, VisitEnterStatement, MaxPageLimit85, sendText, IsDebuggingSection, Model/SymbolTable.cs,
  ReportEntryCandidates, DeclareReportEntries, ReportScope EntriesNamed, Procedure/FunctionValueLane.cs, ArgsOf), each re-pointed
  at its current home; every re-pointed anchor carries a `retired-witness:` mark (audit_witness_loss: 0 unexcused, 40 retired) and
  the rows' notes name the current members. Code reads (`code-reads-1985/`) are evidence at their pin and stay as read.
- R2-3 (LEAD, stale cref): `DataBinder.RenamesEntry.cs` now names `SymbolTable.IdentifyRegionOperand`. Sweep: a documentation build
  of the Compiler project (CS1574 set diffed against the parent 0c29eebc42) found one more cref this branch broke,
  `ReportSumAddend`'s `Name`/`Qualifiers` in `ReportModel.cs`, now `Operand`; every other CS1574 predates the branch (kb/Work PB1322
  holds turning the check on).
