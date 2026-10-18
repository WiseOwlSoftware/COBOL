## PB700: UNLOCK on a sort-merge file goes through the one SD screen (COBOLNET1692), and the descriptor quotes the standard

PB700 recorded that `FileLockBinder.BindUnlock` screened a sort-merge (SD) file with its own inline test and a raw
`COBOLNET1512` literal, while CLOSE, DELETE, DELETE FILE, OPEN, READ, REWRITE, START and WRITE share
`StatementValidation.ScreenSortMergeFile` (COBOLNET1692, ISO §13.4.6.3 SR3/SR4). Re-probe on this tree: `UNLOCK S.` on an
SD still reported `COBOLNET1512: UNLOCK may not name the sort/merge file 'S' (ISO §14.9.47.3 SR1)`. COBOLNET1512 was
therefore spanning two unrelated rules (the AUTOMATIC lock-phrase ban and this one) and `DiagnosticCatalog`'s 1692
descriptor said UNLOCK was already covered.

The fix is at the screen. `ScreenSortMergeFile` takes an optional `verbRule`; UNLOCK passes `§14.9.47.3 SR1`, the one
clause in the standard that restates §13.4.6.3 SR3 in its own words (`cite.py --find "sort file or a merge file"` finds
that clause alone), and the message cites both. `BindUnlock` lost its inline test and its `name` local. The 1692
descriptor had put quotation marks around a sentence ("A sort-merge file is referenced only by a SORT, MERGE, RELEASE,
or RETURN statement") that the standard does not contain; it now quotes §13.4.6.3 SR3 and §14.9.47.3 SR1 verbatim, both
checked with `cite.py --check`. A scratch sweep of every quoted fragment in every `DiagnosticCatalog` descriptor, 425
segments of five words or more matched against the normalized standard, found no other invented sentence.

Goldens: `unlock-sort-file` now expects `COBOLNET1692: UNLOCK may not name the sort-merge file` (it pinned the old
message), and `unlock-record-sort-file` and `unlock-records-sort-file` cover the format's other two spellings, each
refused at 2002, 2014 and 2023. The USE-statement SD screen (`ProcedureTableBuilder`, §14.9.49.3 SR2) is another
statement's rule and is unchanged. No inventory row changed verdict (`closes_rows: []`).
