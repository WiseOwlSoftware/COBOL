#!/usr/bin/env python3
"""git_batch.py — THE BATCHED GIT READS OF THE SCRIPTS: one git process for many objects (kb/Work PB3582).

    import git_batch                                         # a script under scripts/ (sys.path holds scripts/)
    graph = git_batch.Ancestry.read(repo, tips)              # every commit beyond origin/main the tips reach
    graph.beyond(tip)                                        # `git rev-list <tip> ^origin/main`, from memory
    graph.on_base(tip)                                       # `git merge-base --is-ancestor <tip> origin/main`
    git_batch.resolve(repo, names)                           # `git rev-parse --verify -q <name>^{commit}`, each
    git_batch.diff_names(repo, [(commit, parent), ...])      # `git diff --name-only --no-renames <parent> <commit>`
    git_batch.diff_patches(repo, [(commit, parent), ...])    # `git diff -U0 --no-renames <parent> <commit>`
    git_batch.object_ids(repo, ["<rev>:<path>", ...])        # `git rev-parse -q --verify <rev>:<path>`, each
    git_batch.patch_ids(repo, [commit, ...])                 # `git show <commit> | git patch-id --stable`, each
    git_batch.tree_entries(repo, ["<rev>:<dir>", ...])       # `git ls-tree <rev>:<dir>`, each: {file name: object id}
    git_batch.blobs(repo, [object id, ...])                  # `git cat-file blob <object id>`, each: (object id, bytes), one at a time

    python scripts/git_batch.py --self-test                  # each reader against the one-process-per-object form

⛔ WHY (kb/Work PB3582, measured on merge-group run 40004159292, one commit on both runners). A Windows process start
costs tens of milliseconds where Linux pays about one, so a script that asks git one question per object (a branch, a
commit, a pair, a file) is bounded by process creation, not by its own logic: the script self-tests summed 1,444 s on
CI's Windows runner and 144 s on Linux, and GIT_TRACE2 over prune_worktrees.py's self-test counted 4,636 git processes.
The same per-object loop is the loop's and the session probe's runtime (the survey, the landing check, the planner's
in-flight sets), so it costs every session, not only the self-tests. Each reader here answers a whole list of questions
from ONE git process, so a caller's process count is its number of questions KINDS, not its number of objects.

WHAT EACH READER GUARANTEES
  resolve      one `git cat-file --batch-check` over `<name>^{commit}`: a name that is not a commit maps to None,
               never to a wrong commit; the answer count must equal the question count, or GitError.
  Ancestry     one `git log --format=%H %T %ct %P%x00%s --stdin <tips> --not <base>` (after one resolve of the tips
               and the base): the commits beyond the base with their parents, trees, committer dates and subjects. A tip that does not resolve RAISES
               (silently absent would read as "on the base", which the survey reads as MERGED and --apply deletes).
               A missing base excludes nothing (a repository that never fetched origin/main: every commit is beyond
               it), as `git rev-list <tip>` with no exclusion would. Its answers hold for the tips it read and every
               commit they reach; `extend` reads more tips into the same graph.
  diff_names / diff_patches
               one `git diff-tree --stdin --always -r --no-renames` over `<commit> <parent>` lines, so each pair
               gets exactly one header line (its commit) in input order, even when its diff is empty; a pair's
               answer is its block. A root commit's pair names git's empty tree as its parent.
  object_ids   one `git cat-file --batch-check` over `<rev>:<path>` lines; an absent path maps to ''.
  tree_entries / blobs
               one `git cat-file --batch` each: the files of many `<rev>:<dir>` trees, then the content of the distinct
               object ids they name (a note that did not change between two commits is read once, not once per commit).
               A directory a revision lacks maps to None; an id that is no blob raises.
  patch_ids    one `git diff-tree --stdin --root -p` piped into one `git patch-id --stable`: each commit's patch id
               (the key `git cherry` matches on); a merge commit and an empty commit have none and are absent.
Every reader raises GitError when git fails: an unmeasured answer is never read as an empty one.
"""
from __future__ import annotations

