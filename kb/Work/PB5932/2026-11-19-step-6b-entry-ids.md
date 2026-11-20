## Step 6b, the entry ids (w20262104u, branch `listing-step-6b-ids`): every class the table declares enters its entry, every identification carries its id

A lane beside the data-name lane (`listing-step-6b-r1`, items A to C of `2026-11-19-step-6b-close`), dispatched for item D: the
`SymbolId` that kb/Work PB5925's dated file `2026-11-19-step-7b-not-buildable-yet` (finding 2) and this note's
`2026-11-19-entry-ids-owed-to-step-7b` found owed. It touches no data-name or condition-name lookup, no `NameResolution` or
`ReferenceResolver` member and no baseline entry; the baseline is unchanged (86).

**Built** (DESIGN-listing.md section 6.7.1, "Step 6b, the entry ids", which carries the corrections):

- `SymbolTable.EntryStore` (`SymbolTable.Entries.cs`, new): one per compilation group (`OoClassTable.Entries`; a binder outside every
  group is a group of its own). `Enter(element, NameClass, name, at, symbol) -> SymbolId`, `IdOf(symbol)`, `Answer(object, SymbolId)`,
  `MadeBy(element)`; an entry is `SymbolTable.Entry(Element, Class, Name, At, Symbol)`. `SymbolId` is an opaque 1-based number;
  `ICandidateEntries` is a candidate set's entries.
