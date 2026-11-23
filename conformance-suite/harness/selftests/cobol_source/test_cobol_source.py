#!/usr/bin/env python3
"""The text the lint reads after COPY REPLACING and REPLACE (DESIGN-conformance-suite.md section 3.3, kb/Work PB3178): a
separator period inside pseudo-text does not end the statement (§1985:XII.2.2 format; pseudo-text holds any text words)."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

T = selftest_cases.subject(__file__)   # conformance-suite/harness/cobol_source.py

HEAD = ["       DATA DIVISION.", "       WORKING-STORAGE SECTION.", "       01 N PIC 9.", "       PROCEDURE DIVISION.",
        "       P1."]


def _toks(*lines):
    return T.tokenize(T.read_lines("\n".join(lines), "fixed"))


def _operands(tokens):
    return [i.operand for i in T.display_issues(tokens)]


def case_copy_replacing_period_in_pseudo_text(expect):
    lib = _toks("           SHOW.")
    cp = _toks(*HEAD, "           COPY LIB REPLACING ==SHOW== BY ==DISPLAY N==",
               "               ==Q== BY ==P2. CONTINUE==.",
               "           STOP RUN.")
    expect.equal("the pair before the period-holding operand still replaces", _operands(T.expand_copies(cp, lambda n: lib)), ["N"])
    expect("the COPY is seen as REPLACING", T.copy_statements(cp)[0].replacing)


def case_replace_period_in_pseudo_text(expect):
    plain = _toks(*HEAD, "       REPLACE ==SHOW== BY ==DISPLAY N==.", "           SHOW.", "           STOP RUN.")
    holds = _toks(*HEAD, "       REPLACE ==SHOW== BY ==DISPLAY N==", "           ==Q== BY ==P2. CONTINUE==.", "           SHOW.",
                  "           STOP RUN.")
    expect.equal("no period inside the pseudo-text", _operands(T.apply_replace_statements(plain)), ["N"])
    expect.equal("a period inside the second pair's pseudo-text", _operands(T.apply_replace_statements(holds)), ["N"])


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
