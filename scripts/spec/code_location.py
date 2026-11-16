#!/usr/bin/env python3
"""THE PYTHON TWIN of the gate's resolver of a `code-location` (`SpecTraceabilityInventoryDriftTests.SymbolResolves` and
`UnresolvedCodeLocations`, kb/Work PB762, PB3278): does `path#Symbol` name something that is in that file's CODE?

    python scripts/spec/code_location.py --parity FIXTURE        # each planted case's answer, as JSON (the C# gate compares)
    python scripts/spec/code_location.py --digests LISTFILE      # each listed C# file's declared names and code words, digested
    python scripts/spec/code_location.py --symbols FILE...       # the same, in full (to read a disagreement the gate names)

WHY A TWIN (kb/Work PB5863, 2026-11-16). A code read (`code_reads.py`) is evidence at its PIN: its `code-location` entries
are checked ONCE, by the importer, against the tree at the commit the code was read at (`git cat-file` of `<pin>:<path>`),
and a read whose code has since MOVED is owed a re-read (`code_reads.moved_since`), never a red. Both questions are asked
in Python, at import and in `plan`, so the predicate the C# gate holds the inventory's rows to is written here a second
time, and two tests keep the two one rule: `TheCodeLocationResolver_AgreesWithTheFixtureAndWithPython` (the planted cases
of `tests/doc-facts/code-location-parity-cases.json`, each with its expected answer) and
`EveryCSharpFilesSymbols_AgreeWithThePythonResolver` (every tracked C# file of `src/`: the declared names and the code
words of both engines, digested). A change to either engine that the other does not make is red.

THE RULE (the C# docstring is the authority; this restates it). A location is `<repo-relative-path>[#Symbol]` (the
schema's `code-location.pattern`). A DOCUMENT's location (a file kind of `tests/doc-facts/inventory-anchor-kinds.json`)
is `scripts/docchecks/inventory.py`'s to resolve and resolves here. Otherwise the file must exist, and a symbol must be in
its CODE:
  - a C# file: a SIMPLE name is a whole word of its TOKENS (comments, doc comments, preprocessor lines and disabled
    regions are not code; a string literal is), and a DOTTED name is a QUALIFIED name DECLARED in the file: every suffix
    of the scope path of a namespace, type, delegate, method, constructor, property, indexer (`this`), event, enum
    member, local function, field, or record-positional parameter (Roslyn's declarations, `DeclaredNames`);
  - a grammar (`.g4`) file: a whole word of its text with `//` and `/* */` comments removed;
  - any other file: a whole word of its text.
"""
from __future__ import annotations

import argparse
import dataclasses
import hashlib
import json
import pathlib
import re
import subprocess
import sys
from typing import Callable, Iterable

REPO = pathlib.Path(__file__).resolve().parents[2]
ANCHOR_KINDS = REPO / "tests" / "doc-facts" / "inventory-anchor-kinds.json"
PARITY_FIXTURE = REPO / "tests" / "doc-facts" / "code-location-parity-cases.json"

_IDENT_CHAR = re.compile(r"[A-Za-z0-9_]")
_WORD = re.compile(r"[A-Za-z0-9_]+")
GRAMMAR_COMMENTS = re.compile(r"/\*.*?\*/|//[^\n]*", re.S)
#: The C# reserved keywords that are never a type or a declared name (the predefined types are types, so they are not here).
KEYWORDS = frozenset("""abstract as base break case catch checked class const continue default delegate do else enum event
explicit extern false finally fixed for foreach goto if implicit in interface internal is lock namespace new null operator
out override params private protected public readonly ref return sealed sizeof stackalloc static struct switch this throw
true try typeof unchecked unsafe using virtual volatile while""".split())
#: The modifiers a member declaration may open with (and `ref`/`readonly`/`scoped`, which also open a `ref` type).
MODIFIERS = frozenset("""public private protected internal static readonly const volatile virtual override abstract sealed
extern unsafe new partial async required file fixed ref scoped""".split())
LOCAL_MODIFIERS = frozenset(("static", "async", "unsafe", "extern"))
#: The contextual words of a pattern: `(1, true) or (3, true) => ...` in a switch expression is an arm, never a local function.
PATTERN_WORDS = frozenset(("and", "or", "not", "when"))
#: The two-character operators kept whole (never `>>`: a generic argument list closes one `>` at a time).
_PAIRS = frozenset(("=>", "::", "->", "??", "?.", "==", "!=", "<=", "&&", "||", "++", "--", "+=", "-=", "*=", "/=",
                    "%=", "&=", "|=", "^=", ".."))


