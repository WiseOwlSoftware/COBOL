#!/usr/bin/env python3
"""Which coverage points the tests accepted at an edition show: WiseOwl's parse of each test, matched to the catalog's
formats (DESIGN-conformance-suite.md section 3.4, decision 8; kb/Work PB6050).

    python conformance-suite/harness/coverage.py --edition 85 --record [--cobol <cobol.dll>]   parse every test accepted at 85
                                                                                              and write wiseowl/points/
    python conformance-suite/harness/coverage.py --edition 85                                  the points credited and owed

THE CREDIT. `cobol points-batch` (kb/Work PB6050) prints, per test, the token stream of each parse node of the rules named
here (`frames_of`: the grammar rules whose nodes carry a row's formats): a nested statement is one token, and so is each context of
a rule in `OPERAND_RULES`, so an operand of a format (`identifier-1`) is one token whatever its length. This module matches each
node, whole, against each format of the rows that name its rule (`Matcher`: a chart parse of the node's tokens against the
format's tree, every derivation of the whole node) and credits the points that lie on some derivation: the alternative
taken, the bracket or optional word written or left out, the portion repeated, the format itself. A token is a
keyword's text, so a keyword point is credited only where the keyword is in the text, never from a lexical scan of the source.
No verdict reads this: a grammar defect that credits a wrong point is bounded by `operand_ok`'s map, the drift check below, and a
second compiler's run of the test.

THE RECORD, `wiseowl/points/<file>.json` (one per case file, `{"<test id>": {"85": [point ids]}}`), is what this module reads at
`unmet`: the credit is a measurement of the tests as they stand, recorded when they change, and `--check` re-derives nothing.
A point is OWED when no test credits it. A row whose formats no parse node carries (`NO_FRAME`: COPY and REPLACE are text
manipulation, read before the parse) keeps every point owed, which the report names.
"""
from __future__ import annotations

import argparse
import collections
import glob
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
SUITE = HERE.parent
REPO = SUITE.parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
sys.path.append(str(REPO / "scripts"))  # scripts/, for standard_streams
import formats  # noqa: E402
import points  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

POINTS_DIR = SUITE / "wiseowl" / "points"
INPUTS = SUITE / "wiseowl" / "points-inputs.json"
GRAMMAR = REPO / "src" / "WiseOwl.Cobol.Frontend" / "Grammar"

# ── what a row's formats are carried by ──────────────────────────────────────────────────────────────────────────────

#: Rows whose rule is not named by `<The X> <STATEMENT|CLAUSE|PARAGRAPH|ENTRY>` in the grammar, by the row's section
#: (the part of its id after the module's numeral): the grammar rules whose nodes carry the row's formats.
FRAME_OVERRIDES: dict[str, list[str]] = {
    "IV.4.3.8.2.2": ["dataReference"], "IV.4.3.8.3.2": ["functionCall"],
    "IV.4.3.8.4.2": ["dataReference"], "IV.4.3.8.5.2": ["dataReference"],
    "IV.5.3.1": ["programUnit"], "VI.2.3.1": ["programUnit"], "VI.2.4.2": ["endProgramHeader"], "VI.5.5.2": ["dataName"], "VI.5.7.2": ["levelNumber"],
    "VI.5.13.2": ["syncClause"], "VII.2.2.2": ["fileControlParagraph"], "VII.2.3.2": ["fileControlClauseGroup"],
    "VII.2.6.2": ["organizationClause"], "VII.2.7.2": ["paddingCharacterClause"], "VII.2.8.2": ["recordDelimiterClause"],
    "VII.2.9.2": ["fileReserveClause"], "VII.2.10.2": ["ioControlParagraph"], "VII.2.11.2": ["multipleFileClause"],
    "VII.3.9.2": ["valueOfClause"], "VIII.2.3.2": ["fileControlClauseGroup"], "VIII.2.5.2": ["organizationClause"],
    "VIII.2.6.2": ["ioControlParagraph"], "IX.2.3.2": ["fileControlClauseGroup"], "IX.2.5.2": ["alternateKeyClause"],
    "IX.2.6.2": ["organizationClause"], "IX.2.7.2": ["recordKeyClause"], "IX.2.8.2": ["ioControlParagraph"],
    "X.2.3.1": ["programUnit"], "X.2.5.2": ["endProgramHeader"], "X.3.1.2": ["programIdParagraph"],
    "X.4.2.2": ["fileDescriptionEntry"], "X.4.3.2": ["dataDescriptionEntry"], "X.4.4.2": ["reportDescriptionEntry"],
    "X.5.4.2": ["exitStatement"], "X.5.6.2": ["useStatement"], "XI.2.3.2": ["fileControlClauseGroup"],
    "XI.2.4.2": ["ioControlParagraph"], "XI.2.5.2": ["sameClause"], "XI.3.2.2": ["sortMergeDescriptionEntry"],
    "XIII.2.3.2": ["fileControlClauseGroup"], "XIII.2.4.2": ["ioControlParagraph"], "XIII.3.2.2": ["fileDescriptionEntry"],
    "XIII.3.3.2": ["reportClause"], "XIII.3.5.2": ["reportDescriptionEntry"], "XIII.3.6.2": ["reportCodeClause"],
    "XIII.3.7.2": ["reportControlClause"], "XIII.3.8.2": ["reportPageClause"], "XIII.3.9.2": ["reportGroupEntry"],
    "XIII.3.11.2": ["reportColumnClause"], "XIII.3.12.2": ["dataName"], "XIII.3.13.2": ["reportGroupIndicateClause"],
    "XIII.3.14.2": ["levelNumber"], "XIII.3.15.2": ["reportLineClause"], "XIII.3.16.2": ["reportNextGroupClause"],
    "XIII.3.17.2": ["signClause"], "XIII.3.18.2": ["reportSourceClause"], "XIII.3.19.2": ["reportSumClause"],
    "XIII.3.20.2": ["reportTypeClause"], "XIII.3.21.2": ["usageClause"], "XIII.3.22.2": ["valueClause"],
    "XIII.4.8.2": ["useStatement"], "XIII.4.9.2": ["useStatement"], "XIV.3.1.2": ["acceptMessageCountStatement"],
    "XIV.3.5.2": ["communicationReceiveStatement"], "XIV.3.6.2": ["communicationSendStatement"],
    "XV.2.1.2": ["sourceComputerParagraph"], "XV.3.2.2": ["useStatement"], "XVI.2.3.2": ["segmentLimitClause"],
}
STRUCTURE_WHY = ("a division's structure: its operands are sections, paragraphs and entries, which the parse holds as rules nested in "
                 "the division, so no token stream of the division shows them as the format prints them")
