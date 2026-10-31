#!/usr/bin/env python3
"""carry_retired_registers.py — CARRY A MERGE'S EDITS OF A RETIRED SINGLE-FILE REGISTER INTO ITS PER-ENTRY FILES
(kb/Work PB4147, PB4148; the finisher's CONFLICT stop, .claude/skills/workstream/templates/finisher-brief.md).

    python scripts/carry_retired_registers.py [--repo DIR]   # in a merge left in progress (`merge_drivers.py merge <ref> --keep`)
    python scripts/carry_retired_registers.py --self-test

WHY. Three registers that landings edited are one file per entry now, so two branches never share a file: WiseOwl's
refusals (`conformance-suite/wiseowl/diagnostics.json` became `conformance-suite/wiseowl/diagnostics/<test id>.json`, PB4147),
its compile options (`wiseowl/options.json` became `wiseowl/options/<test id>.json`, PB4147) and the traceability inventory (`tests/version-matrix/traceability-inventory.json` became
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
nothing; anything else is a REAL conflict (both sides changed that entry differently), reported by key. Every key that is
not in conflict is carried either way, through the register's own writer (`inventory_store.write_inventory`; one line per
test for the refusals and options, named by `generate.sidecar_path`), and the directory is staged. A register with no key
in conflict has its single file removed from the index and the tree. One with a key in conflict keeps it unmerged as the
finisher's reminder: the finisher resolves ONLY the keys named, `git add`s them and then `git rm`s the single file (the
entries carried are already in the tree, so nothing the editing side changed is lost). The refusal file's `$comment`
header is no entry and is not carried. A single file both sides still hold is not this script's: that is a content conflict.

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
from generate import DIAGNOSTICS_DIR, OPTIONS_DIR, load_sidecars, sidecar_path  # noqa: E402

NL = chr(10)
Entries = dict[str, Any]


def _sidecars_of(text: str) -> Entries:
    """A one-file-per-test registry's retired single file (`diagnostics.json`, `options.json`): key -> entry. The `$comment`
    header the refusals' file carried is no entry."""
    return {k: v for k, v in json.loads(text).items() if not k.startswith("$")}


def _read_sidecars(directory: pathlib.Path) -> Entries:
    entries, problems = load_sidecars(directory, directory.parent)
    if problems:
        raise SystemExit("carry: " + "; ".join(f"{where}: {why}" for where, why in problems))
    return entries


def _sidecar_writer(registry: pathlib.Path) -> Callable[[pathlib.Path, Entries, Entries], None]:
    """The writer of one `wiseowl/<registry>/` directory: only the files whose entry changed, one line each."""
    def write(directory: pathlib.Path, before: Entries, after: Entries) -> None:
        for key in before.keys() - after.keys():
            (directory / sidecar_path(registry, key).name).unlink()
        for key, entry in after.items():
            if before.get(key) != entry:
                (directory / sidecar_path(registry, key).name).write_text(
                    json.dumps(entry, ensure_ascii=False) + NL, encoding="utf-8", newline=NL)
    return write


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
             _sidecars_of, _read_sidecars, _sidecar_writer(DIAGNOSTICS_DIR)),
    Register("conformance-suite/wiseowl/options.json", "conformance-suite/wiseowl/options",
             _sidecars_of, _read_sidecars, _sidecar_writer(OPTIONS_DIR)),
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
        reg.write(directory, current, after)                 # every entry that is not in conflict is carried, either way
        subprocess.run(["git", "add", "-A", "--", reg.directory], cwd=repo, capture_output=True, check=True)
        listing = "".join(f"{NL}  {how}: {key}" for key, how in carried)
        if conflicts:
            # The single file stays unmerged as the reminder: the finisher resolves ONLY the keys named (each key's file
            # holds the deleting side's entry; the editing side's is in `git show :{stage}:{single}`), then `git rm`s it.
            out(f"carry: {reg.single}: {len(conflicts)} entr{'y' if len(conflicts) == 1 else 'ies'} changed on both "
                f"sides, left for you: {', '.join(conflicts)}; the other {len(carried)} "
                f"entr{'y' if len(carried) == 1 else 'ies'} {edited_side} changed {'is' if len(carried) == 1 else 'are'} "
                f"carried and staged. Resolve those keys' files, `git add` them, then `git rm {reg.single}`" + listing)
            worst = 1
            continue
        subprocess.run(["git", "rm", "-q", "--", reg.single], cwd=repo, capture_output=True, check=True)
        out(f"carry: {reg.single} -> {reg.directory}/: carried the {len(carried)} entr{'y' if len(carried) == 1 else 'ies'} "
            f"{edited_side} changed; the single file takes its deletion" + listing)
    return worst


