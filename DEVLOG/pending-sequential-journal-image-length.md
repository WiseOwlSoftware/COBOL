## The sequential journal reads an R record's length word in 64 bits, so a wrapped or oversized word is a torn tail (PB3614)

PB3614 was filed against two sizing sites of SequentialJournal that the PB3340 review rounds had already replaced: `Parse`'s
`at + 1 + payload + 4 > bytes.Length` bound and `Read`'s `checked((int)medium.Length(record))` no longer exist, because
`ReadRecord` now streams the record file through a `BufferedStream`. The re-probe on this build therefore discharged the 2 GiB arm
(a sparse 3 GiB stream is read like any other) and found the same mechanism alive in the streaming reader: `NextRecord`'s `R` arm
allocated `new byte[1 + 20 + length + 4]` from the untrusted length word. A word of `int.MaxValue`, `int.MaxValue - 10` or
`int.MaxValue - 24` threw `OverflowException` (the int sum wraps), `int.MaxValue - 60` threw `OutOfMemoryException`, and either
escaped `ReadRecord`, so a dead transaction's next entry step could not roll it back.

The length word is now judged in `long` arithmetic against `Array.MaxLength` and against the bytes the record file still holds,
before anything is allocated; a word that fails is a torn tail, which 2.2.2.4 already says ENDS the records, so the settle
restores F to its state at the last COMMIT (§14.9.36.4 GR3). Unit witnesses: six length words (wrapping, oversized, negative)
through `ReadRecord`, a sparse 3 GiB record file, and a dead transaction's record with a wrapped word settled by the next entry
(the I11 exception path of the rollback). The sibling readers (`RecordFraming.ReadFrame`, `SortRunFile.Next`) were checked: the
first bounds every length by `Holds` already, the second reads a file the same process wrote. Gate: see the report.
