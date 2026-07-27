# Spec transcription repair plan — all 210 confirmed defects

> **Scope decision (owner, 2026-07-26): fix ALL of them, cosmetic included.** No tier is deferred. An earlier
> draft of this work offered to defer the structural batch as "low-risk"; that is the deferral-as-debt pattern
> `feedback_no_deferral_default` exists to prevent, and the offer was withdrawn.
>
> **SSOT** for the findings themselves is `LEDGER.json` / `REPORT.md` in this directory. This document is the
> ORDER and the MECHANISM — it does not restate findings.

## The shape of the work

210 confirmed · 41 normative · 123 structural · 46 cosmetic.

Batching is by **repair mechanism**, not by severity, because that is what makes 210 tractable. Roughly a third
are a handful of repeated mechanical patterns that should be scripted and verified in bulk; the rest need the
printed page open.

| kind | normative | structural | cosmetic | total |
|---|---:|---:|---:|---:|
| incorrect-figure | 36 | 33 | 10 | 79 |
| misplaced-content | — | 46 | 7 | 53 |
| missing-heading | — | 35 | 5 | 40 |
| incorrect-text | 4 | — | 8 | 12 |
| ocr-error | — | 1 | 11 | 12 |
| missing-text | 1 | 4 | — | 5 |
| other | — | 2 | 5 | 7 |
| missing-figure | — | 1 | — | 1 |
| summarised-not-transcribed | — | 1 | — | 1 |

## Cross-cutting rules — apply to EVERY batch

1. **The printed page is the authority.** Render it (`scripts/spec/page_workunit.py <page> --dpi 300`) and look.
   The markdown is the artifact under repair; it cannot be its own reference.
2. **Repair the FAMILY, never the instance.** The seven ON/OFF directive notes must agree with each other or the
   inconsistency merely relocates. Where a finding names a sibling, fix the sibling in the same change.
3. **After each batch, verify — three checks, all of which can fail:**
   - `python scripts/spec/extract_rule_catalog.py --check` → exit 0 (no parse gaps, no duplicate ids, no meta-text)
   - page anchors still **1261** (`grep -c '^<a id="page-' specs/ISO_COBOL.md`) — load-bearing for
     `render-spec-page.py` and the figure audit, until Batch 5 replaces them with the sidecar index; from then on
     the equivalent check is that the sidecar still resolves a sampled rule to the page its text is printed on
   - no section anchor and no page anchor appears more than once. Added after the duplicated-block batch: the
     count-based checks all stayed green while a whole duplicated block sat in the file, because duplication
     breaks uniqueness, not totals
   - the catalog count is **stable at 3,790**, or the change is explained. A repair that moves the denominator
     without explanation means the repair changed the rule structure, not just its rendering.
4. **A diagram repair is TWO items.** Fixing the markdown does not fix a grammar rule written from the wrong
   diagram. Every normative figure repair carries a grammar-impact question — see Batch 1's exit criterion.
5. **Never hand-edit generated artifacts.** `kb/Conformance/`, `kb/Reference/`, tracker §C are regenerated.
6. Commit per batch with the finding ids, and re-run `merge_reconciliation.py` so `REPORT.md` reflects reality.

## Batch 1 — Normative (41). Hand-repaired, printed page open.

The only batch that changes what the language permits. Every one is falsely restrictive: it makes legal COBOL look
illegal. Sub-order, most-load-bearing first:

1. **Misstated underlining — 21 findings, 20 pages.** Required-vs-optional words. Includes the seven ON/OFF
   directive notes (FLAG-02, FLAG-14, LEAP-SECOND, LISTING, PROPAGATE, REF-MOD-ZERO-LENGTH, TURN) which go as ONE
   commit. Also PROPAGATE's invented rule ("the underlined alternative marks the default") — no such rule exists
   in the standard; it is falsified by LISTING and contradicted by the transcription's own POP note. Delete it.
2. **Lost choice-indicator bars — 11 findings, 8 pages.** Re-render with the bars in the established house style
   (ASCII art + a `Figure notes` block stating the §5.2.6.4 consequence), as already done for GOBACK.
3. **Lost outer brackets — 6 findings, 5 pages.** CURRENCY SIGN, LOCALE, report-group USAGE and siblings.
4. **Remaining normative — 3 findings.** Includes p28 (already repaired) and the incorrect-text items.

**Exit criterion:** run `.claude/workflows/spec-grammar-impact.js` over the normative pages. Every INHERITED
verdict becomes a `CONFORMANCE-FIX-QUEUE` entry — a compiler bug, not a doc fix — carrying the exact ISO syntax
its fix must implement. GOBACK is already proven inherited; `ON SIZE ERROR` is the standing second candidate.

## Batch 2 — Mechanical, scripted (~78 findings across the structural and cosmetic tiers).

These are repeated patterns, not individual judgements. **Script the transform, verify by re-running the sweep on
the affected pages, do not hand-edit 78 sites.** Each sub-batch gets its own script under `scripts/spec/repairs/`
so the change is reviewable and re-runnable.

1. **Heading depth — ✅ DONE.** Estimated at ~42 findings; the real number was **2,039**. The PDF carries an
   EMBEDDED OUTLINE (2,090 entries, levels 1–5) — the document's own machine-readable hierarchy — and only 59 of
   2,098 markdown headings matched it. The 19 reported findings were symptoms of one systemic defect, and their
   verifiers had normalised each to LOCAL siblings rather than true depth. Mapping is outline level + 1; coverage
   extends to the outline's 374 omitted leaves because level == dotted depth for all 1,728 numbered entries
   (verified, asserted in the script). `scripts/spec/repairs/heading_depth.py`, 0 residual.
