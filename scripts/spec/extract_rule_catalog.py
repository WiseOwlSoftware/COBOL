#!/usr/bin/env python3
"""PHASE A of the spec-conformance review — build the rule catalog (THE DENOMINATOR).

`docs/rearchitecture/DESIGN-spec-conformance-review.md` defines P14 Step-0 DONE as every normative ISO/IEC
1989:2023 rule having a located implementation, a spec-verified verdict, and a spec-derived test. None of that is
measurable without first enumerating the rules — the thing the two prior audits never did (they were bounded
SAMPLING). This script produces that enumeration.

The spec transcription is regular enough to parse directly:

    <a id="section-8-4-3-13-3"></a>
    ## 8.4.3.13.3 Syntax rules

    1) Identifier-1 shall be of category alphanumeric or national.
    2) ...
       a) sub-item
       b) sub-item

So a rule is (section-number, kind, ordinal) and its text runs to the next ordinal or heading.

COMPLETENESS IS THE WHOLE POINT: an omitted rule is silent false confidence — the catalog would report a
denominator smaller than reality and every later percentage would be wrong in the flattering direction. So the
script self-checks: every rule-block heading in the file must yield at least one rule, and any that yields zero is
reported as a PARSE GAP rather than silently dropped.

Usage:
    python scripts/spec/extract_rule_catalog.py                 # write the 2023 catalog + print the summary
    python scripts/spec/extract_rule_catalog.py --check         # parse only; non-zero exit if gaps exist
    python scripts/spec/extract_rule_catalog.py --edition 2002 [--check]  # the 2002 catalog (kb/Work PB5256)
    python scripts/spec/extract_rule_catalog.py --edition 1985 --check   # the 1985 arm (rule_catalog_1985.py, kb/Work
                                                                         # PB4886): counts and findings, writes nothing
    python scripts/spec/extract_rule_catalog.py --self-test     # the ISO arm on planted text, a correction applied

THE ISO EDITIONS ARE ONE EXTRACTION OVER A TABLE (kb/Work PB5256). ISO/IEC 1989:2002 and ISO/IEC 1989:2023 are built
alike, so `extract` reads both and `ISO` holds what differs between them (`IsoEdition`: a rule-bearing annex, the
implementor-defined list's annex, the excluded and known-empty blocks, the conventions clause, the table of contents'
form, the row form, and whether the transcription is reconciled yet). The 1985 standard is not built like ISO's (Roman
sections, modules, levels and level-2 boxes, two amendments), so its arm is its own reader, `rule_catalog_1985.py`,
which shares this module's heading rules, kind table, content-keyed scan and standing lists; `--edition` takes the
edition from spec_pdf's one table, and an edition with no arm here (2014) exits naming the table.

WHAT IT CANNOT READ IS A FINDING, NEVER A GUESS (owner 2026-11-10: "If it cannot be resolved, there is a problem/bug").
Each is a `Finding` naming its kind, clause, line and PDF page, counted by kind (`FINDING_KINDS`). A reconciled edition
holds none (`--check` fails on any); an unreconciled one (2002, kb/Work PB4813) records them in its catalog for the
reconciliation, and `--check` fails when they change. THE STABILITY RULE (`IsoEdition.rule_id`): an id is its clause,
kind and printed number, so regenerating after a correction keeps every id whose clause and number did not change.
"""
from __future__ import annotations

import argparse
import bisect
import dataclasses
import functools
import itertools
import json
import pathlib
import re
import sys
from collections import Counter
from collections.abc import Mapping
from typing import Any

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from spec_headings import GENERAL_FORMAT_TITLE  # noqa: E402  — THE one spelling of the heading (kb/Work PB284)
from cite import CORRECTION_NOTE  # noqa: E402  — ONE definition of what an editorial correction note looks like
# The PRINTED level of every sub-list, measured on the PDF (kb/Work PB55) — the key and the opening-item rule are
# the measuring tool's own, so the recorder and this checker cannot disagree about what a sub-list is.
from measure_sublist_levels import DATA as SUBLIST_LEVELS, openings as sublist_openings, sublist_key  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = pathlib.Path(__file__).resolve().parents[2]
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
import inventory_schema  # noqa: E402  (each edition's catalog path: the schema's `editions.of`, the one home)
import citation  # noqa: E402  (each edition's regions: the standing lists' sections and headings)
import element_standing  # noqa: E402  (THE standing of a rule, from the edition's lists; kb/Work PB4993)
#: The manifest's key for the PROSE clauses admitted as one-row-per-paragraph (kb/Work PB284 part 3 / PB479).
PARAGRAPH_BLOCKS = "paragraph-blocks"

# A numbered section heading, e.g. "## 8.4.3.13.3 Syntax rules" or "### 14.9.28.4 General rules".
HEADING = re.compile(r"^#{2,6}\s+(?P<num>\d+(?:\.\d+)*)\s+(?P<title>.+?)\s*$")
PAGE = re.compile(r"^##\s+Page\s+(\d+)\s*$")
# Any markdown heading — used to CLOSE a rule block (see the loop). Annex headings carry no section number.
ANY_HEADING = re.compile(r"^#{1,6}\s+\S")
# A GENERAL-FORMAT DIAGRAM in the transcription. Every diagram is a `<pre style="line-height:1">` block today; the
# fenced (also inside a `>` blockquote, as §2023:7.3.23.2 still is) and `$$` forms are the transcription's earlier
# renderings. Keying `has_diagram` on those two alone left it False on all 322 FMT rows once the diagrams moved to
# <pre> — a field that could never report the one thing it names (kb/Work PB1536).
FMT_DIAGRAM = re.compile(r"<pre\b|^[\s>]*```|\$\$", re.M)
# The numbered-and-named format label of a multi-format construct ("Format 1 (replacing):"), §2023:5.2.1.
FMT_LABEL = re.compile(r"^\s*Format\s+\d+", re.M)
# PAGE-BREAK FURNITURE. At every printed page boundary the transcription injects a running header, and it is
# emitted AS A HEADING — inside rule blocks. Treating it as a block terminator truncates every rule block that
# spans a page break (it cost 896 rules when first tried). Skip it instead. Three transcribed shapes, the same
# three scripts/fix_spec_pagebreaks.py had to learn the hard way; missing the bare form is the dangerous one. The
# header names its edition's year (`IsoEdition.running_header`).
def running_header(year: int) -> re.Pattern[str]:
    return re.compile(rf"^#{{1,6}}\s*(?:\*\*)?ISO/IEC\s+1989:{year}\s*\(E\)(?:\*\*)?\s*$")


# ALL page-break furniture, in every transcribed shape. These lines land INSIDE rule prose at a page boundary and
# were being absorbed as rule TEXT: 592 of 3,247 rules (18%) carried an anchor, a rule, a running header or a
# licence footer spliced into the normative sentence. A rule whose text is contaminated is worse than a missing
# one — it reads as authoritative and would be cited that way. Skipped at the line level, before buffering.
def furniture(year: int) -> re.Pattern[str]:
    return re.compile(
        r"^\s*(?:"
        r"-{3,}"                                              # the --- separators around a page block
        r'|<a\s+id="[^"]*"></a>'                              # ANY anchor — page-N and section-N-N both leak in
        rf"|(?:\*\*)?ISO/IEC\s+1989:{year}\s*\(E\)(?:\*\*)?"  # running header: bold or bare (no leading #)
        rf"|(?:\d+\s+)?(?:©|\(c\))?\s*ISO/IEC\s+{year}"      # copyright footer
        r"|Licensed to .*"                                    # licence footer
        r")\s*$", re.I)
# ⛔ THE IN-CLAUSE GROUP LABEL — the standard's run-in sub-heading INSIDE a rule list ("SEQUENTIAL FILES",
# "RELATIVE FILES", "FORMAT 3", "ALL FORMATS", "FORMATS 1, 2 AND 4", "CLASS SPECIFIER"). It scopes the rules that
# follow it and is NEITHER a heading NOR rule prose, which is exactly the pair of mistakes this project made with
# it (fix-queue PB689):
#   · read as a HEADING it TRUNCATES the block. `specs/ISO_COBOL_2023.md` rendered nine of them as `## FORMAT n` /
#     `## SEQUENTIAL FILES`; the block scan closes at any heading, so §2023:14.9.51.4 (WRITE) contributed 16 of its 42
#     general rules and the traceability inventory's denominator — the number v1.0 is defined against (D13) —
#     was silently short by 60 rows across five clauses. GR-14.9.51.4-23, the line-sequential '71' status, was
#     one of the rules that had no row to verdict.
#   · read as PROSE it CONTAMINATES the preceding rule: seven catalog rows ended "... is set to '44'. RELATIVE
#     FILES" — the same splicing FURNITURE above exists to prevent.
# THE PRINTED PAGE SETTLES WHAT IT IS. Measured in the PDF text layer on pp146, 463, 609, 680 and 820: every one
# is Cambria 10.7pt flags=4 at the rule-number margin — byte-identical typography to the rule text around it —
# where a real clause heading is Cambria,Bold 11.7pt flags=20 ("14.9.51.4  General rules", p817). So the standard
# prints no heading here, and the transcription's own convention agrees: 209 of the 218 labels are plain lines.
# ⛔ KEYED ON SHAPE, NOT ON A VOCABULARY. A list of label spellings is the hand-maintained-list anti-pattern and
# would have to grow for every new one; the shape was DERIVED by measurement instead — 301 all-caps standalone
# lines inside rule blocks, 37 distinct, and every single one is a group label (zero false positives). The
# optional `#` prefix is matched so a transcription that heading-ifies one is absorbed rather than obeyed; those
# are reported as transcription defects to repair (see `heading_labels` below), never silently tolerated.
GROUP_LABEL = re.compile(r"^(?:#{1,6}\s+)?(?P<label>[A-Z][A-Z0-9 \-/&,]*)\s*$")
# A top-level rule ordinal at the start of a line. The transcription uses BOTH "1)" and "1." — an earlier version
# matched only "1)" and silently dropped every rule in §2023:8.8.3.2, §2023:11.9.11.2, §2023:13.18.24.3 and §2023:14.9.30.3. The
# completeness self-check is what surfaced it; without it the denominator would have been quietly short.
# Sub-items ("a)") are indented and stay with their parent rule.
# The label's delimiter is ESCAPED in the transcription (`1\)`, `1\.`) because an unescaped `1)` at column 0
# is a Markdown ordered-list marker — it renders as `1.`, which collides with the standard's own third level,
# and it re-parses the rule's text as block syntax, which ate the leading `>` of eight rules. The backslash is
# optional here so the catalog reads both forms: this regex is the DENOMINATOR of the P14 traceability
# inventory, and a silent zero-rule extraction would read as "no rules in this clause" rather than as a break.
# ⛔ The DELIMITER FORM (")" vs ".") is captured because it is load-bearing (fix-queue PB39): §2023:15.68.3 writes its
# top-level rules `1\)` … and wrote its nested list `1.` … at column 0, and the form — not any arithmetic on the
# ordinals — is what separated them. DEVLOG 1167 proved every ordinal-only heuristic wrong on that clause. (PB55
# has since indented that list at its printed level; the form rule stays the segmenter's evidence for the next.)
ORDINAL = re.compile(r"^(?P<n>\d+)\\?(?P<form>[.)])\s+(?P<text>.*)$")

# An INDENTED numbered item inside a rule's body — the standard's nested lists ("GR 4 a) 1. 2. 3. …").
#
# ⛔ THE ROW SHAPE, DECIDED FOR THE WHOLE CATALOG BY THE OWNER (kb/Work PB55, 2026-08-08, "THE RULE IS THE
# UNIT"): **one row per TOP-LEVEL numbered rule of a rule block; every sub-item — lettered `a)`, or numbered
# under one, at any depth — is FOLDED into its parent rule's text and is addressed by CITATION PATH, never by a
# row of its own.** The transcription prints every nested list at its MEASURED level — the 23 page-break tails
# by kb/Work PB698, the 51 lists it had flattened whole (152 fragment rows) by PB55 — and the shape is held two
# ways: a flattened TAIL is folded here and reported (below), and a flattened WHOLE list, which no markdown
# evidence can tell from a peer list, fails `--check` unless the PDF measurement `measure_sublist_levels.py`
# recorded in `data/sublist-levels.json` puts it at the block's own margin. A sub-list that survives is a PEER
# list — printed at the margin under its own prose lead-in, its items rules in their own right. Three
# independent things said the same as the owner and the extractor was the only dissenter:
#   · the standard's own cross-references address a sub-item as a PATH ON ITS PARENT — "General rule 12c)",
#     "General rule 4b)", 21 such citations — and never renumber one into the top-level sequence;
#   · `scripts/spec/cite.py` resolves that path (`--check 14.9.41.3 "…"` → `6) b) 2.`), so folding costs a
#     sub-item NOTHING in citability;
#   · `scripts/spec/audit_catalog_coverage.py` states the invariant outright in `printed_ordinals`: "Sub-items
#     ('a)', indented '1.') are not column-0 and are not counted; they are part of their parent rule."
# A row whose text cannot be read without its parent is not a rule but a FRAGMENT — the row once at
# `SR-14.9.41.3-L2.2` read "It has the same class, category, and usage as that record key.", whose subject and
# referent both live in the item above it. That is the same silent-false-confidence class as PB689's
# contamination, in the other direction.
#
# The transcription flattened the TAIL of some nested lists onto column 0 at a PAGE BREAK (§2023:14.9.41.3 SR6 b) 2.
# and 3. are printed indented under b) on PDF page 785 / folio 755 — the ordinal glyph measured at x0 115.05 on
# a page whose body margin is 80.04 — and were transcribed at column 0). All 23 such items, in 14 clauses, were
# measured on the PDF and REPAIRED in the transcription by PB698, so this arm catches nothing today and is a
# net for the next one. Such a tail is EVIDENCE the segmenter needs: a column-0 ordinal that CONTINUES
# the most recent indented run, and does NOT continue the innermost open level, is that nested list's tail, so it
# is buffered into the parent rule exactly as its indented siblings are — the indentation accident can no longer
# decide what a row IS. Every one is REPORTED (fatal under `--check`) so the transcription gets repaired rather
# than quietly worked around, which is the same posture `heading_labels` takes for PB689's group labels.
INDENTED_ORDINAL = re.compile(r"^\s+(?P<n>\d+)\\?[.)]\s")