import pathlib
import subprocess
import sys
import tempfile
from typing import Callable, Iterable, Iterator, Sequence
from standard_streams import ensure_utf8

BASE = "refs/remotes/origin/main"
EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"   # git's empty tree: a root commit's diff is taken against it


class GitError(RuntimeError):
    pass


def run(repo: pathlib.Path | str, *args: str, stdin: str | None = None) -> subprocess.CompletedProcess:
    """`git -C <repo> <args>`, text in and out as UTF-8 (errors replaced), output captured, unchecked."""
    return subprocess.run(["git", "-C", str(repo), *args], input=stdin, capture_output=True, text=True,
                          encoding="utf-8", errors="replace")


#: A caller's own git runner, `git(*args, cwd=<repo>, stdin=<text>)` (prune_worktrees.git's shape): a self-test's fake
#: answers the readers' batched commands through it. None runs git here (`run`).
Runner = Callable[..., subprocess.CompletedProcess]


def _checked(repo: pathlib.Path | str, *args: str, stdin: str | None = None, git: Runner | None = None) -> str:
    r = git(*args, cwd=repo, stdin=stdin) if git else run(repo, *args, stdin=stdin)
    if r.returncode != 0:
        raise GitError(f"git {' '.join(args[:3])} in {repo} failed ({r.returncode}): {r.stderr.strip()[:300]}")
    return r.stdout


def _batch_check(repo: pathlib.Path | str, specs: Sequence[str], git: Runner | None = None) -> list[tuple[str, str] | None]:
    """[(object id, type) | None] per spec, from ONE `git cat-file --batch-check` (`<spec> missing` -> None)."""
    if not specs:
        return []
    bad = [s for s in specs if not s or "\n" in s]
    if bad:
        raise GitError(f"git cat-file --batch-check cannot ask about {bad[0]!r}")
    lines = _checked(repo, "cat-file", "--batch-check=%(objectname) %(objecttype)",
                     stdin="".join(f"{s}\n" for s in specs), git=git).splitlines()
    if len(lines) != len(specs):
        raise GitError(f"git cat-file --batch-check answered {len(lines)} lines for {len(specs)} questions")
    out: list[tuple[str, str] | None] = []
    for line in lines:
        oid, _, kind = line.rpartition(" ")
        out.append(None if kind in ("missing", "ambiguous") else (oid, kind))
    return out


def resolve(repo: pathlib.Path | str, names: Iterable[str], git: Runner | None = None) -> dict[str, str | None]:
    """{name: its full commit id, or None when it names no commit} for every name, from ONE process."""
    uniq = list(dict.fromkeys(n for n in names if n))
    return {n: (a[0] if a and a[1] == "commit" else None)
            for n, a in zip(uniq, _batch_check(repo, [f"{n}^{{commit}}" for n in uniq], git))}


def object_ids(repo: pathlib.Path | str, specs: Sequence[str], git: Runner | None = None) -> list[str]:
    """[the object id of each `<rev>:<path>`, '' when absent], from ONE process."""
    return [a[0] if a else "" for a in _batch_check(repo, list(specs), git)]


def _object_stream(repo: pathlib.Path | str, specs: Sequence[str]) -> Iterator[tuple[str, str, bytes] | None]:
    """(object id, type, content) | None per spec, in the questions' order, from ONE `git cat-file --batch` (`<spec>
    missing` and an ambiguous abbreviation -> None). The answers are written to a temporary file and read back one at a
    time, so a caller that reduces each object as it arrives holds one object, not the whole output. A short or torn
    output raises GitError."""
    if not specs:
        return
    bad = [s for s in specs if not s or "\n" in s]
    if bad:
        raise GitError(f"git cat-file --batch cannot ask about {bad[0]!r}")
    with tempfile.TemporaryFile() as out:
        r = subprocess.run(["git", "-C", str(repo), "cat-file", "--batch"], stdout=out, stderr=subprocess.PIPE,
                           input="".join(f"{s}\n" for s in specs).encode("utf-8"))
        if r.returncode != 0:
            raise GitError(f"git cat-file --batch in {repo} failed ({r.returncode}): {r.stderr.decode('utf-8', 'replace').strip()[:300]}")
        out.seek(0)
        for answered in range(len(specs)):
            try:
                header = out.readline().split()
                if header[-1] in (b"missing", b"ambiguous"):
                    yield None
                    continue
                size = int(header[2])
                data = out.read(size)
                if len(data) != size or out.read(1) != b"\n":
                    raise ValueError("the output ends inside an object")
            except (ValueError, IndexError) as e:
                raise GitError(f"git cat-file --batch answered {answered} objects for {len(specs)} questions: {e}") from e
            yield header[0].decode("ascii"), header[1].decode("ascii"), data


