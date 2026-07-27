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
    python scripts/spec/extract_rule_catalog.py                 # write the catalog + print the summary
    python scripts/spec/extract_rule_catalog.py --check         # parse only; non-zero exit if gaps exist
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from collections import Counter

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC = REPO / "specs" / "ISO_COBOL.md"
OUT = REPO / "docs" / "rearchitecture" / "spec-rule-catalog.json"

# A numbered section heading, e.g. "## 8.4.3.13.3 Syntax rules" or "### 14.9.28.4 General rules".
HEADING = re.compile(r"^#{2,6}\s+(?P<num>\d+(?:\.\d+)*)\s+(?P<title>.+?)\s*$")
PAGE = re.compile(r"^##\s+Page\s+(\d+)\s*$")
# Any markdown heading — used to CLOSE a rule block (see the loop). Annex headings carry no section number.
ANY_HEADING = re.compile(r"^#{1,6}\s+\S")
# PAGE-BREAK FURNITURE. At every printed page boundary the transcription injects a running header, and it is
# emitted AS A HEADING — inside rule blocks. Treating it as a block terminator truncates every rule block that
# spans a page break (it cost 896 rules when first tried). Skip it instead. Three transcribed shapes, the same
# three scripts/fix_spec_pagebreaks.py had to learn the hard way; missing the bare form is the dangerous one.
RUNNING_HEADER = re.compile(r"^#{1,6}\s*(?:\*\*)?ISO/IEC\s+1989:2023\s*\(E\)(?:\*\*)?\s*$")
# ALL page-break furniture, in every transcribed shape. These lines land INSIDE rule prose at a page boundary and
# were being absorbed as rule TEXT: 592 of 3,247 rules (18%) carried an anchor, a rule, a running header or a
# licence footer spliced into the normative sentence. A rule whose text is contaminated is worse than a missing
# one — it reads as authoritative and would be cited that way. Skipped at the line level, before buffering.
FURNITURE = re.compile(
    r"^\s*(?:"
    r"-{3,}"                                              # the --- separators around a page block
    r'|<a\s+id="[^"]*"></a>'                              # ANY anchor — page-N and section-N-N both leak in
    r"|(?:\*\*)?ISO/IEC\s+1989:2023\s*\(E\)(?:\*\*)?"    # running header: bold or bare (no leading #)
    r"|(?:\d+\s+)?(?:©|\(c\))?\s*ISO/IEC\s+2023"        # copyright footer
    r"|Licensed to .*"                                    # licence footer
    r")\s*$", re.I)
# A top-level rule ordinal at the start of a line. The transcription uses BOTH "1)" and "1." — an earlier version
# matched only "1)" and silently dropped every rule in §8.8.3.2, §11.9.11.2, §13.18.24.3 and §14.9.30.3. The
# completeness self-check is what surfaced it; without it the denominator would have been quietly short.
# Sub-items ("a)") are indented and stay with their parent rule.
ORDINAL = re.compile(r"^(?P<n>\d+)[.)]\s+(?P<text>.*)$")

# The rule-block kinds the standard uses. Intrinsic functions use argument/returned-value rules instead of SR/GR.
# SINGULAR forms are load-bearing, not a nicety: the standard writes "Syntax rule" when a block holds exactly one
# (§10.6.3, §11.9.4, §13.4.3, …). A plural-only map skipped 15 whole blocks — invisible to the empty-block check,
# because a block that is never RECOGNISED is never counted as seen. The TOC cross-check below is what caught it.
KINDS = {
    "syntax rules": "SR", "syntax rule": "SR",
    "general rules": "GR", "general rule": "GR",
    "argument rules": "AR", "argument rule": "AR", "arguments": "AR",
    "returned value rules": "RV", "returned value rule": "RV", "returned value": "RV",
}


def kind_of(title: str) -> str | None:
    return KINDS.get(title.strip().lower().rstrip("."))


