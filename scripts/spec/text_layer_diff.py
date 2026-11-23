#!/usr/bin/env python3
"""Page-by-page word diff of the 1985 PDF text layer against specs/ANSI_COBOL_1985.md (kb/Work PB6135).

WHY. PB6051 (1985 Markdown accuracy) closed on "a full reading pass finds nothing", and each full agent pass found a new
handful on pages earlier passes had passed. A reader misses at random; a script does not. The PDF carries a text layer on
all 976 pages (it is OCR text, with its own errors: `file-name-l` for `file-name-1`, `lS` for `IS`). This script aligns the
words of each page's text layer with the words of that page's slice of the Markdown (`## Page N` to the next) and
classifies every residual difference:

  OCR-NOISE   the two words are the same after folding the OCR confusions (l/1/I/|, O/0, rn/m, ligatures, quotes, case,
              punctuation), or one is a prefix/suffix fragment of the other split by a line-end hyphen or a space the OCR
              inserted or dropped (`FILE - CONTROL` / `FILE-CONTROL`);
  CORRECTION  the Markdown side is a deliberate difference: a `> ⚠ CORRECTED` note on that page names it, or a
              LEDGER-1985.json claim/sweep of that page has it in a `now` (or `was`) text;
  FIGURE      the difference lies inside a `<pre>` figure: the box and bracket glyphs have no text-layer words, so the
              confidence is lower and it is reported apart;
  UNEXPLAINED anything else. The verdict line counts these.

The result is committed (docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json), so CI reads the measured result
without the licensed PDF (`--check` re-reads only the JSON). The PDF is specs-private/ANSI_COBOL_1985.pdf.

    python scripts/spec/text_layer_diff.py                  # measure all pages, write the JSON, print the verdict
    python scripts/spec/text_layer_diff.py --pages 30-40    # a range, printed only
    python scripts/spec/text_layer_diff.py --check          # CI: the committed JSON, the verdict from it
"""
from __future__ import annotations

import argparse
import collections
import difflib
import json
import pathlib
import re
import sys
import unicodedata

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
sys.path.append(str(HERE.parent))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

MARKDOWN = REPO / "specs" / "ANSI_COBOL_1985.md"
PDF = REPO / "specs-private" / "ANSI_COBOL_1985.pdf"
LEDGER = REPO / "docs" / "rearchitecture" / "spec-reconciliation" / "LEDGER-1985.json"
RESULT = REPO / "docs" / "rearchitecture" / "spec-reconciliation" / "TEXT-LAYER-DIFF-1985.json"
CLASSES = ("OCR-NOISE", "CORRECTION", "FIGURE", "UNCOVERED", "UNEXPLAINED")

PAGE_HEADING = re.compile(r"^## Page (\d+)\s*$")
TAG = re.compile(r"</?[A-Za-z][^>]*>")
BOX = re.compile(r"[─-╿▀-▟║]")
LIGATURES = {"ﬀ": "ff", "ﬁ": "fi", "ﬂ": "fl", "ﬃ": "ffi", "ﬄ": "ffl"}
QUOTES = str.maketrans({"‘": "'", "’": "'", "“": '"', "”": '"', "–": "-", "—": "-",
                        "−": "-", "­": "-", "‐": "-", "‑": "-", " ": " "})


def fold(word: str) -> str:
    """The comparison key of a word: ligatures expanded, accents and case dropped, the OCR confusions folded
    (l, I, 1, | to one letter; O to 0; rn to m), every character that is not a letter or digit dropped."""
    for lig, rep in LIGATURES.items():
        word = word.replace(lig, rep)
    word = unicodedata.normalize("NFKD", word.translate(QUOTES))
    word = "".join(c for c in word if not unicodedata.combining(c)).lower()
    word = word.replace("rn", "m")
    word = re.sub(r"[l1i|!]", "1", word)
    word = word.replace("o", "0").replace("z", "2").replace("s", "5")
    return re.sub(r"[^a-z0-9]", "", word)


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


# ---------------------------------------------------------------- the ledger's explanations

def ledger_texts() -> dict[int, list[str]]:
    """page -> the `now` and `was` texts the ledger records for it (each a deliberate difference from the printed page)."""
    data = json.loads(LEDGER.read_text(encoding="utf-8"))
    by_page: dict[int, list[str]] = {}
    for c in data["claims"]:
        for e in c.get("edits", []) or []:
            for k in ("now", "was"):
                if e.get(k):
                    by_page.setdefault(c["page"], []).append(e[k])
    for s in data["sweeps"]:
        for k in ("now", "was"):
            if s.get(k):
                by_page.setdefault(s["page"], []).append(s[k])
    return by_page