#: Rows no parse node carries, with why: the row keeps every point owed (the report names them).
NO_FRAME: dict[str, str] = {
    "IV.4.2.2.2.1.1": "a literal is one token to the lexer: the characters of a nonnumeric literal are no tokens of the parse",
    "XII.2.2": "COPY is read by the preprocessor before the parse (§1985:XII.2); no parse node holds its text",
    "XII.3.2": "REPLACE is read by the preprocessor before the parse (§1985:XII.3); no parse node holds its text",
    "XVI.3.2.2": "a section header's text is not a parse node of its own: the section's rule holds every paragraph after it",
    "IV.6.1.3.1": STRUCTURE_WHY, "IV.6.2.3.1": STRUCTURE_WHY, "IV.6.3.2.2.1": STRUCTURE_WHY, "VI.3.2.1.1": STRUCTURE_WHY,
}
#: A clause or entry row's frames by the module that holds it, for rows that name no rule of their own.
SUBJECT = re.compile(r"^THE (.*?) (STATEMENT|CLAUSE|PARAGRAPH|ENTRY)$")
#: Sections of the 1989 amendment's intrinsic functions (`1989-FMT-A.2.<n>.2`): every one is a function call.
FUNCTION_ROW = re.compile(r"^A\.2\.\d+\.2$")
#: A row whose name is a rule's name in another spelling or a word the grammar splits.
WORD_FIXUPS = {"I-O-CONTROL": "ioControl", "FILE CONTROL": "fileControl"}


def grammar_rules() -> set[str]:
    rules: set[str] = set()
    for f in glob.glob(str(GRAMMAR / "**" / "*.g4"), recursive=True):
        for line in Path(f).read_text(encoding="utf-8").splitlines():
            if (m := re.match(r"^([a-z][A-Za-z0-9]*)\s*(:|$)", line)):
                rules.add(m.group(1))
    return rules


def camel(words: str) -> str:
    parts = [w for w in re.split(r"[^A-Za-z0-9]+", words.lower()) if w]
    return parts[0] + "".join(w.capitalize() for w in parts[1:])


