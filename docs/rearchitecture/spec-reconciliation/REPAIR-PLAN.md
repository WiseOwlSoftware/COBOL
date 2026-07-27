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
     `render-spec-page.py` and the figure audit
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

1. **Heading levels — ~42 findings.** Four-level `N.N.N.N` subsections transcribed at the wrong depth, making them
   siblings of their parent clause. 18 in clause 13 were already swept; the rest follow the same rule. A generated
   fix plus a drift test asserting depth == dotted-number depth is better than 42 edits.
2. **Anchors / TOC folios — ~24 findings.** Includes the **systemic** offset: TOC links are numbered by printed
   folio while body anchors use PDF page, so every TOC `#page-NNN` is off by the +30 front-matter offset. Fix the
   systemic mapping FIRST, then the residual digit errors (e.g. UNSTRING 769 transcribed 766), or the individual
   fixes will be re-broken.
3. **Duplicated blocks — ~8 findings.** Whole page-bodies emitted twice, re-emitting section anchors. Delete the
   stray copy — the one lacking a page anchor and carrying flattened heading depths. Verify the surviving copy is
   the anchored one, and that anchor counts drop to exactly one per section.
4. **Table cells that lost their line breaks — 2 findings.** Table 12's DELETE and WRITE rows flattened two
   ALTERNATIVE conditional phrases into one compound phrase. Restore `<br>`, matching the three sibling rows.
5. **Spurious spaces inside numeric literals — 2 findings, and SWEEP for more.** Both breaks land after the 29th
   fractional digit of the PI constants — the signature of a fixed-width column split in the extraction pass, so
   this is a class, not two typos. Sweep every long numeric literal for the same artifact before closing it.

## Batch 3 — Misplaced content (53). Page-by-page judgement.

Labels attached to the previous item, sub-items detached from their parent, content filed under the wrong page
anchor, list items reordered. Not scriptable — each needs the printed page. Work in page order so the renders
batch efficiently. Where content sits under the wrong page anchor, moving it changes which page a citation
resolves to; note that in the commit.

## Batch 4 — Remaining cosmetic (46 minus those already swept by Batch 2).

OCR errors, dropped front matter, formatting. **These are fixed, not waived.** Individually harmless; collectively
they are the difference between a transcription that can be trusted and one that must be spot-checked forever.

## Definition of done

- All 210 findings repaired, each referenced by id in a commit message.
- `extract_rule_catalog.py --check` exits 0; anchors 1261; catalog 3,790 (or explained).
- `merge_reconciliation.py --expect 1261` shows every confirmed finding marked repaired.
- Tracker §C regenerated and reading complete.
- Every INHERITED grammar bug from Batch 1's exit criterion filed in `CONFORMANCE-FIX-QUEUE.md`.
- **Then, and only then,** the systematic grammar↔spec audit (1,659 items) runs against a transcription that can
  be trusted — which is the entire reason the reconciliation came first.
