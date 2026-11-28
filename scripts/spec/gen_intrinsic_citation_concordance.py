#!/usr/bin/env python3
"""The 2002 and 2014 counterparts of every §2023 intrinsic-function citation the binder writes (kb/Work PB6118).

    python scripts/spec/gen_intrinsic_citation_concordance.py           # write the table
    python scripts/spec/gen_intrinsic_citation_concordance.py --check   # exit 1 when the committed table is not what this prints

WHY. A diagnostic cites the rule of the edition the program was compiled under (kb/Work PB4950). The intrinsic-function
rules were written once, as §2023:15.n.3 clauses, in `IntrinsicBinder.cs` and the schema rows of `IntrinsicArgumentRules.cs`;
every other edition numbers its functions differently (2002 and 2014 have no CONVERT; TRIM is COBOL-2014's; each edition's
alphabetical function order shifts every later paragraph). `IntrinsicCitations` asks the table this script writes: the 2023
CLAUSE the source spells -> the same clause of 2002 and of 2014.

HOW THE COUNTERPART IS FOUND, never from memory:
  * a function by its heading ("15.n NAME function") in each edition's transcription;
  * its subclauses by the one fixed shape of each edition (2023 .2 General format, .3 Argument rules, .4 Returned value rules;
    2002 and 2014 .1 General format, .2 Arguments, .3 Returned values).
A RULE number is not found here: the argument rules an edition numbers differently are the verified ordinals of
`IntrinsicCitations.Edition2002` and `Edition2014`, held to each edition's text by `IntrinsicEditionClausesDriftTests`, and
`IntrinsicCitations` reads them for every `15.n.3 ARk` citation. Every string written below is checked again by
`audit_code_citations.py`, which resolves each edition-form citation against its own edition's transcription.
"""
from __future__ import annotations

import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "scripts" / "spec"))
sys.path.append(str(REPO / "scripts"))  # for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

OUT = REPO / "src" / "WiseOwl.Cobol.Compiler" / "Binding" / "IntrinsicCitationConcordance.cs"
SOURCES = [
    REPO / "src" / "WiseOwl.Cobol.Compiler" / "Binding" / "Procedure" / "Verbs" / "IntrinsicBinder.cs",
    REPO / "src" / "WiseOwl.Cobol.Compiler" / "Binding" / "IntrinsicArgumentRules.cs",
    REPO / "src" / "WiseOwl.Cobol.Compiler" / "Binding" / "SpecialNamesBinder.cs",
]

# The one reading of a §2023:15 citation: the clause, its subclauses, and an optional rule written as AR3, RV2, r1, rule 1 or
# argument rule 1 (a trailing letter or digit is a sub-item, read at its rule). IntrinsicCitations spells the same pattern.
CITE = re.compile(r"§2023:15\.(\d+)((?:\.\d+)*)(?: (AR|RV|r|rule|argument rule) ?(\d+)(?:[a-z]\d?)?((?:/(?:AR|RV)\d+)*))?(?!\w)(?!\.\d)")

# Clauses of §2023:15 that are not one function's own: 15.3 Arguments, 15.6 Summary of functions, and the date and time format
# clauses 15.3.1.1, 15.3.2, 15.3.3.4, 15.3.3.6, 15.3.3.7 (2014's 15.3.1.1 ... 15.3.1.3; COBOL-2002 has no format arguments).
FIXED = {
    "15.3": ("§1989:A.2.2", "§2002:15.2", "§2014:15.3"),
    "15.6": ("§1989:A.2.4", "§2002:15.5", "§2014:15.6"),
    "15.3.1.1": (None, None, "§2014:15.3.1"),
    "15.3.2": (None, None, "§2014:15.3.1.2"),
    "15.3.3.4": (None, None, "§2014:15.3.1.2.2"),
    "15.3.3.6": (None, None, "§2014:15.3.1.2.4"),
    "15.3.3.7": (None, None, "§2014:15.3.1.3"),
}
SUB = {"": "", ".1": "", ".2": ".1", ".3": ".2", ".4": ".3"}


def headings(edition: int) -> dict[str, int]:
    out: dict[str, int] = {}
    for line in (REPO / "specs" / f"ISO_COBOL_{edition}.md").read_text(encoding="utf-8").splitlines():
        m = re.match(r"#{3,5} 15\.(\d+) (.+) function\s*$", line)
        if m:
            out[m[2].strip()] = int(m[1])
    return out


def canonical(m: re.Match[str]) -> str:
    """The key of a citation: its clause, `15.n[.m]`; a rule's number is `IntrinsicCitations`' (see the module docstring)."""
    return f"15.{m[1]}{m[2] or ''}"


# In the schema rows of IntrinsicArgumentRules a citation is a string literal of its own (the rule IntrinsicCitations.Clause
# translates, possibly with a "(via ...)" tail); its prose descriptions of argument types are another site's, not read here.
SCHEMA_LITERAL = re.compile(r'"(§2023:15\.[\d.]+(?: (?:AR|RV|r|rule|argument rule) ?\d+[a-z]?\d?(?:/(?:AR|RV)\d+)*)?(?: \(via [^"]*\))?)"')


