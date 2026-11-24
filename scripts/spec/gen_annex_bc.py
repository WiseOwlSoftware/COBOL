#!/usr/bin/env python3
"""Generate the Annex B and Annex C tables from the ISO text (kb/Work PB1402).

The ISO standard is the authority for two compile-time tables (CLAUDE.md rule 1):

* Annex B.3 — the characters permitted in a user-defined word, in three POSITION classes: (1) anywhere,
  (2) anywhere but first, (3) anywhere but first or last (§2023:8.1.3.2 GR4; §2023:8.3.2.1).
* Annex C.2 — the uppercase-to-lowercase case mapping that folds COBOL words (§2023:8.1.3.2 GR3b and GR4b).
  The fold APPLIES a pair only when both its letters are basic or both extended (kb/Work PB4420, owner decision
  2026-11-01): GR4 b) folds an uppercase extended letter to "its corresponding lowercase EXTENDED letter", so a pair
  that crosses between the two classes (today (0130,0069), (0131,0069) and (212A,006B)) is transcribed from the standard
  into NotApplied and left out of the applied lists. The generator DERIVES that set from the lists; there is no hand list.

Both are given for COBOL 2023; the COBOL 2014 tables are the 2023 ones with Annex E's recorded changes
reversed — E.2 item 4 (037A deleted, 30FB made medial), E.2 item 14 (two mappings deleted), E.3.3 item 4
(characters newly allowed first), E.3.3 item 5 (characters added) and E.3.3 item 6 (mappings added). COBOL 2002 has its OWN repertoire, Annex C.2 of
specs/ISO_COBOL_2002.md (TR 10176:2001, narrower than 2014's and 2023's), transcribed as C2002Letters and C2002Special;
the 2002 text (§2002:8.1.2.1 GR4b) also bars the combining characters of TR 10176:2001 Annex A from the first and last
place, and Annex C does not label them, so the combining rows that COBOL 2014's Annex B.3.1 does label are transcribed
(B2014Combining) to be intersected with the 2002 repertoire. The generator transcribes each list separately, exactly as the standard prints it, so
AnnexBCDriftTests can re-read every list from specs/ISO_COBOL_2023.md and compare.

Usage: python scripts/spec/gen_annex_bc.py [--check]   (--check: exit 1 when a committed table differs)
"""
from __future__ import annotations

import re
import sys
import unicodedata
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
SPEC = spec_pdf.markdown(2023)

# The section boundaries: (name, first line that OPENS the list, the line that CLOSES it). Each opener and closer
# is a literal prefix of one line of specs/ISO_COBOL_2023.md; the drift test uses the same markers.
SECTIONS = [
    ("B3Anywhere", r"1\) The following characters are permitted in a user-defined word.",
     r"2\) The following characters are permitted in a user-defined word except as the start character."),
    ("B3NotFirst", r"2\) The following characters are permitted in a user-defined word except as the start character.",
     r"3\) The following characters are permitted in a user-defined word except as the start or last character."),
    ("B3Medial", r"3\) The following characters are permitted in a user-defined word except as the start or last character.",
     '<a id="section-annex-c"></a>'),
    ("C2Mappings", "### C.2 General case mappings", '<a id="section-annex-d"></a>'),
    ("E2Item4Deleted", r"4\) **Characters permitted in user-defined words.** The following character, represented",
     "   The following character has been changed so it is not allowed for the start or last character"),
    ("E2Item4MadeMedial", "   The following character has been changed so it is not allowed for the start or last character",
     "   Justification:"),
    ("E2Item14Deleted", r"14\) **General case mappings.** The following case mappings have been deleted.", "Justification:"),
    ("E33Item4MadeFirst", r"4\) **Characters permitted in user-defined words.** The following characters have been changed",
     r"5\) **Characters permitted in user-defined words.** The following characters have been added."),
    ("E33Item5Added", r"5\) **Characters permitted in user-defined words.** The following characters have been added.",
     r"6\) **General case mappings.** The following case mappings have been added."),
    ("E33Item6Added", r"6\) **General case mappings.** The following case mappings have been added.",
     r"7\) **Clarification** of exception handling procedures."),
]

RANGE = re.compile(r"(?<![0-9A-Za-z])([0-9A-F]{4,5})(?:-([0-9A-F]{4,5}))?(?![0-9A-Za-z])")
PAIR = re.compile(r"\(([0-9A-F]{4,5}),([0-9A-F]{4,5})\)")
# A line of a list is hex ranges, script-name labels ("Old_Italic:"), NOTE prose or blank — anything else is an
# OCR defect the generator must not silently skip.
LABEL = re.compile(r"^[A-Za-z][A-Za-z_ ]*:$")


