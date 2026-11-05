## Entry 3128 — 2026-11-04 18:39 PST — The VALUE literal class screens answer at a TYPEDEF template, once, instead of at each TYPE user

**PB4678.** `CheckValueLiteralClassesPass` walked `Forest.Roots` and the condition index, never `Forest.TypeDecls`. An oversize national
literal on a USAGE NATIONAL numeric-edited TYPEDEF (`01 T1 TYPEDEF PIC 99B99 USAGE NATIONAL VALUE N"ABCDEFGHIJ".`, ISO 13.18.63.3 SR5
sentence 2) was refused only at each `TYPE T1` user, anchored at the user, and compiled clean if no item used the type. The SR7 class
screen (`VALUE "ABC"` on the same typedef, COBOL5984) was the same hole. The alphanumeric twin was already refused once at the declaration.
Both probes reproduced on a fresh build.

**Reach wider than the note.** Two more inputs of the same loop missed a template. The Format 2 table literals were read off
`TableValuePlan`, which `ResolveTableValuesPass` never builds for a type declaration, and the condition-names were read off
`Forest.Conditions`, where a template's own are kept out on purpose (13.18.58.4 GR1 makes them part of the declaration but the index is the
referenceable set). So `88 M-OK VALUE "ABC"` under a national typedef member was clean unless the type was used.

**Fix.** `Run` walks `DataBinder.DeclaredForest()` (roots plus the templates and their subtrees) and asks each item's `Own88s`. The
subject of both screens is the entry that wrote the literal, so an item whose VALUE was copied (`ValueCopiedFrom`) and a condition-name a
clone reproduced (`ClonedFrom`) are skipped: the verdict is given once at the declaration, not again per reference site. A VALUE the TYPE
reference site writes itself is still asked there. The table literals now come off the written clause (`TableValues`), which also screens a
phrase the plan dropped as mis-shaped. The other passes that walk the roots alone were swept: the sign, usage, type-expansion and REDEFINES
passes already reach the templates.

**Goldens.** `13.18.63-VALUE/pb4678_typedef_value_national_edited` (2002-2023: exact-size values, a group-inherited national member, a table
literal and a condition-name, used through two TYPE clauses) and the refusals `pb4678-typedef-unused-value-oversize-national` (COBOL0898),
`pb4678-typedef-unused-value-class` and `pb4678-typedef-group-member-condition-class` (COBOL5984). SR-13.18.63.3-5 and -7 stay CONFORMS with
the new witnesses; no GAP moved.

**Gate.** The first gate was RED on one drift test only, `BinderMemberReadersDriftTests`: the pass now reads `DataBinder.DeclaredForest`
instead of `State`, and the reader list says so (a new reader is a decision; the list was updated). Re-gate:
`=== BUILD-LOCAL GATE: GREEN — Conformance 21,718/21,718 · Unit 39,883/39,883 · Characterization 37/37 (run 000002-20261105T021630Z) ===`.
The 12 pb4678 conformance results (the positive at 2002, 2014 and 2023 and the three refusals at each) all passed in `Conformance.trx`.
Self-review (architecture, full code, performance, duplication): one walk now serves the item, table and condition-name screens, and the old
second condition loop is gone; the forest is the existing `DeclaredForest`, not a new enumeration; per-item allocation is the same
(`Distinct` over the written literals).

