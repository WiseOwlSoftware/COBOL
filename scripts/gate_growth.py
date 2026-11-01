#!/usr/bin/env python3
"""gate_growth.py -- THE GROWTH TRIGGER OF PB4323 SECTION 3.4: IS THE WHOLE-POPULATION GATE OUTGROWING ITS BUDGET?
(kb/Work PB4333, step 10 of PB4323; docs/rearchitecture/DESIGN-test-build-ci.md section 3.14.13).

    python scripts/gate_growth.py status           # the one verdict line (session-probe prints it); exit 1 only when FIRED
    python scripts/gate_growth.py show             # every recorded sample, oldest first
    python scripts/gate_growth.py --self-test

WHY A TRIGGER. The gate runs the WHOLE population (owner decision 1 of PB4323), and Conformance grew 94 % in 27 days
(10,503 to 20,406 cases), so the gate grows with it: the owner's "drift back to more delay" built in. The design answers
with a trigger, not a hope (PIPELINE-REVIEW.md section 3.4): the one selection the evidence supports, the emitted-program
differential (a case whose program bytes, runtime and harness inputs are byte-identical to main's cannot be affected),
is built when EITHER

  time        the weekly median of the code lane's test phase (`timings.tests_s`: the seconds from the first test host's
              start to the last one's end) exceeds TIME_LIMIT_S = 6 minutes, or
  population  Conformance exceeds POPULATION_FACTOR = 1.5 times its 20,406 cases of 2026-10-31 (30,609).

Until one fires nothing is built. This tool is what makes "fires" a reading instead of a recollection.

THE SAMPLES. A gate's `verdict.json` lives in its worktree's TestResults and goes with the worktree when the branch
lands, so a week of them is never on disk at once. The gate (scripts/gate.py, `Gate._finish`) therefore writes one small
file per GREEN code-lane gate that ran its hosts, `<git common dir>/cobol-gate-growth/<stamp>-<worktree>-<run>.json`:
the run's UTC start, its `tests_s` and the Conformance cases it discovered. One file per run, written by an atomic
replace, so concurrent gates never share a file and a run recorded twice is one sample. A sample older than RETAIN_DAYS
is removed when the next one is written. The history is this HOST's: the box the owner's fleet gates on is the box whose
gate time the trigger bounds. A gate that ran no host (the inert lane), a REUSED verdict and a RED one are no sample: a
red stops early, a reuse ran nothing.

THE READING (`evaluate`), over the samples of the trailing WINDOW_DAYS:
  FIRED         the weekly median is over TIME_LIMIT_S on a COMPLETE window, or the newest sample's Conformance count is
                over the population limit (that arm needs no window);
  WITHIN        a complete window, both under their limits;
  PARTIAL       the window is not yet a week: the samples span fewer than WINDOW_DAYS days, or number fewer than
                MIN_SAMPLES. The median is shown, never judged: one heavy afternoon of a dozen concurrent gates is not
                a week (the first reading, 2026-11-01, was 7.7 min over 16 gates in a single day);
  NOT MEASURED  no sample in the window.
FIRED names the next step: measure the fraction of programs whose emitted C# changes per non-runtime landing
(`python scripts/arch/emit_fraction.py`), and build the differential only if its median is under 10 %.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import statistics
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

import sharedfile
from gate_slot import git_common_dir
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parent.parent

#: The directory of samples under the git common dir (shared by the main checkout and every linked worktree).
GROWTH_DIR_NAME = "cobol-gate-growth"
SCHEMA = 1
#: PIPELINE-REVIEW.md section 3.4: "the weekly median of the code lane's test phase exceeds 6 min".
TIME_LIMIT_S = 360.0
WINDOW_DAYS = 7
#: Fewer samples than this are a few gates' luck, not a median.
MIN_SAMPLES = 10
#: "...or the population exceeds 1.5 times today's": today's Conformance population is the review's basis, 20,406 cases
#: (PB4323: 10,503 to 20,406 in 27 days), so the limit is 30,609 (the review's "about 30,000").
POPULATION_BASIS = 20406
POPULATION_FACTOR = 1.5
POPULATION_LIMIT = int(POPULATION_BASIS * POPULATION_FACTOR)
#: A sample is kept this long (twice the window, so a pause in landing never empties the window it is judged over).
RETAIN_DAYS = 2 * WINDOW_DAYS
#: The step the trigger names, and the criterion that step is built on (PIPELINE-REVIEW.md section 3.4).
STEP = "step 10 of PB4323 (kb/Work PB4333)"
BUILD_IF_FRACTION_UNDER = 0.10

#: The UTC stamp a run directory's name carries: `<n>-<yyyymmddThhmmssZ>`.
_RUN_STAMP = re.compile(r"(\d{8}T\d{6}Z)")
VERDICT_PREFIX = "=== GATE GROWTH: "


class UnreadableSample(Exception):
    """A sample file that cannot be read: it names the file; a corrupt history is said, never skipped."""


@dataclass(frozen=True)
class Sample:
    """One GREEN code-lane gate: when it started, how long its test phase took, how many Conformance cases it ran."""
    stamp: dt.datetime       # UTC start of the run (from its directory's name)
    run: str                 # the run directory's name
    worktree: str            # the worktree directory's name
    tests_s: float
    conformance: int

    def filename(self) -> str:
        return f"{self.stamp:%Y%m%dT%H%M%SZ}-{self.worktree}-{self.run}.json"


@dataclass(frozen=True)
class Reading:
    state: str               # FIRED | WITHIN | PARTIAL | NOT MEASURED
    line: str
    exit_code: int


def run_stamp(run_name: str) -> dt.datetime:
    """The UTC start a run directory's name carries; ValueError when it has none."""
    m = _RUN_STAMP.search(run_name)
    if m is None:
        raise ValueError(f"{run_name!r} carries no UTC stamp")
    return dt.datetime.strptime(m.group(1), "%Y%m%dT%H%M%SZ").replace(tzinfo=dt.timezone.utc)


