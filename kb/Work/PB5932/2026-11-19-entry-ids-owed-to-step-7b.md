## Owed by step 6b's remaining slices: each declared name's entry id (found by the early step 7b dispatch, w20262104l)

Step 7b was dispatched before step 6b's last slice and not built (kb/Work PB5925's dated file
`2026-11-19-step-7b-not-buildable-yet`). Besides the remainder `2026-11-19-step-6b-close` lists, the code proved one more piece of
the one store that section 6.7.1 of `docs/rearchitecture/DESIGN-listing.md` designs and no slice built: `SymbolTable.Declare`
"records the `Defined` occurrence and returns a `SymbolId`". Every class's declaration keys its symbol and returns nothing
(`git grep -n "SymbolId\b" -- src` finds nothing on 4b4ea135e4), and the entries are made after the bind by `SymbolTableFinish.Of`.

Step 7b joins an occurrence to its entry, and the identified answer is not the entry for most classes (a constant's `ConstantDef`
against the finish's `ConstantSymbol`, an ordering-name's string in `ConfigurationNames<string>`, a paragraph's per-lookup
`ParagraphDefinition(Pc)`, a report group's `(ReportModel, ReportGroupModel)` candidates), and the procedure, CONFIGURATION SECTION,
prototype, DEBUG-ITEM and OO classes have no entry. So the remaining slices carry, for every class (the moved ones too):
- the declaration makes the entry and its id where the binder declares the name, with the declaring word's position (the `Defined`
  occurrence), and the class's table keeps the id beside the symbol;
- `Identify` (and `ClassNames.Lookup`, `IdentifyIndexName`, the coming `IdentifyDataName`) answers the id with the symbol;
- `SymbolTableFinish.Of` reads the entries the bind made, with each attribute still read off its symbol once the bind is finished
  (section 6.6), rather than declaring them a second time from the frozen model.
The last slice may not empty the baseline while a class Identify resolves answers no entry id.
