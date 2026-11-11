#!/usr/bin/env python3
"""reserved_word_order.py — a transcription's reserved-word list keeps the order the standard prints it in.

WHY THIS EXISTS (kb/Work PB5053, PB5060). Each standard prints its reserved words as one alphabetical list, and the list
is the one part of a transcription where a slip is invisible to every other check: a word moved, doubled or put in the
wrong column is still a word of the standard, so a count, a set comparison and a reconciliation of the page's words all
pass. PB5053 found four such entries in the 2023 list (its Addendum, C16) with a one-off check of every adjacent pair;
PB5060 ran the same check on the 1985 list and found the list in order and one layout slip, the blank line between the F
and the G words lost where Amendment 2's FUNCTION was inserted. This is that check, kept, so the next slip is refused
at the gate (`verify_publishable.py` calls `check` for every edition that has a row in `LISTS`; the gate's SPEC
CORRECTIONS audits run it).

THE RULES, each read from the printed lists themselves (1985: PDF pages 191 and 192, folios IV-45 and IV-46; 2023: PDF
pages 235 to 237, printed 205 to 207):

  1. ORDER. Each word is greater than the word before it, character by character, where a hyphen and a digit sort
     before a letter: `END` precedes `END-ADD`, `DEBUG-SUB-3` precedes `DEBUGGING`, and `I-O`, `I-O-CONTROL` precede
     `IDENTIFICATION`. That is the order of the characters' ASCII codes, so the comparison is Python's own. "Greater"
     also refuses a word entered twice.
  2. KEPT PAIRS. A pair the standard prints against rule 1 on purpose is named in the edition's row with its reason
     and the correction record that decided it. A kept pair that is no longer adjacent in the list, or no longer
     against the order, is refused: the row would then excuse nothing, or excuse the wrong thing.
  3. THE SPECIAL-CHARACTER WORDS (`+`, `>=`, ...) close the list: no word follows one. An entry that is neither a
     word in capital letters nor a special-character word (2023 printed `i-O`, its Addendum's C15) is refused.
  4. LETTER GROUPS (an edition whose row says `gaps`). The printed columns leave one blank line where the initial
     letter changes and none anywhere else. A transcription that keeps the columns (1985: one table per page, read
     down each column) keeps the blanks, so inside a column two neighbours share their initial letter and a blank
     separates two groups. The blank cells that only fill a column to the table's height are not entries.

WHICH EDITIONS. 1985 and 2023 have a row. 2002 has none: its list waits for the reconciliation of the 2002
transcription (kb/Work PB5060, the owner's 1985-before-2002 rule), and `check` says so instead of passing silently.

    python scripts/spec/reserved_word_order.py [--edition 1985|2002|2014|2023]
    python scripts/spec/reserved_word_order.py --self-test        # planted lists: no transcription, no PDF
"""
from __future__ import annotations

import argparse
import dataclasses
import pathlib
import re
import sys
from collections.abc import Callable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: A reserved word proper, and a special-character word (`+`, `>=`, `::`); an entry that is neither is a finding.
WORD = re.compile(r"^[A-Z][A-Z0-9-]*$")
SPECIAL = re.compile(r"^[^A-Za-z0-9 ]+$")

#: One printed column: its entries top to bottom, a blank line of the page as "".
Column = list[str]


@dataclasses.dataclass(frozen=True)
class WordList:
    """How one edition's transcription holds its reserved-word list, and what the standard prints against the order."""
    read: Callable[[list[str]], list[Column]]   # the transcription's lines -> the list's columns, in reading order
    gaps: bool                                  # rule 4: the columns keep the printed blank between letter groups
    kept: dict[tuple[str, str], str]            # rule 2: (word, the word printed after it) -> why it is kept