def section_lines(lines: list[str], opener: str, closer: str) -> list[str]:
    start = next(i for i, l in enumerate(lines) if l.startswith(opener))
    end = next(i for i in range(start + 1, len(lines)) if lines[i].startswith(closer))
    return lines[start + 1:end]


def ranges_of(body: list[str]) -> list[tuple[int, int]]:
    out: list[tuple[int, int]] = []
    for line in body:
        text = line.strip()
        if not text or text.startswith("NOTE") or LABEL.match(text) or text.startswith("```"):
            continue
        for m in RANGE.finditer(text):
            lo = int(m.group(1), 16)
            hi = int(m.group(2), 16) if m.group(2) else lo
            if hi < lo:
                raise SystemExit(f"descending range {m.group(0)} in: {text}")
            out.append((lo, hi))
        residue = RANGE.sub("", text)
        residue = re.sub(r"[A-Za-z_]+:", "", residue)          # an inline script label ("1E94B Ahom:")
        residue = residue.replace("—", "").replace(",", "").strip()
        if residue and not re.fullmatch(r"[A-Z][A-Z \-]*", residue):   # a character NAME after a code point
            raise SystemExit(f"unparsed residue {residue!r} in: {text}")
    return out


def pairs_of(body: list[str]) -> list[tuple[int, int]]:
    return [(int(a, 16), int(b, 16)) for line in body for a, b in PAIR.findall(line)]


def merge(rs: list[tuple[int, int]]) -> list[tuple[int, int]]:
    rs = sorted(rs)
    out: list[tuple[int, int]] = []
    for lo, hi in rs:
        if out and lo <= out[-1][1] + 1:
            out[-1] = (out[-1][0], max(out[-1][1], hi))
        else:
            out.append((lo, hi))
    return out


def extract() -> dict[str, list[tuple[int, int]]]:
    lines = SPEC.read_text(encoding="utf-8").splitlines()
    data: dict[str, list[tuple[int, int]]] = {}
    for name, opener, closer in SECTIONS:
        body = section_lines(lines, opener, closer)
        data[name] = pairs_of(body) if name.startswith(("C2", "E2Item14", "E33Item6")) else merge(ranges_of(body))
    return data


#: (name, edition, the line that OPENS the table's section, the line that CLOSES it, which table rows are kept).
TABLE_SECTIONS = [
    # §2002 Annex C.2.1: every row but "Special characters" is a letter or digit the word may hold anywhere; the
    # Special row is barred from the first and last place (§2002:8.1.2.1 GR4b). C.2.2 adds MIDDLE DOT (00B7) as a
    # special character; its hyphen-minus and "underscore" lines are basic characters, judged by the basic rules.
    ("C2002Letters", 2002, "##### C.2.1 Characters from TR 10176:2001, Annex A", "##### C.2.2 Additional characters",
     lambda label: "pecial" not in label),
    ("C2002Special", 2002, "##### C.2.1 Characters from TR 10176:2001, Annex A", "##### C.2.2 Additional characters",
     lambda label: "pecial" in label),
    ("C2002Additional", 2002, "##### C.2.2 Additional characters", "## Page 723", None),
    # §2014 Annex B.3.1 labels its combining rows; 2002 Annex C does not, and cites TR 10176:2001 Annex A for them.
    ("B2014Combining", 2014, "##### B.3.1 Characters from TR 10176:2003, Annex A", "##### B.3.2 Additional characters",
     lambda label: "ombining" in label),
]


def table_ranges(lines: list[str], opener: str, closer: str, keep) -> list[tuple[int, int]]:
    """The ranges of the rows of a markdown table (`| label | ranges |`) between two markers whose label `keep` accepts.
    A row with an empty label continues the row above it across a page break. `keep` None: the section is a list of
    "002D (hyphen-minus)" lines, of which only the characters beyond the basic ones are kept."""
    body = section_lines(lines, opener, closer)
    if keep is None:
        return [(lo, hi) for lo, hi in ranges_of([re.sub(r"\(.*?\)|\\", "", l) for l in body]) if lo >= 0x80]
    label, out = "", []
    for line in body:
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if not line.lstrip().startswith("|") or len(cells) != 2 or set(cells[0] + cells[1]) <= set("-: "):
            continue
        if cells[0]:
            label = cells[0]
        if label.startswith("Script or") or not RANGE.search(cells[1]):
            continue
        if keep(label):
            out.extend(ranges_of([cells[1]]))
    return out