def note_texts(text: str) -> dict[int, list[str]]:
    """page -> the backticked quotes of its `> ⚠ CORRECTED` notes."""
    out: dict[int, list[str]] = {}
    page = None
    for line in text.splitlines():
        m = PAGE_HEADING.match(line)
        if m:
            page = int(m.group(1))
        elif page and line.lstrip().startswith("> ⚠"):
            out.setdefault(page, []).append(line)
    return out


def explained_keys(texts: list[str]) -> set[str]:
    keys: set[str] = set()
    for t in texts:
        keys.update(k for k in (fold(w) for w in words_of_markdown(t)) if k)
    return keys


# ---------------------------------------------------------------- the diff

def clip(text: str, limit: int = 120) -> str:
    """A record keeps the head of a long difference; `words` carries its full size."""
    return text if len(text) <= limit else text[:limit] + " ..."


def similar(x: str, y: str) -> bool:
    """Two folded words a few damaged letters apart: the OCR's reading of the printed word."""
    if not x or not y:
        return False
    return difflib.SequenceMatcher(None, x, y, autojunk=False).ratio() >= (0.6 if min(len(x), len(y)) >= 3 else 0.5)


def vocabulary(pages: dict[int, list[tuple[str, bool, int]]]) -> set[str]:
    """The folded words the transcription itself uses at least twice: a text-layer word that is one of these but is not
    the word the Markdown has is a real difference, not a damaged letter (`of` for `or`, `which` for `that`)."""
    seen: collections.Counter[str] = collections.Counter()
    for words in pages.values():
        seen.update(fold(w) for w, _, _ in words if re.fullmatch(r"[A-Za-z][A-Za-z-]*[,.;:]?", w))
    return {w for w, n in seen.items() if n >= 2 and len(w) >= 2 and not set(w) <= {"0", "1"}}


CLEAN_WORD = re.compile(r"[A-Za-z][A-Za-z-]*[,.;:)]?")
NUMBER = re.compile(r"\(?\d+(?:\.\d+)*\)?[,.;:]?")


def is_real(word: str, key: str, vocab: set[str]) -> bool:
    """A text-layer word the document itself uses (or a plain number): its difference from the Markdown is not a damaged
    letter."""
    return (key in vocab and bool(CLEAN_WORD.fullmatch(word))) or bool(NUMBER.fullmatch(word))


def join_line_end_hyphens(words: list[str], keys: set[str]) -> list[str]:
    """`copy-` `ing` at a line end is the Markdown's `copying`: join the two when the joined word is one the page has."""
    out: list[str] = []
    i = 0
    while i < len(words):
        w = words[i]
        if w.endswith("-") and len(w) > 2 and i + 1 < len(words) and fold(w[:-1] + words[i + 1]) in keys:
            out.append(w[:-1] + words[i + 1])
            i += 2
        else:
            out.append(w)
            i += 1
    return out


