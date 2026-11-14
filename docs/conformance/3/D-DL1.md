- **D-DL1 — §2023:14.9.20.4 GR7's length-zeroing is the GR6c arm's rule, not a post-pass over all three
  sending-operand arms** (2026-09-20; kb/Work PB418 + PB415, rows `GR-14.9.20.4-7` and `SR-14.9.20.3-8`).
  GR7 is printed without a condition — *"When a dynamic-length elementary item is initialized, its length is
  set to zero"* — and read that way it erases the sending operand GR6a and GR6b have just designated, so
  `INITIALIZE D ALL TO VALUE` would not restore D's VALUE and `INITIALIZE D REPLACING ALPHANUMERIC DATA BY
  "ABC"` would store nothing, for the whole dynamic-length population. **WiseOwl COBOL applies GR7 only where GR6c
  supplies the sender** — i.e. where the item qualified through GR5c3 (the DEFAULT phrase) or GR5c4 (neither
  REPLACING nor VALUE specified). Four reasons, none of them a preference:
  **(i)** the standard states the SAME action at the other occasion WITH the carve-out spelled out —
  §2023:14.6.2.3.2 GR7, *"The length of each dynamic-length elementary item that is specified without a VALUE clause
  is set to zero"*; **(ii)** §2023:8.6.4 makes a dynamic-length item's initial-state length the VALUE clause's
  length, and §2023:14.9.20.4 GR6a3 demands a sender that *"produces the same result as the initial value of the
  data item as produced by the application of the VALUE clause"* — under the unconditional reading **no sender
  can satisfy GR6a3** for this population, i.e. a general rule becomes unsatisfiable rather than merely
  narrowed; **(iii)** §2023:14.9.20.3 SR8 designates literal-1 / identifier-2 as *the* sending operand under
  REPLACING, and the unconditional reading makes that designation inert for every dynamic-length receiver;
  **(iv)** GR7 is NECESSARY only on the GR6c arm — §2023:8.3.3.6.4 GR3b gives a bare figurative constant a length of
  one character, so GR6c's *"Figurative constant alphanumeric SPACES"* would otherwise leave the item at
  length 1 instead of §2023:13.18.19.4 GR1's minimum of zero. **Rejected reading:** §2023:14.9.20.4 GR7 as an unconditional
  post-pass, which is what WiseOwl COBOL did before this determination. Written once, in
  `InitializeBinder.ElementaryAction`; witnessed at both poles by
  `conformance:cases/14.9.20-INITIALIZE/initialize_dynamic_length` (the GR6c arm → `LEN=00`) and
  `conformance:cases/14.9.20-INITIALIZE/pb418_initialize_dynamic_length_senders` (the GR6a/GR6b arms, the figurative-VALUE
  one-character case, and the GR5c ordering against `THEN TO DEFAULT`).
