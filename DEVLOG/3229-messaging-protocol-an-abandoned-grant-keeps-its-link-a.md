## Entry 3229 — 2026-11-06 18:44 PST — Messaging protocol: an abandoned grant keeps its link, a RECEIVE ended with a claim closes it, and a message-tag operand is the gate's whatever the verb

Three review leads of the messaging slice, one group (wave 20261887 D).

**PB4866.** A SEND that ended IMP with its link still granted closed the link through `EndOut`, which also answered every reply owed for an earlier
serial on that link as "server ended" (EC-MCS-ABNORMAL-TERMINATION). The unit test (SEND, receive, reply owed; second SEND granted; a failed enumeration ends it IMP)
failed before the change. The link is now kept and marked `OutLink.OwedAck`; the late ACK is consumed and returns the link to Ready, offering a waiting SEND its round.
What the close had also stood in for, a link whose write failed, is now `OnActionFailed`'s rule for every failed `Write`, in both roles (the link is ended after the statement it served).

**PB4867.** A RECEIVE that ended IMP while its CLAIM was in flight left the later GRANT unread, so the requestor stayed granted forever. `CompleteReceive` now closes the claimed link
(design 5.4.1's invariant allows a link to end instead of a GRANT or WITHDRAW; step 9 starts a new round on the requestor, which still holds the message). The deadline wait (5.4.4) is untouched. Review round 1 (R1-1) found that closing marked every reply the server still owed on that link 'server ended' from the requestor's end, the harm PB4866 removes; so the link is closed only when the server owes no reply on it. Otherwise the offer is kept `InFlight`: the next RECEIVE adopts it without a second CLAIM, and a GRANT that arrives between two RECEIVEs is held on the offer and taken by that RECEIVE (unit test `AReceiveEndedImpWithAClaimInFlightKeepsTheLinkWhileRepliesAreOwed...`).

**PB4868.** Below 2023 only SET bound a tag operand to a no-op beside COBOL0900; MOVE T TO N drew COBOL0809 and DISPLAY T COBOL1694 as well. The question now lives once, in
`StatementBinder.BindStatement`, over a statement's own operands (data references, function-identifiers, inline method invocations; a nested statement asks for itself), and the SET-only copies are deleted.

Goldens: the witnesses are unit and conformance tests (`MessagingProtocolTests`, `MessageTagIntroductionGateTests`); no corpus case is added, because none of the three changes a
program's specified behaviour (the protocol's internal frames, and the absence of a second diagnostic, which the harness's refusal files cannot assert). Inventory rows: none claimed, none closed.

Self-review: architecture (the tag question has one funnel; no parse-tree member in the backend input), full code (design sections only are cited; review R1-3 corrected an OoBinder citation to 8.4.3.4.4 GR1 b), performance (the gate walk runs only below 2023, edition test first), duplication (SetBinder's copies deleted). Residual: a compound statement whose own operand is a gated tag binds to a no-op, so its nested statements are not bound below 2023 (COBOL0900 already fails the compile); a SELF or object-view receiver of an inline invocation, and an INVOKE RETURNING, are not asked, so those still draw a class diagnostic beside COBOL0900 (review R1-2, left as a lead).
Gate: see the branch report for the verdict line of the final run.

