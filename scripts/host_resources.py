#!/usr/bin/env python3
"""host_resources.py — WHAT THIS HOST CAN GIVE A GATE, AND HOW ITS RUNNING OUT SHOWS (kb/Work PB5697;
docs/rearchitecture/DESIGN-test-build-ci.md section 3.14.6).

    python scripts/host_resources.py            # this host's commit now, and what one gate needs to be admitted
    python scripts/host_resources.py --self-test

⛔ WHY. Three whole-population gates at once, beside two probe fleets, exhausted this host's commit limit on 2026-11-15,
and one gate reported 24 test failures that were the host's errors (ERROR_PAGEFILE_QUOTA on a CreateDirectory, an out
of memory loading CoreLib, a paging file too small to load hostfxr, and the empty outputs and torn reads that follow
them). The gate said RED: its reader either gated again (a whole-population run spent) or hunted a defect that was not
there. Two mechanisms answer it, and this module is the one home of the data both read:

  THE SIGNATURES (`SIGNATURES`, one table): a host error code or message that says the host, not the test, ran out.
    A PRIMARY signature is the host's own error (a Win32 code, its HRESULT or its message text) and proves the host ran
    out of the resource it names. A SECONDARY symptom (an empty program output, a child that exited non-zero and said
    nothing, a decoder that read torn data) is what a test sees AFTER a host error, and is never evidence on its own: it
    counts only in a run that also shows a primary signature (`judge`). The gate's verdict (scripts/gate.py `_verdict`:
    NOT RUN [host-exhausted] when every red is the host's, RED naming the unjudged cases apart when one is not) and the
    landing lane's class of a merge-queue red (scripts/orchestrator/land_lane.py `classify_red`: INFRA when every red case
    is the host's) both read this table and `judge`, so the two never disagree on what a host error is.

  THE ADMISSION (`admit`): a gate takes a slot only when the host's free commit, less what the gates already holding a
    slot are still expected to commit, covers one gate's measured peak plus a margin (`GATE_NEED_BYTES`). Otherwise it
    WAITS in the FIFO at the head of the queue, as it does for a slot, and says why (scripts/gate_slot.py `acquire`,
    `status`). This is not a second cap: the cap bounds how many gates run, the admission keeps the next one from
    starting into a host that cannot hold it, whatever else (a probe fleet) is using the host.

THE COMMIT READING is Windows': the commit limit (RAM plus the paging files) and the commit charge, from
GetPerformanceInfo. Linux does not refuse a commit by default (overcommit), and the repository's gates run on Windows
(the slot cap does not span operating systems, gate_slot.py), so on Linux `read_commit` answers None, said as such:
the admission is open there by that policy, never by a default standing in for a reading."""
from __future__ import annotations

import os
import re
import sys
from dataclasses import dataclass, field
from typing import Callable, Iterable

IS_WINDOWS = os.name == "nt"
GIB = 1 << 30

#: The resources a signature can name, and what each is. The verdict names the resource, never "the host" alone.
RESOURCES = {
    "commit": "the host's commit limit (the virtual memory RAM and the paging files can back)",
    "memory": "memory (a process could not allocate what it needed to start or run)",
}


@dataclass(frozen=True)
class Signature:
    """One host-resource signature: its TAG, the RESOURCE it names, whether it is PRIMARY (the host's own error) or a
    secondary symptom, the PATTERN a failure's text is searched with, and WHAT it is (the code it comes from)."""
    tag: str
    resource: str
    primary: bool
    pattern: re.Pattern[str]
    what: str


def _sig(tag: str, resource: str, primary: bool, pattern: str, what: str) -> Signature:
    if resource not in RESOURCES:
        raise ValueError(f"signature {tag}: {resource!r} is not one of RESOURCES {sorted(RESOURCES)}")
    return Signature(tag, resource, primary, re.compile(pattern, re.IGNORECASE | re.MULTILINE), what)


