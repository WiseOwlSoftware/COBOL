*>--- conformance-case
*> id: message-tag-refused-redefines-subject
*> rules: [SR-13.18.44.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 13.18.44.3 SR12: "The REDEFINES clause shall not be specified for a data item of class object, message-tag, or pointer or a strongly-typed group item." (cite.py --check 13.18.44.3 OK, SR 12.)
      *> The subject of the REDEFINES entry is a message tag.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDREDEFINESSUBJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U PIC X(8).
       01 T REDEFINES U USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CONTINUE
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDREDEFINESSUBJ.
