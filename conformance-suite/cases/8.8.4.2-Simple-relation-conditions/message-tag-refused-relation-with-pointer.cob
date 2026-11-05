*>--- conformance-case
*> id: message-tag-refused-relation-with-pointer
*> rules: [SR-8.8.4.2.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 8.8.4.2.3 SR5: "Identifier-3 and identifier-4 shall reference data items of class message-tag, object, or pointer, and shall be of the same category." (cite.py --check 8.8.4.2.3 OK, SR 5.)
      *> A message tag is compared with a data pointer: both are of the three classes but they are of different categories.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDRELATIONWITHP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF T = P DISPLAY "X" END-IF
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDRELATIONWITHP.
