#!/usr/bin/env python3
"""The traceability inventory's STORE: one JSON file per ADJUDICATED rule, per edition, and the only code that reads or
writes them (kb/Work PB4148, PB4992).

    python scripts/spec/inventory_store.py --self-test

WHAT A FILE HOLDS: the adjudication, and nothing a catalog or a rule decides (DESIGN-spec-conformance-review.md section
4's table, owner 2026-11-08: "We must eliminate redundant code and data"). A store file is

    {"rule-id": ..., "rule-digest": ..., <the edition's adjudicated fields that say something>}

and a rule nobody has adjudicated has no file. The rule's section, kind, ordinal and subject live in the edition's
catalog; its state (OK or GAP) is computed. Joining the two is the VIEW's job (`inventory_view.py`), so a reader reads
the view and never this module: the store and the view are two modules because they are two jobs.

WHY ONE FILE PER RULE. The inventory was one 7.6 MB JSON list of 4,214 rows. A fix that closes a row edits that file, and
two branches that closed different rows could merge only through a merge driver the merge queue cannot run (kb/Work
PB3642), so `enqueue.py` admitted one such pull request at a time: the second-largest cause of landing holds after the
refusal registry (PB4147). The adjudications now live where two different rules never share a file:

    tests/version-matrix/inventory/<rule-id>.json            2023
    tests/version-matrix/inventory-<edition>/<rule-id>.json  an older edition (kb/Work PB4991)

(each edition's directory is its `store` in the schema's `editions.of`, `inventory_schema.Edition.store`)

each an object, `json.dumps(indent=1)` plus a newline. Git's plain merge keeps both branches' files; a conflict means two
branches changed the SAME rule's adjudication, which is a real one. No merge driver governs a store (there is none anywhere, kb/Work PB5110; `RegistryShapeDriftTests`
holds that), and `.gitattributes` does not name it. An edition's store starts with no directory at all (git
holds no empty one), which reads as a store with no adjudication.

THE FILE'S NAME IS ITS RULE ID, so a reader finds a record without a scan and a rename of a rule is a rename of a file.
`row_name` is the one place that turns an id into a file name and refuses an id that would not be a safe, case-
insensitively unique one.

HISTORY. Every commit before PB4148 stored the 2023 inventory as a single file (the edition's `legacy_file`), and every
commit before PB4992 stored each rule's catalog fields and state in its file. `store_at(tree, edition)` reads either layout
as committed; the view (`inventory_view.inventory_at`) decides what a commit's rows were.
"""
from __future__ import annotations

import json
import os
import pathlib
import re
import sys
from collections.abc import Iterable, Sequence
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from inventory_schema import REPO, STORE_KEYS, Edition, Tree  # noqa: E402  (the keys a store file carries; each edition)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402
import git_batch  # noqa: E402  (scripts/: one git process for many objects)

#: A rule id is a file name: letters, digits, `.` and `-`, no `..` and no leading separator, so no id is a path. A 1985
#: id begins with its year (`1985-SR-VI.5.7.3-1`, `1989-AR-A.2.5.3-1`).
_SAFE_ID = re.compile(r"[A-Za-z0-9]+(?:[.-][A-Za-z0-9]+)*")


def inventory_dir(edition: Edition, root: pathlib.Path = REPO) -> pathlib.Path:
    """The store of the held `edition` in the working tree at `root`."""
    return root / edition.store


def history_pathspecs(edition: Edition) -> tuple[str, ...]:
    """The pathspecs under which a commit "touched `edition`'s store", for `git log`: its directory and the single file
    that held it before PB4148, if it had one."""
    return (edition.store, *((edition.legacy_file,) if edition.legacy_file else ()))


def row_name(rule_id: str) -> str:
    """The file name of the rule `rule_id`: `<rule-id>.json`. An id that is not a safe file name is a SystemExit."""
    if not _SAFE_ID.fullmatch(rule_id):
        raise SystemExit(f"inventory: rule-id {rule_id!r} is not a safe file name (letters, digits, `.` and `-`)")
    return f"{rule_id}.json"


