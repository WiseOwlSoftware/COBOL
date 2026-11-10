#!/usr/bin/env python3
"""doc_audits.py — THE DOCUMENTATION AUDITS: what the Markdown says is checked HERE, never by a compiler test (kb/Work PB4453).

    python scripts/doc_audits.py --check            # every check, over the committed tree (the gate's audit, CI's too)
    python scripts/doc_audits.py --write-register   # regenerate tests/version-matrix/conformance-register.json
    python scripts/doc_audits.py --write-vcr        # regenerate the VERSION_CHANGE_REFERENCE.md gating status index
    python scripts/doc_audits.py --write-manual     # regenerate tests/doc-facts/manual-programs/ from the user manual
    python scripts/doc_audits.py --self-test

⛔ WHY (owner 2026-11-02, kb/Work PB4453: "There is no compelling reason a compiler change needs to read an markdown
note"). Some thirty Unit and Conformance classes read docs/CONFORMANCE.md, VERSION_CHANGE_REFERENCE.md, the user manual,
the README and the design docs, so a one-word edit to a document made the whole population run (about 13 minutes, a build
and three assemblies) to learn nothing about the compiler. A document cannot change what the compiler does; it can only
disagree with a fact, and the fact is one of three things:
  1. another document or a script's data (a README summarizing a register, the VCR's anchors against the construct catalogue):
     a check HERE, over the Markdown alone;
  2. a fact the COMPILER holds (the dynamic-length maximum, the implementor-defined exception names, the command line's
     options): `tests/doc-facts/compiler-facts.json`, written by the Unit test DocumentedFactsDriftTests and held equal to
     the compiler there. The Markdown is checked against that FILE, here;
  3. a datum the compiler tests need that the document ALSO states (a register row, a program the manual prints): an
     EXTRACT the tests read (tests/version-matrix/conformance-register.json, tests/doc-facts/), written from the document
     by this script and held fresh by a check HERE, so a change to the datum reaches the tests and a change to the prose
     around it does not.
`inert_lane.py` classifies every `*.md` and `docs/` as INERT (the audits run, no test does), `check` of that module holds
every compiler test to opening no Markdown file, and the Linux inotify watch proves it on every merge group. The one
Markdown file a test reads is the ISO text itself, the oracle (CLAUDE.md rule 1), a READ entry of that classification.

Each module of scripts/docchecks/ holds its checks as pure functions of the text they judge, and a `self_test()` that
plants every violation in fabricated text and requires the finding back (a guard that cannot fail is no evidence).
"""
from __future__ import annotations

import argparse
import importlib
import sys
from pathlib import Path

from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]
#: The modules of scripts/docchecks/, in the order the audit reports them.
MODULES = ("register", "design", "facts", "vcr", "manual", "inventory", "tree")
VERDICT = "=== DOC AUDITS: "


def load(name: str):
    return importlib.import_module(f"docchecks.{name}")


def check(root: Path) -> int:
    findings = 0
    ran = 0
    for name in MODULES:
        for c in load(name).CHECKS:
            ran += 1
            problems = c.run(root)
            if problems:
                findings += len(problems)
                print(f"RED  {c.name}")
                for p in problems[:40]:
                    print(f"       {p}")
                if len(problems) > 40:
                    print(f"       … and {len(problems) - 40} more")
    if ran < 29:
        print(f"{VERDICT}RED — only {ran} checks ran: a module lost its checks, and a missing observation is not a negative one ===")
        return 1
    if findings:
        print(f"{VERDICT}RED — {findings} finding(s) over {ran} checks ===")
        return 1
    print(f"{VERDICT}GREEN — {ran} checks, no document disagrees with the fact it states ===")
    return 0


def self_test() -> int:
    failures: list[str] = []
    for name in MODULES:
        failures += [f"{name}: {f}" for f in load(name).self_test()]
    for f in failures:
        print(f"FAIL  {f}")
    print(f"{VERDICT}SELF-TEST {'FAIL' if failures else 'PASS'} ===")
    return 1 if failures else 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--check", action="store_true")
    group.add_argument("--write-register", action="store_true")
    group.add_argument("--write-vcr", action="store_true")
    group.add_argument("--write-manual", action="store_true")
    group.add_argument("--self-test", action="store_true")
    parser.add_argument("--root", type=Path, default=REPO)
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    if args.write_register:
        load("register").write_extract(args.root)
        return 0
    if args.write_vcr:
        load("vcr").write_index(args.root)
        return 0
    if args.write_manual:
        load("manual").write_programs(args.root)
        return 0
    return check(args.root)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
