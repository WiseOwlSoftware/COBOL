#!/usr/bin/env python3
"""Replace every general format in the transcription with one generated from the printed page.

KEYED ON THE CLAUSE HIERARCHY, NEVER ON THE PAGE. A page number is a layout artifact — figures straddle page
breaks, and one clause's formats routinely span two pages — so a sweep keyed on "the nth block of page N"
mis-targets, and a mis-targeted replacement still looks like a figure. A figure's identity is its CLAUSE and
its FORMAT number (14.9.8.2 Format 2), which is what the printed page and the transcription agree on.

THE GATE, which is what makes this safe: a replacement is applied ONLY where the generated figure carries the
SAME WORDS as the text it replaces. Notation may change freely — that is the entire point of the sweep — but a
word appearing, vanishing or changing means the target is wrong or the page is one of the transcription's known
defects, and the figure is reported instead of written. `--report` shows what would happen without touching
anything; `--check` regenerates against the file and is the post-sweep regression gate.

THE TRANSCRIPTION DOES NOT FENCE EVERY FIGURE. Roughly a third of the general formats are one-liners set as
bare paragraphs — `\\>\\>compiler-instruction` on folio 54, `inline-invocation-1` and `identifier-2 object-view-1`
on folio 125. They are figures too, and they are where defects hide: folio 730 runs the NEXT format's label
into the figure line (`SET { identifier-7 } … TO identifier-8 Format 10 (data-pointer-arithmetic):`). Sweeping
only the fenced ones would leave the worst-transcribed third behind, in the old style.

    python scripts/spec/sweep_figures.py --report
    python scripts/spec/sweep_figures.py --report --pages 600 660
    python scripts/spec/sweep_figures.py --apply
    python scripts/spec/sweep_figures.py --check
"""
from __future__ import annotations

import argparse
import collections
import io
import contextlib
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

PAGE_ANCHOR = re.compile(r'^<a id="page-(\d+)"></a>\s*$')
MD_HEADING = re.compile(r"^\s*#{1,6}\s+\**(\d+(?:\.\d+)+)\s+(.+?)\**\s*$")
MD_FORMAT = re.compile(r"^\s*\**Formats?\s+(\d+)")
MD_GENERAL_FORMAT = re.compile(r"general\s+formats?\s*$", re.I)
FENCE = re.compile(r"^\s*(?:>\s?)?```")
FORMAT_LABEL = re.compile(r"^\s*(?:>\s?)?\*{0,2}Formats?\s+\d+\b")
HEADING = re.compile(r"^\s*#{1,6}\s")
BLOCKQUOTE = re.compile(r"^\s*>")
HTML_LINE = re.compile(r"^\s*<")

# Notation is expected to change; WORDS are not. Everything here is stripped before the comparison: the house
# box-drawing set, the Miscellaneous Technical extensions the transcription used until now, ASCII delimiters,
# and the two ellipsis spellings.
NOTATION = (set(chr(c) for c in range(0x2500, 0x2580)) | set(chr(c) for c in range(0x239B, 0x23AE))
            | set("[]{}()|…"))


def unescape(line: str) -> str:
    """Markdown escaping the transcription applies to figure text (`\\>\\>SOURCE`, `\\*`, `\\_`)."""
    return re.sub(r"\\([>*_`\[\]])", r"\1", line)


def words_of(text: str) -> collections.Counter:
    """The WORDS of a figure, with everything that is not a word removed.

    Tags, fence markers and back-ticks are markup, not content, and counting them made every comparison fail:
    the first run reported `<u>BY</u>` against `BY` and 229 "word differences" that were nothing of the kind.
    """
    # Strip a blockquote prefix, but never the first `>` of a COMPILER DIRECTIVE: `>>SOURCE FORMAT` is figure
    # text, and eating one of its angle brackets turned every directive format into a word mismatch.
    kept = [re.sub(r"^\s*>(?!>)\s?", "", l) for l in text.splitlines() if not FENCE.match(l)]
    body = re.sub(r"</?[A-Za-z][^>]*>", " ", unescape("\n".join(kept))).replace("`", " ")
    plain = "".join(" " if ch in NOTATION else ch for ch in body)
    plain = plain.replace("...", " ")
    return collections.Counter(w for w in plain.split() if w)