def tree_entries(repo: pathlib.Path | str, specs: Sequence[str]) -> list[dict[str, str] | None]:
    """[{file name: its object id} of each `<rev>:<path>` directory, or None when the revision has no such directory],
    from ONE process (`git ls-tree <rev>:<path>` for each, without the process). Files only: a subdirectory or a
    submodule is not an entry. A spec that names a file, not a directory, raises GitError."""
    out: list[dict[str, str] | None] = []
    for spec, found in zip(specs, _object_stream(repo, specs)):
        if found is None:
            out.append(None)
            continue
        oid, kind, data = found
        if kind != "tree":
            raise GitError(f"{spec} is a {kind}, not a tree")
        width, entries, at = len(oid) // 2, {}, 0   # the raw object id is half as long as its hex (20 bytes SHA-1, 32 SHA-256)
        while at < len(data):
            sp = data.index(b" ", at)
            nul = data.index(b"\0", sp)
            if data[at:sp] in (b"100644", b"100755", b"120000"):
                entries[data[sp + 1:nul].decode("utf-8", "replace")] = data[nul + 1:nul + 1 + width].hex()
            at = nul + 1 + width
        out.append(entries)
    return out


def blobs(repo: pathlib.Path | str, oids: Iterable[str]) -> Iterator[tuple[str, bytes]]:
    """(object id, its content) for every distinct id, from ONE process, one blob at a time: a caller that reduces each
    blob as it arrives (counts, a parsed frontmatter) holds one blob, however many ids it asked. An id that names no
    blob raises GitError: a caller that read it out of a tree must find it."""
    uniq = list(dict.fromkeys(o for o in oids if o))
    for oid, found in zip(uniq, _object_stream(repo, uniq)):
        if found is None or found[1] != "blob":
            raise GitError(f"{oid} is not a blob in {repo}")
        yield oid, found[2]


def patch_ids(repo: pathlib.Path | str, commits: Iterable[str], git: Runner | None = None) -> dict[str, str]:
    """{commit: its stable patch id} for every commit that HAS one, from TWO processes whatever the count
    (`git diff-tree --stdin --root -p --no-renames | git patch-id --stable`). Two commits that make the same change
    (a cherry-pick, a replay onto another base) share an id: it is the key `git cherry` matches on, read for a whole
    list at once. A merge commit and a commit whose diff is empty have no patch, so they are absent from the answer."""
    uniq = list(dict.fromkeys(c for c in commits if c))
    if not uniq:
        return {}
    diffs = _checked(repo, "diff-tree", "--stdin", "--root", "-p", "--no-renames",
                     stdin="".join(f"{c}\n" for c in uniq), git=git)
    out: dict[str, str] = {}
    for line in _checked(repo, "patch-id", "--stable", stdin=diffs, git=git).splitlines():
        pid, _, commit = line.partition(" ")
        if pid and commit:
            out[commit.strip()] = pid
    return out


