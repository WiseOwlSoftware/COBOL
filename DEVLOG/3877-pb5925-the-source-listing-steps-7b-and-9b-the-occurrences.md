## Entry 3877 — 2026-11-20 11:51 PST — PB5925: the source listing, steps 7b and 9b - the occurrences and the cross-reference, and the close-out

Step 7b of `docs/rearchitecture/DESIGN-listing.md` is built on step 6b's one store of names and its entry ids (branch
`listing-step-7b-2`, kb/Work PB5925's dated file `2026-11-20-step-7b-2-built`).

**The occurrences.** Every identification funnel of `SymbolTable` records the reference the program wrote as an occurrence of the
entry its declaration made, joined by `SymbolId`, under its `report` flag (a candidate set only when it has one member): `Identify`,
`IdentifyIndexName`, `IdentifyDataName`, `IdentifyConditionName`, `IdentifyWithin`, `IdentifyEntryClauseOperand`,
`IdentifyRegionOperand`, `IdentifyHeaderOperand`, `IdentifyDataRecord` and the group tier's `ClassNames.Lookup`. One occurrence per
written position. A data division clause operand carries where it is written (`WrittenDataName.Line`, `Column`, `Length`). Recording
happens only for a listing that asks for the cross-reference (`EditionContext.RecordsReferences`), so a compilation without one does
no new work.

**The classification.** `ReferenceResolver.Linked` links the place instance built for each written reference to its occurrence;
`StatementBinder.BindStatement` classifies the occurrences since its mark through the new `BoundStores.StorePositionsOf`: written
(`Write`, `WriteInPart`), modified (`ReadWrite`) or read, with the statement's position and verb (the transfer kind of a PERFORM, GO,
ALTER or SORT reference).

**The cross-reference.** `--listing-sections xref` (`ListingSection.CrossReference`, in `all`, not in `standard`): every declared name
sorted by its Annex C fold with its definition and its references, a name never referenced flagged, the copy members, the PERFORM and
CALL graphs (`ListingCrossReference`, built by `CrossReferenceBuilder`). Each source line carries its references
(`ListingLine.Names`), and a statement line's margin lists the lines that define the names it uses (`DEF 12 15`). The text listing
prints it (`CrossReferenceText`), the HTML page links each name on a source line to its definition and each reference in the
section to its line, and the JSON carries it (schema 1.0, regenerated).

**Proved.** `ReferenceOccurrenceDriftTests` (Unit), over the 374 NIST programs the golden run compiles, reads every name slot's class
from step 7a's grammar labels: every `ref` slot has exactly one occurrence at its first token, every occurrence stands at a `ref`
slot, every `def` slot's name is an entry, and every store position pairs with an occurrence of its statement. Mutation showed both
halves able to fail (369 of 374 programs red each). The oracle's first run found what no funnel recorded, each fixed at the root: a
REDEFINES clause's data-name-2 (recorded when `LinkRedefinitionsPass` links it), an END PROGRAM header's name (a reference to its
unit), and the binder's temporaries the pairing must skip. `ListingCrossReferenceTests` (Conformance) holds a planted program's every
reference, use, graph edge, line name and margin to its source text; `CrossReferenceKindDriftTests` holds the two kind maps total.

**Corrections to the design** (section 6.7.1, "Step 7b, as built"): the place link is the resolver's, since the qualifiable classes
answer typed candidate sets and `PlaceBuilder` names no binder type; `StorePositionsOf` also asks the places linked since the mark,
since `BoundReferences` does not enter a tuple (`BoundSetConditions`); the oracle is in the Unit assembly over the NIST corpus; a
definition's reference stands at its entry.

**Review round 1** (kb/Work PB5925's `2026-11-20-listing-step-7b-2-review-r1`): every store occurrence carries its true kind. An
index-name's store (SET UP BY, SET TO, a PERFORM's or a SEARCH's VARYING index) is found through the receiver node, which
`ExpressionBinder.IndexTargetOf` links to the index-name's occurrence, and a whole table's store (a table SORT, INITIALIZE of a dynamic
table) through the place `ReferenceResolver.WholeTable` links; `StorePositionsOf` asks every site the tree holds, so a store with no
linked occurrence turns the pairing red (shown by mutation for both). A CALL or CANCEL literal-1 is a reference slot the grammar labels
`lit`: its occurrence stands at the literal, of the program it reaches by the scope of a program-name or of the external program of that
name (`ExternalProgram`), and is the CALL graph's edge (a 1985 CALL had none). An identification funnel asks a data entry only for a
listing that asks, and a copy member's sort key is its folded text-name.

**Left, each a lead in the dated file:** the special registers' entries, `BoundReferences` over a value tuple, the statistics' counts of
names and uses, a `--permissive` first-declaration resolution, and an oracle corpus beyond 1985.

**Step 9b, the close-out (kb/Work PB5925, PB5953, PB5954, PB5962).** The HTML page's name links, built with step 7b, are held by a test that
reads every kind of name off `ListingSymbolKind` (mutation: red with the link removed), so a kind added later gets its links checked.
The `All` row was over its 25 % budget with the cross-reference (+24.7 to +31.2 %); its share is cut at its roots: the statement funnel's
classification asks the places the exhaustive visitor asks (`BoundStores.StorePositionsAmong`: one survey pass, then the kind of the
places that stand for an occurrence still to classify) in place of a walk of every statement's fields and a visitor per place, with the old
walk kept as the pairing test's behind `EditionContext.VerifiesStorePairing`; the classification of all 215,230 occurrences of the 374 NIST
programs is byte-identical before and after. The builder makes each `ListingReference` once, the text and the page write numbers and links
with no string of their own, a symbol's link is made once, IL integer and branch-target operands are named once, and `ListingSpan`,
`ListingNameUse` and `ListingInstruction` are structs. The cross-reference's marginal share of the `All` row went from 15.8 to 5.8 MB (IX113A,
thread-level) and stands at 0.25 to 4.7 MB at the merged head; `All` over `None` on a full compile is +19.3 % (IX113A), +22.8 % (NC105A),
+25.5 % (NC218A) and +37.4 % (SM201A) as the mean of two runs, and the part still over is not the cross-reference's: kb/Work PB5962 (the
benchmark README's step-9b table). Every section of the design was checked against the code and the records; the parts the design leaves are
each a filed item, PB5956, PB5957, PB5959, PB5960, PB5961 and PB5962 beside the older PB5930 and PB5952, and kb/Work PB5954 records the
decision that PB5925 is landed with them. PB5925 and PB5953 are `landed`.

