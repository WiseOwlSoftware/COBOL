## PB5932: the source listing, step 6b remainder slice 4: the data division's declarations move into the symbol table

Step 6b moves every name the compiler resolves into the one symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1; kb/Work
PB5932). It lands by name class behind a shrink-only baseline of what has not moved. Remainder slices 1 to 3 built parts A and B: no
lookup outside the table takes a data-name's text. This slice builds the first half of part C, the declarations the data division's
binders made by comparing names themselves. The baseline falls from 52 to 24, and no table keyed by a COBOL name is left outside the
symbol table.

**What changed.**
- The entry order a constant's length phrase reads (kb/Work PB1231) kept a table from each data-name to the places its entries stand.
  The table now keeps the names (`SymbolTable.DeclareDataEntry`, `DataEntriesDeclaring`); the binder keeps where each entry stands,
  keyed by the entry.
- The §2023:8.3.2.2 census of user-defined words is nested in the table (`SymbolTable.UserWordCensus`, keyed by `CobolName`). The
  declaration funnel's once-per-word refusals and the REPOSITORY specifier that names its own unit (§2023:12.3.8.3 SR11, SR15) are
  asked of the table.
- The declaration questions the data division asks of its own entries are the table's: the screen-names, the COBOL-85 index-name
  uniqueness (§1985:VI.5.8.3 SR14), a file-name described twice, a SAME clause's distinct file-names, a DATA RECORDS name
  (§1985:VII.3.5.3 SR1), a CD entry's data-names (§1985:XIV.2.2.3 SR6) and a PROPERTY subject that needs qualification
  (§2023:13.18.42.3 SR3).
- The procedure division header's USING and RETURNING operands, the ADDRESS OF set and a linkage REDEFINES are resolved among the
  element's LINKAGE records by the table (§2023:14.2.2 SR1), for programs and methods alike.
- CORRESPONDING pairs the subordinates of its two operands by the table's key (§2023:14.7.6).
- The externalized-name uniqueness of EXTERNAL subjects (§2023:13.18.22.3 SR2) is classified as a run-unit identity, not a name lookup.

**Proof.** `python scripts/arch/compare_oracle.py --no-build --base 75f9b4fd58`: `=== ARCH-ORACLE: IDENTICAL — 22651 cases ===`.
`NameTableOwnershipDriftTests` is green at the lowered baseline, and red when one moved comparison is put back. The new
`SymbolTableDeclarationsTests` derive each expected value from §2023:8.1.3.2 GR3's case fold and the cited rule.

**What remains** (kb/Work PB5932's dated file `2026-11-20-step-6b-r4`): the compilation group's program and function units, the
parameterized expansion, a TURN directive's file operand, the refused record-key set, then part D, the data-names' and
condition-names' entries made where they are declared.
