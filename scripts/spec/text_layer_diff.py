#!/usr/bin/env python3
"""Page-by-page word diff of the 1985 PDF text layer against specs/ANSI_COBOL_1985.md (kb/Work PB6135).

WHY. PB6051 (1985 Markdown accuracy) closed on "a full reading pass finds nothing", and each full agent pass found a new
handful on pages earlier passes had passed. A reader misses at random; a script does not. The PDF carries a text layer on
all 976 pages. It is OCR text with its own errors (`file-name-l` for `file-name-1`, `lS` for `IS`), so a difference may be
the layer's damage or the transcription's. This script aligns the words of each page's text layer with the words of that
page's slice of the Markdown (`## Page N` to the next) and classifies every residual difference.

THE TOLERANCE IS ASYMMETRIC (review R1-1, R1-2). The alignment key of a word is only its letters and digits, lower case: no
OCR confusion is folded away at that level, so `file-name-l` against `file-name-1` is a difference to explain. A difference
is OCR-NOISE only when the PDF word is the garbled side and the Markdown word is the well-formed reading:

  * the Markdown word is KNOWN: a reserved word of the standard (tests/version-matrix/reserved-words.json), or a word the
    Markdown itself spells at least twice, or a digit-suffixed form whose stem it spells at least twice (`data-name-3`),
    and it is not MALFORMED (a hyphenated word ending in a lone `l`, `i`, `o`, `s`, `z`, a digit inside a letter run);
  * the PDF word is ABSENT from the Markdown (and is no reserved word): a damaged form no transcriber wrote; and
  * the two differ only by the OCR confusions (l/1/I/|, O/0, rn/m, Z/2, S/5, ligatures) or by one confusable letter or one
    stray mark of a word of four letters or more with no digit.

A Markdown word that is itself malformed or unknown (`identifier-i`, `comer`, `date-name-1` where `data-name-1` is the
document's form), a PDF word the document also uses, and two real words of different spelling (`of`/`or`, `B-1`/`B-2`) are
never noise: they are UNEXPLAINED.

  OCR-NOISE   the asymmetric reading above; a stray text-layer mark of no word (`q~7`); a word split or joined by the
              layer's spacing (`FILE - CONTROL` / `FILE-CONTROL`) or a line-end hyphen; a run of two or more words that the
              layer reads in another order (two columns), moved as a matched block;
  CORRECTION  a recorded deliberate difference: a LEDGER-1985.json claim or sweep `was`/`now` pair, or a `> ⚠ Corrected` note,
              OF THAT PAGE, accounts for THIS hunk: the Markdown words lie in the entry's `now` text and the PDF words in
              its `was` text (or the note quotes both), as one contiguous run with its neighbouring word (a hunk of three
              words or more, or the whole entry, needs no neighbour). No page-wide bag of words;
  FIGURE      the difference lies inside a `<pre>` figure: the box and bracket glyphs have no text-layer words, so the
              confidence is lower and it is reported apart;
  PUNCT, CASE the words are the same letters and digits and differ in punctuation or in letter case: neither is folded
              away, both are counted and named in the verdict line, and neither is explained, only unchecked here;
  UNCOVERED   Markdown words with no text-layer counterpart (the layer omits a table, a figure's labels, a block of prose,
              or the Markdown adds them): unchecked by this tool, counted and named in the verdict line;
  OCR-ARBITRATED, LIKELY-ERROR  the second reading (`--arbitrate`, tesseract 5 on the 300 dpi render, `--psm 4`) of each
              UNEXPLAINED row. It must hold the hunk's words in order beside the neighbouring word (positional, never a page
              bag): the Markdown's words and not the layer's make OCR-ARBITRATED; the layer's and not the Markdown's make
              LIKELY-ERROR (two independent readings against the transcription: the strongest signal, counted with the
              unexplained by `--require-zero`); both or neither stay UNEXPLAINED;
  UNEXPLAINED anything else, including a word the PDF has and the Markdown dropped, a moved single word, and a different
              word. The verdict line counts these, and `--require-zero` exits non-zero on them.

The result is committed (docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json) with the SHA-256 of the three
inputs it was measured from (the Markdown, the ledger, this script). `--check` needs no licensed PDF: it fails (the gate's
TEXT LAYER DIFF 1985 audit) when one of them changed since the JSON was written, and names the command that re-measures.

    python scripts/spec/text_layer_diff.py                  # measure all pages, write the JSON, print the verdict
    python scripts/spec/text_layer_diff.py --arbitrate      # the committed result: measure, then the second reading
    python scripts/spec/text_layer_diff.py --pages 30-40    # a range, printed only
    python scripts/spec/text_layer_diff.py --check          # the gate: the JSON is current; prints the verdict
    python scripts/spec/text_layer_diff.py --check --require-zero   # PB6051's closing condition: nothing unexplained
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import hashlib
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import unicodedata
from concurrent.futures import ThreadPoolExecutor

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.insert(0, str(HERE))
sys.path.append(str(HERE.parent))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402
import spec_pdf  # noqa: E402

MARKDOWN = spec_pdf.markdown(1985)
PDF = spec_pdf.path(1985)  # None when the private submodule is not checked out
LEDGER = REPO / "docs" / "rearchitecture" / "spec-reconciliation" / "LEDGER-1985.json"
RESULT = REPO / "docs" / "rearchitecture" / "spec-reconciliation" / "TEXT-LAYER-DIFF-1985.json"
RESERVED = REPO / "tests" / "version-matrix" / "reserved-words.json"
CLASSES = ("OCR-NOISE", "OCR-ARBITRATED", "CORRECTION", "AMENDMENT-APPLIED", "FIGURE", "PUNCT", "CASE", "UNCOVERED", "LIKELY-ERROR", "UNEXPLAINED")
RERUN = "python scripts/spec/text_layer_diff.py"

PAGE_HEADING = re.compile(r"^## Page (\d+)\s*$")
TAG = re.compile(r"</?[A-Za-z][^>]*>")
BOX = re.compile(r"[─-╿▀-▟║]")
LIGATURES = {"ﬀ": "ff", "ﬁ": "fi", "ﬂ": "fl", "ﬃ": "ffi", "ﬄ": "ffl"}
QUOTES = str.maketrans({"‘": "'", "’": "'", "“": '"', "”": '"', "–": "-", "—": "-",
                        "−": "-", "­": "-", "‐": "-", "‑": "-", " ": " "})


def _plain(word: str) -> str:
    """The word with ligatures expanded, accents dropped and the typographic quotes and dashes made plain."""
    for lig, rep in LIGATURES.items():
        word = word.replace(lig, rep)
    word = unicodedata.normalize("NFKD", word.translate(QUOTES))
    return "".join(c for c in word if not unicodedata.combining(c))


def lite(word: str) -> str:
    """The ALIGNMENT key of a word: its letters and digits, lower case. No OCR confusion is folded here, so a changed
    letter is a difference; a changed comma or capital is the same key and is looked at separately (PUNCT, CASE)."""
    return re.sub(r"[^a-z0-9]", "", _plain(word).lower())


def fold(word: str) -> str:
    """The key of a word under the OCR's known confusions (l, I, 1, | one letter; O to 0; Z to 2; S to 5; rn to m). Used
    only to ask whether a PDF word that is not the Markdown's could be the layer's damaged reading of it, never to align."""
    word = _plain(word).lower().replace("rn", "m")
    word = re.sub(r"[l1i|!]", "1", word)
    word = word.replace("o", "0").replace("z", "2").replace("s", "5")
    return re.sub(r"[^a-z0-9]", "", word)


def lexkey(word: str) -> str:
    """A word as the document spells it: lower case, hyphens and digits kept, the punctuation around it stripped."""
    return _plain(word).lower().strip(",.;:()[]{}<>\"'`")


# ---------------------------------------------------------------- the Markdown side

def markdown_pages(text: str) -> dict[int, list[tuple[str, bool, int]]]:
    """page -> [(word, in_figure, line)] with the Markdown markup, box glyphs and the transcription's own notes dropped."""
    pages: dict[int, list[tuple[str, bool, int]]] = {}
    page = None
    in_pre = False
    in_fence = False
    for no, line in enumerate(text.splitlines(), 1):
        m = PAGE_HEADING.match(line)
        if m:
            page = int(m.group(1))
            pages[page] = []
            in_pre = False
            continue
        if page is None:
            continue
        if line.startswith("# Errata"):  # the publisher's errata section follows the last page: not a page of the standard
            break
        if line.lstrip().startswith("> ⚠"):  # the transcription's own correction note, not page text
            continue
        if line.lstrip().startswith("```"):
            in_fence = not in_fence
            continue
        was_pre = in_pre or in_fence
        if re.search(r"<pre\b", line):
            in_pre = True
        if "</pre>" in line:
            in_pre = False
        figure = was_pre or in_pre or in_fence or "</pre>" in line
        pages[page].extend((w, figure, no) for w in words_of_markdown(line))
    return pages


