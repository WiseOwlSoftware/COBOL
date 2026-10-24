#!/usr/bin/env python3
"""cs_members.py — the MEMBERS of a C# source file and the lines each spans (kb/Work PB3190;
DESIGN-test-build-ci.md §3.14.2, tier 0a).

The gate's tier 0a holds every test method a change ADDS OR EDITS: `impacted_tests.changed_tests` asks which members
the diff's lines fall in, and a test method whose span they touch, or whose `[MemberData]` source they touch, is
tier 0a. Until PB3190 tier 0a held only ADDED methods, so an edited test body ran in leg 1 only if the tier-1 budget
reached it, and its red surfaced first at the landing's Linux gate or in CI.

This reads a C# file at the MEMBER level, never the statement level, and is not a C# parser:
  * THE LEXER (`blank`) replaces every comment, preprocessor line, character literal and string — regular, verbatim,
    interpolated (its holes followed, so a hole's own string cannot end the outer one), raw (`\"\"\"`, any `$`
    count) — with spaces, newlines kept, so every brace, bracket and semicolon left is code and line numbers are
    unchanged;
  * A MEMBER is its attribute block, its declaration and its body: a block `{…}` (a property's may be followed by an
    initializer `= …;`), an expression body `=> …;`, an initializer `= …;`, or a bare `;`. A type or namespace body
    (`class`, `struct`, `interface`, `record`, `namespace` before any parenthesis of the header) is a CONTAINER whose
    members are read in turn; an `enum` is one member.
Each member carries its name, its first and last line (1-based; the first is its first attribute line), whether it is
an xunit test method (`[Fact]` or `[Theory]` in its attribute block) and the `[MemberData]` names it reads.

`audit` reads every `.cs` file under some directories and names each file whose code braces do not balance or whose
`[Fact]`/`[Theory]` attributes do not each open a test member: the lexer's net over the real test tree
(`impacted_tests.py --audit-members`, run by `ImpactedTestsDriftTests`), so new C# syntax it misreads is a red, never
a changed test silently missed. `balance` is the same check for one file (`impacted_tests.tests_changed` takes every
test method of a file it fails).

Usage:
    python scripts/spec/cs_members.py <file.cs>...       # its members, one per line
    python scripts/spec/cs_members.py --self-test
"""

from __future__ import annotations

import argparse
import bisect
import re
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

#: A string literal's opening: its `$`/`@` prefix and its run of quotes.
STRING_START = re.compile(r'(?P<pre>\$*@\$*|\$+)?(?P<q>"+)')
#: A container header: a type or namespace keyword followed by its name. Read only before the header's first
#: parenthesis, so a method's `where T : class` constraint is not a type.
CONTAINER_RE = re.compile(r"\b(class|struct|interface|record|namespace)\s+[A-Za-z_@]")
#: An xunit test method's attribute: `[Fact]`, `[Theory(...)]`, or either inside a combined attribute list.
TEST_ATTR_RE = re.compile(r"(?:\[|,)\s*(?:Xunit\.)?(?:Fact|Theory)\s*(?:\(|,|\])")
#: The member a `[MemberData]` attribute reads: `nameof(X)`, `nameof(T.X)` or `"X"`.
MEMBER_DATA_RE = re.compile(r'MemberData\(\s*(?:nameof\(\s*(?:\w+\s*\.\s*)*(\w+)\s*\)|"(\w+)")')
#: Where the lexer must look next: in code, a comment, preprocessor line, character or string can start; inside an
#: interpolation hole, a bracket can also nest or close it.
CODE_STOP = re.compile(r"[#/'\"$@]")
HOLE_STOP = re.compile(r"[#/'\"$@{}()\[\]]")
NON_SPACE = re.compile(r"\S")
NEWLINE = re.compile(r"\n")
NOT_NEWLINE = re.compile(r"[^\n]")
#: The characters the member reader acts on.
BRACKETS = re.compile(r"[{}()\[\]]")
HEADER_STOP = re.compile(r"[{}()\[\];=]")
IDENT_RE = re.compile(r"[A-Za-z_]\w*")
#: Words that precede a member's name but are never it (a tuple return type leaves `public (`).
NOT_A_NAME = {"public", "private", "protected", "internal", "static", "readonly", "sealed", "abstract", "virtual",
              "override", "async", "unsafe", "new", "extern", "partial", "const", "file", "required", "volatile",
              "void", "implicit", "explicit"}


