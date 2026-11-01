#!/usr/bin/env python3
"""emit_fraction.py -- HOW MANY PROGRAMS DOES A NON-RUNTIME LANDING CHANGE? THE MEASURE STEP 10 OF PB4323 IS BUILT ON
(kb/Work PB4333; PIPELINE-REVIEW.md section 3.4; docs/rearchitecture/DESIGN-test-build-ci.md section 3.14.13).

    python scripts/arch/emit_fraction.py [--landings 20] [--rev origin/main]
    python scripts/arch/emit_fraction.py --self-test

The emitted-program differential runs only the Conformance programs whose emitted bytes differ from main's, so what it
saves is decided by one number: the fraction of a landing's compiled programs whose emitted C# (or compile-time
diagnostics) differs from those of the landing's first parent. The trigger (scripts/gate_growth.py) says WHEN to
measure; the design builds the differential only if the MEDIAN fraction over the newest 20 non-runtime landings is under
BUILD_IF_UNDER = 10 %. A "non-runtime landing" is a first-parent merge of `--rev` whose change touches the compiler
(`src/`) and not `src/WiseOwl.Cobol.Runtime`: a runtime change runs everything (the program bytes are not the whole
input), and a change with no `src/` file leaves every program's bytes alone by construction.

THE MEASURE is the architecture oracle's, never a second capture mechanism (scripts/arch/capture_oracle.py, through
compare_oracle.capture_commit: a detached worktree of each commit, its own host, kept under TestResults/arch-oracle/
<commit12>/ so a rerun reuses it). Its manifest holds, per compile the suites perform, the SHA-256 of the emitted C# and of
the diagnostic stream. The emitted ASSEMBLY is a deterministic function of that C# under one Roslyn and one runtime
reference, so identical C# is identical bytes and a different C# is the (rare) over-count of a program the differential
would run anyway. For one landing:

  programs   the cases of the landing's manifest that emit C# (a refusal emits none and runs anyway);
  changed    of those, the cases whose C# or diagnostics hash differs from the first parent's, and the cases the parent's
             manifest lacks (a golden the landing adds is new, so it runs);
  fraction   changed / programs.

Each landing costs two captures (about 3 minutes each on a quiet box); a captured commit is reused. Exit 0 when the median
is under the limit, 1 at or over it, 2 when a capture could not be made (never a verdict).
"""
from __future__ import annotations

