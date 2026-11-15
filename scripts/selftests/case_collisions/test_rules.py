#!/usr/bin/env python3
"""The case-collision rule on planted path sets and a planted repository (kb/Work PB5694): two files or two directories
whose names differ only in letter case are one finding at the shortest prefix, the simple one-to-one case mapping
decides what collides, and the audit is RED on an index that holds both spellings (the shape that got in) and on a
broken enumeration, and GREEN on a clean tree."""
import sys

import selftest_cases
from scratch_repo import ScratchRepo, scratch_dir
from standard_streams import ensure_utf8

cc = selftest_cases.subject(__file__)   # scripts/case_collisions.py


def _index_only(*paths):
    """A planted repository whose INDEX holds `paths` (one blob each), written with `git update-index`, so two spellings a
    case-insensitive checkout cannot hold side by side are both tracked, as the colliding landings left them."""
    repo = ScratchRepo.init(scratch_dir("case-collisions-"))
    blob = repo.git("hash-object", "-w", "--stdin", input="x\n")
    for path in paths:
        repo.git("update-index", "--add", "--cacheinfo", f"100644,{blob},{path}")
    return repo.path


def case_00_two_files_that_differ_only_in_case_are_a_finding(expect):
    expect.equal("README.md and readme.md collide", cc.collisions(["docs/README.md", "docs/readme.md", "docs/x.md"]),
                 [["docs/README.md", "docs/readme.md"]])
    expect.equal("names that differ in more than case do not", cc.collisions(["a/b.txt", "a/b2.txt", "a/B/c.txt"]), [])
    expect.equal("one name listed twice is one name", cc.collisions(["a/b.txt", "a/b.txt"]), [])


def case_01_two_directories_are_one_finding_at_the_directory(expect):
    paths = ["cases/8.4.2.2@2002-FUNCTION-IDENTIFIER/a.cob", "cases/8.4.2.2@2002-FUNCTION-IDENTIFIER/a.out",
             "cases/8.4.2.2@2002-Function-identifier/b.cob", "cases/8.4.2.2@2002-Function-identifier/a.cob"]
    expect.equal("the directory pair is named once, not once per file under it (a.cob is under both)", cc.collisions(paths),
                 [["cases/8.4.2.2@2002-FUNCTION-IDENTIFIER", "cases/8.4.2.2@2002-Function-identifier"]])
    expect.equal("a collision deeper down under one directory is its own finding",
                 cc.collisions(["x/Y/z.txt", "x/y/z.txt", "x/y/Q.txt", "x/y/q.txt"]),
                 [["x/Y", "x/y"]])


def case_02_the_fold_is_the_simple_one_to_one_case_mapping(expect):
    expect.equal("accented letters fold (É and é are one name on NTFS)", cc.collisions(["café.txt", "CAFÉ.txt"]),
                 [["CAFÉ.txt", "café.txt"]])
    expect.equal("ß does not fold to SS (its upper case is two characters)", cc.collisions(["straße.txt", "STRASSE.txt"]), [])
    expect.equal("fold keeps a multi-character upper case as it is", cc.fold("aß"), "Aß")


def case_03_the_audit_reads_the_index_and_is_red_on_a_collision(expect):
    rc, report = cc.check(_index_only("cases/A/x.cob", "cases/a/y.cob", "cases/b.cob"), minimum=1)
    expect("an index holding two spellings of one directory is RED, naming both", rc == 1 and "cases/A | cases/a" in report
           and "RED — 1 set(s)" in report, report)
    rc, report = cc.check(_index_only("cases/A/x.cob", "cases/B/y.cob"), minimum=1)
    expect("a clean index is GREEN", rc == 0 and "GREEN — 2 tracked path(s)" in report, report)
    rc, report = cc.check(_index_only("one.txt"))
    expect("an enumeration of fewer than MIN_TRACKED paths is RED, never a clean tree", rc == 1 and "enumeration" in report,
           report)
    rc, report = cc.check(scratch_dir("case-collisions-nogit-"), minimum=1)
    expect("a directory git cannot enumerate is RED", rc == 1 and "failed" in report, report)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
