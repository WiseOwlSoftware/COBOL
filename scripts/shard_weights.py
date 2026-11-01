#!/usr/bin/env python3
"""shard_weights.py -- HOW EVENLY THE MERGE GROUP'S SHARDS DIVIDE THE TEST WORK (kb/Work PB4332, step 9 of PB4323; the
review's R7: "the weights file goes stale ... the nightly report at > 1.5x spread is the regeneration trigger").

    python scripts/shard_weights.py report                      # the newest merge-group runs of the last 24 hours, from GitHub
    python scripts/shard_weights.py report --from-dir DIR       # trx already downloaded: DIR/<run id>/<artifact>/*.trx
    python scripts/shard_weights.py --self-test

THE MEASURE. Every test job of build-and-test.yml uploads its trx as the artifact `test-results-<OS>-<job>`; that
artifact is one SHARD. A shard's wall time is the sum, over its trx files, of the test run's own `<Times start= finish=>`
(the `dotnet test` step's wall clock, not the sum of its tests: shards run their tests in parallel). The SPREAD of one
OS's shards in one run is the slowest shard's wall over the mean shard wall: 1.0 is a perfect division, and the merge
group's wall time is the slowest shard's. The day's verdict is the WORST spread of any OS in any run of the window:

    === SHARD SPREAD: 1.31x WITHIN (<= 1.50x) (run 123, Linux: slowest test-results-Linux-unit 410 s, mean 313 s over 4 shards; 3 run(s) in the last 24 h) ===

A spread above THRESHOLD is OVER: the division no longer matches the cost of the work (a class grew, a class was added)
and the weights file is regenerated; this command exits 1 so the nightly run is red, and it files nothing itself.

WHEN THE REPORT IS LIVE. OVER names the weights file as the remedy, so the report measures only while the merge group
shards by that file: `tests/_shared/test-weights.json` exists AND build-and-test.yml names `WISEOWL_COBOL_SHARD` (the
weighted-shard matrix, PB4328 and PB4329). Before then the shards are the name split's, whose spread no regeneration can
change, so the live report is `NOT MEASURED` (exit 0) rather than a daily red nobody can act on. `--from-dir` always
measures: it is the tool for reading downloaded trx by hand.

THE COST. A run's `test-results-*` artifacts are about 200 MB unpacked (the trx are most of it) and a day holds dozens of
runs, so `report` never holds the window at once: it downloads ONE run at a time into a scratch directory, reads that
run's shard walls and deletes the directory before the next, and it reads at most MAX_RUNS shard runs, the newest of the
window. The verdict line says when the cap applied.

WHAT IS MEASURED, AND WHAT IS NOT. Only a merge-group run whose conclusion is `success` is read: a red run's shards were
cut short or retried, so their times are not the work's. A run with no `test-results-*` artifact (an inert change skips
the matrix) is not a shard run and is not counted. A window with no shard run is `NOT MEASURED` and exits 0: nothing
landed, so nothing divided. A run whose artifacts cannot be read or downloaded is `UNMEASURED` and exits 2: a report that
cannot see is never a green. Artifacts live 14 days (.github/actions/upload-results), so `--hours` beyond 336 reads nothing.

THE WEIGHTS FILE (`generate`; kb/Work PB4329). `tests/_shared/test-weights.json` gives every test class the seconds it
costs a merge-group run: per (run, OS), the sum over every trx of that run's artifacts of the durations of the class's
results (INV-1's re-run of the NIST classes on Linux included: it runs in the same shard as its class), and per class the
MEDIAN of those observations. A class's assembly is its trx definition's `codeBase`, keyed as the test framework keys it
(`WiseOwl.Cobol.Tests.<key>.dll` -> `<key>`). `default_seconds`, the median class weight, is the weight of a class the
file does not name (one added since). GateTestFramework (tests/_shared/GateLegs.cs, `GateShard`) reads the file, embedded
in every test assembly, and deals the classes to the shards of `WISEOWL_COBOL_SHARD=k/N` longest first; a stale weight
costs balance, never coverage, because every discovered class gets exactly one shard and `ci-gate` checks the names
(test_population.py check). The file changes only when someone regenerates it, which the nightly's OVER asks for:

    python scripts/shard_weights.py generate                    # the newest GENERATE_RUNS successful merge-group runs
    python scripts/shard_weights.py generate --from-dir DIR     # trx already downloaded: DIR/<run id>/<artifact>/*.trx
"""
from __future__ import annotations

import argparse
import datetime
import json
import re
import statistics
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Callable, TypeVar

from standard_streams import ensure_utf8  # (scripts/, this file's own directory)
from test_population import UnreadableTrx, trx_root  # THE one trx reader, which names a file it cannot read (kb/Work PB3661)

#: The spread above which the division is OVER (PIPELINE-REVIEW section 3.1: "above 1.5 times is a kb/Work note").
THRESHOLD = 1.5