def document_kinds() -> frozenset[str]:
    """The file kinds whose anchors `scripts/docchecks/inventory.py` resolves (the one list both engines read)."""
    return frozenset(k.lower() for k in json.loads(ANCHOR_KINDS.read_text(encoding="utf-8"))["documentKinds"])


def resolved_here(path: str, kinds: frozenset[str]) -> bool:
    return pathlib.PurePosixPath(path).suffix.lower() not in kinds


def whole_word(name: str) -> re.Pattern[str]:
    return re.compile(rf"(?<![A-Za-z0-9_]){re.escape(name)}(?![A-Za-z0-9_])")


# -- the C# lexer: tokens without trivia ------------------------------------------------------------------------------------

@dataclasses.dataclass(frozen=True)
class Token:
    kind: str   # id, num, str, chr, punct
    text: str   # the token's text; for an interpolated string, its text parts and its holes' tokens joined by spaces


class _PreprocessorExpr:
    """`#if` / `#elif` expressions: identifiers, true, false, !, &&, ||, ==, !=, parentheses."""

    def __init__(self, text: str, defined: set[str]):
        self.toks = re.findall(r"&&|\|\||==|!=|[!()]|[A-Za-z_][A-Za-z0-9_]*", text)
        self.i, self.defined = 0, defined

    def value(self) -> bool:
        return self._or()

    def _peek(self) -> str | None:
        return self.toks[self.i] if self.i < len(self.toks) else None

    def _or(self) -> bool:
        v = self._and()
        while self._peek() == "||":
            self.i += 1
            v = self._and() or v
        return v

    def _and(self) -> bool:
        v = self._eq()
        while self._peek() == "&&":
            self.i += 1
            v = self._eq() and v
        return v

    def _eq(self) -> bool:
        v = self._unary()
        while self._peek() in ("==", "!="):
            op = self.toks[self.i]
            self.i += 1
            w = self._unary()
            v = (v == w) if op == "==" else (v != w)
        return v

    def _unary(self) -> bool:
        t = self._peek()
        self.i += 1
        if t == "!":
            return not self._unary()
        if t == "(":
            v = self._or()
            if self._peek() == ")":
                self.i += 1
            return v
        if t == "true":
            return True
        if t == "false":
            return False
        return t in self.defined


