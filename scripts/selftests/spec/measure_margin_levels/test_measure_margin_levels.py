#!/usr/bin/env python3
"""The margin levels of the paragraphs after a rule (kb/Work PB5449 item 3, decision PB5448 point 4), on planted data:
the 1985 reader's candidates (a lead-in, a paragraph after a rule, a boxed one, a band label and a continuation that
stay the rule's), the level of a printed line against a page's margin (the margin is the leftmost shared edge, never
the commonest; a first line left of the indent continues the text), the data held to the text without the PDF (a
paragraph the data does not record and a record no paragraph has, each refused), and a level-0 paragraph becoming a
unit numbered after the clause's lead-ins; and (kb/Work PB5998) the best match deciding a level when a sibling's line
matches too, and the page readings of the paragraphs the measurement cannot place, held to the text and the data."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

M = selftest_cases.subject(__file__)   # scripts/spec/measure_margin_levels.py
import rule_catalog_1985 as R  # noqa: E402  (the 1985 reader; scripts/spec is on the path once the subject is loaded)
import text_units as U  # noqa: E402

PLANTED = """# SECTION VI: NUCLEUS MODULE

## Page 257

5.7 LEVEL-NUMBER

5.7.3 Syntax Rules

The rules below apply to every level-number.

(1) A first rule.

a. its first item, and

b. its second item.

NOTE: A note stays inside the rule.

A margin paragraph after rule one.

(2) A second rule ends

with a continuation.

║A boxed paragraph after rule two.║

<u>FORMAT 2</u>:

A paragraph after the band.

