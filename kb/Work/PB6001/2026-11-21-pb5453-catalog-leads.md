## 2026-11-21 — the PB5453 reads' catalog leads join this note (one mechanism: the 1985 catalog's segmentation)

The 1985 read fleet (kb/Work PB5453; triage `E:/COBOL-coord/scratch/pb5453/out/leads-triage.md`) reported these
segmentation and catalog leads. Each is a fact of the 1985 rule catalog or its transcription, read in slice order, and
none changes a compiled program; they are re-segmented with this note's units.

**Sites:** `scripts/spec/rule_catalog_1985.py` (`_rules_of`, list-item minting, box assignment),
`docs/rearchitecture/spec-rule-catalog-1985.json`, `docs/rearchitecture/spec-splits-1985/split-batch.json`,
`specs/ANSI_COBOL_1985.md` (the 1989 amendment's headings).

1. A list-closing paragraph, general format or figure is folded into the last list item's row (both catalogs).
2. Paragraphs and list items never minted as rows, or left with no row after a split
   (`split-batch.json#1985-SR-IV.4.2.2.2.3-L3.3`).
3. Lead-in sentences minted as their own SR/GR rows although their requirement is their items' (slices s-01, s-02,
   s-08; e.g. `1985-SR-IV.4.2.2.1-p2`, read again in s-99).
4. `box: whole` assigned though only one sentence of the paragraph is boxed.
5. Over-minting: each bare-word item of the five X.1.3.8 lists is a row though lead and items state one requirement; SR
   and GR lead kinds mixed in one clause.
6. Kind inconsistency: the Description-subclause rows are RV in A.2.36.1 to A.2.46.1 and GR in A.2.26.1 to A.2.35.1.
7. After a split, suite cases keep their `rules:` tags, code-location and test-ref on the parent row (five IV.4.3.8.1
   cases; `1985-SR-XVI_3_3_3-2` is p2's case but tagged -2).
8. A spurious `definitional` flag on `1985-GR-VI.6.3.1.2-p1` (its five class tests are observable).
9. The 1989 amendment: `specs/ANSI_COBOL_1985.md` writes `**2.2 ARGUMENTS**` as bold text, not a heading, so cite.py
   names the clause `2.2 ARGUMENTS**`, and the view gives the rows the subject `MAINTENANCE COMMITTEE`.
10. The VII.1.2-style LEVEL CHARACTERISTICS paragraphs and the other presentation-only paragraphs no derivation arm fits
    (the eleven DOUBTFUL rows the second read left for a ruling, kb/Work PB5453's import addendum) are a disposition
    question of the same catalog: a not-a-rule disposition would retire them here.
