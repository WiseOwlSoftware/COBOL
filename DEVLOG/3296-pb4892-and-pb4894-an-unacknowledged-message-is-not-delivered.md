## Entry 3296 — 2026-11-07 19:42 PST — PB4892 and PB4894: an unacknowledged message is not delivered, and a kept offer is decided when its reply is sent

**PB4892.** `MessagingProtocol.Take` issued `Write(ACK)` and `Complete` as separate actions. When the ACK write failed, the executor still completed the RECEIVE, so a
program held the message while the requestor, never told it was taken, started a new round and offered it again: two deliveries. The new `Deliver(Ack, Result)` action
writes and then completes in the peer; a failed write answers `ActionFailed(Deliver)`, and the protocol forgets the Active entry, makes the RECEIVE current again and ends
the link. Re-probe: `AnAckThatCannotBeWrittenDeliversNothingSoTheRequestorsNewRoundIsTheOnlyDelivery` failed before (the RECEIVE completed at the failed write).

**PB4894.** An offer kept InFlight only because a reply was owed was never decided again. `BeginSendResponse` now calls `ReleaseKeptOffers`, ending the link when nothing more is
owed, as `CompleteReceive` does; `Owes` is the one test for both. Re-probe: `AReplyThatAnswersTheLastOwedMessageEndsTheLinkOfAnOfferKeptInFlightForIt` failed before (no new OFFER).

Design `DESIGN-message-control.md` 5.4.2 updated. No inventory rows claimed or closed. Sibling sweep: every other `Write` in the protocol (CLAIM, GRANT, WITHDRAW, REPLY, HELLO,
OFFER) either ends its statement through its own failure path (existing tests) or is followed by no completion that depends on it.

