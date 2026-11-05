## The VALUE literal class screens answer at a TYPEDEF template, once, instead of at each TYPE user

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