def used_keys() -> set[str]:
    keys: set[str] = set()
    for src in SOURCES:
        schema = src.name.startswith("IntrinsicArgumentRules")
        for line in src.read_text(encoding="utf-8").splitlines():
            if line.lstrip().startswith("//"):
                continue
            for text in ([m[1] for m in SCHEMA_LITERAL.finditer(line)] if schema else [line]):
                for m in CITE.finditer(text):
                    keys.add(canonical(m))
    return keys


def main() -> int:
    check = "--check" in sys.argv[1:]
    heads = {e: headings(e) for e in (2002, 2014, 2023)}
    names = {n: name for name, n in heads[2023].items()}
    rows: list[str] = []
    for key in sorted(used_keys(), key=lambda k: ([int(x) for x in re.findall(r"\d+", k)], k)):
        m = re.fullmatch(r"15\.(\d+)((?:\.\d+)*)", key)
        n, sub = int(m[1]), m[2] or ""
        if key in FIXED:
            y1985, y2002, y2014 = FIXED[key]
            rows.append(entry(key, None, y1985, y2002, y2014))
            continue
        name = names.get(n)
        if name is None:
            sys.exit(f"{key}: §2023:15.{n} is no function clause of the 2023 transcription")
        cells: list[str | None] = []
        for e in (2002, 2014):
            p = heads[e].get(name)
            if p is None:
                cells.append(None)
                continue
            if sub not in SUB:
                sys.exit(f"{key}: subclause {sub} has no counterpart shape")
            loc = f"15.{p}{SUB[sub]}"
            cells.append(f"§{e}:{loc}")
        rows.append(entry(key, name, None, cells[0], cells[1]))
    text = HEADER + "".join(rows) + FOOTER
    if check:
        if OUT.read_text(encoding="utf-8") != text:
            print(f"{OUT.relative_to(REPO)} is not what {pathlib.Path(__file__).name} prints: run it without --check", file=sys.stderr)
            return 1
        print("intrinsic citation concordance: current")
        return 0
    OUT.write_text(text, encoding="utf-8", newline="\n")
    print(f"wrote {OUT.relative_to(REPO)}: {len(rows)} citations")
    return 0


def entry(key: str, function: str | None, y1985: str | None, y2002: str | None, y2014: str | None) -> str:
    def lit(v: str | None) -> str:
        return "null" if v is None else f'"{v}"'
    fn = "null" if function is None else f'"{function}"'
    return f'        new("{key}", {fn}, {lit(y1985)}, {lit(y2002)}, {lit(y2014)}),\n'


HEADER = '''// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
// <auto-generated>scripts/spec/gen_intrinsic_citation_concordance.py writes this file; never edit it by hand (kb/Work PB6118).</auto-generated>
#nullable enable
namespace WiseOwl.Cobol.Binding;

/// <summary>⛔ THE 2002 AND 2014 COUNTERPART OF EVERY §2023 INTRINSIC-FUNCTION CLAUSE THE BINDER CITES (kb/Work PB6118). Keyed by the
/// clause (<c>15.19.3</c>), each row names the function it is of and the same clause as COBOL-2002 and COBOL-2014 number it, or
/// <see langword="null"/> where that edition does not have the function. A rule's number within the clause is
/// <see cref="IntrinsicCitations.Edition2002"/>'s and <see cref="IntrinsicCitations.Edition2014"/>'s. Read by
/// <see cref="IntrinsicCitations"/>, the one place a message takes its citation. Generated from the three transcriptions by
/// <c>scripts/spec/gen_intrinsic_citation_concordance.py</c> and held current by <c>IntrinsicCitationConcordanceDriftTests</c>;
/// every edition-form string below is resolved again by <c>scripts/spec/audit_code_citations.py</c>.</summary>
internal static class IntrinsicCitationConcordance
{
    /// <summary>One §2023 clause's counterparts: its number, the function (null for a clause of §2023:15 that is no
    /// function's own), the citation in the 1989 module (only for those clauses; a function's own is
    /// <c>IntrinsicCatalog.Paragraph1989</c>'s) and the citation in COBOL-2002 and COBOL-2014 (null where the edition lacks the clause).</summary>
    internal readonly record struct Row(string Key, string? Function, string? In1985, string? In2002, string? In2014);

    /// <summary>The position in <see cref="Rows"/> of a canonical 2023 citation, or -1: a message is a cold path, and the rows are a
    /// list the generator wrote in order.</summary>
    internal static int IndexOf(string key)
    {
        for (int i = 0; i < Rows.Count; i++)
            if (string.Equals(Rows[i].Key, key, StringComparison.Ordinal)) return i;
        return -1;
    }

    /// <summary>The counterparts, in canonical-citation order.</summary>
    internal static readonly IReadOnlyList<Row> Rows =
    [
'''
FOOTER = '''    ];
}
'''

if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
