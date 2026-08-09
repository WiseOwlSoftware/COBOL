#!/usr/bin/env python3
"""Audit `docs/CONFORMANCE.md` §7 against Annex A.1 — the implementor-defined documentation register.

    python scripts/spec/audit_annex_a1.py            # report coverage + findings
    python scripts/spec/audit_annex_a1.py --check    # exit non-zero on any finding (gate form)

⛔ WHY THIS EXISTS. ISO §4.2.5 requires the implementor to SPECIFY every A.1 element identified as *required*
and to DOCUMENT every element identified as *requiring user documentation*; owner decision D13 makes those part
of the definition of v1.0. `docs/CONFORMANCE.md` §7 is that register. Its rows are keyed by A.1 ITEM NUMBER,
and nothing checked those numbers — so an entry could sit under the wrong obligation indefinitely, which
CONFORMANCE.md §7's own preamble calls the worse failure: "an undocumented determination and a wrongly-
documented one are both non-conformance, and the second is worse."

⭐ IT FOUND ONE ON ITS FIRST RUN. The determination for the maximum fractional-seconds precision (§15.3.3.2)
was filed as **item 87**; item 87 is *FORMATTED-CURRENT-DATE (accuracy of returned time)* — a different, still
undocumented obligation. The determination belongs to **item 202**, "Time formats and corresponding function
values (maximum precision not less than nine fractional digits)". That is the INHERITED-CITATION failure mode
CLAUDE.md rule 1 names: the quoted text was genuinely in the standard and the NUMBER was never re-derived.

The authority is `docs/rearchitecture/spec-rule-catalog.json`'s DOC rows, which `extract_rule_catalog.py`
parses straight out of Annex A.1 — so this script adds no second copy of the register (feedback_one_rule_one_place).
"""
from __future__ import annotations

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
CATALOG = ROOT / 'docs' / 'rearchitecture' / 'spec-rule-catalog.json'
CONFORMANCE = ROOT / 'docs' / 'CONFORMANCE.md'

#: Words that must appear in a §7 row for it to be judged as naming the same element as the catalog. Kept
#: deliberately weak — this checks that the NUMBER and the ELEMENT are not wildly divorced, which is the defect
#: class observed; it is not a prose-similarity metric, and pretending otherwise would produce noise nobody reads.
STOPWORDS = {'the', 'of', 'a', 'an', 'and', 'or', 'for', 'in', 'to', 'is', 'not', 'when', 'clause',
             'statement', 'item', 'data', 'this', 'that', 'with', 'by', 'be', 'may', 'shall', 'its'}


def words(text: str) -> set[str]:
    return {w for w in re.findall(r'[a-z0-9-]+', text.lower()) if w not in STOPWORDS and len(w) > 2}


def clauses(text: str) -> set[str]:
    """Every dotted clause number in a string — `§15.3.3.2`, `(15.4.1, Numeric …)`, `13.18.60.4 GR11`."""
    return set(re.findall(r'\b(\d+(?:\.\d+)+)\b', text))


def corresponds(element: str, entry: dict) -> bool:
    """Does a §7 row name the A.1 item it is filed under?

    ⚠ THE CLAUSE IS THE IDENTITY, not the prose. The first draft of this check compared WORDS only, and
    promptly rejected a correct row ("Evaluation of an exact-family intrinsic whose ARGUMENT is
    floating-point") against A.1-92's paraphrase ("Function returned values (characteristics,
    representation, …)") — the register's own preamble says its headers are a *paraphrase* of the normative
    detail, so demanding lexical overlap with a paraphrase is demanding the wrong thing. A row that cites a
    clause A.1 cross-references for that item is naming that item, whatever words it chose.
    """
    if clauses(element) & clauses(entry['text']):
        return True
    return len(words(element) & words(entry['subject'])) >= 2


def load_register() -> dict[int, dict]:
    catalog = json.loads(CATALOG.read_text(encoding='utf-8'))
    return {r['ordinal']: r for r in catalog['rules'] if r.get('kind') == 'DOC'}


def section7_rows(text: str) -> list[tuple[str, str]]:
    """(item-key, element-cell) for every data row of the §7 table."""
    start = text.index('## 7. Annex A.1')
    rows = []
    for line in text[start:].splitlines():
        if not line.startswith('| '):
            continue
        cells = [c.strip() for c in line.split('|')[1:-1]]
        if len(cells) < 2 or cells[0] in ('A.1 item',) or set(cells[0]) <= set('-: '):
            continue
        rows.append((cells[0], cells[1]))
    return rows


SELF_TEST_HEADER = '## 7. Annex A.1\n\n| A.1 item | Element | Our determination |\n|---|---|---|\n'