def render_record(record: dict[str, Any]) -> str:
    """The text of a store file: the one spelling `write_store` writes and `read_store` expects."""
    return json.dumps(record, indent=1, ensure_ascii=False) + "\n"


def record_of(row: dict[str, Any], adjudicated: Sequence[str]) -> dict[str, Any] | None:
    """The store record of `row`: its `STORE_KEYS` and each of `adjudicated` that says something, in that order; None when
    none does (a rule with no adjudication has no file). Whatever else `row` carries (a view's catalog fields and state,
    or a commit's copies of them) is not the store's."""
    said = {f: row[f] for f in adjudicated if row.get(f)}
    if not said:
        return None
    return {**{k: row.get(k, "") for k in STORE_KEYS}, **said}


def _problems_of(files: Iterable[pathlib.Path], parse) -> tuple[list[dict[str, Any]], list[str]]:
    """Records from the `<rule-id>.json` files given (each `parse`d from its path), and the problems of the directory: a
    file that is not `<rule-id>.json` for the record it holds, two ids differing only in case, a subdirectory, a stray
    file."""
    records: list[dict[str, Any]] = []
    problems: list[str] = []
    seen: dict[str, str] = {}
    for f in sorted(files, key=lambda f: f.name):
        if f.suffix != ".json" or f.name.startswith("."):
            problems.append(f"{f.name}: a store holds one `<rule-id>.json` per adjudicated rule and nothing else")
            continue
        try:
            record = parse(f)
        except (ValueError, OSError) as e:
            problems.append(f"{f.name}: is not readable JSON: {e}")
            continue
        if not isinstance(record, dict) or not isinstance(record.get("rule-id"), str):
            problems.append(f"{f.name}: is not a record (an object with a `rule-id`)")
        elif f.name != f"{record['rule-id']}.json":
            problems.append(f"{f.name}: holds the record of {record['rule-id']!r}, so the file is named "
                            f"{record['rule-id']}.json")
        elif (other := seen.setdefault(f.name.lower(), f.name)) != f.name:
            problems.append(f"{f.name}: differs from {other} only in case, so the two collide on a case-insensitive disk")
        else:
            records.append(record)
    return records, problems


def read_store(directory: pathlib.Path) -> dict[str, dict[str, Any]]:
    """Every record of the store at `directory`, by rule id; an absent directory is a store with no record. A stray,
    misnamed or unreadable file is a SystemExit naming each one: a record silently skipped would be a rule silently
    unadjudicated."""
    if not directory.is_dir():
        return {}
    records, problems = _problems_of(directory.iterdir(), lambda f: json.loads(f.read_text(encoding="utf-8")))
    if problems:
        raise SystemExit("inventory: " + str(len(problems)) + " problem(s) in " + str(directory) + ":\n  "
                         + "\n  ".join(problems))
    return {r["rule-id"]: r for r in records}


def write_store(records: Iterable[dict[str, Any]], directory: pathlib.Path) -> int:
    """Make the store at `directory` hold exactly `records`, writing only the files whose text changed and deleting the
    file of a rule no longer among them. Returns the number of files written or deleted.

    Each file is written ATOMICALLY (a temp file in the directory, then an os-level replace), so a torn write never leaves
    a half record; an interrupted batch leaves earlier records written and later ones as they were, and running the same
    batch again completes it (a `.tmp` left behind is a stray file, which `read_store` refuses by name)."""
    wanted: dict[str, str] = {}
    for record in records:
        name = row_name(record["rule-id"])
        if name in wanted:
            raise SystemExit(f"inventory: rule-id {record['rule-id']!r} appears twice")
        wanted[name] = render_record(record)
    if not wanted and not directory.is_dir():
        return 0
    directory.mkdir(parents=True, exist_ok=True)
    changed = 0
    for f in directory.iterdir():
        if f.is_file() and f.name not in wanted and f.suffix == ".json":
            f.unlink()
            changed += 1
    for name, text in wanted.items():
        f = directory / name
        if f.is_file() and f.read_bytes() == text.encode("utf-8"):
            continue
        tmp = f.with_name(name + ".tmp")
        tmp.write_text(text, encoding="utf-8", newline="\n")
        tmp.replace(f)
        changed += 1
    return changed