2. **Anchors / TOC folios — ~24 findings, OUTSTANDING.** Includes the **systemic** offset: TOC links are numbered
   by printed folio while body anchors use PDF page, so every TOC `#page-NNN` is off by the +30 front-matter
   offset. Fix the systemic mapping FIRST or the individual digit errors get re-broken. ⚠ **Sequence with Batch 5**
   — that batch removes page anchors entirely, so decide whether these TOC links survive at all before repairing
   them one by one.
3. **Duplicated blocks — ✅ DONE, and the estimate was wrong in the other direction.** Estimated ~8; exactly **one**
   was real (printed page 546 emitted twice, duplicating four section anchors). Four look-alikes were inspected and
   REJECTED as legitimate repetition — the standard restates identical conditions under different rules, and
   continues tables across page breaks. Had they been swept, three passages of distinct normative text would have
   been deleted **with every gate still green**. The working invariant is duplicated SECTION anchors plus stray
   structural markers, not adjacency. `scripts/spec/repairs/duplicate_block_p576.py`.
4. **Table cells that lost their line breaks — ✅ DONE.** Table 12's DELETE and WRITE rows; all five multi-entry
   cells now consistent.
5. **Spurious spaces inside numeric literals — ✅ DONE.** Both PI constants, both broken after the 29th fractional
   digit. Class-swept: exactly two in the whole file, zero residual; digit counts 31 and 33 match the printed page.

## Batch 3 — Misplaced content (53). Page-by-page judgement.

Labels attached to the previous item, sub-items detached from their parent, content filed under the wrong page
anchor, list items reordered. Not scriptable — each needs the printed page. Work in page order so the renders
batch efficiently. Where content sits under the wrong page anchor, moving it changes which page a citation
resolves to; note that in the commit.

## Batch 4 — Remaining cosmetic (46 minus those already swept by Batch 2).

OCR errors, dropped front matter, formatting. **These are fixed, not waived.** Individually harmless; collectively
they are the difference between a transcription that can be trusted and one that must be spot-checked forever.

## Batch 5 — Remove the page-break furniture entirely (owner decision, 2026-07-26)

**Markdown has no pages, and there is no reason to carry the PDF's.** The furniture is also the ROOT CAUSE of most
of what batches 1–4 repair: split sentences, running headers and anchors spliced into rule text, a duplicated
block introduced by a stray rule and a stray H1, content filed under the wrong page. Fixing the instances while
keeping the mechanism that produces them is treating symptoms.

Current furniture in a 53,163-line file: **1,261 page anchors · 1,233 `## Page N` headings · 1,249 running
headers**, plus the `---` rules around each. The `## Page N` headings are the worst of it — they sit in the heading
hierarchy that batch 2 just spent 2,039 edits making correct.

**Why this runs LAST, not now.** The anchors are load-bearing today: five tools map markdown to PDF page through
them (`render-spec-page.py`, `page_workunit.py`, `extract_rule_catalog.py`, `tracker_section.py`,
`fix_spec_pagebreaks.py`), all 3,790 catalog rules carry a `page` field, and **every open finding is keyed by
page**. Stripping the anchors mid-repair would destroy the ability to locate the work still outstanding.

**What makes it safe.** ISO citations are by CLAUSE, not page — the whole project cites `§14.9.24.3 GR5`. Page
numbers serve only VERIFICATION, proving a rule against the printed original. So the mapping does not need to live
inside the document; it needs to exist.

Order, and the order is the point — capture the mapping while the evidence still exists, the same prove-then-delete
rule the migration follows:

1. **Finish batches 1–4.** They need page location.
2. **Generate a sidecar page index** — rule/section → PDF page — built FROM the anchors before any removal.
3. **Repoint the five tools** at the sidecar, and PROVE it: sample N rules, render the page the sidecar claims,
   confirm the rule's text is actually on it. "The counts match" is not proof; a wrong-by-one mapping keeps every
   count intact. This check must be able to fail.
4. **Strip** page anchors, `## Page N` headings, running headers and the `---` rules.
5. **Re-verify:** catalog stays at 3,790 · `--check` exits 0 · the sampled renders still land correctly.

**What this deletes as a side effect** — worth having, because each is machinery we have tripped over today: the
extractor's furniture-skipping filter, its three-shape running-header special case, the `ANY_HEADING`-versus-page
terminator distinction, and `fix_spec_pagebreaks.py` in its entirety.

**Risk to state honestly:** the sidecar cannot be proven correct until it is built and spot-checked. Given how
often "looked fine" was wrong today, step 3's verification is the load-bearing part of this batch, not step 4.

## Definition of done

- All 210 findings repaired, each referenced by id in a commit message.
- `extract_rule_catalog.py --check` exits 0; anchors 1261; catalog 3,790 (or explained).
- `merge_reconciliation.py --expect 1261` shows every confirmed finding marked repaired.
- Tracker §C regenerated and reading complete.
- Every INHERITED grammar bug from Batch 1's exit criterion filed in `CONFORMANCE-FIX-QUEUE.md`.
- Batch 5 complete: the document FLOWS — no page anchors, no `## Page N` headings, no running headers — and the
  page mapping lives in a verified sidecar, with sampled renders proving it still lands on the right page.
- **Then, and only then,** the systematic grammar↔spec audit (1,659 items) runs against a transcription that can
  be trusted — which is the entire reason the reconciliation came first.
