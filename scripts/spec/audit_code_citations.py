#!/usr/bin/env python3
"""A citation must name the construct its comment is about — the check `cite.py` cannot make for you.

    python scripts/spec/audit_code_citations.py            # report
    python scripts/spec/audit_code_citations.py --check    # exit 1 on any finding (the gate)
    python scripts/spec/audit_code_citations.py --self-test  # prove each check FAILS on a known defect

⛔ WHY THIS EXISTS, AND WHY `cite.py` AND `audit_doc_citations.py` CANNOT SEE THIS DEFECT.
`cite.py --check <clause> "<text>"` answers "is this sentence inside that clause". `audit_doc_citations.py`
answers "the quoted fragment is real — is it filed under the right clause". Both need a QUOTE. The commonest
citation in this repository has none: a statement-header comment (`// MOVE (§14.9.24)`), a lexer rule
(`// COBOL-2002 ROUNDED MODE names (ISO §14.9.4)`), a `<summary>` naming a clause and nothing else. Those carry
the *clause number* and the *construct name* and no quoted text at all, so nothing checked them — and by
2026-09 twenty-eight of them were wrong. Every one of the eight hand-verified first cases named a VALID clause
belonging to a DIFFERENT REAL STATEMENT (`MOVE (§14.9.24)` — §14.9.24 is MERGE), so nothing that merely asks
"does this clause exist" could see any of them either. This is `feedback_a_real_clause_can_answer_a_different_question`
at grammar scale, and it had a measured consumer: the Phase-B dossier ranks a `.g4` rule against a subject partly
BY THE CLAUSE ITS COMMENT CITES, so `phase_b_batch.py 14.9.25` (MOVE) handed an adjudicator `openFileSpec` as
evidence about MOVE (kb/Work PB159).

THE CHECKS, each narrow on purpose:

  PHANTOM   — the citation names a clause the standard does not have (§8.3.1.2, §8.8.4.1.1, §8.3.3.7 were cited
              ~120 times between them; kb/Work PB159/PB182/PB290). Only ISO-SHAPED numbers are considered — at
              least three dot-separated segments under a real top-level clause — because a design doc legitimately
              writes "§4.2" and "§14.4" about its OWN sections and those are not citations of the standard.

  SUBJECT   — the cited clause's catalog subject is a NAMED construct (`MERGE statement`, `HIGHLIGHT clause`,
              `ROUNDED phrase`) whose name appears NOWHERE in the citation's context, while the context DOES name
              another construct that has a clause of its own. That is the exact `MOVE (§14.9.24)` shape.

  HEADER    — a definition-header comment (`MOVE (§14.9.24)`, `LINAGE clause (ISO §13.16)`, `IS GLOBAL
              (§13.18.23)`) names a construct and then cites a clause that is neither that construct's own clause
              nor anything under it. Sharper than SUBJECT, and it catches the case SUBJECT structurally cannot:
              a wrong clause whose own subject is not a named construct (§13.16 is "Data description entry", so
              `LINAGE clause (ISO §13.16)` has no wrong CONSTRUCT to notice — only a wrong ANSWER).

  THE ORDINAL FAMILY (kb/Work PB388) — the clause is RIGHT and the number INSIDE it is wrong, which is the one
  half of rule 1's failure mode neither `cite.py` nor the checks above can see: a format ORDINAL and a rule
  NUMBER are not quotations, so there is nothing for a quote-based check to resolve. Four sub-checks, each
  EXACT — they resolve the ordinal against the standard's own lists and never score or guess:

  FORMAT    — `§14.9.28 Format 4` where §14.9.28.2 prints THREE general formats. (Cited four times, including
              in a differential test's summary; PERFORM VARYING is a PHRASE of formats 1 and 2.)
  FORMAT-RULE — `§14.9.39 Format 4 … GR12`, where the standard's own `FORMAT n` banners partition the rule
              block and rule 12 sits under FORMAT 7. The banners (`ALL FORMATS`, `FORMATS 1 AND 2`, `FORMAT 10`)
              are printed in the rule block itself, so this pairing is decidable with no heuristic at all.
  RULE      — `§14.9.12.3 SR6` where that clause has four syntax rules, `§14.9.47.4 GR6` where it has three.
  SUBITEM   — `§14.9.37.4 GR8b` where rule 8 has no sub-items (the serial-SEARCH VARYING rules are GR3 a)–c)).
  FORMAT-NAME — the context NAMES a format of the cited clause verbatim ("data-pointer assignment") and cites a
              different ordinal. §14.9.39 was the measured case: eleven sites called the data-pointer slice
              "Format 4", which is condition-setting, and two of them were the text of COBOLNET0869.

⛔ THE ORDINAL FAMILY IS LINE-BASED, LIKE PHANTOM, AND THAT IS THE POINT. The checks above read only COMMENT
text, so a citation inside a DIAGNOSTIC MESSAGE STRING — the citation a user actually reads — was covered by
nothing. Both of PB388's user-visible defects lived there. An ordinal check needs no comment convention, so it
runs over every line of every file that carries citations, and the message strings come under the gate with the
comments.

⚠ THE ORDINAL CHECKS NEED `specs/ISO_COBOL.md` — the catalog knows which rules it HARVESTED, which is not the
same question as which rules the standard prints (it carries a `parse_gaps` count for that very reason), so the
standard's own text is what every ordinal is resolved against and what vetoes a finding. Without the submodule
they report SKIPPED, by name, exactly as PHANTOM does.

⚠ RULE AND SUBITEM REPORT BUT DO NOT GATE, and that is a MEASURED debt, not a judgement about the checks. Both
are sound — each was confirmed against the standard's own rule markers — and on the day they were written they
found 195 sites in 124 files, every one of them needing its own derivation before it can be repaired. They print
on every run, with their count; `--check-all` is the gate for the sweep that closes them, and it becomes
`--check` when it does.

⚠ THE CONTEXT IS THE COMMENT BLOCK PLUS THE DECLARATION IT INTRODUCES, not the single line, and that is what
makes SUBJECT quiet enough to gate on. A first draft matched line-by-line and reported 92 candidates of which 64
were correct citations whose construct was simply named two lines up (`// VALUE Clause …` then `// Format 3
(§13.18.63): WHEN SET TO FALSE …`) or in the rule name below (`valueClause`). Reading the block and the
identifier takes the noise out without weakening the signal: all 28 real defects survive it.

⚠ IT NEEDS `specs/ISO_COBOL.md` FOR THE PHANTOM CHECK ONLY. The clause universe is the standard's own headings;
the catalog is derived and has a block only where a clause carries numbered rules, so it cannot answer "does
this clause exist" without inventing phantoms of its own. With the private submodule absent (CI checks out with
`submodules: false`) the phantom check reports SKIPPED — loudly, by name — and the other two still run, because
`spec-rule-catalog.json` is committed.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation_corpus  # noqa: E402

REPO = citation_corpus.REPO
SPEC = REPO / "specs" / "ISO_COBOL.md"
CATALOG = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"

#: A clause reference. Two dots minimum for SUBJECT/HEADER (`§13.16` is a real clause and is cited as one);
#: PHANTOM additionally demands three segments — see the module docstring.
#: The trailing `(?![a-z])` keeps a RULE PATH out of the clause number: the standard prints `5 b) 3.` and this
#: repository writes it glued, `§13.18.35.4.5b.3`; without the guard the audit would read `§13.18.35.4.5` as a
#: clause and call a correct citation a phantom.
CITE = re.compile(r"§\s?(\d+(?:\.\d+)+)(?![a-z])")

#: ⛔ A `§` IS NOT ALWAYS A CITATION OF THE STANDARD, and PHANTOM is the check that has to care. This repository's
#: docs and comments also point at THEIR OWN sections ("DESIGN-locale-facility §4.4.3", "ADR §1.2.1 guardrail 1",
#: "plan §0") and at OTHER EDITIONS of COBOL, whose clause numbering is a different document entirely
#: ("COBOL-85 §4.3.3", "ISO 1989:1985 8.3.1.2"). Both are legitimate and neither is checkable against
#: `specs/ISO_COBOL.md`. This is the one calibration in the file that is a list of words rather than a structure,
#: and it is deliberately short: everything else is derived.
NOT_THE_STANDARD = re.compile(
    r"(?:DESIGN|ADR|PLAN|PROMPT|DEVLOG|SURVEY|CRITIQUE|EVAL|README|CONFORMANCE\.md|REDEFINES_DESIGN"
    r"|this doc|its own|COBOL-(?:85|1985|2002|2014|2023)|1989:(?:1985|2002|2014))[^§\n]{0,45}$",
    re.IGNORECASE)

#: ⛔ AND A PHANTOM THAT IS BEING REPORTED AS A PHANTOM IS NOT A DEFECT. The repairs for this very family leave
#: behind the sentence that records them — "the old comment cited a §8.3.3.7 the standard does not have",
#: "`cite.py --check 8.4.2.4` → there is no clause", "the third premise USED to cite §8.8.4.1.1". Flagging those
#: would make the audit permanently red on its own fix, and would push the next author to DELETE the forensic
#: record to get the gate green — the worst possible incentive.
NAMED_AS_WRONG = re.compile(
    r"(?i)does not (?:exist|have)|no such clause|there is no clause|phantom|used to cite|mis-?cit"
    r"|nonexistent|non-existent|wrong-?\s?§|is not a clause|citations? repaired|fabricat"
    r"ed|A CLAUSE THAT DOES NOT|inherited[- ]citation|but Format \d+ is|names nothing")
#: The heading form `NAME statement` / `NAME clause` / `NAME phrase` / `NAME paragraph`. The name is a COBOL
#: word: upper case, digits and hyphens (GROUP-USAGE, PROGRAM-ID, BLANK WHEN ZERO, ALTERNATE RECORD KEY).
NAMED = re.compile(r"^((?:[A-Z][A-Z0-9-]*)(?:\s+[A-Z][A-Z0-9-]*)*)\s+"
                   r"(statement|clause|phrase|paragraph|section|division|directive|function)\b")
#: A construct word as it appears in prose. Hyphens are part of the word; `SET` must not match `OFFSET`.
WORD = re.compile(r"(?<![A-Za-z0-9-])([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*)(?![A-Za-z0-9-])")
#: The DEFINITION-HEADER shape: a construct name at the head of the comment, then a citation close behind it.
HEADER = re.compile(r"^\W*(?:ISO\s+)?(?:COBOL-\d{2,4}\s+)?"
                    r"((?:[A-Z][A-Z0-9-]{2,})(?:\s+[A-Z][A-Z0-9-]+)?)"
                    r"[^§\n]{0,60}?§\s?\d")

#: The corpus DIRECTIVE line a negative fixture opens with — machine metadata read by
#: `CorpusRunnerTests.EnabledNegativeCase_RejectsWithItsDiagnostic`, not prose. It is skipped when looking
#: for the comment's HEAD; leaving it in place silently disabled the HEADER check on every negative fixture.
DIRECTIVE = re.compile(r"^\W*reject-at:", re.I)

#: Words that make a two-word header name a STRUCTURE rather than a construct (see the HEADER check).
STRUCTURAL = {"SECTION", "DIVISION", "PARAGRAPH", "ENTRY", "SENTENCE", "HEADING", "FOOTING"}

#: A file that NAMES phantom clauses on purpose declares itself with this marker in its first 40 lines.
MARKER = "audit-code-citations: names-phantoms"

COMMENT = {
    ".g4": re.compile(r"//(.*)$"),
    ".cs": re.compile(r"//(.*)$"),
    ".cob": re.compile(r"\*>(.*)$"),
}
#: camelCase / PascalCase identifiers SPLIT INTO WORDS, so `constantRecordClause` names CONSTANT and
#: `roundedPhrase` names ROUNDED. ⛔ The first version matched the identifier WHOLE
#: (`[A-Za-z][A-Za-z0-9]*`), which is not a split at all — `CONSTANTRECORDCLAUSE` contains no word
#: boundary, so the construct-name test could never match it and the audit reported a rule as not
#: naming the construct its own name spells out.
IDENT = re.compile(r"[A-Z]+(?![a-z])|[A-Z]?[a-z0-9]+")


def spec_clauses() -> set[str] | None:
    """Every clause number the standard actually has, or None when the private submodule is absent."""
    if not SPEC.exists():
        return None
    head = re.compile(r"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)(?:\s|$)")
    return {m.group(1) for line in SPEC.read_text(encoding="utf-8").splitlines()
            if (m := head.match(line))}


def catalog_subjects() -> dict[str, str]:
    """clause -> the construct heading it belongs to, from the derived rule catalog (committed, so this half
    of the audit runs with no submodule). A rule's `section` is the RULE block (`14.9.4.3`); its parent is the
    construct (`14.9.4`), and both are keyed here."""
    rules = json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]
    seen: dict[str, set[str]] = {}
    for r in rules:
        for key in (r["section"], r["section"].rsplit(".", 1)[0]):
            seen.setdefault(key, set()).add(r["subject"])
    # An ambiguous key (two constructs claiming one clause) is evidence about the CATALOG, not about the code —
    # drop it rather than accuse a citation on a map we do not trust here.
    return {k: next(iter(v)) for k, v in seen.items() if len(v) == 1}


def construct_clause(subjects: dict[str, str]) -> dict[str, str]:
    """`MOVE` -> `14.9.25`, `ROUNDED` -> `14.7.4`, `GROUP-USAGE` -> `13.18.29`, `SAME AS` -> `13.18.49`.

    ⛔ A MULTI-WORD construct is keyed ONLY by its full name, never by its first word, and that is not tidiness —
    it is what stops the map from answering a different question. `SOURCE` names both the SOURCE clause
    (§13.18.53) and the SOURCE FORMAT directive (§7.3.24); `SAME` names the SAME clause (§12.4.6.4) and the SAME
    AS clause (§13.18.49). Letting a multi-word subject claim the bare word made the audit report
    `// SOURCE (§13.18.53)` as citing the wrong clause — a correct citation accused on the tool's own ambiguity.
    Shortest clause still wins among genuine ties, so a construct keys to its own clause and not a subclause."""
    out: dict[str, str] = {}
    for clause, subj in subjects.items():
        if (m := NAMED.match(subj)) is None:
            continue
        name = m.group(1)
        prev = out.get(name)
        if prev is None or len(clause) < len(prev):
            out[name] = clause
    return out


#: How far a construct's NAME may sit from a citation about it and still count as named. A `.g4` file writes the
#: name once in a section banner and then several rules under it; a C# file writes it in the class `<summary>`.
#: ⚠ CALIBRATION, and it is what makes SUBJECT gateable: at 0 lines of lookback the check reported 2590
#: candidates, essentially all of them citations whose construct was named a few lines up. At 30/10 it reports
#: the real defects and little else, and every one of the 28 hand-adjudicated defects still fires.
BEFORE, AFTER = 30, 10


def blocks(path: pathlib.Path):
    """Yield (first_line_no, comment_parts, context_text, window_text) per contiguous comment block.

    CONTEXT is the block plus the line it introduces — the rule, method or field — with identifiers split into
    words so `roundedPhrase` counts as naming ROUNDED. It answers "what is this comment about".
    WINDOW is the surrounding ±BEFORE/AFTER lines. It answers the weaker "is this construct named anywhere near
    here at all", which is the question absence has to be judged on."""
    pat = COMMENT.get(path.suffix)
    if pat is None:
        return
    try:
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return
    i, n = 0, len(lines)
    while i < n:
        m = pat.search(lines[i])
        if not m:
            i += 1
            continue
        start, parts = i, []
        while i < n and (m := pat.search(lines[i])):
            parts.append(m.group(1))
            i += 1
        tail = ""
        j = i
        while j < n and j < i + 2:
            if lines[j].strip():
                tail = " ".join(IDENT.findall(lines[j])).upper()
                break
            j += 1
        # ⛔ A GOLDEN'S WINDOW IS THE WHOLE PROGRAM. Its citations live in one header block at the top
        # and the construct they name is written in the PROCEDURE DIVISION forty lines down; a ±30-line
        # window called every OO golden's `§11.7 METHOD-ID` citation unnamed. A fixture is small and
        # about one thing, so the file IS the context.
        lo, hi = (0, n) if path.suffix == ".cob" else (max(0, start - BEFORE), min(n, i + AFTER))
        near = " ".join(lines[lo:hi])
        # Raw text (so a hyphenated name survives) PLUS its identifiers split into words (so `roundedPhrase`
        # counts), the whole thing upper-cased — a deliberately GENEROUS reading, since over-reading "named
        # nearby" can only SUPPRESS a finding, never invent one.
        window = (near + " " + " ".join(IDENT.findall(near))).upper()
        # ⛔ THE LABEL IS NOT THE WHOLE BLOCK IN A GOLDEN, AND TREATING IT AS ONE FLAGGED CORRECT
        # CITATIONS. The declaration-header convention binds the citation that NAMES this construct;
        # a `.cob` header goes on to ARGUE, and an argument legitimately cites other clauses —
        # `find_string_zero_length.cob` cites §15.59.3/§15.63.3/§15.66.3/§15.71.3/§15.72.3/§15.85.3 to
        # say those functions DO prohibit a zero-length literal and FIND-STRING does not, and every one
        # of those citations is right. Whether a quoted fragment belongs to the clause it is filed
        # under is `audit_doc_citations.py`'s question, not this one; existence is PHANTOM's, which
        # still reads the whole block. So SUBJECT/HEADER see the first citation-bearing LINE — the same
        # line HEADER already matched — and a `.g4`/`.cs` comment, which sits ON its declaration and is
        # short, is unchanged: the label is the whole block there.
        # SUBJECT reads the first CITATION-BEARING line; HEADER reads the first NON-DIRECTIVE line, and
        # they are not the same line. Taking the citation-bearing one for HEADER too made a CONTINUATION
        # line read as a head: `oo_external_file_shared.cob` wraps "...shared between a PROGRAM and an /
        # OBJECT (§13.18.22.4)", and the second line opens with a capitalised word followed by a citation,
        # which is exactly the header shape. HEADER wants the HEAD of the comment, so it gets the head —
        # skipping only the corpus DIRECTIVE line (`*> reject-at: …`), which is machine metadata and was
        # silently disabling the check on every negative fixture that carries one.
        label = parts
        head = parts
        if path.suffix == ".cob":
            label = next(([p] for p in parts if CITE.search(p)), [])
            head = [p for p in parts if not DIRECTIVE.search(p)] or parts
        yield start + 1, parts, "\n".join(parts).upper() + "\n" + tail, window, label, head


