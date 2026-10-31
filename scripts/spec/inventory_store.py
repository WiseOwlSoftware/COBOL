#!/usr/bin/env python3
"""The traceability inventory's STORE: one JSON file per row, and the only code that reads or writes them (kb/Work PB4148).

    python scripts/spec/inventory_store.py --self-test

WHY ONE FILE PER ROW. The inventory was one 7.6 MB JSON list of 4,214 rows. A fix that closes a row edits that file, and
two branches that closed different rows could merge only through a merge driver the merge queue cannot run (kb/Work
PB3642), so `enqueue.py` admitted one such pull request at a time: the second-largest cause of landing holds after the
refusal registry (PB4147). The row's adjudicated facts now live where two different rows never share a file:

    tests/version-matrix/inventory/<rule-id>.json      one row, an object, `json.dumps(indent=1)` plus a newline

Git's plain merge keeps both branches' rows; a conflict means two branches changed the SAME row, which is a real one.
No merge driver governs the directory (`MergeDriverDriftTests` holds that), and `.gitattributes` does not name it.

THE ROW'S NAME IS ITS RULE ID, so a reader finds a row without a scan and a rename of a rule is a rename of a file. The
catalog's ids are `[A-Z]+-<section>[-<n>]`; `row_name` is the one place that turns an id into a file name and refuses an id
that would not be a safe, case-insensitively unique one.

THE ORDER of `load_inventory()` is the spec's: section by section (numeric parts numerically, `A.1` after the numbered
clauses), then the catalog ordinal, then the id. No reader may depend on the catalog's page order (it is not kept), and
none did: every consumer keys a row by `rule-id`.

HISTORY. Every commit before PB4148 stored the inventory as the single file `LEGACY_FILE_REL`. `inventory_at(rev)` is the
one reader of the inventory at a REVISION and reads either layout, because the audits that walk history
(`audit_witness_loss`, `backfill_closes_rows`, `gen_ledger`'s trend) cross the commit that changed it. It reads history;
nothing writes the old shape.
"""
from __future__ import annotations

import json
import pathlib
import re
import subprocess
import sys
from collections.abc import Iterable
from typing import Any

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
INVENTORY_DIR_REL = "tests/version-matrix/inventory"
INVENTORY_DIR = REPO / INVENTORY_DIR_REL
#: The single file every commit before PB4148 stored the inventory in. History only: `inventory_at` reads it, nothing writes it.
LEGACY_FILE_REL = "tests/version-matrix/traceability-inventory.json"
#: The pathspecs under which a commit "touched the inventory", for `git log`: the directory and, before PB4148, the file.
HISTORY_PATHSPECS = (INVENTORY_DIR_REL, LEGACY_FILE_REL)

#: A rule id is a file name: letters, digits, `.` and `-`, beginning with a letter, no `..`, so no id is a path.
_SAFE_ID = re.compile(r"[A-Za-z][A-Za-z0-9]*(?:[.-][A-Za-z0-9]+)*")


def row_name(rule_id: str) -> str:
    """The file name of the row `rule_id`: `<rule-id>.json`. An id that is not a safe file name is a SystemExit."""
    if not _SAFE_ID.fullmatch(rule_id):
        raise SystemExit(f"inventory: rule-id {rule_id!r} is not a safe file name (letters, digits, `.` and `-`)")
    return f"{rule_id}.json"


def row_path(rule_id: str, directory: pathlib.Path = INVENTORY_DIR) -> pathlib.Path:
    return directory / row_name(rule_id)


def _section_key(section: str) -> list[tuple[int, int, str]]:
    return [(0, int(p), "") if p.isdigit() else (1, 0, p) for p in section.split(".")]


def spec_order(rows: Iterable[dict[str, Any]]) -> list[dict[str, Any]]:
    """`rows` in the order `load_inventory` documents: section, catalog ordinal, rule id."""
    return sorted(rows, key=lambda r: (_section_key(r["section"]), r["ordinal"], r["rule-id"]))


def render_row(row: dict[str, Any]) -> str:
    """The text of a row's file: the one spelling `write_inventory` writes and `load_inventory` expects."""
    return json.dumps(row, indent=1, ensure_ascii=False) + "\n"


def _problems_of(files: Iterable[pathlib.Path], parse) -> tuple[list[dict[str, Any]], list[str]]:
    """Rows from the `<rule-id>.json` files given (each `parse`d from its path), and the problems of the directory: a
    file that is not `<rule-id>.json` for the row it holds, two ids differing only in case, a subdirectory, a stray file."""
    rows: list[dict[str, Any]] = []
    problems: list[str] = []
    seen: dict[str, str] = {}
    for f in sorted(files, key=lambda f: f.name):
        if f.suffix != ".json" or f.name.startswith("."):
            problems.append(f"{f.name}: the inventory directory holds one `<rule-id>.json` per row and nothing else")
            continue
        try:
            row = parse(f)
        except (ValueError, OSError) as e:
            problems.append(f"{f.name}: is not readable JSON: {e}")
            continue
        if not isinstance(row, dict) or not isinstance(row.get("rule-id"), str):
            problems.append(f"{f.name}: is not a row (an object with a `rule-id`)")
        elif f.name != f"{row['rule-id']}.json":
            problems.append(f"{f.name}: holds the row {row['rule-id']!r}, so the file is named {row['rule-id']}.json")
        elif (other := seen.setdefault(f.name.lower(), f.name)) != f.name:
            problems.append(f"{f.name}: differs from {other} only in case, so the two collide on a case-insensitive disk")
        else:
            rows.append(row)
    return rows, problems


