*>--- conformance-case
*> id: message-tag-refused-initialize-replacing-numeric
*> rules: [SR-14.9.20.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 14.9.20.3 SR4: "For each of the categories data-pointer, function-pointer, message-tag, object-
      *> reference, and program-pointer specified in the REPLACING phrase, a SET statement with identifier-2 as
      *> the sending operand and an item of the specified category as the receiving operand shall be valid."
      *> (cite.py --check 14.9.20.3 OK, SR 4.)
      *> REPLACING MESSAGE-TAG DATA BY a numeric item: SET T TO N is not valid.
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
           INITIALIZE T REPLACING MESSAGE-TAG DATA BY N
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDINITIALIZEREP.