# ── self-test ─────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    failures: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            failures.append(what)

    refusals, options, inventory = REGISTERS
    tests = [f"t{i:02}" for i in range(24)]                  # enough entries that git pairs no entry file with the
    base_refusals = {"$comment": "header", **{t: {"refuse-at": ["85"], "expect": f"COBOL{1000 + i}"}  # single file
                                              for i, t in enumerate(tests)}}
    base_options = {t: {"numeric-display": "digits", "n": i} for i, t in enumerate(tests)}
    base_rows = [{"rule-id": f"GR-4.4-{i}", "section": "4.4", "ordinal": i, "verdict": "", "notes": f"n{i}"}
                 for i in range(1, 25)]

    def singles(ref: dict[str, Any], opts: dict[str, Any], rows: list[dict[str, Any]]) -> dict[str, str]:
        return {refusals.single: json.dumps(ref, indent=1) + NL, options.single: json.dumps(opts, indent=1) + NL,
                inventory.single: json.dumps(rows, indent=1) + NL}

    def split(repo: ScratchRepo, ref: dict[str, Any], opts: dict[str, Any], rows: list[dict[str, Any]]) -> None:
        """The PB4147/PB4148 change: the single files become per-entry directories, written by their own writers."""
        repo.write({refusals.single: None, options.single: None, inventory.single: None})
        for reg, entries in ((refusals, _sidecars_of(json.dumps(ref))), (options, opts)):
            (repo.path / reg.directory).mkdir(parents=True)
            reg.write(repo.path / reg.directory, {}, entries)
        inventory_store.write_inventory(rows, repo.path / inventory.directory)

    t = scratch_dir("carry-retired-")
    repo = ScratchRepo.init(t / "repo")
    repo.commit("base", singles(base_refusals, base_options, base_rows))
    # main edits the single files: a refusal added, one reworded, one removed; an option set changed and one added; a
    # row re-sited.
    main_refusals = {**base_refusals, "t99": {"refuse-at": ["2023"], "expect": "COBOL5563"},
                     "t01": {"refuse-at": ["85", "2002"], "expect": "COBOL1001"}}
    del main_refusals["t02"]
    main_options = {**base_options, "t03": {"sign-encoding": "ascii"}, "t98/step.cbl": {"flag-extensions": "on"}}
    main_rows = [dict(r, notes="moved") if r["rule-id"] == "GR-4.4-3" else r for r in base_rows]
    repo.git("checkout", "-q", "-b", "edits")
    repo.commit("main edits the single files", singles(main_refusals, main_options, main_rows))
    edits_head = repo.git("rev-parse", "HEAD")
    repo.git("checkout", "-q", "main")
    repo.git("checkout", "-q", "-b", "split")
    split(repo, base_refusals, base_options, base_rows)
    repo.commit("split the registers")
    split_head = repo.git("rev-parse", "HEAD")
    want_refusals = _sidecars_of(json.dumps(main_refusals))
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
        check(not any((repo.path / reg.single).exists() for reg in REGISTERS), f"{label}: the single files take their deletion")
        check(_read_sidecars(repo.path / refusals.directory) == want_refusals,
              f"{label}: the refusals equal the edited single file's")
        check(_read_sidecars(repo.path / options.directory) == main_options,
              f"{label}: the options equal the edited single file's")
        check(_read_rows(repo.path / inventory.directory) == want_rows, f"{label}: the rows equal the edited single file's")
        repo.git("commit", "-q", "--no-edit")
        return repo.git("rev-parse", "HEAD^{tree}").strip()

    # The direction PB4148's own landing met (the branch split, main edited: DU) and the one every branch cut before
    # the split meets after it lands (the branch edited, main split: UD) give the same tree.
    check(merged("split", edits_head, "DU") == merged("edits", split_head, "UD"), "both directions merge to one tree")

    # A refusal both sides changed differently is a real conflict: named, and the single file stays unmerged. The
    # editing side's OTHER entries (t99 added, t02 removed) are still carried, staged and not lost when the finisher
    # resolves the named key and removes the single file (the review's R1-1 of the first landing).
    repo.git("checkout", "-q", "-b", "split2", split_head)
    repo.write({f"{refusals.directory}/t01.json": json.dumps({"refuse-at": ["2014"], "expect": "COBOL1001"}) + NL})
    repo.commit("the split side rewords t01 too")
    repo.run("merge", "--no-ff", "--no-commit", edits_head)
    said = []
    check(carry(repo.path, said.append) == 1 and any("t01" in s for s in said), f"a both-sides change is refused ({said})")
    check(refusals.single in repo.git("ls-files", "-u"), "the refused register's single file stays unmerged")
    for other in (options, inventory):
        check(other.single not in repo.git("ls-files", "-u"), f"the {other.directory} register is still carried")
    kept = _read_sidecars(repo.path / refusals.directory)
    check(kept.get("t99") == main_refusals["t99"] and "t02" not in kept,
          "the entries not in conflict are carried although another one is")
    check(kept.get("t01") == {"refuse-at": ["2014"], "expect": "COBOL1001"}, "the conflicting key is left as the split side had it")
    check(not repo.git("diff", "--name-only", "--", refusals.directory).strip(), "the carried entries are staged")
    repo.git("rm", "-q", "--", refusals.single)
    check(not repo.git("ls-files", "-u").strip(), "removing the single file ends the conflict")
    repo.git("commit", "-q", "--no-edit")
    check(_read_sidecars(repo.path / refusals.directory).get("t99") == main_refusals["t99"],
          "the committed merge keeps the carried entry")
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
