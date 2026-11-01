#!/usr/bin/env python3
"""self_test_clock.py — THE CLOCK A SCRIPT SELF-TEST IS PROVEN AGAINST, so a fixture dated near its authoring day fails
the day it is written, not the day the wall clock overtakes it (kb/Work PB4358; docs/rearchitecture/DESIGN-test-build-ci.md
§3.14.10, "The clock proof").

    python scripts/self_test_clock.py --self-test

⛔ WHY. PB4353's self-test planted merge-group runs dated the day before it was written and read them through a real
24-hour window. It passed on its branch, in its merge group and on main, then went RED for every pull request at
2026-11-01T09:00Z. No gate ran a self-test against any clock but today's, and the PASS store (scripts/self_tests.py
`PassStore`) keyed a recorded PASS on file content only, so the local gates kept reusing a pre-expiry PASS while CI,
which never reuses, ran it fresh and went RED: local and CI disagreed for a second reason (the CI invariant).

THE RULE (one rule, `Clock.of`): a Python self-test whose code can read the wall clock is run a SECOND time with the
clock moved forward by CLOCK_SHIFT_DAYS in every Python process it starts, and its PASS counts only when both runs pass.
A fixture dated near its authoring day is compared with a wall clock that has moved past it, and goes RED in the first
gate that runs the self-test. A recorded PASS carries the horizon it proved (`Clock.until`), and a gate reuses it only
inside that horizon, so a reused PASS can never outlive the clock proof it rests on.
  SENSITIVE  the self-test's Python closure (the tracked `.py` files its run opened, `scripts/self_test_inputs.py`'s
             trace) reaches a wall-clock function (`reads_wall_clock`: `datetime.now/utcnow/today`, `date.today`,
             `time.time/time_ns/localtime/gmtime/ctime/asctime/strftime`). The runner itself (`inputs.RUNNER`) is left out
             of the closure: it reads the clock for its own bookkeeping, and its own self-test plants what it needs.
             An OVER-approximation is harmless (one more run); the second run is the semantic check, so no judgement of
             intent is made from the syntax tree.
  REAL       a self-test that compares the clock with a SECOND, real one the shift cannot move (file modification times,
             a process's start time, a git commit time, a timestamp PowerShell wrote) declares it on a `#` line among its
             first HEADER_LINES lines, with the reason:
                 # SELF-TEST-CLOCK: real — <the real clock it compares the Python clock with>
             and is not run shifted: the shift would put its two clocks 400 days apart, a red that says nothing about
             the self-test. The declaration is visible on the console line; nothing else exempts a self-test.
  RESIDUE    named, not hidden: a PowerShell or bash self-test is not shifted (`Get-Date` and `date` read a clock no
             Python process moves), and neither is a clock read that is not Python's (a file's mtime, a process's start
             time): the self-test reading it declares REAL. CI's nightly and every merge group still run it at today's
             clock.

HOW THE CLOCK MOVES. The runner's tracer directory (`self_test_inputs.install_tracer`) holds a `sitecustomize` that
begins with SHIM, so every Python process a shifted self-test starts (itself and every child that inherits its
environment) sees `datetime.datetime.now/utcnow/today`, `datetime.date.today`, `time.time`, `time.time_ns` and the
argument-less `time.localtime/gmtime/ctime/asctime/strftime` moved forward by SHIFT_ENV days. `time.monotonic` and
`time.perf_counter` are NOT moved (a duration measured on them is the same on both clocks), nor is `time.sleep`.
"""
from __future__ import annotations

import ast
import datetime as dt
import functools
import json
import os
import re
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]
#: The environment variable the SHIM reads: days to move the clock forward (absent or 0: no shift).
SHIFT_ENV = "COBOL_SELF_TEST_CLOCK_SHIFT_DAYS"
#: How far a proof run moves the clock. A fixture whose window the clock overtakes within this many days goes RED in the
#: proof run; one that expires later is proven only until `Clock.until`. More than a year: a licence, a certificate or a
#: "retain 90 days" window planted at today's date expires inside it.
CLOCK_SHIFT_DAYS = 400
#: A `#` header marker, as the platform and population markers are (scripts/self_tests.py HEADER_LINES).
REAL_RE = re.compile(r"^#\s*SELF-TEST-CLOCK:\s*(real)\s*[—-]+\s*(\S.*?)\s*$", re.MULTILINE)