def classify_page(pdf: list[str], md: list[tuple[str, bool, int]], explain: set[str], vocab: set[str],
                  removed: bool = False) -> list[dict]:
    """The residual differences of one page, each a record {class, reason, pdf, md, line}.

    `removed` is a page whose printed content the transcription deliberately holds elsewhere (the base index, PDF pages
    807 to 819, replaced by Amendment 2's: Errata C337): its text-layer words are CORRECTION."""
    md_keys = {fold(w) for w, _, _ in md}
    pdf = join_line_end_hyphens(pdf, md_keys)
    a = [(w, fold(w)) for w in pdf]
    a = [x for x in a if x[1]]
    keep = [i for i, (w, _, _) in enumerate(md) if fold(w)]
    b = [(md[i][0], fold(md[i][0])) for i in keep]
    figure = [md[i][1] for i in keep]
    line = [md[i][2] for i in keep]
    sm = difflib.SequenceMatcher(None, [k for _, k in a], [k for _, k in b], autojunk=False)
    rows: list[dict] = []

    def emit(cls: str, reason: str, pw: list[tuple[str, str]], mw: list[tuple[str, str]], j: int) -> None:
        ln = line[min(j, len(line) - 1)] if line else 0
        rows.append({"class": cls, "reason": reason, "pdf": clip(" ".join(w for w, _ in pw)),
                     "md": clip(" ".join(w for w, _ in mw)), "line": ln, "words": [len(pw), len(mw)]})

    blocks = []  # [left_p, left_m, figure, j]: the words of a differing block no damaged reading pairs
    for tag, i1, i2, j1, j2 in sm.get_opcodes():
        if tag == "equal":
            continue
        pa, mb = list(a[i1:i2]), list(b[j1:j2])
        fig = any(figure[j1:j2]) or (j2 == j1 and j1 < len(figure) and figure[j1])
        left_p, left_m = list(pa), list(mb)
        paired = False
        if pa and mb and len(pa) * len(mb) <= 400:
            for item in list(pa):
                pk = item[1]
                best = max(left_m, key=lambda m: difflib.SequenceMatcher(None, pk, m[1], autojunk=False).ratio(), default=None)
                if best is not None and similar(pk, best[1]) and (not is_real(item[0], pk, vocab) or pk == best[1]):
                    left_p.remove(item)
                    left_m.remove(best)
                    paired = True
        if paired:
            emit("OCR-NOISE", "damaged letters", pa, mb, j1)
        if "".join(k for _, k in left_p) == "".join(k for _, k in left_m):
            continue  # joined or split differently (`FILE - CONTROL`)
        blocks.append([left_p, left_m, fig, j1])

    # words that moved: the same word left over on both sides of the page, in another order (two columns, a table read by
    # column): a reading-order difference of the layer, not a difference of words
    pdf_n = collections.Counter(k for blk in blocks for _, k in blk[0])
    md_n = collections.Counter(k for blk in blocks for _, k in blk[1])
    cancel = {k: min(n, md_n[k]) for k, n in pdf_n.items() if md_n[k]}
    for side in (0, 1):
        budget = dict(cancel)
        for blk in blocks:
            kept = []
            for item in blk[side]:
                if budget.get(item[1], 0) > 0:
                    budget[item[1]] -= 1
                else:
                    kept.append(item)
            blk[side] = kept
    for left_p, left_m, fig, j in blocks:
        if not left_p and not left_m:
            continue
        if removed and left_p and not left_m:
            emit("CORRECTION", "printed content held elsewhere by a recorded correction", left_p, left_m, j)
            continue
        everything = [k for _, k in left_p + left_m]
        if everything and all(k in explain for k in everything):
            emit("CORRECTION", "ledger or correction note names it", left_p, left_m, j)
            continue
        if fig:
            emit("FIGURE", "inside a drawn figure", left_p, left_m, j)
            continue
        real_p = [x for x in left_p if is_real(x[0], x[1], vocab)]
        real_alpha = [x for x in real_p if not NUMBER.fullmatch(x[0])]
        if left_p and not real_alpha and not left_m:
            emit("OCR-NOISE", "stray text-layer mark", left_p, left_m, j)
        elif not left_p:
            emit("UNCOVERED", "Markdown words the text layer lacks", left_p, left_m, j)
        elif not left_m:
            emit("UNEXPLAINED", "text layer has words the Markdown lacks", left_p, left_m, j)
        elif not real_p:
            emit("OCR-NOISE", "damaged letters", left_p, left_m, j)
        else:
            emit("UNEXPLAINED", "a different word", left_p, left_m, j)
    return rows


def parse_pages(spec: str, total: int) -> list[int]:
    out: list[int] = []
    for part in spec.split(","):
        lo, _, hi = part.partition("-")
        out.extend(range(int(lo), int(hi or lo) + 1))
    return [p for p in out if 1 <= p <= total]


def verdict(result: dict) -> int:
    pages = result["pages"]
    unexplained = sum(p["counts"].get("UNEXPLAINED", 0) for p in pages.values())
    on = sum(1 for p in pages.values() if p["counts"].get("UNEXPLAINED", 0))
    totals = {c: sum(p["counts"].get(c, 0) for p in pages.values()) for c in CLASSES}
    print("  " + "  ".join(f"{c}={totals[c]}" for c in CLASSES))
    print(f"=== TEXT LAYER DIFF 1985: {unexplained} unexplained difference(s) on {on} page(s) ===")
    return 1 if unexplained else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--pages", help="a range such as 30-40,200: measure these only and print, write nothing")
    ap.add_argument("--check", action="store_true", help="read the committed result, no PDF needed")
    ap.add_argument("--verbose", action="store_true", help="print each UNEXPLAINED record")
    ap.add_argument("--all", action="store_true", help="print every record, not only UNEXPLAINED")
    args = ap.parse_args()
    if args.check:
        return verdict(json.loads(RESULT.read_text(encoding="utf-8")))
    if not PDF.is_file():
        sys.exit(f"FATAL: {PDF} is missing: git submodule update --init specs-private")
    text = MARKDOWN.read_text(encoding="utf-8")
    md = markdown_pages(text)
    total = max(md)
    pages = parse_pages(args.pages, total) if args.pages else list(range(1, total + 1))
    pdf = pdf_pages(pages)
    ledger, notes = ledger_texts(), note_texts(text)
    vocab = vocabulary(md)
    result = {"edition": 1985, "pdf_pages": total, "classes": list(CLASSES), "pages": {}}
    for n in pages:
        explain = explained_keys(ledger.get(n, []) + notes.get(n, []))
        rows = classify_page(pdf[n], md[n], explain, vocab, 807 <= n <= 819)
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
    return verdict(result)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