def growth_dir(repo: Path = REPO) -> Path:
    return git_common_dir(repo) / GROWTH_DIR_NAME


def record(directory: Path, run: Path, worktree: str, tests_s: float, conformance: int) -> Sample:
    """Write the sample of the GREEN code-lane gate whose run directory is `run`, then remove the samples older than
    RETAIN_DAYS. One file per run (a second call for the same run replaces the first), atomically."""
    sample = Sample(run_stamp(run.name), run.name, worktree, round(float(tests_s), 1), int(conformance))
    directory.mkdir(parents=True, exist_ok=True)
    body = {"schema": SCHEMA, "stamp": f"{sample.stamp:%Y-%m-%dT%H:%M:%SZ}", "run": sample.run, "worktree": worktree,
            "tests_s": sample.tests_s, "conformance": sample.conformance}
    sharedfile.replace_text(directory / sample.filename(), json.dumps(body, indent=1, sort_keys=True) + "\n")
    cutoff = sample.stamp - dt.timedelta(days=RETAIN_DAYS)
    for old in samples(directory):
        if old.stamp < cutoff:
            sharedfile.remove(directory / old.filename(), missing_ok=True)
    return sample


def samples(directory: Path) -> list[Sample]:
    """Every recorded sample, oldest first. A missing directory is no history; a file that is not a sample raises
    UnreadableSample."""
    if not directory.is_dir():
        return []
    found = []
    for path in sorted(directory.glob("*.json")):
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
            if doc["schema"] != SCHEMA:
                raise ValueError(f"schema {doc['schema']}, expected {SCHEMA}")
            found.append(Sample(run_stamp(doc["run"]), doc["run"], doc["worktree"], float(doc["tests_s"]),
                                int(doc["conformance"])))
        except (OSError, ValueError, KeyError, TypeError) as e:
            raise UnreadableSample(f"{path}: not a gate-growth sample ({type(e).__name__}: {e}); delete it") from None
    return sorted(found, key=lambda s: (s.stamp, s.worktree, s.run))


def _minutes(seconds: float) -> str:
    return f"{seconds / 60:.1f} min"


