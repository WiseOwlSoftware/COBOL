## Suite 85 Stage 0: the coverage-point catalog of the 1985 general formats and its credit from WiseOwl's parse (PB6050)

The 85 suite's points denominator did not exist: `suite_status.py --edition 85` printed `points UNMEASURED`. This landing builds it
(design section 3.4, S14 and S15 cut to the 1985 figures, kb/Work PB6046 revision 4) and changes no test's expected behaviour.

**The catalog.** `harness/formats.py` reads one decided figure of `specs/ANSI_COBOL_1985.md` (the batch's drawing where the refuter
agreed, the refuter's own where it did not: `figures-1985/`) into a tree from its own delimiters: the multi-row brackets and braces
`figure_reading` pairs, and the typed pairs it spans across lines. Rows stacked inside a brace are alternatives; rows of a bracket
that do not begin in one column are one phrase wrapped. A level-2 box that holds only an ellipsis (`║ … ║`) repeats the portion
before it. `harness/points.py` maps each figure to its FMT row (the heading above it and its page: the clause numbers repeat across
modules) and derives the points; 213 rows, 252 formats, 3,846 points in `scripts/spec/data/format-points-1985.json`, with a digest.
Every tree is checked to carry exactly the words of its drawing (a word lost or doubled fails the build), which found the drawn
parentheses of the 42 function figures and a stray `. .`. Two figures whose typed braces open and close lines apart (EVALUATE; the
report group SIGN clause) are read by hand from the page in `formats.DECIDED_READINGS`, held to the same words check. Page 396's
heading prints `3.2.2.` with a full stop, which `sweep_figures`' heading reader does not match; `points.py` reads it.

**The credit.** `cobol points-batch` (`ParseNodeReader`) parses each test under its edition and prints, per parse node of the rules a
row names, its tokens: a nested statement is one token, a longer operand context is one token, a one-token operand is its token.
`coverage.Matcher` chart-parses each node whole against each format of the rows that name its rule and credits the points on a
derivation. Re-probes while building it moved the numbers: crediting from node text without nested entries, without the context
words a clause row prints before its keyword, with function names kept (`FUNCTION COS` lexes COS a user word) and with the
subscripts of a reference kept (`--flat`) took the credit from 1,851 to 2,882 points; deduplicating an optional word that is also an
alternative took 85 duplicate points out of the catalog. The record is `wiseowl/points/<test id>.json` as point-number ranges (ids
weighed 16 MB; ranges 1.7 MB) with `wiseowl/points-inputs.json` holding the catalog's digest, the reading's, and each test's text
digest; `coverage.unmet` raises rather than count a record that is not a measure of the tests as they stand.

**Measured.** 3,846 points; 2,852 credited, 994 owed. The rows no parse node carries keep every point owed and the report names
them: COPY and REPLACE (the preprocessor reads them), a section header, a nonnumeric literal (one lexer token), the four
division-structure rows. Alternatives whose operands share a token class (`identifier-1` and `index-name-1`) are credited together
until the operand-kind map (S15's remainder) names their grammar elements.

**Tests.** Self-tests `selftests/formats` (12 cases), `points` (10) and `coverage` (16: the matcher on planted formats, the operand
kinds, the frames, the record's encoding, the loud failures); `PointsBatchTests` (9, the reader and the verb). No golden: nothing
compiled changes. **Self-review** (architecture, full code, performance, duplication): one mechanism per job (reading, points,
credit, parse reading), no C# text in a bound node, the grammar's rule names owned by the caller so none is written into the compiler;
the matcher memoises its chart and the record dedupes node shapes (4,565 of 113,668 nodes). **Gate:** the verdict line is in the
branch report.
