#!/usr/bin/env python3
"""The units of unnumbered text on planted text (kb/Work PB5449, decision PB5448 point 1): the segmentation (a ".)"
ending, a colon unit's dash list and closing tail, numbered prose items and a held list, a NOTE and a general format,
the lowercase and continuation-word joins, the three table kinds), the key (digest and order), the dispositions'
regeneration and every refusal of their audit, and the 42 blank-line joins of the 2023 text kept as data."""
import json
import sys

import selftest_cases
from standard_streams import ensure_utf8

U = selftest_cases.subject(__file__)   # scripts/spec/text_units.py
#: The 2023 text's sentences broken across a plain blank line (kb/Work PB479's design review, `tails.py`).
JOINS = U.REPO / "scripts" / "spec" / "data" / "paragraph-joins-2023.json"


def _seg(text: str, items: bool = True) -> list:
    return U.segment(list(enumerate(text.split("\n"), start=1)), items)


def _shown(units: list) -> list:
    return [(u.label, u.text) for u in units]


#: The planted catalog the dispositions are checked against: a numbered rule, a general rule that names Table 4, and
#: §2023:14.5.2's own shape ("The conditional phrases are specified in Table 12"), which reads like a pointer and is a row.
ROWS = {"GR-9.9.4-1": "The lock is held until the file is closed.",
        "GR-9.9.4-2": "The values a key may take are those of Table 4.",
        "GR-14.5.2-p1": "The conditional phrases are specified in Table 12."}
RECORDS = "planted/refutations.jsonl"


def _context(records: list | None = None) -> object:
    """A Context over `ROWS`, the edition-wide verbal forms of 2023, the locators 8.4.6 and 14.5.1, and `records` as the
    one refuter record file `RECORDS`."""
    return U.Context(row_ids=set(ROWS), shapes=U.openings(ROWS.values()), verbal=U.verbal_forms(2023),
                     named=lambda clause, u: u.table == 4, resolves=lambda loc: loc in ("8.4.6", "14.5.1"),
                     records=lambda path: records if path == RECORDS else None)


def _decided(units: dict, disposition: str, **fields) -> dict:
    """`units`' one clause, its every unit given `disposition` and `fields` (a regeneration's entries, decided)."""
    (clause, us), = units.items()
    entries = U.regenerated(units, {}, lambda c: "Planted")[clause]["units"]
    return {clause: {"units": [dict(e, disposition=disposition, **fields) for e in entries]}}


def _upheld(units: dict, outcome: str = "UPHELD") -> list:
    """A refuter record of each unit of `units` at its digest."""
    return [{"unit": u.label, "digest": u.digest, "outcome": outcome, "attempt": "planted", "refuter": "planted"}
            for us in units.values() for u in us]


def case_00_a_sentence_ends_in_closing_brackets(expect):
    # §2023:8.4.2.1 reads as three paragraphs, not two: the second ends "(See 8.4.6, Scope of names.)".
    u = _seg("First paragraph. (See 8.3.2.2, User-defined words.)\n\nThen certain conventions apply. (See 8.4.6, "
             "Scope of names.)\n\nThe same name shall not be used both as the name of an external record.")
    expect.equal("'.)' ends a sentence", [x.label for x in u], ["p1", "p2", "p3"])


def case_01_a_colon_unit_owns_its_list_and_its_tail(expect):
    # §2023:14.6.2.2's dash list, and the closing tail of §2023:14.7.5's list.
    u = _seg("An instance remains active until the execution of one of the following:\n\n— a STOP statement\n\n"
             "— a GOBACK statement\n\nand processing proceeds as specified in 14.6.13.1.3.\n\nWhenever an instance is "
             "activated, the control mechanisms are set to their initial states.")
    expect("a dash list and its tail are the colon unit's",
           len(u) == 2 and "STOP" in u[0].text and "proceeds" in u[0].text and u[1].label == "p2", _shown(u))


