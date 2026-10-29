## Entry 2711 — 2026-10-28 18:12 PDT — File PB4011: a report's `Head:` field after other text on its line reads as prose

The loop's STALL alert for w20261740b (PB3974) had run 269 minutes. The branch was finished, but the branch survey read
it as a DECISION, because `reports.sha_field` took the report's HEAD from the Base field's prose ("merged reviewed head
5ba5c563c") instead of from the `Head: caf67d4cf…` written later on the same line. The field pattern admits only a
line start, ` · ` or ` | ` before the word, and a run of spaces is none of those. One of the 924 reports on disk has
that shape. PB4011 records the mechanism and a fix in three parts: the parser takes a colon field after whitespace; the
finished test compares the parsed HEAD with the worktree's head (folded into PB3260); and the prompt names the layout.
The operator moved the field onto its own line and recorded the round-2 finding that the 13:32 STOP had cut short (the
past-carrier fraction floor is 9 digits, not 17: a wrong-answer regression, reproduced). The loop now plans the branch's
finisher.

