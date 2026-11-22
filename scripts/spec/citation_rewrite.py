#!/usr/bin/env python3
"""citation_rewrite.py - THE CANONICALIZER of citation designators: what `audit_code_citations.py --canonicalize` runs
(kb/Work PB4976, PB6000; made safe for the text a program prints by kb/Work PB6035).

    python scripts/spec/audit_code_citations.py --canonicalize <prefix>... [--dry-run] [--report <file.json>]

RESOLVE (`audit_code_citations.resolve_line`) refuses a designator written in no form the citation parser reads
(`designator-not-canonical`: `GR3 c) 2)`, `rule 6`, `6)`, `item (2)`, `SR12a2`). Where it has exactly one canonical form
(`audit_code_citations.canonical_of`) this rewrites it in place. The first run of it over `src/ tests/` (train-1985-c, gate
000003) broke 61 cases, in three ways, and this module is built so none of them can recur:

  1. A sub-item path written without its space (`SR12 a)2.`) was glued only to its first level (`SR12a2.`).
     `citation.SPACED_PATH` reads a later level with or without its space, and `citation.GLUED_NUMBER` makes the glued
     number itself non-canonical (`SR12a2` -> `SR12a.2`), so RESOLVE refuses it and this rewrites it.
  2. A label's `)` that also closed a parenthesis opened before the citation (`(ISO §2023:14.9.40.3 SR14 e)`) was
     dropped with the label. THE PARENTHESES ARE COUNTED (`_kept`): the rewrite keeps as many of the label's closers as
     the text around the citation needs to stay balanced. In a comment or a document that count is read on the line;
     in a string literal it is read over the literal's whole concatenation chain (`_chain`), and where the chain holds
     a non-literal operand (whose parentheses no reader can count) or the count does not balance, the designator is
     REFUSED, never guessed.
  3. A designator followed by an interpolation hole or a `{0}` format item (`rule 9 {arm}))`), or ending a literal that
     is concatenated with more text, may be completed by text no reader can see: it is REFUSED.

⛔ A STRING THE PROGRAM PRINTS CHANGES ONLY WITH EVERY EXPECTATION OF IT (kb/Work PB6035 (e)). A string literal of a C#
file under `src/` is message text: the compiler prints it, and goldens and tests quote it, usually by a fragment (`"SR14
e)"`, `"rule 9 b)"`). Each such rewrite carries its TAIL, the designator's text from its rule on (`SR14 e)` -> `SR14e)`),
and `_couple` rewrites every expectation that quotes the tail, or its stem without the label's closing parenthesis, in
the same run: the string literals of the C# tests (`tests/**/*.cs`) and the conformance suite's diagnostic expectations
(`conformance-suite/wiseowl/diagnostics/*.json`). A tail is rewritten only when it means one thing: when two rewrites of
it differ, when it also stands in a message string this run does not rewrite, or when a file no expectation reader
reads quotes it, every rewrite of it is refused. A test's own string literal is never rewritten by itself (it is an
expectation, or a test's input), and a generated file never (it is fixed where it is generated: kb/Work PB4976's
construct-sources addendum); the Characterization snapshots of the emitted code are regenerated after a run that
changes an emitter's text (`WISEOWL_COBOL_UPDATE_SNAPSHOTS=1`), and the report names them.

Every designator a run leaves is a REFUSAL with its reason (`Refusal`); `audit_code_citations.py --write-baseline` writes
that reason on the designator's baseline entry, so the debt that stays is debt that says why (kb/Work PB4976, PB6000).
"""
from __future__ import annotations

import dataclasses
import functools
import json
import pathlib
import re
import sys
from collections import Counter, defaultdict

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import audit_code_citations as audit  # noqa: E402  (RESOLVE: its population, its refusal of the form, `canonical_of`)
import citation  # noqa: E402  — THE one citation format and its parser (kb/Work PB4887)
import cite as cite_mod  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for csharp_lexer
import csharp_lexer  # noqa: E402  — THE one C# lexer of the tooling (kb/Work PB5230 review R2-1)