#: The functions a script reads the wall clock through. `now/utcnow/today` on any receiver (`datetime.now`,
#: `dt.datetime.now`, `date.today`); the `time` module's by name (`.time()` on another receiver is a datetime's
#: time-of-day, not a clock read).
CLOCK_ATTRS = frozenset({"now", "utcnow", "today"})
TIME_FUNCS = frozenset({"time", "time_ns", "localtime", "gmtime", "ctime", "asctime", "strftime"})

#: ⛔ THE SHIM — the first thing the runner's `sitecustomize` runs, in every Python process a self-test starts. It moves
#: the clocks the module docstring names by SHIFT_ENV days and does nothing without it. A subclass of `datetime` and
#: `date` replaces each in the `datetime` module (the C type's classmethods cannot be assigned), with a metaclass whose
#: `__instancecheck__` keeps `isinstance(x, datetime.datetime)` true for an instance of the original type. Written
#: without backslashes and printing nothing.
SHIM = r'''
def _cobol_clock_shift():
    import os
    days = os.environ.get("COBOL_SELF_TEST_CLOCK_SHIFT_DAYS")
    if not days or not float(days):
        return
    import datetime as dt
    import time
    shift = float(days) * 86400.0
    delta = dt.timedelta(seconds=shift)
    real, real_ns = time.time, time.time_ns
    base_dt, base_date = dt.datetime, dt.date

    def meta(base):
        class Is(type):
            def __instancecheck__(cls, obj):
                return isinstance(obj, base)
        return Is

    def as_cls(cls, r):
        if issubclass(cls, base_dt):
            return cls(r.year, r.month, r.day, r.hour, r.minute, r.second, r.microsecond, r.tzinfo, fold=r.fold)
        return cls(r.year, r.month, r.day)

    class datetime(base_dt, metaclass=meta(base_dt)):
        @classmethod
        def now(cls, tz=None):
            return as_cls(cls, base_dt.now(tz) + delta)

        @classmethod
        def utcnow(cls):
            return as_cls(cls, base_dt.utcnow() + delta)

        @classmethod
        def today(cls):
            return as_cls(cls, base_dt.now() + delta)

    class date(base_date, metaclass=meta(base_date)):
        @classmethod
        def today(cls):
            return as_cls(cls, base_dt.now() + delta)

    datetime.__module__ = date.__module__ = "datetime"
    datetime.__qualname__, date.__qualname__ = "datetime", "date"
    dt.datetime, dt.date = datetime, date
    time.time = lambda: real() + shift
    time.time_ns = lambda: real_ns() + int(shift * 1e9)

    orig_localtime, orig_gmtime = time.localtime, time.gmtime
    orig_ctime, orig_asctime, orig_strftime = time.ctime, time.asctime, time.strftime
    time.localtime = lambda secs=None: orig_localtime(real() + shift if secs is None else secs)
    time.gmtime = lambda secs=None: orig_gmtime(real() + shift if secs is None else secs)
    time.ctime = lambda secs=None: orig_ctime(real() + shift if secs is None else secs)
    time.asctime = lambda t=None: orig_asctime(orig_localtime(real() + shift) if t is None else t)
    time.strftime = lambda fmt, t=None: orig_strftime(fmt, orig_localtime(real() + shift) if t is None else t)


_cobol_clock_shift()
del _cobol_clock_shift
'''


# ── which self-tests the proof reaches ─────────────────────────────────────────────────────────────────────────────────


