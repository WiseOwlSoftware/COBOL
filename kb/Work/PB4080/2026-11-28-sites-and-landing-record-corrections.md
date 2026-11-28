## Later finding (2026-11-28): Sites omit work.py; the Landed section names the wrong branch; the self-review was not run

Source: `E:/COBOL-coord/scratch/reports/w20262174a-PB3259-report.md`, Review R1-1, R1-2 and R1-3 (low and info). Facts, for the record:

- **Sites.** The landed diff changes `scripts/spec/work.py` (new public `member_name`, `_undeclared_names` renamed `undeclared_names`, `_mention_members` and `note_sites` rewired). This note's Sites paragraph (line 29) names only `scripts/arch/file_census_notes.py` (`render_r2`); sites follow the diff (kb/Work PB3640). The missing site is `scripts/spec/work.py` (`member_name`, `undeclared_names`, `_mention_members`, `note_sites`).
- **Branch.** The Landed section (line 37) says branch `worktree-agent-a2787d9fe53a79a18`, the predecessor branch that never landed. The commit landed from `worktree-wf_19b8f6c4-960-1` (the predecessor's commit cherry-picked onto main, plus a gate-red fix, head `9ee04e92e0`).
- **Self-review.** The dispatch spec required a self-review against the `pr-test-analyzer`, `silent-failure-hunter` and `comment-analyzer` agents. The implementer's report says they were not run ("branch reviewer should cover"), and `DEVLOG/pending-r2-filing-mechanism-identity-and-site-blocks.md` (line 13 at review) does not say so.
