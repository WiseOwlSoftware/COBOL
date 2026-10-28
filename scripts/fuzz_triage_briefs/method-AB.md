1. Read every signature's first program (`programs/<id>.cob`) and its differing lines; decide the signature with ONE
   verdict from the vocabulary; checkpoint it. Signatures of 1 row are in scope too.
2. A difference that is one of an earlier record's classes: name the row; do not re-derive.
3. A candidate `WISEOWL-DEFECT`: reduce to the smallest program, derive the expected output from the cited rule, run the
   reduction on the fresh judge (and GnuCOBOL if it helps separate latitude from error), find the code site, search
   `kb/Work` before calling it new.
4. A difference that is GnuCOBOL's (its output contradicts the cited rule while ours follows it): `GNUCOBOL-SIDE`, with its
   `upstream` object and record per common.md.