def words_of_markdown(line: str) -> list[str]:
    line = line.replace("&nbsp;", " ").replace("&lt;", "<").replace("&gt;", ">").replace("&amp;", "&")
    line = TAG.sub(" ", line)
    line = BOX.sub(" ", line)
    line = re.sub(r"^\s*#{1,6}\s+", "", line)
    line = re.sub(r"^\s*>\s?", "", line)
    line = re.sub(r"^\s*\|?(\s*:?-{2,}:?\s*\|)+\s*$", " ", line)  # a table's rule row
    line = line.replace("|", " ").replace("`", "").replace("*", " ")
    line = re.sub(r"\\([\\`*_{}\[\]()#+.!|<>~-])", r"\1", line)
    return [w for w in line.translate(QUOTES).split() if re.search(r"[A-Za-z0-9]", w) or len(w) == 1]


# ---------------------------------------------------------------- the PDF side

def pdf_pages(pages: list[int]) -> dict[int, list[str]]:
    import pymupdf  # noqa: PLC0415  (only the measurement needs it)
    out = {}
    with pymupdf.open(PDF) as doc:
        for n in pages:
            words = [w[4] for w in doc[n - 1].get_text("words")]
            out[n] = drop_licence_stamp([x for w in words for x in w.translate(QUOTES).split()])
    return out


STAMP = re.compile(r"Copyright by INCITS\..*?Copying and networking prohibited\.")


LICENCE_WORDS = re.compile(r"licen[cs]ed|downloaded|copyright by|networking|single.user|ANSI order|X_\d", re.I)


def drop_licence_stamp(words: list[str]) -> list[str]:
    """The purchaser's stamp on every page (`Copyright by INCITS. Licensed to ... prohibited.`) is no page text."""
    return STAMP.sub(" ", " ".join(words)).split()


# ---------------------------------------------------------------- the lexicon (which side of a difference is well-formed)

MALFORMED = re.compile(r"-[lioszj|]$|[a-z][01][a-z]|^0[a-z]")
STEM = re.compile(r"\d+$")
#: letters the OCR takes for one another (both directions), for a damaged letter that is no fold of the confusions above
CONFUSABLE = {frozenset(p) for p in ("ce", "hb", "nu", "ao", "ft", "vy", "il", "ij", "tl", "cl", "ec")}
STRAY_MARKS = set(".,'`~^;:-_*")
#: glyphs the layer reads where a letter is printed
GLYPHS = set(".,'`~^;:-_*[]{}|!?/")


