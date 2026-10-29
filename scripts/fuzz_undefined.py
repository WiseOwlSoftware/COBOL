#!/usr/bin/env python3
"""fuzz_undefined.py — which programs the differential fuzz can say have an UNDEFINED result, decided from the source alone
(kb/Work PB3862; docs/rearchitecture/DESIGN-differential-fuzz.md §2).

    python scripts/test_fuzz_undefined.py        # its self-test: no build, no compiler

WHY THIS EXISTS. A model writes a program that DISPLAYs an item no statement wrote, and the two compilers print different
bytes (WiseOwl's zeros and spaces, GnuCOBOL's NULs). The standard leaves such a value undefined, so the difference is neither
compiler's. Eight of the fifth night's seventeen `OUTPUT_DIFFERS:14.9.25` rows were this and each cost a morning's triage
(PB3468 class 2). `differential_fuzz.classify` asks `undefined_results()` and files the row as the model's non-conforming
program (`UNDEFINED_RESULT`), never a lead. The same function answers the other undefined results PB3468 recorded that a
program's text proves.

THE RULE IS CONSERVATIVE: a finding is a PROOF, an abstention is not a verdict. Every shape the analysis cannot follow (a
COPY member, an OPTIONS paragraph, a RENAMES, a reference it cannot resolve) reads as "defined", because a false finding hides
a real difference from the triage while a missed one only costs the triage it cost before. The three findings:

  UNWRITTEN_STORAGE  a DISPLAY operand has a byte that no VALUE clause initialises and no statement writes (ISO A.2 19: the
                     initial value is undefined without a VALUE clause). A MOVE (and MOVE CORRESPONDING, ISO 14.7.6 1, 2) from
                     storage that is itself unwritten writes nothing defined either, so definedness is the LEAST FIXED POINT of
                     the program's data flow: a data item is defined when it has a VALUE (or a group above it does), is named
                     by any statement other than as a DISPLAY operand or a MOVE sender, or receives a MOVE from a defined
                     item or from anything that is not a data item (a literal, a figurative constant, a function).
  SUBSCRIPT_RANGE    an integer-literal subscript outside 1 .. the OCCURS maximum of its dimension (ISO 8.4.2.3.4 2): the
                     exception condition EC-BOUND-SUBSCRIPT, which when not enabled leaves the results undefined
                     (ISO 14.6.13.1.1 NOTE 3).
  INCOMPATIBLE_DATA  an alphanumeric literal holding a letter MOVEd to a numeric item: the content is false in a numeric class
                     condition, EC-DATA-INCOMPATIBLE, the results of the MOVE undefined (ISO 14.9.25.4 6).

WHAT IT DOES NOT SEE: a subscript or reference-modification position that is a variable (`T (WS-LEN)` after a PERFORM VARYING
ends at 4), and the content a data item holds at run time (a MOVE of a spaces item to a numeric one). Those need the program run.
A literal reference-modification range is not here: WiseOwl and GnuCOBOL both refuse it at compile time (DOC-A.1-69).
"""
from __future__ import annotations

import dataclasses
import re
from collections import defaultdict

UNWRITTEN_STORAGE = "UNWRITTEN_STORAGE"
SUBSCRIPT_RANGE = "SUBSCRIPT_RANGE"
INCOMPATIBLE_DATA = "INCOMPATIBLE_DATA"

#: the clause each finding stands on (every text checked with `python scripts/spec/cite.py --check`)
CITATIONS = {
    UNWRITTEN_STORAGE: "ISO A.2 19) (Undefined language element list): the initial value of a data item with no VALUE clause is undefined",
    SUBSCRIPT_RANGE: "ISO 8.4.2.3.4 2) and 14.6.13.1.1 NOTE 3: a subscript outside the OCCURS bounds sets EC-BOUND-SUBSCRIPT; "
                     "with the checking off the results are undefined",
    INCOMPATIBLE_DATA: "ISO 14.9.25.4 6): a sending operand false in a numeric class condition sets EC-DATA-INCOMPATIBLE and the "
                       "results of the MOVE are undefined",
}


