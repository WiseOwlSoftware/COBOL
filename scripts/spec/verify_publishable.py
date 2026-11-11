#!/usr/bin/env python3
"""Pre-publication check for the spec transcription: no per-copy licence data, and the acknowledgment present.

WHY THIS EXISTS. `specs/ISO_COBOL_2023.md` is published; the PDF it was transcribed from is NOT. Two things therefore
have to be true of the Markdown before it leaves the private submodule, and neither is guaranteed by any other
gate (`--edition {1985,2002,2014,2023}`, default 2023, names the transcription; what is edition-specific, the stamp shapes
and the acknowledgment's wording, is keyed by edition below, and an edition with no row exits naming the table):

  1. **It carries no per-copy licence stamp.** Every page of the source PDF is stamped by the reseller with the
     purchaser's name, order number and download date — "Licensed to …. Single user license only. Copying and
     networking prohibited." That is not part of ISO/IEC 1989:2023: it is applied per purchaser, it is personally
     identifying, and its terms are the reseller's, not the standard's. The OCR pass picked it up once, at the
     end of the cover page, where it had been concatenated onto the genuine "© ISO/IEC 2023" line. One
     occurrence in 53,000 lines is exactly the kind of thing a human review misses and a grep does not.

  2. **The ISO acknowledgment opens the file.** Page 28 of the standard grants reproduction "in whole or in part
     … for any other purpose" and asks that the acknowledgment paragraphs be reproduced "in their entirety as
     part of the preface to any such publication". That request is the condition the transcription is published
     under.

  3. **A transcription that keeps its pages agrees with its reconciliation ledger** (`LEDGER`, 1985: kb/Work PB4850):
     every page of the book compared (`book_pages` the transcription's page count, `pages_swept` every page of it, no
     page in `pages_unread`), every repair on its page, every claim decided, and each page's prose carrying exactly the
     level-2 box sides its marked boxes account for (`check_ledger`).

  4. **Every gap in the printed folios holds a reconstruction, and every reconstruction stands in one** (owner
     2026-11-09, kb/Work PB4952): editorial text between two notes that name the folios, the opening one its sources
     and confidence, accepted as editorial, and refused once its printed page is transcribed (`check_reconstructions`).
     No tool-harness markup leaked into the text (`HARNESS_TAG`).

  5. **Its reserved-word list keeps the order the standard prints it in** (kb/Work PB5053, PB5060): every adjacent pair
     of words in order, and the printed blank between letter groups where the transcription keeps the columns. The
     rules, the per-edition readers and the pairs kept as printed are `reserved_word_order.py`'s; an edition whose
     list has not been swept (2002) has no row there and is named, not checked.

THE WHOLE TRACKED TREE, TOO (owner 2026-11-05: redact the stamp everywhere, history included). The repository is
public, and a reconciliation agent once quoted the stamp, purchaser's name and order number included, into its ledger.
`check_tree` therefore reads EVERY tracked file, as the index holds it, for the personally identifying pieces of a stamp:
an order number, a download date, and "Licensed to" followed by a name. It reads the index, not the working copy, so the
per-machine `.claude/settings.local.json` is judged by what a commit would publish. The files no landing may carry
(`coord.NEVER_WORK`, that settings file among them) are left out: a landing cannot change them, so no gate could ever turn
a finding there GREEN; their published copies were redacted by the history rewrite of 2026-11-05.

DELIBERATELY RUNS WITHOUT THE PDF. `verify_acknowledgment.py` checks the acknowledgment WORD FOR WORD against the
printed page, and needs the PDF to do it. This check needs to run in the public repository, where the PDF is
absent by design — so it verifies presence and structure only, and defers fidelity to the other gate. The two are
complements, not duplicates.

    python scripts/spec/verify_publishable.py [--edition 1985|2002|2014|2023]
    python scripts/spec/verify_publishable.py --self-test        # planted texts: no PDF, no tracked tree
"""
from __future__ import annotations

import argparse
import itertools
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from cite import CORRECTION_NOTE  # noqa: E402  — ONE definition of what an editorial correction note looks like
import cite  # noqa: E402  (THE reconstruction notes and their reader, kb/Work PB4952)
import citation  # noqa: E402  (THE printed folio of the 1985 structure)
import reserved_word_order  # noqa: E402  (THE order of a reserved-word list, kb/Work PB5060)
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402
import git_batch  # noqa: E402  (scripts/: one git process for many objects)

REPO = spec_pdf.REPO