# The rule-block kinds the standard uses. Intrinsic functions use argument/returned-value rules instead of SR/GR.
# SINGULAR forms are load-bearing, not a nicety: the standard writes "Syntax rule" when a block holds exactly one
# (§2023:10.6.3, §2023:11.9.4, §2023:13.4.3, …). A plural-only map skipped 15 whole blocks — invisible to the empty-block check,
# because a block that is never RECOGNISED is never counted as seen. The TOC cross-check below is what caught it.
# ⛔ KEYED ON THE SINGULAR CANONICAL FORM ONLY — every plural variant is normalised into it by `kind_of`.
# The literal-spelling map came back a SECOND time: it carried "argument rules"/"argument rule" and still missed
# §2023:15.10.3, §2023:15.13.3 and §2023:15.100.3, which the standard heads "Argument**s** rule(s)". Ten normative rules were
# absent from the denominator, and the burn-down was measuring against a total that was short — the exact "silent
# false confidence" DESIGN-spec-conformance-review.md §7 names. Adding two more spellings would have set up the
# third occurrence, so the pluralisation is now normalised away and the unrecognised-heading check below fails loudly
# on any rule-shaped heading this map does not know.
#
# ⛔ "Editing rules" AND "Precedence rules" ARE NOT A THIRD AND FOURTH KIND — the standard classifies them
# ITSELF, and this map records that classification rather than inventing vocabulary for it:
#   §2023:5.3.3  "The rules of the PICTURE clause specified in 13.18.40.5, Editing rules, are General rules."
#   §2023:5.3.2  "The rules of the PICTURE clause specified in 13.18.40.6, Precedence rules, are syntax rules."
# (both validated with `scripts/spec/cite.py --check`). So this is the SAME defect class as the pluralisation
# above — a heading spelling the map did not know — not a change to what the denominator MEANS. 17 rules, all
# core PICTURE editing: 15 GR + 2 SR. Owner decision 2026-07-30.
# §2023:5.3 "Rules" itself stays OUT and is the reason the two above come in: it is the clause that DEFINES the
# taxonomy (§2023:5.3.1–5.3.3 is prose about what syntax and general rules are) and carries no numbered rules of its
# own, so there is nothing there to extract.
#
# ⛔ THE ONE KIND TABLE OF EVERY EDITION (kb/Work PB4886, PB5256): 2002 and 2023 share the ISO arm, and the 1985 arm (rule_catalog_1985.py) types its headings with
# this table too, its PICTURE "Editing Rules" and "Precedence Rules" being the same rules; a 1985 paragraph of another
# name is typed by its row in UNHARVESTED_OF[1985] (spec-unharvested-rule-blocks-1985.json), never by a second table.
#
# ⛔ 2002's "Rounding rules" AND BARE "Rules" ARE GENERAL RULES, the kind 2023 gives each paired clause (kb/Work PB5332,
# the design owner's decision of 2026-11-11: type them, never adjudicate them away). §2002:5.2.2: "A general rule
# defines or clarifies the meaning or relationship of meanings of an element or set of elements." The three conditions
# headed "Rules" (§2002:8.8.4.1.4.2 condition-name, 8.8.4.1.5.2 switch-status, 8.8.4.1.6.2 sign) are 2023's §2023:8.8.4.5.3,
# 8.8.4.6.3 and 8.8.4.7.4, each headed "General rule(s)"; §2002:8.8.1.3.1.3 "Rounding rules" (when a standard intermediate
# data item is rounded) became §2023:11.9.11.2's General rules of the INTERMEDIATE ROUNDING clause, and 2023's own
# intermediate-data-item lists (§2023:8.8.1.4.2, 8.8.1.5.2) are admitted as GR. A taxonomy clause headed "Rules"
# (§2002:5.2, §2023:5.3) stays out through `IsoEdition.excluded_blocks`, which `extract` honours before it opens a block.
KINDS = {
    "syntax rule": "SR",
    "general rule": "GR",
    "argument rule": "AR", "argument": "AR",
    "returned value rule": "RV", "returned value": "RV",
    "editing rule": "GR",       # §2023:13.18.40.5 — typed by §2023:5.3.3, not a new kind
    "precedence rule": "SR",    # §2023:13.18.40.6 — typed by §2023:5.3.2, not a new kind
    "rounding rule": "GR",      # §2002:8.8.1.3.1.3 — a general rule by §2002:5.2.2, as §2023:11.9.11.2 (PB5332)
    "rule": "GR",               # §2002:8.8.4.1.4.2, 8.8.4.1.5.2, 8.8.4.1.6.2 — as §2023:8.8.4.5.3-8.8.4.7.4 (PB5332)
}

#: Heading titles that name a rule block in some spelling. Anything matching this and NOT resolving through
#: KINDS is a block the extractor would silently skip.
RULE_SHAPED = re.compile(r"\brules?\b", re.IGNORECASE)

#: The rule-shaped headings deliberately outside the denominator and the blocks that rightly yield no rule are each
#: edition's own facts: `IsoEdition.excluded_blocks` and `known_empty_blocks` (rule_catalog_1985.py holds 1985's).


def canonical_title(title: str) -> str:
    """Lower-case, de-punctuated, and SINGULARISED on both words of the block name.

    The standard is inconsistent in both positions independently — "Syntax rule" / "Syntax rules" /
    "Argument rules" / "Arguments rules" / "Arguments rule" all occur — so canonicalising is the only form that
    does not need a new map entry per combination.
    """
    t = title.strip().lower().rstrip(".")
    t = re.sub(r"\brules\b", "rule", t)
    t = re.sub(r"\barguments\b", "argument", t)
    t = re.sub(r"\bvalues\b", "value", t)      # §2023:15.69.4 "Returned values rules" — a THIRD pluralisation axis
    return t


def kind_of(title: str) -> str | None:
    return KINDS.get(canonical_title(title))


#: The title of a clause that PRINTS a construct's general format is `spec_headings.GENERAL_FORMAT_TITLE`: THE one
#: definition, shared with every script that asks the question (kb/Work PB284: the sibling copies kept missing the
#: one clause, §2023:14.9.7.2, that spells the noun "General forms").

#: A clause title that only INTRODUCES its construct and carries no block of its own ("14.9.7.1 General").
INTRO_TITLE = re.compile(r"general\.?", re.IGNORECASE)


def is_unrecognised_general(title: str) -> bool:
    """Does this heading OPEN with the word "General" and still name no block the extractor knows?

    ⛔ THE GUARD THAT WOULD HAVE SEEN §2023:14.9.7.2 (kb/Work PB284). Every guard before it asked "did a recognised
    block yield nothing" or "does a rule-shaped heading resolve", and a block heading spelt a way nobody listed is
    neither: it is never recognised, so it is never counted, so the denominator is short and every check is
    green. The only block-introducing titles the standard opens with "General" are the intro clause ("General"),
    the format clause (GENERAL_FORMAT_TITLE) and the general rules (KINDS); any other is a spelling variant of one
    of them or a new block kind, and either way a person decides.
    """
    t = title.strip()
    if not re.match(r"general\b", t, re.IGNORECASE):
        return False
    return not (INTRO_TITLE.fullmatch(t) or GENERAL_FORMAT_TITLE.fullmatch(t) or kind_of(t))


def unharvested_rule_blocks(lines: list[str], titles: dict[str, str],
                            heading: re.Pattern[str] = HEADING) -> dict[str, dict]:
    """Every clause whose BODY carries an ascending ``N)`` list that this extractor does not harvest.

    ⛔ THE THIRD INSTANCE OF ONE DEFECT CLASS, AND THE FIRST DETECTOR THAT CAN SEE IT (fix-queue PB29).
    The denominator has been short twice — 3,790 → 3,846 (heading spellings the literal map did not know) →
    3,861 (§2023:13.18.40.5/.6, typed by §2023:5.3) — and both fixes extended a HEADING-keyed guard. ``RULE_SHAPED``
    reports a heading containing the word "rule" that ``KINDS`` cannot resolve, which is exactly why it cannot
    see this one: §2023:8.8.1.2 is headed *"Native, standard-binary, and standard-decimal arithmetic"* and its seven
    numbered rules sit under ordinary prose. A guard keyed on what a block is CALLED can never find a block that
    is called something else, so this one is keyed on what a block CONTAINS.

    ⚠ THE RESULT IS AN UPPER BOUND, NOT A DEFECT COUNT, AND IT MUST NOT BE ADDED TO THE DENOMINATOR WHOLESALE.
    The standard numbers ordinary prose enumerations the same way it numbers rules — §2023:8.3.2.2 "User-defined
    words" lists the KINDS of user-defined word, not fifteen requirements. Admitting all of them would be the
    mirror of the short denominator, and the denominator defines v1.0 (D13). So each clause is adjudicated and
    recorded in the committed manifest beside this script; this function only MEASURES. `heading` is the edition's
    numbered heading (`IsoEdition.heading`), and a page marker (`## Page N`, which the 2002 transcription keeps) is
    neither a heading nor the clause's body.
    """
    found: dict[str, dict] = {}
    cur: tuple[str, str] | None = None
    body: list[str] = []

    def close() -> None:
        if cur is not None and kind_of(cur[1]) is None and (shape := ordinal_shape(body)) is not None:
            found[cur[0]] = {"title": cur[1], **shape}

    for line in lines:
        if PAGE.match(line):
            continue
        if ANY_HEADING.match(line):
            if (m := heading.match(line)) and not line.lstrip().startswith("["):
                close()
                cur, body = (m.group("num"), m.group("title")), []
                continue
            close()
            cur, body = None, []
            continue
        if cur:
            body.append(line)
    close()
    return found


def ordinal_shape(body: list[str], ordinal: re.Pattern[str] = ORDINAL) -> dict | None:
    """THE content-keyed scan's measure of one clause or paragraph body, for every edition (kb/Work PB29, PB4886): how
    many column-0 ordinals continue an ascending list (`ordinals`), how many lists open at 1 (`lists`) and the longest
    single run (`longest`); None when the body carries no list. `ordinal` is the edition's rule ordinal, its number in
    the group `n` (2023's `1)`, 1985's `(1)`).

    ⛔ COUNT THE LISTS, NOT ONLY THE ITEMS — the distinction is the whole triage signal, and reporting only a total
    actively misleads. §2023:8.3.2.2 "User-defined words" reports 15 items, which reads like a numbered rule block; they
    are FIVE nested sub-lists inside prose, each restarting at 1 (1-2-3, 1-2, 1-6, 1-2, 1-2). The shape is no proof of
    normativity either way (the manifest's comment records the test that refuted that), which is why each clause is
    adjudicated; it is what makes a hundred clauses triageable at all. An ordinal that neither opens nor continues a list
    is a stray "3)" mid-prose and is not counted."""
    ordinals = lists = last = run = longest = 0
    for line in body:
        if (m := ordinal.match(line)) is None:
            continue
        n = int(m.group("n"))
        if n == 1:
            lists, last, run = lists + 1, 1, 1
        elif n == last + 1:
            last, run = n, run + 1
        else:
            continue
        ordinals += 1
        longest = max(longest, run)
    return {"ordinals": ordinals, "lists": lists, "longest": longest} if ordinals else None


#: THE COMMITTED MANIFEST of the content-keyed scan, per edition (kb/Work PB29; the 1985 arm's, PB4886; 2002's,
#: PB5256). Each carries the same shape: `blocks`, a clause (an ISO edition: its number; 1985: its citation,
#: `§1985:VI.6.4.2`, since the base text and Amendment 1 both have an `A.2.1`) ⇒ its measured shape, its `disposition`
#: (`rules` with its `kind`, `not-rules` with `why`, or `pending`, never a decision) and the reason.
UNHARVESTED_OF = {year: REPO / "docs" / "rearchitecture" / f"spec-unharvested-rule-blocks{'' if year == 2023 else f'-{year}'}.json"
                  for year in (1985, 2002, 2023)}
#: THE STANDING LISTS of an edition, held once as data (kb/Work PB4993; element_standing.py): each catalog rule's
#: `status` is read from them, by the one call every arm makes (`element_standing.apply`). 1985's is the obsolete list
#: of §1985:C.1; 2002's are the archaic list of §2002:G.1 and the obsolete list of §2002:G.2; 2023's are the archaic
#: list of §2023:F.1 and the obsolete list of §2023:F.2.
STANDING_OF = {edition: REPO / "docs" / "rearchitecture" / f"spec-element-standing-{edition}.json"
               for edition in (1985, 2002, 2023)}


def catalog_path(year: int) -> pathlib.Path:
    """The committed rule catalog of the held edition `year`: the schema's `editions.of` is its one home."""
    return REPO / inventory_schema.edition(year).catalog


def manifest_blocks(path: pathlib.Path) -> dict[str, dict]:
    """The `blocks` of a content-keyed scan manifest, or none while it does not exist."""
    return json.loads(path.read_text(encoding="utf-8"))["blocks"] if path.exists() else {}


