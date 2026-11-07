#!/usr/bin/env python3
"""THE ONE CITATION FORMAT FOR EVERY EDITION OF THE COBOL STANDARD, AND ITS ONE PARSER (kb/Work PB4887).

This docstring is the format's specification; every reader of a citation (`cite.py`, `audit_code_citations.py`,
`audit_doc_citations.py`, `fuzz_triage.py`, `work.py`, `where.py`, `phase_b_batch.py`, `docchecks/vcr.py`) parses
through this module and writes no citation regex of its own.

THE FORMAT (owner 2026-11-06; the edition is MANDATORY):

    §<edition>:<locator>[ <rule>]

    §2023:13.18.35.4 GR1     §2002:14.8.4 SR2     §1985:VI.5.3.2 GR3     §1985:I.1.5.2     §1985:p.VI-20

| Edition | Document                         | Locator                                                               | Checked against          |
|---------|----------------------------------|-----------------------------------------------------------------------|--------------------------|
| `2023`  | ISO/IEC 1989:2023                | clause number `13.18.35.4`; an annex clause `A.1`, `E.2`               | `specs/ISO_COBOL_2023.md` |
| `2014`  | ISO/IEC 1989:2014                | clause number                                                         | parsed only (no transcription) |
| `2002`  | ISO/IEC 1989:2002                | clause number                                                         | `specs/ISO_COBOL_2002.md` once it exists, parsed only until then |
| `1985`  | ANSI X3.23-1985                  | Roman section, then that section's own paragraph number: `VI.5.3.2` (Section VI, the Nucleus); the section alone `VI`; a printed page with no paragraph `p.VI-20`; an appendix `B`, `B.2.1` | `specs/ANSI_COBOL_1985.md` |
| `1989`  | ANSI X3.23a-1989 (the amendment) | its own numbering                                                     | parsed only              |
| `1993`  | ANSI X3.23b-1993 (the amendment) | its own numbering                                                     | parsed only              |

- **Rule** (optional, after ONE space): `SR n` | `GR n` | `Format n` | `Table n`. A syntax or general rule is written
  glued (`GR1`, `SR17`), and its sub-item path is glued after the number as the repository has always written it
  (`GR6d.2`, `SR3b`). `render` writes the glued form; `parse` also reads `GR 1`.
- **Disambiguation.** `§` immediately followed by an edition token and a colon is a citation of the standard, and
  nothing else is: the repository's own documents keep their `§` sections ("plan §0", "DESIGN-locale-facility §4.4.3"). A `§` with
  four digits and a colon whose digits are not an edition (`§5000:`) is a MALFORMED citation, never prose. A placeholder
  in angle brackets after the colon (`§2023:<clause>`, the way a document shows the format), an interpolation hole
  (`$"ISO §2023:{clause}"`, the way the compiler composes a diagnostic) and a regex group (`@"§2023:([0-9.]+)"`, the
  way a test reads citations) are templates, not citations.
- **Paragraph numbers restart in each section of the 1985 standard** (VI.5.3 and VII.5.3 are different paragraphs), so
  a 1985 locator always carries its section, and a paragraph the section does not have is refused even when another
  section has it.

A BARE `§N` IS NO CITATION (kb/Work PB4887 phase 2: the tree's 94,513 bare citations of the 2023 standard were
rewritten once into the edition form, and the transition that read the bare form as 2023 is gone). `parse` refuses a
bare `§13.18.35.4 GR1`, naming the edition it lacks; `scan` does not read one unless asked (`bare=True`), which only
the BARE refusal does: `audit_code_citations.py` refuses a bare `§N` that `bare_standard` classifies as a citation
of the 2023 standard, and leaves a document's own section ("plan §0", "DESIGN-locale-facility §4.4.3") alone. The audit reads the
whole repository, the tooling's scripts and their string literals included (kb/Work PB4911).

THE ONE EXEMPTION: THE EXAMPLE MARKER (kb/Work PB4911). A line that shows the refused form ON PURPOSE (this module's
own examples of a bare `§N`, an audit's planted test data, a malformed `§5000:` a self-test feeds a parser) carries the
text `citation-audit: example` (`EXAMPLE_MARKER`), written in that line's comment or, inside a docstring, at the line's
end. The audit reports nothing on a marked line, PHANTOM, CITATION and BARE alike, and reads only that line: the marker
is visible beside the text it excuses, so it cannot rot the way a hand-kept list of exempt lines would. It is the only
exemption; a citation of the standard is written in the edition form, and a document's section names its document.

    parse(text)           a WHOLE citation ("§2023:14.9.8.4 GR1") -> Citation, or CitationError naming the defect
    scan(text)            every citation in free text, left to right (malformed ones carry their `error`)
    render(citation)      the canonical spelling
    validate(citation)    None, or why the locator names nothing in that edition's transcription
    regions(edition)      (locator, title, start, end) of every citable region of that edition's transcription
    universe(edition)     the set of locators `validate` accepts, or None for a parsed-only edition
    bare_standard(...)    whether a bare `§N` in context is a citation of the 2023 standard (the BARE refusal's classifier)
    example(line)         whether `line` carries the example marker (the audit's one exemption)

Run `python scripts/spec/citation.py --self-test` (planted cases for every edition, every locator shape and each
rejection: no edition, an unknown edition, a phantom clause, a 1985 paragraph outside its section).
"""
from __future__ import annotations

