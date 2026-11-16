#!/usr/bin/env python3
"""Cases of check_practices.py's P17 arm (kb/Work PB5764; MANDATORY-PRACTICES P17): an instruction that writes a
recursive delete of a path holding a variable is refused, in every spelling the practice names, and a literal path, a
non-recursive delete, `git rm -r` and a line citing the practice's note pass; `check` applies it to a brief or spec.
Run: python scripts/self_tests.py --only .claude/selftests/skills/workstream/check_practices/test_variable_deletes.py"""
from __future__ import annotations

import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

cp = selftest_cases.subject(__file__)   # .claude/skills/workstream/check_practices.py


def case_010_each_spelling_of_a_variable_delete_is_refused(expect):
    refused = [
        'rd=$R/$id; rm -rf "$rd"; mkdir -p "$rd"',          # the shape the probing agents wrote (PB5764)
        "reset it first: `rm -rf $B/$id`",
        'rm -r "$d"', 'rm -R ${d}', 'rm -fr "$x"', 'rm --recursive "$x"', "rm -rf $(cat dirs)",
        "cmd /c rmdir /s /q %D%", "rd /S %TEMP%\\x",
        "Remove-Item -Recurse -Force $d", "Remove-Item $d -Recurse", "ri -Recurse $env:TEMP\\x",
        "shutil.rmtree(run_dir)", "shutil.rmtree( root / name )",
        'git clean -fdx "$tree"',
    ]
    for line in refused:
        expect.equal(f"refused: {line}", len(cp.variable_deletes("brief.md", line)), 1)


def case_020_literal_non_recursive_git_rm_and_the_rule_itself_pass(expect):
    passed = [
        "rm -rf E:/COBOL-coord/scratch/w1/runs/x",            # a literal path
        "rm '$literal'",                                      # not recursive
        'rm "$f"', "rm -f $f",
        "`git rm -r <dir>` inside a worktree",                # git's removal, one commit from recovery
        'git rm -r "$d"',
        "shutil.rmtree('E:/x')",                              # a literal argument
        "Remove-Item -Recurse 'E:/x/y'",
        "`rm -rf` is refused; the path is `$d` here",         # two code spans: the delete's own span ends first
        'never `rm -rf "$rd"` (MANDATORY-PRACTICES P17, kb/Work PB5764)',   # the rule names the form it forbids
    ]
    for line in passed:
        expect.equal(f"passes: {line}", cp.variable_deletes("brief.md", line), [])


def case_030_check_refuses_a_brief_or_spec_that_writes_one(expect):
    d = scratch_dir("check-practices-p17-")
    brief = d / "msg-w1-a.txt"
    brief.write_bytes(b"step one\nrun it: rd=$R/$id; rm -rf \"$rd\"\n")
    found = [m for m in cp.check(brief, []) if "P17" in m]
    expect.equal("check() names the line of a brief or rendered spec that writes one",
                 [m.split(":", 2)[:2] for m in found], [["msg-w1-a.txt", "2"]])
    brief.write_bytes(b"step one\nrun it: python scripts/orchestrator/run_probe.py --root E:/s/w1a --std 85 p.cob\n")
    expect.equal("... and passes the same brief once it runs the probe through the tool",
                 [m for m in cp.check(brief, []) if "P17" in m], [])
    expect.equal("the practices file's row is pinned with the tool's name",
                 any("P17" in p and "run_probe" in p for p in cp.BRIEFS["MANDATORY-PRACTICES.md"]), True)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