def admitted_kinds(blocks: dict[str, dict]) -> dict[str, str | dict]:
    """The clauses a manifest ADMITS to the denominator (disposition `rules`) ⇒ their declared kind: so admitting a clause
    is a data edit with a recorded reason, never a code edit. ⛔ A kind need not be uniform across a clause, and the two
    non-uniform shapes are EXPLICIT so they cannot be confused (DEVLOG 1166 recorded the limit; these forms are its
    removal):
      "SR"                                   — every rule in the clause is this kind;
      {"sublists": {"1": "SR", "2": "GR"}}   — kind per SUB-LIST (§2023:8.3.2.2: five normative sub-lists, two constrain
                                               where words may be WRITTEN ⇒ SR per §2023:5.3.2, three state effect or
                                               treatment ⇒ GR per §2023:5.3.3);
      {"items": {"1": "SR", "4": "GR"}}      — kind per ITEM of a SINGLE list (§2023:8.3.3.3.2: items 1-3 constrain the
                                               literal's written form ⇒ SR, item 4 states its value ⇒ GR);
    and a sub-list's kind may itself be an `items` map, for a sub-list whose items differ
    ({"sublists": {"1": {"items": {"1": "GR", "3": "SR"}}, "2": "GR"}})."""
    return {sec: info["kind"] for sec, info in blocks.items() if info.get("disposition") == "rules"}


def rule_kind(kind: str | dict, sublist: int, ordinal: int) -> str | None:
    """The kind of rule `ordinal` of sub-list `sublist` in a clause admitted as `kind` (admitted_kinds' shapes), or None
    when the map does not name it: a manifest/segmentation DISAGREEMENT, which each arm reports, since writing a guessed
    kind would be the silent mis-typing the dict forms exist to prevent."""
    if isinstance(kind, str):
        return kind
    if "sublists" in kind:
        of_sublist = kind["sublists"].get(str(sublist))
        return of_sublist if of_sublist is None or isinstance(of_sublist, str) else rule_kind(of_sublist, 1, ordinal)
    if "items" in kind:
        return kind["items"].get(str(ordinal)) if sublist == 1 else None
    return None


def kinds_of(kind: str | dict) -> set[str]:
    """Every rule kind a clause admitted as `kind` (admitted_kinds' shapes) can give its rules."""
    if isinstance(kind, str):
        return {kind}
    return {k for shape in kind.values() for sub in shape.values() for k in kinds_of(sub)}


def unharvested_report(measured: dict[str, dict], declared: dict[str, dict], manifest: pathlib.Path,
                       label: str = "§") -> tuple[list[str], int]:
    """THE CONTENT-KEYED CHECK's report, for every edition: the scan's `measured` clauses against the committed manifest's
    `declared` ones. Returns the lines and the number of drifts (a clause measured and undeclared, declared and no longer
    measured, or measured with another ordinal count), each of which fails `--check`. `label` prefixes a manifest key
    where it prints (`§2023:` for an ISO edition's bare clause numbers; 1985's keys are citations already)."""
    added, removed, changed = unharvested_drift(measured, declared)
    pending = {s: v for s, v in declared.items() if v.get("disposition") == "pending"}
    out = ["",
           "⛔ CONTENT-KEYED SCAN — clauses carrying an ascending numbered list that this extractor does NOT harvest:",
           f"      {len(measured):4d} clauses, {sum(v['ordinals'] for v in measured.values()):5d} ordinals (an UPPER BOUND"
           " — see ordinal_shape)",
           f"      {len(pending):4d} clauses, {sum(v['ordinals'] for v in pending.values()):5d} ordinals awaiting a "
           "denominator disposition (fix-queue PB29)"]
    if added or removed or changed:
        out.append("")
        out.append("⛔ THE UNHARVESTED SET HAS DRIFTED FROM ITS MANIFEST — the denominator may now be short again.")
        out += [f"      + {label}{s} ({measured[s]['ordinals']} ordinals) '{measured[s]['title']}' — NEW, undeclared"
                for s in added]
        out += [f"      - {label}{s} — declared but no longer measured (harvested now, or the transcription moved)"
                for s in removed]
        out += [f"      ~ {label}{s}  {declared[s]['ordinals']} → {measured[s]['ordinals']} ordinals" for s in changed]
        out.append(f"   Adjudicate each and update {manifest.relative_to(REPO)}: a clause is either a RULE block")
        out.append("   (disposition 'rules' with its kind) or a prose enumeration (disposition 'not-rules', with why).")
    else:
        out.append("      ✓ matches the committed manifest exactly — no clause has silently left the denominator")
    return out, len(added) + len(removed) + len(changed)


def unharvested_drift(measured: dict[str, dict], declared: dict[str, dict]) -> tuple[list[str], list[str], list[str]]:
    """The clauses the scan measured and the manifest does not declare, declared and no longer measured, and measured
    with another ordinal count: the content-keyed check's three drifts, each sorted."""
    return (sorted(set(measured) - set(declared)), sorted(set(declared) - set(measured)),
            sorted(s for s in set(measured) & set(declared) if measured[s]["ordinals"] != declared[s]["ordinals"]))


#: A line that CONTINUES the paragraph before it instead of opening a new one: a numbered or lettered list item, a
#: table row, a markup block, or a NOTE (informative; it stays in the rule's text, where `NOTE_MARK` cuts it from
#: every predicate and the clause's own rows stay the normative paragraphs).
PARAGRAPH_CONTINUATION = re.compile(r"^(?:\s*\d+\\?[.)]\s|\s*[a-z]\\?\)\s|\s*\||<|NOTE\b|\s*Table\s+\d)")
#: Where a sentence may legitimately END. A paragraph that stops short of one is cut by a page break (the
#: running header and licence footer between its halves leave a blank line), and its tail is the same paragraph.
SENTENCE_END = re.compile(r"[.:;?!]\s*$")


def prose_paragraphs(ed: IsoEdition, lines: list[str], at: int) -> list[str]:
    """The normative PARAGRAPHS of the clause whose heading is `lines[at]` — the row unit of a PROSE concept clause.

    ⛔ WHY A CLAUSE WITH NO NUMBERED RULES STILL HAS ROWS (kb/Work PB284 part 3, PB479). §2023:9.1.16 (record locking),
    §2023:9.1.17 (logical unit of work) and §2023:9.1.18 (commit and rollback) state the facility's meaning as running
    prose: no `N)` marker, no "General rules" heading, so the numbered-rule scan never opened them and the
    denominator the burn-down is measured against did not hold the clause that SETS EC-FLOW-APPLY. A rule with no
    row can never be a GAP and can never be closed. The paragraph is the unit the standard's own prose has, so one
    row per paragraph is that clause's row shape, exactly as one row per numbered rule is a rule block's.

    Admission is DATA (`paragraph-blocks` in the edition's content-keyed manifest), never a code edit: the standard is
    full of normative prose, and which clauses are rules is an adjudication (PB479), not a pattern.
    """
    running, furn = ed.running_header, ed.furniture
    paragraphs: list[list[str]] = []
    fresh = True
    for line in lines[at + 1:]:
        if ANY_HEADING.match(line) and not running.match(line) and not PAGE.match(line):
            break
        if running.match(line) or PAGE.match(line) or furn.match(line):
            continue
        if not line.strip():
            fresh = True
            continue
        if fresh and paragraphs and (PARAGRAPH_CONTINUATION.match(line)
                                     or not SENTENCE_END.search(paragraphs[-1][-1])):
            fresh = False
        if fresh:
            paragraphs.append([line.strip()])
            fresh = False
        elif paragraphs:
            paragraphs[-1].append(line.strip())
    return [re.sub(r"\s+", " ", " ".join(p)).strip() for p in paragraphs]


def band_formats(label: str) -> list[int]:
    """The FORMAT NUMBERS a printed band names: "FORMAT 2" -> [2], "FORMATS 1, 2 AND 4" -> [1, 2, 4].

    ⛔ A band that is not about formats ("SEQUENTIAL FILES", "ALL FORMATS", "CLASS SPECIFIER") names none and
    returns [] — "ALL FORMATS" is a band on its own, but it scopes a rule to no particular format, which is what
    an empty list says. The numbers are the clause's OWN (PRESENT WHEN's FORMAT 2 is not ACCEPT's), so a reader
    that keys on them must also key on the rule's section (kb/Work PB284, `formats` in the inventory schema).
    """
    if not re.match(r"FORMATS?\b", label):
        return []
    return [int(n) for n in re.findall(r"\d+", label)]


def subject_for(num: str, titles: dict[str, str]) -> str:
    """The nearest enclosing ancestor section that is not itself a rule block — the construct being ruled."""
    parts = num.split(".")
    for cut in range(len(parts) - 1, 0, -1):
        parent = ".".join(parts[:cut])
        title = titles.get(parent)
        if title and kind_of(title) is None:
            return title
    return titles.get(num, "?")


# ── THE ISO EDITIONS: one extraction, a table of structure facts (kb/Work PB5256) ─────────────────────────────────────

@dataclasses.dataclass(frozen=True)
class IsoEdition:
    """THE STRUCTURE FACTS OF AN ISO-STRUCTURED EDITION, the one place they differ (kb/Work PB5256). ISO/IEC 1989:2002
    and ISO/IEC 1989:2023 are built alike: numbered clauses, a general format and Syntax and General rules per construct,
    Argument and Returned value rules per intrinsic function, an implementor-defined list in an annex, archaic and
    obsolete lists in another. Measured on 2026-11-11, the 2023 extraction read the 2002 text unchanged into 2,990 rows
    with 13 findings, every one of them a fact below or a transcription defect: so the extraction is ONE (`extract`)
    and what differs is this data. The 1985 standard is not built like this (Roman sections, modules and levels, boxes,
    two amendments): its reader is rule_catalog_1985.py, which shares the heading rules, the kind table, the
    content-keyed scan and the standing lists with this one."""
    year: int
    #: Annex letters whose clauses are numbered like the body's and carry rule blocks: 2002's Annex A (normative) is the
    #: communications facility, specified in Syntax and General rules exactly as the body's constructs are. Every
    #: other annex is lists and informative text, read as 2023's annexes are (the implementor-defined list below).
    rule_annexes: str
    #: The implementor-defined language element list, one DOC row per item (2023: A.1; 2002: B.1).
    doc_list: str
    #: Rule-shaped headings DELIBERATELY outside the denominator, each with the reason it carries no extractable
    #: rule. ⛔ A guard that reports an expected condition on every run is a guard people learn to ignore — the same
    #: cry-wolf failure §0 names for line-number code-locations — so a decided exclusion is recorded HERE rather than
    #: left to reappear as a warning forever. Anything NOT in it still fails loudly.
    excluded_blocks: Mapping[str, str]
    #: Recognised rule blocks that legitimately yield ZERO numbered rules, so "0 rules" is the correct answer rather
    #: than a parse failure. Same cry-wolf argument as `excluded_blocks`.
    known_empty_blocks: Mapping[str, str]
    #: The CONVENTIONS clause's sub-clauses: prose DEFINING what a syntax, general, argument and returned-value rule is.
    #: They are rule-block headings that correctly contain no rules.
    conventions: frozenset[str]
    #: How the transcription's table of contents lists an entry (its number and its words), the independent witness of
    #: what the printed standard contains, and what it lists: "rule blocks" (2023's links every one) or "clauses"
    #: (2002's prints the clauses to the third level, each of which must be a heading of the transcription).
    toc: re.Pattern[str]
    toc_lists: str
    #: Do the rows carry the edition form of a catalog row, as 1985's do: an id prefixed with the year
    #: (`2002-SR-14.8.25.2-1`), the rule's `citation` (`§2002:14.8.25.2 SR1`) and its PDF `page`? 2023's rows predate
    #: the form; its ids are the 4,214-row store's file names and the key every reader of it uses.
    edition_rows: bool

    @property
    def reconciled(self) -> bool:
        """Has the transcription been reconciled page by page against the printed standard? The fact is the ledger's
        (`verify_publishable.reconciled`: the reconciliation ledger records every page of the PDF), never a flag here.
        A reconciled edition's catalog holds no finding (`--check` fails on any). An unreconciled one's findings are
        FACTS ABOUT ITS TRANSCRIPTION, written into the catalog for the reconciliation to resolve (kb/Work PB4813), and
        `--check` fails when they differ from the committed ones: a finding is never skipped silently or guessed past."""
        import verify_publishable  # noqa: PLC0415  (the owner of the reconciliation ledgers)
        return verify_publishable.reconciled(self.year)

    @property
    def spec(self) -> pathlib.Path:
        return spec_pdf.markdown(self.year)

    @property
    def catalog(self) -> pathlib.Path:
        return catalog_path(self.year)

    @property
    def unharvested(self) -> pathlib.Path:
        return UNHARVESTED_OF[self.year]

    @property
    def standing(self) -> pathlib.Path:
        return STANDING_OF[self.year]

    @property
    def sublist_levels(self) -> pathlib.Path:
        """The PDF measurement of every sub-list's printed level (measure_sublist_levels.py): 2023's is DATA; another
        edition's file sits beside it, and while it is absent no sub-list of that edition is measured."""
        return SUBLIST_LEVELS if self.year == 2023 else SUBLIST_LEVELS.with_name(f"sublist-levels-{self.year}.json")

    @property
    def heading(self) -> re.Pattern[str]:
        """A numbered clause heading of this edition: the body's, and the rule-bearing annexes'."""
        return numbered_heading(self.rule_annexes)

    @property
    def running_header(self) -> re.Pattern[str]:
        return running_header(self.year)

    @property
    def furniture(self) -> re.Pattern[str]:
        return furniture(self.year)

    def label(self, clause: str) -> str:
        """`clause` in the edition form (`§2002:14.8.25.2`)."""
        return f"§{self.year}:{clause}"

    def rule_id(self, kind: str, section: str, ordinal: int | str, sublist: int = 1) -> str:
        """THE ROW KEY of a rule (DESIGN-spec-conformance-review.md §2, §10.1): `<kind>-<clause>-<n>`, a restarted list's
        `<kind>-<clause>-L<list>.<n>`, a format's `FMT-<clause>`, a prose paragraph's `<kind>-<clause>-p<n>`, prefixed with
        the year where the edition's rows carry it. THE STABILITY RULE follows from it: an id is a function of its
        clause, its kind and its printed number only, so regenerating the catalog after a correction keeps every id
        whose clause and rule number the correction did not change, and changes only the TEXT of a corrected rule."""
        prefix = f"{self.year}-" if self.edition_rows else ""
        if kind == "FMT":
            return f"{prefix}FMT-{section}"
        return f"{prefix}{kind}-{section}-{ordinal}" if sublist == 1 else f"{prefix}{kind}-{section}-L{sublist}.{ordinal}"

    def citation(self, section: str, kind: str | None = None, ordinal: int = 0, sublist: int = 1) -> str:
        """The citation of a row (`citation.render`): a numbered rule's `§2002:14.8.25.2 SR1` (`rule_designator`, the one
        writer of the designator); a format, a prose paragraph or an implementor-defined item cites its clause alone
        (no numbered `kind`), since the citation format has no designator for them."""
        rule = citation.rule_designator(kind, ordinal, sublist) if kind in citation.RULE_KINDS else None
        return citation.render(citation.Citation(self.year, section, rule))


