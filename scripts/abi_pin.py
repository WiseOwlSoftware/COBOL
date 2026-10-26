#!/usr/bin/env python3
"""abi_pin.py — THE RUNTIME ABI PIN, a file nobody edits by hand (kb/Work PB3610; docs/rearchitecture/DESIGN-external-repository.md
section 15.4; tests/WiseOwl.Cobol.Tests.Unit/RuntimeAbiPinDriftTests.cs).

    python scripts/abi_pin.py bump --runtime-major     # an incompatible change to PublicAPI.Shipped.txt or a codec's image
    python scripts/abi_pin.py bump --call-abi          # a compiler-side change to what crosses a CALL (RuntimeAbi.CallAbi)
    python scripts/abi_pin.py repin                    # no bump: the hashes moved and nothing incompatible did (a rename)
    python scripts/abi_pin.py settle [--stage]         # what the merge does when the pin says UNMEASURED
    python scripts/abi_pin.py --self-test

THE PIN (`src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin`): the runtime major and the hash of what it guards (the shipped public
surface and the codecs' images), the call ABI and the hash of the boundary-layout fixture's compiler-side decisions.
`RuntimeAbiPinDriftTests` re-measures all four and is red when the tree's measurement is not the pin.

⛔ WHY A FILE, AND WHY A TOOL. The pins used to be four constants in that test, and the numbers were also hand-edited in
`Directory.Build.props` and `RuntimeAbi.CallAbi`. Every branch that touched the runtime's surface re-derived the hashes
by hand and bumped the major, so two such branches always conflicted (4 landings in the logs of E:\\COBOL-coord\\logs, 2026-10-14
to 2026-10-24), and the resolution is never a line edit: the merged tree has its OWN hash, which no branch measured, and
its own major (a finisher's merge, 2026-10-24: "main's 17 shipped AnnuityDec(CobolRounding, CobolDec, long), so the merged
surface is an incompatible change against main's 17: major 18"). So:
  - an author runs `bump` or `repin`, which edits the three places a number lives and has the test WRITE the measurement
    (`WISEOWL_COBOL_WRITE_ABI_PIN=1`, as `WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC=1` writes the diagnostics doc);
  - the merge driver (merge_drivers.py, `cobol-abi-pin`) adds the counters two branches each raised (base + both
    increments) and marks the hashes UNMEASURED, and `merge_drivers.merge` runs `settle` before it makes the merge
    commit: the numbers are written into `Directory.Build.props` and `RuntimeAbi.cs`, and the test measures the merged
    tree and writes the hashes. Nothing is bumped by a branch's author that the landing then has to guess.

⛔ A SETTLE THAT FAILS SAYS WHOSE FAILURE IT IS (kb/Work PB3673). `settle` returns a `Failure(cause, why)`, the cause one of
the two `merge-*` rows of run_gate_legs.NOT_RUN_CAUSES, the gate's one classification by who can fix a cause: MERGE_BLOCKED
(the host's: a build output another process holds, a full disk, a command that cannot start, a git write refused; no finisher
can fix it and a retry may clear it) or MERGE_FAILED (the merged tree's own: a malformed pin or owned file, a build or the
measuring test that is red, a measurement that wrote no pin). `merge_drivers.merge` hands it to `enqueue.py land` as it is,
so a settle that could not run is never reported as a merge conflict.
"""
from __future__ import annotations

import argparse
import pathlib
import re
import subprocess
import sys
from typing import Callable, NamedTuple

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from test_population import scrubbed_env  # noqa: E402  (scripts/: the gate-environment scrub, kb/Work PB1718)
from standard_streams import ensure_utf8  # noqa: E402

