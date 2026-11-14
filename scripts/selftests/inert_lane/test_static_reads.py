#!/usr/bin/env python3
"""The static reads of compiler test sources (kb/Work PB5397; kb/Work PB4453): `markdown_readers`, which refuses a compiler test that opens a Markdown file, on planted test sources."""
import sys
import tempfile
from pathlib import Path

import source_reads
import selftest_cases
from standard_streams import ensure_utf8

markdown_readers = source_reads.markdown_readers


def case_00_markdown_readers_planted_tree_test_sources(expect):
    with tempfile.TemporaryDirectory() as td:
        planted = {
            "tests/T1.cs": 'class T1 { string a = File.ReadAllText(TestRepo.At("README.md")); }\n',
            "tests/T2.cs": 'class T2 { string a = TestRepo.Docs(); string b = TestRepo.Docs("rearchitecture", "a.json"); }\n',
            "tests/T3.cs": 'class T3 { static readonly TreeScope A = new(Under: ["src/"]);\n'
                           '  static readonly TreeScope B = new(Under: ["src/", "tests/"],\n    Kinds: [".cs"]); }\n',
            "tests/T4.cs": 'class T4 { string a = TestRepo.Specs("ISO_COBOL_2023.md"); /* TestRepo.At("c.md") */ // "d.md"\n'
                           '  string m = "see docs/CONFORMANCE.md rows"; }\n',
            "src/Program.cs": 'class P { string a = "README.md"; }\n',
        }
        for rel, body in planted.items():
            (Path(td) / rel).parent.mkdir(parents=True, exist_ok=True)
            (Path(td) / rel).write_text(body, encoding="utf-8", newline="\n")
        found = markdown_readers(Path(td), sorted(planted), {"ISO_COBOL_2023.md": "the oracle"})
        expect.equal("markdown_readers: a Markdown path a test spells, a docs walk and a TreeScope with no Kinds are each one "
               "finding; an allowed name, a comment, a message that merely mentions a Markdown file and a non-test file are none",
               sorted(f.split(" ", 1)[0] for f in found), ["tests/T1.cs", "tests/T2.cs", "tests/T3.cs"])
        expect.equal("markdown_readers: an allowed name no test spells any more is stale",
               [f for f in markdown_readers(Path(td), ["tests/T3.cs"], {"GONE.md": "why"})
                if "none spells it any more" in f], ["the Markdown name 'GONE.md' is allowed to a compiler test and none spells "
                                                     "it any more: remove it"])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
