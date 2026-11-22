## 1985 code reads re-read after #1946 moved their members (slice 1985-c06); PB6088 filed

PR #1946, a behavior-neutral PB1754 restructuring, removed `CobolTable.cs#OccScaled` and `CobolString.cs#RefModPosition`.
Two stored 1985 code reads named them, so `edition_status.py --edition 1985` read NOT COMPLETE on main at 9bfe9e1c44
(2 code reads). Slice 1985-c06 re-read both rows at 9bfe9e1c44. A cobol-adjudicator read them, a cobol-refuter tried to
overturn them, and both came out CONFORMS and upheld: 1985-GR-IV.4.3.8.2.4-2 (an index holds the occurrence number) and
1985-SR-IV.4.3.8.4.3-2 (reference-modification bounds are arithmetic expressions). `code_reads.py import --replace`
re-pins them to `SubscriptBinder.cs` and `CobolPosition.cs`. After the import, `code_reads.py --check 1985` is GREEN and
the code-read term is 0. Both readers found that at `--std 85`, COBOL1637 in a reference-modification bound cites
§1985:VI.5.8.3 SR13 instead of the bounds rule. That is filed as PB6088 (misleads_diagnosis, editions [1985]). The root
of the regression, a landing allowed to un-pin a COMPLETE edition's reads, is PB6056 (wave 20262138 group B).
