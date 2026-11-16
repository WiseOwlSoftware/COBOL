#!/usr/bin/env python3
"""reformat.py — THE FREE-FORM TO FIXED-FORM CONVERTER of a conformance-suite case, in place (kb/Work PB5754;
DESIGN-conformance-suite.md §3.6).

    python conformance-suite/harness/reformat.py [--dry-run] <case.cob> [<case.cob> ...]

WHY. A suite case witnesses a row at an edition only when its text is in a reference format that edition defines
(kb/Work PB5754, lineage_reads.format_problem): before 2002 that is fixed form alone, "each compiler accepts source
programs written in reference format" (§1985:IV.7.1). Fixed form is valid at every edition, so a free-form case a 1985
row needs is rewritten in fixed form IN PLACE (no twin), and then runs in fixed form at every edition it is placed at
with its expected output and diagnostic unchanged; the gate proves each conversion.

THE LAYOUT IS THE 1985 TEXT'S (§1985:IV.7), never what a compiler tolerates:
  - area A, character positions 8-11 (§1985:IV.7.2), holds what must begin there: a division header (IV.7.3.1), a
    section header (IV.7.3.2), a paragraph header or paragraph-name (IV.7.3.3), a level indicator FD, SD, CD or RD and a
    level-number 01 or 77 (IV.7.4), DECLARATIVES and END DECLARATIVES (IV.7.5) and an end program header (IV.7.6);
  - area B, position 12 on, holds every sentence and entry of a paragraph and every continued line ("Any sentence,
    entry, phrase, or clause may be continued by starting subsequent line(s) in area B", IV.7.2.2; IV.7.3.3);
  - any other data description entry "may begin any number of positions to the right of margin A" (IV.7.4): it keeps
    its column;
  - margin R is column 72, where cobol_source.py (the harness's reader) ends the program-text area: a line that would
    pass it is re-wrapped between separators, its later parts in area B;
  - a comment line has its asterisk in the indicator area, column 7 (IV.7.2.4): the suite writes `      *> text`, and
    an inline floating comment (no 1985 form) becomes a comment line of its own before the line it was on.
The header's `source-format:` becomes `fixed` and its lines move to column 7; nothing else in the header changes; line
endings stay LF. A refusal test's expected diagnostic that names a position (`(line,col)`, `at line N`) is moved with
the text it names.

IT DOES NOT GUESS. Each of these is a REFUSAL naming the file and the line, and the file is left as it was: a `>>`
directive (a debugging line among them); a tab; a literal left open at the end of a line or a free-form continuation
(the continuation rule would be needed); a code line whose kind (area A or B) it cannot decide from the tokens; a
comment-entry paragraph (the two formats read it differently); a line that cannot be re-wrapped between separators to
end by column 72; a library text another case also declares, or that is copied in two divisions; a marked line whose
comment is more than its mark; and any text whose tokens (cobol_source.tokenize, comments aside) are not the original's
in the same order. An input it cannot resolve fails loudly; a refused case is never half written.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import re
import sys
from pathlib import Path

HARNESS = Path(__file__).resolve().parent
sys.path.insert(0, str(HARNESS))

import cobol_source as cobol  # noqa: E402  (this directory's: THE reader of the program text, its tokens and comments)
import generate  # noqa: E402  (THE reader of a case's header, its editions and its refusal entry)
sys.path.append(str(HARNESS.parents[1] / "scripts"))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: The fixed-form columns (1-based) of the 1985 layout (§1985:IV.7.2): the indicator area, area A and area B.
INDICATOR, AREA_A, AREA_B = cobol.SEQUENCE_AREA + 1, cobol.SEQUENCE_AREA + 2, cobol.SEQUENCE_AREA + 2 + cobol.AREA_A_WIDTH
MARGIN_R = cobol.FIXED_TEXT_END
#: The columns a re-wrapped part of a line is indented past the line it continues.
CONTINUED_INDENT = 4
#: What a line must be placed in: area A, area B, or anywhere right of margin A (a data description entry).
A, B, ANY = "A", "B", "any"

#: THE REFUSALS, by kind (the report counts them).
DIRECTIVE, TAB, CONTINUATION, UNDECIDED, COMMENT_ENTRY, TOO_LONG, LIBRARY, MARK, ROUND_TRIP, NOT_FREE, UNREADABLE = (
    "directive", "tab", "continuation", "undecided-line", "comment-entry", "past-margin-r", "library-text",
    "marked-line", "round-trip", "not-free-form", "unreadable")

_DIVISIONS = {"IDENTIFICATION": "identification", "ID": "identification", "ENVIRONMENT": "environment",
              "DATA": "data", "PROCEDURE": "procedure"}
#: The Identification Division's paragraph headers (§2023:11.1 and the 85 text's VI.3.2), and those whose paragraph is
#: a comment-entry (cobol_source._COMMENT_ENTRY_PARAGRAPH's), which this converter refuses.
_ID_PARAGRAPHS = frozenset({"PROGRAM-ID", "FUNCTION-ID", "CLASS-ID", "INTERFACE-ID", "METHOD-ID", "FACTORY", "OBJECT",
                            "OPTIONS"})
_COMMENT_ENTRIES = frozenset({"AUTHOR", "INSTALLATION", "DATE-WRITTEN", "DATE-COMPILED", "SECURITY", "REMARKS"})
_ENV_PARAGRAPHS = frozenset({"SOURCE-COMPUTER", "OBJECT-COMPUTER", "SPECIAL-NAMES", "REPOSITORY", "FILE-CONTROL",
                             "I-O-CONTROL"})
_LEVEL_INDICATORS = frozenset({"FD", "SD", "CD", "RD"})
#: The end markers (§2023:10.7; the 85 text's end program header, IV.7.6): END and one of these begin in area A.
_END_MARKERS = frozenset({"PROGRAM", "FUNCTION", "CLASS", "INTERFACE", "METHOD", "FACTORY", "OBJECT", "DECLARATIVES"})
#: A position a refusal entry's expectation names: the compiler's `(line,col)` and a message's `at line N`.
_POSITION = re.compile(r"\((?P<line>[0-9]+),(?P<col>[0-9]+)\)")
_AT_LINE = re.compile(r"\bat line (?P<line>[0-9]+)\b")
_FREE_CONTINUATION = re.compile(r"""^-\s*["']|["']-\s*$""")


class Refused(Exception):
    """A text the converter will not convert: its kind, the file, the line and why."""

    def __init__(self, kind: str, path: str, line: int, why: str):
        super().__init__(f"{path}:{line}: [{kind}] {why}")
        self.kind, self.path, self.line, self.why = kind, path, line, why


@dataclasses.dataclass(frozen=True)
class Segment:
    """Where a run of an original line's characters went: original columns [start, end) (1-based) now begin at
    (`line`, `col`) of the converted text."""
    start: int
    end: int
    line: int
    col: int


@dataclasses.dataclass
class Converted:
    """One text converted: its new text and, for each original line number, the segments its characters went to."""
    text: str
    moved: dict[int, list[Segment]]

    def position(self, line: int, col: int) -> tuple[int, int]:
        """Where the original position (`line`, `col`) is in the converted text."""
        segs = self.moved.get(line)
        if not segs:
            raise Refused(UNREADABLE, "", line, f"a position names line {line}, which the conversion did not carry")
        seg = next((s for s in segs if s.start <= col < s.end), segs[0] if col < segs[0].start else segs[-1])
        return seg.line, seg.col + (col - seg.start)


def _tokens(text: str, fmt: str) -> list[tuple[str, str]]:
    """The program text's tokens as the harness reads them, comments aside: what a conversion must keep."""
    return [(t.kind, t.text) for t in cobol.tokenize(cobol.read_lines(text, fmt))]


def _breaks(code: str) -> list[int]:
    """The indices of the separator spaces of `code` (one line's program text) between which it may be re-wrapped:
    outside every literal and every pseudo-text. A line end is a separator in both formats (§1985:IV.7.2.2: a line not
    continued by a hyphen begins as if preceded by a space)."""
    out, quote, pseudo, i = [], None, False, 0
    while i < len(code):
        ch = code[i]
        if quote:
            if ch == quote:
                if code[i + 1:i + 2] == quote:
                    i += 2
                    continue
                quote = None
        elif ch in "\"'":
            quote = ch
        elif code.startswith("==", i):
            pseudo = not pseudo
            i += 2
            continue
        elif ch == " " and not pseudo and i and code[i - 1] != " ":
            out.append(i)
        i += 1
    return out


def _reserved_everywhere(word: str, reserved: dict[str, frozenset[str]], editions: list[str]) -> bool | None:
    """Is `word` reserved at every edition of `editions` (True), at none (False), or at some only (None: undecidable)?"""
    hits = [word.upper() in reserved[e] for e in editions]
    return True if all(hits) else False if not any(hits) else None


def classify(text: str, path: str, reserved: dict[str, frozenset[str]], editions: list[str],
             division: str | None = None, fmt: str = "free") -> dict[int, str]:
    """Each code line's placement (A, B or ANY) by the 1985 layout (module docstring), keyed by line number, from the
    text's tokens (read in `fmt`): a line that begins a sentence or entry (the token before it is a separator period)
    is placed by what it begins; every other line continues one, in area B. `editions` are those the free-form text is
    written for (where free form exists): a one-word sentence in the Procedure Division is a statement when its word
    is reserved at each, a paragraph-name when at none, and undecidable otherwise. `division` starts a library text in
    the division it is copied into. The converter asks it of the original, to place each line, and of its own fixed-
    form result, to prove each placed line (a re-wrapped part among them) is where the layout puts it."""
    toks = cobol.tokenize(cobol.read_lines(text, fmt))
    out: dict[int, str] = {}
    for i, t in enumerate(toks):
        if i and toks[i - 1].line == t.line:
            continue
        nxt = [x.upper if x.kind != cobol.PERIOD else "." for x in toks[i + 1:i + 3]] + ["", ""]
        word = t.upper if t.kind in (cobol.WORD, cobol.NUMBER) else ""
        start = i == 0 or toks[i - 1].kind == cobol.PERIOD
        if start and word in _DIVISIONS and nxt[0] == "DIVISION":
            division, place = _DIVISIONS[word], A
        elif not start or t.kind == cobol.PERIOD:
            place = B
        elif word == "END" and nxt[0] in _END_MARKERS:
            place = A
        elif word in _COMMENT_ENTRIES and nxt[0] == ".":
            raise Refused(COMMENT_ENTRY, path, t.line, f"the {word} paragraph's comment-entry is read differently in the "
                                                       "two formats")
        elif word and nxt[0] == "SECTION":
            place = A
        elif division == "identification":
            place = A if word in _ID_PARAGRAPHS and nxt[0] == "." else B
        elif division == "environment":
            place = A if word in _ENV_PARAGRAPHS and nxt[0] == "." else B
        elif division == "data" and t.kind == cobol.NUMBER and t.text.isdigit():
            place = A if int(t.text) in (1, 77) else ANY
        elif division == "data" and word in _LEVEL_INDICATORS:
            place = A
        elif division == "data" and word == "COPY":
            place = B
        elif division == "procedure" and word == "DECLARATIVES" and nxt[0] == ".":
            place = A
        elif division == "procedure" and t.kind == cobol.NUMBER and nxt[0] == ".":
            place = A                                      # an all-digit paragraph-name
        elif division == "procedure" and t.kind == cobol.WORD:
            verb = _reserved_everywhere(word, reserved, editions)
            if verb is None:
                raise Refused(UNDECIDED, path, t.line, f"{t.text} is reserved at some of {editions} only: a statement "
                                                       "or a paragraph-name")
            if not verb and nxt[0] != ".":
                raise Refused(UNDECIDED, path, t.line, f"a sentence opens with the user-defined word {t.text} that no "
                                                       "period follows")
            place = B if verb else A
        else:
            raise Refused(UNDECIDED, path, t.line, f"cannot tell what the line begins ({t.text!r}, division "
                                                   f"{division or 'none'})")
        out[t.line] = place
    return out


def _fits(place: str, col: int) -> bool:
    """Does a line beginning at column `col` satisfy its placement?"""
    return AREA_A <= col < AREA_B if place == A else col >= AREA_B if place == B else col >= AREA_A


def _place(col: int, place: str, offset: int) -> int:
    """The column a line whose program text began at `col` (1-based) begins at in fixed form: its column shifted by
    the text's `offset` when that satisfies its placement, else the first column of its area."""
    at = col + offset
    return at if _fits(place, at) else AREA_A if place == A else max(at, AREA_B if place == B else AREA_A)


def _wrap(code: str, col: int, path: str, number: int) -> list[tuple[int, int, int]]:
    """`code` (the line's program text, stripped) placed at `col`, re-wrapped between separators so each part ends by
    margin R: (index in `code` where the part starts, where it ends, the column it begins at). A part after a separator
    period starts a sentence at the line's own column in area B; any other at the continued indent."""
    parts, start, at = [], 0, col
    while len(code[start:].encode("utf-8")) + at - 1 > MARGIN_R:     # bytes: never fewer than characters
        room = MARGIN_R - at + 1
        fits = [b for b in _breaks(code) if start < b <= start + room and len(code[start:b].encode("utf-8")) <= room]
        if not fits:
            raise Refused(TOO_LONG, path, number, f"no separator lets the line end by column {MARGIN_R}")
        sentence = [b for b in fits if code[b - 1] == "."]
        cut = sentence[-1] if sentence and sentence[-1] - start > room // 2 else fits[-1]
        parts.append((start, cut, at))
        start = cut + 1
        while start < len(code) and code[start] == " ":
            start += 1
        at = max(col, AREA_B) if code[cut - 1] == "." else max(col, AREA_B) + CONTINUED_INDENT
    parts.append((start, len(code), at))
    return parts


def convert_text(text: str, path: str, reserved: dict[str, frozenset[str]], editions: list[str],
                 division: str | None = None, header: bool = True) -> Converted:
    """`text`, a free-form case (with its metadata block when `header`) or library text, in fixed form (module
    docstring), or a `Refused`."""
    lines = text.split("\n")
    end = 0
    if header:
        h, problems = generate.read_header(text)
        if h is None or problems:
            raise Refused(UNREADABLE, path, problems[0][0] if problems else 1, "its metadata block does not read")
        if h.fields.get("source-format") != "free" or h.source_format_seen != "free":
            raise Refused(NOT_FREE, path, h.lines.get("source-format", 1), "the case is not in free form")
        end = h.end_line
    codes = {}
    for n, raw in enumerate(lines, start=1):
        if "\t" in raw:
            raise Refused(TAB, path, n, "a tab, whose column is the implementor's")
        if "\r" in raw:
            raise Refused(UNREADABLE, path, n, "a carriage return: the suite's line endings are LF")
        if n <= end or not raw.strip() or raw.lstrip().startswith("*>"):
            continue
        code, comment, quote = cobol.floating_comment(raw)
        if code.strip().startswith(">>"):
            raise Refused(DIRECTIVE, path, n, "a compiler directive (a debugging line among them)")
        if quote or _FREE_CONTINUATION.search(code.strip()):
            raise Refused(CONTINUATION, path, n, "a literal continued across lines needs the continuation rule")
        codes[n] = (code, comment)
    places = classify(text, path, reserved, editions, division)
    starts = [len(c) - len(c.lstrip()) + 1 for c, _ in codes.values() if c.strip()]
    offset = AREA_A - 1 if starts and min(starts) < AREA_A else 0
    out: list[str] = []
    moved: dict[int, list[Segment]] = {}
    for n, raw in enumerate(lines, start=1):
        if n <= end or (raw.lstrip().startswith("*>") and n not in codes):
            if not raw.strip():
                out.append(raw)
                continue
            body = raw.lstrip()
            fixed = raw if raw.startswith(" " * cobol.SEQUENCE_AREA + "*>") else " " * cobol.SEQUENCE_AREA + body
            moved[n] = [Segment(len(raw) - len(body) + 1, len(raw) + 1, len(out) + 1, len(fixed) - len(body) + 1)]
            out.append(fixed)
            continue
        if n not in codes:
            moved[n] = [Segment(1, len(raw) + 1, len(out) + 1, 1)]
            out.append("")
            continue
        code, comment = codes[n]
        seq = " " * cobol.SEQUENCE_AREA
        if comment is not None:
            if generate.FREE_MARK.fullmatch(comment.rstrip()):
                seq = generate.FIXED_MARK.ljust(cobol.SEQUENCE_AREA)
            elif generate.FREE_MARK.search(comment):
                raise Refused(MARK, path, n, "a marked line whose comment holds more than its mark")
            else:
                out.append(" " * cobol.SEQUENCE_AREA + comment.rstrip())
        if not code.strip():
            moved[n] = [Segment(1, len(raw) + 1, len(out) + 1, 1)]
            out.append(seq.rstrip())
            continue
        place = places.get(n)
        if place is None:
            raise Refused(UNDECIDED, path, n, "a line of program text the token reader assigns no token to")
        col = len(code) - len(code.lstrip()) + 1
        body = code.strip()
        segs = []
        for s, e, at in _wrap(body, _place(col, place, offset), path, n):
            line = (seq if not segs else " " * cobol.SEQUENCE_AREA) + " " * (at - cobol.SEQUENCE_AREA - 1) + body[s:e]
            segs.append(Segment(col + s, col + e + 1, len(out) + 1, at))
            out.append(line.rstrip())
        moved[n] = segs
    new = "\n".join(out)
    if header:
        new = re.sub(r"(?m)^(\s*\*>\s*source-format:\s*)free\s*$", r"\1fixed", new, count=1)
    if _tokens(new, "fixed") != _tokens(text, "free"):
        raise Refused(ROUND_TRIP, path, 1, "the fixed-form text's tokens are not the original's")
    if bad := cobol.program_text_past_margin(new):
        raise Refused(TOO_LONG, path, bad[0], f"program text past column {MARGIN_R}")
    new_lines = new.split("\n")
    for number, place in classify(new, path, reserved, editions, division, "fixed").items():
        raw = new_lines[number - 1]
        at = len(raw) - len(raw[AREA_A - 1:].lstrip()) + 1
        if not _fits(place, at):
            original = next((n for n, segs in moved.items() if any(s.line == number for s in segs)), number)
            raise Refused(UNDECIDED, path, original, f"the converted line {number} begins at column {at}, where the "
                                                     f"layout does not put it (area {place})")
    return Converted(new, moved)


@dataclasses.dataclass
class Plan:
    """A case's conversion, ready to write: each file's new text, and its refusal entry's new text (or None)."""
    files: dict[Path, str]
    entry: tuple[Path, str] | None


def _editions_written_for(fields: dict, entry: dict | None, order: list[str]) -> list[str]:
    """The editions where free form exists among those the case runs at (its accept, its flag's at, its refusal entry's
    refuse-at), or every such edition when it runs at none of them."""
    flag = fields.get("flag") or {}
    runs = {*(fields.get("accept") or []), *(flag.get("at") or [] if isinstance(flag, dict) else []),
            *((entry or {}).get("refuse-at") or [])}
    later = [e for e in order if generate.format_defined_at("free", e, order)]
    return [e for e in later if e in runs] or later


def plan_case(cob: Path, suite: Path = generate.SUITE, reserved: dict[str, frozenset[str]] | None = None,
              order: list[str] | None = None) -> Plan:
    """The conversion of the case `cob` and the library texts it copies, or a `Refused`; nothing is written."""
    rel = cob.relative_to(suite).as_posix()
    text = cob.read_bytes().decode("utf-8")
    h, _ = generate.read_header(text)
    if h is None:
        raise Refused(UNREADABLE, rel, 1, "no metadata block")
    order = order or generate.inventory_schema.load_schema().editions
    reserved = reserved or generate.reserved_words()
    test_id = h.fields.get("id", cob.stem)
    entry_path = suite / generate.sidecar_path(generate.DIAGNOSTICS_DIR, test_id)
    entry = json.loads(entry_path.read_text(encoding="utf-8")) if entry_path.is_file() else None
    editions = _editions_written_for(h.fields, entry, order)
    case = convert_text(text, rel, reserved, editions)
    files = {cob: case.text}
    for name, division in _libraries(text, rel, h.fields.get("files") or [], cob.parent).items():
        lib = cob.parent / name
        others = [p for p in cob.parent.glob("*.cob") if p != cob and name in (generate.read_header(
            p.read_bytes().decode("utf-8"))[0] or generate.Header({}, {}, "", 0)).fields.get("files", [])]
        if others:
            raise Refused(LIBRARY, rel, 1, f"the library text {name} is also declared by {others[0].name}")
        files[lib] = convert_text(cobol.decode(lib.read_bytes()), f"{rel}:{name}", reserved, editions, division,
                                  header=False).text
    new_entry = None
    if entry is not None:
        expect = entry["expect"]
        moved = _POSITION.sub(lambda m: "({},{})".format(*case.position(int(m["line"]), int(m["col"]))), expect)
        moved = _AT_LINE.sub(lambda m: f"at line {case.position(int(m['line']), 1)[0]}", moved)
        if moved != expect:
            raw = entry_path.read_text(encoding="utf-8")
            new_entry = (entry_path, raw.replace(json.dumps(expect)[1:-1], json.dumps(moved)[1:-1], 1))
    return Plan(files, new_entry)


def _libraries(text: str, rel: str, declared: list[str], folder: Path) -> dict[str, str]:
    """The declared library texts the case copies, each with the division it is copied into (a refusal when one is
    copied into two, or is declared and missing). A COPY of a text the case does not declare has none to convert: the
    generator refuses it in a case that runs, so only a refusal test (whose subject it may be) reaches here with one."""
    toks = cobol.tokenize(cobol.read_lines(text, "free"))
    found: dict[str, str | None] = {}
    for stmt in cobol.copy_statements(toks):
        name = next((f for f in declared if f == stmt.name or Path(f).stem == stmt.name), None)
        if name is None:
            continue        # no library text the case declares: nothing to convert (a refusal test may copy none)
        if not (folder / name).is_file():
            raise Refused(LIBRARY, rel, stmt.line, f"COPY {stmt.name}: the declared library text {name} is not there")
        at = _division_at(toks, stmt.line)
        if found.setdefault(name, at) != at:
            raise Refused(LIBRARY, rel, stmt.line, f"the library text {name} is copied into two divisions")
    return found


def _division_at(toks: list[cobol.Token], line: int) -> str | None:
    """The division the text is in at `line`: the last division header before it."""
    division = None
    for i, t in enumerate(toks):
        if t.line > line:
            break
        if t.kind == cobol.WORD and t.upper in _DIVISIONS and i + 1 < len(toks) and toks[i + 1].upper == "DIVISION":
            division = _DIVISIONS[t.upper]
    return division


def write(plan: Plan) -> None:
    """Write a plan's files (LF line endings, UTF-8, as read)."""
    for path, text in plan.files.items():
        path.write_text(text, encoding="utf-8", newline="")
    if plan.entry is not None:
        plan.entry[0].write_text(plan.entry[1], encoding="utf-8", newline="")


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Convert free-form conformance-suite cases to fixed form, in place.")
    ap.add_argument("cases", nargs="+", type=Path, help="the case files (.cob) to convert")
    ap.add_argument("--dry-run", action="store_true", help="convert and report, writing nothing")
    a = ap.parse_args(argv)
    reserved, order = generate.reserved_words(), generate.inventory_schema.load_schema().editions
    refused: dict[str, int] = {}
    done = 0
    for cob in a.cases:
        try:
            plan = plan_case(cob.resolve(), reserved=reserved, order=order)
        except Refused as r:
            refused[r.kind] = refused.get(r.kind, 0) + 1
            print(f"REFUSED {r}")
            continue
        if not a.dry_run:
            write(plan)
        done += 1
        print(f"{'would convert' if a.dry_run else 'converted'} {cob.as_posix()}"
              + (f" (and {len(plan.files) - 1} library text(s))" if len(plan.files) > 1 else "")
              + (" (its refusal entry's position moved)" if plan.entry else ""))
    print(f"reformat: {done} converted{' (dry run)' if a.dry_run else ''}, {sum(refused.values())} refused {refused}")
    return 1 if refused else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