def _columns_1985(lines: list[str]) -> list[Column]:
    """The tables between the heading of Section IV paragraph 8 and Section V, each read down its columns. A table's
    first two lines are its empty header row and the delimiter row; a cell is unescaped to the characters printed."""
    start = next((i for i, line in enumerate(lines) if line.startswith("## 8. <u>COBOL RESERVED WORDS</u>")), None)
    if start is None:
        raise LookupError("the heading `## 8. <u>COBOL RESERVED WORDS</u>` is missing")
    end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith("# SECTION V")), None)
    if end is None:
        raise LookupError("the heading of Section V, which closes the list, is missing")
    tables: list[list[list[str]]] = []
    inside = False
    for line in lines[start:end]:
        if not line.startswith("|"):
            inside = False
            continue
        if not inside:
            tables.append([])
            inside = True
        tables[-1].append([_unescape(cell.strip()) for cell in line.strip().strip("|").split("|")])
    columns: list[Column] = []
    for table in tables:
        rows = table[2:]
        if widths := {len(row) for row in rows} - {len(table[0])}:
            raise LookupError(f"a row of the list's table has {sorted(widths)} cells, its header {len(table[0])}")
        for c in range(len(table[0])):
            column = [row[c] for row in rows]
            while column and not column[-1]:
                column.pop()                    # the cells that fill a shorter column to the table's height
            columns.append(column)
    return columns


def _unescape(cell: str) -> str:
    return cell.replace("\\*", "*").replace("&gt;", ">").replace("&lt;", "<")


_HEAD_2023 = re.compile(r"^#{2,}\s+8\.9\s+Reserved words\s*$")
_ITEM_2023 = re.compile(r"^- (.+)$")


def _columns_2023(lines: list[str]) -> list[Column]:
    """The list items of 8.9, as one column. A word the page breaks after a hyphen is two items, the stem and an
    indented code span (`FLOAT-NOT-A-NUMBER-` then `&nbsp;&nbsp;QUIET`), and is one entry here. The blank lines of
    this list also stand around its correction notes, so they are not the printed letter gaps and are not entries."""
    start = next((i for i, line in enumerate(lines) if _HEAD_2023.match(line)), None)
    if start is None:
        raise LookupError("the heading of 8.9 Reserved words is missing")
    end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith('<a id="section-8-10')), None)
    if end is None:
        raise LookupError("the anchor of 8.10, which closes the list, is missing")
    column: Column = []
    for line in lines[start + 1:end]:
        if not (item := _ITEM_2023.match(line)):
            continue
        entry = item.group(1).strip().strip("`")
        if entry.startswith("&nbsp;") and column and column[-1].endswith("-"):
            column[-1] += entry.replace("&nbsp;", "").strip()
        else:
            column.append(entry)
    return [column]


#: ⛔ ONE ROW PER EDITION WHOSE LIST HAS BEEN SWEPT against its rendered pages. A new edition is a reader and a row.
LISTS: dict[int, WordList] = {
    1985: WordList(_columns_1985, gaps=True, kept={}),
    2023: WordList(_columns_2023, gaps=False, kept={
        ("B-SHIFT-R", "B-SHIFT-LC"): "the two shifts, then their circular forms (the 2023 Addendum, C16)",
        ("FLOAT-BINARY-64", "FLOAT-BINARY-128"): "the FLOAT- words are ordered by width (the 2023 Addendum, C16)",
    }),
}


def check_columns(columns: list[Column], kept: dict[tuple[str, str], str], gaps: bool) -> list[str]:
    """Rules 1 to 4 of the module docstring over one list. Each finding names its words; none prints the list."""
    findings: list[str] = []
    entries = []
    for entry in (entry for column in columns for entry in column if entry):
        if WORD.match(entry) or SPECIAL.match(entry):
            entries.append(entry)
        else:
            findings.append(f"the entry {entry!r} is neither a reserved word (capital letters, digits and hyphens, a "
                            "letter first) nor a special-character word")
    used: set[tuple[str, str]] = set()
    for before, after in zip(entries, entries[1:]):
        if not WORD.match(before):
            if WORD.match(after):
                findings.append(f"the word {after} follows the special-character word {before}: the special-character "
                                "words close the list")
            continue
        if not WORD.match(after) or before < after:
            continue
        if before == after:
            findings.append(f"{before} is entered twice")
        elif (before, after) in kept:
            used.add((before, after))
        else:
            findings.append(f"{after} follows {before}, out of the list's order (a hyphen and a digit sort before a letter)")
    for pair in sorted(set(kept) - used):
        findings.append(f"the kept pair {pair[0]}, {pair[1]} is not in the list as an adjacent pair against the order: "
                        "its row excuses nothing (reserved_word_order.LISTS)")
    if gaps:
        for column in columns:
            findings.extend(_gap_findings(column))
    return findings