#: The artifact prefix every test job's results carry: `test-results-<OS>-<job>`.
ARTIFACT_PREFIX = "test-results-"

#: The window the nightly reads, and the workflow and event whose runs are the shards.
DEFAULT_HOURS = 24
#: The most shard runs one report reads (the newest of the window): each costs a download of about 200 MB and a parse of
#: its trx, so the nightly's 15 minutes bound it, not the day's landing count.
MAX_RUNS = 8
WORKFLOW = "build-and-test.yml"
EVENT = "merge_group"

#: The namespace of a Visual Studio test result (trx) file.
_TRX_NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"

#: The repository root (this file is scripts/shard_weights.py), the weights file and the workflow whose matrix consumes it.
REPO_ROOT = Path(__file__).resolve().parent.parent
WEIGHTS_FILE = "tests/_shared/test-weights.json"
WORKFLOW_FILE = ".github/workflows/" + WORKFLOW
SHARD_ENV = "WISEOWL_COBOL_SHARD"

#: The weights file's schema (GateLegs.cs `TestWeights.Schema` reads exactly this one).
WEIGHTS_SCHEMA = 1
#: How many successful merge-group runs `generate` reads (the newest that ran the matrix), and the window it looks in:
#: the artifacts' whole life (.github/actions/upload-results keeps them 14 days).
GENERATE_RUNS = 3
GENERATE_HOURS = 336

#: The test assemblies are `WiseOwl.Cobol.Tests.<key>.dll`; the weights file and the shard hook name each by its key
#: (GateLegs.cs `GatePlan.AssemblyKey` strips the same prefix).
TEST_ASSEMBLY = re.compile(r"(?:^|[/\\])WiseOwl\.Cobol\.Tests\.(?P<key>[^/\\]+)\.dll$", re.IGNORECASE)
#: A trx duration, `[d.]hh:mm:ss[.fffffff]`.
_DURATION = re.compile(r"^(?:(?P<d>\d+)\.)?(?P<h>\d+):(?P<m>\d+):(?P<s>\d+(?:\.\d+)?)$")

#: A `gh` call: arguments in, standard output out; a non-zero exit raises `GhError`.
Gh = Callable[[list[str]], str]
#: What one downloaded run reduces to (`shard_walls` for the report, `run_class_seconds` for the weights).
T = TypeVar("T")


class GhError(RuntimeError):
    """`gh` failed: the shards could not be listed or downloaded, so the day is UNMEASURED."""


def trx_times(trx: Path) -> float:
    """The wall seconds of one trx's test run: its `<Times>` finish minus start. Raises UnreadableTrx (naming the file)
    for a file that is not a trx, and ValueError for a trx with no `<Times>`: a shard with an unreadable trx is
    UNMEASURED, never zero."""
    root = trx_root(trx)
    times = root.find(f"{_TRX_NS}Times")
    if times is None or not times.get("start") or not times.get("finish"):
        raise ValueError(f"{trx}: no <Times start= finish=> element")
    start, finish = (datetime.datetime.fromisoformat(times.get(k)) for k in ("start", "finish"))
    return (finish - start).total_seconds()


def shard_walls(run_dir: Path) -> dict[str, float]:
    """One run's shards: artifact directory name -> the sum of its trx files' wall seconds. A `test-results-*`
    directory holding no trx (an upload that carried only logs) is left out, since it has no time to divide."""
    walls: dict[str, float] = {}
    for artifact in sorted(p for p in run_dir.iterdir() if p.is_dir() and p.name.startswith(ARTIFACT_PREFIX)):
        trxs = sorted(artifact.rglob("*.trx"))
        if trxs:
            walls[artifact.name] = sum(trx_times(t) for t in trxs)
    return walls


def os_of(shard: str) -> str:
    """The OS of an artifact name `test-results-<OS>-<job>`."""
    return shard.removeprefix(ARTIFACT_PREFIX).split("-", 1)[0]


def spreads(walls: dict[str, float]) -> list[tuple[float, str, str, float, int]]:
    """Per OS with at least two shards: (slowest wall / mean wall, OS, slowest shard, mean wall, shard count). One
    shard cannot be unevenly divided, so an OS with one has no spread."""
    by_os: dict[str, dict[str, float]] = {}
    for shard, wall in walls.items():
        by_os.setdefault(os_of(shard), {})[shard] = wall
    found = []
    for os_name, shards in sorted(by_os.items()):
        if len(shards) < 2:
            continue
        mean = sum(shards.values()) / len(shards)
        slowest = max(shards, key=shards.__getitem__)
        found.append((shards[slowest] / mean if mean > 0 else 1.0, os_name, slowest, mean, len(shards)))
    return found


