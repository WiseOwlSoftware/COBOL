⚠ **A warning nothing asserts is a warning that can silently stop firing**, and that is why the WARNING band
needs assertions the corpora structurally cannot make: the negative corpus matches ERROR codes only and the
`.out` goldens capture stdout, while a warning rides the non-failing compile channel.
`DocumentedNonSupportWitnessTests` is where those assertions live — it pins COBOL1580 (item 3) on the same
sources the positive corpus runs (COBOL1578, item 1, MCS, was retired with the facility's implementation, kb/Work PB4744). Items 2 and 4 are not exposed to this at all: a refusal fails the
compile, so item 2's witness is a negative-corpus entry and item 4's 41 witnesses are ordinary negative-corpus entries
(`a42-*` in `conformance-suite/wiseowl/cases/`), each refusal's expected text naming the construct as well as the code.

⚠ **Two further modules are Not claimed and have no item here** — A.4.8 (FORMAT / SELECT WHEN) and A.4.10's declined optional items
(A.4.13, REWRITE FILE / WRITE FILE, is Claimed and its §5 row is its user documentation). For those the §5 Annex A.4 row IS the whole of
the §2023:4.2.7 user documentation, and it says so in the row.
