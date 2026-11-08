## Entry 3289 — 2026-11-07 18:18 PST — PB4744: the asynchronous messaging exchange (SEND and RECEIVE run), COBOL1578 and the EC-MCS decline retire; The messaging peer group: review round 3 of PB4744 after PB4838, a long Windows temp path, a far pointer token and two citations

### PB4744: the asynchronous messaging exchange (SEND and RECEIVE run), COBOL1578 and the EC-MCS decline retire

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

### The messaging peer group: review round 3 of PB4744 after PB4838, a long Windows temp path, a far pointer token and two citations

Wave 20261915 group A finishes PB4744's branch (review round 3) and the PB4919 continuation, merged onto current main, and fixes PB4921, PB4922, PB4925 and PB4928.
PB4855 was re-probed and stays open.

**The merge.** Main's PB4838 had landed while PB4744's branch was in review: a message-tag leaf now carries its 8-position TOKEN in a group image, and a group
holding only tags has a `FromImage` that decodes it (the file record's inverse). Five files conflicted. The resolution keeps both halves: `FromImage` for the file
record, and `FromMessageImage` for the message, which skips the token positions because a token names a tag of the SENDING run unit. The tags still travel as
the message's slots.

**R3-1 (blocking).** The reviewer's program has a tag in an ELEMENT of an OCCURS DYNAMIC table. It no longer failed to compile after the merge, because the element
group now had `FromImage`, but that decoded the sender's token into the receiver. `TableElementFromImage` now stores a tag-holding element by its
`FromMessageImage`. The two-process test `ADynamicTableWhoseElementsHoldMessageTags_CarriesEachTagBesideItsElement` checks LEN 0021, element 1's tag NULL and
element 2's tag set.

**R3-2.** §2023:14.6.11 7) sizes the run unit's implicit reply as "the size of the receiving data item in the associated RECEIVE statement". An area with no
allocated maximum was recorded as 0 positions. Its size is known only after the store, which recreates each dynamic table and raises it to its FROM minimum
(§2023:14.6.9.2). So the program now states it (`AsyncMessaging.Received`, the length of the current-extent image). In the test, a 4-position message into a FROM 3
table is answered with 6 spaces.

**A merge consequence.** PB4838 also made an elementary message tag 8 token positions (DOC-A.1-212). Before the merge it was a zero-position message that SEND GR1
refused; now its SEND sent the token and no tag. An elementary tag is therefore an 8-position message whose tag travels as its one slot, and a RECEIVE into one
takes the tag back from the slot (`AsyncMessaging.TagOf`). Test `AnElementaryMessageTag_IsAnEightPositionMessage_WhoseTagTravelsAsItsSlot`.

**PB4921.** The re-probe showed that the scenario does not occur. The RECEIVE wait is already bounded by `CobolTiming.Interval`, the one rule of the maximum
meaningful value (§2023:14.9.9.4 GR1, one day), so the 49.7-day timer cap in `MessagingPeer.Arm` was unreachable. `Arm` now clamps to `CobolTiming.MaxSeconds`.

**PB4922.** A Windows temp path too long for the socket budget, which is any user name over 16 characters in the default layout, made every messaging statement
EC-MCS-IMP. The socket directory now falls back to `%ProgramData%\wiseowl-mcs-<u>\<h>\`, the twin of Unix's `/tmp` fallback, where `<u>` hashes the user's SID. The
user level is created with a protected access list that grants the user alone, and an existing one that inherits, belongs to another user or grants anyone else is
refused. Re-probe: the peer, transport and platform tests under a 55-byte TEMP went from 18 red (the note's measurement) to 62/62 green.

**PB4925.** A data-pointer displaced 2^32 or more from its cell decoded to another area's window. Such a pointer is not a valid address of its storage
(§2023:13.18.5.4 GR4), so it now takes a token base of its own (`PointerImage.FarBase`) and round-trips exactly.

**PB4928.** The national and boolean data gates cited §2023:8.5.2 for "category national" and "category boolean". They now cite §2023:8.5.2.10 and §2023:8.5.2.5,
both checked, and the registry is regenerated.

**PB4855** (re-probe: `SORT T` Format 2 still runs no debugging section) needs the `WholeTablePlace` redesign its note names. That is a separate mechanism, and it
stays open.

**Self-review** covered architecture (one predicate `HoldsOnlyMessageTagSlots` for every tag-skipping arm; the socket fallback in the one resolver), full code
(every citation checked with `cite.py`), performance (the far-pointer table grows only with far pointers that are imaged) and duplication (none added).

**Review round 1, R1-1.** The D-MCS-LENGTH determination in docs/CONFORMANCE.md and the design's test-case list still said an elementary message tag has no
character position, so a SEND from one is EC-MCS-MESSAGE-LENGTH. Since the merge of PB4838 it is an 8-position message whose tag travels as its one slot, and both
documents now say so (`MessagingProcessTests.AnElementaryMessageTag_IsAnEightPositionMessage_WhoseTagTravelsAsItsSlot` is the witness).

