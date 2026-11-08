## PB4744: the asynchronous messaging exchange (SEND and RECEIVE run), COBOL1578 and the EC-MCS decline retire

Slice S4 of `DESIGN-message-control.md`. Before it `SEND TO "SRV" FROM X RETURNING MT` bound to the declined-facility seam, drew COBOL1578 and did nothing, so no server was ever found and `ON EXCEPTION` never ran.
Now a request and its reply cross two processes (`MessagingProcessTests`), an unserved name is EC-MCS-NO-SERVER with `EXCEPTION-STATUS` naming it, a RECEIVE of a NULL tag in a run unit that serves nothing is
EC-MCS-NO-REQUESTER at once, a server killed with an active message is EC-MCS-ABNORMAL-TERMINATION at its requestor, a server that ends without replying is EC-MCS-NORMAL-TERMINATION of spaces, a forwarded
`RAISING EXCEPTION` name makes the requestor's RECEIVE unsuccessful with it, a message longer than identifier-1 is taken and EC-MCS-MESSAGE-LENGTH, and `CONTINUE AFTER MESSAGE RECEIVED` / `n SECONDS` poll or wait.

**The shape.** `AsyncMessaging` runs a statement on the run unit's `MessagingPeer` and sets the condition the protocol names only when the statement's own checking profile enables it. The three bound nodes implement
`IProfiledStatement` (the former `IActivatingStatement`, renamed for what it carries), `MessagingEmitter` renders one call each and `EcEmitter.EmitSelection` takes the raised name unless ON EXCEPTION does. The design
asked for five flag declarations per fixed EC-MCS name; the message control system names the condition at run time for all of them and a requestor learns a name a server chose, so one mechanism (the profile) serves
every name and the design says so. `CobolTiming.Interval` is the one CONTINUE AFTER interval rule. `EcRaiseSiteRegistryDriftTests` counts a reference to an `ExceptionCatalog` constant as a raise site.

**Retired:** COBOL1578 (never reallocate), the EC-MCS row of `DeclinedEcFamilies`, `declined-raise-sites.json`'s EC-MCS entries, `derived-verdicts.mcs-only` and its two tests, `DocumentedNonSupportWitnessTests`'s three MCS tests.
`docs/CONFORMANCE.md`: A.3 row 4 flips to Claimed, section 4 item 1 is a tombstone that keeps its number (renumbering items 2 to 5 would touch some 350 citations and lands alone), and D-MCS-SERVER, -LENGTH, -WAIT, -RAISE and
-CHECK are new, with D-MCS-NR extended and DOC-A.1-100 now naming EC-MCS-IMP as a name the implementation sets.

**Review round 1 (two blocking findings).** (1) A successful RECEIVE whose nonfatal EC-CONTINUE-LESS-THAN-ZERO a USE declarative handled ran its NOT ON EXCEPTION phrase
as well; section 14.6.13.1.4 3) skips the phrase. `EcEmitter.EmitSelection` now returns its result local and `MessagingEmitter.EmitOutcome` gates the phrase on
`DispatchResult.RanAHandler` of it, the rule the I-O emitters keep as `UseHookResult.NotPhraseGate`. Pinned by `MessagingProcessTests`. (2) A group item holding message tags
was refused as a message area (COBOL6460) though SEND SR3 and RECEIVE SR2 do not list the message tag: the refusal was a scope cut. The tags now travel beside the image.
`ElementImageCapable` still does not admit the category (equality and group MOVE read it, and the placeholder image is not injective), so a group whose only managed slots are tags
gets a generated `FromMessageImage` (the `FromImage` distribution minus the tags' placeholder positions; a nested struct calls its own) and the message's slots are
`AsyncMessaging.TagsOf` of the group's `AsLeaves` vector on SEND and `WithTags` over it on RECEIVE (storage order, inner OCCURS included, a storage-cell group through
`GroupLeaves`/`WriteGroupLeaves`; a tag the message did not carry is NULL). Golden `send-group-holding-tag`; `MessagingProcessTests` sends a group holding a tag, a tag table
and a nested group's tag between two processes. Round 2 (blocking): the first round left a VARIABLE-LENGTH group (OCCURS DYNAMIC) holding a tag refused with COBOL6460, a refusal of legal source with no owner decision.
It is built instead: `FromVarImage`'s fixed-part distribution skips a tag's positions (`EmitMemberFromImage(messageImage: true)`), the tags travel over the same leaf vector, COBOL6460
and its refusal golden are deleted (the code is returned), the SEND takes the group's one-way current-extent image (`RecordAreaImage(transfer: true)`), a RECEIVE into an area holding an OCCURS DYNAMIC table is `UnboundedCapacity` (its capacity was the file-record maximum, which omits a dynamic table, so any message was too long: a sibling bug of the same mechanism), and golden `send-var-group-holding-tag` plus `MessagingProcessTests.AVariableLengthGroupHoldingAMessageTag_...` pin it.

**Goldens:** `send-no-server`, `send-response-no-requester`, `receive-no-requester`, `pb374_ec_mcs_names_raise`, `w59_mcs_facility_witness` (rewritten to the exchange), `send-group-holding-tag`, `send-var-group-holding-tag`; the runner gives each test its
own MCS directory. **Registry:** fourteen rows CONFORMS (GR-14.9.31.4-1 to -5, GR-14.9.38.4-1 to -8, GR-14.6.11-7). Gate: see the landing.