import argparse
import dataclasses
import functools
import pathlib
import re
import sys
import tempfile
from collections.abc import Iterator

sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import spec_pdf  # noqa: E402  (scripts/spec: the one table of transcription files, kb/Work PB4815)


@dataclasses.dataclass(frozen=True)
class EditionFormat:
    """One row of the format's edition table: the document the token names and the shape of its locator."""
    document: str
    locator: str        # "clause" (ISO dotted clause numbers), "section" (1985 Roman sections), "own" (an amendment)


#: The edition tokens of the format, newest first. A token is the edition's year as printed on the document.
EDITIONS: dict[int, EditionFormat] = {
    2023: EditionFormat("ISO/IEC 1989:2023", "clause"),
    2014: EditionFormat("ISO/IEC 1989:2014", "clause"),
    2002: EditionFormat("ISO/IEC 1989:2002", "clause"),
    1985: EditionFormat("ANSI X3.23-1985", "section"),
    1989: EditionFormat("ANSI X3.23a-1989", "own"),
    1993: EditionFormat("ANSI X3.23b-1993", "own"),
}

#: An ISO clause number: the body's dotted decimal (14.9.39.4) or an annex's letter-headed form (A.1, E.2, D.2.2.5.1).
#: The trailing `(?![a-z])` keeps a glued RULE PATH out of the number: the repository writes `§2023:13.18.35.4.5b.3` for
#: rule 5 b) 3., and the clause is 13.18.35.4.
_CLAUSE = r"(?:\d+(?:\.\d+)*|[A-Z](?:\.\d+)*)(?![a-z])"
#: A 1985 locator: a printed page `p.VI-20`, a Roman section with its own paragraph number `VI.5.3.2`, an appendix `B.2`.
_SECTION_LOCATOR = r"(?:p\.(?:[IVX]+|[A-F])-\d+|[IVX]+(?:\.\d+)*|[A-F](?:\.\d+)*)(?![a-z])"
_OWN_LOCATOR = r"(?:p\.)?[A-Z0-9]+(?:[.-][A-Z0-9]+)*(?![a-z])"
_LOCATOR = {"clause": re.compile(_CLAUSE + r"\Z"), "section": re.compile(_SECTION_LOCATOR + r"\Z"),
            "own": re.compile(_OWN_LOCATOR + r"\Z")}

#: The rule designator, after one space. Its glued sub-item path is `[a-z0-9.]` ending in a letter or digit, so a
#: sentence's full stop is never part of it.
RULE = r"(?:(?:SR|GR) ?\d+(?:[a-z0-9.]*[a-z0-9])?|Format \d+|Table \d+)"
_RULE = re.compile(RULE + r"\Z")

#: THE ONE CITATION PATTERN. `edition`/`locator` are the edition form (the locator read loosely here, and per edition
#: by `parse`); `bare` is a `§` with no edition before a clause-shaped number (`§13.16`, `§ 14.9.39.4`, an annex
#: clause `§A.1`), which is no citation: only the BARE refusal reads it (`scan(..., bare=True)`). A caller that embeds a citation in a larger pattern embeds THIS string (its group
#: `cite` spans the citation) and reads the match with `from_match`.
PATTERN = (r"(?P<cite>§(?:(?P<edition>\d{4}):(?![<{(])(?P<locator>p\.[A-Z]+-\d+|[A-Z0-9]+(?:\.[A-Z0-9]+)*(?![a-z]))?"
           r"|\s?(?P<bare>(?:\d+|[A-Z])(?:\.\d+)+)(?![a-z]|\.[A-Z])))")
_SCAN = re.compile(PATTERN + r"(?: (?P<rule>" + RULE + r"))?")
#: A WHOLE citation, as `cite.py --check` takes it.
_WHOLE = re.compile(r"§(?P<edition>\d{4}):(?P<locator>\S+?)(?: (?P<rule>.+))?")
#: A clause number written without its edition (`§14.9.8`, `14.9.8 GR1`): `parse` refuses it, naming the edition form.
_NO_EDITION = re.compile(r"§?\s?(?P<clause>" + _CLAUSE + r")(?: .+)?")


class CitationError(ValueError):
    """A string that is not a citation in the format, with the reason."""


@dataclasses.dataclass(frozen=True)
class Citation:
    """One citation: its edition, locator and rule, where it was found, and why it is malformed (None when it is not)."""
    edition: int | None
    locator: str
    rule: str | None = None
    start: int = 0
    end: int = 0
    bare: bool = False          # written without the edition: no citation, read only by `scan(..., bare=True)`
    error: str | None = None    # set by `scan` for a malformed citation; `parse` raises it instead

    @property
    def clause(self) -> str:
        """The locator: for an ISO edition it IS the clause number."""
        return self.locator


def _rule(raw: str | None) -> str | None:
    """The rule designator in its canonical spelling (`GR 1` -> `GR1`), or CitationError."""
    if raw is None:
        return None
    raw = raw.strip()
    if not _RULE.match(raw):
        raise CitationError(f"{raw!r} is not a rule designator: write SR n, GR n (its sub-item path glued: GR6d.2), "
                            "Format n or Table n")
    return re.sub(r"^(SR|GR) ", r"\1", raw)


