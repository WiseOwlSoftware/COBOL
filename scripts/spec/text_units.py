#!/usr/bin/env python3
"""THE UNITS OF UNNUMBERED TEXT, for every edition (kb/Work PB5449, decision PB5448).

A numbered rule is a row of the rule catalog; the standard also states rules as prose with no number, as a rule
block's lead-in, and in tables. Decision PB5448 makes the MARGIN PARAGRAPH the unit of that text, a table's unit
follow its kind, and a numbered item of a prose clause a unit by its printed number. This module is the ONE
segmentation of a clause's unnumbered text into those units: the ISO reader (`extract_rule_catalog.py`, 2002 and 2023)
and the 1985 reader (`rule_catalog_1985.py`) each hand it the clause's lines with their own page furniture removed,
and every reader downstream (the catalogs' pending count, `cite.py`'s pending answer, the dispositions audit) reads
what it returns. Design: "Phase A, continued", section 3 of docs/rearchitecture/DESIGN-spec-conformance-review.md.

THE SEGMENTATION (design point 1):
  - Blank lines separate candidate paragraphs. A sentence ends in `.`, `?` or `!`, optionally followed by closing
    brackets or quotes (§2023:8.4.2.1's second paragraph ends "(See 8.4.6, Scope of names.)"). A candidate CONTINUES the
    unit before it when that unit ends no sentence and the candidate opens in lowercase or with a continuation word
    (`CONTINUATION_WORDS`): the 2023 text breaks 42 sentences across a blank line that way
    (`data/paragraph-joins-2023.json`, the join's self-test population).
  - A dash or lettered list item belongs to the unit before it (a unit ending in a colon owns its list); a candidate
    after the list that continues by the rule above is that unit's closing tail (§2023:14.7.5).
  - A numbered item of a prose clause is a unit of its own, keyed by its printed number; its lettered items and indented
    lines are its text. A tail after a numbered list joins the colon unit that leads the list, never the last item.
  - A NOTE or an EXAMPLE stays inside the unit it follows; a general format and code (`<pre>`, a fence) are no unit.
  - A table is never paragraph text. Its kind is proposed by `table_kind` (entries, matrix, data): an entries table or a
    matrix has one unit per body row (`T<t>.<r>`), a data table is one unit (`T<t>`).

THE KEY (design point 3): a unit's disposition is keyed by its clause, the digest of the `cite.norm` form of its WHOLE
text (`digest`) and, among units of one clause whose digests are equal, their order. A repair anywhere in the text
changes the digest, so an old disposition can never be carried to changed text.

    python scripts/spec/text_units.py --self-test
"""
from __future__ import annotations

import dataclasses
import hashlib
import json
import pathlib
import re
import sys
from collections import Counter

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from cite import norm  # noqa: E402  — ONE notion of "the same words" as the citation checker's

REPO = pathlib.Path(__file__).resolve().parents[2]
#: The 2023 text's sentences broken across a plain blank line (kb/Work PB479's design review, `tails.py`): each pair is
#: the line that ends no sentence and the line that continues it. The join rule must keep every one in one unit.
JOINS = REPO / "scripts" / "spec" / "data" / "paragraph-joins-2023.json"

