#!/usr/bin/env python3
"""A second compiler's measurement on planted results (DESIGN-conformance-suite.md section 5, kb/Work PB6054): the bucket of
each failure kind, the record and its checks, the baseline it feeds and the counts the status line reads."""
import json
import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/measurements.py
COMPILE_FAIL = ("it did not translate (exit 1): t.cob:34: warning: COMMUNICATION SECTION is not implemented "
                "[-Wpending] | 32 |  01 X PIC 9.")


def _res(i, verdict, reason="", clause="c"):
    return {"edition": "85", "id": i, "clause": clause, "verdict": verdict, "reason": reason, "expect": "accept"}


def _raises(call):
    try:
        call()
    except ValueError:
        return True
    return False


def case_01_each_failure_kind_has_its_bucket(expect):
    expect.equal("a compile failure is bucketed by its first diagnostic", T.bucket_of(_res("a", "FAIL", COMPILE_FAIL)),
                 "compile: COMMUNICATION SECTION is not implemented [-Wpending]")
    fortify = ("it did not translate (exit 1): <command-line>: warning: ‘_FORTIFY_SOURCE’ redefined | "
               "<command-line>: note: this is the location | t.cob:7: error: 'FOO' redefined 3 times")
    expect.equal("a driver note is not the first diagnostic; names and numbers are normalised",
                 T.bucket_of(_res("a", "FAIL", fortify)), "compile: 'N' redefined # times")
    expect.equal("an output difference is bucketed by clause folder",
                 T.bucket_of(_res("a", "FAIL", "the output differs from x at line 2", "IX.4.5@1985-READ")),
                 "output: IX.4.5@1985-READ")
    expect("a hang has its own bucket",
           T.bucket_of(_res("a", "NO VERDICT", "the program exceeded the hang guard (600 s)")).startswith("hang"))
    expect("a crash has its own bucket",
           T.bucket_of(_res("a", "NO VERDICT", "the compiler crashed (exit -6)")).startswith("abend"))


def case_02_a_result_of_no_known_kind_is_refused(expect):
    for verdict, reason in (("PASS", ""), ("FAIL", "something new"), ("NO VERDICT", "unknown")):
        expect(f"a {verdict} of no known kind is refused", _raises(lambda: T.bucket_of(_res("a", verdict, reason))))


def case_03_only_a_failure_is_bucketed_and_an_adjudicated_cause_is_kept(expect):
    first = T.record_results([_res("a", "PASS"), _res("b", "FAIL", COMPILE_FAIL), _res("c", "NOT APPLICABLE")], None)
    expect("only a failure is bucketed, unclassified",
           list(first["failures"]) == ["b"] and first["failures"]["b"]["cause"] == T.UNCLASSIFIED, str(first))
    first["failures"]["b"] = {"bucket": first["failures"]["b"]["bucket"], "cause": "non-portable-test"}
    again = T.record_results([_res("b", "FAIL", COMPILE_FAIL)], first)
    expect.equal("a re-record keeps the cause adjudicated for the same bucket", again["failures"]["b"]["cause"],
                 "non-portable-test")
    moved = T.record_results([_res("b", "FAIL", "the output differs", "Z")], first)
    expect.equal("a test whose bucket moved is unclassified again", moved["failures"]["b"]["cause"], T.UNCLASSIFIED)


def case_04_a_recorded_measurement_loads_feeds_the_baseline_and_is_counted(expect):
    root = scratch_dir("measure-")
    (root / "baselines").mkdir()
    results = root / "results.json"
    results.write_text(json.dumps({"tests": [_res("a", "PASS"), _res("b", "FAIL", COMPILE_FAIL)]}), encoding="utf-8",
                       newline="\n")
    _, written = T.record(root, "stub", "85", "stub 1", [results], {"a", "b", "c"}, {"a"})
    raw, problems = T.load(T.path_of(root, "stub"))
    expect("a recorded measurement loads clean", raw is not None and not problems, str(problems))
    baseline = T.config.load_baseline(root / "baselines" / "stub.json")[0]
    expect("the cleared test's verdict reaches the baseline, and no other", written == 1 and baseline == {"85": {"a": "PASS"}},
           str(baseline))
    expect.equal("the second-compiler term counts the unrecorded and the unclassified",
                 T.unmet(root, "stub", "85", {"a", "b", "c"}), {"population": 3, "unrecorded": 1, "unclassified": 1})
    expect.equal("no measurement leaves every test unrecorded", T.unmet(root, "none", "85", {"a", "b"}),
                 {"population": 2, "unrecorded": 2, "unclassified": 0})
    expect("a result outside the population is refused",
           _raises(lambda: T.record(root, "stub", "85", "stub 1", [results], {"a"}, set())))
    expect.equal("a test outside the population is a refusal", T.check_tree(root, {"85": {"a"}}),
                 [("measurements/stub.json", "85 b: no test of the 85 population has this id")])


def case_05_a_malformed_measurement_is_refused(expect):
    root = scratch_dir("measure-bad-")
    (root / "baselines").mkdir()
    results = root / "results.json"
    results.write_text(json.dumps({"tests": [_res("a", "PASS"), _res("b", "FAIL", COMPILE_FAIL)]}), encoding="utf-8",
                       newline="\n")
    T.record(root, "stub", "85", "stub 1", [results], {"a", "b"}, set())
    good = T.path_of(root, "stub").read_text(encoding="utf-8")
    planted = root / "planted"
    planted.mkdir()
    cases = (
        ("a verdict outside the vocabulary", lambda b: b["verdicts"].__setitem__("a", "MAYBE")),
        ("a failure with no bucket", lambda b: b["failures"].pop("b")),
        ("a cause outside the three", lambda b: b["failures"]["b"].__setitem__("cause", "bad luck")),
        ("a citation without the conformance cause", lambda b: b["failures"]["b"].__setitem__("citation", "§1985:I.1")),
        ("a conformance cause without a citation",
         lambda b: b["failures"]["b"].__setitem__("cause", "gnucobol-nonconformance")),
        ("a citation of no edition",
         lambda b: b["failures"]["b"].update(cause="gnucobol-nonconformance", citation="p. 3")))
    for what, mutate in cases:
        fresh = json.loads(good)
        mutate(fresh["editions"]["85"])
        bad = planted / "stub.json"
        bad.write_text(json.dumps(fresh), encoding="utf-8", newline="\n")
        expect(f"{what} is refused", bool(T.load(bad)[1]), what)
    ok = json.loads(good)
    ok["editions"]["85"]["failures"]["b"].update(cause="gnucobol-nonconformance", citation="§1985:II.1")
    (planted / "stub.json").write_text(json.dumps(ok), encoding="utf-8", newline="\n")
    expect("a conformance cause with a 1985 citation is accepted", not T.load(planted / "stub.json")[1])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