#: ⛔ THE ONE TABLE of host-resource signatures (kb/Work PB5697). Each PRIMARY row is a Win32 error code, its HRESULT
#: (0x8007xxxx) or the system's own message text for it, exactly as a .NET process or the dotnet host prints it; each
#: SECONDARY row a symptom observed in the same runs. A row is added only from a run that showed it: a guess from
#: "empty output" alone would turn a real wrong answer into an unjudged case, so a secondary row never decides alone.
SIGNATURES: tuple[Signature, ...] = (
    _sig("pagefile-quota", "commit", True,
         r"Insufficient quota to complete the requested service|0x800705A[DE]\b",
         "ERROR_PAGEFILE_QUOTA (1454) and ERROR_WORKING_SET_QUOTA (1453), whose message both are: "
         "`System.IO.IOException : Insufficient quota to complete the requested service.` from Directory.CreateDirectory "
         "(run 000004-20261115T203340Z: ConformanceSuiteRunner.Stage, ScratchDirectory.Create)"),
    _sig("commitment-limit", "commit", True,
         r"0x800705AF\b|The paging file is too small for this operation to complete",
         "ERROR_COMMITMENT_LIMIT (1455): `Failed to load [...]\\hostfxr.dll], HRESULT: 0x800705AF` (the dotnet host "
         "could not commit the pages to load hostfxr)"),
    _sig("out-of-memory", "memory", True,
         r"0x8007000E\b",
         "E_OUTOFMEMORY (ERROR_OUTOFMEMORY 14 as an HRESULT): `Failed to load System.Private.CoreLib.dll (error code "
         "0x8007000E)`, `Failed to create CoreCLR, HRESULT: 0x8007000E` (a .NET process that could not start)"),
    _sig("empty-output", "commit", False,
         r"\bgot ''[ \t]*$",
         "a program whose output was empty where the golden has a first line (`expected '...', got ''`): its process "
         "died before it wrote, in the same runs as the primary rows"),
    _sig("silent-exit", "commit", False,
         r"exit non-zero:[ \t]*$",
         "a child process that exited non-zero and wrote nothing (`run exit non-zero: `): the dotnet host failed before "
         "it could say why"),
    _sig("torn-decode", "commit", False,
         r"Decoder ran into invalid data",
         "a Brotli read that failed (`System.InvalidOperationException: Decoder ran into invalid data`): the decoder "
         "could not allocate, or read a blob another process was writing when the host ran out"),
)

PRIMARY = tuple(s for s in SIGNATURES if s.primary)
SECONDARY = tuple(s for s in SIGNATURES if not s.primary)


def primaries(text: str) -> list[Signature]:
    """The PRIMARY signatures `text` shows (a failure's message, stack trace and output, or a host's log)."""
    return [s for s in PRIMARY if s.pattern.search(text)]


def secondaries(text: str) -> list[Signature]:
    """The SECONDARY symptoms `text` shows. Evidence only beside a primary signature in the same run (`judge`)."""
    return [s for s in SECONDARY if s.pattern.search(text)]


@dataclass(frozen=True)
class HostEvidence:
    """What one test host left that a judgement reads: its assembly, whether it was RED, whether its red is EXPLAINED by
    the failed cases its trx names (its own `Failed!` count equals the trx's Failed results: `gate_reuse.PassCarry`'s
    `red_explained`), each failed case's text (message, stack trace and output, `test_population.failed_texts`), and its
    log (the host's console, where a test host that could not start says why)."""
    asm: str
    red: bool
    explained: bool
    failures: dict[str, str] = field(default_factory=dict)
    log: str = ""


@dataclass(frozen=True)
class Judgement:
    """What a run's failures say about the host (`judge`): the RESOURCES its primary signatures name (empty: the host did
    not run out, and every red is a test red), per assembly the cases it could not judge because the host ran out
    (`unjudged`) and the failed cases that are test reds (`genuine`), and the red hosts whose whole red is the host's
    although no case explains it (`whole_host`: a host whose log shows a primary signature and whose red the trx does not
    account for, so every case it did not pass is unjudged), and the red hosts whose red the trx does not account for and
    whose log shows no primary signature (`unexplained`: a red no signature reaches, so it stays a test red)."""
    resources: tuple[str, ...] = ()
    unjudged: dict[str, tuple[str, ...]] = field(default_factory=dict)
    genuine: dict[str, tuple[str, ...]] = field(default_factory=dict)
    whole_host: frozenset[str] = frozenset()
    unexplained: frozenset[str] = frozenset()

    @property
    def exhausted(self) -> bool:
        """The host ran out in this run: some primary signature was seen."""
        return bool(self.resources)

    def host_is_the_hosts(self, asm: str) -> bool:
        """Whether `asm`'s red is wholly the host's: no failed case of it is a test red, and its red is either accounted
        for by its unjudged cases or is a host that could not run (`whole_host`)."""
        return (self.exhausted and not self.genuine.get(asm)
                and (asm in self.whole_host or (asm not in self.unexplained and bool(self.unjudged.get(asm)))))

    def unjudged_count(self) -> int:
        return sum(len(v) for v in self.unjudged.values())

    def named(self) -> str:
        """The resources, spelled for a verdict line: `commit limit exhausted`, `commit limit and memory exhausted`."""
        words = {"commit": "commit limit", "memory": "memory"}
        return " and ".join(words[r] for r in self.resources) + " exhausted"


