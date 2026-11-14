#!/usr/bin/env python3
"""csharp_lexer.py - THE ONE LEXER OF A C# SOURCE FILE in the build and gate tooling (kb/Work PB5230 review R2-1).

The two audits that read compiler-test sources, the BUILD TARGETS audit (`buildgraph.py`: what a test reads of the
repository) and the INERT LANE's Markdown audit (`inert_lane.py`: which Markdown a test names), each need to know what part
of a file is code and what is text. They used to carry a lexer each, and a form one followed and the other did not (a
verbatim literal ending in an escaped quote, a preprocessor line holding an apostrophe, an interpolation format) made the
two audits disagree on what a test runs. There is this one, and both read it: `parts` (the code with its literals blanked,
and the literals' values) and `kind_at` (what one offset is).

It follows regular, verbatim (`@"..."`), interpolated (`$"..."`, `$@"..."`) and raw (a run of three or more quotes, with any
number of leading `$`) literals, character literals, `//` and `/* */` comments and preprocessor lines, and the holes of an
interpolated string, which are code in their own right: their string literals are values too and their code stays code. A
regular literal that is never closed ends at its line's end, as the C# compiler recovers.

What an offset can be (`kind_at`):
  - "code"    top-level code;
  - "hole"   the code of an interpolated string's `{...}` (it runs, so a reader counts it as code);
  - "string" a literal's text, its quotes and its hole braces, a character literal, an interpolation's `:format`;
  - "comment" a `//` or `/* */` comment or a preprocessor line (`#if`, `#region`, `#nullable`).
"""
from __future__ import annotations

import functools
import re
from dataclasses import dataclass

CODE, COMMENT, STRING, HOLE = 0, 1, 2, 3
KIND_NAMES = ("code", "comment", "string", "hole")

#: Where a C# string literal starts: its `$` and `@` prefix (`$`, `@`, `$@`, `@$`, `$$...`) and its opening run of quotes.
STRING_START_RE = re.compile(r'[$@]*"{1,}')


@dataclass(frozen=True)
class Lexed:
    """A source lexed once: `code` (every comment, preprocessor line and character literal blanked to spaces and every
    string literal to underscores, so an offset and a line mean what they meant), `literals` (the values of the string
    literals, a hole's own before its string's) and `kinds` (one byte per character: CODE, COMMENT, STRING or HOLE)."""
    code: str
    literals: tuple[str, ...]
    kinds: bytes


@functools.lru_cache(maxsize=64)
def lex(text: str) -> Lexed:
    """`text` lexed (cached: an audit asks about many offsets of one file)."""
    kinds = bytearray(len(text))
    literals: list[str] = []
    code, _ = _lex(text, 0, literals, 0, kinds)
    return Lexed(code, tuple(literals), bytes(kinds))


def parts(text: str) -> tuple[str, list[str]]:
    """The code of a C# source with its comments, preprocessor lines, character literals blanked to spaces and its string
    literals to underscores, and the values of its string literals (see `Lexed`)."""
    lexed = lex(text)
    return lexed.code, list(lexed.literals)


def kind_at(text: str, pos: int) -> str:
    """What offset `pos` of the C# source `text` is: "code", "hole", "string" or "comment"."""
    return KIND_NAMES[lex(text).kinds[pos]]


def _blank(piece: str, fill: str) -> str:
    """`piece` with every character but a newline replaced by `fill`, so an offset and a line keep their meaning."""
    return re.sub(r"[^\n]", fill, piece)


def _mark(kinds: bytearray, start: int, end: int, kind: int) -> None:
    kinds[start:end] = bytes([kind]) * (end - start)


