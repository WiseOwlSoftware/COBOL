#!/usr/bin/env python3
"""Validate a spec citation against the transcription of the edition it names — the mechanical check for CLAUDE.md
rule 1. `--check` takes a WHOLE citation in the format `scripts/spec/citation.py` specifies (`§<edition>:<locator>[ <rule>]`,
kb/Work PB4887) and a quotation of the standard; `--find` searches the transcription `--edition` names (default 2023).

WHY THIS EXISTS. Rule 1 requires the exact §/GR for any semantics question, and the failure mode is not
inventing a citation — it is INHERITING one. A fix-queue entry or design doc carries "§8.4.1.2 GR2", the text
it quotes really does say what it claims, and the clause NUMBER is never re-derived; it then propagates into
code comments, goldens and DEVLOG entries as though it had been checked. Two of the CA10 citations were wrong
this way (the real clauses are §2023:8.4.2.3.4 GR2 and §2023:13.18.38.4 GR7) and nothing would have caught them.

    python scripts/spec/cite.py --check "§2023:8.4.2.3.4 GR1" "EC-BOUND-SUBSCRIPT exception condition is set to exist"
    python scripts/spec/cite.py --check "§1985:XI.4.4.4 GR6b" "The file referenced by file-name-1 is sequenced"
    python scripts/spec/cite.py --find  "shall fall within the bounds from integer-1"

⛔ EVERY PART OF THE CITATION IS CHECKED, IN EVERY EDITION, OR THE CHECK FAILS NAMING THE PART (kb/Work PB5317; owner
2026-11-10: "If it cannot be resolved, there is a problem/bug"). Until PB5317 a 1985 citation's rule number was never
compared (`§1985:XI.4.4.4 GR99` passed with any quotation from the paragraph) and a one-character quotation matched
almost any clause. `--check` answers OK only when ALL of these hold, and otherwise prints `FAIL [<kind>]` naming the
first part that failed (`FAILURE_KINDS`) and, where the checker can find it, the designator the text really sits under:

  1. the edition's transcription is in the tree (`edition-not-held`);
  2. the locator names a region of it (`no-clause`; citation.validate);
  3. the quotation carries at least MIN_WORDS real words (`quote-too-short`): the comparison is on words after `norm`
     (punctuation, underline markup and spacing are typography), and a shorter quotation occurs in almost any clause;
  4. with a rule designator, the clause has a rule list of that kind (`no-rule-list`; when a subclause has it, the
     suggestion names the subclause: `§2023:14.9.39 SR17` is `§2023:14.9.39.3 SR17`), that list has a rule with the
     ordinal (`no-ordinal`, with the count), and the rule has the sub-item the designator names (`no-sub-item`, with
     the sub-items it has); a `Format n` names a general format the clause or its general-format subclause prints
     (`no-format`), a `Table n` a table captioned in the clause or under it (`no-table`);
  5. the quotation lies INSIDE the cited unit: the sub-item when one is named, else the rule, else the format or the
     table, else the clause (`text-not-in-rule`, naming the rule or sub-item it IS in; `text-not-in-clause`, naming the
     clause; `text-not-found`).

THE DESIGNATOR FORMS IT UNDERSTANDS (`DESIGNATOR`), and any other FAILS by name (`designator-not-understood`):
`SRn` `GRn` `ARn` `RVn` (a rule of the block's first numbered list); `GRLm.n` (rule n of the block's m-th list, a list
that restarts its numbering: citation.rule_designator); either followed by a glued SUB-ITEM PATH of single letters and
numbers, each optionally after a `.` (`GR6c`, `GR6d.2`, `GR3a1`, `GR6d.2.b`), read level by level against the printed
labels of the rule (`LABELS`: ISO `6)` `d)` `2.` `b.`; 1985 `(6)` `a.` `1)`); `Format n`; `Table n`.

⛔ WHERE A RULE STARTS AND ENDS IS THE RULE CATALOG'S READING, NOT A SECOND PARSER (`rule_book`). For an edition whose
inventory schema names a rule catalog (`inventory_schema.EDITIONS`, `catalog`: 2023 and 1985 today), each catalog row
(its locator, kind, list and ordinal, the extractor's decision: `extract_rule_catalog.py`, `rule_catalog_1985.py`) is
ANCHORED to the transcription line that opens it (its printed ordinal and the first words of its text, by the
extractor's own `ORDINAL`), and runs to the next row's anchor in the same clause: those lines and the row's own text
are the rule. A row that cannot be anchored is a drift of the catalog from the transcription and FAILS the citation
that names it (`unanchored`; the self-test anchors every row of every held catalog). The committed catalogs are the
extractor's current output (the RULE CATALOG audits fail on a stale one), so the boundaries are the extractor's.
An edition with NO catalog (2002 and 2014 until their catalogs land, kb/Work PB5256) is checked in LABEL MODE: its rule
blocks are the clauses the catalog's own kind table types by their heading (`extract_rule_catalog.kind_of`), its
rules run from one column-0 rule label to the next (`LABELS`, the reader of sub-items in every edition), and every
verdict reached that way carries `LABEL_MODE_MARK`: it is never a silent fall back to checking the clause alone.
Sub-items are folded into their rule by every catalog (THE RULE IS THE UNIT, kb/Work PB55), so a sub-item's extent is
read inside the rule's lines from the printed labels (`label_path`): in an ISO edition by indentation and label style;
in the 1985 transcription, which keeps no indentation, by label style alone, an unlabelled paragraph belonging to the
label above it.

⛔ A CHECK THAT CANNOT BE VALID FAILS, IT NEVER ANSWERS OK (kb/Work PB308). A quotation made entirely of punctuation
normalizes to the EMPTY string, which is a substring of everything; one of fewer than MIN_WORDS real words is nearly
as weak. Both are refused before anything is compared.

THE CORRECTED TEXT AND THE PRINTED TEXT BOTH PASS (R66, specs/README.md "Corrections"). Where the transcription
corrects a defect of the printed standard, the corrected line is followed by a `> ⚠ **CORRECTED — see the
Addendum (Cn).**` note (`Errata` in 1985: verify_publishable.CORRECTIONS_SECTION) that quotes the PRINTED line verbatim,
so a quotation of either form is found inside the same rule: the note lies in the rule's lines, and `label_path` gives it
the corrected line's path.

A RECONSTRUCTION PASSES, MARKED (kb/Work PB4952). Where the scan lacks printed pages, the transcription holds an
editorial reconstruction between a `> ⚠ **RECONSTRUCTED — not printed in our copy (folios ...).**` note and its
`> ⚠ **END RECONSTRUCTED ...**` note (`RECONSTRUCTED`). A quotation found there (or in a rule the catalog marks
`provisional`) is reported with `RECONSTRUCTED_MARK`: the citation names the standard's locator, but the text it was
checked against is the transcription's, not the standard's.

`resolve` is the one checker; `check` is its CLI form, and the citation audits call `resolve` in-process
(`audit_code_citations.py` RESOLVE, `audit_doc_citations.py`). Run `--self-test` for every arm, in every edition.
"""
from __future__ import annotations

import argparse
import dataclasses
import functools
import json
import pathlib
import re
import sys
import tempfile
from typing import NamedTuple
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
import citation  # noqa: E402  (scripts/spec: the one citation format and its parser, kb/Work PB4887)
import inventory_schema  # noqa: E402  (each edition's rule catalog: the schema's `editions.of`, the one home)
from spec_headings import GENERAL_FORMAT_TITLE  # noqa: E402  (THE spelling of a general-format heading, kb/Work PB284)

