## Entry 2838 — 2026-10-30 21:46 PDT — A declined SEND/RECEIVE name operand is refused with the one clause that prints it (PB4169)

PB4118's `DeclinedFacilityBinder.ScreenName` passed one fixed rule sentence, "(ISO §14.9.38.2, §14.9.31.2)", to the
shared data-name shape screen, so a subscripted RECEIVE data-name-1 was told SEND's format as well and a subscripted SEND
TO or RETURNING operand RECEIVE's. Re-probe on the landing build: reproduced for `pb4118-receive-data-name-1-subscripted`
and `pb4118-send-to-subscripted` (COBOL2024 text ended with both clauses).

Fix: `ScreenName` takes the clause (`SendClause` = ISO §14.9.38.2 for SEND TO and RETURNING, `ReceiveClause` = ISO
§14.9.31.2 for RECEIVE data-name-1 and data-name-2) and builds the sentence from it; `ScreenSendRequiredPhrases` uses
`SendClause` too, so the file spells each clause once. Both citations were run through `cite.py --check` (OK). The
reference-modified and special-register refusals of `ScreenDataNameShape` never carry the sentence, so they needed no
change. Sibling sweep: the other `ScreenDataNameShape` callers (`DataBinder`, `KeyedIoBinder`) pass their own single
clause; no other fixed two-clause sentence exists.

Pinned by: the `expect` text of the three subscripted `pb4118-*` diagnostics goldens (now the full message ending in
the one clause), `DeclinedFacilityOperandDriftTests` (the census pairs each screened operand with its clause constant;
a new test checks the constants and that no sentence names both). No inventory row claimed or closed.