class _Lexer:
    """The tokens of a C# source as Roslyn's `ParseText` (no preprocessor symbol defined) has them, without trivia."""

    def __init__(self, src: str):
        self.s, self.n, self.i = src, len(src), 0
        self.defined: set[str] = set()
        # the #if stack: (the enclosing region is active, a branch of this #if was taken, this branch is active)
        self.stack: list[tuple[bool, bool, bool]] = []

    @property
    def active(self) -> bool:
        return not self.stack or self.stack[-1][2]

    def tokens(self) -> list[Token]:
        out: list[Token] = []
        line_start = True
        while self.i < self.n:
            c = self.s[self.i]
            if c == "\n":
                self.i += 1
                line_start = True
                continue
            if c in " \t\r\f\v﻿":
                self.i += 1
                continue
            if c == "#" and line_start:
                self._directive()
                line_start = True
                continue
            line_start = False
            tok = self._token()
            if tok is not None:
                out.append(tok)
        return out

    def _line(self) -> str:
        end = self.s.find("\n", self.i)
        end = self.n if end < 0 else end
        text, self.i = self.s[self.i:end], end
        return text

    def _directive(self) -> None:
        """One preprocessor line at `self.i`; when it leaves the region disabled, the disabled text is skipped too."""
        text = self._line()
        self._apply(text)
        while not self.active and self.i < self.n:
            self.i += 1   # the newline
            line = self._line()
            if line.lstrip().startswith("#"):
                self._apply(line.lstrip())

    def _apply(self, text: str) -> None:
        m = re.match(r"#\s*(\w+)\s*(.*)", text)
        if not m:
            return
        word, rest = m.group(1), re.sub(r"//.*", "", m.group(2)).strip()
        if word == "if":
            outer = self.active
            on = outer and _PreprocessorExpr(rest, self.defined).value()
            self.stack.append((outer, on, on))
        elif word == "elif" and self.stack:
            outer, taken, _ = self.stack[-1]
            on = outer and not taken and _PreprocessorExpr(rest, self.defined).value()
            self.stack[-1] = (outer, taken or on, on)
        elif word == "else" and self.stack:
            outer, taken, _ = self.stack[-1]
            self.stack[-1] = (outer, True, outer and not taken)
        elif word == "endif" and self.stack:
            self.stack.pop()
        elif self.active and word == "define" and rest:
            self.defined.add(rest.split()[0])
        elif self.active and word == "undef" and rest:
            self.defined.discard(rest.split()[0])

    def _token(self) -> Token | None:
        s, i = self.s, self.i
        c = s[i]
        if c == "/" and s.startswith("//", i):
            end = s.find("\n", i)
            self.i = self.n if end < 0 else end
            return None
        if c == "/" and s.startswith("/*", i):
            end = s.find("*/", i + 2)
            self.i = self.n if end < 0 else end + 2
            return None
        j = i
        while j < self.n and s[j] in "$@":
            j += 1
        if j < self.n and s[j] == '"' and s[i:j].count("@") <= 1 and (j > i or c == '"'):
            return self._string(i, j)
        if c == "'":
            return self._char()
        if c == "@" and i + 1 < self.n and (s[i + 1].isalpha() or s[i + 1] == "_"):
            self.i = i + 1
            word = self._word()
            return Token("id", "@" + word)
        if c.isalpha() or c == "_":
            return Token("id", self._word())
        if c.isdigit() or (c == "." and i + 1 < self.n and s[i + 1].isdigit()):
            return Token("num", self._number())
        if s[i:i + 2] in _PAIRS:
            self.i = i + 2
            return Token("punct", s[i:i + 2])
        self.i = i + 1
        return Token("punct", c)

    def _word(self) -> str:
        s, j = self.s, self.i
        while j < self.n and (s[j].isalnum() or s[j] == "_"):
            j += 1
        w, self.i = s[self.i:j], j
        return w

    def _number(self) -> str:
        s, j = self.s, self.i
        while j < self.n:
            ch = s[j]
            if ch.isalnum() or ch == "_":
                if ch in "eE" and j + 1 < self.n and s[j + 1] in "+-" and not s[self.i:j].lower().startswith("0x"):
                    j += 2
                    continue
                j += 1
            elif ch == "." and j + 1 < self.n and s[j + 1].isdigit():
                j += 1
            else:
                break
        w, self.i = s[self.i:j], j
        return w

    def _char(self) -> Token:
        s, j = self.s, self.i + 1
        while j < self.n and s[j] not in "'\n":
            j += 2 if s[j] == "\\" else 1
        j = min(j + 1, self.n)
        w, self.i = s[self.i:j], j
        return Token("chr", w)

    def _string(self, start: int, quote: int) -> Token:
        """A string literal whose prefix (`$`, `@`) runs from `start` to the opening quote at `quote`."""
        s = self.s
        prefix = s[start:quote]
        dollars, verbatim = prefix.count("$"), "@" in prefix
        q = quote
        while q < self.n and s[q] == '"':
            q += 1
        quotes = q - quote
        if quotes >= 3 and not verbatim:
            return self._raw(start, q, quotes, dollars)
        j = quote + 1
        if dollars == 0:
            while j < self.n:
                ch = s[j]
                if ch == "\\" and not verbatim:
                    j += 2
                    continue
                if ch == '"':
                    if verbatim and j + 1 < self.n and s[j + 1] == '"':
                        j += 2
                        continue
                    j += 1
                    break
                if ch == "\n" and not verbatim:
                    break
                j += 1
            j = self._suffix(j)
            w, self.i = s[start:j], j
            return Token("str", w)
        pieces: list[str] = [s[start:quote + 1]]
        text_from = j
        while j < self.n:
            ch = s[j]
            if ch == "\\" and not verbatim:
                j += 2
                continue
            if ch == '"':
                if verbatim and j + 1 < self.n and s[j + 1] == '"':
                    j += 2
                    continue
                pieces.append(s[text_from:j + 1])
                j += 1
                break
            if ch == "{" and j + 1 < self.n and s[j + 1] == "{":
                j += 2
                continue
            if ch == "}" and j + 1 < self.n and s[j + 1] == "}":
                j += 2
                continue
            if ch == "{":
                pieces.append(s[text_from:j + 1])
                j, hole = self._hole(j + 1, 1)
                pieces += hole
                text_from = j
                continue
            j += 1
        j = self._suffix(j)
        self.i = j
        return Token("str", " ".join(p for p in pieces if p))

    def _raw(self, start: int, body: int, quotes: int, dollars: int) -> Token:
        s = self.s
        pieces: list[str] = [s[start:body]]
        j = text_from = body
        close = '"' * quotes
        while j < self.n:
            if s.startswith(close, j):
                pieces.append(s[text_from:j + quotes])
                j += quotes
                break
            if dollars and s[j] == "{":
                k = j
                while k < self.n and s[k] == "{":
                    k += 1
                if k - j >= dollars:
                    pieces.append(s[text_from:k])
                    j, hole = self._hole(k, dollars)
                    pieces += hole
                    text_from = j
                    continue
                j = k
                continue
            j += 1
        j = self._suffix(j)
        self.i = j
        return Token("str", " ".join(p for p in pieces if p))

    def _hole(self, j: int, braces: int) -> tuple[int, list[str]]:
        """The tokens of an interpolation hole opened before `j`, up to its `braces` closing braces (a format clause after
        a `:` at depth 0 is text); returns the index after the hole and its pieces."""
        sub = _Lexer(self.s)
        sub.i, sub.n = j, self.n
        pieces: list[str] = []
        depth = 0
        while sub.i < sub.n:
            c = self.s[sub.i]
            if c in " \t\r\n\f\v":
                sub.i += 1
                continue
            if depth == 0 and c == "}":
                pieces.append(self.s[sub.i:sub.i + braces])
                return sub.i + braces, pieces
            if depth == 0 and c == ":" and not self.s.startswith("::", sub.i):
                end = self.s.find("}", sub.i)
                end = sub.n if end < 0 else end
                pieces.append(self.s[sub.i:end + braces])
                return end + braces, pieces
            tok = sub._token()
            if tok is None:
                continue
            if tok.text in ("(", "[", "{"):
                depth += 1
            elif tok.text in (")", "]", "}"):
                depth -= 1
            pieces.append(tok.text)
        return sub.i, pieces

    def _suffix(self, j: int) -> int:
        """A UTF-8 literal's `u8` suffix belongs to its token."""
        if self.s[j:j + 2] in ("u8", "U8"):
            return j + 2
        return j


