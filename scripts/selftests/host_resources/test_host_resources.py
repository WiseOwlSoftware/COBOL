#!/usr/bin/env python3
"""The host-resource signatures, their judgement and the commit admission (kb/Work PB5697; scripts/host_resources.py).
Every failure text is one run 000004-20261115T203340Z recorded (`host_resources.OBSERVED`); every commit reading is
injected, so no case exhausts the host."""
import os
import sys

import selftest_cases
from standard_streams import ensure_utf8

hr = selftest_cases.subject(__file__)   # scripts/host_resources.py
OBSERVED, GENUINE_TEXT, GIB = hr.OBSERVED, hr.GENUINE_TEXT, hr.GIB
HostEvidence, Judgement, Commit, judge, admit, gib = hr.HostEvidence, hr.Judgement, hr.Commit, hr.judge, hr.admit, hr.gib


def case_00_each_row_has_its_observed_text_and_matches_only_it(expect):
    expect("every signature row has an observed failure text, and each text shows exactly its own row",
           sorted(OBSERVED) == sorted(s.tag for s in hr.SIGNATURES)
           and all([s.tag for s in hr.SIGNATURES if s.pattern.search(t)] == [tag] for tag, t in OBSERVED.items()),
           str({tag: [s.tag for s in hr.SIGNATURES if s.pattern.search(t)] for tag, t in OBSERVED.items()}))
    expect("a genuine assertion failure, and an output that differs with text, show no signature",
           not hr.primaries(GENUINE_TEXT) and not hr.secondaries(GENUINE_TEXT)
           and not hr.secondaries("FAIL: the output differs at line 1: expected 'A', got 'B'"))


def case_01_each_primary_signature_exhausts_naming_its_resource(expect):
    for tag in ("pagefile-quota", "commitment-limit", "out-of-memory"):
        j = judge([HostEvidence("Conformance", True, True, {"C.one": OBSERVED[tag], "C.two": OBSERVED["empty-output"]})])
        res = next(s.resource for s in hr.SIGNATURES if s.tag == tag)
        expect(f"a run whose one primary signature is {tag} is exhausted, names {res}, and holds no genuine red: the "
               "secondary case beside it is unjudged too",
               j.exhausted and j.resources == (res,) and j.unjudged == {"Conformance": ("C.one", "C.two")}
               and not j.genuine and j.host_is_the_hosts("Conformance"), str(j))
    expect("the verdict spells both resources", Judgement(("commit", "memory")).named() == "commit limit and memory exhausted")


def case_02_secondary_symptoms_alone_are_test_reds(expect):
    j = judge([HostEvidence("Conformance", True, True, {"C.one": OBSERVED["empty-output"], "C.two": OBSERVED["silent-exit"],
                                                          "C.three": OBSERVED["torn-decode"]})])
    expect("secondary symptoms alone are never the host's: every case is a genuine red",
           not j.exhausted and j.genuine == {"Conformance": ("C.one", "C.three", "C.two")} and not j.unjudged, str(j))


def case_03_mixed_run_keeps_the_genuine_red_apart(expect):
    j = judge([HostEvidence("Conformance", True, True, {"C.q": OBSERVED["pagefile-quota"], "C.e": OBSERVED["empty-output"]}),
               HostEvidence("Unit", True, True, {"U.g": GENUINE_TEXT})])
    expect("a mixed run is exhausted AND keeps its genuine red apart: Conformance is the host's, Unit's case is a test red",
           j.exhausted and j.genuine == {"Unit": ("U.g",)} and j.unjudged == {"Conformance": ("C.e", "C.q")}
           and j.host_is_the_hosts("Conformance") and not j.host_is_the_hosts("Unit"), str(j))


def case_04_hosts_whose_red_no_case_explains(expect):
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


def case_05_admission_waits_under_the_threshold_and_proceeds_over_it(expect):
    big = Commit(limit=140 * GIB, charge=60 * GIB)
    a = admit(big, {})
    expect("no holder, 80 GiB free: admitted", a.ok and "80.0 GiB" in a.why, a.why)
    short = Commit(140 * GIB, 140 * GIB - hr.GATE_NEED_BYTES + GIB)
    a = admit(short, {})
    expect("no holder, one GiB under the need free: WAITS, and says the need", not a.ok and gib(hr.GATE_NEED_BYTES) in a.why,
           a.why)
    a = admit(Commit(140 * GIB, 140 * GIB - hr.GATE_NEED_BYTES), {})
    expect("no holder, exactly the need free: admitted", a.ok, a.why)
    a = admit(big, {"gate A (pid 1)": 2 * GIB, "gate B (pid 2)": hr.GATE_PEAK_COMMIT_BYTES + GIB})
    want = hr.GATE_PEAK_COMMIT_BYTES - 2 * GIB
    expect("a holder that has committed little still reserves the rest of its peak; one past its peak reserves nothing",
           f"less {gib(want)}" in a.why and a.ok == (80 * GIB - want >= hr.GATE_NEED_BYTES), a.why)
    a = admit(Commit(140 * GIB, 140 * GIB - hr.GATE_NEED_BYTES), {"gate B (pid 2)": None})
    expect("an unreadable holder reserves a whole peak: with exactly the need free, it WAITS", not a.ok, a.why)
    expect("no reading (off Windows): open, and says why", admit(None, {"x": None}).ok)
    got = hr.admit_now({0: "gate E:\\w (pid 42, since ...)", 1: "no pid here"}, lambda: big,
                       lambda ps: {p: GIB for p in ps})
    want = 2 * hr.GATE_PEAK_COMMIT_BYTES - GIB
    expect("admit_now reads each holder's pid from its label: pid 42 has 1 GiB of its peak committed, the label with no "
           "pid is unreadable and reserved whole", f"less {gib(want)}" in got.why, got.why)
    expect("holder_pid reads gate.py's and run's labels", hr.holder_pid("gate E:\\x (pid 123, since 2026)") == 123
           and hr.holder_pid("pwsh scripts/x.ps1 (pid 9, 2026-11-15 10:00:00)") == 9 and hr.holder_pid("none") is None)


def case_06_the_real_reading(expect):
    if not hr.IS_WINDOWS:
        expect("off Windows there is no commit reading (the admission is a Windows host's)", hr.read_commit() is None)
        return
    c = hr.read_commit()
    me = hr.tree_commit([os.getpid(), 0x7FFFFFF0])
    expect("this host's commit reads a limit above zero and a charge, and this process's tree a positive charge; a pid "
           "that is not running reads None",
           c is not None and c.limit > 0 and c.charge > 0 and (me[os.getpid()] or 0) > 0 and me[0x7FFFFFF0] is None,
           f"{c} {me}")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