REPO = audit.REPO
#: Where a string literal is message text the compiler or the runtime prints.
PROGRAM = "src/"
#: The expectations `_couple` reads and rewrites: the C# tests' string literals, the suite's diagnostic expectations.
TESTS = "tests/"
DIAGNOSTIC_EXPECTATIONS = "conformance-suite/wiseowl/diagnostics/"
#: The Characterization snapshots of the emitted code: regenerated, never rewritten (`WISEOWL_COBOL_UPDATE_SNAPSHOTS=1`).
SNAPSHOTS = "tests/WiseOwl.Cobol.Tests.Characterization/Snapshots/"
#: COBOL sources: a citation in one is in a comment, and no expectation is read from one.
COBOL_SOURCE = (".cob", ".cpy", ".cbl")
#: A file a generator writes, fixed only at the generator's input.
GENERATED = re.compile(r"\.g\.cs$|\.g\.cs\.txt$")
#: What may follow a designator and complete it with text no reader sees: an interpolation hole or a format item.
_HOLE_AFTER = re.compile(r"\s?\{")
#: A literal's closing quotes (its run), all that may stand between a designator and its literal's end.
_CLOSING = re.compile(r'\s*"*\Z')


@dataclasses.dataclass(frozen=True)
class Edit:
    """One rewrite: `text[start:end]` of `file` (offsets into its whole text) becomes `new`. A rewrite of message text
    (`message`) carries the FRAGMENTS an expectation may quote of it, each with what it becomes: its tail (the designator
    from its rule on, `SR14 e)` -> `SR14e)`), its stem (the tail without the label's closing parentheses, `SR14 e` ->
    `SR14e`) and, for a rule named in words, its head (`rule 9` -> `GR9`)."""
    file: str
    line: int
    start: int
    end: int
    old: str
    new: str
    cited: str = ""
    message: bool = False
    fragments: tuple[tuple[str, str], ...] = ()
    #: An expectation's quotation of a rewritten message, rewritten with it (`_couple`).
    quotes: bool = False


@dataclasses.dataclass(frozen=True)
class Refusal:
    """A non-canonical designator this run leaves as written, and why (written on its baseline entry)."""
    file: str
    line: int
    citation: str
    reason: str
    start: int = -1


@dataclasses.dataclass
class Plan:
    edits: list[Edit] = dataclasses.field(default_factory=list)
    refusals: list[Refusal] = dataclasses.field(default_factory=list)
    #: Snapshot files that quote a rewritten message tail: regenerated after the run.
    regenerate: set[str] = dataclasses.field(default_factory=set)


def balance(text: str) -> int:
    """Opening minus closing parentheses."""
    return text.count("(") - text.count(")")


def _kept(before: int, after: int, closers: int) -> int | None:
    """How many of a label's `closers` (the net `)` of the text the rewrite removes) the rewrite keeps, so the text keeps
    its parentheses: `before` opens before the citation, `after` the balance of the text after it. A label's `)` that also
    closes a parenthesis opened before the citation (`(ISO §2023:14.9.40.3 SR14 e)`) is kept (`SR14e)`); one that is only
    a label (`(COBOL2199, §2023:13.18.35.3 SR6 e)) an`) is not (`SR6e)`). None when no count balances the text."""
    j = before + after
    return j if 0 <= j <= closers else None


@dataclasses.dataclass(frozen=True)
class _Site:
    """Where a citation stands: the text whose parentheses it sits among (`before`, `after` its span), whether that count
    is certain (`closed`), and whether it is message text (`message`) or a test's literal (`literal`)."""
    before: int
    after: int
    closed: bool
    message: bool
    literal: bool
    after_text: str
    ends_literal_concatenated: bool = False


def _runs(kinds: bytes, kind: int, at: int) -> tuple[int, int]:
    """The run of characters of `kind` (or holes, for a string) around offset `at`."""
    same = (kind, csharp_lexer.HOLE) if kind == csharp_lexer.STRING else (kind,)
    s = at
    while s > 0 and kinds[s - 1] in same:
        s -= 1
    e = at
    while e < len(kinds) and kinds[e] in same:
        e += 1
    return s, e


def _string_chars(text: str, kinds: bytes, s: int, e: int) -> str:
    """The characters of `text[s:e]` that are string text (a hole's code is not)."""
    return "".join(ch for ch, k in zip(text[s:e], kinds[s:e]) if k == csharp_lexer.STRING)