def judge(hosts: Iterable[HostEvidence]) -> Judgement:
    """THE ONE JUDGEMENT of a run's failures against the host (kb/Work PB5697). A run is EXHAUSTED when any failed case or
    any red host's log shows a PRIMARY signature. Then a failed case showing a primary signature or a secondary symptom
    is UNJUDGED (the host's, not the test's), and every other failed case is GENUINE; a red host whose red no case
    explains and whose log shows a primary signature is the host's whole. In a run with no primary signature every failed
    case is genuine, whatever symptom it shows: an empty output alone is a test red."""
    hosts = [h for h in hosts if h.red]
    seen: list[str] = []
    for h in hosts:
        for text in [*h.failures.values(), h.log]:
            for s in primaries(text):
                if s.resource not in seen:
                    seen.append(s.resource)
    if not seen:
        return Judgement(genuine={h.asm: tuple(sorted(h.failures)) for h in hosts if h.failures})
    unjudged: dict[str, tuple[str, ...]] = {}
    genuine: dict[str, tuple[str, ...]] = {}
    whole: set[str] = set()
    unexplained: set[str] = set()
    for h in hosts:
        mine = sorted(c for c, t in h.failures.items() if primaries(t) or secondaries(t))
        rest = sorted(c for c in h.failures if c not in mine)
        if mine:
            unjudged[h.asm] = tuple(mine)
        if rest:
            genuine[h.asm] = tuple(rest)
        if not h.explained:
            (whole if primaries(h.log) else unexplained).add(h.asm)
    return Judgement(tuple(r for r in RESOURCES if r in seen), unjudged, genuine, frozenset(whole), frozenset(unexplained))


# ── THE ADMISSION ────────────────────────────────────────────────────────────────────────────────────────────────────

#: ⛔ ONE WHOLE-POPULATION GATE'S PEAK COMMIT ON THIS HOST, MEASURED (kb/Work PB5697; DESIGN-test-build-ci.md section
#: 3.14.6): the PeakJobMemoryUsed of the job a gate's slot holder binds its whole tree into (scripts/process_job.py
#: `bound_peak_bytes`; every gate records its own in its verdict record as `peak_commit_bytes`, so a re-measurement reads
#: the records). The measurement it was set from is in the note and the design doc.
GATE_PEAK_COMMIT_BYTES = 32 * GIB
#: The margin above the peak a gate needs free to start: what the host's own work (an editor, a language server, the
#: kernel's pools) may commit while the gate runs, so a gate admitted at its exact need does not take the last page.
ADMISSION_MARGIN_BYTES = 8 * GIB
#: THE THRESHOLD, stated once: the free commit, after what the gates already holding a slot have still to commit, that
#: one more gate needs before it takes a slot.
GATE_NEED_BYTES = GATE_PEAK_COMMIT_BYTES + ADMISSION_MARGIN_BYTES


@dataclass(frozen=True)
class Commit:
    """The host's commit now: its LIMIT and the CHARGE against it, in bytes."""
    limit: int
    charge: int

    @property
    def free(self) -> int:
        return max(0, self.limit - self.charge)


def gib(n: int | float) -> str:
    return f"{n / GIB:.1f} GiB"