def _edition_locator(token: str, locator: str | None) -> tuple[int, str]:
    edition = int(token)
    if edition not in EDITIONS:
        raise CitationError(f"§{token}: is not an edition of the standard; the editions are "
                            + ", ".join(map(str, EDITIONS)))
    shape = EDITIONS[edition].locator
    if not locator:
        raise CitationError(f"§{token}: carries no locator")
    if not _LOCATOR[shape].match(locator):
        raise CitationError(f"{locator!r} is not a {edition} locator: " + {
            "clause": "an ISO edition is cited by clause number (13.18.35.4, A.1)",
            "section": "a 1985 locator is a Roman section and its own paragraph (VI.5.3.2), the section (VI), "
                       "a printed page (p.VI-20) or an appendix (B.2)",
            "own": "an amendment is cited by its own numbering"}[shape])
    return edition, locator


def parse(text: str) -> Citation:
    """A WHOLE citation: `§2023:14.9.8.4 GR1`. Raises CitationError naming the defect (no edition, an unknown edition,
    a locator of the wrong shape for its edition, a malformed rule)."""
    text = text.strip()
    if m := _WHOLE.fullmatch(text):
        edition, locator = _edition_locator(m["edition"], m["locator"])
        return Citation(edition, locator, _rule(m["rule"]), 0, len(text))
    if m := _NO_EDITION.fullmatch(text):
        raise CitationError(f"{text!r} carries no edition: write §<edition>:<locator>[ <rule>], the edition mandatory "
                            f"(§2023:{m['clause']} for ISO/IEC 1989:2023, §1985:VI.5.3.2 for ANSI X3.23-1985)")
    raise CitationError(f"{text!r} is not a citation: write §<edition>:<locator>[ <rule>], the edition mandatory "
                        "(§2023:13.18.35.4 GR1, §1985:VI.5.3.2 GR3)")


def from_match(m: re.Match, bare: bool = False) -> Citation | None:
    """The citation a match of `PATTERN` (alone or embedded in a caller's larger pattern) found: malformed ones carry
    their `error`; a bare `§N` is no citation and is None, unless `bare` asks for it (the BARE refusal), when it is
    returned as the 2023 clause it would name, marked `bare=True`."""
    rule = m.groupdict().get("rule")
    start, end = m.start("cite"), (m.end("rule") if rule else m.end("cite"))
    if m["bare"] is not None:
        if not bare:
            return None
        return Citation(2023, m["bare"], rule and _rule(rule), start, end, bare=True)
    try:
        edition, locator = _edition_locator(m["edition"], m["locator"])
        return Citation(edition, locator, rule and _rule(rule), start, end)
    except CitationError as e:
        return Citation(int(m["edition"]), m["locator"] or "", None, start, end, error=str(e))


def scan(text: str, bare: bool = False) -> Iterator[Citation]:
    """Every citation in `text`, left to right. A malformed edition-form citation is yielded with its `error` set,
    so a reader can refuse it; a bare `§N` is yielded (as 2023, `bare=True`) only when `bare` is True."""
    for m in _SCAN.finditer(text):
        if (c := from_match(m, bare)) is not None:
            yield c


#: The mark of a citation of the 2023 standard, just left of its clause number. The 2023-only checks (the rule-ordinal
#: family, a dossier's clause search) embed it.
MARK_2023 = r"§2023:"
#: A lookbehind refusing a clause number that belongs to ANOTHER edition's citation (`§2002:14.8.4`): the 2023-only
#: checks that also read a clause number written without its `§` (`(ISO 14.9.39 Format 10 GR18)`) embed it, so a
#: citation of 2002 is never measured against the 2023 standard's rules.
NOT_ANOTHER_EDITION = "".join(rf"(?<!§{e}:)" for e in EDITIONS if e != 2023)


def clause_pattern(clause: str) -> str:
    """The pattern of a citation of exactly `clause` of the 2023 standard, or of one of its subclauses (a following
    digit is refused, so §2023:14.9.2 never matches §2023:14.9.27). `where.py` and `phase_b_batch.py` search with it."""
    return MARK_2023 + re.escape(clause) + r"(?![0-9])"


def render(c: Citation) -> str:
    """The canonical spelling of `c`: always the edition form."""
    if c.edition is None:
        raise CitationError(f"{c.locator!r} carries no edition")
    return f"§{c.edition}:{c.locator}" + (f" {c.rule}" if c.rule else "")


# ── VALIDATION AGAINST EACH EDITION'S TRANSCRIPTION ──────────────────────────────────────────────────────────────────

#: An ISO transcription's clause heading. The TITLE is optional: all 178 headings of clause 3 (Terms and definitions)
#: are a bare number, the term itself being the bold line beneath. The annex alternative needs a dot segment, which is
#: what keeps the annex TITLE headings ("## Annex A") out.
ISO_HEADING = re.compile(r"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(?:\s+(.*?))?\s*$")
#: The 1985 transcription (kb/Work PB4816; not yet reconciled, PB4850) marks a section with a `SECTION <roman>:` heading,
#: an appendix with `APPENDIX <letter>:`, a paragraph with a numbered heading (`### 5.3.2 General Format`, `## 1. <u>…`)
#: or, where the transcription did not head it, a line opening with the number and a capitalised title, and the end of
#: each printed page with its folio alone on a line (`VI-20`).
#: `SECTION A: INTRINSIC FUNCTION MODULE` is Amendment 1 (ANSI X3.23a-1989), cited `§1989:`: it ends the 1985 sections.
#: The markup that may open a 1985 heading or paragraph line: an underline, bold, and the side of a level-2 box (`║`,
#: the Preface's notation note; kb/Work PB4850). None of it is part of the number the line carries.
_MARK_85 = rf"(?:<u>|\*\*|{spec_pdf.BOX_SIDE})*"
_SECTION_85 = re.compile(rf"^#{{1,2}}\s+{_MARK_85}SECTION ([IVX]+|[A-Z]):")
_APPENDIX_85 = re.compile(rf"^#{{1,6}}\s+{_MARK_85}APPENDIX ([A-F]):")
_PARAGRAPH_85 = re.compile(rf"^(?:#{{1,6}}\s+{_MARK_85}(\d+(?:\.\d+)*)\.?(?:\s|<)|{_MARK_85}(\d+(?:\.\d+)+)\s+[A-Z])")
_FOLIO_85 = re.compile(r"^((?:[IVX]+|[A-F])-\d+)\s*$")
_PAGE_85 = re.compile(r"^## Page \d+\s*$")
#: The Roman sections ANSI X3.23-1985 has (I Introductory information ... XVIII Index).
SECTIONS_85 = ("I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XIII", "XIV", "XV", "XVI",
               "XVII", "XVIII")


