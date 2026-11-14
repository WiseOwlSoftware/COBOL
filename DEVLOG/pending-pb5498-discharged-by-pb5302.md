## PB5498 discharged: SEARCH at --std 85 from index -1 already ends AT END (PB5302)

The 1985 code-read lead said an index started at -1 evaluates the WHEN at -1, 0 and 1. Re-probed on main
66ff5cbc77, the reader's program prints `B0 AT END IX=+0`: the incremented index 0 is outside the range, which is
§1985:VI.6.22.4 GR2b. The mechanism is PB5302, landed in 56417bbd82 with golden
`14.9.37-SEARCH/pb5302_search_advance_below_table_85`. The note is retired; no source changed. Gate: inert lane.
