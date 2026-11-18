## PB5932: the source listing, step 6b (in progress) - SymbolTable becomes the one store of names: the drift test, the table's place, Identify, the cd-names and the file-names

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, so
that step 7b's occurrences can be recorded at the one place a written name becomes a symbol. This entry records the first
session of it, on `listing-step-6b`; the step is not finished and the branch is not landable until
`NameTableOwnershipDriftTests` is green (the design's landing condition).

**What was built.**
- `NameTableOwnershipDriftTests` (Unit), written first and red, with its analyzer `NameTableOwnership`: the design's five rules
  applied by symbol through Roslyn's semantic model to a compilation of the compiler's own sources (the source generator run as
  in the build, and the compilation asserted error-free so no site is unbound). Rule (1) no name table outside `SymbolTable`;
  (2) no lookup by `CobolName`, and no `CobolName` or `IdentifiedName` made outside the table; (3) no string-taking member that
  gives a binder symbol unless classified in `string-parameter-members.json`; (4) `SymbolTable.Declarations` answers only
  facts; (5) no `CobolNames.Same`, `Comparer` or name-property equality outside the table unless classified in
  `name-comparisons.json`. A planted compilation shows each rule able to fail and nothing reported inside the table.
- Red on the base (4527cb3dcd), matching section 6.7.2's evidence: 65 tables (evidence 65), 188 string-taking members (194, a
  superset), 299 comparison sites (294 at 7bc7e33469), rules (2) and (4) red.
- `SymbolTable` moved from the model to the binder layer (`Binding/SymbolTable/`, a partial class, one file per name class),
  ending three tolerated model -> binding edges. `SymbolTable.Declarations` holds the data division's declaration questions
  (from `DataBinder.NonDataNames.cs`, deleted); their answer, `NonDataDeclaration`, already named no symbol and is
  `DeclarationFact`. `CobolName` is the key; `Identify(written, wanted, report, reach)` answers an `IdentifiedName`.
- The classifications: 122 members whose string is no COBOL name and 86 comparison sites of a staying class, each with its
  reason; both files only shrink.
- Two name classes moved in with every reference through `Identify`: the cd-names (`CommunicationSlice`'s map) and the
  file-names (`FileSlice`'s map, `FileNameScope` deleted with `DataBindState.VisibleFiles`, `InheritedGlobals`' map and hidden
  set, `FileControlBinder.SelectedFiles`).

**Measured.** Now: rule (1) 56 tables, rule (3) 63 lookups, rule (5) 206 sites; rules (2) and (4) green. Every source change was
shown behaviour-neutral by `python scripts/arch/compare_oracle.py` against the base: `=== ARCH-ORACLE: IDENTICAL — 22641 cases ===`.

**Design corrections** (section 6.7.1, "Step 6b, as built so far"): the table lives in the binder layer; `DeclarationFact` is
the fact the questions answered, not the drafted `(Exists, Conflict, Class, DeclaredAt)`; `IdentifiedName` is made only by the
table by a rule, since C# cannot give an outer type a nested type's private constructor; `Identify` takes a reach for a
position that names only the element's own entries; a `bool` question and an ordered enumeration are the table's non-symbol
answers. The estimate was wrong in size: each name class moves with its resolution code, and the data-name class moves as one
unit (kb/Work PB5932's dated file `2026-11-18-step-6b-progress`, with a lead for landing by class behind a ratchet).