def csharp_tokens(src: str) -> list[Token]:
    return _Lexer(src).tokens()


# -- the C# declarations: Roslyn's `DeclaredNames`, each under its scope ----------------------------------------------------

class _Declarations:
    def __init__(self, toks: list[Token]):
        self.t = [x.text for x in toks]
        self.kind = [x.kind for x in toks]
        self.n = len(self.t)
        self.paths: set[str] = set()
        self.match: dict[int, int] = {}
        stack: list[int] = []
        for k, x in enumerate(self.t):
            if self.kind[k] != "punct":
                continue
            if x in ("(", "[", "{"):
                stack.append(k)
            elif x in (")", "]", "}") and stack:
                self.match[stack.pop()] = k

    def tok(self, k: int) -> str:
        return self.t[k] if 0 <= k < self.n else ""

    def declare(self, scope: list[str], name: str) -> None:
        path = scope + [name]
        for k in range(len(path)):
            self.paths.add(".".join(path[k:]))

    def close(self, k: int) -> int:
        """The index of the bracket closing the one at `k` (the end of the tokens when it is never closed)."""
        return self.match.get(k, self.n - 1)

    def is_name(self, k: int) -> bool:
        return self.kind[k] == "id" if 0 <= k < self.n else False

    def is_type_start(self, k: int) -> bool:
        return self.is_name(k) and self.t[k] not in KEYWORDS

    def skip_angle(self, k: int) -> int:
        """The index after a generic argument list opening at `k` (`<`), or `k` when none opens there."""
        if self.tok(k) != "<":
            return k
        depth, j = 0, k
        while j < self.n:
            x = self.t[j]
            if x == "<":
                depth += 1
            elif x == ">":
                depth -= 1
                if depth == 0:
                    return j + 1
            elif x in ("(", "["):
                j = self.close(j)
            elif x in (";", "{", "}", ")", "=", "=>"):
                return k
            j += 1
        return k

    def qualified(self, k: int) -> int:
        """The index after a qualified name opening at `k`: name [`::` name] [<args>] (`.` name [<args>])*."""
        k += 1
        if self.tok(k) == "::" and self.is_name(k + 1):
            k += 2
        k = self.skip_angle(k)
        while self.tok(k) == "." and self.is_name(k + 1):
            k = self.skip_angle(k + 2)
        return k

    def type_end(self, k: int) -> int | None:
        """The index after a type opening at `k`, or None when no type opens there."""
        while self.tok(k) in ("ref", "readonly", "scoped"):
            k += 1
        if self.tok(k) == "(":
            k = self.close(k) + 1
        elif self.tok(k) == "delegate" and self.tok(k + 1) == "*":
            k += 2
            if self.tok(k) in ("managed", "unmanaged"):
                k += 1
            if self.tok(k) == "[":
                k = self.close(k) + 1
            k = self.skip_angle(k)
        elif self.is_type_start(k):
            k = self.qualified(k)
        else:
            return None
        while True:
            if self.tok(k) in ("?", "*"):
                k += 1
            elif self.tok(k) == "[" and self.tok(k + 1) in ("]", ","):
                k = self.close(k) + 1
            else:
                return k

    def member_name(self, k: int) -> tuple[str, int] | None:
        """(the identifier, the index after it) of a member name at `k`: an explicit interface's `I<T>.M` names `M`."""
        if not (self.is_name(k) and (self.t[k] not in KEYWORDS or self.t[k] == "this")):
            return None
        last = self.t[k]
        k += 1
        while True:
            after = self.skip_angle(k)
            if self.tok(after) == "." and self.is_name(after + 1):
                last, k = self.t[after + 1], after + 2
                continue
            return last, k

    def rest(self, k: int) -> int:
        """The index after the member whose remaining tokens start at `k`: a body, an expression body, an initializer. Once
        an expression opens (`=>`, `=`), a brace is the expression's (a pattern, an initializer) and only `;` ends it."""
        expression = False
        while k < self.n:
            x = self.t[k]
            if x == ";":
                return k + 1
            if x in ("=>", "="):
                expression = True
            if x == "{":
                k = self.close(k) + 1
                if not expression and self.tok(k) != "=":
                    return k
                continue
            if x in ("(", "["):
                k = self.close(k) + 1
                continue
            if x == "}":
                return k
            k += 1
        return k

    def locals_in(self, a: int, b: int, scope: list[str]) -> None:
        """Every local function declared in the tokens [a, b): at a statement's start, [attributes] [modifiers] Type Name
        [<...>] (...) followed by a body, an expression body or a constraint."""
        for k in range(max(a, 1), b):
            if self.t[k - 1] not in ("{", ";", "}"):
                continue
            j = k
            while self.tok(j) == "[":
                j = self.close(j) + 1
            while self.tok(j) in LOCAL_MODIFIERS:
                j += 1
            end = self.type_end(j)
            if end is None or not self.is_name(end) or self.t[end] in KEYWORDS | PATTERN_WORDS:
                continue
            name, j = self.t[end], self.skip_angle(end + 1)
            if self.tok(j) != "(":
                continue
            if self.tok(self.close(j) + 1) in ("{", "=>", "where"):
                self.declare(scope, name)

    def parameters(self, k: int, scope: list[str]) -> None:
        """Each parameter name of the list opening at `k` (`(`): the last name before its `=` or its end."""
        end = self.close(k)
        j, depth, last, in_default = k + 1, 0, None, False
        while j <= end:
            x = self.t[j]
            if j == end or (x == "," and depth == 0):
                if last:
                    self.declare(scope, last)
                last, in_default = None, False
            elif x in ("(", "[", "{"):
                j = self.close(j)
            elif x == "<":
                depth += 1
            elif x == ">":
                depth -= 1
            elif x == "=" and depth == 0:
                in_default = True
            elif self.is_name(j) and not in_default and x not in KEYWORDS:
                last = x
            j += 1

    def fields(self, k: int, scope: list[str]) -> int:
        """Each declarator of a field (or field-like event) whose first name is at `k`; returns the index after it."""
        start = k
        while k < self.n:
            if self.is_name(k) and self.tok(k + 1) in ("=", ",", ";", "["):
                self.declare(scope, self.t[k])
                k += 1
            while k < self.n and self.t[k] not in (",", ";"):
                if self.t[k] in ("(", "[", "{"):
                    k = self.close(k)
                k += 1
            if self.tok(k) == ";":
                self.locals_in(start, k, scope)
                return k + 1
            k += 1   # the comma: a declarator follows, or the comma belonged to the initializer's expression
        return k

    def enum_members(self, a: int, b: int, scope: list[str]) -> None:
        k = a
        while k < b:
            while self.tok(k) == "[":
                k = self.close(k) + 1
            if k < b and self.is_name(k):
                self.declare(scope, self.t[k])
            while k < b and self.t[k] != ",":
                if self.t[k] in ("(", "[", "{"):
                    k = self.close(k)
                k += 1
            k += 1

    def members(self, k: int, end: int, scope: list[str]) -> None:
        """Every declaration of a compilation unit, a namespace or a type body: the tokens [k, end)."""
        while k < end:
            x = self.t[k]
            if x in (";", "}"):
                k += 1
                continue
            if x == "[":
                k = self.close(k) + 1
                continue
            if x == "using" or (x == "global" and self.tok(k + 1) == "using") or (x == "extern" and self.tok(k + 1) == "alias"):
                while k < end and self.t[k] != ";":
                    k += 1
                k += 1
                continue
            j = k
            while self.tok(j) in MODIFIERS:   # `ref` and `readonly` of a `ref` return type too: `type_end` takes it either way
                j += 1
            head = self.tok(j)
            if head == "namespace":
                j += 1
                segments = []
                while self.is_name(j):
                    segments.append(self.t[j])
                    j += 1
                    if self.tok(j) == ".":
                        j += 1
                for seg in segments:
                    self.declare(scope, seg)
                if self.tok(j) == "{":
                    close = self.close(j)
                    self.members(j + 1, close, scope + segments)
                    k = close + 1
                else:
                    self.members(j + 1, end, scope + segments)
                    k = end
                continue
            if head in ("class", "struct", "interface", "enum", "record"):
                j += 1
                if head == "record" and self.tok(j) in ("class", "struct"):
                    j += 1
                name = self.t[j]
                self.declare(scope, name)
                inner = scope + [name]
                j = self.skip_angle(j + 1)
                if head == "record" and self.tok(j) == "(":
                    self.parameters(j, inner)
                    j = self.close(j) + 1
                while j < end and self.t[j] not in ("{", ";"):
                    if self.t[j] in ("(", "["):
                        j = self.close(j)
                    j += 1
                if self.tok(j) == "{":
                    close = self.close(j)
                    if head == "enum":
                        self.enum_members(j + 1, close, inner)
                    else:
                        self.members(j + 1, close, inner)
                    k = close + 1
                else:
                    k = j + 1
                continue
            if head == "delegate" and self.tok(j + 1) != "*":
                after = self.type_end(j + 1)
                if after is not None and self.is_name(after):
                    self.declare(scope, self.t[after])
                k = self.rest(j + 1)
                continue
            if head == "event":
                after = self.type_end(j + 1)
                named = self.member_name(after) if after is not None else None
                if named and self.tok(named[1]) == "{":
                    self.declare(scope, named[0])
                    k = self.rest(named[1])
                elif after is not None:
                    k = self.fields(after, scope)
                else:
                    k = self.rest(j + 1)
                continue
            if head in ("~", "implicit", "explicit"):
                k = self.rest(j + 1)
                continue
            if self.is_type_start(j) and self.tok(j + 1) == "(":   # a constructor
                self.declare(scope, head)
                k = self.rest(j + 1)
                self.locals_in(j, k, scope)
                continue
            after = self.type_end(j)
            if after is None or self.tok(after) == "operator":
                k = self.rest(max(after or j, j + 1))
                continue
            named = self.member_name(after)
            if named is None:
                k = self.rest(after)
                continue
            name, nk = named
            nxt = self.tok(nk)
            if nxt in ("(", "<") or (name == "this" and nxt == "[") or nxt in ("{", "=>"):
                self.declare(scope, name)
                k = self.rest(nk)
                self.locals_in(nk, k, scope)
            elif nxt in ("=", ";", ",", "["):
                k = self.fields(after, scope)
            else:
                k = self.rest(nk)