def load_inventory(directory: pathlib.Path = INVENTORY_DIR) -> list[dict[str, Any]]:
    """Every row of the inventory, in spec order. A directory with a stray, misnamed or unreadable file is a SystemExit
    naming each one: a row silently skipped would be a row silently GAP-free."""
    if not directory.is_dir():
        raise SystemExit(f"inventory not found: {directory}\nRun: python scripts/spec/build_inventory.py")
    rows, problems = _problems_of(directory.iterdir(), lambda f: json.loads(f.read_text(encoding="utf-8")))
    if problems:
        raise SystemExit("inventory: " + str(len(problems)) + " problem(s) in " + str(directory) + ":\n  "
                         + "\n  ".join(problems))
    return spec_order(rows)


def write_inventory(rows: list[dict[str, Any]], directory: pathlib.Path = INVENTORY_DIR) -> int:
    """Make `directory` hold exactly `rows`, writing only the files whose text changed and deleting the file of a row no
    longer in `rows` (a rule the spec dropped). Returns the number of files written or deleted.

    Each file is written ATOMICALLY (a temp file in the directory, then an os-level replace), so a torn write never leaves
    a half row; an interrupted batch leaves earlier rows written and later ones as they were, and running the same batch
    again completes it (a `.tmp` left behind is a stray file, which `load_inventory` refuses by name)."""
    directory.mkdir(parents=True, exist_ok=True)
    wanted: dict[str, str] = {}
    for row in rows:
        name = row_name(row["rule-id"])
        if name in wanted:
            raise SystemExit(f"inventory: rule-id {row['rule-id']!r} appears twice")
        wanted[name] = render_row(row)
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


def _git_bytes(repo: pathlib.Path, *args: str, stdin: bytes | None = None) -> bytes | None:
    r = subprocess.run(["git", *args], cwd=repo, input=stdin, capture_output=True)
    return r.stdout if r.returncode == 0 else None


def inventory_at(rev: str, repo: pathlib.Path = REPO) -> list[dict[str, Any]] | None:
    """The inventory as committed at `rev` of `repo` (spec order), or None when `rev` has none. Reads the per-row
    directory and, for a commit before PB4148, the single legacy file."""
    listing = _git_bytes(repo, "ls-tree", "-r", rev, "--", INVENTORY_DIR_REL)
    if listing:
        entries = [ln.split(b"\t", 1) for ln in listing.splitlines()]
        blobs = _git_bytes(repo, "cat-file", "--batch", stdin=b"".join(meta.split()[2] + b"\n" for meta, _ in entries))
        rows: list[dict[str, Any]] = []
        pos = 0
        for _ in entries:
            end = blobs.index(b"\n", pos)
            size = int(blobs[pos:end].split()[2])
            rows.append(json.loads(blobs[end + 1:end + 1 + size].decode("utf-8")))
            pos = end + 1 + size + 1
        return spec_order(rows)
    legacy = _git_bytes(repo, "show", f"{rev}:{LEGACY_FILE_REL}")
    return spec_order(json.loads(legacy.decode("utf-8"))) if legacy else None