def evaluate(history: list[Sample], now: dt.datetime) -> Reading:
    """THE READING (module docstring): the window is the WINDOW_DAYS before `now`."""
    window = [s for s in history if s.stamp >= now - dt.timedelta(days=WINDOW_DAYS)]
    if not window:
        return Reading("NOT MEASURED", f"{VERDICT_PREFIX}NOT MEASURED -- no GREEN code-lane gate recorded a sample in "
                                       f"the last {WINDOW_DAYS} days (scripts/gate.py writes one per gate) ===", 0)
    median = statistics.median(s.tests_s for s in window)
    newest = max(window, key=lambda s: s.stamp)
    span_days = (now - min(s.stamp for s in history)).total_seconds() / 86400
    facts = (f"test phase median {median:.0f} s = {_minutes(median)} over {len(window)} gates (limit "
             f"{TIME_LIMIT_S:.0f} s = {_minutes(TIME_LIMIT_S)}); Conformance {newest.conformance} cases (limit "
             f"{POPULATION_LIMIT})")
    next_step = (f"build {STEP}: measure the changed-program fraction first (python scripts/arch/emit_fraction.py), "
                 f"build the differential only if its median is under {BUILD_IF_FRACTION_UNDER:.0%}")
    complete = span_days >= WINDOW_DAYS and len(window) >= MIN_SAMPLES
    over_time, over_population = median > TIME_LIMIT_S, newest.conformance > POPULATION_LIMIT
    if over_population or (complete and over_time):
        arms = " and ".join(a for a, over in (("population", over_population), ("time", over_time and complete)) if over)
        return Reading("FIRED", f"{VERDICT_PREFIX}FIRED ({arms}) -- {facts}; {next_step} ===", 1)
    if not complete:
        return Reading("PARTIAL", f"{VERDICT_PREFIX}PARTIAL -- {facts}; the window is not a week yet (samples span "
                                  f"{span_days:.1f} of {WINDOW_DAYS} days, {len(window)} of {MIN_SAMPLES} needed), so the "
                                  f"median is shown and not judged ===", 0)
    return Reading("WITHIN", f"{VERDICT_PREFIX}WITHIN -- {facts} ===", 0)


# -- the self-test ---------------------------------------------------------------------------------------------------