def _group(entry: str) -> str:
    """The letter group an entry is printed in: its initial letter (a malformed word's too, which is refused as
    itself and not again as a gap), and one group for the special-character words."""
    return entry[0].upper() if entry[0].isalpha() else ""


def _gap_findings(column: Column) -> list[str]:
    findings: list[str] = []
    previous, blanks = None, 0
    for entry in column:
        if not entry:
            blanks += 1
            continue
        if previous is not None:
            same = _group(previous) == _group(entry)
            if same and blanks:
                findings.append(f"a blank line stands between {previous} and {entry}, inside one letter group")
            elif not same and blanks != 1:
                findings.append(f"{'no blank line stands' if not blanks else f'{blanks} blank lines stand'} between "
                                f"{previous} and {entry}: one separates two letter groups")
        previous, blanks = entry, 0
    return findings


def check(text: str, edition: int) -> list[str]:
    """The findings against `edition`'s reserved-word list in `text`; an edition with no row has one finding, which
    says its list has not been swept."""
    if edition not in LISTS:
        return [f"the {edition} reserved-word list has no row in reserved_word_order.LISTS: it has not been swept "
                "against its rendered pages (kb/Work PB5060)"]
    row = LISTS[edition]
    try:
        columns = row.read(text.splitlines())
    except LookupError as missing:
        return [f"the {edition} reserved-word list cannot be read: {missing}"]
    if not any(columns):
        return [f"the {edition} reserved-word list is empty"]
    return check_columns(columns, row.kept, row.gaps)


def main(edition: int) -> int:
    spec_md = spec_pdf.markdown(edition)
    if not spec_md.exists():
        sys.exit(f"FATAL: {spec_md} not found")
    findings = check(spec_md.read_text(encoding="utf-8"), edition)
    for finding in findings:
        print(f"  ✗ {finding}")
    print(f"=== RESERVED WORD ORDER {edition}: {'RED - ' + str(len(findings)) + ' finding(s)' if findings else 'GREEN'} ===")
    return 1 if findings else 0