def names_in(text: str) -> set[str]:
    return set(WORD.findall(text))


def _phantoms(line: str, universe: set[str], skip: set[str] = frozenset(), prev: str = ""):
    """The phantom clauses cited on ONE line, with the exclusions the check cannot do without.

    `prev` is the preceding line: prose wraps, and "swept the repo for the phantom clause / \"§8.8.4.1.1\""
    puts the disclaimer and the citation on different lines."""
    for m in CITE.finditer(line):
        c = m.group(1)
        segs = c.split(".")
        if len(segs) < 3 or not segs[0].isdigit() or not 1 <= int(segs[0]) <= 15:
            continue                                     # not ISO-shaped: a doc's own §4.2, a two-part §13.16
        if c in universe or c in skip:
            continue
        if NOT_THE_STANDARD.search(line[:m.start()]):
            continue                                     # "DESIGN-… §4.4.3", "COBOL-85 §4.3.3"
        if NAMED_AS_WRONG.search(line) or NAMED_AS_WRONG.search(prev):
            continue                                     # the text is REPORTING the phantom, not making it
        if re.match(r"-[A-Z]", line[m.end():m.end() + 2]):
            continue                                     # "§9.10.1-C2" — a design doc's own item label
        if re.match(r"\s*(?:→|->)", line[m.end():m.end() + 4]):
            continue                                     # "§8.4.2.4 → §8.4.3.3" — a repair RECORD, not a claim
        if line[m.start() - 1:m.start()] in ('"', "\u201c", "`") and line[m.end():m.end() + 1] in ('"', "\u201d", "`"):
            continue                                     # `"§8.8.4.1.1"` — prose NAMING a spelling, not citing it
        yield c


