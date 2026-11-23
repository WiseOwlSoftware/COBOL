#!/usr/bin/env python3
"""A printed general format read into the tree of its alternatives (DESIGN-conformance-suite.md section 3.4, kb/Work PB6050).

The 1985 text's general formats are drawn in the transcription's house style (spec-reconciliation/FIGURE-STYLE.md): box
drawing for the multi-row brackets and braces, typed `[ ] { }` for the one-row ones, `<u>` for the underlined (required)
words, `...` for the ellipsis. A figure is the page's decided figure (kb/Work PB4849, `figures-1985/`: the written block
is the batch's drawing where the refuter agreed and the refuter's own where it did not). This module reads one figure
into a tree of five node kinds and nothing else, so what the notation of the standard means is written once:

    word      a required word: an underlined keyword, a number, a punctuation mark (section 5.2.2: required)
    optional  a keyword printed without underlining: an optional word (section 5.2.3)
    operand   a lower-case metavariable (`identifier-1`): matches one item of an operand's kind, never a keyword
    seq       a sequence
    alt       a brace (section 5.2.6.3: exactly one), a bracket of stacked rows (section 5.2.6.2: at most one),
              or a choice-indicator pair (section 5.2.6.4: any, in any order)
    opt       a bracket (section 5.2.6.2: that portion may be omitted)
    rep       a portion and the ellipsis after it (section 5.2.7: written one or more times)

The ISO 1989:2023 notation clauses are the model: 1985's own notation is its section II.2.1, the same marks. WHERE THE
STRUCTURE COMES FROM, measured from the drawing: a multi-row delimiter pair (`figure_reading.enclosures`) holds the words
between its two glyph columns; a pair set as typed text across lines (`figure_reading.spans`, kb/Work PB4929) holds the
text between its two glyphs in reading order. Inside a bracket, a brace or a pair of choice indicators, rows that do not
share a line are STACKED alternatives (section 5.2.6.1), an alternative holding every item that shares a row with one of
its items; at the top of a format and inside a level-2 box nothing is stacked, a line that wraps continues the format.

Its cases are `selftests/formats/test_formats.py`.
"""
from __future__ import annotations

import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
sys.path.insert(0, str(REPO / "scripts" / "spec"))
sys.path.append(str(REPO / "scripts"))  # scripts/, for standard_streams
import figure_reading as fr  # noqa: E402

WORD, OPTIONAL, OPERAND, SEQ, ALT, OPT, REP, BAR = "word", "optional", "operand", "seq", "alt", "opt", "rep", "bar"
ELLIPSES = ("…", "...", "¼")
#: A format's own label inside its figure (`Format 4:`), which no token of the program corresponds to.
LABEL = re.compile(r"^Format\s*\d+:?$")


@dataclass
class Node:
    kind: str
    text: str = ""
    kids: list["Node"] = field(default_factory=list)
    underlined: bool = False

    def words(self) -> Iterable[str]:
        """The required and optional words of this subtree in reading order."""
        if self.kind in (WORD, OPTIONAL):
            yield self.text
        for k in self.kids:
            yield from k.words()

    def as_json(self) -> list:
        """`[kind, text, kids...]`, the shape the catalog stores."""
        out: list = [self.kind]
        if self.kind in (WORD, OPTIONAL, OPERAND):
            out.append(self.text)
        out.extend(k.as_json() for k in self.kids)
        return out


def from_json(j: list) -> Node:
    kind = j[0]
    if kind in (WORD, OPTIONAL, OPERAND):
        return Node(kind, j[1])
    return Node(kind, kids=[from_json(k) for k in j[1:]])


class Unreadable(Exception):
    """A figure whose notation this reading cannot resolve: a delimiter with no partner, an operand-less group. Raised, never
    guessed past: a catalog built over a misread figure would count points that are not there."""


@dataclass
class Item:
    top: int
    bottom: int
    left: int
    node: Node | None = None          # a word item
    region: "Region | None" = None    # a region item
    ellipsis: bool = False


@dataclass
class Region:
    family: str                       # bracket, brace, bar, paren, box, top
    top: int
    bottom: int
    left: int
    right: int
    span: tuple[int, int, int, int] | None = None   # (open_bottom, open_col, close_top, close_col): a typed pair across lines
    words: set[int] = field(default_factory=set)
    parent: "Region | None" = None
    children: list["Region"] = field(default_factory=list)
    own: list[int] = field(default_factory=list)    # words whose innermost region this is

    def holds(self, r: int, c: int, end: int | None = None) -> bool:
        if self.span is not None:
            ob, oc, ct, cc = self.span
            return (r, c) > (ob, oc) and (r, c) < (ct, cc)
        return self.top <= r <= self.bottom and self.left < c and (end if end is not None else c) <= self.right

    def corners(self) -> tuple[tuple[int, int], tuple[int, int]]:
        if self.span is not None:
            return (self.top, self.left), (self.bottom, self.right)
        return (self.top, self.left + 1), (self.bottom, self.right - 1)

    def size(self) -> tuple[int, int]:
        if self.span is not None:
            return (len(self.words), (self.bottom - self.top + 1) * 1000 + (self.right - self.left))
        return (len(self.words), (self.bottom - self.top + 1) * (self.right - self.left + 1))


