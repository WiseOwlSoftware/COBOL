## Entry 2579 — 2026-10-25 15:10 PDT — kb/Work: PB3765 points at PB3769; file PB3775 (review_findings.py start accepts a head that is no commit)

PB3765 records that its second requirement, cancelling superseded merge-group runs, landed separately as PB3769 (PR #605). PB3775 files a defect a round-2 reviewer met today: review_findings.py start recorded a mistyped sha as a round head without checking that it is a commit of the branch under review.