#: Where a sentence ends: a stop, then any closing brackets or quotes (§2023:8.4.2.1 "(See 8.4.6, Scope of names.)").
SENTENCE_END = re.compile(r"[.?!][)\]\"'’”*]*\s*$")
#: A unit that leads a list: it ends in a colon (closing emphasis allowed).
LEADS = re.compile(r":[*\s]*$")
#: The closed list of words a continuation may open with, besides any lowercase opening (design point 1).
CONTINUATION_WORDS = ("and", "or", "nor", "but", "then", "where", "unless", "otherwise")
CONTINUES = re.compile(rf"^(?:[a-z]|(?i:{'|'.join(CONTINUATION_WORDS)})\b)")
#: A dash list item ("— a STOP statement"), and a lettered item ("a)", "a.", "b\\)").
DASH_ITEM = re.compile(r"^\s*[—–-]\s+\S")
LETTERED_ITEM = re.compile(r"^\s*[a-z]\\?[.)]\s")
#: A numbered item at the margin: ISO's `1)` / `1.` (the transcription may escape the parenthesis), 1985's `(1)`.
NUMBERED_ITEM = re.compile(r"^(?:\\?\((?P<p>\d+)\\?\)|(?P<n>\d+)\\?[.)])\s+\S")
#: An informative NOTE or EXAMPLE: it stays inside the unit it follows.
INFORMATIVE = re.compile(r"^\s*(?:\*\*)?(?:NOTE|EXAMPLE)S?\b")
#: A table line, and the caption that names a table ("**Table 12 — Procedural statements**").
TABLE_LINE = re.compile(r"^\s*\|")
TABLE_RULE = re.compile(r"^\s*\|(?:\s*:?-{3,}:?\s*\|)+\s*$")
CAPTION = re.compile(r"^\s*(?:\*\*)?Table\s+(?P<t>\d+)\b")
#: Markup that is no text: an anchor, an HTML comment line, an image.
MARKUP = re.compile(r"^\s*(?:<a\s+id=\"[^\"]*\"></a>|<!--.*-->|!\[)\s*$")
#: A general format or code: `<pre …>` … `</pre>`, or a ``` fence.
PRE_OPEN, PRE_CLOSE = re.compile(r"^\s*>?\s*<pre\b"), re.compile(r"</pre>\s*$")
FENCE = re.compile(r"^\s*>?\s*```")

#: The three kinds of table (design point 1): `entries` (each body row states a determination about the subject its
#: first cell names), `matrix` (both axes are categories, the cells take a few values), `data` (values a rule consumes).
TABLE_KINDS = ("entries", "matrix", "data")
#: The most distinct values a matrix's cells take ("Yes", "No", "X", blank ...).
MATRIX_VALUES = 6


@dataclasses.dataclass(frozen=True)
class Unit:
    """One unit of a clause's unnumbered text: a margin paragraph (`p<n>`), a numbered item of a prose clause (its printed
    number), a body row of an entries table or a matrix (`T<t>.<r>`) or a whole data table (`T<t>`)."""
    label: str
    shape: str                 # "paragraph", "item", "table-row" or "table"
    text: str
    line: int                  # the 1-based line its text starts on
    table: int = 0             # the table's number (printed, or its position in the clause when it prints none)
    table_kind: str = ""       # entries, matrix or data (a table unit's)

    @property
    def digest(self) -> str:
        return digest(self.text)


def digest(text: str) -> str:
    """The key of a unit's text: the digest of its `cite.norm` form, so typography and spacing never move it and any
    change of words does (design point 3)."""
    return hashlib.sha1(norm(text.replace("\\", "")).encode("utf-8")).hexdigest()[:12]


def keyed(units: list[Unit]) -> list[tuple[Unit, int]]:
    """Each unit with its ORDER among the clause's units of equal digest (0 for the first): the third part of its key."""
    seen: Counter[str] = Counter()
    out = []
    for u in units:
        out.append((u, seen[u.digest]))
        seen[u.digest] += 1
    return out


def ends_sentence(text: str) -> bool:
    return bool(SENTENCE_END.search(text))


def continues(prev: str, line: str) -> bool:
    """Does a candidate opening with `line` continue a unit whose text is `prev` (design point 1)?"""
    return not ends_sentence(prev) and not LEADS.search(prev) and bool(CONTINUES.match(line.strip()))


def _cells(line: str) -> list[str]:
    return [c.strip() for c in line.strip().strip("|").split("|")]