@dataclasses.dataclass
class Lexicon:
    """What the Markdown itself spells, and the standard's reserved words: the evidence that a Markdown word is the
    well-formed reading and a PDF word is the damaged one (and never the other way round)."""
    count: collections.Counter
    stems: collections.Counter
    reserved: frozenset

    @classmethod
    def of(cls, words: list[str], reserved: frozenset = frozenset()) -> "Lexicon":
        count = collections.Counter(lexkey(w) for w in words)
        stems = collections.Counter()
        for k, n in count.items():
            if STEM.search(k) and STEM.sub("", k):
                stems[STEM.sub("", k)] += n
        return cls(count, stems, reserved)

    def present(self, key: str) -> bool:
        """A word the document (or the standard's word list) spells at all: no transcriber-free damage."""
        return self.count[key] >= 1 or key in self.reserved

    def known(self, key: str) -> bool:
        """The well-formed reading: spelled at least twice, reserved, or a digit suffix on a stem spelled at least twice
        (`data-name-3`); and never a malformed form (`file-name-l`, `identifier-i`)."""
        if not key or MALFORMED.search(key):
            return False
        if self.count[key] >= 2 or key in self.reserved:
            return True
        stem = STEM.sub("", key)
        return stem != key and bool(stem) and self.stems[stem] >= 2


def reserved_words() -> frozenset:
    data = json.loads(RESERVED.read_text(encoding="utf-8"))
    return frozenset(w["word"].lower() for w in data["words"])


def one_edit(damaged: str, printed: str) -> bool:
    """`damaged` is `printed` with one letter swapped for a confusable letter or a stray glyph, one letter lost, or one stray
    mark added: a printed word of four letters or more with no digit (a short word differs from its neighbours by one
    letter: `of` and `or` are two words, never one word damaged)."""
    if len(printed) < 4 or re.search(r"\d", damaged + printed):
        return False
    if len(damaged) == len(printed):
        diff = [(x, y) for x, y in zip(damaged, printed) if x != y]
        return len(diff) == 1 and (frozenset(diff[0]) in CONFUSABLE or diff[0][0] in GLYPHS)
    if len(damaged) == len(printed) - 1 and len(printed) >= 5:  # the layer lost a letter
        return any(printed[:i] + printed[i + 1:] == damaged for i in range(len(printed)))
    if len(damaged) == len(printed) + 1:  # the layer added a stray mark
        return any(damaged[:i] + damaged[i + 1:] == printed and c in STRAY_MARKS for i, c in enumerate(damaged))
    return False


def is_noise_pair(pdf_word: str, md_word: str, lex: Lexicon) -> bool:
    """The PDF word is the layer's damaged reading of the Markdown's well-formed word (never the reverse)."""
    pk, mk = lexkey(pdf_word), lexkey(md_word)
    if pk == mk or not lex.known(mk) or lex.present(pk):
        return False
    return fold(pk) == fold(mk) or one_edit(pk, mk)


def is_stray(word: str, lex: Lexicon) -> bool:
    """A text-layer mark no transcriber wrote and no word is made of: a glyph of no letter, or a short fragment with no
    digit (a number the document does not spell is a different number, never a mark)."""
    if lex.present(lexkey(word)):
        return False
    if re.search(r"[^A-Za-z0-9\-.,;:()'\"/]", word):
        return not re.search(r"\d{2}", word)
    return len(lite(word)) <= 3 and not re.search(r"\d", word)


# ---------------------------------------------------------------- the ledger's explanations

Entry = tuple[list[list[str]], list[list[str]]]  # (the texts the PDF side may lie in, the texts the Markdown side may lie in)


def _keys(text: str) -> list[str]:
    return [k for k in (fold(w) for w in words_of_markdown(text)) if k]


def ledger_entries() -> dict[int, list[Entry]]:
    """page -> its recorded was/now pairs (each a deliberate difference of the Markdown from the printed page)."""
    data = json.loads(LEDGER.read_text(encoding="utf-8"))
    out: dict[int, list[Entry]] = {}

    def add(page: int, was: str | None, now: str | None) -> None:
        if was or now:
            out.setdefault(page, []).append(([_keys(was or "")], [_keys(now or "")]))

    for c in data["claims"]:
        for e in c.get("edits", []) or []:
            add(c["page"], e.get("was"), e.get("now"))
    for s in data["sweeps"]:
        add(s["page"], s.get("was"), s.get("now"))
    return out


def note_entries(text: str) -> dict[int, list[Entry]]:
    """page -> its `> ⚠ Corrected` notes, each an entry whose texts are the note's backticked quotes (the whole line when it
    quotes nothing): a note accounts for a hunk whose two sides it quotes."""
    out: dict[int, list[Entry]] = {}
    page = None
    for line in text.splitlines():
        m = PAGE_HEADING.match(line)
        if m:
            page = int(m.group(1))
        elif page and line.lstrip().startswith("> ⚠"):
            quotes = re.findall(r"`([^`]+)`", line) or [line]
            segs = [k for k in (_keys(q) for q in quotes) if k]
            out.setdefault(page, []).append((segs, segs))
    return out


Change = tuple[int, str, list[str]]  # (the PDF page of the Amendment, the page cell's first word, the instruction's words)
Amendment = tuple[list[list[str]], list[list[str]], Change | None]  # (what the note says the standard printed, the amended
# text it quotes, the Amendment entry the note names: its page, its page cell and the instruction it quotes)
CHANGE = re.compile(r'its change for page (.+?) \(page (\d+), folio [AB]-\d+\)(?:.*?)applied in place(?:: "(.*?)")?')
SIGNATURE = 5   # words of an entry's instruction that find its start in the Amendment page's text layer
MATCHED = 8     # words of the quoted instruction that must open exactly one entry of the Amendment page