PIN_PATH = "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin"
PROPS_PATH = "Directory.Build.props"
CALL_ABI_PATH = "src/WiseOwl.Cobol.Runtime/Control/RuntimeAbi.cs"
#: The files `settle` writes: the review's coverage rule (prune_worktrees.mechanical) accepts a merge commit whose tree
#: differs from the drivers' own merge in these and nowhere else.
OWNED = (PIN_PATH, PROPS_PATH, CALL_ABI_PATH)
UNMEASURED = "UNMEASURED"
COUNTERS = ("runtime-major", "call-abi")
HASHES = ("runtime-hash", "boundary-hash")
KEYS = ("runtime-major", "runtime-hash", "call-abi", "boundary-hash")
WRITE_VARIABLE = "WISEOWL_COBOL_WRITE_ABI_PIN"
TEST_PROJECT = "tests/WiseOwl.Cobol.Tests.Unit/WiseOwl.Cobol.Tests.Unit.csproj"
TEST_FILTER = "FullyQualifiedName~RuntimeAbiPinDriftTests"
NL = chr(10)
_VERSION = re.compile(r"(<Version>)(\d+)(\.0\.0</Version>)")
_CALL_ABI = re.compile(r"(public const int CallAbi = )(\d+)(;)")
#: THE TWO CLASSES OF A CLEAN MERGE THAT COULD NOT BE COMPLETED (kb/Work PB3673): the tags of the two `merge-*` rows of
#: run_gate_legs.NOT_RUN_CAUSES, the gate's ONE classification of a cause by who can fix it (enqueue.py reads the class
#: from there; enqueue.py's self-test holds each tag to a row). BLOCKED is a cause outside the merged tree, which no
#: finisher fixes and a retry may clear; FAILED is the merged tree's own. The settle's Failure carries them, and
#: merge_drivers.merge gives git's own refusals of the merge commit the same two.
MERGE_BLOCKED, MERGE_FAILED = "merge-blocked", "merge-failed"
#: What a failed build or test run says when the cause is the host's, not the tree's: MSBuild's copy of an output over a
#: file another process holds (MSB3027 after its retries, MSB3021: the language server of a session holds an analyzer
#: DLL, an antivirus scan holds a new one), a full disk, and a command that cannot start (gate_slot.py's `run` lets the
#: spawn's FileNotFoundError reach its stderr when there is no `dotnet` on the PATH).
_HOST_CAUSE = re.compile(r"MSB3027|MSB3021|being used by another process|is locked by|not enough space on the disk"
                         r"|No space left on device|FileNotFoundError", re.IGNORECASE)


class Failure(NamedTuple):
    """Why a settle did not settle: `cause`, the tag of the NOT_RUN_CAUSES row (MERGE_BLOCKED or MERGE_FAILED) that
    classifies it, and `why`, what to tell a person."""
    cause: str
    why: str


Runner = Callable[[pathlib.Path], "Failure | None"]


def failure(why: str, output: str = "") -> Failure:
    """The Failure of a command that exited non-zero: MERGE_BLOCKED when its FULL `output` names a cause of the host's
    (_HOST_CAUSE), else MERGE_FAILED. The caller keeps only the tail of the output in `why`, and the error that decides
    the class can be above that tail, so the class is read from the whole output."""
    return Failure(MERGE_BLOCKED if _HOST_CAUSE.search(output) else MERGE_FAILED, why)


def parse(text: str) -> dict[str, str] | None:
    """{key: value} of the pin's four lines (comment and blank lines aside), or None when it does not hold exactly them."""
    out: dict[str, str] = {}
    for ln in text.split(NL):
        if not ln.strip() or ln.lstrip().startswith("#"):
            continue
        parts = ln.split()
        if len(parts) != 2 or parts[0] not in KEYS or parts[0] in out:
            return None
        out[parts[0]] = parts[1]
    return out if set(out) == set(KEYS) else None


def with_values(text: str, values: dict[str, str]) -> str:
    """`text` with each key's value line replaced: the header and the order are the file's own."""
    return NL.join(f"{ln.split()[0]} {values[ln.split()[0]]}" if ln.split()[:1] and ln.split()[0] in values
                   and not ln.lstrip().startswith("#") else ln for ln in text.split(NL))


def merge_text(base: str, ours: str, theirs: str) -> str | None:
    """The merge driver: both sides changed the pin. Each counter is the base plus both sides' increments (two branches
    that each raised the major by one raise it by two: the merged surface is incompatible with each side's); the hashes
    are UNMEASURED, for `settle` to measure on the merged tree. None when a side is not a pin or a counter is not a number."""
    b, o, t = parse(base), parse(ours), parse(theirs)
    if not (b and o and t):
        return None
    try:
        merged = {k: str(int(b[k]) + (int(o[k]) - int(b[k])) + (int(t[k]) - int(b[k]))) for k in COUNTERS}
    except ValueError:
        return None
    return with_values(ours, {**merged, **{k: UNMEASURED for k in HASHES}})


def unmeasured(text: str) -> bool:
    """THE test of an unsettled pin, for every reader (needs_settle, merge_drivers.is_drivers_merge): a hash LINE reads
    UNMEASURED, which merge_text writes when both sides changed the pin. The header's prose names the word too ("the hashes
    read UNMEASURED"), so a text search was true for every tree: every merge of main ran a solution build and the
    measuring test, and the review's coverage rule passed hand edits of the ABI files (kb/Work PB3671 and its review
    R1-1). A pin `parse` refuses (conflict markers) is not unmeasured: RuntimeAbiPinDriftTests reports it."""
    values = parse(text)
    return values is not None and any(values[k] == UNMEASURED for k in HASHES)