def transcription(edition: int, repo: pathlib.Path = spec_pdf.REPO) -> pathlib.Path | None:
    """The transcription `validate` checks `edition` against, or None while the edition is parsed only."""
    if edition not in spec_pdf.EDITIONS:
        return None
    path = spec_pdf.markdown(edition, repo)
    return path if path.is_file() else None


def iso_regions(lines: list[str]):
    """(clause, title, start, end) of every clause heading of an ISO transcription, in order."""
    heads = [(m.group(1), m.group(2) or "", i) for i, l in enumerate(lines) if (m := ISO_HEADING.match(l))]
    return [(n, t, s, heads[k + 1][2] if k + 1 < len(heads) else len(lines)) for k, (n, t, s) in enumerate(heads)]


def _regions_1985(lines: list[str]):
    """Every citable region of the 1985 transcription. A section's paragraph runs to the next paragraph of the SAME
    section (its region nests nothing: `VI.5.3` ends where `VI.5.3.1` begins, the clause-region convention of the ISO
    transcription); a section or an appendix runs to the next one; a printed page runs from the page break above its
    folio to the folio."""
    out = []
    owner, owner_start, para, para_start, title, page_start = None, 0, None, 0, "", 0

    def close_para(at: int) -> None:
        if owner and para:
            out.append((f"{owner}.{para}", title, para_start, at))

    for i, line in enumerate(lines):
        if _PAGE_85.match(line):
            page_start = i
        if (f := _FOLIO_85.match(line)) is not None:
            out.append((f"p.{f.group(1)}", "", page_start, i + 1))
            continue
        head = _SECTION_85.match(line) or _APPENDIX_85.match(line)
        if head is not None:
            name = head.group(1)
            if _SECTION_85.match(line) and name not in SECTIONS_85:
                name = None                       # an amendment's lettered section: not a 1985 locator
            if name != owner:
                close_para(i)
                if owner:
                    out.append((owner, "", owner_start, i))
                owner, owner_start, para = name, i, None
            continue
        if owner and (p := _PARAGRAPH_85.match(line)) is not None:
            close_para(i)
            para, para_start, title = (p.group(1) or p.group(2)), i, line.lstrip("#* ").strip()
    close_para(len(lines))
    if owner:
        out.append((owner, "", owner_start, len(lines)))
    return out


@functools.lru_cache(maxsize=None)
def _loaded(edition: int, repo: pathlib.Path) -> tuple[tuple[str, ...], tuple[tuple[str, str, int, int], ...]] | None:
    path = transcription(edition, repo)
    if path is None:
        return None
    lines = path.read_text(encoding="utf-8").splitlines()
    shape = EDITIONS[edition].locator
    return tuple(lines), tuple(_regions_1985(lines) if shape == "section" else iso_regions(lines))


def regions(edition: int, repo: pathlib.Path = spec_pdf.REPO):
    """`(lines, [(locator, title, start, end), …])` of `edition`'s transcription, or None while it is parsed only.
    A locator may own several regions (a 1985 section the transcription heads twice); a reader searches them all."""
    loaded = _loaded(edition, repo)
    return None if loaded is None else (list(loaded[0]), list(loaded[1]))


@functools.lru_cache(maxsize=None)
def universe(edition: int, repo: pathlib.Path = spec_pdf.REPO) -> frozenset[str] | None:
    """Every locator `validate` accepts for `edition`, or None while the edition is parsed only."""
    loaded = _loaded(edition, repo)
    return None if loaded is None else frozenset(r[0] for r in loaded[1])


def validate(c: Citation, repo: pathlib.Path = spec_pdf.REPO) -> str | None:
    """None when `c` names a region of its edition's transcription (or the edition is parsed only), else why not."""
    if c.error:
        return c.error
    known = universe(c.edition, repo)
    if known is None or c.locator in known:
        return None
    if EDITIONS[c.edition].locator == "section" and "." in c.locator and not c.locator.startswith("p."):
        section, para = c.locator.split(".", 1)
        elsewhere = sorted(k for k in known if k.split(".", 1)[-1] == para and k.split(".", 1)[0] != section)
        return (f"Section {section} of the {c.edition} standard has no paragraph {para}"
                + (f" (paragraph numbers restart in each section: {', '.join(elsewhere[:4])} exist)" if elsewhere else ""))
    return f"the {c.edition} standard has no {'clause' if EDITIONS[c.edition].locator == 'clause' else 'locator'} {c.locator}"