def amendment_notes(text: str) -> dict[int, list[Amendment]]:
    """page -> its amendment notes: the `> ⚠ **CORRECTED` notes that say an Amendment's text was `applied in place`
    (kb/Work PB5045). A note's first backticked quote is what the standard prints; its other quotes are the amended text it
    was corrected to; `its change for page P (page N, folio A-n): "instruction"` names the Amendment's entry for it. The
    markup is the whole rule: a note of another kind (`a damaged letter on the printed page`) is no amendment."""
    out: dict[int, list[Amendment]] = {}
    page = None
    for line in text.splitlines():
        m = PAGE_HEADING.match(line)
        if m:
            page = int(m.group(1))
        elif page and line.lstrip().startswith("> ⚠ **CORRECTED") and "Amendment" in line and "applied in place" in line:
            quotes = [_keys(q) for q in re.findall(r"`([^`]+)`", line)]
            printed, amended = [k for k in quotes[:1] if k], [k for k in quotes[1:] if k]
            c = CHANGE.search(line)
            change = (int(c.group(2)), (_keys(c.group(1)) or [""])[0], _keys(c.group(3) or "")) if c else None
            if printed:
                out.setdefault(page, []).append((printed, amended, change))
    return out


def amendment_rows(text: str) -> dict[int, list[tuple[str, list[str]]]]:
    """page -> the rows of its change tables, in order: (the page cell's first word, the instruction cell's words)."""
    out: dict[int, list[tuple[str, list[str]]]] = {}
    page = None
    for line in text.splitlines():
        m = PAGE_HEADING.match(line)
        if m:
            page = int(m.group(1))
        elif page and line.startswith("|") and not re.match(r"^\|\s*:?-", line):
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            first = (_keys(re.sub(r"<[^>]+>", " ", cells[0])) or [""])[0]
            if len(cells) >= 2 and first not in ("", "page"):
                out.setdefault(page, []).append((first, _keys(re.sub(r"<[^>]+>", " ", " ".join(cells[1:])))))
    return out


def _find(words: list[str], sig: list[str], start: int) -> int | None:
    return next((i for i in range(start, len(words) - len(sig) + 1) if words[i:i + len(sig)] == sig), None)


def entry_text(rows: list[tuple[str, list[str]]], layer: list[str], change: Change) -> list[str] | None:
    """The text layer's words of the ONE entry the note names, or None (fail closed). The entry is the row of the Amendment
    page whose page cell and first words are the note's own, and exactly one row is; its words run in the text layer from the
    place its first words are found to the place the next row's are found (the end of the page for the last row). A row
    whose start, or whose successor's, is not found in the layer gives no entry."""
    _, token, instr = change
    if not instr:
        return None
    n = min(MATCHED, len(instr))
    hits = [i for i, (tok, ins) in enumerate(rows) if tok == token and ins[:n] == instr[:n]]
    if len(hits) != 1:
        return None
    starts: list[int | None] = []
    cursor = 0
    for _, ins in rows:
        at = _find(layer, ins[:SIGNATURE], cursor) if ins else None
        starts.append(at)
        if at is not None:
            cursor = at + 1
    h = hits[0]
    if starts[h] is None or (h + 1 < len(rows) and starts[h + 1] is None):
        return None
    return layer[starts[h]:starts[h + 1] if h + 1 < len(rows) else len(layer)]


def with_cited_text(notes: dict[int, list[Amendment]], layer: dict[int, list[str]],
                    rows: dict[int, list[tuple[str, list[str]]]]) -> tuple[dict[int, list[Amendment]], int]:
    """Each note's amended text, extended by the entry of the Amendment page it names (most notes say `the text above` and
    quote nothing): the Markdown's side of a difference is then checked against the Amendment's own print of THIS change,
    never the whole page and never the Markdown it was written into. A note whose entry is not located gets no such text
    (its rows stay unexplained); the count of those notes is returned."""
    missing = 0
    out: dict[int, list[Amendment]] = {}
    for pg, ns in notes.items():
        for pr, am, change in ns:
            entry = None
            if change is not None:
                keys = [k for k in (fold(w) for w in layer.get(change[0], [])) if k]
                entry = entry_text(rows.get(change[0], []), keys, change)
            missing += entry is None
            out.setdefault(pg, []).append((pr, am + ([entry] if entry else []), change))
    return out, missing


def _adjacent_in(texts: list[list[str]], before: str | None, after: str | None) -> bool:
    """`before` and `after` are consecutive words of one text: the words between them were dropped."""
    return before is not None and after is not None and any(
        t[i] == before and t[i + 1] == after for t in texts for i in range(len(t) - 1))


def amendment_accounts_for(notes: list[Amendment], pdf_hunk: list[str], md_hunk: list[str],
                           pdf_ctx: tuple[str | None, str | None], md_ctx: tuple[str | None, str | None]) -> bool:
    """An amendment note of this page governs the hunk on BOTH sides: the layer's words are a run of what the note says the
    standard printed, and the Markdown's words are a run of the amended text the note says it was corrected to (words the
    Markdown dropped: the two words around the gap are consecutive in the amended text). A run needs a neighbouring word or
    three words (`_run_in`): no page-wide bag of words, and a Markdown word the note does not give (`13` for `12`) is no
    amendment."""
    for printed, amended, _ in notes:
        if pdf_hunk and not _run_in(pdf_hunk, printed, *pdf_ctx):
            continue
        if md_hunk and _run_in(md_hunk, amended, *md_ctx):
            return True
        if not md_hunk and pdf_hunk and _adjacent_in(amended, *md_ctx):
            return True
        if not pdf_hunk and md_hunk and _run_in(md_hunk, amended, *md_ctx):
            return True
    return False


