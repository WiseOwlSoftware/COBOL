## Entry 3802 — 2026-11-16 16:53 PST — PB5860 closed by the PB5815 fix: three 2023 rows re-recorded CONFORMS

kb/Work PB5860 recorded that a variable-length record sequential file written with ADVANCING or under LINAGE could not be read back through the file description that wrote it (OPEN INPUT answered 39 and the READ 47). The fix of kb/Work PB5815 closes it: OPEN INPUT and OPEN I-O of a record sequential print file now answer 37, as the file will not support read operations, so the read-back bound that PB5860 recorded was withdrawn. The three 2023 rows `GR-12.4.5.11.4-5`, `GR-14.9.27.4-10` and `GR-9.1.13.6-7` are re-recorded CONFORMS with PB5815's case `pb5815_print_file_not_read_back`.