def self_test() -> int:
    """Prove every check can FAIL, and for its OWN reason (feedback_green_gates_arent_evidence).

    A gate that has never been shown to fail is not evidence — and one that fails for the wrong reason is
    worse, because it reads as working. Each case below breaks exactly one rule.
    """
    register = load_register()
    good = f'| 2 | ACCEPT statement device used when FROM is unspecified, §14.9.1 GR5 |'
    cases = [
        ('control: a correct row passes', good, None),
        ('a wrong item number is caught',
         '| 87 | **Maximum digit positions in the decimal fraction of the seconds subfield**, §15.3.3.2 |',
         'do not correspond'),
        ('an item number outside A.1 is caught',
         '| 999 | Something, §14.9.1 |', 'not an A.1 item number'),
        ('an unnumbered row is caught',
         '| — (§15.4.1) | Something voluntary |', 'carry no A.1 item number'),
    ]
    rc = 0
    print('=== audit_annex_a1 --self-test ===')
    for name, row, want in cases:
        text = SELF_TEST_HEADER + row + '\n'
        findings = evaluate(register, text)[0]
        if want is None:
            ok = not findings
        else:
            ok = any(want in f for f in findings)
        if ok:
            print(f'  ok: {name}')
        else:
            print(f'  SELF-TEST FAILED: {name} -> {findings}')
            rc = 1
    # kb/Work R23 — the source-citation sweep, proven able to fail for BOTH of its reasons.
    import tempfile
    with tempfile.TemporaryDirectory() as td:
        fake = pathlib.Path(td) / 'Cobol.Net.Fake' / 'X.cs'
        fake.parent.mkdir(parents=True)
        fake.write_text('// precision (implementor item 999)\n// and the real one (implementor item 2)\n',
                        encoding='utf-8')
        sweep_cases = [
            ('a citation of a nonexistent A.1 item is caught',
             sweep_source_citations(register, {2: 'x'}, pathlib.Path(td)), 'no item 999'),
            ('a citation §7 does not discharge is caught',
             sweep_source_citations(register, {}, pathlib.Path(td)), 'undischarged'),
            ('a discharged citation passes',
             [f for f in sweep_source_citations(register, {2: 'x', 999: 'y'}, pathlib.Path(td))
              if 'item 2' in f], None),
        ]
        for name, got, want in sweep_cases:
            ok = (not got) if want is None else any(want in f for f in got)
            if ok:
                print(f'  ok: {name}')
            else:
                print(f'  SELF-TEST FAILED: {name} -> {got}')
                rc = 1
    print('=== audit_annex_a1 --self-test: ' + ('ALL GREEN (every check proven able to fail)' if not rc else 'FAILED') + ' ===')
    return rc


def evaluate(register: dict[int, dict], text: str) -> tuple[list[str], dict[int, str], list[str]]:
    """The checks, over a §7 body — separated from reporting so --self-test can drive them."""
    findings: list[str] = []
    numbered: dict[int, str] = {}
    unnumbered: list[str] = []
    for key, element in section7_rows(text):
        if key.isdigit():
            n = int(key)
            if n not in register:
                findings.append(f'§7 row "{key}" is not an A.1 item number (A.1 has {len(register)} items)')
                continue
            numbered[n] = element
            if not corresponds(element, register[n]):
                findings.append(
                    f'§7 item {n} says "{element[:70]}" but A.1-{n} is "{register[n]["subject"][:70]}" — the '
                    f'NUMBER and the ELEMENT do not correspond, and neither do their clauses')
        else:
            unnumbered.append(element)
    if unnumbered:
        findings.append(f'{len(unnumbered)} §7 row(s) carry no A.1 item number, so no obligation is discharged '
                        f'by them and nothing can check them')
    return findings, numbered, unnumbered


# kb/Work R23 — the drift half: a SOURCE comment claiming "implementor item N" is a documentation claim, and
# a claim the register does not carry is the documentation equivalent of a dead lookup (CobolDate cited item
# 171 for months while §7 had no such row). Each citation must name a REAL A.1 item AND a row §7 actually
# carries. The pattern deliberately catches both spellings used in the tree ("implementor item 171",
# "A.1 item 180", "Annex A.1 item 29" is E.3's numbering and is NOT matched — only A.1-anchored forms).
CITATION_RX = re.compile(r'(?:implementor item|A\.1 item)\s+(\d+)')


