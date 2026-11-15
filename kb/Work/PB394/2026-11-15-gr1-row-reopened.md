## 2026-11-15: GR-14.9.25.4-1 is open again, for kb/Work PB5678

The sentences this note fixed still conform, and every witness it added still passes. `GR-14.9.25.4-1`
(§2023:14.9.25.4 GR1) is now recorded DIVERGES because of a different failure of the same intermediate, kb/Work
PB5678. When an alphanumeric-edited or national-edited sender is moved to two or more receivers, the frozen temporary
takes the sender's edited description, so the move into the temporary edits the content a second time. At
`--std 2023`, `MOVE SXE (PIC XBX "A B") TO RX RX2` stores `[A    ]` in both receivers.

The row is therefore removed from this note's `closes_rows`, because `work.py check` refuses a closed-row claim on a
row the inventory counts as GAP. PB5678 holds the row in its `inventory_rows`, and PB5678's landing re-closes it.
