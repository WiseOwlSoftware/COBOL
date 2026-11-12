## Entry 3525 — 2026-11-12 10:18 PST — Lead filed: classify_red labels a red check step INFRA

The clerk filed PB5370 from the build-stage6-ci finisher's report for PB5235 (w20261996b-PB5235-report.md). `land_lane.classify_red` returns INFRA for any merge-group red with no failed test case, so a branch's own red check step (PR #1530, run 43130044782) was re-queued once as infrastructure and cost a second merge group (run 43132487649) before a finisher was sent. PB4271 and PB4331 designed and landed the classifier but do not cover this arm, so the lead is a new note, not a finding file. No code changed.