def needs_settle(repo: pathlib.Path) -> bool:
    path = repo / PIN_PATH
    return path.is_file() and unmeasured(path.read_text(encoding="utf-8"))


def _sub(path: pathlib.Path, pattern: re.Pattern, number: str) -> None:
    text = path.read_text(encoding="utf-8")
    new, n = pattern.subn(lambda m: m.group(1) + number + m.group(3), text, count=1)
    if n != 1:
        raise ValueError(f"{path.name}: the {pattern.pattern} line is not there exactly once")
    path.write_bytes(new.encode("utf-8"))


def run_measurement(repo: pathlib.Path) -> Failure | None:
    """Build the Unit tests and run RuntimeAbiPinDriftTests in WRITE mode, under the shared gate slot (it is a build):
    None, or why it failed, classified (failure: a build output another process holds is the host's, MERGE_BLOCKED, and a
    command that cannot start is too). The filter is asserted to select something first (filter_population.py: vstest
    answers an unmatched filter with a passing run of zero tests, kb/Work PB708) and the run is scrubbed of the gate's
    environment (test_population.scrubbed_env, kb/Work PB1718); the write switch is the one variable added."""
    slot = [sys.executable, str(HERE / "gate_slot.py"), "run", "--repo", str(repo), "--label", "abi_pin: measure", "--"]
    spawn = {"cwd": repo, "capture_output": True, "text": True, "encoding": "utf-8", "errors": "replace"}
    try:
        guard = subprocess.run([*slot, sys.executable, str(HERE / "filter_population.py"), "--filter", TEST_FILTER, "--filtered",
                                TEST_PROJECT, "--allow-build"], env=scrubbed_env(), **spawn)
        if guard.returncode != 0:
            said = guard.stdout + guard.stderr
            return failure(f"the measuring filter's population check failed (filter_population.py rc {guard.returncode}):{NL}{said}", said)
        run = ["dotnet", "test", TEST_PROJECT, "-c", "Debug", "--filter", TEST_FILTER, "--nologo"]
        p = subprocess.run([*slot, *run], env={**scrubbed_env(), WRITE_VARIABLE: "1"}, **spawn)
    except OSError as e:
        return Failure(MERGE_BLOCKED, f"a command of the measurement could not start ({e}): is the .NET SDK installed?")
    said = p.stdout + p.stderr
    return None if p.returncode == 0 else failure(f"the measuring run exited {p.returncode}:{NL}" + NL.join(said.split(NL)[-40:]), said)


def settle(repo: pathlib.Path, *, stage: bool = True, measure: Runner = run_measurement) -> Failure | None:
    """Make the tree agree with the pin's counters and measure its hashes: the counters are written into
    Directory.Build.props (the major) and RuntimeAbi.cs (the call ABI), `measure` builds the tree and has the test write
    the hashes, and the owned files are staged (`stage`, for a merge in progress). None, or the Failure: MERGE_FAILED
    for the merged tree's own (the pin or an owned file is malformed, the measurement wrote no pin), MERGE_BLOCKED for a
    host that refused a write (the measurement classifies its own, run_measurement; kb/Work PB3673)."""
    try:
        pin = parse((repo / PIN_PATH).read_text(encoding="utf-8"))
    except OSError as e:
        return Failure(MERGE_FAILED, f"{PIN_PATH} cannot be read ({e})")
    if pin is None:
        return Failure(MERGE_FAILED, f"{PIN_PATH} does not hold exactly {', '.join(KEYS)}")
    try:
        _sub(repo / PROPS_PATH, _VERSION, pin["runtime-major"])
        _sub(repo / CALL_ABI_PATH, _CALL_ABI, pin["call-abi"])
    except ValueError as e:
        return Failure(MERGE_FAILED, str(e))
    except OSError as e:
        return Failure(MERGE_BLOCKED, f"an owned file could not be read or written ({e})")
    if why := measure(repo):
        return why
    after = parse((repo / PIN_PATH).read_text(encoding="utf-8"))
    if after is None or UNMEASURED in after.values() or any(after[k] != pin[k] for k in COUNTERS):
        return Failure(MERGE_FAILED, f"the measurement did not write {PIN_PATH} (hashes measured, counters as merged)")
    if stage:
        added = subprocess.run(["git", "add", "--", *OWNED], cwd=repo, capture_output=True, text=True)
        if added.returncode != 0:
            return Failure(MERGE_BLOCKED, f"git add of the settled files failed: {added.stderr.strip()}")
    return None


