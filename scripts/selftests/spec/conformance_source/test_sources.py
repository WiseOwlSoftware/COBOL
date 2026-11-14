#!/usr/bin/env python3
"""The conformance document's SOURCES on planted trees (DESIGN-orchestrator-loop.md section 16.8 step B2, kb/Work PB5430):
a directory per section and a file per entry render the one document `render_section` lays out, and each source out of
shape — a key that is not its file's name, a row whose cells are not its columns, a malformed frontmatter, a stray file,
an entry file in a prose section, two blocks of one order — is a finding naming the file."""
import pathlib
import sys
import tempfile

import selftest_cases
from standard_streams import ensure_utf8

cs = selftest_cases.subject(__file__)   # scripts/spec/conformance_source.py

EXPECTED = (
    "# Conformance\n\n> the status\n\n"
    "## 1. Summary\n\nprose\n\n"
    "## 2. Annex A.3\n\n| A.3 # | Element |\n|---|---|\n| 2 | e2 |\n| 10 | a \\| b |\n| 28–30 | a range |\n\n#### 10\n\nthe rest of 10\n\n"
    + cs.EPILOGUE_MARKER + "\n\nfor the whole section\n\n"
    "## 3. Determinations\n\n- **D-B2 — the second.** The two.\n  continued\n\n- **D-B10 the tenth.** The ten.\n\n"
    "### 3.1 Undefined\n\nThe preamble.\n\n- **D-UND — a case.** text\n\n"
    "### 3.2 Extensions\n\n| Id | Extension |\n|---|---|\n| `ext-a` | x |\n\n"
    "## 4. Facilities\n\nThe preamble.\n\n#### MCS\n\n1. **Message control** — provided\n\n#### SCREEN\n\n2. **Screens**\n\n"
    "## 5. Annex A.4\n\n| A.4 # | Module |\n|---|---|\n| A.4.2 | m |\n\n"
    "## 7. Annex A.1\n\n| A.1 item | E |\n|---|---|\n| DOC-A.1-9 | e |\n\n"
    "## 8. Derivations\n\n| Rule | Arm |\n|---|---|\n| DRV-GR-1-1 | a |\n")


def _plant(root: pathlib.Path) -> pathlib.Path:
    base = root / cs.SOURCE_REL

    def put(rel, text):
        (base / rel).parent.mkdir(parents=True, exist_ok=True)
        (base / rel).write_text(text, encoding="utf-8", newline="\n")

    put(cs.HEAD_FILE, "# Conformance\n\n> the status\n")
    put(cs.README_FILE, "# the directory's page\n")
    put("1/index.md", cs.with_frontmatter({"heading": "## 1. Summary"}, "prose"))
    put("2/index.md", cs.with_frontmatter({"heading": "## 2. Annex A.3", "columns": ["A.3 #", "Element"]}, ""))
    put("2/10.md", cs.with_frontmatter({"cells": ["10", "a | b"]}, "the rest of 10"))
    put("2/2.md", cs.with_frontmatter({"cells": ["2", "e2"]}, ""))
    put("2/28-30.md", cs.with_frontmatter({"cells": ["28–30", "a range"]}, ""))   # an en dash key, an ASCII file name
    put("2/epilogue.md", "for the whole section\n")
    put("3/index.md", cs.with_frontmatter({"heading": "## 3. Determinations"}, ""))
    put("3/D-B10.md", "- **D-B10 the tenth.** The ten.\n")
    put("3/D-B2.md", "- **D-B2 — the second.** The two.\n  continued\n")
    put("3.1/index.md", cs.with_frontmatter({"heading": "### 3.1 Undefined"}, "The preamble."))
    put("3.1/D-UND.md", "- **D-UND — a case.** text\n")
    put("3.2/index.md", cs.with_frontmatter({"heading": "### 3.2 Extensions", "columns": ["Id", "Extension"]}, ""))
    put("3.2/ext-a.md", cs.with_frontmatter({"cells": ["`ext-a`", "x"]}, ""))
    put("4/index.md", cs.with_frontmatter({"heading": "## 4. Facilities"}, "The preamble."))
    put("4/SCREEN.md", cs.with_frontmatter({"order": 2}, "2. **Screens**"))
    put("4/MCS.md", cs.with_frontmatter({"order": 1}, "1. **Message control** — provided"))
    put("5/index.md", cs.with_frontmatter({"heading": "## 5. Annex A.4", "columns": ["A.4 #", "Module"]}, ""))
    put("5/A.4.2.md", cs.with_frontmatter({"cells": ["A.4.2", "m"]}, ""))
    put("7/index.md", cs.with_frontmatter({"heading": "## 7. Annex A.1", "columns": ["A.1 item", "E"]}, ""))
    put("7/DOC-A.1-9.md", cs.with_frontmatter({"cells": ["DOC-A.1-9", "e"]}, ""))
    put("8/index.md", cs.with_frontmatter({"heading": "## 8. Derivations", "columns": ["Rule", "Arm"]}, ""))
    put("8/DRV-GR-1-1.md", cs.with_frontmatter({"cells": ["DRV-GR-1-1", "a"]}, ""))
    return base


