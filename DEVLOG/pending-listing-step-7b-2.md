## PB5925: the source listing, step 7b - the occurrences and the cross-reference

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

**Left, each a lead in the dated file:** an index-name's store (no `Place`), the special registers' entries, a whole table's store, a
literal CALL target, the statistics' counts of names and uses, a `--permissive` first-declaration resolution, and an oracle corpus
beyond 1985.
