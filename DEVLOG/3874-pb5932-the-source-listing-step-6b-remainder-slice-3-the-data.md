## Entry 3874 — 2026-11-20 04:13 PST — PB5932: the source listing, step 6b remainder slice 3: the data-name candidate sets become the symbol table's, and every caller hands it the written words

Step 6b moves every name the compiler resolves into the one symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1; kb/Work
PB5932). It lands by name class behind a shrink-only baseline of what has not moved. Remainder slices 1 and 2 moved the procedure
division's data-names and the file clauses' operands. This slice finishes part B: no member outside the table, and no member of the
table a caller can reach, answers a data-name's text with its items any more.

**What changed.** The §2023:8.4.2.2 candidate set of a written data-name, the set within one record, the data description entry
clause's implicit qualification (§2023:8.4.2.2.1 SR5) and the one qualifier walk (§2023:8.4.2.2.3 SR4) were `DataBinder` members that
took the name as text. They are now private to `SymbolTable` (new file `SymbolTable.DataNameCandidates.cs`), and so are the raw
`TryResolve`, `TryResolveUnqualified` and `NearestDeclaringData`. A caller hands the table the written words: the procedure division a
`WrittenQualifiedName` (the sum counter's and the constant-name's ambiguity counts, the report group's count, the unidentified
reference's wording, a SORT table key, the storage-claim scan), and the data division the `WrittenDataName` its model now keeps for
OCCURS KEY, RENAMES, SAME AS and the report section's CONTROL, SOURCE, SUM and CODE operands. The seven word-class probes ask a yes/no
question, `DeclaresDataName`. A REDEFINES clause's data-name-2 is read off the redefiner by the table, and the record layout follows the
resolved redefinition target instead of matching the written name among the siblings again. The baseline fell from 63 to 52 entries:
one table and 51 comparison sites, no lookup.

**A lead decided.** Remainder slice 1 left open whether the unqualified and the qualified data-name should share one rule. They already
gave one answer: with no qualifier the walk keeps every candidate, a spelling's candidates are never lapsed type-declaration members
alone, and each item is listed once. So one private method answers both, an unqualified reference reads the table's list without a
copy, and the sum counter's count asks the same method as the ordinary lookup.

**Design corrections.** The CAPACITY IN data-name, which slice 2 listed among the operands kept as text, declares the register
(§2023:13.18.38.3 SR30) and resolves nothing. A REDEFINES target is resolved from the item that carries it (§2023:13.18.44.3 SR2, SR6,
SR10). Sections 6.7.1 and 11 say so.

**Behaviour.** `python scripts/arch/compare_oracle.py --no-build --base 0c29eebc42` printed `=== ARCH-ORACLE: IDENTICAL — 22651 cases
===`. Unit 42233, Conformance 28201 and RepoDrift 187 tests passed.

**Measured.** The front end and bind's `None` row, two runs each against the parent 0c29eebc42, alternating: every branch row lies inside
the parent's two runs or below them but one (NC218A 30.50 MB against 30.44 and 30.42); IX113A is 67.66 and 67.60 MB against 67.78 and 67.81. No
measurable allocation moved.

**Review leads.** Lead 6b-r2 R2-1: DEVLOG 3873 headed a paragraph "Review leads fixed here" for four fixes its landing did not carry
(kb/Work PB5951 landed them on main first). That paragraph and kb/Work PB5932's record now say so. Lead pb5951 R4-1: the `All` row's
budget at the merged tree rested on an inference. Two full-compile runs at main 0c29eebc42 now show `All` +13.8 to +23.5 %
over `None` on the four programs, inside the 25 % budget on every run. Every other lead of the listing's review files was checked on main and is
already fixed (kb/Work PB5932's dated file `2026-11-20-step-6b-r3`, "Leads carried").

**Review round 1.** R1-1: four subsystem design docs (`FILES_DESIGN.md`, `DATA_MODEL_DESIGN.md`, `REPORT_WRITER_DESIGN.md`,
`DESIGN-architecture-review.md`) and, by the sibling sweep, `INTERPROGRAM_DESIGN.md` still named the moved `DataBinder` members as the
current resolver; each now names the table's methods. R1-2: reading a redefiner's overlay off `RedefinesTarget` made the layout depend
on when the REDEFINES pass sets it, and that pass ran after `CheckStorageLimitsPass`, which measures every item. The link is now its own
pass, `LinkRedefinitionsPass`, before the size limit, and `BindPipelineTests` pins the order (kb/Work PB5932's dated file
`2026-11-20-listing-step-6b-r3-review-r1`).

**Review round 2.** R2-1: the split had moved the report and clearing of an unfound data-name-2 after every link, so in `05 A. 05 A
REDEFINES ZZ. 05 C REDEFINES A.` C linked to the first A and drew a spurious SR10 error (COBOL2739); the link pass now reports and clears
each unfound clause before the next item is linked, and `RedefinesTargetTests` pins it (red on the split order, 2 of 2). R2-2: 15
code-locations in 10 rows of the 1985 and 2002 inventories still named the moved `DataBinder` members, and no gate saw them because
`EveryCodeLocation_ResolvesInTheTree` read the 2023 store alone. It now reads every edition's store; that found 16 more dead links in 15
older-edition rows, left by earlier changes, each re-pointed at its member's current home. R2-3: a stale cref in `DataBinder.RenamesEntry.cs`,
and by the sweep one in `ReportModel.cs` (`ReportSumAddend`), the only two unresolved crefs this branch added (kb/Work PB1322 holds the rest).

**What remains.** Part C (the declarations, the §2023:8.3.2.2 census, the group tier's program and function units, the parameterized
expansion) and part D (the data-names' and condition-names' entries).