def verdict(runs: dict[str, dict[str, float]], hours: int, threshold: float = THRESHOLD,
            cap: int | None = None) -> tuple[str, int]:
    """The verdict line and exit code for `runs` (run id -> shard walls). The worst spread of any OS in any run
    decides; a run with no OS of two shards or more contributes nothing. `cap` is MAX_RUNS when it limited the read:
    the line then says the newest `cap` runs were read, not the whole window."""
    window = (f"in the last {hours} h" if cap is None or len(runs) < cap
              else f"(the newest {cap} of the last {hours} h)")
    candidates = [(s, rid, rest) for rid, walls in runs.items() for s, *rest in spreads(walls)]
    if not candidates:
        return (f"=== SHARD SPREAD: NOT MEASURED -- no merge-group run with two or more shards of one OS {window} "
                "(an inert day divides nothing) ===", 0)
    worst, run_id, (os_name, slowest, mean, count) = max(candidates, key=lambda c: c[0])
    state = "OVER" if worst > threshold else "WITHIN"
    cmp = ">" if worst > threshold else "<="
    return (f"=== SHARD SPREAD: {worst:.2f}x {state} ({cmp} {threshold:.2f}x) (run {run_id}, {os_name}: slowest {slowest} "
            f"{runs[run_id][slowest]:.0f} s, mean {mean:.0f} s over {count} shards; {len(runs)} run(s) {window}) ===",
            1 if worst > threshold else 0)


def _gh(args: list[str]) -> str:
    try:
        done = subprocess.run(["gh", *args], capture_output=True, text=True, encoding="utf-8", check=False)
    except OSError as error:
        raise GhError(f"gh could not start: {error}") from error
    if done.returncode != 0:
        raise GhError(f"gh {' '.join(args)} exited {done.returncode}: {done.stderr.strip()}")
    return done.stdout


def window_run_ids(listing: str, now: datetime.datetime, hours: int) -> list[str]:
    """The ids of the successful runs created within `hours` of `now`, from `gh run list --json databaseId,createdAt,conclusion`."""
    since = now - datetime.timedelta(hours=hours)
    ids = []
    for run in json.loads(listing):
        created = datetime.datetime.fromisoformat(run["createdAt"].replace("Z", "+00:00"))
        if run.get("conclusion") == "success" and created >= since:
            ids.append(str(run["databaseId"]))
    return ids


def measure_window(hours: int, max_runs: int = MAX_RUNS, gh: Gh = _gh, now: datetime.datetime | None = None,
                   read: Callable[[Path], T] = shard_walls) -> dict[str, T]:
    """`read` (the shard walls, by default) of the window's newest `max_runs` successful merge-group runs that ran the
    matrix: run id -> what `read` made of the run's download. Each run is downloaded into a scratch directory of its own
    that is deleted before the next, so disk holds one run at a time. A run whose download fails raises `GhError` (the
    day is then UNMEASURED); a run with no `test-results-*` artifact (an inert change) is not a shard run and does not
    count toward `max_runs`."""
    listing = gh(["run", "list", "--workflow", WORKFLOW, "--event", EVENT, "--status", "success",
                  "--limit", "100", "--json", "databaseId,createdAt,conclusion"])
    runs: dict[str, T] = {}
    for run_id in window_run_ids(listing, now or datetime.datetime.now(datetime.timezone.utc), hours):
        if len(runs) >= max_runs:
            break
        with tempfile.TemporaryDirectory(prefix="shard-weights-") as scratch:
            try:
                gh(["run", "download", run_id, "--pattern", ARTIFACT_PREFIX + "*", "--dir", scratch])
            except GhError as error:
                # An inert run uploads nothing: `gh run download` then exits non-zero "no artifact matches". That is
                # not a failure of the day; any other reason is.
                if "no valid artifacts" not in str(error) and "no artifact" not in str(error).lower():
                    raise
            found = read(Path(scratch))
        if found:
            runs[run_id] = found
    return runs


def gather(root: Path) -> dict[str, dict[str, float]]:
    """`root/<run id>/<artifact>/*.trx` -> run id -> shard walls, runs with no shard left out."""
    runs = {}
    for run_dir in sorted(p for p in root.iterdir() if p.is_dir()):
        walls = shard_walls(run_dir)
        if walls:
            runs[run_dir.name] = walls
    return runs


def duration_seconds(text: str, source: Path) -> float:
    """A trx `duration` attribute in seconds; ValueError naming the file for one that is not `[d.]hh:mm:ss[.f]`."""
    m = _DURATION.match(text or "")
    if m is None:
        raise ValueError(f"{source}: a result duration {text!r} is not [d.]hh:mm:ss[.fffffff]")
    return ((int(m["d"] or 0) * 24 + int(m["h"])) * 60 + int(m["m"])) * 60 + float(m["s"])