def case_02_a_numbered_prose_item_is_a_unit(expect):
    u = _seg("The following are implementor-defined:\n\n1) the first item\n\na) its sub-item\n\n2) the second item\n\n"
             "and these apply to every element.\n\nA new paragraph.")
    expect("items by printed number; the list's tail is the lead's, never the last item's",
           [x.label for x in u] == ["p1", "1", "2", "p2"] and "every element" in u[0].text and "sub-item" in u[1].text,
           _shown(u))
    u = _seg("The following are implementor-defined:\n\n1) the first item\n\n2) the second item\n\nA new paragraph.",
             items=False)
    expect("a list a block disposition holds is no unit", [x.label for x in u] == ["p1", "p2"]
           and "first" not in u[0].text, _shown(u))


def case_03_a_note_stays_and_a_format_goes(expect):
    u = _seg("The rule applies.\n\nNOTE This is informative.\n\n<pre style=\"line-height:1\">\nFORMAT LINE\n</pre>\n\n"
             "Another rule applies.")
    expect("a NOTE stays in the unit it follows; a general format is no unit",
           [x.label for x in u] == ["p1", "p2"] and "informative" in u[0].text and "FORMAT" not in u[1].text, _shown(u))


def case_04_a_broken_sentence_joins(expect):
    u = _seg("the item is moved to\n\nwhere length is the length of\n\nor the literal.\n\nA new one.")
    expect.equal("lowercase openings join an unterminated unit", [x.label for x in u], ["p1", "p2"])
    u = _seg("A sentence that stops short\n\nBut a capital continuation word joins it.\n\nThis one does not join.")
    expect.equal("a continuation word joins, any other capital does not", [x.label for x in u], ["p1", "p2"])
    joins = json.loads(JOINS.read_text(encoding="utf-8"))["joins"]
    split = [j for j in joins if len(_seg(f"{j['tail']}\n\n{j['head']}")) != 1]
    expect("every one of the 2023 text's 42 blank-line breaks stays one unit", len(joins) == 42 and not split,
           f"{len(joins)} joins, split: {split[:3]}")


def case_05_tables_by_kind(expect):
    entries = ("**Table 12 — Procedural statements**\n\n| **Statement name** | **Conditional phrase** | **Terminator** |\n"
               "|---|---|---|\n| ACCEPT | [NOT] ON EXCEPTION | END-ACCEPT |\n| ALLOCATE | | |\n"
               "| ADD | [NOT] ON SIZE ERROR | END-ADD |\n| CALL | [NOT] ON EXCEPTION | END-CALL |\n"
               "| CANCEL | | |\n| CLOSE | | |\n| COMMIT | | |")
    u = _seg("Statement names are identified in Table 12.\n\n" + entries)
    expect("an entries table: a unit per body row, numbered by its caption",
           [x.label for x in u] == ["p1"] + [f"T12.{r}" for r in range(1, 8)] and u[1].table_kind == "entries"
           and u[2].text.startswith("ALLOCATE"), [(x.label, x.table_kind) for x in u])
    u = _seg("| | Alphabetic | Numeric | National |\n|---|---|---|---|\n| Alphabetic | Yes | No | Yes |\n"
             "| Numeric | No | Yes | No |")
    expect("a matrix (a corner cell, few values): a unit per row line, an uncaptioned table by its position",
           [x.label for x in u] == ["T1.1", "T1.2"] and u[0].table_kind == "matrix", [(x.label, x.table_kind) for x in u])
    u = _seg("**Table 1 — Character repertoire**\n\n| Code | Character |\n|---|---|\n| 0041 | A |\n| 0041 | A |")
    expect("a data table is one unit", [x.label for x in u] == ["T1"] and u[0].table_kind == "data",
           [(x.label, x.table_kind) for x in u])
    expect.equal("tables_named reads a row's table references",
                 U.tables_named("Table 16 specifies the validity of the move; see tables 3 and 4"), {16, 3})


def case_06_the_key_is_the_words_and_their_order(expect):
    u = _seg("The set statement is unsuccessful and\nthe index is unchanged.\n\nThe set statement is unsuccessful and\n"
             "the index is unchanged.")
    expect("equal digests are told apart by order", [o for _, o in U.keyed(u)] == [0, 1] and u[0].digest == u[1].digest)
    expect("a digest is of the words, never the typography",
           U.digest("The  “same” words.") == U.digest("The same words") != U.digest("The same word"))