@functools.cache
def _reads(source: str) -> bool:
    """`reads_wall_clock` for a file the run OPENED, which need not be code that ran (a `.py` fixture a tool was handed):
    one that does not parse reads no clock. Cached by source, since one helper is in the closure of many self-tests."""
    try:
        return reads_wall_clock(source)
    except (SyntaxError, ValueError):
        return False


def reads_wall_clock(source: str) -> bool:
    """True when the module's CODE can reach the wall clock (module docstring, SENSITIVE): it names `now`, `utcnow` or
    `today` on any receiver, a `time`-module clock function through the module (`time.time`, or the module's alias
    `import time as t`), or imports one by name (`from time import time as now`). It need not CALL it there: a clock
    handed on as a default or a parameter (`clock=time.time`) is reached all the same. A comment, a docstring and a
    string are not code; a module that does not parse raises SyntaxError (the caller names the file)."""
    tree = ast.parse(source)
    time_modules = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            time_modules.update(a.asname or a.name for a in node.names if a.name == "time")
        elif isinstance(node, ast.ImportFrom) and node.module == "time" and any(a.name in TIME_FUNCS for a in node.names):
            return True
    return any(isinstance(n, ast.Attribute)
               and (n.attr in CLOCK_ATTRS
                    or (n.attr in TIME_FUNCS and isinstance(n.value, ast.Name) and n.value.id in time_modules))
               for n in ast.walk(tree))


def declared_real(text: str, header_lines: int) -> str:
    """The reason a self-test declares its clock real (`# SELF-TEST-CLOCK: real — <why>` on a `#` line among its first
    `header_lines` lines), or "" when it does not."""
    m = REAL_RE.search("\n".join(text.splitlines()[:header_lines]))
    return m.group(2) if m else ""


@dataclass(frozen=True)
class Clock:
    """What a self-test's PASS must prove about the clock (module docstring): `shift_days` > 0 when it needs the second,
    shifted run, `real` the reason it declares the clock real, and nothing (both empty) when it cannot read the clock."""
    shift_days: int = 0
    real: str = ""

    @classmethod
    def of(cls, test: str, texts: dict[str, str], header_lines: int, runner: tuple[str, ...]) -> "Clock":
        """The clock proof `test` needs. `texts` maps each tracked `.py` file its run opened to its source. Only a Python
        self-test is shifted (RESIDUE); the runner's own files are left out of the closure."""
        if not test.endswith(".py"):
            return cls()
        real = declared_real(texts.get(test, ""), header_lines)
        if real:
            return cls(real=real)
        sensitive = any(_reads(src) for path, src in texts.items() if path not in runner)
        return cls(shift_days=CLOCK_SHIFT_DAYS) if sensitive else cls()

    def until(self, when: dt.datetime) -> str:
        """The last day a PASS recorded at `when` proves anything about the clock: the shift past that moment."""
        return (when + dt.timedelta(days=self.shift_days)).date().isoformat()

    @staticmethod
    def holds(record_clock: dict | None, today: dt.date) -> bool:
        """True when a recorded PASS's clock proof still covers `today` (a record with no clock proof always does)."""
        return record_clock is None or today.isoformat() <= record_clock["until"]


def shifted_env(env: dict[str, str], days: int) -> dict[str, str]:
    """`env` with every Python process it starts seeing the clock `days` forward (the SHIM reads it)."""
    out = dict(env)
    out[SHIFT_ENV] = str(days)
    return out


# ── the self-test ──────────────────────────────────────────────────────────────────────────────────────────────────────


