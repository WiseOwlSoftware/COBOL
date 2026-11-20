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
changes: see the commit messages and the measurements below for each verdict line.

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

**Section 9** (the `None` row of the front end and bind, branch against its parent 8dd07d2eab, twice each, alternating): MEASUREMENT.

**What enters no entry, and why it is left:** a REPOSITORY program-specifier whose literal-3 was refused, and a CAPACITY register SR30
refused: each declares a word no reference resolves to and binds with an error, so step 7b's completeness oracle (clean binds only)
never meets one. The data-names and condition-names enter with their declarations in the data-name lane.

### Leads carried

LEADS.
