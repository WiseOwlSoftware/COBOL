*>--- conformance-case
*> id: message-tag-refused-relation-ordering
*> rules: [GR-8.8.4.2.1-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 8.8.4.2.2 Format 3 (message-tag-object-or-pointer-reference) prints only the equality operators, so
      *> an ordering relation over message tags is a format violation; 8.8.4.2.14: "An operand of class message-
      *> tag may be compared with another operand of class message-tag" for equality only.
      *> The relation T > NULL is refused.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDRELATIONORDER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF T > NULL DISPLAY "X" END-IF
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDRELATIONORDER.
