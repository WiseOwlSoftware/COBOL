#!/usr/bin/env python3
"""differential_fuzz.py — a local model writes COBOL programs; WiseOwl and GnuCOBOL both compile and run each one; only
the disagreements survive to a triage report (kb/Work PB3463; the experiment record is
docs/rearchitecture/evidence/fleet-optimization/2026-10-20-differential-fuzz.md).

    python scripts/differential_fuzz.py --model qwen3-coder:30b --count 40 [--jobs 3] [--clauses 14.9,15] [--out DIR]
    python scripts/differential_fuzz.py --model qwen3-coder:30b --hours 8 --jobs 4           # an overnight run
    python scripts/differential_fuzz.py --bake-off qwen3-coder:30b,qwen3.6:35b --count 20
    python scripts/differential_fuzz.py --report DIR                                           # re-render DIR/report.md
    python scripts/differential_fuzz.py --self-test

THE RUN CADENCE (the owner's standing rule, 2026-10-22 ~12:15 PDT: "We fix all compiler bugs diagnosed by each fuzz test
and get the fixes into main before the next fuzz"). One run, then its triage (every survivor read against the standard;
each confirmed compiler defect a `kb/Work/` note tagged `fuzz`), then every one of those defects fixed and landed on main,
then the judge (`--cobol`) rebuilt from that main, and only then the next run. The harness holds the third step: a run
(`--model`) REFUSES to start while any `kb/Work/` note tagged `fuzz` that is not `process_only` is still open (any status
the register does not call terminal, `work.TERMINAL_STATUSES`), and names those notes; `--force-open-notes` starts it
anyway and names them. A bake-off, `--report` and `--self-test` are not runs and are never refused (kb/Work PB3523).

WHAT THE MODEL IS FOR, AND WHAT IT IS NOT. The owner's rule (2026-10-03, kept by every local-model experiment since): the
ISO spec decides (CLAUDE.md rule 1); a local model produces LEADS, and nothing it writes enters `kb/Work/`, a golden or the
conformance suite without a Claude or human check against the standard. Here the model needs no judgment at all: it only
has to write plausible standard COBOL. Two compilers judge the program, and GnuCOBOL 3.2 is a NET, never authority — an
OUTPUT_DIFFERS row says the compilers disagree, not which one is right. The 2026-10-09 one-shot (kb/Work PB2886) sank on a
test program that used four constructs the standard does not have; here such a program is one BOTH compilers refuse
(BOTH_REJECT), counted against the model and dropped. A program only GnuCOBOL refuses is KEPT (THEY_REJECT): WiseOwl is the
compiler built to the standard, so its acceptance is a reading GnuCOBOL's refusal cannot override (kb/Work PB3488). The
model's commonest invention is an intrinsic function the standard does not have (TRANSLATE, NATIONAL, SUBSTRING), so the
prompt carries the standard's own function list, read from specs/ISO_COBOL.md (15.6 and the 15.n function clauses).

A REFUSED PROGRAM COSTS FOUR GENERATIONS, SO THE PROMPT AIMS AT THE FIRST ONE (kb/Work PB3523). The second night's model
wrote a program both compilers refused 47 % of the time, and about 65 % of its generations went to programs that never
compiled: invented phrases, reserved words used as names, TRUE as a value, a SPECIAL-NAMES "section", 2023 words under
`--std 2014`. So the seed's clause reaches the model cut at its structure (`seed_text`: the general format and the syntax
rules whole, the general rules only as far as the budget allows) with the transcription's markup stripped and the
diagram notation explained; the system prompt carries the program skeleton, the naming rule, the reserved words the run
tripped on (each held to 8.9 by the self-test), the words 2023 added (Annex E.2 item 25) and one worked example per seed
FAMILY (`FAMILIES`: data description, statement, intrinsic function, expression), each compiled with the same output by
both compilers before it was written here. A correction quotes the refused source line beside each diagnostic, every
refusal round's first diagnostics are recorded on the row (`repair_rounds`), and a round whose first diagnostics repeat
the previous round's ends the corrections (`stopped_early`): the same error twice is a program the model cannot fix.
A clause of an optional module the conformance claim declines (docs/CONFORMANCE.md §5: the screen section, VALIDATE,
COMMIT and ROLLBACK, FORMAT and SELECT WHEN) is not a seed (`declined_seed_titles`): WiseOwl refuses it by name, so its
programs could only be refused or explained. MEASURED (2026-10-22, the PB3523 bake-offs, 40 programs an arm): the prompt
did not move first-attempt validity beyond the run-to-run noise (10, 10 and 9 of 40 on one seed walk); the early stop
saved 12 % of the generations; the declined seeds were 10 % of the second night's programs and 14 % of its BOTH_REJECT.
What remains is the model writing constructs it does not know (the report writer, OO, SORT and MERGE files, SET's rarer
formats), which a prompt has not been shown to fix.

THE MECHANICS ARE THE CONFORMANCE SUITE'S, NOT A SECOND COPY. Each program is compiled and run through
`conformance-suite/adapters/<compiler>.json` by the harness's own `Run.operation` (conformance-suite/harness/run.py), so the
command lines, the crash exit codes, the locale pin and the hang guard are the suite's. On Windows the GnuCOBOL steps cross
into WSL through the adapter's hook interface (`WslBridge`), as the adapter's own comment prescribes ("run it where cobc is:
on Windows, under WSL"). Both compilers run the 2014 edition, the latest GnuCOBOL 3.2 has, in free reference format.

THE VERDICT CLASSES, per program (one JSON line each in `results.jsonl`, appended as the run goes so a kill loses nothing):
  BOTH_REJECT      neither compiler compiled it after every correction, or a correction drew the same first diagnostics
                   as the round before it (`stopped_early`): the model's program, not a lead. Counted against the model.
  NO_PROGRAM       the reply held no program, or was cut off, after every correction: counted against the model.
  BOTH_HANG        both compilers' compiles or programs exceeded the hang guard: the model's program (an unsigned PERFORM
                   VARYING counter, a loop that changes neither bound), not a lead. Counted against the model (kb/Work PB3490).
  THEY_REJECT      WiseOwl compiled it, GnuCOBOL refused: a lead — GnuCOBOL 3.2 lacks the construct, or WiseOwl accepts a
                   program the standard forbids (an under-rejection); the spec decides at triage.
  AGREE            both compiled and ran, same normalized output (CutRunner.Normalize). Dropped.
  WE_REJECT        GnuCOBOL compiled it, WiseOwl refused: a candidate reject-legal-source lead — ours only where the
                   construct is ISO (GnuCOBOL's default dialect carries extensions, so `-std=cobol2014` is pinned).
  OUTPUT_DIFFERS   both ran, outputs differ: a candidate wrong answer on one side; the spec decides which.
  OUTPUT_LATITUDE  both ran, the outputs differ ONLY in the display form of numeric items — GnuCOBOL's leading separate sign
                   and inserted decimal point against WiseOwl's digit image with an over-punched sign — and the program
                   declares no picture that prints a point and no SIGN SEPARATE item (display_fold_allowed: the text alone
                   cannot tell the latitude from a dropped point). ONLY A ROW RECORDED BEFORE kb/Work PB3505 can be one: until
                   then WiseOwl's DISPLAY of a numeric item was the digit image (the settled PB803 sign convention, and the
                   point PB3467 asked about); PB3467's answer made GnuCOBOL's form WiseOwl's default (`--numeric-display
                   decimal`), so in a run made now the same difference is a defect and stays OUTPUT_DIFFERS. A row records the
                   form its WiseOwl compile used (`numeric_display`); a row recorded without it is a digit-image row.
                   501 of the first night's 1,141 output rows. Recorded under its own heading, never a survivor.
  WE_CRASH         WiseOwl's compiler crashed (the adapter's crash exit codes), or an unhandled exception at run time.
  WE_FAIL_RUN      same stdout, but WiseOwl's program exited non-zero where GnuCOBOL's exited zero: a run-time abort after the
                   last DISPLAY (a failing CLOSE, a STOP RUN status), invisible in the output.
  EXIT_DIFFERS     same stdout, both programs ended with a non-zero status, not the same one.
  WE_HANG          WiseOwl's compile or program exceeded the hang guard while GnuCOBOL's did not hang.
  THEY_FAIL_RUN    GnuCOBOL compiled it but its program failed at run time while ours ran: low value, kept for the record.
  THEY_HANG        GnuCOBOL's compile or program exceeded the hang guard while WiseOwl's did not hang.
  RUNNER_ERROR     a compiler did not start or the harness could not observe it: never a compiler verdict (plan §11 A12e).
Survivors are grouped by SIGNATURE (class + WiseOwl's diagnostic codes, or class + clause) so one defect hit forty times
is one triage row. `report.md` is rendered at the end and every REPORT_EVERY programs.

Standard library only. Writes under `--out` (default conformance-suite/results/fuzz-<stamp>/, git-ignored); never touches
the repository's tracked files. The model is unloaded when the run ends (the GPU is the owner's and shared).
"""
from __future__ import annotations

import argparse
import contextlib
import dataclasses
import html
import io
import json
import os
import random
import re
import shutil
import tempfile
import sys
import threading
import time
import traceback
import types
import typing
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO = Path(__file__).resolve().parent.parent
SUITE = REPO / "conformance-suite"
sys.path.insert(0, str(SUITE / "harness"))
sys.path.insert(0, str(REPO / "scripts" / "spec"))
import config as suite_config  # noqa: E402
import run as suite_run  # noqa: E402
from spec_headings import GENERAL_FORMAT_NOUN  # noqa: E402  — THE one spelling of the heading (kb/Work PB284)

SPEC = REPO / "specs" / "ISO_COBOL.md"
OLLAMA = os.environ.get("OLLAMA_HOST", "http://localhost:11434")
EDITION = "2014"            # the latest edition GnuCOBOL 3.2 has a dialect for; pinned on both compilers
SOURCE_FORMAT = "free"
HANG_GUARD = 90.0           # seconds per process: a generous limit, never a time expectation
NUM_PREDICT = 6000          # output tokens per reply; an 80-line free-format program is well under 2000
NUM_CTX = 16384             # the context Ollama gives the model: the whole exchange, every correction included
REPORT_EVERY = 25
DEFAULT_CLAUSES = ("14.9", "15", "13.18", "8.8")   # statements, intrinsic functions, data description clauses, arithmetic
#: the spec's own heading shape (one definition for the markdown transcription; the PDF's is clause_page.index)
HEADING = re.compile(r"^#{2,6} ((?:\d+|[A-Z])(?:\.\d+)+|\d+) (.*)$")
#: any info string: a model was seen to write "```cob注释"; the program is the fenced block that has a PROCEDURE DIVISION
CODE_FENCE = re.compile(r"```[^\n]*\n(.*?)(?:```|\Z)", re.DOTALL)   # a truncated reply never closes its fence
#: the structural sub-clauses every statement, function and clause has; each folds into its parent seed. The general-format
#: heading's spelling is THE one in spec_headings.py (kb/Work PB284), composed, never re-typed.
STRUCTURAL = re.compile(r"^(General|" + GENERAL_FORMAT_NOUN + r"|Syntax rules?|General rules?|Format \d+.*|Arguments?(?: rules?)?"
                        r"|Returned values?(?: rules?)?)\s*$", re.IGNORECASE)
#: The spellings are the spec's own, counted over its headings under 15 and 14.9 (2026-10-20): "Argument rules" 45,
#: "Returned value rules" 49, "Returned value rule" 43, "Argument rule" 37, "Arguments rules" 2, "Returned values rules" 1,
#: "Returned Value rule" 1, "Syntax rule" 2. The first version folded "Arguments" and "Returned value(s)" only, so
#: "15.66.3 Argument rules" became a seed of its own with no function name in it, and every model guessed the function.
DIAG_CODE = re.compile(r"\b((?:COBOLNET|COBOL|CBL)\d{4})\b")

FUNCTION_HEADING = re.compile(r"^#{2,6} 15\.\d+ ([A-Z][A-Z0-9-]*) function\s*$")
#: Annex E.3.3's function items: "22) **FUNCTION BASECONVERT.** This function has been added …", "23) **FUNCTION CONCAT.** The
#: CONCAT function has been added …", "27) **FUNCTION FIND-STRING.** The FIND-STRING intrinsic function has been added …";
#: an enhancement reads "An optional argument has been added to this function" (EXCEPTION-FILE) or "has been enhanced" (TRIM).
ANNEX_E33_FUNCTION_ITEM = re.compile(r"^\d+\\?\) \*\*FUNCTION ([A-Z][A-Z0-9-]*)\.?\*\* (.*)$")
FUNCTION_ADDED = re.compile(r"^(?:This function|The ([A-Z][A-Z0-9-]*) (?:intrinsic )?function) has been added\b")
#: a statement's addition is said in a sentence, in its own item or another's ("35) … The DELETE FILE statement has been added")
STATEMENT_ADDED = re.compile(r"\bThe ([A-Z][A-Z0-9 -]*?) statement has been added\b")
ANNEX_HEADING = re.compile(r"^#{2,6} E\.3\.3\b")
ANNEX_END = re.compile(r"^#{2,6} (?:E\.3\.[4-9]|E\.[4-9]|F)\b")


def added_in_2023(text: str) -> tuple[set[str], set[str]]:
    """(functions, statements) the standard itself says 2023 ADDED: Annex E.3.3 of the transcription ("Substantive changes
    probably not affecting existing programs"): a FUNCTION item whose text opens "This function has been added" or "The X
    [intrinsic] function has been added" (TRIM "has been enhanced" and EXCEPTION-FILE, whose optional ARGUMENT "has been
    added", existed in 2014 and stay), and every sentence "The X statement has been added" in any item (DELETE FILE's own
    item describes it; item 35 on the I-O statuses says it was added). The seeds are the 2023 text while both compilers run
    `EDITION` 2014, so these names leave the prompt's function list and the seed list (review R1-6: the prompt told the model
    CONCAT was a function and WiseOwl refused it under --std 2014, COBOLNET1502). Read from the spec, never typed:
    VERSION_CHANGE_REFERENCE.md rows 19 and 58 to 73 are the same seven functions and DELETE FILE."""
    functions: set[str] = set()
    statements: set[str] = set()
    inside = False
    for line in text.splitlines():
        if ANNEX_HEADING.match(line):
            inside = True
            continue
        if inside and ANNEX_END.match(line):
            break
        if not inside:
            continue
        m = ANNEX_E33_FUNCTION_ITEM.match(line)
        if m:
            added = FUNCTION_ADDED.match(m.group(2))
            if added and (added.group(1) is None or added.group(1) == m.group(1)):
                functions.add(m.group(1))
        statements.update(st.strip() for st in STATEMENT_ADDED.findall(line))
    return functions, statements

#: The intrinsic functions whose value depends on the clock, the compilation, the locale or a random sequence: two compilers
#: cannot agree on their output, so the prompt forbids them AND their clauses are not seeds (the second bake-off of
#: 2026-10-20 spent 3 to 4 programs in 20 on WHEN-COMPILED and LOCALE-TIME, every one an OUTPUT_DIFFERS that means nothing).
#: One definition; the self-test holds every name to the spec's function list.
NONDETERMINISTIC_FUNCTIONS = ("CURRENT-DATE", "FORMATTED-CURRENT-DATE", "WHEN-COMPILED", "RANDOM", "SECONDS-PAST-MIDNIGHT",
                              "LOCALE-COMPARE", "LOCALE-DATE", "LOCALE-TIME", "LOCALE-TIME-FROM-SECONDS")


def spec_functions(text: str) -> list[str]:
    """The intrinsic functions of the EDITION the compilers run: one per `15.n NAME function` clause heading of the
    transcription (the same headings 15.6 "Summary of functions" lists), less the ones Annex E.3.3 says 2023 added
    (`added_in_2023`). Read from the spec, never typed here: the 2026-10-20 bake-off's models invented FUNCTION TRANSLATE,
    NATIONAL, SUBSTRING and YEAR, and both compilers refused each of them (WiseOwl citing 15.6)."""
    added, _statements = added_in_2023(text)
    return [m.group(1) for line in text.splitlines() if (m := FUNCTION_HEADING.match(line)) and m.group(1) not in added]


RESERVED_HEADING = re.compile(r"^#{2,6} 8\.9 Reserved words\s*$")
RESERVED_ITEM = re.compile(r"^- ([A-Z][A-Z0-9-]*)\s*$")


def reserved_words(text: str) -> set[str]:
    """The reserved words of §8.9 ("The following is the list of reserved words:", one `- WORD` item per line), which
    §8.3.2.4.1 says "shall not be used as system-names or user-defined words". The prompt names only the ones the
    2026-10-21 run misused (`MISUSED_RESERVED_WORDS`); this list is what the self-test holds those names to."""
    words: set[str] = set()
    inside = False
    for line in text.splitlines():
        if RESERVED_HEADING.match(line):
            inside = True
            continue
        if inside and HEADING.match(line):
            break
        if inside and (m := RESERVED_ITEM.match(line)):
            words.add(m.group(1))
    return words


CONFORMANCE = REPO / "docs" / "CONFORMANCE.md"
ANNEX_A4_HEADING = re.compile(r"^#{2,6} (A\.4\.\d+) ")
#: An Annex A.4 item naming a language element and its clause: "6\) BLINK clause (13.18.9)", "4\) COMMIT statement 14.9.7".
ANNEX_A4_ITEM = re.compile(r"^\d+\\?\) (?P<name>.+?)(?: \([\d.]+\)| \d+(?:\.\d+)+.*)\s*$")
#: An item that is a WHOLE clause or statement; "ACCEPT statement, format 3: screen (14.9.1)" is one format of a claimed
#: statement and "REWRITE FILE file-name-1 (14.9.35)" one phrase of one, so neither is.
WHOLE_ELEMENT = re.compile(r"^[A-Z][A-Z0-9-]*(?: [A-Z][A-Z0-9-]*)* (?:clause|statement)$")
#: A row of CONFORMANCE.md §5, "Annex A.4 optional-element disposition": `| A.4.2 | <module> | Not claimed | … |`.
DISPOSITION_ROW = re.compile(r"^\| (A\.4\.\d+) \| ([^|]+?) \| ([^|]+?) \|")


def declined_modules(conformance_text: str) -> dict[str, str]:
    """{Annex A.4 number: module name} for every optional module docs/CONFORMANCE.md §5 records as Not claimed: the
    conformance claim's one statement of what WiseOwl declines (§4.2.7), read, never restated here."""
    return {m.group(1): m.group(2).strip() for line in conformance_text.splitlines()
            if (m := DISPOSITION_ROW.match(line)) and m.group(3).strip("* ").startswith("Not claimed")}


