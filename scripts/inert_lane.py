"""inert_lane.py — THE LANES' COMMANDS (kb/Work PB2878, PB4395, PB5398; docs/rearchitecture/DESIGN-test-build-ci.md §3.18):

    python scripts/inert_lane.py classify --base <rev> [--head <rev-or-tree>]   # lane=, tooling=, reads_tooling=, reason=
    python scripts/inert_lane.py watch [--root R] [--events F] -- <command …>   # Linux: run it, report any watched read
    python scripts/inert_lane.py check     # THE INERT LANE audit: every entry names a tracked path, every build step
                                           # and every compiler test that runs a script is CODE_READS

The lanes and the one classification are `lane_classification.py`'s (its docstring is the rule: what is INERT, what is READ,
what is TOOLING and CODE_READS, the two flags), the static reads of the build and the compiler tests `source_reads.py`'s, and the
inotify watch `inert_watch.py`'s. This module is what runs them: `classify` (CI's `changes` job, the local gate), `watch` (every
Linux test run) and `check`, THE INERT LANE audit: every entry names a tracked path, every category is in key order, and every
build step and compiler test that runs a script is CODE_READS.
"""
from __future__ import annotations

import argparse
import posixpath
import subprocess
import sys
from pathlib import Path

from inert_watch import watch
from lane_classification import CLASSIFICATION, MARKDOWN_ENTRY, REPO, Classification, Lane, changed, tracked, under
from source_reads import code_readers, markdown_readers, TESTS_MAY_NAME_MARKDOWN
from standard_streams import ensure_utf8


def check(classification: Classification, root: Path) -> list[str]:
    """THE INERT LANE audit (`check`): every category of the classification is in key order; every INERT, READ, TOOLING_READS and
    CODE_READS entry names at least one tracked path; every READ entry lies under an INERT one and every CODE_READS entry is a script-tree path; and
    every tooling the build or a compiler test is SEEN to read (`code_readers`) lies under a CODE_READS entry."""
    paths = tracked(root)
    bad = classification.order_findings()
    bad += [f"the {kind} entry {e!r} names no tracked path: remove it, or correct it" for kind, entries in
           (("INERT", classification.inert), ("READ", classification.read), ("TOOLING_READS", classification.tooling_reads),
            ("CODE_READS", classification.code_reads)) for e in entries
           if not any(under(p, e) for p in paths)]
    bad += [f"the READ entry {e!r} lies under no INERT entry" for e in classification.read
            if not any(under(e, i) for i in classification.inert)]
    bad += [f"the CODE_READS entry {e!r} lies in no tooling tree (TOOLING_UNDER), so it changes no lane: remove it"
            for e in classification.code_reads if not classification.in_tooling_tree(e)]
    for path, reader in code_readers(root, paths, classification.tooling_under):
        if classification.in_tooling_lane(path):
            bad.append(f"{reader} reads {path}, which is in the tooling lane: a change to it would gate with no build "
                       "and no compiler test; give it a CODE_READS entry naming that reader, or make the reader a "
                       "script's self-test (kb/Work PB4395)")
    # A TOOLING_READS or CODE_READS entry that is itself INERT never changes a lane (`classify` asks `is_inert` first), so
    # it is dead; and the Markdown inert entry stands only while no compiler test opens a Markdown file (kb/Work PB4453).
    bad += [f"the {kind} entry {e!r} is inert (an INERT entry covers it), so it changes no lane: remove it"
            for kind, entries in (("TOOLING_READS", classification.tooling_reads), ("CODE_READS", classification.code_reads))
            for e in entries if classification.is_inert(e)]
    if MARKDOWN_ENTRY in classification.inert:
        allowed = {posixpath.basename(e): "a READ entry" for e in classification.read if e.lower().endswith(".md")}
        bad += markdown_readers(root, paths, allowed | TESTS_MAY_NAME_MARKDOWN)
    return bad


def main(argv: list[str]) -> int:
    command: list[str] = []
    if "--" in argv:
        i = argv.index("--")
        argv, command = argv[:i], argv[i + 1:]
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("classify")
    c.add_argument("--base", required=True)
    c.add_argument("--head", default="HEAD")
    c.add_argument("--root", type=Path, default=REPO)
    w = sub.add_parser("watch")
    w.add_argument("--root", type=Path, default=REPO)
    w.add_argument("--events", type=Path)
    k = sub.add_parser("check")
    k.add_argument("--root", type=Path, default=REPO)
    args = ap.parse_args(argv)
    if args.cmd == "classify":
        try:
            lane = CLASSIFICATION.classify(changed(args.root, args.base, args.head))
        except (subprocess.CalledProcessError, OSError) as e:
            lane = Lane("full", f"the change could not be listed ({e}): the full lane", tooling=True, reads_tooling=True)
        print(f"lane={lane.name}")
        print(f"tooling={'true' if lane.tooling else 'false'}")
        print(f"reads_tooling={'true' if lane.reads_tooling else 'false'}")
        print(f"reason={lane.reason}")
        return 0
    if args.cmd == "watch":
        if not command:
            ap.error("watch needs a command after --")
        return watch(args.root.resolve(), command, args.events)
    bad = check(CLASSIFICATION, args.root)
    for b in bad:
        print(f"    {b}")
    n = sum(1 for p in tracked(args.root) if CLASSIFICATION.is_inert(p))
    print(f"=== INERT LANE CHECK: {'RED — ' + str(len(bad)) + ' finding(s)' if bad else 'GREEN'} — "
          f"{n} tracked inert path(s) under {len(CLASSIFICATION.inert)} INERT entr(ies), "
          f"{len(CLASSIFICATION.read)} READ ===")
    return 1 if bad else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