def bump(repo: pathlib.Path, *, runtime_major: bool, call_abi: bool, measure: Runner = run_measurement) -> Failure | None:
    """An author's bump: raise the named counters, mark the hashes UNMEASURED and settle (nothing staged)."""
    path = repo / PIN_PATH
    text = path.read_text(encoding="utf-8")
    pin = parse(text)
    if pin is None:
        return Failure(MERGE_FAILED, f"{PIN_PATH} does not hold exactly {', '.join(KEYS)}")
    values = {**pin, **{k: UNMEASURED for k in HASHES}}
    if runtime_major:
        values["runtime-major"] = str(int(pin["runtime-major"]) + 1)
    if call_abi:
        values["call-abi"] = str(int(pin["call-abi"]) + 1)
    path.write_bytes(with_values(text, values).encode("utf-8"))
    return settle(repo, stage=False, measure=measure)


def self_test() -> int:
    from scratch_repo import ScratchRepo, remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    pin = lambda major, rh, abi, bh: (f"# header{NL}runtime-major {major}{NL}runtime-hash {rh}{NL}call-abi {abi}{NL}"   # noqa: E731
                                      f"boundary-hash {bh}{NL}")
    base = pin(17, "AAAA", 2, "BBBB")
    expect("parse: the four keys, comments aside; anything else is not a pin",
           parse(base) == {"runtime-major": "17", "runtime-hash": "AAAA", "call-abi": "2", "boundary-hash": "BBBB"}
           and parse("runtime-major 17") is None and parse(base + "extra 1" + NL) is None
           and parse(base + "call-abi 3" + NL) is None)
    both = merge_text(base, pin(18, "CCCC", 2, "BBBB"), pin(18, "DDDD", 2, "BBBB"))
    expect("merge: a counter both sides raised by one is raised by two (the merged surface is incompatible with each "
           "side's), and the hashes are UNMEASURED; the header is ours'",
           both == pin(19, UNMEASURED, 2, UNMEASURED), repr(both))
    mixed = merge_text(base, pin(18, "CCCC", 2, "BBBB"), pin(17, "AAAA", 3, "EEEE"))
    expect("merge: a counter only one side raised keeps that side's value, so the two counters add independently",
           mixed == pin(18, UNMEASURED, 3, UNMEASURED), repr(mixed))
    expect("merge: a side that is not a pin, or a counter that is not a number, is a conflict (None)",
           merge_text(base, "x", base) is None and merge_text(base, base.replace("2", "two"), base) is None)
    prose = f"# the hashes read {UNMEASURED} and are measured on the merged tree{NL}"
    expect("unmeasured: a pin whose header names UNMEASURED only in its prose is measured, one whose hash line reads it "
           "is not, and a pin parse refuses (conflict markers) is not unmeasured (kb/Work PB3671: a text search made "
           "every merge of main build and measure)",
           not unmeasured(prose + base) and unmeasured(prose + pin(17, UNMEASURED, 2, "BBBB"))
           and not unmeasured(f"<<<<<<< ours{NL}{base}======={NL}{base}>>>>>>> theirs{NL}"))

    root = scratch_dir("abi-pin-selftest-")
    try:
        repo = ScratchRepo.init(root / "repo").path
        for rel, text in ((PIN_PATH, pin(18, UNMEASURED, 3, UNMEASURED)),
                          (PROPS_PATH, "<Project><Version>17.0.0</Version></Project>" + NL),
                          (CALL_ABI_PATH, "    public const int CallAbi = 2;" + NL)):
            (repo / rel).parent.mkdir(parents=True, exist_ok=True)
            (repo / rel).write_bytes(text.encode("utf-8"))
        s = ScratchRepo(repo)
        s.git("add", "-A")
        s.git("commit", "-q", "-m", "x")
        seen: list[str] = []

        def writes(r: pathlib.Path) -> Failure | None:     # the test's write mode: the tree's live numbers and hashes
            seen.append((r / PROPS_PATH).read_text(encoding="utf-8") + (r / CALL_ABI_PATH).read_text(encoding="utf-8"))
            (r / PIN_PATH).write_bytes(with_values((r / PIN_PATH).read_text(encoding="utf-8"),
                                                   {"runtime-hash": "1111", "boundary-hash": "2222"}).encode("utf-8"))
            return None
        expect("needs_settle: an UNMEASURED hash", needs_settle(repo))
        why = settle(repo, measure=writes)
        expect("settle: the merged counters are written into the props and RuntimeAbi.cs BEFORE the measurement, the "
               "test's hashes land in the pin, and the owned files are staged",
               why is None and "<Version>18.0.0</Version>" in seen[0] and "CallAbi = 3;" in seen[0]
               and parse((repo / PIN_PATH).read_text(encoding="utf-8")) == {
                   "runtime-major": "18", "runtime-hash": "1111", "call-abi": "3", "boundary-hash": "2222"}
               and not needs_settle(repo) and set(s.git("diff", "--cached", "--name-only").split()) == set(OWNED),
               f"{why} {seen}")
        (repo / PIN_PATH).write_bytes(pin(18, UNMEASURED, 3, UNMEASURED).encode("utf-8"))
        exited = Failure(MERGE_FAILED, "the measuring run exited 1")
        silent = settle(repo, measure=lambda r: None)
        expect("settle: a measurement that fails, or writes nothing, is the reason, never a pass; the measurement's own "
               "class passes through, and a measurement that wrote nothing is the merged tree's (MERGE_FAILED)",
               settle(repo, measure=lambda r: exited) == exited and silent is not None and "did not write" in silent.why
               and silent.cause == MERGE_FAILED)
        long_output = NL.join(["error MSB3027: Could not copy SourceGen.dll. The file is locked by: csharp-ls (62616)"]
                              + [f"line {i}" for i in range(100)])
        expect("failure: a build output another process holds (MSB3027, MSB3021), a full disk and a command that cannot "
               "start are MERGE_BLOCKED, read from the WHOLE output though the message keeps only its tail; a compile "
               "error or a red test is MERGE_FAILED (kb/Work PB3673)",
               failure("tail", long_output).cause == MERGE_BLOCKED
               and failure("x", "error MSB3021: Unable to copy file").cause == MERGE_BLOCKED
               and failure("x", "System.IO.IOException: There is not enough space on the disk.").cause == MERGE_BLOCKED
               and failure("x", "FileNotFoundError: [WinError 2] The system cannot find the file specified").cause == MERGE_BLOCKED
               and failure("x", "error CS0103: The name 'x' does not exist in the current context").cause == MERGE_FAILED
               and failure("x", "Failed RuntimeAbiPinDriftTests.RuntimeHash_IsThePin").cause == MERGE_FAILED
               and failure("x").cause == MERGE_FAILED)
        (repo / PROPS_PATH).write_bytes(b"<Project />")
        malformed = settle(repo, measure=writes)
        expect("settle: an owned file that lacks the line the settle rewrites is the merged tree's (MERGE_FAILED), and "
               "the measurement never runs",
               malformed is not None and malformed.cause == MERGE_FAILED and "not there exactly once" in malformed.why
               and len(seen) == 1, f"{malformed}")
        (repo / PROPS_PATH).write_bytes(("<Project><Version>17.0.0</Version></Project>" + NL).encode("utf-8"))
        (repo / PIN_PATH).write_bytes(pin(18, "1111", 3, "2222").encode("utf-8"))
        fresh = bump(repo, runtime_major=True, call_abi=False, measure=writes)
        expect("bump: the named counter rises by one, the other stays, and the tree is measured (nothing staged by it)",
               fresh is None and parse((repo / PIN_PATH).read_text(encoding="utf-8"))["runtime-major"] == "19"
               and parse((repo / PIN_PATH).read_text(encoding="utf-8"))["call-abi"] == "3"
               and "<Version>19.0.0</Version>" in (repo / PROPS_PATH).read_text(encoding="utf-8"), f"{fresh}")
    finally:
        remove_tree(root)
    print(f"abi_pin self-test: {'FAIL' if failures else 'PASS'}")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(NL)[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    b = sub.add_parser("bump")
    b.add_argument("--runtime-major", action="store_true")
    b.add_argument("--call-abi", action="store_true")
    sub.add_parser("repin")
    st = sub.add_parser("settle")
    st.add_argument("--stage", action="store_true", help="stage the files it writes (a merge in progress)")
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    repo = pathlib.Path(subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True).stdout.strip())
    if a.cmd == "bump" and (a.runtime_major or a.call_abi):
        err = bump(repo, runtime_major=a.runtime_major, call_abi=a.call_abi)
    elif a.cmd == "repin":
        err = bump(repo, runtime_major=False, call_abi=False)
    elif a.cmd == "settle":
        err = settle(repo, stage=a.stage)
    else:
        ap.print_help()
        return 2
    if err:
        print(f"abi_pin: [{err.cause}] {err.why}", file=sys.stderr)
        return 1
    print("abi_pin: the pin, Directory.Build.props and RuntimeAbi.cs agree and the tree is measured")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