def table_kind(header: list[str], body: list[list[str]]) -> str:
    """The kind a table's SHAPE proposes (its disposition confirms it): a MATRIX when both axes are categories (its
    first header cell is an empty corner, or half its column heads are also row names) and its cells beyond the first
    column take at most `MATRIX_VALUES` distinct values; ENTRIES when its first column's cells are distinct names;
    otherwise DATA."""
    if not body:
        return "data"
    values = {c for row in body for c in row[1:]}
    first = [row[0] for row in body if row]
    columns = [h.strip("* ") for h in header[1:]]
    shared = sum(c in {f.strip("* ") for f in first} for c in columns) * 2 >= len(columns)
    if len(header) > 2 and (not header[0].strip("* ") or shared) and len(values) <= MATRIX_VALUES:
        return "matrix"
    if all(first) and len(set(first)) == len(first) and len(header) > 1:
        return "entries"
    return "data"


def segment(lines: list[tuple[int, str]], items: bool = True) -> list[Unit]:
    """The units of a clause's unnumbered text. `lines` is (line number, text) of the clause's body, its page furniture
    already removed by the edition's reader (a removed line is simply absent). `items`: whether a numbered item at the
    margin is a unit of its own (a prose clause's, design point 1); when False the clause's numbered list is held by a
    block disposition of its content-keyed manifest (or is a rule list), and its items and their text are no unit."""
    paras: list[list] = []         # [kind, first line, [text lines], number or table data]
    lead: int | None = None        # the paragraph that leads the open numbered list
    in_item = False                # inside a numbered item (its lettered items and indented lines are its text)
    held = False                   # inside a numbered item that is no unit (`items` False)
    fresh = True
    table: list[tuple[int, str]] = []
    caption: tuple[int, int] | None = None   # (table number, line) of the caption naming the next table
    tables = 0
    verbatim = False               # inside a <pre> or a fence: no text

    def close_table() -> None:
        nonlocal table, caption, tables
        if table:
            tables += 1
            t = caption[0] if caption else tables
            paras.append(["table", table[0][0], table, t])
            table, caption = [], None

    for no, line in lines:
        s = line.strip()
        if verbatim:
            if PRE_CLOSE.search(line) or (FENCE.match(line) and not PRE_OPEN.match(line)):
                verbatim = False
            fresh = True
            continue
        if PRE_OPEN.match(line):
            verbatim = not PRE_CLOSE.search(line)
            fresh = True
            continue
        if FENCE.match(line):
            verbatim = True
            fresh = True
            continue
        if TABLE_LINE.match(line):
            table.append((no, line))
            fresh = True
            continue
        close_table()
        if not s or MARKUP.match(line):
            fresh = True
            continue
        if (c := CAPTION.match(line)) and s.endswith(("**", s.split()[-1])) and len(s.split()) <= 16 \
                and not ends_sentence(s.rstrip("*")):
            caption = (int(c.group("t")), no)
            fresh = True
            continue
        n = NUMBERED_ITEM.match(line)
        if fresh and n:
            in_item, fresh = True, False
            if paras and paras[-1][0] == "paragraph" and lead is None and LEADS.search(" ".join(paras[-1][2])):
                lead = len(paras) - 1
            held = not items
            if not held:
                paras.append(["item", no, [s], n.group("p") or n.group("n")])
            continue
        if fresh:
            prev = paras[-1] if paras else None
            joins_list = DASH_ITEM.match(line) or LETTERED_ITEM.match(line) or line.startswith((" ", "\t"))
            if in_item and joins_list:
                if not held:
                    prev[2].append(s)
                fresh = False
                continue
            if in_item and not held and continues(" ".join(prev[2]), s):
                # The list's closing tail belongs to the unit that leads the list, never to its last item (design
                # point 1, §2023:14.7.5); an item with no lead keeps its own continuation.
                paras[lead if lead is not None else -1][2].append(s)
                fresh = False
                continue
            if held and joins_list:
                fresh = False
                continue
            in_item, held = False, False
            if prev and prev[0] == "paragraph" and (
                    INFORMATIVE.match(line) or joins_list or continues(" ".join(prev[2]), s)):
                prev[2].append(s)
                fresh = False
                continue
            if INFORMATIVE.match(line) and prev:
                if prev[0] != "table":
                    prev[2].append(s)
                fresh = False
                continue
            lead = None
            paras.append(["paragraph", no, [s], None])
            fresh = False
            continue
        if held:
            continue
        if paras and paras[-1][0] != "table":
            paras[-1][2].append(s)
    close_table()

    units: list[Unit] = []
    p = 0
    for kind, no, body, extra in paras:
        if kind == "paragraph":
            p += 1
            units.append(Unit(f"p{p}", "paragraph", _text(body), no))
        elif kind == "item":
            units.append(Unit(extra, "item", _text(body), no))
        else:
            units += table_units(extra, body)
    return units