@dataclasses.dataclass(frozen=True)
class Finding:
    kind: str
    detail: str

    def __str__(self) -> str:
        return f"{self.kind}: {self.detail}"


# ── the tokens ────────────────────────────────────────────────────────────────────────────────────────────────────────

#: Free reference format only (the fuzz compiles `--source-format free`). A comment, a string (with its N/X/B/Z/H prefix), a
#: PICTURE clause (its character-string is ONE token: it holds the parentheses and points the tokenizer would split), a decimal
#: number, a word, anything else. The commas and semicolons that separate operands are dropped.
_TOKEN = re.compile(r"""
    \*>[^\n]*
  | (?P<str>(?:[BbNn][Xx]|[NnXxBbZzHh])?(?:"(?:[^"]|"")*"|'(?:[^']|'')*'))
  | (?P<pic>(?<![\w-])PIC(?:TURE)?(?![\w-])(?:\s+IS(?![\w-]))?\s+(?P<picture>\S+))
  | (?P<num>\d+\.\d+)
  | (?P<word>[A-Za-z0-9][A-Za-z0-9_-]*)
  | (?P<punct>[^\s,;])
""", re.VERBOSE | re.IGNORECASE)

Token = tuple[str, str]       # (kind, text): kind is str | pic | num | int | word | punct


def tokenize(source: str) -> list[Token]:
    out: list[Token] = []
    for m in _TOKEN.finditer(source):
        if m.group("str"):
            out.append(("str", m.group("str")))
        elif m.group("pic"):
            picture = m.group("picture")
            ended = picture.endswith(".")
            out.append(("pic", picture.rstrip(".").upper() if ended else picture.upper()))
            if ended:
                out.append(("punct", "."))
        elif m.group("num"):
            out.append(("num", m.group("num")))
        elif m.group("word"):
            w = m.group("word")
            out.append(("int" if w.isdigit() else "word", w.upper()))
        elif m.group("punct"):
            out.append(("punct", m.group("punct")))
    return out


def _is(tok: Token, text: str) -> bool:
    return tok[0] == "word" and tok[1] == text


# ── the data division ─────────────────────────────────────────────────────────────────────────────────────────────────

@dataclasses.dataclass(eq=False)
class Item:
    """One WORKING-STORAGE data description entry (identity-hashed: two entries may share a name)."""
    level: int
    name: str | None                       # None: FILLER
    parent: "Item | None"
    children: list["Item"] = dataclasses.field(default_factory=list)
    picture: str | None = None
    usages: frozenset[str] = frozenset()
    valued: bool = False
    opaque: bool = False                   # EXTERNAL, BASED, an index or a pointer: not something this analysis follows
    redefines: str | None = None
    occurs_max: int | None = None

    def chain(self) -> list["Item"]:
        up, it = [], self
        while it is not None:
            up.append(it)
            it = it.parent
        return up[::-1]

    def leaves(self) -> list["Item"]:
        return [self] if not self.children else [x for c in self.children for x in c.leaves()]

    def numeric(self) -> bool:
        """Numeric category by the PICTURE (only 9 S V P and a repeat count) or by a USAGE that is numeric without one."""
        if self.picture is not None:
            return re.fullmatch(r"[9SVP()0-9]+", self.picture) is not None and any(c in self.picture for c in "9P")
        return bool(self.usages & NUMERIC_USAGES) and not self.children


