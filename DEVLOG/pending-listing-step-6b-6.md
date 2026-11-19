## PB5932: the source listing, step 6b slice 6 - the program-prototype-names move into SymbolTable with their resolution

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands slice 6, from
`listing-step-6b-6`: the program-prototype-names, shown behaviour-neutral by the architecture oracle, and the baseline lowered from 234
to 223.

**The program-prototype-names.** `SymbolTable.Repository.cs` owns the REPOSITORY paragraph's program-specifiers as written and the
refused ones (§2023:12.3.8.2; kb/Work PB4708), which a contained unit and a class half inherit with the configuration-section names
(§2023:12.3.8.4 GR10, §2023:12.3.4 GR1); the program prototypes they resolve to, with §2023:8.4.6.8's containing program definitions
registered last (`ResolveProgramPrototypes`, which was `BinderDriver.ProgramPrototypesOf`); and the §2023:12.3.8.3 SR1 memory of every
name the paragraph specifies (`RepeatedRepositorySpecification`, which was `DataBinder.CheckRepositorySpecification` and its table).
`ConfigurationSlice.ProgramSpecifiers`, `RefusedProgramSpecifiers`, `StatementBinder.ProgramPrototypes`, `ProgramSignatureOf(string?)` and
`ProgramRestrictionOf(string)` are gone. `NameClass` gains `ProgramPrototype`, and every program-prototype-name a statement writes is
`SymbolTable.Identify`: CALL's AS phrase and its bare spelling (b) (§2023:14.9.4.3 SR16), CANCEL's operand (§2023:14.9.5.3 SR3), a
program-address-identifier's program-prototype-name-1 (§2023:8.4.3.13.4 GR3), and the statement funnel's pure question.

**What the code proved about the design** (section 6.7.1, "Step 6b, slice 6"):

- The progress table's "program, function, prototype, repository" row is two classes: §2023:8.3.2.2 lists program-prototype-name and
  function-prototype-name apart, each with its own scope rule (§2023:8.4.6.8, §2023:8.4.6.6). The function-prototype-names are the next
  slice's class.
- A prototype-name a SYMBOL carries as text (a restricted program-pointer's USAGE TO phrase, §2023:13.18.60.4 GR25, which a repository
  record also decodes with no parse tree; a program-address-identifier's bound node) is resolved by the table from the symbol
  (`ProgramSignatureOf(DataItem)`, `ProgramSignatureOf(BoundProgramAddress)`, `ProgramRestrictionOf`, `RestrictedProgramPrototypeOf`),
  in the referencing element's scope as before, so no caller turns a name into a prototype.
- The AS NESTED callee table is read with the name a CALL's literal-1 forms (§2023:14.9.4.3 SR15), an externalized name compared by the
  implementor's mapping rule (DOC-A.1-68), so it is keyed by `ExternalizedProgramName`, a type of its own, like slice 2's tables keyed by
  no COBOL user word.

**Evidence.** `python scripts/arch/compare_oracle.py` against the base 3dd8fd019b: `=== ARCH-ORACLE: IDENTICAL — 22647 cases ===`.
`NameTableOwnershipDriftTests` showed the 11 moved entries STALE (rule (1) 4, rule (3) 2, rule (5) 5) and no new finding before the
baseline was lowered; a planted `Dictionary<string, ProgramPrototype>` on `StatementBinder` turned rule (1) red. `ExternalizedProgramNameTests`
pins the key's equality to the mapping rule. The section 9 measurement of the `None` row against the parent is in the benchmark README's
step-6b table, slice 6.

**Records the move reached.** `architecture-edges.json`'s tolerated `Binding.Bound -> Binding` row lost two pairs (walkUses 46 -> 44);
`BackendContract.Table` lost the rows of the two deleted `StatementBinder` members and classifies the new key's text a run-time datum;
`binder-member-readers.json` lists `OoDriver` as a reader of `DataBinder.Symbols`; 15 inventory rows (11 of 2023, 4 of 2002) whose
code-location named a moved member name the table's.

**What remains** (kb/Work PB5932's dated file `2026-11-19-step-6b-slice-6`): the data-name class, the function-prototype-names and the OO
names.
