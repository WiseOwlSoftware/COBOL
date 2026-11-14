- **D-MCS-ARG — a message-tag argument is checked like a pointer** (kb/Work PB4740; DESIGN-message-control.md §2.3). §2023:14.8.2.3.2
  says "If neither the formal parameter nor the argument is of class message-tag, object, or pointer, the conformance rules are
  the following", and gives class pointer its own rule ("shall be of class pointer and the corresponding items shall be of the
  same category"), leaving the message tag with no rule of its own: the standard names it only to exclude it from the length
  rules. The implementation applies the pointer rule to it: a message tag argument conforms to a message-tag formal and to no
  other class, and the mismatch is EC-PROGRAM-ARG-MISMATCH at run time wherever no prototype makes it checkable. BY REFERENCE
  and BY CONTENT pass the slot, and a BY VALUE formal (§2023:14.2.2 SR2) and an INVOKE BY VALUE argument (§2023:14.9.23.3 SR15) copy the
  struct; a CALL BY VALUE argument is refused by §2023:14.9.4.3 SR22 ("class numeric, object, or pointer"), which does not name the
  message-tag class. Witnessed by `conformance:cases/14.8.2.3.2-Elementary-items-passed-by-reference/message-tag-call-by-reference`,
  and for the mismatch arms by `message-tag-call-argument-mismatch` (EC-PROGRAM-ARG-MISMATCH at run time) and
  `message-tag-refused-call-prototype-argument` (COBOL1688 where a prototype makes the pairing checkable).
