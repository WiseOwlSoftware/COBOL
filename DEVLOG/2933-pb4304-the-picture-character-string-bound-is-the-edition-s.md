## Entry 2933 — 2026-11-01 01:34 PST — PB4304 - the PICTURE character-string bound is the edition's: 30 at COBOL-85, 50 at 2002, 63 from 2014

**Mechanism.** `PictureAnalyzer.AnalyzeCharacterString` measured character-string-1 against one constant, 63, at every
edition (ISO §13.18.40.3 SR4 is the 2023 text's number). The 2002 text's own SR4 reads 50 and its Annex F.2 item 94 records
the increase "from 30 to 50", so COBOL-85 is 30. Re-probe on the unfixed build: 31, 51 and 63 written X's compiled at
every `--std`; 64 drew COBOL2146 at every `--std` (correct).

**Fix shape.** The bound is ONE function, `PictureAnalyzer.MaxStringLength(dialectLevel)`, in the shape of
`CobolLiteral.MaxPositions` (PB1615): two RELAXATIONS, each edge a constructs registry row
(`picture-string-50-2002`, `picture-string-63-2014`) read through `ConstructRegistry.IsAvailable`, so the number of an
edition and the version matrix that samples it cannot name different editions. COBOL2146 names the edition's bound and
where 63 begins, and is an error under `--permissive` too (a length error, nothing was removed). The 2014 half of the
note's claim was refuted as a 50: GnuCOBOL's `cobol2014.conf` carries `pic-length: 63` beside `word-length: 31`, and Annex E
records the word-length increase but no picture-string increase, so 2014 = 2023 (derived edge, VCR row 7.46).

**Goldens.** `cases/13.18.40-PICTURE/pb4304_picture_string_50_2002` (the 50 boundary, accept 2002+) and
`pb4304_picture_string_63_2014` (the 63 boundary, accept 2014+); refusals `pb4304-picture-string-31-at-85` and
`pb4304-picture-string-51-below-2014`; `wiseowl/cases/13.18.40-PICTURE/pb531_picture_repetition_factor` kept its 85
accept with its R3 leg now the 30-character 85 boundary and a header that no longer claims SR4 reads identically at
every edition; unit `PictureStringLengthTests`. A tree-wide scan of every `.cob`/`.cpy`/`.cs`/`.json` for a PICTURE over 30
written characters found no program that a 30-character 85 bound would newly refuse.

**Register.** Row SR-13.18.40.3-4 stays CONFORMS with its notes restated (they said the text read identically at all four
editions); the GAP count stays 19. VCR row 7.46 added; the `DATA_MODEL_DESIGN.md` PICTURE-shape paragraph carries the
edition-keyed bound.

**Review round 1.** The reviewer found the 2002 clause number wrong: the bound is the 2002 text's 13.16.38.2 SR4 (PICTURE
clause, t2002.txt:16902 under the heading at :16889), not 13.16.39.3, which is the PRESENT WHEN clause's general rules. The
number had been inherited from the note and never re-derived; it is corrected in the `MaxPictureStringLength2002` comment,
VCR row 7.46, the `picture-string-50-2002` constructs row, the SR-13.18.40.3-4 inventory notes (by `record_verdicts`
batch, not by hand) and the note. F.2 item 94 ("from 30 to 50", :44179) was re-checked and stands.

