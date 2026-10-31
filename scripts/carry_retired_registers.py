#!/usr/bin/env python3
"""carry_retired_registers.py — CARRY A MERGE'S EDITS OF A RETIRED SINGLE-FILE REGISTER INTO ITS PER-ENTRY FILES
(kb/Work PB4147, PB4148; the finisher's CONFLICT stop, .claude/skills/workstream/templates/finisher-brief.md).

    python scripts/carry_retired_registers.py [--repo DIR]   # in a merge left in progress (`merge_drivers.py merge <ref> --keep`)
    python scripts/carry_retired_registers.py --self-test

WHY. Two registers that most landings edited are one file per entry now, so two branches never share a file:
WiseOwl's refusals (`conformance-suite/wiseowl/diagnostics.json` became `conformance-suite/wiseowl/diagnostics/<test id>.json`,
PB4147) and the traceability inventory (`tests/version-matrix/traceability-inventory.json` became
`tests/version-matrix/inventory/<rule-id>.json`, PB4148). A branch on one side of that change that edited the single file
meets, at a merge with the other side, a MODIFY/DELETE conflict (git's DU or UD): one side deleted the file and the other
changed entries in it, which no merge driver sees. Resolving it is mechanical, so this script does it. The entries the
editing side changed since the merge base are carried into the per-entry files, three-way, entry by entry, and the single
file takes its deletion. PB4148's own landing met it first (four refusals and six inventory rows main had changed), and
this script, replayed on that merge, gives the tree its finisher committed.

THE RULE, for each register whose single file is unmerged with exactly one side still holding it (index stages 1 and 2,
or 1 and 3): BASE is stage 1 (the merge base's file), EDITED is the stage that still holds it, CURRENT is the per-entry
directory in the working tree (the deleting side's files, as git merged them). For every key whose entry differs between
BASE and EDITED: CURRENT equal to BASE takes EDITED's entry (added, changed or removed); CURRENT equal to EDITED needs
nothing; anything else is a REAL conflict (both sides changed that entry differently), reported by key, and that register
is left unmerged for the finisher. A register with no such key is written through its own writer
(`inventory_store.write_inventory`; one `{...}` line per test for the refusals, named by `generate.sidecar_path`), its
single file is removed from the index and the tree, and its directory is staged. The refusal file's `$comment` header is
no entry and is not carried. A single file both sides still hold is not this script's: that is a content conflict.

EXIT: 0, nothing it carries is left unmerged (or there was nothing to carry); 1, a key in conflict, a single file
unmerged in a shape it does not carry, or a per-entry directory its reader refuses (a stray or unreadable file), each
named; 2, not inside a merge.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys
from typing import Any, Callable, NamedTuple

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent
sys.path.insert(0, str(HERE / "spec"))
sys.path.insert(0, str(REPO / "conformance-suite" / "harness"))
import inventory_store  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402
from generate import DIAGNOSTICS_DIR, load_sidecars, sidecar_path  # noqa: E402

NL = chr(10)
Entries = dict[str, Any]


def _refusals_of(text: str) -> Entries:
    return {k: v for k, v in json.loads(text).items() if not k.startswith("$")}


def _read_refusals(directory: pathlib.Path) -> Entries:
    entries, problems = load_sidecars(directory, directory.parent)
    if problems:
        raise SystemExit("carry: " + "; ".join(f"{where}: {why}" for where, why in problems))
    return entries


def _write_refusals(directory: pathlib.Path, before: Entries, after: Entries) -> None:
    for key in before.keys() - after.keys():
        (directory / sidecar_path(DIAGNOSTICS_DIR, key).name).unlink()
    for key, entry in after.items():
        if before.get(key) != entry:
            (directory / sidecar_path(DIAGNOSTICS_DIR, key).name).write_text(
                json.dumps(entry, ensure_ascii=False) + NL, encoding="utf-8", newline=NL)


def _rows_of(text: str) -> Entries:
    return {r["rule-id"]: r for r in json.loads(text)}


def _read_rows(directory: pathlib.Path) -> Entries:
    return {r["rule-id"]: r for r in inventory_store.load_inventory(directory)}


def _write_rows(directory: pathlib.Path, _before: Entries, after: Entries) -> None:
    inventory_store.write_inventory(list(after.values()), directory)


class Register(NamedTuple):
    """A register that was one file and is one file per entry: where each lives, and how each is read and written."""
    single: str                                              # the retired single file, repository-relative
    directory: str                                           # its per-entry directory, repository-relative
    entries_of: Callable[[str], Entries]                     # the single file's text -> key -> entry
    read: Callable[[pathlib.Path], Entries]                  # the directory -> key -> entry
    write: Callable[[pathlib.Path, Entries, Entries], None]  # (directory, before, after): make it hold `after`


REGISTERS = (
    Register("conformance-suite/wiseowl/diagnostics.json", "conformance-suite/wiseowl/diagnostics",
             _refusals_of, _read_refusals, _write_refusals),
    Register(inventory_store.LEGACY_FILE_REL, inventory_store.INVENTORY_DIR_REL, _rows_of, _read_rows, _write_rows),
)


def three_way(base: Entries, edited: Entries, current: Entries) -> tuple[Entries, list[tuple[str, str]], list[str]]:
    """(CURRENT with EDITED's changes since BASE carried in, the carried keys with how, the keys in conflict)."""
    out = dict(current)
    carried: list[tuple[str, str]] = []
    conflicts: list[str] = []
    for key in sorted(base.keys() | edited.keys()):
        was, now, here = base.get(key), edited.get(key), current.get(key)
        if was == now or here == now:
            continue
        if here != was:
            conflicts.append(key)
        elif now is None:
            del out[key]
            carried.append((key, "removed"))
        else:
            out[key] = now
            carried.append((key, "added" if was is None else "changed"))
    return out, carried, conflicts


def _stages(repo: pathlib.Path, path: str) -> dict[int, str]:
    """The unmerged index stages of `path`: stage number -> blob id (empty when `path` is not unmerged)."""
    listing = subprocess.run(["git", "ls-files", "-u", "--", path], cwd=repo, capture_output=True, text=True,
                             encoding="utf-8", check=True).stdout
    return {int(meta.split()[2]): meta.split()[1] for meta, _ in (ln.split("\t", 1) for ln in listing.splitlines())}


def _blob(repo: pathlib.Path, blob: str) -> str:
    return subprocess.run(["git", "cat-file", "blob", blob], cwd=repo, capture_output=True, check=True).stdout.decode("utf-8")


def carry(repo: pathlib.Path, out=print) -> int:
    """Carry every retired register's modify/delete in the merge in progress in `repo` (module doc); the exit code."""
    if subprocess.run(["git", "rev-parse", "-q", "--verify", "MERGE_HEAD"], cwd=repo, capture_output=True).returncode:
        out("carry: no merge in progress (run `python scripts/merge_drivers.py merge <ref> --keep` first)")
        return 2
    worst = 0
    for reg in REGISTERS:
        stages = _stages(repo, reg.single)
        if not stages:
            continue
        if set(stages) not in ({1, 2}, {1, 3}):
            out(f"carry: {reg.single} is unmerged with stages {sorted(stages)}, not a modify/delete: a content conflict")
            worst = 1
            continue
        directory = repo / reg.directory
        if not directory.is_dir():
            out(f"carry: {reg.single} is a modify/delete but the tree has no {reg.directory}/ to carry it into")
            worst = 1
            continue
        current = reg.read(directory)
        edited_side = "HEAD (ours)" if 2 in stages else "MERGE_HEAD (theirs)"
        after, carried, conflicts = three_way(reg.entries_of(_blob(repo, stages[1])),
                                              reg.entries_of(_blob(repo, stages[max(stages)])), current)
        if conflicts:
            out(f"carry: {reg.single}: {len(conflicts)} entr{'y' if len(conflicts) == 1 else 'ies'} changed on both "
                f"sides, left unmerged for you: {', '.join(conflicts)}")
            worst = 1
            continue
        reg.write(directory, current, after)
        subprocess.run(["git", "rm", "-q", "--", reg.single], cwd=repo, capture_output=True, check=True)
        subprocess.run(["git", "add", "-A", "--", reg.directory], cwd=repo, capture_output=True, check=True)
        out(f"carry: {reg.single} -> {reg.directory}/: carried the {len(carried)} entr{'y' if len(carried) == 1 else 'ies'} "
            f"{edited_side} changed; the single file takes its deletion"
            + "".join(f"{NL}  {how}: {key}" for key, how in carried))
    return worst


# ── self-test ─────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    failures: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            failures.append(what)

    refusals, inventory = REGISTERS
    tests = [f"t{i:02}" for i in range(24)]                  # enough entries that git pairs no entry file with the
    base_refusals = {"$comment": "header", **{t: {"refuse-at": ["85"], "expect": f"COBOL{1000 + i}"}  # single file
                                              for i, t in enumerate(tests)}}
    base_rows = [{"rule-id": f"GR-4.4-{i}", "section": "4.4", "ordinal": i, "verdict": "", "notes": f"n{i}"}
                 for i in range(1, 25)]

    def singles(ref: dict[str, Any], rows: list[dict[str, Any]]) -> dict[str, str]:
        return {refusals.single: json.dumps(ref, indent=1) + NL, inventory.single: json.dumps(rows, indent=1) + NL}

    def split(repo: ScratchRepo, ref: dict[str, Any], rows: list[dict[str, Any]]) -> None:
        """The PB4147/PB4148 change: the single files become per-entry directories, written by their own writers."""
        repo.write({refusals.single: None, inventory.single: None})
        (repo.path / refusals.directory).mkdir(parents=True)
        _write_refusals(repo.path / refusals.directory, {}, _refusals_of(json.dumps(ref)))
        inventory_store.write_inventory(rows, repo.path / inventory.directory)

    t = scratch_dir("carry-retired-")
    repo = ScratchRepo.init(t / "repo")
    repo.commit("base", singles(base_refusals, base_rows))
    # main edits the single files: a refusal added, one reworded, one removed; a row re-sited.
    main_refusals = {**base_refusals, "t99": {"refuse-at": ["2023"], "expect": "COBOL5563"},
                     "t01": {"refuse-at": ["85", "2002"], "expect": "COBOL1001"}}
    del main_refusals["t02"]
    main_rows = [dict(r, notes="moved") if r["rule-id"] == "GR-4.4-3" else r for r in base_rows]
    repo.git("checkout", "-q", "-b", "edits")
    repo.commit("main edits the single files", singles(main_refusals, main_rows))
    edits_head = repo.git("rev-parse", "HEAD")
    repo.git("checkout", "-q", "main")
    repo.git("checkout", "-q", "-b", "split")
    split(repo, base_refusals, base_rows)
    repo.commit("split the registers")
    split_head = repo.git("rev-parse", "HEAD")
    want_refusals = _refusals_of(json.dumps(main_refusals))
    want_rows = {r["rule-id"]: r for r in main_rows}

    def merged(side: str, other: str, label: str) -> str:
        repo.git("checkout", "-q", side)
        started = repo.run("merge", "--no-ff", "--no-commit", other)
        check(started.returncode != 0 and "CONFLICT (modify/delete)" in started.stdout,
              f"{label}: the merge is a modify/delete conflict ({started.stdout.strip()[:120]})")
        said: list[str] = []
        code = carry(repo.path, said.append)
        check(code == 0, f"{label}: carry exits 0 ({said})")
        check(not repo.git("ls-files", "-u").strip(), f"{label}: nothing is left unmerged")
        check(not (repo.path / refusals.single).exists() and not (repo.path / inventory.single).exists(),
              f"{label}: the single files take their deletion")
        check(_read_refusals(repo.path / refusals.directory) == want_refusals,
              f"{label}: the refusals equal the edited single file's")
        check(_read_rows(repo.path / inventory.directory) == want_rows, f"{label}: the rows equal the edited single file's")
        repo.git("commit", "-q", "--no-edit")
        return repo.git("rev-parse", "HEAD^{tree}").strip()

    # The direction PB4148's own landing met (the branch split, main edited: DU) and the one every branch cut before
    # the split meets after it lands (the branch edited, main split: UD) give the same tree.
    check(merged("split", edits_head, "DU") == merged("edits", split_head, "UD"), "both directions merge to one tree")

    # A refusal both sides changed differently is a real conflict: named, and the single file stays unmerged.
    repo.git("checkout", "-q", "-b", "split2", split_head)
    repo.write({f"{refusals.directory}/t01.json": json.dumps({"refuse-at": ["2014"], "expect": "COBOL1001"}) + NL})
    repo.commit("the split side rewords t01 too")
    repo.run("merge", "--no-ff", "--no-commit", edits_head)
    said = []
    check(carry(repo.path, said.append) == 1 and any("t01" in s for s in said), f"a both-sides change is refused ({said})")
    check(refusals.single in repo.git("ls-files", "-u"), "the refused register's single file stays unmerged")
    check(inventory.single not in repo.git("ls-files", "-u"), "the other register is still carried")
    repo.run("merge", "--abort")
    said = []
    check(carry(repo.path, said.append) == 2 and "no merge" in said[0], "outside a merge it refuses with exit 2")
    check(three_way({"a": 1}, {"a": 1}, {"a": 2}) == ({"a": 2}, [], []), "an entry only the directory changed stays")
    for f in failures:
        print(f"FAIL: {f}")
    print(f"carry_retired_registers self-test: {'FAIL' if failures else 'OK'}")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(NL, 1)[0])
    ap.add_argument("--repo", type=pathlib.Path, default=pathlib.Path.cwd())
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    return carry(a.repo.resolve())


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
