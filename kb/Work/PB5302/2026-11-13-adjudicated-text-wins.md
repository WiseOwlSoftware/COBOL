**Adjudicated 2026-11-13 (w20262020e): the text wins.** §1985:VI.6.22.4 GR2b is a general rule and normative: after an
unsuccessful pass the process repeats "unless the new value of the index-name settings for identifier-1 corresponds to a table
element outside the permissible range of occurrence values, in which case the search terminates as indicated in 2a above".
"Outside the permissible range" names no bound, so both apply. Figure 1, which loops back to the "greater than" test only, is
explanatory: §1985:VI.6.22.4 GR10 says "This figure is not intended to dictate implementation". §2023:14.9.37.4 GR4 keeps the
same wording for the advance, so the reading is the same at every edition; from 2002 the start test already leaves no index below 1
to increment. The ISO text controls (CLAUDE.md rule 1), so no implementor latitude arises and GnuCOBOL is not consulted. Each
citation was checked with `scripts/spec/cite.py --check`.

Re-probe on the base tree (45199b9c36): the reviewer's `m1.cob` printed `FOUND AT 2` at 85 and `AT END` at 2023. After the fix
both print `AT END`, and `pb5121_search_index_below_table_85` (start index 0) still prints `FOUND AT 2`.
Witness: `conformance-suite/cases/14.9.37-SEARCH/pb5302_search_advance_below_table_85` (`AT END`).
