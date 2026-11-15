**Superseded in part by PB5679 (2026-11-15).** The owner's model of 2026-11-15 ("later version rules can be thought of as
including a prior version's construct as is ... explicitly removing the prior version construct, or adding an entirely
new construct") puts a lineage relation on the LATER edition's row, naming its predecessor (PB5679 D1). PB5679 lands that
decision, the requirements the change of record format must meet and the open questions; it designs no switch yet
(DESIGN-spec-conformance-review.md section 10.7).

- **Decision 2 is superseded** as the target structure: "No edition is linked between 1985 and 2023, ever" and "Every
  older held edition's change record names the newest held edition (2023)". Its follow-up tooling, PB5583, is
  superseded with it (the dated file beside PB5583).
- **Decisions 1, 3, 4 and 5 stand.** Decision 1 is PB5679 D2's ground; decision 3 (every word-identical pair read, a
  script only flags) is D1's read; decision 4 (evidence at the row's own edition) is D2 and PB5675; decision 5
  (documented non-support with its own 2002 verdict) is unchanged.
- **The 2002 work stands.** The 704 stored 2002 KEPT records keep today's format until the switch (PB5682), which must
  record their verdicts as `shared`, never `adjudicated` (PB5679 D3, Q1), and whether a shared verdict is enough to
  close a row is open (PB5692). The stage rows that write 2002 KEPT records keep writing them in today's format.