if IS_WINDOWS:
    import ctypes
    from ctypes import wintypes

    class _PERFORMANCE_INFORMATION(ctypes.Structure):
        _fields_ = [("cb", wintypes.DWORD), ("CommitTotal", ctypes.c_size_t), ("CommitLimit", ctypes.c_size_t),
                    ("CommitPeak", ctypes.c_size_t), ("PhysicalTotal", ctypes.c_size_t),
                    ("PhysicalAvailable", ctypes.c_size_t), ("SystemCache", ctypes.c_size_t),
                    ("KernelTotal", ctypes.c_size_t), ("KernelPaged", ctypes.c_size_t),
                    ("KernelNonpaged", ctypes.c_size_t), ("PageSize", ctypes.c_size_t),
                    ("HandleCount", wintypes.DWORD), ("ProcessCount", wintypes.DWORD), ("ThreadCount", wintypes.DWORD)]

    class _PROCESSENTRY32W(ctypes.Structure):
        _fields_ = [("dwSize", wintypes.DWORD), ("cntUsage", wintypes.DWORD), ("th32ProcessID", wintypes.DWORD),
                    ("th32DefaultHeapID", ctypes.c_size_t), ("th32ModuleID", wintypes.DWORD),
                    ("cntThreads", wintypes.DWORD), ("th32ParentProcessID", wintypes.DWORD),
                    ("pcPriClassBase", ctypes.c_long), ("dwFlags", wintypes.DWORD), ("szExeFile", ctypes.c_wchar * 260)]

    class _PROCESS_MEMORY_COUNTERS_EX(ctypes.Structure):
        _fields_ = [("cb", wintypes.DWORD), ("PageFaultCount", wintypes.DWORD),
                    *[(n, ctypes.c_size_t) for n in ("PeakWorkingSetSize", "WorkingSetSize", "QuotaPeakPagedPoolUsage",
                                                     "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage",
                                                     "QuotaNonPagedPoolUsage", "PagefileUsage", "PeakPagefileUsage",
                                                     "PrivateUsage")]]

    _k32 = ctypes.WinDLL("kernel32", use_last_error=True)
    _k32.K32GetPerformanceInfo.argtypes = [ctypes.POINTER(_PERFORMANCE_INFORMATION), wintypes.DWORD]
    _k32.K32GetPerformanceInfo.restype = wintypes.BOOL
    _k32.CreateToolhelp32Snapshot.argtypes = [wintypes.DWORD, wintypes.DWORD]
    _k32.CreateToolhelp32Snapshot.restype = wintypes.HANDLE
    _k32.Process32FirstW.argtypes = [wintypes.HANDLE, ctypes.POINTER(_PROCESSENTRY32W)]
    _k32.Process32FirstW.restype = wintypes.BOOL
    _k32.Process32NextW.argtypes = [wintypes.HANDLE, ctypes.POINTER(_PROCESSENTRY32W)]
    _k32.Process32NextW.restype = wintypes.BOOL
    _k32.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    _k32.OpenProcess.restype = wintypes.HANDLE
    _k32.CloseHandle.argtypes = [wintypes.HANDLE]
    _k32.K32GetProcessMemoryInfo.argtypes = [wintypes.HANDLE, ctypes.POINTER(_PROCESS_MEMORY_COUNTERS_EX), wintypes.DWORD]
    _k32.K32GetProcessMemoryInfo.restype = wintypes.BOOL
    _TH32CS_SNAPPROCESS, _INVALID_HANDLE = 0x2, wintypes.HANDLE(-1).value
    _PROCESS_QUERY_LIMITED_INFORMATION, _PROCESS_VM_READ = 0x1000, 0x0010


def read_commit() -> Commit | None:
    """The host's commit limit and charge now (GetPerformanceInfo); None off Windows (the module docstring: the
    admission is Windows'). OSError when Windows refuses the reading: never a guessed value."""
    if not IS_WINDOWS:
        return None
    info = _PERFORMANCE_INFORMATION()
    info.cb = ctypes.sizeof(info)
    if not _k32.K32GetPerformanceInfo(ctypes.byref(info), info.cb):
        raise ctypes.WinError(ctypes.get_last_error())
    return Commit(info.CommitLimit * info.PageSize, info.CommitTotal * info.PageSize)


def _parents() -> dict[int, int]:
    """Every process's parent, by pid (one Toolhelp snapshot)."""
    snap = _k32.CreateToolhelp32Snapshot(_TH32CS_SNAPPROCESS, 0)
    if snap == _INVALID_HANDLE or not snap:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        entry = _PROCESSENTRY32W()
        entry.dwSize = ctypes.sizeof(entry)
        parents: dict[int, int] = {}
        ok = _k32.Process32FirstW(snap, ctypes.byref(entry))
        while ok:
            parents[int(entry.th32ProcessID)] = int(entry.th32ParentProcessID)
            ok = _k32.Process32NextW(snap, ctypes.byref(entry))
        return parents
    finally:
        _k32.CloseHandle(snap)


