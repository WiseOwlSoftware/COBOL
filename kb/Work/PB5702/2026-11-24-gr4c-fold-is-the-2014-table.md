**Found 2026-11-24 by the branch review of the PB5702 landing (finding R1-1).**

§2002:8.1.2.1 GR4c folds an uppercase extended letter by the case mapping of 2002 Annex D (D.2.1 and D.2.2, 600 pairs). At
--std 2002 the compiler folds with `CobolNames.Fold(cp, 2002)` (`src/WiseOwl.Cobol.Runtime/CobolNames.cs`), the 2023 Annex C
table with Annex E.2 item 14 and E.3.3 item 6 reversed, which is the 2014 table. It holds 86 pairs that 2002 Annex D does not
list (measured against `specs/ISO_COBOL_2002.md`; the Roman numerals U+2160..U+216B with U+2170.., U+2126, U+212B, Georgian
U+10A0.., Cyrillic U+0400/U+040D and others), and the printed D.2 carries three entries the 2014 table lacks, (0130,0069),
(0131,0069) and (0256,0189) as the OCR shows them.

Repro: `01 X<U+2160>Y` and `01 X<U+2170>Y` at --std 2002: COBOL1639 (one name); 2002 has two names.

Why it is not fixed with PB5702: the one comparer is the 2023 fold, and `EditionSpelling` can only rewrite a character to
the edition's fold, which cannot express "this character is not folded here" for a character 2023 folds. A 2002 fold needs a
design (an edition-keyed comparer, or a spelling no table folds), and 2002 Annex D must first be transcribed by
`scripts/spec/gen_annex_bc.py` and checked against the printed pages. Same root as the 2014 lead on this note (the table
derived from 2023). Row `2002-GR-8.1.2.1-4` is therefore PARTIAL, not CONFORMS.

**Sites:**
- `src/WiseOwl.Cobol.Runtime/CobolNames.cs` (`Fold(int, int)`, `EditionSpelling`, `Tables`)
- `src/WiseOwl.Cobol.Runtime/CobolNames.Table.cs` (whole: generated; it would gain the 2002 Annex D list)
- `scripts/spec/gen_annex_bc.py` (`SECTIONS`, `OUTPUTS`)
