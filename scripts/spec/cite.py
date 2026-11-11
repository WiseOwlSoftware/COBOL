#!/usr/bin/env python3
"""Validate a spec citation against the transcription of the edition it names — the mechanical check for CLAUDE.md
rule 1. `--check` takes a WHOLE citation in the format `scripts/spec/citation.py` specifies (`§<edition>:<locator>[ <rule>]`,
kb/Work PB4887); `--find` searches the transcription `--edition` names (default 2023: `specs/ISO_COBOL_2023.md`).

WHY THIS EXISTS. Rule 1 requires the exact §/GR for any semantics question, and the failure mode is not
inventing a citation — it is INHERITING one. A fix-queue entry or design doc carries "§8.4.1.2 GR2", the text
it quotes really does say what it claims, and the clause NUMBER is never re-derived; it then propagates into
code comments, goldens and DEVLOG entries as though it had been checked. Two of the CA10 citations were wrong
this way (the real clauses are §2023:8.4.2.3.4 GR2 and §2023:13.18.38.4 GR7) and nothing would have caught them.

⛔ IT READS THE MARKDOWN, NOT `spec-rule-catalog.json`. The catalog is derived and has two properties that make
it the wrong oracle here: it has no block at all for a clause whose rules are normative PROSE with lettered
items (§2023:14.8.2.3.3 is one — checking against the catalog reports a real citation as false), and its `ordinal`
is a per-block count that does NOT track the printed rule number once a rule has sub-letters (the abs rule
prints as 6)d)2.b) and the catalog calls it 2). Both produce confident wrong answers.

    python scripts/spec/cite.py --check "§2023:8.4.2.3.4 GR1" "EC-BOUND-SUBSCRIPT exception condition is set to exist"
    python scripts/spec/cite.py --check "§1985:VI.5.3.1" "A data description entry specifies the characteristics"
    python scripts/spec/cite.py --find  "shall fall within the bounds from integer-1"

`--check` exits non-zero unless the text appears INSIDE that clause's own region, so a wrong clause number
cannot pass quietly. That is the load-bearing guarantee. Both modes also print a best-effort RULE PATH
(`6) d)`) to help you write the ordinal — ⚠ it is APPROXIMATE below two levels of nesting, because the standard
mixes `a)` and `b.` styles at the same depth; read the printed rule when the ordinal itself is load-bearing.

⛔ A CHECK THAT CANNOT BE VALID FAILS, IT NEVER ANSWERS OK (kb/Work PB308). The comparison is on WORDS ONLY
(`norm`: punctuation is typography), so a quotation made entirely of punctuation — a table row such as
`| + | + | - |` — normalizes to the EMPTY string, which is a substring of every line of every clause. Both modes
refuse such a quotation outright; it is the gate failing open, which is worse than the gate failing.

THE CORRECTED TEXT AND THE PRINTED TEXT BOTH PASS (R66, specs/README.md "Corrections"). Where the transcription
corrects a defect of the printed standard, the corrected line is followed by a `> ⚠ **CORRECTED — see the
Addendum (Cn).**` note (`Errata` in 1985: verify_publishable.CORRECTIONS_SECTION) that quotes the PRINTED line verbatim, so a quotation of either form is found inside the
same clause, and the rule path reported for a quotation of the printed form is the corrected line's.

A RECONSTRUCTION PASSES, MARKED (kb/Work PB4952). Where the scan lacks printed pages, the transcription holds an
editorial reconstruction between a `> ⚠ **RECONSTRUCTED — not printed in our copy (folios ...).**` note and its
`> ⚠ **END RECONSTRUCTED ...**` note (`RECONSTRUCTED`). A quotation found there is reported with `RECONSTRUCTED_MARK`:
the citation names the standard's locator, but the text it was checked against is the transcription's, not the standard's.
"""
from __future__ import annotations

import argparse
import dataclasses
import functools
import pathlib
import re
import sys
import tempfile
from typing import NamedTuple
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
import citation  # noqa: E402  (scripts/spec: the one citation format and its parser, kb/Work PB4887)

# The citation itself — its edition, its locator and the regions of each edition's transcription a locator names — is
# `citation.py`'s (kb/Work PB4887): `--check` takes a WHOLE citation and checks its text inside that edition's region.
# The transcription escapes a rule label's delimiter (`1\)`) so Markdown does not eat it as a list — see
# repairs/rule_numbering.py. Match both forms so this works on any revision.
TOP = re.compile(r"^(\d+)\\?\)\s")
SUB = re.compile(r"^(\s{2,})([a-z])\\?\)\s")
SUBSUB = re.compile(r"^(\s{2,})(\d+)\\?\.\s")
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