def _text(body: list[str]) -> str:
    return re.sub(r"\s+", " ", " ".join(body)).strip()


def table_units(t: int, rows: list[tuple[int, str]]) -> list[Unit]:
    """The units of table `t` (its `|` lines): one per body row of an entries table or a matrix, one for a data table.
    The header is the first row, and the rule line under it is no row."""
    body_lines = [(no, ln) for no, ln in rows if not TABLE_RULE.match(ln)]
    if not body_lines:
        return []
    header, body = _cells(body_lines[0][1]), body_lines[1:]
    kind = table_kind(header, [_cells(ln) for _, ln in body])
    if kind == "data" or not body:
        text = " ".join(" ".join(_cells(ln)) for _, ln in body_lines)
        return [Unit(f"T{t}", "table", _text([text]), body_lines[0][0], t, kind)]
    return [Unit(f"T{t}.{r}", "table-row", _text([" | ".join(_cells(ln))]), no, t, kind)
            for r, (no, ln) in enumerate(body, start=1)]


def tables_named(text: str) -> set[int]:
    """The printed table numbers a row's text names ("Table 16", "table 16 ... specifies")."""
    return {int(m) for m in re.findall(r"\btables?\s+(\d+)\b", text, re.I)}


# ── THE DISPOSITIONS (decision PB5448 point 3): the content-keyed manifest's `paragraph-blocks` ─────────────────────
#
# Keyed by clause (as the manifest's `blocks` are), each clause holding its units: `{"unit": "p3", "digest": "…",
# "order": 0, "disposition": "pending"}`. `unit` is the unit's label when it was entered; the KEY is (digest, order), so a
# repair that renumbers paragraphs is seen as a move, and a repair of a unit's words as an undisposed unit beside an
# orphaned disposition, never as the old disposition carried to new text.

#: A disposition that makes the unit a ROW of its kind (§2023:5.3.1: syntax and general rules, argument and returned-value
#: rules in an intrinsic function's clause; DOC for an implementor-documentation item, as A.1's rows are).
ROW_KINDS = ("SR", "GR", "AR", "RV", "DOC")
PENDING, RESTATES, NOT_A_RULE = "pending", "restates", "not-a-rule"
#: The closed vocabulary of `not-a-rule`, each with a mechanical test (decision PB5448 point 3).
NOT_A_RULE_CLASSES = ("lead-in", "cross-reference", "data")
DISPOSITIONS = ROW_KINDS + (PENDING, RESTATES, NOT_A_RULE)


def entries_of(blocks: dict[str, dict]) -> dict[str, list[dict]]:
    """The disposition entries of a `paragraph-blocks` map, by clause (a `_comment` key documents the map)."""
    return {clause: info.get("units", []) for clause, info in blocks.items() if not clause.startswith("_")}


