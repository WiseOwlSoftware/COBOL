#!/usr/bin/env python3
"""The inert lane's watch on a planted repository (DESIGN-orchestrator-loop.md section 16.2, kb/Work PB5397; kb/Work PB2878, PB4395): a command that reads or lists an inert path is RED naming it, one that reads only a READ path or code is GREEN and keeps its exit code, a tooling-lane module is RED though a bytecode cache of it exists, and a CODE_READS module is GREEN. The watch is inotify, so these arms run on Linux (CI's audits job and the Linux gate) and each case passes with its reason off it."""
import subprocess
import sys

import fixtures
import inert_watch
import lane_classification
import selftest_cases
from standard_streams import ensure_utf8

Classification = lane_classification.Classification
watch = inert_watch.watch

# The mechanics, on a planted classification with a READ entry.
c = Classification(inert={"DEVLOG/": "", "kb/": "", "tools/pin": ""}, read={"kb/Work/": ""})
py = [sys.executable, "-c"]
ALL_STEPS = ("rename", "tooling", "listings", "imports", "case_files")


def case_00_command_reads_inert_file_red_naming(expect):
    if not sys.platform.startswith("linux"):
        expect("the inotify arms run on Linux only (CI's audits job and the Linux gate)", True)
        return
    root, repo, base = fixtures.world(*ALL_STEPS)
    out: list[str] = []
    expect.equal("a command that reads an inert file is RED, naming it",
                 (watch(root, py + [f"open({str(root / 'DEVLOG' / '0001-x.md')!r}).read()"], None, out.append, c) != 0,
                  any("opened: DEVLOG/0001-x.md" in s for s in out)), (True, True))


def case_01_command_lists_inert_directory_red(expect):
    if not sys.platform.startswith("linux"):
        expect("the inotify arms run on Linux only (CI's audits job and the Linux gate)", True)
        return
    root, repo, base = fixtures.world(*ALL_STEPS)
    out: list[str] = []
    expect.equal("a command that LISTS an inert directory is RED",
                 (watch(root, py + [f"import os; os.listdir({str(root / 'kb' / 'Spec')!r})"], None, out.append, c) != 0,
                  any("opened: kb/Spec/" in s for s in out)), (True, True))


def case_02_command_reads_only_read_path_code_green(expect):
    if not sys.platform.startswith("linux"):
        expect("the inotify arms run on Linux only (CI's audits job and the Linux gate)", True)
        return
    root, repo, base = fixtures.world(*ALL_STEPS)
    out: list[str] = []
    events = root / "events.txt"
    expect.equal("a command that reads only a READ path and code is GREEN, keeps its own exit code, and leaves an "
                 "empty events file",
                 (watch(root, py + [f"open({str(root / 'kb' / 'Work' / 'PB1.md')!r}).read(); raise SystemExit(3)"],
                        events, out.append, c), events.read_text(encoding="utf-8")), (3, ""))


def case_03_command_imports_tooling_lane_module_red_naming(expect):
    if not sys.platform.startswith("linux"):
        expect("the inotify arms run on Linux only (CI's audits job and the Linux gate)", True)
        return
    root, repo, base = fixtures.world(*ALL_STEPS)
    out: list[str] = []
    tw = Classification(inert={"DEVLOG/": ""}, tooling_under=("scripts/",), code_reads={"scripts/a.py": "x"})
    repo.commit("a module", {"scripts/mod.py": "X = 1\n"})
    imp = py + [f"import sys; sys.path.insert(0, {str(root / 'scripts')!r}); import mod"]
    subprocess.run(imp, check=True)  # leaves scripts/__pycache__/mod.*.pyc: the watched run must open the source anyway
    expect.equal("a command that imports a tooling-lane module is RED naming it, though a bytecode cache of it exists; one "
                 "that reads only a CODE_READS script is GREEN (kb/Work PB4395)",
                 (watch(root, imp, None, out.append, tw) != 0, any("opened: scripts/mod.py" in s for s in out),
                  watch(root, py + [f"open({str(root / 'scripts' / 'a.py')!r}).read()"], None, out.append, tw)),
                 (True, True, 0))


def case_04_import_system_s_listing_directory_holds_code(expect):
    if not sys.platform.startswith("linux"):
        expect("the inotify arms run on Linux only (CI's audits job and the Linux gate)", True)
        return
    root, repo, base = fixtures.world(*ALL_STEPS)
    out: list[str] = []
    tm = Classification(inert={"DEVLOG/": ""}, tooling_under=("scripts/",), code_reads={"scripts/mod.py": "x"})
    repo.commit("a module", {"scripts/mod.py": "X = 1\n"})  # each case plants its own world: case 03's module is not in it
    app = py + [f"import sys; sys.path.append({str(root / 'scripts')!r}); import mod"]
    expect.equal("the import system's LISTING of a directory that holds a CODE_READS module is GREEN when the module it "
                 "imports is CODE_READS (kb/Work PB4395 review R2-1: scripts/ for scripts/standard_streams.py); a listing "
                 "of a directory that holds none is RED, naming it",
                 (watch(root, app, None, out.append, tm),
                  watch(root, py + [f"import os; os.listdir({str(root / 'scripts' / 'deep')!r})"], None, out.append, tm) != 0,
                  any("opened: scripts/deep/" in s for s in out)), (0, True, True))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