def _skip_back(text: str, kinds: bytes, i: int) -> int:
    """The offset of the last code character before `i` that is neither white space nor a comment, or -1."""
    i -= 1
    while i >= 0 and (text[i].isspace() or kinds[i] == csharp_lexer.COMMENT):
        i -= 1
    return i


def _skip_forward(text: str, kinds: bytes, i: int) -> int:
    while i < len(text) and (text[i].isspace() or kinds[i] == csharp_lexer.COMMENT):
        i += 1
    return i


def _chain(text: str, kinds: bytes, s: int, e: int) -> tuple[list[tuple[int, int]], bool, bool]:
    """The string literals concatenated with the one at `text[s:e]` by `+` (in order), whether every operand of the
    concatenation is a literal (`closed`), and whether a `+` follows the literal at `text[s:e]`."""
    literals = [(s, e)]
    closed = True
    at = s
    while (p := _skip_back(text, kinds, at)) >= 0 and text[p] == "+" and kinds[p] == csharp_lexer.CODE:
        q = _skip_back(text, kinds, p)
        if q < 0 or kinds[q] != csharp_lexer.STRING:
            closed = False
            break
        ls, _ = _runs(kinds, csharp_lexer.STRING, q)
        literals.insert(0, (ls, q + 1))
        at = ls
    at = e
    followed = False
    while (p := _skip_forward(text, kinds, at)) < len(text) and text[p] == "+" and kinds[p] == csharp_lexer.CODE:
        followed = followed or at == e
        q = _skip_forward(text, kinds, p + 1)
        if q >= len(text) or kinds[q] != csharp_lexer.STRING:
            closed = False
            break
        _, le = _runs(kinds, csharp_lexer.STRING, q)
        literals.append((q, le))
        at = le
    return literals, closed, followed


def _site(rel: str, text: str, lexed, line_start: int, line_end: int, start: int, end: int) -> _Site | str:
    """Where the citation spanning `text[start:end]` stands, or why it is not rewritten (a string)."""
    if lexed is None:  # a document, a grammar, a record, a COBOL source: the line is the text
        return _Site(balance(text[line_start:start]), balance(text[end:line_end]), True, False, False,
                     text[end:line_end])
    kinds = lexed.kinds
    kind = kinds[start]
    if kind == csharp_lexer.COMMENT:
        return _Site(balance("".join(ch for ch, k in zip(text[line_start:start], kinds[line_start:start])
                                     if k == csharp_lexer.COMMENT)),
                     balance("".join(ch for ch, k in zip(text[end:line_end], kinds[end:line_end])
                                     if k == csharp_lexer.COMMENT)), True, False, False, text[end:line_end])
    if kind != csharp_lexer.STRING:
        return "the citation stands in code, not in a comment or a string"
    s, e = _runs(kinds, csharp_lexer.STRING, start)
    literals, closed, followed = _chain(text, kinds, s, e)
    before = sum(balance(_string_chars(text, kinds, ls, min(le, start))) for ls, le in literals if ls < start)
    after = sum(balance(_string_chars(text, kinds, max(ls, end), le)) for ls, le in literals if le > end)
    tail = _string_chars(text, kinds, end, e)
    # the text of this literal after the designator, to its end or its next hole (the hole's brace is string text)
    run_end = end
    while run_end < e and kinds[run_end] == csharp_lexer.STRING:
        run_end += 1
    return _Site(before, after, closed, rel.startswith(PROGRAM), True, text[end:run_end],
                 _CLOSING.match(tail) is not None and followed)


def _tail_start(written: str) -> int:
    """The offset in a citation's written text where its designator begins: after `§<edition>:<locator> `."""
    return written.index(" ") + 1


