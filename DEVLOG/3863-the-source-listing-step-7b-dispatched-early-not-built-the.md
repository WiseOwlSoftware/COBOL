## Entry 3863 — 2026-11-19 15:17 PST — The source listing, step 7b dispatched early: not built; the entry ids it needs are owed by step 6b, and the THROUGH phrase's rules cited by designator

Step 7b of `docs/rearchitecture/DESIGN-listing.md` (the occurrences and the cross-reference) was dispatched (w20262104l, branch
`listing-step-7b`) from origin/main 4b4ea135e4, after step 6b's closing slice's first session. The design orders it after step
6b's LAST slice, and on that tree it cannot be built as written:

- **Step 6b has not landed.** `name-table-ownership.baseline.json` holds 86 findings, and `SymbolTable.Identify` has no arm for a
  data-name or condition-name (`SymbolNamed` throws "not yet a class SymbolTable.Identify resolves" for both): the 22 text
  lookups of step 6b's remainder are still how a data-name becomes a symbol. The class the cross-reference, the store kinds
  (`StorePositionsOf`), the pairing test and the statement-line margin are about would record no occurrence.
- **No entry exists while the bind runs (a design gap the code proves).** Section 6.7.1 has `SymbolTable.Declare` make the entry and
  return a `SymbolId`, and `Identify` append to that entry. Step 6b built each class's declaration as a table of symbols with no id
  (`git grep -n "SymbolId\b" -- src`: nothing), and the entries are made after the bind by `SymbolTableFinish.Of`. The identified
  answer is not the entry for most classes (a constant's `ConstantDef` against the finish's `ConstantSymbol`, an ordering-name's
  string, a paragraph's per-lookup `ParagraphDefinition(Pc)`, a report group's candidates), and the procedure, CONFIGURATION SECTION,
  prototype, DEBUG-ITEM and OO names have no entry. A join by answer would be one adapter per class beside the table.

Decided: step 7b waits; step 6b's remaining slices give each declared name its entry id as its declaration moves, and step 7b then
joins every occurrence to its entry by id (kb/Work PB5925's dated file `2026-11-19-step-7b-not-buildable-yet`, PB5932's
`2026-11-19-entry-ids-owed-to-step-7b`; the design's sections 6.7.1 and 11 and its status banner say so).

**The review leads.** Every LEAD of the listing steps' review records and of PB5932's `2026-11-19-review-leads-slices-2-and-3` was
re-checked on 4b4ea135e4. One was still true, step 6b slice 4's R2-1: the THROUGH phrase's two rules cited as "§2023:14.7.8 rule 1"
and "rule 2", which `cite.py` refuses ("'rule 1' is not a rule designator"). Each site now names GR1 or GR2 (`cite.py --check
"§2023:14.7.8 GR1" "the range of values includes literal-1, literal-2"`: OK, the text is in GR1): seven in `src` (DataBinder,
SpecialNamesBinder.Alphabets, ConditionRenderer, COBOL1998's message in DiagnosticCatalog), two inventory notes, four conformance
cases' comments and DATA_MODEL_DESIGN.md. The rest were already fixed by the 6b slices.

