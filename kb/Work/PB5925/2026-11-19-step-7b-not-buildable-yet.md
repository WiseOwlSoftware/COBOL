## Step 7b dispatched (w20262104l, branch `listing-step-7b`) before what it stands on: what the code proved, and the order it needs

Step 7b of `docs/rearchitecture/DESIGN-listing.md` (the occurrences and the cross-reference) was dispatched from origin/main
4b4ea135e4, after step 6b's closing slice's FIRST session landed. The design orders step 7b after step 6b's LAST slice
(section 11: "Step 7b branches after the last slice"; kb/Work PB5932's dated file `2026-11-18-step-7-split`). Read on that
tree, two facts stop step 7b from being built as the design writes it, and a third is a design gap the code proves.

**1. Step 6b has not landed: the data-names and condition-names do not resolve through the table.**
`tests/WiseOwl.Cobol.Tests.Unit/Listing/name-table-ownership.baseline.json` holds 86 findings (rule (1) 1 table, rule (3) 22
lookups, rule (5) 63 sites), and `SymbolTable.SymbolNamed` (`SymbolTable.Identify.cs`) has no arm for `NameClass.Data` or
`NameClass.Condition`: its switch throws "not yet a class SymbolTable.Identify resolves" for them. The typed
`IdentifyDataName(WrittenQualifiedName, ...)` the data-name lane named does not exist; the 22 text lookups
(`SymbolTable.TryResolve`, `TryResolveUnqualified`, `TryResolveCondition`, `NameResolution.FindItem`, `ResolveQualified`,
`ResolveUnqualified`, `DataBinder.QualifiedCandidates`, ...) are the ways a data-name becomes a symbol, about 100 call sites
(kb/Work PB5932's dated file `2026-11-19-step-6b-close`). "Identify appends each SymbolOccurrence" therefore cannot record a
data-name or condition-name occurrence, the class the cross-reference, the store kinds (`StorePositionsOf`), the pairing test and
the statement-line margin are about; recording at today's text paths instead is what design review rounds 1 and 2 rejected and
6b would undo.

**2. The entries do not exist while the bind runs (a design gap, rule 5).** Section 6.7.1 says "The declaring binders create the
entries (`SymbolTable.Declare`, which records the `Defined` occurrence and returns a `SymbolId`)" and "Occurrences go into their
entry when `Identify` resolves a name". Step 6b built each class's declaration as a table of SYMBOLS (`DeclareParagraph`,
`DeclareSection`, `ConfigurationNames<T>.Declare`, `DeclareDataName`, ...), and no `SymbolId` exists (`git grep -n "SymbolId\b" --
src` finds nothing): the entries are made only after the bind, by `SymbolTableFinish.Of` (`BinderDriver.cs` line 327) from the
frozen model. So an occurrence would have to be joined to its entry after the bind by the identified ANSWER, and for most classes
the answer is not the declaration the finish declares:
- a constant: `Identify` answers the `ConstantDef`; the finish declares a private `ConstantSymbol(name)`;
- an ordering-name: `ConfigurationNames<string>`, so the answer is a string; a switch-status condition-name is `SwitchStatusName(
  ImplementorName, IsOn)`, a value record two switch-names can share; a mnemonic-name answers the shared `ImplementorName` row;
- a paragraph-name answers `ParagraphDefinition(Pc)`, a value made per lookup that carries no position, and the paragraph tables
  are keyed by pc;
- a report group answers `(ReportModel, ReportGroupModel)` candidates; the finish declares the entry as `ReportModel.ItemOf(entry)`
  or the parse context;
- a function-prototype-name answers a `FunctionPrototypeReference` made per lookup; a DEBUG-ITEM name a `DebugRegisterPlace` made per
  lookup;
- the procedure-names, the CONFIGURATION SECTION names, the program- and function-prototype-names, the DEBUG-ITEM registers and the
  object-class-names and interface-names have NO entry in the finished table (`SymbolTableSnapshot.cs`'s `NameClass` members say
  "the finished table holds no entry for it yet (listing step 7b)").
A join by answer would be one adapter per class, written beside the table and kept in step with it by nothing. The root is the
design's own: each class's declaration makes its ENTRY (a `SymbolId`) when the binder declares the name, the class's table keeps the
id beside the symbol, and `Identify` hands back the id in the `IdentifiedName`, so an occurrence joins its entry by id for every
class and the finish reads the entries the bind made instead of re-walking the model.

**3. That restructuring touches every class's declaration path, the data-name class's included,** whose declaration sites are in
step 6b's remainder (`DataBinder._dataEntryLocations`, `LinkageBinder`, `OoDataBinder`, `FileControlBinder`, ...: the
`2026-11-19-step-6b-close` table's "Data-names: the declarations" row). Building it on this branch would edit the same members the
next 6b slice must, in parallel.

**Decided (the codebase owner's call, CLAUDE.md "Ownership"):** step 7b is not built on this branch. The order is:
1. step 6b's remaining slices (the data-name and condition-name resolution through `IdentifyDataName`, their declarations, the
   census, the group tier's units, the parameterized expansion), as kb/Work PB5932 holds them; their declaration work gives each
   declared name its entry id as it moves (the `SymbolId` of section 6.7.1), so the classes 6b has already moved get theirs in the
   same slices or in one slice beside them;
2. then step 7b as section 11 writes it, every `Identify` answer carrying its entry's id.
Section 11's step 7b paragraph and section 6.7.1 record the gap ("Step 7b, dispatched early").

**What this branch carries instead:** the review leads (every `review-listing-step-*.jsonl` LEAD and the two of
`kb/Work/PB5932/2026-11-19-review-leads-slices-2-and-3`, checked on 4b4ea135e4): one still true, step 6b-4 R2-1 (the THROUGH phrase's
rules cited as "§2023:14.7.8 rule 1"/"rule 2", a designator `cite.py` refuses), fixed at its seven `src` sites, two inventory notes,
four conformance cases' comments and DATA_MODEL_DESIGN.md (`python scripts/spec/cite.py --check "§2023:14.7.8 GR1" "the range of
values includes literal-1, literal-2"` -> OK, the text is in GR1); the rest were already fixed (the 6b-close session's table,
re-checked here by `git grep`).
