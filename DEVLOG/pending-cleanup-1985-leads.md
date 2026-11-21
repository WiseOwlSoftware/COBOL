## A cleanup train of nine small 1985 review leads

Nine review leads from the 1985 work, each checked on main before it was fixed. Three citations were repaired against the
standard: the operator-spacing note now states the condition of `§1985:VI.6.2.3 SR4` (a left parenthesis before a unary
operator is required only when the expression immediately follows an identifier or another arithmetic expression), D-SKE
quotes the SORT rule (`§2002:14.8.36.2 SR6d`, "key data-names") and the MERGE rule (`§2002:14.8.23.2 SR4d`, "key data-name")
each in its own words, and the three `§1985:C.1 item <n>` sites of PB5997 take a form cite.py reads. The spaced designator
`SR1 b)` is `SR1b)` in the comments and two diagnostic strings of the five files named (the same spelling remains in other
files and in the runtime's pinned messages: not swept here). `ReportUnidentified`'s comment now says it quotes the source
text, and the 1985 omitted-ORGANIZATION face (COBOL0863, COBOL1858) reads `ANSI §1985:VII.2.6.3 GR3`, with its golden; pb742's loosened
expectation is split into a 2002-2023 pin and a new 85 case that pins the 1985 wording.

The page furniture of the 1985 transcription now has one reader, `rule_catalog_1985.furniture`: `parse`, `_bodies` and
cite.py all read it. `_bodies` had been reading running headers after a page break that stood in a region of its own
as text, which made five spurious units of unnumbered text (X.3.1.4, XIII.3.10.7 twice, XIII.3.19.4, XIII.3.20.3) whose
margin-level records are dropped from `margin-levels-1985.json` (381 to 376 records); cite.py now reads a quotation across a
folio or a reconstruction's closing note as the catalog does (a new self-test). Lead 3 (four unused usings) was already
fixed on main.
