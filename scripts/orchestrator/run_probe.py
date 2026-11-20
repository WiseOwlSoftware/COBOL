#!/usr/bin/env python3
"""run_probe.py — THE ONE WAY an agent runs a COBOL probe program: in a NEW directory, outside every git tree, deleting
nothing (kb/Work PB5764; MANDATORY-PRACTICES P17).

    python scripts/orchestrator/run_probe.py --root <absolute run root> --std <85|2002|2014|2023>
        (--exe <cobol.exe> | --tree <checkout> [--configuration Debug|Release])
        [--with <file> ...] [--expect <file.out>] [--compile-only] [--timeout 120] <program.cob> [-- <compiler args>]

--root          an absolute directory OUTSIDE every git working tree (created when absent), written LITERALLY by the
                caller (your own scratch directory, MANDATORY-PRACTICES P16). The run happens in
                <root>/<program stem>-<nnn>/, the next free number; an earlier run's directory is never reused or removed.
--std           the edition, passed to the compiler as `--std`. Required: no edition is assumed.
--exe / --tree  the compiler: its path, or the checkout whose `src/WiseOwl.Cobol.Cli/bin/<configuration>/<tfm>/`
                holds it (the configuration Debug unless named). Exactly one is required; a compiler that does not
                exist is refused, never replaced by another.
--with          a file the program needs beside it (a library text it copies, a data file it reads): copied in.
--expect        compare the program's standard output with this file by the suite runner's own rule
                (conformance-suite/harness/run.py `normalize`: CR LF is LF, each line's trailing spaces and the trailing
                newlines dropped): `EXPECT: MATCH`, or `EXPECT: DIFFERS` and the differing lines with exit 1.
--compile-only  compile without `--run` (a refusal probe): the diagnostics are the result.
--timeout       seconds before the whole process tree (compiler and program) is killed (exit 124).
The run's environment is this process's with the suite's WiseOwl adapter's `environment` over it
(conformance-suite/adapters/wiseowl.json: the USER and SYSTEM locales pinned, the message-control directory the run
directory), so a MATCH here is what the suite runner would compare.

Exit status: 2 for a refused argument (nothing is run); 124 on the timeout; 1 when --expect differs; else the
compiler's (or, with --run, the program's) own status, so a MATCH of a program that ends with a non-zero status keeps
that status: read the EXPECT line. It prints the run directory, the command, the exit status, standard output and
standard error, and keeps stdout.txt and stderr.txt in the run directory.

⛔ WHY (owner 2026-11-15, kb/Work PB5764). A probe must run where no earlier run left a file (a file the program wrote
last time changes what its OPEN or READ does now), so every agent that probed wrote its own reset, and they converged on
`rd=$R/$id; rm -rf "$rd"; mkdir -p "$rd"`: a recursive delete of a path built from variables, which is the filesystem
root when a variable is empty, and which the harness raised to the owner as a warning each time. A new numbered
directory per run needs no reset, so nothing here deletes anything. The compiler writes its outputs (`.dll`, `.g.cs`,
the runtime config) and the program its data files beside the source, so a run inside a git tree leaves them there
(47 untracked files under conformance-suite/cases on 2026-11-15): the root is refused inside one.

Its cases: scripts/selftests/orchestrator/run_probe/ (`python scripts/self_tests.py --only <case file>`).
"""
from __future__ import annotations

import argparse
import difflib
import os
import pathlib
import shutil
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SUITE = REPO / "conformance-suite"
sys.path.insert(0, str(REPO / "scripts"))  # process_job, standard_streams
sys.path.insert(0, str(SUITE / "harness"))  # the suite runner's adapter reader and its output rule
import config as suite_config  # noqa: E402
import run as suite_run  # noqa: E402
import toolchain  # noqa: E402  (THE target framework, read from Directory.Build.props: kb/Work PB5971)
from process_job import ProcessJob  # noqa: E402  (the one way to hold, and kill, a whole process tree; PB5098)
from standard_streams import ensure_utf8  # noqa: E402

#: The suite's WiseOwl adapter: its `environment` (the USER and SYSTEM locales pinned, the message-control directory in
#: the work directory) is every probe's too, so a program prints here what it prints under the suite runner.
ADAPTER = SUITE / "adapters" / "wiseowl.json"

EXIT_REFUSED = 2
EXIT_DIFFERS = 1
EXIT_TIMEOUT = 124
#: The compiler's place in a built checkout (the CLI project's output; the apphost has no `.exe` off Windows).
CLI_BIN = ("src", "WiseOwl.Cobol.Cli", "bin")
CLI_TFM = toolchain.target_framework()
CLI_NAME = "cobol.exe" if os.name == "nt" else "cobol"


def git_tree_of(path: pathlib.Path) -> pathlib.Path | None:
    """The git working tree `path` lies in (the nearest of it and its ancestors holding a `.git` directory or, for a
    linked worktree or a submodule, a `.git` file), or None."""
    for p in (path, *path.parents):
        if (p / ".git").exists():
            return p
    return None


def compiler_of(tree: pathlib.Path, configuration: str) -> pathlib.Path:
    """The compiler a built checkout `tree` holds for `configuration`."""
    return tree.joinpath(*CLI_BIN, configuration, CLI_TFM, CLI_NAME)


def fresh_directory(root: pathlib.Path, stem: str) -> pathlib.Path:
    """A NEW directory `<root>/<stem>-<nnn>`, the lowest free number: `mkdir` without `exist_ok` claims it, so two
    runs (two agents, one root) never share one and none is ever reused."""
    root.mkdir(parents=True, exist_ok=True)
    n = 1
    while True:
        run = root / f"{stem}-{n:03d}"
        try:
            run.mkdir()
            return run
        except FileExistsError:
            n += 1