def declined_seed_titles(spec_text: str, conformance_text: str) -> set[str]:
    """The seed titles that are a WHOLE clause or statement of a declined optional module (kb/Work PB3523): the Annex A.4
    items of each module `declined_modules` names. WiseOwl refuses such a construct by name (the screen section's
    COBOLNET1560, VALIDATE's, COMMIT's …), so a program seeded there ends BOTH_REJECT or as a WE_REJECT the conformance
    claim already explains, never a lead. In the second night's run (fuzz-20261021-211424) these seeds wrote 486 of the
    4,931 programs, 335 of its 2,326 BOTH_REJECT rows and 40 such WE_REJECT rows, at 1,687 of its 14,479 generations; the
    first fourteen seeds of the 2026-10-22 bake-off held four of them (BLINK, BLANK, UNDERLINE, REQUIRED). Matched by TITLE,
    because the transcription's numbers in these lists are not all right (A.4.14 item 9 prints "VALIDATE statement
    (13.18.64)", the VARYING clause's number). A module that becomes claimed returns its seeds with no change here."""
    declined = declined_modules(conformance_text)
    titles: set[str] = set()
    module = None
    for line in spec_text.splitlines():
        if (h := ANNEX_A4_HEADING.match(line)):
            module = h.group(1)
            continue
        if module and HEADING.match(line):
            module = None
        if module in declined and (m := ANNEX_A4_ITEM.match(line)) and WHOLE_ELEMENT.match(m.group("name")):
            titles.add(m.group("name"))
    return titles


#: Annex E.2 item 25 "The following reserved words have been added": one "- — WORD" line each ("— XOR", the last, lost its
#: list marker in the transcription), up to the item's "Justification:".
ANNEX_E2_RESERVED_ITEM = re.compile(r"^\d+\\?\) \*\*Reserved words\.\*\* The following reserved words have been added:")
ADDED_WORD = re.compile(r"^(?:- )?— ([A-Z][A-Z0-9-]*)\s*$")


def added_reserved_words(text: str) -> list[str]:
    """The words 2023 reserved (Annex E.2 item 25: XOR, EXCLUSIVE-OR, SEND, RECEIVE, COMMIT, ROLLBACK, the B-SHIFT
    operators …), read from the spec: under `EDITION` 2014 each is a user-defined word to both compilers, and the second
    night's model wrote XOR and SEND as 2023 syntax (WiseOwl COBOLNET0900, "requires COBOL-2023"), so the prompt names them
    beside the functions and the statement 2023 added."""
    words: list[str] = []
    inside = False
    for line in text.splitlines():
        if ANNEX_E2_RESERVED_ITEM.match(line):
            inside = True
            continue
        if inside and line.startswith("Justification"):
            break
        if inside and (m := ADDED_WORD.match(line)):
            words.append(m.group(1))
    return words


#: Not a seed (built per spec text in `clauses`): a clause of one of the forbidden functions (its programs cannot agree
#: between two compilers), a function or statement 2023 added (the compilers run 2014: review R1-6), and 15.6 "Summary of
#: functions", a table (it was a seed of the second bake-off of 2026-10-20).

#: The seed FAMILIES (kb/Work PB3523 item 1): a clause's family is the first entry whose prefix it is under, and a clause
#: under none is a statement's. Each family has its own worked example in the system prompt, because the night of
#: 2026-10-21 failed by family: data description clauses (§13.18: 61 % refused by both compilers), statements (§14.9: 58 %)
#: and references and expressions (§8.4: 53 %), each with a shape one statement example did not show.
FAMILIES: tuple[tuple[str, str], ...] = (("15", "function"), ("14", "statement"), ("13", "data"), ("8.5", "data"),
                                         ("8.8", "expression"), ("8.4", "expression"))
DEFAULT_FAMILY = "statement"


def family_of(number: str) -> str:
    """The seed family of clause `number` (FAMILIES; whole components only, so 8.40 is not under 8.4)."""
    return next((family for prefix, family in FAMILIES if number == prefix or number.startswith(prefix + ".")), DEFAULT_FAMILY)


#: One worked example per family, of the shapes the models kept getting wrong: a table with INDEXED BY and the SEARCH of its
#: OCCURS entry, COMPUTE with ROUNDED and both SIZE ERROR arms, EVALUATE, STRING and INSPECT (statement); level numbers,
#: condition-names and SET TO TRUE, REDEFINES, USAGE PACKED-DECIMAL and BINARY, SIGN SEPARATE, JUSTIFIED, BLANK WHEN ZERO
#: and a table inside a group (data); intrinsic functions as operands of COMPUTE and DISPLAY (function); qualification,
#: reference modification, abbreviated combined relations, class, sign and condition-name conditions, EVALUATE ALSO
#: (expression). Each was compiled and run by both compilers with identical output (AGREE) on 2026-10-22, with WiseOwl
#: built from this tree and GnuCOBOL 3.2 under `-std=cobol2014`, before it was written here. Free format, no
#: indentation, PROGRAM-ID FUZZ, as the rules require; the self-test holds every one to those rules.
EXAMPLES: dict[str, str] = {
    "statement": """IDENTIFICATION DIVISION.
PROGRAM-ID. FUZZ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-AMOUNT PIC S9(5)V99 VALUE -123.45.
01 WS-RESULT PIC S9(7)V99 VALUE 0.
01 WS-EDITED PIC -ZZ,ZZ9.99.
01 WS-TEXT PIC X(12) VALUE "ABCDEFGHIJKC".
01 WS-OUT PIC X(12) VALUE SPACES.
01 WS-COUNT PIC 9(3) VALUE 0.
01 WS-IDX PIC 99 VALUE 0.
01 WS-TABLE.
05 WS-ENTRY OCCURS 4 TIMES INDEXED BY WS-TX.
10 WS-KEY PIC 9(2).
10 WS-NAME PIC X(5).
PROCEDURE DIVISION.
MAIN-PARA.
PERFORM VARYING WS-IDX FROM 1 BY 1 UNTIL WS-IDX > 4
MOVE WS-IDX TO WS-KEY (WS-IDX)
MOVE "ITEM" TO WS-NAME (WS-IDX)
END-PERFORM
SET WS-TX TO 1
SEARCH WS-ENTRY
AT END DISPLAY "NOT FOUND"
WHEN WS-KEY (WS-TX) = 3
DISPLAY "FOUND " WS-NAME (WS-TX) " AT " WS-KEY (WS-TX)
END-SEARCH
COMPUTE WS-RESULT ROUNDED = WS-AMOUNT * 3 / 7
ON SIZE ERROR DISPLAY "SIZE ERROR"
NOT ON SIZE ERROR MOVE WS-RESULT TO WS-EDITED
END-COMPUTE
DISPLAY "RESULT " WS-EDITED
EVALUATE TRUE
WHEN WS-AMOUNT < 0 DISPLAY "NEGATIVE"
WHEN WS-AMOUNT = 0 DISPLAY "ZERO"
WHEN OTHER DISPLAY "POSITIVE"
END-EVALUATE
STRING "AB" DELIMITED BY SIZE WS-TEXT (3:4) DELIMITED BY SIZE INTO WS-OUT
END-STRING
DISPLAY "STRING [" WS-OUT "]"
INSPECT WS-TEXT TALLYING WS-COUNT FOR ALL "C"
DISPLAY "COUNT " WS-COUNT
IF WS-COUNT > 1 AND WS-AMOUNT < 0
DISPLAY "BOTH"
ELSE
DISPLAY "NOT BOTH"
END-IF
STOP RUN.
""",
    "data": """IDENTIFICATION DIVISION.
PROGRAM-ID. FUZZ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-RECORD.
05 WS-NAME PIC X(8) VALUE "ALPHA".
05 WS-CODE PIC 9(3) VALUE 42.
05 WS-STATE PIC X VALUE "Y".
88 WS-ACTIVE VALUE "Y".
88 WS-INACTIVE VALUE "N".
05 FILLER PIC X(2) VALUE SPACES.
01 WS-DATE PIC 9(8) VALUE 20261022.
01 WS-DATE-PARTS REDEFINES WS-DATE.
05 WS-YEAR PIC 9(4).
05 WS-MONTH PIC 9(2).
05 WS-DAY PIC 9(2).
01 WS-PACKED PIC S9(5)V99 USAGE PACKED-DECIMAL VALUE -12.5.
01 WS-BIN PIC S9(4) USAGE BINARY VALUE 300.
01 WS-LEAD PIC S9(3) SIGN IS LEADING SEPARATE VALUE -7.
01 WS-RIGHT PIC X(6) JUSTIFIED RIGHT VALUE SPACES.
01 WS-BWZ PIC ZZ9 BLANK WHEN ZERO.
01 WS-EDIT PIC +ZZ,ZZ9.99.
01 WS-LIST.
05 WS-ITEM PIC 9(2) OCCURS 3 TIMES.
PROCEDURE DIVISION.
MAIN-PARA.
DISPLAY "NAME [" WS-NAME "] CODE " WS-CODE
IF WS-ACTIVE
DISPLAY "ACTIVE"
END-IF
SET WS-INACTIVE TO TRUE
DISPLAY "STATE " WS-STATE
DISPLAY "YEAR " WS-YEAR " MONTH " WS-MONTH " DAY " WS-DAY
MOVE WS-PACKED TO WS-EDIT
DISPLAY "PACKED " WS-EDIT
MOVE WS-BIN TO WS-EDIT
DISPLAY "BINARY " WS-EDIT
DISPLAY "LEADING " WS-LEAD
MOVE "AB" TO WS-RIGHT
DISPLAY "JUSTIFIED [" WS-RIGHT "]"
MOVE 0 TO WS-BWZ
DISPLAY "BLANK [" WS-BWZ "]"
MOVE 10 TO WS-ITEM (1)
MOVE 20 TO WS-ITEM (2)
MOVE 30 TO WS-ITEM (3)
DISPLAY "ITEMS " WS-ITEM (1) " " WS-ITEM (2) " " WS-ITEM (3)
DISPLAY "RECORD [" WS-RECORD "]"
STOP RUN.
""",
    "function": """IDENTIFICATION DIVISION.
PROGRAM-ID. FUZZ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-TEXT PIC X(10) VALUE "Hello".
01 WS-NUM PIC S9(5)V99 VALUE 0.
01 WS-INT PIC S9(9) VALUE 0.
01 WS-EDIT PIC -Z(8)9.99.
01 WS-ARG PIC S9(3) VALUE -42.
PROCEDURE DIVISION.
MAIN-PARA.
DISPLAY "UPPER [" FUNCTION UPPER-CASE(WS-TEXT) "]"
DISPLAY "REVERSE [" FUNCTION REVERSE(WS-TEXT) "]"
COMPUTE WS-INT = FUNCTION LENGTH(WS-TEXT)
MOVE WS-INT TO WS-EDIT
DISPLAY "LENGTH " WS-EDIT
COMPUTE WS-INT = FUNCTION MOD(17, 5) + FUNCTION ABS(WS-ARG)
MOVE WS-INT TO WS-EDIT
DISPLAY "MOD+ABS " WS-EDIT
COMPUTE WS-NUM = FUNCTION NUMVAL("12.50") * 2
MOVE WS-NUM TO WS-EDIT
DISPLAY "NUMVAL " WS-EDIT
COMPUTE WS-INT = FUNCTION MAX(3, 9, 4) - FUNCTION MIN(3, 9, 4)
MOVE WS-INT TO WS-EDIT
DISPLAY "MAX-MIN " WS-EDIT
COMPUTE WS-INT = FUNCTION INTEGER-OF-DATE(20261022) - FUNCTION INTEGER-OF-DATE(20260101)
MOVE WS-INT TO WS-EDIT
DISPLAY "DAYS " WS-EDIT
COMPUTE WS-NUM ROUNDED = FUNCTION SQRT(2)
MOVE WS-NUM TO WS-EDIT
DISPLAY "SQRT " WS-EDIT
STOP RUN.
""",
    "expression": """IDENTIFICATION DIVISION.
PROGRAM-ID. FUZZ.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-GROUP.
05 WS-A PIC S9(3) VALUE 7.
05 WS-B PIC S9(3) VALUE -2.
05 WS-WORD PIC X(6) VALUE "ABC123".
01 WS-OTHER.
05 WS-A PIC S9(3) VALUE 1.
01 WS-LEVEL PIC 9 VALUE 2.
88 WS-LOW VALUE 0 THRU 3.
88 WS-HIGH VALUE 4 THRU 9.
01 WS-R PIC S9(5)V99 VALUE 0.
01 WS-EDIT PIC -ZZZZ9.99.
PROCEDURE DIVISION.
MAIN-PARA.
COMPUTE WS-R = (WS-A OF WS-GROUP + WS-B) * 2 ** 3 / 4
MOVE WS-R TO WS-EDIT
DISPLAY "EXPR " WS-EDIT
IF WS-A OF WS-GROUP > 5 AND < 10
DISPLAY "BETWEEN"
END-IF
IF WS-B IS NEGATIVE AND WS-A OF WS-OTHER IS NOT ZERO
DISPLAY "SIGNS"
END-IF
IF WS-WORD (1:3) IS ALPHABETIC AND WS-WORD (4:3) IS NUMERIC
DISPLAY "CLASSES"
END-IF
IF WS-LOW
DISPLAY "LOW"
END-IF
SET WS-HIGH TO TRUE
DISPLAY "LEVEL " WS-LEVEL
EVALUATE TRUE ALSO WS-A OF WS-OTHER
WHEN WS-HIGH ALSO 1 DISPLAY "HIGH AND ONE"
WHEN OTHER DISPLAY "OTHER"
END-EVALUATE
IF (WS-A OF WS-GROUP = 7 OR WS-B = 0) AND NOT (WS-LEVEL = 0)
DISPLAY "COMBINED"
END-IF
STOP RUN.
""",
}

#: The reserved words the 2026-10-21 run used as data names, functions or mnemonic-names, commonest first (WiseOwl's
#: COBOLNET0901 "is a reserved word in COBOL-2014", 294 programs both compilers refused: SCREEN 74, NULL 41, ADDRESS 34,
#: DESTINATION 22, OBJECT 21, CONDITION 18, BOOLEAN 13, VALIDATE-STATUS 13, DEFAULT 10). §8.3.2.4.1: "Reserved words shall
#: not be used as system-names or user-defined words". Named, not the whole §8.9 list of several hundred: the naming rule
#: (a prefix and a hyphen on every name) is what keeps the rest out. The self-test holds each to `reserved_words`.
MISUSED_RESERVED_WORDS = ("SCREEN", "NULL", "ADDRESS", "DESTINATION", "OBJECT", "CONDITION", "BOOLEAN", "VALIDATE-STATUS",
                          "DEFAULT", "LENGTH", "RETURNING", "COLUMN", "TYPE", "CLASS", "STATUS", "FUNCTION")

#: The standing rules (kb/Work PB3523 item 1), each written against what the second night's refusals show the model lacked:
#: the division skeleton (GnuCOBOL "PROCEDURE DIVISION header missing" 83, WiseOwl "unexpected 'SPECIAL-NAMES'" 35 and
#: "unexpected 'WORKING-STORAGE'" 34), the general format as the whole of a statement's or clause's syntax (COBOL0307, an
#: invented phrase, 315; COBOLNET1941, an invented data-description clause, 133), the naming rule and the misused reserved
#: words (COBOLNET0901, 294), TRUE and FALSE as values (COBOL0001 at 'TRUE' 57, 'FALSE' 11), a table at level 01 (GnuCOBOL
#: "01/77 OCCURS does not conform" 52, WiseOwl COBOLNET2404 33: §13.18.38.3 rule 1, "The OCCURS clause shall not be
#: specified in a data description entry that" has a level-number of 01, 66, 77 or 88), the SEARCH of a group rather than
#: of its OCCURS entry (GnuCOBOL "not indexed": §14.9.37.3 rule 2, "The data description of identifier-1 shall contain an
#: OCCURS clause with an INDEXED phrase"), and B in a PICTURE taken for a bit (COBOLNET1934 on PIC B(8), GnuCOBOL's
#: "PICTURE string must contain at least one of" 71).
SYSTEM_PROMPT = """You write small, standalone COBOL test programs for a compiler differential. Rules:
- Standard COBOL only, as in ISO/IEC 1989:2014. No vendor extensions, no COPY, no CALL to other programs, no EXEC.
- Free reference format: EVERY line starts in column 1 with no indentation, one statement per line. At most 80 lines, and no repetitive filler data items: a few well-chosen items exercised many ways.
- The headers, in this order, each ending in a period: IDENTIFICATION DIVISION. PROGRAM-ID. FUZZ. Then only when needed: ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. (a paragraph, never a section) and INPUT-OUTPUT SECTION. FILE-CONTROL. Then DATA DIVISION. with FILE SECTION., WORKING-STORAGE SECTION., LOCAL-STORAGE SECTION., LINKAGE SECTION. and REPORT SECTION. in that order, each only when needed. Then PROCEDURE DIVISION. and a paragraph name such as MAIN-PARA. before the first statement. In the procedure division use either no sections at all or put every paragraph in a section.
- The program must compile and run with no input: no ACCEPT from the console or standard input, no command-line arguments.
- Every result the program computes is shown with DISPLAY, so two compilers' outputs can be compared byte for byte.
- Output must be deterministic: no FUNCTION """ + ", ".join(NONDETERMINISTIC_FUNCTIONS) + """, no ACCEPT FROM DATE/TIME, nothing time, locale or environment dependent.
- No files unless the clause itself is about file handling; then ASSIGN each file to a plain name in the working directory and create it in the program (the harness removes the working directory afterwards).
- Exercise the clause named below in several distinct ways: edge values, both branches of every optional phrase, size and sign boundaries, truncation, ROUNDED, nested forms. Prefer cases a compiler could get subtly wrong.
- A statement or clause has exactly the phrases its general format shows, in that order; anything else is an error. The clause's own general format is given below. In a general format, [ ] (drawn with ┌ │ └) encloses an optional part, { } (drawn with ╭ ┤ ╰) a choice of exactly one alternative, and … lets the part before it repeat; lower-case words (identifier-1, literal-1, integer-1) stand for your own items and values; the figure notes say which upper-case words are required.
- Close every conditional statement with its END- word (END-IF, END-PERFORM, END-COMPUTE, END-EVALUATE, END-SEARCH).
- Every name you define (data items, condition-names, indexes, paragraphs, files) is a prefix and a hyphen and a word, such as WS-COUNT, WS-SCREEN-ROW or P-START, so it is never a reserved word. These are reserved words and never a data name, a function or a mnemonic-name: {reserved}.
- TRUE and FALSE are not values: never VALUE TRUE, never TRUE as an argument or an operand. They appear only in SET condition-name TO TRUE (or FALSE), in EVALUATE TRUE and WHEN TRUE, and in an 88 entry's WHEN SET TO FALSE phrase. NULL is only the value of a POINTER (SET WS-PTR TO NULL); there is no FUNCTION NULL.
- A table is an OCCURS entry inside an 01 group, never at level 01 or 77; SEARCH names that OCCURS entry, which has INDEXED BY.
- In a PICTURE, B inserts a space; it is not a bit, and every PICTURE has at least one 9, X, A, N or editing symbol.
- The ONLY intrinsic functions are these; any other FUNCTION name is an error: {functions}.
- Not in the 2014 edition the compilers run, so an error: {added}.
- Optional facilities one of the compilers does not provide, so never use them: {declined}.
- FUNCTION HIGHEST-ALGEBRAIC and LOWEST-ALGEBRAIC take a numeric DATA ITEM as the argument, never a literal or an expression.
- USAGE is one of the standard's (BINARY, PACKED-DECIMAL, DISPLAY, NATIONAL, INDEX, POINTER, ...); COMP-1 to COMP-5 are not in the standard.
- Reply with exactly one COBOL program inside one ```cobol fenced block and nothing else. PROGRAM-ID is FUZZ.

This program, which both compilers accept, shows the expected shape:
```cobol
{example}```"""