import argparse
import statistics
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import capture_oracle  # noqa: E402  (the one capture mechanism)
import compare_oracle  # noqa: E402
sys.path.append(str(Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

BUILD_IF_UNDER = 0.10
DEFAULT_LANDINGS = 20
RUNTIME = "src/WiseOwl.Cobol.Runtime"
VERDICT_PREFIX = "=== EMIT FRACTION: "


@dataclass(frozen=True)
class Landing:
    commit: str
    parent: str
    programs: int
    changed: int

    @property
    def fraction(self) -> float:
        return self.changed / self.programs if self.programs else 0.0


def non_runtime(files: list[str]) -> bool:
    """A landing the differential applies to: it changes the compiler and not the runtime."""
    return (any(f.startswith("src/") and not f.startswith(RUNTIME) for f in files)
            and not any(f.startswith(RUNTIME) for f in files))


def changed_programs(old: dict, new: dict) -> tuple[int, int]:
    """(programs, changed) of the landing whose manifest `cases` are `new`, against its parent's `old`."""
    programs = changed = 0
    for case_id, (csharp, diagnostics) in new.items():
        if csharp is None:
            continue
        programs += 1
        if tuple(old.get(case_id, ())) != (csharp, diagnostics):
            changed += 1
    return programs, changed


def verdict(landings: list[Landing]) -> tuple[str, int]:
    if not landings:
        return f"{VERDICT_PREFIX}NOT MEASURED -- no non-runtime landing in range ===", 2
    median = statistics.median(landing.fraction for landing in landings)
    worst = max(landing.fraction for landing in landings)
    facts = f"median {median:.2%} (max {worst:.2%}) of the programs changed over {len(landings)} non-runtime landings"
    if median < BUILD_IF_UNDER:
        return f"{VERDICT_PREFIX}UNDER {BUILD_IF_UNDER:.0%} -- {facts}: the fraction criterion for building the differential is met (how often it applies is decided by the harness-change rule, DESIGN-test-build-ci.md section 3.14.13) ===", 0
    return f"{VERDICT_PREFIX}OVER {BUILD_IF_UNDER:.0%} -- {facts}: the differential would run most programs; do not build ===", 1


def git(*args: str) -> str:
    return subprocess.run(["git", "-C", str(capture_oracle.REPO), *args], check=True, capture_output=True,
                          text=True, encoding="utf-8").stdout


def select(rev: str, count: int) -> list[tuple[str, str]]:
    """The newest `count` non-runtime landings of `rev`: (commit, first parent), newest first."""
    chosen: list[tuple[str, str]] = []
    for commit in git("log", "--first-parent", "--merges", "--format=%H", rev).split():
        files = git("diff", "--name-only", f"{commit}^1", commit).splitlines()
        if non_runtime(files):
            chosen.append((commit, git("rev-parse", f"{commit}^1").strip()))
            if len(chosen) == count:
                break
    return chosen


def measure(pairs: list[tuple[str, str]], capture=compare_oracle.capture_commit) -> list[Landing]:
    landings = []
    for commit, parent in pairs:
        old, new = (compare_oracle.load(capture(c))["cases"] for c in (parent, commit))
        programs, changed = changed_programs(old, new)
        print(f"  {commit[:9]}: {changed} of {programs} programs changed ({changed / programs if programs else 0:.2%})",
              flush=True)
        landings.append(Landing(commit, parent, programs, changed))
    return landings


def self_test() -> int:
    failures: list[str] = []

    def expect(what: str, got, want) -> None:
        if got != want:
            failures.append(f"{what}: got {got!r}, wanted {want!r}")

    expect("a compiler change is a non-runtime landing", non_runtime(["src/WiseOwl.Cobol.Binding/A.cs", "docs/x.md"]), True)
    expect("a runtime change is not", non_runtime(["src/WiseOwl.Cobol.Runtime/IO/A.cs", "src/WiseOwl.Cobol.Binding/A.cs"]), False)
    expect("a change with no src file is not (it changes no program)", non_runtime(["tests/A.cs", "kb/Work/PB1.md"]), False)
    expect("a prefix is a directory: src/WiseOwl.Cobol.RuntimeX is the compiler's", non_runtime(["src/WiseOwl.Cobol.RuntimeX/A.cs"]), False)

    old = {"a": ["c1", "d1"], "b": ["c2", "d2"], "c": ["c3", "d3"], "r": [None, "dr"], "gone": ["c9", "d9"]}
    new = {"a": ["c1", "d1"], "b": ["c2x", "d2"], "c": ["c3", "d3x"], "r": [None, "drx"], "added": ["c7", "d7"],
           "added-refusal": [None, "d8"]}
    expect("changed C#, changed diagnostics and an added program count; an unchanged one, a refusal and a removed case do not",
           changed_programs(old, new), (4, 3))
    expect("an empty parent manifest makes every program new", changed_programs({}, new), (4, 4))

    def land(programs: int, changed: int) -> Landing:
        return Landing("c" * 40, "p" * 40, programs, changed)

    line, code = verdict([land(1000, 0), land(1000, 5), land(1000, 900)])
    expect("a median of 0.5 % is under the limit, exit 0", (code, "UNDER 10%" in line, "median 0.50%" in line, "max 90.00%" in line),
           (0, True, True, True))
    line, code = verdict([land(100, 10), land(100, 10)])
    expect("a median of exactly 10 % is OVER (the criterion is 'under'), exit 1", (code, "OVER 10%" in line), (1, True))
    expect("no landing is NOT MEASURED, exit 2", verdict([])[1], 2)
    expect("a landing with no programs has fraction 0, not a division", land(0, 0).fraction, 0.0)

    seen: list[str] = []
    manifests = {"p1": {"cases": {"a": ["c1", "d1"]}}, "m1": {"cases": {"a": ["c1x", "d1"], "b": ["c2", "d2"]}}}

    def fake_capture(commit: str) -> Path:
        seen.append(commit)
        return Path(commit)

    real_load = compare_oracle.load
    compare_oracle.load = lambda path: manifests[str(path)]   # type: ignore[assignment]
    try:
        measured = measure([("m1", "p1")], capture=fake_capture)
    finally:
        compare_oracle.load = real_load
    expect("measure captures the parent and the landing and counts a changed and an added program",
           (seen, [(m.programs, m.changed) for m in measured]), (["p1", "m1"], [(2, 2)]))

    if failures:
        print("emit_fraction self-test FAILED:\n  " + "\n  ".join(failures))
        return 1
    print("emit_fraction self-test OK")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--landings", type=int, default=DEFAULT_LANDINGS, help="how many non-runtime landings to measure")
    parser.add_argument("--rev", default=compare_oracle.MAIN, help="the history to take them from (default: origin/main)")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    try:
        landings = measure(select(args.rev, args.landings))
    except (SystemExit, subprocess.CalledProcessError) as e:
        if isinstance(e, SystemExit) and e.code in (0, 2, None):
            raise
        print(f"emit_fraction: a capture could not be made ({e}); NOT MEASURED", file=sys.stderr)
        return 2
    line, code = verdict(landings)
    print(line)
    return code


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