def _proposals(rel: str, text: str) -> tuple[list[Edit], list[Refusal]]:
    """Every rewrite `rel`'s text admits and every non-canonical designator it leaves, with the reason."""
    edits: list[Edit] = []
    refusals: list[Refusal] = []
    if "§" not in text:
        return edits, refusals
    lexed = csharp_lexer.lex(text) if rel.endswith(".cs") else None
    lines = text.split("\n")
    offset = 0
    for i, line in enumerate(lines):
        line_start, line_end = offset, offset + len(line)
        offset = line_end + 1
        prev = lines[i - 1] if i and not line.startswith("|") else ""
        if "§" not in line or citation.example(line) or audit.NAMED_AS_WRONG.search(line) or audit.NAMED_AS_WRONG.search(prev):
            continue
        for c in citation.scan(line):
            if c.error or c.bare or audit._repair_record(line, c):
                continue
            loose = citation.loose_designator(line, c)
            if loose is not None:
                canonical = audit.canonical_of(loose)
                end = loose.end
                if canonical is None:
                    refusals.append(Refusal(rel, i + 1, citation.render(c), _why_no_form(loose)))
                    continue
            elif (item := _item_rule(c)) is not None:
                canonical, end = item, c.end
            else:
                continue
            removed = line[c.end:end]
            closers = max(0, -balance(removed))
            site = _site(rel, text, lexed, line_start, line_end, line_start + c.start, line_start + end)
            why = _refusal(site, closers)
            if why is not None:
                refusals.append(Refusal(rel, i + 1, citation.render(c), why, line_start + c.start))
                continue
            kept = (0 if not closers else _kept(site.before, site.after, closers) if site.literal
                    else max(0, min(closers, site.before + site.after)))
            old = line[c.start:end]
            new = canonical + ")" * kept
            edits.append(Edit(rel, i + 1, line_start + c.start, line_start + end, old, new, citation.render(c),
                              site.message, _fragments(old, canonical, kept, closers) if site.message else ()))
    return edits, refusals


#: A rule named in words, the head of a tail (`rule 9 b)`: `rule 9`), and the designator that replaces it (`GR9`).
_WORD_HEAD = re.compile(r"(?:[A-Za-z-]+ phrase )?rules? \d+|items? \(?\d+\)?")
_DESIGNATOR_HEAD = re.compile(rf"(?:{'|'.join(citation.RULE_KINDS)})(?:L\d+\.)?\d+|item (?:L\d+\.)?\d+")


def _fragments(old: str, canonical: str, kept: int, closers: int) -> tuple[tuple[str, str], ...]:
    """The fragments an expectation may quote of a rewritten message designator, each with what it becomes: the tail,
    the stem (without the label's closers the rewrite drops or keeps) and a rule named in words' head."""
    tail_old, tail_new = old[_tail_start(old):], canonical[_tail_start(canonical):]
    out = [(old, canonical + ")" * kept), (tail_old, tail_new + ")" * kept)]
    if closers:
        out.append((old[:len(old) - closers] if old.endswith(")" * closers) else old, canonical))
        out.append((tail_old[:len(tail_old) - closers] if tail_old.endswith(")" * closers) else tail_old, tail_new))
    if (h := _WORD_HEAD.match(tail_old)) and (d := _DESIGNATOR_HEAD.match(tail_new)) and h.group(0) != tail_old:
        out.append((h.group(0), d.group(0)))
    return tuple(dict.fromkeys(out))


def _refusal(site: _Site | str, closers: int) -> str | None:
    """Why the designator at `site` is not rewritten, else None."""
    if isinstance(site, str):
        return site
    if _HOLE_AFTER.match(site.after_text):
        return "an interpolation hole or a format item follows the designator and may complete it"
    if not site.literal:
        return None
    if not site.message:
        return "a test's string literal: an expectation changes only with the message it quotes"
    if site.ends_literal_concatenated:
        return "the designator ends its string literal and the string is concatenated, so its text may continue"
    if closers and not site.closed:
        return "the parentheses around the designator lie partly in a non-literal operand of the string"
    if closers and _kept(site.before, site.after, closers) is None:
        return "the parentheses around the designator do not balance in its string"
    return None


def _why_no_form(loose: "citation.Loose") -> str:
    """Why a non-canonical designator has no one canonical form (`audit_code_citations.canonical_of` gave None)."""
    if loose.listed:
        return "a list of designators: no one designator says it"
    if loose.item:
        return "a list item no one printed item answers (several lists print it, or none)"
    if loose.ordinal is not None:
        return "the catalog gives that ordinal no one rule of the clause"
    return "the rule cited has no catalog designator for its sub-item path"