NUMERIC_USAGES = frozenset({"BINARY", "COMP", "COMP-1", "COMP-2", "COMP-3", "COMP-4", "COMP-5", "COMPUTATIONAL", "COMPUTATIONAL-1",
                            "COMPUTATIONAL-2", "COMPUTATIONAL-3", "COMPUTATIONAL-4", "COMPUTATIONAL-5", "PACKED-DECIMAL",
                            "BINARY-CHAR", "BINARY-SHORT", "BINARY-LONG", "BINARY-DOUBLE", "FLOAT-SHORT", "FLOAT-LONG",
                            "FLOAT-EXTENDED", "FLOAT-BINARY-32", "FLOAT-BINARY-64", "FLOAT-BINARY-128", "FLOAT-DECIMAL-16",
                            "FLOAT-DECIMAL-34"})
OPAQUE_USAGES = frozenset({"INDEX", "POINTER", "FUNCTION-POINTER", "PROGRAM-POINTER", "OBJECT", "MESSAGE-TAG"})
_CLAUSE_WORDS = frozenset({"PIC", "USAGE", "VALUE", "VALUES", "OCCURS", "REDEFINES", "EXTERNAL", "BASED", "GLOBAL", "BLANK",
                           "JUSTIFIED", "JUST", "SYNCHRONIZED", "SYNC", "SIGN", "RENAMES", "DISPLAY", "BINARY", "COMP",
                           "COMPUTATIONAL", "PACKED-DECIMAL", "INDEX", "POINTER", "NATIONAL", "CONSTANT", "TYPEDEF", "IS"})
_SECTION_STARTS = ("LOCAL-STORAGE", "LINKAGE", "FILE", "SCREEN", "REPORT", "COMMUNICATION")


class DataDivision:
    """The WORKING-STORAGE entries of one program as a tree, and the name index every reference resolves through."""

    def __init__(self, tokens: list[Token]) -> None:
        self.items: list[Item] = []                   # every entry, FILLER included
        self.by_name: dict[str, list[Item]] = defaultdict(list)
        self.conditions: dict[str, Item] = {}         # a level-88 condition-name -> the item it tests
        self.renames = False
        start = next((i + 3 for i in range(len(tokens) - 3)
                      if _is(tokens[i], "WORKING-STORAGE") and _is(tokens[i + 1], "SECTION")), None)
        if start is None:
            return
        end = len(tokens)
        for i in range(start, len(tokens) - 1):
            if (tokens[i][0] == "word" and tokens[i][1] in _SECTION_STARTS and _is(tokens[i + 1], "SECTION")) \
                    or (_is(tokens[i], "PROCEDURE") and _is(tokens[i + 1], "DIVISION")):
                end = i
                break
        sentence: list[Token] = []
        stack: list[Item] = []
        for tok in tokens[start:end]:
            if tok == ("punct", "."):
                self._entry(sentence, stack)
                sentence = []
            else:
                sentence.append(tok)
        self._entry(sentence, stack)
        # a REDEFINES aliases storage: the redefining item and the item it names are both followed no further
        for it in self.items:
            if it.redefines:
                it.opaque = True
                for base in self.by_name.get(it.redefines, []):
                    base.opaque = True

    def _entry(self, s: list[Token], stack: list[Item]) -> None:
        if not s or s[0][0] != "int":
            return
        level = int(s[0][1])
        if level == 66:
            self.renames = True
            return
        if level == 78:
            return
        rest = s[1:]
        name = None
        if rest and rest[0][0] == "word" and rest[0][1] not in _CLAUSE_WORDS and rest[0][1] != "FILLER":
            name, rest = rest[0][1], rest[1:]
        elif rest and _is(rest[0], "FILLER"):
            rest = rest[1:]
        if level == 88:
            if name is not None and stack:
                self.conditions[name] = stack[-1]
            return
        while stack and (stack[-1].level >= level or level in (1, 77)):
            stack.pop()
        item = Item(level, name, stack[-1] if stack else None)
        if item.parent is not None:
            item.parent.children.append(item)
        self.items.append(item)
        usages: set[str] = set()
        for k, (kind, text) in enumerate(rest):
            if kind == "pic":
                item.picture = text
            elif kind == "word" and text in ("VALUE", "VALUES"):
                item.valued = True
            elif kind == "word" and text in ("EXTERNAL", "BASED"):
                item.opaque = True
            elif kind == "word" and text == "REDEFINES" and k + 1 < len(rest):
                item.redefines = rest[k + 1][1]
            elif kind == "word" and text == "OCCURS":
                counts = []
                for t2 in rest[k + 1:]:
                    if t2[0] == "int":
                        counts.append(int(t2[1]))
                    elif not _is(t2, "TO"):
                        break
                item.occurs_max = max(counts) if counts else None
            elif kind == "word" and text in NUMERIC_USAGES | OPAQUE_USAGES:
                usages.add(text)
        item.usages = frozenset(usages)
        if usages & OPAQUE_USAGES:
            item.opaque = True
        stack.append(item)
        if name is not None:
            self.by_name[name].append(item)


