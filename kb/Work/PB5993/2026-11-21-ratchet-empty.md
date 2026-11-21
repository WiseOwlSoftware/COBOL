## 2026-11-21 — the ratchet is empty; the No1985Rule determinations audited (wave 20262131 group q, branch train85b-cite2)

**The 175 left.** Each descriptor on `EditionCitationCatalogDriftTests.Unmoved` at dda8ee6e75 was derived from the 1985 text by a
read-only reader (nine slices; records `E:/COBOL-coord/scratch/w20262131q/out/q-01..09.json`, every 1985 citation through
`cite.py --check`) and applied by one script: 126 now carry `Rule1985`, 49 `No1985Rule` (each with the 1985 clause that would hold
the rule), and 45 site edits route a message that cited only a 2023 rule at 85 through `EditionInfo.Cite`/`CiteRule`, the 2002,
2014 and 2023 text unchanged. Wrong 1985 rules the readers found and fixed: COBOL0892 (`CURRENCY SIGN IS 5` is §1985:VI.4.5.4 GR11),
COBOL1672 (§1985:VII.3.4.3 SR2), COBOL1692 (§1985:XI.3.2.3 SR3), COBOL2572 (§1985:XII.2.3 SR9), COBOL1585 (no FROM phrase in 85's
VALUE Format 2). The test is STRICT now: no list.

**The No1985Rule audit.** 363 descriptors carried `No1985Rule` at dda8ee6e75 (319 under the 42 family constants, 44 with their own
sentence). Each family's members were read against their emission sites (the message each site builds at 85) and a probe of the
suite's goldens at 85. Four were reversed to `Rule1985`, each reached at 85 by 1985 source with a message that already cited the
1985 rule: COBOL2159 (§1985:X.4.5.3 SR2), COBOL1547 (§1985:III.2), COBOL1548 (§1985:IV.4.2.2.2.3), COBOL0869 (§1985:VI.6.23.2). The
rest stand: each family names a construct 1985 lacks (the edition gate refuses it first) or a rule a later edition added; the
report lists each family with a checked citation of the 1985 clause that would hold it. `EditionCitationNet.Contradicted` now
fails a diagnostic at 85 that cites a 1985 rule under a code whose every descriptor says 1985 has none (it caught COBOL0869).

**Sibling:** the construct row `table-sort-2002` gated with COBOL0870, the code of `picture-with-fixed-width-binary-usage`; it uses
the edition gate's code COBOL0900 now.

Leads filed: PB6024 (a report refused at 85 for a page width 1985 lacks), PB6025, PB6026, PB6027, PB6028, PB6029, PB6030.