def frames_of(row_id: str, subject: str, rules: set[str]) -> list[str] | None:
    """The grammar rules whose parse nodes carry the row's formats; None when the row has no parse node (`NO_FRAME`);
    raises for a row it cannot place, so a row added to the catalog is never silently owed."""
    section = row_id.split("-FMT-", 1)[1]
    if section in NO_FRAME:
        return None
    if section in FRAME_OVERRIDES:
        out = FRAME_OVERRIDES[section]
    elif FUNCTION_ROW.match(section):
        out = ["functionCall"]
    elif (m := SUBJECT.match(subject)) and (cand := camel(m.group(1)) + m.group(2).capitalize()) in rules:
        out = [cand]
    else:
        raise points.CatalogError(f"{row_id} ({subject!r}) has no grammar rule that carries its formats: name it in "
                                  "coverage.FRAME_OVERRIDES, or in NO_FRAME with the reason")
    missing = [r for r in out if r not in rules]
    if missing:
        raise points.CatalogError(f"{row_id}: the grammar has no rule {', '.join(missing)}")
    return out


#: The rules whose contexts inside a node are one token each (an operand of a general format, whatever its length).
OPERAND_RULES = (
    "dataReference", "literal", "figurativeConstant", "arithmeticExpression", "condition", "procedureName", "fileName",
    "mergeFileName", "sortFileName", "dataName", "recordName", "reportName", "cdName", "className", "programName",
    "sectionName", "computerName", "conditionName", "integerOperand", "integerLiteral", "signedIntegerLiteral",
    "functionCall", "identificationDivision", "environmentDivision", "dataDivision", "procedureDivision", "endProgramHeader",
    "programUnit", "dataDescriptionEntry", "reportGroupEntry", "fileControlClauseGroup",
)
DIVISIONS = {"identification-division": "identificationDivision", "environment-division": "environmentDivision",
             "data-division": "dataDivision", "procedure-division": "procedureDivision",
             "end-program-header": "endProgramHeader", "source-program": "programUnit",
             "record-description-entry": "dataDescriptionEntry", "77-level-description-entry": "dataDescriptionEntry",
             "report-group-description-entry": "reportGroupEntry", "file-control-entry": "fileControlClauseGroup"}
NAMES = {"dataReference", "dataName", "fileName", "mergeFileName", "sortFileName", "recordName", "reportName", "cdName",
         "className", "programName", "sectionName", "computerName", "conditionName", "procedureName", "functionCall"}
LITERALS = {"literal", "figurativeConstant", "integerLiteral", "signedIntegerLiteral", "integerOperand"}
EXPRESSIONS = {"arithmeticExpression", "dataReference", "functionCall", "integerOperand", "integerLiteral"} | LITERALS
STATEMENT = "<statement>"
#: The figurative constants (§1985:II.4.2.2.3 and the Procedure Division's literal operands): reserved words standing for a literal.
FIGURATIVES = {"ZERO", "ZEROS", "ZEROES", "SPACE", "SPACES", "HIGH-VALUE", "HIGH-VALUES", "LOW-VALUE", "LOW-VALUES", "QUOTE", "QUOTES",
               "ALL"}
#: The entries a frame's node holds at its end that the format of its row does not print (a record description follows the file
#: description entry's period): the rule whose placeholders are dropped from the end.
TRAILING_ENTRY = {"fileDescriptionEntry": "dataDescriptionEntry", "sortMergeDescriptionEntry": "dataDescriptionEntry",
                  "reportDescriptionEntry": "reportGroupEntry"}
#: Frames whose nodes keep every token of their operands (`cobol points-batch --flat`): a reference shows its subscripts.
FLAT_FRAMES = ("dataReference",)

# A token is `(text, kind)`; kind is `id` (a user word), `int`, `num`, `str`, `kw` (anything else: a reserved word or a mark)
# or `ph` (a context standing as one token: `text` is its rule, or STATEMENT).
Token = tuple[str, str]

#: Operand stem (a format's `identifier-1` less its number) -> the kinds of token it matches. A stem not listed matches any one
#: token and is named by `--report`, so a format added to the catalog shows its new operand.
OPERAND_KINDS: dict[str, "set[str] | None"] = {}
#: Stems that match a run of one or more statements, and stems that match any run of tokens (entries the parse holds as text).
STATEMENT_RUN = {"imperative-statement", "statement"}
ANY_RUN = {"comment-entry", "special-names-entry", "source-computer-entry",
           "object-computer-entry", "input-output-control-entry", "file-description-entry", "report-file-description-entry",
           "sort-merge-file-description-entry", "communication-description-entry", "report-description-entry",
           "program-identification-entry", "==pseudo-text-1==", "==pseudo-text-2==", "character-string"}


