## The gate after the bounce fix (run 000004-20261111T121248Z): two git forms main's hook brought

The gate at f8318e3c70 (bca30fcb55 plus a merge of origin/main) went RED before any test ran. One self-test,
`scripts/hooks/test_forbidden_commands.py`, was 267/267 GREEN but had an UNKEYED READ on two git commands, both new
with main's latest changes to `hooks/forbidden_commands.py` (PB5234/PB5269, `_push_urls`). Both ran in the real
checkout:

- `git config --get-regexp ^url\..*\.(push)?insteadof$`: a configuration READ the grammar did not declare. GIT_GRAMMAR
  now gives `config` explicit read modes (`GitGrammar.modes`). Each mode takes a set number of arguments:
  - `--get`, `--get-all`, `--get-regexp`: a name or pattern, and optionally a value pattern;
  - `--list`/`-l`: none;
  - no mode flag: exactly one name.

  Each one reads nothing a key holds, and so does `--show-origin`, `--show-scope`, `--name-only` or a scope flag beside
  it. Every writing form stays undeclared: `--add`, `--unset-all`, a second positional with no mode, two modes at once,
  `--file`. There are new rows in both grammar arms.
- `git symbolic-ref --short -q HEAD`: the checked-out BRANCH NAME. That is no content of the tree and no key holds it
  (the same reasoning as review finding R3-2 for `rev-parse --abbrev-ref HEAD`), so it stays UNKEYED, and a row now pins
  that. The defect was in the self-test. It ran the hook with the payload `cwd` set to the real checkout, so a push
  naming no repository was resolved from the developer's or the gate's own branch. The payload `cwd` is how Claude Code
  tells the hook where the session is. Every case now runs the hook from `SESSION`, a scratch repository on the branch
  `unit-work` whose origin is this repository's.

Two reads of the real checkout remain, and both are by design. Each is `remote get-url`, a configuration read that is
declared and holds no key:
- the hook's own `_ROOT_DIR` origin URL: "this repository" means the hook's own checkout;
- the case `git -C <this checkout> push origin HEAD`, which is meant to name this checkout.

The test also reads the real checkout's origin URL to set up its scratch repositories, through the same form.

**Sweep.** main changed four files under `scripts/` since bca30fcb55: `hooks/forbidden_commands.py`,
`hooks/shell_location.py`, `hooks/test_forbidden_commands.py` and `hooks/test_shell_location.py`. Only
`forbidden_commands.py` runs git. Each hook test was traced on this host and under WSL: 0 UNKEYED. The other hooks that
run git (`readonly_repo.py`, `session_start.py`, `tooling_check.py`, `unpushed.py`) were already clean in both traced
phases.