def _run_in(hunk: list[str], texts: list[list[str]], before: str | None, after: str | None) -> bool:
    """The hunk's words are a contiguous run of one text, with a neighbouring word of the page beside it (a text that is
    the hunk itself, or a hunk of three words or more, is specific enough alone)."""
    for t in texts:
        if t == hunk:
            return True
        for i in range(len(t) - len(hunk) + 1):
            if t[i:i + len(hunk)] != hunk:
                continue
            if len(hunk) >= 3:
                return True
            if (before is not None and i > 0 and t[i - 1] == before) or \
               (after is not None and i + len(hunk) < len(t) and t[i + len(hunk)] == after):
                return True
    return False


def entry_accounts_for(entry: Entry, pdf_hunk: list[str], md_hunk: list[str],
                       pdf_ctx: tuple[str | None, str | None], md_ctx: tuple[str | None, str | None]) -> bool:
    """One recorded entry accounts for this hunk: each non-empty side is a run of the entry's own text for that side."""
    if not pdf_hunk and not md_hunk:
        return False
    was, now = entry
    return (not pdf_hunk or _run_in(pdf_hunk, was, *pdf_ctx)) and (not md_hunk or _run_in(md_hunk, now, *md_ctx))


# ---------------------------------------------------------------- the diff

def clip(text: str, limit: int = 120) -> str:
    """A record keeps the head of a long difference; `words` carries its full size."""
    return text if len(text) <= limit else text[:limit] + " ..."


def join_line_end_hyphens(words: list[str], keys: set[str]) -> list[str]:
    """`copy-` `ing` at a line end is the Markdown's `copying`: join the two when the joined word is one the page has."""
    out: list[str] = []
    i = 0
    while i < len(words):
        w = words[i]
        if w.endswith("-") and len(w) > 2 and i + 1 < len(words) and lite(w[:-1] + words[i + 1]) in keys:
            out.append(w[:-1] + words[i + 1])
            i += 2
        else:
            out.append(w)
            i += 1
    return out


def _alnum(word: str) -> str:
    return re.sub(r"[^A-Za-z0-9]", "", _plain(word))


def exact_differences(pw: list[str], mw: list[str], line: list[int]) -> list[dict]:
    """The words that align (same letters and digits) and are not spelled the same: punctuation (PUNCT) or letter case (CASE).
    Consecutive words of one class are one record."""
    rows: list[dict] = []
    for p, m, ln in zip(pw, mw, line):
        if p == m:
            continue
        a, b = _alnum(p), _alnum(m)
        cls = "CASE" if a != b else "PUNCT"
        if rows and rows[-1]["class"] == cls and rows[-1]["line"] == ln:
            rows[-1]["pdf"] += " " + p
            rows[-1]["md"] += " " + m
            rows[-1]["words"] = [rows[-1]["words"][0] + 1, rows[-1]["words"][1] + 1]
        else:
            rows.append({"class": cls, "reason": "same letters, different case" if cls == "CASE" else "same letters, different punctuation",
                         "pdf": p, "md": m, "line": ln, "words": [1, 1]})
    for r in rows:
        r["pdf"], r["md"] = clip(r["pdf"]), clip(r["md"])
    return rows


def _longest_run(x: list[str], y: list[str]) -> tuple[int, int, int]:
    """The longest contiguous run of equal items of x and y: (length, start in x, start in y)."""
    best = (0, 0, 0)
    for i in range(len(x)):
        for j in range(len(y)):
            n = 0
            while i + n < len(x) and j + n < len(y) and x[i + n] == y[j + n]:
                n += 1
            if n > best[0]:
                best = (n, i, j)
    return best