def operand_ok(stem: str, tok: Token) -> bool:
    """Whether one token can be an operand named `stem` (a format's metavariable less its number)."""
    text, kind = tok
    if kind == "ph" and text == STATEMENT:
        return False
    if stem in DIVISIONS:
        return tok == (DIVISIONS[stem], "ph")
    if stem in ("identifier", "data-name", "condition-name", "index-name", "cd-name", "record-name", "report-name",
                "file-name", "procedure-name", "class-name", "program-name", "section-name", "symbolic-character", "text-name",
                "library-name", "routine-name", "function-identifier", "subscript", "argument", "word"):
        if stem in ("program-name", "argument", "subscript", "function-identifier"):
            return kind in ("id", "int", "num", "str") or (kind == "ph" and (text in EXPRESSIONS or text in NAMES))
        return kind == "id" or (kind == "ph" and text in NAMES)
    if stem in ("mnemonic-name", "implementor-name", "alphabet-name", "computer-name", "language-name", "function-name",
                "character"):
        return kind in ("id", "kw") or (kind == "ph" and text in NAMES)
    if stem == "literal":
        return kind in ("int", "num", "str") or (kind == "kw" and text in FIGURATIVES) or (kind == "ph" and text in LITERALS)
    if stem in ("integer", "level-number", "segment-number"):
        return kind == "int" or (kind == "ph" and text in ("integerLiteral", "integerOperand", "signedIntegerLiteral"))
    if stem == "condition":
        return kind == "id" or (kind == "ph" and text == "condition")
    if stem in ("arithmetic-expression", "expression", "reference-modifier", "leftmost-character-position", "length"):
        return kind in ("id", "int", "num") or (kind == "ph" and text in EXPRESSIONS)
    return True                                    # an unlisted stem: any one token (`--report` names them)