def subject_for(num: str, titles: dict[str, str]) -> str:
    """The nearest enclosing ancestor section that is not itself a rule block — the construct being ruled."""
    parts = num.split(".")
    for cut in range(len(parts) - 1, 0, -1):
        parent = ".".join(parts[:cut])
        title = titles.get(parent)
        if title and kind_of(title) is None:
            return title
    return titles.get(num, "?")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="parse only; exit non-zero if parse gaps exist")
    args = ap.parse_args()

    # The Windows console defaults to cp1252 and cannot encode the section sign or the warning glyph.
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001 - a reporting nicety must never fail the extraction
        pass

    if not SPEC.exists():
        sys.exit(f"spec not found: {SPEC}\nRun: git submodule update --init --recursive")

    lines = SPEC.read_text(encoding="utf-8", errors="replace").splitlines()
    titles: dict[str, str] = {}

    # Pass 1 — every numbered heading, so a rule block can name its subject.
    for line in lines:
        if (m := HEADING.match(line)) and not line.lstrip().startswith("["):
            titles.setdefault(m.group("num"), m.group("title"))

    # Pass 2 — walk rule blocks and collect ordinals.
    rules: list[dict] = []
    blocks_seen: list[tuple[str, str]] = []      # (section-number, kind)
    blocks_with_rules: set[tuple[str, str]] = set()

    page = 0
    cur: tuple[str, str] | None = None           # (section-number, kind) of the block being read
    buf: list[str] = []
    ordinal = 0
    # A rule block can contain MORE THAN ONE numbered list — the standard restarts numbering under an unheaded
    # sub-list (e.g. §7.2.3.4 General rules runs 1..n for COPY, then restarts 1..n for text-word matching). Keyed
    # on (kind, section, ordinal) alone, 173 rules collided and silently overwrote each other — which would have
    # carried the WRONG verdict forward on every inventory refresh. Track the sub-list and qualify the id.
    sublist = 1
    last_ordinal = 0
    # The page a rule STARTS on. flush() runs when the NEXT ordinal or heading arrives, by which time a
    # "## Page N" marker may already have advanced `page` — so flushing with the live value mis-attributes the
    # last rule of every block that ends near a page boundary (verified against the rendered PDF: GR-7.3.12.4-6
    # is printed on page 95 and was recorded as 96).
    rule_page = 0

    def flush() -> None:
        nonlocal buf, ordinal
        if cur and ordinal and (text := " ".join(x.strip() for x in buf).strip()):
            sec, kind = cur
            rid = f"{kind}-{sec}-{ordinal}" if sublist == 1 else f"{kind}-{sec}-L{sublist}.{ordinal}"
            rules.append({
                "id": rid,
                "section": sec,
                "kind": kind,
                "ordinal": ordinal,
                "sublist": sublist,
                "subject": subject_for(sec, titles),
                "page": rule_page,
                "text": re.sub(r"\s+", " ", text),
            })
            blocks_with_rules.add(cur)
        buf, ordinal = [], 0

    for line in lines:
        if m := PAGE.match(line):
            page = int(m.group(1))
            continue
        # ANY heading closes the current rule block. Terminating only on a NUMBERED heading let §16.2.2.2 — the
        # last rule block in the standard's body — run straight through the un-numbered annex headings ("## Annex
        # A (normative)"), turning every "N." line in Annexes A-F into a fake rule: 601 phantoms in one block.
        # A block that never ends is as wrong as a block never seen, and inflates the denominator instead of
        # shortening it, which is the more dangerous direction because it looks like thoroughness.
        if RUNNING_HEADER.match(line) or FURNITURE.match(line):
            continue
        if ANY_HEADING.match(line) and not HEADING.match(line):
            flush()
            cur = None
            continue
        if (m := HEADING.match(line)) and not line.lstrip().startswith("["):
            flush()
            num, title = m.group("num"), m.group("title")
            k = kind_of(title)
            sublist, last_ordinal = 1, 0
            if k:
                cur = (num, k)
                blocks_seen.append(cur)
            else:
                cur = None
            continue
        if cur is None:
            continue
        if m := ORDINAL.match(line):
            n = int(m.group("n"))
            if n <= last_ordinal:          # numbering went backwards or repeated -> a new sub-list
                flush()
                sublist += 1
            else:
                flush()
            ordinal, last_ordinal = n, n
            rule_page = page
            buf = [m.group("text")]
        elif ordinal:
            buf.append(line)
    flush()

    # ---- completeness self-check -----------------------------------------------------------------------------
    # §5.3.2-5.3.5 are the CONVENTIONS clause: prose DEFINING what a syntax/general/argument/returned-value rule
    # is. They are rule-block headings that correctly contain no rules. Every other empty block is a parse gap.
    EXPECTED_EMPTY = {"5.3.2", "5.3.3", "5.3.4", "5.3.5"}
    gaps = [b for b in blocks_seen if b not in blocks_with_rules and b[0] not in EXPECTED_EMPTY]

    # ---- General formats — the SYNTAX DIAGRAMS ---------------------------------------------------------------
    # These are the artifacts the ANTLR grammar was written FROM, and the ones transcription loss damages most
    # (a dropped choice-indicator bar makes legal source look illegal). They carry no numbered ordinals, so the
    # rule scan never saw them and the denominator omitted all 320. Each becomes one FMT row: the unit of
    # "verify this diagram against the grammar rule that implements it".
    fmt_sections: list[tuple[str, int, int]] = []   # (section, heading-line, page)
    page = 0
    for i, line in enumerate(lines):
        if m := PAGE.match(line):
            page = int(m.group(1))
            continue
        if (m := HEADING.match(line)) and not line.lstrip().startswith("["):
            if re.fullmatch(r"general formats?\.?", m.group("title").strip(), re.I):
                fmt_sections.append((m.group("num"), i, page))

    for sec, at, pg in fmt_sections:
        body: list[str] = []
        for line in lines[at + 1:]:
            if ANY_HEADING.match(line) and not RUNNING_HEADER.match(line) and not PAGE.match(line):
                break
            body.append(line)
        text = "\n".join(body).strip()
        rules.append({
            "id": f"FMT-{sec}", "section": sec, "kind": "FMT", "ordinal": 0, "sublist": 1,
            "subject": subject_for(sec, titles), "page": pg,
            # How many numbered Formats the section declares — Format 1 / Format 2 usually map to DIFFERENT
            # grammar alternatives, so this is a size signal for the verification work, not decoration.
            "formats": len(re.findall(r"^\s*Format\s+\d+", text, re.M)) or 1,
            "has_diagram": bool(re.search(r"^\s*```|\$\$", text, re.M)),
            "has_figure_notes": "Figure notes" in text,
            "text": re.sub(r"\s+", " ", text)[:4000],
        })

    # ---- Annex A.1 — the implementor-defined language element list -------------------------------------------
    # D13 defines "100% conforming" as the mandatory core complete PLUS every required implementor-documentation
    # item, so these ARE inventory rows and were missing from the denominator entirely. The heading is
    # "### **A.1 ...**" — bold inside the heading, so it carries no leading digit and the numbered-heading scan
    # never saw it. Items are "N) <header> (<paraphrase>). This item is <required|optional|conditionally
    # required>. This item [, if provided ...,] shall be documented ... (<cross-reference>)".
    a1_start = next((i for i, l in enumerate(lines) if re.match(r"^#{2,6}\s+\*{0,2}A\.1\b", l)), None)
    a1_items = 0
    if a1_start is not None:
        page = 0
        for line in lines[:a1_start]:
            if m := PAGE.match(line):
                page = int(m.group(1))
        buf, ordinal, rule_page = [], 0, page

        def flush_a1() -> None:
            nonlocal buf, ordinal, a1_items
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
                rules.append({
                    "id": f"DOC-A.1-{ordinal}", "section": "A.1", "kind": "DOC", "ordinal": ordinal,
                    "sublist": 1, "subject": text.split(".")[0][:120], "page": rule_page,
                    "requirement": req, "documented": "shall be documented" in low, "text": text,
                })
                a1_items += 1
            buf, ordinal = [], 0

        for line in lines[a1_start + 1:]:
            if m := PAGE.match(line):
                page = int(m.group(1))
                continue
            if RUNNING_HEADER.match(line) or FURNITURE.match(line):
                continue
            if ANY_HEADING.match(line):          # A.2 (or any later annex heading) ends the list
                flush_a1()
                break
            if m := ORDINAL.match(line):
                flush_a1()
                ordinal, rule_page = int(m.group("n")), page
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
    # replaced by prose ABOUT the page is not. (COBOLNET_DESIGN §1.4 applied to the spec itself.)
    META = [
        (re.compile(r"\bI'?m not able to\b|\bI can quote\b|\bI'?m happy to help\b", re.I), "assistant refusal/offer"),
        (re.compile(r"here is a summary|summary and key excerpts|here are the key", re.I), "summary preamble"),
        (re.compile(r"\bcopyrighted standards document\b|\breproduce the full page\b", re.I), "copyright refusal"),
        (re.compile(r"<!--\s*TRANSCRIPTION-FAILED", re.I), "declared transcription gap (tracked, not hidden)"),
    ]
    meta_hits = []
    pg = 0
    for i, line in enumerate(lines):
        if m := re.match(r'^<a id="page-(\d+)"></a>', line):
            pg = int(m.group(1))
            continue
        for pat, label in META:
            if pat.search(line):
                meta_hits.append((pg, i + 1, label, line.strip()[:100]))
                break
    if meta_hits:
        # LOUD in a normal run, FATAL under --check. Deliberate split: the catalog must stay regenerable while
        # the reconciliation ledger is still being collected (repairs come after the sweep), but the GATE must
        # refuse to call the transcription sound while any page of it is not the standard.
        declared = [h for h in meta_hits if h[2].startswith("declared")]
        hidden = [h for h in meta_hits if not h[2].startswith("declared")]
        print(f"\n⚠ {len(meta_hits)} line(s) of NON-SPEC TEXT inside the standard, "
              f"page(s) {sorted({h[0] for h in meta_hits})} "
              f"({len(hidden)} hidden as content, {len(declared)} declared gap(s)).")
        print("  These are NOT the spec — a rule or citation derived from such a page is unfounded:")
        for pg_, ln, label, txt in meta_hits[:15]:
            print(f"      p{pg_} line {ln} [{label}] {txt}")
        if args.check:
            sys.exit(1)

    # ---- completeness critic: the spec's own TOC ------------------------------------------------------------
    # The empty-block check has a BLIND SPOT — it can only verify blocks it RECOGNISED. A block whose heading was
    # never matched (an unhandled title form, or a heading a page break demoted to bold text and stripped of its
    # anchor, which has happened in this transcription) is invisible to it. The TOC is an independent witness of
    # what the printed standard contains, so cross-check against it.
    toc = {(n, k.strip().lower()) for n, k in
           re.findall(r"\[(\d+(?:\.\d+)*)\s+((?:Syntax|General|Argument|Returned value)\s+rules?)\]\(#section-",
                      "\n".join(lines))}
    seen = {(n, (titles.get(n) or "").strip().lower().rstrip(".")) for n, _ in blocks_seen}
    unseen = sorted(toc - seen, key=lambda x: [int(p) for p in x[0].split(".")])
    if unseen:
        print(f"\n✗ FATAL: {len(unseen)} rule block(s) listed in the spec TOC were never parsed as headings.")
        print("  A block that is never recognised is never counted — the denominator would be short and the")
        print("  empty-block check could not see it. Investigate the heading form before trusting this catalog:")
        for n, k in unseen[:20]:
            print(f"      §{n}  {k}")
        sys.exit(1)

    # Rule ids are the inventory's PRIMARY KEY — a duplicate silently carries the wrong verdict forward on every
    # refresh, and drops rules from any per-rule view. This is not a warning; it invalidates the catalog.
    dupes = [rid for rid, n in Counter(r["id"] for r in rules).items() if n > 1]
    if dupes:
        print(f"\n✗ FATAL: {len(dupes)} DUPLICATE rule id(s) — the inventory key is not unique:")
        for rid in dupes[:10]:
            print(f"      {rid}")
        sys.exit(1)

    by_kind = Counter(r["kind"] for r in rules)
    top = Counter(r["section"].split(".")[0] for r in rules)

    print(f"spec:            {SPEC.relative_to(REPO)}  ({len(lines):,} lines)")
    print(f"rule blocks:     {len(blocks_seen)}")
    print(f"RULES EXTRACTED: {len(rules)}")
    print()
    for k, label in (("SR", "syntax rules"), ("GR", "general rules"),
                     ("AR", "argument rules"), ("RV", "returned value rules"),
                     ("DOC", "Annex A.1 implementor-defined items"),
                     ("FMT", "general formats (syntax diagrams)")):
        if by_kind.get(k):
            print(f"   {by_kind[k]:5d}  {k}  {label}")
    print()
    print("   by top-level clause:")
    # Annex clauses sort after numbered ones ("A.1" is not an int).
    for sec, n in sorted(top.items(), key=lambda kv: (0, int(kv[0])) if kv[0].isdigit() else (1, 0)):
        print(f"      §{sec:<3s} {n:5d}")

    if gaps:
        print(f"\n⚠ {len(gaps)} PARSE GAP(S) — rule-block headings that yielded ZERO rules.")
        print("  An omitted rule is silent false confidence; investigate before trusting the denominator:")
        for sec, kind in gaps[:25]:
            print(f"      {kind} §{sec}  ({titles.get(sec, '?')})")
        if len(gaps) > 25:
            print(f"      ... and {len(gaps) - 25} more")
    else:
        print("\n✓ no parse gaps — every rule-block heading yielded at least one rule")

    if args.check:
        return 1 if gaps else 0

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps({
        "generated_from": str(SPEC.relative_to(REPO)),
        "rule_count": len(rules),
        "blocks": len(blocks_seen),
        "parse_gaps": [{"section": s, "kind": k} for s, k in gaps],
        "rules": rules,
    }, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"\nwrote {OUT.relative_to(REPO)}  ({OUT.stat().st_size / 1_048_576:.1f} MB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