def self_test() -> int:
    failures: list[str] = []

    def expect(what: str, got, want) -> None:
        if got != want:
            failures.append(f"{what}: got {got!r}, wanted {want!r}")

    now = dt.datetime(2026, 11, 10, 12, 0, tzinfo=dt.timezone.utc)

    def plant(age_days: float, tests_s: float, conformance: int = 20000, n: int = 0) -> Sample:
        stamp = now - dt.timedelta(days=age_days)
        return Sample(stamp, f"{n:06d}-{stamp:%Y%m%dT%H%M%SZ}", "wt", tests_s, conformance)

    def week(tests_s: float, conformance: int = 20000, count: int = MIN_SAMPLES + 2) -> list[Sample]:
        """`count` gates spread evenly from 8 days ago to now: the first falls outside the window and covers it, the
        rest (at least MIN_SAMPLES) are its median."""
        return [plant(8 - 8 * i / (count - 1), tests_s, conformance, i) for i in range(count)]

    expect("no history is NOT MEASURED", evaluate([], now).state, "NOT MEASURED")
    expect("only samples older than the window is NOT MEASURED", evaluate([plant(9, 999)], now).state, "NOT MEASURED")
    within = evaluate(week(300), now)
    expect("a complete window under the limit is WITHIN, exit 0", (within.state, within.exit_code), ("WITHIN", 0))
    over = evaluate(week(400), now)
    expect("a complete window over the limit FIRES, exit 1", (over.state, over.exit_code), ("FIRED", 1))
    expect("FIRED names the step and its criterion", all(t in over.line for t in
           ("PB4333", "emit_fraction.py", "under 10%", "(time)")), True)
    expect("the limit is 'exceeds': a median of exactly 360 s is WITHIN", evaluate(week(360), now).state, "WITHIN")
    expect("360.1 s FIRES", evaluate(week(360.1), now).state, "FIRED")
    # One heavy day is not a week: the same slow samples, all within a day, are PARTIAL and exit 0.
    day = evaluate([plant(0.5 * i / 15, 660, n=i) for i in range(16)], now)
    expect("a slow day is PARTIAL, not FIRED", (day.state, day.exit_code), ("PARTIAL", 0))
    expect("PARTIAL shows the median it does not judge", "median 660 s" in day.line and "not judged" in day.line, True)
    # A window a week long but with too few samples is not a median either.
    few = evaluate([plant(8, 900, n=0), plant(0, 900, n=1)], now)
    expect("two samples a week apart are PARTIAL", few.state, "PARTIAL")
    # The span is the whole history's, the median the window's: old samples count for coverage only.
    older = [plant(12, 900, n=99)] + [plant(6 - 6 * i / 9, 300, n=i) for i in range(10)]
    expect("samples older than the window cover it without entering its median", evaluate(older, now).state, "WITHIN")
    # The population arm needs no window.
    pop = evaluate([plant(0.1, 100, POPULATION_LIMIT + 1)], now)
    expect("a population over 1.5x fires at once", (pop.state, pop.exit_code, "(population)" in pop.line), ("FIRED", 1, True))
    expect("exactly the limit does not", evaluate([plant(0.1, 100, POPULATION_LIMIT)], now).state, "PARTIAL")
    expect("the limit is 1.5 x 20,406", POPULATION_LIMIT, 30609)
    both = evaluate(week(400, POPULATION_LIMIT + 1), now)
    expect("both arms are named", "(population and time)" in both.line, True)

    # record / samples: one file per run, atomic, idempotent, pruned, and a corrupt file is said.
    with tempfile.TemporaryDirectory() as tmp:
        d = Path(tmp) / GROWTH_DIR_NAME
        run = Path("TestResults") / "build-local" / "000007-20261101T131225Z"
        first = record(d, run, "wt1", 388.34, 20482)
        expect("the sample's tests_s is rounded to a tenth", first.tests_s, 388.3)
        record(d, run, "wt1", 388.34, 20482)
        record(d, Path("000008-20261101T140000Z"), "wt2", 400, 20482)
        got = samples(d)
        expect("a run recorded twice is one sample, and two worktrees are two", [(s.worktree, s.run) for s in got],
               [("wt1", "000007-20261101T131225Z"), ("wt2", "000008-20261101T140000Z")])
        expect("a sample reads back what was written", got[0], first)
        expect("the history is oldest first", got[0].stamp < got[1].stamp, True)
        record(d, Path("000009-20261125T000000Z"), "wt1", 300, 20482)
        expect("a sample older than RETAIN_DAYS goes when a newer one is written",
               [s.run for s in samples(d)], ["000009-20261125T000000Z"])
        (d / "bad.json").write_text("{not json", encoding="utf-8")
        try:
            samples(d)
            failures.append("a corrupt sample file was read as none")
        except UnreadableSample as e:
            expect("a corrupt sample names its file", "bad.json" in str(e), True)
        expect("a missing directory is no history", samples(Path(tmp) / "absent"), [])
    expect("a run name without a stamp is refused", _refused(lambda: run_stamp("latest")), True)

    if failures:
        print("gate_growth self-test FAILED:\n  " + "\n  ".join(failures))
        return 1
    print("gate_growth self-test OK")
    return 0


def _refused(call) -> bool:
    try:
        call()
    except ValueError:
        return True
    return False


# -- the command line ------------------------------------------------------------------------------------------------


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("command", nargs="?", choices=("status", "show"), default="status")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    try:
        history = samples(growth_dir())
    except (UnreadableSample, subprocess.CalledProcessError, OSError) as e:
        print(f"{VERDICT_PREFIX}UNMEASURED -- {e} ===")
        return 2
    now = dt.datetime.now(dt.timezone.utc)
    if args.command == "show":
        for s in history:
            print(f"{s.stamp:%Y-%m-%d %H:%M}Z  {s.tests_s:7.1f} s  Conformance {s.conformance}  {s.worktree}  {s.run}")
    reading = evaluate(history, now)
    print(reading.line)
    return reading.exit_code


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