class Matcher:
    """A chart parse of a node's tokens against one format's tree: `ends(n, i)` the positions a node can reach from `i`,
    and `credit` the (node, event) pairs that lie on a derivation of the whole text."""

    def __init__(self, root: formats.Node, tokens: list[Token]) -> None:
        self.root, self.t, self.n = root, tokens, len(tokens)
        self.stop = {n.text for n in points.number(root) if n.kind in (formats.WORD, formats.OPTIONAL)}
        self.memo: dict[tuple[int, int], frozenset[int]] = {}
        self.events: set[tuple[int, str]] = set()
        self.seen: set[tuple[int, int, int]] = set()

    # -- the chart
    def ends(self, node: formats.Node, i: int) -> frozenset[int]:
        key = (id(node), i)
        if key in self.memo:
            return self.memo[key]
        self.memo[key] = frozenset()                      # a cycle through an empty portion adds nothing
        out = self._ends(node, i)
        self.memo[key] = out
        return out

    def _ends(self, node: formats.Node, i: int) -> frozenset[int]:
        k, t = node.kind, self.t
        if k == formats.WORD:
            if node.text.isdigit():                        # a level number: any integer, its spelling is no part of a format
                return frozenset({i + 1}) if i < self.n and t[i][1] == "int" else frozenset()
            return frozenset({i + 1}) if i < self.n and t[i][1] in ("kw", "id") and t[i][0] == node.text else frozenset()
        if k == formats.OPTIONAL:
            out = {i}
            if i < self.n and t[i][1] in ("kw", "id") and t[i][0] == node.text:
                out.add(i + 1)
            return frozenset(out)
        if k == formats.OPERAND:
            return self._operand(node, i)
        if k == formats.SEQ:
            pos = {i}
            for c in node.kids:
                pos = {j for p in pos for j in self.ends(c, p)}
                if not pos:
                    break
            return frozenset(pos)
        if k == formats.ALT:
            return frozenset(j for c in node.kids for j in self.ends(c, i))
        if k == formats.OPT:
            return frozenset({i}) | self.ends(node.kids[0], i)
        if k == formats.REP:
            out: set[int] = set()
            frontier = set(self.ends(node.kids[0], i))
            while frontier:
                out |= frontier
                frontier = {j for p in frontier for j in self.ends(node.kids[0], p) if j > p and j not in out}
            return frozenset(out)
        if k == formats.BAR:
            return frozenset(stops[-1] for _, stops in self._bar_chains(node, i))
        raise AssertionError(k)

    def _operand(self, node: formats.Node, i: int) -> frozenset[int]:
        stem = node.text
        if stem in STATEMENT_RUN:
            out, j = set(), i
            while j < self.n and self.t[j] == (STATEMENT, "ph"):
                j += 1
                out.add(j)
            return frozenset(out)
        if stem in ANY_RUN:                                # up to the next word the format itself prints
            end = i
            while end < self.n and not (self.t[end][1] == "kw" and self.t[end][0] in self.stop):
                end += 1
            return frozenset(range(i, end + 1))
        return frozenset({i + 1}) if i < self.n and operand_ok(stem, self.t[i]) else frozenset()

    def _bar_chains(self, node: formats.Node, i: int) -> list[tuple[tuple[int, ...], tuple[int, ...]]]:
        """Every way a pair of choice indicators is written from `i`: the alternatives used, in order, each at most once, and the
        position after each (section 5.2.6.4 of the 2023 text, the notation's: any single alternative alone, or several in any order)."""
        out: list[tuple[tuple[int, ...], tuple[int, ...]]] = []

        def go(used: tuple[int, ...], stops: tuple[int, ...], p: int) -> None:
            for a, alt in enumerate(node.kids):
                if a in used:
                    continue
                for j in self.ends(alt, p):
                    if j > p:
                        out.append(((*used, a), (*stops, j)))
                        go((*used, a), (*stops, j), j)
        go((), (), i)
        return out

    # -- the credit
    def credit(self) -> bool:
        """Mark every event on a derivation of the whole text; False when the format derives none."""
        if self.n not in self.ends(self.root, 0):
            return False
        self.events.add((id(self.root), "format"))
        self._mark(self.root, 0, self.n)
        return True

    def _mark(self, node: formats.Node, i: int, j: int) -> None:
        key = (id(node), i, j)
        if key in self.seen:
            return
        self.seen.add(key)
        k = node.kind
        if k == formats.OPTIONAL:
            self.events.add((id(node), "written" if j > i else "omit"))
        elif k == formats.SEQ:
            self._mark_seq(node.kids, i, j)
        elif k == formats.ALT:
            for c in node.kids:
                if j in self.ends(c, i):
                    self.events.add((id(c), "written" if j > i else "omit"))
                    self._mark(c, i, j)
        elif k == formats.OPT:
            body = node.kids[0]
            if j == i:
                self.events.add((id(node), "omit"))
            elif j in self.ends(body, i):
                if body.kind != formats.ALT:
                    self.events.add((id(node), "written"))
                self._mark(body, i, j)
        elif k == formats.REP:
            self._mark_rep(node, i, j)
        elif k == formats.BAR:
            for used, stops in self._bar_chains(node, i):
                if stops[-1] != j:
                    continue
                if len(used) == 1:
                    self.events.add((id(node.kids[used[0]]), "alone"))
                elif list(used) != sorted(used):
                    self.events.add((id(node), "order"))
                p = i
                for a, q in zip(used, stops):
                    self._mark(node.kids[a], p, q)
                    p = q

    def _mark_seq(self, kids: list[formats.Node], i: int, j: int) -> None:
        fwd = [{i}]
        for c in kids:
            fwd.append({b for a in fwd[-1] for b in self.ends(c, a)})
        back: list[set[int]] = [set() for _ in range(len(kids) + 1)]
        back[len(kids)] = {j}
        for t in range(len(kids) - 1, -1, -1):
            back[t] = {a for a in fwd[t] if any(b in back[t + 1] for b in self.ends(kids[t], a))}
        for t, c in enumerate(kids):
            for a in back[t] & fwd[t]:
                for b in self.ends(c, a) & back[t + 1]:
                    self._mark(c, a, b)

    def _mark_rep(self, node: formats.Node, i: int, j: int) -> None:
        """A portion written one or more times from `i` to `j`: every step on some chain is marked, and the portion is written
        twice when a chain of two or more steps reaches `j`."""
        c = node.kids[0]
        if i == j:
            self._mark(c, i, i)
            return
        edges: dict[int, set[int]] = {}
        stack, seen = [i], {i}
        while stack:
            a = stack.pop()
            for b in self.ends(c, a):
                if b > a:
                    edges.setdefault(a, set()).add(b)
                    if b not in seen:
                        seen.add(b)
                        stack.append(b)
        order = sorted(seen)
        steps: dict[int, set[int]] = {i: {0}}              # position -> the chain lengths (capped at 2) that reach it
        for a in order:
            for b in edges.get(a, ()):
                steps.setdefault(b, set()).update(min(m + 1, 2) for m in steps.get(a, ()))
        if 2 in steps.get(j, ()):
            self.events.add((id(node), "rep"))
        co = {j}
        for a in reversed(order):
            if any(b in co for b in edges.get(a, ())):
                co.add(a)
        for a in order:
            for b in sorted(edges.get(a, ())):
                if a in co and b in co:
                    self._mark(c, a, b)


# ── reading the parse ─────────────────────────────────────────────────────────────────────────────────────────────────

