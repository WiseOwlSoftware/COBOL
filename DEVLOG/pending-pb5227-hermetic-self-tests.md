## PB5227: self-tests no longer read the real repository, so a gate reuses their PASS

A self-test's PASS is reused while nothing its run read has changed. Several self-tests ran git in the real checkout or
read its register, so their PASS was keyed on the whole tree and every gate re-ran them: `test_adopt_main.py` (848 s in
build-stage1's gate), `devlog.py` (989 s), `gen_ledger.py` (370 s), `test_orchestrate_stop.ps1` and `_steer.ps1` (367 s,
349 s), and the adopt and ledger parts of the orchestrate self-test. Each now runs against its own scratch repository:
the planner's guard, the ledger trend arm, `stop.ps1 -Status` (new `-RepoDir`), and `inventory_ratchet.py` (new `--repo`,
with a test that now proves RED and the excuse end to end). The DEVLOG lint of the real log left devlog's self-test,
since `devlog.py check` runs it at every landing; the PB2605 hook repro moved to `tooling_check.py`.

The landing lane's tick read the register of the script's own checkout, not the one it surveyed; it now reads
`<repo>/kb/Work`. The tracer stopped counting the import system's directory cache as a listing, which had keyed 30 of
the gate's 42 re-runs on the names in `scripts/`, and narrows a wildcard pathspec to its literal directory.

Dropping that listing would have hidden a new file that changes what an import loads, so a PASS now also carries an
IMPORT KEY. It records each module name an import looked for, and the entries of each searched directory that could
answer it: a module or extension, a package directory and its `__init__`. A new `scripts/json.py` re-runs every
self-test that imported `json`. A namespace package gaining `__init__.py` re-runs its importers, as does a module
appearing for a failed `try: import x`. An unrelated new file re-runs none. A submodule imported before its package is
keyed in the package's directory, and the directory is matched with the import system's own case rule.

A git command in the real checkout is read by a declared grammar per subcommand: `ls-files`, `rev-parse`, `log`,
`grep`, with their options and what their arguments are. A history read (`rev-parse HEAD`, `log`) is keyed by the object
each revision names. Every form the grammar does not declare, a path outside the repository, and pathspec magic it does
not model turn a passing self-test RED as an `UNKEYED READ`, naming the command, and record no PASS. They are never
keyed as the whole tree: the owner ruled that fallback a hack that hides a hermeticity defect. A traced run of the
whole self-test phase found none. The PASS schema is 5, so older records re-run once.
