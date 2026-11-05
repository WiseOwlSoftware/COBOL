*>--- conformance-case
*> id: message-tag-refused-max-argument
*> rules: [AR-15.59.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 15.59.3 rule 1 (FUNCTION MAX): "Argument-1 shall not be of class boolean, message-tag, object, or
      *> pointer, nor shall it be a strongly-typed group item." (cite.py --check 15.59.3 OK, rule 1.)
      *> Both arguments are message tags.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDMAXARGUMENT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY FUNCTION MAX(T T)
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDMAXARGUMENT.
