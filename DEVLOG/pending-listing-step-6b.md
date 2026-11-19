## PB5932: the source listing, step 6b slice 1 - SymbolTable becomes the one store of names: the drift test, the table's place, Identify, the cd-names and the file-names, behind a shrink-only baseline

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names, so
that step 7b's occurrences can be recorded at the one place a written name becomes a symbol. This entry lands slice 1 of it,
from `listing-step-6b`: the step lands by name class behind a temporary baseline (the decision below), and the classes
not yet moved are held in that baseline, which only shrinks and which the last slice deletes.

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
unit (kb/Work PB5932's dated file `2026-11-18-step-6b-progress`).

**Landing by class (decision, kb/Work PB5932's dated file `2026-11-18-land-by-class`; design sections 6.7.1 and 11 corrected).**
The remainder is the binder's whole name resolution, which one branch would carry for many sessions against a moving main, so
step 6b lands in slices. `NameTableOwnershipDriftTests` reads `tests/WiseOwl.Cobol.Tests.Unit/Listing/name-table-ownership.baseline.json`,
the 325 findings of rules (1), (3) and (5) not yet moved (56 tables, 63 lookups, 206 sites), under a ceiling in the test. A
finding neither classified nor baselined is red (nothing new may bypass the table); a baseline entry no longer found is red (each
slice lowers it in the same change); the baseline names only rules (1), (3) and (5) and shares no key with a classification.
Rules (2) and (4) take none. The test is green, 9 of 9; each red was shown firing on the product (an entry removed, a stale entry
added) and over planted findings. The last slice empties and deletes the baseline and the code that reads it, measures the
no-listing path against its parent and flips PB5932.

**Measured (section 9).** Slice 1 changes how every compilation resolves a cd-name or a file-name, so it measured the front end
and bind with no listing against its parent (`bfb8500569`), twice each, alternating: every row is within the parent's own
run-to-run spread or at most 0.23 MB above it (IX113A 68.87 and 68.91 MB against 68.88 and 68.71; SM201A 15.31 and 15.29 against
15.37 and 15.30), the README's step-6b table. Each later slice measures its own row the same way.

**Also fixed on the way.** After merging main, the tests tree's walk guard could not follow the analyzer's source root (a
parameter from another file); moving the listing into the analyzer then made `drift_rules.py` refuse the class's `Sweep` trait (a
helper's paths never make a sweep). The drift test now lists the tree it sweeps and the analyzer compiles what it is given.
