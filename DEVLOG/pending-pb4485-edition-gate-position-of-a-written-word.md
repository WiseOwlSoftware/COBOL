## PB4485 - the qualified-index-name and alphabet-name class-condition edition gates report at the written word, not at the statement

Mechanism (one, in `NameResolution.ResolveIndexName`): the COBOL-2002 gate for a qualified index-name (Annex F.2
item 108, `qualified-index-name-2002`, PB4305) fired from the shared index-name resolver, which runs inside SET, SEARCH
VARYING, PERFORM VARYING, relation conditions and subscripts, and it reported at the sink's ambient cursor, the
enclosing statement. Re-probe on a fresh build at `--std 85`: `SEARCH E VARYING IX OF T` reported at (9,12) with IX at
(9,29); `SET IX OF T` at (12,12) for IX at (12,16); a subscript `E (IX OF T)` at the subscripted item, not at IX.

Fix: the resolver is handed the written reference (`writtenAt`, a `DataReferenceContext` or the name token) and now
positions the sink at its first token around the check (`WrittenStart`). Sibling sweep (every `ConstructRegistry.Check`
in the binder against PB4305's list): the item-16 gate in `ConditionBinder.BindUserWordClassCondition` had the same
shape (`IF W IS AL` reported at the IF, AL four words later) and is fixed the same way; its `at` parameter became a
`ParserRuleContext`, the type all three callers pass. The item-10 gate (CALL USING a non-01/77 group) is a bound-arm
gate, which PB1925 deliberately positions at the statement; `BoundCallArg` has no position, so it stays.

Witness: six cases of `DiagnosticPositionTests.EditionGateOfAWrittenWord_NamesTheWordNotTheStatement`. No golden or
manifest change: `pb4305_qualified_index_name_2002` and `pb4305-qualified-index-name-at-85` already carry the rule, and
a refusal file has no column. No inventory row claimed or closed.
