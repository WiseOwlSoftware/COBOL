#!/usr/bin/env python3
"""The one C# lexer of the build and gate tooling (kb/Work PB5230 review R2-1): every literal form it follows, as `parts`
(the code and the literals' values, the INERT LANE's Markdown audit) and `kind_at` (what an offset is, the BUILD TARGETS audit)
report it, and the forms the two audits used to read differently."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

lexer = selftest_cases.subject(__file__)
Q = chr(34)
BS = chr(92)


def case_00_parts_comments_and_char_literals_hold_no_literal(expect):
    code, literals = lexer.parts(
        'string a = "x.md"; // "y.md"\n/* "z.md" */ var c = \'' + Q + '\'; var v = @"a""b.md"; var r = """raw.md"""; var e = "";')
    expect.equal("comments and character literals hold no literal, a regular, a verbatim and a raw string do, and no literal's "
           "text stays in the code", (literals, "md" in code), (["x.md", 'a"b.md', "raw.md", ""], False))


def case_01_parts_an_escaped_character_literal_ends_at_its_own_quote(expect):
    code, literals = lexer.parts("char q = '" + BS + "'';  char b = '" + BS + BS + "'; char u = '" + BS + "u0041';\n"
                                 'string a = "README.md"; char r = \'x\'; string w = "after";')
    expect.equal("an escaped quote, backslash and unicode character literal each end at their own closing quote, so the string "
           "after them is still a literal", (literals, "README" in code), (["README.md", "after"], False))


def case_02_parts_an_interpolation_hole_is_code_and_its_literals_are_values(expect):
    code, literals = lexer.parts('var a = $"x{File.ReadAllText(TestRepo.At("README.md"))}y{b}" + "t"; var c = $"{{" + "e.md";')
    expect.equal("an interpolation hole's string literal is a value and its code stays code; the text outside the holes is the "
           "string's own value; a doubled brace is text",
           (literals, "TestRepo.At" in code, "README" in code), (["README.md", "xy", "t", "{", "e.md"], True, False))


def case_03_parts_a_raw_interpolated_string_and_a_preprocessor_line(expect):
    code, literals = lexer.parts('var a = $$"""\n  {{{TestRepo.At("a.md")}}} {{Docs()}} {x}\n  """; var b = "z.md";\n'
                                 "#region Don't\nvar c = 1;")
    expect.equal("a raw interpolated string's holes (two dollars, two braces) are lexed and a lone brace is text; a preprocessor "
           "line's apostrophe opens no character literal",
           (literals, "Docs()" in code, "TestRepo.At" in code, "var c = 1;" in code),
           (["a.md", "\n  {}  {x}\n  ", "z.md"], True, True, True))


def case_04_kind_at_every_literal_form(expect):
    forms = {  # source -> the kind of each `X.Y` in it, in order
        f"var m = {Q * 3}\nX.Y\n{Q * 3};\nX.Y;\n": ["string", "code"],
        f"var m = $${Q * 3}\n{{x}} {{{{X.Y}}}} X.Y\n{Q * 3};\nX.Y;\n": ["hole", "string", "code"],
        f"var m = @{Q}a\nX.Y{Q};\nX.Y;\n": ["string", "code"],
        f"var s = $@{Q}{Q}{Q} {{X.Y}}\nX.Y{Q}; X.Y;\n": ["hole", "string", "code"],
        f"var c = x == '{Q}' ? 1 : 2; X.Y;\nvar e = '\\'' + X.Y;\n": ["code", "code"],
        "/* X.Y\n X.Y */ X.Y; // X.Y\n": ["comment", "comment", "code", "comment"],
        f"var s = ${Q}a {{F({Q}{{{{X.Y}}}}{Q}, X.Y)}} b{Q}; X.Y;\n": ["string", "hole", "code"],
        f"var s = {Q}{Q}; var t = {Q}a\\{Q}X.Y{Q}; X.Y;\n": ["string", "code"],
    }
    got = {src: [lexer.kind_at(src, at) for at in _offsets(src)] for src in forms}
    expect("every literal form is followed: a raw literal and its `{{...}}` holes, a verbatim literal over lines and its "
           "`\"\"`, an interpolated hole holding a string, a character literal's quote, a block comment, a regular "
           "literal's escaped quote", got == forms, "\n".join(f"{s!r}: {g}" for s, g in got.items() if g != forms[s]))


def case_05_a_verbatim_literal_ending_in_an_escaped_quote_is_not_raw(expect):
    # Review round 3 of PB5230: `@"x"""` holds three quotes; a three-quote pattern took two such literals for one raw
    # literal and hid every line of code between them.
    src = f"new(@{Q}x{Q * 3});\nvar a = Y.Z;\nnew(@{Q}y{Q * 3});\n"
    code, literals = lexer.parts(src)
    expect.equal("code between two verbatim literals ending in an escaped quote is code, and their values are `x\"` and `y\"`",
           (lexer.kind_at(src, src.index("Y.Z")), "Y.Z" in code, literals), ("code", True, ['x"', 'y"']))


def case_06_the_two_audits_read_one_partition(expect):
    # The forms the BUILD TARGETS and the INERT LANE audits used to lex differently (review R2-1): a preprocessor line is no
    # code to either; an interpolation's format is text to both; an unterminated regular literal ends at its line's end.
    pre = "#if IMPACT\nvar x = @\"a\";\n#endif\n#region Don't\nvar y = X.Y;\n"
    fmt = f"var s = ${Q}{{DateTime.Now:yyyy-MM-dd HH}}{Q}; X.Y;\n"
    open_ = f"var s = {Q}abc\nX.Y;\n"
    expect.equal("a preprocessor line is a comment, and the code after it is lexed from the line after", (
        lexer.kind_at(pre, pre.index("#if")), lexer.kind_at(pre, pre.index("IMPACT")), lexer.kind_at(pre, pre.index("X.Y")),
        lexer.parts(pre)[1]), ("comment", "comment", "code", ["a"]))
    expect.equal("an interpolation's `:format` is text and its expression is hole code",
           (lexer.kind_at(fmt, fmt.index("DateTime")), lexer.kind_at(fmt, fmt.index("yyyy")), lexer.kind_at(fmt, fmt.index("X.Y"))),
           ("hole", "string", "code"))
    expect.equal("an unterminated regular literal ends at its line's end, so the next line is code",
           (lexer.kind_at(open_, open_.index("abc")), lexer.kind_at(open_, open_.index("X.Y"))), ("string", "code"))


def _offsets(src: str) -> list[int]:
    out, at = [], src.find("X.Y")
    while at >= 0:
        out.append(at)
        at = src.find("X.Y", at + 1)
    return out


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
