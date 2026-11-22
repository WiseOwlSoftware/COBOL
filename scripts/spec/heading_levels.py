#!/usr/bin/env python3
"""The heading levels of the 1985 transcription, derived from the printed Table of Contents (kb/Work PB6057).

THE RULE (owner 2026-11-22: "Shouldn't the PDF Table of Contents provide the correct heading structure?"). The printed
contents (PDF pages 9-15) list every Section, chapter (`6.`) and clause (`6.18 The INSPECT Statement ... VI-94`) with its
folio, and below the clause the number fixes the depth. A Section is level 1, an Appendix level 2, a chapter `n.` level 2, a
clause `n.n` level 3, and each further number one level deeper: a numbered heading's level is its number's depth plus one, at most 6 (Markdown's deepest; the 14 headings of six and seven parts share it).
A numbered title line the printed page sets as a heading is a heading; the running header at the top of a page (the
module and clause names, `Sequential I-O - Input-Output Section`) is no heading.

THE SCOPE is the standard itself, from Section I, and Amendment 1's Section A (the intrinsic function module, set out as
the other modules are), to the part heading of Amendment 2. Amendment 1's addendum and Amendment 2 quote the standard's
clause numbers as change instructions and print their own structure (kb/Work PB6057 Sites).

A heading's level is markup. A citation locator is the number (`citation.ISO_HEADING` reads `#{1,6}`) and the rule catalog
types a block by its title, so a heading changed here leaves both identical; `heading_levels.py --check` is the
`verify_publishable.py --edition 1985` audit, and `normalize` is the one function that applies the rule.

    python scripts/spec/heading_levels.py            # the audit of specs/ANSI_COBOL_1985.md (its cases: scripts/selftests/spec/heading_levels/)
"""
from __future__ import annotations

import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402  (THE printed folio of the 1985 structure)
import spec_pdf  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: What may open a heading or title line besides its number: a level-2 box side, bold, an underline.
OPENERS = r"(?:║|\*\*|<u>)*"
CLOSERS = r"(?:║|\*\*|</u>)*"
#: A heading line: its hashes, then the line.
HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*$")
#: A numbered title: `4.4.3 Subscripting Example`, `## 4. <u>TABLE HANDLING</u>`, `║**3.4.1 Function**║`, `2.1A SUBSTANTIVE ...`.
NUMBERED = re.compile(rf"^({OPENERS})(\d+(?:\.\d+)*)([A-Z]?)(\.?)\s+(\S.*?)\s*$")
#: A section or an appendix title (heading text, markup before it dropped).
SECTION = re.compile(rf"^{OPENERS}SECTION ([IVX]+|A):\s")
APPENDIX = re.compile(rf"^{OPENERS}APPENDIX [A-F]:\s")
#: A plain line that is a clause title: a number of at least two parts and a short capitalised title with no sentence end.
PLAIN_TITLE = re.compile(rf"^{OPENERS}\d+(?:\.\d+)+[A-Z]?\s+[A-Z][A-Za-z0-9 ,/:().'-]{{2,95}}{CLOSERS}$")
#: The unnumbered headings the printed pages set without an underline: page 21's title block, the index's title.
TITLE_BLOCK = re.compile(r"^(?:American National Standard for Information Systems|Programming Language – COBOL|INDEX$)")
FIRST_AMENDMENT = "# AMENDMENT 1:"
SECOND_AMENDMENT = "# AMENDMENT 2:"
SECTION_A = "# SECTION A:"
#: The first PDF page of Section I: the contents (PDF pages 9-15) and the front matter before it hold no heading to level.
BODY_FROM_PAGE = 21
FOLIO = citation.FOLIO_85
MAX_LEVEL = 6


#: The contents entries whose title differs from the heading the body prints, each read on the rendered page (PDF page
#: given): the standard's own contents and its body disagree, and the transcription keeps each as printed.
PRINTED_DIFFERENTLY: dict[tuple[str, str], str] = {
    ("XI", "1."): "page 553 prints `INTRODUCTION OF THE SORT-MERGE MODULE`, the contents `Introduction to the Sort-Merge Module`",
    ("XIII", "4.8"): "page 654 prints `THE USE AFTER STANDARD EXCEPTION PROCEDURE STATEMENT`, the contents `The USE AFTER "
                     "EXCEPTION/ERROR PROCEDURE Statement`",
}


def depth(number: str) -> int:
    return number.count(".") + 1


def level_of(text: str) -> int | None:
    """The level the printed structure gives the heading `text` (its hashes dropped), or None when it is none of the
    structured kinds: a Section 1, an Appendix 2, a numbered title its number's depth plus one."""
    if SECTION.match(text):
        return 1
    if APPENDIX.match(text):
        return 2
    m = NUMBERED.match(text)
    return min(depth(m.group(2)) + 1, MAX_LEVEL) if m else None


