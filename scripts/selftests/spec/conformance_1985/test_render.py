#!/usr/bin/env python3
"""The COBOL-85 conformance statement's computed part (kb/Work PB4997): it renders from this repository's 1985 rows with no
finding, its module table covers every module the standard names and every rule once, and each way section 9.1 can part
from the rows (a rule the claim does not support with no row, a row of a supported rule, a row whose citation is not its
rule's) is a finding naming the rule; a rule is outside the claim by its own DOCUMENTED-NON-SUPPORT verdict or through a
KEPT chain to a 2023 rule with that verdict, and by nothing else."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

c = selftest_cases.subject(__file__)   # scripts/spec/conformance_1985.py


def _render_with(rows_91):
    """Section 9's findings when section 9.1 holds `rows_91` (rule id -> cells) instead of its files."""
    real = c._exception_rows
    c._exception_rows = lambda root, findings: rows_91
    c.render.cache_clear()
    try:
        return c.render(c.REPO)
    finally:
        c._exception_rows = real
        c.render.cache_clear()


def case_00_the_live_statement_renders_with_no_finding(expect):
    text, found = c.render(c.REPO)
    expect.equal("no finding", list(found), [])
    for m in c.REQUIRED + c.OPTIONAL:
        expect(f"the module table has a row for {m}", f"| {m} | " in text, m)
    expect("a totals row", "| **All** |" in text, text[-400:])
    expect("the obsolete list's 18 items", all(f"\n| {n} | " in text for n in range(1, 19)), "")
    # Appendix B's rows (kb/Work PB5452's 85w mint: §1985:B.2.1) have no module: a lettered section orders after the
    # Roman ones (inventory_view._section_key), where reading "B" as a Roman numeral raised KeyError.
    expect("the rows of no module name their sections in the standard's order, a lettered one last",
           "(no module: Sections II, III, IV, B)" in text, [ln for ln in text.splitlines() if "no module" in ln])


def case_01_section_91_and_the_rows_must_agree(expect):
    live = c._exception_rows(c.REPO, [])
    expect("section 9.1 has rows", len(live) > 0, str(len(live)))
    rid = sorted(live)[0]
    _, found = _render_with({k: v for k, v in live.items() if k != rid})
    expect("a rule the claim does not support, with no row, is named",
           any(f"no row for {rid}" in f for f in found) and len(found) == 1, str(found))
    _, found = _render_with({**live, "1985-SR-VI.6.18.3-1": ["`1985-SR-VI.6.18.3-1`", "x", "y", "z"]})
    expect("a row of a supported rule is named", any("1985-SR-VI.6.18.3-1" in f and "supported" in f for f in found)
           and len(found) == 1, str(found))
    _, found = _render_with({**live, "1985-SR-XX.9-1": ["`1985-SR-XX.9-1`", "x", "y", "z"]})
    expect("a row of no rule is named", any("no rule of the 1985 catalog" in f for f in found), str(found))
    wrong = dict(live)
    wrong[rid] = [live[rid][0], "§1985:VI.6.18.3 SR1", live[rid][2], live[rid][3]]
    _, found = _render_with(wrong)
    expect("a row whose citation is not its rule's is named", any(rid in f and "citation and subject" in f for f in found)
           and len(found) == 1, str(found))


def case_02_what_puts_a_rule_outside_the_claim(expect):
    schema = c.load_schema()
    kept = schema.next.chained.name
    targets = {"A": {"verdict": c.NOT_SUPPORTED}, "B": {"verdict": "CONFORMS"}}
    expect.equal("its own DOCUMENTED-NON-SUPPORT verdict", c.not_supported_through({"verdict": c.NOT_SUPPORTED}, schema, targets), [])
    expect.equal("its own CONFORMS verdict", c.not_supported_through({"verdict": "CONFORMS"}, schema, targets), None)
    expect.equal("KEPT into a rule that is not supported",
                 c.not_supported_through({"verdict": "", "next": {"kind": kept, "to": ["A", "B"]}}, schema, targets), ["A"])
    expect.equal("KEPT into supported rules only",
                 c.not_supported_through({"verdict": "", "next": {"kind": kept, "to": ["B"]}}, schema, targets), None)
    expect.equal("a CHANGED record is judged on its own verdict, never its target's",
                 c.not_supported_through({"verdict": "CONFORMS", "next": {"kind": "CHANGED", "to": ["A"]}}, schema, targets), None)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
