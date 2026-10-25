#!/usr/bin/env python3
"""Cross-reference MEASURED underlining against the grammar's keyword spelling, to find optional words we require.

THE DEFECT CLASS. ISO §5.2.3: an uppercase word printed WITHOUT an underline is an OPTIONAL WORD — it may be
written or omitted with no change of meaning. §5.2.2: an underlined one is REQUIRED. That distinction is carried
ENTIRELY by typography, it is the first thing a transcription loses, and a grammar written from a transcription
that lost it will reject legal COBOL.

It has already produced real compiler bugs: `ON` in SIZE ERROR and ON EXCEPTION (DEVLOG 1041), `FROM`, `AFTER`,
`SECONDS` and `TO` in the MCS statements (DEVLOG 1042), `ON` in READ … ADVANCING ON LOCK (kb/Work PB331), and
`WITH` in START … WITH LENGTH plus `AFTER`/`PROCEDURE` in USE (kb/Work PB332). Each was found by hand. This
automates the search.

HOW. For every general format PRINTED IN THE STANDARD — all of them, not the statements alone:

  * the transcription declares the population: every `N.N…  General format[s]` heading in `specs/ISO_COBOL.md`;
  * the PAGE and the EXTENT come from `clause_page.index()`, the one clause→page locator — the format runs from
    its own heading to the NEXT clause heading, across a page break when it takes one;
  * the words in that band are measured — underlined or not — from the PDF's vector rectangles;
  * the grammar rules that spell the construct are its CLOSURE: the rules naming the construct's leading keyword,
    plus every rule reachable from them that NOTHING ELSE references. That closure is what makes helper rules
    visible — `readAdvancingOnLock` never mentions READ, and a keyword-only scan could never have found PB331;
  * a word printed WITHOUT an underline that a rule DEMANDS is reported — demanded by an alternative of the rule,
    or by a `( … )` group of it once the group is written (`examine`, `demanded`).

⛔ A GROUP IS A SPELLING OF ITS OWN, NOT AN EXCUSE (kb/Work PB715). Until 2026-10-25 every token inside an
optional group counted as omittable, so `(FOR (ALPHANUMERIC | NATIONAL))?` hid a demanded FOR and the six optional
words PB695 relaxed by hand (CURRENCY's WITH, CLASS's and SYMBOLIC's FOR, the switches' STATUS, IS, the collating
FOR) all ran clean. And a construct whose keyword is itself optional (`[ USAGE IS ]`), or whose own rule carried
one arm its format no longer prints (USE FOR DEBUGGING), anchored NO rule and was reported clean unexamined; such a
construct now FAILS the run (`examine`'s anchoring, the population assertion).

⛔ IT ASSERTS ITS OWN POPULATION, and that assertion is the point. Until 2026-09-05 this tool located its pages
from the markdown's `#page-N` anchors; DE-PAGING DELETED THE ANCHORS, so it located 0 general formats, scanned
0 pages, printed `0 candidate(s)` and exited 0 — a silently green gate over the whole grammar while READ, START
and USE each rejected legal source (kb/Work PB332). Every count below is now checked against the population it
claims to have measured, and a shortfall is a NON-ZERO EXIT, never a clean zero.

WHAT IT CANNOT DECIDE, and deliberately does not pretend to. A word may be required in one format and optional
in another — `TO` is underlined in `ADD … TO` and bare in `SEND … TO`, and `KEY` is underlined in START's KEY
phrase and bare in its INVALID KEY phrase. This tool matches at CONSTRUCT granularity and drops any word that is
underlined ANYWHERE in the construct's formats, so it under-reports rather than over-reports, and what it does
report still needs a human. It reports CANDIDATES, not findings.

⚠ CONFIRM EVERY CANDIDATE AGAINST THE RAW RECTANGLES BEFORE CHANGING THE GRAMMAR. Two-letter words are the
weak case for underline detection — a 9 pt width floor once made this toolchain accuse two CORRECT pages
(`IN` p322, `TO` p673), and acting on that would have damaged the reference document. `figure_extract.py`'s
docstring records the fix; the discipline stands regardless.

    python scripts/spec/audit_grammar_optional_words.py
    python scripts/spec/audit_grammar_optional_words.py --construct USE
    python scripts/spec/audit_grammar_optional_words.py --clause 14.9.41.2 --show-format
    python scripts/spec/audit_grammar_optional_words.py --self-test     # fixture grammars; needs no PDF
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import re
import sys
from typing import Callable, NamedTuple

# The grammar is SPLIT ACROSS TWELVE FILES — CobolParserCore.g4 plus Grammar/Core/*.g4. An earlier version of this
# script read only the root file and therefore scanned a fraction of the rules, reporting a reassuring "3
# candidates" that was an artifact of not looking. `grammar_source` is THE reader of all of them (kb/Work PB3204):
# it excludes the build-time COPIES under obj/, blanks comments and actions, and refuses an empty population.
import grammar_source
from spec_headings import GENERAL_FORMAT_TITLE   # THE one spelling of the heading (kb/Work PB284)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
SPEC_MD = REPO / "specs" / "ISO_COBOL.md"
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)

#: The printed body band, borrowed from `render_figure` so the running header and the folio/licence footer can
#: never enter a format's measured band when the format spills across a page break.
BODY_TOP, BODY_BOTTOM = 60.0, 730.0

RESERVED = re.compile(r"^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$")
# Words that are structural in the grammar rather than COBOL keywords, or that carry their own rules.
SKIP = {"ISO", "IEC", "NOTE", "FORMAT", "FORMATS", "GENERAL", "TRUE", "FALSE", "ALL"}

#: A markdown clause heading: `##### 14.9.49.2 General formats`.
MD_HEADING = re.compile(r"^#{2,6}\s+(\d+(?:\.\d+)*)\s+(\S.*?)\s*$")
#: The sub-clause that carries the printed diagram(s). Singular in most clauses, plural where there are several.
#: A construct's ANCHOR KEYWORD is the leading all-caps COBOL word of its clause title — `USE statement`,
#: `OCCURS clause`, `>>DEFINE directive`, `ABS function`. A title that does not start with one (`Qualification`,
#: `Report description entry`) has no anchor; those formats are counted and named, never silently dropped.
ANCHOR = re.compile(r"^>{0,2}([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*)\b")


class Format(NamedTuple):
    """One printed general format, as the TRANSCRIPTION declares it."""
    clause: str                 # the general-format sub-clause, e.g. "14.9.49.2"
    parent: str                 # the construct's clause, e.g. "14.9.49"
    title: str                  # the construct's clause title, e.g. "USE statement"
    keyword: str | None         # its anchor keyword, e.g. "USE" — None for a prose-titled construct


def declared_formats(md_text: str) -> list[Format]:
    """Every general format the transcription declares, in document order — this tool's expected population.

    A general format is a LEAF clause. §5.2 is titled "General formats" too, but it is the clause that DEFINES
    the notation (§5.2.1 General … §5.2.10 Meta-terms live under it) and prints no diagram of its own; the
    has-no-sub-clauses test excludes it structurally, which is one clause out of 321 and needs no hand list.
    """
    titles: dict[str, str] = {}
    order: list[str] = []
    for line in md_text.splitlines():
        m = MD_HEADING.match(line)
        if m:
            titles.setdefault(m.group(1), m.group(2))
            order.append(m.group(1))
    out: list[Format] = []
    seen: set[str] = set()
    for clause in order:
        if clause in seen or not GENERAL_FORMAT_TITLE.fullmatch(titles.get(clause, "")):
            continue
        seen.add(clause)
        if any(c.startswith(clause + ".") for c in titles):
            continue                                # a container clause, not a printed diagram — see §5.2
        parent = ".".join(clause.split(".")[:-1])
        title = titles.get(parent, "")
        a = ANCHOR.match(title)
        out.append(Format(clause, parent, title, a.group(1) if a else None))
    return out


def parser_rules() -> dict[str, grammar_source.Rule]:
    """Rule name -> the parser rule, read by `grammar_source` (comments and `{…}` actions blanked, literals intact).

    A comment is prose ABOUT the rule, so a keyword written in one is not a token the parser requires; an ACTION
    or SEMANTIC PREDICATE is target-language code and can never hold a token reference —
    `{facilityWord("RECEIVE")}?` in the `statement` dispatch once made that 200-alternative rule look like the
    RECEIVE statement's own spelling and dragged 233 rules into its closure. A name declared in two files keeps
    the later file's rule, in `grammar_source.grammar_files` order.
    """
    return {r.name: r for r in grammar_source.read() if r.kind == "parser"}


#: What is left of a lexer body once its quoted literals, its `->` action, and its punctuation are removed. A
#: KEYWORD token's body is nothing but literals; a LEXICAL CLASS names fragments, character sets or predicates.
_LEX_LITERAL = re.compile(r"'((?:[^'\\]|\\.)*)'")


def keyword_spellings() -> dict[str, set[str]]:
    """Lexer token -> the COBOL words it spells, for tokens that spell WORDS and nothing else.

    ⛔ THIS IS THE LINE BETWEEN A COBOL WORD AND A LEXICAL CLASS, and it is drawn STRUCTURALLY — a token whose
    rule is one or more quoted literals (`RESERVE : 'RESERVE' ;`, `ZERO : 'ZERO' | 'ZEROS' | 'ZEROES' ;`) spells
    words that the standard can print; one whose rule names fragments, character sets or predicates (IDENTIFIER,
    INTEGERLIT, PIC_STRING) spells a class of user text that appears on no printed page as itself.

    The distinction is what makes `spells_this_construct` safe. That test asks whether a rule demands a word the
    construct's printed format never prints, and `decimalPointClause : DECIMAL_POINT IS? IDENTIFIER` demands
    IDENTIFIER — which is printed nowhere in the standard, in any format. Without this split the test would
    disqualify every rule with an operand token and the audit would report nothing at all, silently: precisely
    the failure mode the population assertion below exists to prevent.

    ⛔ A LITERAL IS ONLY A WORD WHEN IT IS SHAPED LIKE ONE. `DOT : '.' ;` is literal-only too, and admitting it
    cost the whole SPECIAL-NAMES and OBJECT-COMPUTER closures on the first cut of this function: every paragraph
    rule spells `… DOT`, no printed format's word list contains a bare period, so `spells_this_construct`
    disqualified the ANCHOR and the audit reported ZERO candidates for both paragraphs while nine of their
    clauses demanded an optional word. The old-grammar control run is what caught it — a filter is only ever
    evidence about what it RETURNED (feedback_measure_the_selectors_complement).
    """
    out: dict[str, set[str]] = {}
    for rule in grammar_source.read():
        if rule.kind != "lexer":                                         # a fragment is never a token of its own
            continue
        body = re.sub(r"->.*$", "", rule.rhs, flags=re.S)               # a lexer command, not part of the match
        lits = [w for w in _LEX_LITERAL.findall(body) if RESERVED.match(w.upper())]
        if not lits:
            continue
        if re.sub(r"'(?:[^'\\]|\\.)*'|[\s|()?*+]", "", body):           # anything but literals and alternation
            continue
        out.setdefault(rule.name, set()).update(w.upper().replace("-", "_") for w in lits)
    return out


class Elem(NamedTuple):
    """One element of a parser-rule alternative, read from its STRUCTURE rather than from the character after it."""
    kind: str                       # "tok" (an UPPERCASE token), "ref" (a parser rule) or "group" (a `( … )`)
    name: str                       # the token or rule name; a group's normalized source text, suffix included
    suffix: str                     # "", "?", "*" or "+"
    alts: tuple = ()                # a group's alternatives, each a tuple of Elem

    @property
    def optional(self) -> bool:
        return self.suffix in ("?", "*")


_IDENT = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
_UPPER_TOKEN = re.compile(r"[A-Z][A-Z0-9_]*")


def parse_body(rhs: str) -> tuple[tuple[Elem, ...], ...]:
    """One rule's blanked right-hand side (the text between `:` and `;`) as its tree of alternatives.

    `grammar_source` has already blanked comments, actions and predicates. What remains that is not a reference
    is skipped STRUCTURALLY: a quoted literal, an element label (`leftmost=`), an alternative label (`# Name`), an
    `<option>`, and a NEGATED set (`~DOT+` matches every token but DOT, so DOT is no word the rule demands).
    """
    pos = 0
    n = len(rhs)

    def suffix() -> str:
        nonlocal pos
        s = ""
        if pos < n and rhs[pos] in "?*+":
            s = rhs[pos]
            pos += 1
            if pos < n and rhs[pos] == "?":             # a non-greedy `??` / `*?` / `+?` is the same quantity
                pos += 1
        return s

    def alternatives_until(close: str | None) -> tuple[tuple[Elem, ...], ...]:
        nonlocal pos
        alts: list[tuple[Elem, ...]] = []
        cur: list[Elem] = []
        while pos < n:
            c = rhs[pos]
            if c == ")" and close == ")":
                pos += 1
                break
            if c == "|":
                alts.append(tuple(cur))
                cur = []
                pos += 1
            elif c == "(":
                start = pos
                pos += 1
                inner = alternatives_until(")")
                sfx = suffix()
                cur.append(Elem("group", " ".join(rhs[start:pos].split()), sfx, inner))
            elif c == "'":
                pos += 1
                while pos < n and rhs[pos] != "'":
                    pos += 2 if rhs[pos] == "\\" else 1
                pos += 1
                suffix()
            elif c == "<":
                pos = rhs.find(">", pos) + 1 or n
            elif c == "#":
                m = re.compile(r"#\s*[A-Za-z_][A-Za-z0-9_]*").match(rhs, pos)
                pos = m.end() if m else pos + 1
            elif c == "~":
                pos += 1
                while pos < n and rhs[pos].isspace():
                    pos += 1
                if pos < n and rhs[pos] == "(":
                    pos += 1
                    alternatives_until(")")
                else:
                    m = _IDENT.match(rhs, pos)
                    pos = m.end() if m else pos + 1
                suffix()
            elif (m := _IDENT.match(rhs, pos)) is not None:
                pos = m.end()
                label = re.match(r"\s*\+?=", rhs[pos:])
                if label:                                   # `name=` / `name+=` labels the NEXT element
                    pos += label.end()
                    continue
                word = m.group(0)
                sfx = suffix()
                if _UPPER_TOKEN.fullmatch(word):
                    cur.append(Elem("tok", word, sfx))
                elif word[0].islower():
                    cur.append(Elem("ref", word, sfx))
            else:
                pos += 1
        alts.append(tuple(cur))
        return tuple(alts)

    return alternatives_until(None)


def _tokens(alt: tuple[Elem, ...]) -> list[str]:
    """Every token the alternative names, optional or not, nested groups included, in order."""
    out: list[str] = []
    for e in alt:
        if e.kind == "tok":
            out.append(e.name)
        elif e.kind == "group":
            for inner in e.alts:
                out += _tokens(inner)
    return out


def _refs(alt: tuple[Elem, ...]) -> list[str]:
    """Every parser rule the alternative references, nested groups included."""
    out: list[str] = []
    for e in alt:
        if e.kind == "ref":
            out.append(e.name)
        elif e.kind == "group":
            for inner in e.alts:
                out += _refs(inner)
    return out


def _mandatory(alt: tuple[Elem, ...]) -> tuple[str, ...]:
    """The token SEQUENCE every use of this alternative writes: optional tokens and optional groups drop out, and
    a required group contributes every one of its alternatives' mandatory tokens (a choice among required words
    demands each of them — `(IS | ARE)` written bare is a demand of whichever is chosen)."""
    out: list[str] = []
    for e in alt:
        if e.optional:
            continue
        if e.kind == "tok":
            out.append(e.name)
        elif e.kind == "group":
            for inner in e.alts:
                out += _mandatory(inner)
    return tuple(out)


def demanded(alts: tuple[tuple[Elem, ...], ...], omittable: bool) -> list[frozenset[str]]:
    """Per alternative of ONE spelling unit (a rule body, or a group's body): the tokens it DEMANDS once present.

    ⛔ THIS IS THE ONE TEST, applied at every nesting level (kb/Work PB715). §8.3.2.4.3 asks of each word whether it
    may be omitted WHILE ITS PHRASE IS PRESENT, so a group is judged as a unit of its own and never written off:
    `(FOR (ALPHANUMERIC | NATIONAL))?` may be omitted whole, but once written it demands FOR, and the audit that
    marked every token of an optional group omittable could never report that FOR.

    A word is NOT demanded by an alternative when:

    * the alternative is a NAME alternative — a single token in a unit that has several, with no rule reference:
      `cobolWord`'s `| RECEIVE |` says RECEIVE may be a user-defined word, and `readDirection`'s `NEXT | PREVIOUS`
      is a printed choice where one arm is not underlined. Neither is a word the parser demands;
    * deleting it leaves a sequence a SIBLING alternative spells EXACTLY. `performLocationPhrase : WITH LOCATION |
      {is2023()}? LOCATION` writes the two spellings of one optional word as two arms, so WITH is mandatory in the
      first and the unit still accepts `LOCATION` (kb/Work PB695). ⚠ The comparison is on the ORDERED SEQUENCE,
      never on the required SET: with sets, `switchOnClause : ON STATUS IS cobolWord | ON IS? cobolWord` would
      subsume its own IS and hide a real rejection of `ON STATUS condition-name-1`;
    * the unit is OMITTABLE (an optional group, or a rule referenced only with `?`/`*`) and the alternative IS the
      word, alone: `(TIMES)?`, or `timesKeyword : TIMES ;` used as `timesKeyword?`, already permits `OCCURS 5` —
      the token is the optional unit. Contrast `startWithLength : WITH LENGTH …` used as `(startWithLength)?`:
      omitting the whole PHRASE is legal, writing it without WITH is not, so WITH stays demanded.
    """
    seqs = [_mandatory(a) for a in alts]
    out: list[frozenset[str]] = []
    for i, alt in enumerate(alts):
        name_alternative = len(alts) > 1 and len(_tokens(alt)) == 1 and not _refs(alt)
        the_word_alone = omittable and len(alt) == 1 and alt[0].kind == "tok"
        if name_alternative or the_word_alone:
            out.append(frozenset())
            continue
        seq = seqs[i]
        out.append(frozenset(w for k, w in enumerate(seq)
                             if not any(other == seq[:k] + seq[k + 1:] for j, other in enumerate(seqs) if j != i)))
    return out


class Unit(NamedTuple):
    """A group inside an alternative, judged as a spelling of its own once it is present (kb/Work PB715)."""
    text: str                       # the group's normalized source, e.g. `(FOR (ALPHANUMERIC | NATIONAL))?`
    demands: frozenset[str]         # the tokens it demands once written


class Spelling(NamedTuple):
    """One top-level alternative of a parser rule: ONE way of writing the construct."""
    own: frozenset[str]             # the tokens every use of this alternative demands
    groups: tuple[Unit, ...]        # every group inside it, at any depth, with what it demands once present
    mentions: frozenset[str]        # every token it names, optional or not


def _groups(alt: tuple[Elem, ...]) -> list[Unit]:
    """Every group inside an alternative, at any depth, each judged by `demanded` as a unit of its own."""
    out: list[Unit] = []
    for e in alt:
        if e.kind != "group":
            continue
        out.append(Unit(e.name, frozenset().union(*demanded(e.alts, e.optional))))
        for inner in e.alts:
            out += _groups(inner)
    return out


class GrammarModel(NamedTuple):
    """The parser grammar as the audit reads it: each rule's spellings, references and file of origin."""
    spellings: dict[str, tuple[Spelling, ...]]
    refs: dict[str, frozenset[str]]
    origin: dict[str, str]

    def demands(self, rule: str) -> frozenset[str]:
        """Every token the rule demands of every use of one of its alternatives (the union over them)."""
        return frozenset().union(*(s.own for s in self.spellings[rule]))

    @property
    def tokens(self) -> frozenset[str]:
        """Every token some parser rule names."""
        return frozenset().union(*(s.mentions for sps in self.spellings.values() for s in sps))


def model(trees: dict[str, tuple[tuple[Elem, ...], ...]], origin: dict[str, str]) -> GrammarModel:
    """Build the audit's grammar model from each rule's parsed alternatives (`parse_body`).

    A rule referenced ONLY with a `?`/`*` suffix is an omittable unit, exactly as an optional group is: that is
    the one fact the two levels share, and it is why `demanded` takes `omittable` rather than knowing about rules.
    """
    refs = {n: frozenset(r for alt in alts for r in _refs(alt) if r in trees and r != n) for n, alts in trees.items()}
    optional_only: dict[str, bool] = {}

    def note_refs(alt: tuple[Elem, ...], owner: str) -> None:
        for e in alt:
            if e.kind == "ref" and e.name in trees and e.name != owner:
                optional_only[e.name] = optional_only.get(e.name, True) and e.optional
            elif e.kind == "group":
                for inner in e.alts:
                    note_refs(inner, owner)

    for owner, alts in trees.items():
        for alt in alts:
            note_refs(alt, owner)
    spellings = {
        name: tuple(Spelling(own, tuple(_groups(alt)), frozenset(_tokens(alt)))
                    for alt, own in zip(alts, demanded(alts, optional_only.get(name, False))))
        for name, alts in trees.items()}
    return GrammarModel(spellings, refs, origin)


def closure(refs: dict[str, frozenset[str]], anchors: set[str]) -> set[str]:
    """The rules that spell ONE construct: its anchor rules plus every rule EXCLUSIVELY reachable from them.

    ⛔ THIS IS WHY HELPER RULES ARE VISIBLE AT ALL. `readAdvancingOnLock` does not contain the word READ and
    `startWithLength` does not contain START, so a scan of "rules mentioning the keyword" can never see the very
    rules that carry a statement's optional words — PB331's `ADVANCING ON LOCK` lived in one of them.

    "Exclusively reachable" is a STRUCTURAL test, not a hand-maintained exclusion list: a rule joins the closure
    only when EVERY rule that references it is already in the closure. `useOnTarget` (referenced by
    `useStatement` alone) joins; `fileName`, `dataReference`, `statementBlock` and `retryPhrase` — referenced
    from all over the grammar — never do, because they are not this construct's private spelling.
    """
    referrers: dict[str, set[str]] = collections.defaultdict(set)
    for n, rs in refs.items():
        for r in rs:
            referrers[r].add(n)
    inside = set(anchors)
    changed = True
    while changed:
        changed = False
        for n in sorted(inside):
            for r in sorted(refs.get(n, ())):
                if r not in inside and referrers[r] <= inside:
                    inside.add(r)
                    changed = True
    return inside


class Candidate(NamedTuple):
    """A word printed WITHOUT an underline that a rule spelling the construct demands."""
    word: str
    rule: str
    group: str                      # "" when the alternative itself demands it, else the group that does


class Examination(NamedTuple):
    """How one construct was anchored in the grammar, and what that found."""
    anchoring: str                  # one of ANCHORINGS, or "" when no rule spells the construct (UNEXAMINED)
    anchors: frozenset[str]
    candidates: tuple[Candidate, ...]


#: How a construct's rules are found, tried in this order; the first that anchors any rule is the one used.
#: "named": the rules with an alternative that names the anchor keyword, demanded or optional, and spells the
#: format — `usageClause : (USAGE IS?)? usageKeyword …` writes USAGE only inside an optional group and
#: `fileStatusClause : FILE? STATUS …` only as an optional token, and each is still its construct's own rule.
#: "enclosing": the same test, judged against this format plus every format that underlines the keyword — a
#: paragraph printed INSIDE a definition (§11.6.2 INTERFACE-ID within §10.6.1's interface-definition) is spelled
#: by the definition's rule, which demands END INTERFACE from the enclosing format.
ANCHORINGS = ("named", "enclosing")


def keyword_tokens(keyword: str, keywords: dict[str, set[str]], token_names: set[str]) -> frozenset[str]:
    """The lexer tokens that spell a construct's anchor keyword: the token of that NAME, and every token whose
    literals SPELL it — §13.18.40's PICTURE is the token PIC (`'PIC' | 'PICTURE'`), named after neither."""
    word = keyword.replace("-", "_")
    return frozenset({t for t, sp in keywords.items() if word in sp} | ({word} & token_names))


def examine(gm: GrammarModel, keywords: dict[str, set[str]], keyword: str, plain: set[str], printed: set[str],
            enclosing: Callable[[], set[str]]) -> Examination:
    """Anchor one construct in the grammar and report every demanded word its format prints without an underline.

    `printed` holds every word the format prints (normalized `-` → `_`), `plain` the ones it never underlines,
    and `enclosing()` the words of every format that underlines the keyword (measured only when needed).

    ⛔ THE CLOSURE MATCHES ON ONE TOKEN, AND ONE TOKEN IS NOT A CONSTRUCT. The anchor set is "every rule
    demanding the clause title's leading word", so the FILE STATUS clause (§12.4.5.8.2, whose `[FILE]` is
    un-underlined) drew in `deleteFileStatement`, `fileSection`, `multipleFileClause`, `readStatement`,
    `rewriteStatement`, `writeStatement` and `valueOfClause` — seven rules that merely also demand a FILE, and all
    seven were reported against a page none of them is printed on. So a spelling counts only when every COBOL WORD
    it demands is printed in this construct's own format (`deleteFileStatement` demands DELETE, which the FILE
    STATUS clause never prints; `channelClause` demands CHANNEL, which the standard never prints). Operand tokens
    (IDENTIFIER, INTEGERLIT …) are exempt by construction — see `keyword_spellings`.

    ⛔ THE TEST IS PER ALTERNATIVE, NOT PER RULE (kb/Work PB715). An alternative is one way of writing the
    construct, and a rule is several: `useStatement` carries the USE FOR DEBUGGING arm the 2023 format no longer
    prints, so the whole-rule test disqualified the USE statement's own rule and the audit examined NOTHING of
    §14.9.49.2 while reporting a clean run. The same test judges each group before its words are reported, so an
    extension phrase nobody prints cannot raise a candidate from a word it happens to share.

    ⛔ AN EMPTY ANCHOR SET IS NOT A CLEAN CONSTRUCT. Before PB715 the anchor was "a rule DEMANDING the keyword", so
    a construct whose keyword is itself optional (`[ USAGE IS ] { BINARY | … }`) anchored nothing and reported
    nothing — that is how PB848's TO in the pointer usages went unreported. `ANCHORINGS` is tried in order, and a
    construct none of them anchors comes back with `anchoring` "" for the caller to FAIL on.
    """
    toks = keyword_tokens(keyword, keywords, gm.tokens)
    if not toks:
        return Examination("", frozenset(), ())

    def spells(demands: frozenset[str], words: set[str]) -> bool:
        return all(sp & words for t in demands if (sp := keywords.get(t)))

    def anchor(words: set[str]) -> set[str]:
        return {r for r, sps in gm.spellings.items() if any(toks & s.mentions and spells(s.own, words) for s in sps)}

    words, anchors, anchoring = printed, set(), ""
    for anchoring in ANCHORINGS:
        if anchoring == "enclosing":
            words = printed | enclosing()
        anchors = anchor(words)
        if anchors:
            break
    else:
        return Examination("", frozenset(), ())

    def spelled(t: str) -> set[str]:
        return keywords.get(t) or {t}

    found: dict[tuple[str, str], Candidate] = {}
    for rule in sorted(closure(gm.refs, anchors)):
        for s in gm.spellings[rule]:
            if not spells(s.own, words):
                continue
            units = [("", s.own)] + [(u.text, u.demands) for u in s.groups if spells(u.demands, words)]
            for group, demands in units:
                said = set().union(*(spelled(t) for t in demands))
                for w in sorted(plain):
                    # ⛔ THE CONSTRUCT'S OWN ANCHOR KEYWORD IS NEVER A CANDIDATE. It is the word this format was
                    # LOCATED by, so the anchor rule demands it by construction and reporting it is circular. It
                    # reaches `plain` whenever the printed format leaves the head word un-underlined — folio 604
                    # does exactly that to CONTINUE, whose whole format is `CONTINUE [ AFTER … SECONDS ]` with
                    # rules under AFTER and SECONDS only (kb/Work PB695).
                    if w != keyword and w.replace("-", "_") in said:
                        found.setdefault((rule, w), Candidate(w, rule, group))
    return Examination(anchoring, frozenset(anchors), tuple(found[k] for k in sorted(found)))


def band_words(doc, cache: dict[int, list], start, nxt) -> list[dict]:
    """Every word printed between one clause heading and the next, across a page break if it takes one."""
    from figure_extract import extract

    last = nxt.page if nxt is not None and nxt.page >= start.page else doc.page_count
    words: list[dict] = []
    for page in range(start.page, min(last, doc.page_count) + 1):
        if page not in cache:
            cache[page] = extract(doc[page - 1])
        top = start.y if page == start.page else BODY_TOP
        bottom = nxt.y if (nxt is not None and page == nxt.page) else BODY_BOTTOM
        words += [w for w in cache[page] if top <= w["y0"] <= bottom]
    return words


def measure(words: list[dict]) -> tuple[set[str], set[str]]:
    """(never-underlined words, underlined-somewhere words) over one construct's printed formats."""
    by_line: dict[int, list[dict]] = collections.defaultdict(list)
    for w in words:
        by_line[round(w["y0"] / 3)].append(w)
    plain: set[str] = set()
    underlined: set[str] = set()
    for row in by_line.values():
        text = " ".join(w["text"] for w in row)
        if re.search(r"ISO/IEC\s*1989", text):
            continue
        # A row carrying an all-lowercase word is prose or a metavariable line, not a diagram row of keywords.
        if any(re.fullmatch(r"[a-z]{2,}", w["text"].strip(" []{}.,;:()")) for w in row):
            continue
        for w in row:
            t = w["text"].strip(" []{}.,;:()")
            if len(t) > 1 and RESERVED.match(t) and t not in SKIP:
                (underlined if w["underlined"] else plain).add(t)
    # A word underlined ANYWHERE in this construct is required in one of its formats; only words that are never
    # underlined here are unambiguous optional-word candidates.
    return plain - underlined, underlined


def _fixture(rules: str) -> GrammarModel:
    """A grammar model from fixture rule text, `name : body ;` per rule, with no grammar file behind it."""
    trees: dict[str, tuple[tuple[Elem, ...], ...]] = {}
    for m in re.finditer(r"([a-z][A-Za-z0-9_]*)\s*:(.*?);", rules, flags=re.S):
        trees[m.group(1)] = parse_body(m.group(2))
    return model(trees, {n: "Fixture.g4" for n in trees})


#: Fixture keyword spellings: every token below spells its own name, and PIC spells PICTURE too, as the real lexer's
#: `PIC : 'PIC' | 'PICTURE'` does. IDENTIFIER and PIC_STRING are lexical classes, so they spell no word.
_FIXTURE_KEYWORDS = {t: {t} for t in (
    "ALPHANUMERIC ARE BINARY CLASS DEBUGGING DELETE END ERROR FILE FOR IN INHERITS INTERFACE INTERFACE_ID INTO IS "
    "LENGTH READ "
    "LOCATION NATIONAL OCCURS ON PERFORM POINTER PROCEDURE RECEIVE STANDARD START STATUS TIMES USAGE USE "
    "VENDORWORD WITH").split()} | {"PIC": {"PIC", "PICTURE"}}


def self_test() -> int:
    """⛔ A GATE THAT HAS NEVER BEEN SEEN TO FAIL IS NOT EVIDENCE (feedback_prove_the_watchdog_fails). Each arm
    fires the audit on the exact shape it was blind to, and then on the repaired twin, which must be silent. The
    fixtures are grammar text, so this needs neither the private PDF nor the real grammar."""
    ok = True

    def check(name: str, cond: bool, detail: object = "") -> None:
        nonlocal ok
        print(f"  {'ok  ' if cond else 'FAIL'} {name}" + ("" if cond else f"  (got {detail})"))
        ok &= cond

    def run(rules: str, keyword: str, plain: str, printed: str, enclosing: str = "") -> Examination:
        p = set(plain.split())
        return examine(_fixture(rules), _FIXTURE_KEYWORDS, keyword, p, set(printed.split()) | p,
                       lambda: set(enclosing.split()))

    def words(ex: Examination) -> set[tuple[str, str]]:
        return {(c.word, c.group) for c in ex.candidates}

    blind = run("classDefinitionClause : CLASS cobolWord IS? classValueSet (FOR (ALPHANUMERIC | NATIONAL))? "
                "(IN cobolWord)? ; cobolWord : IDENTIFIER ; classValueSet : IDENTIFIER ;",
                "CLASS", "IS FOR", "CLASS ALPHANUMERIC NATIONAL IN")
    check("PB715: a word demanded INSIDE an optional group is a candidate, named with its group",
          words(blind) == {("FOR", "(FOR (ALPHANUMERIC | NATIONAL))?")}, words(blind))
    repaired = run("classDefinitionClause : CLASS cobolWord IS? classValueSet (FOR? (ALPHANUMERIC | NATIONAL))? "
                   "(IN cobolWord)? ; cobolWord : IDENTIFIER ; classValueSet : IDENTIFIER ;",
                   "CLASS", "IS FOR", "CLASS ALPHANUMERIC NATIONAL IN")
    check("…and the repaired `(FOR? …)?` is silent", not repaired.candidates and repaired.anchoring == "named",
          repaired)

    unit = run("occursClause : OCCURS integerLiteral (TIMES)? ; integerLiteral : IDENTIFIER ;", "OCCURS", "TIMES", "")
    check("a one-word optional group IS the optional unit: `(TIMES)?` demands nothing", not unit.candidates, unit)
    rule_unit = run("occursClause : OCCURS integerLiteral timesKeyword? ; timesKeyword : TIMES ; "
                    "integerLiteral : IDENTIFIER ;", "OCCURS", "TIMES", "")
    check("a one-word rule referenced only with `?` is the optional unit too", not rule_unit.candidates, rule_unit)
    phrase = run("startStatement : START fileName startWithLength? ; startWithLength : WITH LENGTH fileName ; "
                 "fileName : IDENTIFIER ;", "START", "WITH", "START LENGTH")
    check("a PHRASE rule referenced with `?` still demands its words (WITH in START … WITH LENGTH)",
          words(phrase) == {("WITH", "")}, words(phrase))
    into = run("readStatement : READ fileName readInto? ; readInto : INTO fileName ; fileName : IDENTIFIER ;",
               "READ", "INTO", "READ")
    check("an optional phrase rule of one word AND an operand demands the word (`readInto : INTO …`)",
          words(into) == {("INTO", "")}, words(into))
    sibling = run("performStatement : PERFORM (WITH LOCATION | LOCATION)? ;", "PERFORM", "WITH", "PERFORM LOCATION")
    check("a sibling arm INSIDE a group subsumes the word (`(WITH LOCATION | LOCATION)?`)",
          not sibling.candidates, sibling)
    seq = demanded(parse_body("ON STATUS IS cobolWord | ON IS? cobolWord"), False)
    check("subsumption compares the ordered SEQUENCE, never the set (`ON STATUS IS … | ON IS? …` keeps IS)",
          seq == [frozenset({"ON", "STATUS", "IS"}), frozenset({"ON"})], seq)
    names = demanded(parse_body("IDENTIFIER | RECEIVE"), False)
    check("a NAME alternative demands nothing (`cobolWord : IDENTIFIER | RECEIVE`)",
          names == [frozenset(), frozenset()], names)
    parsed = parse_body("LPAREN leftmost=arithmeticExpression? COLON RPAREN | ~DOT+ # Label")
    check("parse: an element label and an alternative label are no references, a negated set no demand",
          [_refs(a) for a in parsed] == [["arithmeticExpression"], []]
          and demanded(parsed, False) == [frozenset({"LPAREN", "COLON", "RPAREN"}), frozenset()], parsed)

    usage = run("usageClause : (USAGE IS)? usageKeyword ; usageKeyword : BINARY | POINTER ;",
                "USAGE", "IS", "USAGE BINARY POINTER")
    check("an OPTIONAL anchor keyword still anchors its rule (`(USAGE IS)?`), and the group's IS is reported",
          usage.anchoring == "named" and words(usage) == {("IS", "(USAGE IS)?")}, usage)
    stranger_arm = run("useStatement : USE AFTER? STANDARD ERROR PROCEDURE ON? fileName "
                       "| USE FOR DEBUGGING ON? fileName ; fileName : IDENTIFIER ;",
                       "USE", "AFTER STANDARD PROCEDURE ON", "USE ERROR")
    check("an arm the format does not print (USE FOR DEBUGGING) no longer hides its rule's other arm",
          stranger_arm.anchors == {"useStatement"}
          and words(stranger_arm) == {("STANDARD", ""), ("PROCEDURE", "")}, stranger_arm)
    stranger = run("fileStatusClause : FILE? STATUS IS dataReference ; deleteFileStatement : DELETE FILE "
                   "dataReference IS? ; dataReference : IDENTIFIER ;", "FILE", "FILE IS", "STATUS")
    check("a same-keyword STRANGER that demands an unprinted word is never anchored (FILE STATUS vs DELETE FILE)",
          stranger.anchors == {"fileStatusClause"} and words(stranger) == {("IS", "")}, stranger)
    extension = run("classDefinitionClause : CLASS cobolWord (IS VENDORWORD)? ; cobolWord : IDENTIFIER ;",
                    "CLASS", "IS", "CLASS")
    check("a group demanding a word no format prints (an extension phrase) raises no candidate",
          extension.anchoring == "named" and not extension.candidates, extension)
    picture = run("pictureClause : PIC IS PIC_STRING ;", "PICTURE", "IS", "PICTURE")
    check("a keyword SPELLED by a differently named token anchors (PICTURE is the token PIC)",
          words(picture) == {("IS", "")}, picture)
    definition = ("interfaceDefinition : INTERFACE_ID DOT cobolWord (INHERITS FROM cobolWord)? END INTERFACE "
                  "cobolWord DOT ; cobolWord : IDENTIFIER ;")
    embedded = run(definition, "INTERFACE-ID", "FROM", "INTERFACE_ID INHERITS", enclosing="END INTERFACE")
    check("a paragraph printed INSIDE a definition anchors through the ENCLOSING format",
          embedded.anchoring == "enclosing" and words(embedded) == {("FROM", "(INHERITS FROM cobolWord)?")},
          embedded)
    lost = run(definition, "INTERFACE-ID", "FROM", "INTERFACE_ID INHERITS")
    check("a construct NO rule spells comes back UNEXAMINED, for the run to fail — never a clean zero",
          lost.anchoring == "" and not lost.candidates, lost)
    directive = run(definition, "TURN", "ON", "TURN")
    check("a keyword no parser token spells (a directive) is unexamined here and classified by the caller",
          directive.anchoring == "" and not keyword_tokens("TURN", _FIXTURE_KEYWORDS, _fixture(definition).tokens),
          directive)
    print("SELF-TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--construct", help="check only this construct's anchor keyword, e.g. USE")
    ap.add_argument("--clause", help="check only this general-format clause, e.g. 14.9.41.2")
    ap.add_argument("--show-format", action="store_true", help="print the measured words of each format checked")
    ap.add_argument("--list-unanchored", action="store_true",
                    help="name the formats the run cannot anchor: prose titles, and keywords no parser token spells")
    ap.add_argument("--self-test", action="store_true", help="run the fixture-grammar self-test (needs no PDF)")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a PRIVATE submodule; the public repository carries only the Markdown transcription at specs/ISO_COBOL.md. This tool measures the printed page, so it needs the PDF: "
                 "git submodule update --init specs-private")
    import fitz

    sys.path.insert(0, str(HERE))
    import clause_page

    doc = fitz.open(PDF)

    # ---- the grammar side --------------------------------------------------------------------------------
    grammar = parser_rules()                      # ⛔ grammar_source.read refuses an empty population
    gm = model({n: parse_body(r.rhs) for n, r in grammar.items()}, {n: r.file.name for n, r in grammar.items()})
    keywords = keyword_spellings()
    print(f"grammar files scanned: {len(grammar_source.grammar_files())}   parser rules: {len(gm.spellings)}   "
          f"rules demanding at least one token: {sum(1 for n in gm.spellings if gm.demands(n))}   "
          f"groups judged as units: {sum(len(s.groups) for sps in gm.spellings.values() for s in sps)}   "
          f"lexer tokens spelling COBOL words: {len(keywords)}")

    # ---- the spec side -----------------------------------------------------------------------------------
    formats = declared_formats(SPEC_MD.read_text(encoding="utf-8"))
    heads = clause_page.index(doc)
    where = {}
    for i, h in enumerate(heads):
        where.setdefault(h.clause, i)

    located = [f for f in formats if f.clause in where]
    unresolved = [f for f in formats if f.clause not in where]
    anchored = [f for f in located if f.keyword]
    print(f"general formats declared by the transcription: {len(formats)}   "
          f"located on the printed page: {len(located)}   with an anchor keyword: {len(anchored)}")

    selected = located
    if args.construct:
        selected = [f for f in selected if f.keyword == args.construct.upper()]
    if args.clause:
        selected = [f for f in selected if f.clause == args.clause]
    if (args.construct or args.clause) and not selected:
        sys.exit(f"⛔ no located general format matches {args.construct or args.clause!r} — "
                 f"the transcription declares {len(formats)} and {len(located)} resolve to a printed page")

    print(f"formats checked this run: {len(selected)}\n")

    cache: dict[int, list] = {}
    measures: dict[str, tuple[set[str], set[str]] | None] = {}

    def measured_format(f: Format) -> tuple[set[str], set[str]] | None:
        """(plain, underlined) words of one located format, measured once; None when the band holds no word."""
        if f.clause not in measures:
            i = where[f.clause]
            words = band_words(doc, cache, heads[i], heads[i + 1] if i + 1 < len(heads) else None)
            measures[f.clause] = measure(words) if words else None
        return measures[f.clause]

    def printed_words(plain: set[str], underlined: set[str]) -> set[str]:
        # A word `measure` deliberately SKIPS (FORMAT, ALL, TRUE …) is never measured, so it cannot testify that a
        # rule demands an unprinted word: the FORMAT clause's own rule demands FORMAT (§13.18.24.2).
        return {w.replace("-", "_") for w in plain | underlined | SKIP}

    def enclosing_words(f: Format) -> set[str]:
        """Every word printed by another located format that UNDERLINES this one's keyword."""
        out: set[str] = set()
        for g in located:
            m = measured_format(g) if g.clause != f.clause else None
            if m and f.keyword in m[1]:
                out |= printed_words(*m)
        return out

    candidates = 0
    measured = 0
    empty: list[Format] = []
    by_anchoring: collections.Counter = collections.Counter()
    unexamined: list[Format] = []
    not_parser: list[Format] = []
    for f in selected:
        m = measured_format(f)
        if m is None:
            empty.append(f)
            continue
        measured += 1
        plain, underlined = m
        page = heads[where[f.clause]].page
        if args.show_format:
            print(f"  {f.clause:<14} p{page:<5} {f.title}\n"
                  f"      required: {' '.join(sorted(underlined)) or '(none)'}\n"
                  f"      optional: {' '.join(sorted(plain)) or '(none)'}")
        if not plain or not f.keyword:
            continue
        if not keyword_tokens(f.keyword, keywords, gm.tokens):
            not_parser.append(f)
            continue
        ex = examine(gm, keywords, f.keyword, plain, printed_words(plain, underlined),
                     lambda f=f: enclosing_words(f))
        if args.show_format:
            print(f"      anchored: {ex.anchoring or 'NOTHING'} — {' '.join(sorted(ex.anchors)) or '(no rule)'}")
        if not ex.anchoring:
            unexamined.append(f)
            continue
        by_anchoring[ex.anchoring] += 1
        for c in ex.candidates:
            print(f"  CANDIDATE  {f.keyword:<14} {f.clause:<12} p{page:<5} "
                  f"{c.word:<12} required in {gm.origin[c.rule]}#{c.rule}"
                  + (f" inside {c.group}" if c.group else ""))
            candidates += 1
    doc.close()
    if args.list_unanchored:
        for f in located:
            if not f.keyword:
                print(f"    unanchored  {f.clause:<14} {f.title}")
        for f in not_parser:
            print(f"    no parser token spells {f.keyword:<12} {f.clause:<14} {f.title}")

    # ---- ⛔ THE POPULATION ASSERTION ----------------------------------------------------------------------
    # Every count above is checked against the population it claims to have measured. A tool that measured
    # NOTHING must never be able to print a clean zero: that is precisely how this one stayed green while three
    # statements rejected legal COBOL (kb/Work PB332).
    print(f"\n{candidates} candidate(s) — CONFIRM each against the raw rectangles before editing the grammar")
    print("constructs anchored: " + "   ".join(f"{a}: {by_anchoring[a]}" for a in ANCHORINGS)
          + f"   keyword spelled by no parser token (the preprocessor's directives, a prose title): "
            f"{len(not_parser)} — named by --list-unanchored")
    # (The grammar half of the population — at least one .g4 file and one parser rule — is asserted by
    # `grammar_source.read`, which refuses before a single format is measured.)
    failures: list[str] = []
    if not formats:
        failures.append(f"NO `General format` heading was found in {SPEC_MD} — the transcription declares none")
    if unresolved:
        failures.append(f"{len(unresolved)} declared general format(s) DID NOT RESOLVE to a printed page: "
                        + ", ".join(f.clause for f in unresolved[:12])
                        + (" …" if len(unresolved) > 12 else ""))
    if not anchored:
        failures.append("NO located general format carries an anchor keyword — the clause-title anchor is broken")
    if empty:
        failures.append(f"{len(empty)} located general format(s) measured ZERO words on the page: "
                        + ", ".join(f.clause for f in empty[:12]) + (" …" if len(empty) > 12 else ""))
    if selected and not measured:
        failures.append("every selected general format measured zero words — the audit inspected nothing")
    if unexamined:
        failures.append(f"{len(unexamined)} anchored general format(s) with optional words were EXAMINED BY NO RULE "
                        "— their keyword is a parser token, yet no rule spelling the format was anchored "
                        "(kb/Work PB715): " + ", ".join(f"{f.clause} {f.keyword}" for f in unexamined))
    if failures:
        print("\n⛔ POPULATION ASSERTION FAILED — this run measured less than it claims:", file=sys.stderr)
        for line in failures:
            print(f"   · {line}", file=sys.stderr)
        print("   A clean zero from this tool is only meaningful when it measured the whole population.",
              file=sys.stderr)
        return 1
    print(f"population OK: {measured} general format(s) measured, {len(gm.spellings)} parser rules scanned")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
