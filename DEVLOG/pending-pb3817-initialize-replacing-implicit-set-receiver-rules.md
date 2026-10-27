## PB3817 — INITIALIZE REPLACING stored a pointer or object reference no written SET would admit into a restricted receiver

**What was wrong.** `INITIALIZE pp REPLACING PROGRAM-POINTER DATA BY PU`, with `pp` a restricted program-pointer (TO a prototype) and `PU`
unrestricted, compiled clean and stored an address that §13.18.60.4 GR25 forbids the item to hold. §14.9.20.4 GR4 makes that store "SET
receiving-operand TO sending-operand" per receiving operand, and §14.9.39.3 SR22 holds the SET; the INITIALIZE binder asked only the
phrase-level category agreement (SR4), which cannot see a receiver's restriction.

**Re-probe (wave tree).** The note's repro compiled clean. So did its siblings of the same root: a restricted data-pointer (SR19, GR23), a
function-pointer restricted to a prototype of another signature (SR20, GR26), and an object reference of an unrelated class, which passed the
binder and died in Roslyn as `CS0030` naming no COBOL rule. A written SET refused all four.

**Fix.** `SetBinder.ScreenImplicitSet(receiver, sender, statement)` is the one verdict for a store that is an implicit SET, and
`InitializeBinder.ElementaryAction` asks it for each elementary receiver of the GR6b `InitializeSetFrom` arm. Data-pointer goes through
`PtrBinder.ScreenPointerReceiverRestriction` (COBOL0869), program- and function-pointer through `SetBinder.PointerAssignmentReason`
(COBOL1959), object reference through `OoConformance.ObjectRefAssignmentMismatch` (COBOL0867): the three routes the written SET uses, so the
two statements cannot answer differently. One diagnostic per receiver item (a table's occurrences share it). `BY NULL` is not a place and stays
accepted. DESIGN.md's INITIALIZE paragraph now says the implicit SET is held to its receiver's rules.

**Goldens.** Accepted: `14.9.20-INITIALIZE/pb3817_initialize_replacing_restricted_program_pointer` (same prototype, NULL, unrestricted in both
directions, a STRONG group holding one of each, then a CALL through the stored pointer). Refused: `pb3817-initialize-replacing-restricted-program-pointer`
(the unrestricted first receiver is fine, the restricted second one is refused), `-restricted-function-pointer`, `-restricted-data-pointer`,
`-object-reference-class`, each with its `diagnostics.json` entry. Inventory: nine already-CONFORMS rows gained the witnesses
(SR-14.9.20.3-4, GR-14.9.20.4-4, GR-13.18.60.4-23/25/26, SR-14.9.39.3-12/19/20/22); no GAP closed.

**Self-review.** Architecture: the rule lives in the SET binder beside the three it reuses, the INITIALIZE binder asks and does not copy.
Silent failure: a refused receiver returns no action and the error is reported, so no half-built store reaches the emitter. Sibling sweep: the
only `InitializeSetFrom` constructor is the one changed; the other receiving positions (SET, MOVE, CALL RETURNING) were already screened.
