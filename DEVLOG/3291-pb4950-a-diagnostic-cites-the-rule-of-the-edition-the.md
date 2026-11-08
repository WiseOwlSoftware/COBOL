## Entry 3291 — 2026-11-07 18:46 PST — PB4950: a diagnostic cites the rule of the edition the program was compiled under (owner decision)

A note-only change. Wave 20261917 found that the SAME clause's diagnostics cite 2023 rule numbers when a program is
compiled with `--std=1985`, whose text numbers the same rules differently. The owner decided that a diagnostic cites the
rule of the compilation's edition. Today each diagnostic descriptor carries one 2023 citation. PB4950 records the decision
and the design. Each descriptor carries its citation per edition, and the message renders the compilation's edition. The
1985 data comes from the 1985 audit's mapping pass (PB4886), so the note waits on it. A diagnostic with no 1985 rule cites
its edition gate.

