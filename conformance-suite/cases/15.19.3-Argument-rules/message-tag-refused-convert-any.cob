*>--- conformance-case
*> id: message-tag-refused-convert-any
*> rules: [AR-15.19.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 15.19.3 rule 7: "When the source-format is ANY, argument-1 shall be of any usage, except
      *> index, message-tag, object reference, pointer, function-pointer or program-pointer." (cite.py --check 15.19.3 OK,
      *> rule 7.) The binder's screen named five of the six usages and had no arm for the message tag, so the program was
      *> accepted and aborted at run time asking for the tag's raw storage image, which has none.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTCONVANY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       01 S PIC X(16).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CONVERT(T ANY ANUM HEX) TO S
           STOP RUN.
       END PROGRAM MTCONVANY.
