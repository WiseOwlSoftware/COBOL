**2026-11-17: the mechanism, the net and the first sweep (branch w20262102c).** The design is written in
`docs/VERSION_TEST_MATRIX_DESIGN.md` P2.3a.

- **One decider.** `EditionInfo.Cite` (Editions layer) replaces `EditionContext.Cite`; its 21 callers moved. A decline's message
  at 85 is the descriptor's `Title1985`, rendered by `EditionContext.Declined` through `Cite`. `DiagnosticDescriptor.No1985Rule`
  records in the catalog why a rule has no 1985 counterpart.
- **The net.** `EditionCitationNet`, asked by `WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic` of every refusal compiled at 85.
  Measured with an empty allowlist on this branch's first build: 1810 of the 2459 refusals at 85 failed, 3271 diagnostic lines of
  319 codes; 1615 of the lines (18 codes) are edition gates ("(targeting COBOL-85)"), which cite the edition where the construct
  begins and pass. The other 1656 lines, 301 codes, became the RATCHET
  `conformance-suite/wiseowl/edition-citation-pending-1985.json` (owner: this note): code → refusal id → the exact number of
  such lines that refusal draws. A grown count fails (a new site, under a listed code too), a shrunk one fails until it is
  lowered, a zeroed one until its entry is removed, and `EditionCitationNetDriftTests` fails an entry whose refusal is not
  compiled at 85 (review R1-1). Keyed (refusal, code), not (code, citation): a second site of a code inside one refusal raises
  that refusal's count, where a citation key would pass a site printing a citation already listed elsewhere. Proof it holds
  for a listed code: a probe build that reported a second COBOL1970/COBOL1971 line citing only §2023:12.3.2 failed 22 of the
  2459 refusals at 85, 20 of them "COBOL1970/1971: 2 line(s) ... where the ratchet counts 1 — a NEW site" (probe reverted).
- **First sweep.** COBOL1964, COBOL1965 (kb/Work PB5530) and COBOL1778 (PB5875) left the list; COBOL0863's LINAGE-COUNTER arms
  (PB5906) and COBOL6764 (PB5488) cite 1985 now, though COBOL0863's other messages keep it on the list. Re-measured at the end
  of the branch: **298 codes, 1482 (refusal, code) entries, 1638 diagnostic lines** remain. Among them the witness
  `1985-SR-VI_4_3_3-1`, which now pins COBOL1971 (kb/Work PB5905): its one COBOL1971 line cites §2023:12.3.2 at 85 and is
  counted (COBOL1971 holds 5 refusals, 1 line each). The largest: COBOL1639 103 (kb/Work PB5627's sites), COBOL1757 66,
  COBOL1627 61, COBOL5506 52, COBOL2796 47, COBOL2269 43, COBOL0844 39, COBOL2199 38, COBOL1934 34, COBOL1970 29, COBOL0901 27.
- **Not covered by the net.** Diagnostics no refusal of the wiseowl manifest draws at 85; the edition gates' own citations (they
  cite 2023 where the construct begins in 2002, a second question this note's design names: "the edition where the construct
  begins"); the 2002 and 2014 directions.

This note stays open: the allowlist is its remaining work, and it shrinks one code at a time.
