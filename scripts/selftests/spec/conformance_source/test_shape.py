#!/usr/bin/env python3
"""The conformance document's regular shape on planted documents (DESIGN-orchestrator-loop.md section 16.8 step B1, kb/Work
PB5429): a regular document is GREEN and renders to itself, and each way out of shape — an unkeyed entry, a key used twice,
entries out of natural key order, text after a section's entries outside a row body and the epilogue, a heading the reader
does not list, a section with entries that lost them — is a finding of its own, naming the line."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

cs = selftest_cases.subject(__file__)   # scripts/spec/conformance_source.py

REGULAR = (
    "# Conformance\n\n> the status\n\n"
    "## 1. Summary\n\nprose\n\n"
    "## 2. Annex A.3\n\n| A.3 # | Element |\n|---|---|\n| 2 | e2 |\n| 10 | e10 |\n\n"
    "## 3. Determinations\n\n"
    "- **D-B2 — the second.** The two.\n  continued\n\n  a second paragraph\n\n"
    "- **D-B10 the tenth.** The ten.\n\n"
    "### 3.1 Undefined cases\n\nThe preamble.\n\n- **D-UND — a case.** text\n\n"
    "### 3.2 Extensions\n\n| Id | Extension |\n|---|---|\n| `ext-a` | x |\n| `ext-b` | y |\n\n"
    "#### ext-b\n\nThe rest of ext-b.\n\n" + cs.EPILOGUE_MARKER + "\n\nWhat is not a row.\n\n"
    "### 3.3 A prose section\n\n| a table | of prose |\n|---|---|\n| z | a |\n\n"
    "### 3.7.1 A third-level number\n\nprose\n\n"
    "## 4. Documented non-support\n\nThe preamble.\n\n#### MCS\n\n1. **Message control system** — declined\n\n"
    "#### SCREEN\n\n2. **Screens** — declined\n\n" + cs.EPILOGUE_MARKER + "\n\n⚠ for the whole section\n\n"
    "## 5. Annex A.4\n\n| A.4 # | Module |\n|---|---|\n| A.4.2 | m |\n| A.4.10 | n |\n\n"
    "## 7. Annex A.1\n\n| A.1 item | E |\n|---|---|\n| DOC-A.1-9 | e |\n| DOC-A.1-56 | e |\n\n#### DOC-A.1-56\n\nthe survey\n\n"
    "```text\n#### not a heading inside a fence\n```\n\n"
    "## 8. Derivations\n\n| Rule | Arm |\n|---|---|\n| DRV-1985-GR-1-1 | a |\n| DRV-GR-1-1 | a |\n")


def findings(text):
    return cs.check(cs.parse(text))


def case_00_a_regular_document_is_green_and_renders_to_itself(expect):
    expect.equal("the planted regular document", findings(REGULAR), [])
    expect.equal("it renders byte for byte to itself", cs.render(cs.parse(REGULAR)), REGULAR)
    conf = cs.parse(REGULAR)
    expect.equal("sections 3 and 3.1's determinations by key", sorted(conf.determinations()), ["D-B10", "D-B2", "D-UND"])
    expect.equal("a determination keeps its continuation and inner paragraphs", conf.determinations()["D-B2"].text,
                 "- **D-B2 — the second.** The two.\n  continued\n\n  a second paragraph")
    s32 = conf.shape("3.2")
    expect.equal("section 3.2: its rows keyed without backticks, the body, the epilogue",
                 ([e.key for e in s32.entries], [b.key for b in s32.bodies], s32.epilogue), (["ext-a", "ext-b"], ["ext-b"], "What is not a row."))
    s4 = conf.shape("4")
    expect.equal("§4: its blocks in the order written, the epilogue",
                 ([e.key for e in s4.entries], s4.epilogue), (["MCS", "SCREEN"], "⚠ for the whole section"))
    expect.equal("a prose section's table is its preamble, not entries", conf.shape("3.3").entries, [])
    expect.equal("natural key order: numbers as numbers, a number before text",
                 sorted(["DOC-A.1-10", "DOC-A.1-92b", "DOC-A.1-9", "DOC-A.1-92", "DRV-GR-1", "DRV-1985-GR-1"], key=cs.natural_key),
                 ["DOC-A.1-9", "DOC-A.1-10", "DOC-A.1-92", "DOC-A.1-92b", "DRV-1985-GR-1", "DRV-GR-1"])


def _one(expect, label, text, needle):
    got = findings(text)
    expect(label, len(got) == 1 and needle in got[0], str(got))


def case_01_each_way_out_of_shape_is_one_finding(expect):
    _one(expect, "an entry with no key", REGULAR.replace("- **D-B10 the tenth.**", "- **The tenth.**"), "an entry with no key")
    _one(expect, "a key used twice", REGULAR.replace("- **D-UND — a case.**", "- **D-B2 — a case.**"), "the key `D-B2` is already")
    _one(expect, "determinations out of natural key order",
         REGULAR.replace("- **D-B2 — the second.**", "- **D-B20 — the second.**"), "out of natural key order from `D-B20`")
    _one(expect, "table rows out of natural key order",
         REGULAR.replace("| DOC-A.1-9 | e |\n| DOC-A.1-56 | e |", "| DOC-A.1-56 | e |\n| DOC-A.1-9 | e |"), "out of natural key order")
    _one(expect, "text after a table's rows outside a body and the epilogue",
         REGULAR.replace("| DRV-GR-1-1 | a |\n", "| DRV-GR-1-1 | a |\n\nA stray paragraph.\n"), "text after the section's entries")
    _one(expect, "text after the determinations, outside an entry",
         REGULAR.replace("- **D-B10 the tenth.** The ten.\n", "- **D-B10 the tenth.** The ten.\n\nA stray paragraph.\n"),
         "text after the section's entries")
    _one(expect, "a row body naming no row", REGULAR.replace("#### DOC-A.1-56\n", "#### DOC-A.1-57\n"), "the body of no row")
    _one(expect, "an unlisted heading", REGULAR.replace("### 3.7.1 A third-level number", "### A heading with no number"),
         "a heading the reader does not list")
    _one(expect, "a heading out of number order", REGULAR.replace("### 3.7.1 A third-level", "### 3.0 A third-level"),
         "3.0 follows §3")
    _one(expect, "a `####` heading in a determination section",
         REGULAR.replace("- **D-UND — a case.** text\n", "- **D-UND — a case.** text\n\n#### D-X\n"), "holds no `#### <key>` heading")
    _one(expect, "an epilogue marker in a prose section",
         REGULAR.replace("### 3.3 A prose section\n\n", "### 3.3 A prose section\n\n" + cs.EPILOGUE_MARKER + "\n\n"),
         "a prose section has no entries")
    _one(expect, "a blocks section that lost its entries",
         REGULAR.replace("#### MCS\n\n", "").replace("#### SCREEN\n\n", ""), "a blocks section with no entry")
    gone = findings(REGULAR.replace("## 5. Annex A.4", "## 5x Annex A.4"))
    expect("a section with entries whose heading is gone: the heading unlisted, and the section lost",
           len(gone) == 2 and "does not list" in gone[0] and "lost §5" in gone[1], str(gone))
    _one(expect, "a layout that is not the renderer's (two blank lines)", REGULAR.replace("prose\n\n## 2.", "prose\n\n\n## 2."),
         "the layout is not the renderer's")


def case_02_the_live_document_is_regular(expect):
    conf = cs.load()
    expect.equal("docs/CONFORMANCE.md passes --check", cs.check(conf), [])
    expect("every section 3 and 3.1 bullet is a keyed determination (more than 140)", len(conf.determinations()) > 140,
           str(len(conf.determinations())))
    expect("section 7's item-56 survey is that row's body", [b.key for b in conf.shape("7").bodies] == ["DOC-A.1-56"],
           str([b.key for b in conf.shape("7").bodies]))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
