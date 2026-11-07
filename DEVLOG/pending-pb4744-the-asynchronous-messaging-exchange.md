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

**Not done, refused loudly:** a group item holding a message tag as a message area is COBOL6460 (a tag is no part of a group's storage image; its slot would be lost silently). The design's `ElementImageCapable` admission was not
made: that category is also read by the equality and group-MOVE consumers of the image.

**Goldens:** `send-no-server`, `send-response-no-requester`, `receive-no-requester`, `pb374_ec_mcs_names_raise`, `w59_mcs_facility_witness` (rewritten to the exchange), the refusal `pb4744-send-from-group-holding-tag`; the runner gives each test its
own MCS directory. **Registry:** fourteen rows CONFORMS (GR-14.9.31.4-1 to -5, GR-14.9.38.4-1 to -8, GR-14.6.11-7). Gate: see the landing.