# ── the procedure division ────────────────────────────────────────────────────────────────────────────────────────────

_VERBS = frozenset("""ACCEPT ADD ALLOCATE ALTER CALL CANCEL CLOSE COMPUTE CONTINUE DELETE DISPLAY DIVIDE EVALUATE EXIT FREE
    GENERATE GO GOBACK IF INITIALIZE INITIALISE INITIATE INSPECT INVOKE MERGE MOVE MULTIPLY OPEN PERFORM RAISE READ RELEASE
    RESUME RETURN REWRITE SEARCH SET SORT START STOP STRING SUBTRACT SUPPRESS TERMINATE UNLOCK UNSTRING USE VALIDATE WRITE""".split())
#: the words after which a DISPLAY or MOVE operand list has ended, besides a verb, an END-xxx and the period
_OPERAND_ENDS = _VERBS | {"ELSE", "WHEN", "THEN", "UPON", "WITH", "ON", "NOT", "AT", "INVALID", "AFTER", "BEFORE"}


@dataclasses.dataclass
class Ref:
    items: list[Item]
    end: int                       # index of the first token after the reference
    groups: list[list[Token]]      # each parenthesised group after the name: subscripts, or `start : length`

    @property
    def refmod(self) -> bool:
        return any(("punct", ":") in g for g in self.groups)

    @property
    def one(self) -> Item | None:
        return self.items[0] if len(self.items) == 1 else None


