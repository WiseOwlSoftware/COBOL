## PB5932: the source listing, step 6b - every class the symbol table declares enters its entry, and every identification carries its id

Step 6b of `docs/rearchitecture/DESIGN-listing.md` (kb/Work PB5932) makes `SymbolTable` the one store of the program's names. Step
7b, dispatched early, found the store had no ENTRIES while the bind ran: each class's declaration keyed its symbol and returned
nothing, the finish declared the entries again from the frozen model, and for most classes the identified answer was not what the
finish declared (kb/Work PB5925's dated file `2026-11-19-step-7b-not-buildable-yet`, finding 2). This entry lands the lane that
builds them, from `listing-step-6b-ids`, beside the data-name lane: it moves no table, lookup or comparison, so the baseline stays at 86.

**One store of entries per compilation group.** `SymbolTable.EntryStore` (held by the group's `OoClassTable.Entries`, the one object
every binder of the group reaches) records each declaration where the binder makes it: its table, its class, its name as written,
where the listing places it and its symbol, and returns a `SymbolId`. The id stands beside the symbol in one identity index rather
than in a field of each class table, because three inheritance paths hand a contained element its container's bare symbol; a name an
element inherits therefore answers the container's entry with nothing copied, and the data-names and condition-names, the next class
to move, adopt the store with no new shape.

**Every class enters.** Procedure-names (one `ParagraphDefinition` per paragraph, made once), the nine configuration-section classes
(each declaration now takes its declaring word's token), constants, files, CDs, reports and each report group description entry,
program- and function-prototype-names with each unit's own program entry, the DEBUG-ITEM registers, VARYING counters, index-names,
CAPACITY registers, type-names, and the compilation group tier's classes, interfaces, imports, the standard class BASE and the
parameter-names. `IdentifiedName.Id` carries the entry, a candidate set carries each candidate's, `OoNameResult.Entry` the group
tier's, and an identification without an entry cannot be made: `IdentifiedName` and `ClassNames.Lookup` refuse it, so a declaring
path that enters nothing fails at its first reference in every compilation.

**The finish reads the bind.** `SymbolTableFinish.Of` writes the entries the bind made; `ConstantEntryBinder.DeclaredConstants` is
gone; the procedure-names, configuration-section names, prototype-names, registers, counters and group-tier names are entries of the
finished table for the first time, which no listing view reads yet, so the data map and the program map are unchanged. The data-names
and condition-names are still declared from the forest until their declarations enter them, and `SymbolTableFinish.DeclareEntered`
adopts their entries that day.

**Proved:** `python scripts/arch/compare_oracle.py --no-build` against the base 8dd07d2eabd1 is
`=== ARCH-ORACLE: IDENTICAL — 22647 cases ===`; `SymbolEntryDriftTests` (the guard, four planted programs whose every entry is checked
at the line and column the source writes, a witness of every class, and every planted reference identified through the table's funnels with its id checked against the declaration the rules resolve it to) is shown red with one declaration's entry removed, and, after review round 1, with a funnel that answers another entry (the containing function definition's, a candidate's, a VARYING counter's). Review round 1 also made `IndexEntryOf` fail loudly for a lapsed type member, which has no entry. The section 9
`None` row is in the dated file `kb/Work/PB5932/2026-11-19-step-6b-entry-ids.md`. The lane also fixed the last 14.7.8 rule-number
citations the step 7b review's lead R1-1 found (comments and inventory notes only).
