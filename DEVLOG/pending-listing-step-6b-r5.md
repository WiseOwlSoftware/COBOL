## PB5932: the source listing, step 6b remainder slice 5: the last names move into the symbol table, every data entry is made where it is declared, and the baseline is gone

Step 6b moves every name the compiler resolves into the one symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1; kb/Work
PB5932). It landed by name class behind a shrink-only baseline of what had not moved. This slice is the last. It moves the 24
comparisons the baseline still held and makes the last entries, those of the data-names and condition-names, where they are declared.
The baseline is empty, so the slice deletes it and the code that read it. `NameTableOwnershipDriftTests` now holds its five rules with
no exceptions, and kb/Work PB5932 is landed.

**What changed.**
- The compilation group's FUNCTION-ID units are keyed by user-function-name in the group tier (`SymbolTable.UnitNames`). The same-word
  duplicate (COBOL1508) and the repository search by word (§2023:8.4.6.7) ask it. The OO pair of the externalized-name check asks the
  class-name tier (`ClassNames.DeclareOneWord`).
- The parameterized expansion's names are a table of the group tier (`SymbolTable.ExpansionNames`). This settles the question the OO
  lane left open: the expansion's names belong to the group (§2023:8.4.6.4, §2023:9.3.12), but the class table is built from what the
  expansion makes, so they need a table of their own. The skeletons are looked up by the written word's context, and the formals and
  specifiers of a definition are compared by the table.
- A `>>TURN` directive's file-name is matched against the file the statement references, as an identified `FileModel`
  (§2023:7.3.25.4 GR6, GR8). The implicit enable of an exception-checking PERFORM carries the file its WHEN phrase identified
  (§2023:14.9.28.4 GR14).
- A refused RECORD KEY or ALTERNATE RECORD KEY operand is marked on its own carrier. The key screen used to skip every operand that
  shared the refused one's spelling.
- Every data description entry enters its symbol table entry where the binder declares it. That covers FILLER, type declarations,
  TYPE and SAME AS copies, level-66 and level-88 entries, a CD's record area and an FD's implied record. `DataEntryOf` answers a
  data-name's or condition-name's entry for the occurrences step 7b will record. The finish now only places these entries.
- `string-parameter-members.json`: the three `FunctionsByWord` entries said their string was an externalized name. It is the
  REPOSITORY specifier's user-function-name, and the reasons now say so.

**Proof.** `python scripts/arch/compare_oracle.py --no-build --base 111f7fd0f5` reported `=== ARCH-ORACLE: IDENTICAL — 22651 cases ===`
on the moved comparisons. It reported the same line again with the data entries in place and the symbol table finished for every
compilation (both listing gates forced on for that run). So no data entry in the population is missing, entered twice or misplaced.
Removing one `EnterConditionName` fails the planted programs with "the Condition 'N-ZERO' entered no entry". Putting back
`TurnState`'s comparison turns rule (5) red. The text and JSON listings of IX113A, NC105A, NC218A and SM201A, with every section, are
identical to the parent's except for the output path and the instant.

**Measured** (section 9; the benchmark README's "Step 6b, remainder slice 5"). The data entries are now made for every compilation.
They put IX113A's `None` row 0.29 to 0.50 MB over its parent's, a cost the `None` budget names (one entry and one identity-index slot per
declared name). The other programs stay within their run-to-run spread. The first build's `All` row was 0.84 to 0.94 MB over the
parent's. The finish had listed every entry before choosing the few it writes. It now lists only those, and `All` is 0.13 to 0.26 MB
over. Against the parent of slice 1, the whole step's `None` rows are lower on IX113A and SM201A and within the spread on NC105A and
NC218A.

**Leads.** Every LEAD of the 32 review files, and the two of kb/Work PB5932's `2026-11-19-review-leads-slices-2-and-3`, was already
fixed on this tree (kb/Work PB5932's dated file `2026-11-20-step-6b-r5` holds the table). Step 7b, the occurrences and the
cross-reference, now finds an entry for every class.