@dataclass(frozen=True)
class Member:
    """One member of a C# type: its name, its lines (first attribute line .. last body line, 1-based), whether it is
    an xunit test method, and the `[MemberData]` names it reads."""
    name: str
    lo: int
    hi: int
    test: bool
    data: tuple[str, ...] = ()

    def touched_by(self, new_start: int, new_count: int) -> bool:
        """Whether a `-U0` hunk's NEW side touches this member: a replaced or added line inside its lines, or a pure
        deletion (`new_count` 0: lines removed after line `new_start`) inside them or at their leading edge."""
        if new_count == 0:
            return self.lo - 1 <= new_start < self.hi
        return new_start <= self.hi and self.lo <= new_start + new_count - 1


def blank(text: str) -> str:
    """`text` with every comment, preprocessor line, character literal and string replaced by spaces (newlines kept)."""
    out = list(text)
    _code(text, out, 0, hole=False)
    return "".join(out)


def _wipe(out: list[str], a: int, b: int) -> None:
    seg = "".join(out[a:b])
    out[a:b] = NOT_NEWLINE.sub(" ", seg) if "\n" in seg else " " * len(seg)


def _code(text: str, out: list[str], i: int, hole: bool) -> int:
    """Scan code from `i`, wiping what is not code. Inside an interpolation hole, return the index of the `}` that
    closes it; otherwise the end of the text."""
    n, depth, stop = len(text), 0, HOLE_STOP if hole else CODE_STOP
    while True:
        m = stop.search(text, i)
        if not m:
            return n
        i, c = m.start(), m.group()
        if c == "#":
            if hole or text[text.rfind("\n", 0, i) + 1:i].strip():
                i += 1
                continue
            j = _line_end(text, i)              # a preprocessor directive is one line
        elif text.startswith("//", i):
            j = _line_end(text, i)
        elif text.startswith("/*", i):
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
        elif c == "/":
            i += 1
            continue
        elif c == "'":
            j = _char_end(text, i)
        elif c in '$@"':
            s = STRING_START.match(text, i)
            if not s:
                i += 1
                continue
            j = _string_end(text, out, s)
        else:                                   # a bracket, seen only inside a hole
            if c in "{([":
                depth += 1
            elif depth == 0 and c == "}":
                return i
            else:
                depth -= 1
            i += 1
            continue
        _wipe(out, i, j)
        i = j


def _line_end(text: str, i: int) -> int:
    j = text.find("\n", i)
    return len(text) if j < 0 else j


def _char_end(text: str, i: int) -> int:
    """The index after the character literal opening at `i`."""
    j = i + 1
    if j < len(text) and text[j] == "\\":
        j += 2
    k = text.find("'", j)
    nl = text.find("\n", j)
    return len(text) if k < 0 else (k + 1 if nl < 0 or k < nl else nl)


def _string_end(text: str, out: list[str], m: re.Match) -> int:
    """The index after the string literal `m` opens."""
    pre, q = m.group("pre") or "", len(m.group("q"))
    interpolated, verbatim = "$" in pre, "@" in pre
    first = m.start("q")
    if not verbatim and q == 2:
        return first + 2                       # ""
    if not verbatim and q >= 3:                # a raw string: closed by a run of as many quotes
        k = text.find('"' * q, first + q)
        return len(text) if k < 0 else k + q
    k, n = first + 1, len(text)
    while k < n:
        c = text[k]
        if verbatim and c == '"':
            if text.startswith('""', k):
                k += 2
                continue
            return k + 1
        if not verbatim:
            if c == "\\":
                k += 2
                continue
            if c == '"':
                return k + 1
            if c == "\n":
                return k                        # unterminated: end it at the line
        if interpolated and c == "{":
            if text.startswith("{{", k):
                k += 2
                continue
            k = _code(text, out, k + 1, hole=True) + 1
            continue
        k += 1
    return n


def _match(b: str, i: int) -> int:
    """The index after the bracket closing the one at `i` (every bracket kind counted, as code nests them)."""
    depth = 0
    for m in BRACKETS.finditer(b, i):
        if m.group() in "{([":
            depth += 1
        else:
            depth -= 1
            if depth == 0:
                return m.end()
    return len(b)


def _header_end(b: str, p: int) -> tuple[int, str]:
    """Where a declaration's header ends, and how: `{`, `;`, `=>` or `=` outside its parentheses and brackets."""
    depth = 0
    for m in HEADER_STOP.finditer(b, p):
        c, k = m.group(), m.start()
        if c in "([":
            depth += 1
        elif c in ")]":
            depth -= 1
        elif depth == 0:
            if c in "{;":
                return k, c
            if c == "=":
                if b.startswith("=>", k):
                    return k, "=>"
                if not b.startswith("==", k) and b[k - 1] not in "=!<>":
                    return k, "="
    return len(b), ";"