def figure_ish(line: str) -> bool:
    """Whether a bare markdown line could be a general format rather than prose.

    Same test the generator uses on the printed page: a figure's lower-case terms are all hyphenated
    (identifier-1, rounded-phrase), while prose is full of plain words.
    """
    # The blockquote test runs on the RAW line. Unescaping first turns the transcription's `\>\>SOURCE FORMAT`
    # into `>>SOURCE FORMAT`, which reads as a blockquote — so every compiler-directive format was discarded
    # as prose and its clause reported as having no figure at all.
    if not line.strip() or BLOCKQUOTE.match(line):
        return False
    t = unescape(line).strip()
    if HEADING.match(t) or HTML_LINE.match(t) or FORMAT_LABEL.match(t):
        return False
    if t.startswith("|") or t.startswith("---"):
        return False                                    # table row or rule
    toks = [w.strip(" []{}.,;:()<>") for w in t.split()]
    if any(re.fullmatch(r"[a-z]{2,}", w) for w in toks):
        return False                                    # a plain lower-case word: prose
    return bool(re.search(r"[a-z]+-[a-z0-9]|[A-Z]{2,}|^>>", t))


def md_index(lines):
    """{(clause, format-number) -> (start, end)} — where each figure's content lives in the transcription.

    The same structure the generator reads off the printed page: a numbered `… General format(s)` heading opens
    a region, the next heading of any level closes it, and `Format N (…)` labels divide it.
    """
    heads = [(i, m.group(1), m.group(2)) for i, l in enumerate(lines) if (m := MD_HEADING.match(l))]
    out = {}
    for n, (i, clause, title) in enumerate(heads):
        if not MD_GENERAL_FORMAT.search(title):
            continue
        end = heads[n + 1][0] if n + 1 < len(heads) else len(lines)
        marks = [(j, int(m.group(1))) for j in range(i + 1, end) if (m := MD_FORMAT.match(lines[j]))]
        if not marks:
            out[(clause, None)] = (i + 1, end)
            continue
        for k, (j, num) in enumerate(marks):
            out[(clause, num)] = (j + 1, marks[k + 1][0] if k + 1 < len(marks) else end)
    return out


def clause_spans(lines):
    """{clause -> (start, end)} for every `… General format(s)` region, labels or not."""
    heads = [(i, m.group(1), m.group(2)) for i, l in enumerate(lines) if (m := MD_HEADING.match(l))]
    out = {}
    for n, (i, clause, title) in enumerate(heads):
        if MD_GENERAL_FORMAT.search(title):
            out[clause] = (i + 1, heads[n + 1][0] if n + 1 < len(heads) else len(lines))
    return out


def targets(lines, lo, hi):
    """Ordered figure spans inside one page slice, as (start, end_exclusive, kind).

    Both shapes are found: a fenced block, and a bare run of figure-ish lines. Anything else — prose, notes,
    headings, tables, the `Format N (…)` labels — is skipped.
    """
    out, i, in_fence, start = [], lo, False, None
    run = None
    while i < hi:
        line = lines[i]
        if FENCE.match(line):
            if in_fence:
                out.append((start, i + 1, "fence"))
                in_fence = False
            else:
                if run is not None:
                    out.append((run, i, "bare"))
                    run = None
                in_fence, start = True, i
            i += 1
            continue
        if not in_fence:
            if figure_ish(line):
                if run is None:
                    run = i
            elif run is not None and not line.strip():
                pass                                     # a blank line inside a bare figure keeps the run open
            elif run is not None:
                out.append((run, i, "bare"))
                run = None
        i += 1
    if run is not None:
        out.append((run, hi, "bare"))
    # A FENCED block is a figure by construction — its lines are code, and the transcription blockquotes some
    # of them, so the prose test would throw them away. Only a BARE run has to prove it is not prose.
    return [(a, b, k) for a, b, k in out
            if k == "fence" or any(figure_ish(lines[j]) for j in range(a, b))]


