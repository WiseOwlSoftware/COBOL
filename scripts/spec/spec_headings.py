"""THE ONE DEFINITION of what a "general format" clause heading looks like (kb/Work PB284, branch review round 1).

The standard heads 321 clauses "General format(s)" and exactly ONE "General forms" (§14.9.7.2, the COMMIT statement).
Seven scripts and four tests each carried their own regex for the heading, and keyed on "format" alone: the extractor
was repaired and the siblings kept silently skipping the one clause that spells the noun otherwise (the figure sweep
reported ``figures generated: 0 / CHECK: clean`` for a hand-placed figure). One rule, one place: every script that
needs to ask "is this a general-format heading" imports it from here, the C# tests read the same spelling from
``SpecClauseText.GeneralFormatHeading``, and a regex or substring test of the heading written anywhere else fails one
of the two halves of the no-copy sweep: this module's ``--self-test`` for every tracked Python script under scripts/
(``private_copies``), ``CatalogCoverageDriftTests.NoCompilerOrTestSource_SpellsTheGeneralFormatHeadingItself`` for the
C# under src/ and tests/ (kb/Work PB4395 item 1: a fact about the scripts lives in a script's self-test).
"""

from __future__ import annotations

import re
import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: The noun phrase, both nouns, singular and plural. Compose it; never re-type it.
GENERAL_FORMAT_NOUN = r"general\s+(?:formats?|forms?)"

#: A clause TITLE (or a heading's text) that ends in the phrase: ``search`` it in a PDF heading that carries its clause
#: number, ``fullmatch`` it against a bare title.
GENERAL_FORMAT_TITLE = re.compile(GENERAL_FORMAT_NOUN + r"\.?\s*$", re.IGNORECASE)

#: The phrase as printed ANYWHERE in a page's text layer (capitalised, as a heading is) — the cheap page filter.
GENERAL_FORMAT_PRINTED = re.compile(r"General\s+(?:formats?|forms?)\b")

# ── the no-copy sweep, Python half: a script that WRITES the heading as a pattern instead of importing it ────────────

#: The heading's words spelled out as a pattern: capitalised as a heading is (``General formats?``, ``General\s+form``)
#: or as a lower-case regex (``general\s+form``).
_WORDS_SPELLED_OUT = re.compile(r"General(?:\\s[+*]?|\s)\(?\??:?form|general\\s[+*]?\(?\??:?form")
#: A line that compiles or applies a pattern.
_PATTERN_CONTEXT = re.compile(r"\bre\.(?:compile|search|match|fullmatch|findall|sub)\(|\bcompile\(")
#: A substring test of the heading in ANY case (``"General format" not in page_text``, ``"general format" in
#: title.lower()``, ``title.startswith("general form")``): the case-folded copy branch review round 2 found (R2-1).
_MEMBERSHIP_TEST = re.compile(r"""["']general\s?form[^"']*["']\s+(?:not\s+)?in\b"""
                              r"""|\.(?:startswith|endswith|find|count)\(\s*["']general\s?form""", re.IGNORECASE)
#: A lower-case regex of the heading inside an ``re`` call: ``re.compile(r"general formats?", re.I)``.
_LOWER_PATTERN = re.compile(r"\bre\.(?:compile|search|match|fullmatch|findall|sub)\(.*general(?:\\s[+*]?|\s)\(?\??:?form")


def spells_the_heading(line: str) -> bool:
    """Whether a Python `line` writes the general-format heading as a pattern of its own (a comment never does)."""
    if line.lstrip().startswith("#"):
        return False
    return (bool(_WORDS_SPELLED_OUT.search(line) and _PATTERN_CONTEXT.search(line))
            or bool(_MEMBERSHIP_TEST.search(line)) or bool(_LOWER_PATTERN.search(line)))


def private_copies(repo: Path) -> tuple[int, list[str]]:
    """(scripts scanned, `path:line: text` of each private copy) over every tracked Python script under scripts/ but
    this module, the heading's one owner."""
    import subprocess  # noqa: PLC0415

    out = subprocess.run(["git", "-C", str(repo), "ls-files", "-z", "--", "scripts/"],
                         capture_output=True, check=True).stdout.decode("utf-8")
    own = Path(__file__).resolve().relative_to(repo.resolve()).as_posix()
    scanned, offenders = 0, []
    for rel in sorted(p for p in out.split(chr(0)) if p.endswith(".py") and p != own):
        try:
            lines = (repo / rel).read_text(encoding="utf-8").splitlines()
        except (OSError, UnicodeDecodeError):
            continue  # deleted in the working tree, or not text
        scanned += 1
        offenders += [f"{rel}:{i}: {x.strip()}" for i, x in enumerate(lines, 1) if spells_the_heading(x)]
    return scanned, offenders