def phantom_scan(universe: set[str] | None):
    """PHANTOM over EVERY file that carries a citation, source and prose alike — it needs no context, so it has
    no false-positive problem outside code and there is no reason to leave the design docs unchecked (the
    §8.3.1.2 family lived in both)."""
    findings: list[tuple[str, str, str, str]] = []
    if universe is None:
        return findings
    own_heading = re.compile(r"^#{1,6}\s+(\d+(?:\.\d+)+)")
    for path in citation_corpus.all_files():
        rel = path.relative_to(REPO).as_posix()
        try:
            lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
        except OSError:
            continue
        # ⛔ THE ONE NOTE WHOSE SUBJECT *IS* THE PHANTOM FAMILY HAS TO BE ABLE TO NAME ITS MEMBERS. kb/Work
        # PB290 is a table of "this clause number does not exist and here is what each site meant"; every row
        # spells a phantom, on purpose, and no per-line disclaimer survives being tabulated. A file may
        # therefore declare itself with the marker below, which says "the § spellings in here are QUOTED, not
        # asserted". It is a whole-file opt-out and it is visible in the file, so it cannot rot silently the
        # way a checked-in list of exempt SITES would.
        if MARKER in "\n".join(lines[:40]):
            continue
        # A design doc numbers its OWN sections and then points at them; those are not citations of anything.
        mine = {m.group(1) for line in lines if (m := own_heading.match(line))} if path.suffix == ".md" else set()
        for i, line in enumerate(lines, 1):
            for c in _phantoms(line, universe, mine, lines[i - 2] if i >= 2 else ""):
                findings.append(("PHANTOM", f"{rel}:{i}", c, "the standard has no clause §" + c))
    return findings


