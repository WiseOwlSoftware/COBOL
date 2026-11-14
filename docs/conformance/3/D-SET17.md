- **D-SET17 — SET Format 17's receiver is data-name-4 in both general rules** (kb/Work PB4740; DESIGN-message-control.md
  §0 Q1, owner-approved 2026-11-04; a defect in the printed standard, recorded here and not coded around). The printed
  format is `SET data-name-4 TO { data-name-5 | NULL }`, which makes data-name-4 the receiver. §2023:14.9.39.4 GR40 reads "If
  data-name-5 is specified, the content of data-name-4 is moved to data-name-5" and GR41 "If NULL is specified, the content of
  data-name-5 shall be set to that of the null value"; GR41 names an operand that is not written when NULL is, and GR40 moves
  in the direction opposite to the format. The format, every other SET format and §2023:14.9.39.1 ("setting the content of a
  message-tag data item to NULL or another message-tag") agree on the receiver, so GR40 and GR41 are read with data-name-4 as the
  receiver: `SET T1 TO T2` copies the content of T2 into T1, and `SET T1 TO NULL` makes T1 the null content
  (`SetBinder.BindMessageTagSet`, `SetEmitter.EmitSetMessageTag`). The printed format has one receiving operand, so a list of
  receivers is refused (COBOL0869). Witnessed by `conformance:cases/14.9.39-SET/set-message-tag`.
