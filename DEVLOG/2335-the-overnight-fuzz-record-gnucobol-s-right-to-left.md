## Entry 2335 — 2026-10-21 14:53 PDT — The overnight fuzz record: GnuCOBOL's right-to-left exponentiation is a bug, not a choice

The owner challenged the record's verdict that GnuCOBOL grouping `2 ** 3 ** 4` from the right is a "deliberate but
undocumented choice": why would a compiler writer choose right associativity when the standard (section 8.8.1.2 item 3)
says left to right? A rule written on purpose is not a rule chosen against the standard. The check that could separate
the two came out empty: all 36 files of GnuCOBOL 3.2's test suite hold no chained exponentiation, the grammar rule carries
no comment, and neither ChangeLog nor NEWS mentions it. Nobody asserted the behaviour knowingly; the grammar applies
Fortran's convention. The record's table row and its header now say four bugs, none a documented choice.

