**The Linux failure of two push cases at a branch head is gone at main (2026-11-11).**
- The PB5199 review (E:/COBOL-coord/scratch/reports/w20262000d-PB5199-report.md) saw `scripts/hooks/test_forbidden_commands.py` fail 2 of
  267 cases on the WSL Linux gate at branch head 18246f5b0: `git push https://github.com/WiseOwlSoftware/COBOL x` and the
  `git push --repo=https://...` case. Windows passed at the same head.
- At origin/main e3c9e8551a, with PB5269 landed, the self-test passes on both hosts:
  Windows: `forbidden_commands self-test: 267/267 GREEN`;
  Linux (a fresh clone of the public repository in WSL, `python3 scripts/hooks/test_forbidden_commands.py`):
  `forbidden_commands self-test: 267/267 GREEN`.
- So the branch-head failure was the pre-PB5269 parse of the destination (PB5262's mechanism, closed by PB5269) and nothing
  remains on main. No new note.
- The Linux run used a fresh clone because the Windows worktree's gitfile does not resolve inside WSL, which makes
  `ScratchRepo(REPO).git("remote", "get-url", "origin")` at the top of the test fail there; that is an artifact of running a
  worktree from WSL, not a defect of the test.
