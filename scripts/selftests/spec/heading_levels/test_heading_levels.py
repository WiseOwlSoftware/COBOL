#!/usr/bin/env python3
"""The heading levels of the 1985 transcription (kb/Work PB6057), on planted text: the level its number's depth gives
(Section 1, Appendix 2, chapter 2, clause 3, one deeper for each further number, at most 6), a title line the page sets as
a heading becoming one, the audit's refusals (a clause at the wrong level, a numbered title left plain, a running header
set as a heading, a Section heading off its first page or twice, a contents entry with no heading or on another page), and
the scope (Amendment 1's addendum skipped, its Section A and the standard read)."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

H = selftest_cases.subject(__file__)   # scripts/spec/heading_levels.py

CONTENTS = """## Page 9

# Contents

<u>SECTION I:  INTRODUCTORY INFORMATION</u>

Chapter 1:  Introduction to the Standard

| | | |
|---|---|---|
| 1.1 | Scope and Purpose .................. | I-1 |
| 1.2 | Structure of Language ............... | I-2 |

<u>SECTION II: CONCEPTS</u>

| | | |
|---|---|---|
| 1. | Introduction ........................ | II-1 |
"""
GOOD_BODY = """## Page 21

# <u>SECTION I: INTRODUCTORY INFORMATION</u>

## 1. <u>INTRODUCTION TO THE STANDARD</u>

### 1.1 SCOPE AND PURPOSE

Text.

I-1

## Page 22

### 1.2 STRUCTURE OF LANGUAGE

#### 1.2.1 Function

I-2

## Page 85

# <u>SECTION II: CONCEPTS</u>

## 1. <u>INTRODUCTION</u>