def _to_semicolon(b: str, k: int) -> int:
    """The index after the first `;` outside every bracket from `k`."""
    depth = 0
    for m in HEADER_STOP.finditer(b, k):
        c = m.group()
        if c in "{([":
            depth += 1
        elif c in "})]":
            depth -= 1
        elif c == ";" and depth == 0:
            return m.end()
    return len(b)


def _member_end(b: str, stop: int, how: str) -> int:
    if how == ";":
        return stop + 1
    if how in ("=>", "="):
        return _to_semicolon(b, stop)
    end = _match(b, stop)
    m = NON_SPACE.search(b, end)
    k = m.start() if m else len(b)
    if k < len(b) and b[k] == "=" and not b.startswith("=>", k):  # a property's initializer: `{ get; } = x;`
        return _to_semicolon(b, k)
    return end


def _name(header: str) -> str:
    """A declaration's name: the first identifier before a top-level `(` that is not a modifier (a method, a
    constructor, an indexer's `this`), else the last identifier of the header (a property, a field, a type)."""
    h = header
    while True:
        g = re.sub(r"<[^<>]*>", "", h)
        if g == h:
            break
        h = g
    depth = 0
    for k, c in enumerate(h):
        if c == "(":
            if depth == 0:
                m = re.search(r"([A-Za-z_]\w*)\s*$", h[:k])
                if m and m.group(1) not in NOT_A_NAME:
                    return m.group(1)
            depth += 1
        elif c == ")":
            depth -= 1
    flat = re.sub(r"\[[^\]]*\]|\([^()]*\)", " ", h)
    names = IDENT_RE.findall(flat)
    return names[-1] if names else ""


def members(text: str) -> list[Member]:
    """Every member of every type the source declares, in source order (nested types' members included)."""
    return _members(text, blank(text))


def _members(text: str, b: str) -> list[Member]:
    """`members` of `text`, whose lexed form `b` is given."""
    starts = [0] + [m.end() for m in NEWLINE.finditer(text)]

    def line(idx: int) -> int:
        return bisect.bisect_right(starts, idx)

    found: list[Member] = []
    i = 0
    attrs: int | None = None   # where the pending attribute block starts
    while True:
        m = NON_SPACE.search(b, i)
        if not m:
            break
        i, c = m.start(), m.group()
        if c == "}":            # a container closes: its members are already read
            attrs, i = None, i + 1
            continue
        if c == "[":
            attrs = i if attrs is None else attrs
            i = _match(b, i)
            continue
        if c == ";":
            attrs, i = None, i + 1
            continue
        stop, how = _header_end(b, i)
        header = b[i:stop]
        if how == "{" and CONTAINER_RE.search(header.split("(", 1)[0]):
            attrs, i = None, stop + 1   # read the container's members next
            continue
        end = _member_end(b, stop, how)
        first = attrs if attrs is not None else i
        block_b, block_t = b[first:i], text[first:i]
        data = tuple(x or y for x, y in MEMBER_DATA_RE.findall(block_t)) if attrs is not None else ()
        found.append(Member(_name(header), line(first), line(max(end - 1, first)),
                            bool(TEST_ATTR_RE.search(block_b)), data))
        attrs, i = None, end
    return found


def balance(text: str) -> int:
    """The code brace balance of a source: 0 for a file the lexer reads right."""
    return _balance(blank(text))


def _balance(b: str) -> int:
    return b.count("{") - b.count("}")


def audit(roots: list[Path]) -> list[str]:
    """Every `.cs` file under `roots` whose code braces do not balance, or whose `[Fact]`/`[Theory]` attribute count
    differs from its test-member count."""
    findings = []
    for root in roots:
        for p in sorted(root.rglob("*.cs")):
            if {"bin", "obj"} & set(p.relative_to(root).parts):
                continue
            text = p.read_text(encoding="utf-8", errors="replace")
            b = blank(text)
            if _balance(b):
                findings.append(f"{p.as_posix()}: code braces do not balance ({b.count('{')} open, "
                                f"{b.count('}')} close) — the lexer misreads it")
                continue
            attrs = len(TEST_ATTR_RE.findall(b))
            tests = sum(1 for m in _members(text, b) if m.test)
            if attrs != tests:
                findings.append(f"{p.as_posix()}: {attrs} [Fact]/[Theory] attribute(s) but {tests} test member(s)")
    return findings


