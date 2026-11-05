*>--- conformance-case
*> id: message-tag-refused-picture
*> rules: [SR-13.16.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 13.16.3 SR8 lists USAGE MESSAGE-TAG among the usages whose item has no PICTURE clause: the PICTURE
      *> clause shall not be specified for an item whose usage is message-tag.
      *> The entry writes both.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDPICTURE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CONTINUE
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDPICTURE.