def numbered_heading(rule_annexes: str = "") -> re.Pattern[str]:
    """A numbered clause heading whose clause may also be one of `rule_annexes`' (`A.1.2.3`); `HEADING` without any."""
    if not rule_annexes:
        return HEADING
    return re.compile(rf"^#{{2,6}}\s+(?P<num>[{rule_annexes}](?:\.\d+)+|\d+(?:\.\d+)*)\s+(?P<title>.+?)\s*$")


def toc_pattern(form: str, rule_annexes: str = "") -> re.Pattern[str]:
    """How a table of contents lists an entry: 2023's rule-block entries are Markdown links (`[14.9.8.3 Syntax
    rules](#section-…)`); 2002's entries are the printed lines with their dot leaders (`5.2.1 Syntax rules . . . 19`),
    an annex's clause among them (`A.1 Data division . . . 655`)."""
    if form == "linked":
        return re.compile(r"\[(\d+(?:\.\d+)*)\s+((?:Syntax|General|Argument|Returned value)\s+rules?)\]\(#section-")
    return re.compile(r"^((?:[A-Z](?:\.\d+)+)|\d+(?:\.\d+)*)\s+(\S.*?)\s+\.(?:\s*\.)+\s*[0-9ivx]+\\?\s*$", re.M)


#: The heading of a table of contents' list of tables, which ends its list of clauses (2002's `### Tables`).
TOC_TABLES = re.compile(r"^#{1,6}\s+Tables\s*$")


#: THE TABLE. A new ISO edition is one row here, from its own text (kb/Work PB4815: never another edition's row).
ISO: dict[int, IsoEdition] = {
    2023: IsoEdition(
        year=2023, rule_annexes="", doc_list="A.1",
        excluded_blocks={
            "5.3": "defines the rule taxonomy itself (§2023:5.3.1–5.3.3 is prose about what syntax and general rules ARE) "
                   "and carries no numbered rules of its own — it is the clause that CLASSIFIES §2023:13.18.40.5/.6, which "
                   "is why those two are in KINDS. Owner decision 2026-07-30.",
        },
        known_empty_blocks={
            "15.4": "its numbered rules live in the SUB-CLAUSE §2023:15.4.1 'Numeric and integer functions', whose prose "
                    "heading KINDS does not recognise — so this heading resolves to RV and correctly yields nothing of "
                    "its own. ⛔ THIS WAS AN UNOWNED PARSE GAP FOR MONTHS and the content-keyed scan explained it "
                    "(fix-queue PB29, 2026-08-04): the same defect class as §2023:8.8.1, one level down. §2023:15.4.1 is now "
                    "ADMITTED in spec-unharvested-rule-blocks.json and contributes its 6 RV rules — including r1, 'the "
                    "returned value shall equal the value of the equivalent arithmetic expression', which PB38 and the "
                    "D3 correction both turn on. Declared here rather than left as a warning forever.",
            "13.18.40.6": "the PICTURE precedence rules are UNNUMBERED prose plus Table 10 and Table 11 — the normative "
                          "content IS the two precedence matrices, which have no per-rule ordinal to extract. Typed SR "
                          "by §2023:5.3.2 so the heading resolves; it contributes no rows. Owner decision 2026-07-30.",
        },
        conventions=frozenset({"5.3.2", "5.3.3", "5.3.4", "5.3.5"}),
        toc=toc_pattern("linked"), toc_lists="rule blocks", edition_rows=False),
    2002: IsoEdition(
        year=2002, rule_annexes="A", doc_list="B.1",
        excluded_blocks={
            "5.2": "'Rules' defines the rule taxonomy itself (§2002:5.2: \"Except for intrinsic functions, rules are "
                   "categorized as syntax rules and general rules\"; §2002:5.2.1 to 5.2.4 define each kind in prose) and "
                   "carries no numbered rule, as §2023:5.3 does for 2023; it is the clause that types §2002:13.16.38.4 and "
                   "13.16.38.5, which is why KINDS types them.",
        },
        known_empty_blocks={
            "15.3": "its numbered rules live in the SUB-CLAUSE §2002:15.3.1 'Numeric and integer functions', whose prose "
                    "heading KINDS does not recognise, as §2023:15.4's live in §2023:15.4.1; this heading resolves to RV "
                    "and yields nothing of its own.",
            "13.16.38.5": "the PICTURE precedence rules are unnumbered prose and Table 11 and Table 12, as §2023:13.18.40.6 "
                          "is: typed SR by §2002:5.2.1 (\"The rules of the PICTURE clause specified in 13.16.38.5, "
                          "Precedence rules, are syntax rules\") so the heading resolves, with no row.",
        },
        conventions=frozenset({"5.2.1", "5.2.2", "5.2.3", "5.2.4"}),
        toc=toc_pattern("dotted"), toc_lists="clauses", edition_rows=True),
}


def page_of(lines: list[str]) -> list[int]:
    """The PDF page of each line (`## Page N`, or the earlier transcription's `<a id="page-N">` anchor); 0 before the
    first marker and throughout a transcription that keeps no pages."""
    out, page = [], 0
    for line in lines:
        if (m := PAGE.match(line)) or (m := PAGE_ANCHOR.match(line)):
            page = int(m.group(1))
        out.append(page)
    return out


#: The earlier transcription's page anchor (the integrity critic's page bookkeeping).
PAGE_ANCHOR = re.compile(r'^<a id="page-(\d+)"></a>')


@dataclasses.dataclass(frozen=True)
class Finding:
    """One thing the extractor could not read as the standard, at its place in the transcription: a block that yields
    no rule, a heading no kind resolves, an ordinal no list explains, and the rest of `FINDING_KINDS`. `line` is the
    transcription's (1-based) and `page` the PDF page that holds it, 0 where the finding has no one line."""
    kind: str
    clause: str
    line: int
    page: int
    text: str

    def as_json(self) -> dict[str, Any]:
        return dataclasses.asdict(self)

    def __str__(self) -> str:
        where = (f" line {self.line}" if self.line else "") + (f" (PDF page {self.page})" if self.page else "")
        return f"[{self.kind}] {self.clause}{where}: {self.text}"


#: Every kind of finding, and what it is: the summary prints the count of each, so a new kind cannot hide in another's.
FINDING_KINDS = {
    "parse-gap": "a recognised rule block that yields no rule",
    "unrecognised-heading": "a rule-shaped heading no kind resolves: its rules are absent from the denominator",
    "unrecognised-general": "a heading opening 'General' that names no block this extractor knows",
    "unexplained-ordinal": "a column-0 ordinal no open list explains: kept in the rule it interrupts, never a row",
    "heading-label": "an in-clause group label transcribed as a Markdown heading (PB689)",
    "flattened-tail": "a nested list's tail transcribed at column 0, folded into its parent rule (PB698)",
    "unmeasured-sublist": "a restarted list no PDF measurement holds at the block's margin (PB55)",
    "undrawn-format": "a general-format clause whose transcription carries no diagram and no Format label",
    "unharvested-drift": "a numbered list outside a rule block that the content-keyed manifest does not declare as measured",
    "paragraph-admission": "a prose clause admission the transcription no longer supports",
    "toc-unseen": "a rule block (2023) or a clause (2002) the table of contents lists that the transcription never heads",
    "non-spec-text": "text inside the standard that is not the standard",
    "standing": "a disagreement of the archaic or obsolete lists' data with the text",
    "uncitable": "a row whose citation names no region of the transcription",
    "shared-citation": "two numbered rules that share one citation",
}


@dataclasses.dataclass
class IsoCatalog:
    """What `extract` read from one edition's text: its rows, its blocks, its findings and the report it prints."""
    rules: list[dict[str, Any]]
    blocks: int
    parse_gaps: list[dict[str, str]]
    findings: list[Finding]
    report: list[str]
    #: The content-keyed manifest's clauses that await a disposition: their rules are outside the denominator.
    pending: list[str] = dataclasses.field(default_factory=list)


def manifest_of(ed: IsoEdition) -> tuple[dict[str, dict], dict[str, dict]]:
    """The content-keyed manifest of `ed`: its declared `blocks`, and its PROSE clauses admitted as paragraph rows
    (`paragraph-blocks`: section ⇒ {kind, kinds?, why}; `_comment` documents the key and is not a clause)."""
    if not ed.unharvested.exists():
        return {}, {}
    raw = json.loads(ed.unharvested.read_text(encoding="utf-8"))
    return raw.get("blocks", {}), {s: v for s, v in raw.get(PARAGRAPH_BLOCKS, {}).items() if not s.startswith("_")}


def measured_peers_of(ed: IsoEdition) -> set[str]:
    """The sub-lists the PDF measurement records at their block's own margin (level 0), keyed as `sublist_key` keys them."""
    path = ed.sublist_levels
    return ({m["key"] for m in json.loads(path.read_text(encoding="utf-8"))["sublists"] if m["level"] == 0}
            if path.exists() else set())


