## PB4968 and PB4969: a 1985 citation names one rule, and the conformance requirements are the frame

**PB4969 (re-probed).** 43 citations in the 1985 rule catalog named two rows each: a rule of a restarted numbered list
(`1985-GR-IV.4.2.2.2.3-L2.1`) carried the same citation as rule 1 of the block's first list (`§1985:IV.4.2.2.2.3 GR1`). The
fix is at the format: `citation.py` gains `SUBLIST_RULE` and `rule_designator`, the one writer of a rule designator
(`GR1` in the first list, `GRL2.1` in the second); `parse` reads and canonicalizes `GR L2.1`, so `cite.py --check` takes it.
`rule_catalog_1985.parse` writes the designator, the committed 1985 catalog was regenerated (54 sub-list rows re-cited), and
`Catalog.shared_citations` makes `extract_rule_catalog.py --edition 1985 --check` fail on two rows sharing a citation (the
rows cited by a printed page are exempt: the page is no rule's citation). Self-tests of both scripts cover the format, the
canonical spelling, the rejection of `GRL2` and the shared-citation finding.

**PB4968 (decision, no row).** The 1985 catalog has no rows for §1985:I.1.5 (the subsets, the optional modules, the obsolete
elements of §1985:I.1.5.2.3): unnumbered prose. The 2023 audit treats §2023:4.2 the same way: it frames the definition of DONE
and docs/CONFORMANCE.md by direct citation, and only its numbered lists (§2023:4.2.10) are rows. Both editions now say so in
DESIGN-spec-conformance-review.md §10.1: PB4885's 1985 conformance statement cites §1985:I.1.5 and its sub-paragraphs directly.

Rows closed: none (`closes_rows: []` on both). Citations checked with `cite.py --check`: §1985:I.1.5.2.3, §1985:I.1.5.1,
§2023:4.2.10, §1985:IV.4.2.2.2.3 GRL2.1. Self-review: the tooling lane only, one writer of the designator, no second copy.