- Every class whose declaration is the table's enters at its declaration: procedure-names (`DeclareParagraph`, `DeclareSection` take
  the header; the table makes one `ParagraphDefinition` per paragraph), the nine configuration-section classes (each `Declare*` takes the
  declaring token; `NameDeclaration<T>` is the entry's symbol), constants (FILLER included), files, CDs, reports and their report group
  description entries, program- and function-prototype-names (and every unit's own program entry, `DeclareProgram`), the DEBUG-ITEM
  registers, VARYING counters, index-names, CAPACITY registers, type-names, and the group tier's object-class-names, interface-names,
  imported classes and interfaces, the standard class BASE and the parameter-names of a definition bound as itself.
- `IdentifiedName.Id`, refused when missing (the constructor throws); `ProcedureCandidates`, `ReportCandidates<T>` and
  `CapacityRegisterCandidates` carry `Ids`; `OoNameResult.Entry`, set by `ClassNames.Lookup`, which refuses a missing one;
  `SymbolTable.IndexEntryOf` for an index-name candidate.
- `SymbolTableFinish.Of(..., SymbolTable.EntryStore, ...)` writes the entries the bind made (`SymbolTableWriter.Declare(SymbolId, ...)`);
  `ConstantEntryBinder.DeclaredConstants` is deleted; the data map lists `TypeName` and `CapacityRegister` entries and ignores
  `NameAttributes`; `SymbolTableSnapshot`'s `NameClass` members no longer say "no entry yet" and gain `ObjectClass`, `Interface` and
  `ParameterName`.

**Corrections the code proved** (each in section 6.7.1): the id beside the symbol is ONE identity index in the store, not a field per
class table (three inheritance paths hand a contained element its container's bare symbol); a shared value (`AlphabetDef.Native`, an
implementor-name row) cannot be an entry's symbol, so its table keeps a declaration object; the standard class BASE and an imported
class are entered when the group first names them; a type declaration's entry is its type-name's, which the data-name lane must answer
rather than enter a second time (`Enter` refuses a symbol twice); the finish still declares the data-names and condition-names from
the frozen forest and adopts their entries the day their declarations make them (`SymbolTableFinish.DeclareEntered`), checking the
bind's position against the record's.

**Behaviour-neutral:** `python scripts/arch/compare_oracle.py --no-build` against the branch's base 8dd07d2eabd1 after the source
changes: `=== ARCH-ORACLE: IDENTICAL — 22647 cases (baseline 8dd07d2eabd1, new 8dd07d2eabd1-dirty) ===` (commit a4799a5605; every
compile of the oracle ran with the guard on, so no identification of the population lacked an entry). Unit 42190 and Conformance 28191
passed, RepoDrift 187 passed, on that tree.

**Drift tests:** `SymbolEntryDriftTests` (Unit): the guard (an identification without an entry, and a candidate set with a candidate
without one, are refused), three planted programs (2023, 1985, OO) and a 2023 report program whose every class's entry is checked at
the class, name, line and column written from the source text, and `EveryClassOfTheTable_HasAWitness`. Shown able to fail: with
`DeclareMnemonic`'s `Enter` removed, `Program2023_EnteredEveryName_WhereItIsDeclared` is red with "SymbolTable identified a Mnemonic
(ImplementorName) whose declaration entered no entry"; before the planted programs held a witness of each class, the witness test was
red naming "Condition, NationalAlphabet, OrderingName, DynamicLengthStructure, Locale, ReportVarying, ParameterName".

**Records the change reached:** `binder-member-readers.json` (`ConstantEntryBinder::DeclaredConstants` deleted; `DataBinder::Constants`
loses `SymbolTableFinish`; `DataBinder::OoClasses` gains `SymbolTable`; `DataBinder::Symbols` gains `SymbolTableFinish`),
`ListingOffDoesNoWorkDriftTests` (the finished table of its program holds the paragraph's entry), `SymbolTableConfigurationNamesTests`
(a declaration takes its word).

**Section 9** (the `None` row of the front end and bind, branch a4799a5605 against its parent 8dd07d2eab, Release, branch, branch,
parent, parent; a first parent run failed to build BenchmarkDotNet's boilerplate in its two-minute timeout and measured nothing):
IX113A 68.45 and 68.74 MB against 68.29 and 68.43; NC105A 34.81 and 34.82 against 34.67 and 34.66; NC218A 30.75 and 30.78 against
30.64 and 30.72; SM201A 15.12 and 15.12 against 15.23 and 15.28. The entries are a fixed cost per declared name (about 0.15 MB on
NC105A), made for every compilation by the design, so section 9's `None` budget now names them, as step 7a's label fields were named;
SM201A allocates less (a paragraph's identification no longer makes a `ParagraphDefinition` per lookup). The benchmark README's
step-6b table, the entry-id lane.

**The owed list, settled:** `2026-11-19-entry-ids-owed-to-step-7b`'s three items are built for every class the table declared on
8dd07d2eab (declaration makes the entry, `Identify`, `ClassNames.Lookup` and `IndexEntryOf` answer the id, the finish reads the
bind's entries); the data-names and condition-names owe theirs with their declarations (the data-name lane), in the shape
`SymbolTableFinish.DeclareEntered` already adopts.

**What enters no entry, and why it is left:** a REPOSITORY program-specifier whose literal-3 was refused, and a CAPACITY register SR30
refused: each declares a word no reference resolves to and binds with an error, so step 7b's completeness oracle (clean binds only)
never meets one. The data-names and condition-names enter with their declarations in the data-name lane.

### Leads carried

Every LEAD of `python scripts/orchestrator/review_findings.py show` over the 21 `review-listing-step-*.jsonl` files (steps 1, 5, 6, 6b,
6b-5, 6b-close, 6b-data, 8 and 9 hold none), and the two of `2026-11-19-review-leads-slices-2-and-3`, checked on this branch
(origin/main 8dd07d2eab plus this lane):

| Lead | State | Evidence |
|---|---|---|
| step 7b R1-1 (the 14.7.8 "rule 1"/"rule 2" sweep incomplete) | fixed here (81e1335eb9) | DataBinder.cs, DATA_MODEL_DESIGN.md:382, SpecialNamesBinder.Alphabets.cs, ConditionRenderer.cs:182, ec_range_invalid.cob and GR-8.8.4.5.3-1.json's "§§" were already fixed on main; the rest were still true and are fixed: ConditionRenderer.cs (2 comments), CollatingModel.cs, CONDITIONS_EXCEPTIONS_DESIGN.md (4 sentences), l1_value_thru_through_equivalent(_85).cob, pb401_range_invalid_by_class.cob, inventory DOC-A.1-203 and SR-14.9.13.3-1; `cite.py --check "§2023:14.7.8 GR1" "the range of values includes literal-1, literal-2"` and `"§2023:14.7.8 GR2" "the collating sequence used for range evaluation"` -> OK |
| step 2 R2-1 (ListingRequest init accessors) | already fixed | `ListingRequest.WithDestination` (6 mentions in ListingRequest.cs) |
| step 3 R1-1 (Column a UTF-16 index) | already fixed | `ListingDiagnostic`'s doc: "The 1-based character position in that line's text" |
| step 3 R1-2 (SeverityWord restated) | already fixed | the close session's table; `TextListingRenderer.SeverityWord` reads the diagnostic's one word |
| step 3 R1-3 (Result.Diagnostics coverage) | already fixed | the OO lane's dated file `2026-11-19-step-6b-oo` "Leads carried" |
| step 4a R2-1 (DirectiveEncounter wording) | already fixed | `git grep "first direct" ConditionalCompilationResult.cs` finds nothing; the type is `StateEncounter` |
| step 4b R1-1 (section 11 step 4b's twelve rows), R1-2 (Listing2023Tests' own Prologue) | already fixed | Listing2023Tests reads `_cases.Prologue` (lines 95, 222) |
| step 6b-2 R1-1, step 6b-3 R1-1, review-leads-slices-2-and-3 items 1 and 2 | already fixed (slice 5) | DESIGN-listing.md section 6.7 names `ProcedureNames<T>`; slice 3's paragraph carries slice 5's correction |
| step 6b-4 R2-1 (RangeCollationFields "rule 2") | already fixed | RangeCollationFields.cs's header cites §2023:14.7.8 GR2 |
| step 6b-6 R1-1 (`ContainingDefinitionNames`), R1-2 (inventory prose) | already fixed (slice 7) | `git grep ContainingDefinitionNames -- src` finds nothing; SR-12.3.8.3-15 names `ProgramPrototypesOf` only as its retired witness |
| step 6b-7 R1-1 (banner), R1-2 (PB2418) | already fixed (close session) | the banner names slice 7; `kb/Work/PB2418/2026-11-19-signature-tables-moved-into-symboltable.md` |
| step 6b-oo R2-1 (rule (2)'s companion and with-expressions) | already fixed (close session) | NameTableOwnership.cs:350 refuses `WithExpressionSyntax` and `DefaultExpressionSyntax` |
| step 7 R1-1 (the slot counter compares counts only) | already fixed (slice 7) | NameSlotFixture.g4's `misplaced` rule |
