**2026-11-17: landed (branch w20262102c).** `SystemClock.Now` throws `CobolImplementorFatalException` for a set value
that does not parse; an unset or empty variable reads the system clock (an empty value is the same as unset on Windows). Rerun of
`triage85/g5/src/clk.cob` at 85 with `WISEOWL_COBOL_CLOCK=not-a-date`: `EXIT: 1`, stderr `abnormal run-unit termination:
WISEOWL_COBOL_CLOCK='not-a-date' is not a date-time this runtime can read ...`; with the pin `2001-02-03T04:05:06+09:00`:
`D[010203] T[04050600]`, exit 0. Pinned by `CurrentDateOffsetPinTests.UnparseablePin_StopsTheRun_NamingTheVariable`.

**Sibling inputs, not changed (lead):** `RuntimeConfig` documents `WISEOWL_COBOL_*` collation cache variables whose value
"anything else = the default" (the cache size and eviction): the same silent fallback for an unresolvable value.
