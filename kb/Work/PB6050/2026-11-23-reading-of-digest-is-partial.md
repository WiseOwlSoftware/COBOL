**2026-11-23, lead filed by the enqueue clerk (batch 20261123c).** Source: `E:/COBOL-coord/scratch/reports/w20262143d-PB6054-report.md`, Review findings R1-1 (low).

`conformance-suite/harness/coverage.py:535`: the PB6050 R1-1 fix is partial. `reading_of` digests the source of `frames_of`, `operand_ok`,
`token_of` and `Matcher`, but not the tables `operand_ok` reads (`NAMES`, `LITERALS`, `EXPRESSIONS`, `DIVISIONS`), nor `Catalog.credit` and
`Catalog.without_context` (the crediting logic), `tokens_of`, `camel` or `FUNCTION_ROW`. Its docstring and `DESIGN-conformance-suite.md` §3.4 say
the digest holds the matcher and its tables, so "unmet raising on a changed reading" is still not true for these. Fix by digesting the tables and
the crediting functions, or narrow the docstring and design text to what the digest holds.

Also from the same report (not a WiseOwl defect, not filed here): the Debug-module tests (XV.*, g85_z07, pb5768) are non-portable only because
the gnucobol adapter passes no `-debug`; a per-test flag in `conformance-suite/adapters/gnucobol.json` would turn them into measured verdicts.