def body_region(lines: list[str]) -> tuple[int, int]:
    """The 0-based [first, last) lines in scope: Section I's page to Amendment 2's part heading."""
    first = next(i for i, line in enumerate(lines) if line == f"## Page {BODY_FROM_PAGE}")
    last = next((i for i, line in enumerate(lines) if line.startswith(SECOND_AMENDMENT)), len(lines))
    return first, last


def change_pages(lines: list[str]) -> tuple[int, int]:
    """The 0-based [first, last) lines of Amendment 1 before its Section A: the addendum's change instructions and the
    standard's clauses they quote, out of scope."""
    start = next((i for i, line in enumerate(lines) if line.startswith(FIRST_AMENDMENT)), None)
    if start is None:
        return 0, 0
    return start, next(i for i, line in enumerate(lines) if line.startswith(SECTION_A))


def walk(lines: list[str], first: int, last: int):
    """Yield (index, page, kind, line) for every line of [first, last) outside a fence, a figure and a table, its kind
    `heading`, `plain` (a numbered title line that is no heading) or `other`."""
    page, fence, figure = 0, False, False
    skip = change_pages(lines)
    for i in range(first, last):
        line = lines[i]
        if (m := spec_pdf.PAGE_HEADING.match(line)) is not None:
            page = int(m.group(1))
            continue
        if skip[0] <= i < skip[1]:
            continue
        if line.lstrip().startswith("```"):
            fence = not fence
        if "<pre" in line:
            figure = True
        if fence or figure or line.startswith(("|", ">")):
            if "</pre>" in line:
                figure = False
            continue
        if HEADING.match(line):
            yield i, page, "heading", line
        elif PLAIN_TITLE.match(line) and not line.rstrip().endswith((".", ",", ";")):
            yield i, page, "plain", line
        else:
            yield i, page, "other", line


def canonical(line: str) -> str | None:
    """`line` as the rule writes it, or None when it is as written or no structured line. A heading takes its level; a
    plain numbered title becomes a heading; the boxed side, bold and underline stay where the page prints them."""
    if (h := HEADING.match(line)) is not None:
        text = h.group(2)
    elif PLAIN_TITLE.match(line):
        text = line
    else:
        return None
    level = level_of(text)
    if level is None:
        return None
    return "#" * level + " " + text


def normalize(text: str) -> tuple[str, list[tuple[int, int, str, str]]]:
    """`text` with every structured line of the standard at its derived level, and the changes as (line, page, was, now)."""
    lines = text.split("\n")
    first, last = body_region(lines)
    changes = []
    for i, page, kind, line in walk(lines, first, last):
        if kind == "other":
            continue
        now = canonical(line)
        if now is not None and now != line:
            changes.append((i + 1, page, line, now))
            lines[i] = now
    return "\n".join(lines), changes


def folios(lines: list[str], first: int, last: int) -> dict[int, set[str]]:
    """The printed folios of each page of the standard, by PDF page (a reconstructed page holds more than one)."""
    out, page = {}, 0
    for i in range(first, last):
        if (m := spec_pdf.PAGE_HEADING.match(lines[i])) is not None:
            page = int(m.group(1))
        elif (f := FOLIO.match(lines[i])) is not None:
            out.setdefault(page, set()).add(f.group(1))
    return out


def plain_title(text: str) -> str:
    """A title as the contents and the body both print it, compared case-folded without its markup."""
    t = re.sub(r"\.{2,}.*$", "", text)
    t = re.sub(r"[║*]|</?u>", "", t)
    return re.sub(r"\s+", " ", t).strip().rstrip(".").casefold()


def contents(lines: list[str]) -> list[tuple[str, str, str, str]]:
    """The printed contents (PDF pages 9-15): (section, number, title, folio) for each Section, chapter and clause entry,
    `section` the roman number of the Section it falls under, `number` empty for a Section itself, `folio` empty when the
    entry prints none."""
    first = next(i for i, line in enumerate(lines) if line == "## Page 9")
    last = next(i for i, line in enumerate(lines) if line == f"## Page {BODY_FROM_PAGE}")
    rows, section = [], ""
    for line in lines[first:last]:
        if (s := re.match(rf"^{OPENERS}SECTION ([IVX]+):\s*(.*?)(?:</u>)?(?:\s*\.{{3,}}.*?\|\s*(\S+)\s*\|)?\s*$", line)) \
                or (s := re.match(rf"^\|\s*{OPENERS}SECTION ([IVX]+):\s*(.*?)</u>\s*\.*\s*\|\s*(\S+)\s*\|\s*$", line)):
            section = s.group(1)
            rows.append((section, "", s.group(2).strip(), s.group(3) or ""))
        elif (c := re.match(r"^Chapter (\d+):\s+(.*?)\s*$", line)) is not None:
            if not c.group(2).endswith("(Continued)"):  # the printed page-break repeat of a chapter's own line
                rows.append((section, c.group(1) + ".", c.group(2), ""))
        elif (c := re.match(r"^\|\s*Chapter (\d+):\s*\|?\s*(.*?)\s*\.{2,}\s*\|\s*(\S+)\s*\|\s*$", line)) is not None:
            rows.append((section, c.group(1) + ".", c.group(2), c.group(3)))
        elif re.match(r"^<u>SECTION [A-Z]:", line):
            section = ""  # an amendment's Section A: its entries are outside the standard (module docstring)
        elif (c := re.match(r"^\|\s*(\d+(?:\.\d+)*\.?)\s*\|\s*(.*?)\s*\.{2,}\s*\|\s*(\S+)\s*\|\s*$", line)) is not None:
            rows.append((section, c.group(1), c.group(2), c.group(3)))
    return rows