def self_test() -> int:
    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
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

    def row(rid: str, section: str, ordinal: int, **more: Any) -> dict[str, Any]:
        return {"rule-id": rid, "section": section, "ordinal": ordinal, "verdict": "", **more}

    check(row_name("GR-4.2.10-1") == "GR-4.2.10-1.json" and row_name("FMT-14.9.32.2") == "FMT-14.9.32.2.json"
          and row_name("DOC-A.1-92") == "DOC-A.1-92.json", "ordinary ids are file names")
    for bad in ("", "../x", "a/b", "GR-4..2", "GR-4.2-", "-GR", "GR 4", "GR\\4"):
        check(bool(refuses(lambda b=bad: row_name(b))), f"{bad!r} is no file name")

    ordered = spec_order([row("GR-10.1-1", "10.1", 1), row("DOC-A.1-1", "A.1", 1), row("GR-4.4-2", "4.4", 2),
                          row("GR-4.4-1", "4.4", 1), row("GR-4.10-1", "4.10", 1), row("GR-4.2.10-1", "4.2.10", 1)])
    check([r["rule-id"] for r in ordered] == ["GR-4.2.10-1", "GR-4.4-1", "GR-4.4-2", "GR-4.10-1", "GR-10.1-1", "DOC-A.1-1"],
          "spec order is numeric by section part, then ordinal, with the Annex after the clauses")

    t = scratch_dir("inventory-store-")
    d = t / "inv"
    rows = [row("GR-4.4-1", "4.4", 1, notes="é“"), row("GR-4.4-2", "4.4", 2), row("SR-5.5-1", "5.5", 1)]
    check(write_inventory(rows, d) == 3 and load_inventory(d) == rows, "a written inventory loads back, in order")
    check((d / "GR-4.4-1.json").read_bytes() == render_row(rows[0]).encode("utf-8")
          and b"\r" not in (d / "GR-4.4-1.json").read_bytes(), "a row's file is its render, with LF only")
    check(write_inventory(rows, d) == 0, "an unchanged inventory rewrites nothing")
    rows[1]["verdict"] = "CONFORMS"
    check(write_inventory(rows, d) == 1 and load_inventory(d)[1]["verdict"] == "CONFORMS",
          "a changed row rewrites its own file only")
    check(write_inventory(rows[:2], d) == 1 and not (d / "SR-5.5-1.json").exists(), "a row dropped from the spec loses its file")
    check("twice" in refuses(lambda: write_inventory([rows[0], rows[0]], d)), "a row twice is refused")
    for name, content, why in (("stray.txt", "x", "stray file"), ("GR-4.4-9.json", "{}", "not a row"),
                               ("GR-4.4-8.json", json.dumps(row("GR-4.4-7", "4.4", 7)), "misnamed row"),
                               ("GR-4.4-6.json", "{", "unparseable"), ("GR-4.4-2.json.tmp", "x", "torn write")):
        (d / name).write_text(content, encoding="utf-8")
        msg = refuses(lambda: load_inventory(d))
        check(name in msg, f"a {why} ({name}) is named in the refusal")
        (d / name).unlink()
    (d / "sub").mkdir()
    check("sub" in refuses(lambda: load_inventory(d)), "a subdirectory is refused")
    (d / "sub").rmdir()
    collide = _problems_of([d / "GR-4.4-1.json", d / "gr-4.4-1.json"],
                           lambda f: row(f.stem, "4.4", 1))[1]
    check(any("only in case" in p for p in collide), "two ids that differ only in case are refused")
    check("not found" in refuses(lambda: load_inventory(t / "absent")), "an absent directory is refused")

    # The acceptance of kb/Work PB4148, against git itself: the merge queue runs no merge driver, so two branches that close
    # different rows must merge with git's plain merge, and only the SAME row changed twice is a conflict.
    repo = ScratchRepo.init(t / "repo")
    rows = [row(f"GR-4.4-{i}", "4.4", i, notes="n") for i in (1, 2, 3)]
    write_inventory(rows, repo.path / INVENTORY_DIR_REL)
    repo.commit("base")

    def close(rule_id: str, verdict: str) -> None:
        moved = [dict(r, verdict=verdict) if r["rule-id"] == rule_id else r for r in rows]
        write_inventory(moved, repo.path / INVENTORY_DIR_REL)
        repo.commit(f"{rule_id} {verdict}")

    repo.git("checkout", "-q", "-b", "side")
    close("GR-4.4-1", "CONFORMS")
    repo.git("checkout", "-q", "main")
    close("GR-4.4-2", "CONFORMS")
    plain = repo.run("merge", "--no-edit", "side")
    merged = inventory_at("HEAD", repo.path)
    check(plain.returncode == 0 and merged is not None and [r["verdict"] for r in merged] == ["CONFORMS", "CONFORMS", ""],
          "two branches that closed different rows merge with git's plain merge, no driver" + f" ({plain.stdout.strip()[:60]})")
    check(inventory_at("HEAD~1", repo.path) is not None and inventory_at("no-such-rev", repo.path) is None,
          "inventory_at reads a revision's rows, and none for a revision that does not exist")
    repo.git("checkout", "-q", "-b", "other", "HEAD~1")
    close("GR-4.4-3", "DIVERGES")
    repo.git("checkout", "-q", "main")
    close("GR-4.4-3", "CONFORMS")
    clash = repo.run("merge", "--no-edit", "other")
    check(clash.returncode != 0 and "CONFLICT" in clash.stdout, "the SAME row closed two ways is a conflict, as it must be")
    repo.run("merge", "--abort")

    # A commit before PB4148 holds the one legacy file; inventory_at reads it.
    old = ScratchRepo.init(t / "old")
    old.commit("legacy", {LEGACY_FILE_REL: json.dumps([row("GR-4.4-2", "4.4", 2), row("GR-4.4-1", "4.4", 1)])})
    check([r["rule-id"] for r in inventory_at("HEAD", old.path) or []] == ["GR-4.4-1", "GR-4.4-2"],
          "inventory_at reads the single legacy file of a commit before PB4148, in spec order")
    for f in failures:
        print(f"FAIL: {f}")
    print(f"inventory_store self-test: {'FAIL' if failures else 'OK'}")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if "--self-test" not in sys.argv:
        sys.exit(__doc__)
    sys.exit(self_test())
