#!/usr/bin/env python3
"""The units of unnumbered text on planted text (kb/Work PB5449, decision PB5448 point 1): the segmentation (a ".)"
ending, a colon unit's dash list and closing tail, numbered prose items and a held list, a NOTE and a general format,
the lowercase and continuation-word joins, the three table kinds), the key (digest and order), the dispositions'
regeneration and every refusal of their audit, and the 42 blank-line joins of the 2023 text kept as data; and the three
defects kb/Work PB5998 fixed: a numbered list printed one item per line and two lists in one clause, a table continued
across pages, and 1985's folio pointer ("See page VI-13, <title>.")."""
import json
import sys

import selftest_cases
from standard_streams import ensure_utf8

U = selftest_cases.subject(__file__)   # scripts/spec/text_units.py
#: The 2023 text's sentences broken across a plain blank line (kb/Work PB479's design review, `tails.py`).
JOINS = U.REPO / "scripts" / "spec" / "data" / "paragraph-joins-2023.json"


def _seg(text: str, held=None) -> list:
    return U.segment(list(enumerate(text.split("\n"), start=1)), held)


def _shown(units: list) -> list:
    return [(u.label, u.text) for u in units]


#: The planted catalog the dispositions are checked against: a numbered rule, a general rule that names Table 4, and
#: §2023:14.5.2's own shape ("The conditional phrases are specified in Table 12"), which reads like a pointer and is a row.
ROWS = {"GR-9.9.4-1": "The lock is held until the file is closed.",
        "GR-9.9.4-2": "The values a key may take are those of Table 4.",
        "GR-14.5.2-p1": "The conditional phrases are specified in Table 12."}
RECORDS = "planted/refutations.jsonl"


#: The planted pages a folio pointer may name, each with the titles it prints (kb/Work PB5998).
PAGES = {"p.VI-11": ("The OBJECT-COMPUTER Paragraph",), "p.VI-13": ("The SPECIAL-NAMES Paragraph",),
         "p.VII-21": ("File Section",), "p.III-1": ("glossary",),
         "p.IV-25": ("Explicit and Implicit Transfers of Control",)}


def _context(records: list | None = None) -> object:
    """A Context over `ROWS`, the edition-wide verbal forms of 2023, the locators 8.4.6 and 14.5.1 and the folios of
    `PAGES` with their titles, and `records` as the one refuter record file `RECORDS`."""
    return U.Context(row_ids=set(ROWS), shapes=U.openings(ROWS.values()), verbal=U.verbal_forms(2023),
                     named=lambda clause, u: u.table == 4,
                     resolves=lambda loc, title="": loc in ("8.4.6", "14.5.1") or title in PAGES.get(loc, ()),
                     records=lambda path: records if path == RECORDS else None)


def _decided(units: dict, disposition: str, **fields) -> dict:
    """`units`' one clause, its every unit given `disposition` and `fields` (a regeneration's entries, decided)."""
    (clause, us), = units.items()
    entries = U.regenerated(units, {}, lambda c: "Planted")[clause]["units"]
    return {clause: {"units": [dict(e, disposition=disposition, **fields) for e in entries]}}


def _upheld(units: dict, outcome: str = "UPHELD") -> list:
    """A refuter record of each unit of `units` at its digest."""
    return [{"unit": u.label, "clause": clause, "digest": u.digest, "outcome": outcome, "attempt": "planted",
             "refuter": "planted"} for clause, us in units.items() for u in us]


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
             held=U.NUMBERED_ITEM)
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
    # THE MOVE BATCH (kb/Work PB5449 item 6): read before the relabel, a decided ROW's old and new id; a pending unit
    # that moved has no row and no record, so it moves nothing.
    expect.equal("a moved decided row is a move; a moved pending unit is none",
                 U.moves(moved, decided, lambda k, c, lab: f"{k}-{c}-{lab}"), [("GR-9.9-p1", "GR-9.9-p2")])
    relabelled = U.regenerated(moved, decided, lambda clause: "Planted")
    expect.equal("after the regeneration nothing moves and the audit accepts the labels",
                 (U.moves(moved, relabelled, lambda k, c, lab: lab),
                  [f for f in U.audit(moved, relabelled, label, kinds) if "moved" in f]), ([], []))
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