def _private_bytes(pid: int) -> int:
    """One process's commit charge (PrivateUsage); 0 for one that ended or that this user may not read."""
    h = _k32.OpenProcess(_PROCESS_QUERY_LIMITED_INFORMATION | _PROCESS_VM_READ, False, pid)
    if not h:
        return 0
    try:
        c = _PROCESS_MEMORY_COUNTERS_EX()
        c.cb = ctypes.sizeof(c)
        return int(c.PrivateUsage) if _k32.K32GetProcessMemoryInfo(h, ctypes.byref(c), c.cb) else 0
    finally:
        _k32.CloseHandle(h)


def tree_commit(roots: Iterable[int]) -> dict[int, int | None]:
    """Per root pid, the commit charge of its whole process tree now (the root and every descendant by parent pid);
    None for a root that is not running (its slot's holder ended, or the pid is not this host's). Off Windows every root
    reads None."""
    roots = list(roots)
    if not IS_WINDOWS:
        return {r: None for r in roots}
    parents = _parents()
    children: dict[int, list[int]] = {}
    for pid, parent in parents.items():
        if pid != parent:
            children.setdefault(parent, []).append(pid)
    out: dict[int, int | None] = {}
    for root in roots:
        if root not in parents:
            out[root] = None
            continue
        total, todo, seen = 0, [root], set()
        while todo:
            pid = todo.pop()
            if pid in seen:
                continue
            seen.add(pid)
            total += _private_bytes(pid)
            todo += children.get(pid, [])
        out[root] = total
    return out


@dataclass(frozen=True)
class Admission:
    """Whether one more gate may take a slot now (`admit`), and the line that says why."""
    ok: bool
    why: str


_HOLDER_PID = re.compile(r"\(pid (\d+)\b")


def holder_pid(label: str) -> int | None:
    """The pid a slot's label names (`gate <repo> (pid N, ...)`, `<command> (pid N, ...)`: every holder's label carries
    it, gate.py's and gate_slot.py run's), or None."""
    m = _HOLDER_PID.search(label or "")
    return int(m.group(1)) if m else None


def admit(commit: Commit | None, holders: dict[str, int | None], need: int = GATE_NEED_BYTES,
          peak: int = GATE_PEAK_COMMIT_BYTES) -> Admission:
    """THE ADMISSION RULE (kb/Work PB5697; the module docstring). `commit`: the host's reading (None: not Windows, open
    by policy). `holders`: each slot holder's label and its tree's commit now (None: unreadable, so it is reserved at a
    whole `peak`: a holder whose use cannot be read is assumed to have it all still to commit). A gate is admitted when
    the free commit, less each holder's remaining reservation (`peak` less what it has committed already, never below 0),
    is at least `need`."""
    if commit is None:
        return Admission(True, "no commit reading off Windows: the commit admission is a Windows host's (host_resources.py)")
    reserved = sum(peak if used is None else max(0, peak - used) for used in holders.values())
    room = commit.free - reserved
    how = (f"free commit {gib(commit.free)} of the limit {gib(commit.limit)}"
           + (f", less {gib(reserved)} the {len(holders)} gate(s) holding a slot may still commit" if holders else "")
           + f" = {gib(room)}; one gate needs {gib(need)} (its measured peak {gib(peak)} plus a margin of "
             f"{gib(need - peak)}, host_resources.GATE_NEED_BYTES, kb/Work PB5697)")
    return Admission(room >= need, how)


def admit_now(labels: dict[int, str], reading: Callable[[], Commit | None] = read_commit,
              trees: Callable[[Iterable[int]], dict[int, int | None]] = tree_commit) -> Admission:
    """The admission on this host now, for the held slots' `labels` (slot index to label): the commit reading and each
    holder's tree read live. `reading` and `trees` are injected by a self-test."""
    pids = {k: holder_pid(v) for k, v in labels.items()}
    used = trees([p for p in pids.values() if p is not None])
    return admit(reading(), {labels[k]: (used.get(p) if p is not None else None) for k, p in pids.items()})


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────────