def token_of(text: str, typ: str, keywords: "set[str] | frozenset[str]" = frozenset()) -> Token | None:
    """A token as the matcher sees it; None for the period, which separates sentences and is no token of a format."""
    if typ == "<context>":
        return (text, "ph")
    if text in (".", ","):
        return None                                        # separators: a comma is a space, a period ends a sentence
    if typ == "IDENTIFIER":
        return (text if text in keywords else "", "id")    # a user word's spelling is part of a format only where a format prints it
    if typ == "INTEGERLIT":
        return ("", "int")
    if typ.endswith("LIT"):
        return ("", "num" if typ.startswith(("DECIMAL", "FLOAT", "SIGNED")) else "str")
    return (text, "kw")


def tokens_of(raw: list, keywords: "set[str] | frozenset[str]") -> list[Token]:
    """A node's tokens as the matcher takes them; `keywords` are the words some format prints, which a user word may spell
    (`FUNCTION COS (...)`: COS is lexed a user word, and the format prints it required)."""
    return [t for t in (token_of(x, y, keywords) for x, y in raw) if t is not None]


class Catalog:
    """The committed catalog, ready to match: each row's formats as trees, and the rules whose nodes carry them."""

    def __init__(self, data: dict) -> None:
        import inventory_view
        rules = grammar_rules()
        subjects = {r["rule-id"]: r["subject"] for r in inventory_view.inventory(points.EDITION) if r["kind"] == "FMT"}
        self.formats: list[tuple[str, int, formats.Node, list[dict]]] = []
        self.by_rule: dict[str, list[int]] = collections.defaultdict(list)
        self.no_frame: dict[str, str] = {}
        for row, fmts in data["rows"].items():
            frames = frames_of(row, subjects[row], rules)
            if frames is None:
                self.no_frame[row] = NO_FRAME[row.split("-FMT-", 1)[1]]
            for f in fmts:
                self.formats.append((row, f["format"], formats.from_json(f["tree"]), f["points"]))
                for r in frames or []:
                    self.by_rule[r].append(len(self.formats) - 1)
        self.nodes_of = [points.number(t) for _, _, t, _ in self.formats]
        self.frames = sorted(self.by_rule)
        self.keywords = frozenset(n.text for nodes in self.nodes_of for n in nodes if n.kind in (formats.WORD, formats.OPTIONAL))

    @staticmethod
    def without_context(tree: formats.Node) -> formats.Node | None:
        """The format without the words the printed page sets before a clause to show where it stands (`level-number data-name-1
        REDEFINES data-name-2`, `66 data-name-1 RENAMES ...`): the leading items up to the first required word that is no
        number. None when the format has none to drop."""
        if tree.kind != formats.SEQ:
            return None
        for cut, kid in enumerate(tree.kids):
            if any(n.kind == formats.WORD and not n.text.isdigit() for n in points.number(kid)):
                return formats.Node(formats.SEQ, kids=tree.kids[cut:]) if cut else None
        return None

    def credit(self, rule: str, tokens: list[Token]) -> set[str]:
        """The points of every format of `rule`'s rows that derive the node's whole text: as it stands, or without the entries
        nested at its end (a file description's record descriptions follow the format the page prints), and with or without the
        context words before a clause."""
        variants = [tokens]
        entry = TRAILING_ENTRY.get(rule)
        if entry is not None:
            n = len(tokens)
            while n > 0 and tokens[n - 1] == (entry, "ph"):
                n -= 1
            if n < len(tokens):
                variants.append(tokens[:n])
        out: set[str] = set()
        for k in self.by_rule.get(rule, ()):
            row, number, tree, pts = self.formats[k]
            ids = {id(n): idx for idx, n in enumerate(self.nodes_of[k])}
            for toks in variants:
                for root in (tree, self.without_context(tree)):
                    if root is None:
                        continue
                    m = Matcher(root, toks)
                    if not m.credit():
                        continue
                    got = {(ids[i], e) for i, e in m.events if i in ids}
                    if root is not tree:
                        got.add((0, "format"))
                    out |= {p["id"] for p in pts if (p["node"], p["event"]) in got}
                    break
        return out


def numbered(data: dict) -> list[str]:
    """The catalog's point ids in catalog order (rows, formats, points): a point's number is its place in this list."""
    return [p["id"] for fmts in data["rows"].values() for f in fmts for p in f["points"]]