def audit(units: dict[str, list[Unit]], blocks: dict[str, dict], label, kinds) -> list[str]:
    """THE DISPOSITIONS AUDIT of one edition: every refusal of `units` (the segmentation, by clause) against `blocks` (its
    manifest's `paragraph-blocks`). `label(clause)` prints a clause; `kinds(clause)` is the set of row kinds a unit of that
    clause may take. Refused: a unit with no entry; an entry no unit has (its key, or the label it was entered under, is
    gone); an entry for a unit whose label moved; a disposition outside the vocabulary; a row kind the clause does not
    take; and a `restates` or `not-a-rule`, which needs the UPHELD refuter record decision PB5448 requires and this
    landing does not yet read (kb/Work PB5449 item 4), so none may be recorded until it does."""
    out: list[str] = []
    entries = entries_of(blocks)
    for clause in sorted(units.keys() | entries.keys()):
        have = {(u.digest, order): u for u, order in keyed(units.get(clause, []))}
        seen: set[tuple[str, int]] = set()
        for e in entries.get(clause, []):
            key = (e.get("digest", ""), e.get("order", 0))
            where = f"{label(clause)} {e.get('unit', '?')}"
            d = e.get("disposition")
            if key in seen:
                out.append(f"{where}: two entries for one unit (digest {key[0]}, order {key[1]})")
            seen.add(key)
            if (u := have.get(key)) is None:
                out.append(f"{where}: an entry no unit has (digest {key[0]}): its text changed or left the clause; "
                           "decide the unit again")
            elif u.label != e.get("unit"):
                out.append(f"{where}: the unit moved to {u.label}; regenerate the manifest (a move of a decided unit "
                           "moves its row: kb/Work PB5449 item 6)")
            if d not in DISPOSITIONS:
                out.append(f"{where}: disposition {d!r} is not one of {', '.join(DISPOSITIONS)}")
            elif d in ROW_KINDS and d not in kinds(clause):
                out.append(f"{where}: kind {d} is not a kind of this clause ({', '.join(sorted(kinds(clause)))}; "
                           "§2023:5.3.1)")
            elif d in (RESTATES, NOT_A_RULE):
                out.append(f"{where}: {d} needs an UPHELD refuter record of its digest (decision PB5448 point 3), "
                           "which the audit does not read yet (kb/Work PB5449 item 4)")
        out += [f"{label(clause)} {u.label}: a unit with no disposition entry (digest {u.digest}); regenerate the "
                f"manifest, which enters it pending"
                for (dg, order), u in have.items() if (dg, order) not in seen]
    return out


def regenerated(units: dict[str, list[Unit]], blocks: dict[str, dict], title) -> dict[str, dict]:
    """`blocks` (a `paragraph-blocks` map) after a regeneration over `units`: every unit with no entry is entered
    `pending`, a pending entry that matches no unit leaves, every entry takes its unit's current label, and a DECIDED
    entry that matches no unit is KEPT, so the audit refuses it until a person decides the changed text again. Clauses
    keep the manifest's order; a new clause follows in the segmentation's order. `title(clause)` names a new clause."""
    out: dict[str, dict] = {k: v for k, v in blocks.items() if k.startswith("_")}
    entries = entries_of(blocks)
    for clause in list(entries) + [c for c in units if c not in entries]:
        old = {(e.get("digest"), e.get("order", 0)): e for e in entries.get(clause, [])}
        new, used = [], set()
        for u, order in keyed(units.get(clause, [])):
            e = old.get((u.digest, order))
            used.add((u.digest, order))
            new.append({**(e or {}), "unit": u.label, "digest": u.digest, "order": order,
                        "disposition": (e or {}).get("disposition", PENDING)})
        new += [e for k, e in old.items() if k not in used and e.get("disposition") != PENDING]
        if not new:
            continue
        info = {k: v for k, v in blocks.get(clause, {"title": title(clause)}).items() if k != "units"}
        out[clause] = {**info, "units": new}
    return out


def pending(blocks: dict[str, dict]) -> int:
    """How many units of a `paragraph-blocks` map await a disposition: outside the denominator, counted."""
    return sum(e.get("disposition") == PENDING for es in entries_of(blocks).values() for e in es)


