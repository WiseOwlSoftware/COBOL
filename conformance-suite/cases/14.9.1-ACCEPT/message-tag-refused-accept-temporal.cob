*>--- conformance-case
*> id: message-tag-refused-accept-temporal
*> rules: [SR-14.9.1.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 14.9.1.3 SR3: "Identifier-2 shall not reference a data item of class alphabetic, boolean,
      *> index, message-tag, object, or pointer." (cite.py --check 14.9.1.3 OK, SR 3.) Identifier-2 is the receiver of the
      *> temporal format, here ACCEPT T FROM TIME. The twin of pb139-accept-boolean-temporal for the message-tag class.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTACCTIME.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT T FROM TIME
           STOP RUN.
       END PROGRAM MTACCTIME.
