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

THE THREE CHECKS, each narrow on purpose:

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
    r"ed|A CLAUSE THAT DOES NOT")
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


def audit(subjects: dict[str, str], own: dict[str, str], universe: set[str] | None):
    findings: list[tuple[str, str, str, str]] = phantom_scan(universe)
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


def self_test(subjects, own, universe) -> int:
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
    ap.add_argument("--check", action="store_true", help="exit 1 on any finding (the gate)")
    ap.add_argument("--self-test", action="store_true", help="prove every check fails on a real defect")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    subjects = catalog_subjects()
    own = construct_clause(subjects)
    universe = spec_clauses()

    if args.self_test:
        return self_test(subjects, own, universe)

    if universe is None:
        print("⚠ PHANTOM CHECK SKIPPED — specs/ISO_COBOL.md is absent (the private submodule is not checked "
              "out). SUBJECT and HEADER still run: spec-rule-catalog.json is committed.")

    findings = audit(subjects, own, universe)
    print(f"{len(citation_corpus.all_files())} files scanned for phantoms, {len(citation_corpus.declaration_files())} for construct agreement · {len(own)} constructs keyed to their own clause")
    print(f"⛔ {len(findings)} finding(s)\n")
    for kind, site, cited, msg in findings:
        print(f"  [{kind}] {site}\n      {msg}")
    return 1 if (findings and args.check) else 0


if __name__ == "__main__":
    sys.exit(main())