def landed_head(repo: pathlib.Path = REPO) -> str:
    """The newest commit main had that HEAD holds: HEAD's merge base with origin/main (or main), HEAD when neither exists.
    A history reader walks `--first-parent` from here, so every commit it reads is a landing, a state main had, and never
    a merged branch's WIP checkpoint (branch review R2-1 of PB4992)."""
    for ref in ("origin/main", "main"):
        if (base := Tree(repo).git("merge-base", "HEAD", ref)) is not None:
            return base.decode("utf-8").strip()
    return "HEAD"


def store_at(tree: Tree, edition: Edition) -> list[dict[str, Any]] | None:
    """`edition`'s store as committed at `tree.rev`, in file order, or None when the commit has none. Reads the per-rule
    directory and, for a commit before PB4148, the edition's single legacy file. The records are as committed: a commit
    before PB4992 stored each rule's catalog fields and state beside its adjudication."""
    listing = tree.git("ls-tree", "-r", tree.rev, "--", edition.store)
    if listing:
        # Through git_batch.blobs, so a partial clone (CI's `filter: blob:none`) fetches the commit's records in ONE
        # request, not one round trip per record (the merge-queue bounce of kb/Work PB4992).
        oids = [ln.split(b"\t", 1)[0].split()[2].decode("ascii") for ln in listing.splitlines()]
        data = dict(git_batch.blobs(tree.root, oids))
        return [json.loads(data[oid].decode("utf-8")) for oid in oids]
    return tree.json(edition.legacy_file) if edition.legacy_file else None