class Ancestry:
    """The commits beyond a base that a set of tips reach, with each one's parents and tree."""

    def __init__(self, repo: pathlib.Path | str | None, base: str | None,
                 parents: dict[str, tuple[str, ...]], trees: dict[str, str] | None = None,
                 names: dict[str, str | None] | None = None):
        self.repo, self.base = repo, base
        self._parents, self._trees = dict(parents), dict(trees or {})
        self._times: dict[str, int] = {}
        self._subjects: dict[str, str] = {}
        self._names: dict[str, str | None] = dict(names or {})
        self._beyond: dict[str, frozenset[str]] = {}
        self._base_tree: str | None = None

    @classmethod
    def read(cls, repo: pathlib.Path | str, tips: Iterable[str], base: str = BASE) -> "Ancestry":
        """The graph of `tips` beyond `base`: one resolve of the tips and the base, one `git log`."""
        graph = cls(repo, base, {})
        graph.extend(tips)
        return graph

    @classmethod
    def of(cls, parents: dict[str, Sequence[str]], names: dict[str, str] | None = None,
           subjects: dict[str, str] | None = None) -> "Ancestry":
        """A graph from a known parent map (a self-test's fake: no repository, no git)."""
        graph = cls(None, None, {c: tuple(p) for c, p in parents.items()}, names=dict(names or {}))
        graph._subjects.update(subjects or {})
        return graph

    def sha(self, name: str) -> str | None:
        """The full commit id of `name` (a branch, an abbreviation, a full id), resolved once and cached; None when it
        names no commit."""
        if name not in self._names:
            self.names(name)
        return self._names[name]

    def names(self, *names: str) -> dict[str, str | None]:
        """Resolve every name not yet known in ONE process; {name: full commit id | None}."""
        todo = [n for n in dict.fromkeys(names) if n and n not in self._names]
        if todo:
            if self.repo is None:
                self._names.update({n: (n if n in self._parents else None) for n in todo})
            else:
                self._names.update(resolve(self.repo, todo))
        return {n: self._names.get(n) for n in names}

    def extend(self, tips: Iterable[str]) -> None:
        """Read `tips` (and every commit they reach beyond the base) into the graph: one resolve, one `git log` for
        the tips not already in it. A tip that names no commit raises GitError."""
        tips = list(dict.fromkeys(t for t in tips if t))
        known = self.names(*tips, *([self.base] if self.base else []))
        missing = [t for t in tips if not known[t]]
        if missing:
            raise GitError(f"not a commit in {self.repo}: {', '.join(missing[:5])}")
        todo = [known[t] for t in tips if known[t] not in self._parents]
        if not todo or self.repo is None:
            return
        base = known.get(self.base) if self.base else None
        out = _checked(self.repo, "log", "--format=%H %T %ct %P%x00%s", "--stdin",
                       stdin="".join(f"{c}\n" for c in todo) + (f"--not\n{base}\n" if base else ""))
        for line in out.splitlines():
            ids, _, subject = line.partition("\0")
            commit, tree, when, *parents = ids.split()
            self._parents[commit], self._trees[commit], self._times[commit] = tuple(parents), tree, int(when)
            self._subjects[commit] = subject
        self._beyond.clear()

    def subject(self, commit: str) -> str:
        """The subject line of a commit beyond the base."""
        return self._subjects[self._full(commit)]

    def time(self, commit: str) -> int:
        """The committer date (seconds since the epoch) of a commit beyond the base."""
        return self._times[self._full(commit)]

    def newest_first(self, commits: Iterable[str]) -> list[str]:
        """`commits` (beyond the base) newest first by committer date, as `git rev-list` lists them."""
        return sorted(commits, key=lambda c: (-self._times.get(c, 0), c))

    def base_tree(self) -> str:
        """The base's tree id ('' when the base names no commit), read once."""
        if self._base_tree is None:
            base = self.sha(self.base) if self.base and self.repo is not None else None
            self._base_tree = object_ids(self.repo, [f"{base}^{{tree}}"])[0] if base else ""
        return self._base_tree

    def on_base(self, commit: str) -> bool:
        """`commit` (a tip this graph read, or a commit one reaches) is the base or an ancestor of it."""
        return self._full(commit) not in self._parents

    def is_ancestor(self, ancestor: str, commit: str) -> bool:
        """`git merge-base --is-ancestor <ancestor> <commit>` for two names this graph read: from memory when `ancestor`
        is beyond the base (it is then an ancestor exactly when `commit` reaches it beyond the base); asked of git when
        it is on the base, the one case the graph cannot see (`commit` may or may not contain that much of the base)."""
        a, c = self.sha(ancestor), self.sha(commit)
        if not a or not c:
            return False
        if a in self._parents:
            return a in self.beyond(c)
        return self.repo is not None and run(self.repo, "merge-base", "--is-ancestor", a, c).returncode == 0

    def beyond(self, commit: str) -> frozenset[str]:
        """Every commit `commit` reaches that the base does not (`git rev-list <commit> ^<base>`); empty when it is on
        the base."""
        start = self._full(commit)
        if start in self._beyond:
            return self._beyond[start]
        seen: set[str] = set()
        stack = [start] if start in self._parents else []
        while stack:
            c = stack.pop()
            if c in seen:
                continue
            seen.add(c)
            stack.extend(p for p in self._parents[c] if p in self._parents and p not in seen)
        self._beyond[start] = frozenset(seen)
        return self._beyond[start]

    def parents(self, commit: str) -> tuple[str, ...]:
        """The parents of a commit beyond the base."""
        return self._parents[self._full(commit)]

    def tree(self, commit: str) -> str:
        """The tree of a commit beyond the base."""
        return self._trees[self._full(commit)]

    def _full(self, commit: str) -> str:
        if commit in self._parents:
            return commit
        return self._names.get(commit) or commit


