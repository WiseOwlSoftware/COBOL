#!/usr/bin/env python3
"""Pre-publication check for the spec transcription: no per-copy licence data, and the acknowledgment present.

WHY THIS EXISTS. `specs/ISO_COBOL_2023.md` is published; the PDF it was transcribed from is NOT. Two things therefore
have to be true of the Markdown before it leaves the private submodule, and neither is guaranteed by any other
gate (`--edition {1985,2002,2023}`, default 2023, names the transcription; what is edition-specific, the stamp shapes
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
     every repair on its page, every claim decided, and each page's prose carrying exactly the level-2 box sides its
     marked boxes account for (`check_ledger`).

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

    python scripts/spec/verify_publishable.py [--edition 1985|2002|2023]
    python scripts/spec/verify_publishable.py --self-test        # planted texts: no PDF, no tracked tree
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from cite import CORRECTION_NOTE  # noqa: E402  — ONE definition of what an editorial correction note looks like
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = spec_pdf.REPO

#: The reseller's per-copy stamp of each edition, in pieces (SHAPES, never a licensee's name or an order number), so a
#: partial survival is caught as readily as the whole line. 1985: PB4812 records the stamp as "Licensed to ... order # ...";
#: the first assembly of the adopted run then showed the model leaving
#: the opening sentence and the licence-terms sentence behind on 14 pages (kb/Work PB4816, review R1-1), so those are pieces too.
#: 2002: read from the 2002 PDF's own text layer (kb/Work PB4813). It prints one stamp line, the same on all 882 pages:
#: the 1985 line's opening and licence-terms sentences, a store order reference whose number is longer than six digits
#: (so 2023's `X_` shape, which needs a word boundary after six, misses it), and 2023's download date. Each shape matches
#: that line on every page and no other line of the PDF.
FORBIDDEN: dict[int, list[tuple[str, str]]] = {
    1985: [
        (r"Licensed to\b", "per-copy licence stamp"),
        (r"\border\s*#", "reseller order reference"),
        (r"Copyright by INCITS", "per-copy licence stamp"),
        (r"Single user licen[cs]e", "per-copy licence stamp"),
        (r"Copying and networking", "per-copy licence stamp"),
    ],
    2002: [
        (r"Copyright by INCITS", "per-copy licence stamp"),
        (r"Licensed to\b", "per-copy licence stamp"),
        (r"\border\s*#", "reseller order reference"),
        (r"\bX_\d{6,}\b", "reseller order number"),
        (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
        (r"Single user licen[cs]e", "per-copy licence stamp"),
        (r"Copying and networking", "per-copy licence stamp"),
    ],
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
    2002: [
        (r"\bX_\d{6,}\b", "reseller order number"),
        (r"Downloaded \d{1,2}/\d{1,2}/\d{4}", "per-copy download date"),
        (r"Licensed to [A-Z][a-z]+ [A-Z][a-z]+", "per-copy licence stamp naming a purchaser"),
    ],
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
    batch = subprocess.run(["git", "-C", str(repo), "cat-file", "--batch"], input="\n".join(blobs).encode() + b"\n",
                           capture_output=True, check=True).stdout
    failures, at = [], 0
    pats = [(re.compile(p), what) for p, what in tree_shapes()]
    while at < len(batch):
        nl = batch.index(b"\n", at)
        sha, _kind, size = batch[at:nl].decode().split()
        body = batch[nl + 1: nl + 1 + int(size)]
        at = nl + 1 + int(size) + 1
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
}

#: The words that open the acknowledgment, which must precede clause 1 (the Preface), by edition.
ACKNOWLEDGMENT_OPENING: dict[int, str] = {1985: "COBOL is an industry language", 2023: "COBOL is an industry language"}


#: An Addendum correction entry: `### C6 · page 607 · 14.9.10.4 … · printed → corrected`.
ENTRY_HEADING = re.compile(r"^###\s+(C\d+)\s+·\s+(.*)$")
FLAG_REFERENCE = re.compile(r"see the Addendum \((C\d+)\)")
PAGES_IN_HEADING = re.compile(r"\bpages?\s+((?:\d+(?:,\s*|\s+and\s+)?)+)")
PAGE_IN_NOTE = re.compile(r"\bpage\s+(\d+)\b")
#: A correction that changes a CLASS of characters across the whole text is flagged once, in the Preface, and is
#: listed under this heading in place of a page (C14: the look-alike hyphens and minus signs).
WHOLE_TEXT = "the whole text"


def check_corrections(text: str) -> list[str]:
    """Every correction is FLAGGED where it was made, LISTED in the Addendum, and names its PRINTED FORM and PAGE.

    The Addendum exists so a correction can be reversed if it later proves mistaken (specs/README.md "Corrections",
    kb/Work R66). That only works while three things hold, and each is checked here because each can be edited away
    independently of the others:

      1. the two sets agree — every `see the Addendum (Cn)` flag has a `### Cn` entry and every entry is pointed at;
      2. every in-place flag is a `> ⚠ **CORRECTED — see the Addendum (Cn).**` note that QUOTES THE PRINTED FORM in a code
         span and names the PRINTED PAGE, and that page is one the entry's heading lists — `cite.py --check` finds a
         quotation of the printed form in the note (and reports the corrected line's rule path, cite.rule_path), which
         is what keeps every citation written against the printed form passing;
      3. every entry's heading names the printed page(s) (or `the whole text` for a class of characters), and an entry
         other than such a class states its printed form.
    """
    failures: list[str] = []
    lines = text.split("\n")
    addendum_at = next((i for i, l in enumerate(lines) if l.startswith("# Addendum")), len(lines))
    print("\ncorrections — every flag must match an Addendum entry that names the printed form and page:")

    # the Addendum's entries, each with its body up to the next heading
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
    i = 0
    while i < addendum_at:
        if CORRECTION_NOTE.match(lines[i]):
            j = i
            while j + 1 < addendum_at and lines[j + 1].startswith(">"):
                j += 1
            paragraph = " ".join(l.lstrip("> ").strip() for l in lines[i:j + 1])
            ids = FLAG_REFERENCE.findall(paragraph)
            for cid in ids[:1]:
                flags.setdefault(cid, []).append((i + 1, paragraph))
            i = j
        i += 1
    referenced = set(FLAG_REFERENCE.findall("\n".join(lines[:addendum_at])))

    if not entries and not referenced:
        # A transcription that corrects nothing flags none and lists none. One that has an Addendum requires it by its
        # REQUIRED row, so deleting the Addendum together with its flags is still refused there.
        print("  ✓ no corrections: the text flags none and lists none")
    for cid in sorted(referenced | set(entries), key=lambda c: int(c[1:])):
        problems: list[str] = []
        entry = entries.get(cid)
        if cid not in entries:
            problems.append("referenced from the text, but MISSING from the Addendum")
        elif cid not in referenced:
            problems.append("in the Addendum, but never referenced from the text")
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
            print(f"  ✓ {cid}: flagged in place, listed in the Addendum, printed form and page named")
    return failures


#: The reconciliation ledger of a transcription that keeps its pages (`## Page N`), by edition (kb/Work PB4850): every
#: batch claim with its decision, every repair, and every level-2 box. 2023 dropped its pages; its reconciliation is
#: LEDGER.json (merge_reconciliation.py) and its corrections the Addendum (check_corrections).
LEDGER: dict[int, str] = {1985: "docs/rearchitecture/spec-reconciliation/LEDGER-1985.json"}
#: What a claim's decision may be; each but APPLIED states its reason.
DECISIONS = {"APPLIED", "REFUTED", "ALREADY", "FORMAT", "BOX", "FIGURE", "KEPT"}
BOX_SIDE = spec_pdf.BOX_SIDE
PAGE_HEADING = re.compile(r"^## Page (\d+)$", re.M)
CODE_SPAN = re.compile(r"`[^`\n]*`")


def pages_of(text: str) -> dict[int, str]:
    """The transcription's text by PDF page (`## Page N` opens page N); the front matter before page 1 is page 0."""
    cuts = [(int(m.group(1)), m.start()) for m in PAGE_HEADING.finditer(text)]
    out = {0: text[:cuts[0][1]] if cuts else text}
    for (page, at), (_, end) in zip(cuts, cuts[1:] + [(0, len(text))]):
        out[page] = text[at:end]
    return out


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
            count += CODE_SPAN.sub("", line).count(BOX_SIDE)
        if "</pre>" in line:
            figure = False
    return count


def check_ledger(text: str, ledger: dict) -> list[str]:
    """The transcription agrees with its reconciliation ledger: every applied repair and sweep is on its page, every claim
    is decided, every marked box opens where the ledger says, and each page's prose carries exactly the box sides its
    marked boxes account for (two per marked block), so a box added, lost or moved without the ledger is refused."""
    failures: list[str] = []
    pages = pages_of(text)
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
        print(f"  ✓ every repair on its page, every claim decided, {sum(sides.values()) // 2} boxed blocks where the ledger "
              "puts them")
    return failures


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
    failures.extend(check_corrections(text))
    if edition in LEDGER:
        failures.extend(check_ledger(text, json.loads((REPO / LEDGER[edition]).read_text(encoding="utf-8"))))
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
    check("2002 has a stamp row, so check_text reaches its acknowledgment row and exits there",
          2002 in FORBIDDEN and 2002 in TREE_FORBIDDEN and 2002 not in REQUIRED)
    try:
        quietly(check_text, good, 2002)
        check("2002 with no acknowledgment row exits instead of borrowing 2023's", False)
    except SystemExit as ex:
        check("2002 with no row exits naming the table and the edition",
              "2002" in str(ex.code) and "verify_publishable" in str(ex.code))
    good_1985 = good.replace("ISO/IEC 1989:2023", "American National Standard X3.23-1985").replace(
        '<a id="section-1"></a>\n\n## 1 Scope', "# SECTION I: INTRODUCTORY INFORMATION\n\n## Page 21")
    check("a clean 1985 text passes, its body opening at Section I", quietly(check_text, good_1985, 1985) == [])
    check("1985 reads its own body line: 2023's text, which has no Section I heading, has no 1985 front matter",
          len(quietly(check_text, good, 1985)) == 2)
    section_i = "# SECTION I: INTRODUCTORY INFORMATION\n"
    body_only = good_1985.replace(section_i, "").replace("# Preface\n", "# Preface\n\n" + section_i)
    check("a text whose acknowledgment is only in the body is refused",
          "the acknowledgment appears in the body but NOT in the preface" in quietly(check_text, body_only, 1985))
    check("a text that flags no correction and lists none passes the correction check",
          quietly(check_corrections, good_1985) == [])
    check("2023 requires its Addendum, so deleting it with its flags is still refused",
          "missing: the Addendum that lists every correction (check_corrections)"
          in quietly(check_text, good.replace("# Addendum\n", ""), 2023))
    planted_pages = ("# Preface\n\nthe notation `║boxed║`\n\n## Page 1\n\n<u>Format 1</u>:\n\n(4) a rule ║or extend║ here.\n\n"
                     "<pre style=\"line-height:1\">\n║ a drawn box ║\n</pre>\n\n## Page 2\n\nplain\n")
    planted_ledger = {"claims": [{"page": 1, "i": 0, "decision": "APPLIED",
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