def suite_environment(run: pathlib.Path) -> dict[str, str]:
    """This process's environment with the suite adapter's `environment` over it, `{work}` being the run directory.
    An adapter that does not load stops the run (SystemExit naming its problems): a probe never runs under a guessed
    environment."""
    adapter, problems = suite_config.load_adapter(ADAPTER)
    if adapter is None:
        raise SystemExit(f"run_probe: the suite adapter {ADAPTER} does not load: " + "; ".join(problems))
    return {**os.environ, **{k: suite_run.fill(v, {"work": str(run)}) for k, v in adapter.environment.items()}}


def parse(argv: list[str]) -> tuple[argparse.Namespace, list[str]]:
    """The arguments, and the compiler arguments after a `--`."""
    extra: list[str] = []
    if "--" in argv:
        cut = argv.index("--")
        argv, extra = argv[:cut], argv[cut + 1:]
    ap = argparse.ArgumentParser(prog="run_probe.py", description=__doc__.split("\n\n")[0])
    ap.add_argument("program")
    ap.add_argument("--root", required=True)
    ap.add_argument("--std", required=True)
    which = ap.add_mutually_exclusive_group(required=True)
    which.add_argument("--exe")
    which.add_argument("--tree")
    ap.add_argument("--configuration", default="Debug")
    ap.add_argument("--with", dest="beside", action="append", default=[])
    ap.add_argument("--expect")
    ap.add_argument("--compile-only", action="store_true")
    ap.add_argument("--timeout", type=int, default=120)
    return ap.parse_args(argv), extra


def refusals(a: argparse.Namespace) -> tuple[list[str], dict]:
    """Every reason the run is refused (nothing runs when there is one), and the resolved inputs."""
    problems: list[str] = []
    root = pathlib.Path(a.root)
    if not root.is_absolute():
        problems.append(f"--root must be an absolute path, not {a.root!r}")
    elif (tree := git_tree_of(root.resolve())) is not None:
        problems.append(f"--root {root} is inside the git working tree {tree}: the compiler and the program write beside "
                        "the source, so run under your own scratch directory (MANDATORY-PRACTICES P16)")
    if a.configuration != "Debug" and a.tree is None:
        problems.append("--configuration names a checkout's build, so it needs --tree")
    exe = pathlib.Path(a.exe) if a.exe is not None else compiler_of(pathlib.Path(a.tree), a.configuration)
    if not exe.is_file():
        problems.append(f"no compiler at {exe}" + (" (build the checkout first)" if a.tree is not None else ""))
    if a.timeout <= 0:
        problems.append(f"--timeout must be a positive number of seconds, not {a.timeout}")
    program = pathlib.Path(a.program).resolve()
    if not program.is_file():
        problems.append(f"no such program: {program}")
    beside = [pathlib.Path(b).resolve() for b in a.beside]
    problems += [f"no such file for --with: {b}" for b in beside if not b.is_file()]
    names = [program.name] + [b.name for b in beside]
    problems += [f"two inputs share the name {n!r}: one would overwrite the other in the run directory"
                 for n in sorted({n for n in names if names.count(n) > 1})]
    expect = pathlib.Path(a.expect).resolve() if a.expect else None
    if expect is not None and not expect.is_file():
        problems.append(f"no such file for --expect: {expect}")
    return problems, {"root": root, "exe": exe.resolve(), "program": program, "beside": beside, "expect": expect}


def main(argv: list[str]) -> int:
    a, extra = parse(argv)
    problems, got = refusals(a)
    if problems:
        print("run_probe: REFUSED: " + "; ".join(problems), file=sys.stderr)
        return EXIT_REFUSED
    program: pathlib.Path = got["program"]
    run = fresh_directory(got["root"].resolve(), program.stem)
    for f in (program, *got["beside"]):
        shutil.copy2(f, run / f.name)
    cmd = [str(got["exe"]), program.name, "--std", a.std, *([] if a.compile_only else ["--run"]), *extra]
    out_file, err_file = run / "stdout.txt", run / "stderr.txt"
    # files, never pipes, for the tree's output: a pipe an orphaned grandchild still holds blocks its reader (process_job)
    with open(out_file, "wb") as out, open(err_file, "wb") as err, \
            ProcessJob.start(cmd, cwd=run, env=suite_environment(run), stdin=subprocess.DEVNULL, stdout=out,
                             stderr=err) as job:
        try:
            status = job.wait(a.timeout)
        except subprocess.TimeoutExpired:
            job.kill()
            status = EXIT_TIMEOUT
    if status == EXIT_TIMEOUT:
        with open(err_file, "ab") as err:
            err.write(f"\nrun_probe: timed out after {a.timeout} s; the process tree was killed\n".encode())
    text_out = out_file.read_bytes().decode("utf-8", errors="replace")
    text_err = err_file.read_bytes().decode("utf-8", errors="replace")
    print(f"RUN DIRECTORY: {run}")
    print(f"COMMAND: {subprocess.list2cmdline(cmd)}")
    print(f"EXIT: {status}")
    print("--- stdout ---")
    print(text_out, end="" if text_out.endswith("\n") or not text_out else "\n")
    if text_err.strip():
        print("--- stderr ---")
        print(text_err.rstrip())
    expect: pathlib.Path | None = got["expect"]
    if expect is None:
        return status
    want = suite_run.normalize(expect.read_bytes().decode("utf-8", errors="replace"))
    printed = suite_run.normalize(text_out)
    if printed == want:
        print(f"EXPECT: MATCH ({expect.name})")
        return status
    print(f"EXPECT: DIFFERS from {expect}")
    for line in difflib.unified_diff(want.splitlines(), printed.splitlines(), "expected", "printed", lineterm=""):
        print(line)
    return EXIT_DIFFERS


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
