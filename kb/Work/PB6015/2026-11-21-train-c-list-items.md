## Seven list-item designators grown onto the baseline in train-1985-c (group m2)

Train C's merges brought in seven citations of an unlabelled list item. The citation grammar has no canonical form for
one, so `audit_code_citations.py --canonicalize` leaves each one unread (`designator-not-canonical`). They are
`§2023:9.1.13.2 item 3` (twice) and `§2023:9.1.13.6 item 7` in `conformance-suite/cases/III@1985-section-preamble/1985-GR-III_2-p100.cob`,
`§1985:IV.4.2.2.2.3 item 6` in `DiagnosticCatalog.cs`, `§2002:8.3.1.1 1)` and `§2014:8.3.1.1 1)` in `ClosedFormats.cs`, and
`§1985:B.2.1 item 9` in `scripts/spec/text_units.py`. They belong to the same class as the I-O status items and list items
already on the baseline (`§2023:9.1.13.2 item 6` and the others). They were added with `--write-baseline --grow`. They
leave the baseline when this note gives the grammar a list-item designator.

The same pass took the canonicalizer's newly readable citations that named a wrong rule and fixed each at its text,
without growing the baseline: the sub-item b of §2023:12.3.8.3 SR6 (which has none) became SR6 (5 sites), item c of
§2023:14.9.35.4 GR9 became GR9a, item f of §2023:13.18.57.4 GR6 became GR6, `§2023:14.9.33.4 GR2a.1`'s NOTE quotation was moved to GR2b, and the quotation of
`§2023:8.4.6.2.1 GR1` was given its own citation. Two citations are right against the printed page, but the
transcription joins their sub-items onto one line, so the catalog reads only the first: item c of §2023:14.5.3.3 GR1 (3 sites) and item d of §2023:13.18.38.3 SR25. They were written as the rule with "its item c" / "its item d". The amendment pages cited
as §1985 that group s's PB5059 rule now refuses came in from branches cut before it, at 38 sites: `§1985:p.B-46` became
`§1993:p.B-46` (cite.py OK) and `§1985:p.A-28` became `§1989:p.A-28` (cite.py OK). The new run-time message text of
`ExceptionState` and its unit test changed with them.
