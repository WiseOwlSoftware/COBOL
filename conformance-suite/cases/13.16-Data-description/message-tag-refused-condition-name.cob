*>--- conformance-case
*> id: message-tag-refused-condition-name
*> rules: [SR-13.16.3-24, SR-13.18.60.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 13.16.3 SR24 e): "A data item of the class index, message-tag, object, or pointer." is not a conditional variable; 13.18.60.3 SR11 states it from the usage side.
      *> A level-88 entry follows a message tag.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDCONDITIONNAME.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
          88 T-OK VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CONTINUE
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDCONDITIONNAME.