def extract_tables() -> dict[str, list[tuple[int, int]]]:
    cache: dict[int, list[str]] = {}
    out = {}
    for name, edition, opener, closer, keep in TABLE_SECTIONS:
        lines = cache.setdefault(edition, spec_pdf.markdown(edition).read_text(encoding="utf-8").splitlines())
        out[name] = merge(table_ranges(lines, opener, closer, keep))
    return out


def plausibility(data: dict[str, list[tuple[int, int]]]) -> list[str]:
    """Flag a code point whose Unicode category cannot be what its list says (an OCR digit slip). Python's
    unicodedata is a newer Unicode than 13.0.0, so this is a sanity net, never the authority."""
    notes: list[str] = []
    for lo, hi in data["B3Anywhere"]:
        for cp in range(lo, hi + 1):
            cat = unicodedata.category(chr(cp))
            if cat not in ("Lu", "Ll", "Lt", "Lm", "Lo", "Nl", "Nd") and cp not in (0x2118, 0x212E, 0x309B, 0x309C):
                notes.append(f"B3Anywhere {cp:04X} has category {cat}")
    for lo, hi in data["B3NotFirst"]:
        for cp in range(lo, hi + 1):
            cat = unicodedata.category(chr(cp))
            if cat not in ("Mn", "Mc", "Nd", "Pc", "Lo", "Lm", "No") and cp not in (0x00B7, 0x0387, 0x19DA):
                notes.append(f"B3NotFirst {cp:04X} has category {cat}")
    for up, low in data["C2Mappings"]:
        if chr(up).lower() != chr(low):
            notes.append(f"C2 ({up:04X},{low:04X}) differs from Unicode's lowercase {ord(chr(up).lower()[0]):04X}")
    return notes


def is_basic_letter(cp: int) -> bool:
    """A basic letter: A-Z or a-z (§2023:8.1.3.1 Table 1); every other letter Annex B holds is an EXTENDED letter."""
    return 0x41 <= cp <= 0x5A or 0x61 <= cp <= 0x7A


def crosses_classes(pair: tuple[int, int]) -> bool:
    """Whether an (uppercase, lowercase) pair maps between the basic and the extended letters (§2023:8.1.3.2 GR4b)
    names the corresponding lowercase EXTENDED letter of an extended one; GR3 b) the basic letters among themselves)."""
    return is_basic_letter(pair[0]) != is_basic_letter(pair[1])


#: The three pair lists the fold reads; each loses its crossing pairs to NotApplied (in this order).
FOLD_LISTS = ("C2Mappings", "E2Item14Deleted", "E33Item6Added")


def applied_lists(data: dict[str, list[tuple[int, int]]]) -> dict[str, list[tuple[int, int]]]:
    """`data` with each fold list reduced to the pairs the fold applies, and NotApplied the pairs it left out."""
    out = dict(data)
    out["NotApplied"] = [p for name in FOLD_LISTS for p in data[name] if crosses_classes(p)]
    for name in FOLD_LISTS:
        out[name] = [p for p in data[name] if not crosses_classes(p)]
    return out