# ── THE BARE FORM'S CLASSIFIER (the BARE refusal's) ──────────────────────────────────────────────────────────────────

#: ⛔ A BARE `§` IS NOT ALWAYS A CITATION OF THE STANDARD. The repository's docs and comments also point at THEIR OWN
#: sections ("DESIGN-locale-facility §4.4.3", "ADR §1.2.1 guardrail 1", "plan §0", and a kb/Work site's
#: "`docs/VERSION_CHANGE_REFERENCE.md` (§4.2)", a Markdown file named just before). This screen says "the `§` just right
#: of a document's name is that document's section". Other EDITIONS of COBOL are not in it: they carry their own edition
#: token (`§1985:VI.5.3.2`), and a free-text "COBOL-85 §4.3.3" names its edition beside a number that is no citation.
#: ⛔ THE NAME IS A WHOLE NAME AND IT STANDS RIGHT AGAINST THE `§` (kb/Work PB4887 review R1-1). The first screen matched
#: its names as case-blind SUBSTRINGS anywhere in the 45 characters left of the `§`, so "the EVALUATE statement
#: (§14.9.13)", "evaluated per §8.8.1.5", "statement designated archaic | §F.1", "ReadRandom … owns the §9.1.13" and "a (citation-audit: example)
#: capacity of its own (§8.5.1.9.2)" were all a document's section. That screen called 2,658 bare citations a (citation-audit: example)
#: document's own section; this one agrees on 1,400 of them, so about 1,258 were genuine citations of the standard
#: that the one-time rewrite would have left bare and the BARE refusal would never have seen (review R2-1: 2,658 is
#: the old screen's total, not its genuine part). Now a name is a whole token (a Markdown
#: file, a design document's identifier, one of the repository's named documents, or a prose "the design's"), and
#: between it and the `§` stand only punctuation, a possessive, the word "section", or the earlier members of a list of
#: its sections ("DESIGN-x §4.6, §4.7"). The standard's own transcription is not a document of the repository: "in (citation-audit: example)
#: specs/ISO_COBOL_2023.md §8.7.5.2" cites the standard. (citation-audit: example)
_DOCUMENT_NAME = r"""(?:
      (?<![\w./-])(?![\w./-]*ISO_COBOL_2023\.md)[\w./-]*\w\.md(?!\w)        # a Markdown file: docs/X.md, DESIGN-x.md
    | \b(?:DESIGN|SURVEY|CRITIQUE|EVAL|PHASE|PIPELINE|PLAN)-\w[\w.-]*\w       # a design document's identifier
    | (?i:\b(?:doc|scout\s+brief)\b)                                          # "P11 doc §3.1", "the scout brief §4.1"
    | \b[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*_DESIGN\b                               # OO_DESIGN, CONDITIONS_EXCEPTIONS_DESIGN
    | \b(?:ADR|PLAN|PROMPT|README|CLAUDE|DESIGN|DEVLOG|SSOT)\b                # named documents; SSOT is DESIGN.md
    | \bUnicode(?:\s+Standard)?\b                                            # another standard: "the Unicode Standard §3.11"
    | (?i:\b(?:the|this|that)\s+(?:design|doc|document|deep-dive|ADR)         # prose: "the design's", "this document"
          (?:\s+(?:doc|document|SSOT))?)
    | (?i:\bdesign(?:\s+(?:doc|document|SSOT))?)                              # "design §8", "(design SSOT §9.10)"
)"""
#: What may stand between the name and the `§`: the close of an XML-doc `<c>` wrapping the name
#: (`<c>X-DESIGN.md</c> §9.2`) or of the interpolation hole that prints it (`f"{BINDER_DESIGN} §3.6"`), a line number
#: (`DESIGN-x.md:848`), a possessive, punctuation, the word "section", and the earlier members of a list or a range
#: (`§8.1–§8.7`) of the document's sections. (citation-audit: example)
_DOCUMENT_GAP = r"""(?:</c>|\})?(?::\d+(?:[-–]\d+)?)?(?:'s|’s)?[\s`*"'(\[]*(?:sections?\s+)?(?:§\s?\w[\w.]*\w?[\s,;/–`*)]*(?:and\s+|or\s+)?)*$"""
DOCUMENT_SECTION = re.compile(_DOCUMENT_NAME + _DOCUMENT_GAP, re.VERBOSE)
#: A free-text citation of ANOTHER edition, just left of a bare `§`: "COBOL-85 §4.3.3", "ISO 1989:2002 §14.8.4".
OTHER_EDITION = re.compile(
    r"(?:COBOL[- ]?(?P<cobol>85|1985|2002|2014)|1989:(?P<iso>1985|2002|2014)|X3\.23-?(?P<ansi>1985|85))[^§\n]{0,45}$",
    re.IGNORECASE)


def other_edition(before: str) -> int | None:
    """The edition a free-text prefix names just left of a bare `§` ("COBOL-85 §…" -> 1985), else None."""
    m = OTHER_EDITION.search(before)
    if m is None:
        return None
    year = m["cobol"] or m["iso"] or m["ansi"]
    return {"85": 1985}.get(year, int(year))


