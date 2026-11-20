## Step 7b-2, review round 1: four findings, each fixed at its root (commit 257a2c1a0e)

The review of branch `listing-step-7b-2` at 78b6b87f59 (`review-listing-step-7b-2.jsonl`). One line each, the fix and the commit
(`257a2c1a0e`); the design's corrections are in `DESIGN-listing.md` 6.7.1 ("Step 7b, as built").

- **R1-1 (blocking), an index-name's store and a whole table's store printed Read:** a store position stands at a SITE, a `Place` or a
  `SetIndexTarget`; `BoundStores.StorePositionsOf` asks every site the tree holds (`Reach.IndexTargets`) and every site the table linked, so an
  unlinked store is seen and the pairing is red; `ExpressionBinder.IndexTargetOf`, `BoundSearch.VaryingIndex` and the linking
  `ReferenceResolver.WholeTable` link them; INITIALIZE's dynamic-table arm now records its occurrence; `ReferenceStoreKindDriftTests` shows both
  links red by mutation (257a2c1a0e).
- **R1-2 (blocking), the CALL graph was empty for a 1985 program:** literal-1 of CALL and CANCEL is a `lit`-labelled reference slot of the
  grammar, recorded by `CallBinder.RecordProgramName` as an occurrence of the program it reaches by §2023:8.4.6.3 SR1 to SR3
  (`BinderDriver.ProgramsVisibleFrom`) or of the new `ExternalProgram` entry of that name; `ListingCallGraphTests` holds the edges to a contained
  and an external program at `--std 85` (257a2c1a0e).
- **R1-3 (lead), no listing asked, entry lookup done:** every funnel records a data item through `SymbolTable.RecordData`, which asks the entry
  only when the group records references; a source scan holds it (257a2c1a0e).
- **R1-4 (lead), a copy member sorted by its raw text:** its key is `TextInclusion.MemberFold`, `CobolNames.UpperFold` of its text-name
  (257a2c1a0e).

**Decisions recorded here (rule 3):**

- An unresolved literal CALL or CANCEL target is an edge to a named external program (section 6.7 lists "CALL targets" among the symbols and was
  silent on an undefined one): class `ExternalProgram`, no definition, never flagged unreferenced.
- A literal a constant-name spells (§2023:13.10.3 SR2; 2002 and later, since 1985 has no constant entries) is the constant's reference, which
  owns the position: one occurrence per written position is the table's invariant, so that call draws no edge. It is the one literal target
  whose edge is not drawn, a decision and not an oversight; it is named in the design so a later step can revisit it if a program-name
  constant matters to a reader.

**Moved out of "What is not built"** (the 7b-2 note's list and the design's): leads 1, 4 and 5, built here. Still open: leads 2, 3, 6, 7, 8.