def _item_rule(c: "citation.Citation") -> str | None:
    """An `item <n>` the catalog holds as a numbered rule, printed by one list alone, is cited by that rule (kb/Work
    PB5449 item 4): the rule's citation, else None."""
    if not (c.rule and cite_mod.ITEM.fullmatch(c.rule) and citation.transcription(c.edition) is not None):
        return None
    m = cite_mod.ITEM.fullmatch(c.rule)
    path = tuple(re.findall(r"[a-z]|\d+", m["ipath"] or ""))
    v = audit._resolved(c.edition, c.locator, c.rule, None)
    if v.kind == "item-is-a-rule" and audit._item_designator(c.edition, c.locator, int(m["item"]), path) == v.suggestion.split(" ", 1)[1]:
        return v.suggestion
    return None


def _bounded(fragment: str) -> re.Pattern:
    """`fragment` as a whole token: no word character joined to its ends (`rule 8` is not in `rule 80`)."""
    lead = r"(?<![\w])" if fragment[:1].isalnum() else ""
    trail = r"(?![\w])" if fragment[-1:].isalnum() else ""
    return re.compile(lead + re.escape(fragment) + trail)


def _texts(paths) -> dict[str, str]:
    out = {}
    for p in paths:
        try:
            out[p.relative_to(REPO).as_posix()] = p.read_bytes().decode("utf-8")
        except (OSError, UnicodeDecodeError):
            continue
    return out


def _message_text(texts: dict[str, str]) -> dict[str, str]:
    """Each program file's string text (its literals' characters, everything else blanked), where a message is read."""
    out = {}
    for rel, text in texts.items():
        if rel.startswith(PROGRAM) and rel.endswith(".cs") and not GENERATED.search(rel):
            kinds = csharp_lexer.lex(text).kinds
            out[rel] = "".join(ch if k == csharp_lexer.STRING else " " for ch, k in zip(text, kinds))
    return out


def _expectation_text(rel: str, text: str) -> str | None:
    """The part of `rel` an expectation of a message is read from (its other characters blanked), or None when `rel`
    holds no expectation: a C# test's string literals, a diagnostic expectation's whole text."""
    if rel.startswith(DIAGNOSTIC_EXPECTATIONS) and rel.endswith(".json"):
        return text
    if rel.startswith(TESTS) and rel.endswith(".cs") and not GENERATED.search(rel):
        kinds = csharp_lexer.lex(text).kinds
        return "".join(ch if k == csharp_lexer.STRING else " " for ch, k in zip(text, kinds))
    return None


def _quoters(rel: str, text: str) -> str | None:
    """Text of `rel` that may quote a message but that no expectation reader rewrites, or None: any test or suite file
    but a COBOL source (comments), a generated snapshot (regenerated), the inventory and construct records (citations,
    read as citations) and a C# test's comments."""
    if not rel.startswith((TESTS, "conformance-suite/")) or rel.endswith(COBOL_SOURCE) or rel.startswith(SNAPSHOTS):
        return None
    if rel.startswith("tests/version-matrix/") or _expectation_text(rel, text) is not None or rel.endswith(".cs"):
        return None
    return text


def _occurrences(fragments: list[str], text: str, quoting: bool = True) -> dict[str, list[tuple[int, int]]]:
    """Where each of `fragments` stands in `text`, the longest at each place (a shorter one is not counted inside a
    longer one's), in one pass. In a quotation (`quoting`) a tail written right after a citation is that citation's,
    answered only by its whole-citation fragment (`§2023:6.1 3) a)` is not the `3) a)` of §2023:8.4.6.2.1); in message
    text every occurrence counts."""
    out: dict[str, list[tuple[int, int]]] = {}
    if not fragments or (pattern := _alternation(tuple(sorted(set(fragments), key=lambda f: (-len(f), f))))) is None:
        return out
    for m in pattern.finditer(text):
        if quoting and not m.group(0).startswith("§") and _CITED_BEFORE.search(text, max(0, m.start() - 40), m.start()):
            continue  # a tail after a citation belongs to THAT citation: only its whole-citation fragment answers it
        out.setdefault(m.group(0), []).append(m.span())
    return out