# The citation itself — its edition, its locator and the regions of each edition's transcription a locator names — is
# `citation.py`'s (kb/Work PB4887): `--check` takes a WHOLE citation and checks its text inside that edition's region.
# The transcription escapes a rule label's delimiter (`1\)`) so Markdown does not eat it as a list — see
# repairs/rule_numbering.py. Match both forms so this works on any revision.
TOP = re.compile(r"^(\d+)\\?\)\s")
#: A lettered item: indented under its rule, or at column 0 where the transcription flattened the list (§2023:14.9.23.4
#: rule 7's a) to d)); its depth is still (indentation, style level), so it nests under the rule either way.
SUB = re.compile(r"^(\s*)([a-z])\\?\)\s")
SUBSUB = re.compile(r"^(\s{2,})(\d+)\\?\.\s")
SUBSUBSUB = re.compile(r"^(\s*)([a-z])\\?\.\s")
#: The 1985 transcription's labels (kb/Work PB4850): a rule `(6)` at column 0 (a boxed rule opens with the box's side,
#: spec_pdf.BOX_SIDE), its lettered items `a.` and their numbered items `1)`, all at column 0.
_BOX = re.escape(spec_pdf.BOX_SIDE)
TOP_85 = re.compile(rf"^{_BOX}?\\?\((\d+)\\?\)\s")
SUB_85 = re.compile(rf"^{_BOX}?([a-z])\\?\.\s")
SUBSUB_85 = re.compile(rf"^{_BOX}?(\d+)\\?\)\s")
#: An editorial note on a correction (R66): the paragraph belongs to the line ABOVE it, so a quotation found in
#: the note reports that line's rule path.
CORRECTION_NOTE = re.compile(r"^>\s*⚠\s*\*\*CORRECTED")
#: ⛔ A RECONSTRUCTION IS NOT THE STANDARD'S TEXT (owner 2026-11-09, kb/Work PB4952). Where the scan of a printed standard
#: lacks pages, the transcription fills the gap with EDITORIAL text between an opening note, which names the folios it
#: stands for, its sources and its confidence, and a closing note naming the same folios, until the printed pages are
#: obtained and their verbatim text replaces it. Every reader that takes text from there says so: the rule catalog marks
#: the rows `provisional`, `--check` marks a citation it verifies there, the figure sweep has no printed drawing to read.
RECONSTRUCTED = re.compile(r"^>\s*⚠\s*\*\*RECONSTRUCTED — not printed in our copy \(folios? (?P<folios>[^)]+)\)\.\*\*")
RECONSTRUCTED_END = re.compile(r"^>\s*⚠\s*\*\*END RECONSTRUCTED \(folios? (?P<folios>[^)]+)\)\.\*\*")
#: An editorial note of either kind: never rule text, and a blockquote paragraph that ends at its blank line.
EDITORIAL_NOTE = re.compile(r"^>\s*⚠\s*\*\*(?:CORRECTED|RECONSTRUCTED — |END RECONSTRUCTED )")
#: The mark `--check` and `--find` print on a quotation found inside a reconstruction.
RECONSTRUCTED_MARK = "[RECONSTRUCTED: editorial text, not the standard's; kb/Work PB4952]"


class Reconstruction(NamedTuple):
    """One reconstruction: the line of its opening note, the line of its closing note (None while unclosed), and the
    folios each note names (`folios XIII-30 and XIII-31`)."""
    start: int
    end: int | None
    folios: str
    closing: str | None


def folios_of(named: str) -> list[str]:
    """The folios a note names: `XIII-30 and XIII-31` is two, `XIII-54` one."""
    return re.findall(r"(?:[IVX]+|[A-F])-\d+", named)


def reconstructions(lines: list[str]) -> list[Reconstruction]:
    """Every reconstruction of `lines`, in order. A closing note with no opening one is a Reconstruction whose `start`
    is -1; `verify_publishable.check_reconstructions` refuses both that and an unclosed one."""
    out: list[Reconstruction] = []
    open_at: tuple[int, str] | None = None
    for i, line in enumerate(lines):
        if (m := RECONSTRUCTED.match(line)) is not None:
            if open_at is not None:
                out.append(Reconstruction(open_at[0], None, open_at[1], None))
            open_at = (i, m["folios"])
        elif (m := RECONSTRUCTED_END.match(line)) is not None:
            if open_at is None:
                out.append(Reconstruction(-1, i, "", m["folios"]))
            else:
                out.append(Reconstruction(open_at[0], i, open_at[1], m["folios"]))
            open_at = None
    if open_at is not None:
        out.append(Reconstruction(open_at[0], None, open_at[1], None))
    return out


def in_reconstruction(lines: list[str], at: int) -> bool:
    """Whether line `at` lies inside a reconstruction: the nearest note above it (or on it) is an opening one."""
    for k in range(at, -1, -1):
        if "RECONSTRUCTED" in lines[k]:   # (a substring test first: the walk back reads tens of thousands of lines)
            if RECONSTRUCTED.match(lines[k]):
                return True
            if RECONSTRUCTED_END.match(lines[k]):
                return k == at
    return False


def reconstructed(lines: list[str]) -> list[str | None]:
    """For each line of `lines`, the first folio of the reconstruction it lies inside (its notes included), or None
    outside every one: one pass, read by index. A rule that opens inside one is on that folio, not the PDF page's."""
    folio: list[str | None] = [None] * len(lines)
    for r in reconstructions(lines):
        if r.start >= 0:
            first = next(iter(folios_of(r.folios)), r.folios)
            for i in range(r.start, len(lines) if r.end is None else r.end + 1):
                folio[i] = first
    return folio


EMPTY_NEEDLE = "quotation carries no word characters (letters, digits, underscore); nothing to match"
#: The fewest REAL words (a word carrying a letter, after `norm`) a quotation must have to be checked at all (kb/Work
#: PB5317): "x" was found in §1985:XI.4.4.4 and "input procedure" is in dozens of clauses, so a shorter quotation proves
#: nothing about where it was taken from. One rule for every edition; `--find` (a search, not a check) is not bound by it.
MIN_WORDS = 4


def real_words(needle: str) -> list[str]:
    """The REAL words of a normalized quotation: two characters or more, at least one a letter (`x`, `9`, `5` are not)."""
    return [w for w in needle.split() if len(w) >= 2 and re.search(r"[^\W\d_]", w)]

#: The mark on a verdict reached in an edition with no rule catalog (see the module docstring, LABEL MODE).
LABEL_MODE_MARK = "[LABEL MODE: no rule catalog for this edition; rule boundaries read from the printed labels]"

#: THE FAILURE KINDS of `resolve`, the vocabulary `--check`, the RESOLVE audit and its baseline share.
FAILURE_KINDS = {
    "edition-not-held": "the edition's transcription is not in the tree: nothing can be checked",
    "no-clause": "the locator names no region of the edition's transcription",
    "designator-not-understood": "the rule designator is in no form the checker reads (DESIGNATOR)",
    "no-rule-list": "the clause has no rule list of the cited kind",
    "no-ordinal": "the cited list has no rule with that ordinal",
    "no-sub-item": "the rule has no sub-item with that path",
    "no-format": "the clause prints no general format with that number",
    "no-table": "no table with that number is captioned in the clause or under it",
    "unanchored": "the catalog row the citation names was not found in the transcription (a drift of the catalog)",
    "quote-too-short": f"the quotation has fewer than {MIN_WORDS} real words",
    "text-not-in-rule": "the quotation is in the clause, but not inside the cited rule, sub-item, format or table",
    "text-not-in-clause": "the quotation is in the standard, but in another clause",
    "text-not-found": "the quotation is nowhere in the edition's transcription",
}
#: The rule kinds a designator names, as a reader says them.
KIND_NAMES = {"SR": "syntax rule", "GR": "general rule", "AR": "argument rule", "RV": "returned-value rule"}
#: A numbered rule designator: its kind, the restarted list it names (`GRL2.1`) or its ordinal, and its glued sub-item
#: path (`GR6d.2`: letters and numbers, each optionally after a `.`).
DESIGNATOR = re.compile(rf"(?P<kind>{'|'.join(citation.RULE_KINDS)})(?:L(?P<list>\d+)\.(?P<lord>\d+)|(?P<n>\d+)"
                        r"(?P<path>(?:\.?(?:[a-z]|\d+))*))")
FORMAT_DESIGNATOR = re.compile(r"Format (\d+)")
TABLE_DESIGNATOR = re.compile(r"Table (\d+)")
#: A general format's own label inside its text (`Format 2:`, `<u>Format 2</u>:`).
FORMAT_LABEL = re.compile(r"(?:<u>)?\bFormat\s+(\d+)\b")


@functools.lru_cache(maxsize=1 << 17)  # > the transcription's ~47,500 lines
def norm(s: str) -> str:
    """Compare on words only — dashes, quotes and spacing are typography, not content.

    Memoized, because an in-process caller checks many quotations against ONE loaded standard: `find` normalizes
    every line of the spec per call, and `audit_doc_citations.py` (kb/Work PB2525) spent 25 of its 33 s re-running
    these two regular expressions over the same ~47,500 lines for 59 `find` calls."""
    return re.sub(r"\s+", " ", re.sub(r"[^\w\s]", " ", s)).strip().lower()


