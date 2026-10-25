#!/usr/bin/env python3
"""source_units.py — the natural UNITS of a source file, the units a change touches, and when two claims on units meet
(kb/Work PB3670; docs/rearchitecture/DESIGN-architecture-review.md §8.7 "the file-set partition").

WHY. The wave planner's partition once refused two groups on one FILE (and, for a note with no site block, on a whole
SUBSYSTEM), so unrelated fixes waited hours behind work that never touched their code (the owner, 2026-10-24: "I do
not understand why we cannot update separate regions of code in a single source file from different groups such that a
merge conflict doesn't occur"). Git's three-way merge combines edits to different regions of one file; what conflicts
is an edit to the SAME region. The partition's key is therefore the UNIT a change edits, named stably across versions
of the file (a name survives the lines moving under it), never a line number:

  * C# (`.cs`): a member (method, constructor, property, indexer, operator, event, field, delegate, a nested type's
    member) named `Type.Member` (nested types `Outer.Inner.Member`, overloads one unit), an enum or a body-less
    record as one unit, the lines of a type that lie in no member (its header, its closing brace, the trivia after its
    last member) as the unit `Type.<own>` (so a change there meets a claim on the whole type, never one on a
    member of it), and the file's own lines outside every type as `<header>` (before the first type)
    and `<footer>` (after the last). A member's leading trivia (its doc comment, its attributes, the blank lines before
    it) is part of it. The reading is a tokenizer (comments, every string form: regular, verbatim, interpolated with its
    holes, raw, raw interpolated; char literals; preprocessor lines) and a brace walk; a file it cannot balance has no
    units (the whole file is one unit: a claim on it meets every claim on the file).
  * Python (`.py`): a top-level function or class, a method `Class.method`, a top-level assignment by its first target,
    the imports as `<imports>`, any other top-level statement as `<module>`; a class's other statements are `Class.<own>`.
    Read with `ast`; a file that does not parse has no units.
  * Markdown (`.md`): a section, named by its heading text (to the next heading of any level); the lines before the
    first heading are `<preamble>`. A fenced block's `#` lines are no headings.
  * any other file: no units (the whole file is one unit).

A CHANGE touches the units its lines fall in, on BOTH sides of each hunk (`touched`): the new version's lines it adds
or rewrites, and the old version's lines it removes or rewrites; a pure insertion also touches the old lines on either
side of where it lands, so two insertions at one place (two members appended to one class) touch one unit, as git
would conflict on them.

Two CLAIMS meet on a file when either claims the whole file, or a name of one MATCHES a name of the other (`match`):
one's dotted segments occur as a contiguous run inside the other's (`collision` matches `collision`, `plan_wave.collision`
and nothing else; `DataBinder` matches every `DataBinder.*` member; `Report.finished` matches `Report.finished`), and a
section number (`§8.7`, read from a note as `8.7`) matches a Markdown section whose heading starts with it (`8.7 The wave
plan's shape`, `8.7.1 ...`). Parameter lists are dropped: overloads are one unit.

Callers: `plan_wave.py` (the in-flight units of each worktree and unlanded branch, the meeting rule `collision`) and
`.claude/skills/workstream/check_practices.py` (the shared-MEMBER refusal).

Usage:
    python scripts/arch/source_units.py <file>        # print the file's units with their line ranges
    python scripts/arch/source_units.py --self-test
"""
from __future__ import annotations

import ast
import dataclasses
import functools
import re
import sys
from pathlib import Path
from typing import Iterable, Sequence

HEADER, FOOTER, PREAMBLE, MODULE, IMPORTS, OWN = ("<header>", "<footer>", "<preamble>", "<module>", "<imports>",
                                                 "<own>")


@dataclasses.dataclass(frozen=True)
class Unit:
    name: str
    start: int   # 1-based, inclusive
    end: int     # 1-based, inclusive


# ── C# ──────────────────────────────────────────────────────────────────────────────────────────────────────────
class _Unbalanced(Exception):
    """The C# reader cannot read the file (an unterminated string or comment, an unbalanced brace)."""


