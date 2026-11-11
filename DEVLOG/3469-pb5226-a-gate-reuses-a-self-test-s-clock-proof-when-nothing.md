## Entry 3469 — 2026-11-11 02:11 PST — PB5226: a gate reuses a self-test's clock proof when nothing that can change it changed; PB5225: the line-ending audit checks only what a branch changes and CI keeps one full sweep; PB5227: self-tests no longer read the real repository, so a gate reuses their PASS

### PB5226: a gate reuses a self-test's clock proof when nothing that can change it changed

In the build-stage1 gate, 28 of 42 self-tests were clock-proved and took 6,419 of 10,022 summed seconds, about half of
it the second run with the clock moved forward 400 days. `enqueue.py`, `land_lane.py` and `gate.py` re-ran that second
run only because `inert_lane.py` and `buildgraph.py` changed, and neither imports a time module. The proof now has its
own key: the self-test's own file, the shim, and the inputs that import `datetime`, `time`, `calendar` or `zoneinfo`,
read a clock, carry a date (in Python, a date literal in the code: review R1-1, which puts `inert_lane.py` back in the
key), or cannot be read. A `--reuse` run whose key matches a recorded proof inside its horizon runs
the self-test once and says `clock proof reused`; the new PASS keeps the old proof's horizon. CI still runs both runs.
The residue, accepted and pinned by a self-test arm (review R1-2): a module with no time import, clock read or date
literal that compares time values handed to it. Today's answer is still tested on every re-run, and CI runs both runs
fresh, so such a change bounces in the merge group instead of reaching main. `PASS_SCHEMA` is 4.

### PB5225: the line-ending audit checks only what a branch changes and CI keeps one full sweep

The owner asked why `line_endings.py --check` was still needed when `.gitattributes` says `* text=auto eol=lf`. The
attributes miss three cases: a lone CR makes git store the file as binary, a Windows tool can leave a CRLF checkout, and
a commit made through the API skips the attributes. So the rule stays, but the gate no longer re-reads all 35,241 tracked
files (13.2 s) to apply it. `gate.py` hands the LINE ENDINGS audit the change's base and measured tree, and the script
reads only the paths `inert_lane.changed` lists plus the checkout's modified and untracked files. WRITERS parses only the
changed live Python files. On this host the change form took 1.5 s against 10 to 30 s for the sweep.

CI keeps one full sweep, in the Linux `audits` job. The Windows-job step and the nightly step are gone: a CI checkout is a
fresh checkout of blobs that sweep just read, LF on every OS while the attributes hold. So a new ATTRIBUTES arm asks
`git check-attr text eol` for every file read and refuses any that is neither `-text` nor text with `eol=lf`: a narrower
rule such as `*.cob -eol`, which only the Windows step would have noticed before (review R1-4). A change that edits a
`.gitattributes` or the check's own modules re-classifies files it does not list, so it runs the whole sweep (R1-3).

### PB5227: self-tests no longer read the real repository, so a gate reuses their PASS

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
keyed as the whole tree: the owner ruled that fallback a hack that hides a hermeticity defect. The PASS schema is 5,
so older records re-run once.

The first merge-group run (43001930001) bounced the branch, because that rule met Linux and the merged tree for the
first time. On Linux, seven self-tests failed: the trace was line-based, so a child's multi-line `python -c` program
was read back as several records of no kind. On Windows the same tracer split the command line on whitespace, which
hid the newlines and cut any argument with a space into fragments. A trace record is now one JSON array per line, the
Windows command line is parsed back into its argument list, and a line that is not one whole record is UNKEYED. The
merge of main brought `git remote get-url origin` (from the forbidden-commands hook), which the grammar now declares as
a configuration read. `self_tests.py`'s clock-key arm failed on both runners: it rewrote a module to the same size
within one second, and CPython reused the stale bytecode. Its fixture now drops the bytecode with each rewrite. The
whole self-test phase is GREEN with no UNKEYED read on this host and under WSL, where the unfixed tip reproduced the
merge group's 8 reds exactly (kb/Work PB5227, PB1957). The next merge of main brought two more git forms from the
hook: `config --get-regexp`, now declared among `config`'s read modes, and `symbolic-ref HEAD`, the checked-out branch,
which no key holds. The hook's self-test now runs every case from a scratch repository on a branch it names.