#: The reseller's per-copy stamp of each edition, in pieces (SHAPES, never a licensee's name or an order number), so a
#: partial survival is caught as readily as the whole line. 1985: PB4812 records the stamp as "Licensed to ... order # ...";
#: the first assembly of the adopted run then showed the model leaving
#: the opening sentence and the licence-terms sentence behind on 14 pages (kb/Work PB4816, review R1-1), so those are pieces too.
#: 2002: read from the 2002 PDF's own text layer (kb/Work PB4813). It prints one stamp line, the same on all 882 pages:
#: the 1985 line's opening and licence-terms sentences, a store order reference whose number is longer than six digits
#: (so 2023's `X_` shape, which needs a word boundary after six, misses it), and 2023's download date. Each shape matches
#: that line on every page and no other line of the PDF.
#: 2014: read from the 2014 PDF's own text layer (kb/Work PB5268). It prints one stamp line, the same on all 952 pages, of
#: the 2002 line's shape: the reseller's opening sentence, `Licensed to` and a name, a store order reference whose number
#: has seven digits, a download date and the two licence-terms sentences. Each shape of `_INCITS_STORE_STAMP` and
#: `_INCITS_STORE_TREE` matches that line on every one of the 952 pages and no other line of the 2014 PDF (measured
#: 2026-11-11), so the two editions name the one shape set measured on both PDFs; neither row falls back to the other's.
_INCITS_STORE_STAMP: list[tuple[str, str]] = [
    (r"Copyright by INCITS", "per-copy licence stamp"),
    (r"Licensed to\b", "per-copy licence stamp"),
    (r"\border\s*#", "reseller order reference"),
    (r"\bX_\d{6,}\b", "reseller order number"),
    (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
    (r"Single user licen[cs]e", "per-copy licence stamp"),
    (r"Copying and networking", "per-copy licence stamp"),
]
_INCITS_STORE_TREE: list[tuple[str, str]] = [
    (r"\bX_\d{6,}\b", "reseller order number"),
    (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
    (r"Licensed to [A-Z][a-z]+ [A-Z][a-z]+", "per-copy licence stamp naming a purchaser"),
]
FORBIDDEN: dict[int, list[tuple[str, str]]] = {
    1985: [
        (r"Licensed to\b", "per-copy licence stamp"),
        (r"\border\s*#", "reseller order reference"),
        (r"Copyright by INCITS", "per-copy licence stamp"),
        (r"Single user licen[cs]e", "per-copy licence stamp"),
        (r"Copying and networking", "per-copy licence stamp"),
    ],
    2002: _INCITS_STORE_STAMP,
    2014: _INCITS_STORE_STAMP,
    2023: [
        (r"Licensed to\b", "per-copy licence stamp"),
        (r"Single user licen[cs]e", "per-copy licence stamp"),
        (r"Copying and networking", "per-copy licence stamp"),
        (r"ANSI order\b", "reseller order reference"),
        (r"\bX_\d{6}\b", "reseller order number"),
        (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
    ],
}

#: The personally identifying pieces of a stamp, searched in every tracked file (check_tree). Narrower than FORBIDDEN:
#: a tool that strips the stamp names "Licensed to" itself, and only a purchaser's NAME after it is a leak. The tree is one
#: set of files, so it is searched for the shapes of EVERY edition that has a row, on every run.
TREE_FORBIDDEN: dict[int, list[tuple[str, str]]] = {
    1985: [
        (r"\border\s*#\s*[A-Za-z_]*\d{4,}", "reseller order number"),
    ],
    2002: _INCITS_STORE_TREE,
    2014: _INCITS_STORE_TREE,
    2023: [
        (r"\bX_\d{6}\b", "reseller order number"),
        (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
        (r"Licensed to [A-Z][a-z]+ [A-Z][a-z]+", "per-copy licence stamp naming a purchaser"),
    ],
}


def tree_shapes() -> list[tuple[str, str]]:
    """Every edition's TREE_FORBIDDEN shapes, each once."""
    return list(dict.fromkeys(shape for edition in sorted(TREE_FORBIDDEN) for shape in TREE_FORBIDDEN[edition]))


def check_tree(repo: pathlib.Path = REPO) -> list[str]:
    """Every tracked file, as the index holds it, carries none of the tree shapes."""
    import subprocess
    print("\nper-copy licence data in ANY tracked file (the index) — must be ABSENT:")
    listed = subprocess.run(["git", "-C", str(repo), "ls-files", "-s", "-z"], capture_output=True, check=True).stdout
    entries = [e.split("\t", 1) for e in listed.decode("utf-8").split("\0") if e]
    sys.path.insert(0, str(repo / "scripts" / "orchestrator"))
    import coord  # noqa: E402  (the ONE list of the files no landing carries)
    blobs = {meta.split()[1]: path for meta, path in entries if not meta.startswith("160000") and coord.is_work(path)}
    failures = []
    pats = [(re.compile(p), what) for p, what in tree_shapes()]
    for sha, body in git_batch.blobs(repo, blobs):
        if b"\0" in body[:8000]:
            continue
        text = body.decode("utf-8", "replace")
        for rx, what in pats:
            if m := rx.search(text):
                line = text.count("\n", 0, m.start()) + 1
                failures.append(f"{what}: {blobs[sha]}:{line}")
    if failures:
        for f in failures[:20]:
            print(f"  ✗ {f}")
    else:
        print(f"  ✓ {len(blobs):,} tracked files, none carries an order number, a download date or a purchaser's name")
    return failures


# Must be present. The acknowledgment's own opening words, and the preface framing that marks our editorial
# matter as ours.
#: Keyed by edition: an earlier edition's acknowledgment and Preface are worded by its own text (the 2002 PDF prints no
#: CODASYL grant, PB4813), so its row is written from that text when its run begins, never borrowed from 2023's. 1985's
#: rows are its PDF page 3's words, which are 2023's: the CODASYL acknowledgment did not change (kb/Work PB4850).
REQUIRED: dict[int, list[tuple[str, str]]] = {
    1985: [
        (r"\A# Preface\n", "the file must open with the Preface (PDF page 3: 'as part of the preface')"),
        (r"COBOL is an industry language and is not the property of any company",
         "the acknowledgment's first paragraph"),
        (r"No warranty, expressed or implied, is made by any contributor",
         "the acknowledgment's no-warranty paragraph"),
        (r"have specifically authorized the use of this material in whole or in part",
         "the acknowledgment's authorization paragraph"),
        (r"NOT part of American National Standard X3\.23-1985",
         "the Preface must state that it is not part of the standard"),
        (r"^# Errata", "the Errata that lists every correction (check_corrections, kb/Work PB4972)"),
    ],
    2023: [
        (r"\A# Preface\n", "the file must open with the Preface (page 28: 'as part of the preface')"),
        (r"COBOL is an industry language and is not the property of any company",
         "the acknowledgment's first paragraph"),
        (r"No warranty, expressed or implied, is made by any contributor",
         "the acknowledgment's no-warranty paragraph"),
        (r"have specifically authorized the use of this material in whole or in part",
         "the acknowledgment's authorization paragraph"),
        (r"NOT part of ISO/IEC 1989:2023",
         "the Preface must state that it is not part of the standard"),
        (r"^# Addendum", "the Addendum that lists every correction (check_corrections)"),
    ],
    # 2014 prints the CODASYL grant and its paragraphs on its INCITS cover page, PDF page 2 (kb/Work PB5268), with the
    # printed defects the Preface keeps ("OI" for "or"). It has no corrections section yet: nothing is corrected.
    2014: [
        (r"\A# Preface\n", "the file must open with the Preface (PDF page 2: 'as part of the preface')"),
        (r"COBOL is an industry language and is not the property of any company",
         "the acknowledgment's first paragraph"),
        (r"No warranty, expressed or implied, is made by any contributor",
         "the acknowledgment's no-warranty paragraph"),
        (r"have specifically authorized the use of this material in whole or in part",
         "the acknowledgment's authorization sentence"),
        (r"NOT part of ISO/IEC 1989:2014",
         "the Preface must state that it is not part of the standard"),
    ],
    # 2002 prints no acknowledgment and no reproduction grant; it is reproduced under the permission the owner confirmed
    # on 2026-11-05 (kb/Work PB4813), which the Preface states without naming anyone.
    2002: [
        (r"\A# Preface\n", "the file must open with the Preface"),
        (r"NOT part of ISO/IEC 1989:2002", "the Preface must state that it is not part of the standard"),
        (r"reproduced with permission", "the Preface must state the permission the transcription is reproduced under"),
    ],
}

#: The words that open the acknowledgment, which must precede clause 1 (the Preface), by edition; None for an edition
#: that prints no acknowledgment (2002), whose Preface carries none.
ACKNOWLEDGMENT_OPENING: dict[int, str | None] = {1985: "COBOL is an industry language", 2002: None,
                                                 2014: "COBOL is an industry language",
                                                 2023: "COBOL is an industry language"}


#: The closing section that lists every correction a transcription makes to its printed standard, by edition (kb/Work R66,
#: PB4972). 1985 calls it the Errata because its text already has an Addendum of its own: Amendment 1, Addendum 1. Its
#: heading is `# <name>`, and each in-place flag says `see the <name> (Cn)`. An edition with no row may correct nothing.
CORRECTIONS_SECTION: dict[int, str] = {1985: "Errata", 2023: "Addendum"}
#: A correction entry: `### C6 · page 607 · 14.9.10.4 … · printed → corrected`. Its page is the edition's own page
#: reference: the printed folio in 2023, the PDF page (`## Page N`) in 1985, whose folios are per section (`XIII-56`).
ENTRY_HEADING = re.compile(r"^###\s+(C\d+)\s+·\s+(.*)$")


def corrections_section(edition: int) -> str:
    """The name of `edition`'s corrections section ("Addendum", "Errata"); "Addendum" for an edition with no row."""
    return CORRECTIONS_SECTION.get(edition, "Addendum")


def flag_reference(edition: int) -> re.Pattern:
    """An in-place flag's reference to its entry in `edition`'s corrections section: `see the Errata (C4)`."""
    return re.compile(rf"see the {corrections_section(edition)} \((C\d+)\)")
PAGES_IN_HEADING = re.compile(r"\bpages?\s+((?:\d+(?:,\s*|\s+and\s+)?)+)")
PAGE_IN_NOTE = re.compile(r"\bpage\s+(\d+)\b")
#: A correction that changes a CLASS of characters across the whole text is flagged once, in the Preface, and is
#: listed under this heading in place of a page (C14: the look-alike hyphens and minus signs).
WHOLE_TEXT = "the whole text"


def check_corrections(text: str, edition: int) -> list[str]:
    """Every correction is FLAGGED where it was made, LISTED in the edition's corrections section (the Addendum of
    2023, the Errata of 1985: CORRECTIONS_SECTION), and names its PRINTED FORM and PAGE.

    The section exists so a correction can be reversed if it later proves mistaken (specs/README.md "Corrections",
    kb/Work R66). That only works while three things hold, and each is checked here because each can be edited away
    independently of the others:

      1. the two sets agree — every `see the <section> (Cn)` flag has a `### Cn` entry and every entry is pointed at;
      2. every in-place flag is a `> ⚠ **CORRECTED — see the <section> (Cn).**` note that QUOTES THE PRINTED FORM in a code
         span and names the PRINTED PAGE, and that page is one the entry's heading lists — `cite.py --check` finds a
         quotation of the printed form in the note (and reports the corrected line's rule path, cite.rule_path), which
         is what keeps every citation written against the printed form passing;
      3. every entry's heading names the printed page(s) (or `the whole text` for a class of characters), and an entry
         other than such a class states its printed form.
    """
    failures: list[str] = []
    lines = text.split("\n")
    name, flag = corrections_section(edition), flag_reference(edition)
    addendum_at = next((i for i, l in enumerate(lines) if l.startswith(f"# {name}")), len(lines))
    print(f"\ncorrections — every flag must match an {name} entry that names the printed form and page:")

    # the section's entries, each with its body up to the next heading
    entries: dict[str, dict] = {}
    current = None
    for l in lines[addendum_at:]:
        if m := ENTRY_HEADING.match(l):
            current = entries.setdefault(m.group(1), {"heading": m.group(2), "body": []})
        elif l.startswith("## ") or l.startswith("# "):
            current = None
        elif current is not None:
            current["body"].append(l)

    # the flags in the body: each is a blockquote paragraph opening with the CORRECTED marker
    flags: dict[str, list[tuple[int, str]]] = {}
    i, fenced = 0, False
    while i < addendum_at:
        if lines[i].lstrip().startswith("```"):
            fenced = not fenced
        if CORRECTION_NOTE.match(lines[i]):
            j = i
            while j + 1 < addendum_at and lines[j + 1].startswith(">"):
                j += 1
            paragraph = " ".join(l.lstrip("> ").strip() for l in lines[i:j + 1])
            ids = flag.findall(paragraph)
            for cid in ids[:1]:
                flags.setdefault(cid, []).append((i + 1, paragraph))
            # A flag stands alone: a line right after it continues the blockquote (CommonMark's lazy continuation), so
            # the text it carries would render inside the note and drop out of its own paragraph (kb/Work PB4972 R2-1).
            # Inside a fenced block nothing is a blockquote, and the next line is the example's own code.
            if not fenced and j + 1 < addendum_at and lines[j + 1].strip():
                failures.append(f"the flag at line {i + 1} is followed by text with no blank line: Markdown reads "
                                f"{lines[j + 1].strip()[:40]!r} as part of the note")
            i = j
        i += 1
    referenced = set(flag.findall("\n".join(lines[:addendum_at])))

    if not entries and not referenced:
        # A transcription that corrects nothing flags none and lists none. One that has a corrections section requires
        # it by its REQUIRED row, so deleting the section together with its flags is still refused there.
        print("  ✓ no corrections: the text flags none and lists none")
    for cid in sorted(referenced | set(entries), key=lambda c: int(c[1:])):
        problems: list[str] = []
        entry = entries.get(cid)
        if cid not in entries:
            problems.append(f"referenced from the text, but MISSING from the {name}")
        elif cid not in referenced:
            problems.append(f"in the {name}, but never referenced from the text")
        else:
            heading, body = entry["heading"], "\n".join(entry["body"])
            is_class = WHOLE_TEXT in heading
            listed = {int(n) for n in re.findall(r"\d+", m.group(1))} if (m := PAGES_IN_HEADING.search(heading)) else set()
            if not listed and not is_class:
                problems.append("its heading names no printed page (`page N`) and is not a whole-text class")
            if not is_class and not re.search(r"\*\*Printed|printed form", body, re.I):
                problems.append("it does not state the printed form")
            if is_class and cid in flags:
                problems.append("a whole-text class is flagged once in the Preface, not per occurrence")
            if not is_class and cid not in flags:
                problems.append("its heading is not a whole-text class but no in-place flag carries it")
            for at, paragraph in flags.get(cid, []):
                pages = {int(n) for n in PAGE_IN_NOTE.findall(paragraph)}
                if not re.search(r"`[^`]+`", paragraph):
                    problems.append(f"the flag at line {at} quotes no printed form in a code span")
                if not pages:
                    problems.append(f"the flag at line {at} names no printed page")
                elif listed and not pages & listed:
                    # (a note may also cite the pages that CONFIRM the correction — C1 names three — but one must be
                    # a page the entry's heading lists, so the flag and the entry point at the same printed page)
                    problems.append(f"the flag at line {at} names page {sorted(pages)} but the entry lists {sorted(listed)}")
        if problems:
            for p in problems:
                failures.append(f"{cid}: {p}")
            print(f"  ✗ {cid}: " + "; ".join(problems))
        else:
            print(f"  ✓ {cid}: flagged in place, listed in the {name}, printed form and page named")
    return failures


#: The reconciliation ledger of a transcription that keeps its pages (`## Page N`), by edition (kb/Work PB4850): every
#: batch claim with its decision, every repair, and every level-2 box. 2023 dropped its pages; its reconciliation is
#: LEDGER.json (merge_reconciliation.py) and its corrections the Addendum (check_corrections).
LEDGER: dict[int, str] = {1985: "docs/rearchitecture/spec-reconciliation/LEDGER-1985.json",
                           2002: "docs/rearchitecture/spec-reconciliation/LEDGER-2002.json"}  # every claim decided (PB5332)
#: EVERY edition's reconciliation ledger, the one home of their paths: the page-keeping ledgers above, and each ledger
#: merge_reconciliation.py writes from its agent files (its EDITIONS rows take their ledger from here): 2023's
#: LEDGER.json, and 2002's and 2014's, in LEDGER-1985.json's shape (kb/Work PB5255).
RECONCILIATION: dict[int, str] = {
    **LEDGER,
    2023: "docs/rearchitecture/spec-reconciliation/LEDGER.json",
    2014: "docs/rearchitecture/spec-reconciliation/LEDGER-2014.json",
}


def ledger_pages(ledger: dict) -> set[int]:
    """The PDF pages a reconciliation ledger records as compared: 2023's merge lists them (`pages_swept`); a page-keeping
    batch ledger compared every page of its book and records how many (`pages`, LEDGER-1985.json's `about`)."""
    if "pages_swept" in ledger:
        return set(ledger["pages_swept"])
    return set(range(1, int(ledger.get("pages", 0)) + 1))


def reconciled(edition: int, repo: pathlib.Path = REPO) -> bool:
    """THE FACT that `edition`'s transcription is reconciled (kb/Work PB5256, branch review R1-4): its reconciliation ledger
    exists and records every page of the PDF (`spec_pdf.EDITIONS[edition].pages`) as compared. The ISO rule catalog
    records an unreconciled edition's findings instead of failing on them, and the inventory refuses to LINK an
    unreconciled edition into the lineage (`inventory_schema.link_errors`): its rule ids may still move."""
    path = repo / RECONCILIATION[edition] if edition in RECONCILIATION else None
    if path is None or not path.is_file():
        return False
    return ledger_pages(json.loads(path.read_text(encoding="utf-8"))) == set(range(1, spec_pdf.EDITIONS[edition].pages + 1))
#: What a claim's decision may be; each but APPLIED states its reason.
DECISIONS = {"APPLIED", "REFUTED", "ALREADY", "FORMAT", "BOX", "FIGURE", "KEPT"}
CODE_SPAN = re.compile(r"`[^`\n]*`")


def prose_box_sides(page: str) -> int:
    """The ║ of `page` outside a figure (`<pre>`), a fenced block and a code span: the sides of its prose boxes."""
    count, figure, fence = 0, False, False
    for line in page.split("\n"):
        if line.lstrip().startswith("```"):
            fence = not fence
            continue
        if "<pre" in line:
            figure = True
        if not figure and not fence:
            count += CODE_SPAN.sub("", line).count(spec_pdf.BOX_SIDE)
        if "</pre>" in line:
            figure = False
    return count


def check_ledger(text: str, ledger: dict) -> list[str]:
    """The transcription agrees with its reconciliation ledger: the ledger compared every page of the book (its
    `book_pages` is the transcription's page count, its `pages_swept` every one of them and its `pages_unread` none),
    every applied repair and sweep is on its page, every claim is decided, every marked box opens where the ledger says,
    and each page's prose carries exactly the box sides its marked boxes account for (two per marked block), so a box
    added, lost or moved without the ledger is refused."""
    failures: list[str] = []
    pages = spec_pdf.pages_of(text)
    book = spec_pdf.page_count(text)
    swept = set(ledger.get("pages_swept") or [])
    if ledger.get("book_pages") != book:
        failures.append(f"the ledger's book_pages {ledger.get('book_pages')!r} is not the transcription's {book} pages")
    if unswept := sorted(set(range(1, book + 1)) - swept):
        failures.append(f"{len(unswept)} page(s) were never compared (not in pages_swept): {unswept[:20]}")
    if beyond := sorted(swept - set(range(1, book + 1))):
        failures.append(f"pages_swept names page(s) the transcription does not hold: {beyond[:20]}")
    if unread := ledger.get("pages_unread"):
        failures.append(f"{len(unread)} page(s) are unread: {[u['page'] for u in unread][:20]}")
    print(f"\nreconciliation ledger ({len(ledger['claims'])} claims, {len(ledger['sweeps'])} sweeps, "
          f"{len(ledger['boxes'])} boxes) — the transcription must agree:")

    def on_page(page: int, now: str, what: str) -> None:
        if now not in pages.get(page, ""):
            failures.append(f"{what} (page {page}): {now[:70]!r} is not on the page")

    for c in ledger["claims"]:
        label = f"claim p{c['page']}-{c['i']}"
        if c.get("decision") not in DECISIONS:
            failures.append(f"{label}: decision {c.get('decision')!r} is not one of {sorted(DECISIONS)}")
        elif c["decision"] == "APPLIED":
            for e in c.get("edits") or [None]:
                if e is None:
                    failures.append(f"{label}: APPLIED with no edit")
                else:
                    on_page(c["page"], e["now"], label)
        elif not c.get("reason"):
            failures.append(f"{label}: {c['decision']} with no reason")
    for s in ledger["sweeps"]:
        on_page(s["page"], s["now"], f"sweep {s['sweep']}")
    sides: dict[int, int] = {}
    for b in ledger["boxes"]:
        if b["decision"] == "MARKED":
            sides[b["page"]] = sides.get(b["page"], 0) + 2 * b["blocks"]
            for opening in b["opens"]:
                on_page(b["page"], opening, f"box {b['n']}")
    for page, body in pages.items():
        found = prose_box_sides(body)
        if found != sides.get(page, 0):
            failures.append(f"page {page}: its prose carries {found} box sides (║) and its marked boxes account for "
                            f"{sides.get(page, 0)}")
    for f in failures[:20]:
        print(f"  ✗ {f}")
    if not failures:
        print(f"  ✓ {book} of {book} pages compared, every repair on its page, every claim decided, "
              f"{sum(sides.values()) // 2} boxed blocks where the ledger puts them")
    return failures


#: The printed folio of a transcription that keeps its pages, by edition: a folio set alone on its line (`XIII-29`), or
#: indented inside a figure (page 202's `V-10`).
FOLIOS: dict[int, re.Pattern] = {1985: citation.FOLIO_85}


def check_reconstructions(lines: list[str], edition: int) -> list[str]:
    """⛔ EVERY GAP IN THE PRINTED FOLIOS HOLDS A RECONSTRUCTION, AND EVERY RECONSTRUCTION STANDS IN A GAP (owner
    2026-11-09, kb/Work PB4952). Where the scan lacks printed pages the transcription holds editorial text between a
    `cite.RECONSTRUCTED` note and a `cite.RECONSTRUCTED_END` note naming the same folios; the opening note names its
    **Sources:** and its **Confidence:**, and each note stands alone (a line after it would render inside it). The folios
    a reconstruction names are exactly those the transcription's folio sequence skips where it stands, so a scan gap
    nobody filled is refused, and so is a reconstruction whose printed page has since been transcribed: its verbatim
    text replaces the reconstruction, and the rule catalog's provisional rows go with it."""
    failures: list[str] = []
    print("\nreconstructions — each fills exactly the printed folios the scan lacks where it stands:")
    recons = cite.reconstructions(lines)
    for r in recons:
        if r.start < 0:
            failures.append(f"line {r.end + 1}: a closing note (folios {r.closing}) with no opening note")
            continue
        if r.end is None:
            failures.append(f"line {r.start + 1}: the reconstruction of {r.folios} is never closed")
        elif cite.folios_of(r.closing) != cite.folios_of(r.folios):
            failures.append(f"line {r.start + 1}: the reconstruction of {r.folios} closes as {r.closing}")
        note = " ".join(itertools.takewhile(lambda ln: ln.startswith(">"), lines[r.start:]))   # its own paragraph
        if "**Sources:**" not in note or "**Confidence:" not in note:
            failures.append(f"line {r.start + 1}: the reconstruction of {r.folios} names no **Sources:** or no "
                            "**Confidence:**")
        for at in (r.start, r.end):
            if at is None:
                continue
            j = at
            while j + 1 < len(lines) and lines[j + 1].startswith(">"):
                j += 1
            if j + 1 < len(lines) and lines[j + 1].strip():
                failures.append(f"the note at line {at + 1} is followed by text with no blank line: Markdown reads "
                                f"{lines[j + 1].strip()[:40]!r} as part of the note")
    folio = FOLIOS.get(edition)
    if folio is not None:
        # A page's folio is the LAST folio-shaped line of the page (`## Page N`): an Index entry's continuation line
        # (`      VI-73`) is folio-shaped too, and every page that prints a folio prints it last.
        printed: list[tuple[int, str]] = []
        last: tuple[int, str] | None = None
        for i, ln in enumerate(lines + ["## Page 0"]):
            if spec_pdf.PAGE_HEADING.match(ln):
                if last is not None:
                    printed.append(last)
                last = None
            elif (m := folio.match(ln.strip())) is not None:
                last = (i, m.group(1))
        placed: set[int] = set()
        for (i, a), (j, b) in zip(printed, printed[1:]):
            (sa, na), (sb, nb) = a.rsplit("-", 1), b.rsplit("-", 1)
            between = [r for r in recons if i < r.start < j]
            placed.update(r.start for r in between)
            missing = [f"{sa}-{k}" for k in range(int(na) + 1, int(nb))] if sa == sb and int(nb) > int(na) else []
            named = [f for r in between for f in cite.folios_of(r.folios)]
            if missing != named:
                failures.append(f"between folios {a} (line {i + 1}) and {b} (line {j + 1}) the printed folios "
                                f"{missing or 'none'} are missing and the reconstructions there stand for "
                                f"{named or 'none'}" + (" (the printed page is transcribed: replace the reconstruction "
                                                        "with it)" if named and not missing else ""))
        for r in recons:
            if r.start >= 0 and r.start not in placed:
                failures.append(f"line {r.start + 1}: the reconstruction of {r.folios} stands between no two folios")
    for f in failures:
        print(f"  ✗ {f}")
    if not failures:
        print(f"  ✓ {len(recons)} reconstruction(s), each closed, sourced and filling the folios the scan lacks"
              + ("" if folio is not None else " (this edition keeps no folios)"))
    return failures


#: Markup of an agent's tool harness that leaked into a transcription (page 894 of the 1985 file carried an
#: `<invoking-skill>` pair around the RANDOM function, which two catalog rows read as rule text): a tag whose name joins
#: words with a hyphen or an underscore. Neither standard's own notation (`<storage location>`) nor HTML has one.
HARNESS_TAG = re.compile(r"</?[a-z]+(?:[-_][a-z]+)+>")


def check_stamp(lines: list[str], edition: int) -> list[str]:
    """The stamp shapes of `edition` must be absent from `lines`; an edition the table lacks exits naming the table."""
    forbidden = spec_pdf.edition_data(FORBIDDEN, edition, "per-copy stamp shapes (verify_publishable.FORBIDDEN)")
    failures = []
    print("per-copy licence data — must be ABSENT:")
    for pat, what in forbidden:
        hits = [i + 1 for i, l in enumerate(lines) if re.search(pat, l)]
        if hits:
            failures.append(f"{what} ({pat}) at line(s) {hits[:5]}")
            print(f"  ✗ {pat:<34} FOUND at {hits[:5]}")
        else:
            print(f"  ✓ {pat:<34} absent")
    return failures


def check_text(text: str, edition: int) -> list[str]:
    """The checks that read one transcription's text and nothing else: the stamp shapes of `edition` must be absent, its
    acknowledgment and Preface present, and the acknowledgment before the body. Each row is keyed by edition; an
    edition the table lacks exits naming the table (spec_pdf.edition_data)."""
    failures = check_stamp(text.splitlines(), edition)
    if leaked := [i + 1 for i, line in enumerate(text.splitlines()) if HARNESS_TAG.search(line)]:
        failures.append(f"tool-harness markup (a hyphenated or underscored tag) at line(s) {leaked[:5]}")
        print(f"  ✗ tool-harness markup FOUND at {leaked[:5]}")
    required = spec_pdf.edition_data(REQUIRED, edition, "acknowledgment and Preface patterns (verify_publishable.REQUIRED)")
    opening = spec_pdf.edition_data(ACKNOWLEDGMENT_OPENING, edition,
                                    "acknowledgment opening (verify_publishable.ACKNOWLEDGMENT_OPENING)")

    print("\nacknowledgment and preface — must be PRESENT:")
    for pat, what in required:
        if re.search(pat, text, re.M):
            print(f"  ✓ {what}")
        else:
            failures.append(f"missing: {what}")
            print(f"  ✗ MISSING — {what}")

    # The acknowledgment has to be in the preface, i.e. before the body starts, not only in position. Where the body
    # starts is the edition's own line (spec_pdf.EDITIONS body): 2023's clause 1 anchor, 1985's Section I.
    front = spec_pdf.front_matter(text, edition)
    if front is None:
        failures.append(f"the body's opening line ({spec_pdf.EDITIONS[edition].body}) is missing; cannot locate where "
                        "the front matter ends")
    elif opening is None:
        print(f"  ✓ the {edition} edition prints no acknowledgment, so its preface carries none")
    elif opening not in front:
        failures.append("the acknowledgment appears in the body but NOT in the preface")
        print("  ✗ the acknowledgment is not in the preface (it must precede clause 1)")
    else:
        print("  ✓ the acknowledgment precedes the body, as the preface")
    return failures


def main(edition: int = spec_pdf.DEFAULT_EDITION) -> int:
    spec_md = spec_pdf.markdown(edition)
    if not spec_md.exists():
        sys.exit(f"FATAL: {spec_md} not found")
    text = spec_md.read_text(encoding="utf-8")

    failures = check_text(text, edition)
    failures.extend(check_corrections(text, edition))
    failures.extend(check_reconstructions(text.splitlines(), edition))
    if edition in LEDGER:
        failures.extend(check_ledger(text, json.loads((REPO / LEDGER[edition]).read_text(encoding="utf-8"))))
    if edition in reserved_word_order.LISTS:
        order = reserved_word_order.check(text, edition)
        print("\nreserved-word list — in the printed order:")
        print("\n".join(f"  ✗ {finding}" for finding in order) or "  ✓ every adjacent pair, and the letter groups")
        failures.extend(order)
    else:
        print(f"\nreserved-word list — the {edition} list is not swept (kb/Work PB5060), so its order is not checked")
    failures.extend(check_tree())

    if failures:
        print("\n".join(["", "FATAL — not publishable:"] + [f"  - {f}" for f in failures]))
        return 1
    print(f"\nPUBLISHABLE — {len(text.splitlines()):,} lines, no per-copy licence data, acknowledgment in the preface")
    print("(word-for-word fidelity of the acknowledgment is verify_acknowledgment.py, which needs the PDF)")
    return 0


def self_test() -> int:
    """The edition plumbing and the stamp shapes, on PLANTED texts: no PDF, no transcription, no tracked tree."""
    import contextlib
    import io
    failures: list[str] = []

    def check(name: str, cond: bool) -> None:
        print(("  ok    " if cond else "  FAIL  ") + name)
        if not cond:
            failures.append(name)

    def quietly(fn, *args):
        with contextlib.redirect_stdout(io.StringIO()):
            return fn(*args)

    # kb/Work PB5256, branch review R1-4: an edition is reconciled when its ledger records every page of its PDF compared.
    import tempfile  # noqa: PLC0415
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        (root / RECONCILIATION[2023]).parent.mkdir(parents=True)
        pages23 = spec_pdf.EDITIONS[2023].pages
        (root / RECONCILIATION[2023]).write_text(json.dumps({"pages_swept": list(range(1, pages23))}), encoding="utf-8",
                                                 newline="\n")
        short = reconciled(2023, root)
        (root / RECONCILIATION[2023]).write_text(json.dumps({"pages_swept": list(range(1, pages23 + 1))}),
                                                 encoding="utf-8", newline="\n")
        (root / RECONCILIATION[1985]).write_text(json.dumps({"pages": spec_pdf.EDITIONS[1985].pages}), encoding="utf-8",
                                                 newline="\n")
        check("an edition is reconciled when its ledger records every page compared, in either ledger's shape, and not "
              "with a page short or with no ledger (2002)",
              not short and reconciled(2023, root) and reconciled(1985, root) and not reconciled(2002, root))
    good = ("# Preface\n\nCOBOL is an industry language and is not the property of any company\n"
            "No warranty, expressed or implied, is made by any contributor\n"
            "have specifically authorized the use of this material in whole or in part\n"
            "This preface is NOT part of ISO/IEC 1989:2023.\n\n<a id=\"section-1\"></a>\n\n## 1 Scope\n\n# Addendum\n")
    check("a clean 2023 text passes", quietly(check_text, good, 2023) == [])
    # The planted pieces are ASSEMBLED, never written whole: check_tree reads this very file and refuses a download date or
    # an order number anywhere in the tracked tree.
    download_date = "Downloaded " + "1/2/" + "2026"
    order_number = "X_" + "123456"
    stamped = good.replace("## 1 Scope", f"Licensed to someone. Single user license only.\n{download_date}\n## 1 Scope")
    found = quietly(check_text, stamped, 2023)
    check("a 2023 text carrying the stamp pieces fails once per piece", len(found) == 3)
    check("1985 reads its own stamp shapes: an `order #` line is refused and the 2023-only order number is not looked for",
          len(quietly(check_stamp, ["order # 12"], 1985)) == 1 and quietly(check_stamp, [order_number], 1985) == [])
    check("1985 refuses the opening and the licence-terms sentences the model left behind (PB4816 review R1-1)",
          len(quietly(check_stamp, ["Copyright by INCITS.", "Single user license only. Copying and networking prohibited."], 1985)) == 3)
    # An invented stamp of the 2002 shape (kb/Work PB4813), assembled so the tree check never reads it whole.
    long_order = "X_" + "9876543"
    planted_2002 = ("Copyright by INCITS. Licensed to " + "Ann Example.  ANSI store order # " + long_order + ". "
                    + download_date + ".  Single user license only. Copying and networking prohibited.")
    check("2002 refuses every piece of its stamp, each once",
          len(quietly(check_stamp, [planted_2002], 2002)) == len(FORBIDDEN[2002]))
    check("2002's own shape catches the order number longer than 2023's six-digit shape reads",
          quietly(check_stamp, [long_order], 2002) != [] and quietly(check_stamp, [long_order], 2023) == [])
    check("2002's tree shapes catch the order number, the download date and a purchaser's name",
          all(re.search(p, planted_2002) for p, _ in TREE_FORBIDDEN[2002]))
    good_2002 = ("# Preface\n\n> **This section is NOT part of ISO/IEC 1989:2002.**\n\nIt is reproduced with permission.\n\n"
                 "# Front matter: covers, contents, foreword and introduction\n\n# Clauses 1 to 16\n\n## Page 23\n")
    check("a clean 2002 text passes: its Preface states the permission, and it prints no acknowledgment to carry",
          quietly(check_text, good_2002, 2002) == [])
    check("a 2002 Preface that does not state the permission is refused",
          quietly(check_text, good_2002.replace("reproduced with permission", "reproduced"), 2002)
          == ["missing: the Preface must state the permission the transcription is reproduced under"])
    check("2002 never borrows 2023's acknowledgment: a 2023 text lacks the 2002 Preface's statements",
          len(quietly(check_text, good, 2002)) >= 2)
    # 2014 (kb/Work PB5268): the 2002 stamp's shape, measured on its own PDF, and the CODASYL acknowledgment in its Preface.
    check("2014 refuses every piece of its stamp, each once, the seven-digit order number included",
          len(quietly(check_stamp, [planted_2002], 2014)) == len(FORBIDDEN[2014])
          and quietly(check_stamp, [long_order], 2014) != [])
    check("2014's tree shapes catch the order number, the download date and a purchaser's name",
          all(re.search(p, planted_2002) for p, _ in TREE_FORBIDDEN[2014]))
    good_2014 = good.replace("ISO/IEC 1989:2023", "ISO/IEC 1989:2014").replace(
        '<a id="section-1"></a>\n\n## 1 Scope\n\n# Addendum\n',
        "# Front matter: covers, contents, foreword and introduction\n\n## Page 2\n\n# Clauses 1 to 16\n\n## Page 25\n")
    check("a clean 2014 text passes: its Preface carries the acknowledgment before the body, and it has no Addendum yet",
          quietly(check_text, good_2014, 2014) == [])
    check("a 2014 Preface without the acknowledgment is refused, and its body line is its own, not 2023's",
          len(quietly(check_text, good_2014.replace("COBOL is an industry language", "COBOL is a language"), 2014)) >= 1
          and quietly(check_text, good, 2014) != [])
    good_1985 =good.replace("ISO/IEC 1989:2023", "American National Standard X3.23-1985").replace(
        '<a id="section-1"></a>\n\n## 1 Scope', "# SECTION I: INTRODUCTORY INFORMATION\n\n## Page 21").replace(
        "# Addendum\n", "# Errata\n")
    check("a clean 1985 text passes, its body opening at Section I", quietly(check_text, good_1985, 1985) == [])
    check("1985 reads its own body line: 2023's text, which has no Section I heading, has no 1985 front matter",
          any(f.startswith("the body's opening line") for f in quietly(check_text, good, 1985)))
    section_i = "# SECTION I: INTRODUCTORY INFORMATION\n"
    body_only = good_1985.replace(section_i, "").replace("# Preface\n", "# Preface\n\n" + section_i)
    check("a text whose acknowledgment is only in the body is refused",
          "the acknowledgment appears in the body but NOT in the preface" in quietly(check_text, body_only, 1985))
    check("a text that flags no correction and lists none passes the correction check",
          quietly(check_corrections, good_1985, 1985) == [])
    check("1985 requires its Errata, as 2023 its Addendum",
          "missing: the Errata that lists every correction (check_corrections, kb/Work PB4972)"
          in quietly(check_text, good_1985.replace("# Errata\n", ""), 1985))
    # A 1985 correction (kb/Work PB4972): its flag says `see the Errata (Cn)` and names the PDF page, its entry under
    # `# Errata` names the same page and the printed form.
    flagged = good_1985.replace("## Page 21", "## Page 21\n\nIn control footing, page heading.\n\n> ⚠ **CORRECTED — see "
                                "the Errata (C1).** The standard prints `In control fopting` (page 21, folio I-1).")
    listed = flagged + "\n### C1 · page 21 (folio I-1) · a rule · `fopting` → `footing`\n\n**Printed**: `fopting`\n"
    check("a 1985 correction flagged in place and listed in the Errata passes",
          quietly(check_corrections, listed, 1985) == [])
    glued = listed.replace("(page 21, folio I-1).", "(page 21, folio I-1).\n   an index line right after it")
    check("a flag followed by a line with no blank line between is refused: the line would render inside the note",
          any("followed by text with no blank line" in f for f in quietly(check_corrections, glued, 1985)))
    in_fence = listed.replace("> ⚠ **CORRECTED", "```cobol\n> ⚠ **CORRECTED").replace(
        "(page 21, folio I-1).", "(page 21, folio I-1).\n       DISPLAY 'X'.\n```")
    check("a flag inside a fenced example may be followed by the example's code",
          not any("followed by text" in f for f in quietly(check_corrections, in_fence, 1985)))
    check("a 1985 flag with no Errata entry is refused",
          any("MISSING from the Errata" in f for f in quietly(check_corrections, flagged, 1985)))
    check("1985 reads its own section: a flag that says `see the Addendum` is no 1985 flag, so its entry is unreferenced",
          any("never referenced" in f
              for f in quietly(check_corrections, listed.replace("see the Errata", "see the Addendum"), 1985)))
    check("2023 still reads its Addendum, not the Errata",
          quietly(check_corrections, listed.replace("# Errata", "# Addendum").replace("the Errata", "the Addendum"), 2023)
          == [])
    check("2023 requires its Addendum, so deleting it with its flags is still refused",
          "missing: the Addendum that lists every correction (check_corrections)"
          in quietly(check_text, good.replace("# Addendum\n", ""), 2023))
    planted_pages = ("# Preface\n\nthe notation `║boxed║`\n\n## Page 1\n\n<u>Format 1</u>:\n\n(4) a rule ║or extend║ here.\n\n"
                     "<pre style=\"line-height:1\">\n║ a drawn box ║\n</pre>\n\n## Page 2\n\nplain\n")
    planted_ledger = {"book_pages": 2, "pages_swept": [1, 2], "pages_unread": [], "claims": [{"page": 1, "i": 0, "decision": "APPLIED",
                                  "edits": [{"was": "Format 1:", "now": "<u>Format 1</u>:"}]},
                                 {"page": 2, "i": 0, "decision": "REFUTED", "reason": "the page prints it so"}],
                      "sweeps": [], "boxes": [{"page": 1, "n": 0, "decision": "MARKED", "blocks": 1, "opens": ["║or extend║"]},
                                              {"page": 1, "n": 1, "decision": "FORMAT", "reason": "drawn"}]}
    check("a transcription that agrees with its ledger passes: a code span and a drawn box are not prose box sides",
          quietly(check_ledger, planted_pages, planted_ledger) == [])
    check("a box side the ledger does not account for is refused",
          len(quietly(check_ledger, planted_pages.replace("plain", "║plain║"), planted_ledger)) == 1)
    check("a lost repair is refused", len(quietly(check_ledger, planted_pages.replace("<u>Format 1</u>", "Format 1"),
                                                  planted_ledger)) == 1)
    undecided = dict(planted_ledger, claims=planted_ledger["claims"] + [{"page": 2, "i": 1, "decision": "REFUTED"}])
    check("a claim decided with no reason is refused", len(quietly(check_ledger, planted_pages, undecided)) == 1)
    for name, change in (("a ledger whose book_pages is not the transcription's page count is refused", {"book_pages": 3}),
                         ("a ledger that never compared a page of the book is refused", {"pages_swept": [1]}),
                         ("a ledger that lists a page the book does not hold is refused", {"pages_swept": [1, 2, 3]}),
                         ("a ledger with an unread page is refused",
                          {"pages_unread": [{"page": 2, "reason": "blank render"}]})):
        check(name, len(quietly(check_ledger, planted_pages, dict(planted_ledger, **change))) == 1)
    # kb/Work PB4952: a gap in the printed folios holds a reconstruction, and a reconstruction stands only in a gap.
    opening = ("> ⚠ **RECONSTRUCTED — not printed in our copy (folios XIII-30 and XIII-31).** Planted. **Sources:** the "
               "siblings. **Confidence: low.**")
    closing = "> ⚠ **END RECONSTRUCTED (folios XIII-30 and XIII-31).** Printed again."
    filled = ["## Page 1", "", "text", "", "XIII-29", "", "## Page 2", "", opening, "", "(1) A rule.", "", closing, "",
              "Report Writer - Body", "", "XIII-32", ""]
    check("a reconstruction that fills the folios the scan lacks passes",
          quietly(check_reconstructions, filled, 1985) == [])
    check("a gap in the printed folios with no reconstruction is refused",
          any("['XIII-30', 'XIII-31'] are missing" in f
              for f in quietly(check_reconstructions, [ln for ln in filled if ln not in (opening, closing)], 1985)))
    transcribed = filled[:6] + ["## Page 3", "", "XIII-30", "", "## Page 4", "", "XIII-31", ""] + filled[6:]
    check("a reconstruction whose printed page is transcribed is refused",
          any("the printed page is transcribed" in f for f in quietly(check_reconstructions, transcribed, 1985)))
    check("an unclosed reconstruction, a closing that names other folios, and an unsourced one are refused",
          any("never closed" in f for f in quietly(check_reconstructions, [ln for ln in filled if ln != closing], 1985))
          and any("closes as XIII-30" in f for f in quietly(
              check_reconstructions, [closing.replace("folios XIII-30 and XIII-31", "folio XIII-30") if ln == closing
                                      else ln for ln in filled], 1985))
          and any("no **Sources:**" in f for f in quietly(
              check_reconstructions, [opening.split(" **Sources:**")[0] if ln == opening else ln for ln in filled], 1985)))
    check("a reconstruction note followed by text with no blank line is refused",
          any("followed by text" in f for f in quietly(
              check_reconstructions, [ln for k, ln in enumerate(filled) if k != 9], 1985)))
    check("tool-harness markup in a transcription is refused (page 894's `<invoking-skill>`), a notation's tag is not",
          any("tool-harness markup" in f for f in quietly(
              check_text, good.replace("# Preface\n", "# Preface\n\n<invoking-skill>x</invoking-skill>\n", 1), 2023))
          and not any("tool-harness" in f for f in quietly(check_text, good + "\n<storage location>\n", 2023)))
    check("the tree shapes of every edition are searched on every run",
          (r"\border\s*#\s*[A-Za-z_]*\d{4,}", "reseller order number") in tree_shapes()
          and (r"\bX_\d{6}\b", "reseller order number") in tree_shapes())
    check("a shape names no licensee and no order number",
          not any(re.search(r"\d{6}", p) for table in (FORBIDDEN, TREE_FORBIDDEN)
                  for rows in table.values() for p, _ in rows))
    print(f"=== VERIFY PUBLISHABLE SELF-TEST: {'FAIL - ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--self-test", action="store_true", help="run the planted-text self-test")
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args()
    sys.exit(self_test() if args.self_test else main(args.edition))
