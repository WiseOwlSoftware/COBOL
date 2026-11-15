**Superseded in part by PB5679 (2026-11-15).** The owner's model of 2026-11-15 ("later version rules can be thought of as
including a prior version's construct as is ... explicitly removing the prior version construct, or adding an entirely
new construct") puts the change record on the LATER edition's row, neighbour to neighbour
(DESIGN-spec-conformance-review.md section 10.7).

- **Decision 2 is superseded** as the target structure: "No edition is linked between 1985 and 2023, ever" and "Every
  older held edition's change record names the newest held edition (2023)". PB5679's L7 records each hop between
  neighbours; a hop over an edition with no complete pair (2014 today) is a skipping hop naming it. Its follow-up
  tooling, PB5583, is superseded with it (the dated file beside PB5583).
- **Decisions 1, 3, 4 and 5 stand.** Decision 1 is PB5679's L1; decision 3 (every word-identical pair read, a script only
  flags) is L4's read; decision 4 (evidence at the row's own edition) is L1 and PB5675; decision 5 (documented
  non-support recorded with its own 2002 verdict) is unchanged.
- **The 2002 work stands** as skipping hops 2002 to 2023 over 2014: the 704 stored 2002 KEPT records migrate to as-is
  `from` records on the 2023 rows (PB5682), each kept only when its digests still hold.
- **The stage table's rows** that write 2002 KEPT records (R0, K1, J1, J2) write `from` records once PB5682 lands; until
  then they write as section 10.6 describes.