def self_test() -> int:
    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    from inventory_schema import edition  # noqa: PLC0415
    new, old_ed = edition(2023), edition(1985)
    failures: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            failures.append(what)

    def refuses(thunk) -> str:
        try:
            thunk()
        except SystemExit as e:
            return str(e.code)
        return ""

    adjudicated = ("verdict", "notes")

    def rec(rid: str, **more: Any) -> dict[str, Any]:
        return {"rule-id": rid, "rule-digest": "d", **more}

    check(row_name("GR-4.2.10-1") == "GR-4.2.10-1.json" and row_name("FMT-14.9.32.2") == "FMT-14.9.32.2.json"
          and row_name("DOC-A.1-92") == "DOC-A.1-92.json", "ordinary ids are file names")
    check(row_name("1985-SR-VI.5.7.3-1") == "1985-SR-VI.5.7.3-1.json"
          and row_name("1989-AR-A.2.5.3-1") == "1989-AR-A.2.5.3-1.json",
          "a 1985 id, which begins with its year, is a file name (kb/Work PB4992)")
    check(inventory_dir(old_ed).name == "inventory-1985" and inventory_dir(new) == REPO / new.store
          and "not held" in refuses(lambda: edition(2002)),
          "each edition names its own store, and an edition with none is refused")
    check(history_pathspecs(new) == (new.store, new.legacy_file) and new.legacy_file
          and history_pathspecs(old_ed) == (old_ed.store,), "only the edition the schema says had one has a legacy file")
    for bad in ("", "../x", "a/b", "GR-4..2", "GR-4.2-", "-GR", "GR 4", "GR\\4"):
        check(bool(refuses(lambda b=bad: row_name(b))), f"{bad!r} is no file name")

    view_row = {"rule-id": "GR-4.4-1", "section": "4.4", "kind": "GR", "ordinal": 1, "state": "OK", "rule-digest": "d",
                "verdict": "CONFORMS", "notes": ""}
    check(record_of(view_row, adjudicated) == {"rule-id": "GR-4.4-1", "rule-digest": "d", "verdict": "CONFORMS"},
          "a record holds the rule, its digest and the adjudicated fields that say something; no catalog field, no state")
    check(record_of(dict(view_row, verdict=""), adjudicated) is None, "a rule with no adjudication has no record")

    t = scratch_dir("inventory-store-")
    d = t / "inv"
    check(read_store(d) == {} and write_store([], d) == 0 and not d.exists(),
          "an absent store is a store with no record, and writing none creates nothing")
    records = [rec("GR-4.4-1", notes="é“"), rec("GR-4.4-2", verdict="PARTIAL"), rec("SR-5.5-1", verdict="CONFORMS")]
    check(write_store(records, d) == 3 and read_store(d) == {r["rule-id"]: r for r in records},
          "a written store reads back, by rule id")
    check((d / "GR-4.4-1.json").read_bytes() == render_record(records[0]).encode("utf-8")
          and b"\r" not in (d / "GR-4.4-1.json").read_bytes(), "a record's file is its render, with LF only")
    check(write_store(records, d) == 0, "an unchanged store rewrites nothing")
    records[1]["verdict"] = "CONFORMS"
    check(write_store(records, d) == 1 and read_store(d)["GR-4.4-2"]["verdict"] == "CONFORMS",
          "a changed record rewrites its own file only")
    check(write_store(records[:2], d) == 1 and not (d / "SR-5.5-1.json").exists(), "a record dropped loses its file")
    check("twice" in refuses(lambda: write_store([records[0], records[0]], d)), "a record twice is refused")
    for name, content, why in (("stray.txt", "x", "stray file"), ("GR-4.4-9.json", "{}", "not a record"),
                               ("GR-4.4-8.json", json.dumps(rec("GR-4.4-7")), "misnamed record"),
                               ("GR-4.4-6.json", "{", "unparseable"), ("GR-4.4-2.json.tmp", "x", "torn write")):
        (d / name).write_text(content, encoding="utf-8", newline="\n")
        msg = refuses(lambda: read_store(d))
        check(name in msg, f"a {why} ({name}) is named in the refusal")
        (d / name).unlink()
    (d / "sub").mkdir()
    check("sub" in refuses(lambda: read_store(d)), "a subdirectory is refused")
    (d / "sub").rmdir()
    collide = _problems_of([d / "GR-4.4-1.json", d / "gr-4.4-1.json"], lambda f: rec(f.stem))[1]
    check(any("only in case" in p for p in collide), "two ids that differ only in case are refused")

    # The acceptance of kb/Work PB4148, against git itself: the merge queue runs no merge driver, so two branches that close
    # different rules must merge with git's plain merge, and only the SAME rule changed twice is a conflict.
    repo = ScratchRepo.init(t / "repo")
    store = repo.path / new.store
    base = [rec(f"GR-4.4-{i}", notes="n") for i in (1, 2, 3)]
    write_store(base, store)
    repo.commit("base")

    def close(rule_id: str, verdict: str) -> None:
        write_store([dict(r, verdict=verdict) if r["rule-id"] == rule_id else r for r in base], store)
        repo.commit(f"{rule_id} {verdict}")

    repo.git("checkout", "-q", "-b", "side")
    close("GR-4.4-1", "CONFORMS")
    repo.git("checkout", "-q", "main")
    close("GR-4.4-2", "CONFORMS")
    plain = repo.run("merge", "--no-edit", "side")
    merged = sorted(store_at(Tree(repo.path, "HEAD"), new) or [], key=lambda r: r["rule-id"])
    check(plain.returncode == 0 and [r.get("verdict", "") for r in merged] == ["CONFORMS", "CONFORMS", ""],
          "two branches that closed different rules merge with git's plain merge, no driver" + f" ({plain.stdout.strip()[:60]})")
    check(store_at(Tree(repo.path, "HEAD~1"), new) is not None and store_at(Tree(repo.path, "no-such-rev"), new) is None
          and store_at(Tree(repo.path, "HEAD"), old_ed) is None,
          "store_at reads a revision's records, and none for a revision or an edition the commit lacks")
    repo.git("checkout", "-q", "-b", "other", "HEAD~1")
    close("GR-4.4-3", "DIVERGES")
    repo.git("checkout", "-q", "main")
    close("GR-4.4-3", "CONFORMS")
    clash = repo.run("merge", "--no-edit", "other")
    check(clash.returncode != 0 and "CONFLICT" in clash.stdout, "the SAME rule closed two ways is a conflict, as it must be")
    repo.run("merge", "--abort")

    # A commit before PB4148 holds the one legacy file; store_at reads it.
    old = ScratchRepo.init(t / "old")
    old.commit("legacy", {new.legacy_file: json.dumps([rec("GR-4.4-2"), rec("GR-4.4-1")])})
    check([r["rule-id"] for r in store_at(Tree(old.path, "HEAD"), new) or []] == ["GR-4.4-2", "GR-4.4-1"],
          "store_at reads the single legacy file of a commit before PB4148, as committed")

    # The second merge-queue bounce of PB4992: CI checks out with `filter: blob:none`, and a base commit's store read
    # blob by blob made one lazy fetch per record (cancelled after 8.5 minutes over 4,214). In a planted partial clone
    # whose base records are all absent, store_at fetches them in ONE request, counted from git's own trace.
    src = ScratchRepo.init(t / "promisor")
    src.git("config", "uploadpack.allowFilter", "true")
    src.git("config", "uploadpack.allowAnySHA1InWant", "true")
    others = [f"docs/f{i}.json" for i in range(5)]   # the view's other base files: Tree.read_ahead (review R6-1)
    write_store([rec(f"GR-9.9-{i}", verdict="PARTIAL") for i in range(8)], src.path / new.store)
    src.commit("base", {rel: json.dumps({"at": "base", "n": rel}) for rel in others})
    write_store([rec(f"GR-9.9-{i}", verdict="CONFORMS") for i in range(8)], src.path / new.store)
    src.commit("every record changed", {rel: json.dumps({"at": "head", "n": rel}) for rel in others})
    src.run("clone", "-q", "--filter=blob:none", "--no-checkout", (src.path).as_uri(), str(t / "partial"), cwd=t)
    partial = t / "partial"

    def fetches_of(read):
        """(what `read()` returned, how many `git fetch` processes git's own trace2 log saw start during it)."""
        trace = scratch_dir("trace2-") / "trace2.json"
        os.environ["GIT_TRACE2_EVENT"] = str(trace)
        try:
            got = read()
        finally:
            del os.environ["GIT_TRACE2_EVENT"]
        events = [json.loads(line) for line in trace.read_text(encoding="utf-8").splitlines()] if trace.exists() else []
        return got, len([e for e in events if e.get("event") == "start" and "fetch" in (e.get("argv") or [])])

    at_base, n = fetches_of(lambda: store_at(Tree(partial, "HEAD~1"), new))
    check([r["verdict"] for r in at_base or []] == ["PARTIAL"] * 8 and n == 1,
          f"a partial clone reads a base store of 8 absent records with ONE fetch, not one per record ({n} fetch(es))")
    ahead, n = fetches_of(lambda: Tree(partial, "HEAD~1").read_ahead(others))
    check([ahead.json(rel)["at"] for rel in others] == ["base"] * 5 and n == 1,
          f"Tree.read_ahead reads 5 absent base files with ONE fetch, not one per file ({n} fetch(es))")
    for f in failures:
        print(f"FAIL: {f}")
    print(f"inventory_store self-test: {'FAIL' if failures else 'OK'}")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if "--self-test" not in sys.argv:
        sys.exit(__doc__)
    sys.exit(self_test())