def _tokens(lines: list[str]) -> list[tuple[str, int, int, bool]]:
    """(text, row, column, underlined) of every word of the drawing, a trailing `.` or `:` split off as its own word."""
    stop = fr.COLUMN_GLYPHS | fr.RULE_GLYPHS | set(fr.ONE_ROW) | {fr.BAR, " "}
    out = []
    for r, line in enumerate(lines):
        text, under = fr.plain_row(line)
        for m in re.finditer(r"[^" + re.escape("".join(sorted(stop))) + r"]+", text):
            a, w = m.start(), m.group(0)
            tail = []
            while len(w) > 1 and w[-1] in ".:," and not all(ch == "." for ch in w):
                tail.append((w[-1], a + len(w) - 1))
                w = w[:-1]
            out.append((w, r, a, any(i in under for i in range(a, a + len(w)))))
            out.extend((t, r, c, False) for t, c in reversed(tail))
    return out


def _regions(lines: list[str]) -> list[Region]:
    out = []
    for e in fr.enclosures(lines):
        out.append(Region(e.family, e.top, e.bottom, e.left, e.right))
    for s in fr.spans(lines):
        out.append(Region(s.family, s.open_top, s.close_bottom, s.open_col, s.close_col,
                          span=(s.open_bottom, s.open_col, s.close_top, s.close_col)))
    return out


def _contains(q: Region, r: Region) -> bool:
    """Whether `q` holds `r`. A drawn pair holds a pair whose columns lie strictly inside its own and whose rows it covers by at
    least half: the printed page sets a tall brace one row above the bracket it belongs to (PERFORM's AFTER phrase, page 342)."""
    if (q.top, q.left, q.bottom, q.right, q.span) == (r.top, r.left, r.bottom, r.right, r.span):
        return False
    if q.span is None and r.span is None:
        overlap = min(q.bottom, r.bottom) - max(q.top, r.top) + 1
        return q.left < r.left and r.right < q.right and 2 * overlap >= (r.bottom - r.top + 1)
    (r0, c0), (r1, c1) = r.corners()
    return q.holds(r0, c0) and q.holds(r1, c1)


def _w(text: str) -> list:
    return [WORD, text]


def _o(text: str) -> list:
    return [OPTIONAL, text]


def _x(stem: str) -> list:
    return [OPERAND, stem]


def _alt(*kids: list) -> list:
    return [ALT, *kids]


def _opt(*kids: list) -> list:
    return [OPT, kids[0] if len(kids) == 1 else [SEQ, *kids]]


def _seq(*kids: list) -> list:
    return [SEQ, *kids]


def _rep(kid: list) -> list:
    return [REP, kid]


def _evaluate_object() -> list:
    """One selection object of a WHEN phrase: ANY, a condition, TRUE, FALSE, or [NOT] an operand with an optional THROUGH range."""
    operand = _alt(_x("identifier"), _x("literal"), _x("arithmetic-expression"))
    return _alt(_w("ANY"), _x("condition"), _w("TRUE"), _w("FALSE"),
                _seq(_opt(_w("NOT")), operand, _opt(_alt(_w("THROUGH"), _w("THRU")), operand)))


#: Figures whose drawing the reading above cannot take (a typed brace or bracket opened on one line and closed many lines below, set
#: beside drawn strokes), read by hand from the printed page and checked against the drawing's words (`agrees`): `(rule id, format
#: number) -> the tree`. The page is the authority (CLAUDE.md rule 1); each entry names the folio it was read from.
DECIDED_READINGS: dict[tuple[str, int], list] = {
    # EVALUATE, folios VI-83 and VI-84 (PDF page 316): the subjects with their ALSO bracket repeated, then the repeated WHEN groups
    # (each a WHEN phrase with its own ALSO bracket repeated, then imperative-statement-1), WHEN OTHER and END-EVALUATE.
    ("1985-FMT-VI.6.13.2", 1): _seq(
        _w("EVALUATE"),
        _alt(_x("identifier"), _x("literal"), _x("expression"), _w("TRUE"), _w("FALSE")),
        _rep(_opt(_w("ALSO"), _alt(_x("identifier"), _x("literal"), _x("expression"), _w("TRUE"), _w("FALSE")))),
        _rep(_seq(_rep(_seq(_w("WHEN"), _evaluate_object(),
                            _rep(_opt(_w("ALSO"), _evaluate_object())))),
                  _x("imperative-statement"))),
        _opt(_w("WHEN"), _w("OTHER"), _x("imperative-statement")),
        _opt(_w("END-EVALUATE"))),
    # The report group SIGN clause, folio XIII-25 (PDF page 631): the brace's two rows are typed `{LEADING  }` and `{ TRAILING }`.
    ("1985-FMT-XIII.3.17.2", 1): _seq(_opt(_w("SIGN"), _o("IS")), _alt(_w("LEADING"), _w("TRAILING")), _w("SEPARATE"), _o("CHARACTER")),
}


