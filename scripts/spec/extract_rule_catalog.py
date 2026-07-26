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
# A top-level rule ordinal at the start of a line. The transcription uses BOTH "1)" and "1." — an earlier version
# matched only "1)" and silently dropped every rule in §8.8.3.2, §11.9.11.2, §13.18.24.3 and §14.9.30.3. The
# completeness self-check is what surfaced it; without it the denominator would have been quietly short.
# Sub-items ("a)") are indented and stay with their parent rule.
ORDINAL = re.compile(r"^(?P<n>\d+)[.)]\s+(?P<text>.*)$")

# The rule-block kinds the standard uses. Intrinsic functions use argument/returned-value rules instead of SR/GR.
KINDS = {
    "syntax rules": "SR",
    "general rules": "GR",
    "argument rules": "AR",
    "returned value rules": "RV",
    "arguments": "AR",
    "returned value": "RV",
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
                "page": page,
                "text": re.sub(r"\s+", " ", text),
            })
            blocks_with_rules.add(cur)
        buf, ordinal = [], 0

    for line in lines:
        if m := PAGE.match(line):
            page = int(m.group(1))
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
            buf = [m.group("text")]
        elif ordinal:
            buf.append(line)
    flush()

    # ---- completeness self-check -----------------------------------------------------------------------------
    # §5.3.2-5.3.5 are the CONVENTIONS clause: prose DEFINING what a syntax/general/argument/returned-value rule
    # is. They are rule-block headings that correctly contain no rules. Every other empty block is a parse gap.
    EXPECTED_EMPTY = {"5.3.2", "5.3.3", "5.3.4", "5.3.5"}
    gaps = [b for b in blocks_seen if b not in blocks_with_rules and b[0] not in EXPECTED_EMPTY]

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
                     ("AR", "argument rules"), ("RV", "returned value rules")):
        if by_kind.get(k):
            print(f"   {by_kind[k]:5d}  {k}  {label}")
    print()
    print("   by top-level clause:")
    for sec, n in sorted(top.items(), key=lambda kv: int(kv[0])):
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