def self_test() -> int:
    """Drive every arm on planted sources (feedback_prove_the_watchdog_fails)."""
    failures = 0

    def check(label: str, ok: bool, detail: object = "") -> None:
        nonlocal failures
        failures += 0 if ok else 1
        print(f"  {'PASS' if ok else '⛔ FAIL'}  {label}{': ' + str(detail) if detail != '' else ''}")

    q, nl = '"', chr(10)
    src = nl.join([
        "namespace N;",                                                  # 1
        "#region tests",                                                 # 2
        "public sealed class T",                                         # 3
        "{",                                                             # 4
        "    [Fact]",                                                    # 5
        "    public void A()",                                           # 6
        "    {",                                                         # 7
        f"        string s = {q}}}{q} + @{q}a{q}{q}}}{q} + $\"x{{{{ {{(1 > 0 ? {q}}}{q} : {q}b{q})}}\";",  # 8
        "        char c = '}'; char d = '\\''; // }",                    # 9
        "        /* } */ var r = " + q * 3 + " { " + q * 3 + ";",       # 10
        "    }",                                                         # 11
        "    [Theory]",                                                  # 12
        "    [InlineData(" + q + "]" + q + ")]",                         # 13
        "    [MemberData(nameof(Rows))]",                                # 14
        "    public async Task B<TSlot>(string s) where TSlot : class { await Task.Yield(); }",  # 15
        "    public static IEnumerable<object[]> Rows => new[] { new object[] { 1 } };",        # 16
        "    private static int Helper() => 1;",                         # 17
        "    public int P { get; } = 5;",                                # 18
        "    private sealed class Inner",                                # 19
        "    {",                                                         # 20
        "        [Fact(Skip = " + q + "y" + q + ")] public void C() { }",  # 21
        "    }",                                                         # 22
        "    public (int A, int B) Pair() => (1, 2);",                   # 23
        "}",                                                             # 24
    ])
    got = {m.name: (m.lo, m.hi, m.test, m.data) for m in members(src)}
    check("the lexer blanks strings (regular, verbatim, interpolated with a hole's own string, raw), characters, "
          "comments and preprocessor lines: the code braces balance", balance(src) == 0, balance(src))
    check("a test method spans its attribute block through its body's closing brace",
          got.get("A") == (5, 11, True, ()), got.get("A"))
    check("a theory spans its data attributes and names its [MemberData] source; a `where T : class` constraint is "
          "not a type", got.get("B") == (12, 15, True, ("Rows",)), got.get("B"))
    check("an expression-bodied data member, a helper and a property with an initializer are members, never tests",
          got.get("Rows") == (16, 16, False, ()) and got.get("Helper") == (17, 17, False, ())
          and got.get("P") == (18, 18, False, ()), (got.get("Rows"), got.get("Helper"), got.get("P")))
    check("a nested type's test method is read (its Skip argument kept out of the name)",
          got.get("C") == (21, 21, True, ()), got.get("C"))
    check("a tuple return type does not hide the method's name", "Pair" in got and not got["Pair"][2],
          sorted(got))
    check("a class and a nested class are containers, not members (a file-scoped namespace is a declaration)",
          not {"T", "Inner"} & set(got), sorted(got))
    a = Member("A", 5, 11, True)
    check("a hunk touches a member: a changed line inside it, a deletion inside it or at its leading edge",
          a.touched_by(8, 1) and a.touched_by(4, 0) and a.touched_by(10, 0) and a.touched_by(11, 3), a)
    check("a hunk touches no member it does not reach: lines before or after it, a deletion after its last line",
          not a.touched_by(1, 3) and not a.touched_by(12, 2) and not a.touched_by(11, 0), a)
    broken = "class X { void M() { string s = " + q + "{" + q + "; } }"
    check("the audit's balance: a brace inside a string is not code, an unclosed body is counted",
          balance(broken) == 0 and balance("class X { void M() { ") == 2)
    with tempfile.TemporaryDirectory() as d:
        for name, body in [("Clean.cs", src), ("Open.cs", "class X { void M() { "),
                           ("Orphan.cs", "class X { [Fact] }"), ("obj/Skipped.cs", "class X {")]:
            (Path(d) / name).parent.mkdir(parents=True, exist_ok=True)
            (Path(d) / name).write_text(body, encoding="utf-8", newline="\n")
        found = audit([Path(d)])
    check("the audit names a file whose braces do not balance and one whose test attribute opens no test member, "
          "and skips build output", len(found) == 2 and "Open.cs" in found[0] and "Orphan.cs" in found[1], found)
    print("ALL GREEN" if not failures else f"⛔ {failures} SELF-TEST FAILURE(S)")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("files", nargs="*", type=Path, help="C# files whose members to print")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if args.self_test:
        return self_test()
    for f in args.files:
        for m in members(f.read_text(encoding="utf-8", errors="replace")):
            print(f"{f.as_posix()}:{m.lo}-{m.hi}  {m.name}{'  [test]' if m.test else ''}"
                  f"{'  data=' + ','.join(m.data) if m.data else ''}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
