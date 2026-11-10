#!/usr/bin/env python3
"""The version-matrix construct catalogue's STORE: one JSON file per construct, and the only Python that reads or writes them
(kb/Work PB5110).

    python scripts/spec/construct_store.py --self-test

THE CATALOGUE (VERSION_TEST_MATRIX_DESIGN.md section 10 #5, moved here from the `_comment` of the single file it replaced).
Each entry is a minimal program exercising ONE construct plus its edition metadata. `VersionMatrixTests` computes the
expected outcome per (construct x edition): it compiles iff `introducedIn` <= V and (`removedIn` is null or V <
`removedIn`). The human-readable VERSION_CHANGE_REFERENCE.md and the in-code `ConstructDialectStatus` registry are
renderings of THIS catalogue: extend it here, never fork. `vcr` cites the VERSION_CHANGE_REFERENCE row or section, or the
introducing standard where the document has no row (it covers the 2002 to 2023 deltas only). OPTIONAL `status`: `pending`
= catalogued (its edition metadata frozen by the drift tests) but not yet implemented; the matrix SKIPS its compile
assertions until the owning roadmap phase lands and flips it active (the one pending mechanism, shared with the corpus
discovery manifests). OPTIONAL `declined`: the code that refuses a construct of an optional element WiseOwl COBOL does not
claim (Annex A.4.1) at and after `introducedIn`; such a row compiles at NO edition, its introduction gate
(`expectDiagnostic`) refusing it below `introducedIn` (kb/Work PB5007; VERSION_TEST_MATRIX_DESIGN.md). Vendor-dialect
constructs live in `vendor-constructs.json` (owner decision 2), never here.

WHY ONE FILE PER CONSTRUCT (kb/Work PB5110, the owner's decision of 2026-11-10: "No custom merge drivers are required").
The catalogue was one 4,000-line JSON file, and two branches that each ADDED a construct beside the same neighbour made git
stop on a conflict whose answer was always "keep both". A merge driver answered that, and GitHub's merge queue, which runs
plain git, could not run it, so a branch sharing the file with an open pull request had to wait. A catalogue of entries needs
no driver when each entry is a file (the traceability inventory, `inventory_store.py`, found the shape first):

    tests/version-matrix/constructs/<construct-id>.json

each an object, `json.dumps(indent=1)` plus a newline. Git's plain merge keeps both branches' files; a conflict means two
branches changed the SAME construct, which is a real one. A construct's file name is its id (`row_name`), so a reader finds a
row without a scan and a rename of a construct is a rename of a file. The readers (`read_rows`, `TestRepo.ConstructRows`, the
generator `scripts/gen-constructs.ps1`) take the rows in file-name order (ordinal), the one order every reader agrees on.
"""
from __future__ import annotations

import json
import pathlib
import re
import sys
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
#: The store, relative to the repository root: the one spelling of its path for every Python reader.
STORE_REL = "tests/version-matrix/constructs"

#: A construct id is a file name: lowercase letters, digits and `-`, never a path.
_SAFE_ID = re.compile(r"[a-z0-9]+(?:-[a-z0-9]+)*")


def store_dir(root: pathlib.Path = REPO) -> pathlib.Path:
    """The store in the working tree at `root`."""
    return root / STORE_REL


def row_name(construct_id: str) -> str:
    """The file name of the construct `construct_id`: `<id>.json`. An id that is not a safe file name is a SystemExit."""
    if not _SAFE_ID.fullmatch(construct_id):
        raise SystemExit(f"constructs: id {construct_id!r} is not a safe file name (lowercase letters, digits and `-`)")
    return f"{construct_id}.json"


def render_record(row: dict[str, Any]) -> str:
    """The text of a store file: the one spelling `write_row` writes."""
    return json.dumps(row, indent=1, ensure_ascii=False) + "\n"


def read_rows(root: pathlib.Path = REPO) -> list[dict[str, Any]]:
    """Every construct of the catalogue at `root`, in file-name order. A file whose name is not its row's id, or that is not
    one JSON object, is a SystemExit naming it: the store's shape is its own check."""
    rows: list[dict[str, Any]] = []
    for path in sorted(store_dir(root).glob("*.json"), key=lambda p: p.name):
        try:
            row = json.loads(path.read_text(encoding="utf-8"))
        except ValueError as e:
            raise SystemExit(f"{STORE_REL}/{path.name}: not JSON ({e})") from e
        if not isinstance(row, dict) or row.get("id") is None or row_name(str(row["id"])) != path.name:
            raise SystemExit(f"{STORE_REL}/{path.name}: not one construct object whose `id` is the file's name")
        rows.append(row)
    return rows


def write_row(row: dict[str, Any], root: pathlib.Path = REPO) -> pathlib.Path:
    """Write (or replace) the file of `row`."""
    directory = store_dir(root)
    directory.mkdir(parents=True, exist_ok=True)
    path = directory / row_name(str(row["id"]))
    path.write_text(render_record(row), encoding="utf-8", newline="\n")
    return path


def self_test() -> int:
    import tempfile  # noqa: PLC0415
    failures: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            failures.append(what)

    def refuses(thunk) -> bool:
        try:
            thunk()
        except SystemExit:
            return True
        return False

    check(row_name("nucleus-move-display") == "nucleus-move-display.json", "an id is its file name")
    check(all(refuses(lambda i=i: row_name(i)) for i in ("", "../x", "a/b", "A-b", "a--b", "-a", "a_b")), "an unsafe id is refused")
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        check(read_rows(root) == [], "no directory reads as no constructs")
        write_row({"id": "b-two", "introducedIn": 2002, "text": "§ kept"}, root)
        write_row({"id": "a-one", "introducedIn": 85}, root)
        write_row({"id": "a-one", "introducedIn": 2002}, root)
        got = read_rows(root)
        check([r["id"] for r in got] == ["a-one", "b-two"] and got[0]["introducedIn"] == 2002, "rows read in file-name order, a write replaces")
        check("§ kept" in (store_dir(root) / "b-two.json").read_text(encoding="utf-8"), "text is written unescaped")
        (store_dir(root) / "c-three.json").write_text('{"id": "other"}', encoding="utf-8")
        check(refuses(lambda: read_rows(root)), "a file whose name is not its row's id is refused")
        (store_dir(root) / "c-three.json").write_text("[1]", encoding="utf-8")
        check(refuses(lambda: read_rows(root)), "a file that is not one object is refused")
    real = read_rows()
    check(len(real) >= 300 and len({r["id"] for r in real}) == len(real), f"the real store holds the catalogue ({len(real)} rows)")
    for f in failures:
        print(f"FAIL: {f}")
    print(f"construct_store self-test: {'FAIL' if failures else 'OK'}")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    if "--self-test" not in sys.argv:
        sys.exit(__doc__)
    sys.exit(self_test())