def case_10_a_numbered_list_printed_one_item_per_line(expect):
    # kb/Work PB5998 1: §1985:IV.4.2.2.1.1 prints its two lists one item per line (the 1985 PDF, page 152), with no blank
    # line between items; each item is a unit, and the clause's second list is told from its first.
    u = _seg("The types of user-defined words are:\n\n1. alphabet-name\n2. cd-name\n3. class-name\n\n"
             "The user-defined words are grouped into the following disjoint sets:\n\n1. alphabet-names\n2. cd-names\n\n"
             "All user-defined words belong to one set.")
    expect.equal("each item at its own marker; a later list's items are L<m>.<n>", _shown(u),
                 [("p1", "The types of user-defined words are:"), ("1", "1. alphabet-name"), ("2", "2. cd-name"),
                  ("3", "3. class-name"), ("p2", "The user-defined words are grouped into the following disjoint sets:"),
                  ("L2.1", "1. alphabet-names"), ("L2.2", "2. cd-names"), ("p3", "All user-defined words belong to one set.")])
    expect.equal("an item's ordinal and list, as a numbered rule's",
                 [(getattr(x, "ordinal", None), getattr(x, "sublist", None)) for x in u[1:7]],
                 [(1, 1), (2, 1), (3, 1), (2, 1), (1, 2), (2, 2)])
    u = _seg("The following apply:\n\n(1) the first item\n(2) the second item\n\nA last paragraph.")
    expect.equal("1985's (n) marker likewise", [x.label for x in u], ["p1", "1", "2", "p2"])
    u = _seg("The following apply:\n\n1) the first item, which runs on\n1985 was the year of the standard.\n2) the next")
    expect.equal("a line whose number does not continue the list is the item's text", _shown(u),
                 [("p1", "The following apply:"), ("1", "1) the first item, which runs on 1985 was the year of the standard."),
                  ("2", "2) the next")])
    u = _seg("The following apply:\n\n1. first\n2. second\n\nA paragraph between items.\n\n3. third")
    expect.equal("a list resumed after a paragraph at its next number is the same list", [x.label for x in u],
                 ["p1", "1", "2", "p2", "3"])
    u = _seg("Held:\n\n1. first\n2. second\n\nAfter.", held=U.NUMBERED_ITEM)
    expect.equal("a held list's items one per line are still no unit", _shown(u), [("p1", "Held:"), ("p2", "After.")])


def case_11_a_table_continued_across_pages_numbers_its_rows_on(expect):
    # kb/Work PB5998 2: §1989:A.2.4's Table 1 prints on three pages, each repeating its header, its caption below it.
    head = "| FUNCTION-NAME | ARGUMENTS | TYPE |\n|---|---|---|\n"
    u = _seg("Table 1 summarizes the functions.\n\n" + head + "| ACOS | N1 | N |\n| ASIN | N1 | N |\n\n"
             "Table 1: Table of Functions\n\n" + head + "| COS | N1 | N |\n| LOG | N1 | N |\n\n"
             "Table 1: Table of Functions (Continued)\n\n" + head + "| SIN | N1 | N |\n\n"
             "Table 1: Table of Functions (Continued)")
    expect.equal("one table, its rows numbered on across the pages", [(x.label, x.text.split()[0]) for x in u],
                 [("p1", "Table"), ("T1.1", "ACOS"), ("T1.2", "ASIN"), ("T1.3", "COS"), ("T1.4", "LOG"), ("T1.5", "SIN")])
    expect("the continued table's kind is read over all its rows", {x.table_kind for x in u[1:]} == {"entries"},
           [x.table_kind for x in u])
    u = _seg("**Table 13 — Statements**\n\n| Name | Phrase |\n|---|---|\n| ACCEPT | x |\n\n**Table 13 — Statements "
             "(continued)**\n\n| Name | Phrase |\n|---|---|\n| WRITE | y |")
    expect.equal("a caption above each page's part (2002's Table 13)", [x.label for x in u], ["T13.1", "T13.2"])
    try:
        got = _seg("**Table 4 — One**\n\n| A | B |\n|---|---|\n| x | 1 |\n\n**Table 4 — Other**\n\n| C | D | E |\n"
                   "|---|---|---|\n| y | 2 | 3 |")
    except ValueError as e:
        got = str(e)
    expect("two tables of one number and different headers are refused, never given one id twice",
           isinstance(got, str) and "T4.1" in got, got)


