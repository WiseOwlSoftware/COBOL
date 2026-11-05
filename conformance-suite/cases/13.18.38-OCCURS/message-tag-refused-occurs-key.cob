*>--- conformance-case
*> id: message-tag-refused-occurs-key
*> rules: [SR-13.18.38.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 13.18.38.3 SR8: "The KEY phrase shall not be specified for a data item of class boolean,
      *> message-tag, object, or pointer." (cite.py --check 13.18.38.3 OK, SR 8.) K is a message tag under the table element
      *> E of a strongly-typed table (the placement 13.18.60.3 SR14 permits), named by ASCENDING KEY. The twin of
      *> pb1263-occurs-key-boolean for the message-tag class.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTOCCKEY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT IS TYPEDEF STRONG.
          05 E OCCURS 3 ASCENDING KEY K INDEXED BY IX.
             10 K USAGE MESSAGE-TAG.
       01 R TYPE RT.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
       END PROGRAM MTOCCKEY.