#: The failure texts of run 000004-20261115T203340Z (kb/Work PB5697), as its trx recorded them, one per signature row.
OBSERVED = {
    "pagefile-quota": "System.IO.IOException : Insufficient quota to complete the requested service. : "
                      "'E:\\Temp\\wiseowl-cobol-tests\\497224-22c513cfab4e\\refuse16bd865dfb74'.",
    "commitment-limit": "[run] exit non-zero: Failed to load [C:\\Program Files\\dotnet\\host\\fxr\\11.0.0\\hostfxr.dll], "
                        "HRESULT: 0x800705AF\nThe library hostfxr.dll was found, but loading it failed",
    "out-of-memory": "[chain ST106A] run exit non-zero: Failed to load System.Private.CoreLib.dll (error code 0x8007000E)\n"
                     "Path: C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\10.0.15\\System.Private.CoreLib.dll\n"
                     "Error message: Out Of Memory\nFailed to create CoreCLR, HRESULT: 0x8007000E",
    "empty-output": "[2002/13.10-Constant/pb1232] FAIL: the output differs from cases/13.10-Constant/pb1232.out at line 1: "
                    "expected 'K1=3 K2=4 K3=3', got ''\n   at WiseOwl.Cobol.Tests.Conformance.ConformanceSuiteTests.Run()",
    "silent-exit": "[chain RL107A] run exit non-zero: \n   at WiseOwl.Cobol.Tests.Conformance.NistDifferentialTestsBase`1."
                   "NistProgram_MatchesGolden(String testName)",
    "torn-decode": "[2023/14.9.12-DIVIDE/pb4702] NO VERDICT: the compiler crashed on it: System.InvalidOperationException: "
                   "Decoder ran into invalid data.\n   at System.IO.Compression.BrotliStream.Read(Span`1 buffer)",
}
GENUINE_TEXT = "Assert.Equal() Failure: Values differ\nExpected: True\nActual:   False"