II-1
"""
GOOD = CONTENTS + "\n" + GOOD_BODY


def _found(text, needle):
    return [f for f in H.check(text) if needle in f]


def case_00_a_headings_level_is_its_numbers_depth_plus_one(expect):
    expect.equal("a Section is 1, an Appendix 2, a chapter 2, a clause 3, each further number one deeper",
                 [H.level_of(t) for t in ("<u>SECTION IV: X</u>", "<u>APPENDIX B: X</u>", "4. <u>TABLE HANDLING</u>",
                                          "4.4 SUBSCRIPTING", "4.4.3 Subscripting Example", "6.2.2.2.1 A")],
                 [1, 2, 2, 3, 4, 6])
    expect.equal("no heading is deeper than Markdown's sixth level", H.level_of("4.2.2.1.3.3.1 Special Registers"), 6)
    expect.equal("Amendment 1's Section A is a Section", H.level_of("<u>SECTION A: INTRINSIC FUNCTION MODULE</u>"), 1)
    expect.equal("a box side, bold and an underline do not change the depth", H.level_of("║**3.4.1 Function**║"), 4)
    expect.equal("a running header has no level", H.level_of("Sequential I-O - Input-Output Section"), None)


def case_01_a_title_line_the_page_sets_as_a_heading_becomes_one(expect):
    expect.equal("a heading takes its level", H.canonical("## 4.4.3 Subscripting Example"), "#### 4.4.3 Subscripting Example")
    expect.equal("a plain numbered title becomes a heading",
                 H.canonical("1.6 DEFINITION OF A CONFORMING SOURCE PROGRAM"), "### 1.6 DEFINITION OF A CONFORMING SOURCE PROGRAM")
    expect.equal("its box side stays where the page prints it", H.canonical("║3.4 THE PURGE STATEMENT║"), "### ║3.4 THE PURGE STATEMENT║")
    expect.equal("a bold title stays bold under its hashes", H.canonical("**2.2 ARGUMENTS**"), "### **2.2 ARGUMENTS**")
    expect.equal("a sentence that opens with a number is no title", H.canonical("1.5 million records are read in each pass."), None)
    expect.equal("a heading at its level is left as it is", H.canonical("### 1.1 SCOPE"), "### 1.1 SCOPE")


def case_02_the_normalizer_repairs_what_the_audit_refuses(expect):
    text = GOOD.replace("### 1.1 SCOPE AND PURPOSE", "## 1.1 SCOPE AND PURPOSE").replace(
        "#### 1.2.1 Function", "1.2.1 Function")
    expect("the audit names the clause at the wrong level", bool(_found(text, "is at level 2, its number's depth gives 3")))
    expect("the audit names the title left plain", bool(_found(text, "a numbered title left as plain text")))
    fixed, changes = H.normalize(text)
    expect.equal("the normalizer changes exactly those two lines, naming each",
                 [(was, now) for _, _, was, now in changes],
                 [("## 1.1 SCOPE AND PURPOSE", "### 1.1 SCOPE AND PURPOSE"), ("1.2.1 Function", "#### 1.2.1 Function")])
    expect.equal("the audit then finds nothing", H.check(fixed), [])
    expect.equal("a text at its levels passes", H.check(GOOD), [])
    expect.equal("a second run changes nothing", H.normalize(fixed)[1], [])


def case_03_the_audit_refuses_a_running_header_and_a_misplaced_section_heading(expect):
    header = GOOD.replace("### 1.2 STRUCTURE OF LANGUAGE", "# Sequential I-O - Input-Output Section\n\n### 1.2 STRUCTURE OF LANGUAGE")
    expect("a running header set as a heading is named", bool(_found(header, "a running header or title set as a heading")))
    stray = GOOD.replace("I-1\n\n## Page 22", "I-1\n\n# SECTION II: CONCEPTS\n\n## Page 22")
    expect("a Section heading that is not on the page whose folio is its first is named",
           bool(_found(stray, "not on its first page, folio II-1")))
    expect("a Section headed twice is named", bool(_found(stray, "Section II is headed 2 times")))
    expect.equal("an underlined title is no running header", _found(GOOD.replace("### 1.2 STRUCTURE OF LANGUAGE",
                 "## <u>SUMMARY OF ELEMENTS</u>\n\n### 1.2 STRUCTURE OF LANGUAGE"), "running header"), [])


def case_04_every_contents_entry_is_a_heading_on_the_page_its_folio_names(expect):
    rows = H.contents(GOOD.split("\n"))
    expect.equal("the contents read as (section, number, title, folio)",
                 [r for r in rows if r[1]],
                 [("I", "1.", "Introduction to the Standard", ""), ("I", "1.1", "Scope and Purpose", "I-1"),
                  ("I", "1.2", "Structure of Language", "I-2"), ("II", "1.", "Introduction", "II-1")])
    gone = GOOD.replace("### 1.2 STRUCTURE OF LANGUAGE", "### 1.2 SOMETHING ELSE")
    expect("an entry with no heading of its title is named", bool(_found(gone, "contents entry I.1.2 Structure of Language: no heading")))
    moved = GOOD.replace("### 1.1 SCOPE AND PURPOSE\n\nText.\n\nI-1\n\n## Page 22\n", "text.\n\nI-1\n\n## Page 22\n\n### 1.1 SCOPE AND PURPOSE\n")
    expect("an entry whose heading is on another page is named", bool(_found(moved, "the contents print I-1")))
    differs = GOOD.replace("## 1. <u>INTRODUCTION</u>\n\nII-1", "## 1. <u>INTRODUCTION OF THE MODULE</u>\n\nII-1")
    expect("a title that differs from the contents is named until the difference is recorded as printed",
           bool(_found(differs, "contents entry II.1. Introduction")))
    expect.equal("the printed differences are the ones read on the rendered page",
                 sorted(H.PRINTED_DIFFERENTLY), [("XI", "1."), ("XIII", "4.8")])
    continued = GOOD.replace("<u>SECTION II: CONCEPTS</u>\n\n| | | |", "Chapter 2:  Environment Division (Continued)\n\n<u>SECTION II: CONCEPTS</u>\n\n| | | |", 1)
    expect.equal("a chapter line the page break repeats is no entry",
                 [r for r in H.contents(continued.split("\n")) if "Continued" in r[2]], [])


def case_05_the_scope_is_the_standard_and_amendment_1s_section_a(expect):
    amended = (GOOD + "\n# AMENDMENT 1: INTRINSIC FUNCTION MODULE (ANSI X3.23a-1989)\n\n## Page 831\n\n## 8. INTRINSIC FUNCTION FACILITY\n\n"
               "**4.3.8.3 Function-Identifier**\n\n## Page 857\n\n# SECTION A: INTRINSIC FUNCTION MODULE\n\n## 1. INTRODUCTION\n\n"
               "## 2.3 TYPES OF FUNCTIONS\n\n# AMENDMENT 2: CORRECTION AMENDMENT (ANSI X3.23b-1993)\n\n## 2.4 NOT READ\n")
    found = H.check(amended)
    expect.equal("the addendum's quoted clauses are out of scope", [f for f in found if "Function-Identifier" in f or "8. INTRINSIC" in f], [])
    expect("Section A is held to the rule", any("2.3 TYPES OF FUNCTIONS" in f and "level 2" in f for f in found))
    expect.equal("Amendment 2 is out of scope", [f for f in found if "NOT READ" in f], [])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
