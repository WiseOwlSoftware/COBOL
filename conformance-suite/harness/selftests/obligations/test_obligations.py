#!/usr/bin/env python3
"""The 1985 catalog's row obligations on planted rows and tests (DESIGN-conformance-suite.md section 6.1, kb/Work PB6048):
each witness kind, the obligation of a kind, the counts, and the header edit that names an omitted row."""
import sys
from dataclasses import dataclass

import selftest_cases
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/obligations.py


@dataclass
class Flag:
    at: tuple


@dataclass
class Case:
    test_id: str
    rules: tuple
    accept: tuple = ()
    flag: object = None
    in_suite: bool = False
    tree: str = "cases"


def _row(rid, kind="GR", verdict="", ref=""):
    """A planted row with at most ONE test-ref, so `_classify` lists it without splitting on the schema's separator
    (DESIGN-architecture-review §8.2 rule 7: no script keeps a copy of it, kb/Work PB6098)."""
    return {"rule-id": rid, "kind": kind, "verdict": verdict, "test-ref": ref}


def _classify(rows, cases, diagnostics=None, determined=(), derived=()):
    return {f.rule_id: f for f in T.classify(
        rows, cases, diagnostics=diagnostics or {}, determined=lambda r: r["rule-id"] in determined,
        derived=lambda r: r["rule-id"] in derived, test_refs=lambda r: [r["test-ref"]] if r["test-ref"] else [])}


FIXED = ("      *>--- conformance-case\n      *> id: a\n      *> rules: [SR-1-1]\n      *> accept: [85]\n"
         "      *>---\n       X\n")


def case_01_each_witness_kind_is_counted_under_the_first_that_holds(expect):
    rows = [_row("R-ns", verdict=T.NON_SUPPORT), _row("R-suite"), _row("R-det"), _row("R-der"), _row("R-unc"),
            _row("R-ref", "SR"), _row("R-oth", "RV"), _row("R-hdr", "FMT", ref="conformance:cases/x/t-acc"),
            _row("R-late", "AR", ref="conformance:cases/x/t-late2"), _row("R-none", ref="unit:abc"),
            _row("R-shared", ref="conformance:cases/x/t-shared")]
    cases = [Case("t-suite", ("R-suite",), ("85",), in_suite=True), Case("t-unc", ("R-unc",), ("85",)),
             Case("t-neg", ("R-ref",), (), Flag(("85",))), Case("t-late", ("R-oth",), ("2002",)),
             Case("t-acc", ("R-other",), ("85",)), Case("t-late2", ("R-other2",), ("2002",)),
             Case("t-shared", ("R-other3",), ("85", "2002"))]
    got = _classify(rows, cases, determined={"R-det"}, derived={"R-der"})
    want = {"R-ns": "non-support", "R-suite": "suite", "R-det": "determination", "R-der": "derivation",
            "R-unc": "accepted-uncleared", "R-ref": "refusal-only", "R-oth": "other-edition-only",
            "R-hdr": "header-omits", "R-late": "no-test", "R-none": "no-test", "R-shared": "shared-test"}
    for rid, witness in want.items():
        expect.equal(f"{rid} is {witness}", got[rid].witness, witness)
    expect.equal("a header-omits row names the test to repair", got["R-hdr"].repair, ("t-acc",))
    expect.equal("a shared-test row repairs nothing: its test accepts another edition too", got["R-shared"].repair, ())
    expect.equal("an SR row owes an accepted test of the legal form", got["R-ref"].obligation, T.OBLIGATION["SR"])
    expect.equal("the repairs are test id to rows", T.repairs(got.values()), {"t-acc": ["R-hdr"]})


def case_02_the_counts_count_every_row_once(expect):
    rows = [_row("R-ns", verdict=T.NON_SUPPORT), _row("R-der"), _row("R-suite"), _row("R-det"), _row("R-x")]
    got = _classify(rows, [Case("t", ("R-suite",), ("85",), in_suite=True)], determined={"R-det"}, derived={"R-der"})
    c = T.counts(got.values())
    expect.equal("every row is counted once", (c["rows"], sum(c[k] for k in T.WITNESS)), (5, 5))
    expect.equal("unobservable rows are not owed a test", c["observable"], 3)
    expect.equal("the unmet rows exclude the cleared and the determined", c["unmet"], 1)