# ── THE ORDINAL FAMILY (kb/Work PB388) ───────────────────────────────────────────────────────────────────

#: `§14.9.39 Format 7`, `ISO 14.9.39 format 6`, `14.9.39.2 Formats 11`. The clause may be the CONSTRUCT
#: (§14.9.39) or its general-format subclause (§14.9.39.2) — this repository writes both, so both are keyed.
FORMAT_CITE = re.compile(r"(\d+(?:\.\d+){1,4})\s+Formats?\s+(\d+)", re.I)

#: A rule designator and, optionally, the sub-item letter written against it: `GR4`, `SR16g`, `GR3 c) 2`,
#: `GR7 k)6`, `GR6c's`. The `(?![a-z])` keeps a word (`GRs`, `SRc`… ) out of the sub-item slot.
RULE_CITE = re.compile(r"\b(GR|SR)\s?(\d+)(?:\s?\(?([a-z])\)?(?![a-z]))?")

#: An ISO-shaped clause number, with or without the `§` — `(ISO 14.9.39 Format 10 GR18)` is how half the
#: goldens write it. Three segments minimum, so a doc's own `§4.2` is not read as a citation.
CLAUSE_TOKEN = re.compile(r"(?<![\d.])(\d+(?:\.\d+){2,4})(?![\d.])")

#: How far to the LEFT a rule designator may look for the clause it belongs to. A line cites one clause and
#: then several of its rules (`§14.9.37.4 GR4/GR6/GR9`, `§14.9.39 Format 9 SR21 / §8.4.3.13`), so the carry is
#: necessary; bounding it stops a clause named at the head of a long prose line from claiming a rule at its end.
CARRY = 60

#: How close a `Format n` must sit to a rule designator to be read as CITING it (`§14.9.39 Format 10 GR18`).
ADJACENT = 12

#: A file that spells wrong ordinals ON PURPOSE declares itself, exactly as it does for the phantom and
#: misfiling checks. Both markers are honoured here because a note about INHERITED CITATIONS tabulates wrong
#: clause/quote pairings and wrong clause/ordinal pairings in the same table — one register, one opt-out.
ORDINAL_MARKERS = (MARKER, "audit-doc-citations: names-misfilings")

#: The two ordinal checks that are SOUND but arrived with a backlog larger than the change that added them.
#: They report on every run and gate under `--check-all`; see the note in `main`.
MEASURED_BACKLOG = frozenset({"RULE", "SUBITEM"})

_HEADING = re.compile(r"^#{2,6}\s+([0-9]+(?:\.[0-9]+)*|[A-Z](?:\.[0-9]+)+)\s*(.*)$")
_FORMAT_LINE = re.compile(r"^\s*Format\s+(\d+)\s*(?:\(([^)]*)\))?\s*:")
_RULE_LINE = re.compile(r"^\s*(\d+)\\\)\s")
#: The standard PARTITIONS a rule block by general format, in its own words, on a line of its own: `ALL
#: FORMATS`, `FORMAT 1`, `FORMATS 1 AND 2`. That partition is what makes FORMAT-RULE exact.
_BANNER = re.compile(r"^\s*(ALL FORMATS|FORMATS?(?:\s+\d+)(?:\s*(?:,|AND)\s*\d+)*)\s*$")


def _alias(d: dict):
    """A rule block is cited BOTH ways — `§14.9.39.4 GR29` and `§14.9.39 GR29` — so key it both ways."""
    for (cl, k), v in list(d.items()):
        d.setdefault((cl.rsplit(".", 1)[0], k), v)
    return d


