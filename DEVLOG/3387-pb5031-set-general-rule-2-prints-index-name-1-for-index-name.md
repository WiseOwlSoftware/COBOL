## Entry 3387 — 2026-11-09 13:03 PST — PB5031: SET general rule 2 prints index-name-1 for index-name-2's table; adjudicated, no code change

Re-read page 360 (folio VI-128) of the 1985 standard on the rendered page: it prints `index-name-1` in the paragraph about
index-name-2, as the transcription has it. General rule 3a (index-name-2 related to a table other than index-name-1's, converted
by occurrence number) rules out the printed reading, so the evident sense, index-name-2's own table, is the rule. The requirement
is on a run-time index value whose violation the standard leaves undefined, and the compiler checks it under neither reading, so
no program changes. The transcription keeps the printed word, following the Errata preface's policy (an operand the sense wants
changed stays as printed). The verdict is recorded in kb/Work/PB5031.md, status landed, closes no inventory row. No gate beyond
the inert lane was needed: only kb/ and DEVLOG/ changed.