def class_seconds(trx: Path) -> dict[tuple[str, str], float]:
    """(assembly key, test class) -> the summed durations of the class's results in one trx. A definition whose
    `codeBase` is not a test assembly, or that names no class, and a result with no definition, are a ValueError naming
    the file: a weight the shard hook cannot key is a weight nothing would ever use."""
    root = trx_root(trx)
    owner: dict[str, tuple[str, str]] = {}
    for unit in root.iter(f"{_TRX_NS}UnitTest"):
        method = unit.find(f"{_TRX_NS}TestMethod")
        code_base = method.get("codeBase", "") if method is not None else ""
        cls = method.get("className", "") if method is not None else ""
        m = TEST_ASSEMBLY.search(code_base)
        if m is None or not cls:
            raise ValueError(f"{trx}: test {unit.get('name')!r} names no test assembly and class "
                             f"(codeBase {code_base!r}, className {cls!r})")
        owner[unit.get("id", "")] = (m["key"], cls)
    seconds: dict[tuple[str, str], float] = {}
    for result in root.iter(f"{_TRX_NS}UnitTestResult"):
        key = owner.get(result.get("testId", ""))
        if key is None:
            raise ValueError(f"{trx}: the result {result.get('testName')!r} has no test definition")
        seconds[key] = seconds.get(key, 0.0) + duration_seconds(result.get("duration", ""), trx)
    return seconds


def run_class_seconds(run_dir: Path) -> dict[str, dict[tuple[str, str], float]]:
    """One run: OS -> (assembly, class) -> the class's seconds over every trx of that OS's `test-results-*` artifacts.
    Each OS of a run is one observation of every class it ran; a run with no trx is empty."""
    by_os: dict[str, dict[tuple[str, str], float]] = {}
    for artifact in sorted(p for p in run_dir.iterdir() if p.is_dir() and p.name.startswith(ARTIFACT_PREFIX)):
        for trx in sorted(artifact.rglob("*.trx")):
            seconds = by_os.setdefault(os_of(artifact.name), {})
            for key, value in class_seconds(trx).items():
                seconds[key] = seconds.get(key, 0.0) + value
    return by_os


def weights(observations: list[dict[tuple[str, str], float]]) -> dict[tuple[str, str], float]:
    """Each class's MEDIAN over the observations that ran it, rounded to a tenth of a second."""
    seen: dict[tuple[str, str], list[float]] = {}
    for observation in observations:
        for key, value in observation.items():
            seen.setdefault(key, []).append(value)
    return {key: round(statistics.median(values), 1) for key, values in seen.items()}


def render_weights(classes: dict[tuple[str, str], float], source: str) -> str:
    """The weights file's text: sorted, one class a line, LF, so a regeneration's diff is the classes that moved."""
    if not classes:
        raise ValueError("no class weight was read: a weights file over nothing would put every class at the default")
    default = max(0.1, round(statistics.median(classes.values()), 1))
    assemblies: dict[str, dict[str, float]] = {}
    for (assembly, cls), value in sorted(classes.items()):
        assemblies.setdefault(assembly, {})[cls] = value
    blocks = []
    for assembly, members in assemblies.items():
        rows = ",\n".join(f"      {json.dumps(cls)}: {value}" for cls, value in members.items())
        blocks.append(f"    {json.dumps(assembly)}: {{\n{rows}\n    }}")
    return (f'{{\n  "schema": {WEIGHTS_SCHEMA},\n  "source": {json.dumps(source)},\n  "default_seconds": {default},\n'
            f'  "assemblies": {{\n' + ",\n".join(blocks) + "\n  }\n}\n")


def generate(from_dir: Path | None, out: Path, runs: int = GENERATE_RUNS, gh: Gh = _gh) -> str:
    """Write the weights file from `from_dir`'s runs (DIR/<run id>/<artifact>/*.trx) or the newest `runs` successful
    merge-group runs; return the line that says what it read. Raises GhError, ValueError, UnreadableTrx or OSError
    when it cannot read them, and writes nothing then."""
    if from_dir is not None:
        read = {p.name: run_class_seconds(p) for p in sorted(q for q in from_dir.iterdir() if q.is_dir())}
    else:
        read = measure_window(GENERATE_HOURS, runs, gh, read=run_class_seconds)
    read = {run_id: by_os for run_id, by_os in read.items() if by_os}
    if not read:
        raise ValueError("no merge-group run with test results was read")
    observations = [seconds for by_os in read.values() for seconds in by_os.values()]
    classes = weights(observations)
    source = (f"scripts/shard_weights.py generate: the median over {len(observations)} observation(s) (a run on one OS) "
              f"of merge-group run(s) {', '.join(sorted(read))}")
    text = render_weights(classes, source)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(text, encoding="utf-8", newline="\n")
    shown = out.resolve()
    shown = shown.relative_to(REPO_ROOT) if shown.is_relative_to(REPO_ROOT) else shown
    return f"=== WEIGHTS: {len(classes)} class(es) from {len(observations)} observation(s) -> {shown.as_posix()} ==="


