## Review round 2 (selftest-cost at 05d1cc135b): six findings in the tracer, all fixed

Reviewer's probes: `E:\COBOL-coord\scratch\review-w20261995-a\r2b\` (probe_imports.py, probe_submodule.py,
probe_unkeyed.py); records in `review-w20261995-a.jsonl`.

**R2-1 (medium): a submodule imported before its package was keyed nowhere.** `import a.b` raises the audit event
`import` for `a.b` BEFORE it imports `a` (observed order `['pkg.absent', 'pkg']`), so the tracer found no `a` in
`sys.modules` and recorded the name in no directory. Scenario: `try: import nspkg.absent`, then adding
`nspkg/absent.py` that raises `SystemExit(3)`: `Deps.changed` gave `[]`, and a fresh run exited 3. Fix: the tracer
holds a dotted name until its parent is in `sys.modules`. A package is there, with its `__path__`, before its own code
runs. The pending names are placed at every later import event and once more when the process ends. Each one is then
recorded in every entry of the parent's `__path__` (a namespace package has several).

**R2-2 (medium): a namespace package gaining `__init__.py` was not seen.** `Tree.importable` keyed `name` and
`name<suffix>`. The import system stats `name/__init__<suffix>` and never consults its directory cache for it. Fix: for
an imported name that is a directory, the key also holds its `__init__<suffix>` entries, for every suffix importlib
gives. The siblings are armed too: a regular package losing its `__init__` (it becomes a namespace package), and a
directory appearing where a module file is.

**R2-3 (medium) and R2-4 (low), one root: `git_reads` scanned arguments heuristically.** `git grep -n 'kb/Work/.*md'`
read its PATTERN as a pathspec and keyed `content:kb/Work`, while git searched the whole tree. `ls-files --with-tree
HEAD x` keyed `paths:HEAD`. `log --format=%H -- <path>` read nothing. Fix: a DECLARED GRAMMAR per subcommand
(`GIT_GRAMMAR`) gives its options, which options take a value (a value is never an argument), what its positionals
are, and what it reads. Declared: `ls-files` and `rev-parse` (the self-tests run these in the real checkout), plus
`log`, `grep` and the read of one `config` key, which are cheap to model exactly. Any other subcommand, option or
argument form is UNKEYED, the rule 8da48ffcb3 built. Arms cover each of the reviewer's three forms and one undeclared
option per declared subcommand.

**The history decision.** A command that reads history answers from commits, which no working-tree key holds, and a
ref moves on every commit. It is keyed by what it names instead. Each revision it reads is resolved to its object id
(`rev:<expression>`, `Tree.revision`, one `git cat-file --batch-check` per tree); a `log` that names no revision reads
`HEAD`. An object is immutable, so reusing a PASS while every named revision resolves to the same object is sound.
- `rev-parse HEAD` re-runs on every new commit.
- `log -- <path>` re-runs when HEAD moves.
- `HEAD:<path>` re-runs only when that blob changes.

The cost falls on `orchestrator/test_plan_wave.py`. It reads `HEAD`, `HEAD^{tree}`, `origin/main` and a commit's tree
in the real checkout, so it now re-runs on every commit, where it used to be wrongly reused. Making it hermetic would
end that, and is a lead for the next PB5227 round. The location queries (`--show-toplevel`, `--git-common-dir`,
`--exec-path`) read nothing.

**R2-5 (low): a mixed-case sys.path directory created later was missed on Windows.** The tracer normcases paths, and
`Tree.unfold` restores case only through tracked directories, so a directory that did not exist yet was keyed in lower
case. The comparison was case-sensitive. The CASE RULE now is the import system's own on each platform: the directory
is matched as the file system matches a sys.path entry (`os.path.normcase`, so without case on Windows); a module or
package entry exactly, since FileFinder compares the exact name without PYTHONCASEOK; and an `__init__` file as the
file system finds it, since it is stat'ed. The rule is applied when comparing (`Tree._real_dir`), so a key recorded in
either case finds the directory. Arm: `scripts/o/Gen/gfoo.py` created later changes the key, and `GFOO.py` does not
(the import would not load it).

**R2-6 (low): the DEVLOG entry was stale.** `DEVLOG/pending-pb5227-hermetic-self-tests.md` now covers the import key,
the declared grammar, history keyed by revision, and the UNKEYED READ red.

**The schema.** The record's meaning changed (history keys, `__init__` entries), so `PASS_SCHEMA` is 5. A PASS recorded
under the narrower rule is not reused.

**Proof** (at 05d1cc135b plus this change, in a scratch worktree, while the gate ran in the branch's own):
- `python scripts/self_test_inputs.py --self-test`: ALL GREEN.
- `python scripts/self_tests.py --self-test`: ALL GREEN.
- `python scripts/self_test_clock.py --self-test`: ALL GREEN.
- The reviewer's probes against this tree:
  - probe_imports: `nspkg/__init__.py` added → `['imports:scripts/nspkg/__init__.py']`; `scripts/Gen/foo.py` added →
    `['imports:scripts/gen/foo.py']`.
  - probe_submodule: `nspkg/absent.py` added → `['imports:scripts/nspkg/absent.py']`.
  - probe_unkeyed: work.py, sharedfile.py, gate.py and spec_pdf.py are keyed, with no UNKEYED read.
  - The pathspec probe: every pathspec is keyed over git's match set or UNKEYED.
- The whole self-test phase (172), traced, with every trace record logged: **0 UNKEYED READ**. The 31 distinct git
  commands run inside the checkout are all keyed:
  - `ls-files` (17 self-tests);
  - `rev-parse` location queries (`gate.py`, `hooks/test_readonly_repo.py`, `test_fuzz_triage.py`);
  - `--exec-path`;
  - `test_plan_wave.py`'s `rev-parse HEAD`, `HEAD^{tree}`, `--short origin/main` and `<sha>^{tree}`, now `rev:` keys.

  No git-ignored path was read. Two results are artefacts of running that checkout inside the session's scratch
  directory. `hooks/test_own_scratch.py` was RED: it refuses a write under that directory, and it was RED there at the
  unchanged head too. `guard-verify.sh` (bash, which the tracer never sees) was stopped as HANG. Both were GREEN in
  the earlier full run in the branch's own worktree.
