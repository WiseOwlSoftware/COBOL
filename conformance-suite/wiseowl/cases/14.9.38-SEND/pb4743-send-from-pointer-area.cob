      *>--- conformance-case
      *> id: pb4743-send-from-pointer-area
      *> rules: [SR-14.9.38.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO "SRV" FROM P RETURNING MT, P being USAGE POINTER. Expected: COBOL6266.
      *> ISO 14.9.38.3 SR3: identifier-1 "shall not contain ... the POINTER clause" (the clause list continues with the
      *> OBJECT-REFERENCE, FUNCTION-POINTER and PROGRAM-POINTER clauses, which the conformance tests pin as well).
      *>   cite.py: OK  14.9.38.3 3)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       01 P       USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM P RETURNING MT
           END-SEND.
           STOP RUN.