def classify_page(pdf: list[str], md: list[tuple[str, bool, int]], entries: list[Entry], lex: Lexicon,
                  removed: bool = False, amendments: list[Amendment] | None = None) -> list[dict]:
    """The residual differences of one page, each a record {class, reason, pdf, md, line, words}.

    `amendments` is the page's `amendment_notes`: a hunk one of them governs is AMENDMENT-APPLIED.
    `removed` is a page whose printed content the transcription deliberately holds elsewhere (the base index, PDF pages
    807 to 819, replaced by Amendment 2's: Errata C337): its text-layer words are CORRECTION."""
    import difflib  # noqa: PLC0415
    md_keys = {lite(w) for w, _, _ in md}
    pdf = join_line_end_hyphens(pdf, md_keys)
    a = [(w, lite(w)) for w in pdf if lite(w)]
    keep = [i for i, (w, _, _) in enumerate(md) if lite(w)]
    b = [(md[i][0], lite(md[i][0])) for i in keep]
    figure = [md[i][1] for i in keep]
    line = [md[i][2] for i in keep]
    sm = difflib.SequenceMatcher(None, [k for _, k in a], [k for _, k in b], autojunk=False)
    rows: list[dict] = []

    def emit(cls: str, reason: str, pw: list[tuple[str, str]], mw: list[tuple[str, str]], j: int, src=None, shown=None) -> None:
        ln = line[min(j, len(line) - 1)] if line else 0
        sp, sm_ = shown if shown is not None else (pw, mw)  # the excerpt is the whole aligned block, not what is left of it
        row = {"class": cls, "reason": reason, "pdf": clip(" ".join(w for w, _ in sp)),
               "md": clip(" ".join(w for w, _ in sm_)), "line": ln, "words": [len(pw), len(mw)]}
        if src is not None:
            row["_src"] = src  # the whole differing block with its neighbours, for the second reading (arbitrate_rows)
        rows.append(row)

    blocks: list[list] = []  # [left_p, left_m, figure, j]: what a differing block still owes after the tolerated readings
    for tag, i1, i2, j1, j2 in sm.get_opcodes():
        if tag == "equal":
            rows.extend(exact_differences([w for w, _ in a[i1:i2]], [w for w, _ in b[j1:j2]], line[j1:j2]))
            continue
        pa, mb = list(a[i1:i2]), list(b[j1:j2])
        src = ([k for _, k in pa], [k for _, k in mb], [k for _, k in a[max(0, i1 - 2):i1]], [k for _, k in a[i2:i2 + 2]],
               [k for _, k in b[max(0, j1 - 2):j1]], [k for _, k in b[j2:j2 + 2]])
        fig = any(figure[j1:j2]) or (j2 == j1 and j1 < len(figure) and figure[j1])
        if removed and pa and not mb:
            emit("CORRECTION", "printed content held elsewhere by a recorded correction", pa, mb, j1)
            continue
        pdf_hunk, md_hunk = [fold(w) for w, _ in pa], [fold(w) for w, _ in mb]
        pdf_ctx = (fold(a[i1 - 1][0]) if i1 > 0 else None, fold(a[i2][0]) if i2 < len(a) else None)
        md_ctx = (fold(b[j1 - 1][0]) if j1 > 0 else None, fold(b[j2][0]) if j2 < len(b) else None)
        if any(entry_accounts_for(e, pdf_hunk, md_hunk, pdf_ctx, md_ctx) for e in entries):
            emit("CORRECTION", "a ledger pair or correction note of this page accounts for this hunk", pa, mb, j1)
            continue
        if amendments and amendment_accounts_for(amendments, pdf_hunk, md_hunk, pdf_ctx, md_ctx):
            emit("AMENDMENT-APPLIED", "an Amendment's text applied in place (the page's CORRECTED note quotes the printed words)", pa, mb, j1)
            continue
        left_p, left_m, paired = list(pa), list(mb), []
        for item in list(pa):  # the asymmetric pairing: the PDF word damaged, the Markdown word well-formed
            for cand in left_m:
                if is_noise_pair(item[0], cand[0], lex):
                    left_p.remove(item)
                    left_m.remove(cand)
                    paired.append((item, cand))
                    break
        if paired:
            emit("OCR-NOISE", "the layer's damaged reading of a well-formed word", [p for p, _ in paired], [m for _, m in paired], j1)
        strays = [x for x in left_p if is_stray(x[0], lex)]
        if strays:
            emit("OCR-NOISE", "stray text-layer mark", strays, [], j1)
            left_p = [x for x in left_p if x not in strays]
        if left_p and left_m and "".join(k for _, k in left_p) == "".join(k for _, k in left_m):
            emit("OCR-NOISE", "joined or split by the layer's spacing", left_p, left_m, j1)
            continue
        if left_p or left_m:
            blocks.append([left_p, left_m, fig, j1, src, (pa, mb)])

    # words that moved as a matched block: a run of two or more words left over on both sides, in another place (two
    # columns, a table read by column). A single word that is gone in one place and appears in another is not a move:
    # it is two differences (a dropped `not` against a gained one).
    while True:
        best = (1, None, None, 0, 0)
        for x, bx in enumerate(blocks):
            for y, by in enumerate(blocks):
                n, i, j = _longest_run([k for _, k in bx[0]], [k for _, k in by[1]])
                if n > best[0]:
                    best = (n, x, y, i, j)
        n, x, y, i, j = best
        if x is None:
            break
        moved_p, moved_m = blocks[x][0][i:i + n], blocks[y][1][j:j + n]
        blocks[x][0] = blocks[x][0][:i] + blocks[x][0][i + n:]
        blocks[y][1] = blocks[y][1][:j] + blocks[y][1][j + n:]
        emit("OCR-NOISE", "a run the layer reads in another order", moved_p, moved_m, blocks[y][3])

    pdf_only = collections.Counter(k for left_p, _, _, _, _, _ in blocks for _, k in left_p)
    for left_p, left_m, fig, j, src, shown in blocks:
        if not left_p and not left_m:
            continue
        if fig:
            emit("FIGURE", "inside a drawn figure", left_p, left_m, j, shown=shown)
        elif not left_p and all(pdf_only[k] for _, k in left_m):
            emit("UNEXPLAINED", "a single word moved: gone at one place, present at another", left_p, left_m, j, src, shown=shown)
        elif not left_p:
            emit("UNCOVERED", "Markdown words the text layer lacks", left_p, left_m, j, shown=shown)
        elif not left_m:
            emit("UNEXPLAINED", "text layer has words the Markdown lacks", left_p, left_m, j, src, shown=shown)
        else:
            emit("UNEXPLAINED", "a different word", left_p, left_m, j, src, shown=shown)
    return rows


# ---------------------------------------------------------------- the second reading (tesseract)

TESSERACT_DEFAULT = "C:/Program Files/Tesseract-OCR/tesseract.exe"
OCR_DPI = 300
OCR_CACHE = REPO / "TestResults" / "text-layer-ocr-1985"  # gitignored, per worktree


def tesseract_exe() -> str:
    found = os.environ.get("WISEOWL_COBOL_TESSERACT") or shutil.which("tesseract") or TESSERACT_DEFAULT
    if not pathlib.Path(found).is_file():
        sys.exit(f"FATAL: tesseract not found ({found}): install Tesseract 5 or set WISEOWL_COBOL_TESSERACT")
    return found


def tesseract_version(exe: str) -> str:
    out = subprocess.run([exe, "--version"], capture_output=True, text=True, encoding="utf-8")
    return (out.stdout or out.stderr).splitlines()[0].strip()


