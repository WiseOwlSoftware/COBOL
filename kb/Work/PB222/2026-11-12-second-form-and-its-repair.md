## 2026-11-12 — a second form of the guess, and where the repair lands (kb/Work PB5449)

On origin/main fce1604531 the walk-back is gone but the same answer comes from the catalog's reading: `cite.py --check
"§2023:8.8.4.2.1" "A relation condition shall contain at least one reference to an operand that is not a literal"`
prints "it lies in GR13", and the §2023:14.8.2.2 variable-length sentence "lies in SR2", because the extractor appends
a margin paragraph to the rule above it and `rule_book` takes the row's extent. Measured on the PDF while designing kb/Work
PB479, 121 such margin paragraphs sit inside 57 rows. kb/Work PB5449 repairs both: the margin levels decide where a
rule ends, and a quotation in a margin paragraph answers `(unnumbered prose, pending)` or its own `p<n>`, never the
rule above.