def extract(ed: IsoEdition, lines: list[str], declared: dict[str, dict], paragraph_blocks: dict[str, dict],
            measured_peers: set[str], standing: pathlib.Path | None = None, cite: bool = True) -> IsoCatalog:
    """THE ISO EXTRACTION, one for every ISO edition: read `lines` (the transcription of `ed`) into its rule rows, with
    the content-keyed manifest's `declared` blocks and `paragraph_blocks`, the sub-lists `measured_peers` holds at the
    margin and the standing lists at `standing` (the edition's, by default). Pure but for the standing lists' and the
    citations' reads: `--self-test` drives it on planted text. `cite` validates each row's citation against the
    edition's committed transcription, which a planted text is not."""
    say_lines: list[str] = []
    say = say_lines.append
    findings: list[Finding] = []
    pages = page_of(lines)
    heading, running, furn = ed.heading, ed.running_header, ed.furniture

    def find(kind: str, clause: str, line: int, text: str) -> None:
        findings.append(Finding(kind, clause, line, pages[line - 1] if line else 0, text))

    # The clauses ADMITTED to the denominator by adjudication (PB29) — section ⇒ declared rule kind. Read from
    # the manifest (admitted_kinds, the three kind shapes). A sub-list or item the map does not know is a
    # manifest/segmentation DISAGREEMENT and is FATAL at flush.
    admitted = admitted_kinds(declared)
    titles: dict[str, str] = {}
    title_line: dict[str, int] = {}

    # Pass 1 — every numbered heading, so a rule block can name its subject.
    for i, line in enumerate(lines, start=1):
        if (m := heading.match(line)) and not line.lstrip().startswith("["):
            titles.setdefault(m.group("num"), m.group("title"))
            title_line.setdefault(m.group("num"), i)

    # Pass 2 — walk rule blocks and collect ordinals.
    rules: list[dict] = []
    blocks_seen: list[tuple[str, str]] = []      # (section-number, kind)
    block_line: dict[tuple[str, str], int] = {}
    blocks_with_rules: set[tuple[str, str]] = set()

    page = 0
    cur: tuple[str, str] | None = None           # (section-number, kind) of the block being read
    buf: list[str] = []
    ordinal = 0
    # A rule block can contain MORE THAN ONE numbered list — the standard restarts numbering under an unheaded
    # sub-list (e.g. §2023:7.2.3.4 General rules runs 1..n for COPY, then restarts 1..n for text-word matching). Keyed
    # on (kind, section, ordinal) alone, 173 rules collided and silently overwrote each other — which would have
    # carried the WRONG verdict forward on every inventory refresh. Track the sub-list and qualify the id.
    #
    # ⛔ SEGMENTATION IS DECIDED BY THE DELIMITER FORM AND THE INDENTED-RUN EVIDENCE, NEVER BY ORDINAL ARITHMETIC
    # ALONE (fix-queue PB39, DEVLOG 1167). The old rule — "an ordinal that goes backwards opens a sub-list, and
    # the sub-list never returns to its parent" — filed 326 top-level rules under sub-list ids the standard does
    # not use (SR-12.3.7.3-L7.18 for what the standard calls "§2023:12.3.7.3 syntax rule 18"). The mechanism, read
    # from the transcription itself:
    #   · whatever delimiter form a run opened with is that level's form; a change of form opens a NEST
    #     (§2023:15.68.3 as PB39 found it: `1\)..5\)`, nested `1...7.`, then `6\) 7\)` returning to the top level);
    #   · EXCEPT that the transcription also drifts form mid-run — §2023:13.18.40.3 writes SR 1-5 as `N.` and SR 6-37
    #     as `N\)` — so an ordinal that STRICTLY CONTINUES an open level's run (n == last+1) belongs to that
    #     level regardless of form, preferring a form-matched level when two continue with the same n (§2023:9.3.6);
    #   · EXCEPT that a column-0 ordinal continuing the most recent INDENTED run is the flattened TAIL of that
    #     nested list, not a rule (§2023:14.9.13.4 GR4a: `1\.-3\.` indented, `4.-6.` at column 0; §2023:15.30.3 GR2b).
    #     ⛔ That case never reaches `place()` any more — it is decided in the scan loop by `flattened_tail`
    #     and BUFFERED INTO THE PARENT RULE (fix-queue PB698). It used to open a sub-list here, which gave 27
    #     sub-item FRAGMENTS rows of their own across 14 clauses while every other sub-item in the standard was
    #     folded, so an indentation accident at a page break was deciding what a row IS.
    # A stack of open levels [form, sublist-index, last-ordinal] implements this; `levels[0]` is the block's top
    # level (sublist 1, plain ids). An ordinal none of these cases explains (`explained`) is NO RULE: it stays in the
    # text of the rule it interrupts and is a finding (kb/Work PB5256: §2002:15.16.2's table cell "00 through 99."
    # wrapped onto a line of its own and read as rule 99). Filing it as a nest of its own, as this scan did until
    # then, gave the denominator a row whose id and text were a guess.
    levels: list[list] = []                      # [form, sublist-index, last-ordinal], innermost last
    next_sublist = 2
    last_indented = 0
    cur_sublist = 1
    cur_kinds: dict[str, str] | None = None      # per-sublist kind map for a MIXED-kind admitted block
    # The page a rule STARTS on. flush() runs when the NEXT ordinal or heading arrives, by which time a
    # "## Page N" marker may already have advanced `page` — so flushing with the live value mis-attributes the
    # last rule of every block that ends near a page boundary (verified against the rendered PDF: GR-7.3.12.4-6
    # is printed on page 95 and was recorded as 96).
    rule_page = 0
    #: The printed in-clause GROUP LABEL in effect ("FORMAT 2", "SEQUENTIAL FILES", "ALL FORMATS") — the third way
    #: the standard scopes a rule, beside its clause and its words (kb/Work PB284). Reset at every block heading;
    #: `rule_band` is the label in effect where the rule STARTED, captured like `rule_page` because flush() runs
    #: when the NEXT ordinal arrives, after a following label has already moved `band`.
    band = rule_band = ""
    in_correction_note = False                   # inside a `> ⚠ **CORRECTED …` blockquote paragraph (R66)

    def flattened_tail(n: int, form: str) -> bool:
        """Is this column-0 ordinal the flattened TAIL of the nested list currently open in the rule's body?

        The precedence is `place()`'s, unchanged: a strict same-form continuation of the innermost open level is
        a real rule and wins outright (that is what keeps `GR3` with an indented `1.` to `3.` list from
        swallowing `4)`), and only then does an ordinal that continues the indented run count as the tail.
        """
        if not levels or not last_indented:
            return False
        form0, _sublist, last = levels[-1]
        if form == form0 and n == last + 1:
            return False
        return n == last_indented + 1

    def explained(n: int, form: str) -> bool:
        """Does one of `place`'s cases file this column-0 ordinal: a continuation of the innermost level, a same-form
        restart, a continuation of any open level, or a list's opening?"""
        if not levels:
            return True
        top = levels[-1]
        return ((form == top[0] and n <= top[2] + 1) or any(n == lv[2] + 1 for lv in levels) or n == 1)

    def place(n: int, form: str) -> int:
        """File a column-0 ordinal on the level stack (one `explained` admits); returns the sub-list index for its id."""
        nonlocal levels, next_sublist, last_indented
        if not levels:
            levels = [[form, 1, n]]
            last_indented = 0
            return 1
        top = levels[-1]
        # 1. strict same-form continuation of the innermost open level
        if form == top[0] and n == top[2] + 1:
            top[2] = n
            last_indented = 0
            return top[1]
        # 2. (was: the flattened tail of an indented nested list — now handled by `flattened_tail` in the scan
        #    loop, which folds it into the parent rule instead of giving the fragment a row. PB698.)
        # 3. a same-form restart — a new sub-list, nested on the stack so its parent's run stays open
        if form == top[0] and n <= top[2]:
            levels.append([form, next_sublist, n])
            next_sublist += 1
            last_indented = 0
            return levels[-1][1]
        # 4. an ordinal that strictly continues an OPEN level's run closes every level above it and continues
        #    it — the form may drift (§2023:13.18.40.3). Form-matched levels win over form-drifted ones (§2023:9.3.6).
        for form_matched in (True, False):
            for i in range(len(levels) - 1, -1, -1):
                if n == levels[i][2] + 1 and (levels[i][0] == form) == form_matched:
                    del levels[i + 1:]
                    levels[i][0] = form
                    levels[i][2] = n
                    last_indented = 0
                    return levels[i][1]
        # 5. an opening ordinal — a new nest
        if n == 1:
            levels.append([form, next_sublist, n])
            next_sublist += 1
            last_indented = 0
            return levels[-1][1]
        raise AssertionError(f"place({n}{form}) was given an ordinal `explained` refuses")

    def flush() -> None:
        nonlocal buf, ordinal
        if cur and ordinal and (text := " ".join(x.strip() for x in buf).strip()):
            sec, kind = cur
            if cur_kinds is not None:
                kind = rule_kind(cur_kinds, cur_sublist, ordinal)
                if kind is None:
                    # A sub-list or item the map does not know is a manifest/segmentation DISAGREEMENT —
                    # writing a guessed kind would be the silent mis-typing the dict form exists to prevent.
                    sys.exit(f"FATAL: {sec} sub-list {cur_sublist} ordinal {ordinal} has no kind in its "
                             f"per-sublist/per-item manifest entry ({json.dumps(cur_kinds)}). Re-adjudicate "
                             f"the clause against the current segmentation before regenerating.")
            row = {"id": ed.rule_id(kind, sec, ordinal, cur_sublist)}
            if ed.edition_rows:
                row["citation"] = ed.citation(sec, kind, ordinal, cur_sublist)
            row |= {
                "section": sec,
                "kind": kind,
                "ordinal": ordinal,
                "sublist": cur_sublist,
                "subject": subject_for(sec, titles),
                "text": re.sub(r"\s+", " ", text),
            }
            if ed.edition_rows:
                row["page"] = rule_page
            if rule_band:
                row["band"] = rule_band
                if numbers := band_formats(rule_band):
                    row["band-formats"] = numbers
            rules.append(row)
            blocks_with_rules.add(cur)
        buf, ordinal = [], 0

    for line_no, line in enumerate(lines, start=1):
        if m := PAGE.match(line):
            page = int(m.group(1))
            continue
        # ANY heading closes the current rule block. Terminating only on a NUMBERED heading let §2023:16.2.2.2 — the
        # last rule block in the standard's body — run straight through the un-numbered annex headings ("## Annex
        # A (normative)"), turning every "N." line in Annexes A-F into a fake rule: 601 phantoms in one block.
        # A block that never ends is as wrong as a block never seen, and inflates the denominator instead of
        # shortening it, which is the more dangerous direction because it looks like thoroughness.
        if running.match(line) or furn.match(line):
            continue
        # ⛔ AN IN-CLAUSE GROUP LABEL IS NOT A BLOCK BOUNDARY AND IS NOT RULE TEXT (fix-queue PB689). It is
        # skipped in ONE place, before both the heading branch and the buffering branch, so the truncation and
        # the contamination cannot come back independently. Only inside an open block: outside one, an all-caps
        # heading is a document division ("## BIBLIOGRAPHY") and must keep terminating nothing.
        if cur is not None and (gl := GROUP_LABEL.match(line)):
            if line.lstrip().startswith("#"):
                find("heading-label", ed.label(cur[0]), line_no,
                     f"the group label '{gl.group('label')}' is a Markdown heading; the printed standard sets it in "
                     "body type: demote it to a plain line")
            band = gl.group("label").strip()     # the band the NEXT rule is printed under (a field, never text)
            continue
        if ANY_HEADING.match(line) and not heading.match(line):
            flush()
            cur = None
            continue
        if (m := heading.match(line)) and not line.lstrip().startswith("["):
            flush()
            num, title = m.group("num"), m.group("title")
            # ⛔ A CLAUSE MAY BE A RULE BLOCK WITHOUT ITS HEADING SAYING SO, AND THAT IS DECLARED IN DATA, NOT
            # HERE (fix-queue PB29). §2023:8.8.1.2 is headed "Native, standard-binary, and standard-decimal
            # arithmetic" and carries seven numbered rules, two of which PB28 proved the compiler violates —
            # §2023:5.3.1 settles that they ARE rules ("Except for intrinsic functions, rules are categorized as
            # syntax rules and general rules", validated with cite.py). Adding such titles to KINDS would be a
            # hand-maintained list of prose headings, which is the shape this project keeps deleting; the
            # ADMITTED set lives in the edition's content-keyed manifest beside the reason each was admitted, and
            # the same file's `pending` rows are the worklist of clauses nobody has judged yet.
            # An EXCLUDED block is outside the denominator whatever its title types it as: §2023:5.3 and §2002:5.2,
            # headed "Rules", define the taxonomy and carry no rule (KINDS types a bare "Rules" heading, PB5332).
            k = None if num in ed.excluded_blocks else kind_of(title) or admitted.get(num)
            levels, next_sublist, last_indented, cur_sublist = [], 2, 0, 1
            band = ""
            if isinstance(k, dict):
                # Per-sublist kinds: cur carries a hashable token for the seen/with-rules bookkeeping; the
                # real kind is resolved per rule at flush() from cur_kinds.
                cur, cur_kinds = (num, "MIXED"), k
                blocks_seen.append(cur)
            elif k:
                cur, cur_kinds = (num, k), None
                blocks_seen.append(cur)
            else:
                cur, cur_kinds = None, None
            if cur is not None:
                block_line.setdefault(cur, line_no)
            continue
        if cur is None:
            continue
        # ⛔ AN EDITORIAL CORRECTION NOTE IS NOT RULE TEXT (R66). `> ⚠ **CORRECTED — see the Addendum (Cn).**` flags a
        # line the transcription corrects and QUOTES THE PRINTED FORM so cite.py finds either; buffered into the rule it
        # would put the defective printed text inside the catalog's rule text, beside the corrected one. The note is a
        # blockquote paragraph: every `>` line after its first belongs to it.
        if CORRECTION_NOTE.match(line) or (in_correction_note and line.startswith(">")):
            in_correction_note = True
            continue
        in_correction_note = False
        if m := ORDINAL.match(line):
            n, form = int(m.group("n")), m.group("form")
            # ⛔ A FLATTENED NESTED-LIST TAIL IS PART OF ITS PARENT RULE, NOT A ROW (PB698). Decided BEFORE the
            # flush, because flushing is what would have ended the parent rule and started a new one.
            if ordinal and flattened_tail(n, form):
                find("flattened-tail", ed.label(cur[0]), line_no,
                     f"'{n}{form} {m.group('text')[:80]}' continues the indented list above it at column 0: folded into "
                     "its parent rule; indent it to its printed level (cite.py reports its path wrong until then)")
                last_indented = n
                buf.append(line)
                continue
            if ordinal and not explained(n, form):
                find("unexplained-ordinal", ed.label(cur[0]), line_no,
                     f"'{n}{form} {m.group('text')[:80]}' continues no open list (levels "
                     f"{[(f, s, l) for f, s, l in levels]}, indented run {last_indented}): kept in the text of rule "
                     f"{ordinal}, never a row of its own")
                buf.append(line)
                continue
            flush()
            cur_sublist = place(n, form)
            ordinal = n
            rule_page = page
            rule_band = band
            buf = [m.group("text")]
        elif ordinal:
            if im := INDENTED_ORDINAL.match(line):
                last_indented = int(im.group("n"))
            buf.append(line)
    flush()

    # ---- PROSE concept clauses — one row per normative paragraph (kb/Work PB284 part 3) -------------------------
    # See `prose_paragraphs`. Every admission is checked against the transcription HERE, so a clause that was
    # renumbered, retitled or turned into a numbered rule block is LOUD (fatal under --check) instead of a row
    # family quietly pointing at nothing — or at rows the numbered scan already holds, which would count twice.
    paragraph_defects: list[str] = []
    heading_at = {m.group("num"): i for i, ln in enumerate(lines)
                  if (m := heading.match(ln)) and not ln.lstrip().startswith("[")}
    for sec, info in paragraph_blocks.items():
        kind = info.get("kind")
        at = heading_at.get(sec)
        defect = ""
        if kind not in ("SR", "GR"):
            defect = (f"{ed.label(sec)}: kind {kind!r} is not SR or GR (§2023:5.3.1: rules are syntax rules and "
                      "general rules)")
        elif at is None:
            defect = f"{ed.label(sec)}: no such numbered heading in the transcription"
        elif (kind_of(titles[sec]) or sec in admitted) and any(r["section"] == sec for r in rules):
            # A TYPED block whose rule is printed UNNUMBERED is admitted like any prose clause (kb/Work PB5332):
            # §2002:8.8.4.1.5.2 'Rules' prints its one rule as a paragraph, where §2023:8.8.4.6.3 numbers it 1). Only a
            # block whose numbered scan already yields rows would count twice.
            defect = f"{ed.label(sec)}: already a numbered rule block ({titles[sec]!r}) — its rows would count twice"
        else:
            paras = prose_paragraphs(ed, lines, at)
            overrides = info.get("kinds", {})
            if not paras:
                defect = f"{ed.label(sec)}: admitted as prose but carries no paragraph"
            if unknown := sorted(set(overrides) - {str(i) for i in range(1, len(paras) + 1)}):
                defect = f"{ed.label(sec)}: per-paragraph kind for paragraph(s) {unknown} the clause does not have"
            for n, text in enumerate(paras, start=1):
                k = overrides.get(str(n), kind)
                row = {"id": ed.rule_id(k, sec, f"p{n}")}
                if ed.edition_rows:
                    row["citation"] = ed.citation(sec)
                row |= {"section": sec, "kind": k, "ordinal": n, "sublist": 1, "subject": subject_for(sec, titles),
                        "shape": "paragraph", "text": text}
                if ed.edition_rows:
                    row["page"] = pages[at]
                rules.append(row)
        if defect:
            paragraph_defects.append(defect)
            find("paragraph-admission", ed.label(sec), (at + 1) if at is not None else 0, defect)

    # ---- completeness self-check -----------------------------------------------------------------------------
    # The CONVENTIONS clause's sub-clauses (§2023:5.3.2-5.3.5, §2002:5.2.1-5.2.4) are prose DEFINING what a
    # syntax/general/argument/returned-value rule is. They are rule-block headings that correctly contain no rules.
    # Every other empty block is a parse gap.
    # A typed block whose rule is printed unnumbered holds its paragraph rows instead (paragraph-blocks, above).
    prose_rows = {r["section"] for r in rules if r.get("shape") == "paragraph"}
    gaps = [b for b in blocks_seen if b not in blocks_with_rules and b[0] not in ed.conventions
            and b[0] not in prose_rows]

    # ---- General formats — the SYNTAX DIAGRAMS ---------------------------------------------------------------
    # These are the artifacts the ANTLR grammar was written FROM, and the ones transcription loss damages most
    # (a dropped choice-indicator bar makes legal source look illegal). They carry no numbered ordinals, so the
    # rule scan never saw them and the denominator omitted all 320. Each becomes one FMT row: the unit of
    # "verify this diagram against the grammar rule that implements it".
    #
    # ⛔ A ROW IS A FORMAT THE CLAUSE PRINTS, NOT A HEADING THAT SAYS "General formats" (kb/Work PB1536 Q2). The
    # scan used to emit a row for ANY heading so titled, and §2023:5.2 — the description-techniques clause ABOUT
    # general formats, whose only body is its own sub-clause §2023:5.2.1 — manufactured FMT-5.2: a GAP row whose
    # "text" was a bare anchor, which no grammar rule could ever implement. So a candidate heading yields a row
    # only when its body carries what §2023:5.2.1 says a general format IS: a diagram, or the numbered-and-named
    # "Format N" labels of a multi-format construct ("the general format is separated into multiple formats that
    # are numbered and named" — cite.py --check 5.2.1 → OK). A candidate without either is one of two shapes,
    # and they are told apart structurally rather than by a list of section numbers:
    #   · a PARENT clause — its next numbered heading is its own sub-clause (§2023:5.2 → §2023:5.2.1): the clause is a
    #     topic, not a construct's format. Skipped, and printed, so the exclusion is an observation.
    #   · a LEAF whose diagram the TRANSCRIPTION LOST (§2023:15.78.2 REVERSE printed its format as bare text until
    #     wave 60): still a row — dropping it would shrink the denominator silently — and a LOUD self-check
    #     failure (`--check` exits 1), because the grammar cannot be verified against a diagram that is not there.
    # `CatalogCoverageDriftTests.EveryFmtRow_CarriesADiagramOrAFormatLabel` holds the same bar over the written
    # catalog, independently of this code. A page marker inside the body (`## Page N`, which 2002's transcription
    # keeps) is no part of the format.
    fmt_sections: list[tuple[str, int]] = []   # (section, heading-line)
    unrecognised_general: list[tuple[str, str]] = []   # headings opening "General" that name no known block kind
    numbered_at: list[tuple[int, str]] = []    # every numbered heading, in order — to see a candidate's next one
    for i, line in enumerate(lines):
        if (m := heading.match(line)) and not line.lstrip().startswith("[") and not PAGE.match(line):
            numbered_at.append((i, m.group("num")))
            if GENERAL_FORMAT_TITLE.fullmatch(m.group("title").strip()):
                fmt_sections.append((m.group("num"), i))
            elif is_unrecognised_general(m.group("title")):
                unrecognised_general.append((m.group("num"), m.group("title").strip()))
                find("unrecognised-general", ed.label(m.group("num")), i + 1,
                     f"'{m.group('title').strip()}' opens with 'General' and names no block this extractor knows")
    next_numbered = {at: nxt for (at, _), (_, nxt) in zip(numbered_at, numbered_at[1:])}

    fmt_parents: list[str] = []     # candidate headings that are clauses ABOUT formats, not a construct's format
    fmt_undrawn: list[str] = []     # leaf format clauses whose transcription carries no diagram — a repair
    for sec, at in fmt_sections:
        body: list[str] = []
        for line in lines[at + 1:]:
            if ANY_HEADING.match(line) and not running.match(line) and not PAGE.match(line):
                break
            if not PAGE.match(line):
                body.append(line)
        text = "\n".join(body).strip()
        has_diagram = bool(FMT_DIAGRAM.search(text))
        labels = len(FMT_LABEL.findall(text))
        if not has_diagram and not labels:
            if next_numbered.get(at, "").startswith(sec + "."):
                fmt_parents.append(sec)
                continue
            fmt_undrawn.append(sec)
            find("undrawn-format", ed.label(sec), at + 1,
                 f"the general format of {subject_for(sec, titles)} carries no diagram and no Format label: render "
                 "the printed page and restore it")
        row = {"id": ed.rule_id("FMT", sec, 0)}
        if ed.edition_rows:
            row["citation"] = ed.citation(sec)
        row |= {
            "section": sec, "kind": "FMT", "ordinal": 0, "sublist": 1,
            "subject": subject_for(sec, titles),
            # How many numbered Formats the section declares — Format 1 / Format 2 usually map to DIFFERENT
            # grammar alternatives, so this is a size signal for the verification work, not decoration.
            "formats": labels or 1,
            "has_diagram": has_diagram,
            "has_figure_notes": "Figure notes" in text,
            "text": re.sub(r"\s+", " ", text)[:4000],
        }
        if ed.edition_rows:
            row["page"] = pages[at]
        rules.append(row)

    # ---- The implementor-defined language element list (2023: Annex A.1; 2002: Annex B.1) --------------------
    # D13 defines "100% conforming" as the mandatory core complete PLUS every required implementor-documentation
    # item, so these ARE inventory rows and were missing from the denominator entirely. 2023 heads it
    # "### **A.1 ...**" — bold inside the heading, so it carries no leading digit and the numbered-heading scan
    # never saw it. Items are "N) <header> (<paraphrase>). This item is <required|optional|conditionally
    # required>. This item [, if provided ...,] shall be documented ... (<cross-reference>)".
    doc_heading = re.compile(rf"^#{{2,6}}\s+\*{{0,2}}{re.escape(ed.doc_list)}\b")
    a1_start = next((i for i, l in enumerate(lines) if doc_heading.match(l)), None)
    if a1_start is not None:
        buf, ordinal, rule_page = [], 0, pages[a1_start]

        def flush_a1() -> None:
            nonlocal buf, ordinal
            if ordinal and (text := re.sub(r"\s+", " ", " ".join(x.strip() for x in buf)).strip()):
                # Classify from the item's OWN words. Three items (28, 176, 200) do not use the standard
                # "This item is X" clause, and defaulting them to "required" silently inflated that bucket by
                # exactly 3 — the discrepancy against DEVLOG 932's independent count. An item that states no
                # requirement is marked UNCLASSIFIED and surfaced, never bucketed by assumption.
                low = text.lower()
                if "conditionally required" in low or "this item is conditional" in low:
                    req = "conditionally required"
                elif "this item is optional" in low or "feature is optional" in low:
                    req = "optional"
                elif "this item is required" in low:
                    req = "required"
                else:
                    req = "unclassified"
                row = {"id": ed.rule_id("DOC", ed.doc_list, ordinal)}
                if ed.edition_rows:
                    row["citation"] = ed.citation(ed.doc_list)
                row |= {"section": ed.doc_list, "kind": "DOC", "ordinal": ordinal, "sublist": 1,
                        "subject": text.split(".")[0][:120], "requirement": req,
                        "documented": "shall be documented" in low, "text": text}
                if ed.edition_rows:
                    row["page"] = rule_page
                rules.append(row)
            buf, ordinal = [], 0

        for i, line in enumerate(lines[a1_start + 1:], start=a1_start + 1):
            if PAGE.match(line):
                continue
            if running.match(line) or furn.match(line):
                continue
            if ANY_HEADING.match(line):          # the next annex clause (or any later heading) ends the list
                flush_a1()
                break
            if m := ORDINAL.match(line):
                flush_a1()
                ordinal, rule_page = int(m.group("n")), pages[i]
                buf = [m.group("text")]
            elif ordinal:
                buf.append(line)
        else:
            flush_a1()

    # ---- integrity critic: the transcription must contain the STANDARD, not a description of it -------------
    # Page 28 was found to hold an AI-generated SUMMARY of the Introduction plus a refusal message ("I can quote
    # specific short passages ... but I'm not able to reproduce the full page verbatim"), presented as spec
    # content. That is categorically worse than OCR loss: the text is not the standard at all, so any rule or
    # citation derived from it is unfounded, and it destroyed the sentence naming which annexes are NORMATIVE.
    # A whole-file sweep found this on exactly one page — this check exists so it stays that way.
    #
    # LOUD FAILURE IS THE RULE. A page that cannot be transcribed verbatim is never summarised — it carries the
    # sanctioned marker `<!-- TRANSCRIPTION-FAILED: page N — reason -->` and nothing else. That marker is
    # greppable, counted, and impossible to mistake for the standard, whereas a summary READS like content, which
    # is exactly why page 28's hole survived for months. A known missing page is recoverable; a page quietly
    # replaced by prose ABOUT the page is not. (DESIGN §1.4 applied to the spec itself.) Every hit is a finding: the
    # catalog stays regenerable while the reconciliation is still being collected, and `--check` refuses to call a
    # reconciled transcription sound while any page of it is not the standard.
    for i, line in enumerate(lines, start=1):
        for pat, label in META:
            if pat.search(line):
                find("non-spec-text", f"PDF page {pages[i - 1]}", i, f"[{label}] {line.strip()[:100]}")
                break

    # ---- completeness critic: the spec's own TOC ------------------------------------------------------------
    # The empty-block check has a BLIND SPOT — it can only verify blocks it RECOGNISED. A block whose heading was
    # never matched (an unhandled title form, or a heading a page break demoted to bold text and stripped of its
    # anchor, which has happened in this transcription) is invisible to it. The TOC is an independent witness of
    # what the printed standard contains, so cross-check against it.
    # 2023's TOC links every rule block; 2002's prints the clauses to the third level only (six rule blocks among
    # them), so its witness is every clause it lists, before its list of tables: each must be a heading of the
    # transcription with the TOC's own title.
    end = next((i for i, ln in enumerate(lines) if TOC_TABLES.match(ln)), len(lines)) if ed.toc_lists == "clauses" \
        else len(lines)
    text_all = "\n".join(lines[:end])
    starts = list(itertools.accumulate((len(ln) + 1 for ln in lines[:end - 1]), initial=0))
    toc = {(m.group(1), re.sub(r"\s+", " ", m.group(2)).strip().lower().rstrip("."),
            bisect.bisect_right(starts, m.start())) for m in ed.toc.finditer(text_all)}
    seen = ({(n, (titles.get(n) or "").strip().lower().rstrip(".")) for n, _ in blocks_seen}
            if ed.toc_lists == "rule blocks" else
            {(n, re.sub(r"\s+", " ", t).strip().lower().rstrip(".")) for n, t in titles.items()})
    unseen = sorted(((n, k, ln) for n, k, ln in toc if (n, k) not in seen and ed.heading.match(f"## {n} {k}")),
                    key=lambda x: [int(p) if p.isdigit() else -1 for p in x[0].split(".")])
    for n, k, ln in unseen:
        find("toc-unseen", ed.label(n), ln, f"the table of contents lists '{n} {k}', which the transcription never heads "
                                            f"as {'a rule block' if ed.toc_lists == 'rule blocks' else 'a clause'}")

    # Rule ids are the inventory's PRIMARY KEY — a duplicate silently carries the wrong verdict forward on every
    # refresh, and drops rules from any per-rule view. This is not a finding; it invalidates the catalog.
    dupes = [rid for rid, n in Counter(r["id"] for r in rules).items() if n > 1]
    if dupes:
        sys.exit(f"✗ FATAL: {len(dupes)} DUPLICATE rule id(s) — the inventory key is not unique: {dupes[:10]}")

    # ⛔ THE SEGMENTATION DRIFT GUARD (fix-queue PB39). Every column-0 ordinal must be EXPLAINED — a level
    # continuation, a nested-list tail behind an indented run, a same-form restart, or a list opening at 1.
    # An unexplained ordinal means the transcription carries a list shape the segmenter has never seen; guessing
    # at it is how 326 rules got ids the standard does not use. Zero on the whole 2023 transcription — proven to
    # fail by perturbation before being trusted — so any nonzero here is NEW evidence, not noise.
    unexplained = [f for f in findings if f.kind == "unexplained-ordinal"]
    if unexplained:
        say(f"\n⛔ {len(unexplained)} ordinal(s) the segmenter could not EXPLAIN — kept in the rule they interrupt:")
        say("\n".join(f"      {f}" for f in unexplained[:15]))
        say("   Read the clause (and the printed page): a defect of the transcription, or a list shape the segmenter "
            "has never seen.")

    # ⛔ THE TRANSCRIPTION DEFECT PB689 WAS BUILT ON, REPORTED RATHER THAN ABSORBED IN SILENCE. The extractor no
    # longer LOSES rules to a heading-ified group label, but a transcription that prints one is still wrong
    # against the page (body type, not a heading) and still misleads every human reader and every other tool
    # that keys on headings. Demote it to a plain line in the transcription.
    if labels_ := [f for f in findings if f.kind == "heading-label"]:
        say(f"\n⛔ {len(labels_)} in-clause GROUP LABEL(S) transcribed as a markdown HEADING — the shape that cost 60 "
            "rules in five clauses (PB689):")
        say("\n".join(f"      {f}" for f in labels_))

    # ⛔ THE SAME POSTURE FOR PB698's DEFECT: the extractor no longer lets a flattened nested-list tail become a
    # row, but a transcription that outdents one still makes `cite.py` report the WRONG printed path (§2023:14.9.41.3
    # SR6 b) 2. and 3. both resolved to "6) b) 1." while the printed page indents them under b)), and a wrong
    # citation path is the one thing CLAUDE.md rule 1 exists to prevent. Repair the indentation on the page.
    if tails := [f for f in findings if f.kind == "flattened-tail"]:
        say(f"\n⛔ {len(tails)} nested-list TAIL item(s) transcribed at COLUMN 0 — folded into the parent rule here (PB698):")
        say("\n".join(f"      {f}" for f in tails))

    # ⛔ A SUB-LIST IS A ROW FAMILY ONLY WHEN THE PRINTED PAGE SAYS SO (kb/Work PB55, "THE RULE IS THE UNIT").
    # A column-0 list that restarts at 1 is either a PEER list the standard prints at the block's own margin
    # (§2023:14.7.5's "The size error condition exists in the following cases:") — rules of their own — or a NESTED
    # list the transcription flattened, which is its parent rule's body. The markdown cannot tell the two apart:
    # 51 nested lists sat at column 0 and held 152 fragment rows until PB55 measured every one on the PDF. So
    # every sub-list must be RECORDED in the edition's sub-list data as measured at level 0; one that is not is a
    # flattened nested list (indent it at its printed level) or a new peer list (measure it with
    # `measure_sublist_levels.py --write`, which needs the PDF). The PDF never reaches CI; the measurement does.
    unmeasured = [r for r in sublist_openings(rules) if sublist_key(r["section"], r["text"]) not in measured_peers]
    for r in unmeasured:
        find("unmeasured-sublist", ed.label(r["section"]), 0,
             f"{r['id']} '{r['text'][:70]}': a flattened NESTED list (indent its items) or a peer list nobody has measured")
    if unmeasured:
        say(f"\n⛔ {len(unmeasured)} SUB-LIST(S) not MEASURED as printed at the block's own margin (PB55):")
        say("\n".join(f"      {r['id']}  '{r['text'][:70]}'" for r in unmeasured))
    else:
        say(f"✓ every sub-list is a measured peer list ({len(measured_peers)} recorded) — no nested list holds rows")

    expected_empty = [(s, k) for s, k in gaps if s in ed.known_empty_blocks]
    gaps = [(s, k) for s, k in gaps if s not in ed.known_empty_blocks]
    for sec, k in gaps:
        find("parse-gap", ed.label(sec), block_line[(sec, k)], f"the {k} block '{titles.get(sec, '?')}' yields no rule")

    # ⛔ THE CHECK THAT WOULD HAVE CAUGHT THE MISSING TEN. A parse gap is a block that was RECOGNISED and then
    # yielded nothing; a block whose heading spelling is unknown is never recognised, so it is never counted as a
    # gap and never appears in any total — the denominator is simply short, silently. This walks every numbered
    # heading whose title contains "rule" and reports the ones KINDS cannot resolve.
    unrecognised = sorted(
        ((num, title) for num, title in titles.items()
         if RULE_SHAPED.search(title) and kind_of(title) is None and num not in ed.excluded_blocks
         and num not in admitted),
        key=lambda kv: kv[0])
    for num, title in unrecognised:
        find("unrecognised-heading", ed.label(num), title_line[num],
             f"'{title}' names rules that no kind resolves: they are absent from the denominator. Type it in KINDS, or "
             "adjudicate the clause in the content-keyed manifest")

    # ⛔ THE CONTENT-KEYED CHECK — the one the two previous denominator corrections could not have made.
    # See unharvested_rule_blocks(). Measured against a COMMITTED MANIFEST rather than a remembered number, so a
    # newly-transcribed clause carrying rules is LOUD instead of silently absent from the denominator.
    measured = unharvested_rule_blocks(lines, titles, heading)
    report, _drifts = unharvested_report(measured, declared, ed.unharvested, f"§{ed.year}:")
    added, removed, changed = unharvested_drift(measured, declared)
    for s in added:
        find("unharvested-drift", ed.label(s), title_line.get(s, 0),
             f"'{measured[s]['title']}' carries {measured[s]['ordinals']} list ordinals the manifest does not declare")
    for s in removed:
        find("unharvested-drift", ed.label(s), title_line.get(s, 0), "declared in the manifest but no longer measured")
    for s in changed:
        find("unharvested-drift", ed.label(s), title_line.get(s, 0),
             f"declared with {declared[s]['ordinals']} ordinals, measured with {measured[s]['ordinals']}")

    # THE STANDING of each rule (kb/Work PB4993): archaic and obsolete by the edition's own lists, else normal, read from
    # the lists' data by the call the 1985 arm makes; a disagreement of the data with the text is a finding.
    standing_findings = element_standing.apply(rules, "section", lines, citation.iso_regions(lines),
                                               standing or ed.standing)
    for d in standing_findings:
        m = re.match(r"line (\d+): ", d)
        find("standing", f"§{ed.year}", int(m.group(1)) if m else 0, d)

    # A ROW IS CITED SO THAT ITS TEXT CAN BE CHECKED (kb/Work PB4886): every row's citation must name a region of the
    # transcription (`citation.validate`, the reader `cite.py --check` uses), and a citation names ONE rule (kb/Work
    # PB4969): two numbered rules sharing one would make either's verdict the other's.
    if ed.edition_rows and cite:
        for r in rules:
            if (why := citation.validate(citation.parse(r["citation"]))) is not None:
                find("uncitable", r["citation"], 0, f"{r['id']}: {why}")
        numbered = Counter(r["citation"] for r in rules if r["kind"] in citation.RULE_KINDS)
        for c, n in sorted(numbered.items()):
            if n > 1:
                find("shared-citation", c, 0,
                     f"{n} rows: {[r['id'] for r in rules if r.get('citation') == c][:4]}")

    by_kind = Counter(r["kind"] for r in rules)
    top = Counter(r["section"].split(".")[0] for r in rules)
    say(f"spec:            {ed.spec.relative_to(REPO)}  ({len(lines):,} lines)")
    say(f"rule blocks:     {len(blocks_seen)}")
    say(f"RULES EXTRACTED: {len(rules)}")
    say("")
    for k, label in (("SR", "syntax rules"), ("GR", "general rules"),
                     ("AR", "argument rules"), ("RV", "returned value rules"),
                     ("DOC", f"Annex {ed.doc_list} implementor-defined items"),
                     ("FMT", "general formats (syntax diagrams)")):
        if by_kind.get(k):
            say(f"   {by_kind[k]:5d}  {k}  {label}")
    say("")
    say("   by top-level clause:")
    # Annex clauses sort after numbered ones ("A.1" is not an int).
    for sec, n in sorted(top.items(), key=lambda kv: (0, int(kv[0])) if kv[0].isdigit() else (1, 0)):
        say(f"      §{sec:<3s} {n:5d}")
    for sec, _kind in expected_empty:
        say(f"\n· {ed.label(sec)} yielded 0 rules, as declared: {ed.known_empty_blocks[sec]}")
    if gaps:
        say(f"\n⚠ {len(gaps)} PARSE GAP(S) — rule-block headings that yielded ZERO rules:")
        say("\n".join(f"      {kind} {ed.label(sec)}  ({titles.get(sec, '?')})" for sec, kind in gaps))
    else:
        say("\n✓ no parse gaps — every rule-block heading yielded at least one rule")
    for num, why in sorted(ed.excluded_blocks.items()):
        say(f"\n· {ed.label(num)} '{titles.get(num, '?')}' is deliberately outside the denominator: {why}")
    if unrecognised:
        say(f"\n⛔ {len(unrecognised)} rule-shaped heading(s) the KINDS map does not recognise — their rules are ABSENT "
            "FROM THE DENOMINATOR:")
        say("\n".join(f"      {ed.label(num)}  '{title}'" for num, title in unrecognised))
    else:
        say("✓ every rule-shaped heading in the spec resolves to a known rule kind")
    say("\n".join(report))
    for sec in fmt_parents:
        say(f"\n· {ed.label(sec)} '{titles.get(sec, '?')}' is titled 'General format(s)' but is a PARENT clause (its body"
            " is its own sub-clauses, no diagram, no Format label) — a clause ABOUT formats, not a construct's format;"
            " no FMT row")
    say("✓ every general-format row carries a diagram or a Format label" if not fmt_undrawn else
        f"\n⛔ {len(fmt_undrawn)} general-format clause(s) whose transcription carries NO DIAGRAM: "
        + ", ".join(ed.label(s) for s in fmt_undrawn))
    say("✓ every heading that opens with 'General' is the intro clause, a general format or a rule block"
        if not unrecognised_general else
        f"\n⛔ {len(unrecognised_general)} heading(s) open with 'General' and name no block this extractor knows "
        "(kb/Work PB284): " + ", ".join(f"{ed.label(n)} '{t}'" for n, t in unrecognised_general))
    say(f"✓ {len(paragraph_blocks)} prose clause(s) admitted as paragraph rows, each present, unambiguous and "
        f"non-empty ({sum(1 for r in rules if r.get('shape') == 'paragraph')} rows)" if not paragraph_defects else
        f"\n⛔ {len(paragraph_defects)} PROSE-CLAUSE ADMISSION(S) the transcription no longer supports:\n      "
        + "\n      ".join(paragraph_defects))
    say("")
    for line in element_standing.tally(rules):
        say(line)
    say("✓ the archaic and obsolete lists' data agree with the text" if not standing_findings else
        f"⛔ {len(standing_findings)} disagreement(s) of the standing lists' data with the text "
        "(element_standing.findings):\n      " + "\n      ".join(standing_findings))
    pending = [s for s, v in declared.items() if v.get("disposition") == "pending"]
    return IsoCatalog(rules, len(blocks_seen), [{"section": s, "kind": k} for s, k in gaps], findings, say_lines, pending)


