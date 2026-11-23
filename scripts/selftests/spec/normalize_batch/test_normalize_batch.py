#!/usr/bin/env python3
"""The batch normalizer reads the inventory schema's test-ref form table and both separators, never a copy of its own
(kb/Work PB3210): a form the schema gains is kept and respelled, and a separator the schema changes splits and joins every
batch, with no edit to normalize_batch.py."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

C = selftest_cases.subject(__file__)   # scripts/spec/normalize_batch.py
import inventory_schema  # noqa: E402  (the subject's sibling, on sys.path once `subject` imported the subject)


def _raw() -> dict:
    """A fresh copy of the working tree's schema, for a case to plant a form or a separator in."""
    return inventory_schema.raw_schema()


def _normalize(schema, test_ref: str, code_location: str = "") -> dict:
    rec = {"rule-id": "R-1", "test-ref": test_ref, **({"code-location": code_location} if code_location else {})}
    return C.normalize({"batch": "b", "records": [rec]}, "b", [], schema)["records"][0]


def case_00_a_form_the_schema_gains_survives(expect):
    raw = _raw()
    raw["test-ref"]["forms"]["planted"] = {"syntax": "planted:<Class>.<Method>", "test-dir": "tests/Planted.Tests",
                                           "spec-derived": True}
    raw["test-ref"]["forms"]["planted-file"] = {"syntax": "planted-file:<id>", "path-template": "planted/{0}.cob",
                                                "spec-derived": True}
    rec = _normalize(C.Schema(raw), "planted:FooTests.Bar; tests/Planted.Tests/Sub/FooTests.cs#Baz; planted/x/y.cob")
    expect.equal("a canonical ref of the new form is kept, a path under its test-dir and under its template respelled",
                 rec["test-ref"], "planted:FooTests.Bar; planted:FooTests.Baz; planted-file:x/y")
    rec = _normalize(C.load_schema(), "planted:FooTests.Bar")
    expect.equal("the same ref under the real schema, which lacks the form, is dropped", rec["test-ref"], "")


def case_01_the_schemas_separators_split_and_join(expect):
    raw = _raw()
    raw["test-ref"]["separator"] = " | "
    raw["code-location"]["separator"] = " , "
    rec = _normalize(C.Schema(raw), "unit:A.B | unit:A.C (a remark) | #D", "src/A.cs#A.M , src/B.cs")
    expect.equal("test-ref split on the schema's separator, joined with it", rec["test-ref"],
                 "unit:A.B | unit:A.C | unit:A.D")
    expect.equal("code-location split on the schema's separator, joined with it", rec["code-location"],
                 "src/A.cs#M , src/B.cs")
    rec = _normalize(C.load_schema(), "unit:A.B; unit:A.C", "src/A.cs; src/B.cs")
    expect.equal("the real schema's separators round-trip a canonical record unchanged",
                 (rec["test-ref"], rec["code-location"]), ("unit:A.B; unit:A.C", "src/A.cs; src/B.cs"))



def case_02_a_kept_refs_remark_goes_to_notes(expect):
    """normalize_ref's contract (kb/Work PB6098): a remark after a canonical ref is split off and kept in `notes`,
    a kept ref's as well as a dropped ref's, and a bare `#Method`'s remark too; empty parts are dropped by the schema's
    one splitter (`Schema.split`), never by a copy of it here."""
    rec = _normalize(C.load_schema(), "unit:A.B (corroboration only);; #C (second method); not a ref",
                     "src/A.cs#M (the MAX row);;")
    expect.equal("the kept refs, the bare method inheriting its class, the empty part gone",
                 rec["test-ref"], "unit:A.B; unit:A.C")
    expect.equal("the code-location keeps its path, the empty parts gone", rec["code-location"], "src/A.cs#M")
    expect.equal("every remark and the dropped ref's reason reach notes", rec["notes"],
                 "[ref note: (corroboration only); ref note: (second method); test-ref 'not a ref' did not parse and "
                 "was removed by normalize_batch] [code-location note: (the MAX row)]")

if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