def _indent(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


# ── THE PRINTED LABELS OF A RULE AND ITS SUB-ITEMS ───────────────────────────────────────────────────────────────────

@dataclasses.dataclass(frozen=True)
class LabelStyles:
    """An edition's printed rule labels, outermost first: each a pattern whose LAST group is the label's token, and its
    printed form. `indented`: the transcription keeps each level's indentation (ISO), so an unlabelled paragraph at a
    label's own indentation is its parent's; where it does not (1985), an unlabelled paragraph is the label's above."""
    levels: tuple[tuple[re.Pattern[str], str], ...]
    indented: bool


#: The ISO editions': a rule `6)`, its lettered items `d)`, their `2.` items, and their `b.` items.
ISO_LABELS = LabelStyles(((TOP, "{})"), (SUB, "{})"), (SUBSUB, "{}."), (SUBSUBSUB, "{}.")), True)
#: ANSI X3.23-1985's and its amendments': a rule `(6)`, its items `a.`, their items `1)`.
ANSI_LABELS = LabelStyles(((TOP_85, "({})"), (SUB_85, "{}."), (SUBSUB_85, "{})")), False)
#: By the edition's locator shape (citation.EDITIONS): no code names a year.
LABELS = {"clause": ISO_LABELS, "section": ANSI_LABELS, "own": ANSI_LABELS}


def labels_of(edition: int) -> LabelStyles:
    return LABELS[citation.EDITIONS[edition].locator]


def _label(line: str, styles: LabelStyles = ISO_LABELS):
    """(indent, level, token, printed label) for a rule-label line, else None."""
    for level, (pattern, form) in enumerate(styles.levels, start=1):
        if m := pattern.match(line):
            token = m.groups()[-1]
            return _indent(line), level, token, form.format(token)
    return None


def label_path(lines, at, start, styles: LabelStyles = ISO_LABELS) -> list[tuple[int, str, str]]:
    """The PRINTED label path of the line at `at` — e.g. [(1, '6', '6)'), (2, 'd', 'd)'), (3, '2', '2.')] — by walking
    back to each level, no further than `start`.

    Two lines are not what their position suggests, and each used to report a sub-item it is not in (PB308):
      · an editorial note on a correction belongs to the line above it, so it takes that line's path;
      · in an indented transcription, an UNLETTERED paragraph at the sub-items' own indentation (or shallower) is a
        trailing paragraph of the PARENT rule — §2023:14.7.7 rule 2's "The composite of operands is a hypothetical data
        item…" follows its sub-items a) and b) at their indent and is neither — so it reports the bare rule.
    A paragraph INSIDE a sub-item sits deeper than its label, which is how the two are told apart.

    ⛔ A LABEL ABOVE IS AN ANCESTOR ONLY IF ITS LEVEL IS SHALLOWER THAN EVERYTHING ACCEPTED SO FAR (kb/Work PB55 review
    R1-1). A nested `N.` list is transcribed at the SAME indentation as the lettered items around it (§2023:7.2.3.4 9)
    c) 1.–6. sits at the indent of a)–f)), so the nearest `N.` above `d)` belongs to the EARLIER letter `c)`, and
    taking it reported the impossible `9) 6. d)`. A label's depth is its STYLE LEVEL (`styles.levels` order: ISO `N)`
    `x)` `N.` `x.`, 1985 `(N)` `x.` `N)`), never its indentation (kb/Work PB5317): the transcription flattens some
    nested lists onto column 0 (§2023:14.9.39.4 rule 2's `a.` items under `1.`, §2023:14.9.23.4 rule 7's `a)` to `d)`),
    and an indentation-first depth read a flattened `a.` as shallower than the `1.` it belongs to. Walking back, a
    label whose level is not strictly below the last one accepted is a sibling or a sibling's descendant, never an
    ancestor, and is skipped. Indentation decides only who owns an UNLABELLED line (above); a flattened label (column 0,
    below the rule) owns the column-0 lines after it, there being no indentation to say otherwise."""
    if lines[at].startswith(">"):
        top = at
        while top > start and lines[top - 1].startswith(">"):
            top -= 1
        if CORRECTION_NOTE.match(lines[top]):
            at = top
    while at > start and CORRECTION_NOTE.match(lines[at]):
        at -= 1
        while at > start and not lines[at].strip():
            at -= 1
    own = lines[at]
    is_label = _label(own, styles) is not None
    own_indent = _indent(own)
    path = []
    bound = len(styles.levels) + 1   # an unlabelled line is deeper than every label style
    owned = False
    for k in range(at, start - 1, -1):
        hit = _label(lines[k], styles)
        if hit is None:
            continue
        indent, level, token, printed = hit
        if level >= bound:
            continue  # a sibling, or inside one: not on this line's path
        if level == 1:  # the rule itself ends the walk
            path.append((level, token, printed))
            break
        if is_label or owned or own_indent > indent or not styles.indented or indent == 0:
            path.append((level, token, printed))
            owned = True   # inside this label, so inside each of its ancestors
        bound = level
    return list(reversed(path))


def rule_path(lines, at, start, styles: LabelStyles = ISO_LABELS) -> str:
    """The printed rule path of the line at `at`, as the standard prints it: `6) d) 2.`, `(6) b.`."""
    return " ".join(printed for _level, _token, printed in label_path(lines, at, start, styles))


def render_path(tokens: tuple[str, ...]) -> str:
    """A sub-item path in the designator's glued form: ('d', '2') -> 'd.2', ('c',) -> 'c', and a path that opens with a
    number takes a `.` so it is not read as the ordinal's digits: ('1',) -> '.1' (`GR6.1`, never `GR61`)."""
    return ("." if tokens and tokens[0].isdigit() else "") + ".".join(tokens)


# ── THE RULES OF EACH EDITION: THE CATALOG'S BOUNDARIES, ANCHORED IN THE TRANSCRIPTION ───────────────────────────────

@dataclasses.dataclass(frozen=True)
class Rule:
    """One rule: its locator, kind, list and ordinal, the transcription lines it spans, its text, and whether any of its
    text is a reconstruction (`provisional`). `lines` is empty for a catalog row that could not be anchored."""
    edition: int
    locator: str
    kind: str
    ordinal: int
    sublist: int
    lines: tuple[int, ...]
    text: str
    provisional: bool

    @property
    def designator(self) -> str:
        return citation.rule_designator(self.kind, self.ordinal, self.sublist)


@dataclasses.dataclass
class RuleBook:
    """An edition's rules and general formats by locator, and where their boundaries come from: `catalog` (the rule
    catalog's path) or None (LABEL MODE)."""
    edition: int
    catalog: pathlib.Path | None
    lines: list[str]
    regions: list[tuple[str, str, int, int]]
    rules: dict[str, list[Rule]]
    formats: dict[str, str]    # general-format locator -> its text in the transcription
    unanchored: list[str]
    _items: dict = dataclasses.field(default_factory=dict)
    _by_locator: dict = dataclasses.field(default_factory=dict)

    def regions_of(self, locator: str) -> list[tuple[str, str, int, int]]:
        """Every region `locator` names (a 1985 section the transcription heads twice owns two), indexed once."""
        if not self._by_locator:
            for r in self.regions:
                self._by_locator.setdefault(r[0], []).append(r)
        return self._by_locator.get(locator, [])

    @property
    def styles(self) -> LabelStyles:
        return labels_of(self.edition)

    def rule_at(self, locator: str, line: int) -> Rule | None:
        return next((r for r in self.rules.get(locator, ()) if line in r.lines), None)

    def items(self, rule: Rule) -> list[tuple[int, tuple[str, ...], bool]]:
        """(line, sub-item path, is the line that sub-item's label) of every text line of `rule`."""
        if rule not in self._items:
            out = []
            if rule.lines:
                for i in rule.lines:
                    if not self.lines[i].strip() or _furniture(self.lines, i, self.edition):
                        continue
                    path = label_path(self.lines, i, rule.lines[0], self.styles)
                    tokens = tuple(t for level, t, _p in path if level > 1)
                    hit = _label(self.lines[i], self.styles)
                    out.append((i, tokens, hit is not None and hit[1] > 1 and bool(tokens) and tokens[-1] == hit[2]))
            self._items[rule] = out
        return self._items[rule]

    def item_paths(self, rule: Rule) -> list[tuple[str, ...]]:
        """Every sub-item path `rule` prints, in order."""
        return list(dict.fromkeys(t for _i, t, is_label in self.items(rule) if is_label))

    def text_of(self, rule: Rule, path: tuple[str, ...] = ()) -> list[str]:
        """The normalized texts a quotation of `rule` (or of its sub-item `path`) is looked for in: the catalog's text
        of the rule and its own lines (which carry a correction's printed form), or the sub-item's lines."""
        if path:
            return [norm(" ".join(self.lines[i] for i, t, _l in self.items(rule) if t[:len(path)] == path))]
        own = norm(" ".join(self.lines[i] for i in rule.lines if not _furniture(self.lines, i, self.edition)))
        return [norm(rule.text), own]

    def where(self, locator: str, needle: str) -> str | None:
        """The designator (with the deepest sub-item path) of the rule of `locator` whose text holds `needle`."""
        for rule in self.rules.get(locator, ()):
            if any(needle in t for t in self.text_of(rule)):
                for path in sorted(self.item_paths(rule), key=len, reverse=True):
                    if needle in self.text_of(rule, path)[0]:
                        return rule.designator + render_path(path)
                return rule.designator
        return None


def _extractors():
    """The rule catalog's two extractors, imported on first use: each imports this module's note shapes."""
    import extract_rule_catalog  # noqa: PLC0415
    import rule_catalog_1985  # noqa: PLC0415
    return extract_rule_catalog, rule_catalog_1985


def _furniture(lines: list[str], i: int, edition: int) -> bool:
    """Page-break furniture inside a rule (a page marker, an anchor, a folio, a running header or footer): never text.
    The shapes are the extractors' (`FURNITURE`, `RUNNING_HEADER`, `PAGE`; the 1985 page's running header,
    `rule_catalog_1985.running_header`, the first line after its page marker)."""
    erc, rc85 = _extractors()
    line = lines[i]
    if erc.FURNITURE.match(line) or erc.RUNNING_HEADER.match(line) or erc.PAGE.match(line) or citation.FOLIO_85.match(line):
        return True
    if citation.EDITIONS[edition].locator == "clause":
        return False
    k = i - 1
    while k >= 0 and not lines[k].strip():
        k -= 1
    return k >= 0 and bool(citation.PAGE_85.match(lines[k])) and rc85.running_header(line)


def catalog_path(edition: int, repo: pathlib.Path = spec_pdf.REPO) -> pathlib.Path | None:
    """The rule catalog of `edition` (an amendment's is its base standard's), as the inventory schema names it, or None
    while the schema names none (LABEL MODE). A catalog the schema names and the tree lacks is a broken tree: exit."""
    source = citation.SOURCE_EDITION.get(edition, edition)
    named = next((e.catalog for e in inventory_schema.EDITIONS if e.year == source and e.catalog), None)
    if named is None:
        return None
    path = repo / named
    if not path.is_file():
        sys.exit(f"FATAL: the {source} rule catalog {named} the inventory schema names is not in this tree")
    return path


@functools.lru_cache(maxsize=None)
def rule_book(edition: int, repo: pathlib.Path = spec_pdf.REPO, catalog: pathlib.Path | bool | None = None) -> RuleBook:
    """THE rules of `edition` (module docstring): the catalog's rows anchored in the transcription, or, with no catalog,
    LABEL MODE. `catalog` overrides the schema's: a path (a planted catalog in the self-test), or False for LABEL MODE."""
    lines, regions = load(edition, repo)
    if catalog is None:
        catalog = catalog_path(edition, repo)
    if not catalog:
        return _label_book(edition, lines, regions)
    erc, rc85 = _extractors()
    ordinal = erc.ORDINAL if citation.EDITIONS[edition].locator == "clause" else rc85.ORDINAL
    source = citation.SOURCE_EDITION.get(edition, edition)
    rows = [r for r in json.loads(catalog.read_text(encoding="utf-8"))["rules"]
            if (citation.parse(r["citation"]).edition if "citation" in r else source) == edition]
    spans: dict[str, list[int]] = {}
    for loc, _t, s, e in regions:
        spans.setdefault(loc, []).extend(range(s, e))
    rules: dict[str, list[Rule]] = {}
    formats: dict[str, str] = {}
    unanchored: list[str] = []
    by_loc: dict[str, list[dict]] = {}
    for r in rows:
        loc = r.get("locator") or r["section"]
        if r["kind"] == "FMT":
            # The catalog decides which clause prints a general format; its text is read whole from the transcription
            # (the row's `text` is cut at 4,000 characters, and its `formats` count misses a bold `**Format n**` label).
            formats[loc] = "\n".join(lines[i] for i in spans.get(loc, []))
        elif r["kind"] in KIND_NAMES:
            by_loc.setdefault(loc, []).append(r)
    for loc, rs in by_loc.items():
        ids = spans.get(loc, [])
        anchors: list[tuple[int, dict]] = []
        pos = 0
        for r in rs:
            at = _anchor(lines, ids, pos, r, ordinal)
            if at is None:
                unanchored.append(r["id"])
                rules.setdefault(loc, []).append(Rule(edition, loc, r["kind"], r["ordinal"], r["sublist"], (), r["text"],
                                                      r.get("provisional", "none") != "none"))
                continue
            anchors.append((at, r))
            pos = at + 1
        for k, (at, r) in enumerate(anchors):
            end = anchors[k + 1][0] if k + 1 < len(anchors) else len(ids)
            rules.setdefault(loc, []).append(Rule(edition, loc, r["kind"], r["ordinal"], r["sublist"], tuple(ids[at:end]),
                                                  r["text"], r.get("provisional", "none") != "none"))
    return RuleBook(edition, catalog, lines, regions, rules, formats, unanchored)


def _anchor(lines: list[str], ids: list[int], pos: int, row: dict, ordinal: re.Pattern[str]) -> int | None:
    """The position in `ids` (a clause's lines) of the line that opens catalog row `row`, from `pos` on: the line the
    extractor's `ordinal` reads as that rule's number with the row's first words; else, for a row of a prose clause's
    paragraphs (no printed number), the unlabelled line its text opens with."""
    head = norm(row["text"]).split()[:6]
    for j in range(pos, len(ids)):
        m = ordinal.match(lines[ids[j]])
        if m and int(m["n"]) == row["ordinal"]:
            words = norm(m["text"]).split()
            k = min(len(words), len(head))
            if words[:k] == head[:k]:
                return j
    for j in range(pos, len(ids)):
        line = lines[ids[j]]
        words = norm(line).split()
        k = min(len(words), len(head))
        if k >= min(3, len(head)) and words[:k] == head[:k] and not ordinal.match(line):
            return j
    return None


def _label_book(edition: int, lines: list[str], regions) -> RuleBook:
    """LABEL MODE (module docstring): a clause the catalog's kind table types by its heading is a rule block, its rules
    run from one column-0 rule label to the next, a label that does not continue the run opens the next list, and a
    clause headed as a general format is one."""
    erc, _rc85 = _extractors()
    top = labels_of(edition).levels[0][0]
    rules: dict[str, list[Rule]] = {}
    formats: dict[str, str] = {}
    for loc, title, s, e in regions:
        if GENERAL_FORMAT_TITLE.fullmatch(title.strip()):
            formats[loc] = "\n".join(lines[s + 1:e])
        kind = erc.kind_of(title)
        if kind is None:
            continue
        labels = [(i, int(m.groups()[-1])) for i in range(s + 1, e) if (m := top.match(lines[i]))]
        sublist, last = 1, 0
        for k, (i, n) in enumerate(labels):
            if n != last + 1 and k:
                sublist += 1
            last = n
            end = labels[k + 1][0] if k + 1 < len(labels) else e
            ids = tuple(range(i, end))
            text = " ".join(lines[j] for j in ids if not _furniture(lines, j, edition))
            rules.setdefault(loc, []).append(Rule(edition, loc, kind, n, sublist, ids, text,
                                                  any(in_reconstruction(lines, j) for j in ids[:1])))
    return RuleBook(edition, None, lines, list(regions), rules, formats, [])


# ── THE ONE CHECKER ──────────────────────────────────────────────────────────────────────────────────────────────────

@dataclasses.dataclass(frozen=True)
class Verdict:
    """`kind` is "ok" or a FAILURE_KINDS key; `suggestion` the citation the text (or the rule) really is, when found."""
    kind: str
    message: str
    suggestion: str | None = None
    excerpt: str = ""

    @property
    def ok(self) -> bool:
        return self.kind == "ok"


@functools.lru_cache(maxsize=None)
def _held(edition: int, repo: pathlib.Path) -> pathlib.Path | None:
    """`citation.transcription`, asked of the disk once per edition: the audits resolve citations by the ten thousand."""
    return citation.transcription(edition, repo)


def _hit(lines: list[str], regions, needle: str) -> int | None:
    """The first line of `regions` holding `needle`, or, for a quotation that runs across lines, the line it opens on."""
    for _n, _t, s, e in regions:
        for i in range(s, e):
            if needle in norm(lines[i]):
                return i
    for _n, _t, s, e in regions:
        joined = [(i, norm(lines[i])) for i in range(s, e) if lines[i].strip()]
        whole = " ".join(t for _i, t in joined)
        at = whole.find(needle)
        if at >= 0:
            seen = 0
            for i, t in joined:
                seen += len(t) + 1
                if seen > at:
                    return i
    return None


def _elsewhere(book: RuleBook, cited: citation.Citation, needle: str, text: str) -> Verdict:
    """The quotation is not where the citation says: where in the edition it is, if anywhere."""
    shown = citation.render(cited)
    for loc, _t, s, e in book.regions:
        if loc == cited.locator or loc.startswith("p."):
            continue
        at = _hit(book.lines, [(loc, _t, s, e)], needle)
        if at is not None:
            rule = book.rule_at(loc, at)
            where = f"§{cited.edition}:{loc}" + (f" {book.where(loc, needle) or rule.designator}" if rule else "")
            return Verdict("text-not-in-clause", f"{shown}: {text!r} is not in that clause; it is in {where}", where)
    return Verdict("text-not-found", f"{shown}: {text!r} is nowhere in the {cited.edition} transcription "
                                     "(run --find on a fragment of it)")


def resolve(cited: citation.Citation, text: str | None = None, repo: pathlib.Path = spec_pdf.REPO,
            catalog: pathlib.Path | bool | None = None) -> Verdict:
    """THE ONE CHECKER of a citation (module docstring): every part of it, and the quotation `text` inside the cited
    unit. With `text` None the citation's STRUCTURE alone is checked (the clause, the rule list, the ordinal, the
    sub-item, the format or the table): the citation audit's form for a citation that quotes nothing."""
    shown = citation.render(cited)
    clause_shown = citation.render(dataclasses.replace(cited, rule=None))
    if _held(cited.edition, repo) is None:
        return Verdict("edition-not-held", f"the {cited.edition} transcription is not in this tree, so nothing of {shown} "
                                           "can be checked")
    book = rule_book(cited.edition, repo, catalog)
    regions = book.regions_of(cited.locator)
    if not regions:
        return Verdict("no-clause", citation.validate(cited, repo) or f"there is no {clause_shown} in the "
                                                                       f"{cited.edition} transcription")
    title = regions[0][1]
    needle = None
    if text is not None:
        needle = norm(text)
        if not needle:
            return Verdict("quote-too-short", EMPTY_NEEDLE)
        real = real_words(needle)
        if len(real) < MIN_WORDS:
            return Verdict("quote-too-short", f"{text!r} has {len(real)} real word(s); a quotation is checked on at least "
                                              f"{MIN_WORDS}, because a shorter one occurs in almost any clause")
    mode = "" if book.catalog is not None else f" {LABEL_MODE_MARK}"
    if cited.rule is None:
        if needle is None:
            return Verdict("ok", f"{shown}  ({title})  the clause exists; no quotation checked")
        at = _hit(book.lines, regions, needle)
        if at is None:
            return _elsewhere(book, cited, needle, text)
        mark = f" {RECONSTRUCTED_MARK}" if in_reconstruction(book.lines, at) else ""
        rule = book.rule_at(cited.locator, at)
        under = f"; it lies in {book.where(cited.locator, needle) or rule.designator}{mode}" if rule else ""
        return Verdict("ok", f"{shown}  ({title})  the text is in the clause{under}{mark}", excerpt=book.lines[at].strip()[:220])
    if m := DESIGNATOR.fullmatch(cited.rule):
        return _resolve_rule(book, cited, m, needle, text, title, regions, mode)
    if m := FORMAT_DESIGNATOR.fullmatch(cited.rule):
        return _resolve_format(book, cited, int(m[1]), needle, text, mode)
    if m := TABLE_DESIGNATOR.fullmatch(cited.rule):
        return _resolve_table(book, cited, int(m[1]), needle, text)
    return Verdict("designator-not-understood", f"{shown}: {cited.rule!r} is no designator form the checker reads "
                                                "(SRn GRn ARn RVn, GRLm.n, a glued sub-item path GR6d.2, Format n, Table n)")


def _lists(rules: list[Rule]) -> str:
    """`GR1-GR12` (`GRL2.1-GRL2.4` for a restarted list, `GR1, GR3-GR4` where a mixed block gives a number to another
    kind): the rules of one kind a block has, as runs of consecutive ordinals."""
    out = []
    for sub in sorted({r.sublist for r in rules}):
        mine = sorted((r for r in rules if r.sublist == sub), key=lambda r: r.ordinal)
        run = [mine[0]]
        for r in mine[1:] + [None]:
            if r is not None and r.ordinal == run[-1].ordinal + 1:
                run.append(r)
                continue
            out.append(f"{run[0].designator}-{run[-1].designator}" if len(run) > 1 else run[0].designator)
            if r is not None:
                run = [r]
    return ", ".join(out)


def _resolve_rule(book: RuleBook, cited, m: re.Match, needle, text, title, regions, mode) -> Verdict:
    shown, loc = citation.render(cited), cited.locator
    clause_shown = citation.render(dataclasses.replace(cited, rule=None))
    kind, sub = m["kind"], int(m["list"] or 1)
    n = int(m["lord"] or m["n"])
    path = tuple(re.findall(r"[a-z]|\d+", m["path"] or ""))
    in_clause = book.rules.get(loc, [])
    of_kind = [r for r in in_clause if r.kind == kind]
    if not of_kind:
        # The block the author meant: a subclause's (`§2023:14.9.39 SR17` is 14.9.39.3's) or, when exactly one has it,
        # a sibling's (`§2023:13.18.40.2 SR13`, the general format's clause, is 13.18.40.3's).
        parent = loc.rsplit(".", 1)[0] if "." in loc else None
        near = [r for k, rs in book.rules.items()
                if k.startswith(loc + ".") or (parent and k != loc and k.rsplit(".", 1)[0] == parent)
                for r in rs if r.kind == kind and r.sublist == sub and r.ordinal == n]
        has = sorted({KIND_NAMES[r.kind] + "s" for r in in_clause})
        hint = f" (it has {' and '.join(has)})" if has else " (no numbered rule of any kind)"
        child = next((r for r in near if r.locator.startswith(loc + ".")), None) or (near[0] if len(near) == 1 else None)
        if child is not None:
            where = f"§{cited.edition}:{child.locator} {cited.rule}"
            how = "subclause" if child.locator.startswith(loc + ".") else "sibling clause"
            return Verdict("no-rule-list", f"{clause_shown} has no {KIND_NAMES[kind]}s{hint}; its {how} "
                                           f"{child.locator} has {child.designator}: cite {where}{mode}", where)
        return Verdict("no-rule-list", f"{clause_shown} has no {KIND_NAMES[kind]}s{hint}{mode}")
    rule = next((r for r in of_kind if r.sublist == sub and r.ordinal == n), None)
    if rule is None:
        other = next((r for r in in_clause if r.sublist == sub and r.ordinal == n), None)
        said = (f"; its rule {n} is a {KIND_NAMES[other.kind]} (the catalog types this block's rules one by one): cite "
                f"{clause_shown} {other.designator}" if other is not None else "")
        return Verdict("no-ordinal", f"{clause_shown} has no {citation.rule_designator(kind, n, sub)}: its {KIND_NAMES[kind]}s are "
                                     f"{_lists(of_kind)} ({len(of_kind)} in all){said}{mode}",
                       f"{clause_shown} {other.designator}" if other is not None else None)
    if not rule.lines and (path or needle is not None):
        return Verdict("unanchored", f"{shown}: the catalog row {rule.designator} of {loc} was not found in the "
                                     f"transcription ({book.catalog}): the catalog has drifted; regenerate it")
    if path:
        have = book.item_paths(rule)
        if path not in have:
            peers = [p for p in have if len(p) == len(path) and p[:-1] == path[:-1]]
            said = (f"its sub-items there are {', '.join(rule.designator + render_path(p) for p in peers)}" if peers
                    else "it has no sub-item at that level" if have else "it has no sub-items")
            return Verdict("no-sub-item", f"{clause_shown} {rule.designator} has no sub-item {render_path(path)}: "
                                          f"{said}{mode}")
    mark = f" {RECONSTRUCTED_MARK}" if rule.provisional else ""
    unit = rule.designator + render_path(path)
    if needle is None:
        return Verdict("ok", f"{shown}  ({title})  {unit} exists; no quotation checked{mode}{mark}")
    if any(needle in t for t in book.text_of(rule, path)):
        return Verdict("ok", f"{shown}  ({title})  the text is in {unit}{mode}{mark}", excerpt=rule.text[:220])
    if (where := book.where(loc, needle)) is not None:
        return Verdict("text-not-in-rule", f"{clause_shown} has {text!r} under {where}, not {cited.rule}{mode}",
                       f"{clause_shown} {where}")
    if _hit(book.lines, regions, needle) is not None:
        return Verdict("text-not-in-rule", f"{clause_shown} has {text!r}, but under no rule (its lead-in, a note or a "
                                           f"table), not {cited.rule}{mode}", clause_shown)
    return _elsewhere(book, cited, needle, text)


def _resolve_format(book: RuleBook, cited, n: int, needle, text, mode) -> Verdict:
    loc, shown = cited.locator, citation.render(cited)
    found = [body for k, body in book.formats.items() if k == loc or k.rsplit(".", 1)[0] == loc]
    if not found:
        parent = loc.rsplit(".", 1)[0]
        if "." in loc and any(k.rsplit(".", 1)[0] == parent for k in book.formats):
            where = f"§{cited.edition}:{parent} Format {n}"
            return Verdict("no-format", f"{shown}: {loc} prints no general format; its statement's general format is "
                                        f"{parent}'s: cite {where}{mode}", where)
        return Verdict("no-format", f"{shown}: {loc} prints no general format, nor does a general-format subclause of it{mode}")
    body = found[0]
    starts = [(int(x[1]), x.start()) for x in FORMAT_LABEL.finditer(body)]
    count = len({k for k, _p in starts}) or 1
    if n > count:
        return Verdict("no-format", f"{shown}: the general format of {loc} prints {count} format(s){mode}")
    if needle is None:
        return Verdict("ok", f"{shown}  format {n} of {count} exists; no quotation checked{mode}")
    segments = {k: body[p:(starts[j + 1][1] if j + 1 < len(starts) else len(body))] for j, (k, p) in enumerate(starts)}
    if needle in norm(segments.get(n, body if count == 1 else "")):
        return Verdict("ok", f"{shown}  the text is in format {n} of {count}{mode}")
    other = next((k for k, seg in segments.items() if needle in norm(seg)), None)
    if other is not None:
        return Verdict("text-not-in-rule", f"{shown}: {text!r} is in Format {other}, not Format {n}{mode}",
                       f"§{cited.edition}:{loc} Format {other}")
    return _elsewhere(book, cited, needle, text)


def _resolve_table(book: RuleBook, cited, n: int, needle, text) -> Verdict:
    """A table is no catalog row: its extent is its caption and the table rows that follow it."""
    loc, shown = cited.locator, citation.render(cited)
    caption = re.compile(rf"^\W*Table\s+{n}\b(?![.\d])")
    for k, _t, s, e in book.regions:
        if k != loc and not k.startswith(loc + "."):
            continue
        for i in range(s, e):
            if not caption.match(book.lines[i]):
                continue
            end, seen = e, False
            for j in range(i + 1, e):
                if book.lines[j].startswith("|"):
                    seen, end = True, j + 1
                elif seen and book.lines[j].strip():
                    break
            if needle is None:
                return Verdict("ok", f"{shown}  table {n} is captioned in {k}; no quotation checked")
            if needle in norm(" ".join(book.lines[i:end])):
                return Verdict("ok", f"{shown}  the text is in table {n} ({k})", excerpt=book.lines[i].strip()[:220])
            return Verdict("text-not-in-rule", f"{shown}: {text!r} is not in table {n} ({k}, lines {i + 1}-{end})")
    where = next((k for k, _t, s, e in book.regions if not k.startswith("p.")
                  for i in range(s, e) if caption.match(book.lines[i])), None)
    if where is not None:
        return Verdict("no-table", f"{shown}: no table {n} is captioned in {loc} or under it; it is captioned in {where}",
                       f"§{cited.edition}:{where} Table {n}")
    return Verdict("no-table", f"{shown}: no table {n} is captioned in {loc} or under it, nor anywhere in the "
                               f"{cited.edition} transcription")


def check(cited: citation.Citation, text: str, repo: pathlib.Path = spec_pdf.REPO,
          catalog: pathlib.Path | bool | None = None) -> tuple[int, list[str]]:
    """(exit code, output lines) for `--check`: `resolve`'s verdict as the CLI prints it."""
    v = resolve(cited, text, repo, catalog)
    if v.ok:
        return 0, [f"OK  {v.message}" + (f"\n    {v.excerpt}" if v.excerpt else "")]
    out = [f"FAIL [{v.kind}]: {v.message}"]
    if v.suggestion:
        out.append(f"      the right citation: {v.suggestion}")
    elif v.kind == "text-not-found":
        out.append("      run --find to locate the text and get the clause it is really in")
    return 1, out


def find(lines, cls, text, edition: int):
    """(exit code, output lines) for `--find`: every region line of `edition`'s transcription that contains TEXT."""
    needle = norm(text)
    if not needle:
        return 1, [f"REFUSED: {EMPTY_NEEDLE}"]
    hits = [(n, t, s, e, i) for (n, t, s, e) in cls
            for i in range(s, e) if needle in norm(lines[i])]
    if not hits:
        return 1, [f"NO CLAUSE contains {text!r} — the citation names text the standard does not have"]
    styles = labels_of(edition)
    out = []
    for n, t, s, _e, i in hits[:12]:
        mark = f" {RECONSTRUCTED_MARK}" if in_reconstruction(lines, i) else ""
        out.append(f"§{edition}:{n} {rule_path(lines, i, s, styles)}  ({t}){mark}\n    {lines[i].strip()[:190]}\n")
    out.append(f"{len(hits)} occurrence(s)")
    return 0, out


def load(edition: int, repo: pathlib.Path = spec_pdf.REPO):
    """The lines and citable regions of `edition`'s transcription (`citation.regions`); exits while it is absent."""
    loaded = citation.regions(edition, repo)
    if loaded is None:
        source = citation.SOURCE_EDITION.get(edition, edition)
        where = f"({spec_pdf.markdown(source, repo).relative_to(repo).as_posix()}) " if source in spec_pdf.EDITIONS else ""
        sys.exit(f"FATAL: the {edition} transcription {where}does not exist in this tree.")
    return loaded


# ── SELF-TEST ────────────────────────────────────────────────────────────────────────────────────────────────────────

#: A planted ISO transcription and its catalog (kb/Work PB5317): the catalog mode's arms in an edition whose real catalog
#: is not in the tree yet (2002), and LABEL MODE's on the same text.
_PLANTED_ISO = """## 9.9 Planted statement

### 9.9.3 Syntax rules

1\\) The first planted syntax rule says one thing clearly.

2\\) The second planted syntax rule says another thing:

   a) its first planted item reads like this one;

   b) its second planted item reads like that one.

### 9.9.4 General rules

1\\) The planted general rule governs everything in this planted clause.
"""
_PLANTED_ROWS = [
    {"id": "SR-9.9.3-1", "section": "9.9.3", "kind": "SR", "ordinal": 1, "sublist": 1,
     "text": "The first planted syntax rule says one thing clearly."},
    {"id": "SR-9.9.3-2", "section": "9.9.3", "kind": "SR", "ordinal": 2, "sublist": 1,
     "text": "The second planted syntax rule says another thing: a) its first planted item reads like this one; "
             "b) its second planted item reads like that one."},
    {"id": "GR-9.9.4-1", "section": "9.9.4", "kind": "GR", "ordinal": 1, "sublist": 1,
     "text": "The planted general rule governs everything in this planted clause."},
]


def self_test() -> int:
    """Each guard is shown to FIRE on its defect and to stay SILENT on its neighbour (kb/Work PB308, PB309, PB5317), in
    every edition the tree holds: 1985 and 2023 against their catalogs, 2002 and 2014 in LABEL MODE, and a planted 2002
    catalog for the catalog mode of an ISO edition other than 2023."""
    lines, cls = load(2023)

    def c(text: str) -> citation.Citation:
        return citation.parse(text)

    def v(cite_: str, text: str | None = None, **kw) -> Verdict:
        return resolve(c(cite_), text, **kw)

    def fails(cite_: str, text: str | None, kind: str, names: str | None = None, **kw) -> bool:
        got = v(cite_, text, **kw)
        return got.kind == kind and (names is None or (got.suggestion or "").endswith(names) or names in got.message)

    q85 = "The input procedure may consist of any procedure needed to select, modify, or copy the records"
    q85b = "The file referenced by file-name-1 is sequenced"
    q23 = "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value"
    q02 = "For the purpose of determining its length, identifier-1 is treated as if it were a sending data item"
    q14 = "If LEFT is specified, integer-1 is the leftmost column of the printable item"
    with tempfile.TemporaryDirectory() as td:
        planted = pathlib.Path(td)
        planted_md = spec_pdf.markdown(2002, planted)
        planted_md.parent.mkdir()
        planted_md.write_text(_PLANTED_ISO, encoding="utf-8", newline="\n")
        cat = planted / "catalog-2002.json"
        cat.write_text(json.dumps({"rules": _PLANTED_ROWS}), encoding="utf-8", newline="\n")
        drifted = planted / "drifted-2002.json"
        drifted.write_text(json.dumps({"rules": [dict(_PLANTED_ROWS[0], text="A row the transcription does not carry.")]}),
                           encoding="utf-8", newline="\n")
        pc = {"repo": planted, "catalog": cat}
        pl = {"repo": planted, "catalog": False}
        planted_cases = [
            ("2002 catalog mode: GR99 fails, naming the count", fails("§2002:9.9.4 GR99", None, "no-ordinal", "GR1 (1 in all)", **pc)),
            ("2002 catalog mode: a real quotation under the wrong ordinal fails, naming the right one",
             fails("§2002:9.9.3 SR1", "The second planted syntax rule says another thing", "text-not-in-rule", "SR2", **pc)),
            ("2002 catalog mode: the right ordinal passes, unmarked",
             v("§2002:9.9.3 SR2", "The second planted syntax rule says another thing", **pc).ok
             and LABEL_MODE_MARK not in v("§2002:9.9.3 SR2", "The second planted syntax rule says another thing", **pc).message),
            ("2002 catalog mode: a sub-item's text under the wrong letter fails, naming the letter",
             fails("§2002:9.9.3 SR2a", "its second planted item reads like that one", "text-not-in-rule", "SR2b", **pc)
             and v("§2002:9.9.3 SR2b", "its second planted item reads like that one", **pc).ok),
            ("2002 catalog mode: a one-character quotation fails",
             fails("§2002:9.9.3 SR1", "x", "quote-too-short", **pc)),
            ("2002 catalog mode: a clause with no general rules cited GR1 fails",
             fails("§2002:9.9.3 GR1", "The first planted syntax rule says one thing clearly", "no-rule-list", **pc)),
            ("2002 catalog mode: a catalog row the transcription does not carry fails as unanchored, never OK",
             fails("§2002:9.9.3 SR1", "The first planted syntax rule says one thing clearly", "unanchored",
                   repo=planted, catalog=drifted)),
            ("2002 LABEL MODE on the same text: the same verdicts, each marked",
             fails("§2002:9.9.3 SR1", "The second planted syntax rule says another thing", "text-not-in-rule", "SR2", **pl)
             and LABEL_MODE_MARK in v("§2002:9.9.3 SR2", "The second planted syntax rule says another thing", **pl).message
             and fails("§2002:9.9.4 GR99", None, "no-ordinal", **pl)),
            ("reads the transcription of the edition the citation names (2002), not 2023's",
             check(c("§2002:9.9.4"), "The planted general rule governs everything", repo=planted)[0] == 0),
        ]
        try:
            load(1985, planted)
            absent_1985 = False
        except SystemExit as ex:
            absent_1985 = "1985" in str(ex.code)
        not_held = v("§1985:VI.5.3.1", "A data description entry specifies the characteristics", repo=planted).kind
    planted_notes = ["printed", "> ⚠ **RECONSTRUCTED — not printed in our copy (folio XIII-54).** Sources.", "edited",
                     "> ⚠ **END RECONSTRUCTED (folio XIII-54).** Printed again.", "printed",
                     "> ⚠ **RECONSTRUCTED — not printed in our copy (folios XIII-30 and XIII-31).**", "unclosed"]
    held = [(e, rule_book(e)) for e in sorted(spec_pdf.EDITIONS) if citation.transcription(e) is not None]
    cases = planted_cases + [
        ("an edition whose transcription is absent exits naming it, and resolve says it is not held",
         absent_1985 and not_held == "edition-not-held"),
        # kb/Work PB5317: the 1985 rule number, the arms the old checker passed.
        ("1985: GR99 fails, naming how many general rules there are",
         fails("§1985:XI.4.4.4 GR99", q85, "no-ordinal", "GR1-GR14 (14 in all)")),
        ("1985: a real quotation under the wrong ordinal (GR10) fails, naming the right one (GR7)",
         fails("§1985:XI.4.4.4 GR10", q85, "text-not-in-rule", "GR7")),
        ("1985: the right ordinal passes", v("§1985:XI.4.4.4 GR7", q85).ok),
        ("1985: a one-character quotation fails", fails("§1985:XI.4.4.4 GR7", "x", "quote-too-short")),
        ("1985: a paragraph with no general rules cited GR1 fails", fails("§1985:XI.4.4.3 GR1", q85, "no-rule-list")),
        ("1985: a sub-item's text under the wrong letter fails naming its letter, the right one passes, a missing one fails",
         fails("§1985:XI.4.4.4 GR6c", q85b, "text-not-in-rule", "GR6b") and v("§1985:XI.4.4.4 GR6b", q85b).ok
         and fails("§1985:XI.4.4.4 GR6d", None, "no-sub-item", "GR6a, GR6b, GR6c")),
        ("checks a 1985 citation against the 1985 transcription",
         v("§1985:VI.5.3.1", "A data description entry specifies the characteristics").ok),
        ("fires on a 1985 paragraph of the wrong section",
         not v("§1985:VII.5.3.1", "A data description entry specifies the characteristics").ok),
        ("1989 (Amendment 1): a returned-value rule resolves in its own section, a missing one fails",
         v("§1989:A.2.5.4 RV1").ok and fails("§1989:A.2.5.4 RV99", None, "no-ordinal")),
        # kb/Work PB4952: a quotation of a reconstruction passes MARKED; the printed text beside it carries no mark.
        ("marks a citation verified against a reconstruction (TYPE SR1, folio XIII-55)",
         RECONSTRUCTED_MARK in v("§1985:XIII.3.20.3 SR1", "RH is an abbreviation for REPORT HEADING").message
         and RECONSTRUCTED_MARK in v("§1985:XIII.3.20.3", "RH is an abbreviation for REPORT HEADING").message),
        ("does not mark the printed text after a reconstruction (TYPE SR7, folio XIII-56)",
         RECONSTRUCTED_MARK not in v("§1985:XIII.3.20.3", "The description of a report must include at least one body group").message),
        ("pairs the notes, and reports an unclosed opening and an unopened closing",
         [(r.start, r.end) for r in reconstructions(planted_notes)] == [(1, 3), (5, None)]
         and [(r.start, r.end) for r in reconstructions(planted_notes[3:4])] == [(-1, 0)]
         and reconstructed(planted_notes) == [None, "XIII-54", "XIII-54", "XIII-54", None, "XIII-30", "XIII-30"]),
        # 2023: the same arms.
        ("2023: GR99 fails, naming the count", fails("§2023:14.9.8.4 GR99", q23, "no-ordinal", "GR1-GR3 (3 in all)")),
        ("2023: a real quotation under the wrong ordinal fails, naming the rule and sub-item it is in",
         fails("§2023:14.9.8.4 GR2", q23, "text-not-in-rule", "GR1b")),
        ("2023: the right ordinal passes, and its sub-item", v("§2023:14.9.8.4 GR1", q23).ok and v("§2023:14.9.8.4 GR1b", q23).ok),
        ("2023: the wrong sub-item fails naming the right one; a missing one fails",
         fails("§2023:14.9.8.4 GR1a", q23, "text-not-in-rule", "GR1b") and fails("§2023:14.9.8.4 GR1z", None, "no-sub-item")),
        ("2023: a one-character quotation fails", fails("§2023:14.9.8.4 GR1", "x", "quote-too-short")),
        ("2023: a clause with no syntax rules cited SR1 fails", fails("§2023:14.9.8.4 SR1", q23, "no-rule-list")),
        ("2023: a statement's clause cited for its general rule names the subclause that has them",
         fails("§2023:14.9.8 GR1", q23, "no-rule-list", "§2023:14.9.8.4 GR1")),
        ("2023: the general format's clause cited for a syntax rule names the sibling that has it",
         fails("§2023:13.18.40.2 SR13", None, "no-rule-list", "§2023:13.18.40.3 SR13")),
        ("2023: a list the transcription flattened onto column 0 keeps its levels (14.9.39.4 2) a) 1. a.)",
         v("§2023:14.9.39.4 GR2a1a", "the EC-BOUND-SUBSCRIPT exception condition is set to exist").ok
         and fails("§2023:14.9.39.4 GR2a3b", "If index-name-2 is associated with the same table as index-name-1",
                   "text-not-in-rule", "GR2a.3.a")),
        ("2023: a table cited under the wrong clause names the clause that captions it",
         fails("§2023:9.1.15 Table 19", None, "no-table", "§2023:14.9.27.4 Table 19") and v("§2023:14.9.27.4 Table 19").ok),
        ("2023: a rule of a mixed block cited under the wrong kind names its kind (14.7.7 rule 2 is SR2)",
         fails("§2023:14.7.7 GR2", None, "no-ordinal", "§2023:14.7.7 SR2") and v("§2023:14.7.7 SR2").ok),
        ("2023: a format the statement prints passes, one it does not fails",
         v("§2023:14.9.39 Format 4").ok and fails("§2023:14.9.39 Format 40", None, "no-format")),
        ("2023: a designator form the checker does not read fails by name, never on the clause alone",
         fails("§2023:14.9.8.4 GR1..2", q23, "designator-not-understood")),
        # 2002 and 2014 have no rule catalog in this tree: LABEL MODE, every verdict marked.
        ("2002 LABEL MODE: GR99 fails", fails("§2002:14.8.21.3 GR99", q02, "no-ordinal")),
        ("2002 LABEL MODE: the wrong ordinal fails naming GR1; GR1 passes, marked",
         fails("§2002:14.8.21.3 GR2", q02, "text-not-in-rule", "GR1")
         and v("§2002:14.8.21.3 GR1", q02).ok and LABEL_MODE_MARK in v("§2002:14.8.21.3 GR1", q02).message),
        ("2002 LABEL MODE: a one-character quotation fails; a clause with no syntax rules cited SR1 fails",
         fails("§2002:14.8.21.3 GR1", "x", "quote-too-short") and fails("§2002:14.8.21.3 SR1", q02, "no-rule-list")),
        ("2014 LABEL MODE: GR6c with item b)'s text fails naming GR6b (the reviewer's case); GR6b passes",
         fails("§2014:13.18.14.3 GR6c", q14, "text-not-in-rule", "GR6b") and v("§2014:13.18.14.3 GR6b", q14).ok),
        ("2014 LABEL MODE: SR1 with SR2's text fails; SR2 passes",
         fails("§2014:14.9.23.2 SR1", "File-name-1 shall be described in a sort-merge file description entry",
               "text-not-in-rule", "SR2")
         and v("§2014:14.9.23.2 SR2", "File-name-1 shall be described in a sort-merge file description entry").ok),
        ("every row of every held catalog is anchored in its transcription (the drift test)",
         all(not b.unanchored for _e, b in held) and any(b.catalog for _e, b in held)),
        ("fires EMPTY-NEEDLE (check)", fails("§2023:7.1", "| + | + | - |", "quote-too-short", EMPTY_NEEDLE)),
        ("fires EMPTY-NEEDLE (find)", find(lines, cls, "| + | - |", 2023)[0] == 1),
        ("fires on a table row of two real words", fails("§2023:15.43.4", "| S999 | +999 |", "quote-too-short")),
        ("pins 14.9.8.4 1) b)", find(lines, cls, q23, 2023)[1][0].startswith("§2023:14.9.8.4 1) b)  (")),
        ("pins 14.7.7 2) bare (trailing paragraph)",
         find(lines, cls, "The composite of operands is a hypothetical data item", 2023)[1][0].startswith("§2023:14.7.7 2)  (")),
        # PB55 review R1-1: a letter AFTER a nested `N.` list at its own indent is the list's sibling, not inside it
        ("pins 7.2.3.4 9) d) (letter after a nested list)",
         find(lines, cls, "If no match occurs, the comparison is repeated with each", 2023)[1][0].startswith("§2023:7.2.3.4 9) d)  (")),
        ("pins 12.3.7.3 17) d) (letter after a nested list)",
         find(lines, cls, "Alphabet-name-4 shall not reference an alphabet specified with the LOCALE phrase", 2023)[1][0]
         .startswith("§2023:12.3.7.3 17) d)  (")),
        ("pins 7.2.3.4 9) c) 2. (the nested item keeps its letter)",
         find(lines, cls, "Each operand and operator of a concatenation expression is a separate text-word", 2023)[1][0]
         .startswith("§2023:7.2.3.4 9) c) 2.  (")),
        ("accepts the CORRECTED form (C6), in its clause and its rule",
         v("§2023:14.9.10.4", "The value '62' is placed into the I-O status").ok
         and v("§2023:14.9.10.4 GR15", "The value '62' is placed into the I-O status").ok),
        ("accepts the PRINTED form (C6), in its clause and its rule",
         v("§2023:14.9.10.4", "The valu62' is placed into the I-O status").ok
         and v("§2023:14.9.10.4 GR15", "The valu62' is placed into the I-O status").ok),
        ("accepts the PRINTED form (C12)", v("§2023:14.9.30.4", "whose key value is greater than or equal to the key value in the file position indicator").ok),
        ("fires on a wrong clause, naming the right one",
         fails("§2023:14.9.27.4", "The valu62' is placed into the I-O status", "text-not-in-clause", "§2023:14.9.10.4 GR15")),
        ("--find prints the edition form", find(lines, cls, "equivalent to a literal", 2023)[1][0].startswith("§2023:")),
        ("--find prints the 1985 path in the 1985 style",
         any(o.startswith("§1985:XI.4.4.4 (6) b.  (") for o in find(*load(1985), q85b, 1985)[1])),
    ]
    for name, ok in cases:
        print(f"{'PASS' if ok else 'FAIL'}  {name}")
    bad = [n for n, ok in cases if not ok]
    print("SELF-TEST: " + ("FAIL " + str(bad) if bad else "PASS"))
    return 1 if bad else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", nargs=2, metavar=("CITATION", "TEXT"),
                    help="assert TEXT appears inside the cited unit (the sub-item, rule, format, table or clause): "
                         "--check \"§2023:14.9.8.4 GR1b\" \"<text>\"; exit 1 naming the part that failed")
    ap.add_argument("--find", metavar="TEXT", help="which region(s) of --edition's transcription contain this text")
    ap.add_argument("--self-test", action="store_true", help="prove each guard fires and stays silent")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if not (args.find or args.check):
        ap.print_help()
        return 2
    if args.find:
        code, out = find(*load(args.edition), args.find, args.edition)
    elif any(a == "--edition" or a.startswith("--edition=") for a in sys.argv[1:]):
        print("FAIL: --check takes the edition from the citation (§1985:VI.5.3.2); --edition is --find's")
        return 2
    else:
        try:
            cited = citation.parse(args.check[0])
        except citation.CitationError as e:
            print(f"FAIL: {e}")
            return 1
        code, out = check(cited, args.check[1])
    print("\n".join(out))
    return code


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