VI-25
"""
ROWS = [{"citation": "§1985:VI.5.7.3 SR1", "kind": "SR", "text": "A first rule."},
        {"citation": "§1985:VI.5.7.3 SR2", "kind": "SR", "text": "A second rule ends with a continuation."}]


def _paragraphs():
    return R.block_paragraphs(PLANTED.splitlines(), ROWS)


def case_00_the_1985_reader_finds_the_candidates_after_a_rule(expect):
    leads, after = _paragraphs()
    expect.equal("the lead-in calibrates", [(p.follows, p.unit.text, p.page) for p in leads],
                 [("", "The rules below apply to every level-number.", 257)])
    expect.equal("a paragraph after a rule is a candidate; items, a NOTE, a continuation and a band label stay the rule's",
                 [(p.follows, p.unit.text) for p in after],
                 [("(1)", "A margin paragraph after rule one."), ("(2)", "A boxed paragraph after rule two."),
                  ("(2)", "A paragraph after the band.")])
    expect.equal("a candidate is keyed by its clause, digest and order",
                 after[0].key, ("§1985:VI.5.7.3", U.digest("A margin paragraph after rule one."), 0))


def case_01_a_level_is_read_against_the_page_margin(expect):
    # page 707's shape: four long lines at the margin (81 pt), nine at the indent (99 pt)
    vis = [(81.0, "then any input procedures or output procedures")] * 4 + [(99.0, "(1) Totally within the segments")] * 9 \
        + [(40.0, "a stray line far left of the margin")]
    expect("the margin is the leftmost edge two long lines share, not the commonest", abs(M._margin_of(vis) - 81.0) < 0.01,
           M._margin_of(vis))
    base = 17.7
    expect.equal("level 0 at the first-line indent, deeper at the hanging indents",
                 [M.level_of([(1, off, 1.0, 60)], base) for off in (17.3, 52.6, 0.1)], [0, 2, -1])
    expect.equal("found equally well at two levels, or not found",
                 [M.level_of([(1, 17.0, 1.0, 60), (2, 52.0, 1.0, 60)], base), M.level_of([], base)],
                 [M.AMBIGUOUS, M.NOT_FOUND])


def case_02_the_data_holds_the_text_and_a_level_0_paragraph_is_a_unit(expect):
    _, after = _paragraphs()
    recorded = {p.key: {"clause": p.clause, "follows": p.follows, "level": lv} for p, lv in zip(after, (0, 0, 2))}
    expect.equal("every candidate recorded: no refusal", M.refusals(after, recorded), [])
    missing = dict(recorded)
    del missing[after[2].key]
    orphan = {**recorded, ("§1985:VI.5.7.3", "000000000000", 0): {"follows": "(9)", "level": 0}}
    expect("a candidate the data does not record is refused",
           any("does not record" in r for r in M.refusals(after, missing)), M.refusals(after, missing))
    expect("a record no candidate has is refused", any("no paragraph has" in r for r in M.refusals(after, orphan)))
    margin = M.at_margin(after, recorded)
    expect.equal("the level-0 paragraphs", [p.unit.text for p in margin],
                 ["A margin paragraph after rule one.", "A boxed paragraph after rule two."])
    units = R.units(PLANTED.splitlines(), ROWS, {}, margin)["§1985:VI.5.7.3"]
    expect.equal("a margin paragraph is a unit numbered after the lead-in (n counts every margin paragraph)",
                 [(u.label, u.text) for u in units],
                 [("p1", "The rules below apply to every level-number."), ("p2", "A margin paragraph after rule one."),
                  ("p3", "A boxed paragraph after rule two.")])
    expect.equal("without the data's level 0 a rule block keeps only its lead-in",
                 [u.label for u in R.units(PLANTED.splitlines(), ROWS, {}, [])["§1985:VI.5.7.3"]], ["p1"])
    expect.equal("the tally", M.tally(list(recorded.values()) + [{"level": -1}, {"level": M.NOT_FOUND}]),
                 {"deeper (continues the rule)": 1, "left of the indent (continues the text)": 1,
                  "level 0 (a unit)": 2, "not found (to read on the page)": 1})


def case_03_the_committed_1985_data_holds_the_transcription(expect):
    after = M.READERS[1985]()[1]
    recorded = M.load(1985)
    expect.equal("no refusal of the committed data and readings", M.refusals(after, recorded, M.readings(1985)), [])
    expect("it records level-0 paragraphs", len(M.at_margin(after, recorded)) > 0)
    expect.equal("no paragraph is left to read on the page (kb/Work PB5998)",
                 [(e["clause"], e["follows"], e["level"]) for e in M.unread(recorded)], [])


def case_04_the_best_match_decides_the_level(expect):
    # kb/Work PB5998: page 360's "If index-name-2 is specified" also matches rule (2)'s own opening "If index-name-1 is
    # specified" (at the margin) and its sibling's (at the indent), each less well than its own line.
    base = 17.8
    sibling = [(360, 18.8, 0.90, 60), (360, 48.1, 1.0, 60), (360, 48.5, 0.90, 60)]
    expect.equal("the line that matches best is the paragraph's", M.level_of(sibling, base), 2)
    # page 455: a 29-character line ("the successful execution of an") matches a long opening fully on its few characters.
    short = [(455, 300.2, 1.0, 29), (455, 48.2, 1.0, 60)]
    expect.equal("of equal ratio, the match over more characters", M.level_of(short, base), 2)
    expect.equal("the data's offset is the best match's", M.best(short)[0][1], 48.2)


def case_05_a_page_reading_places_what_the_measurement_cannot(expect):
    _, after = _paragraphs()
    key = after[2].key
    read = {key: {"clause": key[0], "follows": "(2)", "digest": key[1], "order": key[2], "page": 257,
                  "read": "continues the rule", "evidence": "planted"}}
    recorded = {p.key: {"clause": p.clause, "follows": p.follows, "level": lv} for p, lv in zip(after, (0, 0, 2))}
    carried = {**recorded, key: {**recorded[key], "level": M.READ_CONTINUES, "read_on_page": 257}}
    expect.equal("a reading the data carries: no refusal", M.refusals(after, carried, read), [])
    expect("a reading the data does not carry is refused",
           any("does not carry" in r for r in M.refusals(after, recorded, read)), M.refusals(after, recorded, read))
    gone = {("§1985:VI.5.7.3", "000000000000", 0): dict(read[key], digest="000000000000")}
    expect("a reading no paragraph has is refused", any("no paragraph has" in r for r in M.refusals(after, carried, gone)))
    expect.equal("a read level 0 is a unit, a read continuation is none",
                 [p.unit.text for p in M.at_margin(after, {**carried, key: {**carried[key], "level": 0}})],
                 [p.unit.text for p in after])
    expect.equal("the tally names a read continuation", M.tally([{"level": M.READ_CONTINUES}]),
                 {"read on the page: continues the rule": 1})


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