def encode(numbers: list[int]) -> str:
    """Point numbers as ranges, `3-17,21`: a test credits hundreds of points, and a record of ids would weigh megabytes."""
    out, run = [], []
    for n in sorted(set(numbers)):
        if run and n == run[-1] + 1:
            run.append(n)
            continue
        if run:
            out.append(f"{run[0]}-{run[-1]}" if len(run) > 1 else str(run[0]))
        run = [n]
    if run:
        out.append(f"{run[0]}-{run[-1]}" if len(run) > 1 else str(run[0]))
    return ",".join(out)


def decode(text: str) -> list[int]:
    out: list[int] = []
    for part in filter(None, text.split(",")):
        lo, _, hi = part.partition("-")
        out.extend(range(int(lo), int(hi or lo) + 1))
    return out


def read_record(edition: str = "85", directory: Path = POINTS_DIR, ids: list[str] | None = None) -> dict[str, set[str]]:
    """test id -> the points the record credits it at `edition`, one file per test (`wiseowl/points/<test id>.json`,
    `{"85": "3-17,21"}`: the catalog's point numbers), read through the generator's one reader of such a registry. `ids` is
    `numbered(catalog)`; a number past its end is a record of another catalog."""
    import generate
    entries, problems = generate.load_sidecars(directory)
    if problems:
        raise RuntimeError(f"wiseowl/points/ is malformed: {problems[0]}")
    ids = numbered(points.load()) if ids is None else ids
    out: dict[str, set[str]] = {}
    for test, by_edition in entries.items():
        if not isinstance(by_edition, dict) or not all(isinstance(v, str) for v in by_edition.values()):
            raise RuntimeError(f"wiseowl/points/{test}.json is not {{edition: \"point numbers\"}}")
        if edition in by_edition:
            try:
                out[test] = {ids[n] for n in decode(by_edition[edition])}
            except (ValueError, IndexError):
                raise RuntimeError(f"wiseowl/points/{test}.json names point numbers the catalog lacks (coverage.py --record)") from None
    return out


def accepted(edition: str) -> list:
    import obligations
    _, cases, _inputs = obligations.facts_of_repository(edition)
    return [c for c in cases if c.tree == "cases" and edition in c.accept]


def inputs_of(cat: "Catalog", catalog_digest: str, tests: list) -> dict:
    """What the record was measured from: the catalog, the way the parse is read (frames, operand rules), and each test's text.
    `unmet` compares it, so a catalog, a reading or a test that changed since the record fails loudly instead of being counted."""
    config = hashlib.sha256(json.dumps([OPERAND_RULES, FLAT_FRAMES, cat.frames, sorted(TRAILING_ENTRY.items())]).encode()).hexdigest()[:12]
    return {"catalog": catalog_digest, "reading": config,
            "tests": {c.test_id: hashlib.sha256((SUITE / c.path).read_bytes()).hexdigest()[:12] for c in sorted(tests, key=lambda c: c.test_id)}}


def unmet(edition: str) -> int:
    """The points no recorded test credits: the `points` term of `suite_status.py`. Raises when the record is not a measure of
    the tests, the catalog and the reading as they stand (`inputs_of`): a test accepted at `edition` with no record, a record
    of a test that changed, or of an older catalog or reading, or naming a point the catalog lacks."""
    data = points.load()
    ids = points.point_ids(data)
    tests = accepted(edition)
    record = read_record(edition)
    if (missing := sorted({c.test_id for c in tests} - set(record))):
        raise RuntimeError(f"{len(missing)} test(s) accepted at {edition} have no points record (coverage.py --record): {missing[:3]}")
    if not INPUTS.is_file():
        raise RuntimeError("wiseowl/points-inputs.json is missing (coverage.py --record)")
    want = inputs_of(Catalog(data), points.digest(data), tests)
    have = json.loads(INPUTS.read_text(encoding="utf-8"))
    if (have["catalog"], have["reading"]) != (want["catalog"], want["reading"]):
        raise RuntimeError("the points record was measured from another catalog or another reading of the parse (coverage.py --record)")
    if (changed := sorted(t for t, d in want["tests"].items() if have["tests"].get(t) != d)):
        raise RuntimeError(f"{len(changed)} test(s) changed since their points were recorded (coverage.py --record): {changed[:3]}")
    if (stale := sorted({p for c in tests for p in record[c.test_id]} - ids)):
        raise RuntimeError(f"the points record names {len(stale)} point(s) the catalog lacks (coverage.py --record): {stale[:3]}")
    return len(ids - {p for c in tests for p in record[c.test_id]})


def cobol_dll(given: str | None) -> Path:
    found = [Path(given)] if given else sorted((REPO / "src" / "WiseOwl.Cobol.Cli" / "bin" / "Debug").glob("*/cobol.dll"))
    if not found or not found[0].is_file():
        raise RuntimeError("no built cobol.dll: build WiseOwl.Cobol.sln, or pass --cobol")
    return found[0]


