## Entry 3084 — 2026-11-04 10:50 PST — PB4547 and PB4577: COBOL 2014 allows a nested table's enclosing subscripts on SEARCH identifier-1

The owner first decided that PB4547 would be a feature request for a future dialect, if that was correct. It was not. The 2002 text and the 2009 committee draft say "Identifier-1 shall not be subscripted or reference modified". The later drafts (202x CD 1.3 and the 2023 CD2) carry the 2023 rules 2) and 3), which allow the enclosing tables' subscripts. The 2023 Annex E lists the substantive changes since 2014, relaxations included, and has no SEARCH entry, so the relaxation came in the final 2014 standard. The 2002 Annex F.2 lists no SEARCH relaxation over 1985 either. Asked with that evidence, the owner decided that 2014 allows it.

PB4547 is now a `-std=cobol2014` record of refused legal source, edition 2014, to be reported to bugs with this reasoning in its why section. PB4577, our own defect, narrows to 85 and 2002: COBOL2075 must refuse the subscripted identifier-1 there, and the 2014 golden stays.

