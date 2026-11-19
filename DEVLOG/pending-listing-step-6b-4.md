## PB5932: the source listing, step 6b slice 4 - the CONFIGURATION SECTION names move into SymbolTable with their resolution

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, landing
by name class behind a temporary shrink-only baseline (kb/Work/PB5932/2026-11-18-land-by-class.md). This entry lands slice 4, from
`listing-step-6b-4`: the CONFIGURATION SECTION class of the progress table, shown behaviour-neutral by the architecture oracle, and the
baseline lowered from 294 to 279.

**The CONFIGURATION SECTION names.** `SymbolTable.Configuration.cs` owns the names §2023:8.4.6.1 lists as declared within a
configuration section: switch-status condition-names, class-names, alphanumeric and national alphabet-names, symbolic-characters,
ordering-names, dynamic-length-structure-names, locale-names and mnemonic-names, each table keyed by `CobolName`, with the containment
depths §2023:8.4.6.2.1 3) weighs, the symbolic-characters a SPECIAL-NAMES paragraph declares ahead of its clauses and the inherited ones
a nearer declaration hides. `ConfigurationNames<T>` and its file are gone from the binder, `ConfigurationSlice` keeps the configuration's
values but no name table, and `InheritedGlobals` keeps no symbolic-character set. `NameClass` gains one member per class, and every
reference is `SymbolTable.Identify(written, class, report)`.

**What the code proved about the design** (section 6.7.1, "Step 6b, slice 4"):

- `report` already drew the line `ConfigurationNames.TryUse` and `TryPeek` drew: a reference is a §2023:8.3.2.2 use of the word in the
  element it is written in (kb/Work PB3153), a probe is not. So `Identify` records that use, and `DataBinder.RecordConfigurationNameUse`
  has one caller.
- A contained program and a class half inherit the names (§2023:12.3.4 GR1): `ConfigurationStep.Produce` takes the unit and calls
  `SymbolTable.InheritConfigurationNames(container)`.
- The written word travels, never its text: `ChoiceIndicators.AlphabetPair` answers the alphabet-name words, and the class condition,
  a bare EVALUATE class-name, the IN phrase of a THROUGH range, the STANDARD-COMPARE and locale arguments, the mnemonic registry and
  `SymbolicOf` take the word. A COLLATING SEQUENCE clause's alphabet-name is read where written into a `CollatingAlphabet`, which the
  one class test and a file's key resolution read after the bind; the binder keeps it, and the model keeps the names as written.
- The THROUGH range's alphabet rode the bound node as a NAME that the C# renderer looked up in a name table of the data binding. It now
  rides as a `RangeCollationId`, so the backend looks no name up. Review round 1 found the first form, the carrier's POSITION in the
  defining unit's list, wrong across units: a container's GLOBAL `88 OK VALUE "B" THRU "A" IN AL` tested in a contained program read
  the contained program's carrier at that position (a wrong answer) or none (the compiler failed), and the name-keyed table before it
  had weighed such a range natively. The id is now an identity with the sequence kind and ordinal; `RangeCollationFields` finds it in
  the unit's carriers and then its containers', reaching a container's field through the container's type, and the new case
  `pb5932_global_condition_range_in_alphabet` pins it. The same round fixed 10 orphaned doc comments and filed the other 36 as PB5944.
- Three converted members keep a diagnostic's strings beside the word's context, so rule (3) classifies them:
  `string-parameter-members.json` 122 -> 123.

**Proved.** `python scripts/arch/compare_oracle.py`: `=== ARCH-ORACLE: IDENTICAL — 22642 cases ===` after the move and after the
range-collation and file-collating restructuring. `NameTableOwnershipDriftTests` was red naming exactly the 15 baseline entries the move
retired (rule (1) 3, rule (3) 5, rule (5) 7), the 2 retired classification entries and the 3 converted members before the files were
lowered and classified. The `None` row of the front end and bind against the parent c7088a17b9, twice each, alternating: every branch row inside the parent's two
runs or within 0.04 MB of them (the benchmark README's step-6b table, slice 4).

**What remains:** the data-name unit, the report class, the program/function/repository class and the OO class (kb/Work PB5932's dated
file `2026-11-19-step-6b-slice-4`).
