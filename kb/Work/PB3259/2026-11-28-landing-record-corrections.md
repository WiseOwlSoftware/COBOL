## Later finding (2026-11-28): the Landed section names the wrong branch; Sites omit work.py

Source: `E:/COBOL-coord/scratch/reports/w20262174a-PB3259-report.md`, Review R1-1, R1-2 and R1-3 (low and info). Facts, for the record:

- **Branch.** The Landed section (line 60) says branch `worktree-agent-a2787d9fe53a79a18`, the predecessor branch that never landed. The commit landed from `worktree-wf_19b8f6c4-960-1` (the same change as PB4080: the predecessor's commit `b9f60d84be` cherry-picked onto main, then a gate-red fix, head `9ee04e92e0`).
- **Sites.** The diff changes `scripts/spec/work.py` (new public `member_name`, `undeclared_names`, `_mention_members`, `note_sites`), which neither this note's nor PB4080's Sites paragraph names (kb/Work PB3640: sites follow the diff). See `kb/Work/PB4080/2026-11-28-sites-and-landing-record-corrections.md`.
- **Self-review.** The required `pr-test-analyzer`, `silent-failure-hunter` and `comment-analyzer` self-review was not run, per the implementer's report.
