*>--- conformance-case
*> id: message-tag-refused-set-numeric-receiver
*> rules: [SR-14.9.39.3-35]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 14.9.39.3 SR35: "Data-name-4 and data-name-5 shall be message-tag data items." (cite.py --check
      *> 14.9.39.3 OK, SR 35.)
      *> The receiver data-name-4 is a numeric item and the sender is a message tag.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDSETNUMERICREC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET N TO T
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDSETNUMERICREC.