def catalog_rule_tops() -> dict[tuple[str, str], int]:
    """`(clause, kind) -> the highest rule ordinal that block has`, from the COMMITTED rule catalog — so the
    RULE check runs with no submodule, like SUBJECT and HEADER.

    ⛔ THE CATALOG IS THE AUTHORITY ON WHAT A RULE IS, and this audit does not get a second opinion. The
    standard's transcription numbers rules `1\\)` in some blocks and `1.` in others, and the SUB-items of a
    rule are numbered `1.` `2.` in exactly the same shape at exactly the same indent — a second parser written
    here read a sub-item as a rule and reported `§14.9.10.4 has 21 GRs — there is no GR1` about a block whose
    rule 1 it had simply mis-parsed. `extract_rule_catalog.py` settled that ambiguity once (`place()`, the
    sublist machinery); one mechanism, one place (feedback_one_rule_one_place)."""
    rules = json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]
    tops: dict[tuple[str, str], int] = {}
    for r in rules:
        if r["sublist"] != 1:
            continue
        k = (r["section"], r["kind"])
        tops[k] = max(tops.get(k, 0), r["ordinal"])
    return _alias(tops)


def spec_ordinals():
    """What only the STANDARD ITSELF carries; None when the private submodule is absent.

    Returns `(formats, spans, owner)`:
      · `formats[clause] = {n: name}`   — the general formats a construct prints, read from the `Format n
                                          (name):` lines of its `.2` subclause and keyed under both spellings.
      · `spans[(clause, kind)] = {n: text}` — each rule's text FROM the standard, delimited by locating the
                                          catalog's rules inside the clause's own segment. Sub-items (`a)`,
                                          `k)`) survive, which the catalog's own `text` field does not always
                                          keep, and SUBITEM needs them.
      · `owner[(clause, kind)] = {n: {formats} | None}` — the FORMAT partition each rule sits under, from the
                                          standard's own banners (`ALL FORMATS`, `FORMAT 1`, `FORMATS 1 AND 2`).
                                          None where the block carries no banner before that rule.
    """
    if not SPEC.exists():
        return None
    text = SPEC.read_text(encoding="utf-8")
    heads: list[tuple[int, str, str]] = []
    banners: list[tuple[int, set[int] | None]] = []
    formats: dict[str, dict[int, str | None]] = {}
    pos, in_formats, clause = 0, False, None
    for line in text.splitlines(keepends=True):
        stripped = line.rstrip("\r\n")
        if (h := _HEADING.match(stripped)) is not None:
            clause, title = h.group(1), h.group(2)
            heads.append((pos, clause, title))
            in_formats = "general format" in title.lower()
        elif in_formats and clause and (f := _FORMAT_LINE.match(stripped)) is not None:
            for key in (clause, clause.rsplit(".", 1)[0]):
                formats.setdefault(key, {}).setdefault(int(f.group(1)), f.group(2))
        elif (b := _BANNER.match(stripped)) is not None:
            banners.append((pos, None if b.group(1).upper().startswith("ALL")
                            else {int(n) for n in re.findall(r"\d+", b.group(1))}))
        pos += len(line)
    segment = {cl: (start, heads[i + 1][0] if i + 1 < len(heads) else len(text))
               for i, (start, cl, _t) in enumerate(heads)}

    by_block: dict[tuple[str, str], list[tuple[int, str]]] = {}
    for r in json.loads(CATALOG.read_text(encoding="utf-8"))["rules"]:
        if r["sublist"] == 1 and r["section"] in segment:
            by_block.setdefault((r["section"], r["kind"]), []).append((r["ordinal"], r["text"]))

    spans: dict[tuple[str, str], dict[int, str]] = {}
    owner: dict[tuple[str, str], dict[int, set[int] | None]] = {}
    for (cl, kind), items in by_block.items():
        lo, hi = segment[cl]
        seg = text[lo:hi]
        # ⛔ LOCATE THE RULES IN ORDER, EACH AFTER THE LAST. Searching the whole segment per rule matched the
        # FIRST text that looked like the rule's opening, and sibling rules open alike — §8.8.4.4.4 GR3 ("If
        # the data item referenced by identifier-1 …") landed on GR1's sentence, so GR3's "span" was GR1's and
        # every one of its a)–n) sub-items read as absent. Rules are printed in ordinal order; the cursor is
        # what says so.
        located: list[tuple[int, int]] = []
        cursor = 0
        for ordinal, body in sorted(items):
            words = body.split()[:6]
            if not words:
                continue
            probe = re.compile(r"\s+".join(re.escape(w) for w in words))
            if (m := probe.search(seg, cursor)) is not None:
                located.append((m.start(), ordinal))
                cursor = m.end()
        # Ordinal 0 is THE WHOLE BLOCK, and it is what vetoes a RULE finding: the catalog's top ordinal is
        # evidence about what the catalog HARVESTED, never about what the standard prints.
        spans.setdefault((cl, kind), {})[0] = seg
        for idx, (start, ordinal) in enumerate(located):
            end = located[idx + 1][0] if idx + 1 < len(located) else len(seg)
            spans.setdefault((cl, kind), {})[ordinal] = seg[start:end]
            part = [f for p, f in banners if lo <= p < lo + start]
            owner.setdefault((cl, kind), {})[ordinal] = part[-1] if part else None
    return formats, _alias(spans), _alias(owner)


def _names_format(text: str, name: str) -> bool:
    """The line spells this format's OWN name — `data-pointer assignment` for `data-pointer-assignment`,
    hyphen or space.

    ⚠ THREE-PART NAMES ONLY, and the threshold was measured. A one-word name (`all`, `serial`, `inline`,
    `attribute`, `validation`) is ordinary English; a TWO-word one is ordinary COBOL vocabulary
    (`condition-name`, `report-writer`, `index-assignment`) and appears in text that is about something else
    entirely — at two the check reported four correct citations, every one of them a line that merely used the
    words. At three (`data-pointer-assignment`, `object-reference-assignment`, `dynamic-capacity-table`) the
    name is specific enough that writing it is a statement about WHICH FORMAT, which is the claim this check
    is checking."""
    parts = name.split("-")
    if len(parts) < 3:
        return False
    return re.search(r"\b" + r"[ -]".join(re.escape(p) for p in parts) + r"\b", text, re.I) is not None