def case_03_a_wiseowl_refusal_at_the_edition_is_refusal_only(expect):
    got = _classify([_row("R-d")], [Case("t-d", ("R-d",))], diagnostics={"t-d": {"refuse-at": ["85"]}})
    expect.equal("a WiseOwl refusal at 85 is refusal-only", got["R-d"].witness, "refusal-only")


def case_04_an_unknown_row_kind_is_refused(expect):
    try:
        _classify([_row("R-x", "XYZ")], [])
        expect("an unknown row kind is refused", False, "no error")
    except ValueError:
        expect("an unknown row kind is refused", True)


def case_05_a_fixed_form_field_gains_the_id_and_an_id_held_is_not_repeated(expect):
    expect.equal("a one-line fixed-form field gains the id", T.add_rules(FIXED, ["1985-GR-II.1-1"]).split("\n")[2],
                 "      *> rules: [SR-1-1, 1985-GR-II.1-1]")
    expect.equal("an id already named is not repeated", T.add_rules(FIXED, ["SR-1-1"]), FIXED)


def case_06_a_long_field_wraps_inside_its_margin_with_the_continuation_indent(expect):
    long = ", ".join(f"GR-14.9.28.4-{n}" for n in range(1, 7))
    wrapped = T.add_rules(f"*>--- conformance-case\n*> id: a\n*> rules: [{long}]\n*>---\n",
                          [f"1985-GR-II.6.5.{n}-1" for n in range(2, 9)]).split("\n")
    expect("every line stays inside the free-form margin", all(len(ln) <= T.MARGIN["free"] for ln in wrapped[:6]),
           str(wrapped))
    expect("the continuation line carries the indent", wrapped[3].startswith("*>" + T.CONTINUATION), wrapped[3])
    fixed = T.add_rules(FIXED, [f"1985-GR-II.6.5.{n}-1" for n in range(2, 12)]).split("\n")
    expect("a fixed-form line stays inside margin R", all(len(ln) <= T.MARGIN["fixed"] for ln in fixed), str(fixed))


def case_07_a_crlf_file_keeps_its_line_ends(expect):
    out = T.add_rules(FIXED.replace("\n", "\r\n"), ["N-1"])
    expect("no bare LF is written into a CRLF file", "\n" not in out.replace("\r\n", ""))


def case_08_a_header_with_no_rules_field_is_refused(expect):
    try:
        T.add_rules("*>--- conformance-case\n*>---\n", ["N-1"])
        expect("a header with no rules field is refused", False, "no error")
    except ValueError:
        expect("a header with no rules field is refused", True)



def case_09_a_wiseowl_tree_test_is_no_uncleared_witness_and_no_header_to_repair(expect):
    """kb/Work PB6104: the wiseowl/ tree joins no suite, so no clearing pass witnesses a row only it names."""
    rows = [_row("R-wo"), _row("R-mixed"), _row("R-hdr", "FMT", ref="conformance:wiseowl/x/t-wo-alone"),
            _row("R-shared", ref="conformance:wiseowl/x/t-wo-shared")]
    cases = [Case("t-wo", ("R-wo",), ("85",), tree="wiseowl"), Case("t-wo2", ("R-mixed",), ("85",), tree="wiseowl"),
             Case("t-cases", ("R-mixed",), ("85",)), Case("t-wo-alone", ("R-x",), ("85",), tree="wiseowl"),
             Case("t-wo-shared", ("R-y",), ("85", "2002"), tree="wiseowl")]
    got = _classify(rows, cases)
    expect.equal("only a wiseowl-tree test names it", got["R-wo"].witness, "wiseowl-tree-only")
    expect.equal("a cases/ test beside it keeps the row clearable", got["R-mixed"].witness, "accepted-uncleared")
    expect.equal("a wiseowl-tree test-ref is authoring, not a header", got["R-hdr"].witness, "no-test")
    expect.equal("a wiseowl-tree test-ref repairs nothing", got["R-hdr"].repair, ())
    expect.equal("a shared wiseowl-tree test-ref is authoring too", got["R-shared"].witness, "no-test")
    c = T.counts(got.values())
    expect.equal("the kind is counted once and stays observable", (c["wiseowl-tree-only"], c["rows"], c["observable"]), (1, 4, 4))

if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
