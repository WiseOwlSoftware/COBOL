      *>--- conformance-case
      *> id: pb4743-send-from-refmod-based
      *> rules: [SR-14.9.38.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO "SRV" FROM B(1:2) RETURNING MT, B being a BASED item. Expected: COBOL6266.
      *> ISO 8.4.3.3.4 GR5: reference modification "creates a unique data item that is a subset of the data item
      *> referenced by identifier-1", and GR6 gives it identifier-1's class, category and usage, so its data description
      *> entry is B's. 14.9.38.3 SR3: identifier-1 "shall not contain ... the BASED clause".
      *>   cite.py: OK  8.4.3.3.4 5)  OK  14.9.38.3 3)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 B       BASED PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM B(1:2) RETURNING MT
           END-SEND.
           STOP RUN.