#: The catalog key that names what its count leaves out (`catalog_document`; read by `inventory_schema.Edition.outside`).
OUTSIDE = "outside-denominator"


#: Text inside the standard that is not the standard (the integrity critic in `extract`): the shapes a model's refusal or
#: summary takes, and the sanctioned marker of a page that could not be transcribed.
META = [
    (re.compile(r"\bI'?m not able to\b|\bI can quote\b|\bI'?m happy to help\b", re.I), "assistant refusal/offer"),
    (re.compile(r"here is a summary|summary and key excerpts|here are the key", re.I), "summary preamble"),
    (re.compile(r"\bcopyrighted standards document\b|\breproduce the full page\b", re.I), "copyright refusal"),
    (re.compile(r"<!--\s*TRANSCRIPTION-FAILED", re.I), "declared transcription gap (tracked, not hidden)"),
]


def findings_summary(findings: list[Finding]) -> list[str]:
    """The count of each kind of finding (`FINDING_KINDS`, in its order), then every finding."""
    by = Counter(f.kind for f in findings)
    out = [f"FINDINGS: {len(findings)}"]
    out += [f"   {by[k]:5d}  {k}: {why}" for k, why in FINDING_KINDS.items() if by[k]]
    out += [f"      {f}" for f in findings]
    return out