def _findings_after(change):
    with tempfile.TemporaryDirectory() as td:
        root = pathlib.Path(td)
        change(_plant(root))
        return cs.problems(root)


def _one(label, change, needle):
    got = _findings_after(change)
    return label, len(got) >= 1 and needle in got[0], str(got)   # the source's own finding first; the render may echo it


def case_00_a_planted_tree_renders_the_regular_document(expect):
    with tempfile.TemporaryDirectory() as td:
        root = pathlib.Path(td)
        _plant(root)
        text, findings = cs.read_sources(root)
        expect.equal("no source out of shape", findings, [])
        expect.equal("the render: sections in number order, entries in key order, blocks by `order:`", text, EXPECTED)
        expect.equal("and the render is regular", cs.check(cs.parse(text)), [])
        expect.equal("load reads the same document", cs.load(root).text, EXPECTED)
    expect.equal("frontmatter round-trips through JSON values",
                 cs.frontmatter(cs.with_frontmatter({"cells": ["a | b", "“q”"], "order": 3}, "body"), "x"),
                 ({"cells": ["a | b", "“q”"], "order": 3}, "body"))


def case_01_each_source_out_of_shape_is_a_finding_naming_its_file(expect):
    def rename(base, a, b):
        (base / a).rename(base / b)

    def write(base, rel, text):
        (base / rel).write_text(text, encoding="utf-8", newline="\n")

    expect(*_one("a determination whose key is not its file's name",
                 lambda b: rename(b, "3/D-B2.md", "3/D-B3.md"), "3/D-B3.md: the determination does not open `- **D-B3`"))
    expect(*_one("a row whose first cell is not its file's name",
                 lambda b: write(b, "5/A.4.2.md", cs.with_frontmatter({"cells": ["A.4.3", "m"]}, "")), "is not its file's name"))
    expect(*_one("a row whose cells are not its columns",
                 lambda b: write(b, "5/A.4.2.md", cs.with_frontmatter({"cells": ["A.4.2", "m", "x"]}, "")),
                 "3 cells, and the table has 2 columns"))
    expect(*_one("a malformed frontmatter line",
                 lambda b: write(b, "7/DOC-A.1-9.md", "---\ncells: [DOC-A.1-9, e]\n---\n"), "holds no JSON value"))
    expect(*_one("a stray file beside the sections", lambda b: write(b, "notes.md", "x\n"), "a file outside every section"))
    expect(*_one("an entry file in a prose section", lambda b: write(b, "1/D-X.md", "- **D-X** x\n"), "is a prose section"))
    expect(*_one("two blocks of one order",
                 lambda b: write(b, "4/SCREEN.md", cs.with_frontmatter({"order": 1}, "2. **Screens**")), "share an `order:`"))
    expect(*_one("an index whose heading names another section",
                 lambda b: write(b, "5/index.md", cs.with_frontmatter({"heading": "## 6. Annex A.4", "columns": ["A.4 #", "Module"]}, "")),
                 "does not name §5"))
    expect(*_one("a directory that is no section number", lambda b: (b / "old").mkdir(), "no section number"))
    dup = _findings_after(lambda b: write(b, "3/DOC-A.1-9.md", "- **DOC-A.1-9** a determination keyed like a row\n"))
    expect("a key used twice across sections is the rendered whole's finding", any("already the entry" in f for f in dup)
           or any("does not open" in f for f in dup), str(dup))
    with tempfile.TemporaryDirectory() as td:
        try:
            cs.load(pathlib.Path(td))
            refused = ""
        except SystemExit as e:
            refused = str(e)
    expect("load refuses a tree with no sources, naming the remedy", "conformance_doc.py --check" in refused, refused)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