_ID = re.compile(r"@?[A-Za-z_][A-Za-z0-9_]*")
_NUM = re.compile(r"[0-9][A-Za-z0-9_.]*")
_BLANKS = re.compile(r"[ \t\r\f\v\ufeff]+")
_TWO = ("=>", "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "??", "?.", "::", "&&", "||")
_TYPE_KW = ("class", "struct", "interface", "enum", "record", "namespace")


def _cs_tokens(text: str) -> list[tuple[str, int]]:
    """[(token, line)]: identifiers, numbers and punctuation, every string and char literal as one `"` token, comments
    and preprocessor lines dropped. Raises _Unbalanced on an unterminated comment or literal."""
    out: list[tuple[str, int]] = []
    i, n, line = 0, len(text), 1
    bol = True   # only whitespace since the line began: a `#` there is a preprocessor line

    def string(i: int) -> int:
        """The index past the string literal starting at i (its prefix included), counting its newlines."""
        nonlocal line
        j, dollars, verbatim = i, 0, False
        while text[j] in "$@":
            dollars += text[j] == "$"
            verbatim |= text[j] == "@"
            j += 1
        q = j
        while q < n and text[q] == '"':
            q += 1
        quotes = q - j
        if quotes >= 3 and not verbatim:   # raw: ends at a run of as many quotes; holes open with `dollars` braces
            # (`@"""…` is a verbatim string opening with a doubled quote, never a raw one)
            k = q
            while k < n:
                if text.startswith('"' * quotes, k):
                    return k + quotes + (len(text[k + quotes:]) - len(text[k + quotes:].lstrip('"')))
                if dollars and text.startswith("{" * dollars, k) and not text.startswith("{" * (dollars + 1), k):
                    k = hole(k + dollars, "}" * dollars)
                    continue
                if text[k] == "\n":
                    line += 1
                k += 1
            raise _Unbalanced("unterminated raw string")
        if quotes == 2:   # the empty string ""
            return q
        k = j + 1
        while k < n:
            c = text[k]
            if c == "\n":
                if not verbatim:
                    raise _Unbalanced("newline in a string")
                line += 1
            elif c == "\\" and not verbatim:
                k += 2
                continue
            elif c == '"':
                if verbatim and k + 1 < n and text[k + 1] == '"':
                    k += 2
                    continue
                return k + 1
            elif c == "{" and dollars:
                if k + 1 < n and text[k + 1] == "{":
                    k += 2
                    continue
                k = hole(k + 1, "}")
                continue
            k += 1
        raise _Unbalanced("unterminated string")

    def hole(k: int, close: str) -> int:
        """The index past an interpolation hole's closing `close`, skipping its code (strings, chars, comments,
        nested brackets) and, at its own depth, its format specifier after `:`."""
        nonlocal line
        depth = 0
        while k < n:
            c = text[k]
            if c == "\n":
                line += 1
            if depth == 0 and text.startswith(close, k):
                return k + len(close)
            if c in "([{":
                depth += 1
            elif c in ")]}":
                depth -= 1
            elif c == ":" and depth == 0:   # the format specifier: literal text to the hole's close
                end = text.find(close, k)
                if end < 0:
                    raise _Unbalanced("unterminated interpolation")
                line += text.count("\n", k, end)
                return end + len(close)
            elif c == '"' or (c in "$@" and k + 1 < n and text[k + 1] in '"$@'):
                k = string(k)
                continue
            elif c == "'":
                k = char(k)
                continue
            elif text.startswith("/*", k):
                end = text.find("*/", k + 2)
                if end < 0:
                    raise _Unbalanced("unterminated comment")
                line += text.count("\n", k, end)
                k = end + 2
                continue
            elif text.startswith("//", k):   # a hole spanning lines may hold a line comment (C# 11)
                end = text.find("\n", k)
                if end < 0:
                    raise _Unbalanced("unterminated interpolation")
                k = end
                continue
            k += 1
        raise _Unbalanced("unterminated interpolation")

    def char(k: int) -> int:
        j = k + 1
        while j < n and text[j] != "'":
            if text[j] == "\n":
                raise _Unbalanced("newline in a char literal")
            j += 2 if text[j] == "\\" else 1
        if j >= n:
            raise _Unbalanced("unterminated char literal")
        return j + 1

    while i < n:
        c = text[i]
        if c == "\n":
            line, bol, i = line + 1, True, i + 1
            continue
        if c in " \t\r\f\v\ufeff":
            i = _BLANKS.match(text, i).end()
            continue
        if c == "#" and bol:   # a preprocessor line: to its end
            end = text.find("\n", i)
            i = n if end < 0 else end
            continue
        bol = False
        if text.startswith("//", i):
            end = text.find("\n", i)
            i = n if end < 0 else end
            continue
        if text.startswith("/*", i):
            end = text.find("*/", i + 2)
            if end < 0:
                raise _Unbalanced("unterminated comment")
            line += text.count("\n", i, end)
            i = end + 2
            continue
        if c == '"' or (c in "$@" and i + 1 < n and text[i + 1] in '"$@' and
                        re.match(r"[$@]+\"", text[i:i + 8])):
            at = line
            i = string(i)
            out.append(('"', at))
            continue
        if c == "'":
            out.append(('"', line))
            i = char(i)
            continue
        m = _ID.match(text, i)
        if m:
            out.append((m.group(0).lstrip("@"), line))
            i = m.end()
            continue
        m = _NUM.match(text, i)
        if m:
            out.append(("0", line))
            i = m.end()
            continue
        two = text[i:i + 2]
        if two in _TWO:
            out.append((two, line))
            i += 2
            continue
        out.append((c, line))
        i += 1
    return out


def _skip_group(toks: list[tuple[str, int]], i: int) -> int:
    """The index of the bracket closing the one at i (any of `([{`, nested kinds counted together)."""
    depth = 0
    for j in range(i, len(toks)):
        t = toks[j][0]
        if t in "([{":
            depth += 1
        elif t in ")]}":
            depth -= 1
            if depth == 0:
                return j
    raise _Unbalanced("unbalanced bracket")


def _member_name(head: list[str]) -> str:
    """The name a member declaration's header tokens (at its own depth, attributes and bracketed groups collapsed to
    `[`/`(` markers) declare: the identifier before its parameter list (a generic list skipped; `operator +`,
    `this[]`), else the identifier before its first `,`, `=`, `=>`, `{` or `;`."""
    if "(" in head:
        k = head.index("(") - 1
        if k >= 0 and head[k] == ">":   # a generic method: skip back over its type parameters
            depth = 0
            while k >= 0:
                depth += head[k] == ">"
                depth -= head[k] == "<"
                k -= 1
                if depth == 0:
                    break
        if k >= 1 and head[k - 1] == "operator":
            return "operator" + head[k]
        if k >= 0 and head[k] == "operator":
            return "operator"
        while k >= 0 and not _ID.fullmatch(head[k]):
            k -= 1
        return head[k] if k >= 0 else "?"
    if "this" in head and head.index("this") + 1 < len(head) and head[head.index("this") + 1] == "[":
        return "this[]"
    stop = len(head)
    for s in (",", "=", "=>", "{", ";"):
        if s in head:
            stop = min(stop, head.index(s))
    names = [t for t in head[:stop] if _ID.fullmatch(t)]
    return names[-1] if names else "?"


def _cs_body(toks: list[tuple[str, int]], i: int, prefix: str, out: list[tuple[str, int, int, bool]]) -> int:
    """Read declarations from toks[i] to the `}` closing the enclosing body (or the end), appending
    (name, first line, last line, is a type body) for each; return the index of that `}` (len(toks) at the end)."""
    n = len(toks)
    while i < n:
        t = toks[i][0]
        if t == "}":
            return i
        if t == ";":
            i += 1
            continue
        start, head, kw, j = i, [], None, i
        while True:
            if j >= n:
                raise _Unbalanced("a declaration runs off the end")
            t = toks[j][0]
            if t in "([":
                k = _skip_group(toks, j)
                head.append(t)
                j = k + 1
                continue
            if t in _TYPE_KW and kw is None and "(" not in head and "=" not in head and "=>" not in head:
                kw = t
            if t == ";":
                if kw == "namespace" or (not prefix and head and head[0] in ("using", "extern", "global")):
                    i = j + 1   # a file-scoped namespace or a directive: no unit
                    break
                name = _type_name(head, kw) if kw and kw != "namespace" else _member_name(head)
                out.append((prefix + name, toks[start][1], toks[j][1], False))
                i = j + 1
                break
            if t == "{":
                if kw == "namespace":
                    k = _cs_body(toks, j + 1, prefix, out)
                    if k >= n:
                        raise _Unbalanced("an unclosed namespace")
                    i = k + 1
                    break
                if kw and "=" not in head and "=>" not in head:
                    name = _type_name(head, kw)
                    if kw == "enum":
                        k = _skip_group(toks, j)
                    else:
                        k = _cs_body(toks, j + 1, prefix + name + ".", out)
                        if k >= n:
                            raise _Unbalanced(f"an unclosed type {name}")
                    out.append((prefix + name, toks[start][1], toks[k][1], kw != "enum"))
                    i = k + 1 + (k + 1 < n and toks[k + 1][0] == ";")
                    break
                k = _skip_group(toks, j)
                if "=" in head or "=>" in head:   # an expression's braces (a lambda, an initializer): to its `;`
                    head.append("{")
                    j = k + 1
                    continue
                if k + 1 < n and toks[k + 1][0] == "=":   # a property with an initializer: to its `;`
                    head += ["{", "="]
                    j = k + 2
                    continue
                out.append((prefix + _member_name(head + ["{"]), toks[start][1], toks[k][1], False))
                i = k + 1
                break
            if t == "}":
                raise _Unbalanced("a declaration ends at a closing brace")
            head.append(t)
            j += 1
    return n


def _type_name(head: list[str], kw: str) -> str:
    k = head.index(kw) + 1
    while k < len(head) and head[k] in ("class", "struct"):   # record class, record struct
        k += 1
    return head[k] if k < len(head) and _ID.fullmatch(head[k]) else "?"


def csharp_units(text: str) -> list[Unit] | None:
    try:
        toks = _cs_tokens(text)
        decls: list[tuple[str, int, int, bool]] = []
        if _cs_body(toks, 0, "", decls) != len(toks):
            return None   # a stray `}` at the top
    except _Unbalanced:
        return None
    return _cover(text.count("\n") + 1, decls, {ln for _, ln in toks})


def _cover(lines: int, decls: list[tuple[str, int, int, bool]], tokens: set[int]) -> list[Unit]:
    """Every line its unit. `decls` is (name, first line, last line, is a container): a C# type with a body or a
    Python class, whose direct children are the declarations named `<its name>.<one segment>`. A line inside a child
    belongs to that child (to the innermost one, through nested containers). A container's line that lies in none of
    its children belongs, when it and every line after it up to the NEXT child of that container hold no token
    (`tokens` is the set of lines that hold one: that run of blank and comment lines is the child's leading trivia), to
    that child, and otherwise, or with no next child, to the
    container's own unit `<name>.<own>`: a type's own header, braces and tail are the unit `Type.<own>`. The file is the outermost container,
    its own lines `<header>` before its last top-level declaration and `<footer>` after it."""
    owner: list[str | None] = [None] * (lines + 2)
    next_token = [lines + 1] * (lines + 2)   # the first line at or after k that holds a token
    for k in range(lines, 0, -1):
        next_token[k] = k if k in tokens else next_token[k + 1]

    def children(prefix: str) -> list[tuple[str, int, int, bool]]:
        return sorted((d for d in decls if d[0].startswith(prefix) and "." not in d[0][len(prefix):]),
                      key=lambda d: d[1])

    def fill(name: str | None, a: int, b: int) -> None:
        kids = children(f"{name}." if name else "")
        for d in kids:
            if d[3]:
                fill(d[0], d[1], d[2])
            else:
                for k in range(d[1], d[2] + 1):
                    owner[k] = owner[k] or d[0]
        last = kids[-1][1] if kids else None
        for k in range(a, b + 1):
            if owner[k] is not None:
                continue
            nxt = next((d for d in kids if d[1] > k), None)
            if nxt is not None and next_token[k] == nxt[1]:   # trivia running straight into the next child
                owner[k] = f"{nxt[0]}.{OWN}" if nxt[3] else nxt[0]
            elif name is not None:
                owner[k] = f"{name}.{OWN}"
            else:
                owner[k] = HEADER if last is None or k < last else FOOTER

    fill(None, 1, lines)
    return _runs(owner, lines)


def _runs(owner: Sequence[str | None], lines: int) -> list[Unit]:
    out: list[Unit] = []
    for k in range(1, lines + 1):
        name = owner[k] or HEADER
        if out and out[-1].name == name and out[-1].end == k - 1:
            out[-1] = Unit(name, out[-1].start, k)
        else:
            out.append(Unit(name, k, k))
    return out


# ── Python ──────────────────────────────────────────────────────────────────────────────────────────────────────
def python_units(text: str) -> list[Unit] | None:
    try:
        tree = ast.parse(text)
    except (SyntaxError, ValueError):
        return None
    lines = text.count("\n") + 1
    decls: list[tuple[str, int, int, bool]] = []

    def first(node: ast.AST) -> int:
        return min([node.lineno] + [d.lineno for d in getattr(node, "decorator_list", [])])

    def name_of(node: ast.stmt, container: str) -> str:
        if isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            return node.name
        if isinstance(node, (ast.Import, ast.ImportFrom)):
            return IMPORTS
        target = (node.targets[0] if isinstance(node, ast.Assign) else
                  node.target if isinstance(node, (ast.AnnAssign, ast.AugAssign)) else None)
        if isinstance(target, ast.Name):
            return target.id
        return container or MODULE

    for node in tree.body:
        name = name_of(node, "")
        if isinstance(node, ast.ClassDef):
            decls.append((name, first(node), node.end_lineno, True))
            for sub in node.body:
                if isinstance(sub, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
                    decls.append((f"{name}.{sub.name}", first(sub), sub.end_lineno, False))
        else:
            decls.append((name, first(node), node.end_lineno, False))
    tokens = {k for k, ln in enumerate(text.split(chr(10)), 1) if ln.strip() and not ln.lstrip().startswith("#")}
    return _cover(lines, decls, tokens)


# ── Markdown ────────────────────────────────────────────────────────────────────────────────────────────────────
_MD_HEADING = re.compile(r"^#{1,6}[ \t]+(.*?)[ \t#]*$")
_FENCE = re.compile(r"^[ \t]{0,3}(```|~~~)")


def markdown_units(text: str) -> list[Unit]:
    owner: list[str | None] = [None]
    current, fence = PREAMBLE, None
    for raw in text.split("\n"):
        f = _FENCE.match(raw)
        if f:
            fence = None if fence == f.group(1) else (fence or f.group(1))
        elif fence is None and (h := _MD_HEADING.match(raw)):
            current = h.group(1).strip()
        owner.append(current)
    return _runs(owner, len(owner) - 1)


# ── the API ─────────────────────────────────────────────────────────────────────────────────────────────────────
READERS = {".cs": csharp_units, ".py": python_units, ".md": markdown_units}


def units(path: str, text: str) -> list[Unit] | None:
    """The units of `path`'s content `text`, every line in exactly one, or None: the file is one unit (no reader for its
    kind, or the reader cannot read it). Read once per (kind, content): the planner reads one merge base's version of a
    file for every branch cut from it."""
    return _units_of(Path(path).suffix.lower(), text)


@functools.lru_cache(maxsize=256)
def _units_of(suffix: str, text: str) -> list[Unit] | None:
    reader = READERS.get(suffix)
    return reader(text.replace("\r\n", "\n")) if reader else None


def names_at(us: Sequence[Unit], lines: Iterable[int]) -> set[str]:
    """The names of the units holding any of `lines` (a line outside the file is ignored)."""
    out: set[str] = set()
    want = sorted(set(lines))
    k = 0
    for ln in want:
        while k < len(us) and us[k].end < ln:
            k += 1
        if k < len(us) and us[k].start <= ln:
            out.add(us[k].name)
    return out


_HUNK = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", re.M)


def hunks(patch: str) -> list[tuple[int, int, int, int]]:
    """[(old start, old count, new start, new count)] of every `@@` header of a unified diff (counts default to 1)."""
    return [(int(a), int(b) if b else 1, int(c), int(d) if d else 1)
            for a, b, c, d in _HUNK.findall(patch)]


def touched(path: str, old: str | None, new: str | None, hs: Sequence[tuple[int, int, int, int]]) -> frozenset[str] | None:
    """The units of `path` a change from `old` to `new` touches, given its hunks, or None when it touches the whole file
    (the file is added or deleted, or a version has no units). A hunk touches its rewritten lines on both sides, and a
    side with no lines (a pure insertion or deletion) touches the lines on either side of where it lands."""
    if old is None or new is None:
        return None
    uo, un = units(path, old), units(path, new)
    if uo is None or un is None:
        return None
    old_lines: set[int] = set()
    new_lines: set[int] = set()
    for a, b, c, d in hs:
        old_lines |= set(range(a, a + b)) if b else {a, a + 1}
        new_lines |= set(range(c, c + d)) if d else {c, c + 1}
    return frozenset(names_at(uo, old_lines) | names_at(un, new_lines))


_PARAMS = re.compile(r"\(.*$")
_SECTION = re.compile(r"^§?(\d+(?:\.\d+)*)$")


def _segments(name: str) -> list[str]:
    return [s for s in _PARAMS.sub("", name.strip()).split(".") if s]


def match(a: str, b: str) -> bool:
    """Whether two unit names name overlapping code (module doc): one's dotted segments occur as a contiguous run in the
    other's; a section number matches a heading that starts with it."""
    for x, y in ((a, b), (b, a)):
        if s := _SECTION.match(x.strip()):
            num = s.group(1)
            if y.strip().lstrip("§") == num or re.match(re.escape(num) + r"(?:[ .\t]|$)", y.strip().lstrip("§")):
                return True
    if a.strip() == b.strip():
        return True
    sa, sb = _segments(a), _segments(b)
    if not sa or not sb:
        return False
    short, long_ = (sa, sb) if len(sa) <= len(sb) else (sb, sa)
    return any(long_[k:k + len(short)] == short for k in range(len(long_) - len(short) + 1))


def meet(a: Iterable[str], b: Iterable[str]) -> str | None:
    """The first name of `a` (sorted) that matches a name of `b`, or None."""
    bs = list(b)
    return next((x for x in sorted(a) if any(match(x, y) for y in bs)), None)


# ── self-test ───────────────────────────────────────────────────────────────────────────────────────────────────
CS_SAMPLE = '''using System;
namespace N.M;

/// <summary>The binder.</summary>
public sealed partial class DataBinder : Base, IFoo
{
    private readonly int _count = 3, _other;
    public string Text { get; set; } = "a { b";

    /// <summary>Binds.</summary>
    [Obsolete("x(")]
    public void Bind<T>(T x) where T : class
    {
        var s = $"{x:N2} {{ {(x is null ? "a" : "b")} }}";
        var r = """
            raw { "quoted" }
            """;
        var v = @"verbatim "" {";
        var w = @"""verbatim opening on a doubled quote"" {";
        var h = $"{x switch
        {
            // a comment in a hole: it's no char literal {
            _ => "y",
        }}";
        char c = '{';
        Action a = () => { };
    }

    public int Count => _count;

    public static bool operator ==(DataBinder a, DataBinder b) => true;

    public int this[int i] { get { return i; } }

    private enum Kind { A, B }

    private sealed class Inner
    {
        internal void Deep() { }
    }

    public DataBinder() : base(1) { }
}

public record Point(int X, int Y);
'''


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, got: object, want: object) -> None:
        ok = got == want
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + ("" if ok else f"  (got {got!r}, want {want!r})"))
        if not ok:
            failures.append(label)

    us = units("x.cs", CS_SAMPLE)
    by = {u.name: u for u in reversed(us or [])}
    first_line = {}
    for u in us or []:
        first_line.setdefault(u.name, u.start)
    expect("C#: every member, nested type member, enum, operator, indexer, ctor and record is a unit",
           sorted(first_line), sorted(["<header>", "<footer>", "DataBinder.<own>", "DataBinder._count", "DataBinder.Text",
                                       "DataBinder.Bind", "DataBinder.Count", "DataBinder.operator==",
                                       "DataBinder.this[]", "DataBinder.Kind", "DataBinder.Inner.<own>",
                                       "DataBinder.Inner.Deep", "DataBinder.DataBinder", "Point"]))
    lines = CS_SAMPLE.split("\n")
    expect("C#: a member's doc comment and attribute are its own (leading trivia)",
           names_at(us, [lines.index("    /// <summary>Binds.</summary>") + 1,
                         lines.index('    [Obsolete("x(")]') + 1]), {"DataBinder.Bind"})
    expect("C#: braces and quotes inside every string form stay inside Bind",
           names_at(us, [k + 1 for k, ln in enumerate(lines) if "raw {" in ln or "verbatim" in ln or "'{'" in ln]),
           {"DataBinder.Bind"})
    expect("C#: the class's closing brace is the type's own unit, the usings the header",
           (names_at(us, [lines.index("}") + 1]), names_at(us, [1, 2])), ({"DataBinder.<own>"}, {"<header>"}))
    expect("C#: an unbalanced file has no units (the whole file)", units("x.cs", "class A { void M() { }"), None)
    expect("C#: an unterminated string has no units", units("x.cs", 'class A { string s = "abc\n; }'), None)

    py = "import os\nX = 1\n\n\n# lead\ndef f():\n    return 1\n\n\nclass C:\n    k = 2\n\n    @staticmethod\n    def m():\n        pass\n"
    pu = units("x.py", py)
    expect("Python: imports, an assignment, a function (with its leading comment), a class and a method",
           [(u.name, u.start, u.end) for u in pu or []],
           [("<imports>", 1, 1), ("X", 2, 2), ("f", 3, 7), ("C.<own>", 8, 11), ("C.m", 12, 15), ("<footer>", 16, 16)])
    expect("Python: a decorated method's decorator is the method's", names_at(pu, [13, 14]), {"C.m"})
    expect("Python: the comment before f is f's", names_at(pu, [5]), {"f"})
    expect("Python: a class attribute is the class's", names_at(pu, [11]), {"C.<own>"})
    expect("Python: a file that does not parse has no units", units("x.py", "def (:\n"), None)

    md = "intro\n# 8 Top\ntext\n## 8.7 The partition\nbody\n```\n# not a heading\n```\n### 8.7.1 Sub\nmore\n"
    mu = units("x.md", md)
    expect("Markdown: sections by heading, a fenced `#` line no heading",
           (names_at(mu, [1]), names_at(mu, [5, 7]), names_at(mu, [10])),
           ({"<preamble>"}, {"8.7 The partition"}, {"8.7.1 Sub"}))
    expect("another kind of file has no units", units("x.json", "{}"), None)

    old = "class A\n{\n    void M() { }\n\n    void N() { }\n}\n"
    new_m = "class A\n{\n    void M() { int x = 1; }\n\n    void N() { }\n}\n"
    new_add = "class A\n{\n    void M() { }\n\n    void N() { }\n\n    void O() { }\n}\n"
    expect("touched: a body edit touches its member only",
           touched("a.cs", old, new_m, [(3, 1, 3, 1)]), frozenset({"A.M"}))
    expect("touched: an appended member touches itself and the class tail it lands before (two appends meet)",
           touched("a.cs", old, new_add, [(5, 0, 6, 2)]), frozenset({"A.N", "A.<own>", "A.O"}))
    expect("touched: an added or deleted file is the whole file", (touched("a.cs", None, old, []),
                                                                     touched("a.cs", old, None, [])), (None, None))
    expect("hunks: counts default to one", hunks("@@ -3 +3,2 @@\n@@ -9,0 +11 @@"), [(3, 1, 3, 2), (9, 0, 11, 1)])

    expect("match: a bare member, a qualified one, a type and its member, a module-qualified function",
           [match("collision", "collision"), match("Report.finished", "finished"), match("DataBinder", "DataBinder.Bind"),
            match("plan_wave.collision", "collision"), match("collision", "collision_set"),
            match("DataBinder.Bind", "DataBinder.Text"), match("Bind(int)", "DataBinder.Bind")],
           [True, True, True, True, False, False, True])
    expect("match: a section number and its subsections, never a longer number",
           [match("§8.7", "8.7 The partition"), match("8.7", "8.7.1 Sub"), match("§8.7", "8.70 Other"),
            match("§8.7", "8.7")], [True, True, False, True])
    expect("match: a type's own lines meet a claim on the whole type, never one on a member of it",
           [match("Binder.<own>", "Binder"), match("Binder.<own>", "Binder.Bind"), match("Binder.<own>", "Bind")],
           [True, False, False])
    expect("meet: the first shared name, or None", (meet(["b.M", "a.N"], ["N"]), meet(["a"], ["b"])), ("a.N", None))
    print(f"=== SOURCE UNITS SELF-TEST: {'PASS' if not failures else 'FAIL'} ({len(failures)} failed) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
    argv = sys.argv[1:] if argv is None else argv
    if argv == ["--self-test"]:
        return self_test()
    if len(argv) != 1:
        print(__doc__.split("\n\n")[-1])
        return 2
    p = Path(argv[0])
    us = units(p.name, p.read_text(encoding="utf-8", errors="replace"))
    if us is None:
        print(f"{p}: no units (the whole file is one unit)")
        return 0
    for u in us:
        print(f"{u.start:6}-{u.end:<6} {u.name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