def render(data: dict[str, list[tuple[int, int]]], names: list[str], namespace: str, cls: str, what: str) -> str:
    def fmt(rs: list[tuple[int, int]]) -> str:
        items = [f"(0x{lo:04X}, 0x{hi:04X})" for lo, hi in rs]
        rows = [", ".join(items[i:i + 6]) for i in range(0, len(items), 6)]
        return "\n".join(f"        {r}," for r in rows)

    out = [
        "// <auto-generated>",
        "// Generated by scripts/spec/gen_annex_bc.py — DO NOT EDIT; re-run the script.",
        f"// Source: specs/ISO_COBOL_2023.md — {what}, and the Annex E",
        "// changes that reverse it for the earlier editions" + (
            "; the COBOL 2002 repertoire is specs/ISO_COBOL_2002.md Annex C.2 with the combining rows of"
            "\n// specs/ISO_COBOL_2014.md Annex B.3.1. " if "C2002Letters" in names else ". ")
        + "AnnexBCDriftTests re-reads every list from the spec.",
        "// </auto-generated>",
        f"namespace {namespace};",
        "",
        f"public static partial class {cls}",
        "{",
    ]
    docs = {
        "B3Anywhere": "Annex B.3 item 1 (COBOL 2023): permitted anywhere in a user-defined word.",
        "B3NotFirst": "Annex B.3 item 2 (COBOL 2023): permitted except as the start character.",
        "B3Medial": "Annex B.3 item 3 (COBOL 2023): permitted except as the start or last character.",
        "C2Mappings": "Annex C.2 (COBOL 2023): the general case mappings (uppercase, lowercase) the fold of a COBOL word "
                      "applies, less <see cref=\"NotApplied\"/>.",
        "E2Item4Deleted": "Annex E.2 item 4: deleted from Annex B at COBOL 2023 (present before).",
        "E2Item4MadeMedial": "Annex E.2 item 4: made medial-only at COBOL 2023 (permitted anywhere before).",
        "E2Item14Deleted": "Annex E.2 item 14: case mappings deleted at COBOL 2023 (present before), less <see cref=\"NotApplied\"/>.",
        "E33Item4MadeFirst": "Annex E.3.3 item 4: newly permitted as the first character at COBOL 2023 (not first before).",
        "E33Item5Added": "Annex E.3.3 item 5: characters added to Annex B at COBOL 2023 (absent before).",
        "E33Item6Added": "Annex E.3.3 item 6: case mappings added at COBOL 2023 (absent before), less <see cref=\"NotApplied\"/>.",
        "C2002Letters": "Annex C.2.1 (COBOL 2002): the letters and digits of TR 10176:2001 Annex A, every row but the special characters.",
        "C2002Special": "Annex C.2.1 (COBOL 2002): the special characters of TR 10176:2001 Annex A, barred from the first and last place (§2002:8.1.2.1 GR4b).",
        "C2002Additional": "Annex C.2.2 (COBOL 2002): the characters beyond the basic ones the standard adds (MIDDLE DOT), special characters.",
        "B2014Combining": "Annex B.3.1 (COBOL 2014): the rows labelled combining, which 2002 Annex C leaves unlabelled (§2002:8.1.2.1 GR4b bars them from the first and last place).",
        "NotApplied": "The pairs of Annex C.2, E.2 item 14 and E.3.3 item 6 that map an extended letter to a basic one, which the "
                      "fold of a COBOL word does not apply (§2023:8.1.3.2 GR4b: the corresponding lowercase EXTENDED letter; kb/Work PB4420).",
    }
    for name in names:
        out.append(f"    /// <summary>{docs[name]}</summary>")
        out.append(f"    internal static readonly (int First, int Second)[] {name} =")
        out.append("    [")
        out.append(fmt(data[name]))
        out.append("    ];")
        out.append("")
    out[-1] = "}"
    return "\n".join(out) + "\n"


# Annex B is a COMPILE-TIME fact (which characters a word may hold), so it lives with the edition catalogues; Annex
# C is also needed at RUN time (an externalized name is matched by the same fold, DOC-A.1-219), so it lives in the
# runtime, which both the compiler and every generated program reference.
OUTPUTS = [
    (ROOT / "src" / "WiseOwl.Cobol.Editions" / "CobolCharacterRepertoire.Table.cs", "WiseOwl.Cobol.Editions",
     "CobolCharacterRepertoire", "Annex B.3 (characters permitted in user-defined words)",
     ["B3Anywhere", "B3NotFirst", "B3Medial", "E2Item4Deleted", "E2Item4MadeMedial", "E33Item4MadeFirst",
      "E33Item5Added", "C2002Letters", "C2002Special", "C2002Additional", "B2014Combining"]),
    (ROOT / "src" / "WiseOwl.Cobol.Runtime" / "CobolNames.Table.cs", "WiseOwl.Cobol.Runtime", "CobolNames",
     "Annex C.2 (general case mappings, as (uppercase, lowercase) pairs)",
     ["C2Mappings", "E2Item14Deleted", "E33Item6Added", "NotApplied"]),
]


def main() -> int:
    transcribed = extract()
    for note in plausibility(transcribed):
        print("plausibility:", note, file=sys.stderr)
    transcribed.update(extract_tables())
    data = applied_lists(transcribed)
    stale = 0
    for path, namespace, cls, what, names in OUTPUTS:
        text = render(data, names, namespace, cls, what)
        if "--check" in sys.argv:
            current = path.read_text(encoding="utf-8") if path.exists() else ""
            if current != text:
                print(f"{path.relative_to(ROOT)} is stale; re-run scripts/spec/gen_annex_bc.py", file=sys.stderr)
                stale = 1
            continue
        path.write_bytes(text.encode("utf-8"))
    if "--check" not in sys.argv:
        for name, rs in data.items():
            print(f"{name}: {len(rs)} entries")
    return stale


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