#: A citation's edition and locator directly before a fragment (`§2023:6.1 ` before `3) a)`).
_CITED_BEFORE = re.compile(r"§\d{4}:\S+ \Z")


@functools.lru_cache(maxsize=16)
def _alternation(fragments: tuple[str, ...]) -> re.Pattern | None:
    return re.compile("|".join(_bounded(f).pattern for f in fragments)) if fragments else None


def _blanked(text: str, spans) -> str:
    """`text` with each of `spans` blanked (a live rewrite's own text is not an occurrence it leaves)."""
    if not spans:
        return text
    chars = list(text)
    for s, e in spans:
        chars[s:e] = " " * (e - s)
    return "".join(chars)


def _couple(plan: Plan, texts: dict[str, str]) -> None:
    """Hold every message rewrite to its expectations (the module's doc, kb/Work PB6035 (e)): a fragment an expectation
    quotes is rewritten there with its message, and only when it means one thing; else each rewrite of it is refused."""
    messages = [e for e in plan.edits if e.message]
    if not messages:
        return
    # only a file whose raw text holds some fragment can hold an occurrence of one: the others are not lexed
    every = _alternation(tuple(sorted({old for e in messages for old, _ in e.fragments}, key=lambda f: (-len(f), f))))
    texts = {rel: text for rel, text in texts.items() if every.search(text)}
    program = _message_text(texts)
    expectations = {rel: t for rel, text in texts.items() if (t := _expectation_text(rel, text)) is not None}
    others = {rel: t for rel, text in texts.items() if (t := _quoters(rel, text)) is not None}
    refused: dict[int, str] = {}
    while True:
        live = [e for e in messages if id(e) not in refused]
        forms, owners = defaultdict(set), defaultdict(list)
        for e in live:
            for old, new in e.fragments:
                forms[old].add(new)
                owners[old].append(e)
        quoted = {f for t in expectations.values() for f in _occurrences(list(forms), t)}
        loose = {f for t in others.values() for f in _occurrences(list(forms), t)}
        spans = defaultdict(list)
        for e in live:
            spans[e.file].append((e.start, e.end))
        unrewritten = {f for rel, t in program.items()
                       for f in _occurrences(list(quoted), _blanked(t, spans[rel]), quoting=False)}
        grew = False
        for f in forms:
            why = ("a test or suite file no expectation reader rewrites quotes the designator text" if f in loose
                   else None if f not in quoted
                   else "the same designator text is rewritten two ways, so an expectation of it is ambiguous"
                   if len(forms[f]) > 1
                   else "the same designator text stands in a message string this run does not rewrite"
                   if f in unrewritten else None)
            if why:
                for e in owners[f]:
                    if id(e) not in refused:
                        refused[id(e)] = why
                        grew = True
        if not grew:
            break
    for e in messages:
        if id(e) in refused:
            plan.refusals.append(Refusal(e.file, e.line, e.cited, refused[id(e)], e.start))
    plan.edits = [e for e in plan.edits if id(e) not in refused]
    pairs = {old: new for e in plan.edits for old, new in e.fragments}
    added = []
    for rel, t in expectations.items():
        mine = [(e.start, e.end) for e in plan.edits if e.file == rel]
        for old, places in _occurrences(list(pairs), t).items():
            for s, e in places:
                if not any(a < e and s < b for a, b in mine):
                    added.append(Edit(rel, texts[rel].count("\n", 0, s) + 1, s, e, old, pairs[old], quotes=True))
    plan.edits += added
    lines = {(e.file, e.line) for e in added}
    plan.refusals = [r for r in plan.refusals if (r.file, r.line) not in lines
                     or not r.reason.startswith("a test's string literal")]
    for rel, text in texts.items():
        if rel.startswith(SNAPSHOTS) and _occurrences(list(pairs), text):
            plan.regenerate.add(rel)


def never_rewritten(rel: str) -> bool:
    """A file no run rewrites: the work register's note bodies (their owner's history), the DEVLOG, a generated file."""
    return rel.startswith((audit.REGISTER_HISTORY, "DEVLOG/")) or GENERATED.search(rel) is not None


