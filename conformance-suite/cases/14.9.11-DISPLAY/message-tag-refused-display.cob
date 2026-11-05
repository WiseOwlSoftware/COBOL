*>--- conformance-case
*> id: message-tag-refused-display
*> rules: [SR-14.9.11.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 14.9.11.3 SR1: "Identifier-1 shall not reference a data item of class message-tag, object, or
      *> pointer." (cite.py --check 14.9.11.3 OK, SR 1.)
      *> DISPLAY of a message tag is refused; it can fail only if the class lattice does not decide a message tag.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDDISPLAY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 N PIC 9(4).
       01 U PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY T
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDDISPLAY.
