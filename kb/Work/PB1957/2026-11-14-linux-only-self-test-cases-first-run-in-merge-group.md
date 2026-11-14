## 2026-11-14: PR #1640 bounced twice on a self-test case that needs Linux, class (b), first run of Linux

Pull request #1640 failed merge-group runs 43623073747 and 43624633970 in the audits job. `scripts/selftests/inert_lane/test_watch.py` case 04 needed `scripts/mod.py`, which case 03 had created until kb/Work PB5397 split the cases. The inotify cases pass off Linux "with their reason", so the Windows gate recorded the file GREEN and the branch's record claimed cases that never ran. The merge group's Linux audits job was their first run. Fixed on branch `worktree-wf_2e8662c7-06c-15` at deb2c04b6e06.

This is a class (b) red (Linux, first run), so it is a finding against this note's record of that class (CLAUDE.md, "THE RULE"). The mechanism is filed as kb/Work PB5522 (a case that needs another host must be recorded skipped, not passed). Figures are the filing instruction's; not re-run here.
