## PB5000 - a merge landing is credited per row with the commits that changed the row

`backfill_closes_rows.closures_from_history` credited a merge landing with the messages of every commit it brought in,
so attribution arm 3 (a message naming exactly one note) failed as soon as any other brought-in commit named a second
note. Re-probe on this tree: PB2104's landing (c36dfcbb2a) brought in PB2103 commits, and five of its rows were missed.

Fix: `brought_in` reads the merge's commits with the inventory paths each changed, and `message_for_row` credits a row
with the messages of the commits that changed that row's own store file (a commit touching the pre-PB4148 monolith counts
for every row; a row no commit touched keeps the old all-messages evidence). Measured against the 2,500 hand-written
`closes_rows`: rows missing from the derivation 370 to 366, rows it adds that no note claims 518 to 510. PB2104 now
derives in full. PB1086 at a97ddfb80f stays "differs": its closing commit names PB1086 and PB4185 itself, which no
reading of the history can divide, so it is not this mechanism. The self-test gained a merge whose other commits name
a second note and touch another row.

Self-review: the legacy-file branch is the only place a commit that did not change the row is credited to it, and only for
history before kb/Work PB4148; there the old noise is unchanged. Gate: see the landing's record.
