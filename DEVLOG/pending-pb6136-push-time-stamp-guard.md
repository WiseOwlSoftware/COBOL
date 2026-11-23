## A push that would publish a licence stamp is refused before it leaves

PDF-derived text carrying the licensed spec's per-copy stamp reached the public origin twice (PB4814, and commit
`0953e584ed` on 2026-11-23), because `verify_publishable.check_tree` runs inside the gate and a branch is pushed at every
checkpoint. kb/Work PB6136 moves the check to the push. `scripts/hooks/git_pre_push.py` `scan` reads the added lines and
messages of every commit a push would publish (`--not --remotes=<remote>`) against `verify_publishable.tree_shapes()`
and names shape, file, line and commit, never the matched text; it fails closed. The guard hook's new rule 2c calls it
for every `git push` to this repository made through Claude Code, and git's own pre-push hook (`.githooks/pre-push`)
calls it for any other push. `tooling_check.py` now repairs `git config --local core.hooksPath .githooks` at session
start, because the host's global `core.hooksPath` names a directory that does not exist and git ran no hook here.
Self-tests: `forbidden_commands` 387/387 GREEN (19 new), `test_pre_push.py` 6 cases and 14 expectations, `tooling_check`
GREEN. The GitHub purge of `0953e584ed` (ticket #5047892) is still pending.