def tesseract_text(page: int, cache: pathlib.Path, exe: str) -> str:
    """Tesseract's reading of the rendered PDF page (300 dpi, --psm 4), cached per page in `cache` (never committed)."""
    import pymupdf  # noqa: PLC0415
    txt = cache / f"p{page}.txt"
    if txt.is_file():
        return txt.read_text(encoding="utf-8")
    cache.mkdir(parents=True, exist_ok=True)
    png = cache / f"p{page}.png"
    with pymupdf.open(PDF) as doc:
        doc[page - 1].get_pixmap(dpi=OCR_DPI).save(png)
    low = getattr(subprocess, "BELOW_NORMAL_PRIORITY_CLASS", 0)
    out = subprocess.run([exe, str(png), "-", "--psm", "4"], capture_output=True, text=True, encoding="utf-8", creationflags=low)
    if out.returncode != 0:
        sys.exit(f"FATAL: tesseract failed on page {page}: {out.stderr.strip()[:300]}")
    png.unlink()
    txt.write_text(out.stdout, encoding="utf-8", newline="\n")
    return out.stdout


def _supports(hunk: list[str], before: list[str], after: list[str], t: list[str]) -> bool:
    """The second reading holds this reading AT THIS PLACE: the hunk's words in order, with the word before or after it
    beside them (a side with no words holds when its two neighbours touch). Positional, never a bag of the page's words."""
    n = len(hunk)
    if n == 0:
        return bool(before and after) and any(t[i] == before[-1] and t[i + 1] == after[0] for i in range(len(t) - 1))
    for i in range(len(t) - n + 1):
        if t[i:i + n] == hunk and ((before and i > 0 and t[i - 1] == before[-1]) or
                                   (after and i + n < len(t) and t[i + n] == after[0])):
            return True
    return False


def _occurs(hunk: list[str], t: list[str]) -> bool:
    return bool(hunk) and any(t[i:i + len(hunk)] == hunk for i in range(len(t) - len(hunk) + 1))


def zero_read_as_o(pdf_hunk: list[str], md_hunk: list[str]) -> bool:
    """The Markdown's number of two or more digits holding a zero (`100`) against the same word with the letter O for each
    zero (`1OO`): a letter inside a run of digits is no word. A lone `0` against `O` is never decided here, by context or
    otherwise: a typewriter face prints the zero and the letter alike, `'Z', '0', '9'` and `'Z', 'O', '9'` read the same, and
    only the glyph tells them apart (the row stays LIKELY-ERROR for a reader of the page)."""
    if len(pdf_hunk) != 1 or len(md_hunk) != 1:
        return False
    pk, mk = pdf_hunk[0], md_hunk[0]
    if not mk.isdigit() or "0" not in mk or pk != mk.replace("0", "o"):
        return False
    return len(mk) >= 2


def arbitrate_rows(rows: list[dict], tess_text: str) -> None:
    """Reclassify the page's UNEXPLAINED rows by a second, independent reading (tesseract) of the same page:
      the second reading holds the Markdown's words at that place and not the layer's  -> OCR-ARBITRATED;
      it holds the layer's words and not the Markdown's                                  -> LIKELY-ERROR (the strongest signal);
      both or neither                                                                    -> stays UNEXPLAINED.
    A reading-order difference is no witness for the layer (a moved single word; Markdown words the layer lacks that the
    second reading has elsewhere on the page): it stays UNEXPLAINED.
    The row carries the second reading's words around the place as `tesseract`."""
    raw = [w for w in drop_licence_stamp(tess_text.split()) if lite(w)]
    t = [lite(w) for w in raw]
    for r in rows:
        src = r.pop("_src", None)
        if r["class"] != "UNEXPLAINED" or src is None:
            continue
        pdf_hunk, md_hunk, pdf_before, pdf_after, md_before, md_after = src
        md_ok = _supports(md_hunk, md_before, md_after, t)
        pdf_ok = _supports(pdf_hunk, pdf_before, pdf_after, t)
        anchor = next((i for i, k in enumerate(t) if md_before and k == md_before[-1]), None)
        if anchor is None:
            anchor = next((i for i, k in enumerate(t) if md_after and k == md_after[0]), None)
        if pdf_ok and not md_ok and (r["reason"].startswith("a single word moved") or
                                     (not pdf_hunk and _occurs(md_hunk, t))):
            pdf_ok = md_ok  # a reading-order difference, not a second witness: the layer's words are absent and the Markdown's
            # are on the page elsewhere (a table read by column); it stays UNEXPLAINED
        if pdf_ok and not md_ok and zero_read_as_o(pdf_hunk, md_hunk):
            r["class"] = "OCR-NOISE"  # both readers give the letter O for a digit zero of a digit context: the typeface's confusion
            r["reason"] = "both readings give the letter O where the Markdown has the digit zero in a digit context"
        elif md_ok != pdf_ok:
            r["class"] = "OCR-ARBITRATED" if md_ok else "LIKELY-ERROR"
            r["reason"] = ("a second OCR (tesseract) reads the Markdown's words at this place, not the text layer's" if md_ok else
                           "a second OCR (tesseract) reads the text layer's words at this place, not the Markdown's")
        if anchor is not None:
            evidence = " ".join(raw[max(0, anchor - 1):anchor + max(len(md_hunk), len(pdf_hunk)) + 3])
            if not LICENCE_WORDS.search(evidence):  # the purchaser's stamp, however the OCR spelled it, is never kept
                r["tesseract"] = clip(evidence)


def parse_pages(spec: str, total: int) -> list[int]:
    out: list[int] = []
    for part in spec.split(","):
        lo, _, hi = part.partition("-")
        out.extend(range(int(lo), int(hi or lo) + 1))
    return [p for p in out if 1 <= p <= total]


# ---------------------------------------------------------------- the verdict and the binding to the inputs

def digest(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes().replace(b"\r\n", b"\n")).hexdigest()


