"""THE ONE DEFINITION of what a "general format" clause heading looks like (kb/Work PB284, branch review round 1).

The standard heads 321 clauses "General format(s)" and exactly ONE "General forms" (§14.9.7.2, the COMMIT statement).
Seven scripts and four tests each carried their own regex for the heading, and keyed on "format" alone: the extractor
was repaired and the siblings kept silently skipping the one clause that spells the noun otherwise (the figure sweep
reported ``figures generated: 0 / CHECK: clean`` for a hand-placed figure). One rule, one place: every script that
needs to ask "is this a general-format heading" imports it from here, the C# tests read the same spelling from
``SpecClauseText.GeneralFormatHeading``, and ``CatalogCoverageDriftTests.NoScriptOrTest_SpellsTheGeneralFormatHeadingItself``
fails when a regex or substring test of the heading is written anywhere else.
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
    for f in failures:
        print("FAIL:", f)
    print("spec_headings self-test:", "FAIL" if failures else "OK")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    import sys

    sys.exit(_self_test() if sys.argv[1:] == ["--self-test"] else 2)
