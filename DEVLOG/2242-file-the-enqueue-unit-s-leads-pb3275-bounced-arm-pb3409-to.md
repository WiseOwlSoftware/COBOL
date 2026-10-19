## Entry 2242 — 2026-10-18 23:20 PDT — File the enqueue unit's leads: PB3275 BOUNCED arm, PB3409 to PB3411, PB2819 arm 3, PB2749 witness, PB3263 self-test shape

Work-register notes only, no code. Extended: PB3275 (variant 3: the BOUNCED arm compares the tip with the pull request's
current head, so a pushed fixed tip reads BOUNCED forever; title and heading now cover both arms), PB2819 (arm 3: the
auto watch prunes delivered records against an unreadable process table and treats any process as a live session; PB3387
added to related), PB2749 (witness c: the REWRITE and DELETE permanent-error guards duplicate
`MutationTargetRecordId`'s replay; PB3336 added to related), PB3263 (Fix part 4 now prescribes the self-test of files
that existed at the base, matching `landing_check.py`). Filed: PB3409 (`drop_remote` reports an already-deleted remote
branch as an error and leaves stale tracking refs), PB3410 (the 9-page ledger bound has no witness) and PB3411 (the
files design doc's stale `OutOfMemoryException`), the last two blocked by PB3336 in cluster PB3353. No allocated id went
unused. Checked and not filed: the git-maintenance self-test race (fixed on 326-31), fixture git helpers that ignore exit
codes (swept, none), the skills-pin PB3380 text lead (its branch is not queued), landing_measure refusals for PRs #139,
#149 and #162 (already PB3124).

