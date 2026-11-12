## 2026-11-12 — a second form of the guess, and where the repair lands (kb/Work PB5449 and PB5451)

On origin/main fce1604531 the walk-back is gone but the same answer comes from the catalog's reading: `cite.py --check
"§2023:8.8.4.2.1" "A relation condition shall contain at least one reference to an operand that is not a literal"`
prints "it lies in GR13", and the §2023:14.8.2.2 variable-length sentence "lies in SR2", because the extractor appends
a margin paragraph to the rule above it and `rule_book` takes the row's extent. Measured on the PDF while designing kb/Work
PB479, 121 such margin paragraphs sit inside 57 rows. The repair lands in two steps. kb/Work PB5449 makes a quotation in
prose that lies in no row (a prose clause, a dropped lead-in) answer `(unnumbered prose, pending)` or its own `p<n>`,
never the rule above, and leaves every row's extent as it is. kb/Work PB5451 carves the glued margin paragraphs out of
their rows, which removes the "it lies in GR13" and "it lies in SR2" answers, and re-points the stored citations that
relied on them in the same landing, so the citation audit never goes red between the two.