def bare_standard(line: str, c: Citation, own_sections: frozenset[str] | set[str] = frozenset(),
                  known: frozenset[str] | None = None, prev: str = "") -> bool:
    """Whether the bare citation `c` (found in `line`, `prev` the line above it) is a citation of the 2023 standard:
    its number is a clause of the standard (`known`, the 2023 universe), the file does not head a section of its own
    with that number, no document name or other edition stands just left of it, and it is not a design doc's own item
    label (`§9.10.1-C2`)."""
    if not c.bare or c.locator in own_sections:
        return False
    if known is not None and c.locator not in known:
        return False
    return not names_another_text(line, c, prev)


#: What may open a line before a `§` that continues the line above: a comment's leader (`#`, `#:`, `//`, `///`, `*>`,
#: `*`, `--`, `;`, `<!--`, a Markdown quote's `>`) and a string literal's prefix and opening quote (`f"`, the next
#: piece of an implicitly concatenated string), then only what `_DOCUMENT_GAP` allows (punctuation, and the earlier
#: members of a list or a range of sections: `# §3.14.1–` before `§3.14.6`, citation-audit: example). Such a line
#: carries the sentence on from the line above, so the document named at the end of that line owns the number. The
#: same pattern strips the line above's own leader.
_OPENING = re.compile(r"""^\s*(?:#:?|//+|\*>|\*|--|;+|<!--|(?:>\s*)+)?\s*(?:[rRbBfFuU]{0,2}["'])?""")
_GAP = re.compile(_DOCUMENT_GAP, re.VERBOSE)


def names_another_text(line: str, c: Citation, prev: str = "") -> bool:
    """Whether the bare `c` is another text's number by its context (THE ONE SCREEN `bare_standard` and the PHANTOM
    audit share): a document's name stands just left of it, another edition is named just left of it, or it is a
    design doc's own item label (`§9.10.1-C2`, the label glued to the number). A rule range (`§11.9.9.3 SR1-SR6`) is (citation-audit: example)
    not a label, nor is a rule glued with a hyphen (`§13.5.3-SR1`): the check reads the text after the LOCATOR, and (citation-audit: example)
    only when no rule follows it. ("plan §0" is not screened here: 0 is no clause of the standard, and "the plan's
    §13.18.63.4" in a code comment is the codegen plan citing the standard; citation-audit: example)

    ⛔ THE CONTEXT CROSSES A LINE BREAK (kb/Work PB4911). Prose wraps, so a document's name may END the line above
    (`prev`) while its section number OPENS this one ("... DESIGN-test-build-ci.md" / "# §3.14.1"). When nothing but a (citation-audit: example)
    comment's leader, gap punctuation and earlier members of a list of sections stand before `c` on its own line, the
    screen reads the line above, its own leader removed, as the text just left of it."""
    before = line[:c.start]
    if prev and (o := _OPENING.match(before)) and _GAP.match(before, o.end()):
        before = _OPENING.sub("", prev).rstrip() + " " + before[o.end():]
    if DOCUMENT_SECTION.search(before) or other_edition(before) is not None:
        return True
    if c.rule:
        return False
    after = line.index(c.locator, c.start) + len(c.locator)
    return ITEM_LABEL.match(line, after) is not None


#: A design doc's own item label glued to a number (`§9.10.1-C2`, `§9.1-B`), never a rule (`§13.5.3-SR1`). (citation-audit: example)
ITEM_LABEL = re.compile(r"-(?![GS]R\d)[A-Z]")

#: THE EXAMPLE MARKER (see the module docstring): a line carrying it shows the refused form on purpose.
EXAMPLE_MARKER = "citation-audit: example"


def example(line: str) -> bool:
    """Whether `line` carries the example marker: the citations audit reports nothing on it."""
    return EXAMPLE_MARKER in line


# ── SELF-TEST ────────────────────────────────────────────────────────────────────────────────────────────────────────

_PLANTED_1985 = """# SECTION I: INTRODUCTORY INFORMATION

## Page 21

## 1. INTRODUCTION TO THE STANDARD

### 1.5 DEFINITION

1.5.2 Definition of a Conforming Implementation

the planted conforming sentence

I-1

# SECTION VI: NUCLEUS MODULE

## Page 233

## 5.3 THE DATA DESCRIPTION ENTRY

### 5.3.2 General Format

the planted general format

### ║5.4 THE PLANTED LEVEL-2 CLAUSE║

║the planted boxed rule║

VI-20

# SECTION XVII: APPENDICES

## APPENDIX B: DIFFERENCES

### 2.1 Planted

the planted appendix sentence
"""