class PromptFacts(typing.NamedTuple):
    """What the system prompt states that is READ, never typed here: from the standard, the edition's functions
    (`spec_functions`), the functions and statements 2023 added (`added_in_2023`) and the words it reserved
    (`added_reserved_words`); from the conformance claim, the optional modules WiseOwl declines (`declined_modules`)."""
    functions: list[str]
    added: tuple[set[str], set[str]]
    added_words: list[str]
    declined: list[str]

    @classmethod
    def read(cls, spec_text: str, conformance_text: str) -> PromptFacts:
        return cls(spec_functions(spec_text), added_in_2023(spec_text), added_reserved_words(spec_text),
                   list(declined_modules(conformance_text).values()))


def system_prompt(facts: PromptFacts, family: str) -> str:
    """The system prompt for a seed of `family` (FAMILIES): `facts` filled in, the misused reserved words, and the family's
    worked example."""
    names = ([f"FUNCTION {f}" for f in sorted(facts.added[0])] + [f"the {st} statement" for st in sorted(facts.added[1])]
             + ([f"the words {', '.join(facts.added_words)}"] if facts.added_words else []))
    return (SYSTEM_PROMPT.replace("{functions}", ", ".join(facts.functions)).replace("{added}", ", ".join(names))
            .replace("{declined}", "; ".join(facts.declined)).replace("{reserved}", ", ".join(MISUSED_RESERVED_WORDS))
            .replace("{example}", EXAMPLES[family]))


# ── the seeds: spec clauses ───────────────────────────────────────────────────────────────────────────────────────────

def clauses(text: str, prefixes: tuple[str, ...]) -> list[tuple[str, str, str]]:
    """(number, title, body) for every clause of the markdown whose number is under one of `prefixes` and whose body
    carries a general format or rules worth a program. A STRUCTURAL sub-clause (14.9.25.2 "General formats", 14.9.25.4
    "General rules", a function's "Arguments") is not a seed of its own: its text folds into its parent, so one seed is
    one statement, function or clause with its formats and rules together. A bare heading with no body is not a seed. A seed
    nested under a clause of its own (13.18.40.5 "Editing rules" under the PICTURE clause, 15.3.1.6 "Week date formats") is
    titled with its ancestors below the second level ("PICTURE clause > Editing rules"), so the model is told whose rules
    they are: the 2026-10-20 bake-off's models guessed the function behind a bare "Argument rules" seed."""
    out: list[list[Any]] = []        # [number, title, [body lines]]
    cur: list[Any] | None = None
    titles: dict[str, str] = {}      # every heading's number -> title, for the ancestor chain
    added_functions, added_statements = added_in_2023(text)
    not_a_seed = re.compile(r"^(?:(?:" + "|".join(sorted(map(re.escape, set(NONDETERMINISTIC_FUNCTIONS) | added_functions))) + r") function"
                            + "".join(r"|" + re.escape(st) + r" statement" for st in sorted(added_statements)) + r"|Summary of functions)\s*$")

    def titled(number: str, title: str) -> str:
        chain = []
        n = number
        while n.count(".") >= 3:
            n = n.rsplit(".", 1)[0]
            if n in titles:
                chain.insert(0, titles[n])
        return " > ".join(chain + [title])

    def under(number: str) -> bool:
        return any(number == p or number.startswith(p + ".") for p in prefixes)

    for line in text.splitlines():
        m = HEADING.match(line)
        if m:
            number, title = m.group(1), m.group(2).strip()
            titles[number] = title
            if STRUCTURAL.match(title) and number.count(".") >= 2:
                # A structural SUB-clause folds into the seed it belongs to and never starts one of its own; at two
                # components the same words are a clause (15.3 "Arguments", 15.4 "Returned values": the rules every function
                # shares), and a seed.
                if cur is not None and number.startswith(cur[0] + "."):
                    cur[2].append(f"\n{title}\n")
                else:
                    cur = None
                continue
            cur = [number, titled(number, title), []]
            if under(number) and not not_a_seed.match(title):
                out.append(cur)
        elif cur is not None and not line.startswith("<a id="):
            cur[2].append(line)
    seeds = []
    for number, title, lines in out:
        body = "\n".join(lines).strip()
        if len(body) >= 200:
            seeds.append((number, title, body))
    return seeds


def order_seeds(seeds: list[tuple[str, str, str]], seed: int, in_order: bool) -> list[tuple[str, str, str]]:
    """The seed clauses in the order a run walks them: shuffled by `seed` (deterministic: the same seed is the same walk,
    so a run can be reproduced), or the spec's own order when `in_order`."""
    if in_order:
        return list(seeds)
    out = list(seeds)
    random.Random(seed).shuffle(out)
    return out


#: Characters of clause text a prompt carries (kb/Work PB3523 item 6); measured in `self_test` against NUM_CTX.
CLAUSE_BUDGET = 6000
#: A part of a seed's body starts at a sub-clause title `clauses` folded in (STRUCTURAL) other than a "Format n" one: the
#: spec writes "Format 1 (serial):" inside General formats and "FORMAT 1" inside the rules as lines of their own, so a
#: format line is the content of the part it stands in, never a part.
PART_TITLE = re.compile(r"^(?!Format \d)(?:" + STRUCTURAL.pattern.removeprefix("^") + r")", re.IGNORECASE)
FORMAT_PART = re.compile(r"^" + GENERAL_FORMAT_NOUN + r"$", re.IGNORECASE)
SYNTAX_PART = re.compile(r"^(?:Syntax rules?|Arguments?(?: rules?)?)$", re.IGNORECASE)
#: The transcription's markup the model has no use for: the diagrams' <pre> wrappers, the underline of a required word
#: (the figure notes name the underlined words in prose), line breaks, and the escaped list parentheses ("1\)").
MARKUP = re.compile(r"</?(?:pre|u|br|sup|sub)\b[^>]*>")
MD_ESCAPE = re.compile(r"\\([()\[\]*_#.])")
CUT_MARK = "[… the rest of this part is cut to fit the prompt]"


def clean_spec_text(text: str) -> str:
    """Clause text as the model reads it: the transcription's HTML markup and markdown escapes removed, entities decoded,
    runs of blank lines folded. The diagram's drawing (┌ │ └ for brackets, ╭ ┤ ╰ for braces) stays: the system prompt
    explains it."""
    text = html.unescape(MD_ESCAPE.sub(r"\1", MARKUP.sub("", text)))
    return re.sub(r"\n{3,}", "\n\n", text).strip()


def seed_parts(body: str) -> list[tuple[str, str]]:
    """(title, text) for each structural part of a seed's body, in the spec's order: the text before the first folded
    sub-clause title has the title "", and every line `clauses` wrote as a part title (PART_TITLE) starts a part."""
    parts: list[tuple[str, list[str]]] = [("", [])]
    for line in body.split("\n"):
        if line and line == line.strip() and PART_TITLE.match(line):
            parts.append((line, []))
        else:
            parts[-1][1].append(line)
    return [(title, "\n".join(lines)) for title, lines in parts if title or "\n".join(lines).strip()]


def seed_text(number: str, title: str, body: str, budget: int = CLAUSE_BUDGET) -> str:
    """The clause as the prompt carries it (kb/Work PB3523 items 1 and 6). Within `budget` characters it is the whole
    clause, cleaned. Over it, the cut keeps the clause's STRUCTURE rather than its first `budget` characters, in this
    order of claim: the general formats, then the syntax (or argument) rules, then the general introduction, then the
    general rules, each whole while it fits and cut once it does not (what the model gets wrong is the syntax, not the
    general rules). When the formats and the syntax rules together exceed the budget, the formats take at most two thirds
    of it, so the syntax rules are never squeezed out by a statement with fifteen formats (14.9.39 SET). The parts stay in
    the spec's order; a cut part ends with CUT_MARK and an omitted one is named at the end."""
    head = f"Clause {number} {title}\n\n"
    whole = clean_spec_text(body)
    if len(whole) <= budget:
        return head + whole
    parts = [(t, clean_spec_text(x)) for t, x in seed_parts(body)]
    rank = [0 if FORMAT_PART.match(t) else 1 if SYNTAX_PART.match(t) else 2 if t in ("", "General") else 3 for t, _x in parts]
    core = sum(len(t) + len(x) + 2 for k, (t, x) in enumerate(parts) if rank[k] < 2)
    room = budget
    kept: dict[int, str] = {}
    for tier in (0, 1, 2, 3):
        allowance = budget * 2 // 3 if tier == 0 and core > budget else room
        for k, (t, x) in enumerate(parts):
            if rank[k] != tier:
                continue
            cost = len(t) + len(x) + 2
            if cost <= min(room, allowance):
                kept[k] = x
            elif min(room, allowance) > 200:
                kept[k] = x[:min(room, allowance) - len(t) - len(CUT_MARK) - 4].rstrip() + "\n" + CUT_MARK
                cost = len(t) + len(kept[k]) + 2
            else:
                continue
            room -= cost
            allowance -= cost
    text = "\n\n".join((t + "\n" + kept[k]).strip() for k, (t, _x) in enumerate(parts) if k in kept)
    dropped = [t or "the introduction" for k, (t, _x) in enumerate(parts) if k not in kept]
    return head + text + (f"\n\n[{', '.join(dropped)}: omitted to fit the prompt]" if dropped else "")


