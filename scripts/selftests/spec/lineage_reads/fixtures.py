"""The planted read records, witnesses and lineage the read records' cases build (kb/Work PB5597)."""
import types

import selftest_cases

R = selftest_cases.subject(__file__)   # scripts/spec/lineage_reads.py


def cite(c="§2002:1.1 GR1"):
    return [{"cite": c, "quote": "a rule", "check": "OK"}]


def twin(rid):
    """The planted next-edition twin of an older rule id: `2002-GR-1.1.3-1` -> `GR-1.1.3-1`."""
    return rid.split("-", 1)[1]


def read(rid, verdict="SAME", **kw):
    return {"rule-id": rid, "to": [twin(rid)], "verdict": verdict, "citations": cite(),
            "reason": f"planted {verdict}", "probe": None, **kw}


def refute(rid, verdict="UPHELD", **kw):
    return {"rule-id": rid, "verdict": verdict, "reason": f"planted {verdict}", "citations": cite(), "class": None,
            "probe": None, **kw}


def record(name="p-01", reads=(), refutes=None, asked=None):
    doc = {"slice": name, "edition": 2002, "asked": {"ids": [], "flags": asked or {}},
           "read": {"slice": name, "cases": list(reads)},
           "refute": None if refutes is None else {"slice": name, "cases": list(refutes), "disputed-different": []}}
    doc["imported"] = {k: R.digest(doc[k]) for k in ("read", "refute", "asked") if doc[k] is not None}
    return doc


def witnesses(accepted=None, diagnosed=None, verdicts=None):
    """Planted witnesses: each next-edition rule has the one case cases/X/<rule>, ACCEPTED at `accepted[rule]` and
    refused (a refusal test's refuse-at) at `diagnosed[rule]`."""
    accepted, diagnosed = accepted or {}, diagnosed or {}
    cases = {}
    for t in {*accepted, *diagnosed}:
        acc, ref = tuple(accepted.get(t, ())), tuple(diagnosed.get(t, ()))
        cases[f"cases/X/{t}"] = R.CaseInfo(f"cases/X/{t}", R.REPO / "none", tuple(dict.fromkeys((*acc, *ref))), acc, (),
                                           (t,), None, ref)
    return R.Witnesses(cases, {t: [f"conformance:cases/X/{t}"] for t in {*accepted, *diagnosed}},
                       {t: ["2002", "2014", "2023"] for t in {*accepted, *diagnosed}}, verdicts or {})


def lineage(rids, kinds=None, unpaired=()):
    """The planted lineage: each rule of `rids` paired with its twin (but `unpaired`), of the catalog kind its id names
    unless `kinds` says otherwise; the repository's evidence table."""
    rids = list(rids)
    k = {r: r.split("-")[1] for r in rids} | (kinds or {})
    return R.Lineage(k, {r: (twin(r),) for r in rids if r not in unpaired}, R.EvidenceTable.load())


def completed(code=0, stdout=""):
    return types.SimpleNamespace(returncode=code, stdout=stdout, stderr="")


def raises(fn, *needles):
    try:
        fn()
    except (R.RecordError, SystemExit) as e:
        return all(n in str(e) for n in needles)
    return False