def self_test() -> int:
    """Every edition, every locator shape and each rejection, planted (kb/Work PB4887 item 5)."""
    def raises(text: str, needle: str) -> bool:
        try:
            parse(text)
        except CitationError as e:
            return needle in str(e)
        return False

    def bad(text: str, repo: pathlib.Path = spec_pdf.REPO) -> str | None:
        return validate(parse(text), repo)

    with tempfile.TemporaryDirectory() as td:
        planted = pathlib.Path(td)
        md = spec_pdf.markdown(1985, planted)
        md.parent.mkdir()
        md.write_text(_PLANTED_1985, encoding="utf-8", newline="\n")
        spec_pdf.markdown(2002, planted).write_text("## 14.8.4 Planted\n\ntext\n", encoding="utf-8", newline="\n")
        planted_1985 = [
            ("1985 section and paragraph validates", bad("§1985:VI.5.3.2 GR3", planted) is None),
            ("1985 heading paragraph validates", bad("§1985:VI.5.3", planted) is None),
            ("1985 heading boxed as level 2 validates: the box side is no part of its number",
             bad("§1985:VI.5.4", planted) is None),
            ("1985 unheaded paragraph line validates", bad("§1985:I.1.5.2", planted) is None),
            ("1985 printed page validates", bad("§1985:p.VI-20", planted) is None),
            ("1985 section alone validates", bad("§1985:VI", planted) is None),
            ("1985 appendix paragraph validates", bad("§1985:B.2.1", planted) is None),
            ("1985 paragraph outside its section is refused, naming where it is",
             "Section I of the 1985 standard has no paragraph 5.3.2 (paragraph numbers restart in each section: VI.5.3.2"
             in (bad("§1985:I.5.3.2", planted) or "")),
            ("1985 printed page that does not exist is refused", bad("§1985:p.VI-21", planted) is not None),
            ("2002 validates against its transcription once it exists", bad("§2002:14.8.4 SR2", planted) is None),
            ("2002 phantom refused once its transcription exists", bad("§2002:14.8.5", planted) is not None),
        ]
    with tempfile.TemporaryDirectory() as td:
        parse_only = bad("§2002:99.99", pathlib.Path(td)) is None
    text = 'see §2023:13.18.35.4 GR1, "plan §0", §1985:VI.5.3.2 GR3. Also §5000: and §2023:VI.5 and §14.9.24.'  # citation-audit: example
    found = list(scan(text))
    cases = planted_1985 + [
        ("parses the 2023 form with its rule", parse("§2023:13.18.35.4 GR1") == Citation(2023, "13.18.35.4", "GR1", 0, 20)),
        ("parses a glued rule path", parse("§2023:14.9.25.4 GR6d.2").rule == "GR6d.2"),
        ("parses Format n and Table n", parse("§2023:14.9.39 Format 4").rule == "Format 4"
         and parse("§2023:8.9 Table 12").rule == "Table 12"),
        ("canonicalizes GR 1 to GR1", render(parse("§2023:14.9.8.4 GR 1")) == "§2023:14.9.8.4 GR1"),
        ("parses an annex clause", parse("§2023:A.1").locator == "A.1"),
        ("parses every edition token", all(parse(f"§{e}:{'VI.5.3' if e == 1985 else '1.2'}").edition == e
                                           for e in EDITIONS)),
        ("2023 clause validates", bad("§2023:13.18.35.4 GR1") is None),
        ("2023 annex clause validates", bad("§2023:A.1") is None),
        ("2023 phantom refused", bad("§2023:8.3.1.2") == "the 2023 standard has no clause 8.3.1.2"),
        ("2014 is parsed only", bad("§2014:99.99") is None),
        ("2002 is parsed only while its transcription is absent", parse_only),
        ("1989 and 1993 are parsed only", bad("§1989:A.2.1") is None and bad("§1993:3.1") is None),
        ("1985 against the real transcription", bad("§1985:VI.5.3.2") is None and bad("§1985:p.VI-20") is None),
        ("rejects an unknown edition", raises("§2020:1.2", "is not an edition")),
        ("rejects a 1985 locator without its section", raises("§1985:5.3.2", "is not a 1985 locator")),
        ("rejects a Roman locator for 2023", raises("§2023:VI.5.3", "is not a 2023 locator")),
        ("rejects a malformed rule", raises("§2023:14.9.8.4 rule one", "is not a rule designator")),
        ("refuses a bare citation, naming the edition form it lacks",
         raises("§13.18.35.4 GR1", "'§13.18.35.4 GR1' carries no edition: write §<edition>:<locator>[ <rule>], the "  # citation-audit: example
                "edition mandatory (§2023:13.18.35.4")
         and raises("14.9.8", "carries no edition") and raises("§ A.1", "(§2023:A.1 for")),
        ("refuses a document's section as no citation at all", raises("plan §0", "is not a citation")),
        ("scan reads both edition forms with their rules",
         [(c.edition, c.locator, c.rule) for c in found if not c.error and not c.bare]
         == [(2023, "13.18.35.4", "GR1"), (1985, "VI.5.3.2", "GR3")]),
        ("scan leaves a repository section alone", all(c.locator != "0" for c in found)),
        ("scan flags an unknown edition and a wrong-shaped locator",
         [(c.edition, c.locator) for c in found if c.error] == [(5000, ""), (2023, "VI.5")]),
        ("scan reads no bare form unless asked", not any(c.bare for c in found)
         and [(c.edition, c.locator) for c in scan(text, bare=True) if c.bare] == [(2023, "14.9.24")]),
        ("scan stops a glued rule path at the clause", [c.locator for c in scan("§2023:13.18.35.4.5b.3")] == ["13.18.35.4"]
         and [c.locator for c in scan("§13.18.35.4.5b.3", bare=True)] == ["13.18.35.4"]),  # citation-audit: example
        ("scan reads no clause in a placeholder (§14.9.N)", list(scan("the (§14.9.N) banners", bare=True)) == []),
        ("scan reads a template (§2023:<clause>) as no citation", list(scan('check "§2023:<clause>[ <rule>]"')) == []),
        ("scan reads an interpolation hole (§2023:{clause}) as no citation",
         list(scan('$"(ISO §2023:{Clause} SR{rule})"')) == []),
        ("scan reads a regex group (§2023:(...)) as no citation", list(scan(r'@"ISO §2023:(\d+(?:\.\d+)+)"')) == []),
        ("scan keeps the full stop out of a rule", [c.rule for c in scan("as §2023:14.9.8.4 GR1.")] == ["GR1"]),
        ("clause_pattern does not match a longer clause",
         re.search(clause_pattern("14.9.2"), "§2023:14.9.27") is None
         and re.search(clause_pattern("14.9.2"), "§2023:14.9.2.4 GR1") is not None),
        ("bare_standard leaves a document's section and another edition alone",
         not _bare_in("DESIGN-locale-facility §4.4.3") and not _bare_in("COBOL-85 §14.9.24")
         and _bare_in("the MOVE statement §14.9.25")),  # citation-audit: example
        ("bare_standard reads a document's name as a whole name right against the § (review R1-1)",
         all(_bare_in(s) for s in (
             "the EVALUATE statement (§14.9.13) selects the first WHEN", "evaluated per §8.8.1.5",  # citation-audit: example
             "while its owner lives (§9.1.16)", "useValues says (§8.5.1.9.5", "statement designated archaic | §F.1",  # citation-audit: example
             "`ReadNext`, `ReadRandom`) owns the §9.1.13", "a capacity of its own (§8.5.1.9.2)",  # citation-audit: example
             "see `docs/CONFORMANCE.md` DOC-A.1-8, §12.3.7.4", "in specs/ISO_COBOL_2023.md §8.7.5.2",  # citation-audit: example
             "DEVLOG 380).** §13.18.60", "into the plan's §13.18.63.4 GR12", "(§11.9.9.3 SR1-SR6)",  # citation-audit: example
             "Method WS is §13.5.3-SR1"))  # citation-audit: example
         and not any(_bare_in(s) for s in (
             "DESIGN-external-repository §4.5, §11.2", "`docs/rearchitecture/DESIGN-message-control.md` (§7.2",
             "CONDITIONS_EXCEPTIONS_DESIGN §14.9.29", "the design's §5.5", "(design §5.2; dim", "ADR §4.2",
             "per `DESIGN.md` §14.4", "DESIGN §3.3 / §14.1", "the interceptor (§9.1-B)",
             # review round 2 R1-1: the SSOT is DESIGN.md, and an XML-doc <c> may wrap the document's name
             "[canonical: SSOT §14.1]", "(see SSOT §5.1/§14.6)", "<c>PHASE-13-c5-perform-format3-DESIGN.md</c> §9.2",
             "the P11 doc §3.2 places", "per the scout brief §4.1", "(PLAN-bindtime-gating-migration §4.1 task", "Written as design §8.1–§8.7 of",
             # kb/Work PB4911: an interpolated document name, and another standard named in prose
             'f"{BINDER_DESIGN} §3.6 does not name"', "the Unicode Standard (§4.6, \"Numeric Value\")"))),
        ("bare_standard reads a document's name ending the line above (kb/Work PB4911)",
         not _bare_in("# §8.3 the gate's slot", prev="# the gate's design, DESIGN-test-build-ci.md")  # citation-audit: example
         and not _bare_in("> §8.3, the SSOT for it)", prev="> the design is `DESIGN-test-build-ci.md`")  # citation-audit: example
         and not _bare_in("    §5.2 duplication", prev="    per DESIGN-architecture-review")  # citation-audit: example
         and not _bare_in("//   (§4.5, §8.1)", prev="// see `docs/rearchitecture/DESIGN-locale-facility.md`")  # citation-audit: example
         and not _bare_in('    f"§8.1), which needs the walk"', prev='    f"its neutrality (DESIGN-architecture-review "')  # citation-audit: example
         and not any(bare_standard(line, c, known=universe(2023), prev="# per DESIGN-architecture-review")
                     for line in ["# §8.1–§8.3 and the rest"] for c in scan(line, bare=True))  # citation-audit: example
         and _bare_in("the MOVE statement §14.9.25", prev="# see DESIGN-x.md")  # citation-audit: example
         and _bare_in("# §14.9.25 moves it", prev="# the MOVE statement")),  # citation-audit: example
        ("the example marker is read on its own line only",
         example(f"x = '§14.9.24'  # {EXAMPLE_MARKER}") and not example("x = '§14.9.24'")),  # citation-audit: example
        ("other_edition names the edition", other_edition("COBOL-85 ") == 1985 and other_edition("ISO 1989:2002 ") == 2002),
    ]
    for name, ok in cases:
        print(f"{'PASS' if ok else 'FAIL'}  {name}")
    failed = [n for n, ok in cases if not ok]
    print("SELF-TEST: " + ("FAIL " + str(failed) if failed else "PASS"))
    return 1 if failed else 0


def _bare_in(line: str, prev: str = "") -> bool:
    """Whether the first bare `§N` of `line` (`prev` the line above) is a citation of the 2023 standard written
    without its edition."""
    c = next(c for c in scan(line, bare=True) if c.bare)
    return bare_standard(line, c, known=universe(2023), prev=prev)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("citation", nargs="?", help="parse and validate one whole citation, print its canonical form")
    ap.add_argument("--self-test", action="store_true", help="the planted cases for every edition and rejection")
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    if not args.citation:
        ap.print_help()
        return 2
    try:
        c = parse(args.citation)
    except CitationError as e:
        print(f"FAIL: {e}")
        return 1
    problem = validate(c)
    print(f"FAIL: {problem}" if problem else f"OK  {render(c)}  ({EDITIONS[c.edition].document}"
          + ("" if transcription(c.edition) else ", parsed only: no transcription") + ")")
    return 1 if problem else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
