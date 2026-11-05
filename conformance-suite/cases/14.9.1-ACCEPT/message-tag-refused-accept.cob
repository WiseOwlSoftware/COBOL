*>--- conformance-case
*> id: message-tag-refused-accept
*> rules: [SR-14.9.1.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 14.9.1.3 SR1: "Identifier-1 shall reference neither a strongly-typed group item nor a data item of
      *> class index, message-tag, object, or pointer." (cite.py --check 14.9.1.3 OK, SR 1.)
      *> ACCEPT into a message tag is refused. Before PB4740 it reached the generated C# as a string store into
      *> the MessageTag struct.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDACCEPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           ACCEPT T
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDACCEPT.