def case_12_a_1985_folio_pointer_is_a_cross_reference(expect):
    # kb/Work PB5998 3: the 1985 text points by folio; the pointer resolves when the page prints the title it gives.
    label, kinds = str, (lambda clause: {"SR", "GR"})

    def refused(text: str) -> list:
        units = {"9.9": _seg(text)}
        return U.audit(units, _decided(units, U.NOT_A_RULE, **{"class": "cross-reference", "refuted": RECORDS}), label,
                       kinds, _context(_upheld(units)))

    expect.equal("(See page VI-13, <title>.) stands", refused("(See page VI-13, The SPECIAL-NAMES Paragraph.)"), [])
    expect.equal("and its lowercase form with the stop outside", refused("(see page VI-13, The SPECIAL-NAMES Paragraph)."),
                 [])
    expect.equal("two folios in one pointer", refused("See page VI-11, The OBJECT-COMPUTER Paragraph, and page VI-13, The "
                                                      "SPECIAL-NAMES Paragraph."), [])
    expect.equal("'<subject> is specified on page <folio>, <title>.'",
                 refused("The figurative constants are specified on page VI-13, The SPECIAL-NAMES Paragraph."), [])
    expect("a title the page does not print resolves to nothing",
           any("resolves to no clause" in f for f in refused("See page VI-13, The OCCURS Clause.")))
    expect("a folio with no title resolves to nothing", any("resolves to no clause" in f for f in refused("See page VI-13.")))
    # The 1985 text's other phrasings of a pointer (kb/Work PB5452), each resolving on its page.
    expect.equal("'<subject> is located on page <folio>.' names the page by its subject (a stray second stop too)",
                 refused("Information concerning the File Section is located on page VII-21.."), [])
    expect.equal("a part named by where it begins", refused(
        "All other types of user-defined words are defined in the glossary beginning on page III-1."), [])
    expect.equal("a footnote's marker and the pointer's purpose", refused(
        "\\* See page VI-13, The SPECIAL-NAMES Paragraph, for a description of the abbreviations used."), [])
    expect.equal("a paragraph named before its page", refused(
        "See paragraph 4.4.2, Explicit and Implicit Transfers of Control, page IV-25."), [])
    expect.equal("'elsewhere in these specifications' with a sentence that names the page", refused(
        "The clauses are described elsewhere in these specifications. (See page VI-13, The SPECIAL-NAMES Paragraph.)"),
        [])
    expect("'elsewhere in these specifications' alone names no target", any("names no target" in f for f in refused(
        "The clauses are described elsewhere in these specifications.")))
    expect("a located page whose subject the page does not print resolves to nothing", any(
        "resolves to no clause" in f for f in refused("Information concerning the OCCURS Clause is located on page VI-13.")))
    targets = getattr(U, "pointer_targets", lambda s: "no pointer_targets")
    expect.equal("the targets a pointer names, in order", targets(
        "See page VI-11, The OBJECT-COMPUTER Paragraph, and page VI-13, The SPECIAL-NAMES Paragraph."),
        [("p.VI-11", "The OBJECT-COMPUTER Paragraph"), ("p.VI-13", "The SPECIAL-NAMES Paragraph")])
    expect.equal("an ISO pointer's target", targets("(See 8.4.6, Scope of names.)"), [("8.4.6", "Scope of names")])
    expect.equal("no pointer, no targets", targets("The scope is the run unit."), None)
    # The extractor's own resolver on the 1985 transcription: the folio is a page region, and the page prints the title.
    import extract_rule_catalog  # noqa: PLC0415  (scripts/spec, on the path text_units put there)
    real = extract_rule_catalog.unit_context(1985, []).resolves
    expect.equal("on the 1985 text, page VI-13 prints The SPECIAL-NAMES Paragraph and not The OCCURS Clause, and a page "
                 "with no title names nothing", (real("p.VI-13", "The SPECIAL-NAMES Paragraph"),
                                                 real("p.VI-13", "The OCCURS Clause"), real("p.VI-13", ""),
                                                 real("p.VI-9999", "The SPECIAL-NAMES Paragraph")),
                 (True, False, False, False))


def case_13_a_sentence_broken_inside_a_parenthesis_joins(expect):
    # kb/Work PB5998: §1985:VI.4.5.4 rule (3) breaks across pages 246 and 247 inside its parenthesis.
    u = _seg("the status of that switch may be altered by a SET statement (see page VI-127,\n\nThe SET Statement). The "
             "implementor defines which external switches can be referenced.\n\nA new paragraph.")
    expect.equal("an unclosed parenthesis joins the next candidate, whatever it opens with", [x.label for x in u],
                 ["p1", "p2"])
    expect("the join keeps both halves", "page VI-127, The SET Statement). The implementor" in u[0].text, _shown(u))
    u = _seg("Hangul: 1100-11FF, FFA0-FFBE,\n\nHanifi_Rohingya: 10D00-10D23")
    expect.equal("a trailing comma alone joins nothing (2023 Annex B.3's data lines)", [x.label for x in u], ["p1", "p2"])


