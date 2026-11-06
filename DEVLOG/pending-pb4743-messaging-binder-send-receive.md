## Messaging slice S3: SEND and RECEIVE are bound and their syntax rules asked (PB4743)

Slice S3 of `docs/rearchitecture/DESIGN-message-control.md` (§4.1, §4.3), landed on top of S1's message-tag type.

**What the re-probe showed.** On the tree before the change, SEND and RECEIVE read their identifiers (PB3568) but asked none of the syntax rules whose
operand is a message tag: a `PIC X` data-name-1 of RECEIVE, a numeric literal after SEND TO, a `RETURNING` item of class numeric or pointer, a RECEIVE length
that was not an integer, and an identifier-1 holding a `POINTER`, `OBJECT REFERENCE`, `BASED`, `DYNAMIC LENGTH` item or an `OCCURS DEPENDING ON` whose object is
outside it all compiled clean. `CONTINUE AFTER MESSAGE FOO` compiled too (the arm was `messageKeyword cobolWord`).

**The fix.** `MessagingBinder` replaces `DeclinedFacilityBinder`'s SEND and RECEIVE members (the declined binder keeps VALIDATE alone). It builds one node per
statement format (`BoundSendToServer`, `BoundSendResponse`, `BoundReceiveMessage`) and asks the screens in the design's order: COBOL2024 name shape,
literal-1 class (§14.9.38.3 SR1), the message-tag items (SR4, §14.9.31.3 SR1) and RECEIVE's integer item (SR3), all COBOL6265, then one walk over identifier-1 and
its subordinates for the forbidden clauses (§14.9.38.3 SR3, §14.9.31.3 SR2), COBOL6266, then RAISING's exception-name through `EcNameResolution`. The grammar rules
are renamed `messagingReceiveStatement` and `messagingSendStatement`, and the word after `MESSAGE` is `RECEIVED` by a predicate. `StatementBinder` still answers
COBOL1578 (the exchange is S4): both arms call the binder through `BindDeclinedStatement`'s operand action and drop the node, and `BindDeclinedStatement` now owns the
unbuilt-ledger take-back every declined statement needed. The three nodes have arms in `BoundStores` and `UsageCollectionPass`; `StatementEmitter` throws for them.

**A correction to the design.** The design asks RECEIVE's CONSTANT RECORD clause in the walk. It cannot be reached there: a RECEIVE writes identifier-1, so
`ResolveReceiving` refuses a constant record's item first (COBOL1548, §13.18.15.3 SR2). The design now says the arm is the funnel's, and a test pins both halves
(SEND reads a constant record, RECEIVE cannot write one).

**Goldens and tests.** Two positive cases and nine refusals (`pb4743-*`), `MessagingSyntaxRuleTests` for every forbidden-clause arm of both statements, the
legal neighbours and the below-2023 edge. Nine existing goldens and five test sources that declared the message tag as `PIC X` or `POINTER` now declare
`USAGE MESSAGE-TAG`; the below-2023 golden keeps its `PIC X` items because no operand is asked below 2023.

**Registry.** FMT-14.9.31.2, FMT-14.9.38.2, SR-14.9.31.3-1 to -3 and SR-14.9.38.3-1 to -4 are CONFORMS; `mcs-only` selects the general rules alone (kind GR, 14 rows).
The architecture ratchets took one new pair (`StatementBinder` to `MessagingBinder`, which PB2347's move takes with the others).

**Self-review.** Architecture: one binder per statement family, one operand-class helper, no C# text in a node. Full code: the rule texts are `cite.py`-checked and
quoted in the refusals; sibling arms (SEND and RECEIVE) share `ScreenMessageArea`. Performance: the walk is one pass over a record's items per statement at bind time.
Duplication: `ClassifySend`, `ScreenName`, `ScreenSendRequiredPhrases` moved, none copied.

**Branch review, round 1.** Two blocking findings, both fixed at the root. (R1-1) A constant-name after TO is literal-1 (§13.10.3 SR2, §13.10.4 GR1): `BindSend` now asks
`LiteralAliasOf` before it takes the word for a message-server-name-1, so a numeric constant draws COBOL6265 as `SEND TO 5` does, and a string constant binds its literal
(`BoundSendToServer.ServerLiteral`) instead of its name; `ClassifySend` counts the alias as Format 1's evidence, and the drift test's exemption for the statement is gone because
a golden now witnesses the slot. (R1-2) SEND's identifier-1 is any identifier (§8.4.3.1.2): the grammar's FROM operand is `inlineMethodInvocation | dataReference | functionCall`,
and `BoundSendToServer.From`/`BoundSendResponse.From` are `BoundOperand`s (a data item's place or a function's or method's value); only a place is walked by SR3. Leads fixed in the same
files: a figurative constant is a legal literal-1 (R1-3), a reference-modified identifier-1 is asked its base item's own clauses (R1-4), and the COBOL6266 catalog text, the
CONFORMANCE paragraph and the StatementBinder comment no longer name RECEIVE's unreachable CONSTANT RECORD arm or the old binder (R1-5). Goldens: the accepted SEND case gained a
constant-name, a figurative and a FUNCTION leg; `pb4743-send-constant-numeric` (COBOL6265) and `pb4743-send-from-refmod-based` (COBOL6266) are new refusals; `MessagingSyntaxRuleTests`
gained the constant-name, function and slice theories.
