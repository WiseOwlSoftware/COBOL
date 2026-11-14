## Entry 3606 — 2026-11-14 07:30 PST — PB5386, PB5412, PB5486: a receiver is identified at its store, the record-area rule asks the storage, the FROM function rule cites 1985; A property receiver with a function in its reference modifier carries two brackets

### PB5386, PB5412, PB5486: a receiver is identified at its store, the record-area rule asks the storage, the FROM function rule cites 1985

Wave 20262025, group B: three notes the 1985 code read filed against the binder of the implied MOVE of the FROM and INTO phrases.

**PB5386 (wrong answer, silent).** `RETURN SF INTO WE (SEQ)` with SEQ in the returned record stored each record into the element the
PREVIOUS record had selected: `WT[C2..B1]` where the 1985 text gives `WT[B1C2A3]` ("Any subscripting associated with identifier-1 is
evaluated after the record has been read", §1985:XI.4.3.4 GR6b; §2023:14.9.34.4 GR5 b) says the same of item identification). The re-probe
located what the note had not: SEQ lies in the SD's shared record area, so the reference resolver cannot read it in place and lowers the
subscript to a §2023:15.4 temporary whose store is a statement pre-op, hoisted before the RETURN. A single-record SD read SEQ in place
and was right. The same hoist broke every statement whose rules identify a receiver as it stores it: READ INTO, `MOVE 2 TO I, WE (I)`
(§1985:VI.6.19.4 GR2), `ADD 1 TO I, WE (I)` and COMPUTE (§2023:14.7.7 4) b)), DIVIDE's REMAINDER (§1985:VI.6.11.4 GR4), SET's
identifier-1 (§2023:14.9.39.4 GR2) and the PERFORM VARYING induction variable (§2023:14.9.28.4 GR12); each stored into the element the
subscript named before the statement ran.

The fix reuses the placement that already existed for object-property receivers (kb/Work PB2078). `ExpressionBinder.ResolveReceivingAtStore`
takes what a receiver's identification registers off the pre-op queue and keeps it as the receiver's identification bracket;
`BindStatement` wraps the statement in a `BoundReceiverBrackets`; `ReceiverBracketEmitter.Receive`, which every one of these statements
already calls at each store, opens it there. `ReceiverBracket` now names its receiver through a key (a property temporary or a written
reference), so the EC passes, the debug planner and the usage walk see the new steps through the shapes they already walk.
`ExpressionBinder.ResolveTargets`, the MOVE binder's one caller, is deleted. Six goldens, each at all four editions and each red on the
pre-fix compiler: RETURN INTO, READ INTO, MOVE, ADD/COMPUTE/DIVIDE, SET, PERFORM VARYING.

**PB5412 (under-rejects).** At 85 and 2002 a FROM or INTO operand that names exactly the record area by another reference compiled:
`R1 (1:10)`, `R1 (1:)`, the element of a one-occurrence table, a 66 RENAMES of the whole record, and (read 4) the record of a SAME AREA
peer. `StorageSharing.IsRecordAreaOf` decided on the reference's form. It now decides on the storage: `StorageExtent.OfReference` (the
overlap rule's extent, plus a one-occurrence table element and a THROUGH alias) against the whole of each record of the area, the area
now including SAME AREA peers (§1985:VII.2.13.4 GR1). `R (1:)` is R itself (§2023:8.4.3.3.4 GR5c), which covers a variable-length record.
The refusal's message dereferenced `DenotedItem`, null for a slice, and crashed the compiler once the predicate answered true for one;
it names the operand's item now. All fourteen probes are refused; six refusal goldens and one legal-side golden (parts of the record,
a run-time length, one element of two). `docs/FILES_DESIGN.md` D31(b) is revised; eleven 1985 rows re-verdicted CONFORMS
(1985 GAP 232 → 221).

**PB5486 (analysis, answered).** The lead asked whether 1985 states the FROM function-class rule or the compiler over-rejects. 1985 states
it: Amendment 1 opened every FROM syntax rule with "If identifier-1 is a function-identifier, it must reference an alphanumeric function"
(§1985:VII.4.7.3 SR1 and six siblings). Nothing over-rejects; the message quoted the 2023 rule at 85. `FromPhraseRules.FunctionRuleCite`
is an `EditionCite` with the 1985 rule per I-O module, which corrects PB5352's reading that this rule is 2002-and-later. Three refusal
goldens pin the 1985 citation; `EditionCiteTests` pins the selection.

Inventory: batch `w20262025b-pb5386` (six 2023 rows re-recorded CONFORMS with the PB5386 witnesses, and `SR-14.9.25.3-4`'s code
location moved off the deleted `ResolveTargets`; 2023 GAP 0 → 0), `w20262025b-pb5386-85` and `w20262025b-pb5412-85`. Codes: none
allocated were used (COBOL7129 to COBOL7131 returned).

### A property receiver with a function in its reference modifier carries two brackets

Branch review round 1 of the PB5386 landing (R1-1) found a regression: `MOVE "QQ" TO NM OF AR(1) (FUNCTION INTEGER(K):2)` crashed the
emitter with "the object-property receiver ... was claimed ... but no receiver store placed its steps", source main compiles. The property's
PB2078 bracket and the new PB5386 identification bracket sit in two `BoundReceiverBrackets` nodes, and `ReceiverBracketEmitter.Find` returned
only the first match. It now returns every matching bracket of every scope in force, property bracket outermost; `Receive` opens them in that
order and closes them in reverse. Golden `pb5386_move_property_refmod_function` prints `[AQQDEF]` at 2002, 2014 and 2023 (the object
property needs 2002, so no lower edition has a program to refuse). Sibling arms: READ and RETURN INTO, the arithmetic statements and SET all
reach the same `Receive`, so one fix covers them.

