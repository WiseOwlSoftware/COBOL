## Entry 3396 — 2026-11-09 16:16 PST — cite.py checks the 1989 and 1993 amendment citations (PB5043)

`cite.py --check "§1993:p.B-16"` refused with "the 1993 transcription does not exist", and the catalog's `cite_check` looked as if it accepted
the same rows only because `citation.validate` treats an edition without a transcription as "parsed only": the 201 amendment citations
the 1985 catalog emits (190 Section A paragraph rows of 1989, 2 page rows of 1989, 9 page rows of 1993) were checked by nothing. The amendments are printed inside
`specs/ANSI_COBOL_1985.md`, so the fix is one resolver: `citation.amendment_regions` runs the 1985 region reader over the lines of one
`# AMENDMENT n:` heading (Amendment 1 with its Section A, Amendment 2 with printed pages only), `SOURCE_EDITION` points both tokens at the
1985 file, and the CLI, `validate`, the catalog and both citation audits share it. All 2,266 catalog citations now validate; a 1993 page is
no 1989 page; a phantom is refused. Self-tests: real and planted cases in `citation.py --self-test`. The 1985 universe still includes the
amendments' folios (a `§1985:p.B-16` validates); left as a lead. Gate: see the landing record.

