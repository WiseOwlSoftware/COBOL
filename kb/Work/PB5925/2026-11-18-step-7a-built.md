## Step 7a built (branch `listing-step-7`, w20262104h); step 7b waits for step 6b

Step 7 of `docs/rearchitecture/DESIGN-listing.md` was dispatched before step 6b, which it stands on, so it is split (kb/Work
PB5932's dated file `2026-11-18-step-7-split`): step 7a, the grammar half, is built; step 7b (the occurrences recorded by the
one `Identify`, `ReferenceOccurrenceDriftTests`, `CrossReferenceBuilder`, the PERFORM and CALL graphs, `ListingLine.Names`,
the statement-line margin, `xref`) waits for 6b. The design's corrections are marked "(step 7a)" in sections 3, 6.7, 6.7.1,
9, 10 and 11.

- `GrammarNameSlotDriftTests` and its counter `GrammarNameSlots` (Unit, `Listing/`): the slots, the 21 name rules and the 36
  reference rules computed from `CobolParserCore._ATN`, each rule's slot transitions counted against its classified label
  fields. Written first and red on the unlabelled grammar (commit 224a9f2e27: 269 rules, 452 slots); the fixture
  `Listing/Fixtures/NameSlotFixture.g4`, generated at test time with the vendored ANTLR jar and compiled in memory, shows
  each of five planted failures reported at its slot and a clean rule passing.
- The labels: 452 slots in the 11 parser grammar files, 281 `ref`, 73 `nn`, 64 `def`, 25 `name`, 9 `qual`; removing them
  gives the previous text byte for byte, so no parse changed.
- `tests/_shared/GrammarSource.cs`: the label's one form (`LabelPattern`) and the grammar's text without labels (`Read`),
  through which the nine Unit classes the labels broke now read; `scripts/gen-grammar-diagrams.ps1` drops a label.
- Docs: DESIGN-listing.md, DESIGN-frontend-grammar.md section 3.17 (the convention), DESIGN.md section 0.5, DOC_INDEX, the
  new-construct skill.
- Leads: `2026-11-18-grammar-text-readers` (about fifty other tests read the grammar privately) and
  `2026-11-18-resolution-dependent-slots` (four slot kinds whose class the oracle must decide by resolution).
- Measured (DESIGN-listing.md section 9, the benchmark README's step-7a table): the label fields cost every parse, about
  +0.3 MB (0.4 to 0.5 %) per front end and bind on IX113A with no listing, the same on two runs against the parent's two;
  the budget now names them beside the line-kind arrays. Measurements: E:/COBOL-coord/scratch/my-20261115/listing-build/w20262104h/
  bench-parent.md, bench-parent2.md, bench-step7.md, bench-step7-2.md.
