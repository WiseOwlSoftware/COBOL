## Entry 3428 — 2026-11-10 03:59 PST — PB5107: a RECEIVE on a disabled input-output terminal is given no message

The 1985 confirmation pass found that `ENABLE I-O TERMINAL` opened only the send half of the path: a RECEIVE issued before the ENABLE was given the terminal message, and the RECEIVE after it found nothing. §1985:XIV.3.3.4 GR4 says the ENABLE activates the logical path between the terminal and the program, and §1985:XIV.3.2.4 GR5 says the DISABLE deactivates it.

Re-probe on a fresh Debug build reproduced the note's observed text exactly. The fix is one test in `MessageStoreState.TakeFromTerminal`: a terminal that is not enabled gives no message, which stays queued in the store for the ENABLE. `Deliver` is unchanged, since the MCS still queues a destination's message for the terminal. This also closes the DISABLE sibling the note named, because both go through the one take.

Golden: `conformance-suite/wiseowl/cases/_unassigned/pb5107_enable_io_both_ways` (accept 85 and 2002; expected output computed from the cited rules). No negative golden: the communication module has no edition below 1985 to refuse it in. No inventory row closes (1985 rows are not yet in the store, PB5094).

Self-review: no second copy of the rule (every terminal RECEIVE goes through `TakeFromTerminal`); no allocation added on the receive path.