class Analysis:
    def __init__(self, tokens: list[Token], data: DataDivision) -> None:
        self.t = tokens
        self.data = data
        self.defined: set[Item] = set()
        self.edges: list[tuple[list[Item] | None, list[Item]]] = []     # (sources | None: always defined, targets)
        self.displayed: list[tuple[Item, str]] = []
        self.findings: dict[tuple[str, str], Finding] = {}

    # --- references
    def candidates(self, name: str) -> list[Item]:
        if name in self.data.conditions:
            return [self.data.conditions[name]]
        return self.data.by_name.get(name, [])

    def parse_ref(self, i: int) -> Ref | None:
        t = self.t
        if i >= len(t) or t[i][0] != "word":
            return None
        found = self.candidates(t[i][1])
        if not found:
            return None
        j = i + 1
        while j + 1 < len(t) and t[j][0] == "word" and t[j][1] in ("OF", "IN") and t[j + 1][0] == "word":
            found = [it for it in found if any(a.name == t[j + 1][1] for a in it.chain()[:-1])]
            j += 2
        groups: list[list[Token]] = []
        while j < len(t) and t[j] == ("punct", "("):
            depth, k = 1, j + 1
            while k < len(t) and depth:
                depth += (t[k] == ("punct", "(")) - (t[k] == ("punct", ")"))
                k += 1
            groups.append(t[j + 1:k - 1])
            j = k
        return Ref(found, j, groups)

    def touch(self, items: list[Item]) -> None:
        stack = list(items)
        while stack:
            it = stack.pop()
            if it not in self.defined:
                self.defined.add(it)
                stack.extend(it.children)

    def read_group(self, ref: Ref) -> None:
        """The names inside a subscript or reference-modification group are used as they stand: followed no further."""
        for g in ref.groups:
            k = 0
            while k < len(g):
                inner = self.parse_ref_in(g, k)
                if inner is None:
                    k += 1
                else:
                    self.touch(inner.items)
                    k = inner.end
        item = ref.one
        if item is not None and ref.groups and not ref.refmod:
            self.subscripts(item, ref.groups[0])

    def parse_ref_in(self, g: list[Token], k: int) -> Ref | None:
        saved, self.t = self.t, g
        try:
            return self.parse_ref(k)
        finally:
            self.t = saved

    def subscripts(self, item: Item, g: list[Token]) -> None:
        if not g or any(tok[0] != "int" for tok in g):
            return
        maxima = [(x.name or "FILLER", x.occurs_max) for x in item.chain() if x.occurs_max is not None]
        if len(maxima) != len(g):
            return
        for tok, (owner, top) in zip(g, maxima):
            if not 1 <= int(tok[1]) <= top:
                self.add(SUBSCRIPT_RANGE, f"{item.name or 'FILLER'} ({tok[1]}) is outside 1 to {top}, the OCCURS of {owner}")

    def add(self, kind: str, detail: str) -> None:
        self.findings.setdefault((kind, detail), Finding(kind, detail))

    # --- the statements
    def run(self, i: int) -> None:
        t = self.t
        while i < len(t):
            if t[i][0] == "word" and t[i][1] == "DISPLAY":
                i = self.display(i + 1)
            elif t[i][0] == "word" and t[i][1] == "MOVE":
                i = self.move(i + 1)
            elif _is(t[i], "FUNCTION"):
                i = self.skip_function(i)
            else:
                ref = self.parse_ref(i)
                if ref is None:
                    i += 1
                else:
                    self.touch(ref.items)
                    self.read_group(ref)
                    i = ref.end

    def skip_function(self, i: int) -> int:
        """FUNCTION name (arguments): the arguments are read, never written, and the result is never a data item here."""
        j = i + 2
        if j < len(self.t) and self.t[j] == ("punct", "("):
            depth = 0
            while j < len(self.t):
                depth += (self.t[j] == ("punct", "(")) - (self.t[j] == ("punct", ")"))
                j += 1
                if depth == 0:
                    break
        return min(j, len(self.t))

    def operand_ends(self, j: int) -> bool:
        tok = self.t[j]
        return tok == ("punct", ".") or (tok[0] == "word" and (tok[1] in _OPERAND_ENDS or tok[1].startswith("END-")))

    def display(self, i: int) -> int:
        while i < len(self.t) and not self.operand_ends(i):
            if _is(self.t[i], "FUNCTION"):
                i = self.skip_function(i)
                continue
            ref = self.parse_ref(i)
            if ref is None:
                i += 1
                continue
            self.read_group(ref)
            if ref.one is not None:
                text = ref.one.name or "FILLER"
                self.displayed.append((ref.one, text))
            else:
                self.touch(ref.items)           # ambiguous: illegal source, and no proof from it
            i = ref.end
        return i

    def move(self, i: int) -> int:
        t = self.t
        corr = i < len(t) and t[i][0] == "word" and t[i][1] in ("CORR", "CORRESPONDING")
        if corr:
            i += 1
        to = next((k for k in range(i, len(t)) if _is(t[k], "TO") or t[k] == ("punct", ".")
                   or (t[k][0] == "word" and t[k][1] in _VERBS)), None)
        if to is None or not _is(t[to], "TO"):
            return i
        sender = self.parse_ref(i) if to > i else None
        source: Ref | None = sender if sender is not None and sender.end == to else None
        # an unprefixed alphanumeric literal: the only string whose letters are characters, not a national or hex notation
        literal = t[i][1] if to == i + 1 and t[i][0] == "str" and t[i][1][0] in ("'", chr(34)) else None
        if source is not None:
            self.read_group(source)
        j = to + 1
        while j < len(t) and not self.operand_ends(j):
            recv = self.parse_ref(j)
            if recv is None:
                j += 1
                continue
            self.read_group(recv)
            self.move_to(source, recv, corr, literal)
            j = recv.end
        return j

    def move_to(self, source: Ref | None, recv: Ref, corr: bool, literal: str | None) -> None:
        if recv.refmod or len(recv.items) != 1 or (source is not None and len(source.items) != 1):
            self.touch(recv.items)              # a partial or ambiguous receiver: a write whose reach is not followed
            return
        target = recv.items[0]
        if literal is not None and re.search(r"[A-Za-z]", literal) and target.numeric() and not recv.groups:
            self.add(INCOMPATIBLE_DATA, f"MOVE {literal} TO {target.name or 'FILLER'}, a numeric item")
        if source is None:
            self.edges.append((None, target.leaves()))
        elif corr and (source.items[0].children or target.children):
            for s, t in corresponding(source.items[0], target):
                self.edges.append((s.leaves(), t.leaves()))
        else:
            self.edges.append((source.items[0].leaves(), target.leaves()))

    # --- the fixed point
    def settle(self) -> None:
        for it in self.data.items:
            if it.valued or it.opaque:
                self.touch([it])
        changed = True
        while changed:
            changed = False
            for sources, targets in self.edges:
                if (sources is None or any(s in self.defined for s in sources)) and any(x not in self.defined for x in targets):
                    self.touch(targets)
                    changed = True
        for item, text in self.displayed:
            gone = [x for x in item.leaves() if x not in self.defined]
            if gone:
                which = text if item is gone[0] else f"{text} (its member {gone[0].name or 'FILLER'})"
                self.add(UNWRITTEN_STORAGE, f"DISPLAY reads {which}, which has no VALUE clause and no statement writes it")


