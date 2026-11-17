**2026-11-17: landed (branch w20262102c, kb/Work PB4950).** `CounterResolver.LinageFileOf` tells the two causes apart.
A qualifier this program cannot see: `COBOL0863: LINAGE-COUNTER OF 'F': 'F' names no file this program can reference — a
file-name declared in another program is referenced here only when it is global, its file description entry specifying the
GLOBAL clause (ISO §1985:II.6.2.1)` (rerun of `triage85/g0/pb5906.cob` at 85). A visible file with no LINAGE clause keeps the
LINAGE message, now citing §1985:VII.3.7.4 GR9 at 85 (one `LinageCounterRule`). Pinned by
`EditionCitationTests.LinageCounterOfALocalFileOfTheContainer_At85_SaysItIsNotVisible`.

**Lead (not filed):** COBOL2729 `PAGE-COUNTER OF 'R9': the qualifier shall name a report description entry` may be the same
pattern for a report of another program that is not visible.
