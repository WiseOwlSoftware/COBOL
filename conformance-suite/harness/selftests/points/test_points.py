#!/usr/bin/env python3
"""The coverage-point catalog of the 1985 general formats (DESIGN-conformance-suite.md section 3.4, kb/Work PB6050): the points of
a planted tree, the committed catalog against the transcription it is read from, and the digest that guards it."""
import json
import sys

import selftest_cases
from scratch_repo import scratch_dir
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/points.py
F = T.formats


def _ids(tree_json, row="1985-FMT-X.1.1"):
    return [p["id"].split("/", 1)[1] for p in T.points_of(row, 1, F.from_json(tree_json))]


def case_01_a_bracket_is_written_and_omitted(expect):
    expect.equal("points", _ids([F.SEQ, [F.WORD, "ADD"], [F.OPT, [F.WORD, "ROUNDED"]]]),
                 ["f1", "f1/omit:ROUNDED", "f1/written:ROUNDED"])


def case_02_each_alternative_is_written_and_a_bracket_of_them_omitted(expect):
    expect.equal("points", _ids([F.OPT, [F.ALT, [F.WORD, "NEXT"], [F.WORD, "PRIOR"]]]),
                 ["f1", "f1/omit:NEXT+PRIOR", "f1/written:NEXT", "f1/written:PRIOR"])


def case_03_an_optional_word_is_written_and_omitted(expect):
    expect.equal("points", _ids([F.SEQ, [F.WORD, "MOVE"], [F.OPTIONAL, "TO"]]), ["f1", "f1/written:TO", "f1/omit:TO"])


def case_04_a_repeated_portion_is_written_twice(expect):
    expect.equal("points", _ids([F.SEQ, [F.WORD, "GO"], [F.REP, [F.OPERAND, "procedure-name"]]]), ["f1", "f1/rep:opnd:procedure-name"])


def case_05_each_choice_indicator_alternative_alone_and_two_out_of_order(expect):
    expect.equal("points", _ids([F.BAR, [F.WORD, "A"], [F.WORD, "B"]]), ["f1", "f1/alone:A", "f1/alone:B", "f1/order:A+B"])


def case_06_a_name_a_format_gives_twice_is_told_apart_by_its_place(expect):
    got = _ids([F.SEQ, [F.OPT, [F.WORD, "ON"]], [F.OPT, [F.WORD, "ON"]]])
    expect.equal("points", got, ["f1", "f1/omit:ON#1", "f1/written:ON#1", "f1/omit:ON#2", "f1/written:ON#2"])


def case_07_an_alternative_that_can_be_empty_is_a_default_and_is_omitted_too(expect):
    got = _ids([F.ALT, [F.WORD, "FIRST"], [F.OPTIONAL, "LAST"]])
    expect("the default is written and omitted", "f1/omit:LAST" in got and "f1/written:LAST" in got, str(got))


def case_08_a_catalog_that_does_not_match_its_digest_is_refused(expect):
    d = scratch_dir("points-")
    path = d / "catalog.json"
    catalog = {"edition": 1985, "rows": {"1985-FMT-X.1.1": [{"format": 1, "page": 1, "tree": [F.WORD, "STOP"],
                                                             "points": T.points_of("1985-FMT-X.1.1", 1, F.from_json([F.WORD, "STOP"]))}]}}
    path.write_text(T.dump(catalog), encoding="utf-8", newline="\n")
    expect.equal("a catalog as written loads", T.load(path)["rows"].keys(), catalog["rows"].keys())
    tampered = json.loads(path.read_text(encoding="utf-8"))
    tampered["rows"]["1985-FMT-X.1.1"][0]["points"][0]["id"] = "1985-FMT-X.1.1/f9"
    path.write_text(json.dumps(tampered), encoding="utf-8", newline="\n")
    try:
        T.load(path)
        expect("a changed point is refused", False, "no error")
    except T.CatalogError:
        expect("a changed point is refused", True)


def case_09_the_committed_catalog_is_what_the_transcription_yields(expect):
    built = T.build()
    expect("the file is the generated one", T.CATALOG.read_text(encoding="utf-8") == T.dump(built), "run points.py --edition 85 --write")


def case_10_every_fmt_row_of_the_1985_catalog_has_a_format_and_every_point_a_unique_id(expect):
    import inventory_view
    data = T.load()
    rows = {r["rule-id"] for r in inventory_view.inventory(T.EDITION) if r["kind"] == "FMT"}
    expect.equal("the rows", set(data["rows"]), rows)
    ids = [p["id"] for fmts in data["rows"].values() for f in fmts for p in f["points"]]
    expect.equal("no id twice", len(ids), len(set(ids)))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
