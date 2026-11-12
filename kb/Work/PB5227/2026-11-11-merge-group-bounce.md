## The merge-group bounce of PR #1499 (2026-11-11): three causes, each fixed at its root

PR #1499 (selftest-cost, tip a51560c8b2, tree equal to f83a7cc7f4) left the merge queue when merge-group run
43001930001 failed two jobs. Logs: `E:\COBOL-coord\scratch\pr1499-audits-failed.log` (Linux `audits`, which runs every
self-test) and `E:\COBOL-coord\scratch\pr1499-winselftests-failed.log` (GitHub's Windows runner).

- **Linux:** 8 of 152 self-tests RED. `detached.py`, `gate.py`, `hooks/test_forbidden_commands.py`, `prune_worktrees.py`,
  `self_test_clock.py`, `spec/inventory_view.py` and `standard_streams.py` passed but were `UNKEYED READ` (exit 0). The
  eighth, `self_tests.py --self-test`, failed an arm (exit 1).
- **Windows runner:** 2 of 170 RED: `hooks/test_forbidden_commands.py` with one `UNKEYED READ`, and the same
  `self_tests.py` arm.

The traced phase that cleared the branch ran on this Windows host only, at b2070e3574, before the branch merged main.

### Cause 1: the trace format split a record that held a newline (Linux, seven self-tests)

The tracer wrote one record per line as tab-separated text (`X\t<path>`). A child's argument holding a newline, such as
a multi-line `python -c` program or a commit message with trailers, was written raw. `read_trace` then read it back as
several lines of no kind, which is UNKEYED. Every "trace record" read in the Linux log is one: `detached.py`'s
`g = pathlib.Path(...)`, `gate.py`'s planted verdict program, `self_test_clock.py`'s probe, `prune_worktrees.py`'s
`Train-Cluster: E` trailers, `inventory_view.py`'s and `standard_streams.py`'s `-c` programs. Before the UNKEYED rule,
those fragments were silently keyed as untracked files.

**Why this host never saw it.** It is the same defect in another shape. On Windows, `subprocess` turns the argument
list into one command line (`list2cmdline`) before it raises the `subprocess.Popen` audit event. The tracer split that
string on whitespace, so the newlines disappeared, and so did every argument that held a space: a path such as
`scripts/o/a b.txt` was recorded as two fragments, and its read was lost.

**Fix (`scripts/self_test_inputs.py`).**
- **Writer:** the tracer (the only writer; no PowerShell, bash or git-wrapper path writes a trace) writes each record as
  one JSON array per line (`TRACE_SHAPES`: `["F", path]`, `["I", dir, name]`, `["G", dir, [args]]`). JSON escapes every
  newline, so no record can be split. A `G` record now carries its arguments as a list, no longer joined with `chr(31)`.
- **Argv on Windows:** the tracer parses the command line back with `_argv`, by the rules `list2cmdline` quotes for
  (CommandLineToArgvW's). POSIX already passes the list.
- **Reader:** `read_trace` reads each line through one function, `trace_record`. A line that is not one whole record is
  UNKEYED: a record cut short, a line in the old tab format, an unknown kind, a field of the wrong type.
- **Fixtures:** `trace_line` writes the record line for the fixtures that plant a trace (`self_tests.py`).

New arms in `self_test_inputs.py --self-test`:
- A traced child given a multi-line program, `scripts/o/a b.txt`, and `q\"u o` (a backslash before a quote, beside a
  space) records each as one read and nothing UNKEYED. With the old whitespace split restored as a mutation, this arm
  FAILS on this host.
- A torn record, an old-format line, a record of no kind and one of the wrong shape are each UNKEYED.

### Cause 2: `git remote get-url origin` had no declared grammar (Linux and the Windows runner)

`hooks/forbidden_commands.py` asks for this repository's remote URL when it judges a unit's push. That reached the
branch with the merge of main (PB5234, b2d8d77726), after the traced run at b2070e3574. So this was a MERGE red, not a
runner difference: the merged tree had never been traced here.

The Linux log shows a second read, `in E:/claude-skills`. On Linux the hook test's Windows-shaped cwd is a relative
path, so it lands inside the repository. On Windows it is absolute and outside the repository, so the tracer never
records it. That is why the Windows runner shows one read and Linux two.

**Fix:** GIT_GRAMMAR declares `remote` with exactly one verb, `get-url <name>` (`GitGrammar.verb`; flags `--push`,
`--all`). Like `config`'s one-key read, it reads git's configuration, not the tracked tree, so it reads nothing a key
holds. `remote -v`, `remote add`, `remote get-url` with no name and an undeclared option stay UNKEYED (new rows in the
grammar arms).

### Cause 3: stale bytecode in the clock-key arm (both runners; a flake of the fixture, deterministic on a fast host)

Arm R1-1 of `self_tests.py --self-test` (PB5226's clock key) rewrites `helper_plain.py` from `EXPIRES = '<today+900>'`
to `'<today+100>'`. The two versions have the same size. CPython trusts a `.pyc` whose source still has the recorded
size and whole-second mtime, so on a runner that makes both edits inside one second, the shifted run imported the OLD
date and stayed GREEN. Result: `got ('GREEN', ['0', '400'], True), want ('RED', ...)`.

**Reproduced under WSL** at the unfixed tip f2f41442e0: `self_tests.py --self-test: 1 FAILED`, the same arm, exit 1.
It never failed on this slower host.

**Fix:** `self_tests.py` `_rewrite(module, text)` writes a planted module and deletes its compiled bytecode. Every arm
that edits a planted module between runs now uses it: the clock-key `edit` and the `helper_b`/`helper_u` import arms.
Those import arms assert only re-run state, which the blob key decides, so their race was latent, not a wrong answer.

### Proof

The commit message carries the proof's verdict lines: on this host, the full self-test phase in the branch worktree and
the self-tests of every touched script; under WSL, the whole phase as CI's `audits` job runs it, `python3
scripts/self_tests.py` in a full-history clone at `~/pb5227-bounce`, toolchain Python 3.15.2.

This bounce is also recorded against the pipeline as class (b) and class (e): `kb/Work/PB1957/2026-11-11-pr1499-bounce.md`.