# ── the model ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def ollama_chat(model: str, messages: list[dict[str, str]], temperature: float, keep_alive: str = "10m",
                timeout: float = 600.0) -> tuple[str, float, str, int]:
    """One chat completion over `messages` → (content, seconds, done_reason, thinking characters). `done_reason` is
    Ollama's: "stop" for a finished reply, "length" when NUM_PREDICT cut it off (the first smoke runs lost every program that
    way: a 3000-token cap and fixed-form indentation). A model that rejects the `think` field is asked again without it (the
    2026-10-03 spike). A model that thinks despite `think: false` (MiniMax M3.1 Flash on a full-size prompt, 2026-10-20: 28,000
    characters of thinking, every output token spent, empty content) is visible through the thinking length."""
    body: dict[str, Any] = {"model": model, "stream": False, "think": False, "keep_alive": keep_alive,
                            "messages": messages,
                            "options": {"temperature": temperature, "num_ctx": NUM_CTX, "num_predict": NUM_PREDICT}}
    for attempt in (0, 1):
        start = time.monotonic()
        req = urllib.request.Request(f"{OLLAMA}/api/chat", json.dumps(body).encode("utf-8"),
                                     {"Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(req, timeout=timeout) as r:
                reply = json.loads(r.read())
            message = reply["message"]
            return (message["content"].replace("<|eot|>", ""), time.monotonic() - start, reply.get("done_reason", "stop"),
                    len(message.get("thinking") or ""))
        except urllib.error.HTTPError as e:
            msg = e.read()[:300]
            if attempt == 0 and b"think" in msg:
                body.pop("think")
                continue
            if b"prediction aborted" in msg:
                # Ollama stopped a reply that fell into a repeating loop ("token repeat limit reached"): the model's reply,
                # degenerate, not a failure of the harness or the server. The caller treats it as a reply with no program.
                return "", time.monotonic() - start, "aborted", 0
            raise RuntimeError(f"ollama HTTP {e.code}: {msg!r}") from None
    raise AssertionError("unreachable")


def unload(model: str) -> None:
    """Free the GPU: a request with keep_alive 0 unloads the model (the GPU is shared with the owner's other work)."""
    try:
        req = urllib.request.Request(f"{OLLAMA}/api/generate", json.dumps({"model": model, "keep_alive": 0}).encode(),
                                     {"Content-Type": "application/json"})
        urllib.request.urlopen(req, timeout=60).read()
    except (urllib.error.URLError, OSError):
        pass


def extract_program(reply: str) -> str | None:
    """The fenced block of a reply that holds a PROCEDURE DIVISION (the longest, when several do), or the reply itself
    when it has one and no fence does."""
    blocks = [b for b in CODE_FENCE.findall(reply) if "PROCEDURE DIVISION" in b.upper()]
    text = max(blocks, key=len) if blocks else reply
    text = text.strip("\n")
    if "PROCEDURE DIVISION" not in text.upper():
        return None
    return text + "\n"


# ── the two compilers, through the suite's adapters ──────────────────────────────────────────────────────────────────

class WslBridge:
    """The GnuCOBOL adapter's hook on Windows: every operation runs inside WSL in the same working directory, which wsl.exe
    maps from the Windows path (E:\\x → /mnt/e/x); the run step addresses the program relative to it."""

    def __init__(self, distro: str = "Ubuntu") -> None:
        self.distro = distro

    def _execute(self, argv: tuple[str, ...], ctx: suite_config.StepContext) -> suite_config.Step:
        return suite_run.execute(("wsl", "-d", self.distro, "-e", *argv), ctx.work, ctx.environment, ctx.timeout)

    def compile(self, ctx: suite_config.StepContext) -> suite_config.Step:
        assert ctx.argv is not None
        return self._execute(ctx.argv, ctx)

    def flag(self, ctx: suite_config.StepContext) -> suite_config.Step:
        assert ctx.argv is not None
        return self._execute(ctx.argv, ctx)

    def run(self, ctx: suite_config.StepContext) -> suite_config.Step:
        return self._execute(("./" + ctx.placeholders["program"],), ctx)


def load_compiler(name: str, root: Path, work: Path, variables: dict[str, str], keep: bool) -> suite_run.Run:
    adapter, problems = suite_config.load_adapter(SUITE / "adapters" / f"{name}.json")
    if adapter is None:
        raise SystemExit("adapter problems: " + "; ".join(problems))
    profile, problems = suite_config.load_profile(SUITE / "profiles" / f"{adapter.profile}.json")
    if profile is None:
        raise SystemExit("profile problems: " + "; ".join(problems))
    if name == "gnucobol" and os.name == "nt":
        adapter = dataclasses.replace(adapter, hook=WslBridge())
    return suite_run.Run(root=root, adapter=adapter, profile=profile, variables={**adapter.variables, **variables},
                         tests={}, work=work, timeout=HANG_GUARD, keep=keep)


@dataclasses.dataclass
class Observation:
    """What one compiler did with one program."""
    compiled: bool | None          # None: not observed (RUNNER_ERROR)
    compile_rc: int | None
    diagnostics: str
    codes: list[str]
    ran: bool | None = None        # None: not attempted or not observed
    run_rc: int | None = None
    crashed: bool = False
    hung: bool = False
    output: str = ""
    runner_error: str = ""


def observe(run: suite_run.Run, entry: dict[str, Any], work: Path) -> Observation:
    """Compile, then run, one program through one compiler's adapter, as the suite's `accept` would, without judging."""
    source, program = run.stage(entry, work)
    step = run.operation("compile", EDITION, entry, work, source, program)
    if step.timed_out:
        # A compile that does not finish is an observation (a hang), not a harness failure: classify() reads `hung`.
        return Observation(None, step.returncode, step.text(), [], hung=True, runner_error=run.unjudgeable(step, "the compiler") or "")
    if step.error is not None:
        return Observation(None, step.returncode, step.text(), [], runner_error=run.unjudgeable(step, "the compiler") or "")
    diag = step.text()
    codes = sorted(set(DIAG_CODE.findall(diag)))
    if run.crashed(step):
        return Observation(False, step.returncode, diag, codes, crashed=True)
    if step.returncode != 0:
        return Observation(False, step.returncode, diag, codes)
    ran = run.operation("run", EDITION, entry, work, source, program)
    if ran.error is not None:
        return Observation(True, 0, diag, codes, ran=None, runner_error=f"the program did not start: {ran.error}")
    if ran.timed_out:
        return Observation(True, 0, diag, codes, ran=False, hung=True)
    out = suite_run.normalize(ran.stdout.decode("utf-8", errors="replace"))
    return Observation(True, 0, diag, codes, ran=True, run_rc=ran.returncode, crashed=run.crashed(ran), output=out)


def observe_fresh(run: suite_run.Run, entry: dict[str, Any], name: str) -> Observation:
    """`observe` in a work directory that holds nothing of an earlier attempt at the same program: a correction round
    re-stages the program, and `Run.stage` refuses an existing directory. A directory that cannot be removed (a file still
    held by the previous run's process) raises, and the row is a RUNNER_ERROR that names the cause, never a verdict."""
    work = run.work / name
    if work.exists():
        shutil.rmtree(work)
    return observe(run, entry, work)


# ── the verdict ───────────────────────────────────────────────────────────────────────────────────────────────────────

def classify(we: Observation, they: Observation, display_fold: bool = False) -> str:
    """The class of one program from the two observations. A refusal by GnuCOBOL alone is a lead (THEY_REJECT), a refusal
    by both is the model's failure (BOTH_REJECT); an unobserved compiler on either side is never a verdict, and a crash of
    ours outranks every refusal. `display_fold` is display_fold_allowed() of the program's source: without it an output
    difference is never OUTPUT_LATITUDE."""
    if we.hung and they.hung:
        # A program both compilers run (or compile) forever is the generator's, whichever side was observed first: the first
        # overnight report presented three of the model's infinite loops as WiseOwl hangs (kb/Work PB3490).
        return "BOTH_HANG"
    if they.compiled is None:
        return "THEY_HANG" if they.hung else "RUNNER_ERROR"
    if we.compiled is None:
        return "WE_HANG" if we.hung else "RUNNER_ERROR"
    if we.crashed:
        return "WE_CRASH"
    if they.compiled is False:
        return "BOTH_REJECT" if we.compiled is False else "THEY_REJECT"
    if we.compiled and we.ran is None:
        return "RUNNER_ERROR"
    if we.compiled is False:
        return "WE_REJECT"
    if we.hung:
        return "WE_HANG"
    if they.hung:
        return "THEY_HANG"
    if they.ran is None:
        return "RUNNER_ERROR"
    if they.run_rc != 0 and we.run_rc == 0:
        return "THEY_FAIL_RUN"
    if we.output != they.output:
        if display_fold and we.run_rc == they.run_rc and display_image(we.output) == display_image(they.output):
            return "OUTPUT_LATITUDE"
        return "OUTPUT_DIFFERS"
    # The exit status is an observable result too (ISO 14.9.42.4 General rule 2: the operating system is told of an error
    # termination): a fatal condition after the last DISPLAY leaves the same stdout, so it is compared here, never read as
    # agreement.
    if we.run_rc == they.run_rc:
        return "AGREE"
    return "WE_FAIL_RUN" if we.run_rc != 0 and they.run_rc == 0 else "EXIT_DIFFERS"


LEAD_CLASSES = ("WE_CRASH", "WE_REJECT", "THEY_REJECT", "OUTPUT_DIFFERS", "WE_FAIL_RUN", "EXIT_DIFFERS", "WE_HANG", "THEY_HANG",
                "THEY_FAIL_RUN")
#: the model's failures: counted against it in the report and the bake-off, never a lead
MODEL_FAILURE_CLASSES = ("BOTH_REJECT", "NO_PROGRAM", "BOTH_HANG")
#: a known implementor latitude: reported under its own heading, never a survivor
LATITUDE_CLASSES = ("OUTPUT_LATITUDE",)

#: the recorded outputs are cut at this length; a row whose output was cut keeps its recorded verdict under --report
RECORDED_OUTPUT = 4000

#: GnuCOBOL's display form of a numeric item: an optional leading separate sign, digits, a decimal point, digits. A value
#: with an inserted comma (an EDITED item, printed alike by both compilers) is left alone.
_GNU_SCALED = re.compile(r"(?<![0-9A-Za-z.,{}])([+-]?)(\d+)\.(\d+)(?![0-9A-Za-z.,])")
#: WiseOwl's display form of a SIGNED numeric item: the digit image with the sign over-punched on the last digit (IBM / Micro
#: Focus, PB803): { A-I positive 0-9, } J-R negative 0-9
_OVERPUNCH = re.compile(r"(?<![0-9A-Za-z.,{}])(\d+)([{}A-R])(?![0-9A-Za-z])")
_PUNCH = {**{c: ("+", str(i)) for i, c in enumerate("{ABCDEFGHI")}, **{c: ("-", str(i)) for i, c in enumerate("}JKLMNOPQR")}}


#: a PICTURE character-string (the sentence's period stripped; the keyword is not the tail of a data name such as WS-PIC,
#: review R2-1) and the SIGN clause's SEPARATE phrase (the words SIGN IS are optional, ISO 13.18.52.2; review R2-2)
_PICTURE = re.compile(r"(?<![\w-])PIC(?:TURE)?\s+(?:IS\s+)?(\S+)", re.IGNORECASE)
_SIGN_SEPARATE = re.compile(r"(?<![\w-])(?:SIGN\s+(?:IS\s+)?)?(?:LEADING|TRAILING)\s+SEPARATE\b", re.IGNORECASE)
#: the picture symbols that print a point or a sign: an edited item's output can carry them legitimately
_EDITING_SIGN_OR_POINT = re.compile(r"[.+-]|CR|DB", re.IGNORECASE)


#: The display form WiseOwl's DEFAULT compile gives a numeric item (`--numeric-display`, kb/Work PB3505, owner decision PB3467):
#: GnuCOBOL's own, so a run compiled now has no display-form latitude against GnuCOBOL and display_fold_applies() is false.
#: A row recorded before the option existed has no `numeric_display` and is read as the form WiseOwl then had, `digits`.
NUMERIC_DISPLAY = "decimal"
NUMERIC_DISPLAY_BEFORE_PB3505 = "digits"


def display_fold_applies(numeric_display: str, source: str) -> bool:
    """Whether a row compiled with the WiseOwl display form `numeric_display` may fold its outputs: only the digit image
    differs from GnuCOBOL's form by latitude, and only in a program display_fold_allowed() clears."""
    return numeric_display == NUMERIC_DISPLAY_BEFORE_PB3505 and display_fold_allowed(source)


def row_display_fold(rec: dict[str, Any], out: Path) -> bool:
    """The fold of a recorded row: nil for a row compiled with the decimal form; for a digit-image row (every row recorded
    before kb/Work PB3505) the recorded `display_fold`, or, for a row recorded before that field, the program's source under
    `out`; a source that is gone allows no fold."""
    if rec.get("numeric_display", NUMERIC_DISPLAY_BEFORE_PB3505) != NUMERIC_DISPLAY_BEFORE_PB3505:
        return False
    fold = rec.get("display_fold")
    if fold is None:
        src = out / rec.get("source", "")
        fold = display_fold_allowed(src.read_text(encoding="utf-8", errors="replace")) if rec.get("source") and src.is_file() else False
    return bool(fold)


def display_fold_allowed(source: str) -> bool:
    """Whether display_image() may fold this program's outputs. The text of an output cannot tell a scaled item's digit
    image (`PIC 9(5)V99`: `0001250` against `00012.50`, the form WiseOwl showed before kb/Work PB3505) from an EDITED item
    whose point one compiler dropped (`PIC ZZ9.99`: a wrong answer), nor an over-punched sign from a SIGN SEPARATE item
    printed without its sign.
    So the fold is allowed only when no picture in the program carries a point or an editing sign (`.`, `+`, `-`, `CR`, `DB`)
    and no item declares SIGN SEPARATE: in that program a point or a separate sign in GnuCOBOL's output can only be its
    display form (branch review R1-1). A program with such a picture keeps every difference as OUTPUT_DIFFERS for the
    triage to read."""
    if _SIGN_SEPARATE.search(source):
        return False
    return not any(_EDITING_SIGN_OR_POINT.search(m.group(1).rstrip(".")) for m in _PICTURE.finditer(source))


def display_image(text: str) -> str:
    """Both compilers' display forms of a numeric item folded to one image, sign then digits with no decimal point:
    GnuCOBOL's `+00012.50` and WiseOwl's `000125{` both become `+0001250`, `00012.50` and `0001250` both `0001250`. Two
    outputs equal under this fold differ only in the display form of a digit-image WiseOwl compile (PB803 settled the sign,
    PB3467 the point) WHEN display_fold_applies() holds for the row; the fold is used to CLASSIFY the row, never to judge it."""
    def punch(m: re.Match[str]) -> str:
        sign, digit = _PUNCH[m.group(2)]
        return sign + m.group(1) + digit
    return _OVERPUNCH.sub(punch, _GNU_SCALED.sub(lambda m: m.group(1) + m.group(2) + m.group(3), text))


def signature(verdict: str, clause: str, we: Observation) -> str:
    if verdict == "WE_REJECT" or verdict == "WE_CRASH":
        return verdict + ":" + (",".join(we.codes) or "no-code")
    return verdict + ":" + clause


# ── the run ───────────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclasses.dataclass
class Settings:
    model: str
    out: Path
    jobs: int
    count: int | None
    hours: float | None
    prefixes: tuple[str, ...]
    temperature: float
    cobol_exe: str
    keep: bool
    repairs: int = 3       # corrections offered after a refusal by both compilers (one lifted 5 of 10 in the bake-off; at
                           # 7 model-seconds a round, three cost less than one discarded program)
    variations: int = 3    # programs asked per clause before the seed list wraps
    seed: int = 20261020   # shuffles the seed clauses deterministically, so a short run samples every area (the first
                           # 40-program run walked the spec in order and never left §8.8); `in_order` keeps the spec's order
    in_order: bool = False


class Fuzz:
    def __init__(self, s: Settings) -> None:
        self.s = s
        self.out = s.out
        self.programs = self.out / "programs"
        self.programs.mkdir(parents=True, exist_ok=True)
        self.results_path = self.out / "results.jsonl"
        self.lock = threading.Lock()
        spec_text = SPEC.read_text(encoding="utf-8")
        conformance_text = CONFORMANCE.read_text(encoding="utf-8")
        declined = declined_seed_titles(spec_text, conformance_text)
        every = clauses(spec_text, s.prefixes)
        self.seeds = order_seeds([c for c in every if c[1] not in declined], s.seed, s.in_order)
        self.declined_seeds = len(every) - len(self.seeds)
        facts = PromptFacts.read(spec_text, conformance_text)
        self.system_prompts = {family: system_prompt(facts, family) for family in EXAMPLES}
        if not self.seeds:
            raise SystemExit(f"no clauses under {s.prefixes} in {SPEC}")
        work = self.out / "work"
        self.we = load_compiler("wiseowl", self.out, work / "wiseowl", {"cobol": s.cobol_exe}, s.keep)
        self.they = load_compiler("gnucobol", self.out, work / "gnucobol", {}, s.keep)
        self.deadline = time.monotonic() + s.hours * 3600 if s.hours else None
        self.done = 0

    def seed_for(self, i: int) -> tuple[str, str, str, int]:
        n = len(self.seeds)
        number, title, body = self.seeds[(i // self.s.variations) % n]
        return number, title, body, i % self.s.variations

    def one(self, i: int) -> dict[str, Any]:
        """One program, start to verdict. A harness failure inside it is a RUNNER_ERROR row with the traceback, never the
        end of the run: the first 40-program test died in its third program on a missing file, taking 37 unwritten rows
        with it, and an overnight run cannot afford that shape."""
        number, title, body, variation = self.seed_for(i)
        name = f"fz{i:05d}"
        rec: dict[str, Any] = {"id": name, "clause": number, "title": title, "model": self.s.model,
                               "at": datetime.now(timezone.utc).isoformat(timespec="seconds")}
        try:
            return self._one(i, name, number, title, body, variation, rec)
        except Exception as e:  # noqa: BLE001 — every failure of the harness itself is recorded, none is a verdict
            rec.update(verdict="RUNNER_ERROR", reason=f"harness: {e!r}", traceback=traceback.format_exc()[-2000:])
            return rec

    def _one(self, i: int, name: str, number: str, title: str, body: str, variation: int, rec: dict[str, Any]) -> dict[str, Any]:
        user = seed_text(number, title, body) + f"\n\nThis is program {variation + 1} for this clause: exercise an aspect the previous programs would not."
        rec["family"] = family_of(number)
        messages = [{"role": "system", "content": self.system_prompts[rec["family"]]}, {"role": "user", "content": user}]
        src = self.programs / f"{name}.cob"
        rec["source"] = str(src.relative_to(self.out)).replace("\\", "/")
        entry = {"id": name, "source": rec["source"], "files": [], "source-format": SOURCE_FORMAT}
        rec["model_seconds"] = 0.0
        rec["repairs"] = 0
        # Every round both compilers refused, with each one's first diagnostic (kb/Work PB3523 item 3): the record that
        # measures whether a correction fixed anything.
        rounds: list[dict[str, Any]] = []
        rec["repair_rounds"] = rounds
        they = Observation(None, None, "", [])
        we: Observation | None = None
        # The first reply, then up to `repairs` corrections. A program GnuCOBOL refuses is compiled by WiseOwl before anything
        # else happens: one WiseOwl compiles is a THEY_REJECT lead (GnuCOBOL 3.2 lacks the construct, or we under-reject; the
        # spec decides at triage) and is NOT sent back — a correction would replace the very construct under test, which is
        # how the 2026-10-20 bake-off lost two fifths of its refusals. One both compilers refuse is the model's slip and goes
        # back with both compilers' first diagnostics (WiseOwl's cite the standard). The verdict is the last program's;
        # every earlier one is kept as <name>.r<k>.cob for diagnosis.
        for attempt in range(self.s.repairs + 1):
            try:
                reply, secs, done, thinking = ollama_chat(self.s.model, messages, self.s.temperature)
            except (RuntimeError, urllib.error.URLError, OSError, TimeoutError) as e:
                rec.update(verdict="RUNNER_ERROR", reason=f"model: {e}")
                return rec
            rec["model_seconds"] = round(rec["model_seconds"] + secs, 1)
            (self.programs / f"{name}.reply{attempt}.txt").write_text(reply, encoding="utf-8", newline="\n")
            program = extract_program(reply)
            if program is None or done == "length":
                why = cut_off_reason(done, thinking, len(reply))
                if attempt == self.s.repairs:
                    rec.update(verdict="NO_PROGRAM", reason=why, truncated=done == "length")
                    return rec
                ask = ("Your reply was cut off before the program ended. Reply with a SHORTER complete program: at most 50 lines, "
                       "no indentation, a few data items, one ```cobol fenced block." if done == "length" else
                       "That reply had no COBOL program. Reply with exactly one complete program in one ```cobol fenced block.")
                messages += [{"role": "assistant", "content": reply}, {"role": "user", "content": ask}]
                continue
            if attempt:
                rec["repairs"] = attempt
                if src.exists():        # the previous attempt may have had no program to keep
                    shutil.copyfile(src, self.programs / f"{name}.r{attempt - 1}.cob")
            src.write_text(program, encoding="utf-8", newline="\n")
            they = observe_fresh(self.they, entry, name)
            if they.compiled is not False:
                we = None
                break
            we = observe_fresh(self.we, entry, name)
            if we.compiled is not False or we.crashed:
                # WiseOwl compiled it (THEY_REJECT), or CRASHED on it (WE_CRASH: a compiler crash is a lead whatever GnuCOBOL
                # said, and a correction would have replaced the program that crashed it: review R1-1).
                break
            rounds.append(refusal_round(attempt, they.diagnostics, we.diagnostics))
            if attempt == self.s.repairs:
                break           # the rounds are spent (BOTH_REJECT)
            if len(rounds) >= 2 and refusal_key(rounds[-1]) == refusal_key(rounds[-2]):
                # The correction drew the same first diagnostics from both compilers as the round before it: the model
                # cannot fix this program, and a further round would cost a generation and two compiles for nothing
                # (kb/Work PB3523 item 4). A program whose errors change keeps the whole budget.
                rec["stopped_early"] = True
                break
            messages += [{"role": "assistant", "content": reply},
                         {"role": "user", "content": repair_request(they.diagnostics, we.diagnostics, program)}]
        if we is None:
            we = observe_fresh(self.we, entry, name)
        display_fold = display_fold_applies(NUMERIC_DISPLAY, program)
        verdict = classify(we, they, display_fold)
        rec.update(verdict=verdict, signature=signature(verdict, number, we), display_fold=display_fold,
                   numeric_display=NUMERIC_DISPLAY,
                   we={"compiled": we.compiled, "codes": we.codes,
                       "first_error": first_error(we.diagnostics) if we.compiled is False or we.crashed else "",
                       "run_rc": we.run_rc, "crashed": we.crashed, "hung": we.hung, "output": we.output[:RECORDED_OUTPUT],
                       "runner_error": we.runner_error},
                   they={"compiled": they.compiled,
                         "first_error": first_error(they.diagnostics) if they.compiled is False else "",
                         "run_rc": they.run_rc, "hung": they.hung, "output": they.output[:RECORDED_OUTPUT],
                         "runner_error": they.runner_error})
        if not self.s.keep:
            for r in (self.we, self.they):
                shutil.rmtree(r.work / name, ignore_errors=True)
        return rec

    def record(self, rec: dict[str, Any]) -> None:
        with self.lock:
            with self.results_path.open("a", encoding="utf-8", newline="\n") as f:
                f.write(json.dumps(rec, ensure_ascii=False) + "\n")
            self.done += 1
            print(f"[{self.done}] {rec['id']} {rec['clause']:<10} {rec['verdict']:<14} "
                  f"{rec.get('signature', rec.get('reason', ''))[:70]}", flush=True)
            if self.done % REPORT_EVERY == 0:
                write_report(self.out)

    def run(self) -> None:
        (self.out / "run.json").write_text(json.dumps({**dataclasses.asdict(self.s), "out": str(self.out),
                                                       "seeds": len(self.seeds), "declined_seeds": self.declined_seeds,
                                                       "edition": EDITION,
                                                       "started": datetime.now(timezone.utc).isoformat(timespec="seconds")},
                                                      indent=1), encoding="utf-8", newline="\n")
        total = self.s.count if self.s.count is not None else 10 ** 9
        print(f"model {self.s.model} · {len(self.seeds)} seed clauses under {self.s.prefixes} · jobs {self.s.jobs} · "
              f"{'count ' + str(self.s.count) if self.s.count is not None else f'{self.s.hours} h'} · out {self.out}", flush=True)
        try:
            with ThreadPoolExecutor(max_workers=self.s.jobs) as pool:
                pending = set()
                i = 0
                while i < total and (self.deadline is None or time.monotonic() < self.deadline):
                    while len(pending) < self.s.jobs and i < total:
                        pending.add(pool.submit(self.one, i))
                        i += 1
                    done_now = next(as_completed(pending))
                    pending.discard(done_now)
                    self.record(done_now.result())
                for fut in as_completed(pending):
                    self.record(fut.result())
        finally:
            unload(self.s.model)
            write_report(self.out)


def cut_off_reason(done: str, thinking_chars: int, content_chars: int) -> str:
    """Why a reply yielded no program: cut off at the output cap, with the thinking that spent the cap named when the content
    is empty (a model that ignores `think: false`), or simply no program in it."""
    if done == "aborted":
        return "Ollama aborted the reply: the model fell into a repeating loop (token repeat limit reached)"
    if done != "length":
        return "no COBOL program in the reply"
    if thinking_chars and not content_chars:
        return (f"the model spent all {NUM_PREDICT} output tokens thinking ({thinking_chars} characters; think=false was not "
                "honoured) and wrote no content")
    return f"the reply was cut off at {NUM_PREDICT} tokens"


def first_error(diag: str) -> str:
    for line in diag.splitlines():
        if "error" in line.lower():
            return line.strip()[:300]
    return diag.strip().splitlines()[0][:300] if diag.strip() else ""


def error_lines(diag: str, limit: int) -> list[str]:
    """The first `limit` diagnostic lines that report an error: a compiler cascades after the first slip (the bake-off's
    GnuCOBOL replies ran to thirty lines of "unexpected Identifier"), and a model given the cascade fixes the wrong line."""
    return [line.strip()[:300] for line in diag.splitlines() if "error" in line.lower()][:limit]


REPAIR_GNU_LINES = 6
REPAIR_WISEOWL_LINES = 4


#: The source line a diagnostic names: GnuCOBOL's "fz00609.cob:18: error: …", WiseOwl's "fz00609.cob(24,4): error COBOL…".
DIAG_LINE = re.compile(r"^\S*?(?::(\d+):|\((\d+),\d+\):)")


def diagnostic_line(diag_line: str) -> int | None:
    """The 1-based source line number a diagnostic line names, or None when it names none."""
    m = DIAG_LINE.match(diag_line.strip())
    return int(m.group(1) or m.group(2)) if m else None


def with_source(diag_lines: list[str], program: str) -> str:
    """The diagnostic lines, each followed by the source line it names, quoted (kb/Work PB3523 item 2): a model given only
    "line 18: 'WS-TABLE' not indexed" rewrites whatever it guesses line 18 was; given the line itself it changes that line."""
    source = program.splitlines()
    out = []
    for d in diag_lines:
        out.append(d)
        n = diagnostic_line(d)
        if n is not None and 1 <= n <= len(source):
            out.append(f"    > line {n}: {source[n - 1].strip()[:200]}")
    return "\n".join(out)


def repair_request(they_diag: str, we_diag: str, program: str) -> str:
    """The correction asked after BOTH compilers refuse a program: GnuCOBOL's first errors (the judge's), then WiseOwl's
    (they cite the standard's clause, which tells the model what the rule is rather than where it tripped), each beside the
    refused source line, and the ask for the smallest change that fixes them."""
    gnu = with_source(error_lines(they_diag, REPAIR_GNU_LINES), program) or they_diag.strip()[:600]
    ours = with_source(error_lines(we_diag, REPAIR_WISEOWL_LINES), program)
    text = "GnuCOBOL 3.2 (-std=cobol2014, free format) refused that program:\n\n" + gnu
    if ours:
        text += "\n\nA second standard-conforming compiler refused it too, citing ISO/IEC 1989:\n\n" + ours
    return text + ("\n\nFix every error with the smallest change: correct the quoted lines and keep the rest of the program "
                   "as it is. Use only standard COBOL 2014 (do not drop the clause under test; replace a construct the "
                   "standard lacks with one it has). Reply with the whole corrected program in one ```cobol fenced block.")


#: GnuCOBOL's diagnostics carry no code, so a refusal is identified by its message FAMILY: the message with the file and
#: line, every quoted name and every number taken out ("'WS-TABLE' not indexed" and "'WS-T2' not indexed" are one family).
GNU_LOCATION = re.compile(r"^\S*?:\d+:\s*")
GNU_QUOTED = re.compile(r"'[^']*'")


def gnucobol_family(message: str) -> str:
    """The message family of one GnuCOBOL diagnostic line (GNU_LOCATION, GNU_QUOTED)."""
    return re.sub(r"\d+", "N", GNU_QUOTED.sub("'_'", GNU_LOCATION.sub("", message.strip())))


def refusal_round(attempt: int, they_diag: str, we_diag: str) -> dict[str, Any]:
    """One round both compilers refused, as the row records it (`repair_rounds`, kb/Work PB3523 item 3): WiseOwl's first
    error's code and message, and GnuCOBOL's first error."""
    ours = first_error(we_diag)
    code = DIAG_CODE.search(ours)
    return {"round": attempt, "code": code.group(1) if code else "", "message": ours, "gnucobol": first_error(they_diag)}


def refusal_key(rnd: dict[str, Any]) -> tuple[str, str]:
    """What makes two refusal rounds THE SAME error (kb/Work PB3523 item 4): WiseOwl's first code (its message when it
    printed none) and GnuCOBOL's first message family. Both must repeat: a WiseOwl code as broad as COBOL0001 ("unexpected
    token") repeats across different slips, and GnuCOBOL's family tells them apart."""
    return rnd["code"] or rnd["message"], gnucobol_family(rnd["gnucobol"])


# ── the report ────────────────────────────────────────────────────────────────────────────────────────────────────────

def recorded_observation(d: dict[str, Any]) -> Observation:
    """One compiler's recorded observation (the `we` / `they` dict of a results.jsonl row) as an Observation again."""
    hung = bool(d.get("hung"))
    run_rc = d.get("run_rc")
    return Observation(d.get("compiled"), None, "", list(d.get("codes") or []),
                       ran=True if run_rc is not None else (False if hung and d.get("compiled") else None),
                       run_rc=run_rc, crashed=bool(d.get("crashed")), hung=hung, output=d.get("output") or "",
                       runner_error=d.get("runner_error") or "")


def rederived(rec: dict[str, Any], out: Path) -> dict[str, Any]:
    """The row's verdict derived again by today's classify() from what the run recorded, so `--report` renders a run under the
    classes the harness has NOW (kb/Work PB3490: the first night's rows were re-read under BOTH_HANG and OUTPUT_LATITUDE
    without a second night). A row that recorded no observations (NO_PROGRAM, a model RUNNER_ERROR) or an output the record
    cut keeps its verdict; a recorded compiler crash keeps its class whichever way the record's fields read. The display
    fold is row_display_fold()'s: a row compiled with the decimal form has none (kb/Work PB3505), a digit-image row follows
    the recorded `display_fold` or the program's source under `out`."""
    we, they = rec.get("we"), rec.get("they")
    if not we or not they or rec["verdict"] in ("WE_CRASH", "RUNNER_ERROR") or \
            len(we.get("output") or "") >= RECORDED_OUTPUT or len(they.get("output") or "") >= RECORDED_OUTPUT:
        return rec
    verdict = classify(recorded_observation(we), recorded_observation(they), row_display_fold(rec, out))
    if verdict == rec["verdict"]:
        return rec
    return {**rec, "verdict": verdict, "signature": signature(verdict, rec["clause"], recorded_observation(we)),
            "recorded_verdict": rec["verdict"]}


def read_results(out: Path) -> list[dict[str, Any]]:
    path = out / "results.jsonl"
    if not path.exists():
        return []
    rows = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.strip():
            rows.append(rederived(json.loads(line), out))
    return rows


def render_report(rows: list[dict[str, Any]], out: Path) -> str:
    counts: dict[str, int] = {}
    for r in rows:
        counts[r["verdict"]] = counts.get(r["verdict"], 0) + 1
    n = len(rows)
    judged = n - counts.get("RUNNER_ERROR", 0)
    failed = sum(counts.get(c, 0) for c in MODEL_FAILURE_CLASSES)
    model_secs = [r["model_seconds"] for r in rows if "model_seconds" in r]
    repaired = sum(1 for r in rows if r.get("repairs") and r["verdict"] not in MODEL_FAILURE_CLASSES)
    first = first_attempt_usable(rows)
    early = sum(1 for r in rows if r.get("stopped_early"))
    lines = [f"# Differential fuzz — triage report", "",
             f"Programs: {n} · judged: {judged} · usable: {judged - failed} ({(judged - failed) / judged:.0%} of judged; "
             f"{repaired} of them after a repair round) · usable on the first attempt: {first} ({first / judged:.0%} of judged) · "
             f"the model's failures: {failed} ({early} ended early on a repeated diagnostic) · GnuCOBOL alone refused "
             f"(THEY_REJECT leads): {counts.get('THEY_REJECT', 0)}" if judged else f"Programs: {n}",
             f"Model seconds per program (median): {sorted(model_secs)[len(model_secs) // 2]:.0f}" if model_secs else "",
             "", "| Class | Count |", "|---|---|"]
    lines += [f"| {k} | {v} |" for k, v in sorted(counts.items(), key=lambda kv: -kv[1])]
    lines += ["", "⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the",
              "compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A",
              "confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a",
              "suite case with its expected output derived from the standard, never copied from either compiler.", ""]
    groups: dict[str, list[dict[str, Any]]] = {}
    for r in rows:
        if r["verdict"] in LEAD_CLASSES:
            groups.setdefault(r.get("signature", r["verdict"]), []).append(r)
    lines.append(f"## Survivors: {sum(len(g) for g in groups.values())} programs in {len(groups)} signatures")
    for sig, g in sorted(groups.items(), key=lambda kv: (-len(kv[1]), kv[0])):
        r = g[0]
        lines += ["", f"### `{sig}` × {len(g)} — first: {r['id']} (clause {r['clause']} {r['title']})",
                  f"- programs: {', '.join(x['id'] for x in g[:12])}{' …' if len(g) > 12 else ''}",
                  f"- source: `{r.get('source', '')}`"]
        we, they = r.get("we", {}), r.get("they", {})
        if we.get("first_error"):
            lines.append(f"- WiseOwl: `{we['first_error']}`")
        if r["verdict"] == "THEY_REJECT" and they.get("first_error"):
            lines.append(f"- GnuCOBOL: `{they['first_error']}`")
        if we.get("runner_error") or they.get("runner_error"):
            lines.append(f"- runner: {we.get('runner_error') or they.get('runner_error')}")
        if r["verdict"] in ("OUTPUT_DIFFERS", "THEY_FAIL_RUN", "WE_FAIL_RUN", "EXIT_DIFFERS"):
            lines.append("- outputs (WiseOwl | GnuCOBOL), first differing lines:")
            fold = row_display_fold(r, out)
            a, b = we.get("output", "").splitlines(), they.get("output", "").splitlines()
            shown = latitude_only = 0
            for k in range(max(len(a), len(b))):
                x, y = (a[k] if k < len(a) else "<end>"), (b[k] if k < len(b) else "<end>")
                if x == y:
                    continue
                if fold and display_image(x) == display_image(y):
                    latitude_only += 1       # the digit-image display form (PB803 / PB3467) on a line of a row that differs elsewhere
                    continue
                if shown < 5:
                    lines.append(f"  - line {k + 1}: `{x[:120]}` | `{y[:120]}`")
                shown += 1
            if latitude_only:
                lines.append(f"  - {latitude_only} other line(s) differ only in the digit-image display form of a numeric item (PB803 / PB3467)")
            if we.get("run_rc") not in (None, 0):
                lines.append(f"  - WiseOwl run exit {we['run_rc']}")
            if they.get("run_rc") not in (None, 0):
                lines.append(f"  - GnuCOBOL run exit {they['run_rc']}: `{they.get('first_error', '')}`")
    latitude = [r for r in rows if r["verdict"] in LATITUDE_CLASSES]
    if latitude:
        by_clause: dict[str, list[dict[str, Any]]] = {}
        for r in latitude:
            by_clause.setdefault(r["clause"], []).append(r)
        lines += ["", f"## Display-form latitude: {len(latitude)} programs (not survivors)",
                  "The outputs differ only in how a numeric item is displayed: GnuCOBOL's leading separate sign and inserted",
                  "decimal point against the digit image with an over-punched sign that WiseOwl displayed before kb/Work PB3505",
                  "(the sign is the settled decision PB803, the point PB3467's). These rows were recorded by a digit-image compile",
                  "and need no triage: a run made now compiles WiseOwl's default decimal form, where this difference is a defect."]
        lines += [f"- clause {c} ({g[0]['title']}): {len(g)} — {', '.join(x['id'] for x in g[:6])}{' …' if len(g) > 6 else ''}"
                  for c, g in sorted(by_clause.items(), key=lambda kv: (-len(kv[1]), kv[0]))]
    runner = [r for r in rows if r["verdict"] == "RUNNER_ERROR"]
    if runner:
        lines += ["", f"## Runner errors: {len(runner)} (not compiler verdicts)"]
        lines += [f"- {r['id']}: {r.get('reason') or (r.get('we') or {}).get('runner_error') or (r.get('they') or {}).get('runner_error')}"
                  for r in runner[:20]]
    return "\n".join(line for line in lines if line is not None) + "\n"


def write_report(out: Path) -> Path:
    path = out / "report.md"
    path.write_text(render_report(read_results(out), out), encoding="utf-8", newline="\n")
    return path


# ── the bake-off ──────────────────────────────────────────────────────────────────────────────────────────────────────

def bake_off(models: list[str], base: Settings) -> None:
    """Each model writes `count` programs from the same seeds; the score is the share of its programs that are USABLE (at
    least one compiler accepted: both compiled, or GnuCOBOL alone refused, which is a lead) and the seconds per program.
    The winner generates the overnight run."""
    table = []
    for m in models:
        s = dataclasses.replace(base, model=m, out=base.out / re.sub(r"[^A-Za-z0-9_.-]", "_", m))
        Fuzz(s).run()
        table.append((m, *bake_off_score(read_results(s.out))))
    lines = ["# Bake-off", "", "| Model | Programs | Usable | Usable on the first attempt | Model failures (both refuse, no program, or both hang) | GnuCOBOL alone refuses | Median model s | Lead rows |",
             "|---|---|---|---|---|---|---|---|"]
    for m, n, usable, judged, both, theirs, med, leads, first in table:
        lines.append(f"| {m} | {n} | {usable}/{judged} ({usable / judged:.0%}) | {first}/{judged} ({first / judged:.0%}) | {both} | {theirs} | {med:.0f} | {leads} |"
                     if judged else f"| {m} | {n} | 0/0 | 0/0 | 0 | 0 | - | {leads} |")
    text = "\n".join(lines) + "\n"
    (base.out / "bake-off.md").write_text(text, encoding="utf-8", newline="\n")
    print(text)


def first_attempt_usable(rows: list[dict[str, Any]]) -> int:
    """The judged programs usable without a correction round (kb/Work PB3523 item 5: the prompt's own validity, before the
    repair loop lifts it)."""
    return sum(1 for r in rows if r["verdict"] != "RUNNER_ERROR" and r["verdict"] not in MODEL_FAILURE_CLASSES and not r.get("repairs"))


def bake_off_score(rows: list[dict[str, Any]]) -> tuple[int, int, int, int, int, float, int, int]:
    """(programs, usable, judged, model failures (MODEL_FAILURE_CLASSES: BOTH_REJECT, NO_PROGRAM, BOTH_HANG), GnuCOBOL-alone-
    refuses, median model seconds, lead rows, usable on the first attempt) of one model's run."""
    judged = [r for r in rows if r["verdict"] != "RUNNER_ERROR"]
    usable = [r for r in judged if r["verdict"] not in MODEL_FAILURE_CLASSES]
    both = sum(1 for r in judged if r["verdict"] in MODEL_FAILURE_CLASSES)
    theirs = sum(1 for r in judged if r["verdict"] == "THEY_REJECT")
    secs = sorted(r["model_seconds"] for r in rows if "model_seconds" in r)
    leads = sum(1 for r in rows if r["verdict"] in LEAD_CLASSES)
    return (len(rows), len(usable), len(judged), both, theirs, secs[len(secs) // 2] if secs else float("nan"), leads,
            first_attempt_usable(rows))


# ── self-test ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    rc = 0

    def check(name: str, ok: bool, got: object = None) -> None:
        nonlocal rc
        print(("  ok: " if ok else "  SELF-TEST FAILED: ") + name + ("" if ok else f" — got {got!r}"))
        rc |= 0 if ok else 1

    print("=== differential_fuzz.py --self-test ===")
    md = ("## 14 Procedure division\n<a id=\"x\"></a>\n#### 14.9.25 MOVE statement\n" + "body " * 60 +
          "\n##### 14.9.25.1 General\nshort\n#### 15.3 ABS function\n" + "rules " * 60 + "\n### 13.18.1 ALIGNED clause\n" + "x " * 150)
    got = clauses(md, ("14.9", "15"))
    check("seeds: clauses under the prefixes with a body, bare and short headings skipped",
          [c[0] for c in got] == ["14.9.25", "15.3"] and got[0][1] == "MOVE statement", got)
    check("seeds: a prefix matches whole components only (13.1 is not under 13.18 and 13.18 is not under 13.1)",
          [c[0] for c in clauses(md, ("13.1",))] == [] and [c[0] for c in clauses(md, ("13.18",))] == ["13.18.1"])
    folded = ("#### 14.9.25 MOVE statement\nintro\n##### 14.9.25.1 General\n" + "g " * 40 + "\n##### 14.9.25.2 General formats\n"
              + "f " * 40 + "\n###### 14.9.25.2.1 Format 1 (simple)\nfmt\n##### 14.9.25.4 General rules\n" + "r " * 40
              + "\n#### 14.9.26 MULTIPLY statement\n" + "m " * 120)
    got = clauses(folded, ("14.9",))
    check("seeds: the structural sub-clauses (General, General formats, Format n, General rules) fold into the statement's seed",
          [c[0] for c in got] == ["14.9.25", "14.9.26"] and "General rules" in got[0][2] and "fmt" in got[0][2]
          and got[0][1] == "MOVE statement", [(c[0], c[1]) for c in got])
    fn_md = ("### 15.66 NATIONAL-OF function\nintro\n#### 15.66.1 General\n" + "g " * 40 + "\n#### 15.66.2 General format\n" + "f " * 40
             + "\n#### 15.66.3 Argument rules\n" + "a " * 40 + "\n#### 15.66.4 Returned value rules\n" + "r " * 40
             + "\n### 15.67 NUMVAL function\n#### 15.67.3 Arguments rules\n" + "a " * 60 + "\n#### 15.67.4 Returned Value rule\n" + "r " * 60)
    got = clauses(fn_md, ("15",))
    check("seeds: a function's Argument rules and Returned value rules (every spelling the spec uses) fold into the function's seed, never a seed of their own",
          [c[0] for c in got] == ["15.66", "15.67"] and "Argument rules" in got[0][2] and "Returned value rules" in got[0][2]
          and got[1][1] == "NUMVAL function", [(c[0], c[1]) for c in got])
    real_seeds = clauses(SPEC.read_text(encoding="utf-8"), DEFAULT_CLAUSES)
    # The detector is INDEPENDENT of STRUCTURAL (review R1-2: a test that filters with the pattern under test cannot fail):
    # a structural title starts with one of these words, whatever follows and however it is spelled or hyphenated.
    structural_word = re.compile(r"^(?:general|syntax|arguments?|returned|format \d|summary)", re.IGNORECASE)
    stray = sorted({f"{n} {title}" for n, title, _b in real_seeds if n.count(".") >= 2 and structural_word.match(title.rsplit(" > ", 1)[-1])})
    check("seeds: no nested seed of the real spec is a bare structural sub-clause (a drift test with its own detector: a new spelling such as 'Argument-rules' fails here; 15.3 Arguments is a clause of its own)",
          stray == [] and any(t == "Arguments" for _n, t, _b in real_seeds), stray)
    check("seeds: the independent detector would catch a spelling the folding misses",
          clauses("### 15.66 NATIONAL-OF function\n" + "b " * 120 + "\n#### 15.66.3 Argument-rules\n" + "a " * 120, ("15",))[1:] != []
          and bool(structural_word.match("Argument-rules")))
    by_number = {n: t for n, t, _b in real_seeds}
    check("seeds: a nested seed carries its ancestors' titles below the second level, a statement or function seed carries none",
          by_number.get("13.18.40.5") == "PICTURE clause > Editing rules" and by_number.get("14.9.25") == "MOVE statement"
          and by_number.get("15.66") == "NATIONAL-OF function" and by_number.get("15.3.1.6", "").endswith("> Week date formats"),
          {k: by_number.get(k) for k in ("13.18.40.5", "14.9.25", "15.66", "15.3.1.6")})
    bodies = {n: (t, b) for n, t, b in clauses(SPEC.read_text(encoding="utf-8"), ("14.9", "13.18", "8.4", "15"))}
    search = seed_text("14.9.37", *bodies["14.9.37"])
    check("seed: the prompt for a seed carries its clause's general format and syntax rules, the diagram's markup stripped (14.9.37 SEARCH: both formats, syntax rule 2 on INDEXED; kb/Work PB3523 item 1)",
          "SEARCH identifier-1" in search and "SEARCH ALL identifier-1" in search and "Syntax rules" in search
          and "shall contain an OCCURS clause with an INDEXED phrase" in search and "<u>" not in search and "<pre" not in search
          and "1)" in search and "1\\)" not in search, search[:400])
    sizes = {n: len(seed_text(n, t, b)) for n, (t, b) in bodies.items()}
    overhead = max(len(f"Clause {n} {t}\n\n") for n, (t, _b) in bodies.items()) + len(CUT_MARK) + 200
    longest = max(sizes, key=lambda n: sizes[n])
    check("seed: no clause's prompt text exceeds the budget (the longest whole clause, 14.9.39 SET, is over 40,000 characters; kb/Work PB3523 item 6)",
          sizes[longest] <= CLAUSE_BUDGET + overhead and max(len(b) for _t, b in bodies.values()) > 40000, (longest, sizes[longest]))
    set_text = seed_text("14.9.39", *bodies["14.9.39"])
    occurs = seed_text("13.18.38", *bodies["13.18.38"])
    check("seed: a clause over the budget keeps its formats and its syntax rules ahead of its general rules, says where it cut, and names what it omitted (14.9.39 SET's fifteen formats take at most two thirds; 13.18.38 OCCURS keeps syntax rule 1)",
          set_text.count("Format ") >= 2 and "Syntax rules" in set_text and CUT_MARK in set_text and "omitted to fit the prompt" in set_text
          and "General formats" in occurs and "The OCCURS clause shall not be specified in a data description entry that" in occurs
          and occurs.index("Syntax rules") < len(occurs) and ("General rules" not in occurs or occurs.index("General rules") > occurs.index("Syntax rules")),
          (set_text[-300:], occurs[-300:]))
    short = "#### 14.9.99 FOO statement\n##### 14.9.99.1 General\n" + "intro " * 30 + "\n##### 14.9.99.2 General format\nFOO <u>BAR</u> x\n##### 14.9.99.3 Syntax rules\n1\\) rule\n##### 14.9.99.4 General rules\n" + "g " * 40
    foo = clauses(short, ("14.9",))[0]
    whole, cut = seed_text(*foo), seed_text(*foo, budget=100)
    order = [whole.find(s) for s in ("General", "intro", "General format", "FOO BAR x", "Syntax rules", "1) rule", "General rules", "g g")]
    check("seed: a clause within the budget is whole, cleaned and in the spec's order; one over it keeps its format and syntax rules and names the parts it omitted",
          whole.startswith("Clause 14.9.99 FOO statement\n\nGeneral") and -1 not in order and order == sorted(order)
          and "<u>" not in whole and "1\\)" not in whole
          and "FOO BAR x" in cut and "1) rule" in cut and "intro" not in cut and "g g" not in cut
          and cut.endswith("[General, General rules: omitted to fit the prompt]"), (whole, cut))
    budget_facts = PromptFacts.read(SPEC.read_text(encoding="utf-8"), CONFORMANCE.read_text(encoding="utf-8"))
    check("budget: the longest system prompt and the longest clause, at a conservative 2.5 characters a token, leave the reply's NUM_PREDICT and three corrections of 1,500 tokens inside NUM_CTX",
          (max(len(system_prompt(budget_facts, f)) for f in EXAMPLES) + sizes[longest]) / 2.5 + NUM_PREDICT + 3 * 1500 <= NUM_CTX,
          (max(len(system_prompt(budget_facts, f)) for f in EXAMPLES), sizes[longest]))
    check("cut off: the reason names the thinking that spent the output cap when the content is empty, the cap alone otherwise",
          "spent all" in cut_off_reason("length", 28259, 0) and "think=false" in cut_off_reason("length", 28259, 0)
          and cut_off_reason("length", 0, 500) == f"the reply was cut off at {NUM_PREDICT} tokens"
          and cut_off_reason("length", 900, 500) == f"the reply was cut off at {NUM_PREDICT} tokens"
          and cut_off_reason("stop", 0, 0) == "no COBOL program in the reply")
    many = [(f"14.9.{k}", f"S{k}", "b") for k in range(1, 41)]
    check("seeds: the walk is shuffled deterministically by the seed (same seed, same order; a different seed, a different one) and in_order keeps the spec's",
          order_seeds(many, 1, False) == order_seeds(many, 1, False) and order_seeds(many, 1, False) != many
          and order_seeds(many, 2, False) != order_seeds(many, 1, False) and order_seeds(many, 1, True) == many
          and sorted(order_seeds(many, 1, False)) == sorted(many))
    prog = extract_program("Here:\n```cobol\nIDENTIFICATION DIVISION.\nPROGRAM-ID. FUZZ.\nPROCEDURE DIVISION.\n STOP RUN.\n```\n")
    check("extract: the fenced program, newline-terminated", prog is not None and prog.startswith("IDENTIFICATION") and prog.endswith("STOP RUN.\n"), prog)
    odd = extract_program("不经.fromJson\n```cob注释\nIDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.\n```\n\n```text\nnotes\n```\n")
    check("extract: any fence info string (a model wrote one with CJK characters after cob); the block with the PROCEDURE DIVISION is taken, prose before it dropped",
          odd == "IDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.\n", odd)
    cut = extract_program("```cobol\nIDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nDISPLAY 1\nADD 1 TO")
    check("extract: a truncated reply's unterminated fence yields its program (the fence line itself is never part of it)",
          cut is not None and cut.startswith("IDENTIFICATION") and "```" not in cut, cut)
    check("extract: a reply without a PROCEDURE DIVISION is no program", extract_program("I cannot do that.") is None)
    check("extract: an unfenced program is accepted", extract_program("IDENTIFICATION DIVISION.\nPROCEDURE DIVISION.\nSTOP RUN.") is not None)

    ok_run = Observation(True, 0, "", [], ran=True, run_rc=0, output="A\n1")
    gnu_no = Observation(False, 1, "x.cob:3: error: syntax error, unexpected Identifier", [])
    check("classify: a refusal by both compilers is the model's failure (BOTH_REJECT)",
          classify(Observation(False, 1, "e: error COBOL0001", ["COBOL0001"]), gnu_no) == "BOTH_REJECT")
    check("classify: a refusal by GnuCOBOL alone is a lead (THEY_REJECT), whatever our program then did at run time",
          classify(ok_run, gnu_no) == "THEY_REJECT" and classify(dataclasses.replace(ok_run, run_rc=1), gnu_no) == "THEY_REJECT"
          and "THEY_REJECT" in LEAD_CLASSES and "BOTH_REJECT" in MODEL_FAILURE_CLASSES and "NO_PROGRAM" in MODEL_FAILURE_CLASSES
          and not set(LEAD_CLASSES) & set(MODEL_FAILURE_CLASSES))
    check("classify: our compiler crash on a program GnuCOBOL refused outranks the refusal; our hang on one is our hang",
          classify(Observation(False, -1, "", [], crashed=True), gnu_no) == "WE_CRASH"
          and classify(Observation(None, None, "", [], hung=True), gnu_no) == "WE_HANG")
    check("classify: an unobserved WiseOwl beside GnuCOBOL's refusal is never a verdict",
          classify(Observation(None, None, "", [], runner_error="did not start"), gnu_no) == "RUNNER_ERROR")
    check("signature: a THEY_REJECT groups by clause", signature("THEY_REJECT", "15.42", ok_run) == "THEY_REJECT:15.42")
    cascade = "\n".join([f"x.cob:{k}: error: syntax error, unexpected Identifier" for k in range(3, 20)] + ["x.cob:1: note: hint"])
    refused = "\n".join(f"LINE-{k}" for k in range(1, 30))
    ask = repair_request(cascade, "y.cob(4,1): error COBOLNET2993: the unary operator — ISO §8.7.1\ny.cob(5,1): error COBOLNET1625: VALUE", refused)
    check("repair: the correction quotes GnuCOBOL's first errors only (the cascade is cut) and WiseOwl's cited errors, and keeps the clause under test",
          ask.count("unexpected Identifier") == REPAIR_GNU_LINES and "note: hint" not in ask and "COBOLNET2993" in ask
          and "ISO/IEC 1989" in ask and "do not drop the clause under test" in ask, ask)
    check("repair: without a WiseOwl diagnostic the correction names GnuCOBOL alone",
          "second standard-conforming compiler" not in repair_request("x.cob:3: error: bad", "", refused))
    check("repair: each diagnostic is followed by the source line it names, GnuCOBOL's file:line and WiseOwl's file(line,column) alike; a line past the program or no line quotes nothing (kb/Work PB3523 item 2)",
          "x.cob:3: error: syntax error, unexpected Identifier\n    > line 3: LINE-3" in ask and "    > line 8: LINE-8" in ask
          and "    > line 9: LINE-9" not in ask and "COBOLNET2993: the unary operator — ISO §8.7.1\n    > line 4: LINE-4" in ask
          and "COBOLNET1625: VALUE\n    > line 5: LINE-5" in ask and "smallest change" in ask
          and with_source(["x.cob:99: error: past the end", "error: no line"], refused) == "x.cob:99: error: past the end\nerror: no line"
          and diagnostic_line("fz00609.cob(24,4): error COBOLNET2072: x") == 24 and diagnostic_line("fz00609.cob:18: error: x") == 18
          and diagnostic_line("cobc: error: x") is None, ask)
    for family, example in EXAMPLES.items():
        ex_lines = example.splitlines()
        check(f"prompt: the {family} example obeys the rules it illustrates (free format from column 1, under 80 lines, PROGRAM-ID FUZZ, DISPLAY, STOP RUN, every IF closed by END-IF, the 2014 edition's functions only) and extracts as a program",
              all(line and line[0] != " " for line in ex_lines) and len(ex_lines) < 80 and "PROGRAM-ID. FUZZ." in ex_lines
              and any(line.startswith("DISPLAY") for line in ex_lines) and ex_lines[-1] == "STOP RUN."
              and sum(line.startswith("IF ") for line in ex_lines) == ex_lines.count("END-IF")
              and set(re.findall(r"FUNCTION ([A-Z][A-Z0-9-]*)", example)) <= set(spec_functions(SPEC.read_text(encoding="utf-8")))
              and extract_program("```cobol\n" + example + "```") == example, ex_lines)
    check("prompt: one example per family, and every family FAMILIES names has one",
          set(EXAMPLES) == {f for _p, f in FAMILIES} | {DEFAULT_FAMILY} == {"statement", "data", "function", "expression"})
    check("families: a clause's family is its first matching prefix, whole components only (8.40 is not under 8.4), a statement's otherwise",
          [family_of(n) for n in ("13.18.38", "8.5.1.6", "14.9.37", "14.6.9", "15.66", "8.8.4.2.5", "8.4.3", "8.40", "12.4.5")]
          == ["data", "data", "statement", "statement", "function", "expression", "expression", "statement", "statement"])
    fns = spec_functions("### 15.6 Summary of functions\n### 15.7 ABS function\n#### 15.7.1 General\n### 15.84 TRIM function\n### 15.85 Something else\n")
    check("prompt: the standard's functions are read from the 15.n function headings (15.6's summary and a sub-clause are not functions)",
          fns == ["ABS", "TRIM"], fns)
    spec_text = SPEC.read_text(encoding="utf-8")
    added_fns, added_sts = added_in_2023(spec_text)
    check("edition: Annex E.3.3 names the seven functions and the one statement 2023 added, and not the ones it only enhanced (TRIM, EXCEPTION-FILE)",
          added_fns == {"BASECONVERT", "CONCAT", "CONVERT", "FIND-STRING", "MODULE-NAME", "SMALLEST-ALGEBRAIC", "SUBSTITUTE"}
          and added_sts == {"DELETE FILE"}, (sorted(added_fns), sorted(added_sts)))
    check("edition: the Annex reader takes a FUNCTION item whose text says the function was added and any sentence saying a statement was added; an added ARGUMENT or an enhancement is not an addition; E.3.4 is outside",
          added_in_2023("#### E.3.3 Not affecting\n1\\) **FUNCTION FOO.** This function has been added.\n2\\) **FUNCTION BAR.** The BAR function has been enhanced.\n"
                        "5\\) **FUNCTION BAZ.** An optional argument has been added to this function.\n6\\) **FUNCTION QUUX.** The QUUX intrinsic function has been added to provide x.\n"
                        "3\\) **Setting of statuses.** The ZAP FILE statement has been added, so these statuses may be set.\n#### E.3.4 Other\n4\\) **FUNCTION QUX.** This function has been added.\n")
          == ({"FOO", "QUUX"}, {"ZAP FILE"}), added_in_2023("#### E.3.3 x\n5\\) **FUNCTION BAZ.** An optional argument has been added to this function.\n"))
    real = spec_functions(spec_text)
    check("edition: the prompt's function list is the 2014 edition's (no CONCAT, SUBSTITUTE, FIND-STRING; REVERSE, TRIM and EXCEPTION-FILE stay), and no seed is a 2023-only function's clause or the DELETE FILE statement",
          not {"CONCAT", "SUBSTITUTE", "FIND-STRING"} & set(real) and {"REVERSE", "TRIM", "EXCEPTION-FILE"} <= set(real)
          and not any(t in ("CONCAT function", "SUBSTITUTE function", "DELETE FILE statement") for _n, t, _b in clauses(spec_text, DEFAULT_CLAUSES)),
          sorted({"CONCAT", "SUBSTITUTE", "FIND-STRING"} & set(real)))
    check("prompt: the spec defines REVERSE, HIGHEST-ALGEBRAIC and NUMVAL-F and none of the names the bake-off's models invented (TRANSLATE, NATIONAL, SUBSTRING, YEAR)",
          {"REVERSE", "HIGHEST-ALGEBRAIC", "NUMVAL-F"} <= set(real) and not {"TRANSLATE", "NATIONAL", "SUBSTRING", "YEAR"} & set(real)
          and len(real) > 80, (len(real), sorted(set(real) & {"TRANSLATE", "NATIONAL", "SUBSTRING", "YEAR"})))
    added_words = added_reserved_words(spec_text)
    reserved = reserved_words(spec_text)
    conformance_text = CONFORMANCE.read_text(encoding="utf-8")
    facts = PromptFacts.read(spec_text, conformance_text)
    prompts = {family: system_prompt(facts, family) for family in EXAMPLES}
    sp = prompts["statement"]
    check("prompt: the facts are the spec's and the conformance claim's (the functions, the 2023 additions, the declined modules), and every family's prompt names the declined modules",
          facts.functions == real and facts.added == (added_fns, added_sts) and facts.added_words == added_words
          and "ACCEPT/DISPLAY screen handling" in facts.declined and "VALIDATE" in facts.declined
          and all("never use them: " + "; ".join(facts.declined) in p for p in prompts.values()), facts.declined)
    declined_titles = declined_seed_titles(spec_text, conformance_text)
    every_title = {t for _n, t, _b in clauses(spec_text, ("13.18", "14.9"))}
    check("seeds: a WHOLE clause or statement of an optional module CONFORMANCE.md §5 records as Not claimed is not a seed (the screen clauses, VALIDATE's, COMMIT and ROLLBACK, FORMAT, SELECT WHEN), matched by title; a module's single format of a claimed statement (ACCEPT format 3, SET format 6, COLUMN format 2) and a claimed module's clause (Report Writer's PAGE) stay",
          {"BLINK clause", "UNDERLINE clause", "REQUIRED clause", "BLANK clause", "VALIDATE statement", "VALIDATE-STATUS clause",
           "DEFAULT clause", "COMMIT statement", "ROLLBACK statement", "FORMAT clause", "SELECT WHEN clause"} <= declined_titles
          and not {"ACCEPT statement", "SET statement", "COLUMN clause", "PAGE clause", "REWRITE statement", "WRITE statement",
                   "VALUE clause", "OCCURS clause", "DISPLAY statement"} & declined_titles
          and declined_titles <= every_title, sorted(declined_titles - every_title))
    fixture_spec = ("#### A.4.2 Screens\n1\\) ACCEPT statement, format 3: screen (14.9.1)\n2\\) BLINK clause (13.18.9)\n"
                    "#### A.4.3 Commit\n4\\) COMMIT statement 14.9.7\n#### A.4.4 Tables\n1\\) OCCURS clause (13.18.38)\n#### A.5 Other\n3\\) FOO clause (1.2)\n")
    fixture_claim = ("| A.4.2 | Screens | Not claimed | x |\n| A.4.3 | Commit | **Not claimed** | x |\n| A.4.4 | Tables | **Claimed** | x |\n")
    check("seeds: the declined titles are read from the Annex A.4 items of the modules the claim declines, whole elements only, the parenthesized and the bare clause number alike, nothing past A.4",
          declined_seed_titles(fixture_spec, fixture_claim) == {"BLINK clause", "COMMIT statement"}
          and declined_modules(fixture_claim) == {"A.4.2": "Screens", "A.4.3": "Commit"},
          declined_seed_titles(fixture_spec, fixture_claim))
    check("edition: Annex E.2 item 25 names the sixteen words 2023 reserved, the last one (XOR) without its list marker, and §8.9 lists each",
          added_words == ["B-SHIFT-L", "B-SHIFT-LC", "B-SHIFT-R", "B-SHIFT-RC", "COMMIT", "EDITING", "END-RECEIVE", "END-SEND",
                          "EXCLUSIVE-OR", "FINALLY", "LOCATION", "MESSAGE-TAG", "RECEIVE", "ROLLBACK", "SEND", "XOR"]
          and set(added_words) <= reserved, added_words)
    check("prompt: every reserved word the prompt names as misused is in §8.9's list (the list has hundreds), and every family's prompt names them, the 2023 words, its own example and nothing unfilled",
          set(MISUSED_RESERVED_WORDS) <= reserved and len(reserved) > 300
          and all(", ".join(MISUSED_RESERVED_WORDS) in p and "the words B-SHIFT-L" in p and "XOR" in p and EXAMPLES[f] in p
                  and not re.search(r"\{[a-z]+\}", p) for f, p in prompts.items()),
          sorted(set(MISUSED_RESERVED_WORDS) - reserved))
    check("seeds: the environment-dependent functions' clauses are not seeds (their output cannot agree), every name is a function of the spec, and the prompt forbids each by name",
          set(NONDETERMINISTIC_FUNCTIONS) <= set(real) and not any(t.rsplit(" ", 1)[0] in NONDETERMINISTIC_FUNCTIONS for _n, t, _b in real_seeds if t.endswith(" function"))
          and all(f in sp for f in NONDETERMINISTIC_FUNCTIONS),
          sorted(set(NONDETERMINISTIC_FUNCTIONS) - set(real)))
    check("seeds: 15.6 Summary of functions (a table) is not a seed, and a structural title at two components is a clause and stays one (15.3 Arguments, 15.4 Returned values)",
          "15.6" not in by_number and by_number.get("15.3") == "Arguments" and by_number.get("15.4") == "Returned values", {k: by_number.get(k) for k in ("15.6", "15.3", "15.4")})
    check("cut off: a reply Ollama aborted for a repeating loop is the model's failure, named",
          "repeating loop" in cut_off_reason("aborted", 0, 0))
    check("prompt: the system prompt carries the example, every function of the edition, the 2023 additions as errors, no DELETE FILE rule, and nothing unfilled",
          EXAMPLES["statement"] in sp and all(f in sp for f in real) and "{functions}" not in sp and "{added}" not in sp
          and "FUNCTION CONCAT, FUNCTION CONVERT" in sp and "the DELETE FILE statement" in sp and "DELETE FILE it" not in sp
          and "HIGHEST-ALGEBRAIC and LOWEST-ALGEBRAIC take a numeric DATA ITEM" in sp)
    check("classify: same output agrees", classify(ok_run, ok_run) == "AGREE")
    check("classify: different output differs", classify(dataclasses.replace(ok_run, output="A\n2"), ok_run) == "OUTPUT_DIFFERS")
    check("classify: our refusal of a program GnuCOBOL compiled", classify(Observation(False, 1, "e: error COBOL1234", ["COBOL1234"]), ok_run) == "WE_REJECT")
    check("classify: our compiler crash outranks the refusal", classify(Observation(False, -1, "", [], crashed=True), ok_run) == "WE_CRASH")
    check("classify: our run-time crash", classify(dataclasses.replace(ok_run, crashed=True, run_rc=3762504530), ok_run) == "WE_CRASH")
    check("classify: our hang", classify(Observation(True, 0, "", [], ran=False, hung=True), ok_run) == "WE_HANG")
    check("classify: their hang", classify(ok_run, Observation(True, 0, "", [], ran=False, hung=True)) == "THEY_HANG")
    check("classify: their run-time failure while ours ran", classify(ok_run, dataclasses.replace(ok_run, run_rc=1)) == "THEY_FAIL_RUN")
    both_run_hang = Observation(True, 0, "", [], ran=False, hung=True)
    both_compile_hang = Observation(None, None, "", [], hung=True, runner_error="hang")
    check("classify: a hang on BOTH sides is the model's program, never ours, at run time, at compile time, and mixed (kb/Work PB3490)",
          classify(both_run_hang, both_run_hang) == "BOTH_HANG" and classify(both_compile_hang, both_compile_hang) == "BOTH_HANG"
          and classify(both_run_hang, both_compile_hang) == "BOTH_HANG" and classify(both_compile_hang, both_run_hang) == "BOTH_HANG"
          and "BOTH_HANG" in MODEL_FAILURE_CLASSES and "BOTH_HANG" not in LEAD_CLASSES)
    check("display image: GnuCOBOL's sign and point and WiseOwl's over-punch fold to one image; an unsigned point folds; words and edited values do not",
          display_image("R=+00012.50") == "R=+0001250" and display_image("R=000125{") == "R=+0001250"
          and display_image("N=-00012.50") == "N=-0001250" and display_image("N=000125}") == "N=-0001250"
          and display_image("U=00012.50") == "U=0001250" and display_image("I=0001B") == "I=+00012" and display_image("I=0001K") == "I=-00012"
          and display_image("A B R 1,234.50 X1A") == "A B R 1,234.50 X1A",
          [display_image(x) for x in ("R=+00012.50", "R=000125{", "N=-00012.50", "N=000125}", "U=00012.50", "I=0001B", "I=0001K", "A B R 1,234.50 X1A")])
    check("display fold allowed: a scaled picture (V) and a sentence period allow it; an edited picture with a point, a leading-sign edited picture, a SIGN SEPARATE item forbid it (review R1-1)",
          display_fold_allowed("01 A PIC 9(5)V99." + chr(10) + "01 B PICTURE IS S9(3)V9(2) VALUE 1.5." + chr(10) + "DISPLAY 1.5.")
          and not display_fold_allowed("01 A PIC ZZ9.99.") and not display_fold_allowed("01 A PIC +9(5).99 VALUE 1.")
          and not display_fold_allowed("01 A pic $9.99.") and not display_fold_allowed("01 A PIC S9(5) SIGN IS LEADING SEPARATE.")
          and not display_fold_allowed("01 A PIC -9(3).") and not display_fold_allowed("01 A PIC 9(3)CR.") and not display_fold_allowed("01 A PIC 9(3)DB.")
          and display_fold_allowed("01 A PIC Z(4)9." + chr(10) + "01 B PIC X(3) VALUE 'CR'.")
          and not display_fold_allowed("01 WS-EDIT-PIC PIC ZZ9.99.") and not display_fold_allowed("01 WS-EDIT-PIC PICTURE ZZ9.99.")
          and not display_fold_allowed("01 A PIC S9(5) LEADING SEPARATE.") and not display_fold_allowed("01 A PIC S9(5) TRAILING SEPARATE CHARACTER.")
          and display_fold_allowed("01 A PIC S9(5) SIGN LEADING." + chr(10) + "01 WS-SEPARATE PIC 9.")
          and not display_fold_allowed("01 A PIC S9(5) SIGN TRAILING SEPARATE CHARACTER."))
    check("classify: outputs that differ only in the display form are OUTPUT_LATITUDE only when the fold is allowed, not a survivor; any other difference, a differing exit, or a program with an edited point stays OUTPUT_DIFFERS",
          classify(dataclasses.replace(ok_run, output="R=000125{"), dataclasses.replace(ok_run, output="R=+00012.50"), True) == "OUTPUT_LATITUDE"
          and classify(dataclasses.replace(ok_run, output="R=000125{"), dataclasses.replace(ok_run, output="R=+00012.50")) == "OUTPUT_DIFFERS"
          and classify(dataclasses.replace(ok_run, output="R=000125{ Z"), dataclasses.replace(ok_run, output="R=+00012.50"), True) == "OUTPUT_DIFFERS"
          and classify(dataclasses.replace(ok_run, output="R=000125{", run_rc=1), dataclasses.replace(ok_run, output="R=+00012.50", run_rc=0), True) == "OUTPUT_DIFFERS"
          and "OUTPUT_LATITUDE" not in LEAD_CLASSES and "OUTPUT_LATITUDE" in LATITUDE_CLASSES)
    check("classify: same stdout but our program exits non-zero where theirs exits zero is a lead, never AGREE (review R1-1)",
          classify(dataclasses.replace(ok_run, run_rc=1), ok_run) == "WE_FAIL_RUN")
    check("classify: same stdout, both exit non-zero with different statuses (review R1-1)",
          classify(dataclasses.replace(ok_run, run_rc=1), dataclasses.replace(ok_run, run_rc=8)) == "EXIT_DIFFERS"
          and classify(dataclasses.replace(ok_run, run_rc=8), dataclasses.replace(ok_run, run_rc=8)) == "AGREE")
    check("classify: a compile that exceeds the hang guard is a hang on its side, not a runner error (review R1-2)",
          classify(Observation(None, None, "", [], hung=True, runner_error="hang"), ok_run) == "WE_HANG"
          and classify(ok_run, Observation(None, None, "", [], hung=True, runner_error="hang")) == "THEY_HANG")
    check("classify: an unobserved compiler is never a verdict (plan §11 A12e)",
          classify(Observation(None, None, "", [], runner_error="did not start"), ok_run) == "RUNNER_ERROR"
          and classify(ok_run, Observation(None, None, "", [], runner_error="x")) == "RUNNER_ERROR"
          and classify(Observation(True, 0, "", [], ran=None, runner_error="x"), ok_run) == "RUNNER_ERROR")
    check("signature: a refusal groups by our codes, a difference by clause",
          signature("WE_REJECT", "14.9.25", Observation(False, 1, "", ["COBOL1234", "COBOLNET4001"])) == "WE_REJECT:COBOL1234,COBOLNET4001"
          and signature("OUTPUT_DIFFERS", "14.9.25", ok_run) == "OUTPUT_DIFFERS:14.9.25")

    rows = [{"id": "fz00000", "clause": "14.9.25", "title": "MOVE", "verdict": "AGREE", "model_seconds": 10},
            {"id": "fz00001", "clause": "14.9.25", "title": "MOVE", "verdict": "OUTPUT_DIFFERS", "signature": "OUTPUT_DIFFERS:14.9.25",
             "source": "programs/fz00001.cob", "model_seconds": 12,
             "we": {"compiled": True, "run_rc": 0, "output": "A\n1", "first_error": "", "codes": []}, "they": {"compiled": True, "output": "A\n2", "run_rc": 0}},
            {"id": "fz00002", "clause": "15.3", "title": "ABS", "verdict": "BOTH_REJECT", "model_seconds": 8, "repairs": 3},
            {"id": "fz00003", "clause": "15.3", "title": "ABS", "verdict": "RUNNER_ERROR", "reason": "model: down"},
            {"id": "fz00004", "clause": "15.42", "title": "TRANSLATE", "verdict": "THEY_REJECT", "signature": "THEY_REJECT:15.42",
             "source": "programs/fz00004.cob", "model_seconds": 9, "repairs": 1,
             "we": {"output": "X", "first_error": "", "codes": []}, "they": {"first_error": "x.cob:9: error: FUNCTION 'TRANSLATE' unknown", "run_rc": None}},
            {"id": "fz00005", "clause": "15.3", "title": "ABS", "verdict": "NO_PROGRAM", "model_seconds": 4, "reason": "no COBOL program in the reply"},
            {"id": "fz00006", "clause": "14.9.11", "title": "DISPLAY", "verdict": "OUTPUT_LATITUDE", "signature": "OUTPUT_LATITUDE:14.9.11",
             "source": "programs/fz00006.cob", "model_seconds": 7,
             "we": {"output": "R=000125{", "first_error": "", "codes": []}, "they": {"output": "R=+00012.50", "run_rc": 0}},
            {"id": "fz00007", "clause": "14.9.28", "title": "PERFORM", "verdict": "BOTH_HANG", "signature": "BOTH_HANG:14.9.28", "model_seconds": 5},
            {"id": "fz00008", "clause": "14.9.25", "title": "MOVE", "verdict": "OUTPUT_DIFFERS", "signature": "OUTPUT_DIFFERS:14.9.25",
             "source": "programs/fz00008.cob", "model_seconds": 11, "display_fold": True,
             "we": {"output": "R=000125{" + chr(10) + "Q=1", "first_error": "", "codes": []}, "they": {"output": "R=+00012.50" + chr(10) + "Q=2", "run_rc": 0}}]
    rep = render_report(rows, Path("."))
    check("report: usable over judged programs only (the model's failures are BOTH_REJECT, NO_PROGRAM and BOTH_HANG), repairs counted on usable rows, the THEY_REJECT count and group with GnuCOBOL's error, the differing line, the latitude rows under their own heading and not among the survivors",
          "judged: 8 · usable: 5 (62% of judged; 1 of them after a repair round) · usable on the first attempt: 4 (50% of judged) · "
          "the model's failures: 3 (0 ended early on a repeated diagnostic) · GnuCOBOL alone refused (THEY_REJECT leads): 1" in rep
          and "| AGREE | 1 |" in rep and "### `OUTPUT_DIFFERS:14.9.25` × 2" in rep and "### `THEY_REJECT:15.42` × 1" in rep
          and "line 2: `Q=1` | `Q=2`" not in rep       # the group shows its FIRST row's lines (fz00001); fz00008 is listed in it
          and "- GnuCOBOL: `x.cob:9: error: FUNCTION 'TRANSLATE' unknown`" in rep
          and "line 2: `1` | `2`" in rep and "Runner errors: 1" in rep
          and "## Survivors: 3 programs in 2 signatures" in rep and "OUTPUT_LATITUDE:14.9.11" not in rep and "BOTH_HANG:14.9.28" not in rep
          and "## Display-form latitude: 1 programs (not survivors)" in rep and "- clause 14.9.11 (DISPLAY): 1 — fz00006" in rep, rep)
    check("bake-off: the score is the usable share, with both-refuse and GnuCOBOL-alone counts beside it, and the share usable on the first attempt (a repaired THEY_REJECT is usable, not first-attempt)",
          bake_off_score(rows) == (9, 5, 8, 3, 1, 9, 3, 4), bake_off_score(rows))
    hang_row = {"id": "fz01771", "clause": "14.9.28", "title": "PERFORM", "verdict": "WE_HANG", "signature": "WE_HANG:14.9.28", "model_seconds": 9,
                "we": {"compiled": True, "codes": [], "first_error": "", "run_rc": None, "crashed": False, "hung": True, "output": "", "runner_error": ""},
                "they": {"compiled": True, "first_error": "", "run_rc": None, "hung": True, "output": "", "runner_error": ""}}
    lat_row = {**rows[1], "id": "fz00024", "we": {**rows[1]["we"], "compiled": True, "run_rc": 0, "output": "R=000C"}, "they": {**rows[1]["they"], "compiled": True, "output": "R=+0003"}}
    cut_row = {**lat_row, "id": "fz00099", "we": {**lat_row["we"], "output": "R=000C" + "x" * RECORDED_OUTPUT}, "they": {**lat_row["they"], "output": "R=+0003" + "x" * RECORDED_OUTPUT}}
    with tempfile.TemporaryDirectory() as td:
        out = Path(td)
        (out / "programs").mkdir()
        (out / "programs" / "fz00024.cob").write_text("01 R PIC S9(3)." + chr(10), encoding="utf-8", newline="\n")
        (out / "programs" / "fz00025.cob").write_text("01 R PIC +9(3).99." + chr(10), encoding="utf-8", newline="\n")
        edited_row = {**lat_row, "id": "fz00025", "source": "programs/fz00025.cob"}
        recorded_fold = {**lat_row, "id": "fz00026", "source": "programs/gone.cob", "display_fold": True}
        gone_row = {**lat_row, "id": "fz00027", "source": "programs/gone.cob"}
        check("rederived: a recorded row is re-read under today's classes (a both-sides hang, the latitude), keeps its recorded verdict beside it; the fold follows the recorded display_fold, else the program's source (an edited point forbids it, a missing source forbids it); an unchanged, unobserved or cut row is left alone",
              rederived(hang_row, out)["verdict"] == "BOTH_HANG" and rederived(hang_row, out)["recorded_verdict"] == "WE_HANG"
              and rederived({**lat_row, "source": "programs/fz00024.cob"}, out)["verdict"] == "OUTPUT_LATITUDE"
              and rederived({**lat_row, "source": "programs/fz00024.cob"}, out)["signature"] == "OUTPUT_LATITUDE:14.9.25"
              and rederived(edited_row, out)["verdict"] == "OUTPUT_DIFFERS" and rederived(recorded_fold, out)["verdict"] == "OUTPUT_LATITUDE"
              and rederived(gone_row, out)["verdict"] == "OUTPUT_DIFFERS"
              and rederived(rows[1], out) is rows[1] and rederived(rows[2], out) is rows[2] and rederived(rows[3], out) is rows[3] and rederived(cut_row, out) is cut_row,
              [rederived(hang_row, out)["verdict"], rederived(edited_row, out)["verdict"], rederived(gone_row, out)["verdict"]])
    solo = render_report([rows[8]], Path("."))
    check("report: a digit-image row that differs elsewhere lists its REAL differing line first and counts the lines that differ only in the display form",
          "line 2: `Q=1` | `Q=2`" in solo and "line 1:" not in solo
          and "1 other line(s) differ only in the digit-image display form of a numeric item (PB803 / PB3467)" in solo, solo)
    decimal_row = {**rows[8], "numeric_display": "decimal", "display_fold": False}
    decimal_solo = render_report([decimal_row], Path("."))
    check("report: a row compiled with WiseOwl's default decimal form lists the display-form line as a REAL difference (kb/Work PB3505)",
          "line 1: `R=000125{` | `R=+00012.50`" in decimal_solo and "differ only in the digit-image display form" not in decimal_solo, decimal_solo)
    check("display fold: only a digit-image compile can fold, in a program the text allows (kb/Work PB3505); a recorded row follows its form",
          not display_fold_applies(NUMERIC_DISPLAY, "01 A PIC 9(5)V99.") and display_fold_applies("digits", "01 A PIC 9(5)V99.")
          and not display_fold_applies("digits", "01 A PIC ZZ9.99.") and NUMERIC_DISPLAY == "decimal"
          and not row_display_fold({"numeric_display": "decimal", "display_fold": True}, Path("."))
          and row_display_fold({"display_fold": True}, Path(".")) and not row_display_fold({"display_fold": False}, Path("."))
          and not row_display_fold({"source": "programs/gone.cob"}, Path(".")))
    with tempfile.TemporaryDirectory() as td2:
        decimal_lat = {**lat_row, "id": "fz00028", "numeric_display": "decimal", "display_fold": True}
        check("rederived: a row compiled with the decimal form is never OUTPUT_LATITUDE, whatever its recorded fold (kb/Work PB3505)",
              rederived(decimal_lat, Path(td2))["verdict"] == "OUTPUT_DIFFERS" and rederived({**lat_row, "display_fold": True}, Path(td2))["verdict"] == "OUTPUT_LATITUDE")
    check("report: the triage rule is printed", "the spec decides" in rep)
    adapter, problems = suite_config.load_adapter(SUITE / "adapters" / "gnucobol.json")
    check("adapters: the suite's gnucobol adapter loads and has the 2014 edition this script pins",
          adapter is not None and EDITION in adapter.editions, problems)
    adapter_w, problems = suite_config.load_adapter(SUITE / "adapters" / "wiseowl.json")
    check("adapters: the suite's wiseowl adapter loads and names its `cobol` variable",
          adapter_w is not None and "cobol" in adapter_w.variables and EDITION in adapter_w.editions, problems)
    bridge = WslBridge("Ubuntu")
    ctx = suite_config.StepContext("run", ("E:/w/fz00001",), Path("."), {}, 1.0, {"program": "fz00001", "work": "E:/w"})
    seen: list[tuple[str, ...]] = []
    orig = suite_run.execute
    try:
        suite_run.execute = lambda argv, work, env, timeout: (seen.append(argv), suite_config.Step(0))[1]  # type: ignore[assignment]
        bridge.run(ctx)
        bridge.compile(suite_config.StepContext("compile", ("cobc", "-x", "fz00001.cob"), Path("."), {}, 1.0, {}))
    finally:
        suite_run.execute = orig
    check("wsl bridge: the run addresses the program relative to the mapped working directory; compile passes the argv through",
          seen == [("wsl", "-d", "Ubuntu", "-e", "./fz00001"), ("wsl", "-d", "Ubuntu", "-e", "cobc", "-x", "fz00001.cob")], seen)
    hanging = types.SimpleNamespace(stage=lambda entry, work: ("a.cob", "a"), unjudgeable=lambda step, what: f"{what} hang",
                                    operation=lambda *_a: suite_config.Step(None, timed_out=True))
    hung_obs = observe(hanging, {}, Path("."))  # type: ignore[arg-type]
    check("observe: a compile that times out is an observation with hung set, so the verdict is a hang and not a runner error (review R1-2)",
          hung_obs.compiled is None and hung_obs.hung and classify(hung_obs, ok_run) == "WE_HANG", hung_obs)
    staged: list[bool] = []
    stale = Path(__file__).resolve().parent.parent / "conformance-suite" / "results" / "self-test-observe-fresh" / "fz00009"
    stale.mkdir(parents=True, exist_ok=True)
    (stale / "left-over.dll").write_bytes(b"x")
    fresh_run = types.SimpleNamespace(work=stale.parent, stage=lambda entry, work: (staged.append(work.exists()), ("a.cob", "a"))[1],
                                      operation=lambda *_a: suite_config.Step(0, timed_out=False), crashed=lambda step: False,
                                      unjudgeable=lambda step, what: "")
    try:
        observe_fresh(fresh_run, {}, "fz00009")  # type: ignore[arg-type]
    except Exception as e:  # noqa: BLE001 — the check below reports it
        staged.append(repr(e))  # type: ignore[arg-type]
    finally:
        shutil.rmtree(stale.parent, ignore_errors=True)
    check("observe_fresh: an earlier attempt's work directory is removed before the program is staged again (the first PB3488 bake-off lost 3 of 20 programs to FileExistsError)",
          staged == [False], staged)
    def stubbed_one(sides: dict[str, Any]) -> tuple[dict[str, Any], list[str], list[str]]:
        """Fuzz._one over a stubbed model (one fixed program, line 4 "DISPLAY WS-X") and stubbed compilers: sides[name](k) is
        that compiler's Observation of its k-th compile. Returns the row, the calls in order and every user message sent."""
        one_dir = Path(__file__).resolve().parent.parent / "conformance-suite" / "results" / "self-test-one"
        shutil.rmtree(one_dir, ignore_errors=True)
        (one_dir / "programs").mkdir(parents=True)
        calls: list[str] = []
        asked: list[str] = []
        seen = {"they": 0, "we": 0}

        def chat(_model: str, messages: list[dict[str, str]], _temperature: float) -> tuple[str, float, str, int]:
            calls.append("model")
            asked.append(messages[-1]["content"])
            return "```cobol\nIDENTIFICATION DIVISION.\nPROGRAM-ID. FUZZ.\nPROCEDURE DIVISION.\nDISPLAY WS-X\nSTOP RUN.\n```", 1.0, "stop", 0

        def fresh(run: Any, _entry: dict[str, Any], _name: str) -> Observation:
            calls.append(run.name)
            seen[run.name] += 1
            return sides[run.name](seen[run.name])

        saved_chat, saved_fresh = ollama_chat, observe_fresh
        try:
            globals()["ollama_chat"], globals()["observe_fresh"] = chat, fresh
            fake = types.SimpleNamespace(s=types.SimpleNamespace(model="m", temperature=0.3, repairs=3, keep=True), out=one_dir,
                                         programs=one_dir / "programs", we=types.SimpleNamespace(name="we", work=one_dir / "w"),
                                         they=types.SimpleNamespace(name="they", work=one_dir / "t"),
                                         system_prompts={family: f"sys {family}" for family in EXAMPLES})
            row = Fuzz._one(fake, 0, "fz00000", "14.9.25", "MOVE statement", "body", 0, {"id": "fz00000"})  # type: ignore[arg-type]
        finally:
            globals()["ollama_chat"], globals()["observe_fresh"] = saved_chat, saved_fresh
            shutil.rmtree(one_dir, ignore_errors=True)
        return row, calls, asked

    gnu_refuses = lambda k: Observation(False, 1, f"fz00000.cob:4: error: 'WS-X{k}' is not defined", [])  # noqa: E731
    # R1-1: a WiseOwl crash on a program GnuCOBOL refused ends the rounds as WE_CRASH; it is never sent back for a correction.
    rec_crash, calls, _asked = stubbed_one({"they": gnu_refuses, "we": lambda k: Observation(False, -1, "crash", [], crashed=True)})
    check("run: a WiseOwl crash on a program GnuCOBOL refused is WE_CRASH after ONE round (review R1-1: it was sent back and lost), and no refusal round is recorded",
          rec_crash["verdict"] == "WE_CRASH" and calls == ["model", "they", "we"] and rec_crash["repairs"] == 0
          and rec_crash["repair_rounds"] == [], (rec_crash.get("verdict"), calls))
    same = lambda k: Observation(False, 1, "fz00000.cob(4,1): error COBOLNET1639: 'WS-X' is not defined", ["COBOLNET1639"])  # noqa: E731
    rec_same, calls, asked = stubbed_one({"they": gnu_refuses, "we": same})
    check("run: a correction that draws the same first diagnostics from both compilers ends the rounds there, BOTH_REJECT and stopped_early, after one correction rather than three (kb/Work PB3523 item 4)",
          rec_same["verdict"] == "BOTH_REJECT" and rec_same.get("stopped_early") is True and rec_same["repairs"] == 1
          and calls == ["model", "they", "we"] * 2, (rec_same.get("verdict"), rec_same.get("stopped_early"), calls))
    check("run: each refusal round is recorded on the row with WiseOwl's first code and message and GnuCOBOL's first error, and the row names its seed family (kb/Work PB3523 item 3)",
          rec_same["repair_rounds"] == [{"round": k, "code": "COBOLNET1639", "message": "fz00000.cob(4,1): error COBOLNET1639: 'WS-X' is not defined",
                                         "gnucobol": f"fz00000.cob:4: error: 'WS-X{k + 1}' is not defined"} for k in (0, 1)]
          and rec_same["family"] == "statement", rec_same["repair_rounds"])
    check("run: the correction sent to the model quotes the refused source line beside each compiler's diagnostic (kb/Work PB3523 item 2)",
          len(asked) == 2 and asked[1].count("    > line 4: DISPLAY WS-X") == 2, asked[1:])
    changing = lambda k: Observation(False, 1, f"fz00000.cob({k},1): error COBOL000{k}: unexpected", [f"COBOL000{k}"])  # noqa: E731
    rec_changing, calls, _asked = stubbed_one({"they": gnu_refuses, "we": changing})
    check("run: a program whose first diagnostic changes every round keeps the whole budget of three corrections (four rounds recorded, not stopped early)",
          rec_changing["verdict"] == "BOTH_REJECT" and "stopped_early" not in rec_changing and rec_changing["repairs"] == 3
          and calls == ["model", "they", "we"] * 4 and [r["code"] for r in rec_changing["repair_rounds"]] == ["COBOL0001", "COBOL0002", "COBOL0003", "COBOL0004"],
          (rec_changing.get("verdict"), calls))
    check("refusal key: GnuCOBOL's message family drops the file, the line, the quoted names and the numbers; WiseOwl's code must repeat too, and a broad code with a new GnuCOBOL family is a new error",
          gnucobol_family("a.cob:4: error: 'WS-T' not indexed") == gnucobol_family("b.cob:19: error: 'X2' not indexed") == "error: '_' not indexed"
          and refusal_key({"code": "COBOL0001", "message": "m", "gnucobol": "a.cob:3: error: syntax error, unexpected Identifier"})
          != refusal_key({"code": "COBOL0001", "message": "m", "gnucobol": "a.cob:9: error: PROCEDURE DIVISION header missing"})
          and refusal_key({"code": "", "message": "x.cob(3,1): error: boom", "gnucobol": "g"})[0] == "x.cob(3,1): error: boom")
    def boom(*_a: object) -> dict[str, Any]:
        raise FileNotFoundError("fz00002.cob")
    fake = types.SimpleNamespace(seed_for=lambda i: ("14.9.25", "MOVE statement", "body", 0), s=types.SimpleNamespace(model="m"), _one=boom)
    rec = Fuzz.one(fake, 2)  # type: ignore[arg-type]
    check("run: a harness failure inside one program is a RUNNER_ERROR row with its traceback, never the end of the run",
          rec["verdict"] == "RUNNER_ERROR" and rec["reason"].startswith("harness: FileNotFoundError") and "boom" in rec["traceback"]
          and rec["id"] == "fz00002", rec)
    # The run cadence (kb/Work PB3523): a fixture register with one note of each kind that must and must not hold a run.
    with tempfile.TemporaryDirectory() as td3:
        reg = Path(td3)
        for nid, status, tags, process_only in (("PB9001", "open", "[work, defect, fuzz]", "false"), ("PB9002", "landed", "[work, defect, fuzz]", "false"),
                                                ("PB9003", "open", "[work, analysis, fuzz]", "true"), ("PB9004", "open", "[work, defect]", "false"),
                                                ("PB9005", "owner", "[work, defect, fuzz]", "false"), ("PB9006", "retired", "[fuzz]", "false")):
            (reg / f"{nid}.md").write_text(f'---\ntitle: "{nid} — a fixture note"\nid: {nid}\nkind: defect\nstatus: {status}\n'
                                           f"process_only: {process_only}\ntags: {tags}\n---\n\n# {nid}\n", encoding="utf-8", newline="\n")
        check("cadence: an open note tagged fuzz that is a compiler defect holds the next run, whatever its non-terminal status; a landed or retired one, a process-only one and an untagged one do not",
              [it["id"] for it in open_fuzz_notes(reg)] == ["PB9001", "PB9005"], [it.get("id") for it in open_fuzz_notes(reg)])
        missing = str(reg / "no-such-cobol.exe")
        saved_work = WORK_DIR
        outputs: dict[str, tuple[object, str]] = {}
        try:
            globals()["WORK_DIR"] = reg
            for case, argv in (("run", ["--model", "m", "--count", "1", "--cobol", missing]),
                               ("forced", ["--model", "m", "--hours", "8", "--cobol", missing, "--force-open-notes"]),
                               ("bake-off", ["--bake-off", "m", "--count", "1", "--cobol", missing])):
                buf = io.StringIO()
                with contextlib.redirect_stdout(buf), contextlib.redirect_stderr(io.StringIO()):
                    try:
                        got: object = main(argv)
                    except SystemExit as e:     # argparse's error on the missing compiler: past the cadence
                        got = f"exit {e.code}"
                outputs[case] = (got, buf.getvalue())
        finally:
            globals()["WORK_DIR"] = saved_work
        check("cadence: a run is REFUSED while such a note is open (exit EXIT_OPEN_NOTES, before anything starts), naming each note with its status",
              outputs["run"][0] == EXIT_OPEN_NOTES and "REFUSED" in outputs["run"][1] and "PB9001 [open]" in outputs["run"][1]
              and "PB9005 [owner]" in outputs["run"][1] and "PB9002" not in outputs["run"][1], outputs["run"])
        check("cadence: --force-open-notes starts the run anyway (it reaches the compiler check) and still names every open note",
              outputs["forced"][0] == "exit 2" and "--force-open-notes: starting despite" in outputs["forced"][1]
              and "PB9001" in outputs["forced"][1] and "PB9005" in outputs["forced"][1], outputs["forced"])
        check("cadence: a bake-off is not a run and is never refused", outputs["bake-off"][0] == "exit 2" and outputs["bake-off"][1] == "",
              outputs["bake-off"])
    print("self-test " + ("PASSED" if rc == 0 else "FAILED"))
    return rc


# ── the run cadence ───────────────────────────────────────────────────────────────────────────────────────────────────

#: The register a run reads (the self-test points it at a fixture directory).
WORK_DIR = REPO / "kb" / "Work"
#: The exit status of a run refused by the cadence (argparse's own errors exit 2).
EXIT_OPEN_NOTES = 3


def open_fuzz_notes(work_dir: Path) -> list[dict[str, Any]]:
    """The notes that hold the next run (the owner's rule in the module docstring, kb/Work PB3523): tagged `fuzz`, not
    `process_only` (a compiler defect, not a note about the harness), and open: any status the register does not call
    terminal (`work.TERMINAL_STATUSES`, the register's one vocabulary for DONE, kb/Work PB3196), so a note waiting on the
    owner or blocked still holds the run."""
    import work  # scripts/spec, on sys.path above
    return [it for it in work.load(work_dir) if "fuzz" in (it.get("tags") or []) and it.get("process_only") is not True
            and it.get("status") not in work.TERMINAL_STATUSES]


def cadence_gate(work_dir: Path, force: bool) -> bool:
    """True when a run may start: no note holds it, or `force` (`--force-open-notes`). Either way every holding note is
    printed with its status and title, so an overridden start is never a silent one."""
    holding = open_fuzz_notes(work_dir)
    if not holding:
        return True
    print(f"{'--force-open-notes: starting despite' if force else 'REFUSED: the run cadence holds this run;'} "
          f"{len(holding)} open kb/Work note(s) from an earlier fuzz run (fix and land them on main, rebuild the judge, then run):")
    for it in holding:
        print(f"  {it.get('id', it.get('_file'))} [{it.get('status')}] {str(it.get('title', ''))[:140]}")
    return force


# ── main ──────────────────────────────────────────────────────────────────────────────────────────────────────────────

def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--model", help="the Ollama model tag that writes the programs")
    ap.add_argument("--bake-off", help="comma-separated model tags; each writes --count programs, scored by GnuCOBOL acceptance")
    ap.add_argument("--count", type=int, help="programs to write (per model in a bake-off)")
    ap.add_argument("--hours", type=float, help="stop starting new programs after this many hours")
    ap.add_argument("--jobs", type=int, default=2, help="programs in flight at once (Ollama queues the generations)")
    ap.add_argument("--clauses", default=",".join(DEFAULT_CLAUSES), help="comma-separated clause prefixes to seed from")
    ap.add_argument("--temperature", type=float, default=0.3, help="0.3: the syntax slips are a sampling error, not a shortage of ideas")
    ap.add_argument("--repairs", type=int, default=3, help="corrections offered to the model after both compilers refuse (0: none)")
    ap.add_argument("--seed", type=int, default=20261020, help="shuffles the seed clauses deterministically (same seed, same walk)")
    ap.add_argument("--in-order", action="store_true", help="walk the seed clauses in the spec's order instead")
    ap.add_argument("--cobol", default=str(REPO / "src/Cobol.Net.Cli/bin/Debug/net10.0" / ("cobol.exe" if os.name == "nt" else "cobol")),
                    help="the WiseOwl compiler to test")
    ap.add_argument("--out", help="the run directory (default conformance-suite/results/fuzz-<stamp>)")
    ap.add_argument("--keep", action="store_true", help="keep every program's working directories")
    ap.add_argument("--report", help="re-render DIR/report.md from DIR/results.jsonl and exit")
    ap.add_argument("--force-open-notes", action="store_true",
                    help="start a run although kb/Work notes from an earlier fuzz run are open (they are listed)")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.report:
        print(write_report(Path(a.report)))
        return 0
    if not (a.model or a.bake_off):
        ap.error("--model or --bake-off is required")
    if a.count is None and a.hours is None:
        ap.error("--count or --hours is required")
    if not a.bake_off and not cadence_gate(WORK_DIR, a.force_open_notes):
        return EXIT_OPEN_NOTES
    if not Path(a.cobol).exists():
        ap.error(f"compiler not found: {a.cobol} (build src/Cobol.Net.Cli first, or pass --cobol)")
    out = Path(a.out) if a.out else SUITE / "results" / f"fuzz-{datetime.now().strftime('%Y%m%d-%H%M%S')}"
    s = Settings(model=a.model or "", out=out, jobs=a.jobs, count=a.count, hours=a.hours,
                 prefixes=tuple(p.strip() for p in a.clauses.split(",") if p.strip()), temperature=a.temperature,
                 cobol_exe=str(Path(a.cobol).resolve()), keep=a.keep, repairs=max(0, a.repairs), seed=a.seed, in_order=a.in_order)
    if a.bake_off:
        bake_off([m.strip() for m in a.bake_off.split(",") if m.strip()], s)
    else:
        Fuzz(s).run()
    print(f"report: {out / 'report.md'}")
    return 0


if __name__ == "__main__":
    # A Windows console in cp1252 cannot print the report's and a model's non-Latin characters; never die on a print.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    sys.exit(main(sys.argv[1:]))