def _diff_tree(repo: pathlib.Path | str, pairs: Sequence[tuple[str, str]], args: Sequence[str],
               pathspec: Sequence[str], git: Runner | None) -> list[list[str]]:
    """The output lines of ONE `git diff-tree --stdin --always -r --no-renames <args>` per (commit, parent) pair, in
    input order: `--always` gives every pair its header line (the commit) even when its diff is empty. A pair whose
    parent is EMPTY_TREE (a root commit) is sent as the commit alone, which `--root` diffs against the empty tree
    (`<commit> <parent>` takes only a commit as the parent)."""
    if not pairs:
        return []
    lines = _checked(repo, "diff-tree", "--stdin", "--always", "--root", "-r", "--no-renames", *args,
                     *(["--", *pathspec] if pathspec else []),
                     stdin="".join(f"{c}\n" if p == EMPTY_TREE else f"{c} {p}\n" for c, p in pairs),
                     git=git).splitlines()
    blocks: list[list[str]] = []
    want = iter(c for c, _ in pairs)
    nxt = next(want)
    for line in lines:
        if nxt is not None and line == nxt:
            blocks.append([])
            nxt = next(want, None)
        elif blocks:
            blocks[-1].append(line)
        else:
            raise GitError(f"git diff-tree --stdin answered {line!r} before its first header")
    if len(blocks) != len(pairs):
        raise GitError(f"git diff-tree --stdin answered {len(blocks)} blocks for {len(pairs)} pairs")
    return blocks


def diff_names(repo: pathlib.Path | str, pairs: Sequence[tuple[str, str]], *pathspec: str,
               git: Runner | None = None) -> list[set[str]]:
    """[the files each (commit, parent) pair changes], renames off (a moved file counts at both paths). Both sides of a
    pair are object ids (`git diff-tree --stdin` reads hex, never a ref name)."""
    return [{s for s in b if s.strip()} for b in _diff_tree(repo, pairs, ["--name-only"], pathspec, git)]


def diff_patches(repo: pathlib.Path | str, pairs: Sequence[tuple[str, str]], *pathspec: str,
                 git: Runner | None = None) -> list[str]:
    """[the -U0 patch of each (commit, parent) pair], renames off; object ids on both sides, as diff_names."""
    return ["\n".join(b) for b in _diff_tree(repo, pairs, ["-p", "-U0"], pathspec, git)]