def catalog_document(ed: IsoEdition, cat: IsoCatalog) -> dict[str, Any]:
    """The committed catalog of `ed`: its source, its counts, its parse gaps, its findings (an unreconciled edition's;
    a reconciled one fails `--check` on any, so it records none) and its rows."""
    doc: dict[str, Any] = {"generated_from": ed.spec.relative_to(REPO).as_posix(), "rule_count": len(cat.rules),
                           "blocks": cat.blocks, "parse_gaps": cat.parse_gaps}
    if not ed.reconciled:
        doc["findings"] = [f.as_json() for f in cat.findings]
    # WHAT THE COUNT LEAVES OUT (kb/Work PB5256, branch review R1-5): the manifest's clauses awaiting a disposition and the
    # rule-shaped headings no kind types hold rules the denominator does not count yet, so `rule_count` is a lower bound
    # while either is non-empty, and every reader that prints the count prints this beside it (`Edition.outside`).
    untyped = [f.clause for f in cat.findings if f.kind == "unrecognised-heading"]
    if cat.pending or untyped:
        doc[OUTSIDE] = {"pending-clauses": [ed.label(s) for s in cat.pending], "untyped-headings": untyped}
    doc["rules"] = cat.rules
    return doc


def main_iso(ed: IsoEdition, args: argparse.Namespace) -> int:
    """THE ISO ARM, for every ISO edition (`ISO`): extract the catalog of `ed` (`extract`), print its report and its
    findings, and write it (or, under `--check`, compare it with the committed one). `--check` fails on a committed
    catalog this run does not reproduce, and, for a reconciled edition, on any finding."""
    if not ed.spec.exists():
        sys.exit(f"spec not found: {ed.spec}\nRun: git submodule update --init --recursive")
    lines = ed.spec.read_text(encoding="utf-8", errors="replace").splitlines()
    declared, paragraph_blocks = manifest_of(ed)
    cat = extract(ed, lines, declared, paragraph_blocks, measured_peers_of(ed))
    print("\n".join(cat.report))
    print()
    print("\n".join(findings_summary(cat.findings)))
    if not ed.reconciled and cat.findings:
        print(f"   ({ed.year}'s transcription is not reconciled yet (kb/Work PB4813): each finding is recorded in the "
              "catalog for the reconciliation, and --check fails only when they change)")

    # ⛔ THE COMMITTED CATALOG IS THIS RUN'S OUTPUT, OR IT IS STALE (kb/Work PB55). The inventory, every drift
    # test and every derived-verdict selector read the COMMITTED catalog, never the transcription; three landings
    # (PB1221, PB1374, PB351+PB1146+PB937) corrected general-format text in specs/ISO_COBOL_2023.md without regenerating it, and the
    # catalog carried the old text of FMT-7.3.15.2, FMT-13.18.35.2 and FMT-14.9.38.2 until PB55's rebuild found it.
    # `--check` compared the transcription with the manifests and never with the file it writes.
    doc = catalog_document(ed, cat)
    stale: list[str] = []
    if ed.catalog.exists():
        old = json.loads(ed.catalog.read_text(encoding="utf-8"))
        committed = {r["id"]: r for r in old["rules"]}
        fresh = {r["id"]: r for r in cat.rules}
        stale = sorted(i for i in committed.keys() | fresh.keys() if committed.get(i) != fresh.get(i))
        if old.get("findings") != doc.get("findings"):
            stale.append("(its findings)")
    if stale:
        print(f"\n⛔ THE COMMITTED CATALOG IS STALE — {len(stale)} rule(s) differ from what the transcription yields now:")
        for rid in stale[:10]:
            print(f"      {rid}")
        print(f"   Regenerate it in the same change as the transcription edit: python scripts/spec/extract_rule_catalog.py"
              f"{'' if ed.year == 2023 else f' --edition {ed.year}'}")
        print("   (then python scripts/spec/inventory_view.py --check, which refuses any adjudication decided against changed text).")

    if args.check:
        return 1 if stale or (ed.reconciled and cat.findings) else 0
    ed.catalog.parent.mkdir(parents=True, exist_ok=True)
    ed.catalog.write_text(json.dumps(doc, indent=1, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    print(f"\nwrote {ed.catalog.relative_to(REPO)}  ({ed.catalog.stat().st_size / 1_048_576:.1f} MB)")
    return 0


def main_1985(args: argparse.Namespace) -> int:
    """The 1985 arm (kb/Work PB4886; DESIGN-spec-conformance-review.md §10): the 1985 document's own structure, read by
    rule_catalog_1985.py. `--check` prints the counts and the findings and writes nothing; otherwise the catalog is
    written to its schema path. Either way a committed catalog that differs from this run is reported, and fails `--check`."""
    import rule_catalog_1985  # noqa: PLC0415  (it imports this module's heading rules; only the 1985 arm needs it)
    out = catalog_path(1985)
    spec = spec_pdf.markdown(1985)
    if not spec.exists():
        sys.exit(f"spec not found: {spec}")
    lines = spec.read_text(encoding="utf-8").splitlines()
    cat = rule_catalog_1985.parse(lines, manifest_blocks(UNHARVESTED_OF[1985]))
    rule_catalog_1985.cite_check(cat)
    cat.standing = element_standing.apply(cat.rules, "locator", lines, citation.regions_1985(lines), STANDING_OF[1985])
    print(f"spec:            {spec.relative_to(REPO)}  ({len(lines):,} lines)")
    for line in rule_catalog_1985.summary(cat):
        print(line)
    stale = False
    if out.exists():
        stale = json.loads(out.read_text(encoding="utf-8"))["rules"] != cat.rules
        if stale:
            print(f"⛔ THE COMMITTED 1985 CATALOG IS STALE: {out.relative_to(REPO)} differs from this run; regenerate it")
    if args.check:
        return 1 if cat.findings() or stale else 0
    out.write_text(json.dumps({"generated_from": spec.relative_to(REPO).as_posix(), "rule_count": len(cat.rules),
                               "blocks": len(cat.blocks), "rules": cat.rules}, indent=1, ensure_ascii=False) + "\n",
                   encoding="utf-8", newline="\n")
    print(f"\nwrote {out.relative_to(REPO)}")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="parse only; exit non-zero if the committed catalog is stale "
                                                         "or a reconciled edition's text yields a finding")
    ap.add_argument("--self-test", action="store_true", help="the ISO arm on planted text, a correction applied")
    ap.add_argument("--held", action="store_true",
                    help="every edition the inventory schema HOLDS (inventory_schema.HELD), oldest first: the gate's "
                         "RULE CATALOGS audit, so a newly held edition needs no new audit line")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    if args.self_test:
        return self_test()
    arms = {year: functools.partial(main_iso, ed) for year, ed in ISO.items()} | {1985: main_1985}
    if not args.held:
        return spec_pdf.edition_data(arms, args.edition, "rule catalog arm")(args)
    verdicts = {}
    for year in inventory_schema.HELD:
        print(f"\n════ the {year} rule catalog ════")
        verdicts[year] = spec_pdf.edition_data(arms, year, "rule catalog arm")(args)
    print(f"\n=== RULE CATALOGS: {'GREEN' if not any(verdicts.values()) else 'RED'} — "
          + " · ".join(f"{y} {'ok' if v == 0 else 'RED'}" for y, v in verdicts.items()) + " ===")
    return 1 if any(verdicts.values()) else 0


# ── SELF-TEST: the ISO arm on planted text, and the stability rule under a planted correction ────────────────────────

PLANTED_2002 = """## Page 5

14.8.90 FROB statement . . . 278\\
A.9 Data division . . . 655

### Tables

1 Verbal forms . . . 21

# Clauses 1 to 16

## Page 40

##### 5.2.1 Syntax rules

Syntax rules supplement general formats.

## Page 300

##### 14.8.90 FROB statement

###### 14.8.90.1 General format

<pre style="line-height:1">
<u>FROB</u> identifier-1
</pre>

###### 14.8.90.2 Syntax rules

1\\) Identifier-1 shall be numeric.

2\\) Identifier-1 shall not be a FROBNICATOR.

###### 14.8.90.3 General rules

1\\) The value of identifier-1 is frobbed.

## Page 301

2\\) The size error condition exists when it overflows the range 00 through

99\\. The value 00 is returned when nothing is frobbed.

3\\) Control passes to the next statement.

#### A.9 Data division

##### A.9.1 Syntax rules

1\\) A cd-name shall be unique.

### Annex B (normative) Language element lists

#### B.1 Implementor-defined language element list

1\\) FROB statement (the frob). This item is required. This item shall be documented in the implementor's user documentation. (14.8.90, FROB statement, general rule 1)

#### B.2 Undefined language element list
"""


def self_test() -> int:
    failures: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(f"{'PASS' if cond else 'FAIL'}  {what}")
        if not cond:
            failures.append(what)

    import tempfile  # noqa: PLC0415
    ed = ISO[2002]
    with tempfile.TemporaryDirectory() as tmp:
        standing = pathlib.Path(tmp) / "standing.json"
        standing.write_text(json.dumps({"lists": []}), encoding="utf-8", newline="\n")

        def run(text: str) -> IsoCatalog:
            return extract(ed, text.splitlines(), {}, {}, set(), standing, cite=False)

        before = run(PLANTED_2002)
        ids = [r["id"] for r in before.rules]
        by_id = {r["id"]: r for r in before.rules}
        ok(ids == ["2002-SR-14.8.90.2-1", "2002-SR-14.8.90.2-2", "2002-GR-14.8.90.3-1", "2002-GR-14.8.90.3-2",
                   "2002-GR-14.8.90.3-3", "2002-SR-A.9.1-1", "2002-FMT-14.8.90.1", "2002-DOC-B.1-1"],
           f"every rule, a rule-bearing annex's and the implementor-defined list's, with the year in its id (got {ids})")
        ok(by_id["2002-GR-14.8.90.3-2"]["citation"] == "§2002:14.8.90.3 GR2"
           and by_id["2002-FMT-14.8.90.1"]["citation"] == "§2002:14.8.90.1"
           and by_id["2002-DOC-B.1-1"]["citation"] == "§2002:B.1",
           "each row cites its rule in the edition form; a format and an implementor-defined item cite their clause")
        ok(by_id["2002-GR-14.8.90.3-2"]["page"] == 301 and by_id["2002-SR-14.8.90.2-1"]["page"] == 300,
           "each row carries the PDF page its rule starts on")
        ok("99" in by_id["2002-GR-14.8.90.3-2"]["text"] and not any("-99" in i for i in ids),
           "an ordinal no list explains is kept in the rule it interrupts and is never a row (§2002:15.16.2's '99.')")
        kinds = Counter(f.kind for f in before.findings)
        unexplained = [f for f in before.findings if f.kind == "unexplained-ordinal"]
        ok(kinds == Counter({"unexplained-ordinal": 1}) and unexplained[0].page == 301
           and unexplained[0].line == PLANTED_2002.splitlines().index("99\\. The value 00 is returned when nothing is "
                                                                       "frobbed.") + 1,
           f"the interrupting ordinal is a finding naming its line and its page, and nothing else is ({kinds})")
        ok(not any(r["section"].startswith("5.2") for r in before.rules) and not before.parse_gaps,
           "the conventions clause's rule-kind headings yield no row and no parse gap")

        # THE STABILITY RULE under a planted correction: the reconciliation re-joins the wrapped table cell, corrects a
        # misprint in a syntax rule and inserts a general rule the transcription lost. Every id whose clause and rule
        # number did not change survives with its text updated; the inserted rule renumbers only the rules after it in
        # its own block; the finding the correction resolved is gone.
        corrected = (PLANTED_2002
                     .replace("00 through\n\n99\\. The value", "00 through 99. The value")
                     .replace("a FROBNICATOR", "a FROBNICATOR-ITEM")
                     .replace("1\\) The value of identifier-1 is frobbed.",
                              "1\\) The value of identifier-1 is frobbed.\n\n2\\) The frob is atomic."))
        corrected = corrected.replace("2\\) The size error", "3\\) The size error").replace("3\\) Control", "4\\) Control")
        after = run(corrected)
        new = {r["id"]: r for r in after.rules}
        ok(not after.findings, "the correction resolves the finding: nothing is reported")
        ok(new["2002-SR-14.8.90.2-2"]["text"].endswith("a FROBNICATOR-ITEM.")
           and set(new) >= {i for i in ids if "14.8.90.3" not in i},
           "a corrected rule keeps its id and takes the corrected text; every id outside the corrected block survives")
        ok([i for i in new if "14.8.90.3" in i] == [f"2002-GR-14.8.90.3-{n}" for n in (1, 2, 3, 4)]
           and new["2002-GR-14.8.90.3-3"]["text"].startswith("The size error")
           and new["2002-GR-14.8.90.3-1"]["text"] == by_id["2002-GR-14.8.90.3-1"]["text"],
           "an inserted rule renumbers only the rules after it in its own block, as the printed numbers do")

    ok(ISO[2023].rule_id("GR", "14.9.8.4", 2) == "GR-14.9.8.4-2" and not ISO[2023].edition_rows,
       "2023's ids keep the shape its store is keyed by")
    ok(numbered_heading("A").match("###### A.1.2.3 Syntax rules") and not HEADING.match("###### A.1.2.3 Syntax rules"),
       "a rule-bearing annex's clause heads a block only in the edition that declares it")
    with tempfile.TemporaryDirectory() as tmp:
        standing = pathlib.Path(tmp) / "standing.json"
        standing.write_text(json.dumps({"lists": []}), encoding="utf-8", newline="\n")
        lost = PLANTED_2002.replace("A.9 Data division . . . 655", "A.9 Data division . . . 655\\\n14.8.91 FRAB statement . . . 279")
        unseen = [f for f in extract(ed, lost.splitlines(), {}, {}, set(), standing, cite=False).findings
                  if f.kind == "toc-unseen"]
        ok([(f.clause, f.line) for f in unseen] == [("§2002:14.8.91", 5)],
           f"a clause 2002's table of contents lists and the transcription never heads is a finding at its TOC line, and "
           f"the list of tables is no clause ({[str(f) for f in unseen]})")
        # kb/Work PB5332: 2002's 'Rounding rules' and bare 'Rules' headings are general rules; the taxonomy clause headed
        # 'Rules' stays out (excluded); a typed block whose one rule is printed unnumbered is admitted as a paragraph row.
        typed = (PLANTED_2002.replace("##### 5.2.1 Syntax rules", "#### 5.2 Rules\n\nRules are categorized.\n\n##### 5.2.1 Syntax rules")
                 + "\n## Page 302\n\n###### 8.8.1.3.1.3 Rounding rules\n\n1\\) When compared.\n\n2\\) When moved.\n\n"
                 "###### 8.8.4.1.5.2 Rules\n\nThe result of the test is true if the switch is set.\n\n"
                 "###### 8.8.4.1.6.2 Rules\n\n1\\) NOT and the next keyword specify one sign condition.\n")
        got = extract(ed, typed.splitlines(), {}, {"8.8.4.1.5.2": {"kind": "GR", "why": "planted"}}, set(), standing,
                      cite=False)
        tids = [r["id"] for r in got.rules if r["section"].startswith(("8.8", "5.2"))]
        ok(tids == ["2002-GR-8.8.1.3.1.3-1", "2002-GR-8.8.1.3.1.3-2", "2002-GR-8.8.4.1.6.2-1", "2002-GR-8.8.4.1.5.2-p1"]
           and not any(f.kind in ("unrecognised-heading", "parse-gap") for f in got.findings),
           f"'Rounding rules' and 'Rules' type as GR, the taxonomy clause 5.2 'Rules' stays out, and an unnumbered rule "
           f"of a typed block is one admitted paragraph row, with no finding ({tids}, {[str(f) for f in got.findings]})")
        doubled = extract(ed, typed.splitlines(), {}, {"8.8.4.1.6.2": {"kind": "GR", "why": "planted"}}, set(), standing,
                          cite=False)
        ok(any(f.kind == "paragraph-admission" and "count twice" in str(f) for f in doubled.findings),
           "a typed block whose numbered scan yields rows is refused as a paragraph admission (its rows would count twice)")
    print(f"=== extract_rule_catalog self-test: {'PASS' if not failures else f'FAIL ({len(failures)})'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
