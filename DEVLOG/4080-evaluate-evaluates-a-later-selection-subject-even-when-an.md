## Entry 4080 — 2026-11-27 19:25 PST — EVALUATE evaluates a later selection subject even when an earlier pair of the first phrase is false (PB6232)

PB6232: `SubjectUsageOf` set `ReadByFirstPhrase` from the first WHEN phrase alone. A subject at ordinal i > 0 with one
use in that phrase therefore skipped the PRE-op intermediate, but its render sits behind the `&&` chain of pairs 0..i-1
(§2023:14.9.13.4 GR4 c), so it was never evaluated when an earlier pair was false. Re-probe on this build before the fix:
`EVALUATE X ALSO R (J) WHEN 1 ALSO 5 ...` with X = 2, J = 9, R OCCURS 3, EC-BOUND-SUBSCRIPT checking on, printed "O" and
raised nothing. Fix: `ReadByFirstPhrase` holds only when every earlier pair of the first phrase is ANY
(`AllAnyBefore`), otherwise the subject takes the intermediate (GR3). The sibling arm, a use in a later phrase, was
already handled by `Uses == 1 && ReadByFirstPhrase`. Golden: `14.9.13-EVALUATE/pb6232_evaluate_later_subject_evaluated_first`
(measured before the fix: wrong output, after: expected output). The rule is an edition-independent semantic repair, so no refused
program applies. No inventory row closes.