# ── self-test: each reader against the one-process-per-object form it replaces ──────────────────────────────────────
def self_test() -> int:
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one fixture)
    fails: list[str] = []

    def check(name: str, ok: bool, detail: object = "") -> None:
        print(f"  {'ok  ' if ok else 'FAIL'} {name}" + ("" if ok else f": {detail}"))
        if not ok:
            fails.append(name)

    tmp = scratch_dir("git-batch-selftest-")
    try:
        isolate()
        repo = ScratchRepo.init(tmp / "repo")
        r = repo.path
        repo.commit("root", {"a.txt": "one\n", "kb/Work/PB1.md": "status: open\n"})
        root = repo.git("rev-parse", "HEAD")
        repo.git("update-ref", BASE, "HEAD")
        repo.git("switch", "-q", "-c", "side")
        repo.commit("side", {"b.txt": "two\n"})
        repo.git("switch", "-q", "main")
        repo.commit("main", {"a.txt": "uno\n", "kb/Work/PB1.md": "status: landed\n"})
        repo.git("merge", "-q", "--no-ff", "side", "-m", "merge side")
        merge = repo.git("rev-parse", "HEAD")
        repo.git("switch", "-q", "-c", "empty")
        repo.git("commit", "-q", "--allow-empty", "-m", "nothing")
        empty = repo.git("rev-parse", "HEAD")

        got = resolve(r, ["main", "side", root[:10], "nope", "HEAD:a.txt"])
        want = {n: (repo.run("rev-parse", "--verify", "-q", f"{n}^{{commit}}").stdout.strip() or None)
                for n in ("main", "side", root[:10], "nope", "HEAD:a.txt")}
        check("resolve: each name as `rev-parse --verify -q <name>^{commit}` answers it (None for a non-commit)",
              got == want and got["nope"] is None and got["HEAD:a.txt"] is None, (got, want))

        graph = Ancestry.read(r, ["main", "side", "empty"])
        for tip in ("main", "side", "empty", root):
            listed = set(repo.git("rev-list", tip, f"^{BASE}").split())
            check(f"Ancestry.beyond({tip[:10]}) is `rev-list <tip> ^base`", graph.beyond(graph.sha(tip) or tip) == listed,
                  (sorted(graph.beyond(graph.sha(tip) or tip)), sorted(listed)))
            check(f"Ancestry.on_base({tip[:10]}) is `merge-base --is-ancestor <tip> base`",
                  graph.on_base(graph.sha(tip) or tip)
                  == (repo.run("merge-base", "--is-ancestor", tip, BASE).returncode == 0))
        check("Ancestry: parents and tree as `rev-list --parents` / `<c>^{tree}` read them",
              graph.parents(merge) == tuple(repo.git("rev-list", "--parents", "-n", "1", merge).split()[1:])
              and graph.tree(merge) == repo.git("rev-parse", f"{merge}^{{tree}}"))
        asked = [(a, b) for a in ("side", "empty", root, "main") for b in ("main", "side", "empty")]
        got_anc = [graph.is_ancestor(a, b) for a, b in asked]
        want_anc = [repo.run("merge-base", "--is-ancestor", a, b).returncode == 0 for a, b in asked]
        check("Ancestry.is_ancestor is `merge-base --is-ancestor` (from memory beyond the base, from git on it)",
              got_anc == want_anc, list(zip(asked, got_anc, want_anc)))
        try:
            Ancestry.read(r, ["main", "no-such-branch"])
            check("Ancestry: a tip that names no commit raises (never 'on the base')", False)
        except GitError:
            check("Ancestry: a tip that names no commit raises (never 'on the base')", True)
        nobase = Ancestry.read(r, ["side"], base="refs/remotes/origin/nosuch")
        check("Ancestry: a missing base excludes nothing (every commit the tip reaches)",
              nobase.beyond(nobase.sha("side")) == set(repo.git("rev-list", "side").split()))
        fake = Ancestry.of({"c2": ("c1",), "c1": ()})
        check("Ancestry.of: a known parent map, no git", fake.beyond("c2") == {"c1", "c2"} and fake.on_base("c0"))

        pairs = [(merge, p) for p in graph.parents(merge)] + [(empty, merge), (root, EMPTY_TREE)]
        names = diff_names(r, pairs)
        each = [set(repo.git("diff", "--name-only", "--no-renames", p, c).split()) for c, p in pairs]
        check("diff_names: one block per pair, empty diffs included, as `git diff --name-only` per pair",
              names == each, (names, each))
        patches = diff_patches(r, pairs, "kb/Work/")
        each_p = [repo.git("diff", "-U0", "--no-renames", p, c, "--", "kb/Work/") for c, p in pairs]
        check("diff_patches: each pair's -U0 patch of the pathspec, as `git diff -U0` per pair",
              [("+status: landed" in x, "-status: open" in x) for x in patches]
              == [("+status: landed" in x, "-status: open" in x) for x in each_p], (patches, each_p))

        ids = object_ids(r, [f"{merge}:a.txt", f"{merge}:b.txt", f"{root}:b.txt"])
        check("object_ids: each `<rev>:<path>` as `rev-parse -q --verify` answers it ('' when absent)",
              ids == [repo.run("rev-parse", "-q", "--verify", s).stdout.strip()
                      for s in (f"{merge}:a.txt", f"{merge}:b.txt", f"{root}:b.txt")], ids)

        specs = [f"{root}:kb/Work", f"{merge}:kb/Work", f"{merge}:kb", f"{merge}:no/such/dir"]
        listed = tree_entries(r, specs)
        each_tree = [{n: repo.git("rev-parse", f"{s}/{n}") for n in repo.git("ls-tree", "--name-only", s).split()}
                     if repo.run("rev-parse", "-q", "--verify", s).returncode == 0 else None for s in specs]
        each_tree[2] = {}   # kb holds only the directory Work: files only
        check("tree_entries: each `<rev>:<dir>` as `ls-tree` lists its files (subdirectories left out; None for no such "
              "directory)", listed == each_tree and listed[3] is None, (listed, each_tree))
        try:
            tree_entries(r, [f"{merge}:a.txt"])
            check("tree_entries: a spec that names a file raises", False)
        except GitError:
            check("tree_entries: a spec that names a file raises", True)
        wanted = [oid for entries in listed if entries for oid in entries.values()]
        got_blobs = list(blobs(r, [*wanted, *wanted, ""]))
        check("blobs: each distinct object id as `cat-file blob` reads it, once however often it is asked",
              dict(got_blobs) == {o: repo.run("cat-file", "blob", o, binary=True).stdout for o in set(wanted)}
              and len(got_blobs) == len(set(wanted)), got_blobs)
        try:
            list(blobs(r, ["0" * 40]))
            check("blobs: an id that names no blob raises", False)
        except GitError:
            check("blobs: an id that names no blob raises", True)
        check("tree_entries and blobs: no questions, no process", tree_entries(r, []) == [] and list(blobs(r, [])) == [])

        repo.git("switch", "-q", "-c", "orig", root)
        repo.commit("orig", {"c.txt": "three\n"})
        orig = repo.git("rev-parse", "HEAD")
        repo.git("switch", "-q", "-c", "replayed", "main")
        repo.git("cherry-pick", orig)
        replayed = repo.git("rev-parse", "HEAD")
        pids = patch_ids(r, [orig, replayed, merge, empty, root, orig, ""])
        each_pid = {c: run(r, "patch-id", "--stable", stdin=repo.run("show", c).stdout).stdout.split()[0]
                    for c in (orig, replayed, root)}
        check("patch_ids: each commit as `git show | git patch-id --stable` gives it, a root commit included, merges absent",
              pids == each_pid and merge not in pids and empty not in pids, (pids, each_pid))
        check("patch_ids: a cherry-pick of a commit shares its id (the key `git cherry` matches on)",
              pids[orig] == pids[replayed] and orig != replayed)
        check("patch_ids: no commits, no process", patch_ids(r, []) == {} and patch_ids(r, [""]) == {})
    finally:
        remove_tree(tmp)
    print(f"=== git_batch self-test: {'GREEN' if not fails else 'RED'} ({len(fails)} failure(s)) ===")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    if sys.argv[1:] == ["--self-test"]:
        sys.exit(self_test())
    print(__doc__)
    sys.exit(2)