def weighted_shards_in_force(repo: Path = REPO_ROOT) -> bool:
    """Does the merge group shard by the weights file? True when the weights file exists and build-and-test.yml names the
    shard variable its test framework reads (PB4328 builds the file and the hook, PB4329 the matrix)."""
    workflow = repo / WORKFLOW_FILE
    return (repo / WEIGHTS_FILE).is_file() and workflow.is_file() and SHARD_ENV in workflow.read_text(encoding="utf-8")


def report(from_dir: Path | None, hours: int, threshold: float, max_runs: int = MAX_RUNS, gh: Gh = _gh,
           repo: Path = REPO_ROOT, now: datetime.datetime | None = None) -> tuple[str, int]:
    """The verdict line and exit code of the `report` command. `now` is the window's end (the wall clock when None); a
    caller with planted runs passes the clock they were dated against, or the window leaves them behind (kb/Work PB4353)."""
    if from_dir is None and not weighted_shards_in_force(repo):
        return (f"=== SHARD SPREAD: NOT MEASURED -- the merge group does not shard by {WEIGHTS_FILE} yet (PB4328, PB4329): "
                "its spread is the name split's, which no regeneration changes ===", 0)
    try:
        if from_dir is not None:
            return verdict(gather(from_dir), hours, threshold)
        return verdict(measure_window(hours, max_runs, gh, now), hours, threshold, max_runs)
    except (GhError, ValueError, UnreadableTrx, OSError) as error:
        return f"=== SHARD SPREAD: UNMEASURED -- {error} ===", 2


# -- the self-test -----------------------------------------------------------------------------------------------------

_TRX = ('<?xml version="1.0" encoding="UTF-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
        '<Times creation="{start}" queuing="{start}" start="{start}" finish="{finish}" /></TestRun>')


def _write_trx(path: Path, seconds: int) -> None:
    """A trx whose run took `seconds`, its times spelled as .NET writes them (seven fractional digits, an offset)."""
    path.parent.mkdir(parents=True, exist_ok=True)
    start = datetime.datetime(2026, 10, 31, 4, 0, 0, tzinfo=datetime.timezone.utc)
    finish = start + datetime.timedelta(seconds=seconds)
    fmt = lambda t: t.strftime("%Y-%m-%dT%H:%M:%S") + ".1234567+00:00"  # noqa: E731
    path.write_text(_TRX.format(start=fmt(start), finish=fmt(finish)), encoding="utf-8", newline="\n")


def _write_class_trx(path: Path, results: list[tuple[str, str, str]]) -> None:
    """A trx of `results` (code base, class, duration), one test definition and one result each, as the trx logger writes
    them: `<UnitTest><TestMethod codeBase= className=>` under `<TestDefinitions>`, `<UnitTestResult duration=>`."""
    path.parent.mkdir(parents=True, exist_ok=True)
    units = "".join(f'<UnitTest name="{cls}.M{i}" id="id{i}"><TestMethod codeBase="{code_base}" className="{cls}" '
                    f'name="M{i}" /></UnitTest>' for i, (code_base, cls, _) in enumerate(results))
    rows = "".join(f'<UnitTestResult testId="id{i}" testName="{cls}.M{i}" duration="{duration}" outcome="Passed" />'
                   for i, (_, cls, duration) in enumerate(results))
    path.write_text('<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/'
                    f'TeamTest/2010"><Results>{rows}</Results><TestDefinitions>{units}</TestDefinitions></TestRun>',
                    encoding="utf-8", newline="\n")


