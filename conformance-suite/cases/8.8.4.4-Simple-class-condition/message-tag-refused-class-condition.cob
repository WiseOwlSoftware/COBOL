*>--- conformance-case
*> id: message-tag-refused-class-condition
*> rules: [SR-8.8.4.4.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 8.8.4.4.3 SR1: "Identifier-1 shall not reference a data item of class index, message-tag, object, or pointer, nor a strongly-typed group, nor a variable-length group." (cite.py --check 8.8.4.4.3 OK, SR 1.)
      *> The class condition IS NUMERIC is written over a message tag, which SR1 refuses: it can fail only if the operand is not classified as class message-tag.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDCLASSCONDITIO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF T IS NUMERIC DISPLAY "X" END-IF
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDCLASSCONDITIO.
