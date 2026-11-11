## An unresolved read fails the self-test; it is never keyed as the whole tree

**Supersedes** the fix described in `2026-11-10-absolute-pathspec.md` (commit 2f9ad0bedf), which made every git path
argument `repo_path` could not place inside the repository key the WHOLE tree. That addendum stays as the record of
what was done; this one replaces its rule.

**The owner (2026-11-10), on 2f9ad0bedf:** "This is a bad hack: anything that can't be resolved will count as the whole
tree changed. If it cannot be resolved, there is a problem/bug."

**Why the fallback was wrong.** A self-test that runs git on a path outside the repository, or with a form the tracer
does not model, is either not hermetic (it reads the real checkout or the machine instead of its scratch) or exposes a
gap in `git_reads`. Keying it as the whole tree turned that defect into a self-test that silently re-runs on every
gate: the cost PB5227 exists to remove, with the cause hidden. The same fallback also covered "any other magic" and
every subcommand not in the environment list.

**The rule** (`scripts/self_test_inputs.py`, `scripts/self_tests.py`). `git_reads` has three outcomes:
- RESOLVED: each pathspec keys the directory it reads under. An absolute path inside the repository becomes
  repository-relative, a wildcard is narrowed to its literal prefix's directory, `:(top)`/`:/` read from the root,
  `:(literal)` keeps the pattern literal, `:(exclude)`/`:!`/`:^` narrow nothing (the key is the positive pathspecs':
  a superset by meaning). Before `--`, an ls-files positional is a pathspec, and elsewhere git's own rule applies:
  `<rev>:<path>` reads that path, an argument naming an existing path (or carrying a wildcard or magic) is a pathspec,
  and anything else is a revision.
- THE WHOLE TREE BY MEANING: no pathspec, a wildcard at the root, only exclusions, `show <rev>:`. That is the read set.
- UNKEYED (`UnkeyedRead`, carrying the argv and directory): a subcommand outside `GIT_ENVIRONMENT`, `GIT_NAMES` and
  `GIT_CONTENT`; a global option other than `-c` and the inert ones; magic other than top, glob, literal and exclude
  (`icase` and `attr:` included); an empty pathspec; and a path outside the repository, on another drive, UNC,
  drive-relative or past the top (a backslash on POSIX).
`read_trace` collects them, along with any trace record of no known kind. `Runner.run_one` turns a passing run that has
any into a RED: `UNKEYED READ`, naming the self-test and each command and directory, and saying what to do (make it
hermetic in a scratch repository, or teach `git_reads` the form). It records no PASS. A `--built` run records nothing,
so it is exempt.

**Swept for the same pattern.**
- `Tree.under()` (and through it `entries`, `digest`, `importable`) now raises ValueError for a path that is not
  repository-relative. No caller may pass one, and an old record holding such a key is no PASS (`lookup` counts it unusable).
- A trace record of an unknown kind (or with no tab) used to be read as a file. It is now an UnkeyedRead.
- A listed directory the tree does not know (`D` record, owner None) used to be dropped. It is now keyed `entries:<dir>`,
  so a tracked file appearing there re-runs the self-test.
- An unknown git subcommand used to mean "content under its pathspec". It is now UNKEYED, and `GIT_CONTENT` lists the
  ones that are modelled.
- `rev-parse <rev>:<path>` used to read nothing (environment). It now reads that path's content.
- Reported, not fixed: a read of a path git IGNORES is keyed by its absence from the tree (`-`, `Tree.load` lists no
  ignored file), not by its content. No self-test reads one today (below). Keying it by content is a change to
  `Tree.load` of its own.

- Found while proving this: the merge of main brought PB5226's `_proof_key`, which builds a `Deps` without the import
  key, so every clock-shifted self-test raised TypeError in the runner. The clock key now carries the import key whole,
  because a module appearing under an imported name is new code the proof never ran. `_key_changes` compares it too.

**Measured before committing.** The whole self-test phase (172) ran with this change, traced, and every trace record
was logged: 0 UNKEYED READ. 31 distinct git commands ran inside the real checkout, all `ls-files` (17 self-tests),
`rev-parse` (4) or `--exec-path`, and every one is RESOLVED. No read of a git-ignored path. The one red,
`inert_lane.py` ("scripts/gate.py no longer says `self.host.audits(_no_slot, …)`"), is red at the branch head
2c676e55c8 without this change too. A static grep of scripts/ for magic pathspecs found `:(glob)` (devlog.py,
work.py), `:(exclude)` (arch/capture_oracle.py) and `:!` (orchestrator/coord.py), all modelled. The reviewer's probe,
adapted: 21 pathspecs against real `git ls-files`. Each is keyed with git's match set under the key, or UNKEYED
(`:(icase)`, `:(attr:…)`, outside, `../../x`); none is keyed narrower or constant.