def case_07_the_dispositions_are_regenerated_and_audited(expect):
    units = {"9.9": _seg("The record is locked.\n\nThe lock is released at CLOSE.")}
    label, kinds = str, (lambda clause: {"SR", "GR"})
    fresh = U.regenerated(units, {}, lambda clause: "Planted")
    expect("a regeneration enters every unit pending, and the audit accepts it",
           [e["disposition"] for e in fresh["9.9"]["units"]] == [U.PENDING, U.PENDING] and U.pending(fresh) == 2
           and not U.audit(units, fresh, label, kinds), fresh)
    decided = {"9.9": {"title": "Planted", "units": [dict(fresh["9.9"]["units"][0], disposition="GR"),
                                                    fresh["9.9"]["units"][1]]}}
    repaired = {"9.9": _seg("The record is locked.\n\nThe lock is released at CLOSE or COMMIT.")}
    found = U.audit(repaired, decided, label, kinds)
    expect("a repair of a unit's words orphans its entry and leaves the new text undisposed, both refused",
           any("no disposition entry" in f for f in found) and any("an entry no unit has" in f for f in found), found)
    again = U.regenerated(repaired, decided, lambda clause: "Planted")
    expect.equal("a regeneration keeps a decided entry and drops a pending one that lost its unit",
                 [e["disposition"] for e in again["9.9"]["units"]], ["GR", U.PENDING])
    moved = {"9.9": _seg("A new first paragraph.\n\nThe record is locked.\n\nThe lock is released at CLOSE.")}
    expect("a decided unit whose label moved is refused", any("moved to p2" in f for f in U.audit(moved, decided, label,
                                                                                                  kinds)))
    wrong = {"9.9": {"units": [dict(fresh["9.9"]["units"][0], disposition="RV"),
                               dict(fresh["9.9"]["units"][1], disposition=U.NOT_A_RULE, **{"class": "lead-in"})]}}
    found = U.audit(units, wrong, label, kinds, _context())
    expect("a kind the clause does not take and a not-a-rule with no refuter record are refused",
           any("kind RV is not a kind of this clause" in f for f in found) and any("names no refuter record" in f
                                                                                  for f in found), found)
    twice = {"9.9": {"units": fresh["9.9"]["units"] + [dict(fresh["9.9"]["units"][0])]}}
    expect("two entries for one unit are refused", any("two entries" in f for f in U.audit(units, twice, label, kinds)))
    odd = {"9.9": {"units": [dict(fresh["9.9"]["units"][0], disposition="maybe"), fresh["9.9"]["units"][1]]}}
    expect("a disposition outside the vocabulary is refused",
           any("is not one of" in f for f in U.audit(units, odd, label, kinds)))


def case_08_a_not_a_rule_passes_its_class_test_its_guards_and_its_refuter(expect):
    label, kinds = str, (lambda clause: {"SR", "GR"})

    def refused(text: str, cls: str, records: object = "upheld", clause: str = "9.9") -> list:
        units = {clause: _seg(text)}
        recs = _upheld(units) if records == "upheld" else records
        return U.audit(units, _decided(units, U.NOT_A_RULE, **{"class": cls, "refuted": RECORDS}), label, kinds,
                       _context(recs))

    expect.equal("a lead-in (a colon) on an UPHELD record stands", refused("The size error condition exists in:",
                                                                           "lead-in"), [])
    expect.equal("a lead-in is not guarded: its words are its list's", refused("The following shall apply:", "lead-in"),
                 [])
    expect("a lead-in that ends a sentence fails its class test",
           any("a lead-in ends in a colon" in f for f in refused("The size error condition exists in this case.",
                                                                 "lead-in")))
    expect.equal("a cross-reference whose every sentence is a pointer to a resolving locator stands",
                 refused("See 8.4.6, Scope of names.", "cross-reference"), [])
    expect("a cross-reference with a sentence that is no pointer fails",
           any("is no pointer" in f for f in refused("See 8.4.6. The scope is the run unit.", "cross-reference")))
    expect("a cross-reference to a locator that resolves to nothing fails",
           any("resolves to no clause" in f for f in refused("See 99.9, Nothing.", "cross-reference")))
    expect("THE SHAPE GUARD: a pointer-shaped unit that opens as a row's sentence does is refused (§2023:14.5.2's shape)",
           any("the shape guard" in f for f in refused("The conditional phrases are specified in 14.5.1.",
                                                       "cross-reference")))
    expect("THE VERBAL FORMS: a data unit holding 'the type of this function is' is refused",
           any("verbal form" in f for f in refused("**Table 4 — Types**\n\n| Name | Type |\n|---|---|\n"
                                                   "| X | the type of this function is numeric |", "data")))
    table = "**Table 4 — Key values**\n\n| Value | Meaning |\n|---|---|\n| 00 | none |\n| 00 | none |"
    expect.equal("data a row names (Table 4) stands", refused(table, "data"), [])
    expect("data no row names is a row itself",
           any("no row's text names" in f for f in refused(table.replace("Table 4", "Table 5"), "data")))
    expect("data holds no sentence", any("holds no sentence" in f for f in refused("The values are listed here.", "data")))
    expect("a class outside the vocabulary is refused", any("is not one of" in f for f in refused("A label:", "summary")))
    expect("an OVERTURNED record does not let a not-a-rule stand",
           any("not UPHELD" in f for f in refused("See 8.4.6, Scope of names.", "cross-reference",
                                                  _upheld({"9.9": _seg("See 8.4.6, Scope of names.")}, "OVERTURNED"))))
    expect("a record of other text does not let it stand",
           any("holds no record of this unit" in f for f in refused("See 8.4.6, Scope of names.", "cross-reference",
                                                                    _upheld({"9.9": _seg("See 8.4.6, Names.")}))))
    expect("a record file the tree lacks is refused",
           any("is not in the tree" in f for f in refused("See 8.4.6, Scope of names.", "cross-reference", None)))


