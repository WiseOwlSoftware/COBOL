**2026-11-10, resume unit: branch `worktree-wf_226daebb-cb5-1` ABANDONED,** superseded by `worktree-wf_226daebb-cb5-2` (pull
request #1487, on main at 447a19e364). Its one commit fef5a9c2a1 answered review findings R1-1 (a `populations_green` arm for a
record RED for an audit and a non-audit reason) and R1-2 (the re-derivation residue wording). Main carries both in its own form:
`scripts/gate.py` has the self-test arm "a run RED for an audit AND the post-build self-tests, every host green (kb/Work PB5205
review R1-1)" and the residue "an uncommitted edit made while the run ran and undone at any time before this re-derivation". The
branch's gate never ran (NOT RUN [toolchain], report w20261989a-PB5205-report.md), and it carries nothing main lacks.
