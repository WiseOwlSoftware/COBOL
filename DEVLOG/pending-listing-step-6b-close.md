## PB5932: the source listing, step 6b, the closing slice's first session: the data-name, condition-name and screen-name tables move into the symbol table; 86 baseline entries remain

Dispatched as the closing slice of step 6b (the one store of names, docs/rearchitecture/DESIGN-listing.md sections 6.7.1 and 11): move
whatever the shrink-only baseline of `NameTableOwnershipDriftTests` still held, then empty and delete it. It started at 122 entries on
origin/main 6fda8efbac, after every lane had landed, and ends at 86. It does not close the step, and the design now says so.

**What moved** (four commits, each behaviour-neutral by `python scripts/arch/compare_oracle.py --no-build`: `=== ARCH-ORACLE: IDENTICAL —
22647 cases ===` after every source change):

- The element's and each method's data-name and condition-name multimaps and the refused SCREEN SECTION's names left `ForestSlice` and
  `OoMethodDataScope` for `SymbolTable.DataNames.cs` and `SymbolTable.MethodNames`, keyed by `CobolName`. The model holds no name table
  now. `SymbolTable.MethodDeclaresWord` is the one §2023:11.7.4 GR5 question ("does this method declare the word"); the two halves it
  replaced (`DataBinder.MethodDeclares`, `OoMethodDataScope.IndexesDeclare`) are deleted. A SELECT WHEN condition-name is resolved
  from the record that carries it.
- USE's §2023:14.9.49.3 SR8 and SR14 registers and the exception PERFORM's WHEN census (§2023:14.9.28.3 SR14, SR15) key the file a
  file-name identified, not its spelling.
- One end-marker question for END PROGRAM, END FUNCTION, END CLASS, END INTERFACE and END METHOD (`SymbolTable.EndMarkerAgrees`); the
  flag pass identifies a written file-name through the table; the version pass keys its level-01 facts by the table's `CobolName`; a
  subscript's word is pre-filtered against an INDEXED BY name by `SymbolTable.SpellsIndexName`.
- Ten comparisons that resolve no written name were classified (five words of the standard or the implementor, five externalized or
  compilation-variable names).

**What remains** (kb/Work PB5932's dated file `2026-11-19-step-6b-close`, by class): the data-names' and condition-names' resolution by
text (22 lookups, about 100 call sites), their declaration sites, the §2023:8.3.2.2 census, the compilation group's function units and
the parameterized expansion. The next slice converts the lookups to the typed `IdentifyDataName` the data-name lane named.

**Review leads carried:** the design's status banner (step 6b-7 R1-1) and PB2418's plan for two members slices 6 and 7 already moved
(R1-2) are fixed; rule (2)'s companion now refuses a `with` copy of a table-made answer (the OO lane's R2-1). Every other open lead was
already fixed on main (slice 7, commit 5e86899090; slice 5).

**Section 9:** the `None` row of the front end and bind, against the parent 6fda8efbac, twice each, alternating: IX113A, NC218A and
SM201A lie inside the parent's runs or within 0.04 MB of them; NC105A's two runs straddle the parent's (one 0.55 MB above, inside its
own run-to-run spread). No measurable allocation moved (the benchmark README's step-6b table).