def heads_as(heading: str, number: str, title: str) -> bool:
    """Whether the heading text is the contents entry (`number`, `title`): the same title, and the same number where the
    body numbers it (the summary tables of Section I Chapters 2 and 3 are titled in print, not numbered)."""
    m = NUMBERED.match(heading)
    if m is None:
        return plain_title(heading) == plain_title(title)
    return m.group(2) + m.group(3) == number.rstrip(".") and plain_title(m.group(5)) == plain_title(title)


def check(text: str) -> list[str]:
    """The audit (kb/Work PB6057): every numbered heading of the standard at its number's depth plus one, no numbered title
    line left as plain text, no running header promoted to a heading, each Section heading once and on its first page,
    and every contents entry a heading of its level, on the page its folio names."""
    failures: list[str] = []
    lines = text.split("\n")
    first, last = body_region(lines)
    page_folio = folios(lines, first, last)
    heads: list[tuple[int, int, str, str, int]] = []  # (line, page, section, text, level)
    section = ""
    for i, page, kind, line in walk(lines, first, last):
        if kind == "plain":
            failures.append(f"line {i + 1} (page {page}): a numbered title left as plain text: {line[:70]!r}")
        elif kind == "heading":
            h = HEADING.match(line)
            text_, level = h.group(2), len(h.group(1))
            want = level_of(text_)
            if (s := SECTION.match(text_)) is not None:
                section = s.group(1)
                if section != "A" and f"{section}-1" not in page_folio.get(page, set()):
                    failures.append(f"line {i + 1} (page {page}): the heading of Section {section} is on page {page} "
                                    f"(folios {sorted(page_folio.get(page, set()))}), not on its first page, folio {section}-1")
            if want is None:
                if not re.search(r"<u>", text_) and not TITLE_BLOCK.match(text_):
                    failures.append(f"line {i + 1} (page {page}): a running header or title set as a heading: {line[:70]!r}")
            elif want != level:
                failures.append(f"line {i + 1} (page {page}): {line[:60]!r} is at level {level}, its number's depth gives {want}")
            heads.append((i + 1, page, section, text_, level))
    titled: dict[str, set[str]] = {}  # folio -> the underlined titles a page of it prints, heading or not
    page = 0
    for i in range(first, last):
        if (m := spec_pdf.PAGE_HEADING.match(lines[i])) is not None:
            page = int(m.group(1))
        elif (t := re.match(r"^(?:#+\s+)?<u>(.+)</u>\s*$", lines[i])) is not None and page in page_folio:
            for f in page_folio[page]:
                titled.setdefault(f, set()).add(plain_title(t.group(1)))
    seen_sections = [h for h in heads if SECTION.match(h[3])]
    names = [SECTION.match(h[3]).group(1) for h in seen_sections]
    for name in sorted(set(names)):
        if names.count(name) > 1:
            failures.append(f"Section {name} is headed {names.count(name)} times")
    for sec, number, title, folio in contents(lines):
        if not number or not sec or (sec, number) in PRINTED_DIFFERENTLY:
            continue
        hit = [h for h in heads if h[2] == sec and heads_as(h[3], number, title)]
        label = f"contents entry {sec}.{number} {title}"
        if not hit and folio and plain_title(title) in titled.get(folio, set()):
            continue  # a summary table the standard titles in print and does not number (Section I chapters 2 and 3)
        if not hit:
            failures.append(f"{label}: no heading of that number and title in Section {sec}")
        elif folio and not any(folio in page_folio.get(h[1], {folio}) for h in hit) \
                and plain_title(title) not in titled.get(folio, set()):
            failures.append(f"{label}: the heading is on page {hit[0][1]} (folios {sorted(page_folio.get(hit[0][1], set()))}), the contents print {folio}")
    return failures


def main() -> int:
    text = spec_pdf.markdown(1985).read_text(encoding="utf-8")
    failures = check(text)
    print("\n".join(f"  x {f}" for f in failures[:60]) or "  ok every heading of the 1985 standard at its derived level")
    print(f"=== HEADING LEVELS: {'RED' if failures else 'GREEN'} ({len(failures)} finding(s))")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
