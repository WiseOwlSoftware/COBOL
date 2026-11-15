"""The planted screen the lineage screen's cases read: older rules and their newer twins, three pending clauses (one
changed and judged, one identical, one changed and not judged), a definitions clause each, a 2014 text, and a trigger
table with one row of every mechanism."""
from typing import Any

import selftest_cases

L = selftest_cases.subject(__file__)   # scripts/spec/lineage_screen.py


def rule(rid: str, section: str, text: str, kind: str = "GR", **kw: Any) -> dict[str, Any]:
    return {"id": rid, "section": section, "kind": kind, "subject": "S", "text": text, **kw}


#: Each planted older rule, its section and its text; the newer twin of each is `GR-<section>-<n>` with text "x".
OLDER = [rule("2002-GR-9.1.1.3-1", "9.1.1.3", "The ADD statement uses standard arithmetic."),
         rule("2002-GR-9.1.1.3-2", "9.1.1.3", "The ROUNDED phrase rounds."),
         rule("2002-GR-9.2.1.3-1", "9.2.1.3", "A record is written; see 8.4.2.", **{"band-formats": [1, 2]}),
         rule("2002-GR-9.3.1.3-1", "9.3.1.3", "An alphabet name is an old word here."),
         rule("2002-GR-9.4.1.3-1", "9.4.1.3", "A sort file is read."),
         rule("2002-GR-9.4.1.3-2", "9.4.1.3", "A sort file is merged."),
         rule("2002-GR-9.5.1.3-1", "9.5.1.3", "A clean rule holds."),
         rule("2002-GR-9.6.1.3-1", "9.6.1.3", "A record entry is described."),
         rule("2002-GR-9.6.1.3-2", "9.6.1.3", "This entry is valid only in the file section."),
         rule("2002-FMT-9.7.1", "9.7.1", "CD entry: see 9.8.1 for its records.", kind="FMT"),
         rule("2002-GR-9.8.1.3-1", "9.8.1.3", "A clause of an entry is described."),
         rule("2002-GR-9.9.1.3-1", "9.9.1.3", "A rule two newer rules state."),
         rule("2002-SR-8.7.1.2-1", "8.7.1.2", "A name is unique.", kind="SR"),
         rule("2002-GR-9.10.1.3-1", "9.10.1.3", "Qualification makes the name unique.")]
#: The 2014 text: every planted rule's words but one (2002-GR-9.3.1.3-1's), for `absent-from`.
TEXT_2014 = " ".join(r["text"] for r in OLDER if r["id"] != "2002-GR-9.3.1.3-1")


def planted() -> Any:
    """A fresh planted screen (each case mutates its own)."""
    older = [dict(r) for r in OLDER]
    newer = [rule(r["id"].split("-", 1)[1], r["section"], "x", kind=r["kind"],
                  **({"band-formats": [1]} if r["id"] == "2002-GR-9.2.1.3-1" else {}))
             for r in OLDER if r["kind"] != "FMT"] + [rule("GR-9.4.1.3-3", "9.4.1.3", "x"), rule("GR-9.9.1.3-2", "9.9.1.3", "x")]
    pairs = [{"rule-id": o["id"], "to": [o["id"].split("-", 1)[1]], "match": "exact"} for o in older if o["kind"] != "FMT"]
    pairs[1]["match"] = "normalized"
    next(p for p in pairs if p["rule-id"] == "2002-GR-9.9.1.3-1")["to"].append("GR-9.9.1.3-2")
    old_cl = L.Clauses({"4": "Terms", "8.7": "Qualification", "8.8": "Arithmetic", "8.9": "Status"},
                       {"4": "4.1 alphabet name: An old word.\n4.2 sort file: A file sorted.",
                        "8.7": "Names are qualified by IN.", "8.8": "The sum is truncated to 32 digits.",
                        "8.9": "Status 38 means locked."}, ("4", "8.7", "8.8", "8.9"))
    new_cl = L.Clauses({"3": "Terms", "3.1": "", "8.7": "Qualification", "8.8": "Arithmetic", "8.9": "Status"},
                       {"3": "", "3.1": "**sort file**\nfile that is sorted", "8.7": "Names are qualified by OF or IN.",
                        "8.8": "The sum is exact to 34 digits.", "8.9": "Status 38 means locked."},
                       ("3", "3.1", "8.7", "8.8", "8.9"))
    table = {"definitions": {"older": "4", "newer": "3"},
             "pending": {"8.8": {"governs": ["9.1"], "invokes": [r"\bstandard arithmetic\b"], "why": "planted"}},
             "behaviours": {"b-2023": {"sections": ["9.4"], "why": "planted"}},
             "holds": {"2002-GR-9.4.1.3-2": "a planted hold"},
             "triggers": [{"name": t, "mechanism": m, "params": p, "source": "planted", "reason": "planted"}
                          for t, m, p in (("pending-clause", "pending-clause", {}),
                                          ("definition-changed", "definitions", {"which": "changed"}),
                                          ("definition-dropped", "definitions", {"which": "dropped"}),
                                          ("cross-reference", "text-pattern", {"pattern": r"\b\d+(?:\.\d+)+\b"}),
                                          ("normalized", "pair-match", {"accept": ["exact"]}),
                                          ("ambiguous", "pair-targets", {"max": 1}),
                                          ("absent-from-2014", "absent-from", {"edition": 2014, "except-kinds": ["FMT"]}),
                                          ("band-formats", "field-differs", {"field": "band-formats"}),
                                          ("behaviour-switch", "behaviour-switch", {}),
                                          ("held", "listed", {"rows": "holds"}),
                                          ("communication-reach", "construct-reach",
                                           {"context": "the planted CD entry", "clauses": ["9.6.1"], "formats-of": ["9.7"],
                                            "clause-pattern": r"9\.8\.\d+", "confined": r"\bonly in the file section"}))]
             + [{"name": "clause-identity", "mechanism": "clause-identity", "params": {}, "source": "planted",
                 "reason": "planted", "measured-only": True}]}
    cl = {2002: old_cl, 2023: new_cl}
    return L.Screen(2002, 2023, {r["id"]: r for r in older}, {r["id"]: r for r in newer}, pairs, table,
                    ["8.7", "8.8", "8.9"], cl.__getitem__, lambda y: L.probe_text(TEXT_2014),
                    [{"id": "b-2023", "changedIn": 2023}, {"id": "b-2002", "changedIn": 2002}])