def case_09_a_restates_names_existing_rules_and_a_block_its_record(expect):
    label, kinds = str, (lambda clause: {"SR", "GR"})
    units = {"9.9": _seg("The lock is held until the file is closed, as the rule says.")}
    ctx = _context(_upheld(units))
    expect.equal("a restates of an existing rule on an UPHELD record stands",
                 U.audit(units, _decided(units, U.RESTATES, rules=["GR-9.9.4-1"], refuted=RECORDS), label, kinds, ctx), [])
    expect("a restates of a rule the catalog lacks is refused",
           any("rules the catalog lacks: GR-9.9.4-9" in f
               for f in U.audit(units, _decided(units, U.RESTATES, rules=["GR-9.9.4-9"], refuted=RECORDS), label,
                                kinds, ctx)))
    expect("a restates of no rule is refused",
           any("names no rule" in f for f in U.audit(units, _decided(units, U.RESTATES, refuted=RECORDS), label, kinds,
                                                     ctx)))
    expect("a restates with no record is refused",
           any("names no refuter record" in f
               for f in U.audit(units, _decided(units, U.RESTATES, rules=["GR-9.9.4-1"]), label, kinds, ctx)))
    expect("with no context, a decided restates is refused, never passed",
           any("none was given" in f for f in U.audit(units, _decided(units, U.RESTATES, rules=["GR-9.9.4-1"],
                                                                      refuted=RECORDS), label, kinds)))
    lead = {"9.9": _seg("The following apply:")}
    counted = U.class_counts(_decided(lead, U.NOT_A_RULE, **{"class": "lead-in", "refuted": RECORDS}),
                             _context(_upheld(lead)))
    expect.equal("the per-class count holds dispositions and their UPHELD records equal", counted["lead-in"], (1, 1))
    blocks = {"§2023:15.1": {"disposition": "not-rules", "why": "a prose enumeration"}}
    expect.equal("a not-rules block with no record passes while its edition is not refuted yet",
                 U.block_refusals(blocks, _context([]), required=False), [])
    expect("and is refused once the manifest requires its record",
           any("names no refuter record" in f for f in U.block_refusals(blocks, _context([]), required=True)))
    recorded = {"§2023:15.1": dict(blocks["§2023:15.1"], digest="abc", refuted=RECORDS)}
    expect.equal("a not-rules block on its UPHELD record stands",
                 U.block_refusals(recorded, _context([{"unit": "§2023:15.1", "digest": "abc", "outcome": "UPHELD"}]),
                                  required=True), [])
    expect("a block record already given is held to its form before the edition is refuted",
           any("not UPHELD" in f for f in U.block_refusals(recorded, _context(
               [{"unit": "§2023:15.1", "digest": "abc", "outcome": "OVERTURNED"}]), required=False)))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