def _clause_at(line: str, pos: int) -> str | None:
    """The clause a rule designator at `pos` is filed under: the nearest ISO-shaped number to its left,
    within CARRY characters."""
    best = None
    for m in CLAUSE_TOKEN.finditer(line, 0, pos):
        if pos - m.end() <= CARRY:
            best = m.group(1)
    return best


def _ordinal_findings(rel: str, lines: list[str], data, tops: dict[tuple[str, str], int] | None = None):
    formats, spans, owner = data if data is not None else ({}, {}, {})
    tops = tops if tops is not None else {}
    out: list[tuple[str, str, str, str]] = []
    for i, line in enumerate(lines, 1):
        site = f"{rel}:{i}"
        # ⛔ AND A WRONG ORDINAL THAT IS BEING REPORTED AS ONE IS NOT A DEFECT — the same rule PHANTOM lives
        # by, and the same regex. A `kb/Work` note that says "the site cites Format 4 for a data-pointer SET,
        # but data-pointer assignment is Format 7" is the REPAIR RECORD; flagging it would make the audit red
        # on its own fix and teach the next author to delete the forensics.
        if NAMED_AS_WRONG.search(line):
            continue
        cited_formats: list[tuple[int, str, int]] = []
        for m in FORMAT_CITE.finditer(line):
            clause, n = m.group(1), int(m.group(2))
            if (d := formats.get(clause)) is None:
                continue
            if n not in d:
                out.append(("FORMAT", site, clause,
                            f"§{clause} prints {len(d)} general formats ({', '.join(map(str, sorted(d)))}) — "
                            f"there is no Format {n}"))
                continue
            cited_formats.append((m.end(), clause, n))
            named = {k for k, v in d.items() if v and _names_format(line, v)}
            if named and n not in named:
                k = sorted(named)[0]
                out.append(("FORMAT-NAME", site, clause,
                            f"§{clause} Format {n} is {d[n]}, but this line names Format {k} ({d[k]})"))
        for m in RULE_CITE.finditer(line):
            kind, num, sub = m.group(1), int(m.group(2)), m.group(3)
            if (clause := _clause_at(line, m.start())) is None:
                continue
            if (top := tops.get((clause, kind))) is None:
                continue                       # not a rule block this repository's catalog knows
            block = spans.get((clause, kind)) or {}
            if num > top:
                # ⛔ THE CATALOG HAS PARSE GAPS AND THE GATE MUST NOT ACCUSE ON ONE. `spec-rule-catalog.json`
                # carries a `parse_gaps` count for exactly this reason — a block whose tail did not harvest
                # reports a low top, and READ's `§14.9.32.4 GR9` was reported missing from a clause that has
                # it. The standard's own text is the veto: if a line in that clause OPENS with the ordinal,
                # the rule exists and nothing is said (over-accepting here is free; accusing wrongly is not).
                if re.search(rf"^\s*{num}\\?[.)]\s", block.get(0, ""), re.M):
                    continue
                out.append(("RULE", site, clause,
                            f"§{clause} has {top} {kind}s — there is no {kind}{num}"))
                continue
            body = block.get(num)
            # ⛔ ABSENCE OF THE LETTER IS ONLY EVIDENCE IN A RULE THAT HAS NO LETTERS AT ALL. A rule whose
            # sub-items run past a page break can lose one in transcription, so "this rule has a) and b), and
            # you cited c)" proves nothing about the STANDARD. A rule with no sub-item marker anywhere is a
            # different statement — §14.9.37.4 GR8 is one sentence, and `GR8b` was cited five times.
            # ⚠ `\)` — the transcription ESCAPES the closing paren of a list marker in some blocks and not in
            # others, and reading only the bare form made SUBITEM report thirteen correct citations of
            # §8.8.4.4.4 GR3's a)–n) table as naming nothing.
            if body and sub and not re.search(r"(?<![A-Za-z0-9])[a-z]\\?\)", body):
                out.append(("SUBITEM", site, clause,
                            f"§{clause} {kind}{num} has no sub-items — {kind}{num}{sub} names nothing"))
            part = (owner.get((clause, kind)) or {}).get(num, "absent")
            if part in (None, "absent"):
                continue                       # ALL FORMATS, or a block the standard does not partition
            # ⛔ ONE PAIRING PER CITATION, AND IT HAS TO BE ADJACENT. Pairing every format on the line with
            # every rule on it is a CROSS PRODUCT, and a line that legitimately discusses several
            # (`DIAGNOSTICS.md`'s generated rows, a kb table of format→rule) then produced a dozen findings
            # about a sentence that claimed nothing. A PAIRED citation is written `§14.9.39 Format 10 GR18` —
            # the rule follows its format immediately — so only the format that ENDS within ADJACENT
            # characters of the rule is read as making a claim about it.
            near = [(c, n) for end, c, n in cited_formats if 0 <= m.start() - end <= ADJACENT]
            if len(near) != 1:
                continue
            fclause, fnum = near[0]
            if fclause.split(".")[:3] == clause.split(".")[:3] and fnum not in part:
                out.append(("FORMAT-RULE", site, clause,
                            f"§{clause} {kind}{num} is printed under FORMAT {'/'.join(map(str, sorted(part)))}"
                            f", not under the Format {fnum} this line cites"))
    return out


def ordinal_scan(data, tops=None):
    """The ordinal family over EVERY line of every citation-bearing file — message strings included, which is
    the half the comment-block checks structurally cannot reach."""
    findings: list[tuple[str, str, str, str]] = []
    if data is None and not tops:
        return findings
    for path in citation_corpus.all_files():
        try:
            lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
        except OSError:
            continue
        if any(mk in "\n".join(lines[:40]) for mk in ORDINAL_MARKERS):
            continue
        findings += _ordinal_findings(path.relative_to(REPO).as_posix(), lines, data, tops)
    return findings


def audit(subjects: dict[str, str], own: dict[str, str], universe: set[str] | None, ordinals=None,
          tops=None):
    findings: list[tuple[str, str, str, str]] = phantom_scan(universe) + ordinal_scan(ordinals, tops)
    for path in citation_corpus.declaration_files():
        rel = path.relative_to(REPO).as_posix()
        for lineno, parts, context, window, label, head in blocks(path):
            cited = [c for p in parts for c in CITE.findall(p)]
            if not cited:
                continue
            site = f"{rel}:{lineno}"
            for kind, cite_, msg in _checks(parts, context, window, subjects, own, universe,
                                            label=label, head=head):
                findings.append((kind, site, cite_, msg))
    return findings