def agrees(lines: list[str], tree: Node) -> bool:
    """Whether a hand-read tree carries the words of the figure it stands for: the same required and optional words and the same
    operand stems, each as often, so a figure that changes under a decided reading refuses it instead of being read past."""
    figure: list[str] = []
    for text, _row, _col, underlined in _drop_label(_tokens(lines)):
        if text in ELLIPSES or set(text) <= set("."):
            continue
        node = _word_node(text, underlined)
        if node.kind in (WORD, OPTIONAL, OPERAND):
            figure.append(f"{node.kind}:{node.text}")
    mine: list[str] = []

    def walk(n: Node) -> None:
        if n.kind in (WORD, OPTIONAL, OPERAND) and n.text not in ("(", ")"):      # a drawn pair of parentheses is read as the two marks
            mine.append(f"{n.kind}:{n.text}")
        for k in n.kids:
            walk(k)
    walk(tree)
    return sorted(figure) == sorted(mine)


def read(lines: list[str]) -> Node:
    """The tree of one figure's lines. Raises `Unreadable` for a delimiter that pairs with nothing."""
    toks = [t for t in _tokens(lines) if not (t[1] == 0 and LABEL.match(t[0]))]
    toks = _drop_label(toks)
    loose = [s for s in fr.strays(lines)]
    spans = fr.spans(lines)
    # every stray delimiter must have been paired into a span; one that is not is a misread figure
    spanned = 2 * len(spans)
    strays_n = len([d for d in loose if d[0] in ("bracket", "brace")])
    other = [d for d in loose if d[0] not in ("bracket", "brace", "bar", "box")]
    if strays_n != spanned or other:
        raise Unreadable(f"{strays_n - spanned} delimiter(s) pair with nothing: {loose[:6]}")
    regions = _regions(lines)
    for q in regions:
        for k, (_, r, c, _u) in enumerate(toks):
            end = c + len(toks[k][0])
            if q.holds(r, c, end):
                q.words.add(k)
    for q in regions:
        cands = [p for p in regions if p is not q and _contains(p, q)]
        q.parent = min(cands, key=lambda p: p.size(), default=None)
        if q.parent is not None:
            q.parent.children.append(q)
    top = Region("top", 0, max(len(lines) - 1, 0), -1, 10**6)
    for q in regions:
        if q.parent is None:
            q.parent = top
            top.children.append(q)
    for k, (_, r, c, _u) in enumerate(toks):
        end = c + len(toks[k][0])
        inner = [q for q in regions if q.holds(r, c, end)]
        (min(inner, key=lambda p: p.size(), default=top)).own.append(k)
    _dissolve_ellipsis_boxes(regions, toks)
    return simplify(_build(top, toks, lines))


def _dissolve_ellipsis_boxes(regions: list[Region], toks) -> None:
    """A level-2 box that holds only an ellipsis (`║ … ║`, the 1985 page's way of marking the repetition level 2 adds) groups
    and chooses nothing: its ellipsis is an item of the region round the box, where it repeats the portion before it."""
    for q in regions:
        if q.family == "box" and not q.children and q.own and all(toks[k][0] in ELLIPSES for k in q.own):
            q.parent.own.extend(q.own)
            q.parent.children.remove(q)
            q.own = []


def simplify(n: Node) -> Node:
    """Drop the empty sequences a period leaves, and a sequence of one item is that item."""
    n.kids = [simplify(k) for k in n.kids]
    if n.kind == SEQ:
        n.kids = [k for k in n.kids if not (k.kind == SEQ and not k.kids)]
        if len(n.kids) == 1:
            return n.kids[0]
    return n