def self_test() -> int:
    fails: list[str] = []

    def check(name: str, got, want) -> None:
        print(f"  {'ok  ' if got == want else 'FAIL'} {name}" + ("" if got == want else f": got {got!r}, want {want!r}"))
        if got != want:
            fails.append(name)

    for name, src, want in (
        ("datetime.now()", "import datetime\nx = datetime.datetime.now()\n", True),
        ("from-import datetime.utcnow()", "from datetime import datetime\nx = datetime.utcnow()\n", True),
        ("date.today()", "import datetime as d\nx = d.date.today()\n", True),
        ("time.time()", "import time\nx = time.time()\n", True),
        ("an aliased time module", "import time as t\nx = t.time_ns()\n", True),
        ("a time function imported by name", "from time import time as clock\nx = clock()\n", True),
        ("time.strftime", "import time\nx = time.strftime('%Y')\n", True),
        ("a datetime's .time() (time of day, not a clock read)", "def f(d):\n    return d.time()\n", False),
        ("time.monotonic and time.sleep (durations)", "import time\nt = time.monotonic(); time.sleep(1)\n", False),
        ("a mention in a docstring, a comment and a string", '"""datetime.now() reads the clock"""\n# time.time()\ns = "date.today()"\n', False),
        ("a clock handed on as a default and never called here", "import time\ndef f(clock=time.time):\n    return clock\n", True),
        ("a time function that is not a clock, imported by name", "from time import sleep\nsleep(1)\n", False),
        ("a `.time` attribute of another object and another module's `time`", "import os\nf = os.time\nx = d.time\n", False),
    ):
        check(f"arm: reads_wall_clock sees {name}", reads_wall_clock(src), want)

    marker = "#!/usr/bin/env python3\n# SELF-TEST-CLOCK: real — compares file mtimes with the clock\nimport os\n"
    shown = '"""\n1\n2\n3\n4\n# SELF-TEST-CLOCK: real — shown, not declared\n"""\n'
    check("arm: the real-clock marker is read with its reason; one shown past the header declares nothing",
          (declared_real(marker, 5), declared_real(shown, 5)), ("compares file mtimes with the clock", ""))
    runner = ("scripts/self_tests.py",)
    reading = "import datetime\nx = datetime.datetime.now()\n"
    quiet = "x = 1\n"
    cases = (
        ("a self-test that reads the clock is shifted", Clock.of("scripts/t.py", {"scripts/t.py": reading}, 5, runner),
         Clock(CLOCK_SHIFT_DAYS)),
        ("one whose imported helper reads it is shifted", Clock.of("scripts/t.py", {"scripts/t.py": quiet, "scripts/h.py": reading}, 5, runner),
         Clock(CLOCK_SHIFT_DAYS)),
        ("one that cannot read it is not", Clock.of("scripts/t.py", {"scripts/t.py": quiet}, 5, runner), Clock()),
        ("the runner's own clock reads leave it out", Clock.of("scripts/t.py", {"scripts/t.py": quiet, "scripts/self_tests.py": reading}, 5, runner),
         Clock()),
        ("a declared real clock is not shifted, and says why",
         Clock.of("scripts/t.py", {"scripts/t.py": marker + reading}, 5, runner), Clock(real="compares file mtimes with the clock")),
        ("a PowerShell self-test is not shifted (the residue)", Clock.of("scripts/t.ps1", {"scripts/h.py": reading}, 5, runner), Clock()),
    )
    for name, got, want in cases:
        check(f"arm: {name}", got, want)
    when = dt.datetime(2026, 11, 1, 12, 0, tzinfo=dt.timezone.utc)
    until = Clock(CLOCK_SHIFT_DAYS).until(when)
    check("arm: a PASS's proof reaches the shift past the day it was recorded, and no further",
          (until, Clock.holds({"until": until}, dt.date.fromisoformat(until)),
           Clock.holds({"until": until}, dt.date.fromisoformat(until) + dt.timedelta(days=1)), Clock.holds(None, dt.date.max)),
          ("2027-12-06", True, False, True))

    # The shim, in a child process exactly as a shifted self-test's children run it: every clock moves ONCE, an
    # instance of the original type is still a datetime, a duration is unchanged, and an unshifted child is untouched.
    PROBED = ("now", "utcnow", "today", "aware", "time", "ns", "local", "gm", "ctime", "asctime", "strftime")
    probe = (
        "import datetime as dt, time, json, warnings\n"
        "warnings.simplefilter('ignore')\n"
        "n = dt.datetime.now(); u = dt.datetime.utcnow(); t = dt.datetime.today(); d = dt.date.today()\n"
        "z = dt.datetime.now(dt.timezone.utc)\n"
        "base = dt.datetime.fromisoformat('2026-01-01T00:00:00')\n"
        "print(json.dumps({'now': n.timestamp(), 'utcnow': u.replace(tzinfo=dt.timezone.utc).timestamp(),\n"
        "  'today': t.timestamp(), 'date': d.toordinal(), 'aware': z.timestamp(), 'time': time.time(),\n"
        "  'ns': time.time_ns() / 1e9, 'local': time.mktime(time.localtime()), 'gm': time.mktime(time.gmtime()),\n"
        "  'ctime': time.mktime(time.strptime(time.ctime())), 'asctime': time.mktime(time.strptime(time.asctime())),\n"
        "  'strftime': time.mktime(time.strptime(time.strftime('%Y-%m-%d %H:%M:%S'), '%Y-%m-%d %H:%M:%S')),\n"
        "  'isinstance': [isinstance(base, dt.datetime), isinstance(n, dt.datetime), isinstance(d, dt.date),\n"
        "     isinstance(n, dt.date), type(n).__name__], 'span': (lambda a: time.monotonic() - a)(time.monotonic())}))\n")
    with tempfile.TemporaryDirectory() as d:
        shim_dir = Path(d)
        (shim_dir / "sitecustomize.py").write_text(SHIM, encoding="utf-8", newline="\n")

        def run(days: int | None) -> dict:
            env = dict(os.environ, PYTHONPATH=str(shim_dir))
            if days is not None:
                env = shifted_env(env, days)
            out = subprocess.run([sys.executable, "-c", probe], env=env, capture_output=True, text=True, encoding="utf-8")
            if out.returncode != 0 or out.stderr.strip():
                raise OSError(f"the shim probe failed (exit {out.returncode}): {out.stderr.strip()}")
            return json.loads(out.stdout)
        plain, moved = run(None), run(CLOCK_SHIFT_DAYS)
        span = CLOCK_SHIFT_DAYS * 86400
        # `utcnow` is naive UTC read back as UTC; `today`/`now()` are naive local read back as local: compare like with like
        deltas = {k: round(moved[k] - plain[k]) for k in PROBED}
        # the drift guard: every function `reads_wall_clock` names is one the probe reads and the shim moves, so a clock
        # function added to the detection without the shim (or the reverse) fails here, never in a gate
        check("arm: the probe reads, and the shim moves, exactly the functions the detection names",
              {"now", "utcnow", "today", "time", "time_ns", "localtime", "gmtime", "ctime", "asctime", "strftime"}
              == set(CLOCK_ATTRS | TIME_FUNCS) and {"now", "utcnow", "today", "time", "ns", "local", "gm", "ctime",
                                                    "asctime", "strftime", "aware"} == set(PROBED), True)
        check("arm: the shim moves every wall-clock function forward by exactly the shift, once (a double move, or none, "
              "would show here)", {k: abs(v - span) <= 5 for k, v in deltas.items()}, {k: True for k in deltas})
        check("arm: the shim moves the date by the same days", moved["date"] - plain["date"], CLOCK_SHIFT_DAYS)
        check("arm: an instance of the original type is still a datetime/date, the subclass is named datetime, and a "
              "monotonic span stays a span", (moved["isinstance"], moved["span"] < 1.0),
              ([True, True, True, True, "datetime"], True))
        check("arm: without the variable the shim changes nothing", (plain["isinstance"], abs(plain["time"] - plain["ns"]) < 5),
              ([True, True, True, True, "datetime"], True))
    print(f"self_test_clock.py --self-test: {'ALL GREEN' if not fails else f'{len(fails)} FAILED'}")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    args = sys.argv[1:] if argv is None else argv
    if args[:1] == ["--self-test"]:
        return self_test()
    print(__doc__.split("\n")[0])
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