def generated(doc, extract, classify_page, R):
    """Every figure in the standard, gathered under its (clause, format) key, in reading order."""
    out, failed = collections.OrderedDict(), []
    for pno in range(1, doc.page_count + 1):
        page = doc[pno - 1]
        for band in R.find_figures(page, extract):
            key = R.figure_key(doc, pno, band)
            try:
                with contextlib.redirect_stdout(io.StringIO()):
                    grid, marks = R.build(page, band[0], band[1], extract, classify_page)
            except SystemExit as exc:
                failed.append((key, pno, str(exc)))
                continue
            if grid is None:
                continue
            out.setdefault(key, []).append((pno, R.render(grid, marks, False)))
    return out, failed


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", action="store_true", help="show what would change; write nothing")
    ap.add_argument("--apply", action="store_true", help="rewrite the transcription in place")
    ap.add_argument("--check", action="store_true", help="regenerate and diff against the file (regression gate)")
    ap.add_argument("--clause", help="restrict to one clause number, e.g. 14.9.8.2")
    ap.add_argument("--limit", type=int, default=40, help="how many problems to list")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in "
                 "a PRIVATE submodule: git submodule update --init specs-private")
    import fitz

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    import render_figure as R
    from figure_extract import extract
    from figure_geometry import classify_page

    doc = fitz.open(PDF)
    lines = SPEC_MD.read_text(encoding="utf-8").splitlines()
    index = md_index(lines)
    clause_span = clause_spans(lines)
    figs, failed = generated(doc, extract, classify_page, R)

    stats = collections.Counter()
    edits, problems = [], []
    for key, parts in figs.items():
        if args.clause and key[0] != args.clause:
            continue
        stats["figures"] += 1
        clause, fmt = key
        if clause is None:
            problems.append((key, parts[0][0], "no clause could be resolved for the figure"))
            stats["no_clause"] += 1
            continue
        span = index.get(key)
        tg = targets(lines, *span) if span else []
        if span is None or len(tg) != 1:
            # The transcription does not always carry the `Format N` labels the printed page does. Fall back
            # to the CLAUSE as a whole and pair its figures in reading order — the printed page and the
            # transcription both run top to bottom, so the nth figure is the nth figure.
            whole = index.get((clause, None)) or clause_span.get(clause)
            tg_all = targets(lines, *whole) if whole else []
            ordered = [k for k in figs if k[0] == clause]
            if whole and len(tg_all) == len(ordered):
                tg = [tg_all[ordered.index(key)]]
            elif span is None:
                problems.append((key, parts[0][0],
                                 f"clause has {len(ordered)} printed figure(s) but {len(tg_all)} in the "
                                 f"transcription, and no Format labels to pair them by"))
                stats["no_target"] += 1
                continue
        body = [l for _, blk in parts for l in blk]
        if len(tg) != 1:
            problems.append((key, parts[0][0],
                             f"{len(tg)} candidate spans in the transcription, expected 1"))
            stats["ambiguous"] += 1
            continue
        a, b, _kind = tg[0]
        have = words_of("\n".join(lines[a:b]))
        want = words_of("\n".join(body))
        if have != want:
            problems.append((key, parts[0][0], f"words differ — generated-only "
                                               f"{list((want - have).elements())[:4]}, markdown-only "
                                               f"{list((have - want).elements())[:4]}"))
            stats["word_mismatch"] += 1
            continue
        repl = ["<pre>"] + body + ["</pre>"]
        if lines[a:b] == repl:
            stats["already_current"] += 1
            continue
        edits.append((a, b, repl))
        stats["replaceable"] += 1

    print(f"figures generated                : {stats['figures']}")
    print(f"  replaceable (words match)      : {stats['replaceable']}")
    print(f"  already in generated form      : {stats['already_current']}")
    print(f"  no clause resolved             : {stats['no_clause']}")
    print(f"  no matching clause region      : {stats['no_target']}")
    print(f"  ambiguous target span          : {stats['ambiguous']}")
    print(f"  words differ                   : {stats['word_mismatch']}")
    print(f"  generator failed               : {len(failed)}")

    if (args.report or args.check) and problems:
        print(f"\nneeding attention ({len(problems)}):")
        for key, pno, why in problems[:args.limit]:
            fmt = f" Format {key[1]}" if key[1] else ""
            print(f"  §{key[0]}{fmt} (folio {pno - 30})  {why}")
        if len(problems) > args.limit:
            print(f"  … and {len(problems) - args.limit} more")

    if args.apply:
        if not edits:
            print("\nnothing to apply")
            return 0
        out = list(lines)
        for a, b, repl in sorted(edits, reverse=True):
            out[a:b] = repl
        SPEC_MD.write_text("\n".join(out) + "\n", encoding="utf-8")
        print(f"\napplied {len(edits)} figure(s) to {SPEC_MD}")
        return 0
    if args.check:
        ok = not stats["replaceable"]
        print("\nCHECK: clean — every located figure matches its generated form." if ok else
              f"\nCHECK: {stats['replaceable']} figure(s) have DRIFTED from the generated form.")
        return 0 if ok else 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
