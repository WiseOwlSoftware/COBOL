## Entry 2540 — 2026-10-25 11:29 PDT — PB3676: a waiting lock-file region hold is owed its extra publication round after every lost round that follows progress

PB3632's review fix (R1-1) let a WAITING `SidecarRegionLocks.Hold` publish one more round when a lost round found nothing in the
way, so a holder that left during the round did not turn the hold into `Unavailable`. The allowance was a flag set once per call
and never reset: a long wait that lost round 1 beside a departed holder, then saw a live holder and waited for it, then lost
round 3 beside that holder's departure answered `Unavailable`, the very interleaving the extra round exists to absorb.

Re-probe: a one-thread driver (`RaceInjector` loses rounds 1 to 3, releasing holder A as round 1 begins and letting holder C take
the byte as round 2 begins; C is released as the wait pauses) answered `Unavailable` at round 3 on the unfixed build.

Fix: the allowance is a property of the previous iteration (`lastRoundWasTheExtraOne`). A lost round with nothing in the way earns
one more round unless the round just before was itself the extra one; any iteration between them (a live holder seen and waited
for) clears it. Two lost rounds in a row with nothing in the way stay `Unavailable`, so the wait is bounded. New test seam
`SidecarRegionLocks.BackoffObserver` (beside `SidecarLockFiles.PauseObserver`) reports the pause of a waiting hold instead of
sleeping, so the test lets a holder go between two attempts without a thread or a timed sleep.

Witness: `RegionLocksDriftTests.AWaitingHoldThatSawALiveHolderBetweenTwoLostRounds_IsOwedAnotherRound` (red before, green after;
the PB3632 bounded-wait half of `AWaitingHoldWhoseHolderLetGoDuringTheLostRound_IsGrantedOnTheNextRound` still holds). Sibling
sweep: no other waiting hold keeps a once-per-call allowance. Rows: none claimed, none closed. Note: PB3676 landed.

