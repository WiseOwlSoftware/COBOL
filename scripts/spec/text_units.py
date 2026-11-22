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
    lines are its text. An item starts at its own marker, after a blank line or on the line after the item before it
    when its number is the list's next (the 1985 PDF prints a list one item per line, page 152; kb/Work PB5998). A
    clause's first list labels its items by number; its m-th list by `L<m>.<n>`, the form a rule block's later list
    has. A tail after a numbered list joins the colon unit that leads the list, never the last item.
  - A NOTE or an EXAMPLE stays inside the unit it follows; a general format and code (`<pre>`, a fence) are no unit.
  - A table is never paragraph text. Its kind is proposed by `table_kind` (entries, matrix, data): an entries table or a
    matrix has one unit per body row (`T<t>.<r>`), a data table is one unit (`T<t>`). A table printed across pages
    (each page's part under its header again, its caption above or below) is ONE table: a part with the number and
    the header of a table already read continues it, its rows numbered on (§1989:A.2.4's Table 1; kb/Work PB5998).
  - No two units of a clause share a label (their row ids would collide): `segment` refuses such a clause.

THE KEY (design point 3): a unit's disposition is keyed by its clause, the digest of the `cite.norm` form of its WHOLE
text (`digest`) and, among units of one clause whose digests are equal, their order. A repair anywhere in the text
changes the digest, so an old disposition can never be carried to changed text.

    Its cases: scripts/selftests/spec/text_units/test_text_units.py (python scripts/self_tests.py --only <it>).
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
#: A table the transcription holds in HTML markup (the 1985 text prints sixteen so, kb/Work PB6001): `<table>` to
#: `</table>`, each `<tr>` a row and each `<td>`/`<th>` a cell. It is read as a `|` table is (`html_rows`), so the two
#: markups have ONE table reading.
HTML_TABLE_OPEN, HTML_TABLE_CLOSE = re.compile(r"^\s*<table\b"), re.compile(r"</table>\s*$")
HTML_ROW = re.compile(r"<tr\b[^>]*>(?P<row>.*?)</tr>", re.S)
HTML_CELL = re.compile(r"<t[dh]\b[^>]*>(?P<cell>.*?)</t[dh]>", re.S)
#: Markup that is no text: an anchor, an HTML comment line, an image.
MARKUP = re.compile(r"^\s*(?:<a\s+id=\"[^\"]*\"></a>|<!--.*-->|!\[)\s*$")
#: A general format or code: `<pre …>` … `</pre>`, or a ``` fence.
PRE_OPEN, PRE_CLOSE = re.compile(r"^\s*>?\s*<pre\b"), re.compile(r"</pre>\s*$")
FENCE = re.compile(r"^\s*>?\s*```")


def display_mask(lines: list[str]) -> list[bool]:
    """THE DISPLAYS OF A TEXT, ONE READING (kb/Work PB6001): for each line, whether it lies in a display that holds no
    unit text, its opening and closing lines included: a general format or code (`<pre …>` … `</pre>`, or a ``` fence,
    either closed by `</pre>` or a bare fence, as `segment` reads them) or an HTML table (`<table>` … `</table>`). The
    readers that must agree with `segment` on what is no unit text read it here: the 1985 unit spans
    (rule_catalog_1985._spans) and the prose-holes measure (catalog_prose_holes.prose), so a printed code line is never
    a hole the segmentation cannot make a unit of."""
    out: list[bool] = []
    close: re.Pattern[str] | None = None
    for line in lines:
        if close is HTML_TABLE_CLOSE:
            out.append(True)
            close = None if HTML_TABLE_CLOSE.search(line) else close
            continue
        if close is not None:
            out.append(True)
            if PRE_CLOSE.search(line) or (FENCE.match(line) and not PRE_OPEN.match(line)):
                close = None
            continue
        if HTML_TABLE_OPEN.match(line):
            close = None if HTML_TABLE_CLOSE.search(line) else HTML_TABLE_CLOSE
        elif PRE_OPEN.match(line):
            close = None if PRE_CLOSE.search(line) else PRE_CLOSE
        elif FENCE.match(line):
            close = FENCE
        else:
            out.append(False)
            continue
        out.append(True)
    return out

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
    #: Every 1-based line whose text the unit holds (its own lines, never a display or a held list among them): where a
    #: measure finds the unit a printed line is in (catalog_prose_holes, kb/Work PB6001). Not part of its identity.
    lines: tuple[int, ...] = dataclasses.field(default=(), compare=False)

    @property
    def digest(self) -> str:
        return digest(self.text)

    @property
    def ordinal(self) -> int:
        """The `ordinal` of the unit's row: its paragraph number, its item's printed number, its table row's position,
        0 for a whole table."""
        return int(re.sub(r"^(?:p|T\d+\.?|L\d+\.)", "", self.label) or 0)

    @property
    def sublist(self) -> int:
        """The `sublist` of the unit's row: an item's list (`L<m>.<n>` is list m), 1 for every other unit."""
        return int(m[1]) if (m := re.match(r"L(\d+)\.", self.label)) else 1


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


def caption_number(line: str) -> int | None:
    """THE ONE READING OF A TABLE CAPTION (kb/Work PB6015): the number of the table `line` captions ("**Table 12 -
    Procedural statements**", "Table 2: Error Key Values"), or None. A caption is a short title, never a sentence: the
    prose that names a table ("Table 2 shown below points to the appropriate presentation rules ...") is no caption, so
    a citation's `Table <n>` is held to the caption the page prints (cite.py#_resolve_table), as the segmentation is.
    A 1985 caption inside a level-2 box ("║Table 1: Combinations of Conditions, ...║") is a caption too: the box marks
    the level, not the text."""
    s = line.strip().strip("║").strip()
    c = CAPTION.match(s)
    if c is None or len(s.split()) > 16 or ends_sentence(s.rstrip("*")):
        return None
    return int(c.group("t"))


def open_at_end(text: str) -> bool:
    """Does `text` stop where no paragraph can, inside a parenthesis it opened and did not close? The 1985 text breaks a
    sentence there across a page ("... (see page VI-127," / page 247: "The SET Statement). The implementor defines ...";
    kb/Work PB5998). A trailing comma alone is no such stop: Annex B.3's data lines of 2023 end in one at every wrap."""
    return text.count("(") > text.count(")")


def continues(prev: str, line: str) -> bool:
    """Does a candidate opening with `line` continue a unit whose text is `prev` (design point 1)? It does when `prev`
    ends no sentence and leads no list, and the candidate opens in lowercase or with a continuation word, or `prev`
    stops where no paragraph can (`open_at_end`), whatever the candidate opens with."""
    return (not ends_sentence(prev) and not LEADS.search(prev)
            and (bool(CONTINUES.match(line.strip())) or open_at_end(prev)))


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


def html_rows(block: list[tuple[int, str]]) -> list[tuple[int, str]]:
    """An HTML table's rows as `|` table lines, each on the line its `<tr>` opens: the one table reading of both markups
    (kb/Work PB6001). Its first row is its header, as a `|` table's is; a cell's inner markup is kept, as a `|` cell's."""
    text, starts, at = "", [], 0
    for no, line in block:
        starts.append((at, no))
        text += line + "\n"
        at = len(text)
    out = []
    for m in HTML_ROW.finditer(text):
        no = max(n for off, n in starts if off <= m.start())
        cells = [re.sub(r"\s+", " ", c["cell"]).strip() for c in HTML_CELL.finditer(m["row"])]
        out.append((no, "| " + " | ".join(cells) + " |"))
    return out


def segment(lines: list[tuple[int, str]], held: re.Pattern[str] | None = None) -> list[Unit]:
    """The units of a clause's unnumbered text. `lines` is (line number, text) of the clause's body, its page furniture
    already removed by the edition's reader (a removed line is simply absent). `held`: the item marker of the numbered
    lists that are no unit, or None when every numbered item at the margin is a unit of its own (a prose clause's,
    design point 1). A list is held PER LIST, by its marker, never per clause (kb/Work PB6001): a content-keyed block
    disposition holds the list its scan measured (the edition's rule ordinal), and another list of the clause, printed
    in another marker, is units; a rule's own text holds every list (`after_rule`, `NUMBERED_ITEM`)."""
    paras: list[list] = []         # [kind, first line, [text lines], number or table data, [line numbers]]
    lead: int | None = None        # the paragraph that leads the open numbered list
    in_item = False                # inside a numbered item (its lettered items and indented lines are its text)
    is_held = False                # inside a numbered item that is no unit (its marker is `held`)
    number = 0                     # the printed number of the last numbered item
    lists = 0                      # the clause's numbered lists so far (an item of the m-th is `L<m>.<n>`, m > 1)
    fresh = True
    table: list[tuple[int, str]] = []
    html: list[tuple[int, str]] | None = None   # the lines of an open HTML table
    caption: tuple[int, int] | None = None   # (table number, line) of the caption naming the next table
    tables = 0
    uncaptioned: list | None = None  # the table just closed with no caption above it: a caption right below names it
    below = False                  # caption was printed below the table before it
    verbatim = False               # inside a <pre> or a fence: no text

    def close_table() -> None:
        nonlocal table, caption, tables, uncaptioned, below
        if table:
            t = caption[0] if caption else tables + 1
            whole = next((p for p in paras if p[0] == "table" and p[3] == t), None)
            if below and whole is not None and _header(whole[2]) != _header(table):
                # The caption below the table before named THAT table; this one, under another header, is not its
                # part printed on, and is numbered by its position.
                t, whole = tables + 1, None
            # A part with the number and header of a table already read is that table printed on: its rows follow.
            if whole is not None and _header(whole[2]) == _header(table):
                whole[2].extend(_rows(table)[1:])
                uncaptioned = None
            else:
                tables += 1
                paras.append(["table", table[0][0], table, t, [no for no, _ in table]])
                uncaptioned = None if caption else paras[-1]
            table, caption, below = [], None, False

    for no, line in lines:
        s = line.strip()
        if html is not None:
            html.append((no, line))
            if HTML_TABLE_CLOSE.search(line):
                table, html = html_rows(html), None
                close_table()
            continue
        if HTML_TABLE_OPEN.match(line):
            close_table()
            html = [(no, line)]
            if HTML_TABLE_CLOSE.search(line):
                table, html = html_rows(html), None
                close_table()
            fresh = True
            continue
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
        if (t := caption_number(line)) is not None:
            # A caption printed right below a table that had none above names it (the 1985 text captions its tables
            # below them: "Table 2: Page Heading Group Presentation Rules Table", kb/Work PB6001), unless another table
            # of the clause has that number; it is still the caption of a part printed after it (a table continued
            # across pages, its caption below each part: §1989:A.2.4).
            if uncaptioned is not None and paras and paras[-1] is uncaptioned \
                    and not any(p[0] == "table" and p[3] == t for p in paras if p is not uncaptioned):
                uncaptioned[3] = t
                below = True
            else:
                below = False
            uncaptioned = None
            caption = (t, no)
            fresh = True
            continue
        uncaptioned = None
        n = NUMBERED_ITEM.match(line)
        at = int(n.group("p") or n.group("n")) if n else 0
        if n and (fresh or (in_item and at == number + 1)):
            # An item starts at its own marker: after a blank line, or on the next line when its number is the open
            # list's next (the 1985 PDF prints a list one item per line, page 152; kb/Work PB5998).
            if not lists or at != number + 1:
                lists += 1
            in_item, fresh, number = True, False, at
            if paras and paras[-1][0] == "paragraph" and lead is None and LEADS.search(" ".join(paras[-1][2])):
                lead = len(paras) - 1
            is_held = held is not None and held.match(line) is not None
            if not is_held:
                paras.append(["item", no, [s], str(at) if lists == 1 else f"L{lists}.{at}", [no]])
            continue
        if fresh:
            prev = paras[-1] if paras else None
            joins_list = DASH_ITEM.match(line) or LETTERED_ITEM.match(line) or line.startswith((" ", "\t"))
            if in_item and joins_list:
                if not is_held:
                    prev[2].append(s)
                    prev[4].append(no)
                fresh = False
                continue
            if in_item and not is_held and continues(" ".join(prev[2]), s):
                # The list's closing tail belongs to the unit that leads the list, never to its last item (design
                # point 1, §2023:14.7.5); an item with no lead keeps its own continuation.
                owner = paras[lead if lead is not None else -1]
                owner[2].append(s)
                owner[4].append(no)
                fresh = False
                continue
            if is_held and joins_list:
                fresh = False
                continue
            in_item, is_held = False, False
            if prev and prev[0] == "paragraph" and (
                    INFORMATIVE.match(line) or joins_list or continues(" ".join(prev[2]), s)):
                prev[2].append(s)
                prev[4].append(no)
                fresh = False
                continue
            if INFORMATIVE.match(line) and prev:
                if prev[0] != "table":
                    prev[2].append(s)
                    prev[4].append(no)
                fresh = False
                continue
            lead = None
            paras.append(["paragraph", no, [s], None, [no]])
            fresh = False
            continue
        if is_held:
            continue
        if paras and paras[-1][0] != "table":
            paras[-1][2].append(s)
            paras[-1][4].append(no)
    close_table()

    units: list[Unit] = []
    p = 0
    for kind, no, body, extra, nos in paras:
        if kind == "paragraph":
            p += 1
            units.append(Unit(f"p{p}", "paragraph", _text(body), no, lines=tuple(nos)))
        elif kind == "item":
            units.append(Unit(extra, "item", _text(body), no, lines=tuple(nos)))
        else:
            units += table_units(extra, body)
    if twice := sorted(lab for lab, k in Counter(u.label for u in units).items() if k > 1):
        # Two units of one label would mint one row id twice (kb/Work PB5998): an unresolvable segmentation fails.
        raise ValueError(f"units of one clause share the labels {', '.join(twice)} (text from line {lines[0][0]}): "
                         "two tables of one number with different headers, or a list the segmentation cannot number")
    return units


def after_rule(lines: list[tuple[int, str]]) -> list[Unit]:
    """THE CANDIDATE MARGIN PARAGRAPHS a numbered rule's text holds (design point 4; kb/Work PB5449 item 3). `lines` is the
    rule's text as `segment` takes it, its first line the rule's opening with its ordinal removed. The rule's own
    paragraph is the first that `segment` returns, its lettered and dash items, indented lines, NOTEs and continuations
    included by the segmentation's own rules; every LATER paragraph is a candidate (its label is provisional), whose
    printed level, measured on the PDF (`measure_margin_levels.py`), says whether it continues the rule (a level below
    the margin) or is a margin paragraph of the clause (level 0, a unit). A table or a numbered item in a rule's text
    stays the rule's: every list is held (`NUMBERED_ITEM`)."""
    return [u for u in segment(lines, held=NUMBERED_ITEM) if u.shape == "paragraph"][1:]


def _text(body: list[str]) -> str:
    return re.sub(r"\s+", " ", " ".join(body)).strip()


def _rows(table: list[tuple[int, str]]) -> list[tuple[int, str]]:
    """A table's `|` lines but the rule line under its header: the header first, then the body rows."""
    return [(no, ln) for no, ln in table if not TABLE_RULE.match(ln)]


def _header(table: list[tuple[int, str]]) -> list[str]:
    """A table's header cells (normalized, so a page's reprint of the header compares equal)."""
    rows = _rows(table)
    return [norm(c) for c in _cells(rows[0][1])] if rows else []


def table_units(t: int, rows: list[tuple[int, str]]) -> list[Unit]:
    """The units of table `t` (its `|` lines): one per body row of an entries table or a matrix, one for a data table.
    The header is the first row, and the rule line under it is no row."""
    body_lines = _rows(rows)
    if not body_lines:
        return []
    header, body = _cells(body_lines[0][1]), body_lines[1:]
    kind = table_kind(header, [_cells(ln) for _, ln in body])
    if kind == "data" or not body:
        text = " ".join(" ".join(_cells(ln)) for _, ln in body_lines)
        return [Unit(f"T{t}", "table", _text([text]), body_lines[0][0], t, kind, tuple(no for no, _ in body_lines))]
    return [Unit(f"T{t}.{r}", "table-row", _text([" | ".join(_cells(ln))]), no, t, kind, (no,))
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


#: THE BLOCK DISPOSITION that decides a content-keyed clause UNIT BY UNIT (kb/Work PB5452, PB6001): neither `rules` (its
#: numbered list admitted whole) nor `not-rules` (its list kept out whole), but its whole text, every list included,
#: segmented into units wherever the clause lies, each disposed in `paragraph-blocks` like any other. It is the block a
#: refuter overturned whose rule sentences are no item of its list (§1985:III.2's glossary) or whose list holds both
#: restatements and rules (§1985:B.2.1 item 9).
UNITS = "units"


def entries_of(blocks: dict[str, dict]) -> dict[str, list[dict]]:
    """The disposition entries of a `paragraph-blocks` map, by clause (a `_comment` key documents the map)."""
    return {clause: info.get("units", []) for clause, info in blocks.items() if not clause.startswith("_")}


#: The outcome a refuter record must carry for a disposition to stand (kb/Work PB5374's form: the refuter tried to state
#: the obligation the unit imposes that the named rules, or nothing, do not, and failed).
UPHELD = "UPHELD"
#: The closed pointer grammar of a `cross-reference` unit: every sentence is "See <target>." or "<subject> is/are
#: specified/described/defined in <target>." (decision PB5448 point 3). A target is an ISO locator with an optional title
#: ("8.4.6, Scope of names"), or, as the 1985 text points, one or more printed pages each with the title of what it
#: names ("page VI-11, The OBJECT-COMPUTER Paragraph, and page VI-13, The SPECIAL-NAMES Paragraph"; kb/Work PB5998).
#: The 1985 text's own phrasings of a page target are part of the grammar (kb/Work PB5452; every one read in the 1985
#: units its refuters upheld as pure pointers): a paragraph named before its page ("See paragraph 4.4.2, Explicit and
#: Implicit Transfers of Control, page IV-25."), a part named by where it begins ("defined in the glossary beginning on
#: page III-1"), a footnote's marker before "See" and its purpose after the target ("\* See page XIII-25, LINE NUMBER
#: Clause Notation, for a description of the abbreviations used in column 1."), the verb "located" ("Information
#: concerning the File Section is located on page VII-21.": a page with no title is named by the sentence's subject),
#: and a sentence that says the subject is "explained elsewhere in these specifications", whose target another
#: sentence of the unit gives. Page 585 prints a stray second stop after "page VII-6." (rendered), which the
#: transcription keeps: a doubled stop ends such a sentence.
FOLIO = r"(?:[IVX]+|[A-F])-\d+"
_PAGE_TARGET = rf"(?:paragraph [\d.]+, [^.;()]+?, |the [\w -]+? beginning on )?page {FOLIO}(?:, [^.;()]+?)?"
_TARGETS = rf"(?:{_PAGE_TARGET}(?:(?:,? and |; ){_PAGE_TARGET})*|[A-Z0-9][\w.]*(?:,[^.]*?)?)"
POINTER = re.compile(rf"^(?:(?:\\?\*\s*)?\(?[Ss]ee (?P<see>{_TARGETS})(?:, for [^.()]+)?\)?\.\)?"
                     rf"|(?:Information concerning )?(?P<subject>[^.]*?) (?:is|are) "
                     rf"(?:specified|described|defined|located) (?:in|on) (?P<in>{_TARGETS})\.\.?"
                     rf"|[^.]*? (?:is|are) (?:specified|described|defined|explained) elsewhere in these specifications\.)$")
#: One page target of a pointer: its folio and the title it gives (or the paragraph or part it names before the page).
PAGE_TARGET = re.compile(rf"(?:paragraph [\d.]+, (?P<before>[^.;()]+?), |the (?P<part>[\w -]+?) beginning on )?"
                         rf"page (?P<folio>{FOLIO})(?:, (?P<title>[^.;()]+?))?"
                         rf"(?=(?:,? and |; )(?:paragraph |the |page )|$)")
#: Where one sentence of a unit ends and the next begins.
SENTENCE_BREAK = re.compile(r"(?<=[.?!])[)\]\"'’”]*\s+(?=[A-Z(])")
#: How many normalized words of a sentence's opening the SHAPE guard compares (decision PB5448 point 3).
SHAPE_WORDS = 5
#: How many words a label-shaped lead-in may hold (a label with no sentence stop: "The following are reserved words").
LABEL_WORDS = 10


def sentences(text: str) -> list[str]:
    """The sentences of a unit's text, NOTEs included (a NOTE is informative, but the guards read the whole unit)."""
    return [s.strip() for s in SENTENCE_BREAK.split(text) if s.strip()]


def openings(texts) -> set[str]:
    """THE SHAPE GUARD's set: the first `SHAPE_WORDS` normalized words of every sentence of every text in `texts` (every
    row's text, numbered and paragraph rows alike), derived at every audit so a row minted today guards tomorrow."""
    return {" ".join(norm(s).split()[:SHAPE_WORDS]) for t in texts for s in sentences(t)
            if len(norm(s).split()) >= SHAPE_WORDS}


@dataclasses.dataclass
class Context:
    """What the audit reads besides the units and their entries, each from the edition it audits (decision PB5448 point
    3): the catalog's row ids (a `restates` names existing rules); the shape set (`openings` of every row's text); the
    edition's verbal forms (`verbal_forms`); whether a catalog row's text names a unit's clause or table (a `data` unit is
    a row's input only when one does); whether a cross-reference's target resolves (a locator; a printed page only when
    it prints the title the pointer gives, kb/Work PB5998); and the refuter records a file holds (`records(path)`, JSON
    lines, None when the tree has no such file)."""
    row_ids: set[str]
    shapes: set[str]
    verbal: list[re.Pattern[str]]
    named: object        # (clause, Unit) -> bool
    resolves: object     # (locator, title) -> bool
    records: object      # (path) -> list[dict] | None


#: The edition's verbal forms, as data beside the manifests (decision PB5448 point 3): a `not-a-rule` unit holding one
#: is refused.
VERBAL_FORMS = REPO / "scripts" / "spec" / "data" / "verbal-forms.json"


def verbal_forms(edition: int) -> list[re.Pattern[str]]:
    """The verbal forms of `edition` (`VERBAL_FORMS`): its own list and the list every edition shares."""
    data = json.loads(VERBAL_FORMS.read_text(encoding="utf-8"))
    return [re.compile(p, re.I) for p in data["every edition"] + data.get(str(edition), [])]


def pointer_targets(sentence: str) -> list[tuple[str, str]] | None:
    """The targets of a sentence of the pointer grammar (`POINTER`), each (locator, title) in order, or None when the
    sentence is no pointer. A page target's locator is the page's citable locator (`p.VI-13`, `§1985:p.VI-13`); its
    title is the one it gives, the paragraph or part it names before the page, or else the sentence's subject ("the File
    Section is located on page VII-21"); "" when it names none. A sentence that points "elsewhere in these
    specifications" has no target of its own ([])."""
    m = POINTER.match(sentence.strip())
    if m is None:
        return None
    target = m["see"] or m["in"]
    if target is None:
        return []
    if PAGE_TARGET.match(target):
        subject = re.sub(r"^the ", "", (m["subject"] or "").strip(), flags=re.I)
        return [(f"p.{t['folio']}", (t["title"] or t["before"] or t["part"] or subject).strip())
                for t in PAGE_TARGET.finditer(target)]
    locator, _, title = target.partition(",")
    return [(locator.rstrip("."), title.strip())]


def class_refusal(cls: str, u: Unit, clause: str, ctx: Context) -> str | None:
    """Why unit `u` fails the mechanical test of the `not-a-rule` class `cls` (decision PB5448 point 3), or None."""
    words = norm(u.text).split()
    if cls == "lead-in":
        if LEADS.search(u.text) or (len(words) <= LABEL_WORDS and not ends_sentence(u.text)):
            return None
        return "a lead-in ends in a colon, or is a label of at most ten words with no sentence stop"
    if cls == "cross-reference":
        named = 0
        for s in sentences(u.text):
            if (targets := pointer_targets(s)) is None:
                return (f"the sentence {s[:60]!r} is no pointer (\"See <locator>.\", \"See page <folio>, <title>.\", "
                        "\"<subject> is specified in <locator>.\")")
            named += len(targets)
            for locator, title in targets:
                if not ctx.resolves(locator, title):
                    return f"its locator {locator}{f', {title},' if title else ''} resolves to no clause"
        return None if named else "it names no target (\"elsewhere in these specifications\" needs a sentence that does)"
    if cls == "data":
        if u.shape != "table" and any(ends_sentence(s) for s in sentences(u.text)):
            return "a data unit holds no sentence (list or table lines of values)"
        if not ctx.named(clause, u):
            return "no row's text names its clause or table: data no row consumes is a row itself (-T<t> or -C)"
        return None
    return f"class {cls!r} is not one of {', '.join(NOT_A_RULE_CLASSES)}"


def _bare_see(sentence: str) -> bool:
    """Is `sentence` a pointer of the "See <target>." form (no subject of its own)?"""
    return (m := POINTER.match(sentence.strip())) is not None and m["see"] is not None


def guard_refusal(u: Unit, ctx: Context) -> str | None:
    """THE TWO GUARDS on a `not-a-rule` whose class test passed (a lead-in excepted, its words carried by its list): a
    verbal form of the edition, or a sentence opening like a row's (the SHAPE guard)."""
    for s in sentences(u.text):
        if form := next((f for f in ctx.verbal if f.search(s)), None):
            return f"the sentence {s[:60]!r} holds the verbal form /{form.pattern}/"
        if " ".join(norm(s).split()[:SHAPE_WORDS]) in ctx.shapes and not _bare_see(s):
            # A bare "See <target>." states nothing, whatever row's text holds the same pointer; a "<subject> is
            # specified in <locator>." still can open as a row does (§2023:14.5.2's shape) and stays guarded.
            return f"the sentence {s[:60]!r} opens as a row's sentence does (the shape guard)"
    return None


def keyed_to(r: dict, clause: str, e: dict) -> bool:
    """Is refuter record `r` a record of the entry `e` of `clause`? By THE KEY (design point 3): its clause, its digest and
    its order (a record that states none read the first unit of its text, order 0), never by the unit's label, which a
    repair before it in the clause moves (kb/Work PB6001: §1985:XIII.3.10.7's p3 became p2 when its HTML table became a
    table unit, its words and its record unchanged). A block's record names its clause as its `unit`."""
    return (r.get("clause", r.get("unit")) == clause and r.get("digest") == e.get("digest")
            and r.get("order", 0) == e.get("order", 0))


def record_refusal(e: dict, u: Unit | None, clause: str, records) -> str | None:
    """Why the refuter record a `restates` or `not-a-rule` entry `e` of `clause` names does not let it stand, or None: the
    entry names no record file, the file or the record of this unit (`keyed_to`) is missing, its outcome is not UPHELD,
    or the digest it read is not the unit's. `records(path)` reads a record file (`Context.records`)."""
    path = e.get("refuted")
    if not path:
        return "it names no refuter record (`refuted`: the JSON-lines file of an independent refuter's pass)"
    recs = records(path)
    if recs is None:
        return f"its refuter record file {path} is not in the tree"
    mine = [r for r in recs if keyed_to(r, clause, e)]
    if not mine:
        return f"{path} holds no record of this unit at digest {e.get('digest')}"
    if mine[-1].get("outcome") != UPHELD:
        return f"its refuter record is {mine[-1].get('outcome')!r}, not {UPHELD}"
    if u is not None and mine[-1]["digest"] != u.digest:
        return "its refuter record read other text"
    return None


def audit(units: dict[str, list[Unit]], blocks: dict[str, dict], label, kinds, ctx: Context | None = None) -> list[str]:
    """THE DISPOSITIONS AUDIT of one edition: every refusal of `units` (the segmentation, by clause) against `blocks` (its
    manifest's `paragraph-blocks`). `label(clause)` prints a clause; `kinds(clause)` is the set of row kinds a unit of that
    clause may take; `ctx` is what the decided dispositions are checked against (`Context`; None refuses every
    `restates` and `not-a-rule`, which have nothing to be checked against). Refused (decision PB5448 points 3 and 4):
      - a unit with no entry; an entry no unit has (its text changed or left the clause); an entry whose unit's label
        moved; two entries for one unit;
      - a disposition outside the vocabulary; a row kind the clause does not take;
      - a `restates` that names no rule or a rule the catalog lacks; a `not-a-rule` whose class fails its test or, but for
        a lead-in, whose text holds a verbal form or opens like a row (the two guards);
      - either, without an UPHELD refuter record of its digest (`record_refusal`)."""
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
                out.append(f"{where}: the unit moved to {u.label}; regenerate the catalog, which relabels the manifest "
                           "and writes the move batch of a decided row for record_verdicts.py (text_units.moves)")
            if d not in DISPOSITIONS:
                out.append(f"{where}: disposition {d!r} is not one of {', '.join(DISPOSITIONS)}")
            elif d in ROW_KINDS and d not in kinds(clause):
                out.append(f"{where}: kind {d} is not a kind of this clause ({', '.join(sorted(kinds(clause)))}; "
                           "§2023:5.3.1)")
            elif d in (RESTATES, NOT_A_RULE):
                if ctx is None:
                    out.append(f"{where}: {d} is checked against the catalog and its refuter record, and none was given")
                    continue
                if d == RESTATES:
                    named = e.get("rules") or []
                    if not named or (missing := [r for r in named if r not in ctx.row_ids]):
                        out.append(f"{where}: restates names " + (f"rules the catalog lacks: {', '.join(missing)}"
                                                                  if named else "no rule"))
                elif u is not None:
                    cls = e.get("class", "")
                    why = class_refusal(cls, u, clause, ctx) or (guard_refusal(u, ctx) if cls != "lead-in" else None)
                    if why:
                        out.append(f"{where}: not-a-rule ({cls}) refused: {why}")
                if why := record_refusal(e, u, clause, ctx.records):
                    out.append(f"{where}: {d} refused: {why}")
        out += [f"{label(clause)} {u.label}: a unit with no disposition entry (digest {u.digest}); regenerate the "
                f"manifest, which enters it pending"
                for (dg, order), u in have.items() if (dg, order) not in seen]
    if ctx is not None:
        out += [f"not-a-rule ({cls}): {n} disposition(s) and {k} UPHELD record(s) keyed to them; the two are equal"
                for cls, (n, k) in class_counts(blocks, ctx).items() if n != k]
    return out


def class_counts(blocks: dict[str, dict], ctx: Context) -> dict[str, tuple[int, int]]:
    """Per `not-a-rule` class, (dispositions, UPHELD records keyed to them): the audit holds the two equal (decision
    PB5448 point 3), so a record can never stand for two units nor a unit for none."""
    out: dict[str, list[int]] = {c: [0, 0] for c in NOT_A_RULE_CLASSES}
    for clause, es in entries_of(blocks).items():
        for e in es:
            if e.get("disposition") == NOT_A_RULE and e.get("class") in out:
                out[e["class"]][0] += 1
                recs = ctx.records(e.get("refuted") or "") or []
                out[e["class"]][1] += any(keyed_to(r, clause, e) and r.get("outcome") == UPHELD for r in recs)
    return {c: (n, k) for c, (n, k) in out.items()}


def block_refusals(declared: dict[str, dict], ctx: Context, required: bool,
                   measured: dict[str, dict] | None = None) -> list[str]:
    """The same question one level up (decision PB5448 point 3): a content-keyed `blocks` entry disposed `not-rules` (a
    whole numbered list kept out of the denominator) names an UPHELD refuter record of its clause (`refuted`, keyed by
    `unit` = the clause and `digest` = the entry's `digest`). `required` is the manifest's own `not-rules-refuted`:
    false until the step that refutes the edition's blocks (2023 PB5450, 1985 PB5452) sets it, so a record already
    given is held to its form and a missing one is refused only from then on. `measured` is the content-keyed scan's
    reading of the text (clause => {..., digest}): an entry whose `digest` is not the one the text has now was refuted
    against other text, and is refused (kb/Work PB5452)."""
    out = []
    for clause, info in declared.items():
        if info.get("disposition") != "not-rules" or (not required and not info.get("refuted")):
            continue
        if why := block_refusal(clause, info, ctx.records, (measured or {}).get(clause, {}).get("digest")):
            out.append(f"{clause}: not-rules refused: {why}")
    return out


def block_refusal(clause: str, info: dict, records, now: str | None) -> str | None:
    """Why the `not-rules` block `info` of `clause` does not stand on its refuter record, or None: the record is missing or
    not UPHELD (`record_refusal`, the record keyed by the clause and the block's digest), or the text now digests to
    `now`, not the digest its record read. THE ONE TEST of a block's record: the dispositions audit (`block_refusals`)
    and the prose-holes measure (catalog_prose_holes, kb/Work PB6001) both ask it."""
    why = record_refusal({"unit": clause, "digest": info.get("digest"), "refuted": info.get("refuted")}, None, clause,
                         records)
    if why is None and now is not None and now != info.get("digest"):
        why = f"its text now digests to {now}, not the {info.get('digest')} its record read: refute it again"
    return why

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


def moves(units: dict[str, list[Unit]], blocks: dict[str, dict], row_id) -> list[tuple[str, str]]:
    """THE MOVES of a regeneration (decision PB5448 point 4; kb/Work PB5449 item 6), read BEFORE `regenerated` relabels
    `blocks`: for every DECIDED row unit (a `ROW_KINDS` disposition) whose text is where it was (its digest and order)
    and whose label moved, because a repair split, joined or re-levelled a paragraph before it, (its row's old id, its
    new id), in the order of the manifest. `row_id(kind, clause, label)` spells the edition's id. The regeneration hands
    them to `rule_moves.write_move_batch`, and the view refuses the moved records until `record_verdicts.py` applies it."""
    out = []
    for clause, es in entries_of(blocks).items():
        have = {(u.digest, order): u for u, order in keyed(units.get(clause, []))}
        for e in es:
            u = have.get((e.get("digest"), e.get("order", 0)))
            if (d := e.get("disposition")) in ROW_KINDS and u is not None and u.label != e.get("unit"):
                out.append((row_id(d, clause, e.get("unit")), row_id(d, clause, u.label)))
    return out


def pending(blocks: dict[str, dict]) -> int:
    """How many units of a `paragraph-blocks` map await a disposition: outside the denominator, counted."""
    return sum(e.get("disposition") == PENDING for es in entries_of(blocks).values() for e in es)
