#!/usr/bin/env python3
"""Self-test for readonly_repo.py — a write inside a git tree is blocked, a write in a non-repo directory passes.
Run: python scripts/hooks/test_readonly_repo.py   (every gate and CI run it through scripts/self_tests.py.)"""
import json
import os
import pathlib
import subprocess
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from scratch_repo import scratch_dir  # noqa: E402  (scripts/: the one scratch fixture)
from standard_streams import ensure_utf8  # noqa: E402

ensure_utf8()

HOOK = pathlib.Path(__file__).with_name("readonly_repo.py")
REPO = pathlib.Path(__file__).resolve().parents[2]
OUTSIDE = scratch_dir("test-readonly-repo-")

CASES = [
    (str(REPO / "src" / "x.cs"), True),
    (str(REPO / "docs" / "new-dir" / "deeper" / "y.md"), True),   # a not-yet-existing directory inside the tree
    (str(OUTSIDE / "out" / "refute-a.jsonl"), False),
    (str(OUTSIDE / "report.md"), False),
]


# git's answer decides every case, never the clock (kb/Work PB3536): the hook's 15 s hang guard used to PASS the write
# when it expired, so a loaded gate could turn the BLOCK cases red
ENV = dict(os.environ, COBOL_READONLY_GIT_WAIT_S="86400")


def blocked(path: str, env: dict = ENV) -> tuple[bool, str]:
    r = subprocess.run([sys.executable, str(HOOK)], input=json.dumps({"tool_input": {"file_path": path}}),
                       capture_output=True, text=True, timeout=300, env=env)
    return r.returncode == 2, r.stderr


fails = [(p, e) for p, e in CASES if blocked(p)[0] != e]
# a git with no answer in time REFUSES the write, even outside a tree: a planted 1 us wait expires before git can start
was, why = blocked(str(OUTSIDE / "report.md"), dict(ENV, COBOL_READONLY_GIT_WAIT_S="0.000001"))
if not (was and "gave no answer within" in why):
    fails.append(("a git with no answer in time refuses the write", True))
for p, e in fails:
    print(f"FAIL: expected {'BLOCK' if e else 'PASS'}: {p}")
print(f"readonly_repo self-test: {len(CASES) + 1 - len(fails)}/{len(CASES) + 1} " + ("GREEN" if not fails else "RED"))
sys.exit(1 if fails else 0)
