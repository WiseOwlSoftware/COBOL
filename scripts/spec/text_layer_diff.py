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
  UNEXPLAINED anything else, including a word the PDF has and the Markdown dropped, a moved single word, and a different
              word. The verdict line counts these, and `--require-zero` exits non-zero on them.

The result is committed (docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json) with the SHA-256 of the three
inputs it was measured from (the Markdown, the ledger, this script). `--check` needs no licensed PDF: it fails (the gate's
TEXT LAYER DIFF 1985 audit) when one of them changed since the JSON was written, and names the command that re-measures.

    python scripts/spec/text_layer_diff.py                  # measure all pages, write the JSON, print the verdict
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
import pathlib
import re
import sys
import unicodedata

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
CLASSES = ("OCR-NOISE", "CORRECTION", "FIGURE", "PUNCT", "CASE", "UNCOVERED", "UNEXPLAINED")
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
                  removed: bool = False) -> list[dict]:
    """The residual differences of one page, each a record {class, reason, pdf, md, line, words}.

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

    def emit(cls: str, reason: str, pw: list[tuple[str, str]], mw: list[tuple[str, str]], j: int) -> None:
        ln = line[min(j, len(line) - 1)] if line else 0
        rows.append({"class": cls, "reason": reason, "pdf": clip(" ".join(w for w, _ in pw)),
                     "md": clip(" ".join(w for w, _ in mw)), "line": ln, "words": [len(pw), len(mw)]})

    blocks: list[list] = []  # [left_p, left_m, figure, j]: what a differing block still owes after the tolerated readings
    for tag, i1, i2, j1, j2 in sm.get_opcodes():
        if tag == "equal":
            rows.extend(exact_differences([w for w, _ in a[i1:i2]], [w for w, _ in b[j1:j2]], line[j1:j2]))
            continue
        pa, mb = list(a[i1:i2]), list(b[j1:j2])
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
            blocks.append([left_p, left_m, fig, j1])

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

    pdf_only = collections.Counter(k for left_p, _, _, _ in blocks for _, k in left_p)
    for left_p, left_m, fig, j in blocks:
        if not left_p and not left_m:
            continue
        if fig:
            emit("FIGURE", "inside a drawn figure", left_p, left_m, j)
        elif not left_p and all(pdf_only[k] for _, k in left_m):
            emit("UNEXPLAINED", "a single word moved: gone at one place, present at another", left_p, left_m, j)
        elif not left_p:
            emit("UNCOVERED", "Markdown words the text layer lacks", left_p, left_m, j)
        elif not left_m:
            emit("UNEXPLAINED", "text layer has words the Markdown lacks", left_p, left_m, j)
        else:
            emit("UNEXPLAINED", "a different word", left_p, left_m, j)
    return rows


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
    on = sum(1 for p in pages.values() if p["counts"].get("UNEXPLAINED", 0))
    print("  " + "  ".join(f"{c}={totals[c]}" for c in CLASSES))
    unchecked = totals["PUNCT"] + totals["CASE"]
    print(f"=== TEXT LAYER DIFF 1985: {totals['UNEXPLAINED']} unexplained difference(s) on {on} page(s) · "
          f"{totals['UNCOVERED']} uncovered · {unchecked} punctuation/case (unchecked) ===")
    return totals["UNEXPLAINED"]


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--pages", help="a range such as 30-40,200: measure these only and print, write nothing")
    ap.add_argument("--check", action="store_true", help="the gate: the committed result is current for its inputs (no PDF needed)")
    ap.add_argument("--require-zero", action="store_true", help="also exit non-zero when anything is UNEXPLAINED (PB6051's close)")
    ap.add_argument("--verbose", action="store_true", help="print each UNEXPLAINED record")
    ap.add_argument("--all", action="store_true", help="print every record, not only UNEXPLAINED")
    args = ap.parse_args()
    if args.check:
        result = json.loads(RESULT.read_text(encoding="utf-8"))
        changed = stale(result)
        unexplained = verdict(result)
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
    ledger, notes = ledger_entries(), note_entries(text)
    lex = Lexicon.of([w for words in md.values() for w, _, _ in words], reserved_words())
    result = {"edition": 1985, "pdf_pages": total, "inputs": inputs(), "classes": list(CLASSES), "pages": {}}
    for n in pages:
        rows = classify_page(pdf[n], md[n], ledger.get(n, []) + notes.get(n, []), lex, 807 <= n <= 819)
        counts: dict[str, int] = {}
        for r in rows:
            counts[r["class"]] = counts.get(r["class"], 0) + 1
        if rows:
            result["pages"][str(n)] = {"counts": counts, "differences": rows}
        for r in rows:
            if args.all or (r["class"] == "UNEXPLAINED" and (args.verbose or args.pages)):
                print(f"p{n} L{r['line']} {r['class']}/{r['reason']}: pdf=[{r['pdf']}] md=[{r['md']}]")
    if not args.pages:
        RESULT.write_text(json.dumps(result, ensure_ascii=False, indent=1) + "\n", encoding="utf-8", newline="\n")
        print(f"wrote {RESULT.relative_to(REPO)}")
    unexplained = verdict(result)
    return 1 if (unexplained and args.require_zero) else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