@dataclasses.dataclass(frozen=True)
class CSharpSymbols:
    """What a C# file holds for resolution: `code`, its tokens joined by spaces, and `paths`, every declared name in
    every suffix of its qualified path (the C# `CSharpSymbols`)."""
    code: str
    paths: frozenset[str]

    def words(self) -> list[str]:
        return sorted(set(_WORD.findall(self.code)))

    def digest(self) -> str:
        """The digest both engines compute of the declared names and the code words (`--digests`)."""
        body = "\n".join(sorted(self.paths)) + "\0" + "\n".join(self.words())
        return hashlib.sha256(body.encode("utf-8")).hexdigest()


def csharp_symbols(src: str) -> CSharpSymbols:
    toks = csharp_tokens(src)
    d = _Declarations(toks)
    d.members(0, d.n, [])
    return CSharpSymbols(" ".join(t.text for t in toks), frozenset(d.paths))


# -- the predicate ------------------------------------------------------------------------------------------------------------

class Tree:
    """A tree a location is resolved in: `text(path)` is the file's text, or None when the tree has no such file."""

    def __init__(self, read: Callable[[str], str | None], label: str):
        self._read, self.label = read, label
        self._texts: dict[str, str | None] = {}
        self._cs: dict[str, CSharpSymbols] = {}

    def text(self, path: str) -> str | None:
        if path not in self._texts:
            self._texts[path] = self._read(path)
        return self._texts[path]

    def symbols(self, path: str) -> CSharpSymbols:
        if path not in self._cs:
            self._cs[path] = csharp_symbols(self.text(path) or "")
        return self._cs[path]

    def symbol_resolves(self, path: str, symbol: str) -> bool:
        """THE PREDICATE (`SymbolResolves`): is `symbol` in the CODE of `path`, which exists?"""
        suffix = pathlib.PurePosixPath(path).suffix.lower()
        if suffix != ".cs":
            text = self.text(path) or ""
            if suffix == ".g4":
                text = GRAMMAR_COMMENTS.sub(" ", text)
            return whole_word(symbol).search(text) is not None
        declared = self.symbols(path)
        if "." in symbol:
            return symbol in declared.paths
        return whole_word(symbol).search(declared.code) is not None