SELF_TEST = [
    # (kind that must fire, the comment block, the declaration line under it, the file suffix)
    ("SUBJECT", "// ROUNDED [MODE IS rounding-mode] (§14.9.4, COBOL-2002). The MODE phrase selects one of the\n"
                "// eight ISO rounding modes; bare ROUNDED defaults to NEAREST-AWAY-FROM-ZERO.",
     "roundedPhrase", ".g4"),
    ("HEADER", "// LINAGE clause (ISO §13.16) — page-based printing for sequential files", "linageClause",
     ".g4"),
    ("PHANTOM", "// A figurative constant operand (ISO §8.3.1.2).", "figurative", ".g4"),
    # ⛔ THE GOLDEN LEG. Narrowing SUBJECT/HEADER to a `.cob` header's LABEL line can only be trusted if
    # the narrowed check is still SEEN to fire on the defect it exists for: a golden whose OPENING
    # citation names the wrong construct (feedback_measure_the_selectors_complement — a selector is
    # evidence about what it returned, never about what it dropped).
    ("SUBJECT", "      *> reject-at: 2023\n"
                "      *> ISO §14.9.4 — the ROUNDED MODE phrase on this ADD.",
     "       ADD 1 TO WS-A ROUNDED MODE IS TRUNCATION.", ".cob"),
    # ⛔ AND THE HEADER LEG BEHIND THE `reject-at:` LINE. Skipping the corpus DIRECTIVE when looking for
    # the comment's head is a WIDENING — before it, HEADER matched `reject-at: 2023`, found no capitalised
    # construct name, and was silently disabled on every negative fixture in the corpus. A widening that
    # has never been seen to fire is not a widening.
    ("HEADER", "      *> reject-at: 2023\n"
               "      *> LINAGE clause (ISO §13.16) — page-based printing for sequential files",
     "       DISPLAY \"X\".", ".cob"),
]
SELF_TEST_CLEAN = [
    ("// ROUNDED [MODE IS rounding-mode] (§14.7.4, COBOL-2002). The MODE phrase selects one of the\n"
     "// eight ISO rounding modes; bare ROUNDED defaults to NEAREST-AWAY-FROM-ZERO.", "roundedPhrase",
     ".g4"),
    ("// LINAGE clause (ISO §13.18.34) — page-based printing for sequential files", "linageClause", ".g4"),
    ("// A figurative constant operand (ISO §8.3.3.6).", "figurative", ".g4"),
    # the repaired twin of the golden leg — AND the false positive the narrowing exists to stop: the
    # label is right and a LATER line cites a DIFFERENT construct's clause on purpose, to contrast.
    ("      *> reject-at: 2023\n"
     "      *> ISO §14.7.4 — the ROUNDED MODE phrase on this ADD.\n"
     "      *> Unlike §13.18.34, the LINAGE clause, which has no rounding of any kind.",
     "       ADD 1 TO WS-A ROUNDED MODE IS TRUNCATION.", ".cob"),
    ("      *> reject-at: 2023\n"
     "      *> LINAGE clause (ISO §13.18.34) — page-based printing for sequential files",
     "       DISPLAY \"X\".", ".cob"),
]


#: ⛔ THE ORDINAL LEGS. Each pair is (kind that must fire, the DEFECT line, its REPAIRED twin) and both halves
#: go through the real `_ordinal_findings`. Every defect here is one this audit was written against — measured
#: in the tree on 2026-09-13, not invented (kb/Work PB388).
ORDINAL_SELF_TEST = [
    ("FORMAT",
     "/// PERFORM VARYING (ISO §14.9.28 Format 4, GR12–13): nested induction loops",
     "/// PERFORM VARYING (ISO §14.9.28, the VARYING phrase of Formats 1 and 2 — §14.9.28.4 GR12–13)"),
    ("RULE",
     '    + "(ISO §14.9.12.2 / §14.9.12.3 SR6)");',
     '    + "(ISO §14.9.12.2)");'),
    ("SUBITEM",
     "/// serial SEARCH with VARYING index-of-ANOTHER-table (ISO §14.9.37.4 GR8b) falls through",
     "/// serial SEARCH with VARYING index-of-ANOTHER-table (ISO §14.9.37.4 GR3 c) 2) falls through"),
    ("FORMAT-NAME",
     "/// <summary>SET data-pointer assignment (§14.9.39 Format 4; Phase-4b increment 1)",
     "/// <summary>SET data-pointer assignment (§14.9.39 Format 7; Phase-4b increment 1)"),
    # The pairing check: rule 12 is printed under the standard's own `FORMAT 7` banner.
    ("FORMAT-RULE",
     "// SET pointer TO NULL (ISO §14.9.39 Format 4, GR12 — the address is stored)",
     "// SET pointer TO NULL (ISO §14.9.39 Format 7, GR12 — the address is stored)"),
]


def ordinal_self_test(ordinals, tops) -> bool:
    if ordinals is None:
        print("  ⚠ THE ORDINAL CHECKS could not be self-tested — specs/ISO_COBOL.md is absent")
        return True
    ok = True
    for want, defect, repaired in ORDINAL_SELF_TEST:
        fires = {k for k, _s, _c, _m in _ordinal_findings("probe", [defect], ordinals, tops)}
        quiet = {k for k, _s, _c, _m in _ordinal_findings("probe", [repaired], ordinals, tops)}
        good = want in fires and not quiet
        ok &= good
        print(f"  {'ok  ' if good else 'FAIL'} fires {want:12s} on {defect.strip()[:56]}"
              + ("" if good else f"   (fired {sorted(fires)}, repaired twin fired {sorted(quiet)})"))
    return ok