# ── self-test ──────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    bad: list[str] = []

    def ok(cond: bool, what: str) -> None:
        if not cond:
            bad.append(what)

    def seg(text: str, items: bool = True) -> list[Unit]:
        return segment(list(enumerate(text.split("\n"), start=1)), items)

    # A ".)" ending ends a sentence: §2023:8.4.2.1 reads as three paragraphs, not two.
    u = seg("First paragraph. (See 8.3.2.2, User-defined words.)\n\nThen certain conventions apply. (See 8.4.6, "
            "Scope of names.)\n\nThe same name shall not be used both as the name of an external record.")
    ok([x.label for x in u] == ["p1", "p2", "p3"], f"'.)' ends a sentence: {[x.label for x in u]}")
    # A dash list belongs to its colon unit, and so does a closing tail that continues it (§2023:14.6.2.2, 14.7.5).
    u = seg("An instance remains active until the execution of one of the following:\n\n— a STOP statement\n\n"
            "— a GOBACK statement\n\nand processing proceeds as specified in 14.6.13.1.3.\n\nWhenever an instance is "
            "activated, the control mechanisms are set to their initial states.")
    ok(len(u) == 2 and "STOP" in u[0].text and "proceeds" in u[0].text and u[1].label == "p2",
       f"a dash list and its tail are the colon unit's: {[(x.label, x.text[:30]) for x in u]}")
    # A numbered prose item is a unit by its printed number; a tail after the list is the lead's, not the last item's.
    u = seg("The following are implementor-defined:\n\n1) the first item\n\na) its sub-item\n\n2) the second item\n\n"
            "and these apply to every element.\n\nA new paragraph.")
    ok([x.label for x in u] == ["p1", "1", "2", "p2"] and "every element" in u[0].text and "sub-item" in u[1].text,
       f"numbered items and the list's tail: {[(x.label, x.text) for x in u]}")
    u = seg("The following are implementor-defined:\n\n1) the first item\n\n2) the second item\n\nA new paragraph.",
            items=False)
    ok([x.label for x in u] == ["p1", "p2"] and "first" not in u[0].text,
       f"a held numbered list is no unit: {[(x.label, x.text) for x in u]}")
    # A NOTE stays inside the unit it follows; a general format is no unit.
    u = seg("The rule applies.\n\nNOTE This is informative.\n\n<pre style=\"line-height:1\">\nFORMAT LINE\n</pre>\n\n"
            "Another rule applies.")
    ok([x.label for x in u] == ["p1", "p2"] and "informative" in u[0].text and "FORMAT" not in u[1].text,
       f"a NOTE stays, a format goes: {[(x.label, x.text) for x in u]}")
    # A sentence broken across a blank line, opening in lowercase or with a continuation word, is one unit.
    u = seg("the item is moved to\n\nwhere length is the length of\n\nor the literal.\n\nA new one.")
    ok([x.label for x in u] == ["p1", "p2"], f"lowercase and continuation-word joins: {[(x.label, x.text) for x in u]}")
    u = seg("A sentence that stops short\n\nBut a capital continuation word joins it.\n\nThis one does not join.")
    ok([x.label for x in u] == ["p1", "p2"], f"a continuation word joins: {[(x.label, x.text) for x in u]}")
    # Tables, by kind: entries (a row per statement), matrix (a corner cell, few values), data (one unit).
    entries = ("**Table 12 — Procedural statements**\n\n| **Statement name** | **Conditional phrase** | **Terminator** |\n"
               "|---|---|---|\n| ACCEPT | [NOT] ON EXCEPTION | END-ACCEPT |\n| ALLOCATE | | |\n"
               "| ADD | [NOT] ON SIZE ERROR | END-ADD |\n| CALL | [NOT] ON EXCEPTION | END-CALL |\n"
               "| CANCEL | | |\n| CLOSE | | |\n| COMMIT | | |")
    u = seg("Statement names are identified in Table 12.\n\n" + entries)
    ok([x.label for x in u] == ["p1"] + [f"T12.{r}" for r in range(1, 8)] and u[1].table_kind == "entries"
       and u[2].text.startswith("ALLOCATE"), f"an entries table: {[(x.label, x.table_kind) for x in u]}")
    u = seg("| | Alphabetic | Numeric | National |\n|---|---|---|---|\n| Alphabetic | Yes | No | Yes |\n"
            "| Numeric | No | Yes | No |")
    ok([x.label for x in u] == ["T1.1", "T1.2"] and u[0].table_kind == "matrix", f"a matrix: {[(x.label, x.table_kind) for x in u]}")
    u = seg("**Table 1 — Character repertoire**\n\n| Code | Character |\n|---|---|\n| 0041 | A |\n| 0041 | A |")
    ok([x.label for x in u] == ["T1"] and u[0].table_kind == "data", f"a data table: {[(x.label, x.table_kind) for x in u]}")
    # The key: equal digests are told apart by order; any change of words moves the digest, typography does not.
    u = seg("The set statement is unsuccessful and\nthe index is unchanged.\n\nThe set statement is unsuccessful and\n"
            "the index is unchanged.")
    ok([o for _, o in keyed(u)] == [0, 1] and u[0].digest == u[1].digest, "equal digests are keyed by order")
    ok(digest("The  “same” words.") == digest("The same words") and digest("The same words") != digest("The same word"),
       "a digest is of the words")
    ok(tables_named("Table 16 specifies the validity of the move; see tables 3 and 4") == {16, 3},
       "tables_named reads a row's table references")
    # THE DISPOSITIONS: a regeneration enters a new unit pending, drops a pending entry no unit has and keeps a decided
    # one; the audit refuses an undisposed unit, an orphaned or moved entry, a kind the clause does not take, and a
    # `not-a-rule` (no refuter record is read yet), and accepts a current manifest.
    units = {"9.9": seg("The record is locked.\n\nThe lock is released at CLOSE.")}
    label, kinds = str, (lambda clause: {"SR", "GR"})
    fresh = regenerated(units, {}, lambda clause: "Planted")
    ok([e["disposition"] for e in fresh["9.9"]["units"]] == [PENDING, PENDING] and pending(fresh) == 2
       and not audit(units, fresh, label, kinds), f"a regeneration enters every unit pending: {fresh}")
    decided = {"9.9": {"title": "Planted", "units": [dict(fresh["9.9"]["units"][0], disposition="GR"),
                                                    fresh["9.9"]["units"][1]]}}
    repaired = {"9.9": seg("The record is locked.\n\nThe lock is released at CLOSE or COMMIT.")}
    ok(any("no disposition entry" in f for f in audit(repaired, decided, label, kinds))
       and any("an entry no unit has" in f for f in audit(repaired, decided, label, kinds)),
       "a repair of a unit's words orphans its entry and leaves the new text undisposed, both refused")
    again = regenerated(repaired, decided, lambda clause: "Planted")
    ok([e["disposition"] for e in again["9.9"]["units"]] == ["GR", PENDING],
       f"a regeneration keeps a decided entry and drops a pending one that lost its unit: {again}")
    moved = {"9.9": seg("A new first paragraph.\n\nThe record is locked.\n\nThe lock is released at CLOSE.")}
    ok(any("moved to p2" in f for f in audit(moved, decided, label, kinds)), "a decided unit whose label moved is refused")
    wrong = {"9.9": {"units": [dict(fresh["9.9"]["units"][0], disposition="RV"),
                               dict(fresh["9.9"]["units"][1], disposition=NOT_A_RULE, **{"class": "lead-in"})]}}
    found = audit(units, wrong, label, kinds)
    ok(any("kind RV is not a kind of this clause" in f for f in found) and any("refuter record" in f for f in found),
       f"a kind the clause does not take and an unrecorded not-a-rule are refused: {found}")
    # The join population: every sentence the 2023 text breaks across a blank line stays one unit.
    joins = json.loads(JOINS.read_text(encoding="utf-8"))["joins"]
    split = [j for j in joins if len(seg(f"{j['tail']}\n\n{j['head']}")) != 1]
    ok(len(joins) == 42 and not split, f"{len(joins)} joins, {len(split)} split: {split[:3]}")

    for b in bad:
        print(f"  FAIL  {b}")
    print(f"text_units self-test: {'FAIL' if bad else 'OK'} ({len(bad)} failure(s))")
    return 1 if bad else 0


def main() -> int:
    if "--self-test" in sys.argv[1:]:
        return self_test()
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main())