def sweep_source_citations(register: dict[int, dict], numbered: dict[int, str], src_root: pathlib.Path) -> list[str]:
    """Every `implementor item N` / `A.1 item N` citation in the greenfield source must correspond to a real
    A.1 item that §7 documents. Returns findings; separated from evaluate() so --self-test drives it too."""
    findings: list[str] = []
    for path in sorted(src_root.rglob('*.cs')):
        parts = path.parts
        if 'obj' in parts or 'bin' in parts or 'Generated' in parts:
            continue
        if not any(p.startswith('Cobol.Net.') for p in parts):
            continue
        text = path.read_text(encoding='utf-8', errors='replace')
        for m in CITATION_RX.finditer(text):
            n = int(m.group(1))
            line = text.count('\n', 0, m.start()) + 1
            where = f'{path.name}:{line}'
            if n not in register:
                findings.append(f'{where} cites "{m.group(0)}" but A.1 has no item {n}')
            elif n not in numbered:
                findings.append(f'{where} cites "{m.group(0)}" as documented, but CONFORMANCE.md §7 carries '
                                f'no item-{n} row — the claim is undischarged (A.1-{n}: '
                                f'"{register[n]["subject"][:60]}")')
    return findings


def main(argv: list[str]) -> int:
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding='utf-8', errors='replace')
        except (AttributeError, ValueError):
            pass

    if '--self-test' in argv:
        return self_test()

    register = load_register()
    text = CONFORMANCE.read_text(encoding='utf-8')
    rows = section7_rows(text)
    findings: list[str] = []

    required = sum(1 for r in register.values() if r['requirement'] == 'required')
    optional = sum(1 for r in register.values() if r['requirement'] == 'optional')
    conditional = sum(1 for r in register.values() if r['requirement'] == 'conditionally required')
    unclassified = sum(1 for r in register.values() if r['requirement'] == 'unclassified')
    doc_required = sum(1 for r in register.values() if r['documented'])

    print('=== ANNEX A.1 REGISTER (from spec-rule-catalog.json, parsed out of the standard) ===')
    print(f'  items {len(register)}  =  required {required} · optional {optional} · '
          f'conditionally required {conditional} · unclassified {unclassified}')
    print(f'  identified as REQUIRING USER DOCUMENTATION: {doc_required}   (not required: {len(register) - doc_required})')
    if required + optional + conditional + unclassified != len(register):
        findings.append('the requirement classes do not sum to the item count')

    # ⛔ THE CHECKS LIVE IN evaluate() AND NOWHERE ELSE. The reporting loop below only RENDERS; it must not
    # re-decide anything, or --self-test would be proving a different implementation than the one that runs.
    # (The first draft did exactly that — two copies of the same rules, which is the defect this repo keeps
    # re-learning: feedback_one_rule_one_place.)
    findings_from_rows, numbered, unnumbered = evaluate(register, text)
    findings.extend(findings_from_rows)
    # kb/Work R23 — source comments claiming "implementor item N" must be discharged by a real §7 row.
    findings.extend(sweep_source_citations(register, numbered, ROOT / 'src'))

    print('\n=== docs/CONFORMANCE.md §7 ROWS ===')
    seen: set[int] = set()
    for key, element in rows:
        if not key.isdigit():
            print(f'  ⚠ (unnumbered) {element[:74]}')
            continue
        n = int(key)
        if n not in register:
            print(f'  ⛔ {key:<5} NOT AN A.1 ITEM')
            continue
        # ⚠ MORE THAN ONE ROW PER ITEM IS LEGITIMATE, not a finding. One A.1 element can need several
        # determinations (A.1-92 needs one for a function result's text image and one for evaluation under a
        # floating-point argument). The first draft failed on the duplicate and was simply wrong.
        mark = 'ok' if corresponds(element, register[n]) else '⛔'
        print(f'  {mark} {key:<5} {register[n]["subject"][:74]}'
              + ('   (2nd determination)' if n in seen else ''))
        seen.add(n)

    covered_doc = sorted(n for n in numbered if register[n]['documented'])
    covered_free = sorted(n for n in numbered if not register[n]['documented'])
    print('\n=== COVERAGE ===')
    print(f'  A.1 items with a §7 determination : {len(numbered)} of {len(register)}')
    print(f'    of which the standard REQUIRES documented : {len(covered_doc)} of {doc_required}'
          f'  ({100.0 * len(covered_doc) / doc_required:.1f}%)')
    if covered_free:
        print(f'    documented VOLUNTARILY (A.1 does not require it) : {covered_free}')
    print(f'  ⛔ REMAINING documentation obligations : {doc_required - len(covered_doc)}')

    print('\n=== FINDINGS ===')
    if not findings:
        print('  none — every §7 row names an A.1 item whose element it matches')
    for f in findings:
        print(f'  ⛔ {f}')
    if '--check' in argv:
        return 1 if findings else 0
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