def self_test(subjects, own, universe, ordinals=None, tops=None) -> int:
    """⛔ A GATE THAT HAS NEVER BEEN SEEN TO FAIL IS NOT EVIDENCE. Each check is fired on the exact defect it
    was written for, and then on its repaired twin, which must be silent."""
    import tempfile

    def kinds(comment: str, decl: str, suffix: str = ".g4") -> set[str]:
        with tempfile.TemporaryDirectory() as d:
            p = pathlib.Path(d) / ("probe" + suffix)
            p.write_text(comment + "\n" + decl + "\n", encoding="utf-8")
            _, parts, ctx, win, label, head = next(blocks(p))
            return {k for k, _c, _m in _checks(parts, ctx, win, subjects, own, universe, phantom=True,
                                              label=label, head=head)}

    ok = True
    for want, comment, decl, suffix in SELF_TEST:
        got = kinds(comment, decl, suffix)
        print(f"  {'ok  ' if want in got else 'FAIL'} fires {want:8s} on {comment.splitlines()[0][:66]}")
        ok &= want in got
    for comment, decl, suffix in SELF_TEST_CLEAN:
        got = kinds(comment, decl, suffix)
        print(f"  {'ok  ' if not got else 'FAIL'} silent on the REPAIRED {comment.splitlines()[0][:52]}"
              + (f"  (got {sorted(got)})" if got else ""))
        ok &= not got
    if universe is None:
        print("  ⚠ PHANTOM could not be self-tested — specs/ISO_COBOL.md is absent")
    ok &= ordinal_self_test(ordinals, tops)
    print("SELF-TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


def _related(a: str, b: str) -> bool:
    """One clause is the other, or is under it — `§14.9.25.4` answers a citation of `§14.9.25`."""
    return a == b or a.startswith(b + ".") or b.startswith(a + ".")


def _checks(parts, context, window, subjects, own, universe, phantom=False, label=None, head=None):
    """The three checks over ONE comment block — the ONE implementation, shared by the audit and the self-test
    so a self-test cannot pass against a check the audit no longer runs. `phantom` is off in the audit, where
    PHANTOM runs once per LINE over the wider corpus (`phantom_scan`) and would otherwise double-report.

    `label` is the sub-block whose citations LABEL the construct and `head` is the line the comment OPENS
    with (see `blocks`); SUBJECT reads the first, HEADER the second, PHANTOM the whole block. Both default
    to the whole block, which is what every non-`.cob` file yields."""
    out: list[tuple[str, str, str]] = []
    if label is None:
        label = parts
    if head is None:
        head = parts
    cited = [c for p in label for c in CITE.findall(p)]
    ctx_names = names_in(context)
    near = names_in(window)

    if phantom and universe is not None:
        for p in parts:
            for c in _phantoms(p, universe):
                out.append(("PHANTOM", c, f"the standard has no clause §{c}"))

    for c in cited:
        subj = subjects.get(c)
        if not subj or (m := NAMED.match(subj)) is None:
            continue
        head = m.group(1).split()[0]
        if head in near:
            continue
        other = sorted(w for w in ctx_names if w in own and not _related(own[w], c))
        if other:
            out.append(("SUBJECT", c,
                        f"§{c} is {subj}, and {head} is named nowhere near here; this comment is about "
                        + " / ".join(f"{w} (§{own[w]})" for w in other[:3])))

    if head and (hm := HEADER.match(head[0].strip())):
        words = hm.group(1).split()
        # ⛔ A TWO-WORD HEADER WHOSE SECOND WORD IS STRUCTURAL IS NOT A ONE-WORD CONSTRUCT. "REPORT SECTION
        # rules (§13.14 …)" is about the report section, not about the REPORT clause (§13.18.46) — falling
        # back to the first word accused a correct citation. `DELETE RECORD` still falls back, because RECORD
        # there is the statement's own optional word, not a structural noun.
        cands = [hm.group(1)] if len(words) > 1 and words[1] in STRUCTURAL else [hm.group(1), words[0]]
        for cand in cands:
            if (t := own.get(cand)) is None:
                continue
            if not any(_related(c, t) for c in cited):
                out.append(("HEADER", ",".join(cited),
                            f"the header names {cand} (§{t}) but cites " + ", ".join("§" + c for c in cited)))
            break
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", action="store_true", help="exit 1 on any GATING finding (the gate)")
    ap.add_argument("--check-all", action="store_true",
                    help="exit 1 on EVERY finding, the measured backlog included — the gate kb/Work's "
                         "rule-ordinal sweep drives to zero, and what --check becomes the day it gets there")
    ap.add_argument("--self-test", action="store_true", help="prove every check fails on a real defect")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    subjects = catalog_subjects()
    own = construct_clause(subjects)
    universe = spec_clauses()
    ordinals = spec_ordinals()
    tops = catalog_rule_tops()

    if args.self_test:
        return self_test(subjects, own, universe, ordinals, tops)

    if universe is None:
        print("⚠ PHANTOM AND THE ORDINAL CHECKS SKIPPED — specs/ISO_COBOL.md is absent (the private submodule "
              "is not checked out). SUBJECT and HEADER still run: spec-rule-catalog.json is committed.")

    findings = audit(subjects, own, universe, ordinals, tops)
    gating = [f for f in findings if f[0] not in MEASURED_BACKLOG]
    backlog = [f for f in findings if f[0] in MEASURED_BACKLOG]
    print(f"{len(citation_corpus.all_files())} files scanned for phantoms, {len(citation_corpus.declaration_files())} for construct agreement · {len(own)} constructs keyed to their own clause")
    print(f"⛔ {len(gating)} finding(s)\n")
    for kind, site, cited, msg in gating:
        print(f"  [{kind}] {site}\n      {msg}")
    if backlog:
        # ⛔ NOT SILENT, AND NOT GREEN EITHER. RULE and SUBITEM were SOUND on the day they were written and
        # found a backlog too large for the change that added them (kb/Work PB388 measured 132 + 72 sites in
        # 124 files, each needing its own derivation from the standard). Hiding them would be a green gate
        # over a known defect (feedback_green_test_can_hold_a_gap_open); failing on them would stop every
        # other lane on a backlog none of those lanes wrote. So they PRINT, every run, with their count and
        # their owner, and `--check-all` is the gate whoever burns them down runs.
        print(f"\n⚠ {len(backlog)} MEASURED, NOT YET GATING — rule-ordinal citations naming a rule number or "
              "sub-item its clause does not have. Each needs its own derivation from the standard; they are "
              "owned as one sweep in kb/Work (see PB388's report). `--check-all` gates on them.")
        if args.check:
            # Under the per-commit gate this is a HEADLINE, not a wall: the count is the fact another lane
            # needs, and the list is one command away. Run without --check (or with --check-all) to see it.
            print("   (run `python scripts/spec/audit_code_citations.py` for the list)\n")
        else:
            print()
            for kind, site, cited, msg in backlog:
                print(f"  [{kind}] {site}\n      {msg}")
    if args.check_all:
        return 1 if findings else 0
    return 1 if (gating and args.check) else 0


if __name__ == "__main__":
    sys.exit(main())
