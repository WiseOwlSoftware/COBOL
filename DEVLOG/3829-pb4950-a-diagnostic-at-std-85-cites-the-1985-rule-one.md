## Entry 3829 — 2026-11-17 14:36 PST — PB4950: a diagnostic at --std 85 cites the 1985 rule — one decider, a net over every 85 refusal, and eight notes of the 1985 triage

**What.** The owner's decision of 2026-11-07 (kb/Work PB4950) is that a diagnostic cites the rule of the edition the program is
compiled under. This change gives that decision one mechanism and a net, and fixes the 1985 diagnostic-text notes the 85 triage
confirmed.

- **One decider.** `EditionInfo.Cite` (moved from `EditionContext`, its 21 callers changed, no forwarder) chooses a citation, or
  the words a message quotes, by edition. A decline's message at 85 is its descriptor's new `Title1985`, rendered by
  `EditionContext.Declined`; `DiagnosticDescriptor.No1985Rule` records why a rule has no 1985 counterpart. Design:
  `docs/VERSION_TEST_MATRIX_DESIGN.md` P2.3a.
- **The net.** `EditionCitationNet` asks every diagnostic of every refusal the wiseowl manifest compiles at 85. With an empty
  allowlist, 1810 of the 2459 refusals failed: 3271 diagnostic lines of 319 codes, of which 1615 lines (18 codes) are edition
  gates that rightly cite the edition where a construct begins. The other 301 codes went on a ratchet,
  `conformance-suite/wiseowl/edition-citation-pending-1985.json` (owner kb/Work PB4950): code → refusal → exact line count. A
  grown count fails, a shrunk one fails until lowered, a zeroed one until removed; 298 codes, 1482 entries, 1638 lines remain.
  A probe that added one 2023-only line under the listed COBOL1970/1971 failed 22 refusals.
- **The notes.** PB5530 (SEARCH ALL's COBOL1964/COBOL1965 cite and quote §1985:VI.6.22.3 at 85, one rule map), PB5875
  (COBOL1778's 1985 ground), PB5906 (LINAGE-COUNTER OF a file the program cannot see says so), PB5488 (COBOL6764 quotes its rule
  exactly), PB5905 (no area-A message beside COBOL1971), PB5667 (one verdict for a function-identifier in SEND WITH), PB5636 (a
  VALUE literal over the digit capacity draws COBOL0802 alone), PB5839 (an unparseable `WISEOWL_COBOL_CLOCK` stops the run),
  PB5540 (VCR row 7.16). PB5592 closed as fixed on main, rerun here. PB5530 is half: its VCR row and construct anchor remain.
- **Also.** The construct rows `report-control-reference-modification-2002` and `report-control-name-qualification-2002` cite
  the SUM RESET operand's rules and write their 2002 rules in the edition form (PB5910 review lead R2-1).

**Goldens changed:** `1985-SR-VI_4_3_3-1` expects COBOL1971 (it pinned the extra COBOL3734). No other expected output changed.