def read_parses(dll: Path, tests: list, frames: list[str], edition: str):
    """`cobol points-batch` over `tests`: yields (case, parsed, nodes) in order. The manifest and the output are written
    outside every git tree and removed after."""
    import tempfile
    with tempfile.TemporaryDirectory(prefix="points-") as tmp:
        manifest, out = Path(tmp) / "manifest.tsv", Path(tmp) / "out.jsonl"
        manifest.write_text("".join(f"{SUITE / c.path}\t{edition}\t{c.source_format}\n" for c in tests), encoding="utf-8")
        with out.open("wb") as fh:
            run = subprocess.run(["dotnet", str(dll), "points-batch", str(manifest), "--frames", ",".join(frames),
                                  "--operands", ",".join(OPERAND_RULES), "--flat", ",".join(FLAT_FRAMES)], stdout=fh, stderr=subprocess.PIPE)
        if run.returncode != 0:
            raise RuntimeError(f"cobol points-batch exited {run.returncode}: {run.stderr.decode(errors='replace')[:400]}")
        lines = out.read_text(encoding="utf-8").splitlines()
    if len(lines) != len(tests):
        raise RuntimeError(f"cobol points-batch printed {len(lines)} line(s) for {len(tests)} test(s)")
    for c, line in zip(tests, lines):
        entry = json.loads(line)
        yield c, entry["parsed"], entry["nodes"]


def record(edition: str, dll: Path) -> int:
    cat = Catalog(points.load())
    tests = accepted(edition)
    credited: dict[str, set[str]] = {}
    cache: dict[tuple, set[str]] = {}
    refused = []
    for c, parsed, nodes in read_parses(dll, tests, cat.frames, edition):
        if not parsed:
            refused.append(c.test_id)
            continue
        got: set[str] = set()
        for n in nodes:
            toks = tokens_of(n["tokens"], cat.keywords)
            key = (n["rule"], tuple(toks))
            if key not in cache:
                cache[key] = cat.credit(n["rule"], toks)
            got |= cache[key]
        credited[c.test_id] = got
    if refused:
        print(f"=== POINTS {edition}: ERROR — {len(refused)} test(s) accepted at {edition} did not parse: {refused[:5]} ===")
        return 2
    POINTS_DIR.mkdir(parents=True, exist_ok=True)
    index = {pid: n for n, pid in enumerate(numbered(points.load()))}
    for test, ids in credited.items():
        (POINTS_DIR / f"{test}.json").write_text(json.dumps({edition: encode([index[i] for i in ids])}) + "\n", encoding="utf-8", newline="\n")
    for old in sorted(POINTS_DIR.glob("*.json")):
        if old.stem not in credited:
            old.unlink()                                   # a test that no longer accepts the edition keeps no record
    INPUTS.write_text(json.dumps(inputs_of(cat, points.digest(points.load()), tests), indent=1) + "\n", encoding="utf-8", newline="\n")
    print(f"recorded {len(credited)} test(s), {len(cache)} distinct node shapes")
    return 0


def report(edition: str) -> int:
    data = points.load()
    ids = points.point_ids(data)
    tests = {c.test_id for c in accepted(edition)}
    rec = read_record(edition)
    got = {p for t in tests for p in rec.get(t, ())}
    cat = Catalog(data)
    per_row: dict[str, list[int]] = {}
    for row, fmts in data["rows"].items():
        mine = {p["id"] for f in fmts for p in f["points"]}
        per_row[row] = [len(mine), len(mine & got)]
    for row, (n, c) in per_row.items():
        why = f"  [{cat.no_frame[row]}]" if row in cat.no_frame else ""
        print(f"{row:<28} {c:>4} of {n:<4} credited{why}")
    print(f"=== POINTS {edition}: {len(ids)} points in {len(per_row)} rows · {len(got & ids)} credited · {len(ids - got)} owed ===")
    return 0


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--edition", required=True, choices=["85"])
    ap.add_argument("--record", action="store_true", help="parse every test accepted at the edition and write wiseowl/points/")
    ap.add_argument("--cobol", help="the built cobol.dll (default: src/WiseOwl.Cobol.Cli/bin/Debug/*/cobol.dll)")
    args = ap.parse_args(argv)
    try:
        if args.record:
            return record(args.edition, cobol_dll(args.cobol))
        return report(args.edition)
    except (RuntimeError, points.CatalogError) as e:
        print(f"=== POINTS {args.edition}: ERROR — {e} ===")
        return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
