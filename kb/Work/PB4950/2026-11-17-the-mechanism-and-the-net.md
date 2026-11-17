**2026-11-17: the mechanism, the net and the first sweep (branch w20262102c).** The design is written in
`docs/VERSION_TEST_MATRIX_DESIGN.md` P2.3a.

- **One decider.** `EditionInfo.Cite` (Editions layer) replaces `EditionContext.Cite`; its 21 callers moved. A decline's message
  at 85 is the descriptor's `Title1985`, rendered by `EditionContext.Declined` through `Cite`. `DiagnosticDescriptor.No1985Rule`
  records in the catalog why a rule has no 1985 counterpart.
- **The net.** `EditionCitationNet`, asked by `WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic` of every refusal compiled at 85.
  Measured with an empty allowlist on this branch's first build: 1810 of the 2459 refusals at 85 failed, 3271 diagnostic lines of
  319 codes; 1615 of the lines (18 codes) are edition gates ("(targeting COBOL-85)"), which cite the edition where the construct
  begins and pass. The other 1656 lines, 301 codes, are the allowlist
  `conformance-suite/wiseowl/edition-citation-pending-1985.json`, each entry naming this note and its measured count.
- **First sweep.** COBOL1964, COBOL1965 (kb/Work PB5530) and COBOL1778 (PB5875) left the list; COBOL0863's LINAGE-COUNTER arms
  (PB5906) and COBOL6764 (PB5488) cite 1985 now, though COBOL0863's other messages keep it on the list. Re-measured at the end
  of the branch: **298 codes, 1638 diagnostic lines** remain. The largest: COBOL1639 103 (kb/Work PB5627's sites), COBOL1757 66,
  COBOL1627 61, COBOL5506 52, COBOL2796 47, COBOL2269 43, COBOL0844 39, COBOL2199 38, COBOL1934 34, COBOL1970 29, COBOL0901 27.
- **Not covered by the net.** Diagnostics no refusal of the wiseowl manifest draws at 85; the edition gates' own citations (they
  cite 2023 where the construct begins in 2002, a second question this note's design names: "the edition where the construct
  begins"); the 2002 and 2014 directions.

This note stays open: the allowlist is its remaining work, and it shrinks one code at a time.
