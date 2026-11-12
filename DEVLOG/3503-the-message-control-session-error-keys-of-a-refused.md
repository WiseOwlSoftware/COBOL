## Entry 3503 — 2026-11-11 15:11 PST — The message control session: error keys of a refused statement, the count of a partial message, one message per destination occurrence, the terminal lock, the source of a received message

Five defects of `MessageControlSession` (kb/Work PB5281, PB5285, PB5288, PB5283, PB5280), one root: the session's rules were written per
path, so each early return, each key and each lookup missed a rule that binds the whole statement. Each was re-probed on this build first
(the repro programs of the notes, run with `cobol.exe --std 85 --run`); all five reproduced.

- **PB5281, error keys (§1985:XIV.2.2.4 GR22).** ENABLE and DISABLE OUTPUT with a wrong KEY (status 40) and a SEND of text too long (50)
  or of no text (60) returned before the loop that writes the error keys, which stayed `**`. `SetErrorKeys` now writes every occurrence
  1 to the destination count on those returns: 1 for an unknown destination, else Table 2's value for the destination as it is (5 already
  enabled or disabled, 2 disabled), else 0. The value choice is in docs/CONFORMANCE.md §3.3.
- **PB5285, the count (§1985:XIV.3.1.4 GR1; §1985:VI.6.24.4 GR3).** A message partly received left the count at its first portion. The run
  unit's ACCEPT MESSAGE COUNT now adds the remainders it holds under the designated queue structure (`Dialogue.PartlyReceivedFrom`). The
  store keeps letting go of a message at its first portion, so the remainder dies with a run unit that dies (14.5.10 rule 2) and a crash
  leaves no stale count; another run unit's count does not include it, which §3.3 records.
- **PB5288, a destination named twice (§1985:XIV.3.6.4 GR6, decided by PB5306).** The pending message was keyed by destination name, so
  two occurrences merged into [AA, AA, BB] and [BB]. It is keyed by the name and the occurrence of that name (`DestinationKey`), so each
  occurrence receives the whole message with its own segment and message control, and PURGE purges per occurrence the same way.
- **PB5283, the terminal lock (§1985:XIV.2.2.4 GR31).** SEND took the lock only when it reached `SendToTerminal`; it now takes it where the
  statement is entered, before status 50 or 60, as RECEIVE, PURGE and ENABLE already did.
- **PB5280, the source (§1985:XIV.2.2.4 GR10).** A message's source defaulted to its destination's name and was written unchecked. It is
  written only when it names a source or a terminal of the definitions (`MessageStoreState.KnowsCommunicationTerminal`), else spaces. The
  note's lookup, `FindSource` alone, would have blanked the source of a message from a defined I-O terminal (an existing unit test seeds
  one), and a terminal is a communication terminal the MCS knows.

Goldens, one per mechanism, each failing on the tree before the fix and passing after (run both ways): `1985-GR-XIV_2_2_4-22-refused-statement`,
`1985-GR-XIV_3_1_4-1-partly-received`, `1985-GR-XIV_3_6_4-6-duplicate-destination`, `1985-GR-XIV_2_2_4-31-failing-first-statement`,
`1985-GR-XIV_2_2_4-10-unknown-source`. The rules exist at 1985 alone, so no program is refused below. Rows: the 1985 rule ids stay out of
`closes_rows` (kb/Work PB5094). Self-review found nothing beyond the source-lookup correction above.

