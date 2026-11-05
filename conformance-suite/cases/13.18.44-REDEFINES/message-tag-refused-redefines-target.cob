*>--- conformance-case
*> id: message-tag-refused-redefines-target
*> rules: [SR-13.18.44.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 13.18.44.3 SR14: "Data-name-2 shall not be of class object, message-tag, or pointer, a strongly-typed group item, or an item subordinate to a strongly-typed group item." (cite.py --check 13.18.44.3 OK, SR 14.)
      *> The item redefined (data-name-2) is a message tag.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDREDEFINESTARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 U REDEFINES T PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CONTINUE
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDREDEFINESTARG.
