#!/usr/bin/env python3
"""selftest_cases.py — THE COLLECTOR A CASE FILE RUNS ITSELF WITH, and the one way it reaches its subject
(docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.2; kb/Work PB5428, PB3726).

A case file (`<root>/selftests/<path>/<name>/test_<world>.py`) holds one `case_<name>(expect)` function per case and ends

    if __name__ == "__main__":
        sys.exit(selftest_cases.run(sys.modules[__name__]))

`run` finds every `case_*` function the module defines and runs them in NAME order (the order the file keeps them in:
scripts/selftest_layout.py refuses a file whose cases are out of order), each with a fresh `Expect`. It prints `PASS` or
`FAIL` per expectation, counts an exception as a FAIL of that case with its traceback, counts a case that made no
expectation as a FAIL (a case that returns before its first `expect` passes nothing), refuses a module with no case
(exit 1: an empty file is not a pass), and ends with `=== <file>: PASS|FAIL (n cases, m expectations) ===`. The scratch
directories a case makes through `scratch_repo.scratch_dir` are removed when the process ends, whatever the way out.
No decorator, no registry, no plug-in.

`subject(__file__)` derives the case file's subject from the file's place (`selftest_layout.subject_of`), puts the
subject's directory first on `sys.path` (so the subject's own sibling imports resolve as they do when it runs), imports
it once and returns the module. A test reads and plants internals as attributes of that module; nothing in a subject is
made public for a test, and no case file edits `sys.path` itself.

`python scripts/selftest_cases.py` has no entry point: a case file runs through `python scripts/self_tests.py --only
<path>`, which gives it the `PYTHONPATH` (`scripts/`) its `import selftest_cases` needs. Run bare it fails at that import.
"""
from __future__ import annotations

import importlib
import inspect
import sys
import traceback
from collections.abc import Callable
from pathlib import Path
from types import ModuleType

import selftest_layout
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]


class Expect:
    """One case's expectations: `expect(label, ok, detail="")`, the shape every self-test already uses, and
    `expect.equal(label, got, want)` for the comparison that most of them are."""

    def __init__(self) -> None:
        self.labels: list[str] = []
        self.failed: list[str] = []

    def __call__(self, label: str, ok: object, detail: str = "") -> None:
        self.labels.append(label)
        print(f"{'PASS' if ok else 'FAIL'}     {label}" + (f": {detail}" if detail and not ok else ""), flush=True)
        if not ok:
            self.failed.append(label)

    def equal(self, label: str, got: object, want: object) -> None:
        self(label, got == want, f"got {got!r}, want {want!r}")


def case_functions(module: ModuleType) -> list[tuple[str, Callable[[Expect], None]]]:
    """The `case_*` functions the module itself defines, in name order."""
    return sorted((n, f) for n, f in vars(module).items()
                  if n.startswith("case_") and inspect.isfunction(f) and f.__module__ == module.__name__)


def run(module: ModuleType) -> int:
    """Run every case of `module` (module docstring); the process's exit code."""
    ensure_utf8()
    file = Path(module.__file__).resolve()
    try:
        shown = file.relative_to(REPO).as_posix()
    except ValueError:
        shown = file.as_posix()
    cases = case_functions(module)
    if not cases:
        print(f"=== {shown}: FAIL (no case_ function: an empty file is not a pass) ===")
        return 1
    expectations, failed = 0, 0
    for name, fn in cases:
        expect = Expect()
        try:
            fn(expect)
        except Exception:  # noqa: BLE001 — any exception is the case's FAIL, with its traceback
            expect(f"{name} raised", False, "\n" + traceback.format_exc())
        if not expect.labels:
            expect(f"{name} made no expectation", False, "a case that returns before its first expect passes nothing")
        expectations += len(expect.labels)
        failed += bool(expect.failed)
    print(f"=== {shown}: {'FAIL' if failed else 'PASS'} ({len(cases)} cases, {expectations} expectations"
          + (f", {failed} failed" if failed else "") + ") ===", flush=True)
    return 1 if failed else 0


def subject(path: str | Path) -> ModuleType:
    """The module a case file tests (module docstring). `path` is the case file's `__file__`. A subject that is not a
    Python file is run as a process by its cases, and refused here."""
    file = Path(path).resolve()
    try:
        rel = file.relative_to(REPO).as_posix()
    except ValueError as e:
        raise LookupError(f"{file} is outside the repository {REPO}: it has no subject") from e
    found = selftest_layout.subject_of(rel, REPO)
    if found is None:
        raise LookupError(f"{rel}: no subject (selftest_layout.subject_of finds no script for its directory)")
    if not found.endswith(".py"):
        raise LookupError(f"{rel}: its subject {found} is not Python; its cases run it as a process")
    directory = str((REPO / found).parent)
    if directory not in sys.path:
        sys.path.insert(0, directory)
    return importlib.import_module(Path(found).stem)
