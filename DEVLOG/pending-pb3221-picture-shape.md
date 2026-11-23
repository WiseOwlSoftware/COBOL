## PB3221: the PICTURE composition hands the analyzer its resolved shape, and the two lists of digit positions become one

kb/Work PB3221 (an R2 duplication finding of the PB1754 architecture review): `PictureComposition.Validate` resolved a
character-string's floating strings, digit positions and 'P' split, threw them away, and `PictureAnalyzer` derived them
again: `FloatingString` for the geometry, `HasFloatingString` once per FOR-form rule in `ValidateEditing`, `FloatingMarks`
for `SeparateFixedFromFloating`, and its own `IsDigitAnchor` P classifier. Each re-ran `Tokenize` and `MarkFloating`.

**The re-probe** (run_probe at 2023 on the base ae1136f781) showed the note's disagreement was already live rather than
latent. The composition's digit positions counted a floating '+', '-' or currency string but not an extended editing
sign control symbol's; the analyzer's counted every floating symbol. `PIC LLLPPV EDITING L FOR NEGATIVE IS "-"` was
refused (COBOL1935: Validate read the 'P's as a LEADING string, which 'V' may not follow), which rejected legal source.
`PIC LLPP99` bound, and the analyzer, which saw the 'P's between two digit positions, gave it scale 0: MOVE -5
rendered `  00`. `PIC LLLPP` was accepted by both and rendered -300 as ` -3`.

**The fix.** `Validate` returns `PictureShape?`: the runs, which runs float, the floating string's occurrences, and the
leading/trailing 'P' counts. A 'P' string that is every digit position counts as leading, the reading
`DecimalPointPosition` already took. The analyzer reads its scale inputs from that shape. `ResolveFloating` sets
`EditRule.Floating` from `PictureShape.IsFloating` after the composition, and `SeparateFixedFromFloating` reads
`PictureShape.FloatingMarks`. `FloatingString`, `HasFloatingString`, `FloatingMarks`, the `IsDigitAnchor` classifier
and the now-uncalled `PictureSymbolRuns.Span` are deleted. The one digit-position list follows §2023:13.18.40.5 rule 6,
which names "the extended editing sign control symbols, if specified" among the floating insertion symbols. So the
list counts every floating run, the reading the analyzer already had. LLLPPV now binds and renders -300 as ` -3`;
LLPP99 is COBOL1934 under §2023:13.18.40.3 SR16; and LL99PPFF, accepted before with a garbage image, is SR16 too.

**Goldens.** `cases/13.18.40-PICTURE/pb3221_picture_p_beside_extended_float_2023` (LLLPPV -300 / 1200, LLLPP -300)
and `pb3221-picture-p-between-extended-float` (COBOL1934 at 2023; the below-2023 EDITING gate is pb490's). Units
`ThePSplit_CountsEveryFloatingStringsDigitPositions` and `APStringBesideAnExtendedFloatingString_IsBetweenDigitPositions`;
the two adjacency theories now read the shape. Inventory batch `w20262145c-pb3221-picture-shape`: SR-13.18.40.3-24
re-sites its code-location (two dead links retired) and restates its notes, and SR-16, SR-19 and GR-13.18.40.4-14 take
the new witnesses. GAP 2 -> 2. `docs/DATA_MODEL_DESIGN.md` names the shape.