def self_test() -> int:
    results: list[tuple[str, bool, str]] = []

    def expect(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, ok, detail))

    expect("every signature row has an observed failure text, and each text shows exactly its own row",
           sorted(OBSERVED) == sorted(s.tag for s in SIGNATURES)
           and all([s.tag for s in SIGNATURES if s.pattern.search(t)] == [tag] for tag, t in OBSERVED.items()),
           str({tag: [s.tag for s in SIGNATURES if s.pattern.search(t)] for tag, t in OBSERVED.items()}))
    expect("a genuine assertion failure, and an output that differs with text, show no signature",
           not primaries(GENUINE_TEXT) and not secondaries(GENUINE_TEXT)
           and not secondaries("FAIL: the output differs at line 1: expected 'A', got 'B'"))
    for tag in ("pagefile-quota", "commitment-limit", "out-of-memory"):
        j = judge([HostEvidence("Conformance", True, True, {"C.one": OBSERVED[tag], "C.two": OBSERVED["empty-output"]})])
        res = next(s.resource for s in SIGNATURES if s.tag == tag)
        expect(f"a run whose one primary signature is {tag} is exhausted, names {res}, and holds no genuine red: the "
               "secondary case beside it is unjudged too",
               j.exhausted and j.resources == (res,) and j.unjudged == {"Conformance": ("C.one", "C.two")}
               and not j.genuine and j.host_is_the_hosts("Conformance"), str(j))
    j = judge([HostEvidence("Conformance", True, True, {"C.one": OBSERVED["empty-output"], "C.two": OBSERVED["silent-exit"],
                                                          "C.three": OBSERVED["torn-decode"]})])
    expect("secondary symptoms alone are never the host's: every case is a genuine red",
           not j.exhausted and j.genuine == {"Conformance": ("C.one", "C.three", "C.two")} and not j.unjudged, str(j))
    j = judge([HostEvidence("Conformance", True, True, {"C.q": OBSERVED["pagefile-quota"], "C.e": OBSERVED["empty-output"]}),
               HostEvidence("Unit", True, True, {"U.g": GENUINE_TEXT})])
    expect("a mixed run is exhausted AND keeps its genuine red apart: Conformance is the host's, Unit's case is a test red",
           j.exhausted and j.genuine == {"Unit": ("U.g",)} and j.unjudged == {"Conformance": ("C.e", "C.q")}
           and j.host_is_the_hosts("Conformance") and not j.host_is_the_hosts("Unit"), str(j))
    j = judge([HostEvidence("Unit", True, False, {}, "Failed to create CoreCLR, HRESULT: 0x8007000E"),
               HostEvidence("RepoDrift", True, False, {}, "Test run aborted.")])
    expect("a host that could not start (its log shows a primary signature, no case explains its red) is the host's "
           "whole; one whose log shows none stays a test red",
           j.resources == ("memory",) and j.whole_host == frozenset({"Unit"}) and j.host_is_the_hosts("Unit")
           and not j.host_is_the_hosts("RepoDrift"), str(j))
    j = judge([HostEvidence("Conformance", True, False, {"C.q": OBSERVED["pagefile-quota"]}, "Failed!  - Failed: 2")])
    expect("a host whose unjudged cases do not account for its red (its log counts more failures, and shows no primary "
           "signature) stays a test red", j.exhausted and not j.host_is_the_hosts("Conformance"), str(j))
    j = judge([HostEvidence("Unit", False, True, {}, "0x8007000E in a green host's log")])
    expect("a green host is not judged", not j.exhausted, str(j))
    expect("the verdict spells both resources", Judgement(("commit", "memory")).named() == "commit limit and memory exhausted")

    big = Commit(limit=140 * GIB, charge=60 * GIB)
    a = admit(big, {})
    expect("no holder, 80 GiB free: admitted", a.ok and "80.0 GiB" in a.why, a.why)
    a = admit(Commit(140 * GIB, 110 * GIB), {})
    expect("no holder, 30 GiB free (under the need): WAITS, and says the need", not a.ok and gib(GATE_NEED_BYTES) in a.why, a.why)
    a = admit(big, {"gate A (pid 1)": 2 * GIB, "gate B (pid 2)": GATE_PEAK_COMMIT_BYTES + GIB})
    expect("a holder that has committed little still reserves the rest of its peak: 80 free less 30 = 50, admitted",
           a.ok and "less 30.0 GiB" in a.why, a.why)
    a = admit(Commit(140 * GIB, 80 * GIB), {"gate A (pid 1)": 0, "gate B (pid 2)": None})
    expect("an unreadable holder reserves a whole peak: 60 free less 64 reserved WAITS", not a.ok, a.why)
    expect("no reading (off Windows): open, and says why", admit(None, {"x": None}).ok)
    got = admit_now({0: "gate E:\\w (pid 42, since ...)", 1: "no pid here"}, lambda: big, lambda ps: {p: GIB for p in ps})
    expect("admit_now reads each holder's pid from its label: pid 42 has 1 GiB of its peak committed (31 reserved), the "
           "label with no pid is unreadable (32 reserved): 80 free less 63 WAITS",
           not got.ok and "less 63.0 GiB" in got.why, got.why)
    expect("holder_pid reads gate.py's and run's labels", holder_pid("gate E:\\x (pid 123, since 2026)") == 123
           and holder_pid("dotnet test (pid 9, 2026-11-15 10:00:00)") == 9 and holder_pid("none") is None)
    if IS_WINDOWS:
        c = read_commit()
        me = tree_commit([os.getpid(), 0x7FFFFFF0])
        expect("this host's commit reads a limit above its charge, and this process's tree a positive charge; a pid "
               "that is not running reads None",
               c is not None and c.limit > 0 and c.charge > 0 and (me[os.getpid()] or 0) > 0 and me[0x7FFFFFF0] is None,
               f"{c} {me}")
    bad = [r for r in results if not r[1]]
    for name, ok, detail in results:
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"  [{detail}]" if not ok and detail else ""))
    print(f"=== HOST RESOURCES SELF-TEST: {'GREEN' if not bad else 'RED'} — {len(results) - len(bad)}/{len(results)} ===")
    return 1 if bad else 0


def main(argv: list[str]) -> int:
    if argv[:1] == ["--self-test"]:
        return self_test()
    c = read_commit()
    if c is None:
        print("host_resources: no commit reading off Windows (the admission is a Windows host's)")
        return 0
    print(f"host_resources: commit limit {gib(c.limit)}, charge {gib(c.charge)}, free {gib(c.free)}; one gate needs "
          f"{gib(GATE_NEED_BYTES)} free (peak {gib(GATE_PEAK_COMMIT_BYTES)} + margin {gib(ADMISSION_MARGIN_BYTES)})")
    return 0


if __name__ == "__main__":
    from standard_streams import ensure_utf8
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