def plan_for(prefixes: list[str]) -> Plan:
    """The rewrites and refusals of a run over the RESOLVE population's files under `prefixes`."""
    population = audit.resolve_population()
    scope = [p for p in population
             if (rel := p.relative_to(REPO).as_posix()).startswith(tuple(prefixes)) and not never_rewritten(rel)]
    texts = _texts(scope)
    # the expectations a message rewrite couples to are read wherever they are, inside the prefixes or not
    wider = {p.relative_to(REPO).as_posix(): p for p in audit.citation_corpus.repository_files()
             if p.relative_to(REPO).as_posix().startswith((PROGRAM, TESTS, "conformance-suite/"))}
    return plan_texts(texts, _texts(p for rel, p in wider.items() if rel not in texts))


def plan_texts(scope: dict[str, str], around: dict[str, str]) -> Plan:
    """The plan for the files `scope` maps (repository path -> text), the files `around` read only for the messages
    and the expectations a rewrite couples to."""
    plan = Plan()
    for rel, text in scope.items():
        if GENERATED.search(rel):
            continue
        edits, refusals = _proposals(rel, text)
        plan.edits += edits
        plan.refusals += refusals
    _couple(plan, {**around, **scope})
    return plan


def applied(plan: Plan, texts: dict[str, str]) -> dict[str, str]:
    """`texts` (repository path -> text) with `plan`'s edits made: the one place an edit is written."""
    out = dict(texts)
    for e in sorted(plan.edits, key=lambda x: (x.file, x.start), reverse=True):
        t = out[e.file]
        assert t[e.start:e.end] == e.old, (e.file, e.line, e.old)
        out[e.file] = t[:e.start] + e.new + t[e.end:]
    return out


def apply(plan: Plan) -> dict[str, int]:
    """Write `plan`'s edits; the count of changed lines per file."""
    files = sorted({e.file for e in plan.edits})
    for rel, text in applied(plan, _texts(REPO / f for f in files)).items():
        (REPO / rel).write_bytes(text.encode("utf-8"))
    return {rel: len({e.line for e in plan.edits if e.file == rel}) for rel in files}


def report(plan: Plan) -> dict:
    """What a run did, for its caller: the files changed, each message rewrite (old -> new, where), each expectation
    rewritten with them (their files' tests run by filter), the snapshots to regenerate, and the refusals by reason."""
    ordered = sorted(plan.edits, key=lambda x: (x.file, x.start))
    return {"edits": len(plan.edits), "files": sorted({e.file for e in plan.edits}),
            "messages": [{"file": e.file, "line": e.line, "old": e.old, "new": e.new} for e in ordered if e.message],
            "expectations": [{"file": e.file, "line": e.line, "old": e.old, "new": e.new} for e in ordered if e.quotes],
            "regenerate": sorted(plan.regenerate),
            "refusals": Counter(r.reason for r in plan.refusals).most_common()}


def reasons(prefixes: list[str] | None = None) -> dict[tuple[str, int, str], str]:
    """(file, line, written designator) -> why a run leaves it (`Refusal.reason`), for the baseline's entries."""
    return {(r.file, r.line, r.citation): r.reason for r in plan_for(prefixes or [""]).refusals}


def canonicalize(prefixes: list[str], dry_run: bool = False, report_path: str | None = None) -> int:
    """`--canonicalize`: plan, then (unless `dry_run`) write the rewrites; print the per-file counts and the report."""
    plan = plan_for(prefixes)
    rep = report(plan)
    if not dry_run:
        for rel, n in apply(plan).items():
            print(f"  {rel}: {n} line(s)")
    print(f"canonicalized {rep['edits']} designator(s) in {len(rep['files'])} file(s)"
          + (" (dry run: nothing written)" if dry_run else ""))
    print(f"  message designators rewritten: {len(rep['messages'])}; their expectations rewritten with them: "
          f"{len(rep['expectations'])} in {len({e['file'] for e in rep['expectations']})} file(s); snapshots to "
          f"regenerate: {len(rep['regenerate'])}")
    for reason, n in rep["refusals"]:
        print(f"  left ({n}): {reason}")
    if report_path:
        pathlib.Path(report_path).write_text(json.dumps(rep, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    return 0
