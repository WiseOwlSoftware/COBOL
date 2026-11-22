## The second pass is complete and repaired; the note stays open for the next full pass

All 976 pages were compared a second time (chunks 1 to 4, `1985/compare-*.json`, `1985/verify-*.json`): 236 claims, 167
confirmed (15 normative, 142 structural, 10 cosmetic), 69 refuted, each claim with its own adversarial verdict. Every confirmed claim
is now in `LEDGER-1985.json` (claims with `pass: 2` and the id of their compare file), decided and on its page:

- **121 heading claims** (kind `missing-heading`): all heading LEVELS, not missing headings. They are repaired by the rule of
  kb/Work PB6057 (`heading_levels.normalize`: 1,228 heading-type lines changed, each a ledger sweep `heading-levels(PB6057)` or an
  edit of the claim that named it). Nine of them name a clause already at its derived level whose siblings were the defect
  (p149-1, p278-1, p307-1, p326-1, p397-1, p465-1, p561-1, p877-1, p903-1): decided `ALREADY`. The audit run on the text before
  the repair flags 112 of the 121 claims' pages, and the siblings of the other nine.
- **Box sides** (level 2): pages 237, 257, 290, 371, 522, 544, 580, 665, 667, 680, 684, 703, 920 and 413 (the OPEN format's box),
  and the square brackets and pipes the first pass left around a box in prose (pages 378, 453, 462, 505). The ledger's `boxes`
  follow (`blocks` of boxes 32, 193, 507, 544, 602, 609 and 703 raised; box 733 on page 920 added).
- **`CHARACTERS` in the BLOCK CONTAINS braces is not underlined** (pages 201, 202, 204, 444, 531, 533): the sweep found two more
  pages with the same defect (396 and 486), read on page 396. The figure verdicts of those figures carry the change; the five one-row formats a level-2 box now encloses (pages 237, 290, 580, 680, 703) have verdicts of their own, so `sweep_figures.py --edition 1985 --check` stays clean for them (it still reports page 634 3.20.2, a block the PDF page does not locate, as it did before this change).
- **Summary-table indentation** (pages 32, 34, 54, 738, 741) read on the rendered pages; page 738's three clipped `Z` flags
  (Debug, obsolete) replace the first pass's `KEPT` (the glyph is clipped at the scan's edge exactly as on page 734).
- **Section dividers** (pages 20, 232, 688, 698 and the other fourteen): a `# SECTION` line after the previous page's folio is gone
  and the page's own title is the level-1 heading.
- Text: page 23 `of` (not `or`), page 933 `the` (not `thc`); page 772's correction note (C171) sits where the deleted sentences were
  printed.
- **Not transcribed, decided `KEPT`:** pages 815 to 818 hold the index's heading and folio only, as pages 807 to 814 and 819 do
  (Errata C337: Amendment 2 replaces the index whole and prints the replacement).

`python scripts/spec/verify_publishable.py --edition 1985` is PUBLISHABLE with the heading audit in it.

**The note is not closed.** It closes when a full pass finds nothing, and the pass that found 167 repairs read the text the
repairs now change. The next full pass (a third, over the repaired text, with the lint's headings no longer a source of claims)
is the owner's quota decision: pass 2 cost 23.7M subagent tokens, and its heading claims (73 % of the confirmed claims) will not
recur.
