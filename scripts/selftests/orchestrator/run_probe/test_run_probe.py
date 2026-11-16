#!/usr/bin/env python3
"""Cases of run_probe.py (kb/Work PB5764; MANDATORY-PRACTICES P17): every refusal runs nothing, each run is a NEW
directory that keeps what the earlier run wrote, and MATCH, DIFFERS and the timeout read as the docstring says.

The compiler is this Python interpreter (`--exe sys.executable`) and each "program" is a Python script with a `.cob`
name, so the tool's command `<exe> <program> --std <std> [--run] [args]` runs the script with those arguments: the
tool is exercised end to end, through a real process tree, with no build.
Run: python scripts/self_tests.py --only scripts/selftests/orchestrator/run_probe/test_run_probe.py"""
from __future__ import annotations

import contextlib
import io
import sys
from pathlib import Path

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

rp = selftest_cases.subject(__file__)   # scripts/orchestrator/run_probe.py
EXE = sys.executable

# A program that appends one line to a data file in its directory and prints how many lines it holds: 1 in a fresh
# directory, 2 in a reused one. It also prints the arguments it was given and the files beside it.
COUNTER = (
    "import os, sys\n"
    "with open('data.txt', 'a', newline='\\n') as f:\n"
    "    f.write('x\\n')\n"
    "print('lines', sum(1 for _ in open('data.txt')))\n"
    "print('args', ' '.join(sys.argv[1:]))\n"
    "print('files', ' '.join(sorted(n for n in os.listdir('.') if not n.endswith('.txt'))))\n")
HELLO = "print('HELLO')\nprint('WORLD')\n"
SLEEPER = "import time\ntime.sleep(60)\nprint('never')\n"


def _plant(d: Path, name: str, text: str) -> Path:
    path = d / name
    path.write_bytes(text.encode("utf-8"))
    return path


def _run(*argv: str) -> tuple[int, str, str]:
    """run_probe.main in this process: (exit status, standard output, standard error); an argparse refusal is its
    SystemExit status."""
    out, err = io.StringIO(), io.StringIO()
    with contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
        try:
            status = rp.main(list(argv))
        except SystemExit as e:
            status = e.code
    return status, out.getvalue(), err.getvalue()


def _run_directory(printed: str) -> Path:
    return Path(next(l for l in printed.splitlines() if l.startswith("RUN DIRECTORY: ")).split(": ", 1)[1])


def case_010_every_refusal_exits_2_and_runs_nothing(expect):
    d = scratch_dir("run-probe-refuse-")
    prog = _plant(d, "prog.cob", COUNTER)
    root = d / "runs"
    gitted = d / "tree"
    (gitted / ".git").mkdir(parents=True)     # a working tree's marker: the root below it is inside a git tree
    (d / "other").mkdir()
    twin = _plant(d / "other", "prog.cob", "")
    cases = {
        "a relative root": (["--root", "runs", "--std", "85", "--exe", EXE, str(prog)], "must be an absolute path"),
        "a root inside a git tree": (["--root", str(gitted / "runs"), "--std", "85", "--exe", EXE, str(prog)],
                                     "inside the git working tree"),
        "a compiler that does not exist": (["--root", str(root), "--std", "85", "--exe", str(d / "no.exe"), str(prog)],
                                           "no compiler at"),
        "a checkout with no build": (["--root", str(root), "--std", "85", "--tree", str(d), str(prog)],
                                     "(build the checkout first)"),
        "a configuration without --tree": (["--root", str(root), "--std", "85", "--exe", EXE, "--configuration",
                                            "Release", str(prog)], "needs --tree"),
        "a program that does not exist": (["--root", str(root), "--std", "85", "--exe", EXE, str(d / "none.cob")],
                                          "no such program"),
        "a --with file that does not exist": (["--root", str(root), "--std", "85", "--exe", EXE, "--with",
                                               str(d / "lib.cpy"), str(prog)], "no such file for --with"),
        "an --expect file that does not exist": (["--root", str(root), "--std", "85", "--exe", EXE, "--expect",
                                                  str(d / "prog.out"), str(prog)], "no such file for --expect"),
        "a --with file named like the program": (["--root", str(root), "--std", "85", "--exe", EXE, "--with",
                                                  str(twin), str(prog)], "share the name 'prog.cob'"),
        "a timeout that is not positive": (["--root", str(root), "--std", "85", "--exe", EXE, "--timeout", "0",
                                            str(prog)], "--timeout must be a positive"),
    }
    for label, (argv, says) in cases.items():
        status, printed, said = _run(*argv)
        expect(f"{label}: exit 2, the reason named, nothing printed as a run",
               status == 2 and says in said and printed == "", f"got {(status, said.strip()[:160], printed[:80])}")
        expect.equal(f"{label}: ... and nothing ran", status == 2 and not root.exists(), True)
    for label, argv in {"no --std": ["--root", str(root), "--exe", EXE, str(prog)],
                        "neither --exe nor --tree": ["--root", str(root), "--std", "85", str(prog)],
                        "both --exe and --tree": ["--root", str(root), "--std", "85", "--exe", EXE, "--tree", str(d),
                                                  str(prog)]}.items():
        expect.equal(f"{label}: refused by the argument parser (exit 2), nothing ran",
                     (_run(*argv)[0], root.exists()), (2, False))


