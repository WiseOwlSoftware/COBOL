#!/usr/bin/env python3
# SELF-TEST-CLOCK: real — its arms start child processes and compare their clocks with this process's own, which the runner would move
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
             the self-test. The declaration is visible on the console line; nothing else exempts a self-test. A clock
             that cannot be proved past today leaves no horizon to reuse a PASS inside, so a PASS of such a self-test is
             reused only on the day it was recorded (`Clock.until` is that day), and CI, which never reuses, runs it
             fresh against any clock the dated fixture has since been overtaken by.
  SCRIPT     a PowerShell or bash self-test is not shifted (`Get-Date` and `date` read a clock no Python process
             moves), so its PASS is held to the day it was recorded, as REAL's is, when ANY of its closure reads a clock:
             its own text, a script it runs or sources (the closure `Deriver.closure` derives and the PASS is keyed on:
             the files its text names, transitively, and the directories it reads whole) reads one (`SCRIPT_CLOCK_RE`), or
             a Python process it started (the tracer's trace) opened a tracked `.py` file that SENSITIVE reaches.
  RESIDUE    named, not hidden: a clock read that is not Python's (a file's mtime, a process's start time) in a Python
             self-test is declared REAL; a script the closure does not name (built from a variable at run time) is not
             read. CI's nightly and every merge group still run every self-test at today's clock.

HOW THE CLOCK MOVES. The runner's tracer directory (`self_test_inputs.install_tracer`) holds a `sitecustomize` that
begins with SHIM, so every Python process a shifted self-test starts (itself and every child that inherits its
environment) sees `datetime.datetime.now/utcnow/today`, `datetime.date.today`, `time.time`, `time.time_ns` and the
argument-less `time.localtime/gmtime/ctime/asctime/strftime` moved forward by SHIFT_ENV days. Each reading is the ONE
shifted instant (`time.time() + shift`) converted by the host zone at that instant, so a naive local reading and
`time.time()` agree on a day whose zone offset differs from today's (daylight saving). `time.monotonic` and
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
import time
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

#: A PowerShell or bash self-test reads the clock through `Get-Date`, `[DateTime]`, `date`, `$SECONDS` or a file's
#: modification time. No Python shim moves those, so such a self-test is not run shifted; its PASS is reused only on the
#: day it was recorded (`Clock.until`). Applied to every script of its closure (module docstring, SCRIPT). An
#: over-approximation (the word `date` in a comment) costs one re-run a day.
SCRIPT_CLOCK_RE = re.compile(r"Get-Date|\[(?:System\.)?DateTime\]|Get-Uptime|LastWriteTime|\.StartTime|\bdate\b|\$SECONDS\b|\bstat\b|-mtime\b")
SCRIPT_CLOCK = "a clock read by a PowerShell or bash script it runs, which no Python shim moves"

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
    real, real_ns = time.time, time.time_ns
    base_dt, base_date = dt.datetime, dt.date
    utc = dt.timezone.utc

    def meta(base):
        class Is(type):
            def __instancecheck__(cls, obj):
                return isinstance(obj, base)
        return Is

    def as_cls(cls, r):
        if issubclass(cls, base_dt):
            return cls(r.year, r.month, r.day, r.hour, r.minute, r.second, r.microsecond, r.tzinfo, fold=r.fold)
        return cls(r.year, r.month, r.day)

    # Every reading is derived from the ONE shifted instant (`real() + shift`) and converted by the host zone AT that
    # instant, so a naive local reading and `time.time()` agree on a day whose zone offset differs from today's (a
    # daylight-saving change inside the shift): adding a timedelta to today's wall time is an hour off there.
    class datetime(base_dt, metaclass=meta(base_dt)):
        @classmethod
        def now(cls, tz=None):
            return as_cls(cls, base_dt.fromtimestamp(real() + shift, tz))

        @classmethod
        def utcnow(cls):
            return as_cls(cls, base_dt.fromtimestamp(real() + shift, utc).replace(tzinfo=None))

        @classmethod
        def today(cls):
            return as_cls(cls, base_dt.fromtimestamp(real() + shift))

    class date(base_date, metaclass=meta(base_date)):
        @classmethod
        def today(cls):
            return as_cls(cls, base_dt.fromtimestamp(real() + shift))

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
    shifted run; `real` the reason its clock cannot be shifted (declared, or a PowerShell or bash clock read), which
    bounds its PASS to the day it was recorded; and nothing (both empty) when it cannot read the clock."""
    shift_days: int = 0
    real: str = ""

    @classmethod
    def of(cls, test: str, texts: dict[str, str], header_lines: int, runner: tuple[str, ...]) -> "Clock":
        """The clock proof `test` needs. `texts` maps the self-test and each file of its closure to its source: the
        tracked `.py` files its run opened, and for a PowerShell or bash self-test also the scripts it names. A Python
        self-test is shifted; a declared real clock, and a PowerShell or bash self-test whose closure reads one (SCRIPT),
        cannot be, so its PASS holds for the day it was recorded; the runner's own files are left out of the closure."""
        real = declared_real(texts.get(test, ""), header_lines)
        if real:
            return cls(real=real)
        python = [src for path, src in texts.items() if path.endswith(".py") and path not in runner]
        if not test.endswith(".py"):
            scripts = (src for path, src in texts.items() if not path.endswith(".py"))
            return (cls(real=SCRIPT_CLOCK)
                    if any(SCRIPT_CLOCK_RE.search(src) for src in scripts) or any(map(_reads, python)) else cls())
        return cls(shift_days=CLOCK_SHIFT_DAYS) if any(map(_reads, python)) else cls()

    def until(self, when: dt.datetime) -> str:
        """The last day a PASS recorded at `when` proves anything about the clock: the shift past that moment, and for a
        clock that cannot be shifted the day it was recorded."""
        return (when + dt.timedelta(days=self.shift_days)).date().isoformat()

    @property
    def bounded(self) -> bool:
        """True when a PASS recorded under this clock carries a horizon (`until`): every self-test that can read it."""
        return bool(self.shift_days or self.real)

    @staticmethod
    def holds(record_clock: dict | None, today: dt.date) -> bool:
        """True when a recorded PASS's clock proof still covers `today` (a record with no clock proof always does: the
        self-test cannot read a clock)."""
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
        ("a PowerShell or bash self-test is never shifted, and one whose closure reads no clock has none",
         (Clock.of("scripts/t.ps1", {"scripts/t.ps1": "Write-Output 1", "scripts/h.py": quiet}, 5, runner),
          Clock.of("scripts/t.sh", {"scripts/t.sh": "echo 1"}, 5, runner),
          Clock.of("scripts/t.ps1", {"scripts/t.ps1": "Write-Output 1", "scripts/self_tests.py": reading}, 5, runner)),
         (Clock(), Clock(), Clock())),
        ("a PowerShell self-test whose closure reads a clock is bounded to its day though its own text reads none: a "
         "script it runs or sources (R2-1), or a Python process it started whose file reads one",
         (Clock.of("scripts/t.ps1", {"scripts/t.ps1": "Write-Output 1", "scripts/h.ps1": "$a = Get-Date"}, 5, runner),
          Clock.of("scripts/t.sh", {"scripts/t.sh": "echo 1", "scripts/h.sh": "stamp=$(date +%s)"}, 5, runner),
          Clock.of("scripts/t.ps1", {"scripts/t.ps1": "Write-Output 1", "scripts/h.py": reading}, 5, runner)),
         (Clock(real=SCRIPT_CLOCK), Clock(real=SCRIPT_CLOCK), Clock(real=SCRIPT_CLOCK))),
        ("a PowerShell or bash self-test whose own text reads a clock is bounded to its day",
         (Clock.of("scripts/t.ps1", {"scripts/t.ps1": "$a = (Get-Date).AddDays(-1)"}, 5, runner),
          Clock.of("scripts/t.sh", {"scripts/t.sh": "stamp=$(date +%s)"}, 5, runner),
          Clock.of("scripts/t.ps1", {"scripts/t.ps1": "# SELF-TEST-CLOCK: real — a meter reading\nGet-Date"}, 5, runner)),
         (Clock(real=SCRIPT_CLOCK), Clock(real=SCRIPT_CLOCK), Clock(real="a meter reading"))),
    )
    for name, got, want in cases:
        check(f"arm: {name}", got, want)
    when = dt.datetime(2026, 11, 1, 12, 0, tzinfo=dt.timezone.utc)
    until = Clock(CLOCK_SHIFT_DAYS).until(when)
    check("arm: a PASS's proof reaches the shift past the day it was recorded, and no further",
          (until, Clock.holds({"until": until}, dt.date.fromisoformat(until)),
           Clock.holds({"until": until}, dt.date.fromisoformat(until) + dt.timedelta(days=1)), Clock.holds(None, dt.date.max)),
          ("2027-12-06", True, False, True))
    sameday = Clock(real="a meter reading").until(when)
    check("arm: a PASS under a clock that cannot be shifted holds for the day it was recorded and no longer (R1-2: it was "
          "reused forever, while CI runs the self-test fresh); only a self-test that cannot read a clock has no horizon",
          (sameday, Clock.holds({"until": sameday}, when.date()), Clock.holds({"until": sameday}, when.date() + dt.timedelta(days=1)),
           Clock(real="x").bounded, Clock(shift_days=1).bounded, Clock().bounded),
          ("2026-11-01", True, False, True, True, False))

    # The shim, in a child process exactly as a shifted self-test's children run it: every clock moves ONCE, an
    # instance of the original type is still a datetime, the monotonic clocks are untouched, and an unshifted child is
    # untouched. No arm limits how long a child takes: a reading is checked against the interval the PARENT's own clock
    # brackets the child's run in (a slow start-up only widens the interval), and RESOLUTION allows for the whole-second
    # readers (`ctime`, `asctime`, `strftime`) and the two clocks' rounding, never for slowness.
    RESOLUTION = 2.0
    PROBED = ("now", "utcnow", "today", "aware", "time", "ns", "local", "gm", "ctime", "asctime", "strftime")
    # every reading as an epoch second: a naive local one back through the host zone, a UTC one through `timegm`
    probe = (
        "import calendar, datetime as dt, time, json, warnings\n"
        "warnings.simplefilter('ignore')\n"
        "n = dt.datetime.now(); u = dt.datetime.utcnow(); t = dt.datetime.today(); d = dt.date.today()\n"
        "z = dt.datetime.now(dt.timezone.utc)\n"
        "base = dt.datetime.fromisoformat('2026-01-01T00:00:00')\n"
        "def parsed(s, f=None):\n"
        "    return time.mktime(time.strptime(s) if f is None else time.strptime(s, f))\n"
        "print(json.dumps({'now': n.timestamp(), 'utcnow': calendar.timegm(u.timetuple()) + u.microsecond / 1e6,\n"
        "  'today': t.timestamp(), 'date': d.toordinal(), 'aware': z.timestamp(), 'time': time.time(),\n"
        "  'ns': time.time_ns() / 1e9, 'local': time.mktime(time.localtime()), 'gm': calendar.timegm(time.gmtime()),\n"
        "  'ctime': parsed(time.ctime()), 'asctime': parsed(time.asctime()),\n"
        "  'strftime': parsed(time.strftime('%Y-%m-%d %H:%M:%S'), '%Y-%m-%d %H:%M:%S'),\n"
        "  'isinstance': [isinstance(base, dt.datetime), isinstance(n, dt.datetime), isinstance(d, dt.date),\n"
        "     isinstance(n, dt.date), type(n).__name__],\n"
        "  'untouched': [type(f).__name__ for f in (time.monotonic, time.perf_counter, time.sleep)]}))\n")
    # daylight saving: a fixed instant in daylight time under a POSIX zone with a daylight rule, shifted into standard time
    # (2026-10-20 + 400 days is 2027-11-24), must still read the same instant from every clock, however today's date
    # falls against the host zone's own transitions
    dst_probe = (
        "import calendar, datetime as dt, json, sys, time, warnings\n"
        "warnings.simplefilter('ignore')\n"
        "FAKE = 1792522800.0\n"
        "time.time = lambda: FAKE\n"
        "exec(compile(open(sys.argv[1], encoding='utf-8').read(), 'shim', 'exec'))\n"
        "now = time.time()\n"
        "u = dt.datetime.utcnow()\n"
        "print(json.dumps({'now': dt.datetime.now().timestamp() - now, 'today': dt.datetime.today().timestamp() - now,\n"
        "  'aware': dt.datetime.now(dt.timezone.utc).timestamp() - now, 'local': time.mktime(time.localtime()) - now,\n"
        "  'utcnow': calendar.timegm(u.timetuple()) + u.microsecond / 1e6 - now,\n"
        "  'date': dt.date.today() == dt.date.fromtimestamp(now),\n"
        "  'crossed': time.localtime(FAKE).tm_isdst != time.localtime().tm_isdst}))\n")
    with tempfile.TemporaryDirectory() as d:
        shim_dir = Path(d)
        (shim_dir / "sitecustomize.py").write_text(SHIM, encoding="utf-8", newline="\n")

        def child(argv: list[str], days: int | None, **extra: str) -> tuple[dict, float, float]:
            """One child's JSON, with this process's own readings from before it started and after it ended."""
            # this self-test declares its clock real; and the runner's own shim (PYTHONPATH holds the tracer directory under a
            # gate) must not also load in a child that applies SHIM itself, or the clock would move twice
            env = {k: v for k, v in os.environ.items() if k not in (SHIFT_ENV, "PYTHONPATH")}
            env.update(extra)
            if days is not None:
                env = shifted_env(env, days)
            before = time.time()
            out = subprocess.run([sys.executable, *argv], env=env, capture_output=True, text=True, encoding="utf-8")
            after = time.time()
            if out.returncode != 0 or out.stderr.strip():
                raise OSError(f"the shim probe failed (exit {out.returncode}): {out.stderr.strip()}")
            return json.loads(out.stdout), before, after

        def inside(read: dict, before: float, after: float, shift: float) -> dict[str, bool]:
            """Each reading against the parent's interval moved by `shift`: the child's clock moved by exactly that."""
            lo, hi = before + shift - RESOLUTION, after + shift + RESOLUTION
            held = {k: lo <= read[k] <= hi for k in PROBED}
            held["date"] = (dt.date.fromtimestamp(lo).toordinal() <= read["date"] <= dt.date.fromtimestamp(hi).toordinal())
            return held

        (plain, p0, p1) = child(["-c", probe], None, PYTHONPATH=str(shim_dir))
        (moved, m0, m1) = child(["-c", probe], CLOCK_SHIFT_DAYS, PYTHONPATH=str(shim_dir))
        span = CLOCK_SHIFT_DAYS * 86400
        # the drift guard: every function `reads_wall_clock` names is one the probe reads and the shim moves, so a clock
        # function added to the detection without the shim (or the reverse) fails here, never in a gate
        check("arm: the probe reads, and the shim moves, exactly the functions the detection names",
              {"now", "utcnow", "today", "time", "time_ns", "localtime", "gmtime", "ctime", "asctime", "strftime"}
              == set(CLOCK_ATTRS | TIME_FUNCS) and {"now", "utcnow", "today", "time", "ns", "local", "gm", "ctime",
                                                    "asctime", "strftime", "aware"} == set(PROBED), True)
        want = {k: True for k in (*PROBED, "date")}
        check("arm: the shim moves every wall-clock function forward by exactly the shift, once (a double move, or none, "
              "would put a reading outside the interval the parent's clock brackets the child's run in)",
              inside(moved, m0, m1, span), want)
        check("arm: and without the variable the shim changes nothing (every reading is inside the unmoved interval)",
              inside(plain, p0, p1, 0.0), want)
        check("arm: an instance of the original type is still a datetime/date, and the subclass is named datetime",
              (moved["isinstance"], plain["isinstance"]), ([True, True, True, True, "datetime"],) * 2)
        check("arm: the monotonic clocks and sleep are the interpreter's own functions, never replaced",
              (moved["untouched"], plain["untouched"]), (["builtin_function_or_method"] * 3,) * 2)
        shim_path = shim_dir / "sitecustomize.py"
        zone = {"TZ": "PST8PDT"}   # a POSIX zone with a daylight rule: Windows and Linux both honour it
        gaps, _, _ = child(["-c", dst_probe, str(shim_path)], CLOCK_SHIFT_DAYS, **zone)
        check("arm: a shift that crosses a daylight-saving change leaves every reading on the one shifted instant (a naive "
              "local reading made by adding days to today's wall time is an hour off there)",
              (gaps.pop("crossed"), gaps.pop("date"), {k: abs(v) <= 1.0 for k, v in gaps.items()}),
              (True, True, {k: True for k in gaps}))
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
