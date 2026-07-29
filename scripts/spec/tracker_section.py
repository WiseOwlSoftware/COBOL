#!/usr/bin/env python3
"""Generate section C of kb/Remaining Work Tracker.md from the spec-reconciliation ledger.

The tracker is the owner's working checklist; the LEDGER is the SSOT. This mirrors one into the other rather than
letting them be maintained separately — the same relationship sections A and B already have with constructs.json
and the conformance fix-queue. Regenerating is idempotent: the section lives between HTML markers and is replaced
whole, so re-running after more sweeps never duplicates or drops content.

GRANULARITY IS DELIBERATE. The tracker's own rule for section B is "do not enumerate item IDs here (they drift) —
track them in the SSOT". 60+ individual findings would be unreadable and would drift the same way, and they are
not independently actionable anyway: the seven ON/OFF directive notes must be corrected as ONE family or the
inconsistency between them just moves. So section C is checkboxed by DEFECT FAMILY, with counts, and points at
REPORT.md for the per-finding detail and each verifier's reasoning.

    python scripts/spec/tracker_section.py            # write the section into the tracker
    python scripts/spec/tracker_section.py --print    # print it, change nothing
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
from collections import defaultdict

REPO = pathlib.Path(__file__).resolve().parents[2]
LEDGER = REPO / "docs" / "rearchitecture" / "spec-reconciliation" / "LEDGER.json"
TRACKER = REPO / "kb" / "Remaining Work Tracker.md"
BEGIN = "<!-- BEGIN generated: spec-reconciliation (scripts/spec/tracker_section.py) -->"
END = "<!-- END generated: spec-reconciliation -->"

TOTAL_PAGES = 1261

# Families are the unit of REPAIR, not of discovery. Order is repair order: normative first, and within that the
# families whose defects make legal source look illegal.
FAMILIES = [
    ("Directive figure notes — misstated underlining (required vs optional words)",
     lambda f: f["severity"] == "normative" and ("underlin" in f["why_it_matters"].lower()
                                                 or "optional word" in f["why_it_matters"].lower())),
    ("Lost choice-indicator bars — bracket reads 'at most one' instead of 'zero or more'",
     lambda f: f["severity"] == "normative" and ("choice indicator" in f["why_it_matters"].lower()
                                                 or "bars" in f["why_it_matters"].lower())),
    ("Lost brackets/braces — an optional clause reads as mandatory",
     lambda f: f["severity"] == "normative" and "bracket" in f["why_it_matters"].lower()),
    ("Other normative diagram/text defects", lambda f: f["severity"] == "normative"),
    ("Structural — misplaced content, headings, split or detached items", lambda f: f["severity"] == "structural"),
    ("Cosmetic — dropped front matter, formatting", lambda f: f["severity"] == "cosmetic"),
]


def build(ledger: dict) -> str:
    confirmed = ledger.get("confirmed", [])
    swept = ledger.get("pages_swept", [])
    # TWO independent conditions for "final", because they fail differently. Sweeping every page says the SEARCH
    # is complete; zero unverified says every claim has been ADJUDICATED. An earlier version checked only the
    # first, so a fully-swept run with outstanding verdicts would have rendered as final — publishing hypotheses
    # as work items, and acting on one risks "fixing" correct text.
    unverified = ledger.get("unverified", [])
    complete = len(swept) >= TOTAL_PAGES and not unverified

    assigned: set[int] = set()
    buckets: list[tuple[str, list[dict]]] = []
    for title, pred in FAMILIES:
        got = [f for i, f in enumerate(confirmed) if i not in assigned and pred(f)]
        for i, f in enumerate(confirmed):
            if f in got:
                assigned.add(i)
        buckets.append((title, got))

    by_sev = defaultdict(int)
    for f in confirmed:
        by_sev[f["severity"]] += 1

    if complete:
        status = "**COMPLETE** — all %d pages reconciled, every claim adjudicated." % TOTAL_PAGES
    elif len(swept) < TOTAL_PAGES:
        status = ("⚠ **Sweep IN PROGRESS — %d of %d pages reconciled. This list is NOT final; do not start "
                  "repairs until it is.**" % (len(swept), TOTAL_PAGES))
    else:
        status = ("⚠ **All %d pages swept, but %d claim(s) still have NO adversarial verdict. This list is NOT "
                  "final.** An unverified claim is a hypothesis — repairing one risks \"fixing\" correct text. "
                  "Finish verification first (see the RESUME POINT in "
                  "`docs/rearchitecture/spec-reconciliation/README.md`)." % (TOTAL_PAGES, len(unverified)))

    out = [BEGIN, "",
           "## C. Spec transcription corrections (%d confirmed — repair the markdown, not the compiler)" % len(confirmed),
           "",
           status,
           "",
           "Discrepancies between the canonical ISO PDF and `specs/ISO_COBOL.md`, found by the "
           "`spec-reconcile` workflow and each one adversarially verified. **SSOT:** "
           "[[docs/rearchitecture/spec-reconciliation/REPORT|REPORT.md]] (per-finding detail plus the verifier's "
           "reasoning) and `LEDGER.json`. Do not enumerate individual findings here — they drift; this section "
           "is generated by `scripts/spec/tracker_section.py`.",
           "",
           "**%d confirmed · %d normative · %d structural · %d cosmetic**" %
           (len(confirmed), by_sev["normative"], by_sev["structural"], by_sev["cosmetic"]),
           "",
           "Why this matters more than its size suggests: **every normative finding so far is falsely "
           "restrictive** — it makes legal COBOL look illegal.",
           "",
           "⛔ **A diagram defect is TWO items, not one.** The markdown is wrong (repair it here), and the grammar "
           "may have been written FROM the wrong diagram — in which case the compiler rejects legal COBOL and that "
           "is a **§B fix-queue bug**. Proven on GOBACK: the figure lost its choice-indicator bars, "
           "`CobolParserCore.g4` encodes `(raisingPhrase | statusPhrase)?` calling them \"mutually-exclusive\", and "
           "`GOBACK RAISING … WITH NORMAL STATUS` is rejected with *no viable alternative at input 'WITH'*. The "
           "same inheritance hit ON SIZE ERROR once already (fixed 2026-07-19). Run "
           "`.claude/workflows/spec-grammar-impact.js` over the normative pages to classify each as INHERITED "
           "(compiler bug) or NOT-INHERITED (doc-only) — with a repro, never a reading.",
           ""]

    for title, items in buckets:
        if not items:
            continue
        pages = sorted({f["page"] for f in items})
        shown = ", ".join(f"p{p}" for p in pages[:14]) + (f" … (+{len(pages)-14} more)" if len(pages) > 14 else "")
        out.append(f"- [ ] **{title}** — {len(items)} finding(s) across {len(pages)} page(s): {shown}")
    out += ["",
            "**Repair discipline:** correct each FAMILY as a whole — the seven ON/OFF directive notes must agree "
            "with each other or the inconsistency merely moves. After each family: re-run "
            "`python scripts/spec/extract_rule_catalog.py` (the catalog derives from this file) and confirm the "
            "page-anchor count is unchanged at 1261, since the anchors are load-bearing for "
            "`render-spec-page.py`.",
            "", END]
    return "\n".join(out)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--print", action="store_true", dest="show", help="print only; change nothing")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass

    if not LEDGER.exists():
        sys.exit(f"no ledger: {LEDGER}\nRun: python scripts/spec/merge_reconciliation.py")

    section = build(json.loads(LEDGER.read_text(encoding="utf-8")))
    if args.show:
        print(section)
        return 0

    text = TRACKER.read_text(encoding="utf-8")
    if BEGIN in text and END in text:
        text = re.sub(re.escape(BEGIN) + r".*?" + re.escape(END), section, text, flags=re.S)
    else:
        text = text.rstrip() + "\n\n" + section + "\n"
    TRACKER.write_text(text, encoding="utf-8")
    print(f"updated {TRACKER.relative_to(REPO)}")
    print(section.splitlines()[2])
    return 0


if __name__ == "__main__":
    sys.exit(main())