def inputs() -> dict[str, str]:
    return {"markdown_sha256": digest(MARKDOWN), "ledger_sha256": digest(LEDGER), "script_sha256": digest(pathlib.Path(__file__))}


def stale(result: dict) -> list[str]:
    """The inputs that changed since the result was written, by name."""
    recorded = result.get("inputs", {})
    return [k.removesuffix("_sha256") for k, v in inputs().items() if recorded.get(k) != v]


def verdict(result: dict) -> int:
    pages = result["pages"]
    totals = {c: sum(p["counts"].get(c, 0) for p in pages.values()) for c in CLASSES}
    on = sum(1 for p in pages.values() if p["counts"].get("UNEXPLAINED", 0) or p["counts"].get("LIKELY-ERROR", 0))
    print("  " + "  ".join(f"{c}={totals[c]}" for c in CLASSES))
    unchecked = totals["PUNCT"] + totals["CASE"]
    print(f"=== TEXT LAYER DIFF 1985: {totals['LIKELY-ERROR']} likely error(s) · {totals['UNEXPLAINED']} unexplained on {on} page(s) · "
          f"{totals['UNCOVERED']} uncovered · {unchecked} punctuation/case (unchecked) · "
          f"amendment entry not located: {result.get('amendment_entries_not_located', 0)} ===")
    return totals["UNEXPLAINED"] + totals["LIKELY-ERROR"]


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--pages", help="a range such as 30-40,200: measure these only and print, write nothing")
    ap.add_argument("--check", action="store_true", help="the gate: the committed result is current for its inputs (no PDF needed)")
    ap.add_argument("--require-zero", action="store_true", help="also exit non-zero when anything is UNEXPLAINED (PB6051's close)")
    ap.add_argument("--arbitrate", action="store_true",
                    help="second reading of every UNEXPLAINED row by tesseract (needs the PDF and tesseract); the committed result is arbitrated")
    ap.add_argument("--ocr-cache", type=pathlib.Path, default=OCR_CACHE, help="where tesseract's per-page text is cached (never committed)")
    ap.add_argument("--likely", action="store_true", help="with --check: print each LIKELY-ERROR row (page: pdf / md / tesseract)")
    ap.add_argument("--verbose", action="store_true", help="print each UNEXPLAINED record")
    ap.add_argument("--all", action="store_true", help="print every record, not only UNEXPLAINED")
    args = ap.parse_args()
    if args.check:
        result = json.loads(RESULT.read_text(encoding="utf-8"))
        changed = stale(result)
        unexplained = verdict(result)
        if args.likely:
            for pg, page in result["pages"].items():
                for r in page["differences"]:
                    if r["class"] == "LIKELY-ERROR":
                        print(f"p{pg} L{r['line']}: pdf=[{r['pdf']}] md=[{r['md']}] tesseract=[{r.get('tesseract', '')}]")
        if changed:
            print(f"=== TEXT LAYER DIFF 1985 STALE: {', '.join(changed)} changed since {RESULT.name} was measured; "
                  f"re-run: {RERUN} ===")
            return 1
        return 1 if (unexplained and args.require_zero) else 0
    if PDF is None or not PDF.is_file():
        sys.exit("FATAL: the 1985 PDF is missing: git submodule update --init specs-private")
    text = MARKDOWN.read_text(encoding="utf-8")
    md = markdown_pages(text)
    total = max(md)
    pages = parse_pages(args.pages, total) if args.pages else list(range(1, total + 1))
    pdf = pdf_pages(pages)
    ledger, notes, amended = ledger_entries(), note_entries(text), amendment_notes(text)
    cited = sorted({ch[0] for ns in amended.values() for _, _, ch in ns if ch} - set(pdf))
    amended, not_located = with_cited_text(amended, {**pdf, **pdf_pages(cited)}, amendment_rows(text))
    lex = Lexicon.of([w for words in md.values() for w, _, _ in words], reserved_words())
    result = {"edition": 1985, "pdf_pages": total, "inputs": inputs(), "classes": list(CLASSES),
              "amendment_entries_not_located": not_located, "pages": {}}
    measured = {n: classify_page(pdf[n], md[n], ledger.get(n, []) + notes.get(n, []), lex, 807 <= n <= 819,
                                  amended.get(n)) for n in pages}
    if args.arbitrate:
        exe = tesseract_exe()
        result["inputs"]["tesseract"] = tesseract_version(exe)
        todo = [n for n, rows in measured.items() if any(r["class"] == "UNEXPLAINED" for r in rows)]
        with ThreadPoolExecutor(max_workers=6) as pool:
            texts = dict(zip(todo, pool.map(lambda n: tesseract_text(n, args.ocr_cache, exe), todo)))
        for n in todo:
            arbitrate_rows(measured[n], texts[n])
    for n, rows in measured.items():
        for r in rows:
            r.pop("_src", None)
        counts: dict[str, int] = {}
        for r in rows:
            counts[r["class"]] = counts.get(r["class"], 0) + 1
        if rows:
            result["pages"][str(n)] = {"counts": counts, "differences": rows}
        for r in rows:
            if args.all or (r["class"] in ("UNEXPLAINED", "LIKELY-ERROR") and (args.verbose or args.pages)):
                print(f"p{n} L{r['line']} {r['class']}/{r['reason']}: pdf=[{r['pdf']}] md=[{r['md']}] tesseract=[{r.get('tesseract', '')}]")
    if not args.pages:
        RESULT.write_text(json.dumps(result, ensure_ascii=False, indent=1) + "\n", encoding="utf-8", newline="\n")
        print(f"wrote {RESULT.relative_to(REPO)}")
    unexplained = verdict(result)
    return 1 if (unexplained and args.require_zero) else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
