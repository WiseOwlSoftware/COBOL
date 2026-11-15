"""The planted read records and witnesses the read records' cases build (kb/Work PB5597)."""

import selftest_cases

R = selftest_cases.subject(__file__)   # scripts/spec/lineage_reads.py


def cite(c="§2002:1.1 GR1"):
    return [{"cite": c, "quote": "a rule", "check": "OK"}]


def read(rid, verdict="SAME", **kw):
    return {"rule-id": rid, "to": [rid.split("-", 1)[1]], "verdict": verdict, "citations": cite(),
            "reason": f"planted {verdict}", "probe": None, **kw}


def refute(rid, verdict="UPHELD", **kw):
    return {"rule-id": rid, "verdict": verdict, "reason": f"planted {verdict}", "citations": cite(), "class": None,
            "probe": None, **kw}


def record(name="p-01", reads=(), refutes=None, asked=None):
    return {"slice": name, "edition": 2002, "asked": {"ids": [], "flags": asked or {}},
            "read": {"slice": name, "cases": list(reads)},
            "refute": None if refutes is None else {"slice": name, "cases": list(refutes), "disputed-different": []}}


def witnesses(runs):
    """Planted witnesses: each next-edition rule GR-<n> has the one case cases/X/<rule>, running at `runs[rule]`."""
    cases = {f"cases/X/{t}": R.CaseInfo(f"cases/X/{t}", R.REPO / "none", tuple(e), tuple(e), (), (t,), None)
             for t, e in runs.items()}
    return R.Witnesses(cases, {t: [f"conformance:cases/X/{t}"] for t in runs},
                       {t: ["2002", "2014", "2023"] for t in runs})


def raises(fn, *needles):
    try:
        fn()
    except (R.RecordError, SystemExit) as e:
        return all(n in str(e) for n in needles)
    return False