def corresponding(src: Item, dst: Item) -> list[tuple[Item, Item]]:
    """The pairs of ISO 14.7.6 1) and 2) for a MOVE CORRESPONDING: the same data-name under the same qualifiers below the two
    groups, FILLER excluded, at least one of the pair elementary. Rules 4) to 6) (OCCURS, REDEFINES, uniqueness) are not applied:
    a pair counted that the standard excludes can only make a receiver read as written, the safe direction."""
    def below(group: Item, path: tuple[str, ...]) -> dict[tuple[str, ...], list[Item]]:
        found: dict[tuple[str, ...], list[Item]] = defaultdict(list)
        for c in group.children:
            if c.name is not None:
                found[path + (c.name,)].append(c)
            for k, v in below(c, path + ((c.name,) if c.name is not None else ("",))).items():
                found[k].extend(v)
        return found
    a, b = below(src, ()), below(dst, ())
    return [(s, t) for key in a.keys() & b.keys() for s in a[key] for t in b[key] if not s.children or not t.children]


def undefined_results(source: str) -> list[Finding]:
    """Every undefined result the program's text proves (module docstring); [] for a program the analysis does not follow."""
    tokens = tokenize(source)
    words = {t[1] for t in tokens if t[0] == "word"}
    if words & {"COPY", "REPLACE", "OPTIONS", "INITIALIZED"} or ">>" in source or sum(_is(t, "PROGRAM-ID") for t in tokens) != 1:
        return []
    data = DataDivision(tokens)
    proc = next((i + 2 for i in range(len(tokens) - 1) if _is(tokens[i], "PROCEDURE") and _is(tokens[i + 1], "DIVISION")), None)
    if proc is None:
        return []
    analysis = Analysis(tokens, data)
    analysis.run(proc)
    if data.renames:
        analysis.displayed.clear()          # RENAMES aliases storage this analysis does not follow
    analysis.settle()
    return list(analysis.findings.values())