def _decode(data: bytes) -> str:
    return data.decode("utf-8-sig", errors="replace")


def working_tree(root: pathlib.Path) -> Tree:
    def read(path: str) -> str | None:
        f = root / path
        return _decode(f.read_bytes()) if f.is_file() else None
    return Tree(read, "the working tree")


class PinnedTree(Tree):
    """The tree of the commit `pin` of the repository at `root`, read through ONE `git cat-file --batch` process."""

    def __init__(self, root: pathlib.Path, pin: str):
        self._proc: subprocess.Popen[bytes] | None = None
        self.root, self.pin = root, pin
        super().__init__(self._cat, f"the pin {pin[:10]}")

    def _cat(self, path: str) -> str | None:
        if self._proc is None:
            self._proc = subprocess.Popen(["git", "-C", str(self.root), "cat-file", "--batch"],
                                          stdin=subprocess.PIPE, stdout=subprocess.PIPE)
        assert self._proc.stdin is not None and self._proc.stdout is not None
        self._proc.stdin.write(f"{self.pin}:{path}\n".encode("utf-8"))
        self._proc.stdin.flush()
        header = self._proc.stdout.readline().decode("utf-8").split()
        if len(header) != 3 or header[1] != "blob":
            return None   # `<object> missing`, or a tree
        data = self._proc.stdout.read(int(header[2]))
        self._proc.stdout.read(1)   # the newline after the object
        return _decode(data)

    def close(self) -> None:
        if self._proc is not None:
            assert self._proc.stdin is not None
            self._proc.stdin.close()
            self._proc.wait()
            self._proc = None