def _lex(text: str, i: int, literals: list[str], hole_close: int, kinds: bytearray) -> tuple[str, int]:
    """The CODE of `text` from `i`, appending the string values to `literals` and marking each character's kind in `kinds`: to
    the end of `text` (`hole_close` 0) or, inside an interpolation hole, to the run of `hole_close` closing braces that ends
    it at bracket depth 0 (returned unconsumed). A `:` at depth 0 in a hole starts its format string, which is text."""
    code: list[str] = []
    n = len(text)
    depth = 0
    here = HOLE if hole_close else CODE
    while i < n:
        ch = text[i]
        pair = text[i:i + 2]
        if hole_close and depth == 0 and text.startswith("}" * hole_close, i):
            break
        if hole_close and depth == 0 and ch == ":":
            j = text.find("}" * hole_close, i)
            j = n if j < 0 else j
            code.append(_blank(text[i:j], " "))
            _mark(kinds, i, j, STRING)
            i = j
        elif pair == "//":
            j = text.find("\n", i)
            j = n if j < 0 else j
            code.append(" " * (j - i))
            _mark(kinds, i, j, COMMENT)
            i = j
        elif pair == "/*":
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
            code.append(_blank(text[i:j], " "))
            _mark(kinds, i, j, COMMENT)
            i = j
        elif ch == "#" and not text[text.rfind("\n", 0, i) + 1:i].strip():  # a preprocessor line may hold an apostrophe
            j = text.find("\n", i)
            j = n if j < 0 else j
            code.append(" " * (j - i))
            _mark(kinds, i, j, COMMENT)
            i = j
        elif ch == "'":
            j = i + 3 if text[i + 1:i + 2] == chr(92) else i + 1  # past the backslash and the escaped character
            j = text.find("'", j)
            j = n if j < 0 else j + 1
            code.append(_blank(text[i:j], " "))
            _mark(kinds, i, j, STRING)
            i = j
        elif (m := STRING_START_RE.match(text, i)) is not None:
            piece, i = _lex_string(text, i, m, literals, kinds)
            code.append(piece)
        else:
            depth += ch in "([{"
            depth -= ch in ")]}"
            code.append(ch)
            kinds[i] = here
            i += 1
    return "".join(code), i


def _lex_string(text: str, i: int, start: re.Match[str], literals: list[str], kinds: bytearray) -> tuple[str, int]:
    """The string literal opening at `i` (`start` matched its opening): its CODE (underscores, but for the code of its
    interpolation holes), appending its value to `literals`; and the offset after it."""
    n = len(text)
    prefix = start.group(0).rstrip('"')
    quotes = len(start.group(0)) - len(prefix)
    dollars = prefix.count("$")
    verbatim = "@" in prefix
    raw = quotes >= 3 and not verbatim
    if quotes == 2 and not verbatim:  # the empty string
        literals.append("")
        _mark(kinds, i, start.end(), STRING)
        return _blank(text[i:start.end()], "_"), start.end()
    k = start.end() if (quotes == 1 or raw) else i + len(prefix) + 1
    close_run = '"' * quotes
    value: list[str] = []
    code: list[str] = []
    seg = i  # the start of the text not yet turned into CODE
    ended = False  # whether the closing quote (or run) was found, so it is consumed
    while k < n:
        c = text[k]
        if raw and text.startswith(close_run, k):
            ended = True
            break
        if not raw and c == '"':
            if verbatim and text[k + 1:k + 2] == '"':
                value.append('"')
                k += 2
                continue
            ended = True
            break
        if not raw and not verbatim and c == "\n":  # an unterminated regular literal ends at its line's end
            break
        if dollars and c == "{":
            run = len(text[k:]) - len(text[k:].lstrip("{"))
            if not raw and text[k + 1:k + 2] == "{":  # `{{` is a brace of the text
                value.append("{")
                k += 2
                continue
            if raw and run < dollars:  # fewer braces than the string's dollars are text
                value.append("{" * run)
                k += run
                continue
            value.append("{" * (run - dollars if raw else 0))
            hole_at = k + run
            _mark(kinds, seg, hole_at, STRING)
            code.append(_blank(text[seg:hole_at], "_"))
            hole, end = _lex(text, hole_at, literals, dollars, kinds)
            code.append(hole)
            seg = end
            k = min(end + dollars, n)
            continue
        if dollars and c == "}" and not raw and text[k + 1:k + 2] == "}":  # `}}` is a brace of the text
            value.append("}")
            k += 2
            continue
        if not raw and not verbatim and c == chr(92):
            value.append(text[k + 1:k + 2])
            k += 2
            continue
        value.append(c)
        k += 1
    j = k + quotes if (ended and raw) else k + 1 if ended else min(k, n)
    j = min(j, n)
    literals.append("".join(value))
    _mark(kinds, seg, j, STRING)
    code.append(_blank(text[seg:j], "_"))
    return "".join(code), j
