*>--- conformance-case
*> id: message-tag-refused-initialize-replacing-literal
*> rules: [SR-14.9.20.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 14.9.20.3 SR3: "For each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2 shall be specified" (cite.py --check 14.9.20.3 OK, SR 3.)
      *> REPLACING MESSAGE-TAG DATA BY a literal: identifier-2 is not specified.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDINITIALIZEREP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           INITIALIZE T REPLACING MESSAGE-TAG DATA BY "X"
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDINITIALIZEREP.
