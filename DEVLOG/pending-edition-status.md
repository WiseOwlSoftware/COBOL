## Edition status: "is edition N complete?" is now a computed answer

kb/Work PB5924 step 1. `scripts/spec/edition_status.py --edition {1985,2002,2014,2023} [--json]` measures four parts and
prints `=== EDITION <N>: COMPLETE ===` only when all four are zero: the inventory GAP (`inventory_view.gaps`), the code reads
owed a read, owed a re-read, contradicted or undecided (`code_reads`, called as functions; the newest edition has no store,
said and counted 0), the open `kb/Work` notes whose frontmatter touches the edition (read by `work.load`, one pattern
table, split HARM, QUESTION, TOOLING, OTHER), and the catalog prose holes. A part that cannot be measured (2014 is not a held
edition; prose holes exist for 1985 only) prints `not measured` and makes the verdict NOT COMPLETE, never a zero.

`scripts/spec/catalog_prose_holes.py` is the scratch measurement moved into the repository over the 1985 catalog reader's own
structure readers: 198 body sentences of the 1985 transcription are the text of no catalog row (41 more in Appendix C, a
summary). At this head 1985 reads `NOT COMPLETE — 93 open work notes · 198 catalog prose holes`. Self-test:
`scripts/selftests/spec/edition_status/` (the verdict on planted sources, a planted register, a planted transcription).
The brief asked for a `--self-test` flag; the repository's layout rule (`selftest_layout.py`) refuses a subject that
handles one, so the cases live in the tests directory as every converted tool's do.