@functools.lru_cache(maxsize=1 << 17)  # > the transcription's ~47,500 lines
def norm(s: str) -> str:
    """Compare on words only — dashes, quotes and spacing are typography, not content.

    Memoized, because an in-process caller checks many quotations against ONE loaded standard: `find` normalizes
    every line of the spec per call, and `audit_doc_citations.py` (kb/Work PB2525) spent 25 of its 33 s re-running
    these two regular expressions over the same ~47,500 lines for 59 `find` calls.

    MARKUP IS NOT CONTENT EITHER (kb/Work PB5256): the underline of a key word (`<u>FUNCTION</u> <u>ABS</u>`) left its
    tag's letter behind as a word ("u function u u abs u"), so no quotation of two underlined words, in a general format
    or a rule, could ever be found."""
    return re.sub(r"\s+", " ", re.sub(r"[^\w\s]", " ", _MARKUP_TAG.sub(" ", s))).strip().lower()


#: A markup tag of the transcription (`<u>`, `</u>`, `<pre style="line-height:1">`): typography, never words.
_MARKUP_TAG = re.compile(r"</?[a-zA-Z][^<>]*>")


def _indent(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def rule_path(lines, at, start):
    """The PRINTED rule path of the line at `at` — e.g. ['6)', 'd)', '2.'] — by walking back to each level.

    Two lines are not what their position suggests, and each used to report a sub-item it is not in (PB308):
      · an editorial note on a correction belongs to the line above it, so it takes that line's path;
      · an UNLETTERED paragraph at the sub-items' own indentation (or shallower) is a trailing paragraph of the
        PARENT rule — §2023:14.7.7 rule 2's "The composite of operands is a hypothetical data item…" follows its
        sub-items a) and b) at their indent and is neither — so it reports the bare rule, not the last letter.
    A paragraph INSIDE a sub-item sits deeper than its label, which is how the two are told apart.

    ⛔ A LABEL ABOVE IS AN ANCESTOR ONLY IF IT IS SHALLOWER THAN EVERYTHING ACCEPTED SO FAR (kb/Work PB55 review
    R1-1). A nested `N.` list is transcribed at the SAME indentation as the lettered items around it (§2023:7.2.3.4 9)
    c) 1.–6. sits at the indent of a)–f)), so the nearest `N.` above `d)` belongs to the EARLIER letter `c)`, and
    taking it reported the impossible `9) 6. d)`. Each label's depth is (indentation, style level): a deeper
    indent nests, and at one indent `N)` < `x)` < `N.`. Walking back, a label whose depth is not strictly below
    the last one accepted is a sibling or a sibling's descendant, never an ancestor, and is skipped."""
    while at > start and CORRECTION_NOTE.match(lines[at]):
        at -= 1
        while at > start and not lines[at].strip():
            at -= 1
    own = lines[at]
    is_label = bool(TOP.match(own) or SUB.match(own) or SUBSUB.match(own))
    own_indent = _indent(own)
    path = []
    # an unlabelled line is deeper than any label at its own indentation: (indent, a level below every style)
    bound = (own_indent, len(_LEVELS) + 1)
    for k in range(at, start - 1, -1):
        hit = _label(lines[k])
        if hit is None:
            continue
        depth, text = hit
        if depth >= bound:
            continue  # a sibling, or inside one: not on this line's path
        if depth[1] == 1:  # the rule itself (`N)` at column 0) ends the walk
            path.append(text)
            break
        if is_label or own_indent > depth[0]:
            path.append(text)
        bound = depth
    return " ".join(reversed(path))


#: The label styles by nesting level at one indentation: the rule `N)`, its lettered items `x)`, their `N.` items.
_LEVELS = ((TOP, ")"), (SUB, ")"), (SUBSUB, "."))


def _label(line: str):
    """((indent, style level), printed label) for a rule-label line, else None."""
    for level, (pattern, delim) in enumerate(_LEVELS, start=1):
        if m := pattern.match(line):
            return (_indent(line), level), f"{m.groups()[-1]}{delim}"
    return None


def find(lines, cls, text, edition: int):
    """(exit code, output lines) for `--find`: every region line of `edition`'s transcription that contains TEXT."""
    needle = norm(text)
    if not needle:
        return 1, [f"REFUSED: {EMPTY_NEEDLE}"]
    hits = [(n, t, s, e, i) for (n, t, s, e) in cls
            for i in range(s, e) if needle in norm(lines[i])]
    if not hits:
        return 1, [f"NO CLAUSE contains {text!r} — the citation names text the standard does not have"]
    out = []
    for n, t, s, _e, i in hits[:12]:
        mark = f" {RECONSTRUCTED_MARK}" if in_reconstruction(lines, i) else ""
        out.append(f"§{edition}:{n} {rule_path(lines, i, s)}  ({t}){mark}\n    {lines[i].strip()[:190]}\n")
    out.append(f"{len(hits)} occurrence(s)")
    return 0, out


def check(lines, cls, cited: citation.Citation, text):
    """(exit code, output lines) for `--check`: TEXT must appear inside the cited locator's own region, and, when the
    citation of an ISO edition names a numbered rule (`GR6d.2`, `AR1`: citation.NUMBERED_RULE), under that rule: the
    printed path must open with `6)`. A 1985 rule is not checked: that transcription numbers its rules in another style, which `rule_path`
    does not read (its reconciliation is kb/Work PB4850).

    ⛔ THE RULE ORDINAL IS CHECKED, NOT ONLY THE CLAUSE (kb/Work PB4887). A whole citation carries its rule, and a
    sentence in the right clause under the wrong rule is the inherited-citation shape kb/Work PB388 measured."""
    shown = citation.render(dataclasses.replace(cited, rule=None))
    region = [c for c in cls if c[0] == cited.locator]
    if not region:
        return 1, [f"FAIL: there is no {shown} in the {cited.edition} transcription"]
    needle = norm(text)
    if not needle:
        return 1, [f"FAIL: {EMPTY_NEEDLE}"]
    iso = citation.EDITIONS[cited.edition].locator == "clause"
    ordinal = m.group(2) if iso and cited.rule and (m := citation.NUMBERED_RULE.match(cited.rule)) else None
    elsewhere = []
    for n, t, s, e in region:
        for i in range(s, e):
            if needle in norm(lines[i]):
                path = rule_path(lines, i, s)
                if ordinal is None or path.split(" ")[0] == f"{ordinal})":
                    mark = f" {RECONSTRUCTED_MARK}" if in_reconstruction(lines, i) else ""
                    return 0, [f"OK  §{cited.edition}:{n} {path}  ({t}){mark}\n    {lines[i].strip()[:220]}"]
                elsewhere.append(path or "(no rule)")
    if elsewhere:
        return 1, [f"FAIL: {shown} contains {text!r}, but under rule {elsewhere[0]}, not {cited.rule}"]
    return 1, [f"FAIL: {shown} exists but does NOT contain {text!r}",
               "      run --find to locate the text and get the clause it is really in"]


def load(edition: int, repo: pathlib.Path = spec_pdf.REPO):
    """The lines and citable regions of `edition`'s transcription (`citation.regions`); exits while it is absent."""
    loaded = citation.regions(edition, repo)
    if loaded is None:
        source = citation.SOURCE_EDITION.get(edition, edition)
        where = f"({spec_pdf.markdown(source, repo).relative_to(repo).as_posix()}) " if source in spec_pdf.EDITIONS else ""
        sys.exit(f"FATAL: the {edition} transcription {where}does not exist in this tree.")
    return loaded


def self_test() -> int:
    """Each guard is shown to FIRE on its defect and to stay SILENT on its neighbour (kb/Work PB308, PB309)."""
    lines, cls = load(2023)

    def c(text: str) -> citation.Citation:
        return citation.parse(text)

    with tempfile.TemporaryDirectory() as td:
        planted = pathlib.Path(td)
        planted_md = spec_pdf.markdown(2002, planted)
        planted_md.parent.mkdir()
        planted_md.write_text("## 9.9 Planted\n\nthe planted sentence\n", encoding="utf-8", newline="\n")
        edition_2002 = check(*load(2002, planted), c("§2002:9.9"), "the planted sentence")[0]
        try:
            load(1985, planted)
            absent_1985 = False
        except SystemExit as ex:
            absent_1985 = "1985" in str(ex.code)
    planted_notes = ["printed", "> ⚠ **RECONSTRUCTED — not printed in our copy (folio XIII-54).** Sources.", "edited",
                     "> ⚠ **END RECONSTRUCTED (folio XIII-54).** Printed again.", "printed",
                     "> ⚠ **RECONSTRUCTED — not printed in our copy (folios XIII-30 and XIII-31).**", "unclosed"]
    cases = [
        ("a quotation of underlined key words is found: the markup is no word (kb/Work PB5256)",
         norm("FUNCTION ABS") in norm("<u>FUNCTION</u> <u>ABS</u> ( argument-1 )")
         and norm("<u>MOVE</u>") == "move" and norm("a < b > c") == "a b c"),
        ("reads the transcription of the edition the citation names (2002), not 2023's", edition_2002 == 0),
        ("an edition whose transcription is absent exits naming it", absent_1985),
        ("checks a 1985 citation against the 1985 transcription",
         check(*load(1985), c("§1985:VI.5.3.1"), "A data description entry specifies the characteristics")[0] == 0),
        ("fires on a 1985 paragraph of the wrong section",
         check(*load(1985), c("§1985:VII.5.3.1"), "A data description entry specifies the characteristics")[0] == 1),
        # kb/Work PB4952: a quotation of a reconstruction passes MARKED; the printed text beside it carries no mark.
        ("marks a citation verified against a reconstruction (TYPE SR1, folio XIII-55)",
         RECONSTRUCTED_MARK in check(*load(1985), c("§1985:XIII.3.20.3"), "RH is an abbreviation for REPORT HEADING")[1][0]),
        ("does not mark the printed text after a reconstruction (TYPE SR7, folio XIII-56)",
         RECONSTRUCTED_MARK not in check(*load(1985), c("§1985:XIII.3.20.3"),
                                         "The description of a report must include at least one body group")[1][0]),
        ("pairs the notes, and reports an unclosed opening and an unopened closing",
         [(r.start, r.end) for r in reconstructions(planted_notes)] == [(1, 3), (5, None)]
         and [(r.start, r.end) for r in reconstructions(planted_notes[3:4])] == [(-1, 0)]
         and reconstructed(planted_notes) == [None, "XIII-54", "XIII-54", "XIII-54", None, "XIII-30", "XIII-30"]),
        ("fires EMPTY-NEEDLE (check)", check(lines, cls, c("§2023:7.1"), "| + | + | - |")[0] == 1),
        ("fires EMPTY-NEEDLE (find)", find(lines, cls, "| + | - |", 2023)[0] == 1),
        ("silent on a needle carrying digits", check(lines, cls, c("§2023:15.43.4"), "| S999 | +999 |")[0] == 0),
        ("pins 14.9.8.4 1) b)", "1) b)" in check(lines, cls, c("§2023:14.9.8.4"), "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value")[1][0]),
        ("accepts the rule the text is under (GR1)", check(lines, cls, c("§2023:14.9.8.4 GR1"), "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value")[0] == 0),
        ("fires on a rule the text is not under (GR2)", check(lines, cls, c("§2023:14.9.8.4 GR2"), "Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value")[0] == 1),
        ("pins 14.7.7 2) bare (trailing paragraph)", check(lines, cls, c("§2023:14.7.7"), "The composite of operands is a hypothetical data item")[1][0].startswith("OK  §2023:14.7.7 2)  (")),
        # PB55 review R1-1: a letter AFTER a nested `N.` list at its own indent is the list's sibling, not inside it
        ("pins 7.2.3.4 9) d) (letter after a nested list)", check(lines, cls, c("§2023:7.2.3.4"), "If no match occurs, the comparison is repeated with each")[1][0].startswith("OK  §2023:7.2.3.4 9) d)  (")),
        ("pins 12.3.7.3 17) d) (letter after a nested list)", check(lines, cls, c("§2023:12.3.7.3"), "Alphabet-name-4 shall not reference an alphabet specified with the LOCALE phrase")[1][0].startswith("OK  §2023:12.3.7.3 17) d)  (")),
        ("pins 7.2.3.4 9) c) 2. (the nested item keeps its letter)", check(lines, cls, c("§2023:7.2.3.4"), "Each operand and operator of a concatenation expression is a separate text-word")[1][0].startswith("OK  §2023:7.2.3.4 9) c) 2.  (")),
        ("pins 8.8.3.3 3)", check(lines, cls, c("§2023:8.8.3.3"), "equivalent to a literal")[1][0].startswith("OK  §2023:8.8.3.3 3)")),
        ("accepts the CORRECTED form (C6)", check(lines, cls, c("§2023:14.9.10.4"), "The value '62' is placed into the I-O status")[0] == 0),
        ("accepts the PRINTED form (C6)", check(lines, cls, c("§2023:14.9.10.4"), "The valu62' is placed into the I-O status")[0] == 0),
        ("accepts the PRINTED form (C12)", check(lines, cls, c("§2023:14.9.30.4"), "whose key value is greater than or equal to the key value in the file position indicator")[0] == 0),
        ("fires on a wrong clause", check(lines, cls, c("§2023:14.9.27.4"), "The valu62' is placed into the I-O status")[0] == 1),
        ("--find prints the edition form", find(lines, cls, "equivalent to a literal", 2023)[1][0].startswith("§2023:")),
    ]
    for name, ok in cases:
        print(f"{'PASS' if ok else 'FAIL'}  {name}")
    bad = [n for n, ok in cases if not ok]
    print("SELF-TEST: " + ("FAIL " + str(bad) if bad else "PASS"))
    return 1 if bad else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--check", nargs=2, metavar=("CITATION", "TEXT"),
                    help="assert TEXT appears inside the cited region (and rule, when the citation names one): "
                         "--check \"§2023:14.9.8.4 GR1\" \"<text>\"; exit 1 if not")
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
        code, out = check(*load(cited.edition), cited, args.check[1])
    print("\n".join(out))
    return code


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