def self_test() -> int:
    failures: list[str] = []

    def check(label: str, got, want) -> None:
        if got != want:
            failures.append(f"{label}: got {got!r}, want {want!r}")

    with tempfile.TemporaryDirectory(prefix="shard-weights-selftest-") as scratch:
        root = Path(scratch) / "runs"
        # Run 1: Linux 100/100/400 (mean 200, spread 2.0), Windows 150/150 (1.0). Two trx in one artifact are summed.
        _write_trx(root / "1" / "test-results-Linux-a" / "x.trx", 100)
        _write_trx(root / "1" / "test-results-Linux-b" / "x.trx", 40)
        _write_trx(root / "1" / "test-results-Linux-b" / "nested" / "y.trx", 60)
        _write_trx(root / "1" / "test-results-Linux-c" / "x.trx", 400)
        _write_trx(root / "1" / "test-results-Windows-a" / "x.trx", 150)
        _write_trx(root / "1" / "test-results-Windows-b" / "x.trx", 150)
        (root / "1" / "test-results-Windows-logs").mkdir()          # an artifact with no trx has no time to divide
        (root / "1" / "unrelated-artifact").mkdir()                 # not a shard
        (root / "2").mkdir()                                        # an inert run: no artifact at all
        _write_trx(root / "3" / "test-results-Linux-a" / "x.trx", 100)
        _write_trx(root / "3" / "test-results-Linux-b" / "x.trx", 110)

        check("trx_times reads .NET's seven-digit fractions", trx_times(root / "1" / "test-results-Linux-a" / "x.trx"), 100.0)
        check("shard_walls", shard_walls(root / "1"),
              {"test-results-Linux-a": 100.0, "test-results-Linux-b": 100.0, "test-results-Linux-c": 400.0,
               "test-results-Windows-a": 150.0, "test-results-Windows-b": 150.0})
        runs = gather(root)
        check("gather leaves out the inert run", sorted(runs), ["1", "3"])
        check("os_of", os_of("test-results-Windows-conformance-rest"), "Windows")

        line, code = verdict(runs, 24)
        check("worst spread over 1.5 is OVER and exits 1", code, 1)
        check("the verdict names the run, the OS and the slowest shard",
              ("2.00x OVER" in line, "run 1, Linux: slowest test-results-Linux-c 400 s, mean 200 s over 3 shards" in line, "2 run(s)" in line),
              (True, True, True))
        line, code = verdict({"3": runs["3"]}, 24)
        check("a balanced run is WITHIN and exits 0", (code, "WITHIN" in line, "1.05x" in line), (0, True, True))
        line, code = verdict({"1": runs["1"]}, 24, threshold=2.0)
        check("exactly the threshold is WITHIN, not OVER", (code, "WITHIN" in line), (0, True))
        line, code = verdict({}, 24)
        check("no shard run is NOT MEASURED and exits 0", (code, "NOT MEASURED" in line), (0, True))
        line, code = verdict({"9": {"test-results-Linux-only": 50.0}}, 24)
        check("one shard of an OS has no spread", (code, "NOT MEASURED" in line), (0, True))

        # report --from-dir, and the failure arms: an unreadable trx and a missing directory are UNMEASURED, exit 2.
        check("report --from-dir", report(root, 24, THRESHOLD)[1], 1)
        (root / "4" / "test-results-Linux-bad").mkdir(parents=True)
        bad = root / "4" / "test-results-Linux-bad" / "x.trx"
        bad.write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"/>', encoding="utf-8", newline="\n")
        line, code = report(root, 24, THRESHOLD)
        check("a trx without <Times> is UNMEASURED, never zero", (code, "UNMEASURED" in line, "<Times" in line), (2, True, True))
        bad.write_text("<torn", encoding="utf-8", newline="\n")
        line, code = report(root, 24, THRESHOLD)
        check("a torn trx is UNMEASURED and the line names the file", (code, "UNMEASURED" in line, "x.trx" in line), (2, True, True))
        check("a missing directory is UNMEASURED", report(Path(scratch) / "absent", 24, THRESHOLD)[1], 2)

        # measure_window / window_run_ids with a fake gh.
        now = datetime.datetime(2026, 10, 31, 12, 0, tzinfo=datetime.timezone.utc)
        listing = json.dumps([
            {"databaseId": 11, "createdAt": "2026-10-31T09:00:00Z", "conclusion": "success"},
            {"databaseId": 12, "createdAt": "2026-10-31T10:00:00Z", "conclusion": "failure"},
            {"databaseId": 13, "createdAt": "2026-10-29T10:00:00Z", "conclusion": "success"},
            {"databaseId": 14, "createdAt": "2026-10-31T08:00:00Z", "conclusion": "success"},
            {"databaseId": 15, "createdAt": "2026-10-31T07:00:00Z", "conclusion": "success"},
        ])
        check("window_run_ids keeps successful runs inside the window", window_run_ids(listing, now, 24), ["11", "14", "15"])
        calls: list[list[str]] = []
        scratches: list[Path] = []

        def fake_gh(args: list[str]) -> str:
            calls.append(args)
            if args[:2] == ["run", "list"]:
                return listing
            target = Path(args[args.index("--dir") + 1])
            scratches.append(target)
            _write_trx(target / "test-results-Linux-a" / "x.trx", 10)
            _write_trx(target / "test-results-Linux-b" / "x.trx", 40)
            return ""

        runs = measure_window(24, 8, fake_gh, now)
        check("measure_window reads the window's runs", sorted(runs), ["11", "14", "15"])
        check("measure_window reads each run's shard walls", runs["11"], {"test-results-Linux-a": 10.0, "test-results-Linux-b": 40.0})
        check("measure_window asks for the merge group's successful runs of the workflow",
              ("--workflow" in calls[0] and WORKFLOW in calls[0] and EVENT in calls[0] and "success" in calls[0]), True)
        check("measure_window downloads only the shard artifacts", calls[1][:4], ["run", "download", "11", "--pattern"])
        check("measure_window deletes each run's download before the next (disk holds one run)",
              [p.exists() for p in scratches], [False, False, False])
        calls.clear()
        capped = measure_window(24, 2, fake_gh, now)
        check("measure_window stops at max_runs, newest first", (sorted(capped), len(calls)), (["11", "14"], 3))
        line, code = verdict(capped, 24, cap=2)
        check("a capped read says so", (code, "the newest 2 of the last 24 h" in line), (1, True))
        line, code = verdict(capped, 24, cap=8)
        check("an uncapped read does not", "the newest" in line, False)

        def inert_gh(args: list[str]) -> str:
            if args[:2] == ["run", "list"]:
                return listing
            raise GhError("gh run download 11: no valid artifacts found to download")

        check("a run with no artifact is not a failure and does not count", measure_window(24, 8, inert_gh, now), {})

        def broken_gh(args: list[str]) -> str:
            raise GhError("HTTP 502")

        # report(): the live report measures only while the merge group shards by the weights file.
        repo = Path(scratch) / "repo"
        (repo / ".github" / "workflows").mkdir(parents=True)
        (repo / "tests" / "_shared").mkdir(parents=True)
        workflow = repo / WORKFLOW_FILE
        workflow.write_text("name: ci" + "\n", encoding="utf-8", newline="\n")
        check("no weights file: not in force", weighted_shards_in_force(repo), False)
        line, code = report(None, 24, THRESHOLD, 8, broken_gh, repo)
        check("a name-split matrix is NOT MEASURED, green, and never calls gh", (code, "NOT MEASURED" in line, "PB4329" in line), (0, True, True))
        (repo / WEIGHTS_FILE).write_text("{}", encoding="utf-8", newline="\n")
        check("a weights file the workflow does not use: not in force", weighted_shards_in_force(repo), False)
        workflow.write_text("env:" + "\n" + f"  {SHARD_ENV}: 1/5" + "\n", encoding="utf-8", newline="\n")
        check("weights file and shard variable: in force", weighted_shards_in_force(repo), True)
        line, code = report(None, 24, THRESHOLD, 8, broken_gh, repo, now)
        check("a gh failure is UNMEASURED and exits 2", (code, "UNMEASURED" in line and "HTTP 502" in line), (2, True))
        line, code = report(None, 24, THRESHOLD, 8, fake_gh, repo, now)
        check("the live report measures", (code, "OVER" in line), (1, True))
        # The planted runs are dated against the fixed `now`. The arm above is the guard: the wall clock is past every
        # planted run, so a report() that dropped `now` and read the wall clock would find its window empty and answer
        # NOT MEASURED there (it went red that way at 2026-11-01T09:00Z, kb/Work PB4353). This arm only checks that the
        # window's end follows a clock passed in two days later; on its own it would also pass with that regression.
        line, code = report(None, 24, THRESHOLD, 8, fake_gh, repo, now + datetime.timedelta(days=2))
        check("the live report's window ends at the clock it is given", (code, "NOT MEASURED" in line), (0, True))

        # generate: the weights file (kb/Work PB4329).
        win = "D:\\a\\COBOL\\COBOL\\tests\\WiseOwl.Cobol.Tests.Unit\\bin\\Release\\net10.0\\WiseOwl.Cobol.Tests.Unit.dll"
        lin = "/home/runner/work/COBOL/COBOL/tests/WiseOwl.Cobol.Tests.Conformance/bin/Debug/net10.0/WiseOwl.Cobol.Tests.Conformance.dll"
        check("duration_seconds reads hours, minutes, fractions and days",
              [duration_seconds(t, Path("x")) for t in ("00:00:01.5000000", "00:01:00", "1.00:00:00.25")], [1.5, 60.0, 86400.25])
        gen = Path(scratch) / "gen"
        # Run 1: Windows sums a class over its results (2 + 3) and over two trx (4 + 6); Linux, its two trx, likewise.
        _write_class_trx(gen / "1" / "test-results-Windows-shard-1" / "Unit.trx",
                         [(win, "N.A", "00:00:02"), (win, "N.A", "00:00:03"), (win, "N.B", "00:00:04")])
        _write_class_trx(gen / "1" / "test-results-Windows-shard-2" / "Unit.trx", [(win, "N.B", "00:00:06")])
        _write_class_trx(gen / "1" / "test-results-Linux-shard-1" / "Conformance.trx", [(lin, "C.N_P0", "00:00:10")])
        _write_class_trx(gen / "1" / "test-results-Linux-shard-2" / "inv1-strong.trx", [(lin, "C.N_P0", "00:00:20")])
        (gen / "1" / "unrelated").mkdir()
        _write_class_trx(gen / "2" / "test-results-Windows-shard-1" / "Unit.trx", [(win, "N.A", "00:00:07"), (win, "N.B", "00:00:01")])
        (gen / "3").mkdir()                                          # an inert run: no artifact at all
        check("class_seconds keys a class by its assembly and sums its results",
              class_seconds(gen / "1" / "test-results-Windows-shard-1" / "Unit.trx"), {("Unit", "N.A"): 5.0, ("Unit", "N.B"): 4.0})
        by_os = run_class_seconds(gen / "1")
        check("run_class_seconds is one observation per OS, every trx of its artifacts summed (INV-1's re-run included)",
              by_os, {"Windows": {("Unit", "N.A"): 5.0, ("Unit", "N.B"): 10.0}, "Linux": {("Conformance", "C.N_P0"): 30.0}})
        check("weights is the median over the observations that ran the class",
              weights([{("U", "A"): 5.0, ("U", "B"): 10.0}, {("U", "A"): 7.0, ("U", "B"): 1.0}, {("U", "A"): 6.04}]),
              {("U", "A"): 6.0, ("U", "B"): 5.5})
        out = Path(scratch) / "weights.json"
        line = generate(gen, out)
        written = json.loads(out.read_text(encoding="utf-8"))
        check("generate writes schema 1, every assembly's classes and the median class as the default",
              (written["schema"], written["assemblies"], written["default_seconds"]),
              (WEIGHTS_SCHEMA, {"Conformance": {"C.N_P0": 30.0}, "Unit": {"N.A": 6.0, "N.B": 5.5}}, 6.0))
        check("generate says what it read, and the inert run is no observation",
              ("3 class(es) from 3 observation(s)" in line, "runs 1, 2" in written["source"] or "run(s) 1, 2" in written["source"]),
              (True, True))
        check("the weights file is sorted, one class a line, LF-ended (a regeneration diffs by class)",
              out.read_bytes().count(b"\n"), 14)
        before = out.read_bytes()
        generate(gen, out)
        check("generate is deterministic", out.read_bytes(), before)
        foreign = Path(scratch) / "foreign"
        _write_class_trx(foreign / "1" / "test-results-Linux-shard-1" / "x.trx", [("/tmp/Other.dll", "N.A", "00:00:01")])
        (Path(scratch) / "inert-only" / "9").mkdir(parents=True)
        accepted = []
        for label, root in (("a codeBase that is no test assembly", foreign), ("a window of inert runs only", Path(scratch) / "inert-only")):
            try:
                generate(root, out)
                accepted.append(label)
            except ValueError:
                pass
        check("generate refuses a codeBase it cannot key and a window with no test results, and keeps the file it had",
              (accepted, out.read_bytes() == before), ([], True))
        try:
            duration_seconds("1 s", Path("t.trx"))
            check("a malformed duration is refused", "accepted", "ValueError")
        except ValueError as error:
            check("a malformed duration names the file", "t.trx" in str(error), True)

    if failures:
        print("shard_weights self-test FAILED:\n  " + "\n  ".join(failures))
        return 1
    print("shard_weights self-test: ok")
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="How evenly the merge group's shards divide the test work.")
    parser.add_argument("--self-test", action="store_true", help="run the planted cases")
    sub = parser.add_subparsers(dest="command")
    rep = sub.add_parser("report", help="the spread of the window's merge-group shards; exit 1 when OVER, 2 when UNMEASURED")
    rep.add_argument("--hours", type=int, default=DEFAULT_HOURS, help="the window, in hours (default %(default)s)")
    rep.add_argument("--threshold", type=float, default=THRESHOLD, help="the spread above which it is OVER (default %(default)s)")
    rep.add_argument("--max-runs", type=int, default=MAX_RUNS, help="the most shard runs to read, newest first (default %(default)s)")
    rep.add_argument("--from-dir", type=Path, help="read trx from DIR/<run id>/<artifact>/ instead of downloading")
    gen = sub.add_parser("generate", help=f"write {WEIGHTS_FILE} from merge-group trx; exit 2 when they cannot be read")
    gen.add_argument("--runs", type=int, default=GENERATE_RUNS, help="the newest successful runs to read (default %(default)s)")
    gen.add_argument("--from-dir", type=Path, help="read trx from DIR/<run id>/<artifact>/ instead of downloading")
    gen.add_argument("--out", type=Path, default=REPO_ROOT / WEIGHTS_FILE, help="the file to write (default the weights file)")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    if args.command == "generate":
        try:
            print(generate(args.from_dir, args.out, args.runs))
        except (GhError, ValueError, UnreadableTrx, OSError) as error:
            print(f"=== WEIGHTS: NOT WRITTEN -- {error} ===")
            return 2
        return 0
    if args.command != "report":
        parser.error("a command (report, generate) or --self-test is required")
    line, code = report(args.from_dir, args.hours, args.threshold, args.max_runs)
    print(line)
    return code


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