def self_test() -> int:
    """Every rule on planted lists: no transcription, no PDF."""
    failures: list[str] = []

    def expect(name: str, cond: bool) -> None:
        print(("  ok    " if cond else "  FAIL  ") + name)
        if not cond:
            failures.append(name)

    def table(*columns: Column) -> str:
        height = max(len(column) for column in columns)
        rows = [[column[r] if r < len(column) else "" for column in columns] for r in range(height)]
        body = ["|" + "".join(f" {cell} |" if cell else " |" for cell in row) for row in rows]
        return "\n".join(["|" + " |" * len(columns), "|" + "---|" * len(columns), *body])

    def text_1985(*pages: str) -> str:
        return "\n\n".join(["## 8. <u>COBOL RESERVED WORDS</u>", "The following is a list.", *pages,
                            "# SECTION V: COMPOSITE LANGUAGE SKELETON", "| NOT | A | LIST |"]) + "\n"

    page_1 = table(["ADD", "ALL", "", "BY", "", "CALL", "CD"], ["CF", "", "DATA", "DE", "DEBUG-SUB-3", "DEBUGGING"],
                   ["END", "END-ADD", "", "I-O", "I-O-CONTROL", "IF"])
    page_2 = table(["NO", "NOT", "", "ZERO"], ["ZEROS", "", "+", "\\*", "&gt;="])
    good = text_1985(page_1, page_2)
    expect("a 1985 list in order passes: a hyphen and a digit before a letter, the fill cells of a short column not read",
           check(good, 1985) == [])
    expect("the 1985 reader reads each table down its columns and unescapes the special-character words",
           LISTS[1985].read(good.splitlines())[3:] == [["NO", "NOT", "", "ZERO"], ["ZEROS", "", "+", "*", ">="]])
    expect("two words exchanged are refused, naming both",
           check(good.replace("| DE |", "| DATE |").replace("| DATA |", "| DAY |"), 1985)
           == ["DATE follows DAY, out of the list's order (a hyphen and a digit sort before a letter)"])
    expect("a word that sorts a letter before a hyphen is refused (DEBUGGING before DEBUG-SUB-3)",
           len(check(text_1985(table(["DEBUGGING", "DEBUG-SUB-3"])), 1985)) == 1)
    expect("a word entered twice is refused",
           check(text_1985(table(["ADD", "ADD"])), 1985) == ["ADD is entered twice"])
    expect("an order slip across two columns and across two pages is refused",
           len(check(text_1985(table(["CALL", "CD"], ["BY"])), 1985)) == 1
           and len(check(text_1985(table(["NO"]), table(["ADD"])), 1985)) == 1)
    expect("a word after a special-character word is refused",
           any("close the list" in f for f in check(text_1985(table(["ZERO", "", "+", "", "ZEROS"])), 1985)))
    lost_gap = text_1985(table(["FROM", "FUNCTION", "GENERATE", "GIVING"]))
    expect("a lost blank between two letter groups is refused (FUNCTION, GENERATE: what PB5060 found)",
           check(lost_gap, 1985) == ["no blank line stands between FUNCTION and GENERATE: one separates two letter groups"])
    expect("a blank inside a letter group and a doubled blank between two groups are refused",
           len(check(text_1985(table(["FOR", "", "FROM"])), 1985)) == 1
           and len(check(text_1985(table(["FROM", "", "", "GO"])), 1985)) == 1)
    expect("the special-character words are a group of their own: they need the blank after the last word",
           len(check(text_1985(table(["ZERO", "+", "-"])), 1985)) == 1)
    expect("an entry that is neither a word nor a special-character word is refused once, as itself (2023 printed `i-O`)",
           check(text_1985(table(["HIGH-VALUES", "", "i-O", "IF"])), 1985)
           == ["the entry 'i-O' is neither a reserved word (capital letters, digits and hyphens, a letter first) nor a "
               "special-character word"])
    expect("a 1985 text without the list's heading, or with an empty list, is refused, never passed",
           len(check("# SECTION V\n", 1985)) == 1 and len(check(text_1985(), 1985)) == 1)
    expect("a table row with a cell too many is refused",
           len(check(good.replace("| DATA |", "| DATA | DATE |"), 1985)) == 1)

    def text_2023(*items: str) -> str:
        return "\n".join(["### 8.9 Reserved words", "", "The following is the list of reserved words:", "",
                          *(f"- {item}" for item in items), "", "> a note between two entries", "",
                          '<a id="section-8-10"></a>', "- NOT-IN-THE-LIST"]) + "\n"

    kept_2023 = ["B-AND", "B-SHIFT-L", "B-SHIFT-R", "B-SHIFT-LC", "B-SHIFT-RC", "B-XOR", "BY", "FLOAT-BINARY-32",
                 "FLOAT-BINARY-64", "FLOAT-BINARY-128", "FLOAT-NOT-A-NUMBER", "FLOAT-NOT-A-NUMBER-",
                 "`&nbsp;&nbsp;QUIET`", "FLOAT-NOT-A-NUMBER-", "`&nbsp;&nbsp;SIGNALING`", "FLOAT-SHORT", "ZEROS",
                 "`+`", "`>>`"]
    expect("a 2023 list passes: its two kept pairs, a word wrapped after its hyphen joined, no letter gaps read",
           check(text_2023(*kept_2023), 2023) == [])
    expect("the 2023 reader joins a wrapped word and reads nothing past the anchor of 8.10",
           "FLOAT-NOT-A-NUMBER-SIGNALING" in LISTS[2023].read(text_2023(*kept_2023).splitlines())[0]
           and "NOT-IN-THE-LIST" not in LISTS[2023].read(text_2023(*kept_2023).splitlines())[0])
    expect("a 2023 slip is refused beside the kept pairs (END-RECEIVE before END-READ, the 2023 Addendum's C16)",
           len(check(text_2023(*kept_2023[:7], "END-RECEIVE", "END-READ", *kept_2023[7:]), 2023)) == 1)
    expect("a kept pair that is no longer adjacent is refused, each once",
           len(check(text_2023("B-SHIFT-R", "B-SHIFT-RC", "FLOAT-BINARY-128"), 2023)) == 2)
    expect("an edition with no row is a finding that names the register note, never a pass",
           len(check(good, 2002)) == 1 and "PB5060" in check(good, 2002)[0])
    expect("every row's kept pairs are against the order, so none excuses a pair the rule already passes",
           all(not before < after for row in LISTS.values() for before, after in row.kept))
    print(f"=== RESERVED WORD ORDER SELF-TEST: {'FAIL - ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="run the planted-list self-test")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    sys.exit(self_test() if args.self_test else main(args.edition))
