#!/usr/bin/env python3
"""The static reads of compiler test sources (kb/Work PB5397; kb/Work PB4453): the C# lexer that finds the string literals a test spells, and `markdown_readers`, which refuses a compiler test that opens a Markdown file, on planted test sources."""
import sys
import tempfile
from pathlib import Path

import source_reads
import selftest_cases
from standard_streams import ensure_utf8

csharp_parts = source_reads.csharp_parts
markdown_readers = source_reads.markdown_readers


def case_00_lexer_comments_character_literals_hold_no_literal(expect):
    # THE PIN THAT NO COMPILER TEST OPENS A MARKDOWN FILE (kb/Work PB4453), on planted test sources.
    quote = chr(34)
    lexed_code, lexed = csharp_parts(
        'string a = "x.md"; // "y.md"\n/* "z.md" */ var c = \'' + quote + '\'; var v = @"a""b.md"; var r = """raw.md"""; var e = "";')
    expect.equal("the lexer: comments and character literals hold no literal, a regular, a verbatim and a raw string do, and no "
                 "literal's text stays in the code",
                 (lexed, "md" in lexed_code), (["x.md", 'a"b.md', "raw.md", ""], False))


def case_01_lexer_escaped_quote_backslash_unicode_character_literal(expect):
    # The two forms the first lexer misread, each hiding a Markdown path from the pin (review R1-1): an escaped quote or
    # backslash character literal (the closing quote was sought from the escaped character), and an interpolation hole that
    # holds a string literal (the hole ended at that literal's first quote); plus the raw interpolated string and a
    # preprocessor line holding an apostrophe.
    bs = chr(92)
    esc_code, esc = csharp_parts("char q = '" + bs + "'';  char b = '" + bs + bs + "'; char u = '" + bs + "u0041';\n"
                                 'string a = "README.md"; char r = \'x\'; string w = "after";')
    expect.equal("the lexer: an escaped quote, backslash and unicode character literal each end at their own closing quote, so "
                 "the string after them is still a literal",
                 (esc, "README" in esc_code), (["README.md", "after"], False))


def case_02_lexer_interpolation_hole_s_string_literal_value(expect):
    hole_code, hole = csharp_parts('var a = $"x{File.ReadAllText(TestRepo.At("README.md"))}y{b}" + "t"; var c = $"{{" + "e.md";')
    expect.equal("the lexer: an interpolation hole's string literal is a value and its code stays code; the text outside the holes "
                 "is the string's own value; a doubled brace is text",
                 (hole, "TestRepo.At" in hole_code, "README" in hole_code), (["README.md", "xy", "t", "{", "e.md"], True, False))


def case_03_lexer_raw_interpolated_string_s_holes_two(expect):
    raw_code, raw = csharp_parts('var a = $$"""\n  {{{TestRepo.At("a.md")}}} {{Docs()}} {x}\n  """; var b = "z.md";\n'
                                 "#region Don't\nvar c = 1;")
    expect.equal("the lexer: a raw interpolated string's holes (two dollars, two braces) are lexed and a lone brace is text; a "
                 "preprocessor line's apostrophe opens no character literal",
                 (raw, "Docs()" in raw_code, "TestRepo.At" in raw_code, "var c = 1;" in raw_code),
                 (["a.md", "\n  {}  {x}\n  ", "z.md"], True, True, True))


def case_04_markdown_readers_planted_tree_test_sources(expect):
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
