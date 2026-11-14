- **D-MCS-SORTKEY — a message tag is no sort key in any SORT or MERGE form** (kb/Work PB4740 review R1-3;
  DESIGN-message-control.md §2.3). The file forms refuse it by the printed rule: §2023:14.9.40.3 SR6 c) and §2023:14.9.24.3 SR4 c) say "Key data
  items shall not be of the class boolean, message-tag, object, or pointer" (COBOL1757). The table form's SR14 c) lists
  only "boolean, object, or pointer", but a key is ordered and §2023:8.8.4.2.14 gives two message-tag operands a comparison and
  no order ("An operand of class message-tag may be compared with another operand of class message-tag"), so the
  implementation refuses the key there too, on that ground, with the same code. Witnessed by `conformance:cases/14.9.40-SORT/message-tag-refused-sort-file-key`,
  `.../message-tag-refused-sort-table-key` and `conformance:cases/14.9.24-MERGE/message-tag-refused-merge-key`.
