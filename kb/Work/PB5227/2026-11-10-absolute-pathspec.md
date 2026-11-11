## Review R1-1: an absolute or escaping git pathspec keyed a digest that never changes

**The false green** (review of 2d7f83b1bd, finding R1-1, medium). `git_reads` joined a pathspec onto the command's
directory with `posixpath`, so an ABSOLUTE pathspec inside the worktree, which git accepts
(`git ls-files -- E:/<repo>/scripts/*.py`), was keyed `paths:E:/<repo>/scripts`. `Tree.under()` of that string is
empty, the digest is constant, and the PASS was reused whatever changed under `scripts/`. A literal absolute pathspec
had the hole before 2d7f83b1bd; the wildcard narrowing extended it to wildcards, which had keyed the whole tree. The same
shape: a pathspec or `show <rev>:<path>` climbing past the top (`../../x`) keyed `../x`, and `show HEAD:` keyed `.`.

**The root pattern.** `Tree.under(d)` and `Tree.digest(kind, d)` of a path that names nothing tracked return the empty
list and its digest, silently. For a path inside the repository that is right (a file appearing there changes it); for
a path no tracked file can ever appear under it is a key that never changes. So every path `git_reads` keys must be
placed inside the repository first, or be the whole tree.

**The fix** (`scripts/self_test_inputs.py`). `repo_path(path, where, root)` gives the repository-relative path a git
path argument names, or None: an absolute path (`/…`, `X:/…`, `X:\…`, UNC) is made relative to the root
(`os.path.relpath`, case-insensitive on Windows) when inside it; one outside it, on another drive, drive-relative
(`X:x`), climbing past the top, or (on POSIX) containing a backslash, git's glob escape, is None. `git_reads` takes the
repository root (its one caller, `read_trace`, passes `tree.repo`) and keys the whole tree for any None, literal and
wildcard alike; `show` paths go through the same function (from the root, or from the directory for `./`/`../`, as git
resolves them). The other keys were swept: trace `D` and `I` directories and git directories come from the tracer's
`_inside` (always inside the repository), and STATIC directories from `Deriver.resolve`, which drops `..`.

**Proof.** `python scripts/self_test_inputs.py --self-test`: ALL GREEN, with a new arm of 11 rows (absolute wildcard and
absolute literal inside the repository, either separator; absolute path and wildcard outside; `../../x`; `../*.py`;
`/*.py`; `show HEAD:../x`, `HEAD:./x`, `HEAD:`) and an arm pinning that `Tree.under` of an unresolvable path is empty.
The reviewer's probe, rerun against the new signature over this worktree with real `git ls-files --full-name`: 13
pathspecs (including `<repo>/scripts/*.py`, `<repo>\scripts\spec`, `<repo>/docs/*.md` from `scripts`,
`../../*.md` from `scripts/spec`), 0 NOT SUPERSET; `<repo>/scripts/*.py` matched 243 files and is keyed `paths:scripts`.