class PinnedTrees:
    """The trees of the commits of the repository at `root` one run reads, each through its one `git cat-file` process
    (`PinnedTree`), and whether each pin is a commit at all; a context manager that closes them."""

    def __init__(self, root: pathlib.Path):
        self.root = root
        self._trees: dict[str, PinnedTree] = {}
        self._commits: dict[str, bool] = {}

    def is_commit(self, pin: str) -> bool:
        if pin not in self._commits:
            self._commits[pin] = not subprocess.run(["git", "-C", str(self.root), "cat-file", "-e", f"{pin}^{{commit}}"],
                                                    capture_output=True).returncode
        return self._commits[pin]

    def at(self, pin: str) -> PinnedTree:
        if pin not in self._trees:
            self._trees[pin] = PinnedTree(self.root, pin)
        return self._trees[pin]

    def close(self) -> None:
        for t in self._trees.values():
            t.close()
        self._trees.clear()

    def __enter__(self) -> "PinnedTrees":
        return self

    def __exit__(self, *_: object) -> None:
        self.close()


def location_problem(location: str, tree: Tree, pattern: re.Pattern[str], kinds: frozenset[str]) -> str | None:
    """Why `location` does not resolve in `tree` (`UnresolvedCodeLocations`, one entry), or None."""
    if not pattern.match(location):
        return f"code-location '{location}' is not '<repo-relative-path>[#Symbol]'"
    path, _, symbol = location.partition("#")
    if not resolved_here(path, kinds):
        return None
    if tree.text(path) is None:
        return f"code-location '{location}' - no such file in {tree.label}"
    if symbol and not tree.symbol_resolves(path, symbol):
        return f"code-location '{location}' - '{symbol}' is not in that file's code in {tree.label}"
    return None