def case_020_each_run_is_a_new_directory_and_none_is_removed(expect):
    d = scratch_dir("run-probe-fresh-")
    prog = _plant(d, "prog.cob", COUNTER)
    root = d / "runs"
    first = _run("--root", str(root), "--std", "85", "--exe", EXE, str(prog))
    second = _run("--root", str(root), "--std", "85", "--exe", EXE, str(prog))
    expect.equal("both runs exit 0", (first[0], second[0]), (0, 0))
    one, two = _run_directory(first[1]), _run_directory(second[1])
    expect.equal("the runs are prog-001 and prog-002 under the root (as the tool resolves it)", (one, two),
                 (root.resolve() / "prog-001", root.resolve() / "prog-002"))
    expect.equal("the second run did not see the first one's data file: each printed one line",
                 ("lines 1" in first[1], "lines 1" in second[1]), (True, True))
    expect.equal("the first run's directory is kept whole: its program, its data file, its captured streams",
                 sorted(p.name for p in one.iterdir()), ["data.txt", "prog.cob", "stderr.txt", "stdout.txt"])
    expect.equal("stdout.txt holds what the run printed", (one / "stdout.txt").read_bytes().decode().splitlines()[0],
                 "lines 1")
    expect("the report names the command and the exit status",
           "COMMAND: " in first[1] and "--std 85 --run" in first[1] and "EXIT: 0" in first[1], first[1])


def case_030_with_files_compile_only_and_extra_arguments(expect):
    d = scratch_dir("run-probe-args-")
    prog = _plant(d, "prog.cob", COUNTER)
    lib = _plant(d, "lib.cpy", "copied\n")
    status, printed, _ = _run("--root", str(d / "runs"), "--std", "2002", "--exe", EXE, "--with", str(lib),
                              "--compile-only", str(prog), "--", "-x", "y")
    expect.equal("exit 0", status, 0)
    expect.equal("--compile-only drops --run; the words after `--` reach the compiler; --std is passed as given",
                 next(l for l in printed.splitlines() if l.startswith("args ")), "args --std 2002 -x y")
    expect.equal("a --with file is copied in beside the program",
                 next(l for l in printed.splitlines() if l.startswith("files ")), "files lib.cpy prog.cob")


def case_040_expect_match_and_differs(expect):
    d = scratch_dir("run-probe-expect-")
    prog = _plant(d, "hello.cob", HELLO)
    same = _plant(d, "same.out", "HELLO  \r\nWORLD\r\n\r\n")    # the suite's rule: CR LF, trailing spaces and newlines aside
    other = _plant(d, "other.out", "HELLO\nEARTH\n")
    status, printed, _ = _run("--root", str(d / "runs"), "--std", "85", "--exe", EXE, "--expect", str(same), str(prog))
    expect.equal("MATCH: the program's own status, and `EXPECT: MATCH`", (status, "EXPECT: MATCH (same.out)" in printed),
                 (0, True))
    status, printed, _ = _run("--root", str(d / "runs"), "--std", "85", "--exe", EXE, "--expect", str(other), str(prog))
    expect.equal("DIFFERS: exit 1, `EXPECT: DIFFERS`, and the differing lines",
                 (status, "EXPECT: DIFFERS" in printed, "-EARTH" in printed.splitlines(), "+WORLD" in printed.splitlines()),
                 (1, True, True, True))


def case_050_a_run_past_its_timeout_is_killed_and_exits_124(expect):
    d = scratch_dir("run-probe-timeout-")
    prog = _plant(d, "slow.cob", SLEEPER)
    status, printed, _ = _run("--root", str(d / "runs"), "--std", "85", "--exe", EXE, "--timeout", "1", str(prog))
    run = _run_directory(printed)
    expect.equal("exit 124, and the timeout is written to stderr.txt and printed",
                 (status, "timed out after 1 s" in (run / "stderr.txt").read_bytes().decode(), "timed out" in printed),
                 (124, True, True))
    expect.equal("the program never reached its print", "never" in printed, False)


def case_055_the_run_has_the_suite_adapter_s_environment(expect):
    d = scratch_dir("run-probe-env-")
    prog = _plant(d, "env.cob", "import os\nfor k in ('COBOL_USER_LOCALE', 'COBOL_SYSTEM_LOCALE', 'WISEOWL_COBOL_MCS'):\n"
                                "    print(k, os.environ.get(k))\n")
    status, printed, _ = _run("--root", str(d / "runs"), "--std", "85", "--exe", EXE, str(prog))
    run = _run_directory(printed)
    adapter, _ = rp.suite_config.load_adapter(rp.ADAPTER)
    want = {k: v.replace("{work}", str(run)) for k, v in adapter.environment.items()}
    expect.equal("every variable of the adapter's environment reaches the program, {work} being the run directory",
                 (status, {k: v for k, v in (l.split(" ", 1) for l in printed.splitlines() if l.startswith(tuple(want)))}),
                 (0, want))


def case_060_a_tree_resolves_its_built_compiler(expect):
    d = scratch_dir("run-probe-tree-")
    expect.equal("--tree <checkout> names src/WiseOwl.Cobol.Cli/bin/<configuration>/net10.0/<cobol>",
                 rp.compiler_of(d, "Release"),
                 d / "src" / "WiseOwl.Cobol.Cli" / "bin" / "Release" / "net10.0" / rp.CLI_NAME)
    status, _, said = _run("--root", str(d / "runs"), "--std", "85", "--tree", str(d), "--configuration", "Release",
                           str(_plant(d, "p.cob", HELLO)))
    expect("an unbuilt configuration is refused, naming the path it looked for",
           status == 2 and str(rp.compiler_of(d, "Release")) in said, said)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