def _drop_label(toks):
    """A box's first row carries `Format 4:` as two words: drop the pair wherever the label's words stand."""
    out, i = [], 0
    while i < len(toks):
        t = toks[i]
        if t[0].lower() == "format" and i + 1 < len(toks) and re.fullmatch(r"\d+:?", toks[i + 1][0]) and toks[i + 1][1] == t[1]:
            i += 2
            if i < len(toks) and toks[i - 1][0].endswith(":") is False and toks[i][0] == ":":
                i += 1
            continue
        out.append(t)
        i += 1
    return out


def _word_node(text: str, underlined: bool) -> Node:
    if text in ELLIPSES:
        raise AssertionError("an ellipsis is not a word")
    if any(ch.islower() for ch in text):
        return Node(OPERAND, re.sub(r"-\d+$", "", text.lower()))
    if set(text) == {"."}:
        return Node(SEQ)                          # the sentence's own period (a drawn `. .`): no token of a statement's text
    if underlined or not re.search(r"[A-Z]", text):
        return Node(WORD, text, underlined=underlined)
    return Node(OPTIONAL, text)


def _items(region: Region, toks, lines) -> list[Item]:
    items = []
    for k in region.own:
        text, r, c, u = toks[k]
        if text in ELLIPSES:
            items.append(Item(r, r, c, ellipsis=True))
        else:
            items.append(Item(r, r, c, node=_word_node(text, u)))
    for q in region.children:
        items.append(Item(q.top, q.bottom, q.left, region=q))
    return items


def _clusters(items: list[Item]) -> list[list[Item]]:
    """Group items whose rows overlap; each group in column order."""
    out: list[list[Item]] = []
    bottom = -1
    for it in sorted(items, key=lambda i: (i.top, i.left)):
        if out and it.top <= bottom:
            out[-1].append(it)
            bottom = max(bottom, it.bottom)
        else:
            out.append([it])
            bottom = it.bottom
    return [sorted(c, key=lambda i: (i.left, i.top)) for c in out]


def _seq(cluster: list[Item], toks, lines) -> Node:
    """One cluster as a sequence; an ellipsis repeats the item before it (section 5.2.7)."""
    kids: list[Node] = []
    for it in cluster:
        if it.ellipsis:
            if not kids:
                raise Unreadable("an ellipsis with no portion before it")
            kids[-1] = Node(REP, kids=[kids[-1]])
        elif it.node is not None:
            kids.append(it.node)
        else:
            kids.append(_region(it.region, toks, lines))
    return kids[0] if len(kids) == 1 else Node(SEQ, kids=kids)


def _sequence_of_clusters(items: list[Item], toks, lines) -> Node:
    """Clusters in reading order as one sequence, a cluster of only ellipses repeating the item before it."""
    kids: list[Node] = []
    for cl in _clusters(items):
        if all(i.ellipsis for i in cl):
            if not kids:
                raise Unreadable("an ellipsis with no portion before it")
            kids[-1] = Node(REP, kids=[kids[-1]])
            continue
        kids.append(_seq(cl, toks, lines))
    return kids[0] if len(kids) == 1 else Node(SEQ, kids=kids)


def _alternatives(items: list[Item], toks, lines, stacked_only: bool) -> Node:
    """Clusters as alternatives (section 5.2.6.1: vertically stacked). A brace's rows are alternatives whatever their columns (section
    5.2.6.3: exactly one of them); the rows of a bracket or of choice indicators that do not begin in one column are one phrase
    wrapped over several lines, a sequence. A cluster of only ellipses repeats the one before."""
    alts: list[Node] = []
    starts: list[int] = []
    for cl in _clusters(items):
        if all(i.ellipsis for i in cl):
            if not alts:
                raise Unreadable("an ellipsis with no portion before it")
            alts[-1] = Node(REP, kids=[alts[-1]])
            continue
        alts.append(_seq(cl, toks, lines))
        starts.append(cl[0].left)
    if stacked_only and len(set(starts)) > 1:
        return Node(SEQ, kids=alts)          # rows that do not begin in one column are one phrase wrapped, not a stack
    return alts[0] if len(alts) == 1 else Node(ALT, kids=alts)


def _build(region: Region, toks, lines) -> Node:
    items = _items(region, toks, lines)
    if not items:
        return Node(SEQ)
    if region.family in ("top", "box", "paren") or region.span is not None:
        body = _sequence_of_clusters(items, toks, lines)
    else:
        body = _alternatives(items, toks, lines, stacked_only=region.family != "brace")
    if region.family == "bracket":
        return Node(OPT, kids=[body])
    if region.family == "bar":
        return Node(BAR, kids=body.kids if body.kind == ALT else [body])
    if region.family == "paren":
        return Node(SEQ, kids=[Node(WORD, "("), body, Node(WORD, ")")])
    return body


def _region(q: Region, toks, lines) -> Node:
    return _build(q, toks, lines)