def _self_test() -> int:
    """The heading rule on every spelling the standard uses, and in AGREEMENT with the C# tests' spelling.

    The C# drift guards cannot import this module, so their pattern is a second copy by necessity
    (``tests/_shared/SpecClauseText.cs#GeneralFormatNoun``). This reads that constant out of the C# source and holds
    the two to the SAME set of clauses over the transcription: the rule is written twice, and it cannot drift.
    """
    import pathlib

    repo = pathlib.Path(__file__).resolve().parents[2]
    spellings = ["General format", "General formats", "General forms", "General format.", "general FORMATS"]
    not_headings = ["General", "General rules", "General description", "Syntax rules", "Subscripting general", "Format 1"]
    failures: list[str] = []
    for s in spellings:
        if not GENERAL_FORMAT_TITLE.fullmatch(s):
            failures.append(f"title not recognised: {s!r}")
    for s in not_headings:
        if GENERAL_FORMAT_TITLE.search(s):
            failures.append(f"title wrongly recognised: {s!r}")
    # a PDF heading carries its clause number in front of the title
    if not GENERAL_FORMAT_TITLE.search("14.9.7.2 General forms"):
        failures.append("PDF heading '14.9.7.2 General forms' not recognised")
    if not GENERAL_FORMAT_PRINTED.search("see 7.3.23.2 General format."):
        failures.append("page-text filter missed 'General format.'")

    cs = (repo / "tests" / "_shared" / "SpecClauseText.cs").read_text(encoding="utf-8")
    m = re.search(r'GeneralFormatNoun\s*=\s*@"([^"]+)"', cs)
    if not m:
        failures.append("tests/_shared/SpecClauseText.cs declares no GeneralFormatNoun")
    else:
        heading = re.compile(r"^#+\s+(\d+(?:\.\d+)*)\s+" + m.group(1) + r"\s*$", re.IGNORECASE)
        py_heading = re.compile(r"^#+\s+(\d+(?:\.\d+)*)\s+(.+?)\s*$")
        spec = (repo / "specs" / "ISO_COBOL.md").read_text(encoding="utf-8").splitlines()
        cs_set = {h.group(1) for line in spec if (h := heading.match(line))}
        py_set = {h.group(1) for line in spec
                  if (h := py_heading.match(line)) and GENERAL_FORMAT_TITLE.fullmatch(h.group(2).strip())}
        if cs_set != py_set:
            failures.append(f"C# and Python disagree on the general-format clauses: C# only {sorted(cs_set - py_set)[:5]}, "
                            f"Python only {sorted(py_set - cs_set)[:5]}")
        for must in ("14.9.7.2", "7.3.23.2", "14.9.36.2"):
            if must not in cs_set:
                failures.append(f"section {must} is not a recognised general-format heading")
        if len(cs_set) < 320:
            failures.append(f"only {len(cs_set)} general-format headings found; the standard prints 322")

    # The no-copy sweep can fail: each Python way of writing the heading is flagged, a keyword list or a comment is not.
    flagged = [r'GF = re.compile(r"general\s+formats?$", re.I)', 'if "General format" not in page.get_text():',
               'in_formats = "general format" in title.lower()', 'GF = re.compile(r"general formats?", re.I)',
               'if title.lower().startswith("general form"):', r'm = re.match(r"^#+ \d+ General formats?$", t)']
    prose = ['KEYS = ("syntax rule", "general format", "non-ISO")', "    # the clause that prints its General formats",
             'print("General format rows:", n)']
    failures += [f"the sweep misses the copy {x!r}" for x in flagged if not spells_the_heading(x)]
    failures += [f"the sweep flags the non-copy {x!r}" for x in prose if spells_the_heading(x)]
    scanned, offenders = private_copies(repo)
    if scanned < 100:
        failures.append(f"only {scanned} tracked scripts scanned: the no-copy sweep reads nothing")
    failures += [f"a script writes the general-format heading pattern itself (import GENERAL_FORMAT_TITLE or "
                 f"GENERAL_FORMAT_PRINTED from scripts/spec/spec_headings.py): {o}" for o in offenders]
    for f in failures:
        print("FAIL:", f)
    print("spec_headings self-test:", "FAIL" if failures else "OK")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    import sys

    sys.exit(_self_test() if sys.argv[1:] == ["--self-test"] else 2)
