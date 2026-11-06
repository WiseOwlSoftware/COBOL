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
