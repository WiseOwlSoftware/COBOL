#!/usr/bin/env python3
"""The OPTION STRUCTURE of a printed general format, measured: every word with the delimiters that hold it.

WHY THIS EXISTS (kb/Work PB2024). An underlined word is REQUIRED (§5.2.2: "They are required in order to select the
functionality associated with that keyword, subject to the conventions specified in 5.2.6"), and the conventions of
§5.2.6 are what make a portion of a format omissible: brackets (§5.2.6.2, "that portion of the general format may be
omitted") and choice indicators (§5.2.6.4, "zero or more of the alternatives contained within the choice indicators
shall be specified"). So whether an underlined word may be left out is not a property of the word: it is a property
of WHERE it is printed, and which brackets hold it apart from the words around it. `MULTIPLE` in `[ WITH LOCK ON
[ MULTIPLE ] { RECORD / RECORDS } ]` is underlined and omissible, because a bracket of its own holds it; `ON` beside
it is underlined and NOT omissible while `LOCK` is written, because every bracket that holds ON also holds LOCK.
Underlining alone cannot tell those two apart, and that is why the grammar audit needs this structure.

The same structure answers §5.2.7's question, which portion an ellipsis repeats: "scanning right to left, determine
the right bracket or right brace delimiter immediately to the left of the ellipsis; continue scanning right to left
and determine the logically matching left bracket or left brace delimiter". A keyword outside that portion is
written once, however often the portion after it is.

WHERE THE DELIMITERS COME FROM. Three ways, all measured, none drawn by hand:
  * a delimiter that spans SEVERAL printed lines is a vector stem (a bracket, or a choice-indicator bar —
    `figure_geometry.classify_page`) or a Symbol-face glyph stack (a brace, or COBOL's full-height parentheses);
    `render_figure.enclosures` pairs them, the one pairing the rendered figures in specs/ISO_COBOL.md are drawn from;
  * a delimiter ON ONE printed line is set as TEXT — `[ MULTIPLE ]`, `{ identifier-1 }`, `|` — and is paired here,
    on its line, left to right, the way a reader matches parentheses;
  * a word set hard against its delimiter (`[MULTIPLE`, `}...`) is split at the delimiter first.

WHAT A HOLDER HOLDS. A text pair holds the words on its own line between its two delimiters. A vector or glyph pair
holds the words between its two delimiters whose height lies within the band `render_figure.enclosures` states
(`top`, `bottom`), a glyph pair down to its bottom piece's line (`low`).

WHEN TWO WORDS ARE IN SEQUENCE. Inside one holder, words printed on different lines are STACKED ALTERNATIVES
(§5.2.6.1: "Options are indicated in a general format by vertically stacking alternative possibilities"), unless
one of them sits inside a nested delimiter whose height spans the other's line — the alternatives of `{ RECORD /
RECORDS }` are stacked, but the brace as a whole stands in sequence after `WITH LOCK ON`. At the top level of a
format nothing is stacked: a line that wraps continues the format.

    python scripts/spec/figure_options.py --self-test      # measured fixtures; needs no PDF
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys
from typing import Iterable, NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import render_figure  # noqa: E402  (the one pairing of multi-line delimiters, and the one row clustering)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: The families whose content may be left out: §5.2.6.2 brackets, §5.2.6.4 choice indicators.
OMISSIBLE = frozenset({"bracket", "bar"})
#: The families an ellipsis can repeat: §5.2.7 names the "right bracket or right brace delimiter".
REPEATABLE = frozenset({"bracket", "brace"})
#: Delimiters set as text on one printed line.
TEXT_OPEN = {"[": "bracket", "{": "brace"}
TEXT_CLOSE = {"]": "bracket", "}": "brace"}
TEXT_BAR = "|"
#: The three ways the standard sets the §5.2.7 ellipsis (render_figure.TEXT_GLYPHS / ASCII_ELLIPSIS).
ELLIPSIS = "…"
_ELLIPSIS_SPELLINGS = ("...", "…", "¼")
#: A printed line that carries an all-lowercase word is prose, not a diagram line (the audit's `measure` row rule).
_PROSE_WORD = re.compile(r"[a-z]{2,}")
_STRIP = " []{}.,;:()"
#: A format label line: `Format 2 (boolean-compute):`.
_FORMAT_WORD = "Format"


def prose_row(texts: Iterable[str]) -> bool:
    """Whether a printed line is prose or a metavariable line rather than a line of the diagram: it carries a word
    of lowercase letters alone (`the`, `statement`), which no diagram line prints. The ONE spelling of the rule
    (the optional-word audit's `measure` asks it too)."""
    return any(_PROSE_WORD.fullmatch(t.strip(_STRIP)) for t in texts)


class Holder(NamedTuple):
    """One delimiter pair, and the part of its page it encloses."""
    family: str                     # "bracket", "bar", "brace" or "paren"
    page: int
    x_open: float
    x_close: float
    top: float                      # a text pair: its line; a vector or glyph pair: `Enclosure.top`
    bottom: float                   # a text pair: its line; a vector pair: `Enclosure.bottom`; a glyph pair: `low`
    text: bool                      # set as text on one line (then `top` and `bottom` are that line)

    def holds(self, page: int, x0: float, y0: float, row: float) -> bool:
        if page != self.page or not self.x_open < x0 < self.x_close:
            return False
        if self.text:
            return row == self.top
        return self.top <= y0 <= self.bottom

    def span(self) -> tuple[float, float]:
        return self.top, self.bottom


class Word(NamedTuple):
    """One printed word of a diagram line, with the delimiters that hold it."""
    text: str                       # stripped of punctuation and delimiters, `-` kept (`END-COMPUTE`)
    underlined: bool
    page: int
    x0: float
    y0: float
    row: float                      # the printed line it sits on (render_figure's row clustering)
    diagram: int                    # which diagram of the band it is printed in (`read`: a label or prose ends one)
    holders: tuple[Holder, ...]     # outermost first
    repeated: bool                  # §5.2.7: inside the portion an ellipsis repeats, or the word one follows

    def footprint(self, level: int) -> tuple[float, float]:
        """The height this word takes inside its holder at depth `level - 1`: its own line when it is held directly
        there, else the span of the nested holder it sits in."""
        if level < len(self.holders):
            return self.holders[level].span()
        return self.row, self.row


def _pieces(words: list[dict]) -> list[dict]:
    """Split delimiters and ellipses set hard against a word (`[MULTIPLE`, `}...`) into pieces of their own, each
    placed by its share of the word's width; a piece keeps the word's line and, for the word itself, its
    underline. Glyph-fused words are split before this, against the raw text layer (`render_figure.split_fused`)."""
    out: list[dict] = []
    for w in words:
        text = w["text"]
        for spelling in _ELLIPSIS_SPELLINGS:
            text = text.replace(spelling, ELLIPSIS)
        width = (w["x1"] - w["x0"]) / max(len(text), 1)
        lead = len(text) - len(text.lstrip("[{|"))
        core_end = len(text.rstrip("]}|" + ELLIPSIS))
        if core_end < lead:
            core_end = lead
        cuts = [(i, i + 1) for i in range(lead)] + [(lead, core_end)] + [(i, i + 1) for i in range(core_end, len(text))]
        for a, b in cuts:
            piece = text[a:b]
            if not piece:
                continue
            core = (a, b) == (lead, core_end)
            out.append({"text": piece, "x0": w["x0"] + a * width, "x1": w["x0"] + b * width, "y0": w["y0"],
                        "underlined": bool(w["underlined"]) if core else False})
    return out


def _text_holders(page: int, pieces: list[dict], rowof: dict[float, float]) -> tuple[list[Holder], int]:
    """Pair the delimiters set as text, line by line, the way a reader matches parentheses: a closer takes the
    nearest open delimiter of its family, and a bar closes an open bar or opens one. Returns the pairs and how many
    delimiters found no partner (a line the PDF sets oddly; the caller reports the count)."""
    by_row: dict[float, list[dict]] = collections.defaultdict(list)
    for p in pieces:
        by_row[rowof[p["y0"]]].append(p)
    out: list[Holder] = []
    unpaired = 0
    for row, ps in by_row.items():
        stack: list[tuple[str, float]] = []
        for p in sorted(ps, key=lambda p: p["x0"]):
            t = p["text"]
            if t in TEXT_OPEN:
                stack.append((TEXT_OPEN[t], p["x0"]))
            elif t == TEXT_BAR and stack and stack[-1][0] == "bar":
                out.append(Holder("bar", page, stack.pop()[1], p["x0"], row, row, True))
            elif t == TEXT_BAR:
                stack.append(("bar", p["x0"]))
            elif t in TEXT_CLOSE:
                k = next((k for k in range(len(stack) - 1, -1, -1) if stack[k][0] == TEXT_CLOSE[t]), None)
                if k is None:
                    unpaired += 1
                    continue
                unpaired += len(stack) - 1 - k             # an opener skipped over is one that never closes
                family, x_open = stack[k]
                del stack[k:]
                out.append(Holder(family, page, x_open, p["x0"], row, row, True))
        unpaired += len(stack)
    return out, unpaired


class Reading(NamedTuple):
    """One band read: its words, and how many text delimiters found no partner."""
    words: list[Word]
    unpaired: int


def read(pages: Iterable[tuple[int, list[dict], list[dict]]]) -> Reading:
    """Every word of a band's diagram lines, with its holders, its format and whether an ellipsis repeats it.

    `pages` is one (page number, words, stems) per page the band covers, in order: `words` as
    `figure_extract.extract` measures them (glyph-fused words already split), `stems` as
    `figure_geometry.classify_page` classifies the delimiter stems that reach the band."""
    out: list[Word] = []
    unpaired = 0
    diagram = 0
    for page, words, stems in pages:
        pieces = _pieces(words)
        if not pieces:
            continue
        rowof = render_figure.cluster([p["y0"] for p in pieces], render_figure.ROW_TOL, render_figure.LINE_GAP)
        glyphs = [p for p in pieces if p["text"] in render_figure.GLYPH_PIECES]
        holders, lost = _text_holders(page, pieces, rowof)
        unpaired += lost
        for e in render_figure.enclosures(glyphs, stems):
            bottom = e.bottom if e.bottom is not None else e.low + render_figure.ROW_TOL
            holders.append(Holder(e.family, page, e.x_open, e.x_close, e.top, bottom, False))

        def held(p: dict) -> tuple[Holder, ...]:
            hs = [h for h in holders if h.holds(page, p["x0"], p["y0"], rowof[p["y0"]])]
            return tuple(sorted(hs, key=lambda h: (-(h.x_close - h.x_open), h.top)))

        # §5.2.7: the portion an ellipsis repeats is the bracket or brace whose closer stands immediately to its
        # left; a word standing nearer than any such closer is repeated alone (`identifier-1 …`).
        repeated_holders: set[Holder] = set()
        repeated_words: set[int] = set()
        notation = set(TEXT_OPEN) | set(TEXT_CLOSE) | {TEXT_BAR, ELLIPSIS} | set(render_figure.GLYPH_PIECES)
        for e in (p for p in pieces if p["text"] == ELLIPSIS):
            row = rowof[e["y0"]]
            closer = max((h for h in holders if h.family in REPEATABLE and h.x_close <= e["x0"]
                          and h.holds(page, h.x_open + 0.01, e["y0"], row)), key=lambda h: h.x_close, default=None)
            word = max((p for p in pieces if p["text"] not in notation and rowof[p["y0"]] == row
                        and p["x1"] <= e["x0"] + 0.5), key=lambda p: p["x0"], default=None)
            if word is not None and (closer is None or word["x0"] > closer.x_close):
                repeated_words.add(id(word))
            elif closer is not None:
                repeated_holders.add(closer)

        lines: dict[float, list[dict]] = collections.defaultdict(list)
        for p in pieces:
            lines[rowof[p["y0"]]].append(p)
        for row in sorted(lines):
            ps = sorted(lines[row], key=lambda p: p["x0"])
            texts = [p["text"] for p in ps]
            # A `Format N` label, or a line of prose (`where until-phrase is:`), ends one diagram and opens the next:
            # PERFORM's until-phrase prints `UNTIL { condition-1 / EXIT }` below Format 3, and that EXIT is no word
            # of the PERFORM format above it.
            if prose_row(texts) or any(t == _FORMAT_WORD and i + 1 < len(texts) and texts[i + 1].isdigit()
                                       for i, t in enumerate(texts)):
                diagram += 1
                continue
            for p in ps:
                if p["text"] in notation:
                    continue
                hs = held(p)
                out.append(Word(p["text"].strip(_STRIP), p["underlined"], page, p["x0"], p["y0"], row, diagram, hs,
                                id(p) in repeated_words or any(h in repeated_holders for h in hs)))
    return Reading(out, unpaired)


_MARK = {"bracket": "[", "bar": "|", "brace": "{", "paren": "("}


def describe(words: list[Word]) -> list[str]:
    """One line per printed line of the diagram: each word as `_WORD_` when underlined, prefixed by the delimiters
    that hold it, outermost first (`[|_SIZE_` is held by a bracket and then a pair of choice indicators), and
    suffixed `…` when an ellipsis repeats it — the reading the audit joins, to set beside the rendered page."""
    lines: dict[tuple[int, int, float], list[Word]] = collections.defaultdict(list)
    for w in words:
        lines[(w.diagram, w.page, w.row)].append(w)
    out = []
    for (diagram, page, row), ws in sorted(lines.items()):
        shown = ("".join(_MARK[h.family] for h in w.holders) + (f"_{w.text}_" if w.underlined else w.text)
                 + ("…" if w.repeated else "") for w in sorted(ws, key=lambda w: w.x0))
        out.append(f"D{diagram} p{page} y{row:6.1f}  " + "  ".join(shown))
    return out


def _stacked(a: Word, b: Word, h: Holder) -> bool:
    """Whether `a` and `b`, both held by `h`, are stacked alternatives inside it: the heights they take one level
    inside `h` do not overlap."""
    (a0, a1), (b0, b1) = a.footprint(a.holders.index(h) + 1), b.footprint(b.holders.index(h) + 1)
    return a.page != b.page or a1 < b0 or b1 < a0


def in_sequence(a: Word, b: Word) -> bool:
    """Whether two words of one format stand in SEQUENCE rather than as stacked alternatives (see the module
    docstring): inside their innermost common holder they are not stacked."""
    if a.diagram != b.diagram:
        return False
    common = [h for h, g in zip(a.holders, b.holders) if h == g]
    return not common or not _stacked(a, b, common[-1])   # the top level of a format stacks nothing, on any page


class Layout:
    """A band's words indexed by spelling (`-` normalized to `_`, as the audit spells grammar tokens)."""

    def __init__(self, words: list[Word]):
        self.words = words
        self.by_text: dict[str, list[Word]] = collections.defaultdict(list)
        for w in words:
            self.by_text[w.text.replace("-", "_")].append(w)

    def printed(self, spellings: set[str]) -> list[Word]:
        return [w for s in spellings for w in self.by_text.get(s, ())]

    def demands(self, x: Word, y: Word) -> bool:
        """Whether the format demands the printed word `x` wherever `y` is written: `x` is underlined (§5.2.2), stands
        in sequence with `y`, and no holder that holds `x` apart from `y` lets it go — not a bracket or a choice
        indicator (§5.2.6.2, §5.2.6.4), and not a brace in which `x` is one of several stacked alternatives
        (§5.2.6.3: then the brace demands one of them, not `x`)."""
        if not x.underlined or x is y or not in_sequence(x, y):
            return False
        for h in x.holders:
            if h in y.holders:
                continue
            if h.family in OMISSIBLE:
                return False
            if any(h in w.holders and _stacked(x, w, h) for w in self.words):
                return False
        return True

    def demanded_with(self, x: set[str], y: set[str]) -> bool:
        """Whether every printed `y` (in any spelling) has a printed `x` the format demands wherever it is written —
        so a spelling of the construct that writes `y` and leaves `x` out is one the format does not print."""
        ys = self.printed(y)
        xs = self.printed(x)
        return bool(ys) and bool(xs) and all(any(self.demands(a, b) for a in xs) for b in ys)

    def repeatable(self, x: set[str]) -> bool:
        """Whether some printed `x` lies in a portion an ellipsis repeats (§5.2.7), or among choice indicators: §5.2.6.4
        lets one or more of their alternatives be written, in any order, which a context-free grammar can only spell
        as a repetition of the choice (`initializeCategoryName+`); that each is written once is a rule the phase
        after the parser checks, not a shape the grammar can carry."""
        return any(w.repeated or any(h.family == "bar" for h in w.holders) for w in self.printed(x))


# ---- self-test ------------------------------------------------------------------------------------------------
# Measured fixtures: (text, x0, x1, y0, underlined) words and (x, y0, y1, kind, side) stems, replayed from the
# printed page so the self-test needs no PDF (CI and a fresh worktree do not have it), as test_render_figure.py does.

#: §12.4.5.9.2 LOCK MODE clause, PDF page 355 (folio 325), band y 200-245.
LOCK_MODE = (
    [("ì", 160.2, 165.5, 206.3, False), ("ü", 236.3, 241.6, 206.3, False), ("MANUAL", 172.1, 214.0, 207.2, True),
     ("ì", 398.7, 404.0, 208.0, False), ("ü", 461.5, 466.8, 208.0, False), ("RECORD", 410.5, 450.0, 208.9, True),
     ("LOCK", 86.6, 112.0, 214.6, True), ("MODE", 115.1, 144.0, 214.6, False), ("IS", 146.4, 155.1, 214.6, False),
     ("í", 160.2, 165.5, 215.5, False), ("ý", 236.3, 241.6, 215.5, False), ("WITH", 251.7, 278.7, 216.3, False),
     ("LOCK", 281.8, 307.2, 216.3, True), ("ON", 311.1, 325.3, 216.3, True), ("[", 328.4, 332.2, 216.3, False),
     ("MULTIPLE", 335.3, 384.4, 216.3, True), ("]", 387.5, 391.3, 216.3, False), ("í", 398.7, 404.0, 217.2, False),
     ("ý", 461.5, 466.8, 217.2, False), ("AUTOMATIC", 172.1, 230.1, 224.5, True), ("î", 160.2, 165.5, 224.6, False),
     ("þ", 236.3, 241.6, 224.6, False), ("î", 398.7, 404.0, 226.4, False), ("RECORDS", 410.5, 455.3, 226.2, True),
     ("þ", 461.5, 466.8, 226.4, False)],
    [(248.56, 202.24, 244.02, "bracket", "L"), (468.22, 202.24, 244.02, "bracket", "R")])

#: §14.9.8.2 COMPUTE Format 1, PDF page 632, band y 200-300.
COMPUTE = (
    [("Format", 63.5, 97.0, 204.3, False), ("1", 99.4, 105.3, 204.3, False),
     ("(arithmetic-compute):", 107.6, 209.8, 204.3, False),
     ("COMPUTE", 64.5, 111.7, 236.6, True), ("{", 114.7, 118.9, 236.6, False),
     ("identifier-1", 121.2, 173.2, 236.6, False), ("[", 175.5, 179.3, 236.6, False),
     ("rounded-phrase", 182.4, 256.3, 236.6, False), ("]", 259.4, 263.1, 236.6, False),
     ("}", 265.4, 269.6, 236.6, False), ("...", 274.3, 280.8, 236.6, False), ("=", 285.5, 291.5, 236.6, False),
     ("arithmetic-expression-1", 296.1, 406.4, 236.6, False), ("ON", 88.7, 103.0, 259.0, False),
     ("SIZE", 106.1, 126.8, 259.0, True), ("ERROR", 130.6, 163.6, 259.0, True),
     ("imperative-statement-1", 166.7, 275.5, 259.0, False), ("NOT", 88.7, 109.3, 276.3, True),
     ("ON", 112.4, 126.6, 276.3, False), ("SIZE", 129.7, 150.4, 276.3, True), ("ERROR", 153.5, 186.5, 276.3, True),
     ("imperative-statement-2", 189.6, 298.4, 276.3, False), ("[", 75.0, 78.7, 295.3, False),
     ("END-COMPUTE", 81.8, 153.0, 295.3, True), ("]", 156.1, 159.9, 295.3, False)],
    [(81.19, 255.69, 293.27, "bar", None), (304.36, 255.69, 293.27, "bar", None),
     (76.29, 259.43, 291.41, "bracket", "L"), (309.72, 259.43, 291.41, "bracket", "R")])


def fixture(case) -> Layout:
    words, stems = case
    ws = [{"text": t, "x0": x0, "x1": x1, "y0": y0, "underlined": u} for t, x0, x1, y0, u in words]
    ss = [{"x": x, "y0": y0, "y1": y1, "h": y1 - y0, "kind": k, "side": s} for x, y0, y1, k, s in stems]
    return Layout(read([(1, ws, ss)]).words)


def self_test() -> int:
    """⛔ A CHECK THAT HAS NEVER BEEN SEEN TO FAIL IS NOT EVIDENCE: each arm is the shape the audit relies on, on a
    measured figure, with its omissible or repeated twin."""
    ok = True

    def check(name: str, cond: bool, detail: object = "") -> None:
        nonlocal ok
        print(f"  {'ok  ' if cond else 'FAIL'} {name}" + ("" if cond else f"  (got {detail})"))
        ok &= cond

    lock = fixture(LOCK_MODE)
    on = lock.printed({"ON"})
    check("LOCK MODE: ON is held by the outer bracket alone", [[h.family for h in w.holders] for w in on]
          == [["bracket"]], [w.holders for w in on])
    check("PB755: the format demands ON wherever RECORD is written (one bracket holds both)",
          lock.demanded_with({"ON"}, {"RECORD"}) and lock.demanded_with({"ON"}, {"RECORDS"}))
    check("…and wherever the inner LOCK is written, but not the LOCK of LOCK MODE (a bracket holds ON apart)",
          not lock.demanded_with({"ON"}, {"LOCK"})
          and any(lock.demands(o, w) for o in on for w in lock.printed({"LOCK"})))
    check("MULTIPLE is held by a bracket of its own, so nothing demands it (§5.2.6.2)",
          not lock.demanded_with({"MULTIPLE"}, {"RECORD"}) and not lock.demanded_with({"MULTIPLE"}, {"LOCK"}))
    check("WITH is not underlined, so nothing demands it (§5.2.3)", not lock.demanded_with({"WITH"}, {"LOCK"}))
    check("RECORD and RECORDS are stacked alternatives of one brace, never in sequence",
          not in_sequence(lock.printed({"RECORD"})[0], lock.printed({"RECORDS"})[0]))
    check("MANUAL demands nothing of RECORD: the brace's alternatives and the bracket stand apart",
          not lock.demanded_with({"MANUAL"}, {"RECORD"}))

    comp = fixture(COMPUTE)
    check("COMPUTE: every word is in the one diagram after its Format 1 label", {w.diagram for w in comp.words} == {1},
          {w.diagram for w in comp.words})
    check("SIZE inside the choice indicators is demanded with ERROR, its own phrase",
          comp.demanded_with({"SIZE"}, {"ERROR"}))
    check("…but not with COMPUTE: a bar pair and a bracket hold it apart (§5.2.6.4)",
          not comp.demanded_with({"SIZE"}, {"COMPUTE"}) and not comp.demanded_with({"END_COMPUTE"}, {"COMPUTE"}))
    check("NOT ON SIZE ERROR and ON SIZE ERROR are stacked alternatives of the bars",
          not comp.demanded_with({"NOT"}, {"ERROR"}))
    check("the ellipsis repeats `{ identifier-1 [ rounded-phrase ] }`, not COMPUTE (§5.2.7)",
          comp.repeatable({"identifier_1"}) and comp.repeatable({"rounded_phrase"}) and not comp.repeatable({"COMPUTE"}))

    pieces = _pieces([{"text": "[MULTIPLE]", "x0": 0.0, "x1": 50.0, "y0": 0.0, "underlined": True},
                      {"text": "}...", "x0": 60.0, "x1": 80.0, "y0": 0.0, "underlined": False}])
    check("a delimiter or ellipsis set hard against a word is split off it, the word keeping its underline",
          [(p["text"], p["underlined"]) for p in pieces]
          == [("[", False), ("MULTIPLE", True), ("]", False), ("}", False), (ELLIPSIS, False)], pieces)
    holders, unpaired = _text_holders(1, _pieces([{"text": t, "x0": x, "x1": x + 5, "y0": 0.0, "underlined": False}
                                                  for t, x in (("[", 0), ("|", 10), ("A", 20), ("|", 30), ("]", 40),
                                                               ("}", 50))]), {0.0: 0.0})
    check("text delimiters pair on their line: bars inside a bracket, and a stray closer is counted",
          sorted((h.family, h.x_open, h.x_close) for h in holders) == [("bar", 10, 30), ("bracket", 0, 40)]
          and unpaired == 1, (holders, unpaired))
    check("a line carrying a lowercase word is prose", prose_row(["The", "COMPUTE", "statement"])
          and not prose_row(["COMPUTE", "identifier-1", "[", "END-COMPUTE", "]"]))
    print("SELF-TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="run the measured-fixture self-test (needs no PDF)")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    ap.print_help()
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