def unresolved(locations: Iterable[str], tree: Tree, pattern: re.Pattern[str],
               kinds: frozenset[str] | None = None) -> list[str]:
    kinds = document_kinds() if kinds is None else kinds
    return [p for loc in locations if (p := location_problem(loc, tree, pattern, kinds))]


# -- the command line: the twin's answers, for the C# gate to compare -------------------------------------------------------

def parity(fixture: pathlib.Path) -> dict[str, bool]:
    """Each planted case of the parity fixture: does its `location` resolve in the fixture's planted `files`?"""
    import inventory_schema   # the schema's `code-location.pattern`, as the C# engine reads it
    doc = json.loads(fixture.read_text(encoding="utf-8"))
    files: dict[str, str] = doc["files"]
    tree = Tree(lambda p: files.get(p), "the fixture")
    pattern, kinds = inventory_schema.load_schema().code_location_re, document_kinds()
    return {c["name"]: location_problem(c["location"], tree, pattern, kinds) is None for c in doc["cases"]}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The Python twin of the gate's code-location resolver (kb/Work PB5863).")
    ap.add_argument("--parity", metavar="FIXTURE")
    ap.add_argument("--digests", metavar="LISTFILE", help="a file listing repository-relative C# paths, one per line")
    ap.add_argument("--symbols", nargs="+", metavar="FILE")
    a = ap.parse_args(argv)
    if a.parity:
        print("JSON " + json.dumps(parity(pathlib.Path(a.parity)), sort_keys=True))
        return 0
    if a.digests:
        tree = working_tree(REPO)
        paths = [p.strip() for p in pathlib.Path(a.digests).read_text(encoding="utf-8").splitlines() if p.strip()]
        print("JSON " + json.dumps({p: tree.symbols(p).digest() for p in paths}, sort_keys=True))
        return 0
    if a.symbols:
        tree = working_tree(REPO)
        print(json.dumps({p: {"paths": sorted(tree.symbols(p).paths), "words": tree.symbols(p).words()}
                          for p in a.symbols}, indent=1))
        return 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))
    from standard_streams import ensure_utf8  # noqa: E402
    ensure_utf8()
    sys.exit(main())
