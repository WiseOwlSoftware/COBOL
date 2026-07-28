#!/usr/bin/env python3
"""Lint the transcription for defects that only appear when the Markdown is RENDERED.

WHY THIS EXISTS. Every other check in this directory compares the file's TEXT against the printed page, and
each one passed both before and after the three defects it was written for. They were found by the owner
opening the file and reading it. The audits measure fidelity of content; nothing measured whether the result is
legible, and a reference document that renders as a wall of italics is useless however faithful its words are.

None of these checks needs the PDF, so this is cheap enough to run on every change.

WHAT IT CATCHES, each earned by a real defect:

  EMPHASIS    A bare `*` opens Markdown emphasis. `| * | comment indicator |`, `(operand-1 * operand-1)` and
              the PICTURE symbol `'*'` are literal asterisks, and 137 of them italicised everything downstream
              until the next stray one closed it — which also suppresses block parsing, so headings render as
              literal `######`. Literal asterisks must be written `\\*`.

  LINE-HEIGHT A box-drawing glyph is drawn spanning the full em box so consecutive `│` meet and read as one
              continuous rule. At a previewer's ordinary ~1.45 default, every row boundary opens a gap and each
              brace becomes a dotted column. A Markdown file has no stylesheet of its own, so every figure has
              to carry `style="line-height:1"` itself. See FIGURE-STYLE.md rule 8.

  HEADER-ROW  The standard's long tables break across printed pages and restart under a repeated caption and a
              repeated column header. Preserved in Markdown — where there are no pages — the repeat lands in
              the table BODY as a data row reading `Exception-name | Cat | Description`.

  RAGGED      A row whose cell count differs from its header's. This is what a mis-split table looks like from
              the inside, and it is how the Table 10 rebuild was verified.

  CAPTION     `## Table N — …` makes a caption into a section. The six that were written this way were exactly
              the tables broken across pages; the other 34 captions were already bold.

  TAGS        `<pre>` and `<u>` must balance. An unclosed `<pre>` swallows the rest of the document.

  SWALLOWED   A `<pre>` block is RAW HTML, so `<blank>` in it is a tag, not text — and every sanitizing
              renderer DROPS an unknown tag, taking the words with it while leaving the surrounding line
              intact. Figure D.6 is the first figure whose own notation is angle-bracketed (`<blank>`,
              `<Detail lines>`); written literally, ten of its lines would have gone blank on the page with
              every text-level audit still green. Such characters must be written `&lt;` / `&gt;`.

  RUN-ON LIST A table of contents, a list of figures or an index written as bare consecutive lines. Markdown
              joins consecutive lines into one paragraph, so every entry flows into the next — which is how
              the TOC, the Figures list and the whole 3,000-line index rendered. 231 such regions existed.

  LINKS       Pages were removed from the transcription and replaced by clause links, so a dangling link is a
              cross-reference the reader cannot follow.

    python scripts/spec/lint_rendering.py
    python scripts/spec/lint_rendering.py --verbose
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"

SEPARATOR = re.compile(r"^\s*\|(?:\s*:?-+:?\s*\|)+\s*$")
ROW = re.compile(r"^\s*\|.*\|\s*$")
PRE_OPEN = re.compile(r"<pre(?:\s[^>]*)?>")
INLINE_CODE = re.compile(r"`[^`]*`")
# The glyphs that only TILE at line-height 1 — the whole reason that rule exists.
BOX_DRAWING = set("─│┌┐└┘├┤┬┴┼╭╮╰╯━┃▲▼")


def code_mask(lines):
    """True for every line inside a <pre> block or a fenced block, where a `*` is already literal."""
    mask, inpre, infence = [], False, False
    for l in lines:
        s = l.strip()
        if s.startswith("<pre"):
            inpre = True
            mask.append(True)
            continue
        if s == "</pre>":
            inpre = False
            mask.append(True)
            continue
        if s.startswith("```"):
            infence = not infence
            mask.append(True)
            continue
        mask.append(inpre or infence)
    return mask


def stars(text):
    """Count asterisks that Markdown would read as emphasis: not escaped, not inside inline code."""
    return INLINE_CODE.sub("", text).replace("\\*", "").count("*")


def check_emphasis(lines, mask):
    """Emphasis may span the lines of one paragraph, so balance is counted per BLOCK — except in a table,
    where emphasis cannot cross a cell boundary and each cell can therefore be checked exactly."""
    out, block, start = [], [], 0
    for i, l in enumerate(lines + [""]):
        if i < len(lines) and mask[i]:
            continue
        if i < len(lines) and ROW.match(l) and not SEPARATOR.match(l):
            for cell in l.strip().strip("|").split("|"):
                if stars(cell) % 2:
                    out.append((i + 1, f"unbalanced emphasis in a table cell: {cell.strip()[:40]!r}"))
            continue
        if i < len(lines) and l.strip():
            if not block:
                start = i
            block.append(l)
            continue
        if block and stars(" ".join(block)) % 2:
            out.append((start + 1, f"unbalanced emphasis in this paragraph: {block[0].strip()[:60]!r}"))
        block = []
    return out


def check_runon_lists(lines, mask):
    """A run of link-per-line entries that was never written as a list.

    Markdown joins consecutive lines into ONE PARAGRAPH. A table of contents, a list of figures or an index
    written as bare consecutive lines therefore renders as a single run-on block with every entry flowing into
    the next — which is exactly how the TOC, the Figures list and the whole 3,000-line index rendered. The
    content and the links are fine; the list markers were simply never there. Nothing else in this document
    looks like this: ordinary prose paragraphs do not carry a cross-reference link on every line."""
    out, run = [], []
    for i, l in enumerate(lines + [""]):
        s = l.strip()
        linky = (i < len(lines) and not mask[i] and s and len(s) <= 150
                 and "](#" in s
                 and not re.match(r"^[-*+]\s|^\d+\.\s|^\||^#|^>", s))
        if linky:
            run.append(i)
            continue
        if len(run) >= 4:
            out.append((run[0] + 1, f"{len(run)} consecutive link lines with no list marker — "
                                    f"these render as one paragraph"))
        run = []
    return out


# `<` opening something a parser will read as a tag. `<u>`/`</u>` are the transcription's own underlining and
# are stripped before the test; nothing else in a figure is markup.
TAGGISH = re.compile(r"<[A-Za-z/!]")


def check_swallowed_tags(lines):
    """A `<word>` inside a `<pre>` block, which renders as NOTHING.

    A `<pre>` block is raw HTML: its contents are not escaped, so `<blank>` is parsed as a tag. An unknown
    tag is silently dropped by every sanitizing renderer — the line stays, the word disappears, and no
    text-level audit notices because the file still contains the characters. Figure D.6's own notation is
    angle-bracketed (`<blank>`, `<Detail lines>`, `<Control Heading lines>`), so ten of its lines are one
    unescaped character away from rendering empty. The generators escape at write time; this is the gate
    that keeps a hand edit from undoing it."""
    out, inpre = [], False
    for i, l in enumerate(lines):
        s = l.strip()
        if s.startswith("<pre"):
            inpre = True
            continue
        if s == "</pre>":
            inpre = False
            continue
        if inpre and TAGGISH.search(re.sub(r"</?u>", "", l)):
            out.append((i + 1, f"unescaped `<` inside a <pre> — renders as a dropped tag: {s[:60]!r}"))
    return out


FIG_CAPTION = re.compile(r"^\*\*(Figure [^ ]+) — ")


def check_figures(lines):
    """A figure caption with no drawn body beneath it.

    The 484 general formats are GENERATED from the printed page and are exact. The Annex D illustrations were
    drawn by hand, and seven of the fifteen were never drawn at all — what stands under the caption is the
    chart's labels in reading order, or a loose sketch of arrows. It renders as a run-on jumble of words, and
    nothing else in the document flagged it: the words are all present, so a word-conservation check passes.

    A figure's body must START right after its caption. Searching further afield finds the NEXT figure's block
    and calls the empty one clean, which is exactly what an earlier version of this check did for D.10 and
    D.14."""
    caps = [(i, FIG_CAPTION.match(l).group(1)) for i, l in enumerate(lines) if FIG_CAPTION.match(l)]
    out = []
    for n, (i, name) in enumerate(caps):
        end = caps[n + 1][0] if n + 1 < len(caps) else len(lines)
        for k in range(i + 1, min(end, len(lines))):
            if lines[k].startswith("<a id="):
                end = min(end, k)
                break
        drawn = False
        for k in range(i + 1, end):
            s = lines[k].strip()
            if s.startswith("<pre") or s.startswith("```") or s.startswith("|"):
                drawn = True
                break
            if s:
                break
        if not drawn:
            out.append((i + 1, f"{name} has no drawn body — its caption is followed by loose text"))
    return out


def check_tables(lines):
    """Ragged rows, and a repeated header row sitting in the body."""
    ragged, repeated, i = [], [], 0
    while i < len(lines):
        if SEPARATOR.match(lines[i]) and i and ROW.match(lines[i - 1]):
            header = [c.strip().replace("*", "") for c in lines[i - 1].strip().strip("|").split("|")]
            n = lines[i].count("|") - 1
            j = i + 1
            while j < len(lines) and ROW.match(lines[j]):
                if lines[j].count("|") - 1 != n:
                    ragged.append((j + 1, f"{lines[j].count('|') - 1} cells, header has {n}"))
                body = [c.strip().replace("*", "") for c in lines[j].strip().strip("|").split("|")]
                # A LAYOUT table — the multi-column reserved-word lists — carries an empty header row, and
                # its blank spacer rows match it trivially. Only a header with content can be repeated.
                if body == header and any(header):
                    repeated.append((j + 1, "the column header repeats as a body row (a page-break joint)"))
                j += 1
            i = j
        else:
            i += 1
    return ragged, repeated


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--verbose", action="store_true", help="list every occurrence, not the first few")
    # Any Markdown file, so the lint can be pointed at an older revision to prove it still fails on the
    # defects it was written for. A check that has never been seen to fail is not evidence of anything.
    ap.add_argument("file", nargs="?", default=SPEC_MD, type=pathlib.Path)
    args = ap.parse_args()
    spec = args.file
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    text = spec.read_text(encoding="utf-8")
    lines = text.splitlines()
    mask = code_mask(lines)
    findings = {}

    findings["EMPHASIS"] = check_emphasis(lines, mask)

    bare = [(i + 1, "figure opens with a bare <pre> — box-drawing rows will not meet")
            for i, l in enumerate(lines) if l.strip() == "<pre>"]
    # A ``` fence gets no line-height either, and the Annex D illustrations are drawn from the same glyphs as
    # the general formats. They were missed the first time because that fix keyed on `<pre>` and these had
    # never been `<pre>`. Key on the GLYPHS instead — that is what actually needs the line-height.
    infence, start = False, 0
    for i, l in enumerate(lines):
        if not l.strip().startswith("```"):
            continue
        if infence:
            if any(BOX_DRAWING & set(x) for x in lines[start + 1:i]):
                bare.append((start + 1, "box-drawing inside a ``` fence — a fence has no line-height, so "
                                        "the rows will not meet; use <pre style=\"line-height:1\">"))
            infence = False
        else:
            infence, start = True, i
    findings["LINE-HEIGHT"] = bare

    findings["RUN-ON LIST"] = check_runon_lists(lines, mask)

    findings["UNDRAWN FIGURE"] = check_figures(lines)

    ragged, repeated = check_tables(lines)
    findings["RAGGED"] = ragged
    findings["HEADER-ROW"] = repeated

    findings["CAPTION"] = [(i + 1, l.strip()[:70]) for i, l in enumerate(lines)
                           if re.match(r"^#{1,6}\s+Table \d+ — ", l)]

    tags = []
    for tag, opens, closes in (("pre", len(PRE_OPEN.findall(text)), text.count("</pre>")),
                               ("u", len(re.findall(r"<u>", text)), text.count("</u>"))):
        if opens != closes:
            tags.append((0, f"<{tag}> opened {opens} times, closed {closes}"))
    findings["TAGS"] = tags

    findings["SWALLOWED"] = check_swallowed_tags(lines)

    anchors = set(re.findall(r'<a id="([^"]+)"></a>', text))
    for h in re.findall(r"^\s*#{1,6}\s+(.+?)\s*$", text, re.M):
        anchors.add(re.sub(r"[^\w\- ]", "", h.replace("*", "")).strip().lower().replace(" ", "-"))
    dangling = collections.Counter(t for t in re.findall(r"\]\(#([^)]+)\)", text) if t not in anchors)
    findings["LINKS"] = [(0, f"#{t} ({n} references)") for t, n in dangling.most_common()]

    total = sum(len(v) for v in findings.values())
    width = max(len(k) for k in findings)
    for kind, rows in findings.items():
        print(f"  {kind:<{width}} : {len(rows)}")
        for line, why in (rows if args.verbose else rows[:6]):
            where = f"line {line}" if line else "document"
            print(f"      {where}: {why}")
        if not args.verbose and len(rows) > 6:
            print(f"      … and {len(rows) - 6} more (use --verbose)")

    # The file that was ACTUALLY read — the point of the positional argument is to lint an older revision,
    # and reporting the default path there names the wrong file under a verdict line.
    try:
        where = spec.resolve().relative_to(REPO)
    except ValueError:
        where = spec
    print(f"\n{where} — {len(lines):,} lines")
    if total:
        print(f"LINT FAILED — {total} rendering defects")
        return 1
    print("LINT CLEAN — the transcription renders as intended")
    return 0


if __name__ == "__main__":
    sys.exit(main())