def case_14_a_list_is_held_by_its_marker_not_by_its_clause(expect):
    # kb/Work PB6001 defect 1: §1985:IV.4.2.2.1.3 (page 154) holds a `(n)` list its not-rules block disposes, then a
    # second list printed `1.` one item per line, which no block holds.
    import re  # noqa: PLC0415
    paren = re.compile(r"^\\?\(\d+\\?\)\s")
    u = _seg("Reserved words satisfy the following conditions:\n\n(1) Reserved words do not begin with X.\n\n"
             "(2) Reserved words do not contain one character.\n\nThere are three types of reserved words:\n\n"
             "1. required words\n2. optional words\n3. special purpose words", held=paren)
    expect.equal("the held (n) list is no unit; the clause's other list is units, as its second list", _shown(u),
                 [("p1", "Reserved words satisfy the following conditions:"),
                  ("p2", "There are three types of reserved words:"), ("L2.1", "1. required words"),
                  ("L2.2", "2. optional words"), ("L2.3", "3. special purpose words")])
    expect.equal("a rule's text holds every list", [x.label for x in _seg("A.\n\n1. x\n\n(1) y", held=U.NUMBERED_ITEM)],
                 ["p1"])
    expect.equal("each unit holds its own lines, never a held item's", [x.lines for x in u],
                 [(1,), (7,), (9,), (10,), (11,)])


def case_15_an_html_table_is_a_table_and_a_caption_below_numbers_it(expect):
    # kb/Work PB6001 defect 2: §1985:XIII.3.10.7 holds its Table 2 as an HTML <table>, captioned below it.
    html = ("Table 2 shown below points to the rules.\n\n<table>\n<tr>\n<td>Sequence</td>\n<td>Rule</td>\n</tr>\n"
            "<tr><td>A R</td><td>1</td></tr>\n<tr><td>R</td><td>2</td></tr>\n</table>\n\n"
            "Table 2: Page Heading Group Presentation Rules Table\n\n\\* A note after the table.")
    u = _seg(html)
    expect.equal("the HTML table is table 2 by its caption below, read as a | table; no paragraph holds its markup",
                 [(x.label, x.shape) for x in u], [("p1", "paragraph"), ("T2.1", "table-row"), ("T2.2", "table-row"),
                                                   ("p2", "paragraph")])
    expect.equal("its rows are its <tr> cells, on the line the row opens", [(x.text, x.line) for x in u[1:3]],
                 [("A R | 1", 8), ("R | 2", 9)])
    u = _seg("| A | B |\n|---|---|\n| x | 1 |\n\nTable 3: Below\n\n| C | D | E |\n|---|---|---|\n| y | 2 | 3 |")
    expect.equal("a caption below names the table above it; a table under another header after it is not its part",
                 [x.label for x in u], ["T3.1", "T2.1"])
    expect("and never takes a number another table of the clause has",
           not any(x.label.startswith("T3.") for x in _seg("**Table 3 — A**\n\n| A | B |\n|---|---|\n| x | 1 |\n\n"
                                                          "| C | D |\n|---|---|\n| y | 2 |\n\nTable 3: again")[1:]))


def case_16_a_record_is_keyed_by_clause_digest_and_order_never_by_label(expect):
    # kb/Work PB6001: a repair before a unit (an HTML table that becomes a table unit) moves its label, not its words.
    label, kinds = str, (lambda clause: {"SR", "GR"})
    before = {"9.9": _seg("A first paragraph.\n\nSee 8.4.6, Scope of names.")}
    records = [dict(r, unit="p2") for r in _upheld(before) if r["unit"] == "p2"]
    after = {"9.9": _seg("See 8.4.6, Scope of names.")}
    blocks = _decided(after, U.NOT_A_RULE, **{"class": "cross-reference", "refuted": RECORDS})
    expect.equal("the record read as p2 stands for the same words now p1", U.audit(after, blocks, label, kinds,
                                                                                    _context(records)), [])
    other = [dict(r, clause="9.8") for r in records]
    expect("a record of the same words in another clause does not",
           any("holds no record" in f for f in U.audit(after, blocks, label, kinds, _context(other))))
    second = [dict(r, order=1) for r in records]
    expect("a record of the second unit of those words does not stand for the first",
           any("holds no record" in f for f in U.audit(after, blocks, label, kinds, _context(second))))


def case_17_one_reading_of_the_displays(expect):
    # kb/Work PB6001 (wave 20262132 group a): the measure and the 1985 spans read a display as `segment` does, so a
    # fenced code line is no unit text anywhere; a <pre> or a fence closes on </pre> or a bare fence, a table on </table>.
    text = ["Prose one.", "```", "CODE LINE", "```", "After two.", '<pre style="x">', "FORMAT", "</pre>", "<pre>one</pre>",
            "<table>", "<tr><td>c</td></tr>", "</table>", "End three."]
    expect.equal("every display line, its fences and tags included, and nothing else", U.display_mask(text),
                 [False, True, True, True, False, True, True, True, True, True, True, True, False])
    expect.equal("segment makes no unit of code; the HTML table is read as a table (its unit)",
                 [x.text for x in _seg("\n".join(text))], ["Prose one.", "After two.", "c", "End three."])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
