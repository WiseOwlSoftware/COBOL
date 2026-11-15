"""The planted screen the rule stage's cases read: six older rules, their newer twins, two pending clauses, a definitions clause each and a trigger table with one of each text mechanism and an empty slot."""
from typing import Any

import selftest_cases

L = selftest_cases.subject(__file__)   # scripts/spec/lineage_screen.py


def planted() -> Any:
    """A fresh planted screen (each case mutates its own)."""
    def rule(rid: str, section: str, text: str, kind: str = "GR", **kw: Any) -> dict[str, Any]:
        return {"id": rid, "section": section, "kind": kind, "subject": "S", "text": text, **kw}

    older = [rule("2002-GR-9.1.1.3-1", "9.1.1.3", "The ADD statement uses standard arithmetic."),
             rule("2002-GR-9.1.1.3-2", "9.1.1.3", "The ROUNDED phrase rounds."),
             rule("2002-GR-9.2.1.3-1", "9.2.1.3", "A record is written; see 8.4.2.", **{"band-formats": [1, 2]}),
             rule("2002-GR-9.3.1.3-1", "9.3.1.3", "An alphabet name is an old word here."),
             rule("2002-GR-9.4.1.3-1", "9.4.1.3", "A sort file is read."),
             rule("2002-GR-9.4.1.3-2", "9.4.1.3", "A sort file is merged."),
             rule("2002-GR-9.5.1.3-1", "9.5.1.3", "A clean rule holds.")]
    newer = [rule("GR-9.1.1.3-1", "9.1.1.3", "x"), rule("GR-9.1.1.3-2", "9.1.1.3", "x"),
             rule("GR-9.2.1.3-1", "9.2.1.3", "x", **{"band-formats": [1]}), rule("GR-9.3.1.3-1", "9.3.1.3", "x"),
             rule("GR-9.4.1.3-1", "9.4.1.3", "x"), rule("GR-9.4.1.3-2", "9.4.1.3", "x"),
             rule("GR-9.5.1.3-1", "9.5.1.3", "x"), rule("GR-9.4.1.3-3", "9.4.1.3", "x")]
    pairs = [{"rule-id": o["id"], "to": [n["id"]], "match": "exact"} for o, n in zip(older, newer)]
    pairs[1]["match"] = "normalized"
    old_cl = L.Clauses({"4": "Terms", "8.8": "Arithmetic", "8.9": "Status", "9.1.1.3": "General rules"},
                     {"4": "4.1 alphabet name: An old word.\n4.2 sort file: A file sorted.",
                      "8.8": "The sum is truncated to 32 digits.", "8.9": "Status 38 means locked.",
                      "9.1.1.3": "text"}, ("4", "8.8", "8.9", "9.1.1.3"))
    new_cl = L.Clauses({"3": "Terms", "3.1": "", "8.8": "Arithmetic", "8.9": "Status", "9.1.1.3": "General rules"},
                     {"3": "", "3.1": "**sort file**\nfile that is sorted", "8.8": "The sum is exact to 34 digits.",
                      "8.9": "Status 38 means locked.", "9.1.1.3": "text"}, ("3", "3.1", "8.8", "8.9", "9.1.1.3"))
    table = {"definitions": {"older": "4", "newer": "3"},
             "pending": {"8.8": {"governs": ["9.1"], "invokes": [r"\bstandard arithmetic\b"], "why": "planted"}},
             "behaviours": {"b-2023": {"sections": ["9.4"], "why": "planted"}},
             "holds": {"2002-GR-9.4.1.3-2": "a planted hold"},
             "triggers": [{"name": t, "mechanism": m, "at": "text", "params": p, "source": "planted", "reason": "planted"}
                          for t, m, p in (("pending-clause", "pending-clause", {}),
                                          ("definition-dropped", "definitions", {"which": "dropped"}),
                                          ("cross-reference", "text-pattern", {"pattern": r"\b\d+(?:\.\d+)+\b"}),
                                          ("normalized", "pair-match", {"accept": ["exact"]}),
                                          ("band-formats", "field-differs", {"field": "band-formats"}),
                                          ("clause-identity", "clause-identity", {}),
                                          ("behaviour-switch", "behaviour-switch", {}),
                                          ("held", "listed", {"rows": "holds"}))]
             + [{"name": "removed-context", "mechanism": None, "source": "PLAN-2002-v3-K0.md"}]}
    cl = {2002: old_cl, 2023: new_cl}
    return L.Screen(2002, 2023, {r["id"]: r for r in older}, {r["id"]: r for r in newer}, pairs, table, ["8.8", "8.9"],
                  cl.__getitem__, lambda y: "", [{"id": "b-2023", "changedIn": 2023}, {"id": "b-2002", "changedIn": 2002}])
